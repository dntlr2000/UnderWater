using UnityEngine;


public class FieldItem : MonoBehaviour, InteractableObject
{
    public bool getAble = true;
    public int itemID; //연관된 아이템 DB의 아이디
    public int amount; //개수
    public string itemName; //아이템을 얻지 않아도 이름을 확인할 수 있게.
    //public int durability = -1;
    private Inventory inventory;


    public void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {
        //Debug.Log("Item Detected");
        if (getAble && Input.GetMouseButtonDown(1))
        {
            Debug.Log("아이템 습득 시도");
            GetItem();
        }
    }

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

