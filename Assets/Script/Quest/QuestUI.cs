using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;

    [Header("Main Panel")]
    public GameObject questWindow;

    [Header("List")]
    public Transform contentParent;
    public GameObject questItemPrefab;

    [Header("Detail Area")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text objectivesText;
    public TMP_Text rewardsText;

    public Button completeButton;
    private QuestData currentSelectedQuest;

    public bool isActive = false; //isActive


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        isActive = false;
        questWindow.SetActive(false);

        completeButton.gameObject.SetActive(false);
        completeButton.onClick.AddListener(OnCompleteButtonClicked);
    }

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListUpdated += RefreshQuestList;
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListUpdated -= RefreshQuestList;
        }
    }

    public void ToggleQuestWindow()
    {
        isActive = !isActive;
        questWindow.SetActive(isActive);

        if (isActive)
        {
            RefreshQuestList();
        }
    }

    public void RefreshQuestList()
    {
        //if (!isActive) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (Player.localPlayer == null)
            return;

        var activeQuests = QuestManager.Instance.GetActiveQuestsForPlayer(Player.localPlayer);

        Debug.Log($"[QuestUI] 활성 퀘스트 개수: {activeQuests.Count}");

        foreach (var quest in activeQuests)
        {
            GameObject item = Instantiate(questItemPrefab, contentParent);

            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = quest.title;

            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowQuestDetail(quest));
            }
        }

        if (currentSelectedQuest != null && activeQuests.Contains(currentSelectedQuest))
        {
            ShowQuestDetail(currentSelectedQuest);
        }
        else
        {
            ClearQuestDetail();
        }
    }

    void ShowQuestDetail(QuestData quest)
    {
        currentSelectedQuest = quest;

        titleText.text = quest.title;
        descriptionText.text = quest.description;

        // 목표 구성
        List<string> objTexts = new();
        bool allObjectivesComplete = true;
        foreach (var obj in quest.objectives)
        {
            objTexts.Add($"{obj.description} ({obj.currentAmount}/{obj.targetAmount})");

            if (obj.currentAmount < obj.targetAmount)
                allObjectivesComplete = false;
        }
        objectivesText.text = string.Join("\n", objTexts);

        // 보상 구성
        List<string> rewardTexts = new();
        foreach (var reward in quest.rewards)
        {
            rewardTexts.Add($"{reward.rewardType} +{reward.amount}");
        }
        rewardsText.text = string.Join("\n", rewardTexts);

        completeButton.gameObject.SetActive(true);
        completeButton.interactable = allObjectivesComplete;

        completeButton.onClick.RemoveAllListeners();
        completeButton.onClick.AddListener(() =>
        {
            QuestManager.Instance.CompleteQuest(quest);
        });
    }

    void OnCompleteButtonClicked()
    {
        // 중복 방지를 위해 ShowQuestDetail 내부 리스너 사용 권장, 여기는 비워두거나 제거
    }

    void ClearQuestDetail()
    {
        currentSelectedQuest = null;
        titleText.text = "";
        descriptionText.text = "";
        objectivesText.text = "";
        rewardsText.text = "";
        completeButton.gameObject.SetActive(false);
    }
}
