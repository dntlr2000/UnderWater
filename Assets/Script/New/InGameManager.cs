using UnityEngine;
using Photon.Pun;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [Header("직업별 프리팹")]
    public GameObject[] jobPrefabs;

    void Start()
    {
        // 플레이어 스폰 위치 계산
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnBase = new Vector3(0, 7f, 0);
        Vector3 offset = new Vector3(2f * playerIndex, 0, 0);
        Vector3 spawnPos = spawnBase + offset;

        // 로비에서 저장해둔 직업 인덱스 가져오기
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object jobIndexObj))
        {
            int jobIndex = (int)jobIndexObj;

            // 직업에 맞는 프리팹 생성
            GameObject playerObj = PhotonNetwork.Instantiate(jobPrefabs[jobIndex].name, spawnPos, Quaternion.identity);

            // Player 스크립트에서 직업 데이터 할당
            Player player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                player.currentJob = player.allJobs[jobIndex];
                Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] 직업 스폰 완료: {player.currentJob.jobName}");
            }
        }
        else
        {
            Debug.LogError("JobIndex가 설정되지 않았습니다. (로비에서 직업 선택 확인 필요)");
        }
    }
}
