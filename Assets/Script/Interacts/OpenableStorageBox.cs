using Photon.Pun;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class OpenableStorageBox : InteractableObject
{
    public string boxName = "storageBox";
    public bool interactable = true;
    public event Action<OpenableStorageBox> StorageChanged;
    protected InventoryData storageData;
    public bool IsStorageReady { get; private set; }
    private Coroutine initialization;

    // 실제 상자의 네트워크 식별자를 준비합니다.
    protected override void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (pv == null) Debug.LogError("OpenableStorageBox에 PhotonView가 없습니다!");
    }

    // 활성화된 상자만 공유 상태 복원에 참여합니다.
    protected virtual void OnEnable() => initialization = StartCoroutine(InitializeWhenReady());

    // 비활성 상자를 거래에서 제외하고 연결된 화면에도 사용 종료를 알립니다.
    protected virtual void OnDisable()
    {
        if (initialization != null) StopCoroutine(initialization);
        initialization = null;
        IsStorageReady = false;
        StorageChanged?.Invoke(this);
    }

    // 준비 순서와 관계없이 공유 데이터를 우선 적용하고 최초 한 번만 저장 데이터를 가져옵니다.
    private IEnumerator InitializeWhenReady()
    {
        while (!PhotonNetwork.InRoom || SaveManager.Instance?.IsDataReady != true ||
            RewardDeliveryService.Instance?.InitializeBox(this) != true) yield return null;
        initialization = null;
        RewardDeliveryService.Instance.RequestProcessing();
    }

    // 상호작용 입력에 따라 기존 열기 게이지를 유지합니다.
    public override void Interact() => UpdateGuage(interactable && Input.GetMouseButton(1), holdDuration);

    // 유일하고 준비된 상자를 UI에 연결하며 조회 때문에 공유 데이터를 재발행하지 않습니다.
    public void OpenBox()
    {
        if (!RefreshSharedStorage()) return;
        var controller = FindAnyObjectByType<UIController>();
        if (controller == null) return;
        controller.SetBoxScreen(true);
        var box = FindAnyObjectByType<StorageBox>();
        if (box != null) box.LinkToPhysicalBox(pv.ViewID);
    }

    // 길게 누르기가 끝나면 창고 화면을 엽니다.
    public override void HoldInteract() => OpenBox();

    // 기존 입고 RPC 서명을 유지하고 최신 공유 데이터에 전량 입고를 시도합니다.
    [PunRPC]
    public void PunRPC_RequestStoreItem(int inventorySlot, int itemID, int quantity, float durability, PhotonMessageInfo info)
    {
        if (PhotonNetwork.IsMasterClient) TryDepositItem(itemID, quantity, durability);
    }

    // 공유 원본으로 입고를 계산하고 기준 버전이 유지된 경우에만 확정합니다.
    public bool TryDepositItem(int itemID, int quantity, float durability)
    {
        var service = RewardDeliveryService.Instance;
        if (!PhotonNetwork.IsMasterClient || service == null || !service.TryGetBoxSnapshot(this, out var data, out int revision)) return false;
        var delivery = new RewardDelivery { deliveryId = "deposit/" + Guid.NewGuid().ToString("N"),
            rewardType = RewardType.Item, itemId = itemID, amount = quantity, durability = durability };
        if (!RewardInventory.TryCredit(data, data.id.Length, delivery, out var updated)) return false;
        updated.receivedDeliveryIds.Remove(delivery.deliveryId);
        return service.PublishBox(this, revision, updated);
    }

    // 공유 잔액을 기준으로 오버플로 없이 입금을 확정합니다.
    public bool TryDepositMoney(int amount)
    {
        var service = RewardDeliveryService.Instance;
        if (!PhotonNetwork.IsMasterClient || service == null || amount <= 0 ||
            !service.TryGetBoxSnapshot(this, out var data, out int revision) || data.money < 0 ||
            (long)data.money + amount > int.MaxValue) return false;
        data.money += amount;
        return service.PublishBox(this, revision, data);
    }

    // 네트워크 발신자에게만 출고를 예약하고 수령 성공 전에는 원본을 차감하지 않습니다.
    [PunRPC]
    public void PunRPC_RequestWithdrawItem(int boxSlot, int requesterViewID, int amount, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender == null) return;
        RewardDeliveryService.Instance?.RequestWithdrawal(this, QuestNetworkBridge.PlayerId(info.Sender), boxSlot, amount, false);
    }

    // 구형 RPC는 공유 데이터가 없는 초기화 중에만 허용하여 최신 내용을 덮지 못하게 합니다.
    [PunRPC]
    public void PunRPC_SyncBoxData(string jsonData)
    {
        if (QuestManager.Instance?.IsInitialized == true &&
            QuestManager.Instance.DeliveryState.boxes.Any(b => b.boxId == boxName)) return;
        ApplySharedStorage(JsonUtility.FromJson<InventoryData>(jsonData));
    }

    // 공유 상태를 실제 상자와 저장 캐시에 복사하고 구독 중인 화면에 변경을 알립니다.
    public void ApplySharedStorage(InventoryData data)
    {
        if (data?.id == null || data.quantity?.Length != data.id.Length || data.durability?.Length != data.id.Length ||
            RewardDeliveryService.FindBox(boxName, false) != this) return;
        storageData = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(data));
        IsStorageReady = true;
        SaveManager.Instance?.UpdateBoxCache(boxName, storageData);
        StorageChanged?.Invoke(this);
    }

    // 최신 공유 내용을 읽어 화면만 갱신하며 공유 버전과 내용은 변경하지 않습니다.
    public bool RefreshSharedStorage()
    {
        var service = RewardDeliveryService.Instance;
        if (service == null || !service.TryGetBoxSnapshot(this, out var data, out _)) return false;
        ApplySharedStorage(data);
        return true;
    }

    // 실제 상자의 복사본을 반환하여 호출자가 원본을 직접 바꾸지 못하게 합니다.
    public InventoryData CaptureStorageData() => storageData == null ? null :
        JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(storageData));

    // UI에는 공유 원본에서 출고 예약 수량을 제외한 내용만 전달합니다.
    public InventoryData CaptureVisibleStorageData()
    {
        var service = RewardDeliveryService.Instance;
        return service != null && service.TryGetBoxSnapshot(this, out var data, out _)
            ? service.GetVisibleBoxData(boxName, data) : null;
    }

    // 기존 금액 입고 RPC를 최신 공유 잔액 변경에 연결합니다.
    [PunRPC]
    public void PunRPC_RequestDepositMoney(int amount, int requesterViewID, PhotonMessageInfo info) => TryDepositMoney(amount);

    // 돈도 아이템과 같은 수령 확인 및 원본 차감 경로로 출고합니다.
    [PunRPC]
    public void PunRPC_RequestWithdrawMoney(int amount, int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender == null) return;
        RewardDeliveryService.Instance?.RequestWithdrawal(this, QuestNetworkBridge.PlayerId(info.Sender), -1, amount, true);
    }

    // 열기/조회 요청은 공유 상태를 읽기만 하며 과거 스냅샷을 발행하지 않습니다.
    [PunRPC]
    public void PunRPC_RequestLatestData() => RefreshSharedStorage();

    // 유효한 슬롯의 중첩 불가 여부를 조회합니다.
    public bool getSingularity(int index) => storageData?.id != null && index >= 0 && index < storageData.id.Length &&
        ItemDatabase.Instance.getSingularity(storageData.id[index]);

    // 준비된 실제 상자를 대상으로만 기존 아이템 입고 RPC를 보냅니다.
    public void RequestInsertItemOnRPC(int itemId, int amount, float duration)
    {
        if (RewardDeliveryService.FindBox(boxName) != this) return;
        pv.RPC(nameof(PunRPC_RequestStoreItem), RpcTarget.MasterClient, 0, itemId, amount, duration);
    }

    // 준비된 실제 상자를 대상으로만 기존 돈 입고 RPC를 보냅니다.
    public void RequestInsertMoneyOnRPC(int amount)
    {
        if (RewardDeliveryService.FindBox(boxName) != this) return;
        pv.RPC(nameof(PunRPC_RequestDepositMoney), RpcTarget.MasterClient, amount, pv.ViewID);
    }
}
