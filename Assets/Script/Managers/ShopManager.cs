using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    Inventory inventory;
    ItemDatabase database;
    public TextMeshProUGUI GoldText;

    public bool ifShopOn = false;
    public RawImage buyScreen;
    public ItemSlot[] shopList;

    public RawImage sellScreen;
    public ItemSlot[] inventoryList;

    public Scrollbar scrollbar;

    int scrollRate = 0;

    int selectedID = -1;

    int[] shopItems; //상점에 팔 아이템 ID 저장
    int[] shopPrice; //상점 품목 별 가격

    private void Start()
    {
        database = new ItemDatabase();
        database.GenerateData();
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

    void UpdateMoneyData()
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
        if (shopItems[selectedID] == -1)
        {
            return;
        }
        if (inventory.GetMoneyData() < shopPrice[selectedID])
        {
            Debug.Log("돈이 부족합니다!");
            return;
        }

        inventory.GetMoney(-shopPrice[selectedID]);
        inventory.GetItem(shopItems[selectedID]);
        UpdateMoneyData();

    }

    void SellItem(int Index, int amount)
    {

    }

    public void SellItem(int amount = 1)
    {
        if (inventory.GetItemID(selectedID) == -1)
        {
            Debug.Log("선택된 아이템이 없습니다.");
            return;
        }

        inventory.GetMoney(database.getPrice(inventory.GetItemID(selectedID)) * amount);
        inventory.RemoveItem(selectedID, amount);
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
            UpdateSellMenu();
        }
        else
        {
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
            scrollRate = 0;
            UpdateBuyMenu();
        }
        else
        {
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
            int k = invLen * scrollRate + i;

            inventoryList[i].SlotID = k;

            if (k >= 30) //현재 인벤토리 슬롯 개수 : 30
            {
                scrollRate = 4;
                return;
            }

            if (inventory.GetItemID(k) == -1) {
                inventoryList[i].itemName.gameObject.SetActive(false);
                inventoryList[i].priceText.gameObject.SetActive(false);
                inventoryList[i].quatitiy.gameObject.SetActive(false);
                inventoryList[i].itemSlotIcon.gameObject.SetActive(false);
                continue;
            }
            inventoryList[i].itemName.text = database.getItemName(inventory.GetItemID(k));
            inventoryList[i].priceText.text = database.getPrice(inventory.GetItemID(k)) + "G";
            inventoryList[i].quatitiy.text = inventory.GetQuantity(k).ToString();
            inventoryList[i].itemSlotIcon.texture = database.LoadIcons(inventory.GetItemID(k)).texture;
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
            
            if (shopItems[i] == -1)
            {
                shopList[i].itemName.gameObject.SetActive(false);
                shopList[i].priceText.gameObject.SetActive(false);
                //shopList[i].quatitiy.gameObject.SetActive(false);
                shopList[i].itemSlotIcon.gameObject.SetActive(false);

                if (k >= 10) //현재 상점 슬롯 개수 : 10
                {
                    scrollRate = 1;
                    ///return;
                }
                continue;
            }

            shopList[i].itemName.text = database.getItemName(shopItems[i]);
            shopList[i].priceText.text = shopPrice[i] + "G";
            //shopList[i].quatitiy.text = 
            shopList[i].itemSlotIcon.texture = database.LoadIcons(shopItems[i]).texture;

        }


    }

    public void onScroll(int y)
    {
        scrollRate += y;
        if (scrollRate <= 0) scrollRate= 0;
        if (scrollRate >= 2) scrollRate= 2;
        Debug.Log($"Scroll Rate = {scrollRate}");
        UpdateBuyMenu();
        UpdateSellMenu();
    }

    public void SelectSlot(int index)
    {
        selectedID = index;
    }


    public void GenerateShopData(int level = 0)
    {
        //level : 레벨에 따른 순차 개방 기능을 위해 구현
        shopItems = new int[10]; //임시로 2개 품목만 구현
        shopPrice = new int[10];

        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i] = -1;
            shopPrice[i] = 0;
        }

        shopItems[0] = 0;
        shopItems[1] = 1;

        for (int i = 0; i < shopItems.Length; i++)
        {
            if (shopItems[i] == -1) continue;
            shopPrice[i] = 2 * database.items[shopItems[i]].price;
        }

    }

    public int GetItemId(int shopId)
    {
        return database.items[shopId].itemId;
    }
}

