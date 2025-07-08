using Photon.Pun;
using UnityEngine;

public class Rock : MonoBehaviour, Interactable
{
    private PhotonView pv;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    public string GetCursorType() => "Hand";
    public string GetInteractionID() => "Rock";
    public InteractionType GetInteractionType() => InteractionType.Instant;

    public void Interact()
    {
        Debug.Log("돌 채집중");

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
    private void RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
