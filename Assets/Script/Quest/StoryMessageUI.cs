using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class StoryMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0.1f)] private float displaySeconds = 5f;
    private QuestManager manager;
    private Coroutine binding, hiding;
    private string displayedQuestId;
    private readonly Queue<string> messages = new();

    // 관리자 준비를 기다리고 안내 텍스트가 마우스 입력을 막지 않게 합니다.
    private void OnEnable()
    {
        if (messageText != null) { messageText.enabled = false; messageText.raycastTarget = false; }
        RewardDeliveryService.OnNotice += EnqueueMessage;
        binding = StartCoroutine(BindWhenReady());
    }

    // UI 수명 종료 시 구독과 타이머를 모두 정리합니다.
    private void OnDisable()
    {
        RewardDeliveryService.OnNotice -= EnqueueMessage;
        messages.Clear();
        if (binding != null) StopCoroutine(binding);
        if (hiding != null) StopCoroutine(hiding);
        binding = hiding = null;
        if (manager != null) manager.OnQuestListUpdated -= Refresh;
        manager = null;
        if (messageText != null) messageText.enabled = false;
    }

    // 새 게임, 불러오기, 늦은 입장 모두 현재 메인 안내를 최초 한 번 표시합니다.
    private IEnumerator BindWhenReady()
    {
        yield return new WaitUntil(() => QuestManager.Instance != null && QuestManager.Instance.IsInitialized);
        manager = QuestManager.Instance;
        manager.OnQuestListUpdated += Refresh;
        Refresh();
        binding = null;
    }

    // 메인 퀘스트 ID가 바뀐 경우에만 새 안내를 표시 대기열에 넣습니다.
    private void Refresh()
    {
        var quest = manager.GetActiveQuests().Where(q => q.questType == QuestType.Main)
            .OrderBy(q => q.sortOrder).ThenBy(q => q.questID).FirstOrDefault();
        if (quest == null || quest.questID == displayedQuestId || messageText == null) return;
        displayedQuestId = quest.questID;
        EnqueueMessage(string.IsNullOrWhiteSpace(quest.storyMessage) ? quest.description : quest.storyMessage);
    }

    // 다음 퀘스트 안내와 보상 안내가 서로 덮이지 않게 순서대로 표시합니다.
    private void EnqueueMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || messageText == null) return;
        messages.Enqueue(message);
        if (hiding == null) ShowNextMessage();
    }

    // 대기 중 첫 메시지를 표시하고 기존 자동 숨김 시간을 적용합니다.
    private void ShowNextMessage()
    {
        if (messages.Count == 0 || messageText == null) return;
        messageText.text = messages.Dequeue();
        messageText.enabled = true;
        hiding = StartCoroutine(HideAfterDelay());
    }

    // 게임 시간 배율과 관계없이 안내를 숨긴 뒤 다음 메시지를 표시합니다.
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displaySeconds);
        if (messageText != null) messageText.enabled = false;
        hiding = null;
        ShowNextMessage();
    }
}
