using UnityEngine;
using Photon.Pun;


public class LockedObject : InteractableObject
{
    public bool isLocked = true;
    public int requiredKeyID = 4;

    public override void Interact() //상속받는 오브젝트가 마저 구현
    {
        throw new System.NotImplementedException();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }


    public override string GetCursorType()
    {
        if (isLocked)
        {
            return "Key";
        }

        else
        {
            return base.GetCursorType();
        }
    }

    protected void OpenObject(bool useItem = false)
    {
        int handHeldID = GetItemIDFromPlayer();
        if (!isLocked || handHeldID != requiredKeyID) return;

        if (useItem)
        {
            inventory.RemoveItem(inventory.index, 1);
        }

        //isLocked = false;
        RequestUnlockObject();
    }

    public void LockObject(bool state)
    {
        isLocked = state;
    }

    public void RequestUnlockObject()
    {
        if (!usePhoton) LockObject(false);
        else
        {
            pv.RPC("PunRPC_UnlockObject", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    public void PunRPC_UnlockObject()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        pv.RPC("PunRPC_SyncOpen", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void PunRPC_SyncOpen()
    {
        LockObject(false);
    }


    public void RequestLockObject()
    {
        if (!usePhoton) LockObject(true);
        else
        {
            pv.RPC("PunRPC_LockObject", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    public void PunRPC_LockObject()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        pv.RPC("PunRPC_SyncLock", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void PunRPC_SyncLock()
    {
        LockObject(true);
    }
}
