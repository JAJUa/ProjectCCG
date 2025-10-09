using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using BackEnd.Tcp;
using LitJson;

public class RealtimeMatchSystem : MonoBehaviour
{
    /*private BackendGameManager gameManager;

    // ��ġ Ÿ�� ����
    private MatchType matchType = MatchType.turngame;
    private MatchModeType matchMode = MatchModeType.Random;

    // ��Ī ����
    private bool isMatching = false;
    private bool isInMatch = false;

    // ��ġ ����
    private SessionId currentSessionId;
    private string myPlayerId;
    private List<SessionId> enemySessionIds = new List<SessionId>();

    // �� ����ȭ
    private int currentTurn = 0;
    private Dictionary<SessionId, bool> turnReadyStatus = new Dictionary<SessionId, bool>();

    void Start()
    {
        gameManager = BackendGameManager.Instance;
        myPlayerId = Backend.UserInDate;

        // ��ġ ���� �ڵ鷯 ���
        RegisterMatchHandlers();
    }

    // ============================================
    // ��ġ ���� �ڵ鷯 ���
    // ============================================

    private void RegisterMatchHandlers()
    {
        // ��Ī ���� �ڵ鷯
        Backend.Match.OnMatchSuccess = (matchType, sessionId) =>
        {
            Debug.Log($"��Ī ����! SessionId: {sessionId}");
            currentSessionId = sessionId;
            isMatching = false;
            isInMatch = true;

            OnMatchSuccess();
        };

        // ��Ī ��� �ڵ鷯
        Backend.Match.OnMatchCancel = (reason) =>
        {
            Debug.Log($"��Ī ���: {reason}");
            isMatching = false;
            isInMatch = false;
        };

        // ��ġ �� ���� �ڵ鷯
        Backend.Match.OnSessionJoinInServer = (sessionId) =>
        {
            Debug.Log("��ġ �� ����!");
            StartCoroutine(SendInitialDeckData());
        };

        // ���� ����Ʈ ������Ʈ
        Backend.Match.OnSessionListInServer = (sessionList) =>
        {
            Debug.Log($"���� ����Ʈ ����: {sessionList.Count}��");

            enemySessionIds.Clear();
            foreach (var session in sessionList)
            {
                if (session != currentSessionId)
                {
                    enemySessionIds.Add(session);
                }
            }
        };

        // ��ġ �޽��� ����
        Backend.Match.OnMatchRelay = OnReceiveMatchMessage;

        // ��ġ ����
        Backend.Match.OnLeaveMatchServer = (matchType) =>
        {
            Debug.Log("��ġ ���� ����");
            isInMatch = false;
            CleanupMatch();
        };
    }

    // ============================================
    // ��Ī ����
    // ============================================

    public void StartMatchmaking()
    {
        if (isMatching)
        {
            Debug.Log("�̹� ��Ī ���Դϴ�");
            return;
        }

        Debug.Log("��Ī ����...");
        isMatching = true;

        // �ڳ� ��ġ ������ ��Ī ��û
        Backend.Match.RequestMatchMaking(matchType, matchMode, "default");
    }

    public void CancelMatchmaking()
    {
        if (!isMatching) return;

        Debug.Log("��Ī ���");
        Backend.Match.CancelMatchMaking();
        isMatching = false;
    }

    // ============================================
    // ��Ī ���� ó��
    // ============================================

    private void OnMatchSuccess()
    {
        // ��ġ ���� ����
        Backend.Match.JoinMatchServer(matchType);

        // ���� �Ŵ����� �˸�
        if (gameManager != null)
        {
            gameManager.OnMatchFound?.Invoke(true);
        }
    }

    private IEnumerator SendInitialDeckData()
    {
        yield return new WaitForSeconds(0.5f);

        // �� �� ������ ��뿡�� ����
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "DECK_DATA",
            data = JsonUtility.ToJson(gameManager.currentPlayerDeck)
        };

        SendMatchMessage(packet);
    }

    // ============================================
    // ��ġ �޽��� �ۼ���
    // ============================================

    private void SendMatchMessage(MatchDataPacket packet)
    {
        if (!isInMatch)
        {
            Debug.LogWarning("��ġ ���� �ƴմϴ�");
            return;
        }

        string jsonData = JsonUtility.ToJson(packet);

        // ��� ���ǿ��� ����
        Backend.Match.SendDataToInGameRoom(jsonData);
    }

    private void OnReceiveMatchMessage(MatchRelayEventArgs args)
    {
        Debug.Log($"�޽��� ���� from {args.From}");

        try
        {
            MatchDataPacket packet = JsonUtility.FromJson<MatchDataPacket>(args.BinaryUserData);
            ProcessMatchMessage(packet, args.From);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�޽��� �Ľ� ����: {e.Message}");
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
    // ���� ���� ����ȭ
    // ============================================

    private void OnReceiveDeckData(string deckJson, SessionId from)
    {
        Debug.Log("��� �� ������ ����");

        PlayerDeckData enemyDeck = JsonUtility.FromJson<PlayerDeckData>(deckJson);

        // ��ġ ������ ����
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

        // ���� �ܰ� ����
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
        Debug.Log("��밡 ���� �Ϸ�!");
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
        Debug.Log("��밡 ���� �غ� �Ϸ�!");
        turnReadyStatus[from] = true;

        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        // ��� �÷��̾ �غ�Ǿ����� Ȯ��
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
            Debug.Log("��� �÷��̾� �غ� �Ϸ� - ���� ����!");
            turnReadyStatus.Clear();

            // ���� ����
            gameManager.StartBattlePhase();
        }
    }

    // ============================================
    // ���� ����ȭ
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

        Debug.Log($"��� �ൿ ����: {actionData.actionType}");

        // �ൿ ó��
        // ...
    }

    // ============================================
    // ��ġ ����
    // ============================================

    public void LeaveMatch()
    {
        if (!isInMatch) return;

        Debug.Log("��ġ ����");
        Backend.Match.LeaveGameServer();

        CleanupMatch();
    }

    public void Surrender()
    {
        if (!isInMatch) return;

        // �׺� �޽��� ����
        MatchDataPacket packet = new MatchDataPacket
        {
            type = "SURRENDER",
            data = ""
        };

        SendMatchMessage(packet);

        // ��ġ ����
        LeaveMatch();
    }

    private void OnEnemySurrender(SessionId from)
    {
        Debug.Log("��밡 �׺��߽��ϴ�!");

        // �¸� ó��
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
    // ��ƿ��Ƽ
    // ============================================

    void Update()
    {
        // ��ġ ���� Poll
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
// ��ġ ������ ��Ŷ
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