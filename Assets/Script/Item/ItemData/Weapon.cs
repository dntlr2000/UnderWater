using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon")]
public class Weapon : ItemData
{
    public override int Use(Player player, int quantity)
    {
        //현재는 기능이 존재하지 않으므로 미구현 상태
        return quantity;
    }
}
