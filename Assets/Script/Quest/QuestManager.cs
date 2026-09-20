using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(QuestNetworkBridge), typeof(RewardDeliveryService))]
public class QuestManager : MonoBehaviour
{
    public const string TutorialQuestId = "quest_main_tutorial_001";
    public static QuestManager Instance;
    public event Action OnQuestListUpdated;
    public int Difficulty => 1;
    public bool IsInitialized { get; private set; }
    public bool HasSharedState => sharedState.initialized;
    public bool CanWriteMain => PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
    public List<QuestRuntimeData> allQuests = new();
    private readonly HashSet<string> completedQuests = new();
    private readonly List<QuestRuntimeData> activeQuests = new();
    private SharedQuestState sharedState = new();
    private Player localPlayer;
    private QuestNetworkBridge bridge;
    private RewardDeliveryService rewards;
    private readonly List<QuestRewardClaim> jobRewardClaims = new();
    private string completingPlayerId;
    public RewardDeliveryState DeliveryState => sharedState.rewards ??= new RewardDeliveryState();
    public int SharedRevision => sharedState.revision;

    // 단일 관리자와 네트워크 전달자를 연결합니다.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        bridge = GetComponent<QuestNetworkBridge>();
        if (bridge == null) bridge = gameObject.AddComponent<QuestNetworkBridge>();
        rewards = GetComponent<RewardDeliveryService>();
        if (rewards == null) rewards = gameObject.AddComponent<RewardDeliveryService>();
        rewards.Configure(this, bridge);
    }

    // 씬 종료 후 정적 참조를 정리합니다.
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // 플레이어와 저장 데이터가 준비된 뒤 메인/직업 진행을 한 번 복원합니다.
    public void InitializeForSession(Player player, SaveData save, bool loadedGame)
    {
        if (IsInitialized || player == null || save == null || DataLoader.Instance == null) return;
        allQuests = DataLoader.Instance.GetAllQuests().Select(CloneQuest).ToList();
        localPlayer = player;
        IsInitialized = true;
        save.worldProgress ??= new WorldProgress();
        if (string.IsNullOrEmpty(save.saveId)) save.saveId = PhotonNetwork.CurrentRoom?.Name ?? "offline";
        var state = bridge.ReadRoomState(save.saveId);
        if (CanWriteMain)
        {
            if (state == null)
            {
                state = save.worldProgress.mainQuests ?? new SharedQuestState();
                if (save.questSaveVersion < 1) state = MigrateLegacyMain(save);
                state.saveId = save.saveId;
                state.initialized = true;
            }
            ApplySharedState(state, true);
            UnlockEligibleQuests(QuestType.Main);
        }
        else if (state != null) ApplySharedState(state, true);
        var personal = save.players?.FirstOrDefault(p => p.playerId == QuestNetworkBridge.LocalPlayerId);
        jobRewardClaims.Clear();
        if (personal?.rewardClaims != null) jobRewardClaims.AddRange(personal.rewardClaims);
        RestoreQuests(QuestType.Job, personal?.completedQuestIds, personal?.activeQuests);
        UnlockEligibleQuests(QuestType.Job);
        if (CanWriteMain) CommitMainChanges();
        NotifyUIUpdate();
        bridge.PublishLocalJobState();
    }

    // 최초 공유 발행 전에 방장이 떠난 경우에도 저장 데이터로 메인을 시작합니다.
    public void RestoreMainAfterMasterChange()
    {
        if (!IsInitialized || !CanWriteMain) return;
        var save = SaveManager.Instance.GetCurrentSave();
        if (save == null) return;
        var state = save.worldProgress?.mainQuests ?? new SharedQuestState();
        if (save.questSaveVersion < 1) state = MigrateLegacyMain(save);
        state.saveId = save.saveId;
        state.initialized = true;
        ApplySharedState(state, true);
        UnlockEligibleQuests(QuestType.Main);
        CommitMainChanges();
        NotifyUIUpdate();
    }

    // 구형 저장 소유자의 메인 진행을 옮기고 새 튜토리얼만 건너뜁니다.
    private SharedQuestState MigrateLegacyMain(SaveData save)
    {
        string owner = string.IsNullOrEmpty(save.saveOwnerId) ? QuestNetworkBridge.LocalPlayerId : save.saveOwnerId;
        var old = save.players?.FirstOrDefault(p => p.playerId == owner) ?? save.players?.FirstOrDefault();
        var ids = new HashSet<string>(allQuests.Where(q => q.questType == QuestType.Main).Select(q => q.questID));
        var state = new SharedQuestState { initialized = true, saveId = save.saveId };
        state.completedQuestIds = old?.completedQuestIds?.Where(ids.Contains).Distinct().ToList() ?? new();
        state.activeQuests = old?.activeQuests?.Where(q => ids.Contains(q.questId)).ToList() ?? new();
        if (!state.completedQuestIds.Contains(TutorialQuestId)) state.completedQuestIds.Add(TutorialQuestId);
        return state;
    }

    // 기존 등록 호출과 호환하면서 로컬 플레이어 참조를 갱신합니다.
    public void RegisterLocalPlayer(Player player)
    {
        if (player == null) return;
        localPlayer = player;
        if (IsInitialized) InitStartingQuests();
    }

    // 메인 권한과 개인 직업 조건을 구분하여 시작 퀘스트를 해제합니다.
    public void InitStartingQuests()
    {
        if (!IsInitialized) return;
        bool mainChanged = CanWriteMain && UnlockEligibleQuests(QuestType.Main);
        bool jobChanged = UnlockEligibleQuests(QuestType.Job);
        if (mainChanged) CommitMainChanges();
        if (jobChanged) bridge.PublishLocalJobState();
        NotifyUIUpdate();
    }

    // 기존 해제 API를 유지하며 메인이 직업 준비에 종속되지 않도록 합니다.
    public void TryUnlockQuests(JobData jobData) => InitStartingQuests();

    // 조건을 만족하는 미등록 퀘스트를 실행용 복사본으로 추가합니다.
    private bool UnlockEligibleQuests(QuestType type)
    {
        bool changed = false;
        foreach (var quest in allQuests.Where(q => q.questType == type).OrderBy(q => q.sortOrder))
        {
            if (completedQuests.Contains(quest.questID) || activeQuests.Any(q => q.questID == quest.questID)) continue;
            if (type == QuestType.Job && (localPlayer?.currentJob == null || quest.requiredJob != localPlayer.CurrentJobType)) continue;
            if (!IsUnlocked(quest)) continue;
            activeQuests.Add(CloneQuest(quest));
            changed = true;
        }
        return changed;
    }

    // 기존 수동 해제 플래그와 선행 완료 조건을 평가합니다.
    private bool IsUnlocked(QuestRuntimeData quest) => quest.isUnlockedManually || string.IsNullOrEmpty(quest.prerequisiteQuestID) || completedQuests.Contains(quest.prerequisiteQuestID);

    // 외부 등록도 권한, 중복, 직업 및 선행 조건을 검사합니다.
    public void AddQuest(QuestRuntimeData quest)
    {
        if (!IsInitialized || quest == null || completedQuests.Contains(quest.questID) || activeQuests.Any(q => q.questID == quest.questID)) return;
        if (!IsUnlocked(quest) || (quest.questType == QuestType.Main && !CanWriteMain)) return;
        if (quest.questType == QuestType.Job && (localPlayer?.currentJob == null || quest.requiredJob != localPlayer.CurrentJobType)) return;
        activeQuests.Add(CloneQuest(quest));
        if (quest.questType == QuestType.Main) CommitMainChanges(); else bridge.PublishLocalJobState();
        NotifyUIUpdate();
    }

    // UI 완료 요청을 메인 권한자 또는 개인 직업 완료 경로로 보냅니다.
    public void CompleteQuest(QuestRuntimeData quest)
    {
        if (!IsInitialized || quest == null) return;
        if (quest.questType == QuestType.Main) { bridge.RequestMainCompletion(quest.questID); return; }
        if (!CompleteQuestCore(quest.questID, QuestType.Job)) return;
        bridge.PublishLocalJobState();
        NotifyUIUpdate();
        SaveManager.Instance.SaveGame();
    }

    // 방장이 현재 목표를 재검사하고 메인 완료 결과를 공유합니다.
    public bool CompleteMainQuest(string questId, string playerId = null)
    {
        completingPlayerId = playerId ?? QuestNetworkBridge.LocalPlayerId;
        if (!IsInitialized || !CanWriteMain || !CompleteQuestCore(questId, QuestType.Main)) return false;
        CommitMainChanges();
        NotifyUIUpdate();
        SaveManager.Instance.SaveGame();
        return true;
    }

    // 활성 퀘스트와 목표를 확인한 뒤 한 번만 완료 및 후속 해제를 수행합니다.
    private bool CompleteQuestCore(string questId, QuestType type)
    {
        var quest = activeQuests.FirstOrDefault(q => q.questID == questId && q.questType == type);
        if (quest == null || completedQuests.Contains(questId) || !AreObjectivesComplete(quest)) return false;
        if (type == QuestType.Job && (localPlayer?.currentJob == null || quest.requiredJob != localPlayer.CurrentJobType)) return false;
        completedQuests.Add(questId);
        activeQuests.Remove(quest);
        GrantRewards(quest);
        UnlockEligibleQuests(type);
        return true;
    }

    // 빈 목표의 오완료를 막고 모든 목표 달성 여부를 반환합니다.
    public static bool AreObjectivesComplete(QuestRuntimeData quest) => quest?.objectives != null && quest.objectives.Count > 0 && quest.objectives.All(o => o.currentAmount >= o.targetAmount);

    // 모든 화면에 변경 후 상태를 알립니다.
    private void NotifyUIUpdate() => OnQuestListUpdated?.Invoke();

    // 완료 ID를 조회합니다.
    public bool IsQuestCompleted(string questID) => !string.IsNullOrEmpty(questID) && completedQuests.Contains(questID);

    // 외부에서 활성 목록 자체를 바꾸지 못하도록 목록을 복사합니다.
    public List<QuestRuntimeData> GetActiveQuests() => activeQuests.ToList();

    // 메인과 플레이어의 준비된 직업 퀘스트만 표시합니다.
    public List<QuestRuntimeData> GetActiveQuestsForPlayer(Player player) => activeQuests.Where(q => q.questType == QuestType.Main || (player?.currentJob != null && q.requiredJob == player.CurrentJobType)).ToList();

    // 메인은 팀 보상을 등록하고 직업은 완료자의 보상 요청을 개인 저장에 남깁니다.
    private void GrantRewards(QuestRuntimeData quest)
    {
        if (quest.questType == QuestType.Main) rewards.RegisterMainRewards(quest, completingPlayerId);
        else foreach (var reward in quest.rewards.Where(RewardDeliveryService.IsSupported))
        {
            if (jobRewardClaims.Any(c => c.questId == quest.questID && c.rewardId == reward.rewardID)) continue;
            jobRewardClaims.Add(new QuestRewardClaim { questId = quest.questID, rewardId = reward.rewardID });
        }
    }

    // 개인 보상 요청을 복사하여 외부 목록 변경이 완료 상태에 영향을 주지 않게 합니다.
    public List<QuestRewardClaim> GetJobRewardClaims() => jobRewardClaims
        .Select(c => new QuestRewardClaim { questId = c.questId, rewardId = c.rewardId }).ToList();

    // 지급 결과와 원본 상자/인벤토리를 같은 공유 버전 및 저장 스냅샷에 확정합니다.
    public void CommitDeliveryChanges()
    {
        if (!IsInitialized || !CanWriteMain) return;
        CommitMainChanges();
        SaveManager.Instance.SaveGame();
    }

    // 개인 저장에는 직업 퀘스트만 포함합니다.
    public (List<string> completed, List<QuestProgressData> active) GetQuestSaveData() => (CompletedFor(QuestType.Job), ProgressFor(QuestType.Job));

    // 기존 개인 복원 호출은 공유 메인을 보존하고 직업만 복원합니다.
    public void LoadQuestSaveData(List<string> completed, List<QuestProgressData> active, JobData jobData)
    {
        if (!IsInitialized) return;
        RestoreQuests(QuestType.Job, completed, active);
        UnlockEligibleQuests(QuestType.Job);
        NotifyUIUpdate();
        bridge.PublishLocalJobState();
    }

    // 한 종류만 교체하며 목표 ID 우선, 구형 저장은 인덱스로 진행을 복원합니다.
    private void RestoreQuests(QuestType type, List<string> completed, List<QuestProgressData> active)
    {
        var ids = new HashSet<string>(allQuests.Where(q => q.questType == type).Select(q => q.questID));
        completedQuests.RemoveWhere(ids.Contains);
        if (completed != null) completedQuests.UnionWith(completed.Where(ids.Contains));
        activeQuests.RemoveAll(q => q.questType == type);
        if (active == null) return;
        foreach (var progress in active)
        {
            if (progress == null || !ids.Contains(progress.questId) || completedQuests.Contains(progress.questId) || activeQuests.Any(q => q.questID == progress.questId)) continue;
            var quest = CloneQuest(allQuests.First(q => q.questID == progress.questId));
            for (int i = 0; i < quest.objectives.Count; i++)
            {
                int index = progress.objectiveIds?.Length > 0 ? Array.IndexOf(progress.objectiveIds, quest.objectives[i].objectiveID) : i;
                if (progress.objectiveCounts != null && index >= 0 && index < progress.objectiveCounts.Length)
                    quest.objectives[i].currentAmount = Mathf.Clamp(progress.objectiveCounts[index], 0, quest.objectives[i].targetAmount);
            }
            activeQuests.Add(quest);
        }
    }

    // 로컬 행동은 개인 직업에 반영하고 공유 메인에는 요청으로 전달합니다.
    public void ReportObjectiveProgress(ObjectiveType type, int amount = 1, string itemID = "")
    {
        if (!IsInitialized || amount <= 0 || type == ObjectiveType.FindStoryItem) return;
        ReportLocalJobProgress(type, amount, itemID);
        bridge.RequestMainProgress(type, amount, itemID);
    }

    // 로컬 행동을 개인 직업 목표에 적용하고 변경된 진행을 공유합니다.
    public void ReportLocalJobProgress(ObjectiveType type, int amount, string itemId)
    {
        if (!IsInitialized || amount <= 0 || !ApplyProgress(QuestType.Job, type, amount, itemId)) return;
        bridge.PublishLocalJobState();
        NotifyUIUpdate();
    }

    // 재전송을 제외하고 획득 직후 인벤토리와 메인 진행을 함께 반영합니다.
    public void ApplyMainProgress(string eventId, ObjectiveType type, int amount, string itemId, PlayerData acquiredPlayer = null, string playerId = null)
    {
        if (!CanWriteMain || !IsInitialized || type == ObjectiveType.FindStoryItem || amount <= 0 || string.IsNullOrEmpty(eventId)) return;
        if (sharedState.processedEvents.Contains(eventId)) return;
        sharedState.processedEvents.Add(eventId);
        if (sharedState.processedEvents.Count > 512) sharedState.processedEvents.RemoveAt(0);
        if (type == ObjectiveType.CollectItem && acquiredPlayer?.items != null)
            SaveManager.Instance.UpdatePlayerCache(acquiredPlayer);
        completingPlayerId = playerId ?? acquiredPlayer?.playerId ?? QuestNetworkBridge.LocalPlayerId;
        int completedBefore = completedQuests.Count;
        ApplyProgress(QuestType.Main, type, amount, itemId);
        CommitMainChanges();
        NotifyUIUpdate();
        // 수집형 튜토리얼도 자동 완료 직후 최신 획득 정보로 저장합니다.
        if (completedQuests.Count > completedBefore) SaveManager.Instance.SaveGame();
    }

    // 유형과 식별자를 검사하고 목록 순회 후 자동 완료를 처리합니다.
    private bool ApplyProgress(QuestType questType, ObjectiveType type, int amount, string itemId)
    {
        bool changed = false;
        var candidates = activeQuests.Where(q => q.questType == questType).ToList();
        foreach (var quest in candidates)
        {
            if (questType == QuestType.Job && (localPlayer?.currentJob == null || quest.requiredJob != localPlayer.CurrentJobType)) continue;
            foreach (var objective in quest.objectives)
            {
                if (objective.type != type || objective.currentAmount >= objective.targetAmount) continue;
                if ((type == ObjectiveType.CollectItem || type == ObjectiveType.CraftItem) &&
                    (string.IsNullOrEmpty(itemId) || (!string.IsNullOrEmpty(objective.collectItemName) && objective.collectItemName != itemId))) continue;
                objective.currentAmount += Math.Min(amount, objective.targetAmount - objective.currentAmount);
                changed = true;
            }
        }
        foreach (var quest in candidates.Where(q => q.autoComplete && AreObjectivesComplete(q))) CompleteQuestCore(quest.questID, questType);
        return changed;
    }

    // 네트워크와 저장 계층에 공유 상태 복사본을 제공합니다.
    public SharedQuestState CaptureSharedState() => JsonUtility.FromJson<SharedQuestState>(JsonUtility.ToJson(sharedState));

    // 공유 메인만 교체하고 개인 직업 진행을 보존합니다.
    public void ApplySharedState(SharedQuestState state, bool force = false)
    {
        if (!IsInitialized || state == null || !state.initialized) return;
        if (!force && sharedState.initialized && state.revision <= sharedState.revision) return;
        sharedState = JsonUtility.FromJson<SharedQuestState>(JsonUtility.ToJson(state));
        sharedState.completedQuestIds ??= new(); sharedState.activeQuests ??= new();
        sharedState.processedEvents ??= new();
        sharedState.rewards ??= new();
        rewards.ApplySharedState();
        RestoreQuests(QuestType.Main, sharedState.completedQuestIds, sharedState.activeQuests);
        SaveManager.Instance.StoreSharedQuestState(sharedState);
        NotifyUIUpdate();
    }

    // 메인 변경을 한 버전으로 묶어 저장 캐시와 방 상태에 반영합니다.
    private void CommitMainChanges()
    {
        sharedState.completedQuestIds = CompletedFor(QuestType.Main);
        sharedState.activeQuests = ProgressFor(QuestType.Main);
        sharedState.revision++;
        sharedState.initialized = true;
        rewards.ApplySharedState();
        SaveManager.Instance.StoreSharedQuestState(sharedState);
        bridge.PublishSharedState(CaptureSharedState());
    }

    // 지정 종류의 완료 ID만 추출합니다.
    private List<string> CompletedFor(QuestType type) => allQuests.Where(q => q.questType == type && completedQuests.Contains(q.questID)).Select(q => q.questID).ToList();

    // 목표 ID와 수량을 함께 저장하여 데이터 행 순서 변경에 대응합니다.
    private List<QuestProgressData> ProgressFor(QuestType type) => activeQuests.Where(q => q.questType == type).Select(q => new QuestProgressData
    {
        questId = q.questID, objectiveIds = q.objectives.Select(o => o.objectiveID).ToArray(),
        objectiveCounts = q.objectives.Select(o => o.currentAmount).ToArray()
    }).ToList();

    // 정의의 목표 수량이 실행 중 변경되지 않도록 깊게 복사합니다.
    private static QuestRuntimeData CloneQuest(QuestRuntimeData source)
    {
        return new QuestRuntimeData
        {
            questID = source.questID, title = source.title, description = source.description,
            questType = source.questType, requiredJob = source.requiredJob, prerequisiteQuestID = source.prerequisiteQuestID,
            isUnlockedManually = source.isUnlockedManually, iconPath = source.iconPath, sortOrder = source.sortOrder,
            storyMessage = source.storyMessage, autoComplete = source.autoComplete,
            objectives = source.objectives.Select(o => new QuestObjective { objectiveID = o.objectiveID, description = o.description,
                type = o.type, targetAmount = o.targetAmount, currentAmount = 0, collectItemName = o.collectItemName }).ToList(),
            rewards = source.rewards.Select(r => new QuestReward { rewardID = r.rewardID, rewardType = r.rewardType,
                amount = r.amount, itemID = r.itemID, enabled = r.enabled, destination = r.destination, mailboxID = r.mailboxID }).ToList()
        };
    }
}
