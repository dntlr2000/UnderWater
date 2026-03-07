/*using System.Linq;
using Photon.Pun;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // Resources에서 자동 로드 가능

    void Start()
    {
        Debug.Log("[PlayerManager] Start called");
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("포톤 네트워크에 연결되어 있지 않거나 방에 입장하지 않았습니다.");
            return;
        }

        string userId = NetworkManager.Instance.currentUserId;

        // SaveManager에서 현재 플레이어 데이터 가져오기
        var save = SaveManager.Instance.GetCurrentSave();
        Debug.Log($"[PlayerManager] Save loaded: {save != null}");

        PlayerData myData = save?.players.FirstOrDefault(p => p.playerId == userId);
        Debug.Log($"[PlayerManager] MyData: {myData?.playerId}, Pos: {myData?.position?.ToVector3()}");
        Vector3 spawnPos = myData?.position?.ToVector3() ?? GetRandomSpawnPosition();
        int jobIndex = myData?.jobIndex ?? -1;

        // Photon Instantiate 시, 초기 데이터 전달
        object[] initData = new object[] { spawnPos, jobIndex };
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity, 0, initData);
        Debug.Log($"[PlayerManager] Instantiate sent: Pos={spawnPos}, JobIndex={jobIndex}");

        Player pc = playerObj.GetComponent<Player>();

        if (pc != null && jobIndex >= 0 && pc.photonView.IsMine)
        {
            pc.SetJob(jobIndex);

            // 인벤토리 적용 가능
            // if (myData.items != null) pc.ApplyInventory(myData.items);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }
}
*/