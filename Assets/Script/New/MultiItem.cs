using System;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;

public class MultiItem : MonoBehaviourPun
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PhotonView playerView = other.GetComponent<PhotonView>();

        if (playerView != null)
        {
            string nickname = playerView.Owner.NickName;
            Debug.Log($"{nickname} 님이 아이템을 주웠습니다!");

            if (photonView.IsMine || PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
