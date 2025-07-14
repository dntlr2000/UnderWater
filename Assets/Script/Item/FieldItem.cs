using UnityEngine;


public class FieldItem : InteractableObject, Interactable
{
    public bool getAble = true;
    public int itemID; //연관된 아이템 DB의 아이디
    public int amount; //개수
    //public int durability = -1;
    //private Inventory inventory;



    public override void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {
        //Debug.Log("Item Detected");
        if (getAble && Input.GetMouseButtonDown(1))
        {
            Debug.Log("아이템 습득 시도");
            GetItem();
        }
    }


    //현재로서는 Instant, Guage만 정의되어있음
    public override InteractionType GetInteractionType() => InteractionType.Instant;

    public void GetItem()
    {
        inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Inventory를 찾을 수 없습니다.");
            return;
        }
        inventory.GetItem(itemID, amount);
        gameObject.SetActive(false);
    }
}

