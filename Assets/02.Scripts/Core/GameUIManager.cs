using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("�κ� UI")]
    public GameObject lobbyPanel;
    public Button startMatchButton;
    public Text playerGoldText;
    public Text playerHealthText;
    public Text playerRoundText;

    [Header("���� UI")]
    public GameObject shopPanel;
    public Transform shopCardContainer;
    public GameObject cardPrefab;
    public Button refreshShopButton;
    public Button startBattleButton;
    public Text shopGoldText;

    [Header("�� �� UI")]
    public Transform formationContainer;
    public GameObject formationSlotPrefab;
    public Text formationInfoText;

    [Header("���� UI")]
    public GameObject battlePanel;
    public Transform playerBattleContainer;
    public Transform enemyBattleContainer;
    public GameObject battleUnitPrefab;
    public Text battleLogText;
    public ScrollRect battleLogScrollRect;

    [Header("��� UI")]
    public GameObject resultPanel;
    public Text resultText;
    public Button nextRoundButton;
    public Button returnToLobbyButton;

    // �Ŵ��� ����
    private BackendGameManager gameManager;
    private ShopSystem shopSystem;
    private BattleSystem battleSystem;

    // ���� ǥ�õ� ī�� UI��
    private List<GameObject> currentShopCards = new List<GameObject>();
    private List<GameObject> currentFormationSlots = new List<GameObject>();
    private List<GameObject> currentBattleUnits = new List<GameObject>();

    void Start()
    {
        // �Ŵ��� ����
        gameManager = BackendGameManager.Instance;
        shopSystem = GetComponent<ShopSystem>();
        battleSystem = GetComponent<BattleSystem>();

        // ��ư �̺�Ʈ ����
        startMatchButton.onClick.AddListener(OnStartMatchClicked);
        refreshShopButton.onClick.AddListener(OnRefreshShopClicked);
        startBattleButton.onClick.AddListener(OnStartBattleClicked);
        nextRoundButton.onClick.AddListener(OnNextRoundClicked);
        returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

        // ���� �̺�Ʈ ����
        if (gameManager != null)
        {
            gameManager.OnMatchFound += OnMatchFound;
            gameManager.OnShopPhaseStart += OnShopPhaseStart;
            gameManager.OnBattleEnd += OnBattleEnd;
        }

        // �ʱ� ȭ�� ����
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
        // �ǽð� ���� ������Ʈ
        UpdatePlayerInfo();
    }

    // ============================================
    // ȭ�� ��ȯ
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

        string resultMessage = result.isWin ? "�¸�!" : "�й�...";
        resultMessage += $"\n������: {result.damageDealt}";
        resultMessage += $"\nȹ�� ���: {result.goldEarned}";
        resultMessage += $"\n\n���� ü��: {gameManager.currentPlayerDeck.health}";

        resultText.text = resultMessage;
    }

    // ============================================
    // �÷��̾� ���� ������Ʈ
    // ============================================

    private void UpdatePlayerInfo()
    {
        if (gameManager == null || gameManager.currentPlayerDeck == null) return;

        playerGoldText.text = $"���: {gameManager.currentPlayerDeck.gold}";
        playerHealthText.text = $"ü��: {gameManager.currentPlayerDeck.health}";
        playerRoundText.text = $"����: {gameManager.currentPlayerDeck.round}";

        shopGoldText.text = $"���: {gameManager.currentPlayerDeck.gold}";
    }

    // ============================================
    // �κ� UI
    // ============================================

    private void OnStartMatchClicked()
    {
        Debug.Log("��Ī ����!");
        startMatchButton.interactable = false;
        gameManager.StartMatchmaking();
    }

    private void OnMatchFound(bool success)
    {
        if (success)
        {
            Debug.Log("��Ī ����!");
            gameManager.StartShopPhase();
        }
        else
        {
            Debug.Log("��Ī ����");
            startMatchButton.interactable = true;
        }
    }

    // ============================================
    // ���� UI
    // ============================================

    private void OnShopPhaseStart()
    {
        ShowShop();
    }

    private void UpdateShopUI()
    {
        // ���� ī�� UI ����
        foreach (var card in currentShopCards)
        {
            Destroy(card);
        }
        currentShopCards.Clear();

        ShopData shop = shopSystem.GetCurrentShop();
        if (shop == null) return;

        // ���� ī�� ����
        for (int i = 0; i < shop.availableCharacters.Count; i++)
        {
            CharacterData character = shop.availableCharacters[i];
            GameObject cardObj = Instantiate(cardPrefab, shopCardContainer);

            // ī�� ���� ǥ��
            Text cardText = cardObj.GetComponentInChildren<Text>();
            if (cardText != null)
            {
                cardText.text = $"{character.name}\n" +
                              $"����: {character.baseAttack} ü��: {character.baseHealth} �ӵ�: {character.baseSpeed}\n" +
                              $"���: {character.buyCost}G";
            }

            // ���� ��ư
            Button buyButton = cardObj.GetComponentInChildren<Button>();
            if (buyButton != null)
            {
                int index = i; // Ŭ���� ���� ����
                buyButton.onClick.AddListener(() => OnBuyCharacterClicked(index));
            }

            currentShopCards.Add(cardObj);
        }

        // �ŷ� ī�� (2���� ����)
        if (shop.availableSpirit != null)
        {
            // �ŷ� ī�� UI ���� (���� ��ġ��)
            // ...
        }
    }

    private void OnBuyCharacterClicked(int slotIndex)
    {
        if (shopSystem.BuyCharacter(slotIndex))
        {
            Debug.Log("ĳ���� ���� ����!");
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
    // �� �� UI
    // ============================================

    private void UpdateFormationUI()
    {
        // ���� ���� ����
        foreach (var slot in currentFormationSlots)
        {
            Destroy(slot);
        }
        currentFormationSlots.Clear();

        // �� ���� ���� (�ִ� 6��)
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

                // ���� ��ư
                Button removeButton = slotObj.GetComponentInChildren<Button>();
                if (removeButton != null)
                {
                    int index = i;
                    removeButton.onClick.AddListener(() => OnRemoveFromFormationClicked(index));
                }
            }
            else
            {
                // �� ����
                Text slotText = slotObj.GetComponentInChildren<Text>();
                if (slotText != null)
                {
                    slotText.text = "�� ����";
                }
            }

            currentFormationSlots.Add(slotObj);
        }

        // ���� ĳ���� ��� (���� ����)
        int formationCount = gameManager.currentPlayerDeck.currentFormation.Count;
        int ownedCount = gameManager.currentPlayerDeck.ownedCharacters.Count;
        formationInfoText.text = $"��: {formationCount}/{gameManager.config.maxFormationSlots}\n����: {ownedCount}";
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
            Debug.Log("ĳ���͸� ��ġ���ּ���!");
            return;
        }

        gameManager.StartBattlePhase();
        ShowBattle();
    }

    // ============================================
    // ���� UI
    // ============================================

    private void UpdateBattleUI()
    {
        // ���� ���� ����
        foreach (var unit in currentBattleUnits)
        {
            Destroy(unit);
        }
        currentBattleUnits.Clear();

        // �÷��̾� ��
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

        // �� ��
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

        battleLogText.text = "���� ����!";
    }

    public void AddBattleLog(string message)
    {
        battleLogText.text += "\n" + message;

        // ��ũ���� �Ʒ���
        Canvas.ForceUpdateCanvases();
        battleLogScrollRect.verticalNormalizedPosition = 0f;
    }

    // ============================================
    // ��� UI
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
    // ī�� �� ���� (�߰� ���)
    // ============================================

    public void ShowCharacterDetail(CharacterData character)
    {
        // ĳ���� �� ���� �˾� ǥ��
        string detail = $"�̸�: {character.name}\n" +
                       $"Ÿ��: {character.type}\n" +
                       $"��ȭ: {character.evolutionType}\n" +
                       $"���ݷ�: {character.currentAttack}\n" +
                       $"ü��: {character.currentHealth}\n" +
                       $"�ӵ�: {character.currentSpeed}\n" +
                       $"��ų: {character.skillDescription}\n";

        if (character.attributes.Count > 0)
        {
            detail += $"�Ӽ�: {string.Join(", ", character.attributes)}";
        }

        Debug.Log(detail);
        // �����δ� �˾� UI�� ǥ��
    }
}