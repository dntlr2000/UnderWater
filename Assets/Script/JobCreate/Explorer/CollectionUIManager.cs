using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionUIManager : MonoBehaviour
{
    public static CollectionUIManager Instance;

    [Header("Main Panel")]
    public GameObject collectionWindow;

    [Header("List")]
    public Transform Col_contentParent;
    public GameObject Col_ItemPrefab;

    [Header("Info UI")]
    public Text Col_Text;

    [Header("Detail Area")]
    public Image Col_detailIcon;
    public TMP_Text Col_titleText;
    public TMP_Text Col_descriptionText;
    public TMP_Text Col_timeText;

    [Header("Detail Area - Requirements")]
    public Transform Col_materialsParent;
    public GameObject Col_materialSlotPrefab; // 재료 슬롯 프리팹 재활용

    [Header("Detail Area - Result")]
    public TMP_Text Col_rewardsText;
    public Button Col_researchButton; // 제작/요리 대신 '연구하기' 버튼

    private CollectionData currentSelectedCollection;
    private UIController cachedUIController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (collectionWindow != null) collectionWindow.SetActive(false);
        if (Col_researchButton != null) Col_researchButton.gameObject.SetActive(false);

        cachedUIController = FindAnyObjectByType<UIController>();
    }

    public void OpenUI(List<CollectionData> collections, bool isSpecialist)
    {
        collectionWindow.SetActive(true);

        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(false);
            cachedUIController.LockCursor(false);
        }

        Col_Text.text = isSpecialist ? "전문가용 수집 보관함" : "수집 보관함 (기본 열람)";

        foreach (Transform child in Col_contentParent) Destroy(child.gameObject);

        foreach (CollectionData collection in collections)
        {
            GameObject newSlot = Instantiate(Col_ItemPrefab, Col_contentParent);

            TMP_Text text = newSlot.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = collection.displayName;

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowCollectionDetail(collection));
        }

        if (collections.Count > 0) ShowCollectionDetail(collections[0]);
        else ClearCollectionDetail();
    }

    private void ShowCollectionDetail(CollectionData collection)
    {
        currentSelectedCollection = collection;

        Col_titleText.text = collection.displayName;
        Col_descriptionText.text = collection.collectionDescription;
        if (Col_timeText) Col_timeText.text = $"연구 소요 시간: {collection.researchTime}초";
        if (Col_detailIcon && collection.icon != null) Col_detailIcon.sprite = collection.icon;

        foreach (Transform child in Col_materialsParent) Destroy(child.gameObject);

        bool canResearch = true;

        foreach (RecipeIngredient ingredient in collection.requiredItems)
        {
            GameObject matSlot = Instantiate(Col_materialSlotPrefab, Col_materialsParent);

            int ownedAmount = ingredient.requiredAmount;

            Image iconImg = matSlot.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text nameTxt = matSlot.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text countTxt = matSlot.transform.Find("CountText")?.GetComponent<TMP_Text>();

            if (iconImg) iconImg.sprite = ingredient.itemIcon;
            if (nameTxt) nameTxt.text = ingredient.itemName;

            string colorHex = (ownedAmount >= ingredient.requiredAmount) ? "#FFFFFF" : "#FF0000";
            if (countTxt) countTxt.text = $"<color={colorHex}>{ownedAmount}</color> / {ingredient.requiredAmount}";
        }

        // 보상 텍스트 조립
        string rewardStr = "[연구 보상]\n";
        if (collection.rewardStoryItemID > 0) rewardStr += $"스토리 단서 (ID: {collection.rewardStoryItemID})\n";
        if (collection.rewardBonusStat > 0) rewardStr += $"탐험 스탯 +{collection.rewardBonusStat}";
        if (collection.rewardStoryItemID == 0 && collection.rewardBonusStat == 0) rewardStr += "알려진 보상 없음";

        Col_rewardsText.text = rewardStr;

        Col_researchButton.gameObject.SetActive(true);
        Col_researchButton.interactable = canResearch;

        Col_researchButton.onClick.RemoveAllListeners();
        Col_researchButton.onClick.AddListener(() =>
        {
            StartResearch(collection);
        });
    }

    private void StartResearch(CollectionData collection)
    {
        Debug.Log($"[{collection.displayName}] 연구 시작! 수집품을 조합하여 보상을 얻습니다.");
    }

    private void ClearCollectionDetail()
    {
        currentSelectedCollection = null;
        Col_titleText.text = "선택된 수집품 없음";
        Col_descriptionText.text = "";
        if (Col_timeText) Col_timeText.text = "";
        if (Col_detailIcon) Col_detailIcon.sprite = null;

        foreach (Transform child in Col_materialsParent) Destroy(child.gameObject);

        Col_rewardsText.text = "";
        Col_researchButton.gameObject.SetActive(false);
    }

    public void CloseUI()
    {
        collectionWindow.SetActive(false);
        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(true);
            cachedUIController.LockCursor(true);
        }
    }
}