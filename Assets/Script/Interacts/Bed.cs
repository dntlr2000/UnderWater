using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Bed : InteractableObject
{
    public Transform lyingPoint;
    public Transform awakePoint;

    public bool isUsing;
    public int usingPlayerID;

    //private IEnumerator SleepRoutine;

    public void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Y))
        {
            if (isUsing)
            {
                SetAwake();
            }
        }
    }

    public override void Interact()
    {
        if (!isUsing && Input.GetMouseButton(1))
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
        if (player == null)
        {
            return;
        }
        else
        {
            if (isUsing) return;
            SetSleep();
        }
    }

    public void SetSleep()
    {
        
        player.gameObject.transform.position = lyingPoint.position;
        isUsing = true;
        player.condition.onWork = true;
        StartCoroutine(getSleepCoroutine());
        player.condition.ResetMove();

        if (usePhoton)
        {
            usingPlayerID = player.photonView.ViewID;
            RequestSetUsing(true, usingPlayerID);
        }
    }

    public void SetAwake()
    {
        //if (usingPlayer != player) return;
        if (usePhoton)
        {
            if (usingPlayerID != player.photonView.ViewID) return;
        }

        player.gameObject.transform.position = awakePoint.position;
        isUsing = false;
        player.condition.onWork = false;
        StopAllCoroutines();

        if (usePhoton)
        {
            usingPlayerID = -1;
            RequestSetUsing(false, -1);
        }
    }

    public void RequestSetUsing(bool isUsing, int viewID)
    {
        pv.RPC("PunRPC_SetUsing", RpcTarget.AllBuffered, isUsing, viewID);
    }

    [PunRPC]
    public void PunRPC_SetUsing(bool value, int viewID)
    {
        this.isUsing = value;
        this.usingPlayerID = viewID;
    }

    public IEnumerator getSleepCoroutine()
    {
        //onWork = true;
        int maxCount = 10;
        while (player.condition.onWork && maxCount > 0)
        {
            yield return new WaitForSeconds(1f);
            if (player.condition.onWork)
            {
                player.condition.RecoverFatigue(1f);
                maxCount--;
            }
        }
        
        if (isUsing) SetAwake();

        yield return null;
    }
}
