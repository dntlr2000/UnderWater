using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public event Action OnQuestListUpdated;

    public int Difficulty => 1;

    public List<QuestRuntimeData> allQuests = new();
    private HashSet<string> completedQuests = new HashSet<string>();
    private List<QuestRuntimeData> activeQuests = new();

    private Player localPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        LoadQuestsFromDataLoader();
        InitStartingQuests();
    }

    private void LoadQuestsFromDataLoader()
    {
        if (DataLoader.Instance == null)
        {
            Debug.LogError("[QuestManager] DataLoader 인스턴스가 없습니다.");
            return;
        }

        allQuests = DataLoader.Instance.GetAllQuests().ToList();
        Debug.Log($"[QuestManager] 퀘스트 {allQuests.Count}개 로드 완료");
    }

    public void RegisterLocalPlayer(Player player)
    {
        localPlayer = player;
        // 플레이어 등록 시점에 바로 퀘스트 갱신 시도
        if (localPlayer.currentJob != null)
        {
            TryUnlockQuests(localPlayer.currentJob);
        }
        NotifyUIUpdate();
    }

    public void InitStartingQuests()
    {
        foreach (var quest in allQuests)
        {
            if (completedQuests.Contains(quest.questID) || IsQuestInProgress(quest.questID))
                continue;

            if (string.IsNullOrEmpty(quest.prerequisiteQuestID))
                AddQuest(quest);
        }
    }

    public void TryUnlockQuests(JobData jobData)
    {
        if (jobData == null) return;

        bool changed = false;

        foreach (var quest in allQuests)
        {
            // 이미 완료했거나 진행 중이면 패스
            if (completedQuests.Contains(quest.questID) || activeQuests.Any(q => q.questID == quest.questID))
                continue;

            // 선행 퀘스트 조건 및 해금 플래그 확인
            if (IsUnlocked(quest))
            {
                if (quest.questType == QuestType.Main ||
                   (quest.questType == QuestType.Job && quest.requiredJob == jobData.jobType))
                {
                    AddQuest(quest);
                    changed = true;
                }
            }
        }

        if (changed) NotifyUIUpdate();
    }

    private bool IsUnlocked(QuestRuntimeData quest)
    {
        if (quest.isUnlockedManually) return true;
        if (string.IsNullOrEmpty(quest.prerequisiteQuestID)) return true;
        return completedQuests.Contains(quest.prerequisiteQuestID);
    }

    public void AddQuest(QuestRuntimeData quest)
    {
        if (IsQuestInProgress(quest.questID)) return;

        foreach (var obj in quest.objectives)
        {
            obj.currentAmount = 0;
        }

        activeQuests.Add(quest);
        Debug.Log($"[QuestManager] 퀘스트 시작: {quest.title}");
        NotifyUIUpdate();
    }

    private bool IsQuestInProgress(string id) => activeQuests.Any(q => q.questID == id);

    public void CompleteQuest(QuestRuntimeData quest)
    {
        if (!activeQuests.Contains(quest)) return;

        completedQuests.Add(quest.questID);
        activeQuests.Remove(quest);

        Debug.Log($"퀘스트 완료: {quest.title}");
        GrantRewards(quest);

        if (localPlayer != null && localPlayer.currentJob != null)
        {
            TryUnlockQuests(localPlayer.currentJob);
        }
        else
        {
            Debug.LogWarning("로컬 플레이어 또는 직업 정보가 없습니다.");
        }

        SaveManager.Instance.SaveGame();
        NotifyUIUpdate();
    }

    private void NotifyUIUpdate()
    {
        OnQuestListUpdated?.Invoke();
    }

    public bool IsQuestCompleted(string questID)
    {
        return !string.IsNullOrEmpty(questID) && completedQuests.Contains(questID);
    }

    public List<QuestRuntimeData> GetActiveQuests() => activeQuests;

    public List<QuestRuntimeData> GetActiveQuestsForPlayer(Player player)
    {
        return activeQuests.Where(q =>
            q.questType == QuestType.Main ||
            (q.questType == QuestType.Job && player != null && q.requiredJob == player.CurrentJobType)
        ).ToList();
    }

    private void GrantRewards(QuestRuntimeData quest)
    {
        foreach (var reward in quest.rewards)
        {
            switch (reward.rewardType)
            {
                case RewardType.Item:
                    Debug.Log($"[보상] 아이템 x{reward.amount} 지급");
                    // 예: InventoryManager.Instance.AddItem(itemID, reward.amount);
                    break;

                case RewardType.Money:
                    Debug.Log($"[보상] 골드 +{reward.amount}");
                    // 예: localPlayer.AddMoney(reward.amount);
                    break;

                case RewardType.Experience:
                    Debug.Log($"[보상] 경험치 +{reward.amount}");
                    // 예: localPlayer.AddExperience(reward.amount);
                    break;

                case RewardType.UnlockRecipe:
                    Debug.Log($"[보상] 레시피 해금 (ID: {reward.amount})");
                    // 예: localPlayer.UnlockRecipe(reward.amount);
                    break;
            }
        }
    }

    public (List<string> completed, List<QuestProgressData> active) GetQuestSaveData()
    {
        var completedList = completedQuests.ToList();
        var activeList = activeQuests.Select(q => new QuestProgressData
        {
            questId = q.questID,
            objectiveCounts = q.objectives.Select(o => o.currentAmount).ToArray()
        }).ToList();

        return (completedList, activeList);
    }

    // 저장된 데이터를 받아 퀘스트 상태 복구
    public void LoadQuestSaveData(List<string> completed, List<QuestProgressData> active, JobData jobData)
    {
        completedQuests.Clear();
        if (completed != null)
            foreach (var id in completed) completedQuests.Add(id);

        activeQuests.Clear();
        if (active != null)
        {
            foreach (var progress in active)
            {
                var original = allQuests.FirstOrDefault(q => q.questID == progress.questId);
                if (original == null) continue;

                for (int i = 0; i < original.objectives.Count; i++)
                {
                    if (i < progress.objectiveCounts.Length)
                        original.objectives[i].currentAmount = progress.objectiveCounts[i];
                }
                activeQuests.Add(original);
            }
        }
        if (jobData != null) TryUnlockQuests(jobData);

        NotifyUIUpdate();
        Debug.Log($"[QuestManager] 퀘스트 데이터 로드 완료. (완료: {completedQuests.Count}, 진행중: {activeQuests.Count})");
    }

    public void ReportObjectiveProgress(ObjectiveType type, int amount = 1, string itemID = "")
    {
        bool progressChanged = false;

        foreach (var quest in activeQuests)
        {
            foreach (var obj in quest.objectives)
            {
                if (obj.type != type) continue;
                if (obj.currentAmount >= obj.targetAmount) continue;

                if (type == ObjectiveType.CollectItem || type == ObjectiveType.CraftItem)
                {
                    if (!string.IsNullOrEmpty(obj.collectItemName) &&
                        !string.IsNullOrEmpty(itemID) &&
                        obj.collectItemName != itemID) continue;
                }

                obj.currentAmount = Mathf.Min(obj.currentAmount + amount, obj.targetAmount);
                progressChanged = true;
            }
        }
        if (progressChanged)
        {
            NotifyUIUpdate();
            // 실시간 저장을 원하면 여기서 SaveManager.Instance.SaveGame(); 호출
        }

    }
}