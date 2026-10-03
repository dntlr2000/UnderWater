using Photon.Pun;
using TMPro;
using UnityEngine;

public class StorageBox : InventoryFrame
{
    public int inventoryIndex = -1;
    public int boxIndex = -1;
    public Inventory inventory;
    public ItemUIManager boxUI;
    public string boxName = "storageBox";
    public bool ifBoxOpen = false;
    public TMP_InputField inputField;
    public int exchangeMoney;
    public int linkedViewID;
    private PhotonView linkedPhotonView;
    private OpenableStorageBox linkedBox;
    private Inventory observedInventory;
    public ComfirmScreen depositScreen;
    public ComfirmScreen withdrawScreen;

    // 최초 열기 이전/이후 어느 시점에 Start가 호출되어도 받은 내용을 초기화하지 않습니다.
    private void Start()
    {
        SetBox();
        if (depositScreen != null) depositScreen.onConfirmAction = StorageItem;
        if (withdrawScreen != null) withdrawScreen.onConfirmAction = WithdrawItem;
    }

    // 화면이 켜질 때 로컬 인벤토리 변경을 구독합니다.
    private void OnEnable()
    {
        SetBox();
        BindInventory();
    }

    // 화면이 닫히면 이전 상자와 인벤토리에서 온 늦은 변경을 받지 않습니다.
    private void OnDisable() => Unlink();

    // 원격 인벤토리를 선택하지 않고 로컬 인벤토리에만 변경 알림을 연결합니다.
    private void BindInventory()
    {
        var local = Inventory.Local;
        if (local != null) inventory = local;
        else if (inventory != null && inventory.player != Player.localPlayer) inventory = null;
        if (observedInventory == inventory) return;
        if (observedInventory != null) observedInventory.ContentsChanged -= OnInventoryChanged;
        observedInventory = inventory;
        if (observedInventory != null) observedInventory.ContentsChanged += OnInventoryChanged;
    }

    // 연결된 인벤토리의 실제 수령/잔액 변경 직후 플레이어 패널을 갱신합니다.
    private void OnInventoryChanged(Inventory changed)
    {
        if (isActiveAndEnabled && changed == observedInventory) UpdateInventoryMenu();
    }

    // 연결 ID와 PhotonView가 모두 같은 상자에서 온 변경만 화면에 반영합니다.
    private void OnStorageChanged(OpenableStorageBox changed)
    {
        if (!isActiveAndEnabled || changed != linkedBox || linkedPhotonView == null || changed.boxName != boxName ||
            changed.GetComponent<PhotonView>() != linkedPhotonView || linkedPhotonView.ViewID != linkedViewID) return;
        if (!HasValidBinding()) { CloseBox(); return; }
        UpdateBoxUIFromData(changed.CaptureVisibleStorageData());
        UpdateInventoryMenu();
    }

    // 거래 직전에도 활성 상태·준비 상태·상자 ID의 유일성을 확인합니다.
    private bool HasValidBinding() => linkedBox != null && linkedPhotonView != null && linkedPhotonView.ViewID == linkedViewID &&
        linkedBox.boxName == boxName && RewardDeliveryService.FindBox(boxName) == linkedBox;

    // 구독과 선택 슬롯을 정리하여 다른 상자의 이벤트가 현재 화면에 남지 않게 합니다.
    private void Unlink()
    {
        if (linkedBox != null) linkedBox.StorageChanged -= OnStorageChanged;
        if (observedInventory != null) observedInventory.ContentsChanged -= OnInventoryChanged;
        linkedBox = null;
        linkedPhotonView = null;
        observedInventory = null;
        linkedViewID = 0;
        ifBoxOpen = false;
        if (inventoryIndex >= 0) ItemUI?.SetColors(inventoryIndex);
        if (boxIndex >= 0) boxUI?.SetColors(boxIndex);
        inventoryIndex = boxIndex = -1;
    }

    // 빈 슬롯의 아이콘·수량·내구도를 함께 지운 뒤 로컬 인벤토리 내용을 표시합니다.
    public void UpdateInventoryMenu()
    {
        BindInventory();
        if (ItemUI == null || ItemUI.itemSlots == null) return;
        for (int i = 0; i < ItemUI.itemSlots.Length; i++) ItemUI.ResetIcons(i);
        if (inventory == null || !inventory.IsInventoryReady) return;
        int length = Mathf.Min(ItemUI.itemSlots.Length, inventory.NormalSlotCount);
        for (int i = 0; i < length; i++)
        {
            int id = inventory.GetItemID(i);
            if (id < 0 || inventory.GetQuantity(i) <= 0) continue;
            ItemUI.SetQuantity(i, inventory.GetQuantity(i), inventory.GetSingularity(i));
            ItemUI.LoadIcons(i, inventory.GetIcon(id));
            ItemUI.SetDurability(i, inventory.GetDurability(i), ItemDatabase.Instance.getMaxDurability(id));
        }
        ItemUI.UpdateMoney(inventory.GetMoneyData());
    }

