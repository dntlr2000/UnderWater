using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopPurchaseRecord
{
    public string requestId, buyerId, message;
    public int itemId, amount, cost;
    public bool success;
}

public static class ShopTransactions
{
    // 구매 총액을 정수 범위 안에서 계산하며 잘못된 수량과 가격을 거절합니다.
    public static int BuyPrice(ItemData item, int amount)
    {
        long value = item == null ? -1 : (long)item.price * 2 * amount;
        return amount > 0 && item != null && item.price >= 0 && value <= int.MaxValue ? (int)value : -1;
    }

    // 기존 판매 정책인 총 기준 금액의 60%를 정수 연산으로 계산합니다.
    public static int SellPrice(ItemData item, int amount)
    {
        long basis = item == null ? -1 : (long)item.price * amount;
        long value = basis / 5 * 3 + basis % 5 * 3 / 5;
        return amount > 0 && item != null && item.price >= 0 && value <= int.MaxValue ? (int)value : -1;
    }

    // 잔액과 우편함 복사본을 모두 검사한 뒤 성공할 때만 두 결과를 반환합니다.
    public static bool TryPurchase(InventoryData buyer, InventoryData mailbox, ShopCatalogEntry entry,
        int amount, string requestId, out InventoryData paid, out InventoryData stocked, out string message)
    {
        paid = stocked = null;
        message = "구매할 수 없는 상품 또는 수량입니다.";
        int cost = BuyPrice(entry?.item, amount);
        if (buyer?.id == null || mailbox?.id == null || entry?.item == null || entry.item.itemId <= 0 ||
            cost < 0 || string.IsNullOrEmpty(requestId)) return false;
        if (buyer.appliedPurchaseIds?.Contains(requestId) == true || mailbox.receivedDeliveryIds?.Contains(requestId) == true)
        { message = "이미 처리된 구매입니다."; return false; }
        if (buyer.money < cost) { message = "돈이 부족합니다."; return false; }
        var delivery = new RewardDelivery { deliveryId = requestId, rewardType = RewardType.Item,
            itemId = entry.item.itemId, amount = amount, durability = entry.InitialDurability };
        if (!RewardInventory.TryCredit(mailbox, mailbox.id.Length, delivery, out stocked))
        { message = "우편함 공간이 부족합니다. 비운 뒤 다시 구매해 주세요."; return false; }
        paid = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(buyer));
        paid.appliedPurchaseIds ??= new();
        paid.money -= cost;
        paid.appliedPurchaseIds.Add(requestId);
        message = "구매한 아이템을 공용 우편함에 보냈습니다.";
        return true;
    }

    // 확인한 슬롯의 아이템과 실제 판매량을 검증하고 소모와 수익을 함께 계산합니다.
    public static bool TrySell(InventoryData source, int slot, int expectedItemId, int amount, int slots,
        out InventoryData updated)
    {
        updated = null;
        if (source?.id == null || source.quantity?.Length != source.id.Length || source.durability?.Length != source.id.Length ||
            slot < 0 || slot >= Math.Min(slots, source.id.Length) || source.id[slot] != expectedItemId || expectedItemId <= 0 || amount <= 0) return false;
        int actual = Math.Min(amount, source.quantity[slot]);
        int price = SellPrice(ItemDatabase.Instance?.GetItem(expectedItemId), actual);
        if (price < 0 || source.money < 0 || (long)source.money + price > int.MaxValue) return false;
        updated = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(source));
        updated.RemoveItem(slot, actual);
        if (updated.id[slot] < 0) updated.durability[slot] = -1f;
        updated.money += price;
        return true;
    }

    // 확정 구매를 아직 반영하지 않은 인벤토리에는 비용을 한 번만 적용합니다.
    public static bool ApplyPurchases(InventoryData data, string buyerId, IEnumerable<ShopPurchaseRecord> purchases)
    {
        if (data == null || purchases == null || string.IsNullOrEmpty(buyerId)) return false;
        data.appliedPurchaseIds ??= new();
        bool changed = false;
        foreach (var purchase in purchases)
        {
            if (purchase == null || !purchase.success || purchase.buyerId != buyerId || purchase.cost < 0 ||
                string.IsNullOrEmpty(purchase.requestId) || data.appliedPurchaseIds.Contains(purchase.requestId)) continue;
            data.money = Math.Max(0, data.money - purchase.cost);
            data.appliedPurchaseIds.Add(purchase.requestId);
            changed = true;
        }
        return changed;
    }
}
