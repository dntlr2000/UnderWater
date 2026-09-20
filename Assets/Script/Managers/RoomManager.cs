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

    private const string JobSelectionRevisionKey = "JobSelectionRevision";
    private int localJobSelectionRevision;
    private bool hasPendingJobSelection;
    private string pendingJobType = "";

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

    // 입장 시 최신 저장을 한 번 읽고, 초기 직업만 Photon 속성에 반영합니다.
    public override void OnJoinedRoom()
    {
        ResetPendingJobSelection();
        OutgameCanvasManager.Instance.ShowRoomPanel(PhotonNetwork.CurrentRoom.Name);

        Debug.Log($"[RoomManager] 방 입장 완료. ID(Firebase): {GetPlayerId(PhotonNetwork.LocalPlayer)}");

        // 1. 방 속성(Room Properties)에 저장 데이터가 있는지 확인
        bool hasSaveData = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SaveData");
        bool isLoadedGameRoom = CheckIsLoadedGameRoom();

        // 2. 데이터가 있다면 즉시 로컬 SaveManager에 주입 (수신 대기하지 않음)
        if (hasSaveData && SaveMngr != null)
        {
            string json = (string)PhotonNetwork.CurrentRoom.CustomProperties["SaveData"];
            SaveMngr.HandleBroadcastedSaveData(json); // 방의 새 게임/불러오기 구분도 함께 복원합니다.
            Debug.Log("[RoomManager] 입장 즉시 방 데이터를 로컬에 로드했습니다.");
        }

        // 4. 내 직업 확인 및 적용
        string mySavedJobType = SaveMngr?.GetSavedJobType(GetPlayerId(PhotonNetwork.LocalPlayer)) ?? "";
        Debug.Log($"[RoomManager] 로드된 데이터에서 내 직업 확인: {mySavedJobType} (ID: {GetPlayerId(PhotonNetwork.LocalPlayer)})");

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

        // 속성이 이미 같아 알림이 없어도 최초 참가자 정보와 최신 스냅샷은 등록합니다.
        if (PhotonNetwork.IsMasterClient && SaveMngr != null)
        {
            SaveMngr.UpdateLocalPlayerJob(GetPlayerId(PhotonNetwork.LocalPlayer), PhotonNetwork.NickName,
                isLoadedGameRoom ? mySavedJobType : "");
            SaveSyncManager.Instance?.PublishCurrentSave();
        }

        RoomRenewal();
        OutgameCanvasManager.Instance.StartBtn.interactable = PhotonNetwork.IsMasterClient;
        OutgameCanvasManager.Instance.JobSelectPanel.SetActive(true);

        RefreshJobButtons();
        RefreshPlayerSlots();
        RefreshReadyGauge();
    }

    // 새 참가자는 자신의 입장 처리에서 직업을 복원하고, 방장은 현재 속성만 저장합니다.
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RoomRenewal();
        ChatRPC("System", $"<color=yellow>{newPlayer.NickName}님이 참가하셨습니다.</color>");
        if (PhotonNetwork.IsMasterClient && SaveMngr != null)
        {
            string job = SaveMngr.isGameLoadedFromSave
                ? SaveMngr.GetSavedJobType(GetPlayerId(newPlayer))
                : newPlayer.CustomProperties["JobType"] as string ?? "";
            SaveMngr.UpdateLocalPlayerJob(GetPlayerId(newPlayer), newPlayer.NickName, job);
        }
        ApplySavedJobs();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RoomRenewal();
        ChatRPC("System", $"<color=yellow>{otherPlayer.NickName}님이 퇴장하셨습니다.</color>");
        RefreshPlayerSlots();
        RefreshReadyGauge();
    }

    // 저장 수신은 로컬 복원과 화면 갱신만 수행하고, 방장은 자신의 오래된 응답을 적용하지 않습니다.
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (!PhotonNetwork.InRoom || !propertiesThatChanged.ContainsKey("SaveData")) return;
        if (!PhotonNetwork.IsMasterClient && SaveMngr != null &&
            PhotonNetwork.CurrentRoom.CustomProperties["SaveData"] is string json)
            SaveMngr.HandleBroadcastedSaveData(json);
        ApplySavedJobs();
    }

    // 새 방장은 각 참가자의 현재 직업 속성으로 저장을 맞춘 뒤 최신 상태를 게시합니다.
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient && SaveMngr != null)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties["JobType"] is string job)
                    SaveMngr.UpdateLocalPlayerJob(GetPlayerId(player), player.NickName, job);
            }
            SaveSyncManager.Instance?.PublishCurrentSave();
        }
        if (OutgameCanvasManager.Instance?.StartBtn != null)
            OutgameCanvasManager.Instance.StartBtn.interactable = PhotonNetwork.IsMasterClient;
        ApplySavedJobs();
    }

    // 퇴장하면 이전 방에서 기다리던 선택을 버리고 직업 입력을 비활성화합니다.
    public override void OnLeftRoom()
    {
        ResetPendingJobSelection();
        ApplySavedJobs();
        if (OutgameCanvasManager.Instance?.StartBtn != null)
            OutgameCanvasManager.Instance.StartBtn.interactable = false;
    }

    // 연결 종료 후 직업 선택과 게임 시작으로 새 네트워크 요청을 보내지 못하게 합니다.
    public override void OnDisconnected(DisconnectCause cause)
    {
        ResetPendingJobSelection();
        ApplySavedJobs();
        if (OutgameCanvasManager.Instance?.StartBtn != null)
            OutgameCanvasManager.Instance.StartBtn.interactable = false;
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

    // 저장 동기화에 사용한 참가자 ID를 기준으로 준비된 인원을 계산합니다.
    private void RefreshReadyGauge()
    {
        if (OutgameCanvasManager.Instance == null) return;

        var players = PhotonNetwork.PlayerList;
        int jobSelectedCount = 0;

        foreach (var p in players)
        {
            string jobType = SaveMngr?.GetSavedJobType(GetPlayerId(p)) ?? "";
            if (!string.IsNullOrEmpty(jobType)) jobSelectedCount++;
        }

        OutgameCanvasManager.Instance.UpdateReadyGauge(jobSelectedCount, players.Length);
    }

    // 연결과 마지막 선택의 확정을 확인한 뒤 모든 참가자의 직업이 준비되면 시작합니다.
    public void TryStartGame()
    {
        if (!CanSendJobSelection()) return;
        if (hasPendingJobSelection)
        {
            OutgameCanvasManager.Instance?.SetStatus("캐릭터 선택을 확인하고 있습니다. 잠시 기다려주세요.");
            return;
        }
        if (!PhotonNetwork.IsMasterClient)
        {
            OutgameCanvasManager.Instance.SetStatus("게임 시작은 마스터 클라이언트만 가능합니다.");
            return;
        }

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string jobType = SaveMngr?.GetSavedJobType(GetPlayerId(player)) ?? "";
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

    // 현재 방에서 직업 속성을 전송할 수 있는 상태인지 확인합니다.
    private static bool CanSendJobSelection()
    {
        return PhotonNetwork.InRoom && (PhotonNetwork.OfflineMode || PhotonNetwork.IsConnectedAndReady);
    }

    // Photon 인증 ID를 우선하여 저장과 화면에서 동일한 참가자를 조회합니다.
    private static string GetPlayerId(Photon.Realtime.Player player)
    {
        if (player == null) return "";
        if (!string.IsNullOrEmpty(player.UserId)) return player.UserId;
        if (player.IsLocal && AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.currentUserId))
            return AuthManager.Instance.currentUserId;
        return !string.IsNullOrEmpty(player.NickName) ? player.NickName : "UnknownUser_" + player.ActorNumber;
    }

    // 마지막 클릭이 확인되기 전에는 과거 응답 대신 사용자의 최신 선택을 표시합니다.
    private string GetDisplayedJobType(Photon.Realtime.Player player)
    {
        if (player == null) return "";
        if (player.IsLocal && hasPendingJobSelection) return pendingJobType;
        if (player.CustomProperties["JobType"] is string job) return job;
        return SaveMngr?.GetSavedJobType(GetPlayerId(player)) ?? "";
    }

    // 새 방이나 연결 종료 시 이전 방의 선택 대기 상태를 초기화합니다.
    private void ResetPendingJobSelection()
    {
        hasPendingJobSelection = false;
        pendingJobType = "";
        localJobSelectionRevision = 0;
    }

    // 직업과 요청 순번을 한 번에 보내며 같은 값의 재전송을 생략합니다.
    private bool SetLocalPlayerJobProperty(string jobType)
    {
        if (!CanSendJobSelection()) return false;
        jobType ??= "";
        var local = PhotonNetwork.LocalPlayer;
        if ((hasPendingJobSelection && pendingJobType == jobType) ||
            (!hasPendingJobSelection && local.CustomProperties["JobType"] is string current && current == jobType))
            return true;

        bool previousPending = hasPendingJobSelection;
        string previousJob = pendingJobType;
        int previousRevision = localJobSelectionRevision;
        int confirmedRevision = local.CustomProperties[JobSelectionRevisionKey] is int revision ? revision : 0;
        localJobSelectionRevision = Math.Max(localJobSelectionRevision, confirmedRevision) + 1;
        hasPendingJobSelection = true;
        pendingJobType = jobType;
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "JobType", jobType }, { JobSelectionRevisionKey, localJobSelectionRevision }
        };
        if (local.SetCustomProperties(props)) return true;

        // 전송 자체가 실패했다면 이전에 대기하던 선택 상태로 돌아갑니다.
        hasPendingJobSelection = previousPending;
        pendingJobType = previousJob;
        localJobSelectionRevision = previousRevision;
        return false;
    }

    // 직업 속성 알림은 방장만 저장에 반영하며 수신자가 다시 변경 요청을 보내지 않습니다.
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!PhotonNetwork.InRoom || targetPlayer == null) return;
        if (changedProps.ContainsKey("JobType"))
        {
            if (targetPlayer.IsLocal && hasPendingJobSelection &&
                changedProps[JobSelectionRevisionKey] is int revision && revision == localJobSelectionRevision &&
                changedProps["JobType"] as string == pendingJobType)
                hasPendingJobSelection = false;

            if (PhotonNetwork.IsMasterClient && SaveMngr != null &&
                targetPlayer.CustomProperties["JobType"] is string job)
                SaveMngr.UpdateLocalPlayerJob(GetPlayerId(targetPlayer), targetPlayer.NickName, job);
        }
        ApplySavedJobs();
    }

    // 마지막 클릭을 기준으로 선택/취소하며 저장용 RPC를 별도로 중복 전송하지 않습니다.

    public void SelectJob(int index)
    {
        if (!CanSendJobSelection())
        {
            OutgameCanvasManager.Instance?.SetStatus("방에 연결된 후 캐릭터를 선택해주세요.");
            return;
        }
        if (jobDatas == null || index < 0 || index >= jobDatas.Length || jobDatas[index] == null) return;

        string selectedJobType = jobDatas[index].jobType.ToString();
        bool isRoomLoaded = CheckIsLoadedGameRoom();

        // 1. 로드된 게임이고, 내 직업이 데이터에 존재한다면 -> 절대 변경 불가
        if (isRoomLoaded)
        {
            string savedJob = SaveMngr?.GetSavedJobType(GetPlayerId(PhotonNetwork.LocalPlayer)) ?? "";
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
                if (pData.playerId != GetPlayerId(PhotonNetwork.LocalPlayer) && pData.jobType == selectedJobType)
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

        // 확인 대기 중에도 마지막 클릭을 기준으로 같은 캐릭터의 선택을 취소합니다.
        string myCurrentJobType = GetDisplayedJobType(PhotonNetwork.LocalPlayer);

        if (myCurrentJobType == selectedJobType)
        {
            if (!SetLocalPlayerJobProperty("")) return;
            OutgameCanvasManager.Instance?.SetStatus("직업 선택을 취소했습니다.");
        }
        else
        {
            if (!SetLocalPlayerJobProperty(selectedJobType)) return;
            OutgameCanvasManager.Instance?.SetStatus($"{jobDatas[index].jobName}을(를) 선택했습니다.");
        }
        ApplySavedJobs();
    }

    // 저장된 로컬 직업에 대응하는 데이터 인덱스를 찾습니다.
    private int GetMyJobIndex()
    {
        string myJobType = SaveMngr?.GetSavedJobType(GetPlayerId(PhotonNetwork.LocalPlayer)) ?? "";
        for (int i = 0; i < jobDatas.Length; i++)
            if (jobDatas[i].jobType.ToString() == myJobType) return i;
        return -1;
    }

    // 마지막 로컬 선택을 강조하고 방 밖 또는 고정 직업에서는 선택 버튼을 잠급니다.
    public void RefreshJobButtons()
    {
        if (OutgameCanvasManager.Instance == null || OutgameCanvasManager.Instance.JobBtns == null) return;
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;

        // 내가 고정되어야 하는 상태인지 확인
        bool isRoomLoaded = CheckIsLoadedGameRoom();

        string myJobType = SaveMngr?.GetSavedJobType(GetPlayerId(PhotonNetwork.LocalPlayer)) ?? "";
        bool isMyJobFixed = isRoomLoaded && !string.IsNullOrEmpty(myJobType);

        string myCurrentPropJobType = GetDisplayedJobType(PhotonNetwork.LocalPlayer);

        for (int i = 0; i < canvas.JobBtns.Length; i++)
        {
            if (canvas.JobBtns[i] == null) continue;
            if (jobDatas == null || i >= jobDatas.Length || jobDatas[i] == null)
            {
                canvas.JobBtns[i].interactable = false;
                continue;
            }

            string thisJobType = jobDatas[i].jobType.ToString();

            bool isTakenByOther = false;
            if (SaveMngr != null && SaveMngr.GetCurrentSave() != null && SaveMngr.GetCurrentSave().players != null)
            {
                foreach (var pData in SaveMngr.GetCurrentSave().players)
                {
                    if (pData.playerId != GetPlayerId(PhotonNetwork.LocalPlayer) && pData.jobType == thisJobType)
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

            canvas.JobBtns[i].interactable = CanSendJobSelection() && !isMyJobFixed && !isTakenByOther;

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

    // 캐릭터 슬롯도 버튼과 같은 최신 직업을 표시하여 저장 응답 지연으로 되돌아가지 않게 합니다.
    public void RefreshPlayerSlots()
    {
        if (OutgameCanvasManager.Instance == null) return;

        var players = PhotonNetwork.PlayerList;
        List<PlayerInfo> playerInfos = new List<PlayerInfo>();

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            string jobType = GetDisplayedJobType(p);

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
        RefreshReadyGauge();
    }
    #endregion

    // 명시적인 직업 복원 시에만 사용하며 동일한 속성은 다시 전송하지 않습니다.
    public void ApplyLoadedJobToPhoton(string loadedJobType)
    {
        SetLocalPlayerJobProperty(loadedJobType ?? ""); // ★ 변경
        Debug.Log($"[RoomManager] Loaded Job ({loadedJobType}) applied to Photon Custom Properties.");
    }
}