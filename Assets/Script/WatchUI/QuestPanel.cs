using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestPanel : WatchPanelBase
{
    [System.Serializable]
    private struct QuestEntry
    {
        public string Title;
        public string Status;
    }

    [SerializeField] private Transform _listContainer;
    [SerializeField] private GameObject _entryPrefab;

    private readonly List<QuestEntry> _dummyData = new List<QuestEntry>
    {
        new QuestEntry { Title = "수중 유물 수거",   Status = "진행" },
        new QuestEntry { Title = "실종자 단서 확보", Status = "대기" },
        new QuestEntry { Title = "발전기 부품 납품", Status = "완료" },
        new QuestEntry { Title = "조류 데이터 수집", Status = "진행" },
    };

    public override void RefreshData()
    {
        PopulateList(_dummyData);
    }

    private void PopulateList(List<QuestEntry> entries)
    {
        if (_listContainer == null || _entryPrefab == null) return;

        foreach (Transform child in _listContainer)
            Destroy(child.gameObject);

        foreach (var entry in entries)
        {
            var go = Instantiate(_entryPrefab, _listContainer);

            var titleText = go.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var statusText = go.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();

            if (titleText != null) titleText.text = entry.Title;
            if (statusText != null) statusText.text = entry.Status;
        }
    }
}