using Photon.Pun;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // 유니티 인스펙터에 연결하지 않아도 Resources에서 자동 로드 가능

    void Start()
    {
        if (PhotonNetwork.IsConnected || PhotonNetwork.InRoom)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            PhotonNetwork.Instantiate("Playerable", spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogError("포톤 네트워크에 연결되어 있지 않거나 방에 입장하지 않았습니다.");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // 원하는 범위에 맞게 수정 가능
        return new Vector3(0f, 0f, 0f);
    }
}