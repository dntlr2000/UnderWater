using Photon.Pun;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
public class OpenableStorageBox : InteractableObject
{
    public string boxName = "storageBox";
    public bool interactable = true;
    StorageBox box;

    // 이 박스의 데이터를 저장하기 위한 변수 -> StorageBox의 인벤토리 데이터는 임시용으로만 사용하도록 수정
    protected InventoryData storageData;
    public bool IsStorageReady { get; private set; }

    protected override void Awake()
    {
        //base.Awake();
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("OpenableStorageBox에 PhotonView가 없습니다!");
        }
    }

    // 저장과 공유 상태를 먼저 복원하여 준비 전 보상이 초기화로 사라지지 않게 합니다.
    private IEnumerator Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            float timeout = 2f;
            // SaveManager가 아직 초기화되지 않았거나 데이터가 준비되지 않았으면 최대 2초간 대기합니다.
            while ((SaveManager.Instance == null || !SaveManager.Instance.IsDataReady) && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // SaveManager가 여전히 준비되지 않았다면, 임시로 현재 방 이름을 기반으로 한 빈 저장 데이터를 생성합니다.
            if (SaveManager.Instance != null && !SaveManager.Instance.IsDataReady)
            {
                string roomName = PhotonNetwork.CurrentRoom?.Name;
                if (string.IsNullOrEmpty(roomName)) roomName = "OfflineRoom";
                SaveManager.Instance.SetCurrentSave(new SaveData(roomName), false);
            }

            // 1. SaveManager에 내 이름(boxName)으로 된 저장 데이터가 있는지 확인
            InventoryData savedData = SaveManager.Instance != null ? SaveManager.Instance.GetBoxData(boxName) : null;

            if (savedData != null)
            {
                // 불러온 데이터가 있으면 적용
                storageData = savedData;
                Debug.Log($"[{boxName}] 저장된 창고 데이터를 성공적으로 불러왔습니다.");
            }
            else
            {
                // 없으면 새 게임이므로 새로 생성 후 SaveManager에 등록
                storageData = new InventoryData();
                storageData.GenerateData();
                SaveManager.Instance?.UpdateBoxCache(boxName, storageData);
                Debug.Log($"[{boxName}] 새 창고 데이터를 생성했습니다.");
            }
        }

        if (storageData == null)
        {
            // 오프라인 테스트/초기화 순서 이슈에서도 storageData가 null로 남지 않도록 최종 안전망을 둡니다.
            storageData = new InventoryData();
            storageData.GenerateData();
        }
        IsStorageReady = true;
        if (PhotonNetwork.IsMasterClient) SyncDataToAll();
    }

    public override void Interact()
    {
        if (interactable && Input.GetMouseButton(1))
        {
            UpdateGuage(true, holdDuration);
        }
        else
        {
            UpdateGuage(false, holdDuration);
        }
    }

    // 공용 우편함과 창고를 연결하고 현재 출고 가능한 수량을 표시합니다.
    public void OpenBox()
    {
        UIController uIController = FindAnyObjectByType<UIController>();
        if (uIController == null)
        {
            Debug.LogError("UI에서 UI 컨트롤러를 찾을 수 없습니다.");
            return;
        }
        uIController.SetBoxScreen(true);

        box = FindAnyObjectByType<StorageBox>();
        if (box == null)
        {
            Debug.LogError("UI에서 박스 UI 스크립트를 찾을 수 없습니다.");
            return;
        }

        box.gameObject.SetActive(true);
        box.SetBoxName(boxName);


        // 박스를 열 때, 이 박스의 PhotonView ID를 StorageBox UI 스크립트에 넘겨줍니다.
        // 이를 통해 UI는 어떤 박스에 대한 요청을 보내야 하는지 알 수 있습니다.
        box.LinkToPhysicalBox(pv.ViewID);

        // 마스터 클라이언트에게 최신 데이터를 요청하거나, 
        // 이미 데이터가 있다면 바로 UI를 업데이트합니다.
        if (storageData != null)
        {
            box.UpdateBoxUIFromData(VisibleStorageData());
        }
        else
        {
            // 내가 마스터가 아니라면, 마스터에게 최신 데이터를 요청할 수 있습니다.
            // 하지만 보통은 Buffered RPC로 데이터가 이미 와있을 것입니다.
        }
        box.UpdateInventoryMenu();
        pv.RPC(nameof(PunRPC_RequestLatestData), RpcTarget.MasterClient);
    }

    public override void HoldInteract()
    {
        OpenBox();
    }

    // 기존 상점/창고 입고 RPC 서명을 유지하면서 전량 추가에 성공한 경우만 반영합니다.
    [PunRPC]
    public void PunRPC_RequestStoreItem(int inventorySlot, int itemID, int quantity, float durability, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !TryDepositItem(itemID, quantity, durability)) return;
        SyncDataToAll();
    }

    // UI와 무관하게 창고에 아이템 전량을 추가하며 실패하면 원본을 보존합니다.
    public bool TryDepositItem(int itemID, int quantity, float durability)
    {
        if (!PhotonNetwork.IsMasterClient || !IsStorageReady) return false;
        var delivery = new RewardDelivery { deliveryId = "deposit/" + Guid.NewGuid().ToString("N"),
            rewardType = RewardType.Item, itemId = itemID, amount = quantity, durability = durability };
        if (!RewardInventory.TryCredit(storageData, storageData.id.Length, delivery, out var updated)) return false;
        // 일반 입고는 기존 호출자의 처리이므로 보상 수령 영수증을 추가하지 않습니다.
        updated.receivedDeliveryIds.Remove(delivery.deliveryId);
        storageData = updated;
        return true;
    }

    // 금액 범위를 검사한 뒤 창고 잔액에 더합니다.
    public bool TryDepositMoney(int amount)
    {
        if (!PhotonNetwork.IsMasterClient || !IsStorageReady || amount <= 0 ||
            storageData.money < 0 || (long)storageData.money + amount > int.MaxValue) return false;
        storageData.money += amount;
        return true;
    }

    // 요청자 본인에게만 출고를 예약하며 실제 차감은 수령 성공 응답 이후 수행합니다.
    [PunRPC]
    public void PunRPC_RequestWithdrawItem(int boxSlot, int requesterViewID, int amount, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender == null) return;
        // 로컬 UI의 ViewID 대신 검증된 네트워크 발신자로 수령자를 결정합니다.
        RewardDeliveryService.Instance?.RequestWithdrawal(this, QuestNetworkBridge.PlayerId(info.Sender), boxSlot, amount, false);
    }

    // 보상/출고가 사용하는 공유 상태와 동일한 버전으로 창고 변경을 발행합니다.
    protected void SyncDataToAll()
    {
        if (!PhotonNetwork.IsMasterClient || storageData == null) return;
        SaveManager.Instance?.UpdateBoxCache(boxName, storageData);
        if (RewardDeliveryService.Instance?.PublishBox(boxName, storageData) == true) return;
        if (pv != null && pv.ViewID != 0)
            pv.RPC(nameof(PunRPC_SyncBoxData), RpcTarget.AllBuffered, JsonUtility.ToJson(storageData));
    }

    // 초기화 중 기존 RPC는 허용하되 확정된 공유 버전보다 오래된 내용을 덮지 않습니다.
    [PunRPC]
    public void PunRPC_SyncBoxData(string jsonData)
    {
        if (QuestManager.Instance?.IsInitialized == true &&
            QuestManager.Instance.DeliveryState.boxes.Any(b => b.boxId == boxName)) return;
        ApplySharedStorage(JsonUtility.FromJson<InventoryData>(jsonData));
    }

    // 확정된 공유 상태를 적용하고 열려 있는 창고 화면을 즉시 갱신합니다.
    public void ApplySharedStorage(InventoryData data)
    {
        if (data?.id == null) return;
        storageData = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(data));
        IsStorageReady = true;
        SaveManager.Instance?.UpdateBoxCache(boxName, storageData);
        if (box != null && box.gameObject.activeInHierarchy && box.linkedViewID == pv.ViewID)
        {
            box.UpdateBoxUIFromData(VisibleStorageData());
            box.UpdateInventoryMenu();
        }
    }

    // 서비스의 지급 계산이 원본 상자 데이터를 직접 변경하지 않도록 복사합니다.
    public InventoryData CaptureStorageData() => storageData == null ? null :
        JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(storageData));

    // 출고 대기 중인 수량을 제외한 표시용 데이터만 UI에 전달합니다.
    private InventoryData VisibleStorageData() => RewardDeliveryService.Instance?.GetVisibleBoxData(boxName, storageData) ?? storageData;

    // 기존 상점의 금액 입고 경로를 유지하며 유효한 금액만 추가합니다.


    [PunRPC]
    public void PunRPC_RequestDepositMoney(int amount, int requesterViewID, PhotonMessageInfo info)
    {
        if (!TryDepositMoney(amount)) return;
        SyncDataToAll();
    }

    // 돈도 아이템과 동일하게 수령 확인 전까지 예약 상태로 남깁니다.

    [PunRPC]
    public void PunRPC_RequestWithdrawMoney(int amount, int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender == null) return;
        RewardDeliveryService.Instance?.RequestWithdrawal(this, QuestNetworkBridge.PlayerId(info.Sender), -1, amount, true);
    }

    // 창고를 열거나 다시 조회하면 방장의 현재 내용을 공유합니다.

    [PunRPC]
    public void PunRPC_RequestLatestData()
    {
        //UI 동기화
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("클라이언트로부터 최신 데이터 요청을 받아 동기화를 시작합니다.");
        SyncDataToAll();
    }

    public bool getSingularity(int index)
    {
        return ItemDatabase.Instance.getSingularity(storageData.id[index]);
    }

    public void RequestInsertItemOnRPC(int itemId, int amount, float duration)
    {
        Debug.Log($"[OpenableStorageBox] 아이템 보관 요청: ID {itemId}, 수량 {amount}, 내 ViewID {pv.ViewID}");
        pv.RPC(nameof(PunRPC_RequestStoreItem), RpcTarget.MasterClient, 0, itemId, amount, duration);
    }

    public void RequestInsertMoneyOnRPC(int amount)
    {
        Debug.Log($"[OpenableStorageBox] 돈 입금 요청: {amount}원, 내 ViewID {pv.ViewID}");
        pv.RPC(nameof(PunRPC_RequestDepositMoney), RpcTarget.MasterClient, amount, pv.ViewID);
    }
}
