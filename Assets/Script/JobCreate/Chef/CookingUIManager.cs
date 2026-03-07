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
            GameObject newSlot = Instantiate(CB_ItemPrefab, CB_contentParent);

            // [추천] 프리팹에 아이콘 이미지도 넣어서 매칭해주면 더 좋습니다.
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

        bool canCook = true; // 요리 가능 여부 체크

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            // 재료 슬롯 하나 생성
            GameObject matSlot = Instantiate(CB_materialSlotPrefab, CB_materialsParent);

            // TODO: 현재 인벤토리를 뒤져서 이 아이템(ingredient.itemID)이 몇 개 있는지 가져와야 합니다.
            // 임시로 0개라고 가정하겠습니다.
            int ownedAmount = 0; // GetOwnedItemCount(ingredient.itemID);

            if (ownedAmount < ingredient.requiredAmount) canCook = false;

            // 슬롯 내부의 이미지와 텍스트 컴포넌트 찾아서 값 넣기 (이름으로 찾거나, 별도 스크립트 부착 권장)
            // 아래는 자식 오브젝트 이름을 기준으로 찾는 예시입니다.
            Image iconImg = matSlot.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text nameTxt = matSlot.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text countTxt = matSlot.transform.Find("CountText")?.GetComponent<TMP_Text>();

            if (iconImg) iconImg.sprite = ingredient.itemIcon;
            if (nameTxt) nameTxt.text = ingredient.itemName;

            // 보유량 / 필요량 텍스트 색상 처리 (부족하면 빨간색)
            string colorHex = (ownedAmount >= ingredient.requiredAmount) ? "#FFFFFF" : "#FF0000";
            if (countTxt) countTxt.text = $"<color={colorHex}>{ownedAmount}</color> / {ingredient.requiredAmount}";
        }

        // 3. 보상 정보 세팅
        CB_rewardsText.text = $"[완성품] {recipe.displayName} (x{recipe.resultAmount})";

        // 4. 버튼 상태 세팅 (재료가 다 있으면 클릭 가능, 부족하면 비활성화)
        CB_cookButton.gameObject.SetActive(true);
        CB_cookButton.interactable = canCook;

        CB_cookButton.onClick.RemoveAllListeners();
        CB_cookButton.onClick.AddListener(() =>
        {
            CookRecipe(recipe);
        });
    }

    private void CookRecipe(CookingRecipe recipe)
    {
        Debug.Log($"[{recipe.displayName}] 요리 시작! 재료를 차감하고 결과물을 인벤토리에 넣으세요.");
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