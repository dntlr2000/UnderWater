/*using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataLoadeone : MonoBehaviour
{
    public static DataLoadeone Instance { get; private set; }

    [Header("Databases")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private StringTable stringTable;

    [Header("Language")]
    [SerializeField] private StringTable.Language language = StringTable.Language.KO;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    private void LoadAll()
    {
        var itemRows = ParseTSV("Data/TSV/04_Items");
        var questRows = ParseTSV("Data/TSV/01_Quests");
        var objectiveRows = ParseTSV("Data/TSV/02_Objectives");
        var rewardRows = ParseTSV("Data/TSV/03_Rewards");
        var stringRows = ParseTSV("Data/TSV/06_Strings");

        itemDatabase.Build(BuildItems(itemRows));
        questDatabase.Build(BuildQuests(questRows, objectiveRows, rewardRows));
        stringTable.Build(stringRows, language);

        Debug.Log("[DataLoader] 모든 TSV 로드 완료");
    }

    // ── Items ──────────────────────────────────────────
    private List<ItemRuntimeData> BuildItems(List<Dictionary<string, string>> rows)
    {
        var list = new List<ItemRuntimeData>();
        foreach (var row in rows)
        {
            var id = TSVParser.Get(row, "itemID");
            if (string.IsNullOrEmpty(id)) continue;

            var data = new ItemRuntimeData
            {
                itemID = id,
                displayName = TSVParser.Get(row, "displayName"),
                description = TSVParser.Get(row, "description"),
                itemType = TSVParser.GetEnum<ItemType>(row, "itemType"),
                maxStack = TSVParser.GetInt(row, "maxStack", 1),
                baseValue = TSVParser.GetFloat(row, "baseValue"),
                weight = TSVParser.GetFloat(row, "weight"),
                iconPath = TSVParser.Get(row, "iconPath"),
                isDroppable = TSVParser.GetBool(row, "isDroppable"),
                isTradeable = TSVParser.GetBool(row, "isTradeable"),
                rarity = TSVParser.Get(row, "rarity"),
            };

            var iconPath = TSVParser.Get(row, "iconPath");
            if (!string.IsNullOrEmpty(iconPath))
                data.icon = Resources.Load<Sprite>(iconPath);

            list.Add(data);
        }
        return list;
    }

    // ── Quests ─────────────────────────────────────────
    private List<QuestRuntimeData> BuildQuests(
        List<Dictionary<string, string>> questRows,
        List<Dictionary<string, string>> objectiveRows,
        List<Dictionary<string, string>> rewardRows)
    {
        // Objectives / Rewards를 questID로 그룹핑
        var objectiveMap = objectiveRows
            .GroupBy(r => TSVParser.Get(r, "questID"))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => TSVParser.GetInt(r, "seq")).ToList());

        var rewardMap = rewardRows
            .GroupBy(r => TSVParser.Get(r, "questID"))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => TSVParser.GetInt(r, "seq")).ToList());

        var list = new List<QuestRuntimeData>();

        foreach (var row in questRows)
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

            if (objectiveMap.TryGetValue(id, out var objRows))
                quest.objectives = objRows.Select(ParseObjective).ToList();

            if (rewardMap.TryGetValue(id, out var rwdRows))
                quest.rewards = rwdRows.Select(ParseReward).ToList();

            list.Add(quest);
        }

        return list;
    }

    private QuestObjective ParseObjective(Dictionary<string, string> row)
    {
        return new QuestObjective
        {
            objectiveID = TSVParser.Get(row, "objectiveID"),
            description = TSVParser.Get(row, "description"),
            type = TSVParser.GetEnum<ObjectiveType>(row, "objectiveType"),
            targetAmount = TSVParser.GetInt(row, "targetAmount", 1),
            currentAmount = 0,
            collectItemName = TSVParser.Get(row, "collectItemID"),
        };
    }

    private QuestReward ParseReward(Dictionary<string, string> row)
    {
        return new QuestReward
        {
            rewardType = TSVParser.GetEnum<RewardType>(row, "rewardType"),
            amount = TSVParser.GetInt(row, "amount"),
        };
    }

    private List<Dictionary<string, string>> ParseTSV(string path)
    {
        var asset = TSVParser.LoadTSV(path);
        return asset != null ? TSVParser.Parse(asset.text) : new List<Dictionary<string, string>>();
    }
}*/