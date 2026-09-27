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
    private float nextRetry;
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
    }

    // 씬 종료 후 정적 서비스 참조를 정리합니다.
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // 준비된 세션에서만 낮은 빈도로 미지급 보상을 다시 확인합니다.
    private void Update()
    {
        if (Time.unscaledTime < nextRetry) return;
        nextRetry = Time.unscaledTime + 1f;
        ProcessPending();
    }

    // 방장이 저장된 개인 완료 요청을 보상 정의와 대조한 뒤 대기 지급을 실행합니다.
    public void ProcessPending()
    {
        if (!IsReady || !manager.CanWriteMain || bridge.IsRecoveringPlayerStates) return;
        bool added = RegisterJobClaims();
        if (added) { manager.CommitDeliveryChanges(); return; }
        // 지급 의도가 방에 확정되기 전에 아이템을 전달하지 않습니다.
        if (!bridge.IsSharedRevisionConfirmed(manager.SharedRevision)) return;
        foreach (string id in State.deliveries.Where(d => d.status == RewardDeliveryStatus.Pending).Select(d => d.deliveryId).ToList())
        {
            var delivery = FindDelivery(id);
            if (delivery == null || delivery.status != RewardDeliveryStatus.Pending) continue;
            if (delivery.destination == RewardDestination.Mailbox) TryMailboxDelivery(delivery);
            else
            {
                var recipient = PhotonNetwork.PlayerList.FirstOrDefault(p => QuestNetworkBridge.PlayerId(p) == delivery.recipientPlayerId);
                if (recipient != null && !recipient.IsInactive) bridge.SendDeliveryOffer(recipient.ActorNumber, delivery.deliveryId);
            }
        }
    }

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
        var data = box.CaptureStorageData();
        if (!RewardInventory.TryCredit(data, data.id.Length, delivery, out var updated))
        {
            NoticeOnce(delivery.deliveryId, "우편함이 가득 차 보상 배송을 기다리고 있습니다.");
            return;
        }
        PutBox(delivery.mailboxId, updated);
        delivery.status = RewardDeliveryStatus.Delivered;
        manager.CommitDeliveryChanges();
    }

    // 방장의 전달 요청을 로컬 인벤토리에 적용하고 수령 직후 스냅샷으로 응답합니다.
    public void ReceiveOffer(string deliveryId)
    {
        if (!IsReady) return;
        var delivery = FindDelivery(deliveryId);
        if (delivery == null || delivery.destination != RewardDestination.PlayerInventory ||
            delivery.recipientPlayerId != QuestNetworkBridge.LocalPlayerId || delivery.status != RewardDeliveryStatus.Pending) return;
        var inventory = FindObjectsByType<Inventory>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(i => i.player == Player.localPlayer);
        if (inventory == null || Player.localPlayer == null) return;
        var result = inventory.TryReceiveDelivery(delivery);
        if (result == RewardReceiveResult.NotReady) return;
        if (result == RewardReceiveResult.Full)
            NoticeOnce(deliveryId, string.IsNullOrEmpty(delivery.sourceBoxId)
                ? "인벤토리 공간이 부족해 보상을 기다리고 있습니다." : "인벤토리 공간이 부족해 꺼내지 못했습니다.");
        bridge.SendDeliveryResult(deliveryId, result, Player.localPlayer.CaptureQuestPlayerState(inventory));
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
        else if (result == RewardReceiveResult.Full && rejected && !string.IsNullOrEmpty(delivery.sourceBoxId))
            delivery.status = RewardDeliveryStatus.Cancelled;
        else return;
        player.playerId = senderId;
        SaveManager.Instance.UpdatePlayerCache(player);
        PutRecipient(player);
        manager.CommitDeliveryChanges();
    }

    // 출고 수량을 예약하고 동일 플레이어의 중복 클릭을 진행 중 한 건으로 제한합니다.
    public bool RequestWithdrawal(OpenableStorageBox box, string recipientId, int slot, int amount, bool money)
    {
        if (!IsReady || !manager.CanWriteMain || box == null || !box.IsStorageReady || amount <= 0 || string.IsNullOrEmpty(recipientId)) return false;
        if (State.deliveries.Any(d => d.status == RewardDeliveryStatus.Pending && d.sourceBoxId == box.boxName &&
            d.recipientPlayerId == recipientId && d.sourceSlot == (money ? -1 : slot))) return false;
        var data = box.CaptureStorageData();
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
        PutBox(box.boxName, data);
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

    // 일반 상점/창고 입고도 보상 출고와 같은 최신 상자 스냅샷에 반영합니다.
    public bool PublishBox(string boxId, InventoryData data)
    {
        if (!IsReady || !manager.CanWriteMain || data?.id == null || string.IsNullOrEmpty(boxId)) return false;
        PutBox(boxId, data);
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
        State.deliveries ??= new(); State.boxes ??= new(); State.recipients ??= new();
        bool initial = observedSaveId != SaveId;
        if (initial) { observedSaveId = SaveId; announced.Clear(); blockedNotices.Clear(); }
        foreach (var entry in State.boxes)
        {
            SaveManager.Instance.UpdateBoxCache(entry.boxId, entry.items);
            FindBox(entry.boxId)?.ApplySharedStorage(entry.items);
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
            if (!announced.Add(delivery.deliveryId) || initial || !string.IsNullOrEmpty(delivery.sourceBoxId)) continue;
            if (delivery.destination == RewardDestination.PlayerInventory && delivery.recipientPlayerId != QuestNetworkBridge.LocalPlayerId) continue;
            string content = delivery.rewardType == RewardType.Money ? $"{delivery.amount}G" :
                $"{ItemDatabase.Instance?.GetItem(delivery.itemId)?.itemName ?? "아이템"} ×{delivery.amount}";
            OnNotice?.Invoke(delivery.destination == RewardDestination.Mailbox ? $"공용 우편함에 {content} 보상이 도착했습니다." : $"{content} 보상을 받았습니다.");
        }
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

    // 저장용 boxName으로 실제 상자를 찾아 이름이 같은 UI 오브젝트와 구분합니다.
    private static OpenableStorageBox FindBox(string boxId) => FindObjectsByType<OpenableStorageBox>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .FirstOrDefault(b => b.boxName == boxId);
}
