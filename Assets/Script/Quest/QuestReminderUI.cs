using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class QuestReminderUI : MonoBehaviour
{
    [SerializeField] private QuestReminderItemUI[] slots = new QuestReminderItemUI[3];
    [SerializeField] private Graphic background;
    private QuestManager manager;
    private Coroutine binding;

    // 기존 HUD 폭과 위쪽 위치를 유지하고 각 항목의 실제 높이만큼 세로로 배치합니다.
    private void Awake()
    {
        var rect = (RectTransform)transform;
        float top = rect.anchoredPosition.y + (1f - rect.pivot.y) * rect.rect.height;
        rect.pivot = new Vector2(rect.pivot.x, 1f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top);
        // 세 칸이 합쳐진 기존 이미지는 크기 계산과 렌더링에서 제외합니다.
        // 배경은 각 RemindQuest의 자식에 두어 글자와 같은 높이로 표시합니다.
        if (background != null) { background.raycastTarget = false; background.enabled = false; }
        var layout = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = GetComponent<ContentSizeFitter>() ?? gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        foreach (var slot in slots) if (slot != null) slot.Clear();
    }

    // 생성 순서와 관계없이 관리자가 준비되면 이벤트를 구독합니다.
    private void OnEnable() => binding = StartCoroutine(BindWhenReady());

    // 비활성화 시 중복 구독과 대기 코루틴을 정리합니다.
    private void OnDisable()
    {
        if (binding != null) StopCoroutine(binding);
        binding = null;
        if (manager != null) manager.OnQuestListUpdated -= Refresh;
        manager = null;
    }

    // 퀘스트 초기화 직후 현재 상태를 한 번 읽고 이후에는 변경 이벤트로 갱신합니다.
    private IEnumerator BindWhenReady()
    {
        yield return new WaitUntil(() => QuestManager.Instance != null && QuestManager.Instance.IsInitialized);
        manager = QuestManager.Instance;
        manager.OnQuestListUpdated += Refresh;
        Refresh();
        binding = null;
    }

    // 추후 사용자 선택 기능을 추가할 수 있도록 표시 대상 선정만 분리합니다.
    public static List<QuestRuntimeData> SelectVisibleQuests(IEnumerable<QuestRuntimeData> quests, int count)
    {
        return quests.OrderBy(q => q.questType == QuestType.Main ? 0 : 1).ThenBy(q => q.sortOrder)
            .ThenBy(q => q.questID, System.StringComparer.Ordinal).Take(count).ToList();
    }

    // 활성 퀘스트 최대 세 개를 배치하고 나머지 슬롯은 완전히 초기화합니다.
    private void Refresh()
    {
        if (manager == null) return;
        var visible = SelectVisibleQuests(manager.GetActiveQuestsForPlayer(Player.localPlayer), slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (i < visible.Count) slots[i].Bind(visible[i]); else slots[i].Clear();
        }
        // 빈 슬롯은 배경까지 함께 숨기고 관리자는 새 퀘스트를 계속 감지합니다.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}
