using Photon.Pun;
using System;
using System.IO;
using UnityEngine;

public class Inventory : InventoryFrame
{
    public int index; //현재 들고 있는 아이템
    public Transform IndexLine;
    public Player player; //플레이어가 포톤을 통해 자신의 인벤토리를 할당하는 기능 필요
    public string playerInventoryName = "inventory";
    //private bool showInventory = false; //ItemUI에서 일단 가져와봄 Update 부하를 줄이기 위해 ItemUI의 메서드를 여기로 옮길 수 있음

    void Start()
    {
        GenerateData();
        inventoryName = playerInventoryName;
        //GetItem(0, 1);
    }

    public override void GenerateData()
    {
        inventoryData = new InventoryData();
        inventoryData.GenerateData();
        GetMoney(100);

        //GetItem(0, 1);
        Debug.Log("Inventory data generated");
    }

    // Update is called once per frame

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Q))
        {
            //RemoveAllItem(index);
            //RemoveItem(index, 1);
            DropItem(index, 1);
        }

        //들고 있는 아이템 변경하기
        Vector2 delta = Input.mouseScrollDelta;
        if (delta.y > 0f)
        {
            index += 1;
            if (index > 4) index = 4;
            IndexSetter();
        }
        else if (delta.y < 0f)
        {
            index -= 1;
            if (index < 0) index = 0;
            IndexSetter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            index = 0;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            index = 1;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            index = 2;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            index = 3;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            index = 4;
            IndexSetter();
        }

        //아이템 사용하기
        if (Input.GetMouseButtonDown(1))
        {
            if (inventoryData.id[index] < 0) return;

            inventoryData.useItem(index);
            ItemUI.SetQuantity(index, inventoryData.quantity[index]);
            if (inventoryData.quantity[index] <= 0)
            {
                inventoryData.id[index] = -1;
                ItemUI.ResetIcons(index);
            }
        }
    }

    private void FixedUpdate()
    {
        
    }

    protected void IndexSetter()
    {
        IndexLine.localPosition = new Vector2(-140 + 70 * index, 0);
    }

    public void DropItem(int index, int amount = 1)
    {
        if (inventoryData.id[index] == -1)
        {
            return;
        }
        else
        {
            int trueQuantity = inventoryData.quantity[index] < amount ? inventoryData.quantity[index] : amount;
            Transform playerTransform = player.transform;
            Vector3 DropLocation = playerTransform.position + playerTransform.forward * 3f;
            inventoryData.item.generateFieldItem(inventoryData.id[index], DropLocation, trueQuantity);

            RemoveItem(index, amount);

        }

    }

    public bool HoldingInteractableItem() //들고 있을 때 상호작용 가능한 아이템인지 확인 =>InteractableObject와 연계
    {
        if (inventoryData.id[index] == -1) return true;
        return inventoryData.item.getInteractable(inventoryData.id[index]);
    }

    [PunRPC]
    public void PunRPC_AddItem(int id, int quantity)
    {
        GetItem(id, quantity); // 기존에 있던 아이템 추가 로직 호출
        Debug.Log($"네트워크를 통해 아이템 수신: ID {id}, 수량 {quantity}");
    }

    [PunRPC]
    public void PunRPC_SetMoney(int newTotalMoney)
    {
        // inventoryData.money -= amount; 와 같이 계산하는 것보다
        // 서버가 계산한 최종 금액을 그대로 덮어쓰는 것이 동기화에 더 안전합니다.
        inventoryData.money = newTotalMoney;
        ItemUI.UpdateMoney(inventoryData.money);
        Debug.Log($"네트워크를 통해 돈 수신. 현재 잔액: {inventoryData.money}");
    }
}
