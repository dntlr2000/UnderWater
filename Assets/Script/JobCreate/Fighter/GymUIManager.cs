using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GymUIManager : MonoBehaviour
{
    public static GymUIManager Instance;

    [Header("Main Panel")]
    public GameObject gymBenchWindow;

    [Header("List")]
    public Transform GB_contentParent;
    public GameObject GB_ItemPrefab;

    [Header("Info UI")]
    public Text GB_Text;

    [Header("Detail Area")]
    public Image GB_detailIcon;
    public TMP_Text GB_titleText;
    public TMP_Text GB_descriptionText;
    public TMP_Text GB_timeText;

    [Header("Detail Area - Costs (소모 스탯)")]
    public Transform GB_materialsParent;
    public GameObject GB_materialSlotPrefab; // 기존 재료 슬롯 프리팹 그대로 사용!

    [Header("Detail Area - Result")]
    public TMP_Text GB_rewardsText;
    public Button GB_exerciseButton; // 제작/요리 대신 '운동하기' 버튼

    private GymExercise currentSelectedExercise;
    private UIController cachedUIController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (gymBenchWindow != null) gymBenchWindow.SetActive(false);
        if (GB_exerciseButton != null) GB_exerciseButton.gameObject.SetActive(false);

        cachedUIController = FindAnyObjectByType<UIController>();
    }

    public void OpenUI(List<GymExercise> exercises, bool isSpecialist)
    {
        gymBenchWindow.SetActive(true);

        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(false);
            cachedUIController.LockCursor(false);
        }

        GB_Text.text = isSpecialist ? "전문가용 운동 기구" : "운동 기구 (기본 스트레칭)";

        foreach (Transform child in GB_contentParent) Destroy(child.gameObject);

        foreach (GymExercise exercise in exercises)
        {
            GameObject newSlot = Instantiate(GB_ItemPrefab, GB_contentParent);

            TMP_Text text = newSlot.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = exercise.displayName;

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowExerciseDetail(exercise));
        }

        if (exercises.Count > 0) ShowExerciseDetail(exercises[0]);
        else ClearExerciseDetail();
    }

    private void ShowExerciseDetail(GymExercise exercise)
    {
        currentSelectedExercise = exercise;

        GB_titleText.text = exercise.displayName;
        GB_descriptionText.text = exercise.exerciseDescription;
        if (GB_timeText) GB_timeText.text = $"운동 소요 시간: {exercise.exerciseTime}초";
        if (GB_detailIcon && exercise.icon != null) GB_detailIcon.sprite = exercise.icon;

        foreach (Transform child in GB_materialsParent) Destroy(child.gameObject);

        bool canExercise = true;

        // 동적 슬롯 생성 (배고픔 소모량이 0보다 크면 슬롯 1개 생성)
        if (exercise.costHunger > 0)
        {
            CreateCostSlot("배고픔 소모", exercise.costHunger);
        }

        // 동적 슬롯 생성 (수분 소모량이 0보다 크면 슬롯 1개 생성)
        if (exercise.costWater > 0)
        {
            CreateCostSlot("수분 소모", exercise.costWater);
        }

        // 보상 텍스트 조립 (체력, 스테미너 증가량)
        string rewardStr = "[운동 효과]\n";
        if (exercise.bonusMaxHP > 0) rewardStr += $"최대 체력 +{exercise.bonusMaxHP}\n";
        if (exercise.bonusMaxStamina > 0) rewardStr += $"최대 스테미너 +{exercise.bonusMaxStamina}";
        GB_rewardsText.text = rewardStr;

        GB_exerciseButton.gameObject.SetActive(true);
        GB_exerciseButton.interactable = canExercise;

        GB_exerciseButton.onClick.RemoveAllListeners();
        GB_exerciseButton.onClick.AddListener(() =>
        {
            StartExercise(exercise);
        });
    }

    // 소모 스탯을 UI 슬롯으로 예쁘게 그려주는 전용 함수
    private void CreateCostSlot(string costName, float requiredAmount)
    {
        GameObject matSlot = Instantiate(GB_materialSlotPrefab, GB_materialsParent);

        float currentStat = requiredAmount + 50f; // 항상 필요량보다 50 많게 설정

        Image iconImg = matSlot.transform.Find("Icon")?.GetComponent<Image>();
        TMP_Text nameTxt = matSlot.transform.Find("NameText")?.GetComponent<TMP_Text>();
        TMP_Text countTxt = matSlot.transform.Find("CountText")?.GetComponent<TMP_Text>();

        // 아이콘은 직업별로 맞게 넣으실 수 있도록 비워두거나 기본 처리하시면 됩니다.
        if (nameTxt) nameTxt.text = costName;

        string colorHex = (currentStat >= requiredAmount) ? "#FFFFFF" : "#FF0000";
        if (countTxt) countTxt.text = $"<color={colorHex}>{currentStat}</color> / {requiredAmount}";
    }

    private void StartExercise(GymExercise exercise)
    {
        Debug.Log($"[{exercise.displayName}] 운동 시작! 수치를 소모하고 스탯을 올립니다.");
    }

    private void ClearExerciseDetail()
    {
        currentSelectedExercise = null;
        GB_titleText.text = "선택된 운동 없음";
        GB_descriptionText.text = "";
        if (GB_timeText) GB_timeText.text = "";
        if (GB_detailIcon) GB_detailIcon.sprite = null;

        foreach (Transform child in GB_materialsParent) Destroy(child.gameObject);

        GB_rewardsText.text = "";
        GB_exerciseButton.gameObject.SetActive(false);
    }

    public void CloseUI()
    {
        gymBenchWindow.SetActive(false);
        if (cachedUIController != null)
        {
            cachedUIController.SetPlayerControl(true);
            cachedUIController.LockCursor(true);
        }
    }
}