using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Realtime;
using UnityEngine;

public abstract class InteractableObject : MonoBehaviour, Interactable
{
    public string cursorType = "Set CursorType Name"; //커서 이미지 바꾸기
    public string interactionId = "InteractionID"; //무슨 오브젝트와 상호작용하는지 체크(개별) //objectName으로 대체 가능하면 삭제 가능?

    protected Inventory inventory;
    public string objectName = "Object Name";

    protected float holdDuration = 2f;
    protected float holdTimer = 0f;

    InteractionUI interactionUI;

    public Player player;
    protected Camera playerCamera;
    protected RaycastHit lastHit;
    protected int playerId;
    public bool usePhoton;

    //public float cooldownTime;

    //이 구조로 구현하면 InteractionType이 필요한가? 싶음. 
    public virtual InteractionType GetInteractionType() => InteractionType.Instant;
    public virtual string GetCursorType() => cursorType; // => return cursorType와 동일
    public virtual string GetInteractionID() => interactionId;

    public abstract void Interact(); //카메라가 이 오브젝트를 바라볼 때 호출됨
    public virtual void HoldInteract() {
        Debug.Log("홀딩 완료");
    }

    protected PhotonView pv;
    protected virtual void Awake()
    {
        pv = GetComponent<PhotonView>();
        interactionUI = FindAnyObjectByType<InteractionUI>(); //여러 플레이어가 있을 경우를 대비해야할듯
    }
    

    public void UpdateGuage(bool interact, float holdTime)
    {
        if (interactionUI == null) interactionUI = FindAnyObjectByType<InteractionUI>();

        if (interactionUI == null) { Debug.LogError("InteractionUI를 찾을 수 없습니다."); }

        //interactionUI.ShowGauge();
        if (interact)
        {
            interactionUI.ShowGauge();
            holdTimer += Time.deltaTime;
            interactionUI.UpdateGauge(holdTimer / holdTime);

            if (holdTimer >= holdTime)
            {
                HoldInteract();
                ResetInteractionState();
            }
        }
        else
        {
            interactionUI.ShowCursor();
            holdTimer = 0f;
            interactionUI.UpdateGauge(0f);
        }

    }

    public string GetObjectName()
    {
        return objectName;
    }

    public void ResetInteractionState()
    {
        if (interactionUI == null)
        {
            Debug.LogWarning("ResetInteractionState을 실행할 수 없습니다.");
            return;
        }
        holdTimer = 0f;
        //currentTarget = null;
        interactionUI.ResetUI();
        interactionUI.UpdateGauge(0f);
    }


    [PunRPC]
    protected void RPC_Deactivate()
    {
        gameObject.SetActive(false);
    }

    protected void DestroyOnPhoton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // 마스터에게 파괴 요청
            pv.RPC("RequestDestroy", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    protected void RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    protected GameObject GenerateOnPhoton(string objName, Vector3 pos)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameObject generated = PhotonNetwork.Instantiate(objName, pos, Quaternion.identity);
            // Resources/"오브젝트명".prefab
            return generated;
        }
        else
        {
            Debug.LogWarning("PhotonNetwork가 존재하지 않습니다");
            return null;
        }
    }

    public virtual void SetInteractor(Player interactor, Camera cam = null, Inventory inv = null, RaycastHit hit = default)
    {
        //Debug.Log("상호작용에 필요한 정보를 수신하였습니다.");

        player = interactor;
        playerCamera = cam != null ? cam : Camera.main; //Camera.main으로도 아예 할당 없이 될까
        //이후 플레이어 간의 인벤토리 구분 기능이 필요하면 수정 필요
        inventory = inv != null ? inv : null; //일단 인벤토리가 UI에 귀속되어 있으므로 null로
        lastHit = hit;

        // PUN에서 조작자 식별이 필요하면 ActorNumber를 함께 저장(옵션)
        playerId = Photon.Pun.PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
    }

    protected virtual void DestroySelf()
    {
        // 포톤을 사용하고 PhotonView가 있다면 네트워크 파괴를 시도합니다.
        if (usePhoton && pv != null)
        {
            DestroyOnPhoton(); // 이 메소드는 이미 마스터/클라이언트 로직을 포함하고 있습니다.
        }
        // 포톤을 사용하지 않는 일반 오브젝트라면 그냥 로컬에서 파괴합니다.
        else if (!usePhoton)
        {
            Destroy(gameObject);
        }
    }


}
