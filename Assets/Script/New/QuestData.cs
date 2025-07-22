using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public enum QuestType { Main, Job }
public enum ObjectiveType
{
    CollectItem,   // 아이템 수집
    KillMonster,   // 몬스터 처치
    VisitArea,     // 지역 탐방
    FindStoryItem, // 스토리 아이템 수집
    CookFood,      // 요리
    CraftItem,     // 제작
    DismantleItem  // 분해
}

public enum RewardType
{
    Item,
    Money,
    Experience,
    UnlockRecipe
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    public string questID;
    public string title;
    public string description;
    public Sprite icon;

    public QuestType questType;
    public JobType requiredJob;

    public List<QuestObjective> objectives = new();
    public List<QuestReward> rewards = new();

    public QuestData prerequisiteQuest; //선행퀘스트
    public bool isCompleted;

    public bool IsUnlocked => prerequisiteQuest == null || QuestManager.Instance.IsQuestCompleted(prerequisiteQuest);
    
}

[System.Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string description;
    public ObjectiveType type;
    public int targetAmount;
    public int currentAmount;
}

[System.Serializable]
public class QuestReward
{
    public RewardType rewardType;
    public int amount;
}