using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("로비 UI")]
    public GameObject lobbyPanel;
    public Button startMatchButton;
    public Text playerGoldText;
    public Text playerHealthText;
    public Text playerRoundText;

    [Header("상점 UI")]
    public GameObject shopPanel;
    public Transform shopCardContainer;
    public GameObject cardPrefab;
    public Button refreshShopButton;
    public Button startBattleButton;
    public Text shopGoldText;

    [Header("덱 편성 UI")]
    public Transform formationContainer;
    public GameObject formationSlotPrefab;
    public Text formationInfoText;

    [Header("전투 UI")]
    public GameObject battlePanel;
    public Transform playerBattleContainer;
    public Transform enemyBattleContainer;
    public GameObject battleUnitPrefab;
    public Text battleLogText;
    public ScrollRect battleLogScrollRect;

    [Header("결과 UI")]
    public GameObject resultPanel;
    public Text resultText;
    public Button nextRoundButton;
    public Button returnToLobbyButton;

    // 매니저 참조
    private BackendGameManager gameManager;
    private ShopSystem shopSystem;
    private BattleSystem battleSystem;

    // 현재 표시된 카드 UI들
    private List<GameObject> currentShopCards = new List<GameObject>();
    private List<GameObject> currentFormationSlots = new List<GameObject>();
    private List<GameObject> currentBattleUnits = new List<GameObject>();

    void Start()
    {
        // 매니저 참조
        gameManager = BackendGameManager.Instance;
        shopSystem = GetComponent<ShopSystem>();
        battleSystem = GetComponent<BattleSystem>();

        // 버튼 이벤트 연결
        startMatchButton.onClick.AddListener(OnStartMatchClicked);
        refreshShopButton.onClick.AddListener(OnRefreshShopClicked);
        startBattleButton.onClick.AddListener(OnStartBattleClicked);
        nextRoundButton.onClick.AddListener(OnNextRoundClicked);
        returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

        // 게임 이벤트 구독
        if (gameManager != null)
        {
            gameManager.OnMatchFound += OnMatchFound;
            gameManager.OnShopPhaseStart += OnShopPhaseStart;
            gameManager.OnBattleEnd += OnBattleEnd;
        }

        // 초기 화면 설정
        ShowLobby();
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnMatchFound -= OnMatchFound;
            gameManager.OnShopPhaseStart -= OnShopPhaseStart;
            gameManager.OnBattleEnd -= OnBattleEnd;
        }
    }

    void Update()
    {
        // 실시간 정보 업데이트
        UpdatePlayerInfo();
    }

    // ============================================
    // 화면 전환
    // ============================================

    private void ShowLobby()
    {
        lobbyPanel.SetActive(true);
        shopPanel.SetActive(false);
        battlePanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    private void ShowShop()
    {
        lobbyPanel.SetActive(false);
        shopPanel.SetActive(true);
        battlePanel.SetActive(false);
        resultPanel.SetActive(false);

        UpdateShopUI();
        UpdateFormationUI();
    }

    private void ShowBattle()
    {
        lobbyPanel.SetActive(false);
        shopPanel.SetActive(false);
        battlePanel.SetActive(true);
        resultPanel.SetActive(false);

        UpdateBattleUI();
    }

    private void ShowResult(BattleResult result)
    {
        resultPanel.SetActive(true);

        string resultMessage = result.isWin ? "승리!" : "패배...";
        resultMessage += $"\n데미지: {result.damageDealt}";
        resultMessage += $"\n획득 골드: {result.goldEarned}";
        resultMessage += $"\n\n현재 체력: {gameManager.currentPlayerDeck.health}";

        resultText.text = resultMessage;
    }

    // ============================================
    // 플레이어 정보 업데이트
    // ============================================

    private void UpdatePlayerInfo()
    {
        if (gameManager == null || gameManager.currentPlayerDeck == null) return;

        playerGoldText.text = $"골드: {gameManager.currentPlayerDeck.gold}";
        playerHealthText.text = $"체력: {gameManager.currentPlayerDeck.health}";
        playerRoundText.text = $"라운드: {gameManager.currentPlayerDeck.round}";

        shopGoldText.text = $"골드: {gameManager.currentPlayerDeck.gold}";
    }

    // ============================================
    // 로비 UI
    // ============================================

    private void OnStartMatchClicked()
    {
        Debug.Log("매칭 시작!");
        startMatchButton.interactable = false;
        gameManager.StartMatchmaking();
    }

    private void OnMatchFound(bool success)
    {
        if (success)
        {
            Debug.Log("매칭 성공!");
            gameManager.StartShopPhase();
        }
        else
        {
            Debug.Log("매칭 실패");
            startMatchButton.interactable = true;
        }
    }

    // ============================================
    // 상점 UI
    // ============================================

    private void OnShopPhaseStart()
    {
        ShowShop();
    }

    private void UpdateShopUI()
    {
        // 기존 카드 UI 제거
        foreach (var card in currentShopCards)
        {
            Destroy(card);
        }
        currentShopCards.Clear();

        ShopData shop = shopSystem.GetCurrentShop();
        if (shop == null) return;

        // 상점 카드 생성
        for (int i = 0; i < shop.availableCharacters.Count; i++)
        {
            CharacterData character = shop.availableCharacters[i];
            GameObject cardObj = Instantiate(cardPrefab, shopCardContainer);

            // 카드 정보 표시
            Text cardText = cardObj.GetComponentInChildren<Text>();
            if (cardText != null)
            {
                cardText.text = $"{character.name}\n" +
                              $"공격: {character.baseAttack} 체력: {character.baseHealth} 속도: {character.baseSpeed}\n" +
                              $"비용: {character.buyCost}G";
            }

            // 구매 버튼
            Button buyButton = cardObj.GetComponentInChildren<Button>();
            if (buyButton != null)
            {
                int index = i; // 클로저 문제 방지
                buyButton.onClick.AddListener(() => OnBuyCharacterClicked(index));
            }

            currentShopCards.Add(cardObj);
        }

        // 신령 카드 (2라운드 이후)
        if (shop.availableSpirit != null)
        {
            // 신령 카드 UI 생성 (별도 위치에)
            // ...
        }
    }

    private void OnBuyCharacterClicked(int slotIndex)
    {
        if (shopSystem.BuyCharacter(slotIndex))
        {
            Debug.Log("캐릭터 구매 성공!");
            UpdateShopUI();
            UpdateFormationUI();
        }
    }

    private void OnRefreshShopClicked()
    {
        if (shopSystem.RefreshShop())
        {
            UpdateShopUI();
        }
    }

    // ============================================
    // 덱 편성 UI
    // ============================================

    private void UpdateFormationUI()
    {
        // 기존 슬롯 제거
        foreach (var slot in currentFormationSlots)
        {
            Destroy(slot);
        }
        currentFormationSlots.Clear();

        // 편성 슬롯 생성 (최대 6개)
        for (int i = 0; i < gameManager.config.maxFormationSlots; i++)
        {
            GameObject slotObj = Instantiate(formationSlotPrefab, formationContainer);

            if (i < gameManager.currentPlayerDeck.currentFormation.Count)
            {
                CharacterData character = gameManager.currentPlayerDeck.currentFormation[i];

                Text slotText = slotObj.GetComponentInChildren<Text>();
                if (slotText != null)
                {
                    slotText.text = $"{character.name}\n" +
                                   $"ATK: {character.currentAttack} HP: {character.currentHealth} SPD: {character.currentSpeed}";
                }

                // 제거 버튼
                Button removeButton = slotObj.GetComponentInChildren<Button>();
                if (removeButton != null)
                {
                    int index = i;
                    removeButton.onClick.AddListener(() => OnRemoveFromFormationClicked(index));
                }
            }
            else
            {
                // 빈 슬롯
                Text slotText = slotObj.GetComponentInChildren<Text>();
                if (slotText != null)
                {
                    slotText.text = "빈 슬롯";
                }
            }

            currentFormationSlots.Add(slotObj);
        }

        // 소유 캐릭터 목록 (간단 버전)
        int formationCount = gameManager.currentPlayerDeck.currentFormation.Count;
        int ownedCount = gameManager.currentPlayerDeck.ownedCharacters.Count;
        formationInfoText.text = $"편성: {formationCount}/{gameManager.config.maxFormationSlots}\n보유: {ownedCount}";
    }

    private void OnRemoveFromFormationClicked(int index)
    {
        gameManager.RemoveFromFormation(index);
        UpdateFormationUI();
    }

    private void OnStartBattleClicked()
    {
        if (gameManager.currentPlayerDeck.currentFormation.Count == 0)
        {
            Debug.Log("캐릭터를 배치해주세요!");
            return;
        }

        gameManager.StartBattlePhase();
        ShowBattle();
    }

    // ============================================
    // 전투 UI
    // ============================================

    private void UpdateBattleUI()
    {
        // 기존 유닛 제거
        foreach (var unit in currentBattleUnits)
        {
            Destroy(unit);
        }
        currentBattleUnits.Clear();

        // 플레이어 팀
        foreach (var character in gameManager.currentMatch.player1Deck.currentFormation)
        {
            GameObject unitObj = Instantiate(battleUnitPrefab, playerBattleContainer);

            Text unitText = unitObj.GetComponentInChildren<Text>();
            if (unitText != null)
            {
                unitText.text = $"{character.name}\nHP: {character.currentHealth}";
            }

            currentBattleUnits.Add(unitObj);
        }

        // 적 팀
        foreach (var character in gameManager.currentMatch.player2Deck.currentFormation)
        {
            GameObject unitObj = Instantiate(battleUnitPrefab, enemyBattleContainer);

            Text unitText = unitObj.GetComponentInChildren<Text>();
            if (unitText != null)
            {
                unitText.text = $"{character.name}\nHP: {character.currentHealth}";
            }

            currentBattleUnits.Add(unitObj);
        }

        battleLogText.text = "전투 시작!";
    }

    public void AddBattleLog(string message)
    {
        battleLogText.text += "\n" + message;

        // 스크롤을 아래로
        Canvas.ForceUpdateCanvases();
        battleLogScrollRect.verticalNormalizedPosition = 0f;
    }

    // ============================================
    // 결과 UI
    // ============================================

    private void OnBattleEnd(BattleResult result)
    {
        ShowResult(result);
    }

    private void OnNextRoundClicked()
    {
        resultPanel.SetActive(false);
        ShowShop();
    }

    private void OnReturnToLobbyClicked()
    {
        gameManager.currentMatch = null;
        ShowLobby();
        startMatchButton.interactable = true;
    }

    // ============================================
    // 카드 상세 정보 (추가 기능)
    // ============================================

    public void ShowCharacterDetail(CharacterData character)
    {
        // 캐릭터 상세 정보 팝업 표시
        string detail = $"이름: {character.name}\n" +
                       $"타입: {character.type}\n" +
                       $"진화: {character.evolutionType}\n" +
                       $"공격력: {character.currentAttack}\n" +
                       $"체력: {character.currentHealth}\n" +
                       $"속도: {character.currentSpeed}\n" +
                       $"스킬: {character.skillDescription}\n";

        if (character.attributes.Count > 0)
        {
            detail += $"속성: {string.Join(", ", character.attributes)}";
        }

        Debug.Log(detail);
        // 실제로는 팝업 UI에 표시
    }
}