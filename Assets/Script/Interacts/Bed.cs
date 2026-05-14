using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Bed : InteractableObject
{
    public Transform lyingPoint;
    public Transform awakePoint;

    //public bool isUsing;
    

    //private IEnumerator SleepRoutine;

    public void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.E))
        {
            if (!isInteractable)
            {
                SetAwake();
            }
        }

    }

    public void Start()
    {
        isInteractable = true;
    }

    public override void Interact()
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

    public override void HoldInteract()
    {
        if (player == null)
        {
            return;
        }
        else
        {
            if (!isInteractable) return;
            SetSleep();
        }
    }

    public void SetSleep()
    {
        
        player.gameObject.transform.position = lyingPoint.position;
        isInteractable = false;
        player.condition.SetIsBusy(true);
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
        if (usePhoton)
        {
            if (usingPlayerID != player.photonView.ViewID) return;
        }

        player.gameObject.transform.position = awakePoint.position;
        isInteractable = true;
        player.condition.SetIsBusy(false);
        StopAllCoroutines();

        if (usePhoton)
        {
            usingPlayerID = -1;
            RequestSetUsing(false, -1);
        }
    }

    [PunRPC]
    public override void PunRPC_SetUsing(bool value, int viewID)
    {
        this.isInteractable = !value;
        this.usingPlayerID = viewID;
    }

    public IEnumerator getSleepCoroutine()
    {
        //onWork = true;
        int maxCount = 10;
        while (player.condition.GetIsBusy() && maxCount > 0)
        {
            yield return new WaitForSeconds(1f);
            if (player.condition.GetIsBusy())
            {
                player.condition.RecoverFatigue(1f);
                maxCount--;
            }
        }
        
        if (!isInteractable) SetAwake();

        yield return null;
    }
}
