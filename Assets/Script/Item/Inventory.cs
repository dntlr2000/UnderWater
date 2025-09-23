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
    PhotonView photonView;

    protected void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("Inventory 스크립트에 PhotonView가 없습니다! 플레이어 프리팹에 추가해주세요.");
        }
    }

    void Start()
    {
        /*
        if (!photonView.IsMine)
        {
            this.enabled = false;
            return;
        }
        */


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

        int itemIDToDrop = inventoryData.id[index];
        int quantityToDrop = inventoryData.quantity[index] < amount ? inventoryData.quantity[index] : amount;

        // **STEP 1: Remove the item from the local inventory immediately.**
        // This provides instant feedback to the player.
        RemoveItem(index, quantityToDrop);

        // Calculate the drop position
        if (player == null) player = GetComponent<Player>();
        Transform playerTransform = player.transform;
        Vector3 dropLocation = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 0.5f;

        // **STEP 2: Send a request to the Master Client to create the item for everyone.**
        photonView.RPC("PunRPC_Master_InstantiateDroppedItem", RpcTarget.MasterClient, itemIDToDrop, quantityToDrop, dropLocation);
    }

    public bool HoldingInteractableItem() //들고 있을 때 상호작용 가능한 아이템인지 확인 =>InteractableObject와 연계
    {
        if (inventoryData.id[index] == -1) return true;
        return ItemDatabase.Instance.getInteractable(inventoryData.id[index]);
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


    [PunRPC]
    public void PunRPC_DropItem(int itemID, int amount, Vector3 location, PhotonMessageInfo info)
    {
        // --- 1. 프리팹 경로 결정 ---
        string prefabPath = $"FieldItem/Object{itemID}";
        if (Resources.Load(prefabPath) == null)
        {
            prefabPath = "FieldItem/Object1";
        }

        // --- 2. 아이템 생성 (instantiationData 없이) ---
        GameObject droppedItem = PhotonNetwork.Instantiate(prefabPath, location, Quaternion.identity);

        // --- 3. 생성된 아이템에 속성 설정 RPC 호출 ---
        if (droppedItem != null)
        {
            PhotonView itemView = droppedItem.GetComponent<PhotonView>();
            if (itemView != null)
            {
                // 모든 클라이언트에게 이 아이템의 속성을 설정하라고 명령합니다.
                itemView.RPC("PunRPC_SetItemProperties", RpcTarget.All, itemID, amount);
            }
        }

        // --- 4. 인벤토리에서 아이템 제거 ---
        if (info.Sender == PhotonNetwork.LocalPlayer)
        {
            RemoveItem(this.index, amount);
        }
    }
    
    [PunRPC]
    public void PunRPC_Master_InstantiateDroppedItem(int itemID, int amount, Vector3 location)
    {
        // Safety check: ensure only the master client runs this.
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // --- 1. Determine Prefab Path ---
        string prefabPath = $"FieldItem/Object{itemID}";
        if (Resources.Load(prefabPath) == null)
        {
            prefabPath = "FieldItem/Object1"; // Fallback prefab
        }

        // --- 2. Instantiate Item ONCE on the network ---
        GameObject droppedItem = PhotonNetwork.Instantiate(prefabPath, location, Quaternion.identity);

        // --- 3. Set properties on the new item for all clients ---
        if (droppedItem != null)
        {
            PhotonView itemView = droppedItem.GetComponent<PhotonView>();
            if (itemView != null)
            {
                // Use the existing RPC on FieldItem.cs to sync its data (ID and amount)
                itemView.RPC("PunRPC_SetItemProperties", RpcTarget.All, itemID, amount);
            }
            else
            {
                Debug.LogError($"Dropped item prefab '{prefabPath}' is missing a PhotonView component.");
            }
        }
    }
}
