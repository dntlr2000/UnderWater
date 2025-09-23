using Photon.Pun;
using System.Collections;
using UnityEngine;


public class FieldItem : InteractableObject//, Interactable
{
    public bool getAble = true;
    public int itemID; //연관된 아이템 DB의 아이디
    public int amount; //개수
    //public int durability = -1;
    //private Inventory inventory;
    public bool ifPool = true;

    public override InteractionType GetInteractionType() => InteractionType.Gauge; //사실 이 구조면 InteractionType이 필요없을거 같기도
    
    public void Start()
    {
        StartCoroutine(WaitforGetable());
        holdDuration = 1f;
        
    }

    public override void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {
        //Debug.Log("Item Detected");
        if (GetInteractionType() == InteractionType.Instant)
        {
            if (getAble && Input.GetMouseButtonDown(1))
            {
                //Debug.Log("아이템 습득 시도");
                GetItem();
                RPC_Deactivate();
            }

            if (getAble && Input.GetKey(KeyCode.E))
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
            if (getAble && Input.GetMouseButton(1))
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
        // 게이지가 다 차면 아이템 습득을 '요청'합니다.
        RequestGetItem();
        getAble = false;

    }

    //현재로서는 Instant, Guage만 정의되어있음

    public void GetItem()
    {
        inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("Inventory를 찾을 수 없습니다.");
            return;
        }
        if (!inventory.HoldingInteractableItem()) return; //아이템을 주울 수 있는 상태인지 판단 기준1 : 손에 든 채로 또 아이템을 주울 수 있는 아이템을 들고 있는지 리턴
        //소모형 아이템이 1개 남아서 사용하고 아이템이 비워지자마자 아이템이 주워지는 현상 발생. inventory의 아이템 사용 스크립트가 먼저 처리되기 때문
        //해결 방안1 : 아이템이 소모되어 삭제되는 시점을 코루틴 등으로 미루기

        inventory.GetItem(itemID, amount);
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void RequestGetItem()
    {
        if (!getAble) return;

        // 자신의 인벤토리를 찾아 PhotonView ID를 마스터에게 보냅니다.
        // 마스터는 이 ID를 보고 누구에게 아이템을 줘야 할지 알 수 있습니다.
        Inventory localInventory = FindAnyObjectByType<Inventory>();
        if (localInventory == null)
        {
            Debug.LogError("로컬 인벤토리를 찾을 수 없습니다.");
            return;
        }

        PhotonView playerPhotonView = localInventory.GetComponent<PhotonView>();
        if (playerPhotonView != null)
        {
            // 모든 클라이언트가 이 RPC를 호출하지만, 마스터 클라이언트만 응답하게 됩니다.
            // pv는 FieldItem 자신의 PhotonView를 가리킵니다.
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
        getAble = true;
    }

    [PunRPC]
    private void PunRPC_TryToPickup(int requesterViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 아이템이 이미 다른 사람에 의해 파괴되었을 수 있으므로,
        // 이 오브젝트가 아직 유효한지 확인하는 것이 좋습니다.
        if (gameObject == null) return;

        Debug.Log($"'{info.Sender.NickName}' (ViewID: {requesterViewID}) 플레이어가 아이템 습득을 요청합니다.");

        // 요청자의 PhotonView를 찾습니다.
        PhotonView requesterView = PhotonView.Find(requesterViewID);
        if (requesterView != null)
        {
            // 요청자에게만 "PunRPC_AddItem" RPC를 보내 아이템을 인벤토리에 추가하도록 합니다.
            // (이전에 창고 기능 구현 시 Inventory.cs에 만들어 둔 RPC를 재사용합니다.)
            requesterView.RPC("PunRPC_AddItem", info.Sender, this.itemID, this.amount);

            // 아이템 지급에 성공했으므로, 이 필드 아이템을 네트워크에서 파괴합니다.
            // PhotonNetwork.Destroy()는 모든 클라이언트에서 이 오브젝트를 파괴합니다.
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.Log($"[RECEIVER CHECK] OnPhotonInstantiate called for {this.gameObject.name}.");

        object[] data = info.photonView.InstantiationData;

        // 1. 데이터가 아예 없는지 확인
        if (data == null)
        {
            //Debug.LogError("[RECEIVER CHECK] Instantiation data is NULL! No data was received.");
            return;
        }

        //Debug.Log($"[RECEIVER CHECK] Received data array with length: {data.Length}");

        // 2. 데이터는 있는데 내용물이 부족한지 확인
        if (data.Length >= 2)
        {
            // 3. 데이터가 정상일 경우, 어떤 값을 받았는지 확인
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
    public void PunRPC_SetItemProperties(int id, int amt)
    {
        this.itemID = id;
        this.amount = amt;
        Debug.Log($"[PROPERTY SET] Item properties received via RPC. ID set to {this.itemID}, Amount to {this.amount}");
    }
}