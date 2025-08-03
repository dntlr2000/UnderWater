using Photon.Pun;
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

    //이 구조로 구현하면 InteractionType이 필요한가? 싶음. 
    public virtual InteractionType GetInteractionType() => InteractionType.Instant;
    public virtual string GetCursorType() => cursorType; // => return cursorType와 동일
    public virtual string GetInteractionID() => interactionId;

    public abstract void Interact(); //카메라가 이 오브젝트를 바라볼 때 호출됨
    public virtual void HoldInteract() { }

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
        if (interactionUI != null)
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
}
