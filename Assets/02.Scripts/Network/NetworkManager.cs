/*using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using BackEnd.Tcp;
using SpiritAge.Core;
using SpiritAge.Utility;
using BackEnd.BackndNewtonsoft.Json;

namespace SpiritAge.Network
{
    /// <summary>
    /// ��Ʈ��ũ �Ŵ��� - �ǽð� PVP
    /// </summary>
    public class NetworkManager : AbstractSingleton<NetworkManager>
    {
        [Header("Network Settings")]
        [SerializeField] private float syncInterval = 0.5f;
        [SerializeField] private float timeoutDuration = 30f;
        [SerializeField] private int maxReconnectAttempts = 3;

        // Connection State
        public bool IsConnected { get; private set; }
        public bool IsInMatch { get; private set; }
        public string SessionId { get; private set; }
        public string OpponentNickname { get; private set; }

        // Match Data
        private MatchInfo currentMatch;
        private PlayerData localPlayerData;
        private PlayerData opponentData;

        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<MatchInfo> OnMatchFound;
        public event Action<string> OnMatchEnded;
        public event Action<NetworkMessage> OnMessageReceived;

        protected override void OnSingletonAwake()
        {
            InitializeNetwork();
        }

        /// <summary>
        /// ��Ʈ��ũ �ʱ�ȭ
        /// </summary>
        private void InitializeNetwork()
        {
            // Backend Match �ʱ�ȭ
            Backend.Match.OnJoinMatchMakingServer += OnJoinMatchMakingServer;
            Backend.Match.OnMatchMakingResponse += OnMatchMakingResponse;
            Backend.Match.OnException += OnException;
            Backend.Match.OnSessionJoinInServer += OnSessionJoinInServer;
            Backend.Match.OnSessionOnline += OnSessionOnline;
            Backend.Match.OnSessionOffline += OnSessionOffline;
            Backend.Match.OnMatchInGameAccess += OnMatchInGameAccess;
            Backend.Match.OnMatchInGameStart += OnMatchInGameStart;
            Backend.Match.OnMatchResult += OnMatchResult;
            Backend.Match.OnMatchRelay += OnMatchRelay;
        }

        /// <summary>
        /// ��Ī ���� ����
        /// </summary>
        public void ConnectToMatchServer()
        {
            if (IsConnected)
            {
                Debug.LogWarning("[NetworkManager] Already connected to match server");
                return;
            }

            Debug.Log("[NetworkManager] Connecting to match server...");

            ErrorInfo errorInfo = Backend.Match.JoinMatchMakingServer();
            if (errorInfo.Category != ErrorCode.Success)
            {
                Debug.LogError($"[NetworkManager] Failed to join match server: {errorInfo}");
            }
        }

        /// <summary>
        /// ��Ī ����
        /// </summary>
        public void StartMatchmaking(MatchType matchType = MatchType.Rank)
        {
            if (!IsConnected)
            {
                Debug.LogError("[NetworkManager] Not connected to match server");
                return;
            }

            if (IsInMatch)
            {
                Debug.LogWarning("[NetworkManager] Already in match");
                return;
            }

            Debug.Log($"[NetworkManager] Starting matchmaking: {matchType}");

            // Create match request
            var matchRequest = new MatchMakingRequestInfo
            {
                matchType = matchType,
                matchModeType = MatchModeType.OneOnOne,
                sandboxMode = false
            };

            Backend.Match.RequestMatchMaking(matchRequest);
        }

        /// <summary>
        /// ��Ī ���
        /// </summary>
        public void CancelMatchmaking()
        {
            Debug.Log("[NetworkManager] Canceling matchmaking...");
            Backend.Match.CancelMatchMaking();
        }

        /// <summary>
        /// ���� ������ ����
        /// </summary>
        public void SendGameData(NetworkMessage message)
        {
            if (!IsInMatch)
            {
                Debug.LogWarning("[NetworkManager] Not in match, cannot send data");
                return;
            }

            var data = new Dictionary<string, object>
            {
                ["type"] = message.type,
                ["data"] = message.data,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string jsonData = JsonConvert.SerializeObject(data);
            Backend.Match.SendDataToInGameRoom(jsonData);
        }

        /// <summary>
        /// �� ������ ����
        /// </summary>
        public void SendFormationData(List<BaseUnit> formation)
        {
            var formationData = new FormationData
            {
                units = new List<UnitSyncData>()
            };

            foreach (var unit in formation)
            {
                formationData.units.Add(new UnitSyncData
                {
                    id = unit.Id,
                    name = unit.Name,
                    evolutionType = unit.EvolutionType,
                    attack = unit.Stats.Attack,
                    health = unit.Stats.Health,
                    speed = unit.Stats.Speed,
                    attributes = unit.Attributes
                });
            }

            var message = new NetworkMessage
            {
                type = MessageType.Formation,
                data = JsonConvert.SerializeObject(formationData)
            };

            SendGameData(message);
        }

        /// <summary>
        /// �� �׼� ����
        /// </summary>
        public void SendTurnAction(TurnAction action)
        {
            var message = new NetworkMessage
            {
                type = MessageType.TurnAction,
                data = JsonConvert.SerializeObject(action)
            };

            SendGameData(message);
        }

        // ========== Backend Callbacks ==========

        private void OnJoinMatchMakingServer(JoinChannelEventArgs args)
        {
            if (args.ErrInfo.Category == ErrorCode.Success)
            {
                IsConnected = true;
                SessionId = args.Session.SessionId;
                Debug.Log($"[NetworkManager] Connected to match server. Session: {SessionId}");
                OnConnected?.Invoke();
            }
            else
            {
                Debug.LogError($"[NetworkManager] Failed to connect: {args.ErrInfo}");
            }
        }

        private void OnMatchMakingResponse(MatchMakingResponseEventArgs args)
        {
            switch (args.ErrInfo.Category)
            {
                case ErrorCode.Success:
                    Debug.Log("[NetworkManager] Matchmaking in progress...");
                    break;

                case ErrorCode.Match_InProgress:
                    Debug.Log("[NetworkManager] Match found! Waiting for all players...");
                    break;

                case ErrorCode.Match_MatchMakingCanceled:
                    Debug.Log("[NetworkManager] Matchmaking canceled");
                    break;

                default:
                    Debug.LogError($"[NetworkManager] Matchmaking error: {args.ErrInfo}");
                    break;
            }
        }

        private void OnMatchInGameAccess(MatchInGameSessionEventArgs args)
        {
            if (args.ErrInfo != ErrorCode.Success)
            {
                Debug.LogError($"[NetworkManager] In-game access error: {args.ErrInfo}");
                return;
            }

            Debug.Log("[NetworkManager] Match access granted. Loading game...");

            // Store match info
            currentMatch = new MatchInfo
            {
                matchId = args.GameRecord.m_matchId,
                hostSession = args.GameRecord.m_hostSession,
                sessionList = args.GameRecord.m_sessionList
            };
        }

        private void OnMatchInGameStart(MatchInGameSessionEventArgs args)
        {
            IsInMatch = true;

            Debug.Log("[NetworkManager] Match started!");

            // Find opponent
            foreach (var session in currentMatch.sessionList)
            {
                if (session.SessionId != SessionId)
                {
                    OpponentNickname = session.NickName;
                    break;
                }
            }

            OnMatchFound?.Invoke(currentMatch);

            // Start game
            StartCoroutine(MatchGameLoop());
        }

        private void OnMatchRelay(RelayEventArgs args)
        {
            if (args.ErrInfo != ErrorCode.Success)
            {
                Debug.LogError($"[NetworkManager] Relay error: {args.ErrInfo}");
                return;
            }

            try
            {
                var messageData = JsonConvert.DeserializeObject<Dictionary<string, object>>(args.Data.ToString());

                var message = new NetworkMessage
                {
                    type = (MessageType)Enum.Parse(typeof(MessageType), messageData["type"].ToString()),
                    data = messageData["data"].ToString(),
                    timestamp = Convert.ToInt64(messageData["timestamp"])
                };

                OnMessageReceived?.Invoke(message);
                ProcessNetworkMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Failed to process relay data: {e.Message}");
            }
        }

        private void OnMatchResult(MatchResultEventArgs args)
        {
            IsInMatch = false;

            Debug.Log($"[NetworkManager] Match ended: {args.ErrInfo}");
            OnMatchEnded?.Invoke(args.ErrInfo.Reason);
        }

        private void OnSessionJoinInServer(JoinChannelEventArgs args)
        {
            Debug.Log($"[NetworkManager] Player joined: {args.Session.NickName}");
        }

        private void OnSessionOnline(SessionOnlineEventArgs args)
        {
            Debug.Log($"[NetworkManager] Player online: {args.Session.NickName}");
        }

        private void OnSessionOffline(SessionOfflineEventArgs args)
        {
            Debug.Log($"[NetworkManager] Player offline: {args.Session.NickName}");

            // Handle disconnection
            if (IsInMatch)
            {
                HandleOpponentDisconnect();
            }
        }

        private void OnException(Exception e)
        {
            Debug.LogError($"[NetworkManager] Exception: {e.Message}");
        }

        // ========== Game Logic ==========

        /// <summary>
        /// ��ġ ���� ����
        /// </summary>
        private IEnumerator MatchGameLoop()
        {
            // Wait for both players to be ready
            yield return WaitForPlayersReady();

            // Sync initial data
            SendPlayerData();

            // Wait for opponent data
            yield return WaitForOpponentData();

            // Start battle
            StartNetworkBattle();
        }

        /// <summary>
        /// �÷��̾� �غ� ���
        /// </summary>
        private IEnumerator WaitForPlayersReady()
        {
            float timeout = Time.time + timeoutDuration;

            while (Time.time < timeout)
            {
                // Check if both players are ready
                if (localPlayerData != null && opponentData != null)
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            Debug.LogError("[NetworkManager] Timeout waiting for players");
            DisconnectFromMatch();
        }

        /// <summary>
        /// �÷��̾� ������ ����
        /// </summary>
        private void SendPlayerData()
        {
            localPlayerData = new PlayerData
            {
                nickname = Backend.BMember.GetUserInfo().GetReturnValue()["nickname"].ToString(),
                formation = BackendGameManager.Instance.CurrentPlayerDeck.formation,
                round = BackendGameManager.Instance.CurrentRound
            };

            var message = new NetworkMessage
            {
                type = MessageType.PlayerData,
                data = JsonConvert.SerializeObject(localPlayerData)
            };

            SendGameData(message);
        }

        /// <summary>
        /// ��� ������ ���
        /// </summary>
        private IEnumerator WaitForOpponentData()
        {
            float timeout = Time.time + timeoutDuration;

            while (Time.time < timeout)
            {
                if (opponentData != null)
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            Debug.LogError("[NetworkManager] Timeout waiting for opponent data");
            DisconnectFromMatch();
        }

        /// <summary>
        /// ��Ʈ��ũ ��Ʋ ����
        /// </summary>
        private void StartNetworkBattle()
        {
            Debug.Log("[NetworkManager] Starting network battle!");

            // Convert opponent data to units
            var opponentUnits = ConvertToUnits(opponentData.formation);

            // Start battle
            BattleManager.Instance.StartBattle(
                BackendGameManager.Instance.CurrentPlayerDeck.formation,
                opponentUnits
            );
        }

        /// <summary>
        /// ��Ʈ��ũ �޽��� ó��
        /// </summary>
        private void ProcessNetworkMessage(NetworkMessage message)
        {
            switch (message.type)
            {
                case MessageType.PlayerData:
                    opponentData = JsonConvert.DeserializeObject<PlayerData>(message.data);
                    Debug.Log($"[NetworkManager] Received opponent data: {opponentData.nickname}");
                    break;

                case MessageType.Formation:
                    var formationData = JsonConvert.DeserializeObject<FormationData>(message.data);
                    UpdateOpponentFormation(formationData);
                    break;

                case MessageType.TurnAction:
                    var turnAction = JsonConvert.DeserializeObject<TurnAction>(message.data);
                    ProcessOpponentAction(turnAction);
                    break;

                case MessageType.Emote:
                    var emote = message.data;
                    ShowOpponentEmote(emote);
                    break;
            }
        }

        /// <summary>
        /// ��� �� ������Ʈ
        /// </summary>
        private void UpdateOpponentFormation(FormationData formationData)
        {
            // Update opponent units based on formation data
            Debug.Log($"[NetworkManager] Opponent formation updated: {formationData.units.Count} units");
        }

        /// <summary>
        /// ��� �׼� ó��
        /// </summary>
        private void ProcessOpponentAction(TurnAction action)
        {
            // Process opponent's turn action
            Debug.Log($"[NetworkManager] Opponent action: {action.actionType}");
        }

        /// <summary>
        /// ��� �̸�Ʈ ǥ��
        /// </summary>
        private void ShowOpponentEmote(string emote)
        {
            UIManager.Instance.ShowNotification($"{OpponentNickname}: {emote}", NotificationType.Info);
        }

        /// <summary>
        /// ���� ��ȯ
        /// </summary>
        private List<BaseUnit> ConvertToUnits(List<BaseUnit> formationData)
        {
            // Convert network data to actual units
            // This is simplified - implement proper conversion
            return formationData;
        }

        /// <summary>
        /// ��� ���� ���� ó��
        /// </summary>
        private void HandleOpponentDisconnect()
        {
            Debug.Log("[NetworkManager] Opponent disconnected!");

            // Award win to remaining player
            BattleManager.Instance.OnBattleEnd?.Invoke(BattleResult.Victory);

            DisconnectFromMatch();
        }

        /// <summary>
        /// ��ġ ���� ����
        /// </summary>
        public void DisconnectFromMatch()
        {
            if (IsInMatch)
            {
                Backend.Match.LeaveGameRoom();
                IsInMatch = false;
            }

            currentMatch = null;
            localPlayerData = null;
            opponentData = null;
            OpponentNickname = null;
        }

        /// <summary>
        /// ���� ���� ����
        /// </summary>
        public void DisconnectFromServer()
        {
            if (IsConnected)
            {
                Backend.Match.LeaveMatchMakingServer();
                IsConnected = false;
                SessionId = null;
            }

            OnDisconnected?.Invoke();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsInMatch)
            {
                // Handle app pause during match
                SendGameData(new NetworkMessage
                {
                    type = MessageType.Pause,
                    data = "paused"
                });
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsInMatch)
            {
                // Handle loss of focus during match
            }
        }

        private void OnDestroy()
        {
            DisconnectFromMatch();
            DisconnectFromServer();
        }
    }

    // ========== Data Structures ==========

    /// <summary>
    /// ��ġ ����
    /// </summary>
    [Serializable]
    public class MatchInfo
    {
        public string matchId;
        public SessionId hostSession;
        public List<SessionId> sessionList;
    }

    /// <summary>
    /// ��Ʈ��ũ �޽���
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public MessageType type;
        public string data;
        public long timestamp;
    }

    /// <summary>
    /// �޽��� Ÿ��
    /// </summary>
    public enum MessageType
    {
        PlayerData,
        Formation,
        TurnAction,
        Emote,
        Pause,
        Resume,
        Surrender
    }

    /// <summary>
    /// ��ġ Ÿ��
    /// </summary>
    public enum MatchType
    {
        Rank,
        Casual,
        Friend,
        Tournament
    }

    /// <summary>
    /// �÷��̾� ������
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string nickname;
        public List<BaseUnit> formation;
        public int round;
        public int health;
        public int gold;
    }

    /// <summary>
    /// �� ������
    /// </summary>
    [Serializable]
    public class FormationData
    {
        public List<UnitSyncData> units;
    }

    /// <summary>
    /// ���� ����ȭ ������
    /// </summary>
    [Serializable]
    public class UnitSyncData
    {
        public string id;
        public string name;
        public EvolutionType evolutionType;
        public int attack;
        public int health;
        public int speed;
        public List<ElementAttribute> attributes;
    }

    /// <summary>
    /// �� �׼�
    /// </summary>
    [Serializable]
    public class TurnAction
    {
        public string actionType;
        public string unitId;
        public string targetId;
        public int damage;
        public Dictionary<string, object> additionalData;
    }
}*/