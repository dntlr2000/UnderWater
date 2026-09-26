using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Items/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    public List<ShopCatalogEntry> entries = new();

    // 목록 순번과 아이템 ID를 구분하여 실제 판매 정의를 찾습니다.
    public ShopCatalogEntry Find(int itemId) => entries.FirstOrDefault(e => e?.item != null && e.item.itemId == itemId);
}

[Serializable]
public class ShopCatalogEntry
{
    public ItemData item;
    public bool overrideDurability;
    public float durability = -1f;
    public float InitialDurability => overrideDurability ? durability : item != null ? item.durability : -1f;
}
