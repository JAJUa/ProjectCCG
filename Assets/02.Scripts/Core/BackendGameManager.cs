using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

public class BackendGameManager : MonoBehaviour
{
    public static BackendGameManager Instance { get; private set; }
    
    // ���� �÷��̾� ����
    public PlayerDeckData currentPlayerDeck;
    public string myPlayerId;
    
    // ��Ī ����
    public MatchData currentMatch;
    public bool isMatching = false;
    
    // ���� ����
    public GameConfig config = new GameConfig();
    
    // �̺�Ʈ
    public Action<bool> OnMatchFound;
    public Action<BattleResult> OnBattleEnd;
    public Action OnShopPhaseStart;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeBackend();
    }

    // ============================================
    // �ڳ� �ʱ�ȭ
    // ============================================
    
    private void InitializeBackend()
    {
        var bro = Backend.Initialize();
        
        if(bro.IsSuccess())
        {
            Debug.Log("�ڳ� �ʱ�ȭ ����");
            
            // �α��� �� �÷��̾� ������ �ε�
            if(Backend.IsLogin)
            {
                myPlayerId = Backend.UserInDate;
                LoadPlayerDeck();
            }
        }
        else
        {
            Debug.LogError("�ڳ� �ʱ�ȭ ����: " + bro);
        }
    }

    // ============================================
    // �÷��̾� �� ������ ����
    // ============================================
    
    public void LoadPlayerDeck()
    {
        // �ڳ� ������������ �� ������ �ҷ�����
        var bro = Backend.GameData.GetMyData("PlayerDeck", new Where());

        if (bro.IsSuccess())
        {
            Debug.Log("�� ������ �ε� ����");
            
            if(bro.GetReturnValuetoJSON()["rows"].Count > 0)
            {
                JsonData data = bro.GetReturnValuetoJSON()["rows"][0];
                string deckJson = data["deckData"]["S"].ToString();
                currentPlayerDeck = JsonUtility.FromJson<PlayerDeckData>(deckJson);
            }
            else
            {
                // �� �÷��̾� - �⺻ �� ����
                CreateNewPlayerDeck();
            }
        }
        else
        {
            Debug.Log("�� ������ ���� - ���� ����");
            CreateNewPlayerDeck();
        }
    }
    
    private void CreateNewPlayerDeck()
    {
        currentPlayerDeck = new PlayerDeckData
        {
            playerId = myPlayerId,
            gold = config.startGold,
            health = config.startHealth,
            round = 1,
            ownedCharacters = new List<CharacterData>(),
            currentFormation = new List<CharacterData>()
        };
        
        // ���� ĳ���� ���� (����)
        var startCharacter = CharacterDatabase.GetAllCharacters()[0].Clone();
        currentPlayerDeck.ownedCharacters.Add(startCharacter);
        
        SavePlayerDeck();
    }
    
    public void SavePlayerDeck()
    {
        Param param = new Param();
        param.Add("deckData", JsonUtility.ToJson(currentPlayerDeck));

        var bro = Backend.GameData.Insert("Ranking", param);

        if (bro.IsSuccess())
        {
            Debug.Log("�� ���� ����");
        }
        else
        {
            Debug.LogError("�� ���� ����: " + bro);
        }
    }
    
    public void UpdatePlayerDeck()
    {
        Where where = new Where();
        where.Equal("playerId", myPlayerId);
        
        Param param = new Param();
        param.Add("deckData", JsonUtility.ToJson(currentPlayerDeck));
        /*
        var bro = Backend.URank.User.GetRankList(count);

        if (bro.IsSuccess())
        {
            Debug.Log("�� ������Ʈ ����");
        }
        else
        {
            Debug.LogError("�� ������Ʈ ����: " + bro);
        }*/
    }

    // ============================================
    // PVP ��Ī �ý��� (�ڳ� ��ġ ���)
    // ============================================
    
    public void StartMatchmaking()
    {
        if(isMatching)
        {
            Debug.Log("�̹� ��Ī ���Դϴ�");
            return;
        }
        
        isMatching = true;
        Debug.Log("��Ī ����...");
        
        // �ڳ� ��ġ ������ ����� ��Ī
        // ���� �����ÿ��� Backend.Match ��� ���
        // ���⼭�� ������ ���� ����
        
        // �ӽ�: ���� ��Ī �ùķ��̼�
        StartCoroutine(SimulateMatching());
    }
    
    private System.Collections.IEnumerator SimulateMatching()
    {
        yield return new UnityEngine.WaitForSeconds(2f);
        
        // ��Ī ���� (�ӽ� AI ���)
        CreateMatchWithAI();
        
        isMatching = false;
        OnMatchFound?.Invoke(true);
    }
    
    private void CreateMatchWithAI()
    {
        currentMatch = new MatchData
        {
            matchId = System.Guid.NewGuid().ToString(),
            player1Id = myPlayerId,
            player2Id = "AI_Player",
            player1Deck = currentPlayerDeck,
            player2Deck = CreateAIDeck(),
            currentRound = 1,
            currentPhase = "Shop"
        };
        
        Debug.Log("��Ī ����! ���� ����");
    }
    
    private PlayerDeckData CreateAIDeck()
    {
        // AI �� ���� (������ ����)
        PlayerDeckData aiDeck = new PlayerDeckData
        {
            playerId = "AI_Player",
            gold = config.startGold,
            health = config.startHealth,
            round = 1
        };
        
        // AI �⺻ ĳ���� �߰�
        var characters = CharacterDatabase.GetAllCharacters();
        for(int i = 0; i < 3; i++)
        {
            var randomChar = characters[UnityEngine.Random.Range(0, characters.Count)].Clone();
            aiDeck.currentFormation.Add(randomChar);
        }
        
        return aiDeck;
    }
    
    public void CancelMatchmaking()
    {
        isMatching = false;
        Debug.Log("��Ī ���");
    }

    // ============================================
    // ���� ���� ����
    // ============================================
    
    public void StartShopPhase()
    {
        if(currentMatch == null) return;
        
        currentMatch.currentPhase = "Shop";
        Debug.Log($"���� {currentMatch.currentRound} - ���� �ܰ� ����");
        
        OnShopPhaseStart?.Invoke();
    }
    
    public void StartBattlePhase()
    {
        if(currentMatch == null) return;
        
        currentMatch.currentPhase = "Battle";
        Debug.Log($"���� {currentMatch.currentRound} - ���� �ܰ� ����");
        
        // BattleSystem���� ���� ����
        BattleSystem battleSystem = GetComponent<BattleSystem>();
        if(battleSystem != null)
        {
            battleSystem.StartBattle(
                currentMatch.player1Deck.currentFormation,
                currentMatch.player2Deck.currentFormation
            );
        }
    }
    
    public void OnBattleComplete(BattleResult result)
    {
        if(currentMatch == null) return;
        
        Debug.Log($"���� ���: {(result.isWin ? "�¸�" : "�й�")}");
        
        // ��� ����
        int earnedGold = result.isWin ? config.winGold : config.loseGold;
        currentPlayerDeck.gold += earnedGold;
        
        // �й�� ü�� ����
        if(!result.isWin)
        {
            currentPlayerDeck.health -= config.damagePerLoss;
            currentMatch.player2Deck.health -= config.damagePerLoss;
        }
        else
        {
            currentMatch.player2Deck.health -= result.damageDealt;
        }
        
        // ���� ���� üũ
        if(currentPlayerDeck.health <= 0)
        {
            EndGame(false);
            return;
        }
        else if(currentMatch.player2Deck.health <= 0)
        {
            EndGame(true);
            return;
        }
        
        // ���� �����
        currentMatch.currentRound++;
        currentPlayerDeck.round++;
        
        SavePlayerDeck();
        OnBattleEnd?.Invoke(result);
        
        // �ٽ� ���� �ܰ��
        StartShopPhase();
    }
    
    private void EndGame(bool isWin)
    {
        Debug.Log($"���� ����: {(isWin ? "�¸�!" : "�й�...")}");
        
        // �¸�/�й迡 ���� ���� ó��
        if(isWin)
        {
            currentPlayerDeck.gold += 20; // �¸� ���ʽ�
        }
        
        // �� �ʱ�ȭ
        currentPlayerDeck.health = config.startHealth;
        currentPlayerDeck.round = 1;
        currentPlayerDeck.currentFormation.Clear();
        
        SavePlayerDeck();
        
        currentMatch = null;
        
        // ���� ��� UI ǥ�� ��...
    }

    // ============================================
    // ��ŷ �ý���
    // ============================================
    
    public void UpdateRanking(int score)
    {
        Param param = new Param();
        param.Add("score", score);
        param.Add("wins", 1); // �¸� Ƚ�� ��
        
        /*var bro = Backend.GameInfo.InsertOrUpdate("Ranking", param);
        
        if(bro.IsSuccess())
        {
            Debug.Log("��ŷ ������Ʈ ����");
        }*/
    }
    
    public void GetTopRankings(int count = 10)
    {
        /*var bro = Backend.GameInfo.GetRankList("Ranking", count);
        
        if(bro.IsSuccess())
        {
            Debug.Log("��ŷ ��ȸ ����");
            // ��ŷ ������ �Ľ� �� ǥ��
        }*/
    }

    // ============================================
    // ��ƿ��Ƽ
    // ============================================
    
    public void AddGold(int amount)
    {
        currentPlayerDeck.gold += amount;
        UpdatePlayerDeck();
    }
    
    public bool SpendGold(int amount)
    {
        if(currentPlayerDeck.gold >= amount)
        {
            currentPlayerDeck.gold -= amount;
            UpdatePlayerDeck();
            return true;
        }
        return false;
    }
    
    public bool CanAddToFormation()
    {
        return currentPlayerDeck.currentFormation.Count < config.maxFormationSlots;
    }
    
    public void AddToFormation(CharacterData character)
    {
        if(CanAddToFormation())
        {
            // �ŷ� �ߺ� üũ
            if(character.type == CharacterType.Spirit)
            {
                // ���� �ŷ� ����
                currentPlayerDeck.currentFormation.RemoveAll(c => c.type == CharacterType.Spirit);
            }
            
            currentPlayerDeck.currentFormation.Add(character);
            UpdatePlayerDeck();
        }
    }
    
    public void RemoveFromFormation(int index)
    {
        if(index >= 0 && index < currentPlayerDeck.currentFormation.Count)
        {
            currentPlayerDeck.currentFormation.RemoveAt(index);
            UpdatePlayerDeck();
        }
    }
}