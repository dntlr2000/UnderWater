using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;

    public GameObject questWindow;
    public Transform contentParent;
    public GameObject questItemPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        questWindow.SetActive(false);
    }

    public void ToggleQuestWindow()
    {
        bool isActive = questWindow.activeSelf;
        questWindow.SetActive(!isActive);
        if (!isActive)
        {
            RefreshQuestList();
        }
    }
    void RefreshQuestList()
    {
        // 기존 아이템 모두 제거
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // QuestManager에서 활성화된 퀘스트 리스트 가져오기
        var activeQuests = QuestManager.Instance.GetActiveQuests();

        foreach (var quest in activeQuests)
        {
            GameObject item = Instantiate(questItemPrefab, contentParent);
            Text text = item.GetComponentInChildren<Text>();
            text.text = $"{quest.title}\n{quest.description}";
        }
    }
}
