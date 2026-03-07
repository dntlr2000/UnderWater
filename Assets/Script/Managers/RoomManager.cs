using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using System.Collections.Generic;
using ExitGames.Client.Photon;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private AuthManager AuthMngr => AuthManager.Instance;
    private SaveManager SaveMngr => SaveManager.Instance;

    [HideInInspector] public bool isLoadedFromSave = false;
    public JobData[] jobDatas;

    private bool CheckIsLoadedGameRoom()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("IsLoadedGame"))
        {
            return (bool)PhotonNetwork.CurrentRoom.CustomProperties["IsLoadedGame"];
        }
        return false;
    }

    #region Room Join / Leave / Start

    public override void OnJoinedRoom()
    {
        OutgameCanvasManager.Instance.ShowRoomPanel();

        Debug.Log($"[RoomManager] 방 입장 완료. ID(Firebase): {AuthMngr.currentUserId}");

        // 1. 방 속성(Room Properties)에 저장 데이터가 있는지 확인
        bool hasSaveData = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SaveData");
        bool isLoadedGameRoom = CheckIsLoadedGameRoom();

        // 2. 데이터가 있다면 즉시 로컬 SaveManager에 주입 (수신 대기하지 않음)
        if (hasSaveData && SaveMngr != null)
        {
            string json = (string)PhotonNetwork.CurrentRoom.CustomProperties["SaveData"];
            SaveMngr.HandleBroadcastedSaveData(json); // 여기서 isGameLoadedFromSave가 true가 됨
            Debug.Log("[RoomManager] 입장 즉시 방 데이터를 로컬에 로드했습니다.");
        }

        // 3. 방장이라면 로컬 데이터를 다시 한 번 확실하게 방에 뿌림 (동기화 보장)
        if (PhotonNetwork.IsMasterClient && SaveMngr != null)
        {
            SaveMngr.UpdateLocalPlayerJob(AuthMngr.currentUserId, PhotonNetwork.NickName, SaveMngr.GetSavedJob(AuthMngr.currentUserId) ?? -1);
        }

        // 4. 내 직업 확인 및 적용
        int mySavedJobIndex = -1;
        if (SaveMngr != null)
        {
            mySavedJobIndex = SaveMngr.GetSavedJob(AuthMngr.currentUserId) ?? -1;
            Debug.Log($"[RoomManager] 로드된 데이터에서 내 직업 확인: {mySavedJobIndex} (ID: {AuthMngr.currentUserId})");
        }

        // 5. 직업 설정 분기
        if (isLoadedGameRoom)
        {
            if (mySavedJobIndex != -1)
            {
                // 저장된 직업이 있으면 그걸로 강제 고정
                SetLocalPlayerJobProperty(mySavedJobIndex);
                Debug.Log($"[RoomManager] 저장된 직업({mySavedJobIndex})으로 고정합니다.");
                OutgameCanvasManager.Instance.SetStatus("저장된 게임입니다. 직업이 고정됩니다.");
            }
            else
            {
                // 로드된 게임이지만 내 정보가 없으면(신규 유저) -1
                Debug.Log("[RoomManager] 저장된 데이터에 내 정보가 없습니다. (신규 참가)");
            }
        }
        else
        {
            // 새 게임이면 -1
            SetLocalPlayerJobProperty(-1);
        }

        RoomRenewal();
        OutgameCanvasManager.Instance.StartBtn.interactable = PhotonNetwork.IsMasterClient;
        OutgameCanvasManager.Instance.JobSelectPanel.SetActive(true);

        RefreshJobButtons();
        RefreshPlayerSlots();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RoomRenewal();
        ChatRPC("System", $"<color=yellow>{newPlayer.NickName}님이 참가하셨습니다.</color>");

        if (PhotonNetwork.IsMasterClient && SaveMngr != null && SaveMngr.isGameLoadedFromSave)
        {
            // 방장이 가진 최신 SaveData에서 새로 들어온 플레이어의 ID 검색
            int savedJob = SaveMngr.GetSavedJob(newPlayer.UserId) ?? -1;

            if (savedJob != -1)
            {
                Debug.Log($"[RoomManager] (Master) 입장한 {newPlayer.NickName}의 저장된 직업({savedJob})을 찾아 강제 설정합니다.");

                // 타겟 플레이어의 커스텀 프로퍼티를 방장이 직접 수정 (권한 행사)
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "JobIndex", savedJob } };
                newPlayer.SetCustomProperties(props);
            }
        }
        RefreshPlayerSlots();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RoomRenewal();
        ChatRPC("System", $"<color=yellow>{otherPlayer.NickName}님이 퇴장하셨습니다.</color>");
        RefreshPlayerSlots();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("SaveData"))
        {
            string json = (string)propertiesThatChanged["SaveData"];
            Debug.Log("[RoomManager] 실시간 데이터 업데이트 수신.");

            if (SaveMngr != null)
            {
                SaveMngr.HandleBroadcastedSaveData(json);
                RefreshJobButtons();
                RefreshPlayerSlots();
            }
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        OutgameCanvasManager.Instance.StartBtn.interactable = PhotonNetwork.IsMasterClient;
    }

    private void RoomRenewal()
    {
        if (OutgameCanvasManager.Instance == null) return;
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;
        if (canvas.ListText != null)
            canvas.ListText.text = string.Join(", ", Array.ConvertAll(PhotonNetwork.PlayerList, p => p.NickName));
        if (canvas.RoomInfoText != null)
            canvas.RoomInfoText.text = $"{PhotonNetwork.CurrentRoom.Name} / {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
    }

    public void TryStartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            OutgameCanvasManager.Instance.SetStatus("게임 시작은 마스터 클라이언트만 가능합니다.");
            return;
        }

        foreach (var player in PhotonNetwork.PlayerList)
        {
            int jobIndex = SaveMngr?.GetSavedJob(player.UserId) ?? -1;
            if (jobIndex < 0)
            {
                OutgameCanvasManager.Instance.SetStatus($"{player.NickName}이 직업을 선택하지 않았습니다.");
                return;
            }
        }

        SaveMngr?.SaveGame();
        PhotonNetwork.LoadLevel("SampleScene");
    }
    #endregion

    #region Chat
    public void SendChat(string message)
    {
        if (!string.IsNullOrEmpty(message))
            NetworkBootstrap.Instance.PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName, message);
    }

    [PunRPC]
    public void ChatRPC(string user, string message)
    {
        if (OutgameCanvasManager.Instance == null || OutgameCanvasManager.Instance.ChatText == null) return;

        List<string> chatHistory = new List<string>();
        for (int i = 0; i < OutgameCanvasManager.Instance.ChatText.Length; i++)
        {
            if (OutgameCanvasManager.Instance.ChatText[i] != null && !string.IsNullOrEmpty(OutgameCanvasManager.Instance.ChatText[i].text))
                chatHistory.Add(OutgameCanvasManager.Instance.ChatText[i].text);
        }
        chatHistory.Insert(0, user + " : " + message);
        if (chatHistory.Count > OutgameCanvasManager.Instance.ChatText.Length)
            chatHistory.RemoveRange(OutgameCanvasManager.Instance.ChatText.Length, chatHistory.Count - OutgameCanvasManager.Instance.ChatText.Length);
        OutgameCanvasManager.Instance.UpdateChat(chatHistory.ToArray());
    }
    #endregion

    #region Job Management

    private void SetLocalPlayerJobProperty(int jobIndex)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "JobIndex", jobIndex } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == null) return;

        string safeUserId = targetPlayer.UserId;
        if (string.IsNullOrEmpty(safeUserId))
        {
            safeUserId = targetPlayer.NickName;
            if (string.IsNullOrEmpty(safeUserId)) safeUserId = "UnknownUser_" + targetPlayer.ActorNumber;
        }

        if (changedProps.ContainsKey("job") || changedProps.ContainsKey("JobIndex") || changedProps.ContainsKey("Job"))
        {
            object jobValue = null;
            if (changedProps.TryGetValue("job", out jobValue) ||
                changedProps.TryGetValue("Job", out jobValue) ||
                changedProps.TryGetValue("JobIndex", out jobValue))
            {
                int newJobIndex = (int)jobValue;
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.UpdateLocalPlayerJob(safeUserId, targetPlayer.NickName, newJobIndex);
                }
            }
        }
        RefreshJobButtons();
        RefreshPlayerSlots();
    }

    public void SelectJob(int index)
    {
        if (jobDatas == null || index < 0 || index >= jobDatas.Length) return;

        int currentSavedJob = SaveMngr?.GetSavedJob(AuthMngr.currentUserId) ?? -1;

        bool isRoomLoaded = CheckIsLoadedGameRoom();

        // 1. 로드된 게임이고, 내 직업이 데이터에 존재한다면 -> 절대 변경 불가
        if (isRoomLoaded)
        {
            if (currentSavedJob != -1)
            {
                if (index != currentSavedJob)
                    OutgameCanvasManager.Instance.SetStatus("저장된 게임에서는 직업을 변경할 수 없습니다.");
                return;
            }
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object val))
            {
                int propJob = (int)val;
                if (propJob != -1 && propJob != index)
                {
                    OutgameCanvasManager.Instance.SetStatus("저장된 정보 동기화 중입니다. 변경할 수 없습니다.");
                    return;
                }
            }
        }

        bool isTakenByOther = false;
        if (SaveMngr != null && SaveMngr.GetCurrentSave() != null && SaveMngr.GetCurrentSave().players != null)
        {
            foreach (var pData in SaveMngr.GetCurrentSave().players)
            {
                if (pData.playerId != AuthMngr.currentUserId && pData.jobIndex == index)
                {
                    isTakenByOther = true;
                    break;
                }
            }
        }

        if (isTakenByOther)
        {
            OutgameCanvasManager.Instance.SetStatus("다른 플레이어가 이미 해당 직업을 선택했습니다.");
            return;
        }

        // 3. [토글 로직] 현재 내가 선택한 상태인지 확인 (Photon Property 기준)
        int myCurrentPropJob = -1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object val2))
        {
            myCurrentPropJob = (int)val2;
        }

        if (myCurrentPropJob == index)
        {
            SetLocalPlayerJobProperty(-1);
            OutgameCanvasManager.Instance.SetStatus("직업 선택을 취소했습니다.");
        }
        else
        {
            // 새로운 직업 선택
            SetLocalPlayerJobProperty(index);
            OutgameCanvasManager.Instance.SetStatus($"{jobDatas[index].jobName}을(를) 선택했습니다.");
        }
    }

    public void RefreshJobButtons()
    {
        if (OutgameCanvasManager.Instance == null || OutgameCanvasManager.Instance.JobBtns == null) return;
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;

        // 내가 고정되어야 하는 상태인지 확인
        bool isRoomLoaded = CheckIsLoadedGameRoom();

        bool isMyJobFixed = false;
        int myFixedJobIndex = -1;

        if (isRoomLoaded && SaveMngr != null)
        {
            int savedJob = SaveMngr.GetSavedJob(AuthMngr.currentUserId) ?? -1;
            if (savedJob != -1)
            {
                isMyJobFixed = true;
                myFixedJobIndex = savedJob;
            }
        }

        int myCurrentPropJob = -1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object val))
        {
            myCurrentPropJob = (int)val;
        }

        for (int i = 0; i < canvas.JobBtns.Length; i++)
        {
            if (canvas.JobBtns[i] == null) continue;

            // 다른 사람이 선점했는지 확인
            bool isTakenByOther = false;
            if (SaveMngr != null && SaveMngr.GetCurrentSave() != null && SaveMngr.GetCurrentSave().players != null)
            {
                foreach (var pData in SaveMngr.GetCurrentSave().players)
                {
                    if (pData.playerId != AuthMngr.currentUserId && pData.jobIndex == i)
                    {
                        isTakenByOther = true;
                        break;
                    }
                }
            }

            // 고정된 상태라면 fixedIndex, 아니면 프로퍼티 상의 Index
            bool isThisButtonMyJob = isMyJobFixed ? (myFixedJobIndex == i) : (myCurrentPropJob == i);

            if (isMyJobFixed)
            {
                // [고정 상태] 내 직업이든 아니든 모두 '클릭 불가'로 만듦
                canvas.JobBtns[i].interactable = false;
            }
            else
            {
                canvas.JobBtns[i].interactable = !isTakenByOther;
            }

            // 색상 처리 (고정 상태라도 내 직업은 초록색으로 표시)
            var img = canvas.JobBtns[i].GetComponent<Image>();
            if (img != null)
            {
                if (isThisButtonMyJob)
                {
                    img.color = Color.green; // 내 직업 (고정됨 or 선택함)
                }
                else if (isTakenByOther)
                {
                    img.color = Color.gray; // 남이 가져감
                }
                else
                {
                    img.color = Color.white; // 선택 가능
                }
            }
        }
    }

    public void RefreshPlayerSlots()
    {
        if (OutgameCanvasManager.Instance == null) return;

        var players = PhotonNetwork.PlayerList;
        List<PlayerInfo> playerInfos = new List<PlayerInfo>();

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            int jobIndex = SaveMngr?.GetSavedJob(p.UserId) ?? -1;

            PlayerInfo info = new PlayerInfo
            {
                Nickname = p.NickName,
                UserId = p.UserId,
                JobName = "직업 없음",
                JobIcon = null
            };

            if (jobIndex >= 0 && jobIndex < jobDatas.Length)
            {
                info.JobName = jobDatas[jobIndex].jobName;
                if (OutgameCanvasManager.Instance.JobIcons != null && jobIndex < OutgameCanvasManager.Instance.JobIcons.Length)
                {
                    info.JobIcon = OutgameCanvasManager.Instance.JobIcons[jobIndex];
                }
            }
            playerInfos.Add(info);
        }

        OutgameCanvasManager.Instance.UpdatePlayerSlots(playerInfos);
    }

    public void ApplySavedJobs()
    {
        RefreshJobButtons();
        RefreshPlayerSlots();
    }
    #endregion

    public void ApplyLoadedJobToPhoton(int loadedJobIndex)
    {
        if (loadedJobIndex >= 0)
        {
            Debug.Log($"[RoomManager] Loaded Job ({loadedJobIndex}) applied to Photon Custom Properties.");
            SetLocalPlayerJobProperty(loadedJobIndex);
        }
        else
        {
            SetLocalPlayerJobProperty(-1);
        }
    }
}