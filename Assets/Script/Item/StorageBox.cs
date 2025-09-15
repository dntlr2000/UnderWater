using Photon.Pun;
using TMPro;
using UnityEngine;

public class StorageBox : InventoryFrame
{
    public int inventoryIndex;
    public int boxIndex;

    public Inventory inventory;
    public ItemUIManager boxUI; //박스의 아이템 UI, InventoryFrame의 itemUI는 사용자의 인벤토리의 UI에 할당
    public string boxName = "storageBox";
    public bool ifBoxOpen = false;

    public TMP_InputField inputField;
    public int exchangeMoney;

    public int linkedViewID; // 현재 상호작용 중인 OpenableStorageBox의 PhotonView ID
    private PhotonView linkedPhotonView;

    //public bool usingPhoton = false;

    private void Start()
    {
        SetBox();
        inventoryName = boxName;

    }

    private void Awake()
    {
        //UpdateMenu();
    }

    public void UpdateInventoryMenu()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }
        int invLen = ItemUI.itemSlots.Length;

        for (int i = 0; i < invLen; i++)
        {
            ItemUI.itemSlots[i].itemSlotIcon.gameObject.SetActive(true);
            ItemUI.itemSlots[i].quatitiy.gameObject.SetActive(true);
        }

        //인벤토리에서 로드
        for (int i = 0; i < invLen; i++)
        {
            if (inventory.GetItemID(i) == -1)
            {
                ItemUI.itemSlots[i].itemSlotIcon.gameObject.SetActive(false);
                ItemUI.itemSlots[i].quatitiy.gameObject.SetActive(false);
                continue;
            }
            ItemUI.SetQuantity(i, inventory.GetQuantity(i));
            ItemUI.LoadIcons(i, inventory.GetIcon(inventory.GetItemID(i)));

        }

        ItemUI.UpdateMoney(inventory.GetMoneyData());
    }

    private void UpdateBoxMenu()
    {
        if (inventoryData == null)
        {
            inventoryData = new InventoryData();
            GenerateData();
        }
        int invLen = boxUI.itemSlots.Length;

        for (int i = 0; i < invLen; i++)
        {
            boxUI.itemSlots[i].itemSlotIcon.gameObject.SetActive(true);
            boxUI.itemSlots[i].quatitiy.gameObject.SetActive(true);
        }

        //박스창에서 로드
        //Debug.Log("박스창에서 로드를 시도합니다.");
        for (int i = 0; i < invLen; i++)
        {
            if (GetItemID(i) == -1)
            {
                boxUI.itemSlots[i].itemSlotIcon.gameObject.SetActive(false);
                boxUI.itemSlots[i].quatitiy.gameObject.SetActive(false);
                continue;
            }
            boxUI.SetQuantity(i, GetQuantity(i));
            boxUI.LoadIcons(i, GetIcon(GetItemID(i)));
        }
        boxUI.UpdateMoney(GetMoneyData());
        //Debug.Log("박스창에서 로드를 마쳤습니다.");
    }

    public void UpdateMenu()
    {
        UpdateInventoryMenu();
        UpdateBoxMenu();
    }


    // OpenableStorageBox에서 호출하여 어떤 박스와 연결되었는지 알려주는 함수
    public void LinkToPhysicalBox(int viewID)
    {
        linkedViewID = viewID;
        linkedPhotonView = PhotonView.Find(viewID);
        if (linkedPhotonView == null)
        {
            Debug.LogError($"ID {viewID}를 가진 PhotonView를 찾을 수 없습니다.");
        }
    }

    // 마스터로부터 받은 데이터로 UI를 직접 업데이트하는 함수
    public void UpdateBoxUIFromData(InventoryData data)
    {
        Debug.Log("UpdateBoxUIFromData 메서드 호출");
        inventoryData = data; // 데이터 교체
        UpdateBoxMenu(); // UI 새로고침
    }


    public void StorageItem(int index)
    {
        if (inventory.GetItemID(index) == -1 || inventory.GetQuantity(index) <= 0) return;

        // --- 로컬에서 직접 데이터를 변경하는 대신 RPC 호출 ---
        int itemID = inventory.GetItemID(index);
        int quantity = inventory.GetQuantity(index);

        if (linkedPhotonView != null)
        {
            // 마스터 클라이언트에게 아이템을 보관해달라고 요청
            linkedPhotonView.RPC("PunRPC_RequestStoreItem", RpcTarget.MasterClient, index, itemID, quantity);

            // 요청을 보낸 후, 클라이언트 측의 인벤토리에서 아이템을 즉시 제거하여 반응성을 높임
            inventory.RemoveAllItem(index);
            UpdateInventoryMenu(); // 인벤토리 UI 즉시 업데이트
        }

        // UpdateMenu()는 이제 동기화 RPC를 받았을 때 자동으로 호출되므로 여기서 호출하지 않습니다.
        // Debug.Log($"{index}번 아이템 보관을 요청합니다.");
    }

    public void StorageItem()
    {
        StorageItem(inventoryIndex);
    }

    public void WithdrawItem(int index)
    {
        if (GetItemID(index) == -1 || GetQuantity(index) <= 0) return;

        if (linkedPhotonView != null)
        {
            // 자신의 플레이어 캐릭터(Inventory 스크립트가 있는)의 PhotonView를 찾습니다.
            PhotonView playerPhotonView = inventory.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 요청 시 플레이어의 PhotonView ID를 함께 넘겨줍니다.
                linkedPhotonView.RPC("PunRPC_RequestWithdrawItem", RpcTarget.MasterClient, index, playerPhotonView.ViewID);
            }
            else
            {
                Debug.LogError("플레이어의 PhotonView를 찾을 수 없습니다! Inventory.cs와 같은 오브젝트에 PhotonView를 추가해주세요.");
            }
        }

        UpdateInventoryMenu();
    }

    public void WithdrawItem()
    {
        WithdrawItem(boxIndex);
    }



    public void StorageMoney()
    {
        SetExchangeMoney();
        if (exchangeMoney <= 0 || inventory.GetMoneyData() < exchangeMoney) return;

        if (linkedPhotonView != null)
        {
            PhotonView playerPhotonView = inventory.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 주석을 풀고 RPC를 호출합니다.
                linkedPhotonView.RPC("PunRPC_RequestDepositMoney", RpcTarget.MasterClient, exchangeMoney, playerPhotonView.ViewID);

                // 로컬 돈 즉시 차감 (반응성을 위해)
                inventory.GetMoney(-exchangeMoney);
                UpdateInventoryMenu();
                inputField.text = "0";
                exchangeMoney = 0;
            }
        }
    }

    public void WithdrawMoney()
    {
        SetExchangeMoney();
        if (exchangeMoney <= 0) return;

        // 로컬에서 미리 체크 (선택사항, 더 나은 UX를 위함)
        if (GetMoneyData() < exchangeMoney)
        {
            Debug.Log("UI에 표시된 잔액이 부족합니다.");
            return;
        }

        if (linkedPhotonView != null)
        {
            PhotonView playerPhotonView = inventory.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 로컬 데이터를 직접 바꾸는 대신, 마스터에게 출금을 요청합니다.
                linkedPhotonView.RPC("PunRPC_RequestWithdrawMoney", RpcTarget.MasterClient, exchangeMoney, playerPhotonView.ViewID);
                inputField.text = "0";
                exchangeMoney = 0;
            }
        }
    }

    public void WithdrawMoney(int amount)
    {
        if (GetMoneyData() < amount) return;
        GetMoney(-amount);
        inventory.GetMoney(amount);
        UpdateMenu();

    }

    public void SetExchangeMoney()
    {
        if (int.TryParse(inputField.text, out int result))
        {
            if (result <= 0) return;
            exchangeMoney = result;

        }
        else
        {
            Debug.LogWarning("정수형 및 양수만 입력해주세요.");
            exchangeMoney = 0;
        }
    }

    public void SetBoxName(string name)
    {
        inventoryName = name;
    }

    public void SetBoxIndex(int _index)
    {
        boxIndex = _index;
    }

    public void SetInventorytIndex(int _index)
    {
        inventoryIndex = _index;
    }


    public void CloseBox()
    {
        UIController uIController = FindAnyObjectByType<UIController>();
        if (uIController != null) uIController.SetBoxScreen(false);

        // 링크 해제
        linkedViewID = 0;
        linkedPhotonView = null;
    }

    public void SetBox()
    {
        inventoryData = new InventoryData();
        inventoryData.GenerateData();

        GetMoney(200);

        ItemUI.SetSlotIDs();
        boxUI.SetSlotIDs();
    }

    public void LoadBox()
    {
        LoadData();
        UpdateMenu();
    }

    public override void GenerateData()
    {
        // 이 함수는 이제 마스터 클라이언트의 OpenableStorageBox에서만 호출되므로,
        // 클라이언트의 StorageBox UI에서는 필요가 없어지거나 비워둘 수 있습니다.
        // : 아닌 것으로 보임
        base.GenerateData(); 
    }



}
