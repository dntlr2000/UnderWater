using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CookingUIManager : MonoBehaviour
{
    public static CookingUIManager Instance;

    [Header("Main Panel")]
    public GameObject cookingBenchWindow;          // 전체 UI 패널 (켜고 끄기용)

    [Header("List")]
    public Transform CB_contentParent;        // 슬롯들이 생성될 부모 오브젝트 (Content)
    public GameObject CB_ItemPrefab;       // 생성할 슬롯 프리팹

    [Header("Info UI")]
    public Text CB_Text;              // 제목 텍스트

    [Header("Detail Area")]
    public Image CB_detailIcon;
    public TMP_Text CB_titleText;
    public TMP_Text CB_descriptionText;
    public TMP_Text CB_timeText;

    [Header("Detail Area - Materials")]
    public Transform CB_materialsParent;  // 재료 슬롯들이 생성될 부모 (Horizontal / Grid Layout 추천)
    public GameObject CB_materialSlotPrefab;

    [Header("Detail Area - Result")]
    public TMP_Text CB_rewardsText;
    public Button CB_cookButton;

    private CookingRecipe currentSelectedRecipe;
    private UIController cachedUIController;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작할 때 UI 끄기
        if (cookingBenchWindow != null) cookingBenchWindow.SetActive(false);
        if (CB_cookButton != null) CB_cookButton.gameObject.SetActive(false);

        cachedUIController = FindAnyObjectByType<UIController>();
    }

    // CookingWorkbench에서 호출하는 함수
    public void OpenUI(List<CookingRecipe> recipes, bool isSpecialist)
    {
        // UI 켜기 및 조작 잠금 (UIController 활용)
        cookingBenchWindow.SetActive(true);

        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(false);
            cachedUIController.LockCursor(false);
        }

        CB_Text.text = isSpecialist ? "전문가용 요리 작업대" : "요리 작업대 (기본)";

        // 기존 좌측 레시피 목록 지우기
        foreach (Transform child in CB_contentParent) Destroy(child.gameObject);

        // 좌측 레시피 목록 생성
        foreach (CookingRecipe recipe in recipes)
        {
            GameObject newSlot = Instantiate(CB_ItemPrefab, CB_contentParent, false);

            TMP_Text text = newSlot.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = recipe.displayName;

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowRecipeDetail(recipe));
        }

        if (recipes.Count > 0) ShowRecipeDetail(recipes[0]);
        else ClearRecipeDetail();
    }

    private void ShowRecipeDetail(CookingRecipe recipe)
    {
        currentSelectedRecipe = recipe;

        // 1. 기본 정보 설정
        CB_titleText.text = recipe.displayName;
        CB_descriptionText.text = recipe.recipeDescription;
        if (CB_timeText) CB_timeText.text = $"조리 시간: {recipe.cookTime}초";
        if (CB_detailIcon && recipe.icon != null) CB_detailIcon.sprite = recipe.icon;

        foreach (Transform child in CB_materialsParent) Destroy(child.gameObject);

       /* Inventory myInventory = FindAnyObjectByType<Inventory>();*/
        bool canCook = true; // 요리 가능 여부 체크

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            // 재료 슬롯 하나 생성
            GameObject matSlot = Instantiate(CB_materialSlotPrefab, CB_materialsParent);

            matSlot.transform.localPosition = Vector3.zero;
            matSlot.transform.localScale = Vector3.one;

            int ownedAmount = ingredient.requiredAmount; // GetOwnedItemCount(ingredient.itemID); 원래 0으로 해야함 게이지 보려고 바꿈
            /*if (myInventory != null)
            {
                ownedAmount = myInventory.GetOwnedItemCount(ingredient.itemID);
            }

            if (ownedAmount < ingredient.requiredAmount) canCook = false;*/

            Image iconImg = matSlot.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text nameTxt = matSlot.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text countTxt = matSlot.transform.Find("CountText")?.GetComponent<TMP_Text>();

            if (iconImg) iconImg.sprite = ingredient.itemIcon;
            if (nameTxt) nameTxt.text = ingredient.itemName;

            string colorHex = (ownedAmount >= ingredient.requiredAmount) ? "#FFFFFF" : "#FF0000";
            if (countTxt) countTxt.text = $"<color={colorHex}>{ownedAmount}</color> / {ingredient.requiredAmount}";
        }

        // 3. 보상 정보 세팅
        CB_rewardsText.text = $"[완성품] {recipe.displayName} (x{recipe.resultAmount})";

        // 4. 버튼 상태 세팅 (재료가 다 있으면 클릭 가능, 부족하면 비활성화)
        CB_cookButton.gameObject.SetActive(true);
        CB_cookButton.interactable = canCook;

        TMP_Text buttonText = CB_cookButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = "요리하기";

        CB_cookButton.onClick.RemoveAllListeners();
        CB_cookButton.onClick.AddListener(() =>
        {
            StartCraftingProcess(recipe, buttonText);
        });
    }

    private void CookRecipe(CookingRecipe recipe)
    {
        Debug.Log($"[{recipe.displayName}] 요리 시작! 재료를 차감하고 결과물을 인벤토리에 넣으세요.");
    }

    private void StartCraftingProcess(CookingRecipe recipe, TMP_Text buttonText)
    {
        // 딤드 처리 및 텍스트 변경
        CB_cookButton.interactable = false;
        if (buttonText != null) buttonText.text = "요리 중...";

        if (GlobalProgressBar.Instance != null)
        {
            GlobalProgressBar.Instance.StartProgress(
                $"[{recipe.displayName}] 요리 중...",
                recipe.cookTime,
                () =>
                {
                    // ==========================================
                    // 6. 버튼 3단계 상태: "보상 받기" 로 전환
                    // ==========================================
                    if (buttonText != null) buttonText.text = "보상 받기";
                    CB_cookButton.interactable = true; // 다시 클릭 가능하게 활성화

                    // 기존 이벤트를 지우고 보상 수령 로직으로 교체
                    CB_cookButton.onClick.RemoveAllListeners();
                    CB_cookButton.onClick.AddListener(() =>
                    {
                        ClaimReward(recipe);
                    });
                }
            );
        }
        else
        {
            Debug.LogError("GlobalProgressBar가 씬에 없습니다! 게이지를 띄울 수 없습니다.");
        }
    }

    private void ClaimReward(CookingRecipe recipe)
    {
        /*Inventory myInventory = FindAnyObjectByType<Inventory>();

        if (myInventory != null)
        {
            // 1. 재료 차감
            foreach (RecipeIngredient ingredient in recipe.ingredients)
            {
                myInventory.ConsumeItemByID(ingredient.itemID, ingredient.requiredAmount);
            }

            // 2. 완성품 획득
            myInventory.GetItem(recipe.resultItemID, recipe.resultAmount);
            Debug.Log($"[{recipe.displayName}] 요리 완성! 인벤토리에 지급되었습니다.");
        }*/

        // 로그만 띄워서 작동 확인
        Debug.Log($"[{recipe.displayName}] 요리 완성 테스트! (인벤토리 차감/지급 건너뜀)");
        // 3. 재료가 소모되었으므로 UI를 새로고침하여 숫자를 갱신 (버튼도 다시 초기화됨)
        ShowRecipeDetail(recipe);
    }

    private void ClearRecipeDetail()
    {
        currentSelectedRecipe = null;
        CB_titleText.text = "선택된 레시피 없음";
        CB_descriptionText.text = "";
        if (CB_timeText) CB_timeText.text = "";
        if (CB_detailIcon) CB_detailIcon.sprite = null;

        foreach (Transform child in CB_materialsParent) Destroy(child.gameObject);

        CB_rewardsText.text = "";
        CB_cookButton.gameObject.SetActive(false);
    }

    public void CloseUI()
    {
        cookingBenchWindow.SetActive(false);
        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(true);
            cachedUIController.LockCursor(true);
        }
    }
}