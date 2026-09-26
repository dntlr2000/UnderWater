using Photon.Realtime;
using UnityEngine;
[CreateAssetMenu(fileName = "New FoodItem", menuName = "Items/FoodItem")]
public class FoodItem : ItemData
{
    [Header("DiscountAmount")]
    public int discountAmount = 1;

    [Header("HealingValue")]
    public float health = 0f;
    public float hunger = 0f;
    public float thirst = 0f;

    // 회복 효과가 정의된 음식만 한 번 소비하고 효과를 소유자에게 적용합니다.
    public override int Use(Player player, int quantity)
    {
        if (player == null || player.condition == null || discountAmount <= 0 || quantity < discountAmount ||
            (health == 0 && hunger == 0 && thirst == 0)) return quantity;
        if (health != 0) player.condition.Damaged(-health);
        player.condition.getFood(thirst: thirst, hunger: hunger);
        quantity -= discountAmount;
        return quantity;
    }
}
