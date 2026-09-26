using System;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class ShopPurchaseService : MonoBehaviour
{
    public static ShopPurchaseService Instance { get; private set; }
    public static event Action<string> OnNotice;
    public bool IsBusy => !string.IsNullOrEmpty(pendingId);
    private string pendingId;
    private float nextPollTime;
    private QuestManager manager;
    private QuestNetworkBridge bridge;
    private ShopCatalog catalog;

    // 결과 안내를 기존 Canvas 메시지와 연결합니다.
    public static void ShowNotice(string message) => OnNotice?.Invoke(message);

    // 기존 세션 관리자에 붙어 UI가 닫혀도 구매 확인과 재시도를 유지합니다.
    private void Awake()
    {
        Instance = this;
        manager = GetComponent<QuestManager>();
        bridge = GetComponent<QuestNetworkBridge>();
        catalog = Resources.Load<ShopCatalog>("Data/ShopCatalog");
    }

    // 세션이 끝나면 로컬 결제 잠금과 정적 참조를 정리합니다.
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // 서버에 확정된 거래만 로컬 잔액에 반영하고 결과를 화면에 알립니다.
    private void Update()
    {
        if (!PhotonNetwork.InRoom) { pendingId = null; return; }
        if (manager == null || !manager.IsInitialized || Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + 0.2f;
        var state = bridge.ReadRoomState(SaveManager.Instance.GetCurrentSave()?.saveId);
        if (state?.rewards?.purchases == null) return;
        var inventory = Inventory.Local;
        if (inventory != null) inventory.ApplyConfirmedPurchases(state.rewards.purchases);
        var record = state.rewards.purchases.FirstOrDefault(p => p.requestId == pendingId && p.buyerId == QuestNetworkBridge.LocalPlayerId);
        if (record == null) return;
        pendingId = null;
        OnNotice?.Invoke(record.message);
    }

    // 구매 시점의 본인 잔액을 고정하고 같은 요청 ID로 확인될 때까지 기다립니다.
    public bool RequestPurchase(int itemId, int amount)
    {
        var inventory = Inventory.Local;
        if (IsBusy || inventory == null || !inventory.IsInventoryReady || !PhotonNetwork.InRoom || !manager.IsInitialized || !manager.HasSharedState)
        { OnNotice?.Invoke("상점이 준비 중이거나 구매를 처리하고 있습니다."); return false; }
        var entry = catalog?.Find(itemId);
        int cost = ShopTransactions.BuyPrice(entry?.item, amount);
        if (cost < 0 || cost > inventory.GetMoneyData()) { OnNotice?.Invoke("상품, 수량 또는 보유 금액을 확인해 주세요."); return false; }
        pendingId = Guid.NewGuid().ToString("N");
        var player = Player.localPlayer.CaptureQuestPlayerState(inventory);
        bridge.RequestShopPurchase(pendingId, itemId, amount, player);
        return true;
    }

    // 방장이 가격과 전량 입고를 확인하고 돈·우편함·거래 기록을 같은 공유 버전으로 확정합니다.
    public void AcceptPurchase(string requestId, int itemId, int amount, PlayerData buyer)
    {
        if (!manager.CanWriteMain || !manager.HasSharedState || bridge.IsRecoveringPlayerStates ||
            buyer?.items == null || string.IsNullOrEmpty(requestId)) return;
        var state = manager.DeliveryState;
        state.purchases ??= new();
        if (state.purchases.Any(p => p.requestId == requestId)) return;
        var record = new ShopPurchaseRecord { requestId = requestId, buyerId = buyer.playerId, itemId = itemId, amount = amount };
        var mailbox = FindObjectsByType<OpenableStorageBox>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.CompareTag("Mailbox") && b.IsStorageReady);
        if (mailbox == null) record.message = "우편함이 준비되지 않아 구매하지 못했습니다.";
        else
        {
            ShopTransactions.ApplyPurchases(buyer.items, buyer.playerId, state.purchases);
            record.success = ShopTransactions.TryPurchase(buyer.items, mailbox.CaptureStorageData(), catalog?.Find(itemId),
                amount, requestId, out var paid, out var stocked, out record.message);
            if (record.success)
            {
                record.cost = ShopTransactions.BuyPrice(catalog.Find(itemId).item, amount);
                buyer.items = paid;
                var box = state.boxes.FirstOrDefault(b => b.boxId == mailbox.boxName);
                if (box == null) { box = new BoxSaveData { boxId = mailbox.boxName }; state.boxes.Add(box); }
                box.items = stocked;
                state.recipients.RemoveAll(p => p.playerId == buyer.playerId);
                state.recipients.Add(JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(buyer)));
            }
        }
        state.purchases.Add(record);
        manager.CommitDeliveryChanges();
    }
}
