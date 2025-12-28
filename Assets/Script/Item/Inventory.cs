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

    public bool canUseItem = true;

    //public ItemSlot[] equipment;
    //private InventoryData equipData;

    //private static Inventory _instance;
    /*
    public static Inventory Instance
    {
        get
        {
            // 만약 인스턴스가 아직 없다면 씬에서 찾아봅니다.
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<Inventory>();
                if (_instance == null)
                {
                    Debug.LogError("씬에 ItemDatabase 오브젝트가 존재하지 않습니다!");
                }
            }
            return _instance;
        }
    }
    */

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

        GenerateData(25, 1);
        //0 ~ 24까지 인벤토리, 25부터는 장비
        inventoryName = playerInventoryName;
        //GetItem(0, 1);
    }

    public override void GenerateData(int slots, int equipSlots = 0)
    {
        base.GenerateData(slots, equipSlots);
        GetMoney(100);
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
            if (!canUseItem) return;
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

    public float getPowerFromItem()
    {
        return ItemDatabase.Instance.getWeaponDamage(inventoryData.id[index]);
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
        float durabilityToDrop = inventoryData.durability[index];

        RemoveItem(index, quantityToDrop);

        if (player == null) player = GetComponent<Player>();
        Transform playerTransform = player.transform;
        Vector3 dropLocation = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 0.5f;

        photonView.RPC("PunRPC_Master_InstantiateDroppedItem", RpcTarget.MasterClient, itemIDToDrop, quantityToDrop, durabilityToDrop, dropLocation);
    }

    public bool HoldingInteractableItem() //들고 있을 때 상호작용 가능한 아이템인지 확인 =>InteractableObject와 연계
    {
        if (inventoryData.id[index] == -1) return true;
        return ItemDatabase.Instance.getInteractable(inventoryData.id[index]);
    }

    [PunRPC]
    public void PunRPC_AddItem(int id, int quantity, float durability)
    {
        GetItem(id, quantity, durability); // 기존에 있던 아이템 추가 로직 호출
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
    public void PunRPC_Master_InstantiateDroppedItem(int itemID, int amount, float durability, Vector3 location)
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
                itemView.RPC("PunRPC_SetItemProperties", RpcTarget.All, itemID, amount, durability);
            }
            else
            {
                Debug.LogError($"Dropped item prefab '{prefabPath}' is missing a PhotonView component.");
            }
        }
    }

    public override void MoveItemSlot(int before, int after)
    {

        base.MoveItemSlot(before, after);
        //Debug.Log($"Switched Complete");

        if (before >= INVENTORY_SIZE || after >= INVENTORY_SIZE)
        {
            Condition condition = player.condition;

            condition.ResetStateOrigin();
            //Debug.Log($"@@ INVENTORY_SIZE = {INVENTORY_SIZE}, EQUIP_SLOTS = {inventoryData.id.Length - INVENTORY_SIZE}");
            for (int i = INVENTORY_SIZE; i < inventoryData.id.Length; i++)
            {
                //Debug.Log($"@@@ {i - INVENTORY_SIZE} 슬롯에 장착중인 장비 효과 반영 : ID = {inventoryData.id[i]}");
                condition.EquipEffect(inventoryData.id[i], i,inventoryData.durability[i]);
            }
            condition.SetBarUI();
        }
       
    }


    


}
