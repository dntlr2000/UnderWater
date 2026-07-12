using System.Collections.Generic;
using UnityEngine;

public enum QuestType { Main, Job }

public enum ObjectiveType
{
    CollectItem,
    KillMonster,
    VisitArea,
    FindStoryItem,
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
    public RewardType rewardType;
    public int amount;
}