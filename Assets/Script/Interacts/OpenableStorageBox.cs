using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
public class OpenableStorageBox : InteractableObject
{
    public string boxName = "storageBox";
    public bool interactable = true;
    StorageBox box;

    // 이 박스의 데이터를 저장하기 위한 변수 -> StorageBox의 인벤토리 데이터는 임시용으로만 사용하도록 수정
    protected InventoryData storageData;

    protected override void Awake()
    {
        //base.Awake();
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("OpenableStorageBox에 PhotonView가 없습니다!");
        }
    }

    private IEnumerator Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitUntil(() => SaveManager.Instance != null && SaveManager.Instance.IsDataReady);

            // 1. SaveManager에 내 이름(boxName)으로 된 저장 데이터가 있는지 확인
            InventoryData savedData = SaveManager.Instance.GetBoxData(boxName);

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
                SaveManager.Instance.UpdateBoxCache(boxName, storageData);
                Debug.Log($"[{boxName}] 새 창고 데이터를 생성했습니다.");
            }
        }
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
            box.UpdateBoxUIFromData(storageData);
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

    [PunRPC]
    public void PunRPC_RequestStoreItem(int inventorySlot, int itemID, int quantity, float durability, PhotonMessageInfo info)
    {
        // 마스터 클라이언트가 아니면 이 요청을 무시합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        // 실제 아이템 보관 로직 (마스터 클라이언트에서만 실행)
        Debug.Log($"'{info.Sender.NickName}'로부터 아이템 보관 요청 받음: ID {itemID}, 수량 {quantity}");

        // 여기에 유효성 검사 추가 가능 (예: 해당 플레이어가 정말 그 아이템을 가지고 있었는지)

        // 이미 같은 아이템이 있는지 확인
        for (int i = 0; i < storageData.id.Length; i++)
        {
            if (storageData.id[i] == itemID)
            {
                if (getSingularity(i)) break;
                storageData.quantity[i] += quantity;
                SyncDataToAll();
                return;
            }
        }

        // 빈 슬롯 찾아서 추가
        for (int i = 0; i < storageData.id.Length; i++)
        {
            if (storageData.id[i] == -1)
            {
                storageData.id[i] = itemID;
                storageData.quantity[i] = quantity;
                storageData.durability[i] = durability;
                SyncDataToAll();
                return;
            }
        }

        //Debug.LogWarning("창고가 가득 찼습니다.");
    }

    [PunRPC]
    public void PunRPC_RequestWithdrawItem(int boxSlot, int requesterViewID, int amount, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int itemID = storageData.id[boxSlot];
        int quantity = storageData.quantity[boxSlot];
        float durability = storageData.durability[boxSlot];

        if (itemID == -1 || quantity < amount) return;

        // 아이템을 꺼낼 수 있는지 유효성 검사
        // 아직 미구현 상태 (InventoryFrame에 일부 구현이 되어 있긴 한데 보강 필요)

        // 1. 요청한 플레이어의 PhotonView를 찾습니다.
        PhotonView requesterPhotonView = PhotonView.Find(requesterViewID);
        if (requesterPhotonView != null)
        {
            // 2. 해당 플레이어에게만 아이템을 주도록 RPC를 보냅니다.
            requesterPhotonView.RPC("PunRPC_AddItem", info.Sender, itemID, amount, durability);

            // 3. 창고에서 아이템을 제거합니다.
            //storageData.id[boxSlot] = -1;
            //storageData.quantity[boxSlot] = 0;
            storageData.RemoveItem(boxSlot, amount);

            // 4. 변경된 창고 데이터를 모든 클라이언트에게 동기화합니다.
            SyncDataToAll();
        }
        else
        {
            Debug.LogError($"ID {requesterViewID}를 가진 요청자를 찾을 수 없습니다.");

        }
    }


    // 데이터를 모든 클라이언트에게 동기화하는 메서드
    protected void SyncDataToAll()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        //storageData.SaveInventory(boxName);
        //Debug.Log($"SyncDataToAll - 현재 잔액 : {storageData.money}");
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateBoxCache(boxName, storageData);
        }

        string jsonData = JsonUtility.ToJson(storageData);
        pv.RPC(nameof(PunRPC_SyncBoxData), RpcTarget.AllBuffered, jsonData);
        Debug.Log("모든 클라이언트에게 창고 데이터 동기화 전송");

    }

    [PunRPC]
    public void PunRPC_SyncBoxData(string jsonData)
    {
        //Debug.Log($"PUNRPC_SyncBoxData - 현재 잔액 : {storageData.money}");
        InventoryData data = JsonUtility.FromJson<InventoryData>(jsonData);
        //data.InitializeItemDatabase(); // ItemDatabase 초기화
        this.storageData = data; // 로컬 데이터 업데이트
        //Debug.Log($"PUNRPC_SyncBoxData2 - 현재 잔액 : {storageData.money}");
        // 만약 이 박스의 UI가 현재 열려있다면, UI를 즉시 업데이트
        if (box != null && box.gameObject.activeInHierarchy && box.linkedViewID == pv.ViewID)
        {
            box.UpdateBoxUIFromData(storageData);
            box.UpdateInventoryMenu();
        }
        Debug.Log("창고 데이터 동기화 받음.");
        //Debug.Log($"PUNRPC_SyncBoxData3 - 현재 잔액 : {storageData.money}");
    }

    [PunRPC]
    public void PunRPC_RequestDepositMoney(int amount, int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 유효성 검사: 요청한 플레이어의 돈을 실제로 검증하는 로직이 필요하지만,
        // 일단 클라이언트를 신뢰하고 진행합니다.

        //창고 데이터 돈 추가 로직
        storageData.money += amount;
        Debug.Log($"'{info.Sender.NickName}'로부터 {amount}원 입금 요청. 현재 창고 잔액: {storageData.money}");
        //storageData.SaveInventory();
        //변경된 창고 데이터를 모든 클라이언트에게 동기화
        SyncDataToAll();
        //Debug.Log($"싱크 완료, 현재 창고 잔액: {storageData.money}");
    }

    [PunRPC]
    public void PunRPC_RequestWithdrawMoney(int amount, int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (storageData.money >= amount)
        {
            storageData.money -= amount;
            Debug.Log($"'{info.Sender.NickName}'에게 {amount}원 출금. 현재 창고 잔액: {storageData.money}");

            // 1. 요청한 플레이어(UI)의 PhotonView를 찾습니다.
            PhotonView requesterPhotonView = PhotonView.Find(requesterViewID);

            if (requesterPhotonView != null)
            {
                // [핵심 수정] 방장이 계산하지 않고, 클라이언트에게 "amount만큼 추가해라!" 라고 명령만 보냅니다.
                requesterPhotonView.RPC("PunRPC_AddMoney", info.Sender, amount);
            }

            // 변경된 창고 데이터를 모든 클라이언트에게 동기화.
            SyncDataToAll();
        }
        else
        {
            Debug.LogWarning($"'{info.Sender.NickName}'의 출금 요청 실패. 잔액 부족.");
        }
    }

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