    // 모든 슬롯 표시를 지운 뒤 현재 상자 수량과 금액만 다시 그립니다.
    private void UpdateBoxMenu()
    {
        if (boxUI == null || boxUI.itemSlots == null) return;
        for (int i = 0; i < boxUI.itemSlots.Length; i++) boxUI.ResetIcons(i);
        if (inventoryData?.id == null) return;
        int length = Mathf.Min(boxUI.itemSlots.Length, inventoryData.id.Length);
        for (int i = 0; i < length; i++)
        {
            int id = GetItemID(i);
            if (id < 0 || GetQuantity(i) <= 0) continue;
            boxUI.SetQuantity(i, GetQuantity(i), GetSingularity(i));
            boxUI.LoadIcons(i, GetIcon(id));
            boxUI.SetDurability(i, GetDurability(i), ItemDatabase.Instance.getMaxDurability(id));
        }
        boxUI.UpdateMoney(GetMoneyData());
    }

    // 양쪽 패널을 현재 표시 데이터로 갱신합니다.
    public void UpdateMenu() { UpdateInventoryMenu(); UpdateBoxMenu(); }

    // 이전 구독을 해제하고 실제 상자와 로컬 인벤토리를 새로 연결합니다.
    public void LinkToPhysicalBox(int viewID)
    {
        Unlink();
        var view = PhotonView.Find(viewID);
        var source = view == null ? null : view.GetComponent<OpenableStorageBox>();
        if (source == null || RewardDeliveryService.FindBox(source.boxName) != source) return;
        linkedViewID = viewID;
        linkedPhotonView = view;
        linkedBox = source;
        SetBoxName(source.boxName);
        ifBoxOpen = true;
        source.StorageChanged += OnStorageChanged;
        BindInventory();
        OnStorageChanged(source);
    }

