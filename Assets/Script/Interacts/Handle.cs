using UnityEngine;
using Photon.Pun;

public class Handle : InteractableObject
{
    public SubmarineOutside submarineBody;

    private void Start()
    {
        if (submarineBody == null) submarineBody = FindAnyObjectByType<SubmarineOutside>();
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

        if (isInteractable == false)
        {
            return;
        }
        else
        {
            submarineBody.player = player;
            submarineBody.SwitchSubmarineState(true);
            
            if (usePhoton)
            {
                usingPlayerID = player.photonView.ViewID;
                RequestSetUsing(true, usingPlayerID);
                submarineBody.ConnectHandle(this);
            }
        }
    }

    [PunRPC]
    public override void PunRPC_SetUsing(bool value, int viewID)
    {
        this.isInteractable = !value;
        submarineBody.controllable = value;
        this.usingPlayerID = viewID;
    }
}
