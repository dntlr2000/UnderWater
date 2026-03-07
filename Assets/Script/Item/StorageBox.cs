using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public RawImage comfirmScreen;
    public TextMeshProUGUI ItemNameText; //구매 또는 판매임을 알리는 텍스트
    public TextMeshProUGUI amountText;
    public int amount;
    public Button depositComfirmButton;
    public Button withdrawComfirmButton;
    public Scrollbar amountBar;

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
            inventory = FindAnyObjectByType<Inventory>(); //플레이어 인벤토리
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


    public void StorageItem(int index, int amount)
    {
        if (inventory.GetItemID(index) == -1 || inventory.GetQuantity(index) <= 0) return;

        int itemID = inventory.GetItemID(index);
        int quantity = inventory.GetQuantity(index);
        float durability = inventory.GetDurability(index);

        int trueAmount = amount;

        if (quantity < amount)
        {
            Debug.Log("보관하려는 개수가 소지 개수보다 많으므로 소지 개수로 재조정됩니다. ");
            trueAmount = quantity;
        };
        if (quantity <= 0) return;

        if (linkedPhotonView != null)
        {
            linkedPhotonView.RPC("PunRPC_RequestStoreItem", RpcTarget.MasterClient, index, itemID, trueAmount, durability);

            inventory.RemoveItem(index, amount);
            UpdateInventoryMenu(); // 인벤토리 UI 즉시 업데이트
        }

    }

    public void StorageItem()
    {
        if (inventoryIndex == -1) return;
        StorageItem(inventoryIndex, amount);
    }

    public void WithdrawItem(int index, int amount)
    {
        if (GetItemID(index) == -1 || GetQuantity(index) <= 0) return;

        int quantity = GetQuantity(index);
        int trueAmount = amount;

        if (quantity < amount)
        {
            trueAmount= quantity;
            Debug.Log("꺼내려는 개수가 실제로 보관되어 있는 아이템의 개수보다 많으므로 재조정됩니다.");
        };

        if (quantity <= 0) return;

        if (linkedPhotonView != null)
        {
            // 자신의 플레이어 캐릭터(Inventory 스크립트가 있는)의 PhotonView를 찾습니다.
            PhotonView playerPhotonView = inventory.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 요청 시 플레이어의 PhotonView ID를 함께 넘겨줍니다.
                linkedPhotonView.RPC("PunRPC_RequestWithdrawItem", RpcTarget.MasterClient, index, playerPhotonView.ViewID, trueAmount);
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
        if (boxIndex== -1) return;
        WithdrawItem(boxIndex, amount);
    }



    public void StorageMoney()
    {
        SetExchangeMoney();
        int trueExchangeMoney = exchangeMoney;
        if (exchangeMoney <= 0) return;
        if (inventory.GetMoneyData() < exchangeMoney) trueExchangeMoney = inventory.GetMoneyData();

        if (linkedPhotonView != null)
        {
            PhotonView playerPhotonView = inventory.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 주석을 풀고 RPC를 호출합니다.
                linkedPhotonView.RPC("PunRPC_RequestDepositMoney", RpcTarget.MasterClient, trueExchangeMoney, playerPhotonView.ViewID);

                // 로컬 돈 즉시 차감 (반응성을 위해)
                inventory.GetMoney(-trueExchangeMoney);
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
        int trueExchangeMoney = exchangeMoney;
        // 로컬에서 미리 체크 (선택사항, 더 나은 UX를 위함)
        if (GetMoneyData() < exchangeMoney)
        {
            Debug.Log("UI에 표시된 잔액이 부족합니다.");
            trueExchangeMoney = GetMoneyData();
        }

        if (linkedPhotonView != null)
        {
            PhotonView playerPhotonView = inventory.GetComponent<PhotonView>();
            if (playerPhotonView != null)
            {
                // 로컬 데이터를 직접 바꾸는 대신, 마스터에게 출금을 요청합니다.
                linkedPhotonView.RPC("PunRPC_RequestWithdrawMoney", RpcTarget.MasterClient, trueExchangeMoney, playerPhotonView.ViewID);
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
        if (boxIndex != -1) boxUI.SetColors(boxIndex);
        boxIndex = _index;
        boxUI.SetColors(_index, 110, 123, 150);

        if (inventoryIndex != -1) ItemUI.SetColors(inventoryIndex);
        inventoryIndex = -1;
    }

    public void SetInventorytIndex(int _index)
    {
        if (inventoryIndex != -1)ItemUI.SetColors(inventoryIndex);
        inventoryIndex = _index;
        ItemUI.SetColors(_index, 110, 123, 150);

        if (boxIndex != -1) boxUI.SetColors(boxIndex);
        boxIndex = -1;
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

    public void SetComfirmScreen(bool ifDeposit)
    {
        //comfirmScreen 오브젝트가 활성화 되어야 스크립트를 사용할 수 있으므로 예외처리 후에 활성화, 그리고 보관/반출 모드 적용
        if (ifDeposit) //보관 모드
        {
            if (inventoryIndex == -1) return;
            if (inventory.GetItemID(inventoryIndex) == -1) return;
        }

        if (!ifDeposit) //반출 모드
        {
            if (boxIndex== -1) return;
            if (GetItemID(boxIndex) == -1) return;
        }
        comfirmScreen.gameObject.SetActive(true);

        if (ifDeposit) //보관 모드
        {
            ItemNameText.text = ItemDatabase.Instance.getItemName(inventory.GetItemID(inventoryIndex));
            withdrawComfirmButton.gameObject.SetActive(false);
            depositComfirmButton.gameObject.SetActive(true);
        }

        if (!ifDeposit) //반출 모드
        {
            ItemNameText.text = ItemDatabase.Instance.getItemName(GetItemID(boxIndex));
            withdrawComfirmButton.gameObject.SetActive(true);
            depositComfirmButton.gameObject.SetActive(false);
        }
        amount = 1;
        amountBar.value = 0;
        amountText.text = "1 / 10";
        //priceText.text = "\\ " + (shopPrice[selectedID] * amount);
    }

    public void DisableComfirmScreen()
    {
        comfirmScreen.gameObject.SetActive(false);
        return;
    }

    public void onScrollAmountChanged()
    {
        amount = Mathf.RoundToInt(amountBar.value * (amountBar.numberOfSteps - 1)) + 1;
        amountText.text = $"{amount} / 10";
        //priceText.text = "\\ " + (shopPrice[selectedID] * amount);
    }


}