    // 표시용 복사본을 사용하여 UI가 실제 공유 상자 데이터를 수정하지 않게 합니다.
    public void UpdateBoxUIFromData(InventoryData data)
    {
        if (data?.id == null) return;
        inventoryData = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(data));
        UpdateBoxMenu();
    }

    // 기존 수납 방식을 유지하며 유효한 상자와 보유 수량에 대해서만 요청합니다.
    public void StorageItem(int index, int amount)
    {
        if (!HasValidBinding() || inventory == null || index < 0 || index >= inventory.NormalSlotCount || amount <= 0 ||
            inventory.GetItemID(index) < 0 || inventory.GetQuantity(index) <= 0 || linkedBox.CompareTag("Mailbox")) return;
        int trueAmount = Mathf.Min(amount, inventory.GetQuantity(index));
        linkedPhotonView.RPC(nameof(OpenableStorageBox.PunRPC_RequestStoreItem), RpcTarget.MasterClient,
            index, inventory.GetItemID(index), trueAmount, inventory.GetDurability(index));
        // 수납 승인/롤백 개편은 별도 범위이며 기존 로컬 차감 정책을 유지합니다.
        inventory.RemoveItem(index, trueAmount);
        UpdateInventoryMenu();
    }

    // 수납 확인창의 선택 수량을 기존 입고 경로로 전달합니다.
    public void StorageItem() { if (depositScreen != null) StorageItem(inventoryIndex, depositScreen.amount); }

    // 표시된 가용 수량만 요청하고 실제 인벤토리 반영은 서버 확인을 기다립니다.
    public void WithdrawItem(int index, int amount)
    {
        if (!HasValidBinding() || inventory == null || inventoryData?.id == null || index < 0 ||
            index >= inventoryData.id.Length || amount <= 0 || GetItemID(index) < 0 || GetQuantity(index) <= 0) return;
        var playerView = inventory.GetComponent<PhotonView>();
        if (playerView != null) linkedPhotonView.RPC(nameof(OpenableStorageBox.PunRPC_RequestWithdrawItem),
            RpcTarget.MasterClient, index, playerView.ViewID, Mathf.Min(amount, GetQuantity(index)));
    }

    // 출고 확인창의 선택 수량을 기존 출고 경로로 전달합니다.
    public void WithdrawItem() { if (withdrawScreen != null) WithdrawItem(boxIndex, withdrawScreen.amount); }

    // 진행 중 결제와 우편함 입금을 제외하고 기존 돈 수납 방식을 유지합니다.
    public void StorageMoney()
    {
        if (!HasValidBinding() || inventory == null || ShopPurchaseService.Instance?.IsBusy == true || linkedBox.CompareTag("Mailbox")) return;
        SetExchangeMoney();
        int amount = Mathf.Min(exchangeMoney, inventory.GetMoneyData());
        var playerView = inventory.GetComponent<PhotonView>();
        if (amount <= 0 || playerView == null) return;
        linkedPhotonView.RPC(nameof(OpenableStorageBox.PunRPC_RequestDepositMoney), RpcTarget.MasterClient, amount, playerView.ViewID);
        inventory.GetMoney(-amount);
        UpdateInventoryMenu();
        inputField.text = "0";
        exchangeMoney = 0;
    }

    // 입력 금액을 검증된 돈 출고 요청으로 변환합니다.
    public void WithdrawMoney()
    {
        SetExchangeMoney();
        WithdrawMoney(exchangeMoney);
        if (inputField != null) inputField.text = "0";
        exchangeMoney = 0;
    }

    // 인자로 호출해도 로컬 잔액을 미리 바꾸지 않고 동일한 수령 확인 경로를 사용합니다.
    public void WithdrawMoney(int amount)
    {
        if (!HasValidBinding() || inventory == null || amount <= 0) return;
        amount = Mathf.Min(amount, GetMoneyData());
        var playerView = inventory.GetComponent<PhotonView>();
        if (amount > 0 && playerView != null) linkedPhotonView.RPC(nameof(OpenableStorageBox.PunRPC_RequestWithdrawMoney),
            RpcTarget.MasterClient, amount, playerView.ViewID);
    }

    // 잘못된 입력에는 이전 입력 금액이 남지 않게 합니다.
    public void SetExchangeMoney() => exchangeMoney = inputField != null && int.TryParse(inputField.text, out int value) && value > 0 ? value : 0;

    // 표시용 이름과 실제 거래 대상 ID를 함께 설정합니다.
    public void SetBoxName(string name) { boxName = name; inventoryName = name; }

    // 상자 슬롯 선택 시 플레이어 슬롯 선택을 해제합니다.
    public void SetBoxIndex(int index)
    {
        if (boxIndex >= 0) boxUI.SetColors(boxIndex);
        boxIndex = index;
        boxUI.SetColors(index, 110, 123, 150);
        if (inventoryIndex >= 0) ItemUI.SetColors(inventoryIndex);
        inventoryIndex = -1;
    }

    // 플레이어 슬롯 선택 시 상자 슬롯 선택을 해제합니다.
    public void SetInventorytIndex(int index)
    {
        if (inventoryIndex >= 0) ItemUI.SetColors(inventoryIndex);
        inventoryIndex = index;
        ItemUI.SetColors(index, 110, 123, 150);
        if (boxIndex >= 0) boxUI.SetColors(boxIndex);
        boxIndex = -1;
    }

    // 확인창과 화면을 닫고 모든 변경 구독을 해제합니다.
    public void CloseBox()
    {
        CloseComfirmScreen();
        Unlink();
        FindAnyObjectByType<UIController>()?.SetBoxScreen(false);
    }

    // UI 슬롯만 초기화하며 이미 받은 상자 내용과 실제 금액을 보존합니다.
    public void SetBox()
    {
        if (inventoryData == null) { inventoryData = new InventoryData(); inventoryData.GenerateData(); }
        inventoryName = boxName;
        ItemUI?.SetSlotIDs();
        boxUI?.SetSlotIDs();
    }

    // 열려 있는 상자의 최신 공유 데이터를 읽으며 과거 저장 파일을 화면에 덮지 않습니다.
    public void LoadBox() { if (HasValidBinding()) linkedBox.RefreshSharedStorage(); }

    // 유효한 선택 슬롯에 대해서만 기존 수납/출고 확인창을 엽니다.
    public void SetComfirmScreen(bool ifDeposit)
    {
        if (!HasValidBinding()) return;
        if (ifDeposit)
        {
            if (inventory == null || inventoryIndex < 0 || inventoryIndex >= inventory.NormalSlotCount ||
                inventory.GetItemID(inventoryIndex) < 0 || inventory.GetQuantity(inventoryIndex) <= 0) return;
            depositScreen.gameObject.SetActive(true);
            depositScreen.ConstructComfirmScreen(inventory.GetItemID(inventoryIndex));
        }
        else
        {
            if (boxIndex < 0 || inventoryData?.id == null || boxIndex >= inventoryData.id.Length ||
                GetItemID(boxIndex) < 0 || GetQuantity(boxIndex) <= 0) return;
            withdrawScreen.gameObject.SetActive(true);
            withdrawScreen.ConstructComfirmScreen(GetItemID(boxIndex));
        }
    }

    // 수납과 출고 확인창을 모두 닫습니다.
    public void CloseComfirmScreen()
    {
        if (depositScreen != null) depositScreen.gameObject.SetActive(false);
        if (withdrawScreen != null) withdrawScreen.gameObject.SetActive(false);
    }
}
