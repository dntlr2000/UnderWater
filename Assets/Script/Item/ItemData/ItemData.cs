using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public int itemId;
    public string stringID;
    public string type = "item";
    public Sprite itemIcon;
    [TextArea(2, 4)]
    public string description;
    public string modelPath;
    public string equipEffectType;

    [Header("Attributes")]
    public int price;
    public float weight;
    public float durability;
    public bool interactable;
    public float damage = 10f;
    public bool sigularity;

    // 별도 사용 효과가 없는 재료와 수집품은 입력만으로 소비하지 않습니다.
    public virtual int Use(Player player, int quantity)
    {
        return quantity;
    }
}