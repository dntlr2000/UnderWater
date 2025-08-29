using Photon.Pun;
using UnityEngine;

public class InGameNetworkManager : MonoBehaviour
{
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            // 디버깅 테스트용: Photon 연결 안 되어있으면 오프라인 모드로 플레이어 생성
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.CreateRoom("OfflineRoom");
        }

        if (PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
        }
    }
}
