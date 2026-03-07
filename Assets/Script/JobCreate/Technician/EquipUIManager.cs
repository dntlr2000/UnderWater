using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipUIManager : MonoBehaviour
{
    public static EquipUIManager Instance;

    [Header("Main Panel")]
    public GameObject equipBenchWindow;

    [Header("List")]
    public Transform EB_contentParent;
    public GameObject EB_ItemPrefab;

    [Header("Info UI")]
    public Text EB_Text; // 상단 제목

    [Header("Detail Area")]
    public Image EB_detailIcon;
    public TMP_Text EB_titleText;
    public TMP_Text EB_descriptionText;
    public TMP_Text EB_timeText;

    [Header("Detail Area - Materials")]
    public Transform EB_materialsParent;
    public GameObject EB_materialSlotPrefab;

    [Header("Detail Area - Result")]
    public TMP_Text EB_rewardsText;
    public Button EB_craftButton; // 요리 버튼 대신 제작 버튼

    private EquipRecipe currentSelectedRecipe;
    private UIController cachedUIController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (equipBenchWindow != null) equipBenchWindow.SetActive(false);
        if (EB_craftButton != null) EB_craftButton.gameObject.SetActive(false);

        cachedUIController = FindAnyObjectByType<UIController>();
    }

    public void OpenUI(List<EquipRecipe> recipes, bool isSpecialist)
    {
        equipBenchWindow.SetActive(true);

        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(false);
            cachedUIController.LockCursor(false);
        }

        EB_Text.text = isSpecialist ? "전문가용 장비 제작대" : "장비 제작대 (기본)";

        foreach (Transform child in EB_contentParent) Destroy(child.gameObject);

        foreach (EquipRecipe recipe in recipes)
        {
            GameObject newSlot = Instantiate(EB_ItemPrefab, EB_contentParent);

            TMP_Text text = newSlot.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = recipe.displayName;

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowRecipeDetail(recipe));
        }

        if (recipes.Count > 0) ShowRecipeDetail(recipes[0]);
        else ClearRecipeDetail();
    }

    private void ShowRecipeDetail(EquipRecipe recipe)
    {
        currentSelectedRecipe = recipe;

        EB_titleText.text = recipe.displayName;
        EB_descriptionText.text = recipe.recipeDescription;
        if (EB_timeText) EB_timeText.text = $"제작 시간: {recipe.craftTime}초";
        if (EB_detailIcon && recipe.icon != null) EB_detailIcon.sprite = recipe.icon;

        foreach (Transform child in EB_materialsParent) Destroy(child.gameObject);

        bool canCraft = true;

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            GameObject matSlot = Instantiate(EB_materialSlotPrefab, EB_materialsParent);

            // 주의: Inventory 스크립트에 GetOwnedItemCount 함수가 있어야 합니다.
            int ownedAmount = 0;
            /*if (Inventory.Instance != null)
            {
                // ownedAmount = Inventory.Instance.GetOwnedItemCount(ingredient.itemID);
            }

            if (ownedAmount < ingredient.requiredAmount) canCraft = false;*/

            Image iconImg = matSlot.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text nameTxt = matSlot.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text countTxt = matSlot.transform.Find("CountText")?.GetComponent<TMP_Text>();

            if (iconImg) iconImg.sprite = ingredient.itemIcon;
            if (nameTxt) nameTxt.text = ingredient.itemName;

            string colorHex = (ownedAmount >= ingredient.requiredAmount) ? "#FFFFFF" : "#FF0000";
            if (countTxt) countTxt.text = $"<color={colorHex}>{ownedAmount}</color> / {ingredient.requiredAmount}";
        }

        EB_rewardsText.text = $"[완성품] {recipe.displayName} (x{recipe.resultAmount})";

        EB_craftButton.gameObject.SetActive(true);
        EB_craftButton.interactable = canCraft;

        EB_craftButton.onClick.RemoveAllListeners();
        EB_craftButton.onClick.AddListener(() =>
        {
            CraftEquip(recipe);
        });
    }

    private void CraftEquip(EquipRecipe recipe)
    {
        Debug.Log($"[{recipe.displayName}] 장비 제작 시작!");
    }

    private void ClearRecipeDetail()
    {
        currentSelectedRecipe = null;
        EB_titleText.text = "선택된 도면 없음";
        EB_descriptionText.text = "";
        if (EB_timeText) EB_timeText.text = "";
        if (EB_detailIcon) EB_detailIcon.sprite = null;

        foreach (Transform child in EB_materialsParent) Destroy(child.gameObject);

        EB_rewardsText.text = "";
        EB_craftButton.gameObject.SetActive(false);
    }

    public void CloseUI()
    {
        equipBenchWindow.SetActive(false);
        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(true);
            cachedUIController.LockCursor(true);
        }
    }
}