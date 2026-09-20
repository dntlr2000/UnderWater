using System;
using System.Collections.Generic;
using UnityEngine;

public enum RewardDestination { PlayerInventory, Mailbox }
public enum RewardDeliveryStatus { Pending, Delivered, Cancelled }
public enum RewardReceiveResult { Success, Full, NotReady, Rejected }

[Serializable]
public class QuestRewardClaim
{
    public string questId;
    public string rewardId;
}

[Serializable]
public class RewardDelivery
{
    public string deliveryId;
    public string questId;
    public string recipientPlayerId;
    public RewardType rewardType;
    public RewardDestination destination;
    public int itemId = -1;
    public int amount;
    public float durability = -1f;
    public string mailboxId;
    public RewardDeliveryStatus status;
    // 출고 중에는 원본 수량을 남겨 두고 이 기록으로 다른 출고/판매를 막습니다.
    public string sourceBoxId;
    public int sourceSlot = -1;
}

[Serializable]
public class RewardDeliveryState
{
    public List<RewardDelivery> deliveries = new();
    public List<BoxSaveData> boxes = new();
    // 지급 완료와 수령 직후 인벤토리를 같은 방 스냅샷에 포함합니다.
    public List<PlayerData> recipients = new();
}

public static class RewardInventory
{
    // UI와 독립된 복사본에서 전량 지급을 검사하여 부분 지급과 재지급을 막습니다.
    public static bool TryCredit(InventoryData source, int slots, RewardDelivery delivery, out InventoryData updated)
    {
        updated = null;
        if (source?.id == null || source.quantity?.Length != source.id.Length ||
            source.durability?.Length != source.id.Length || delivery == null || delivery.amount <= 0 ||
            string.IsNullOrEmpty(delivery.deliveryId) || slots <= 0 || slots > source.id.Length) return false;
        updated = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(source));
        updated.receivedDeliveryIds ??= new();
        if (updated.receivedDeliveryIds.Contains(delivery.deliveryId)) return true;
        if (delivery.rewardType == RewardType.Money)
        {
            long total = (long)updated.money + delivery.amount;
            if (updated.money < 0 || total > int.MaxValue) { updated = null; return false; }
            updated.money = (int)total;
        }
        else if (delivery.rewardType == RewardType.Item)
        {
            var item = ItemDatabase.Instance?.GetItem(delivery.itemId);
            if (item == null || !TryAddItems(updated, slots, item, delivery.amount, delivery.durability))
            { updated = null; return false; }
        }
        else { updated = null; return false; }
        updated.receivedDeliveryIds.Add(delivery.deliveryId);
        return true;
    }

    // 기존 중첩 규칙을 유지하되 개별 장비는 필요한 슬롯 수를 먼저 확인합니다.
    private static bool TryAddItems(InventoryData data, int slots, ItemData item, int amount, float durability)
    {
        if (!item.sigularity)
        {
            for (int i = 0; i < slots; i++)
            {
                if (data.id[i] != item.itemId) continue;
                long total = (long)data.quantity[i] + amount;
                if (data.quantity[i] < 0 || total > int.MaxValue) return false;
                data.quantity[i] = (int)total;
                return true;
            }
        }
        int needed = item.sigularity ? amount : 1;
        int empty = 0;
        for (int i = 0; i < slots; i++) if (data.id[i] == -1) empty++;
        if (empty < needed) return false;
        for (int i = 0; i < slots && needed > 0; i++)
        {
            if (data.id[i] != -1) continue;
            data.id[i] = item.itemId;
            data.quantity[i] = item.sigularity ? 1 : amount;
            data.durability[i] = durability;
            needed--;
        }
        return true;
    }
}
