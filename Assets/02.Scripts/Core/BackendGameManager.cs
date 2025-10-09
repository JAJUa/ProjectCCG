using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

public class BackendGameManager : MonoBehaviour
{
    public static BackendGameManager Instance { get; private set; }
    
    // 현재 플레이어 정보
    public PlayerDeckData currentPlayerDeck;
    public string myPlayerId;
    
    // 매칭 정보
    public MatchData currentMatch;
    public bool isMatching = false;
    
    // 게임 설정
    public GameConfig config = new GameConfig();
    
    // 이벤트
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
    // 뒤끝 초기화
    // ============================================
    
    private void InitializeBackend()
    {
        var bro = Backend.Initialize();
        
        if(bro.IsSuccess())
        {
            Debug.Log("뒤끝 초기화 성공");
            
            // 로그인 후 플레이어 데이터 로드
            if(Backend.IsLogin)
            {
                myPlayerId = Backend.UserInDate;
                LoadPlayerDeck();
            }
        }
        else
        {
            Debug.LogError("뒤끝 초기화 실패: " + bro);
        }
    }

    // ============================================
    // 플레이어 덱 데이터 관리
    // ============================================
    
    public void LoadPlayerDeck()
    {
        // 뒤끝 게임정보에서 덱 데이터 불러오기
        var bro = Backend.GameData.GetMyData("PlayerDeck", new Where());

        if (bro.IsSuccess())
        {
            Debug.Log("덱 데이터 로드 성공");
            
            if(bro.GetReturnValuetoJSON()["rows"].Count > 0)
            {
                JsonData data = bro.GetReturnValuetoJSON()["rows"][0];
                string deckJson = data["deckData"]["S"].ToString();
                currentPlayerDeck = JsonUtility.FromJson<PlayerDeckData>(deckJson);
            }
            else
            {
                // 새 플레이어 - 기본 덱 생성
                CreateNewPlayerDeck();
            }
        }
        else
        {
            Debug.Log("덱 데이터 없음 - 새로 생성");
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
        
        // 시작 캐릭터 지급 (예시)
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
            Debug.Log("덱 저장 성공");
        }
        else
        {
            Debug.LogError("덱 저장 실패: " + bro);
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
            Debug.Log("덱 업데이트 성공");
        }
        else
        {
            Debug.LogError("덱 업데이트 실패: " + bro);
        }*/
    }

    // ============================================
    // PVP 매칭 시스템 (뒤끝 매치 사용)
    // ============================================
    
    public void StartMatchmaking()
    {
        if(isMatching)
        {
            Debug.Log("이미 매칭 중입니다");
            return;
        }
        
        isMatching = true;
        Debug.Log("매칭 시작...");
        
        // 뒤끝 매치 서버를 사용한 매칭
        // 실제 구현시에는 Backend.Match 기능 사용
        // 여기서는 간단한 구현 예시
        
        // 임시: 랜덤 매칭 시뮬레이션
        StartCoroutine(SimulateMatching());
    }
    
    private System.Collections.IEnumerator SimulateMatching()
    {
        yield return new UnityEngine.WaitForSeconds(2f);
        
        // 매칭 성공 (임시 AI 상대)
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
        
        Debug.Log("매칭 성공! 게임 시작");
    }
    
    private PlayerDeckData CreateAIDeck()
    {
        // AI 덱 생성 (간단한 예시)
        PlayerDeckData aiDeck = new PlayerDeckData
        {
            playerId = "AI_Player",
            gold = config.startGold,
            health = config.startHealth,
            round = 1
        };
        
        // AI 기본 캐릭터 추가
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
        Debug.Log("매칭 취소");
    }

    // ============================================
    // 게임 진행 관리
    // ============================================
    
    public void StartShopPhase()
    {
        if(currentMatch == null) return;
        
        currentMatch.currentPhase = "Shop";
        Debug.Log($"라운드 {currentMatch.currentRound} - 상점 단계 시작");
        
        OnShopPhaseStart?.Invoke();
    }
    
    public void StartBattlePhase()
    {
        if(currentMatch == null) return;
        
        currentMatch.currentPhase = "Battle";
        Debug.Log($"라운드 {currentMatch.currentRound} - 전투 단계 시작");
        
        // BattleSystem으로 전투 시작
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
        
        Debug.Log($"전투 결과: {(result.isWin ? "승리" : "패배")}");
        
        // 골드 지급
        int earnedGold = result.isWin ? config.winGold : config.loseGold;
        currentPlayerDeck.gold += earnedGold;
        
        // 패배시 체력 감소
        if(!result.isWin)
        {
            currentPlayerDeck.health -= config.damagePerLoss;
            currentMatch.player2Deck.health -= config.damagePerLoss;
        }
        else
        {
            currentMatch.player2Deck.health -= result.damageDealt;
        }
        
        // 게임 종료 체크
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
        
        // 다음 라운드로
        currentMatch.currentRound++;
        currentPlayerDeck.round++;
        
        SavePlayerDeck();
        OnBattleEnd?.Invoke(result);
        
        // 다시 상점 단계로
        StartShopPhase();
    }
    
    private void EndGame(bool isWin)
    {
        Debug.Log($"게임 종료: {(isWin ? "승리!" : "패배...")}");
        
        // 승리/패배에 따른 보상 처리
        if(isWin)
        {
            currentPlayerDeck.gold += 20; // 승리 보너스
        }
        
        // 덱 초기화
        currentPlayerDeck.health = config.startHealth;
        currentPlayerDeck.round = 1;
        currentPlayerDeck.currentFormation.Clear();
        
        SavePlayerDeck();
        
        currentMatch = null;
        
        // 게임 결과 UI 표시 등...
    }

    // ============================================
    // 랭킹 시스템
    // ============================================
    
    public void UpdateRanking(int score)
    {
        Param param = new Param();
        param.Add("score", score);
        param.Add("wins", 1); // 승리 횟수 등
        
        /*var bro = Backend.GameInfo.InsertOrUpdate("Ranking", param);
        
        if(bro.IsSuccess())
        {
            Debug.Log("랭킹 업데이트 성공");
        }*/
    }
    
    public void GetTopRankings(int count = 10)
    {
        /*var bro = Backend.GameInfo.GetRankList("Ranking", count);
        
        if(bro.IsSuccess())
        {
            Debug.Log("랭킹 조회 성공");
            // 랭킹 데이터 파싱 및 표시
        }*/
    }

    // ============================================
    // 유틸리티
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
            // 신령 중복 체크
            if(character.type == CharacterType.Spirit)
            {
                // 기존 신령 제거
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