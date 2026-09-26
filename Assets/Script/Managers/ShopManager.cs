using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    private Inventory inventory;
    public TextMeshProUGUI GoldText;
    public bool ifShopOn;
    public RawImage buyScreen;
    public ItemSlot[] shopList;
    public RawImage sellScreen;
    public ItemSlot[] inventoryList;
    public Scrollbar scrollbar;
    public ComfirmScreen buyComfirmScreen;
    public ComfirmScreen sellComfirmScreen;
    [SerializeField] private ShopCatalog catalog;
    private ShopCatalogEntry[] goods = Array.Empty<ShopCatalogEntry>();
    private int scrollRate, selectedID = -1, confirmedItemId = -1;
    private bool ifBuyState = true;

    // 기존 Inspector 연결을 유지하면서 상품 데이터와 확인창 콜백을 준비합니다.
    private void Start()
    {
        GenerateShopData();
        UpdateMoneyData();
        if (buyComfirmScreen != null) buyComfirmScreen.onConfirmAction = ComfirmBuy;
        if (sellComfirmScreen != null) sellComfirmScreen.onConfirmAction = ComfirmSell;
    }

    // 결제가 끝나면 갱신할 수 있게 UI 수명 동안만 결과를 구독합니다.
    private void OnEnable() => ShopPurchaseService.OnNotice += RefreshAfterPurchase;

    // 비활성화된 UI가 중복으로 결과를 처리하지 않도록 구독을 해제합니다.
    private void OnDisable() => ShopPurchaseService.OnNotice -= RefreshAfterPurchase;

    // 상점을 보는 동안만 스크롤과 금액 표시를 처리합니다.
    private void Update()
    {
        if (!ifShopOn) return;
        UpdateMoneyData();
        if (Input.mouseScrollDelta.y != 0) onScroll(Input.mouseScrollDelta.y < 0 ? 1 : -1);
    }

    // 실제 로컬 플레이어의 인벤토리가 준비된 경우에만 금액을 읽습니다.
    public void UpdateMoneyData()
    {
        inventory = Inventory.Local;
        if (GoldText != null) GoldText.text = inventory != null && inventory.IsInventoryReady ? inventory.GetMoneyData() + "G" : "—G";
    }

    // 거래 완료 후 개인 금액과 현재 페이지를 다시 표시합니다.
    private void RefreshAfterPurchase(string message)
    {
        UpdateMoneyData();
        if (ifBuyState) UpdateBuyMenu(); else UpdateSellMenu();
    }

    // 현재 목록의 상품 ID로 구매를 요청하며 클라이언트에서 선차감하지 않습니다.
    public void BuyItem(int amount = 1)
    {
        if (!ifBuyState || selectedID < 0 || selectedID >= goods.Length || amount <= 0) return;
        if (ShopPurchaseService.Instance == null) { ShopPurchaseService.ShowNotice("상점이 아직 준비되지 않았습니다."); return; }
        ShopPurchaseService.Instance.RequestPurchase(goods[selectedID].item.itemId, amount);
    }

    // 판매할 아이템을 재확인하고 수량 감소와 금액 증가를 같은 인벤토리에 반영합니다.
    public void SellItem(int amount = 1)
    {
        inventory = Inventory.Local;
        if (ifBuyState || inventory == null || ShopPurchaseService.Instance?.IsBusy == true || selectedID < 0) return;
        var snapshot = inventory.CaptureInventorySnapshot();
        if (snapshot?.id == null || selectedID >= inventory.NormalSlotCount) return;
        int itemId = confirmedItemId >= 0 ? confirmedItemId : snapshot.id[selectedID];
        if (!ShopTransactions.TrySell(snapshot, selectedID, itemId, amount, inventory.NormalSlotCount, out var sold)) return;
        inventory.ApplyLoadedData(sold);
        inventory.player?.SyncInventory(sold);
        if (inventory.player != null) SaveManager.Instance.UpdatePlayerCache(inventory.player.CaptureQuestPlayerState(inventory));
        ResetSlot();
        UpdateSellMenu();
        UpdateMoneyData();
    }

    // 판매 화면 전환 시 선택과 페이지를 초기화합니다.
    public void SetSellMenu(bool state)
    {
        ResetSlot();
        DisableComfirmScreen();
        if (sellScreen != null) sellScreen.gameObject.SetActive(state);
        if (!state) return;
        ifBuyState = false;
        scrollRate = 0;
        if (buyScreen != null) buyScreen.gameObject.SetActive(false);
        UpdateMoneyData();
        UpdateSellMenu();
    }

    // 구매 화면 전환 시 실제 상품 개수에 맞는 페이지를 표시합니다.
    public void SetBuyMenu(bool state)
    {
        ResetSlot();
        DisableComfirmScreen();
        if (buyScreen != null) buyScreen.gameObject.SetActive(state);
        if (!state) return;
        ifBuyState = true;
        scrollRate = 0;
        if (sellScreen != null) sellScreen.gameObject.SetActive(false);
        UpdateBuyMenu();
    }

    // 판매 페이지의 일반 인벤토리 슬롯만 표시하고 빈 슬롯의 잔상도 지웁니다.
    public void UpdateSellMenu()
    {
        inventory = Inventory.Local;
        if (inventoryList == null || inventoryList.Length == 0) return;
        var data = inventory?.CaptureInventorySnapshot();
        int count = data == null ? 0 : inventory.NormalSlotCount;
        ClampPage(count, inventoryList.Length);
        for (int i = 0; i < inventoryList.Length; i++)
        {
            int slot = scrollRate * inventoryList.Length + i;
            var item = slot < count && data.id[slot] >= 0 ? ItemDatabase.Instance.GetItem(data.id[slot]) : null;
            FillSlot(inventoryList[i], slot, item,
                item == null ? "" : (item.price * 0.6m).ToString("0.##") + "G",
                item == null ? 0 : data.quantity[slot], item == null ? -1 : data.durability[slot]);
        }
    }

    // 상품 순번으로 아이콘과 가격을 함께 읽어 두 번째 페이지의 가격 오류를 막습니다.
    public void UpdateBuyMenu()
    {
        if (shopList == null || shopList.Length == 0) return;
        ClampPage(goods.Length, shopList.Length);
        for (int i = 0; i < shopList.Length; i++)
        {
            int index = scrollRate * shopList.Length + i;
            var entry = index < goods.Length ? goods[index] : null;
            FillSlot(shopList[i], index, entry?.item,
                entry == null ? "" : ShopTransactions.BuyPrice(entry.item, 1) + "G", 0,
                entry == null ? -1 : entry.InitialDurability);
        }
    }

    // 공통 슬롯 표시를 설정하며 아이콘·수량·내구도·선택 상태를 전부 초기화합니다.
    private void FillSlot(ItemSlot slot, int index, ItemData item, string price, int quantity, float durability)
    {
        if (slot == null) return;
        slot.SlotID = index;
        if (slot.background != null) slot.SetColor();
        if (slot.itemName != null) { slot.itemName.gameObject.SetActive(item != null); slot.itemName.text = item?.itemName ?? ""; }
        if (slot.priceText != null) { slot.priceText.gameObject.SetActive(item != null); slot.priceText.text = price; }
        if (slot.quatitiy != null) { slot.quatitiy.gameObject.SetActive(item != null && quantity > 0); slot.quatitiy.text = quantity.ToString(); }
        if (slot.itemSlotIcon != null)
        {
            slot.itemSlotIcon.texture = item?.itemIcon?.texture;
            slot.itemSlotIcon.gameObject.SetActive(item?.itemIcon != null);
        }
        if (slot.durabilityRoot != null)
        {
            slot.durabilityRoot.gameObject.SetActive(false);
            if (item != null && item.durability > 0) slot.SetDurability(durability, item.durability);
        }
        if (item != null && index == selectedID && slot.background != null) slot.SetColor(110, 123, 150);
    }

    // 목록의 실제 길이와 화면 슬롯 수로 마지막 페이지를 계산합니다.
    private void ClampPage(int count, int pageSize)
    {
        int last = Math.Max(0, (count - 1) / Math.Max(1, pageSize));
        scrollRate = Mathf.Clamp(scrollRate, 0, last);
        if (scrollbar != null)
        {
            scrollbar.gameObject.SetActive(last > 0);
            scrollbar.numberOfSteps = last + 1;
            scrollbar.SetValueWithoutNotify(last == 0 ? 0 : (float)scrollRate / last);
        }
    }

    // 활성 화면만 이동하고 이전 페이지의 확인창과 선택은 취소합니다.
    public void onScroll(int y)
    {
        if (!ifShopOn) return;
        ResetSlot();
        DisableComfirmScreen();
        scrollRate += y;
        if (ifBuyState) UpdateBuyMenu(); else UpdateSellMenu();
    }

    // 현재 화면의 유효한 슬롯만 선택하고 다른 화면의 같은 번호는 건드리지 않습니다.
    public void SelectSlot(int index)
    {
        ResetSlot();
        DisableComfirmScreen();
        var slots = ifBuyState ? shopList : inventoryList;
        int count = ifBuyState ? goods.Length : Inventory.Local?.NormalSlotCount ?? 0;
        if (slots == null || slots.Length == 0 || index < 0 || index >= count || index / slots.Length != scrollRate) return;
        if (!ifBuyState && Inventory.Local.GetItemID(index) < 0) return;
        selectedID = index;
        if (slots[index % slots.Length]?.background != null) slots[index % slots.Length].SetColor(110, 123, 150);
    }

    // 화면 전환과 스크롤에서 이전 선택의 강조와 확인 대상 ID를 지웁니다.
    public void ResetSlot()
    {
        var slots = ifBuyState ? shopList : inventoryList;
        if (selectedID >= 0 && slots?.Length > 0 && slots[selectedID % slots.Length]?.background != null)
            slots[selectedID % slots.Length].SetColor();
        selectedID = confirmedItemId = -1;
    }

    // 기존 공개 메서드를 유지하고 에셋의 중복 없는 실제 상품만 목록으로 만듭니다.
    public void GenerateShopData(int level = 0)
    {
        if (catalog == null) catalog = Resources.Load<ShopCatalog>("Data/ShopCatalog");
        goods = catalog == null ? Array.Empty<ShopCatalogEntry>() : catalog.entries
            .Where(e => e?.item != null && e.item.itemId > 0).GroupBy(e => e.item.itemId).Select(g => g.First()).ToArray();
        ResetSlot();
    }

    // 상점 순번을 실제 아이템 ID로 변환하며 잘못된 순번은 -1을 반환합니다.
    public int GetItemId(int shopId) => shopId >= 0 && shopId < goods.Length ? goods[shopId].item.itemId : -1;

    // 확인 시의 아이템을 기억하고 실제 수량별 계산기로 최종 금액을 보여줍니다.
    public void SetComfirmScreen(bool ifBuy)
    {
        if (selectedID < 0 || ifBuy != ifBuyState || ShopPurchaseService.Instance?.IsBusy == true) return;
        if (ifBuy)
        {
            if (selectedID >= goods.Length || buyComfirmScreen == null) return;
            var item = goods[selectedID].item;
            confirmedItemId = item.itemId;
            buyComfirmScreen.gameObject.SetActive(true);
            buyComfirmScreen.ConstructComfirmScreen(item.itemId, n => ShopTransactions.BuyPrice(item, n), 10);
        }
        else
        {
            inventory = Inventory.Local;
            if (inventory == null || selectedID >= inventory.NormalSlotCount || inventory.GetItemID(selectedID) < 0 || sellComfirmScreen == null) return;
            var item = ItemDatabase.Instance.GetItem(inventory.GetItemID(selectedID));
            confirmedItemId = item.itemId;
            sellComfirmScreen.gameObject.SetActive(true);
            sellComfirmScreen.ConstructComfirmScreen(item.itemId, n => ShopTransactions.SellPrice(item, n), Math.Min(10, inventory.GetQuantity(selectedID)));
        }
    }

    // 확인창을 연 뒤 상품이 바뀌지 않은 경우에만 구매합니다.
    public void ComfirmBuy()
    {
        if (GetItemId(selectedID) == confirmedItemId) BuyItem(buyComfirmScreen.amount);
    }

    // 확인창 이후 선택이 취소되었다면 오래된 판매 콜백을 무시합니다.
    public void ComfirmSell()
    {
        if (confirmedItemId >= 0 && sellComfirmScreen != null) SellItem(sellComfirmScreen.amount);
    }

    // 화면 전환 시 두 확인창을 닫습니다.
    public void DisableComfirmScreen()
    {
        if (buyComfirmScreen != null) buyComfirmScreen.gameObject.SetActive(false);
        if (sellComfirmScreen != null) sellComfirmScreen.gameObject.SetActive(false);
    }
}
