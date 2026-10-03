using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class RewardDeliveryService : MonoBehaviour
{
    public static RewardDeliveryService Instance { get; private set; }
    public static event Action<string> OnNotice;
    private QuestManager manager;
    private QuestNetworkBridge bridge;
    private float nextScan;
    private bool processingRequested = true;
    private bool processing;
    private bool wasMaster;
    private int processedFrame = -1;
    private readonly Dictionary<string, float> retryAt = new();
    private static readonly HashSet<string> duplicateBoxWarnings = new();
    private string observedSaveId;
    private readonly HashSet<string> announced = new();
    private readonly HashSet<string> blockedNotices = new();
    public bool IsReady => manager != null && manager.IsInitialized && manager.HasSharedState;
    private RewardDeliveryState State => manager.DeliveryState;
    private string SaveId => SaveManager.Instance?.GetCurrentSave()?.saveId ?? "";

    // 퀘스트 관리자와 전달자를 연결하며 씬 설정을 추가로 요구하지 않습니다.
    private void Awake()
    {
        Configure(GetComponent<QuestManager>(), GetComponent<QuestNetworkBridge>());
    }

    // 컴포넌트 Awake 순서와 관계없이 관리자가 준비된 참조를 전달할 수 있게 합니다.
    public void Configure(QuestManager questManager, QuestNetworkBridge networkBridge)
    {
        Instance = this;
        manager = questManager;
        bridge = networkBridge;
        RequestProcessing();
    }

    // 씬 종료 후 정적 서비스 참조를 정리합니다.
    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        duplicateBoxWarnings.Clear();
    }

    // 최초 지급은 다음 프레임에 처리하고, 응답이 없는 지급만 각자의 재시도 시각까지 기다립니다.
    private void Update()
    {
        bool master = IsReady && manager.CanWriteMain;
        if (master != wasMaster)
        {
            wasMaster = master;
            retryAt.Clear();
            RequestProcessing();
        }
        if (!master || bridge.IsRecoveringPlayerStates) return;
        if (!processingRequested && Time.unscaledTime < nextScan) return;
        ProcessPending();
    }

    // 같은 프레임의 변경 알림을 합치며 콜백 안에서 지급 처리를 재귀 호출하지 않습니다.
    public void RequestProcessing() => processingRequested = true;

    // 확정된 지급 건만 실행하며 다른 변경의 공유 버전이나 다른 지급의 재시도에 막히지 않게 합니다.
    public void ProcessPending()
    {
        if (!IsReady || !manager.CanWriteMain || bridge.IsRecoveringPlayerStates) return;
        if (processing || processedFrame == Time.frameCount) { RequestProcessing(); return; }
        processing = true;
        processedFrame = Time.frameCount;
        processingRequested = false;
        float now = Time.unscaledTime;
        nextScan = now + 1f;
        try
        {
            if (RegisterJobClaims()) manager.CommitDeliveryChanges();
            var confirmed = bridge.ReadRoomState(SaveId)?.rewards?.deliveries;
            if (confirmed == null) return;
            foreach (string id in State.deliveries.Where(d => d.status == RewardDeliveryStatus.Pending).Select(d => d.deliveryId).ToList())
            {
                var delivery = FindDelivery(id);
                if (!MatchesConfirmedDelivery(delivery, confirmed.FirstOrDefault(d => d.deliveryId == id))) continue;
                if (retryAt.TryGetValue(id, out float retry) && now < retry)
                {
                    nextScan = Math.Min(nextScan, retry);
                    continue;
                }
                // 재시도 기한은 실제 전달을 시도할 때만 설정하므로 새 공유 확인을 지연시키지 않습니다.
                retryAt[id] = now + 1f;
                if (!string.IsNullOrEmpty(delivery.sourceFieldId) && FieldItem.FindPickup(delivery.sourceFieldId) == null) continue;
                if (delivery.destination == RewardDestination.Mailbox) TryMailboxDelivery(delivery);
                else
                {
                    var recipient = PhotonNetwork.PlayerList.FirstOrDefault(p => QuestNetworkBridge.PlayerId(p) == delivery.recipientPlayerId);
                    if (recipient != null && !recipient.IsInactive) bridge.SendDeliveryOffer(recipient.ActorNumber, id);
                }
            }
            foreach (string id in retryAt.Keys.ToList())
                if (FindDelivery(id)?.status != RewardDeliveryStatus.Pending) retryAt.Remove(id);
        }
        finally { processing = false; }
    }

    // JSON의 null/빈 문자열 차이는 허용하되 지급 대상·수량·원본이 서버 확정 내용과 같은지 검사합니다.
    private static bool MatchesConfirmedDelivery(RewardDelivery local, RewardDelivery confirmed) =>
        local != null && confirmed != null && local.status == RewardDeliveryStatus.Pending && confirmed.status == RewardDeliveryStatus.Pending &&
        SameText(local.deliveryId, confirmed.deliveryId) && SameText(local.questId, confirmed.questId) &&
        SameText(local.recipientPlayerId, confirmed.recipientPlayerId) && SameText(local.mailboxId, confirmed.mailboxId) &&
        SameText(local.sourceBoxId, confirmed.sourceBoxId) && SameText(local.sourceFieldId, confirmed.sourceFieldId) &&
        local.sourceSlot == confirmed.sourceSlot && local.rewardType == confirmed.rewardType && local.destination == confirmed.destination &&
        local.itemId == confirmed.itemId && local.amount == confirmed.amount && local.durability.Equals(confirmed.durability);

    // Unity JSON에서 빈 값으로 복원되는 문자열을 동일한 값으로 비교합니다.
    private static bool SameText(string left, string right) => string.Equals(left ?? "", right ?? "", StringComparison.Ordinal);

    // 유효한 보상만 완료 시점에 등록하며 완료된 옛 저장에는 소급 생성하지 않습니다.
    public void RegisterMainRewards(QuestRuntimeData quest, string completingPlayerId)
    {
        if (!IsReady || !manager.CanWriteMain) return;
        foreach (var reward in quest.rewards.Where(IsSupported))
            AddQuestDelivery(quest, reward, "team", completingPlayerId);
    }

    // 직업 완료 요청은 소유자의 직업 및 완료 기록과 함께 확인합니다.
    private bool RegisterJobClaims()
    {
        bool changed = false;
        var players = SaveManager.Instance.GetCurrentSave()?.players;
        if (players == null) return false;
        foreach (var player in players.ToList())
        {
            if (player.rewardClaims == null || player.completedQuestIds == null) continue;
            foreach (var claim in player.rewardClaims)
            {
                var quest = manager.allQuests.FirstOrDefault(q => q.questID == claim.questId && q.questType == QuestType.Job);
                if (quest == null || !player.completedQuestIds.Contains(quest.questID) ||
                    !string.Equals(quest.requiredJob.ToString(), player.jobType, StringComparison.OrdinalIgnoreCase)) continue;
                var reward = quest.rewards.FirstOrDefault(r => r.rewardID == claim.rewardId && IsSupported(r));
                if (reward != null) changed |= AddQuestDelivery(quest, reward, player.playerId, player.playerId);
            }
        }
        return changed;
    }

    // 퀘스트·보상·완료 주체로 고정 키를 만들어 재요청에도 한 건만 생성합니다.
    private bool AddQuestDelivery(QuestRuntimeData quest, QuestReward reward, string scope, string playerId)
    {
        int itemId = -1;
        if (reward.rewardType == RewardType.Item)
        {
            var item = ItemDatabase.Instance?.GetItemByStringId(reward.itemID);
            if (item == null) return false;
            itemId = item.itemId;
        }
        string id = $"{SaveId}/quest/{quest.questID}/{scope}/{reward.rewardID}";
        return AddDelivery(new RewardDelivery {
            deliveryId = id, questId = quest.questID, rewardType = reward.rewardType,
            destination = reward.destination, recipientPlayerId = playerId, mailboxId = reward.mailboxID,
            itemId = itemId, amount = reward.amount,
            durability = itemId >= 0 ? ItemDatabase.Instance.GetItem(itemId).durability : -1f
        });
    }

    // 퀘스트 밖에서도 방장이 특정 플레이어 또는 공용 우편함에 아이템 지급을 등록합니다.
    public bool QueueItem(string sourceId, int itemId, int amount, RewardDestination destination,
        string recipientPlayerId = "", string mailboxId = "Mailbox")
    {
        var item = ItemDatabase.Instance?.GetItem(itemId);
        if (item == null) return false;
        return QueueExternal(sourceId, new RewardDelivery { rewardType = RewardType.Item, itemId = itemId,
            amount = amount, durability = item.durability, destination = destination,
            recipientPlayerId = recipientPlayerId, mailboxId = mailboxId });
    }

    // 퀘스트 밖에서도 동일한 중복 방지 경로로 돈 지급을 등록합니다.
    public bool QueueMoney(string sourceId, int amount, RewardDestination destination,
        string recipientPlayerId = "", string mailboxId = "Mailbox")
    {
        return QueueExternal(sourceId, new RewardDelivery { rewardType = RewardType.Money, amount = amount,
            destination = destination, recipientPlayerId = recipientPlayerId, mailboxId = mailboxId });
    }

    // 호출자가 유지하는 업무 ID를 저장 세션과 결합하여 한 번만 지급하도록 확정합니다.
    private bool QueueExternal(string sourceId, RewardDelivery delivery)
    {
        if (!IsReady || !manager.CanWriteMain || string.IsNullOrWhiteSpace(sourceId)) return false;
        delivery.deliveryId = SaveId + "/external/" + sourceId;
        var existing = FindDelivery(delivery.deliveryId);
        if (existing != null) return existing.rewardType == delivery.rewardType && existing.itemId == delivery.itemId &&
            existing.amount == delivery.amount && existing.destination == delivery.destination &&
            existing.recipientPlayerId == delivery.recipientPlayerId && existing.mailboxId == delivery.mailboxId;
        if (!AddDelivery(delivery)) return false;
        manager.CommitDeliveryChanges();
        return true;
    }

    // 잘못된 지급 대상이나 수량은 대기열에 넣지 않습니다.
    private bool AddDelivery(RewardDelivery delivery)
    {
        if (delivery.amount <= 0 || string.IsNullOrEmpty(delivery.deliveryId) || FindDelivery(delivery.deliveryId) != null) return false;
        if (!Enum.IsDefined(typeof(RewardDestination), delivery.destination) ||
            (delivery.rewardType != RewardType.Item && delivery.rewardType != RewardType.Money)) return false;
        if (delivery.destination == RewardDestination.PlayerInventory && string.IsNullOrEmpty(delivery.recipientPlayerId)) return false;
        if (delivery.destination == RewardDestination.Mailbox && string.IsNullOrEmpty(delivery.mailboxId)) return false;
        State.deliveries.Add(delivery);
        return true;
    }

    // 켜져 있는 아이템/돈만 실제 지급 대상으로 사용합니다.
    public static bool IsSupported(QuestReward reward) => reward != null && reward.enabled &&
        !string.IsNullOrEmpty(reward.rewardID) && reward.amount > 0 &&
        Enum.IsDefined(typeof(RewardDestination), reward.destination) &&
        (reward.rewardType == RewardType.Money || (reward.rewardType == RewardType.Item && !string.IsNullOrEmpty(reward.itemID))) &&
        (reward.destination == RewardDestination.PlayerInventory || !string.IsNullOrEmpty(reward.mailboxID));

    // 우편함 내용·수령 영수증·지급 완료를 동일한 공유 스냅샷으로 확정합니다.
    private void TryMailboxDelivery(RewardDelivery delivery)
    {
        var box = FindBox(delivery.mailboxId);
        if (box == null || !box.IsStorageReady || !box.CompareTag("Mailbox")) return;
        if (!TryGetBoxSnapshot(box, out var data, out int revision)) return;
        if (!RewardInventory.TryCredit(data, data.id.Length, delivery, out var updated))
        {
            NoticeOnce(delivery.deliveryId, "우편함이 가득 차 보상 배송을 기다리고 있습니다.");
            return;
        }
        if (!TryStageBoxChange(box, revision, updated)) return;
        delivery.status = RewardDeliveryStatus.Delivered;
        manager.CommitDeliveryChanges();
    }

    // 방장의 전달 요청을 로컬 인벤토리에 적용하고 수령 직후 스냅샷으로 응답합니다.
    public void ReceiveOffer(string deliveryId)
    {
        if (!IsReady) return;
        var delivery = FindDelivery(deliveryId);
        var confirmed = bridge.ReadRoomState(SaveId)?.rewards?.deliveries?.FirstOrDefault(d => d.deliveryId == deliveryId);
        if (!MatchesConfirmedDelivery(delivery, confirmed)) return;
        if (delivery == null || delivery.destination != RewardDestination.PlayerInventory ||
            delivery.recipientPlayerId != QuestNetworkBridge.LocalPlayerId || delivery.status != RewardDeliveryStatus.Pending) return;
        var inventory = FindObjectsByType<Inventory>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(i => i.player == Player.localPlayer);
        if (inventory == null || Player.localPlayer == null) return;
        bool alreadyReceived = inventory.CaptureInventorySnapshot()?.receivedDeliveryIds?.Contains(deliveryId) == true;
        var result = inventory.TryReceiveDelivery(delivery);
        if (result == RewardReceiveResult.NotReady) return;
        if (result == RewardReceiveResult.Full)
            NoticeOnce(deliveryId, string.IsNullOrEmpty(delivery.sourceBoxId) && string.IsNullOrEmpty(delivery.sourceFieldId)
                ? "인벤토리 공간이 부족해 보상을 기다리고 있습니다." : "인벤토리 공간이 부족해 꺼내지 못했습니다.");
        bridge.SendDeliveryResult(deliveryId, result, Player.localPlayer.CaptureQuestPlayerState(inventory));
        if (result == RewardReceiveResult.Success && !alreadyReceived && !string.IsNullOrEmpty(delivery.sourceFieldId))
        {
            var item = ItemDatabase.Instance.GetItem(delivery.itemId);
            if (item != null) manager.ReportLocalJobProgress(ObjectiveType.CollectItem, delivery.amount, item.stringID);
        }
    }

    // 수령자와 영수증을 확인한 응답만 반영하며 지연된 중복 응답은 무시합니다.
    public void AcceptResult(string deliveryId, RewardReceiveResult result, PlayerData player, string senderId)
    {
        if (!IsReady || !manager.CanWriteMain || player?.items == null) return;
        var delivery = FindDelivery(deliveryId);
        if (delivery == null || delivery.destination != RewardDestination.PlayerInventory ||
            delivery.status != RewardDeliveryStatus.Pending || delivery.recipientPlayerId != senderId) return;
        bool received = player.items.receivedDeliveryIds?.Contains(deliveryId) == true;
        bool rejected = player.items.rejectedDeliveryIds?.Contains(deliveryId) == true;
        if (result == RewardReceiveResult.Success && received)
        {
            if (!string.IsNullOrEmpty(delivery.sourceBoxId) && !CommitWithdrawal(delivery)) return;
            delivery.status = RewardDeliveryStatus.Delivered;
        }
        else if (result == RewardReceiveResult.Full && rejected &&
            (!string.IsNullOrEmpty(delivery.sourceBoxId) || !string.IsNullOrEmpty(delivery.sourceFieldId)))
            delivery.status = RewardDeliveryStatus.Cancelled;
        else return;
        retryAt.Remove(deliveryId);
        player.playerId = senderId;
        SaveManager.Instance.UpdatePlayerCache(player);
        PutRecipient(player);
        if (delivery.status == RewardDeliveryStatus.Delivered && !string.IsNullOrEmpty(delivery.sourceFieldId))
        {
            FieldItem.FindPickup(delivery.sourceFieldId)?.CompletePickup();
            var item = ItemDatabase.Instance.GetItem(delivery.itemId);
            if (item != null) manager.ApplyMainProgress("pickup/" + delivery.deliveryId, ObjectiveType.CollectItem,
                delivery.amount, item.stringID, player, senderId);
        }
        manager.CommitDeliveryChanges();
    }

    // 공유 상태보다 필드 식별자가 늦게 도착해도 이미 수령한 대상을 다시 숨깁니다.
    public bool IsFieldPickupCompleted(string pickupId) => IsReady && !string.IsNullOrEmpty(pickupId) &&
        State.deliveries.Any(d => d.sourceFieldId == pickupId && d.status == RewardDeliveryStatus.Delivered);

    // 필드 대상을 한 명에게 예약하고 실제 수령 영수증이 올 때까지 원본을 보존합니다.
    public bool RequestFieldPickup(FieldItem field, string recipientId)
    {
        if (!IsReady || !manager.CanWriteMain || bridge.IsRecoveringPlayerStates || field == null ||
            !field.gameObject.activeSelf || string.IsNullOrEmpty(field.PickupId) || string.IsNullOrEmpty(recipientId) ||
            field.amount <= 0 || ItemDatabase.Instance.GetItem(field.itemID) == null) return false;
        if (State.deliveries.Any(d => d.sourceFieldId == field.PickupId && d.status != RewardDeliveryStatus.Cancelled)) return false;
        State.deliveries.Add(new RewardDelivery { deliveryId = SaveId + "/pickup/" + Guid.NewGuid().ToString("N"),
            sourceFieldId = field.PickupId, recipientPlayerId = recipientId, destination = RewardDestination.PlayerInventory,
            rewardType = RewardType.Item, itemId = field.itemID, amount = field.amount, durability = field.durability });
        manager.CommitDeliveryChanges();
        return true;
    }

    // 출고 수량을 예약하고 동일 플레이어의 중복 클릭을 진행 중 한 건으로 제한합니다.
    public bool RequestWithdrawal(OpenableStorageBox box, string recipientId, int slot, int amount, bool money)
    {
        if (!IsReady || !manager.CanWriteMain || bridge.IsRecoveringPlayerStates || amount <= 0 || string.IsNullOrEmpty(recipientId) ||
            !TryGetBoxSnapshot(box, out var data, out _)) return false;
        if (State.deliveries.Any(d => d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == box.boxName &&
            d.recipientPlayerId == recipientId && d.sourceSlot == (money ? -1 : slot))) return false;
        if (money)
        {
            long reserved = State.deliveries.Where(d => d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == box.boxName && d.rewardType == RewardType.Money).Sum(d => (long)d.amount);
            if ((long)data.money - reserved < amount) return false;
        }
        else
        {
            if (slot < 0 || slot >= data.id.Length || data.id[slot] < 0) return false;
            long reserved = State.deliveries.Where(d => d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == box.boxName && d.sourceSlot == slot).Sum(d => (long)d.amount);
            if ((long)data.quantity[slot] - reserved < amount) return false;
        }
        State.deliveries.Add(new RewardDelivery {
            deliveryId = SaveId + "/withdraw/" + Guid.NewGuid().ToString("N"), sourceBoxId = box.boxName,
            sourceSlot = money ? -1 : slot, recipientPlayerId = recipientId, amount = amount,
            destination = RewardDestination.PlayerInventory, rewardType = money ? RewardType.Money : RewardType.Item,
            itemId = money ? -1 : data.id[slot], durability = money ? -1f : data.durability[slot]
        });
        manager.CommitDeliveryChanges();
        return true;
    }

    // 성공 영수증이 있는 출고만 원본 수량에서 차감합니다.
    private bool CommitWithdrawal(RewardDelivery delivery)
    {
        var savedBox = State.boxes.FirstOrDefault(b => b.boxId == delivery.sourceBoxId);
        if (savedBox?.items == null) return false;
        var data = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(savedBox.items));
        if (delivery.rewardType == RewardType.Money)
        {
            if (data.money < delivery.amount) return false;
            data.money -= delivery.amount;
        }
        else
        {
            int slot = delivery.sourceSlot;
            if (slot < 0 || slot >= data.id.Length || data.id[slot] != delivery.itemId || data.quantity[slot] < delivery.amount) return false;
            data.RemoveItem(slot, delivery.amount);
            if (data.id[slot] == -1) data.durability[slot] = -1f;
        }
        PutBox(delivery.sourceBoxId, data);
        return true;
    }

    // 진행 중 출고가 있는 상자를 자동 판매가 변경하지 않도록 조회합니다.
    public bool HasReservedWithdrawals(string boxId) => IsReady && State.deliveries.Any(d =>
        d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == boxId);

    // 공유 상태가 없는 상자만 저장 캐시로 초기화하며 이후에는 공유 데이터를 실제 상자에 적용합니다.
    public bool InitializeBox(OpenableStorageBox box)
    {
        if (!IsReady || box == null || FindBox(box.boxName, false) != box) return false;
        var entry = State.boxes.FirstOrDefault(b => b.boxId == box.boxName);
        if (entry?.items?.id != null) { box.ApplySharedStorage(entry.items); return true; }
        if (!manager.CanWriteMain || bridge.IsRecoveringPlayerStates) return false;
        var initial = SaveManager.Instance.GetBoxData(box.boxName);
        if (initial?.id == null) { initial = new InventoryData(); initial.GenerateData(); }
        PutBox(box.boxName, initial);
        manager.CommitDeliveryChanges();
        return true;
    }

    // 변경 계산에 사용할 공유 원본 복사본과 기준 버전을 함께 반환합니다.
    public bool TryGetBoxSnapshot(OpenableStorageBox box, out InventoryData data, out int revision)
    {
        data = null;
        revision = -1;
        if (!IsReady || box == null || FindBox(box.boxName) != box) return false;
        var entry = State.boxes.FirstOrDefault(b => b.boxId == box.boxName);
        if (entry?.items?.id == null) return false;
        data = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(entry.items));
        revision = manager.SharedRevision;
        return true;
    }

    // 상점의 잔액·거래 기록과 같은 Commit에 묶을 수 있도록 검증된 변경만 공유 상태에 준비합니다.
    public bool TryStageBoxChange(OpenableStorageBox box, int expectedRevision, InventoryData data)
    {
        if (!IsReady || !manager.CanWriteMain || bridge.IsRecoveringPlayerStates || expectedRevision != manager.SharedRevision ||
            data?.id == null || data.quantity?.Length != data.id.Length || data.durability?.Length != data.id.Length ||
            !TryGetBoxSnapshot(box, out var current, out _) || current.id.Length != data.id.Length) return false;
        var reserved = State.deliveries.Where(d => d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == box.boxName).ToList();
        if (reserved.Where(d => d.rewardType == RewardType.Money).Sum(d => (long)d.amount) > data.money) return false;
        foreach (var group in reserved.Where(d => d.rewardType == RewardType.Item).GroupBy(d => d.sourceSlot))
        {
            int slot = group.Key;
            if (slot < 0 || slot >= data.id.Length || group.Any(d => d.itemId != data.id[slot] || !d.durability.Equals(data.durability[slot])) ||
                group.Sum(d => (long)d.amount) > data.quantity[slot]) return false;
        }
        PutBox(box.boxName, data);
        return true;
    }

    // 실제 대상과 계산 기준 버전이 유효한 변경만 발행해 오래된 물리 스냅샷의 덮어쓰기를 막습니다.
    public bool PublishBox(OpenableStorageBox box, int expectedRevision, InventoryData data)
    {
        if (!TryStageBoxChange(box, expectedRevision, data)) return false;
        manager.CommitDeliveryChanges();
        return true;
    }

    // 우편함 지급과 출고의 원본 데이터를 공유 상태에 복사합니다.
    private void PutBox(string boxId, InventoryData data)
    {
        var entry = State.boxes.FirstOrDefault(b => b.boxId == boxId);
        if (entry == null) { entry = new BoxSaveData { boxId = boxId }; State.boxes.Add(entry); }
        entry.items = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(data));
    }

    // 수령 직후 스냅샷보다 오래된 응답이 복원 자료를 덮지 않게 합니다.
    private void PutRecipient(PlayerData player)
    {
        var old = State.recipients.FirstOrDefault(p => p.playerId == player.playerId);
        if (old != null && old.inventoryActor == player.inventoryActor && old.inventorySequence > player.inventorySequence) return;
        if (old != null) State.recipients.Remove(old);
        State.recipients.Add(JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(player)));
    }

    // 모든 참가자와 새 방장의 저장 캐시를 확정된 지급/상자 상태로 맞춥니다.
    public void ApplySharedState()
    {
        if (!IsReady) return;
        State.deliveries ??= new(); State.boxes ??= new(); State.recipients ??= new(); State.purchases ??= new();
        bool initial = observedSaveId != SaveId;
        if (initial) { observedSaveId = SaveId; announced.Clear(); blockedNotices.Clear(); retryAt.Clear(); }
        foreach (var entry in State.boxes)
        {
            SaveManager.Instance.UpdateBoxCache(entry.boxId, entry.items);
            FindBox(entry.boxId, false)?.ApplySharedStorage(entry.items);
        }
        foreach (var recipient in State.recipients)
        {
            var connected = PhotonNetwork.PlayerList.FirstOrDefault(p => QuestNetworkBridge.PlayerId(p) == recipient.playerId);
            // 재입장 이전 Actor의 지급 스냅샷이 현재 세션의 인벤토리를 덮지 않게 합니다.
            if (connected != null && connected.ActorNumber != recipient.inventoryActor) continue;
            SaveManager.Instance.UpdatePlayerCache(recipient);
        }
        foreach (var delivery in State.deliveries.Where(d => d.status == RewardDeliveryStatus.Delivered))
        {
            if (!string.IsNullOrEmpty(delivery.sourceFieldId))
            {
                FieldItem.FindPickup(delivery.sourceFieldId)?.CompletePickup();
                continue;
            }
            if (!announced.Add(delivery.deliveryId) || initial || !string.IsNullOrEmpty(delivery.sourceBoxId)) continue;
            if (delivery.destination == RewardDestination.PlayerInventory && delivery.recipientPlayerId != QuestNetworkBridge.LocalPlayerId) continue;
            string content = delivery.rewardType == RewardType.Money ? $"{delivery.amount}G" :
                $"{ItemDatabase.Instance?.GetItem(delivery.itemId)?.itemName ?? "아이템"} ×{delivery.amount}";
            OnNotice?.Invoke(delivery.destination == RewardDestination.Mailbox ? $"공용 우편함에 {content} 보상이 도착했습니다." : $"{content} 보상을 받았습니다.");
        }
        RequestProcessing();
    }

    // UI에는 예약한 수량을 제외해 추가 출고 가능한 수량만 보여 줍니다.
    public InventoryData GetVisibleBoxData(string boxId, InventoryData original)
    {
        if (!IsReady || original == null) return original;
        var visible = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(original));
        foreach (var pending in State.deliveries.Where(d => d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == boxId))
        {
            if (pending.rewardType == RewardType.Money) visible.money = Math.Max(0, visible.money - pending.amount);
            else if (pending.sourceSlot >= 0 && pending.sourceSlot < visible.id.Length)
            {
                visible.RemoveItem(pending.sourceSlot, pending.amount);
                if (visible.id[pending.sourceSlot] == -1) visible.durability[pending.sourceSlot] = -1f;
            }
        }
        return visible;
    }

    // 같은 지급의 대기 알림은 세션 동안 한 번만 표시합니다.
    private void NoticeOnce(string id, string message) { if (blockedNotices.Add(id)) OnNotice?.Invoke(message); }

    // 공유 상태가 교체될 수 있으므로 항상 ID로 현재 지급 기록을 조회합니다.
    public RewardDelivery FindDelivery(string id) => IsReady ? State.deliveries.FirstOrDefault(d => d.deliveryId == id) : null;

    // 활성 네트워크 상자의 ID가 유일할 때만 반환하며 준비 전 중복도 임의로 선택하지 않습니다.
    public static OpenableStorageBox FindBox(string boxId, bool requireReady = true)
    {
        if (string.IsNullOrWhiteSpace(boxId)) return null;
        var matches = FindObjectsByType<OpenableStorageBox>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(b => b.isActiveAndEnabled && b.boxName == boxId && b.GetComponent<PhotonView>() != null && b.GetComponent<PhotonView>().ViewID != 0).ToArray();
        if (matches.Length > 1)
        {
            if (duplicateBoxWarnings.Add(boxId)) Debug.LogError($"[Storage] 중복된 상자 ID '{boxId}'의 거래를 차단합니다: {string.Join(", ", matches.Select(b => b.name))}");
            return null;
        }
        duplicateBoxWarnings.Remove(boxId);
        return matches.Length == 1 && (!requireReady || matches[0].IsStorageReady) ? matches[0] : null;
    }
}
