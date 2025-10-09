using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using BackEnd.Tcp;
using LitJson;

public class RealtimeMatchSystem : MonoBehaviour
{
    /*private BackendGameManager gameManager;

    // 매치 타입 정의
    private MatchType matchType = MatchType.turngame;
    private MatchModeType matchMode = MatchModeType.Random;

    // 매칭 상태
    private bool isMatching = false;
    private bool isInMatch = false;

    // 매치 정보
    private SessionId currentSessionId;
    private string myPlayerId;
    private List<SessionId> enemySessionIds = new List<SessionId>();

    // 턴 동기화
    private int currentTurn = 0;
    private Dictionary<SessionId, bool> turnReadyStatus = new Dictionary<SessionId, bool>();

    void Start()
    {
        gameManager = BackendGameManager.Instance;
        myPlayerId = Backend.UserInDate;

        // 매치 서버 핸들러 등록
        RegisterMatchHandlers();
    }

    // ============================================
    // 매치 서버 핸들러 등록
    // ============================================

    private void RegisterMatchHandlers()
    {
        // 매칭 성공 핸들러
        Backend.Match.OnMatchSuccess = (matchType, sessionId) =>
        {
            Debug.Log($"매칭 성공! SessionId: {sessionId}");
            currentSessionId = sessionId;
            isMatching = false;
            isInMatch = true;

            OnMatchSuccess();
        };

        // 매칭 취소 핸들러
        Backend.Match.OnMatchCancel = (reason) =>
        {
            Debug.Log($"매칭 취소: {reason}");
            isMatching = false;
            isInMatch = false;
        };

        // 매치 방 입장 핸들러
        Backend.Match.OnSessionJoinInServer = (sessionId) =>
        {
            Debug.Log("매치 방 입장!");
            StartCoroutine(SendInitialDeckData());
        };

        // 세션 리스트 업데이트
        Backend.Match.OnSessionListInServer = (sessionList) =>
        {
            Debug.Log($"세션 리스트 수신: {sessionList.Count}명");

            enemySessionIds.Clear();
            foreach (var session in sessionList)
            {
                if (session != currentSessionId)
                {
                    enemySessionIds.Add(session);
                }
            }
        };

        // 매치 메시지 수신
        Backend.Match.OnMatchRelay = OnReceiveMatchMessage;

        // 매치 종료
        Backend.Match.OnLeaveMatchServer = (matchType) =>
        {
            Debug.Log("매치 서버 퇴장");
            isInMatch = false;
            CleanupMatch();
        };
    }

    // ============================================
    // 매칭 시작
    // ============================================

    public void StartMatchmaking()
    {
        if (isMatching)
        {
            Debug.Log("이미 매칭 중입니다");
            return;
        }

        Debug.Log("매칭 시작...");
        isMatching = true;

        // 뒤끝 매치 서버에 매칭 요청
        Backend.Match.RequestMatchMaking(matchType, matchMode, "default");
    }

    public void CancelMatchmaking()
    {
        if (!isMatching) return;

        Debug.Log("매칭 취소");
        Backend.Match.CancelMatchMaking();
        isMatching = false;
    }

    // ============================================
    // 매칭 성공 처리
    // ============================================

    private void OnMatchSuccess()
    {
        // 매치 서버 접속
        Backend.Match.JoinMatchServer(matchType);

        // 게임 매니저에 알림
        if (gameManager != null)
        {
            gameManager.OnMatchFound?.Invoke(true);
        }
    }

    private IEnumerator SendInitialDeckData()
    {
        yield return new WaitForSeconds(0.5f);

        // 내 덱 정보를 상대에게 전송
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "DECK_DATA",
            data = JsonUtility.ToJson(gameManager.currentPlayerDeck)
        };

        SendMatchMessage(packet);
    }

    // ============================================
    // 매치 메시지 송수신
    // ============================================

    private void SendMatchMessage(MatchDataPacket packet)
    {
        if (!isInMatch)
        {
            Debug.LogWarning("매치 중이 아닙니다");
            return;
        }

        string jsonData = JsonUtility.ToJson(packet);

        // 모든 세션에게 전송
        Backend.Match.SendDataToInGameRoom(jsonData);
    }

    private void OnReceiveMatchMessage(MatchRelayEventArgs args)
    {
        Debug.Log($"메시지 수신 from {args.From}");

        try
        {
            MatchDataPacket packet = JsonUtility.FromJson<MatchDataPacket>(args.BinaryUserData);
            ProcessMatchMessage(packet, args.From);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"메시지 파싱 실패: {e.Message}");
        }
    }

    private void ProcessMatchMessage(MatchDataPacket packet, SessionId from)
    {
        switch (packet.type)
        {
            case "DECK_DATA":
                OnReceiveDeckData(packet.data, from);
                break;

            case "SHOP_COMPLETE":
                OnEnemyShopComplete(from);
                break;

            case "BATTLE_READY":
                OnEnemyBattleReady(from);
                break;

            case "TURN_ACTION":
                OnReceiveTurnAction(packet.data, from);
                break;

            case "SURRENDER":
                OnEnemySurrender(from);
                break;
        }
    }

    // ============================================
    // 게임 진행 동기화
    // ============================================

    private void OnReceiveDeckData(string deckJson, SessionId from)
    {
        Debug.Log("상대 덱 데이터 수신");

        PlayerDeckData enemyDeck = JsonUtility.FromJson<PlayerDeckData>(deckJson);

        // 매치 데이터 생성
        if (gameManager.currentMatch == null)
        {
            gameManager.currentMatch = new MatchData
            {
                matchId = currentSessionId.ToString(),
                player1Id = myPlayerId,
                player2Id = from.ToString(),
                player1Deck = gameManager.currentPlayerDeck,
                player2Deck = enemyDeck,
                currentRound = 1,
                currentPhase = "Shop"
            };
        }

        // 상점 단계 시작
        gameManager.StartShopPhase();
    }

    public void NotifyShopComplete()
    {
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "SHOP_COMPLETE",
            data = JsonUtility.ToJson(gameManager.currentPlayerDeck)
        };

        SendMatchMessage(packet);
    }

    private void OnEnemyShopComplete(SessionId from)
    {
        Debug.Log("상대가 상점 완료!");
        turnReadyStatus[from] = true;

        CheckAllPlayersReady();
    }

    public void NotifyBattleReady()
    {
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "BATTLE_READY",
            data = ""
        };

        SendMatchMessage(packet);
    }

    private void OnEnemyBattleReady(SessionId from)
    {
        Debug.Log("상대가 전투 준비 완료!");
        turnReadyStatus[from] = true;

        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        // 모든 플레이어가 준비되었는지 확인
        bool allReady = true;
        foreach (var enemy in enemySessionIds)
        {
            if (!turnReadyStatus.ContainsKey(enemy) || !turnReadyStatus[enemy])
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            Debug.Log("모든 플레이어 준비 완료 - 전투 시작!");
            turnReadyStatus.Clear();

            // 전투 시작
            gameManager.StartBattlePhase();
        }
    }

    // ============================================
    // 전투 동기화
    // ============================================

    public void SendTurnAction(string actionType, string actionData)
    {
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "TURN_ACTION",
            data = JsonUtility.ToJson(new TurnActionData
            {
                actionType = actionType,
                actionData = actionData,
                turn = currentTurn
            })
        };

        SendMatchMessage(packet);
    }

    private void OnReceiveTurnAction(string data, SessionId from)
    {
        TurnActionData actionData = JsonUtility.FromJson<TurnActionData>(data);

        Debug.Log($"상대 행동 수신: {actionData.actionType}");

        // 행동 처리
        // ...
    }

    // ============================================
    // 매치 종료
    // ============================================

    public void LeaveMatch()
    {
        if (!isInMatch) return;

        Debug.Log("매치 퇴장");
        Backend.Match.LeaveGameServer();

        CleanupMatch();
    }

    public void Surrender()
    {
        if (!isInMatch) return;

        // 항복 메시지 전송
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "SURRENDER",
            data = ""
        };

        SendMatchMessage(packet);

        // 매치 종료
        LeaveMatch();
    }

    private void OnEnemySurrender(SessionId from)
    {
        Debug.Log("상대가 항복했습니다!");

        // 승리 처리
        BattleResult result = new BattleResult
        {
            isWin = true,
            damageDealt = 0,
            goldEarned = 10
        };

        gameManager.OnBattleComplete(result);
        LeaveMatch();
    }

    private void CleanupMatch()
    {
        isInMatch = false;
        currentSessionId = null;
        enemySessionIds.Clear();
        turnReadyStatus.Clear();
        currentTurn = 0;
    }

    // ============================================
    // 유틸리티
    // ============================================

    void Update()
    {
        // 매치 서버 Poll
        if (isInMatch)
        {
            Backend.Match.Poll();
        }
    }

    void OnApplicationQuit()
    {
        if (isInMatch)
        {
            LeaveMatch();
        }
    }*/
}

// ============================================
// 매치 데이터 패킷
// ============================================

[System.Serializable]
public class MatchDataPacket
{
    public string type;
    public string data;
}

[System.Serializable]
public class TurnActionData
{
    public string actionType;
    public string actionData;
    public int turn;
}