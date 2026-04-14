using Photon.Pun;
using UnityEngine;

public class InGameNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("테스트 설정")]
    public string playerPrefabName = "Playerable"; // Resources 폴더에 있는 플레이어 프리팹 이름
    public int testJobIndex = 0;               // 테스트해보고 싶은 직업 번호

    void Start()
    {
        // 1. 포톤에 연결되어 있지 않다면 (로비를 거치지 않고 인게임 씬에서 바로 시작했다면)
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[테스트 모드] 포톤 미연결 감지! 오프라인 모드로 전환합니다.");
            PhotonNetwork.OfflineMode = true;
            // OfflineMode를 true로 켜는 순간, 포톤이 알아서 가상의 방을 만들고 입장합니다.
            // 그리고 아래에 있는 OnJoinedRoom() 콜백을 자동으로 실행해줍니다.
        }
        else
        {
            // 정상적으로 로비를 거쳐서 들어왔다면 여기서 바로 소환
            SpawnTestPlayer();
        }
    }

    // 2. 가상의 방(오프라인 룸)에 입장이 완료되면 자동으로 실행되는 함수
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            SpawnTestPlayer();
        }
    }

    // 3. 실제 플레이어 소환 로직
    private void SpawnTestPlayer()
    {
        // 하늘에서 떨어지게 임의의 위치 설정 (원하는 테스트 위치로 수정 가능)
        Vector3 spawnPos = new Vector3(600f, 1000f, 600f);

        // 핵심: Player.cs의 OnPhotonInstantiate가 요구하는 데이터(InstantiationData)를 가짜로 만들어서 넣어줍니다.
        object[] instantiateData = new object[]
        {
            spawnPos,       // data[0]: 스폰 위치
            testJobIndex    // data[1]: 직업 인덱스 (기본값 0)
        };

        // 포톤 네트워크를 통해 플레이어 생성!
        PhotonNetwork.Instantiate(playerPrefabName, spawnPos, Quaternion.identity, 0, instantiateData);

        Debug.Log($"[테스트 모드] 플레이어 스폰 완료! (직업 인덱스: {testJobIndex})");
    }
}