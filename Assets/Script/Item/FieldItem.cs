using UnityEngine;


public class FieldItem : InteractableObject//, Interactable
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
        if (!inventory.HoldingInteractableItem()) return; //아이템을 주울 수 있는 상태인지 판단 기준1 : 손에 든 채로 또 아이템을 주울 수 있는 아이템을 들고 있는지 리턴
        //소모형 아이템이 1개 남아서 사용하고 아이템이 비워지자마자 아이템이 주워지는 현상 발생. inventory의 아이템 사용 스크립트가 먼저 처리되기 때문
        //해결 방안1 : 아이템이 소모되어 삭제되는 시점을 코루틴 등으로 미루기

        inventory.GetItem(itemID, amount);
        gameObject.SetActive(false);
    }
}

