using UnityEngine;

[CreateAssetMenu(fileName = "New KeyItem", menuName = "Items/Key")]
public class KeyItem : ItemData
{
    //현재는 기능이 존재하지 않으므로 미구현 상태
    public override int Use(Player player, int quantity)
    {

        return quantity;
    }
}
