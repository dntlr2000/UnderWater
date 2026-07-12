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
            string masterJobType = SaveMngr.GetSavedJobType(AuthMngr.currentUserId);
            SaveMngr.UpdateLocalPlayerJob(AuthMngr.currentUserId, PhotonNetwork.NickName, masterJobType);
        }

        // 4. 내 직업 확인 및 적용
        string mySavedJobType = SaveMngr?.GetSavedJobType(AuthMngr.currentUserId) ?? "";
        Debug.Log($"[RoomManager] 로드된 데이터에서 내 직업 확인: {mySavedJobType} (ID: {AuthMngr.currentUserId})");

        // 5. 직업 설정 분기
        if (isLoadedGameRoom)
        {
            if (!string.IsNullOrEmpty(mySavedJobType))
            {
                // 저장된 직업이 있으면 고정
                SetLocalPlayerJobProperty(mySavedJobType);
                Debug.Log($"[RoomManager] 저장된 직업({mySavedJobType})으로 고정합니다.");
                OutgameCanvasManager.Instance.SetStatus("저장된 게임입니다. 직업이 고정됩니다.");
            }
            else
            {
                // 로드된 게임이지만 내 정보가 없는 신규 유저 → 직업 선택 허용
                SetLocalPlayerJobProperty("");
                Debug.Log("[RoomManager] 저장된 데이터에 내 정보가 없습니다. (신규 참가) 직업 선택 가능.");
                OutgameCanvasManager.Instance.SetStatus("신규 참가자입니다. 직업을 선택해주세요.");
            }
        }
        else
        {
            // 새 게임 → 직업 선택 허용
            SetLocalPlayerJobProperty("");
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
            string savedJobType = SaveMngr.GetSavedJobType(newPlayer.UserId);

            if (!string.IsNullOrEmpty(savedJobType))
            {
                Debug.Log($"[RoomManager] (Master) 입장한 {newPlayer.NickName}의 저장된 직업({savedJobType})을 찾아 강제 설정합니다.");
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "JobType", savedJobType } };
                newPlayer.SetCustomProperties(props);
            }
            else
            {
                Debug.Log($"[RoomManager] (Master) {newPlayer.NickName}은 신규 참가자입니다. 직업 선택 허용.");
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
            string jobType = SaveMngr?.GetSavedJobType(player.UserId) ?? "";
            if (string.IsNullOrEmpty(jobType))
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

    private void SetLocalPlayerJobProperty(string jobType)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "JobType", jobType } };
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

        if (changedProps.ContainsKey("JobType"))
        {
            string newJobType = (string)changedProps["JobType"];
            if (SaveManager.Instance != null)
                SaveManager.Instance.UpdateLocalPlayerJob(safeUserId, targetPlayer.NickName, newJobType);
        }

        RefreshJobButtons();
        RefreshPlayerSlots();
    }

    public void SelectJob(int index)
    {
        if (jobDatas == null || index < 0 || index >= jobDatas.Length) return;

        string selectedJobType = jobDatas[index].jobType.ToString();
        bool isRoomLoaded = CheckIsLoadedGameRoom();

        // 1. 로드된 게임이고, 내 직업이 데이터에 존재한다면 -> 절대 변경 불가
        if (isRoomLoaded)
        {
            string savedJob = SaveMngr?.GetSavedJobType(AuthMngr.currentUserId) ?? "";
            if (!string.IsNullOrEmpty(savedJob))
            {
                if (selectedJobType != savedJob)
                    OutgameCanvasManager.Instance.SetStatus("저장된 게임에서는 직업을 변경할 수 없습니다.");
                return;
            }
        }

        bool isTakenByOther = false;
        if (SaveMngr?.GetCurrentSave()?.players != null)
        {
            foreach (var pData in SaveMngr.GetCurrentSave().players)
            {
                if (pData.playerId != AuthMngr.currentUserId && pData.jobType == selectedJobType)
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
        string myCurrentJobType = SaveMngr?.GetSavedJobType(AuthMngr.currentUserId) ?? "";

        if (myCurrentJobType == selectedJobType)
        {
            SetLocalPlayerJobProperty("");
            SaveMngr?.UpdateLocalPlayerJob(AuthMngr.currentUserId, PhotonNetwork.NickName, "");
            OutgameCanvasManager.Instance.SetStatus("직업 선택을 취소했습니다.");
        }
        else
        {
            SetLocalPlayerJobProperty(selectedJobType);
            SaveMngr?.UpdateLocalPlayerJob(AuthMngr.currentUserId, PhotonNetwork.NickName, selectedJobType);
            OutgameCanvasManager.Instance.SetStatus($"{jobDatas[index].jobName}을(를) 선택했습니다.");
        }
    }

    private int GetMyJobIndex()
    {
        string myJobType = SaveMngr?.GetSavedJobType(AuthMngr.currentUserId) ?? "";
        for (int i = 0; i < jobDatas.Length; i++)
            if (jobDatas[i].jobType.ToString() == myJobType) return i;
        return -1;
    }

    public void RefreshJobButtons()
    {
        if (OutgameCanvasManager.Instance == null || OutgameCanvasManager.Instance.JobBtns == null) return;
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;

        // 내가 고정되어야 하는 상태인지 확인
        bool isRoomLoaded = CheckIsLoadedGameRoom();

        string myJobType = SaveMngr?.GetSavedJobType(AuthMngr.currentUserId) ?? "";
        bool isMyJobFixed = isRoomLoaded && !string.IsNullOrEmpty(myJobType);

        string myCurrentPropJobType = "";
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobType", out object val))
            myCurrentPropJobType = (string)val ?? "";

        for (int i = 0; i < canvas.JobBtns.Length; i++)
        {
            if (canvas.JobBtns[i] == null) continue;

            string thisJobType = jobDatas[i].jobType.ToString();

            bool isTakenByOther = false;
            if (SaveMngr != null && SaveMngr.GetCurrentSave() != null && SaveMngr.GetCurrentSave().players != null)
            {
                foreach (var pData in SaveMngr.GetCurrentSave().players)
                {
                    if (pData.playerId != AuthMngr.currentUserId && pData.jobType == thisJobType)
                    {
                        isTakenByOther = true;
                        break;
                    }
                }
            }

            // 고정된 상태라면 fixedIndex, 아니면 프로퍼티 상의 Index
            bool isThisButtonMyJob = isMyJobFixed
                ? myJobType == thisJobType
                : myCurrentPropJobType == thisJobType;

            canvas.JobBtns[i].interactable = isMyJobFixed ? false : !isTakenByOther;

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
            string jobType = SaveMngr?.GetSavedJobType(p.UserId) ?? "";

            PlayerInfo info = new PlayerInfo
            {
                Nickname = p.NickName,
                UserId = p.UserId,
                JobName = "직업 없음",
                JobIcon = null
            };

            if (!string.IsNullOrEmpty(jobType))
            {
                for (int j = 0; j < jobDatas.Length; j++)
                {
                    if (jobDatas[j].jobType.ToString() == jobType)
                    {
                        info.JobName = jobDatas[j].jobName;
                        if (OutgameCanvasManager.Instance.JobIcons != null &&
                            j < OutgameCanvasManager.Instance.JobIcons.Length)
                            info.JobIcon = OutgameCanvasManager.Instance.JobIcons[j];
                        break;
                    }
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

    public void ApplyLoadedJobToPhoton(string loadedJobType)
    {
        SetLocalPlayerJobProperty(loadedJobType ?? ""); // ★ 변경
        Debug.Log($"[RoomManager] Loaded Job ({loadedJobType}) applied to Photon Custom Properties.");
    }
}