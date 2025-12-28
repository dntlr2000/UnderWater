using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    Inventory inventory;
    //ItemDatabase database;
    public TextMeshProUGUI GoldText;

    public bool ifShopOn = false;
    public RawImage buyScreen;
    public ItemSlot[] shopList;

    public RawImage sellScreen;
    public ItemSlot[] inventoryList;

    public Scrollbar scrollbar;

    int scrollRate = 0;
    int MaxScrollRate = 4;

    int selectedID = -1;

    int[] shopItems; //상점에 팔 아이템 ID 저장
    float[] shopDurability;
    int[] shopPrice; //상점 품목 별 가격

    public RawImage comfirmScreen;
    public TextMeshProUGUI ItemNameText; //구매 또는 판매임을 알리는 텍스트
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI priceText;
    public int amount;
    protected int price;
    //protected bool ifBuyComfirm;
    public Button buyComfirmButton;
    public Button sellComfirmButton;
    public Scrollbar amountBar;

    bool ifBuyState = false;


    private void Start()
    {
        //database = new ItemDatabase();
        //database.GenerateData();
        UpdateMoneyData();
        GenerateShopData(0);
    }
    private void Awake()
    {
        //UpdateMoneyData();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetBuyMenu(false);
            SetSellMenu(false);
        }

        if (Input.mouseScrollDelta.y < 0)
        {
            //scrollRate += 1;
            onScroll(1);
        }

        if (Input.mouseScrollDelta.y > 0)
        {
            //scrollRate -= 1;
            onScroll(-1);
        }
    }

    public void UpdateMoneyData()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
            if (inventory == null) { Debug.LogError("인벤토리를 찾을 수 없습니다"); }

        }

        GoldText.text = inventory.GetMoneyData() + "G";
    }

    void BuyItem(int itemId, int amount)
    {

    }

    public void BuyItem(int amount = 1)
    {
        if (selectedID == -1 || shopItems[selectedID] == -1 || selectedID >= shopItems.Length)
        {
            return;
        }
        if (inventory.GetMoneyData() < shopPrice[selectedID] * amount)
        {
            Debug.Log("돈이 부족합니다!");
            return;
        }

        inventory.GetMoney(-shopPrice[selectedID] * amount);
        if (ItemDatabase.Instance.getSingularity(shopItems[selectedID]) == true)
        {
            for (int i = 0; i < amount; i++) inventory.GetItem(shopItems[selectedID], 1, shopDurability[selectedID]);
        }  
        else inventory.GetItem(shopItems[selectedID], amount, shopDurability[selectedID]);
        UpdateMoneyData();

    }

    void SellItem(int Index, int amount)
    {

    }

    public void SellItem(int amount = 1)
    {
        if (selectedID == -1 || inventory.GetItemID(selectedID) == -1)
        {
            Debug.Log("선택된 아이템이 없습니다.");
            return;
        }
        int trueAmount = amount;
        if (amount > inventory.GetQuantity(selectedID)) trueAmount = inventory.GetQuantity(selectedID);

        inventory.GetMoney(ItemDatabase.Instance.getPrice(inventory.GetItemID(selectedID)) * trueAmount);
        inventory.RemoveItem(selectedID, trueAmount);
        UpdateSellMenu();
        UpdateMoneyData();
    }

    public void SetSellMenu(bool state)
    {
        if (state)
        {
            sellScreen.gameObject.SetActive(true);
            //scrollbar.gameObject.SetActive(true);
            scrollRate = 0;
            MaxScrollRate = 3;
            UpdateSellMenu();
        }
        else
        {
            ResetSlot();
            sellScreen.gameObject.SetActive(false);
            scrollbar.gameObject.SetActive(false);
        }
    }

    public void SetBuyMenu(bool state)
    {
        if (state)
        {
            //scrollbar.gameObject.SetActive(true);
            buyScreen.gameObject.SetActive(true);
            MaxScrollRate = 2;
            scrollRate = 0;
            UpdateBuyMenu();
        }
        else
        {
            ResetSlot();
            buyScreen.gameObject.SetActive(false);
            scrollbar.gameObject.SetActive(false);
        }
    }

    public void UpdateSellMenu()
    {
        int invLen = inventoryList.Length;
        for (int i = 0; i < invLen; i++)
        {
            inventoryList[i].itemName.gameObject.SetActive(true);
            inventoryList[i].priceText.gameObject.SetActive(true);
            inventoryList[i].quatitiy.gameObject.SetActive(true);
            inventoryList[i].itemSlotIcon.gameObject.SetActive(true);
        }

        //인벤토리에서 로드
        for (int i = 0; i < invLen; i++)
        {
            int k = invLen * (scrollRate) + i;

            inventoryList[i].SlotID = k;


            //if (k >= 25) //현재 인벤토리 슬롯 개수 : 25
            //{
            //    return;
            //}



            if (k >= 25 || inventory.GetItemID(k) == -1) {
                inventoryList[i].itemName.gameObject.SetActive(false);
                inventoryList[i].priceText.gameObject.SetActive(false);
                inventoryList[i].quatitiy.gameObject.SetActive(false);
                inventoryList[i].itemSlotIcon.gameObject.SetActive(false);
                continue;
            }
            inventoryList[i].itemName.text = ItemDatabase.Instance.getItemName(inventory.GetItemID(k));
            inventoryList[i].priceText.text = ItemDatabase.Instance.getPrice(inventory.GetItemID(k)) + "G";
            inventoryList[i].quatitiy.text = inventory.GetQuantity(k).ToString();
            //inventoryList[i].itemSlotIcon.texture = database.LoadIcons(inventory.GetItemID(k)).texture;
            inventoryList[i].itemSlotIcon.texture = inventory.GetIcon(inventory.GetItemID(k)).texture;
        }

        if (selectedID != -1)
        {
            //if (selectedID < shopList.Length) shopList[selectedID].SetColor();
            //if (selectedID < 30) inventoryList[selectedID % 8].SetColor();
            
            //ResetSlot();
        }
    }


    public void UpdateBuyMenu()
    {
        int shopLen = shopList.Length;
        for (int i = 0; i < shopLen; i++)
        {
            shopList[i].itemName.gameObject.SetActive(true);
            shopList[i].priceText.gameObject.SetActive(true);
            shopList[i].itemSlotIcon.gameObject.SetActive(true);
        }

        if (inventory == null) Debug.LogError("인벤토리가 없습니다.");



        for (int i = 0; i < shopLen; i++)
        {
            int k = shopLen * scrollRate + i;
            shopList[i].SlotID = k;
            
            if (k >= shopItems.Length || shopItems[k] == -1)
            {
                shopList[i].itemName.gameObject.SetActive(false);
                shopList[i].priceText.gameObject.SetActive(false);
                //shopList[i].quatitiy.gameObject.SetActive(false);
                shopList[i].itemSlotIcon.gameObject.SetActive(false);
                continue;
            }

            shopList[i].itemName.text = ItemDatabase.Instance.getItemName(shopItems[k]);
            shopList[i].priceText.text = shopPrice[i] + "G";
            //shopList[i].quatitiy.text = 
            shopList[i].itemSlotIcon.texture = ItemDatabase.Instance.LoadIcons(shopItems[k]).texture;

        }

        if (selectedID != -1)
        {
            if (selectedID < shopList.Length) shopList[selectedID].SetColor();
            if (selectedID < 30) inventoryList[selectedID % 8].SetColor();
        }
    }

    public void onScroll(int y)
    {
        scrollRate += y;
        if (scrollRate <= 0) scrollRate= 0;
        if (scrollRate >= MaxScrollRate) scrollRate = MaxScrollRate;
        Debug.Log($"Scroll Rate = {scrollRate}");

        ResetSlot();
        //selectedID = -1;
        UpdateBuyMenu();
        UpdateSellMenu();
        
    }

    public void SelectSlot(int index)
    {
        if (selectedID != -1)
        {
            if (selectedID < shopItems.Length) shopList[selectedID % 8].SetColor();
            if (selectedID < 25) inventoryList[selectedID % 8].SetColor();
        }
        selectedID = index;
        if (ifBuyState && index >= 25)
        {
            ResetSlot();
            return;
        }
        
        if (selectedID < shopItems.Length) shopList[index % 8].SetColor(110, 123, 150);
        if (selectedID < 25) inventoryList[index % 8].SetColor(110, 123, 150);
    }

    public void ResetSlot()
    {
        if (selectedID == -1) return;
        shopList[selectedID % 8].SetColor();
        inventoryList[selectedID % 8].SetColor();
        selectedID = -1;
    }

    public void GenerateShopData(int level = 0)
    {
        //level : 레벨에 따른 순차 개방 기능을 위해 구현
        shopItems = new int[10]; //임시로 2개 품목만 구현
        shopPrice = new int[10];
        shopDurability = new float[10];

        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i] = -1;
            shopPrice[i] = 0;
            shopDurability[i] = -1;
        }

        shopItems[0] = 0;
        shopItems[1] = 1;
        shopItems[2] = 2;
        shopItems[3] = 3;
        shopItems[4] = 4;
        
        //shopItems[5] = 5;
        //shopDurability[5] = 50f;

        shopItems[6] = 6;
        shopDurability[6] = 100f;

        for (int i = 0; i < shopItems.Length; i++)
        {
            if (shopItems[i] == -1) continue;
            shopPrice[i] = 2 * ItemDatabase.Instance.items[shopItems[i]].price;
        }

    }

    public int GetItemId(int shopId)
    {
        return ItemDatabase.Instance.items[shopId].itemId;
    }

    public void SetComfirmScreen(bool ifBuy)
    {
        if (selectedID == -1) return;
        ifBuyState = ifBuy;
        if (ifBuy && (selectedID >= shopItems.Length || shopItems[selectedID] == -1)) //구매 모드
        {
            ResetSlot();
            return;
        }

        if (!ifBuy && (selectedID >= 25 || inventory.GetItemID(selectedID) == -1)) //판매 모드
        {
            ResetSlot();
            return;
        }

        
        comfirmScreen.gameObject.SetActive(true);
        amount = 1;
        amountBar.value = 0;
        amountText.text = "1 / 10";

        if (ifBuy) //구매 모드
        {
            ItemNameText.text = ItemDatabase.Instance.items[shopItems[selectedID]].itemName;
            sellComfirmButton.gameObject.SetActive(false);
            buyComfirmButton.gameObject.SetActive(true);
            priceText.text = "G: " + (shopPrice[selectedID] * amount);
        }

        if (!ifBuy) //판매 모드
        {
            ItemNameText.text = ItemDatabase.Instance.items[inventory.GetItemID(selectedID)].itemName;
            sellComfirmButton.gameObject.SetActive(true);
            buyComfirmButton.gameObject.SetActive(false);
            priceText.text = "G: " + (ItemDatabase.Instance.getPrice(inventory.GetItemID(selectedID)) * amount);
        }
        
        //priceText.text = "\\ " + (shopPrice[selectedID] * amount);
    }

    public void DisableComfirmScreen()
    {
        comfirmScreen.gameObject.SetActive(false);
        return;
    }

    public void ComfirmBuy()
    {
        BuyItem(amount);
    }

    public void ComfirmSell()
    {
        SellItem(amount);
    }

    public void onScrollAmountChanged()
    {
        if (selectedID == -1)
        {
            comfirmScreen.gameObject.SetActive(false);
            return;
        }
        amount = Mathf.RoundToInt(amountBar.value * (amountBar.numberOfSteps - 1)) + 1;
        amountText.text = $"{amount} / 10";
        
        if (ifBuyState) priceText.text = "G: " + (shopPrice[selectedID] * amount);
        else priceText.text = "G: " + (ItemDatabase.Instance.getPrice(inventory.GetItemID(selectedID)) * amount);
    }
}

