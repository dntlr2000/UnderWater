using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;
    [Header("Main Panel")] public GameObject questWindow;
    [Header("List")] public Transform contentParent;
    public GameObject questItemPrefab;
    [Header("Detail Area")] public TMP_Text titleText;
    public TMP_Text descriptionText, objectivesText, rewardsText;
    public Button completeButton;
    public bool isActive;
    private QuestManager manager;
    private string selectedQuestId;
    private Coroutine binding;

    // 기존 Inspector 참조를 유지하고 완료 버튼을 한 번만 연결합니다.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        isActive = false;
        questWindow.SetActive(false);
        completeButton.gameObject.SetActive(false);
        completeButton.onClick.AddListener(OnCompleteButtonClicked);
    }

    // 관리자 생성 순서와 관계없이 준비가 완료되면 구독합니다.
    private void OnEnable() => binding = StartCoroutine(BindWhenReady());

    // 비활성화 시 대기 및 이벤트를 정리해 중복 갱신을 방지합니다.
    private void OnDisable()
    {
        if (binding != null) StopCoroutine(binding);
        binding = null;
        if (manager != null) manager.OnQuestListUpdated -= RefreshQuestList;
        manager = null;
    }

    // 씬 종료 시 정적 UI 참조를 정리합니다.
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // 현재 퀘스트 초기화 이후 즉시 한 번 갱신합니다.
    private IEnumerator BindWhenReady()
    {
        yield return new WaitUntil(() => QuestManager.Instance != null && QuestManager.Instance.IsInitialized);
        manager = QuestManager.Instance;
        manager.OnQuestListUpdated += RefreshQuestList;
        RefreshQuestList();
        binding = null;
    }

    // 창을 열 때 최신 목록을 표시합니다.
    public void ToggleQuestWindow()
    {
        isActive = !isActive;
        questWindow.SetActive(isActive);
        if (isActive) RefreshQuestList();
    }

    // 동기화로 실행 데이터가 교체되어도 선택 ID로 최신 퀘스트를 다시 찾습니다.
    public void RefreshQuestList()
    {
        if (manager == null) manager = QuestManager.Instance;
        if (manager == null || !manager.IsInitialized) return;
        var active = manager.GetActiveQuestsForPlayer(Player.localPlayer);
        if (isActive)
        {
            foreach (Transform child in contentParent) Destroy(child.gameObject);
            foreach (var quest in QuestReminderUI.SelectVisibleQuests(active, active.Count))
            {
                var item = Instantiate(questItemPrefab, contentParent);
                var text = item.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = quest.title;
                string questId = quest.questID;
                var button = item.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => SelectQuest(questId));
            }
        }
        var selected = active.FirstOrDefault(q => q.questID == selectedQuestId);
        if (selected == null) ClearQuestDetail(); else ShowQuestDetail(selected);
    }

    // 목록 버튼이 오래된 실행 객체 대신 ID를 통해 현재 데이터를 선택합니다.
    private void SelectQuest(string questId)
    {
        selectedQuestId = questId;
        var quest = manager.GetActiveQuestsForPlayer(Player.localPlayer).FirstOrDefault(q => q.questID == questId);
        if (quest != null) ShowQuestDetail(quest);
    }

    // 현재 목표와 보상을 표시하고 수동 완료가 가능한 경우에만 버튼을 활성화합니다.
    private void ShowQuestDetail(QuestRuntimeData quest)
    {
        selectedQuestId = quest.questID;
        titleText.text = quest.title;
        descriptionText.text = quest.description;
        objectivesText.text = string.Join("\n", quest.objectives.Select(o => $"{o.description} ({o.currentAmount}/{o.targetAmount})"));
        rewardsText.text = string.Join("\n", quest.rewards.Where(RewardDeliveryService.IsSupported).Select(FormatReward));
        completeButton.gameObject.SetActive(!quest.autoComplete);
        completeButton.interactable = QuestManager.AreObjectivesComplete(quest);
    }

    // 실제 지급되는 보상의 이름·수량과 수령 위치를 표시합니다.
    private static string FormatReward(QuestReward reward)
    {
        string content = reward.rewardType == RewardType.Money ? $"{reward.amount}G" :
            $"{ItemDatabase.Instance?.GetItemByStringId(reward.itemID)?.itemName ?? reward.itemID} ×{reward.amount}";
        return content + (reward.destination == RewardDestination.Mailbox ? " — 공용 우편함 지급" : " — 인벤토리 지급");
    }

    // 실제 완료 판정과 메인 네트워크 요청은 관리자에게 맡깁니다.
    private void OnCompleteButtonClicked()
    {
        var quest = manager?.GetActiveQuestsForPlayer(Player.localPlayer).FirstOrDefault(q => q.questID == selectedQuestId);
        if (quest != null) manager.CompleteQuest(quest);
    }

    // 완료되거나 사라진 퀘스트의 상세 내용과 버튼을 초기화합니다.
    private void ClearQuestDetail()
    {
        selectedQuestId = null;
        titleText.text = descriptionText.text = objectivesText.text = rewardsText.text = "";
        completeButton.gameObject.SetActive(false);
    }
}