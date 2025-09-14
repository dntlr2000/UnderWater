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

    private void UpdateInventoryMenu()
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
    }

    public void UpdateMenu()
    {
        UpdateInventoryMenu();
        UpdateBoxMenu();
    }

    public void StorageItem(int index)
    {
        if (inventory.GetItemID(index) == -1 || inventory.GetQuantity(index) <= 0) return;
        GetItem(inventory.GetItemID(index), inventory.GetQuantity(index));
        inventory.RemoveAllItem(index);
        Debug.Log($"{index}번 아이템을 보관합니다.");
        
        UpdateMenu();
    }

    public void StorageItem()
    {
        StorageItem(inventoryIndex);
    }

    public void WithdrawItem(int index)
    {
        if (GetItemID(index) == -1 || GetQuantity(index) <= 0) return;
        inventory.GetItem(GetItemID(index), GetQuantity(index));
        RemoveAllItem(index);
        Debug.Log($"{index}번 아이템을 꺼냅니다.");
        UpdateMenu();
    }

    public void WithdrawItem()
    {
        WithdrawItem(boxIndex);
    }

    public void StorageMoney(int amount)
    {
        if (inventory.GetMoneyData() < amount) return;
        GetMoney(amount);
        inventory.GetMoney(-amount);
        UpdateMenu();
        exchangeMoney = 0;
        inputField.text = "0";
    }

    public void StorageMoney()
    {
        StorageMoney(exchangeMoney);
        exchangeMoney = 0;
        inputField.text = "0";
    }

    public void WithdrawMoney(int amount)
    {
        if (GetMoneyData() < amount) return;
        GetMoney(-amount);
        inventory.GetMoney(amount);
        UpdateMenu();

    }

    public void WithdrawMoney()
    {
        WithdrawMoney(exchangeMoney);
        exchangeMoney = 0;
        inputField.text = "0";
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
        SaveData();
        UIController uIController = FindAnyObjectByType<UIController>();
        if (uIController != null) uIController.SetBoxScreen(false);
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
}
