using Photon.Pun;
using UnityEngine;

public class Door : LockedObject
{
    public Animator animator;
    public bool isOpen = false;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        OpenDoor(isOpen);
    }

    public override void Interact()
    {
        if (Input.GetMouseButton(1))
        {
            UpdateGuage(true, holdDuration);
        }
        else
        {
            UpdateGuage(false, holdDuration);
        }
    }

    public override void HoldInteract()
    {
        if (isLocked) { OpenObject(false); }
        else
        {
            isOpen = !isOpen;
            RequestOpenDoor(isOpen);
        }
    }

    public void OpenDoor(bool state)
    {
        if (animator == null)
        {
            Debug.LogWarning("애니메이션이 할당되지 않았습니다");
            return;
        }

        animator.SetBool("Open", state);
    }

    public void RequestOpenDoor(bool state)
    {
        if (!usePhoton)
        {
            OpenDoor(state);
        }
        else
        {
            pv.RPC("PunRPC_RequestDoor", RpcTarget.MasterClient, state);
        }
    }

    [PunRPC]
    public void PunRPC_RequestDoor(bool state)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        pv.RPC("PunRPC_SyncDoor", RpcTarget.AllBuffered, state);
    }

    [PunRPC]
    public void PunRPC_SyncDoor(bool state)
    {
        OpenDoor(state);
    }
}
