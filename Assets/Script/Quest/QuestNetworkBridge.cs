using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class QuestNetworkBridge : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte EventCode = 110;
    private const string StateKey = "uw.quest.state.v1";
    private const string RevisionKey = "uw.quest.revision.v1";
    private const string JobKey = "uw.quest.job.v1";
    // 삭제한 전용 획득 메시지의 번호(3~5)는 재사용하지 않습니다.
    private const int Progress = 1, Complete = 2, Rejected = 6, PlayerStateRequest = 7, PlayerStateResponse = 8, DeliveryOffer = 9, DeliveryResult = 10;
    private QuestManager manager;
    private SharedQuestState pendingPublish;
    private string sentJson;
    private float sentAt, nextRetry;
    private readonly Dictionary<string, QuestWireMessage> requests = new();
    private readonly HashSet<int> awaitingPlayerStates = new();
    private string recoveryId;
    public bool IsRecoveringPlayerStates => awaitingPlayerStates.Count > 0;

    public static string LocalPlayerId => PlayerId(PhotonNetwork.LocalPlayer);

    // 플레이어의 저장 ID를 네트워크 Actor ID와 구분합니다.
    public static string PlayerId(Photon.Realtime.Player player) => !string.IsNullOrEmpty(player?.UserId)
        ? player.UserId : $"Actor_{player?.ActorNumber ?? 0}";

    // 퀘스트 규칙을 소유하는 관리자를 연결합니다.
    private void Awake() => manager = GetComponent<QuestManager>();

    // 준비 이후 상태 발행과 같은 요청 ID의 재전송을 낮은 빈도로 처리합니다.
    private void Update()
    {
        if (manager == null || !manager.IsInitialized || !PhotonNetwork.InRoom) return;
        PumpPublish();
        if (Time.unscaledTime < nextRetry) return;
        nextRetry = Time.unscaledTime + 1f;
        var state = ReadRoomState(CurrentSaveId());
        if (state != null)
        {
            manager.ApplySharedState(state);
            RemoveConfirmedRequests(state);
        }
        foreach (var request in requests.Values.ToList()) SendToMaster(request);
        if (!PhotonNetwork.IsMasterClient || state == null) return;
        foreach (int actor in awaitingPlayerStates.ToList())
            Send(actor, new QuestWireMessage { kind = PlayerStateRequest, requestId = recoveryId });
    }

    // 현재 세션 저장 ID를 반환하여 이전 게임의 패킷을 걸러냅니다.
    private string CurrentSaveId() => SaveManager.Instance.GetCurrentSave()?.saveId ?? "";

    // 방의 확정된 공유 상태를 읽고 다른 저장 세션은 제외합니다.
    public SharedQuestState ReadRoomState(string saveId)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(StateKey, out var raw) || !(raw is string json)) return null;
        try
        {
            var state = JsonUtility.FromJson<SharedQuestState>(json);
            return state != null && state.initialized && state.saveId == saveId ? state : null;
        }
        catch (ArgumentException) { return null; }
    }

    // 연속된 로컬 변경 중 가장 최신 스냅샷을 발행 대기열에 남깁니다.
    public void PublishSharedState(SharedQuestState state)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        pendingPublish = state;
        PumpPublish();
    }

    // 한 번에 한 스냅샷을 비교 후 교환하고 서버 확인 뒤 다음 변경을 발행합니다.
    private void PumpPublish()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
        var properties = PhotonNetwork.CurrentRoom.CustomProperties;
        if (sentJson != null)
        {
            if (properties.TryGetValue(StateKey, out var confirmed) && (string)confirmed == sentJson)
            {
                if (pendingPublish != null && JsonUtility.ToJson(pendingPublish) == sentJson) pendingPublish = null;
                sentJson = null;
            }
            else if (Time.unscaledTime - sentAt < 3f) return;
            else sentJson = null;
        }
        if (pendingPublish == null) return;
        var server = ReadRoomState(pendingPublish.saveId);
        if (server != null && server.revision > pendingPublish.revision)
        {
            pendingPublish = null;
            manager.ApplySharedState(server, true);
            return;
        }
        sentJson = JsonUtility.ToJson(pendingPublish);
        sentAt = Time.unscaledTime;
        var update = new Hashtable { [StateKey] = sentJson, [RevisionKey] = pendingPublish.revision };
        Hashtable expected = properties.ContainsKey(RevisionKey) ? new Hashtable { [RevisionKey] = properties[RevisionKey] } : null;
        if (!PhotonNetwork.CurrentRoom.SetCustomProperties(update, expected)) sentJson = null;
    }

    // 수집은 획득 직후 인벤토리도 포함해 방장의 완료 저장이 최신 상태를 쓰게 합니다.
    public void RequestMainProgress(ObjectiveType type, int amount, string itemId)
    {
        var message = new QuestWireMessage { kind = Progress, type = (int)type, amount = amount, itemId = itemId };
        if (type == ObjectiveType.CollectItem && Player.localPlayer != null)
        {
            var inventory = FindAnyObjectByType<Inventory>();
            if (inventory != null && inventory.CaptureInventorySnapshot() != null)
                message.playerState = Player.localPlayer.CaptureQuestPlayerState(inventory);
        }
        QueueRequest(message);
    }

    // 서버가 해당 지급 의도를 보관한 뒤에만 실제 전달을 시작합니다.
    public bool IsSharedRevisionConfirmed(int revision) => ReadRoomState(CurrentSaveId())?.revision >= revision;

    // 지급 내용은 공유 상태에서 조회하므로 전달 메시지에는 고정 지급 ID만 넣습니다.
    public void SendDeliveryOffer(int actor, string deliveryId) =>
        Send(actor, new QuestWireMessage { kind = DeliveryOffer, requestId = deliveryId });

    // 수령 결과와 수령 직후 인벤토리/영수증을 현재 방장에게 함께 보냅니다.
    public void SendDeliveryResult(string deliveryId, RewardReceiveResult result, PlayerData player) =>
        SendToMaster(new QuestWireMessage { kind = DeliveryResult, requestId = deliveryId, result = (int)result, playerState = player });

    // 메인 수동 완료 요청을 전달하며 실제 조건은 방장이 재검사합니다.
    public void RequestMainCompletion(string questId)
    {
        QueueRequest(new QuestWireMessage { kind = Complete, questId = questId });
    }

    // 신뢰 가능한 재시도를 위해 새 요청에 고유 ID를 한 번만 부여합니다.
    private void QueueRequest(QuestWireMessage message)
    {
        if (!PhotonNetwork.InRoom) return;
        message.requestId = Guid.NewGuid().ToString("N");
        message.saveId = CurrentSaveId();
        requests[message.requestId] = message;
        SendToMaster(message);
    }

    // 로컬 방장도 같은 수신 경로를 사용합니다.
    private void SendToMaster(QuestWireMessage message) => Send(PhotonNetwork.MasterClient.ActorNumber, message);

    // 퀘스트 전용 이벤트로 한 플레이어에게 전달합니다.
    private void Send(int actor, QuestWireMessage message)
    {
        if (!PhotonNetwork.InRoom) return;
        message.saveId = CurrentSaveId();
        if (actor == PhotonNetwork.LocalPlayer.ActorNumber) { HandleMessage(message, actor); return; }
        PhotonNetwork.RaiseEvent(EventCode, JsonUtility.ToJson(message), new RaiseEventOptions { TargetActors = new[] { actor } }, SendOptions.SendReliable);
    }

    // 이 기능의 이벤트만 파싱하고 다른 시스템의 이벤트에는 관여하지 않습니다.
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == EventCode && photonEvent.CustomData is string json) HandleMessage(DeserializeMessage(json), photonEvent.Sender);
    }

    // 잘못된 패킷은 상태를 바꾸지 않고 무시합니다.
    private static QuestWireMessage DeserializeMessage(string json)
    {
        try { return JsonUtility.FromJson<QuestWireMessage>(json); }
        catch (ArgumentException) { return null; }
    }

    // 세션, 발신자 및 메시지 방향을 검사한 뒤 해당 처리기로 분기합니다.
    private void HandleMessage(QuestWireMessage message, int sender)
    {
        if (message == null || manager == null || !manager.IsInitialized || message.saveId != CurrentSaveId() || string.IsNullOrEmpty(message.requestId)) return;
        if (PhotonNetwork.CurrentRoom.GetPlayer(sender) == null) return;
        if (message.kind == DeliveryOffer)
        {
            if (sender != PhotonNetwork.MasterClient.ActorNumber) return;
            var confirmed = ReadRoomState(CurrentSaveId());
            if (confirmed != null) manager.ApplySharedState(confirmed);
            GetComponent<RewardDeliveryService>().ReceiveOffer(message.requestId);
            return;
        }
        if (message.kind == Rejected || message.kind == PlayerStateRequest)
        {
            if (sender != PhotonNetwork.MasterClient.ActorNumber) return;
            if (message.kind == PlayerStateRequest) ReplyCurrentPlayerState(message.requestId);
            else requests.Remove(message.requestId);
            return;
        }
        if (!PhotonNetwork.IsMasterClient) return;
        switch (message.kind)
        {
            case Progress:
                if (!Enum.IsDefined(typeof(ObjectiveType), message.type)) break;
                if (message.playerState != null)
                {
                    message.playerState.playerId = PlayerId(PhotonNetwork.CurrentRoom.GetPlayer(sender));
                    message.playerState.inventoryActor = sender;
                }
                manager.ApplyMainProgress(message.requestId, (ObjectiveType)message.type, message.amount, message.itemId, message.playerState, PlayerId(PhotonNetwork.CurrentRoom.GetPlayer(sender)));
                break;
            case Complete:
                if (!manager.CompleteMainQuest(message.questId, PlayerId(PhotonNetwork.CurrentRoom.GetPlayer(sender)))) Send(sender, new QuestWireMessage { kind = Rejected, requestId = message.requestId });
                break;
            case DeliveryResult:
                if (message.playerState == null || !Enum.IsDefined(typeof(RewardReceiveResult), message.result)) break;
                message.playerState.playerId = PlayerId(PhotonNetwork.CurrentRoom.GetPlayer(sender));
                message.playerState.inventoryActor = sender;
                GetComponent<RewardDeliveryService>().AcceptResult(message.requestId, (RewardReceiveResult)message.result,
                    message.playerState, message.playerState.playerId);
                break;
            case PlayerStateResponse:
                if (message.requestId != recoveryId || message.playerState?.items == null) return;
                message.playerState.playerId = PlayerId(PhotonNetwork.CurrentRoom.GetPlayer(sender));
                message.playerState.inventoryActor = sender;
                SaveManager.Instance.UpdatePlayerCache(message.playerState);
                awaitingPlayerStates.Remove(sender);
                if (!IsRecoveringPlayerStates) SaveManager.Instance.SaveGame();
                break;
        }
    }

    // 새 방장에게 획득 당시 스냅샷이 아닌 현재 인벤토리와 상태를 전달합니다.
    private void ReplyCurrentPlayerState(string requestId)
    {
        var inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null || Player.localPlayer == null || inventory.CaptureInventorySnapshot() == null) return;
        SendToMaster(new QuestWireMessage { kind = PlayerStateResponse, requestId = requestId,
            playerState = Player.localPlayer.CaptureQuestPlayerState(inventory) });
    }

    // 서버 상태에 반영된 요청만 로컬 재전송 목록에서 제거합니다.
    private void RemoveConfirmedRequests(SharedQuestState state)
    {
        foreach (var request in requests.Values.ToList())
        {
            bool done = request.kind == Progress && state.processedEvents.Contains(request.requestId) ||
                request.kind == Complete && state.completedQuestIds.Contains(request.questId);
            if (done) requests.Remove(request.requestId);
        }
    }

    // 개인 직업 상태를 별도 패킷으로 보관하여 빈 목록도 명시적으로 저장합니다.
    public void PublishLocalJobState()
    {
        if (manager == null || !manager.IsInitialized || !PhotonNetwork.InRoom) return;
        var progress = manager.GetQuestSaveData();
        var state = new PlayerQuestState { saveId = CurrentSaveId(), playerId = LocalPlayerId, completed = progress.completed, active = progress.active, rewardClaims = manager.GetJobRewardClaims() };
        SaveManager.Instance.UpdatePlayerQuestCache(state);
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { [JobKey] = JsonUtility.ToJson(state) });
    }

    // 다른 플레이어의 직업 데이터는 자신의 퀘스트에 적용하지 않고 저장 캐시에만 반영합니다.
    private void CachePlayerJob(Photon.Realtime.Player player)
    {
        if (!player.CustomProperties.TryGetValue(JobKey, out var raw) || !(raw is string json)) return;
        try
        {
            var state = JsonUtility.FromJson<PlayerQuestState>(json);
            if (state == null || state.saveId != CurrentSaveId()) return;
            state.playerId = PlayerId(player);
            SaveManager.Instance.UpdatePlayerQuestCache(state);
        }
        catch (ArgumentException) { }
    }

    // 공유 변경을 UI에 적용하고 발행 확인을 진행합니다.
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (manager == null || !manager.IsInitialized || !propertiesThatChanged.ContainsKey(StateKey)) return;
        var state = ReadRoomState(CurrentSaveId());
        if (state != null) { manager.ApplySharedState(state); RemoveConfirmedRequests(state); }
        PumpPublish();
    }

    // 직업 저장 패킷이 변경되면 방장의 개인별 캐시에 반영합니다.
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        if (manager != null && manager.IsInitialized && PhotonNetwork.IsMasterClient && changedProps.ContainsKey(JobKey)) CachePlayerJob(targetPlayer);
    }

    // 새 방장은 서버 확정 상태와 참가자별 저장 정보를 이어받습니다.
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (manager == null || !manager.IsInitialized) return;
        sentJson = null;
        pendingPublish = null;
        var state = ReadRoomState(CurrentSaveId());
        if (state != null) manager.ApplySharedState(state, true);
        else if (PhotonNetwork.IsMasterClient) manager.RestoreMainAfterMasterChange();
        awaitingPlayerStates.Clear();
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var player in PhotonNetwork.PlayerList) CachePlayerJob(player);
            recoveryId = Guid.NewGuid().ToString("N");
            awaitingPlayerStates.UnionWith(PhotonNetwork.PlayerList.Where(p => !p.IsInactive).Select(p => p.ActorNumber));
            foreach (int actor in awaitingPlayerStates.ToList())
                Send(actor, new QuestWireMessage { kind = PlayerStateRequest, requestId = recoveryId });
        }
        nextRetry = 0f;
        PublishLocalJobState();
    }

    // 떠난 참가자를 복원 대기에서 제외하고 남은 참가자의 최신 상태로 저장합니다.
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        awaitingPlayerStates.Remove(otherPlayer.ActorNumber);
        if (!IsRecoveringPlayerStates && manager != null && manager.IsInitialized) SaveManager.Instance.SaveGame();
    }
}

[Serializable]
internal class QuestWireMessage
{
    public int kind;
    public string saveId, requestId, questId, itemId;
    public int type, amount, result;
    public PlayerData playerState;
}
