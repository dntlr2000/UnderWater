using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public event Action OnQuestListUpdated;

    public int CurrentQuestId => activeQuests.Count > 0 ? int.Parse(activeQuests[0].questID) : 0;
    public int Difficulty => 1;

    public List<QuestData> allQuests = new List<QuestData>();

    private HashSet<string> completedQuests = new HashSet<string>();
    private List<QuestData> activeQuests = new List<QuestData>();

    private Player localPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        InitStartingQuests();
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
        // 1번 퀘스트(혹은 조건 없는 퀘스트) 자동 수주
        foreach (var quest in allQuests)
        {
            if (completedQuests.Contains(quest.questID) || IsQuestInProgress(quest.questID))
                continue;

            // 선행 퀘스트가 없고, 수동 해금도 아니며, 메인 퀘스트인 경우
            if (quest.prerequisiteQuest == null && quest.questType == QuestType.Main)
            {
                AddQuest(quest);
            }
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
            if (quest.IsUnlocked)
            {
                // 공통 퀘스트이거나, 내 직업과 일치하는 직업 퀘스트인 경우
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

    public void AddQuest(QuestData quest)
    {
        if (IsQuestInProgress(quest.questID)) return;

        foreach (var obj in quest.objectives)
        {
            if (!IsQuestInProgress(quest.questID))
                obj.currentAmount = 0;
        }

        activeQuests.Add(quest);
        Debug.Log($"[QuestManager] 퀘스트 수주: {quest.title}");

        NotifyUIUpdate();
    }

    private bool IsQuestInProgress(string id) => activeQuests.Any(q => q.questID == id);

    public void CompleteQuest(QuestData quest)
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
            TryUnlockQuests(null);
            Debug.LogWarning("로컬 플레이어 또는 직업 정보가 없습니다.");
        }

        SaveManager.Instance.SaveGame();

        NotifyUIUpdate();
    }

    private void NotifyUIUpdate()
    {
        OnQuestListUpdated?.Invoke();
    }

    public bool IsQuestCompleted(QuestData quest)
    {
        if (quest == null) return false;
        return completedQuests.Contains(quest.questID);
    }
    public List<QuestData> GetActiveQuests() => activeQuests;

    public List<QuestData> GetActiveQuestsForPlayer(Player player)
    {
        JobType? currentJobType = player?.CurrentJobType;

        return activeQuests.Where(q =>
            q.questType == QuestType.Main ||
            (q.questType == QuestType.Job && player != null && q.requiredJob == player.CurrentJobType)
        ).ToList();
    }

    private void GrantRewards(QuestData quest)
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
        List<string> completedList = completedQuests.ToList();
        List<QuestProgressData> activeList = new List<QuestProgressData>();

        foreach (var q in activeQuests)
        {
            QuestProgressData progress = new QuestProgressData
            {
                questId = q.questID,
                objectiveCounts = q.objectives.Select(o => o.currentAmount).ToArray()
            };
            activeList.Add(progress);
        }

        return (completedList, activeList);
    }

    // 저장된 데이터를 받아 퀘스트 상태 복구
    public void LoadQuestSaveData(List<string> completed, List<QuestProgressData> active, JobData jobData)
    {
        // 1. 완료 목록 복구
        completedQuests.Clear();
        if (completed != null)
        {
            foreach (var id in completed) completedQuests.Add(id);
        }

        // 2. 진행 중 목록 복구
        activeQuests.Clear();
        if (active != null)
        {
            foreach (var progress in active)
            {
                // ID로 원본 퀘스트 데이터 찾기
                QuestData original = allQuests.FirstOrDefault(q => q.questID == progress.questId);
                if (original != null)
                {
                    // 목표 진행도 복구
                    for (int i = 0; i < original.objectives.Count; i++)
                    {
                        if (i < progress.objectiveCounts.Length)
                        {
                            original.objectives[i].currentAmount = progress.objectiveCounts[i];
                        }
                    }
                    activeQuests.Add(original);
                }
            }
        }

        // 3. 데이터 로드 후, 혹시 해금되어야 할 새 퀘스트가 있는지 체크
        if (jobData != null)
        {
            TryUnlockQuests(jobData);
        }

        NotifyUIUpdate();

        Debug.Log($"[QuestManager] 퀘스트 데이터 로드 완료. (완료: {completedQuests.Count}, 진행중: {activeQuests.Count})");
    }

    public void ReportObjectiveProgress(ObjectiveType type, int amount = 1)
    {
        bool progressChanged = false;

        foreach (var quest in activeQuests)
        {
            foreach (var obj in quest.objectives)
            {
                // 목표 타입이 일치하고, 아직 달성하지 못했다면
                if (obj.type == type && obj.currentAmount < obj.targetAmount)
                {
                    obj.currentAmount += amount;
                    // 최대치를 넘지 않게 고정
                    if (obj.currentAmount > obj.targetAmount)
                        obj.currentAmount = obj.targetAmount;

                    progressChanged = true;
                }
            }
        }
        if (progressChanged)
        {
            NotifyUIUpdate();
            // 실시간 저장을 원하면 여기서 SaveManager.Instance.SaveGame(); 호출
        }

    }
}