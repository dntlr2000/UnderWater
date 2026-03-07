using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
[CreateAssetMenu(fileName = "New ItemBox", menuName = "Items/Itembox")]
public class ItemBox : ItemData
{
    [Header("Rewards")]
    public int ItemId;
    public int amount;

    public override int Use(Player player, int quantity)
    {
        Inventory inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null) Debug.LogError("인벤토리가 존재하지 않습니다.");
        inventory.GetItem(ItemId, amount);
        quantity--;
        return quantity;
    }

}
