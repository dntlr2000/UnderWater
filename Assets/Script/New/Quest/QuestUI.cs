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

        questWindow.SetActive(false);
        completeButton.gameObject.SetActive(false);
        completeButton.onClick.AddListener(OnCompleteButtonClicked);
    }

    public void ToggleQuestWindow()
    {
        //isActive = questWindow.activeSelf; //isActive에서 이름 변경
        questWindow.SetActive(!isActive);
        if (questWindow.activeSelf)
        {
            RefreshQuestList();

            // 퀘스트 창이 열리면 마우스 커서 보이게
            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;
        }
        else
        {
            // 퀘스트 창이 닫히면 마우스 커서 숨기고 잠그기
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
    }

    void RefreshQuestList()
    {
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
            text.text = quest.title;

            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowQuestDetail(quest));
            }
        }

        // 아무것도 없으면 상세 비움
        if (activeQuests.Count == 0)
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

        completeButton.onClick.RemoveAllListeners();
        completeButton.onClick.AddListener(() =>
        {
            QuestManager.Instance.CompleteQuest(quest);
            RefreshQuestList(); // 목록 다시 로드
        });
        bool canComplete = quest.objectives.TrueForAll(obj => obj.currentAmount >= obj.targetAmount);
        completeButton.interactable = canComplete;
    }

    void OnCompleteButtonClicked()
    {
        if (currentSelectedQuest != null)
        {
            QuestManager.Instance.CompleteQuest(currentSelectedQuest);
            RefreshQuestList(); // 완료되면 새로고침
        }
    }

    void ClearQuestDetail()
    {
        titleText.text = "";
        descriptionText.text = "";
        objectivesText.text = "";
        rewardsText.text = "";
    }
}
