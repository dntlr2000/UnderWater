using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataLoader : MonoBehaviour
{
    public static DataLoader Instance { get; private set; }

    // 각 시트별 파싱 결과 (전체 행을 그대로 보관)
    private Dictionary<string, QuestRuntimeData> _quests;
    private Dictionary<string, MonsterRuntimeData> _monsters;
    private Dictionary<string, WorkbenchRuntimeData> _workbenches;
    private Dictionary<string, RecipeRuntimeData> _recipes;
    private Dictionary<string, string> _strings;
    private Dictionary<string, JobRuntimeData> _jobs;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    private void LoadAll()
    {
        _jobs = LoadJobs();
        _quests = LoadQuests();
        _monsters = LoadMonsters();
        _workbenches = LoadWorkbenches();
        _recipes = LoadRecipes();
        _strings = LoadStrings();

        UnlockDefaultRecipes();

        Debug.Log($"[DataLoader] 로드 완료 직업:{_jobs.Count} 퀘스트:{_quests.Count} " +
                  $"몬스터:{_monsters.Count} 작업대:{_workbenches.Count} " +
                  $"레시피:{_recipes.Count} 문자열:{_strings.Count}");
    }

    public QuestRuntimeData GetQuest(string id) =>
        _quests.TryGetValue(id, out var v) ? v : null;

    public IEnumerable<QuestRuntimeData> GetAllQuests() =>
        _quests.Values;

    public MonsterRuntimeData GetMonster(string id) =>
        _monsters.TryGetValue(id, out var v) ? v : null;

    public WorkbenchRuntimeData GetWorkbench(string id) =>
        _workbenches.TryGetValue(id, out var v) ? v : null;

    public IEnumerable<RecipeRuntimeData> GetRecipesByWorkbench(string workbenchId) =>
        _recipes.Values.Where(r => r.workbenchID == workbenchId);

    public RecipeRuntimeData GetRecipe(string id) =>
        _recipes.TryGetValue(id, out var v) ? v : null;

    public string GetString(string key, string fallback = "") =>
        _strings.TryGetValue(key, out var v) ? v : fallback;

    public JobRuntimeData GetJob(string jobType) =>
        _jobs.TryGetValue(jobType, out var v) ? v : null;

    private Dictionary<string, JobRuntimeData> LoadJobs()
    {
        var result = new Dictionary<string, JobRuntimeData>();
        var rows = Parse("Data/TSV/01_Jobs");
        foreach (var row in rows)
        {
            var id = TSVParser.Get(row, "jobType");
            if (string.IsNullOrEmpty(id)) continue;
            result[id] = new JobRuntimeData
            {
                jobType = TSVParser.GetEnum<JobType>(row, "jobType"),
                jobName = TSVParser.Get(row, "jobName"),
                description = TSVParser.Get(row, "description"),
                iconPath = TSVParser.Get(row, "iconPath"),
            };
        }
        return result;
    }

    private Dictionary<string, QuestRuntimeData> LoadQuests()
    {
        var objectivesByQuest = ParseGrouped("Data/TSV/08_Objectives", "questID");
        var rewardsByQuest = ParseGrouped("Data/TSV/09_Rewards", "questID");

        var result = new Dictionary<string, QuestRuntimeData>();
        var rows = Parse("Data/TSV/07_Quests");

        foreach (var row in rows)
        {
            var id = TSVParser.Get(row, "questID");
            if (string.IsNullOrEmpty(id)) continue;

            var quest = new QuestRuntimeData
            {
                questID = id,
                title = TSVParser.Get(row, "title"),
                description = TSVParser.Get(row, "description"),
                questType = TSVParser.GetEnum<QuestType>(row, "questType"),
                requiredJob = TSVParser.GetEnum<JobType>(row, "requiredJob"),
                prerequisiteQuestID = TSVParser.Get(row, "prerequisiteQuestID"),
                isUnlockedManually = TSVParser.GetBool(row, "isUnlockedManually"),
                iconPath = TSVParser.Get(row, "iconPath"),
                sortOrder = TSVParser.GetInt(row, "sortOrder"),
            };

            if (objectivesByQuest.TryGetValue(id, out var objRows))
                quest.objectives = objRows.Select(ParseObjective).ToList();

            if (rewardsByQuest.TryGetValue(id, out var rwdRows))
                quest.rewards = rwdRows.Select(ParseReward).ToList();

            result[id] = quest;
        }
        return result;
    }

    private Dictionary<string, MonsterRuntimeData> LoadMonsters()
    {
        var result = new Dictionary<string, MonsterRuntimeData>();
        var rows = Parse("Data/TSV/06_Monsters");
        foreach (var row in rows)
        {
            var id = TSVParser.Get(row, "monsterID");
            if (string.IsNullOrEmpty(id)) continue;
            result[id] = new MonsterRuntimeData
            {
                monsterID = id,
                displayName = TSVParser.Get(row, "displayName"),
                description = TSVParser.Get(row, "description"),
                maxHP = TSVParser.GetFloat(row, "maxHP", 10f),
                attackPower = TSVParser.GetFloat(row, "attackPower"),
                moveSpeed = TSVParser.GetFloat(row, "moveSpeed", 1f),
                dropItemID = TSVParser.Get(row, "dropItemID"),
                dropChance = TSVParser.GetFloat(row, "dropChance", 1f),
                spawnZoneID = TSVParser.Get(row, "spawnZoneID"),
                modelPath = TSVParser.Get(row, "modelPath"),
            };
        }
        return result;
    }

    private Dictionary<string, WorkbenchRuntimeData> LoadWorkbenches()
    {
        var result = new Dictionary<string, WorkbenchRuntimeData>();
        var rows = Parse("Data/TSV/10_Workbenches");
        foreach (var row in rows)
        {
            var id = TSVParser.Get(row, "workbenchID");
            if (string.IsNullOrEmpty(id)) continue;
            result[id] = new WorkbenchRuntimeData
            {
                workbenchID = id,
                displayName = TSVParser.Get(row, "displayName"),
                description = TSVParser.Get(row, "description"),
                requiredJob = TSVParser.Get(row, "requiredJob"),
                isPortable = TSVParser.GetBool(row, "isPortable"),
                isCraftable = TSVParser.GetBool(row, "isCraftable"),
            };
        }
        return result;
    }

    private Dictionary<string, RecipeRuntimeData> LoadRecipes()
    {
        var ingredientsByRecipe = ParseGrouped("Data/TSV/12_RecipeIngredients", "recipeID");

        var result = new Dictionary<string, RecipeRuntimeData>();
        var rows = Parse("Data/TSV/11_Recipes");
        foreach (var row in rows)
        {
            var id = TSVParser.Get(row, "recipeID");
            if (string.IsNullOrEmpty(id)) continue;

            var recipe = new RecipeRuntimeData
            {
                recipeID = id,
                displayName = TSVParser.Get(row, "displayName"),
                description = TSVParser.Get(row, "description"),
                workbenchID = TSVParser.Get(row, "workbenchID"),
                resultItemID = TSVParser.Get(row, "resultItemID"),
                resultAmount = TSVParser.GetInt(row, "resultAmount", 1),
                requiredJob = TSVParser.Get(row, "requiredJob"),
                craftTimeSec = TSVParser.GetFloat(row, "craftTimeSec"),
                isUnlockedByDefault = TSVParser.GetBool(row, "isUnlockedByDefault"),
            };

            if (ingredientsByRecipe.TryGetValue(id, out var ingRows))
                recipe.ingredients = ingRows
                    .OrderBy(r => TSVParser.GetInt(r, "seq"))
                    .Select(r => new RecipeIngredientData
                    {
                        itemID = TSVParser.Get(r, "itemID"),
                        amount = TSVParser.GetInt(r, "amount", 1),
                    }).ToList();

            result[id] = recipe;
        }
        return result;
    }

    private Dictionary<string, string> LoadStrings()
    {
        var result = new Dictionary<string, string>();
        var rows = Parse("Data/TSV/02_Strings");
        foreach (var row in rows)
        {
            var key = TSVParser.Get(row, "stringKey");
            if (string.IsNullOrEmpty(key)) continue;
            result[key] = TSVParser.Get(row, "KO");
        }
        return result;
    }

    private QuestObjective ParseObjective(Dictionary<string, string> row) => new QuestObjective
    {
        objectiveID = TSVParser.Get(row, "objectiveID"),
        description = TSVParser.Get(row, "description"),
        type = TSVParser.GetEnum<ObjectiveType>(row, "objectiveType"),
        targetAmount = TSVParser.GetInt(row, "targetAmount", 1),
        currentAmount = 0,
        collectItemName = TSVParser.Get(row, "collectItemID"),
    };

    private QuestReward ParseReward(Dictionary<string, string> row) => new QuestReward
    {
        rewardType = TSVParser.GetEnum<RewardType>(row, "rewardType"),
        amount = TSVParser.GetInt(row, "amount"),
    };

    private List<Dictionary<string, string>> Parse(string path)
    {
        var asset = TSVParser.LoadTSV(path);
        return asset != null ? TSVParser.Parse(asset.text) : new List<Dictionary<string, string>>();
    }

    private Dictionary<string, List<Dictionary<string, string>>> ParseGrouped(string path, string groupKey)
    {
        return Parse(path)
            .GroupBy(r => TSVParser.Get(r, groupKey))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private void UnlockDefaultRecipes()
    {
        if (GlobalUnlockManager.Instance == null)
        {
            Debug.LogWarning("[DataLoader] GlobalUnlockManager가 없어 기본 레시피 해금 불가");
            return;
        }

        foreach (var recipe in _recipes.Values)
        {
            if (recipe.isUnlockedByDefault)
            {
                GlobalUnlockManager.Instance.UnlockItem(recipe.recipeID);
                Debug.Log($"[DataLoader] 기본 해금: {recipe.recipeID}");
            }
        }
    }
}