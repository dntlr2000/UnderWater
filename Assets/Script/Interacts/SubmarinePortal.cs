using UnityEngine;
using Photon.Pun;

public class SubmarinePortal : Portal
{
    public Transform submarine;
    public bool ifIndoor = false; //실내로 들어가는 문인지 여부

    
    public override void HoldInteract()
    {
        base.HoldInteract();
        FixRader(ifIndoor);
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props.Add("IsIndoor", ifIndoor);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void FixRader(bool ifLock)
    {
        RaderScript rader = FindAnyObjectByType<RaderScript>();
        if (ifLock)
        {
            rader.SetCenter(submarine);
            Debug.Log("레이더 중심 : 잠수함");
        }
        else
        {
            rader.SetCenter(player.transform);
            Debug.Log("레이더 중심 : 플레이어");
        }
    }
}
