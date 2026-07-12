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

    public override int Use(Player player, int quantity)
    {
        player.condition.Damaged(-health);
        player.condition.getFood(hunger, thirst);
        quantity -= discountAmount;
        return quantity;
    }
}
