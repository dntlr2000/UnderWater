using Photon.Pun;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : InventoryFrame
{
    public int index; //현재 들고 있는 아이템
    public Transform IndexLine;
    public Player player; //플레이어가 포톤을 통해 자신의 인벤토리를 할당하는 기능 필요
    public string playerInventoryName = "inventory";
    //private bool showInventory = false; //ItemUI에서 일단 가져와봄 Update 부하를 줄이기 위해 ItemUI의 메서드를 여기로 옮길 수 있음
    PhotonView photonView;

    private bool canUseItem = true;

    public ComfirmScreen throwScreen;
    public int throwIndex;

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
        GenerateData(25, 1);
        //0 ~ 24까지 인벤토리, 25부터는 장비
        inventoryName = playerInventoryName;
        //GetItem(0, 1);
        if (throwScreen != null)
        {
            throwScreen.onConfirmAction = this.ConfirmThrowItem;
        }
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
            //if (!HoldingInteractableItem()) return; //들고 있는 아이템이 상호작용을 거부하는 아이템인 경우 false가 리턴됨
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
        if (player != null) player.SyncInventory(inventoryData);
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

        var itemData = ItemDatabase.Instance.GetItem(id);
        if (itemData != null && QuestManager.Instance != null)
            QuestManager.Instance.ReportObjectiveProgress(ObjectiveType.CollectItem, quantity, itemData.stringID);
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
            RefreshEquipments();
        }
       
    }


    public void ApplyLoadedData(InventoryData loadedData)
    {
        if (loadedData == null || loadedData.id == null || loadedData.id.Length < INVENTORY_SIZE)
        {
            Debug.LogWarning("수신된 인벤토리 데이터가 비어있거나 손상되어 새로 생성합니다.");
            GenerateData(25, 1);
            return;
        }

        //데이터 덮어쓰기
        this.inventoryData = loadedData;

        //돈 UI 업데이트
        ItemUI.UpdateMoney(inventoryData.money);

        //슬롯 UI 업데이트
        for (int i = 0; i < inventoryData.id.Length; i++)
        {
            if (inventoryData.id[i] == -1)
            {
                ItemUI.ResetIcons(i);
            }
            else
            {
                Sprite itemSprite = ItemDatabase.Instance.GetIcons(inventoryData.id[i]);
                ItemUI.LoadIcons(i, itemSprite);
                ItemUI.SetQuantity(i, inventoryData.quantity[i]);
            }
        }
        RefreshEquipments();
        Debug.Log("저장된 인벤토리 데이터 복구 완료!");
    }

    public void RefreshEquipments()
    {
        if (player == null || player.condition == null) return;

        Condition condition = player.condition;
        condition.ResetStateOrigin(); //먼저 기본 상태로 초기화 후 다시 장착 효과 반영

        for (int i = INVENTORY_SIZE; i < inventoryData.id.Length; i++)
        {
            condition.EquipEffect(inventoryData.id[i], i, inventoryData.durability[i]);
        }

        condition.SetBarUI();
    }

    public void ChangeCanUseItem(bool value)
    {
        canUseItem = value;
    }

    public void SetThrowScreen(bool ifThrow)
    {
        //comfirmScreen 오브젝트가 활성화 되어야 스크립트를 사용할 수 있으므로 예외처리 후에 활성화, 그리고 보관/반출 모드 적용
        if (ifThrow) //보관 모드
        {
            throwScreen.gameObject.SetActive(true);
            throwScreen.ConstructComfirmScreen(GetItemID(throwIndex));
        }
        else
        {
            throwScreen.gameObject.SetActive(false);
        }

    }

    private void ConfirmThrowItem()
    {
        // index는 Inventory가 이미 알고 있는 '현재 선택된 인덱스'입니다.
        int dropAmount = throwScreen.amount;
        DropItem(throwIndex, dropAmount);
        Debug.Log($"아이템 {dropAmount}개를 버렸습니다.");
    }

    [PunRPC]
    public void PunRPC_AddMoney(int amount)
    {
        GetMoney(amount);
        Debug.Log($"[Inventory] 돈 획득 승인됨. {amount}G 획득! 현재 잔액: {inventoryData.money}G");
    }
}
