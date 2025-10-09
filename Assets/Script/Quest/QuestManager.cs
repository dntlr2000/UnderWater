using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public int CurrentQuestId => activeQuests.Count > 0 ? int.Parse(activeQuests[0].questID) : 0;
    public int Difficulty => 1;

    public List<QuestData> allQuests;

    private HashSet<string> completedQuests = new();
    private List<QuestData> activeQuests = new();

    private Player localPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RegisterLocalPlayer(Player player)
    {
        localPlayer = player;
    }

    private void Start()
    {
        if (allQuests.Count > 0)
            AddQuest(allQuests[0]);
    }

    public void InitQuestsForPlayer(Player player)
    {
        RegisterLocalPlayer(player);
        TryUnlockQuests(player.currentJob);
    }

    public void TryUnlockQuests(JobData jobData)
    {
        foreach (var quest in allQuests)
        {
            if (completedQuests.Contains(quest.questID) || activeQuests.Contains(quest))
                continue;

            if (quest.IsUnlocked)
            {
                if (quest.questType == QuestType.Main ||
                   (quest.questType == QuestType.Job && quest.requiredJob == jobData.jobType))
                {
                    AddQuest(quest);
                }
            }
        }
    }
    public void AddQuest(QuestData quest)
    {
        activeQuests.Add(quest);
    }

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
            Debug.LogWarning("로컬 플레이어 또는 직업 정보가 없습니다.");
        }
    }

    public bool IsQuestCompleted(QuestData quest)
    {
        return completedQuests.Contains(quest.questID);
    }
    public List<QuestData> GetActiveQuests()
    {
        return activeQuests;
    }

    public List<QuestData> GetActiveQuestsForPlayer(Player player)
    {
        List<QuestData> playerQuests = new List<QuestData>();
        foreach (var quest in activeQuests)
        {
            if (quest.questType == QuestType.Main ||
            (quest.questType == QuestType.Job && quest.requiredJob == player.CurrentJobType))
            {
                playerQuests.Add(quest);
            }
        }
        return playerQuests;
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

}