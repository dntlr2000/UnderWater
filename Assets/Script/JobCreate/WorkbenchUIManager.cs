using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkbenchUIManager : MonoBehaviour
{
    public static WorkbenchUIManager Instance;

    [Header("Main Panel")]
    public GameObject window;
    public TMP_Text benchTitleText;
    public Button closeButton;

    [Header("List Area")]
    public Transform contentParent;
    public GameObject itemPrefab;

    [Header("Detail Area")]
    public Image detailIcon;
    public TMP_Text detailTitleText;
    public TMP_Text detailDescText;
    public TMP_Text detailTimeText;

    [Header("Costs")]
    public Transform costsParent;
    public GameObject costSlotPrefab;

    [Header("Rewards & Action")]
    public TMP_Text rewardsText;
    public Button actionButton;
    public TMP_Text actionButtonText;

    [Header("Progress UI")]
    public Slider localProgressBar;

    private UIController cachedUIController;
    private WorkbenchData currentData;

    private WorkbenchManager activeBench;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (window != null) window.SetActive(false);
        cachedUIController = FindAnyObjectByType<UIController>();

        if (closeButton != null) closeButton.onClick.AddListener(CloseUI);
    }

    public void OpenUI(string benchName, List<WorkbenchData> dataList, WorkbenchManager bench)
    {
        window.SetActive(true);
        benchTitleText.text = benchName;
        activeBench = bench;

        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(false);
            cachedUIController.LockCursor(false);
        }

        foreach (Transform child in contentParent) Destroy(child.gameObject);

        foreach (WorkbenchData data in dataList)
        {
            GameObject newSlot = Instantiate(itemPrefab, contentParent);
            newSlot.GetComponentInChildren<TMP_Text>().text = data.displayName;
            newSlot.GetComponent<Button>().onClick.AddListener(() => ShowDetail(data));
        }

        if (dataList.Count > 0) ShowDetail(dataList[0]);
    }

    private void Update()
    {
        if (window.activeSelf && activeBench != null)
        {
            if (activeBench.isWorking && currentData == activeBench.currentTaskData)
            {
                if (localProgressBar != null)
                    localProgressBar.value = activeBench.currentTimer / activeBench.maxTime;
            }

            if (activeBench.isRewardReady && currentData == activeBench.currentTaskData && !actionButton.interactable)
            {
                RefreshDetail();
            }
        }
    }

    public void ShowDetail(WorkbenchData data)
    {
        currentData = data;
        foreach (Transform child in costsParent) Destroy(child.gameObject);

        bool canExecute = true;

        if (data is CookingRecipe cook)
        {
            SetDetailText(cook.displayName, cook.recipeDescription, cook.cookTime, cook.icon);
            canExecute = DrawIngredientSlots(cook.ingredients);
            rewardsText.text = $"[완성품] (ID: {cook.resultItemID}) x{cook.resultAmount}";
        }
        else if (data is EquipRecipe equip)
        {
            SetDetailText(equip.displayName, equip.recipeDescription, equip.craftTime, equip.icon);
            canExecute = DrawIngredientSlots(equip.ingredients);
            rewardsText.text = $"[완성품] 장비 (ID: {equip.resultEquipID}) x{equip.resultAmount}";
        }
        else if (data is CollectionData collection)
        {
            SetDetailText(collection.displayName, collection.collectionDescription, collection.researchTime, collection.icon);
            canExecute = DrawIngredientSlots(collection.requiredItems);
            rewardsText.text = $"[연구 보상]\n단서 ID: {collection.rewardStoryItemID} / 보너스 스탯: +{collection.rewardBonusStat}";
        }
        else if (data is GymExercise gym)
        {
            SetDetailText(gym.displayName, gym.exerciseDescription, gym.exerciseTime, gym.icon);
            canExecute = DrawStatCostSlots(gym.costHunger, gym.costWater);
            rewardsText.text = $"[운동 효과]\n최대 체력 +{gym.bonusMaxHP} / 스테미너 +{gym.bonusMaxStamina}";
        }

        actionButton.gameObject.SetActive(true);
        actionButton.onClick.RemoveAllListeners();

        if (activeBench.currentTaskData == data)
        {
            if (activeBench.isWorking)
            {
                actionButton.interactable = false;
                actionButtonText.text = "진행 중...";
                if (localProgressBar) localProgressBar.gameObject.SetActive(true);
            }
            else if (activeBench.isRewardReady)
            {
                actionButton.interactable = true;
                actionButtonText.text = "보상 받기";
                if (localProgressBar) localProgressBar.gameObject.SetActive(false);
                actionButton.onClick.AddListener(() => activeBench.ClaimReward());
            }
        }
        else if (activeBench.isWorking || activeBench.isRewardReady)
        {
            actionButton.interactable = false;
            actionButtonText.text = "다른 작업 진행 중";
            if (localProgressBar) localProgressBar.gameObject.SetActive(false);
        }
        else
        {
            actionButton.interactable = canExecute;
            actionButtonText.text = "시작하기";
            if (localProgressBar) localProgressBar.gameObject.SetActive(false);
            actionButton.onClick.AddListener(() => activeBench.StartCraftingProcess(data));
        }
    }

    public void RefreshDetail()
    {
        if (currentData != null) ShowDetail(currentData);
    }

    private void SetDetailText(string title, string desc, float time, Sprite icon)
    {
        detailTitleText.text = title;
        detailDescText.text = desc;
        detailTimeText.text = $"소요 시간: {time}초";
        if (detailIcon && icon != null) detailIcon.sprite = icon;
    }

    private bool DrawIngredientSlots(List<RecipeIngredient> ingredients)
    {
        bool hasAllItems = true;
        Inventory myInventory = FindAnyObjectByType<Inventory>();

        foreach (var ing in ingredients)
        {
            GameObject slot = Instantiate(costSlotPrefab, costsParent);

            int ownedAmount = myInventory != null ? myInventory.GetOwnedItemCount(ing.itemID) : 0;
            if (ownedAmount < ing.requiredAmount) hasAllItems = false;

            slot.transform.Find("NameText").GetComponent<TMP_Text>().text = ing.itemName;
            if (ing.itemIcon) slot.transform.Find("Icon").GetComponent<Image>().sprite = ing.itemIcon;

            string color = ownedAmount >= ing.requiredAmount ? "#FFFFFF" : "#FF0000";
            slot.transform.Find("CountText").GetComponent<TMP_Text>().text = $"<color={color}>{ownedAmount}</color> / {ing.requiredAmount}";
        }
        return hasAllItems;
    }

    private bool DrawStatCostSlots(float hunger, float water)
    {
        bool hasEnoughStats = true;
        float currentHunger = Player.localPlayer != null ? Player.localPlayer.condition.hunger : 100f;
        float currentWater = Player.localPlayer != null ? Player.localPlayer.condition.thirst : 100f;

        if (hunger > 0)
        {
            GameObject slot = Instantiate(costSlotPrefab, costsParent);
            slot.transform.Find("NameText").GetComponent<TMP_Text>().text = "배고픔 소모";
            string color = currentHunger >= hunger ? "#FFFFFF" : "#FF0000";
            slot.transform.Find("CountText").GetComponent<TMP_Text>().text = $"<color={color}>{currentHunger}</color> / {hunger}";
            if (currentHunger < hunger) hasEnoughStats = false;
        }
        if (water > 0)
        {
            GameObject slot = Instantiate(costSlotPrefab, costsParent);
            slot.transform.Find("NameText").GetComponent<TMP_Text>().text = "수분 소모";
            string color = currentWater >= water ? "#FFFFFF" : "#FF0000";
            slot.transform.Find("CountText").GetComponent<TMP_Text>().text = $"<color={color}>{currentWater}</color> / {water}";
            if (currentWater < water) hasEnoughStats = false;
        }
        return hasEnoughStats;
    }

    public void CloseUI()
    {
        window.SetActive(false);
        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(true);
            cachedUIController.LockCursor(true);
        }
    }
}