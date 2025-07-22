using UnityEngine;
using Photon.Pun;

public class InGameManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1; // 플레이어 번호 (0부터 시작)
        Vector3 spawnBase = new Vector3(0, 7f, 0);
        Vector3 offset = new Vector3(2f * playerIndex, 0, 0); // X축으로 간격 띄움
        Vector3 spawnPos = spawnBase + offset;

        PhotonNetwork.Instantiate("Playerable", spawnPos, Quaternion.identity);
    }
}
