using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;


public class FieldItem : InteractableObject, ISavable
{
    public int itemID; //연관된 아이템 DB의 아이디
    public int amount; //개수
    public float durability = -1; //내구성, 또는 게이지형 장비 및 소모품 전용
    //private Inventory inventory;
    public bool ifPool = true;

    public override InteractionType GetInteractionType() => InteractionType.Gauge; //사실 이 구조면 InteractionType이 필요없을거 같기도

    [Serializable]
    public struct FieldItemSaveStruct
    {
        public int itemID;
        public int amount;
        public float durability;
    }

    public virtual void Start()
    {
        StartCoroutine(WaitforGetable());
        holdDuration = 1f;
        
    }

    public override void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {
        //Debug.Log("Item Detected");
        if (GetInteractionType() == InteractionType.Instant)
        {
            if (isInteractable && Input.GetMouseButtonDown(1))
            {
                //Debug.Log("아이템 습득 시도");
                GetItem();
                RPC_Deactivate();
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

    public override void HoldInteract()
    {
        // 게이지가 다 차면 아이템 습득을 요청
        RequestGetItem();
        isInteractable = false;

    }

    //현재로서는 Instant, Guage만 정의되어있음

    public virtual void GetItem()
    {
        inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Inventory를 찾을 수 없습니다.");
            return;
        }
        //if (!inventory.HoldingInteractableItem()) return; //아이템을 주울 수 있는 상태인지 판단 기준1 : 손에 든 채로 또 아이템을 주울 수 있는 아이템을 들고 있는지 리턴
        //소모형 아이템이 1개 남아서 사용하고 아이템이 비워지자마자 아이템이 주워지는 현상 발생. inventory의 아이템 사용 스크립트가 먼저 처리되기 때문
        //해결 방안1 : 아이템이 소모되어 삭제되는 시점을 코루틴 등으로 미루기
        if (!inventory.CheckInventoryEmpty()) return;
        inventory.GetItem(itemID, amount);
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void RequestGetItem()
    {
        if (!isInteractable) return;

        // 자신의 인벤토리를 찾아 PhotonView ID를 호스트에게 보내 습득 요청
        Inventory localInventory = FindAnyObjectByType<Inventory>();
        if (localInventory == null)
        {
            Debug.LogError("로컬 인벤토리를 찾을 수 없습니다.");
            return;
        }

        PhotonView playerPhotonView = localInventory.GetComponent<PhotonView>(); //pv로 대체 가능한지 테스트 후 변경 예정
        if (playerPhotonView != null)
        {
            // 모든 클라이언트가 이 RPC를 호출하지만, 호스트가 처리하도록
            pv.RPC("PunRPC_TryToPickup", RpcTarget.MasterClient, playerPhotonView.ViewID);
        }
        else
        {
            Debug.LogError("플레이어의 PhotonView를 찾을 수 없습니다. Inventory 컴포넌트와 같은 오브젝트에 PhotonView가 있는지 확인하세요.");
        }
    }


    IEnumerator WaitforGetable()
    {
        yield return new WaitForSeconds(2f);
        isInteractable = true;
    }

    [PunRPC]
    protected void PunRPC_TryToPickup(int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 아이템이 이미 다른 유저에 의해 파괴되었을 수 있으므로 확인
        if (gameObject == null) return;

        Debug.Log($"'{info.Sender.NickName}' (ViewID: {requesterViewID}) 플레이어가 아이템 습득을 요청합니다.");

        // 요청자의 PhotonView를 찾기기
        PhotonView requesterView = PhotonView.Find(requesterViewID);
        if (requesterView != null)
        {
            // 요청자에게만 "PunRPC_AddItem" RPC를 보내 아이템을 인벤토리에 추가하도록 합니다.
            // (이전에 창고 기능 구현 시 Inventory.cs에 만들어 둔 RPC를 재사용합니다.)
            requesterView.RPC("PunRPC_AddItem", info.Sender, this.itemID, this.amount, this.durability);

            // 아이템 지급에 성공했으므로, 이 필드 아이템을 네트워크에서 파괴합니다.
            // PhotonNetwork.Destroy()는 모든 클라이언트에서 이 오브젝트를 파괴합니다.
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

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

    public string GetSaveDataJson()
    {
        FieldItemSaveStruct data = new FieldItemSaveStruct
        {
            itemID = this.itemID,
            amount = this.amount,
            durability = this.durability
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreSaveData(string json)
    {
        FieldItemSaveStruct data = JsonUtility.FromJson<FieldItemSaveStruct>(json);
        // 마스터 클라이언트가 복구하면서 다른 클라이언트에게도 동기화
        if (pv != null && PhotonNetwork.IsMasterClient)
        {
            pv.RPC(nameof(PunRPC_SetItemProperties), RpcTarget.All, data.itemID, data.amount, data.durability);
        }
    }
}