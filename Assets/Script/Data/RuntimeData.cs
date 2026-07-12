using System.Collections.Generic;

public class QuestRuntimeData
{
    public string questID;
    public string title;
    public string description;
    public QuestType questType;
    public JobType requiredJob;
    public string prerequisiteQuestID;
    public bool isUnlockedManually;
    public string iconPath;
    public int sortOrder;
    public List<QuestObjective> objectives = new();
    public List<QuestReward> rewards = new();
}

public class MonsterRuntimeData
{
    public string monsterID;
    public string displayName;
    public string description;
    public float maxHP;
    public float attackPower;
    public float moveSpeed;
    public string dropItemID;
    public float dropChance;
    public string spawnZoneID;
    public string modelPath;
}

public class WorkbenchRuntimeData
{
    public string workbenchID;
    public string displayName;
    public string description;
    public string requiredJob;
    public bool isPortable;
    public bool isCraftable;
}

public class RecipeRuntimeData
{
    public string recipeID;
    public string displayName;
    public string description;
    public string workbenchID;
    public string resultItemID;
    public int resultAmount;
    public string requiredJob;
    public float craftTimeSec;
    public bool isUnlockedByDefault;
    public List<RecipeIngredientData> ingredients = new();
}

public class RecipeIngredientData
{
    public string itemID;
    public int amount;
}
