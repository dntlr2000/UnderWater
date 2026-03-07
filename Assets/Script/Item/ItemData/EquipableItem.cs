using UnityEngine;

[CreateAssetMenu(fileName = "New EquipableItem", menuName = "Items/EquipableItem")]
public class EquipableItem : ItemData
{
    public override int Use(Player player, int quantity)
    {
        //현재는 기능이 존재하지 않으므로 미구현 상태
        return quantity;
    }
}
