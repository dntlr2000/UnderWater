using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestReminderItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questTitle;
    [SerializeField] private TMP_Text questDescription;
    private bool layoutReady;

    // 배경의 테두리와 장식 안쪽에 여백을 두고 제목·목표의 높이로 항목을 배치합니다.
    private void ConfigureLayout()
    {
        if (layoutReady) return;
        layoutReady = true;
        var layout = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 10, 8, 8);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        foreach (var text in new[] { questTitle, questDescription })
        {
            if (text == null) continue;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = Vector4.zero;
        }
    }

    // 한 퀘스트의 설명과 각 목표 수량을 표시합니다.
    public void Bind(QuestRuntimeData quest)
    {
        ConfigureLayout();
        gameObject.SetActive(true);
        if (questTitle != null) questTitle.text = quest.title;
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(quest.description)) lines.Add(quest.description);
        foreach (var objective in quest.objectives) lines.Add($"{objective.description} ({objective.currentAmount}/{objective.targetAmount})");
        if (!quest.autoComplete && QuestManager.AreObjectivesComplete(quest)) lines.Add("완료 가능 · 퀘스트 탭에서 완료하세요.");
        if (questDescription != null) questDescription.text = string.Join("\n", lines);
    }

    // 빈 항목에 이전 퀘스트의 내용이 남지 않도록 지우고 숨깁니다.
    public void Clear()
    {
        if (questTitle != null) questTitle.text = "";
        if (questDescription != null) questDescription.text = "";
        gameObject.SetActive(false);
    }
}
