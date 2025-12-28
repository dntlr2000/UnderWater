using UnityEngine;

public class OxygenCylinder : FieldItem
{
    public float MAX_OXYGEN = 100f;

    ///public float remainOxygen; 
    //durability에서 대체

    public override void Start()
    {
        base.Start();
    }

    public override void GetItem()
    {
        inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Inventory를 찾을 수 없습니다.");
            return;
        }
        if (!inventory.HoldingInteractableItem()) return; 

        inventory.GetItem(itemID, amount, durability);
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
