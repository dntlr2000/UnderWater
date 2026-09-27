using System.Collections.Generic;
using UnityEngine;

public enum QuestType { Main, Job }

public enum ObjectiveType
{
    CollectItem,
    KillMonster,
    VisitArea,
    FindStoryItem, // 구형 직렬화 값의 번호만 유지하며 전용 획득 처리는 사용하지 않습니다.
    CookFood,
    CraftItem,
    DismantleItem,
    PressKey
}

public enum RewardType
{
    Item,
    Money,
    Experience,
    UnlockRecipe
}

[System.Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string description;
    public ObjectiveType type;
    public int targetAmount;
    public int currentAmount;
    public string collectItemName;
}

[System.Serializable]
public class QuestReward
{
    public string rewardID;
    public RewardType rewardType;
    public int amount;
    public string itemID;
    public bool enabled;
    public RewardDestination destination;
    public string mailboxID;
}
