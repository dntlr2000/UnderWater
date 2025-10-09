using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class Launcher : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // 1. Photon 서버 연결
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();  // AppID 자동 사용
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버에 연결됨");
        // 2. 랜덤 방 입장 또는 없으면 생성
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("랜덤 방 없음, 새로 생성함");
        // 3. 랜덤 방 없으면 새로 생성
        RoomOptions options = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom("Room_" + Random.Range(0, 1000), options);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방에 입장 성공: " + PhotonNetwork.CurrentRoom.Name);
        // 4. 방 입장 완료 → GameScene으로 이동
        //PhotonNetwork.LoadLevel("Multitest");
        PhotonNetwork.LoadLevel("SampleScene");
    }
}
