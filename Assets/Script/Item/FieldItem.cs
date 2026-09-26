using Photon.Pun;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;


public class FieldItem : InteractableObject, ISavable
{
    public int itemID; //연관된 아이템 DB의 아이디
    public int amount; //개수
    public float durability = -1; //내구성, 또는 게이지형 장비 및 소모품 전용
    //private Inventory inventory;
    public bool ifPool = true;
    [SerializeField] private string pickupId;
    public string PickupId => pickupId;
    private bool pickupCompleted;
    private bool pickupDestroyRequested;

    // 저장 복원과 방장 변경에도 같은 필드 대상을 식별합니다.
    public static FieldItem FindPickup(string id) => string.IsNullOrEmpty(id) ? null :
        FindObjectsByType<FieldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(f => f.pickupId == id);

    // 방장이 생성한 필드 식별자만 받아 이전 방이나 다른 요청자가 바꾸지 못하게 합니다.
    [PunRPC]
    protected void RPC_SetPickupId(string id, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient || string.IsNullOrEmpty(id)) return;
        pickupId = id;
        if (RewardDeliveryService.Instance?.IsFieldPickupCompleted(id) == true) CompletePickup();
    }

    // 방장 변경 후 늦게 입장하는 플레이어도 새 방장이 보낸 최신 식별자를 받습니다.
    public void PublishPickupIdentity()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || !usePhoton || pv == null || pv.ViewID == 0 || pickupCompleted) return;
        if (string.IsNullOrEmpty(pickupId)) pickupId = Guid.NewGuid().ToString("N");
        PhotonNetwork.RemoveBufferedRPCs(pv.ViewID, nameof(RPC_SetPickupId));
        pv.RPC(nameof(RPC_SetPickupId), RpcTarget.AllBuffered, pickupId);
    }

    // 수령이 확정된 대상은 저장 대상에서 즉시 제외하고 방장이 네트워크에서 제거합니다.
    public void CompletePickup()
    {
        pickupCompleted = true;
        isInteractable = false;
        gameObject.SetActive(false);
        // 손님으로서 숨겨 둔 대상도 방장을 넘겨받으면 네트워크 제거를 마칩니다.
        if (pickupDestroyRequested) return;
        if (PhotonNetwork.InRoom && usePhoton && pv != null && pv.ViewID != 0)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            pickupDestroyRequested = true;
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            pickupDestroyRequested = true;
            Destroy(gameObject);
        }
    }

    public override InteractionType GetInteractionType() => InteractionType.Gauge; //사실 이 구조면 InteractionType이 필요없을거 같기도

    [Serializable]
    public struct FieldItemSaveStruct
    {
        public int itemID;
        public int amount;
        public float durability;
        public string pickupId;
    }

    // 네트워크 식별자를 준비하고 기존 상호작용 대기 시간을 적용합니다.
    public virtual void Start()
    {
        if (PhotonNetwork.InRoom && usePhoton && pv != null && pv.ViewID != 0) PublishPickupIdentity();
        else if (string.IsNullOrEmpty(pickupId)) pickupId = Guid.NewGuid().ToString("N");
        StartCoroutine(WaitforGetable());
        holdDuration = 1f;
        
    }

    // 실제 인벤토리 획득 결과에 따라 대상 수명을 결정합니다.
    public override void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {
        //Debug.Log("Item Detected");
        if (GetInteractionType() == InteractionType.Instant)
        {
            if (isInteractable && Input.GetMouseButtonDown(1))
            {
                //Debug.Log("아이템 습득 시도");
                GetItem();

            }

            if (isInteractable && Input.GetKey(KeyCode.E))
            {
                UpdateGuage(true, holdDuration);
            }
            else
            {
                UpdateGuage(false, holdDuration);
            }
        }
        else
        {
            if (isInteractable && Input.GetMouseButton(1))
            {
                UpdateGuage(true, holdDuration);
            }
            else
            {
                UpdateGuage(false, holdDuration);
            }
        }
        
    }

    // 실패한 획득은 다음 상호작용으로 다시 시도할 수 있게 유지합니다.
    public override void HoldInteract()
    {
        GetItem();
    }

    //현재로서는 Instant, Guage만 정의되어있음

    // 네트워크 필드는 방장에게 요청하고 오프라인 필드는 추가 성공 뒤에만 제거합니다.
    public virtual void GetItem()
    {
        if (!gameObject.activeSelf || !isInteractable) return;
        if (PhotonNetwork.InRoom && usePhoton && pv != null && pv.ViewID != 0) { RequestGetItem(); return; }
        inventory = Inventory.Local ?? inventory;
        if (inventory == null || !inventory.TryAddItem(itemID, amount, durability)) return;
        isInteractable = false;
        CompletePickup();
        var item = ItemDatabase.Instance.GetItem(itemID);
        if (item != null) QuestManager.Instance?.ReportObjectiveProgress(ObjectiveType.CollectItem, amount, item.stringID);
    }

    // 씬 UI의 공유 ViewID 대신 실제 요청자 캐릭터의 ViewID를 방장에게 전달합니다.
    public void RequestGetItem()
    {
        var local = Inventory.Local;
        if (!isInteractable || local == null || Player.localPlayer == null || pv == null || string.IsNullOrEmpty(pickupId)) return;
        var requester = Player.localPlayer.photonView;
        if (requester != null && requester.IsMine)
            pv.RPC(nameof(PunRPC_TryToPickup), RpcTarget.MasterClient, requester.ViewID);
    }

    // 생성 직후 즉시 다시 줍지 못하도록 기존 지연을 유지합니다.
    IEnumerator WaitforGetable()
    {
        yield return new WaitForSeconds(2f);
        isInteractable = true;
    }

    // 발신자 소유 캐릭터인지 검사하고 로컬 UI 인벤토리로 전달할 지급을 예약합니다.
    [PunRPC]
    protected void PunRPC_TryToPickup(int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !gameObject.activeSelf || string.IsNullOrEmpty(pickupId)) return;
        var requester = PhotonView.Find(requesterViewID);
        if (requester == null || requester.Owner != info.Sender || requester.GetComponent<Player>() == null) return;
        RewardDeliveryService.Instance?.RequestFieldPickup(this, QuestNetworkBridge.PlayerId(info.Sender));
    }

    // 네트워크 생성 정보에서 아이템 ID와 수량을 읽습니다.
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.Log($"[RECEIVER CHECK] OnPhotonInstantiate called for {this.gameObject.name}.");

        object[] data = info.photonView.InstantiationData;

        // 데이터가 아예 없는지 확인
        if (data == null)
        {
            //Debug.LogError("[RECEIVER CHECK] Instantiation data is NULL! No data was received.");
            return;
        }

        //Debug.Log($"[RECEIVER CHECK] Received data array with length: {data.Length}");

        // 데이터는 있는데 내용물이 부족한지 확인
        if (data.Length >= 2)
        {
            // 데이터가 정상일 경우, 어떤 값을 받았는지 확인
            int receivedID = (int)data[0];
            int receivedAmount = (int)data[1];
            //Debug.Log($"[RECEIVER CHECK] Data received. ID: {receivedID}, Amount: {receivedAmount}");

            this.itemID = receivedID;
            this.amount = receivedAmount;

            //Debug.Log($"[RECEIVER CHECK] Successfully set this item's ID to {this.itemID}");
        }
        else
        {
            Debug.LogError("[RECEIVER CHECK] Instantiation data was received, but it's too short!");
        }
    }

    // 생성·복원 호출이 사용하는 기존 아이템 속성 RPC를 유지합니다.
    [PunRPC]
    public void PunRPC_SetItemProperties(int id, int amt, float durability)
    {
        this.itemID = id;
        this.amount = amt;
        this.durability = durability;
        Debug.Log($"[PROPERTY SET] Item properties received via RPC. ID set to {this.itemID}, Amount to {this.amount}");
    }

    public string PrefabPath
    {
        get
        {
            string path = $"FieldItem/Object{itemID}";
            if (Resources.Load(path) == null)
            {
                Debug.Log("아이템이 존재하지 않습니다! 기본 아이템 경로로 설정합니다.");
                return "FieldItem/Object1";
            }
            return path;
        }
    }

    // 필드 고유 ID도 저장하여 진행 중 습득을 재접속 후 같은 대상으로 복원합니다.
    public string GetSaveDataJson()
    {
        FieldItemSaveStruct data = new FieldItemSaveStruct
        {
            itemID = this.itemID,
            amount = this.amount,
            durability = this.durability,
            pickupId = this.pickupId
        };
        return JsonUtility.ToJson(data);
    }

    // 저장된 식별자와 내용물을 복원하고 모든 참가자에게 같은 값을 알립니다.
    public void RestoreSaveData(string json)
    {
        FieldItemSaveStruct data = JsonUtility.FromJson<FieldItemSaveStruct>(json);
        pickupId = string.IsNullOrEmpty(data.pickupId) ? Guid.NewGuid().ToString("N") : data.pickupId;
        // 마스터 클라이언트가 복구하면서 다른 클라이언트에게도 동기화
        if (pv != null && PhotonNetwork.IsMasterClient)
        {
            PublishPickupIdentity();
            pv.RPC(nameof(PunRPC_SetItemProperties), RpcTarget.All, data.itemID, data.amount, data.durability);
        }
    }
}