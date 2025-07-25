using Photon.Pun;
using UnityEngine;

public abstract class InteractableObject : MonoBehaviour, Interactable
{
    public string cursorType = "Set CursorType Name"; //커서 이미지 바꾸기
    public string interactionId = "InteractionID"; //무슨 오브젝트와 상호작용하는지 체크(개별) //objectName으로 대체 가능하면 삭제 가능?

    protected Inventory inventory;
    public string objectName = "Object Name";

    protected float holdDuration;
    protected float holdTime;

    //이 구조로 구현하면 InteractionType이 필요한가? 싶음. 
    public virtual InteractionType GetInteractionType() => InteractionType.Instant;
    public virtual string GetCursorType() => cursorType; // => return cursorType와 동일
    public virtual string GetInteractionID() => interactionId;

    public abstract void Interact(); //카메라가 이 오브젝트를 바라볼 때 호출됨

    /*
    protected PhotonView pv;
    protected virtual void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    */

    public void UpdateGuage(bool interact, float holdTime)
    {

    }

    public string GetObjectName()
    {
        return objectName;
    }

}
