using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using SpiritAge.Core.Enums;
using SpiritAge.Units;
using SpiritAge.Battle;
using SpiritAge.Utility;

namespace SpiritAge.Core
{
    /// <summary>
    /// �鿣�� ���� ���� �Ŵ���
    /// </summary>
    public class BackendGameManager : AbstractSingleton<BackendGameManager>
    {
        [Header("Game Configuration")]
        [SerializeField] private int startingGold = 10;
        [SerializeField] private int startingHealth = 100;
        [SerializeField] private int maxFormationSlots = 6;
        [SerializeField] private int victoryGold = 5;
        [SerializeField] private int defeatGold = 3;
        [SerializeField] private int damageOnDefeat = 10;

        // Player Data
        public PlayerDeck CurrentPlayerDeck { get; private set; }
        public int CurrentRound { get; private set; }

        // Game State
        public GamePhase CurrentPhase { get; private set; }

        // Events
        public event Action OnShopPhaseStart;
        public event Action OnFormationPhaseStart;
        public event Action OnBattlePhaseStart;
        public event Action<BattleResult> OnBattleComplete;
        public event Action<int> OnGoldChanged;
        public event Action<int> OnHealthChanged;

        protected override void OnSingletonAwake()
        {
            InitializeBackend();
            InitializePlayerData();

            // Subscribe to battle events
            BattleManager.Instance.OnBattleEnd += HandleBattleEnd;
        }

        /// <summary>
        /// �鿣�� �ʱ�ȭ
        /// </summary>
        private void InitializeBackend()
        {
            Backend.Initialize(/*() =>
            {
                Debug.Log("[Backend] Initialized successfully");
                // Additional backend setup...
            }*/);
        }

        /// <summary>
        /// �÷��̾� ������ �ʱ�ȭ
        /// </summary>
        private void InitializePlayerData()
        {
            CurrentPlayerDeck = new PlayerDeck
            {
                gold = startingGold,
                health = startingHealth,
                ownedUnits = new List<BaseUnit>(),
                formation = new List<BaseUnit>()
            };

            CurrentRound = 0;
        }

        /// <summary>
        /// ���� ����
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[BackendGameManager] Starting new game");
            CurrentRound = 1;
            StartShopPhase();
        }

        /// <summary>
        /// ���� ������ ����
        /// </summary>
        public void StartShopPhase()
        {
            CurrentPhase = GamePhase.Shop;
            Debug.Log($"[BackendGameManager] Starting Shop Phase - Round {CurrentRound}");
            OnShopPhaseStart?.Invoke();
        }

        /// <summary>
        /// �� ������ ����
        /// </summary>
        public void StartFormationPhase()
        {
            CurrentPhase = GamePhase.Formation;
            Debug.Log("[BackendGameManager] Starting Formation Phase");
            OnFormationPhaseStart?.Invoke();
        }

        /// <summary>
        /// ���� ������ ����
        /// </summary>
        public void StartBattlePhase()
        {
            CurrentPhase = GamePhase.Battle;
            Debug.Log("[BackendGameManager] Starting Battle Phase");
            OnBattlePhaseStart?.Invoke();

            // Get opponent (for now, generate random opponent)
            var opponentFormation = GenerateOpponentFormation();

            // Start battle
            BattleManager.Instance.StartBattle(CurrentPlayerDeck.formation, opponentFormation);
        }

        /// <summary>
        /// ���� ���� ó��
        /// </summary>
        private void HandleBattleEnd(BattleResult result)
        {
            Debug.Log($"[BackendGameManager] Battle ended: {result}");

            switch (result)
            {
                case BattleResult.Victory:
                    AddGold(victoryGold);
                    break;

                case BattleResult.Defeat:
                    AddGold(defeatGold);
                    TakeDamage(damageOnDefeat);
                    break;

                case BattleResult.Draw:
                    AddGold(defeatGold);
                    break;
            }

            OnBattleComplete?.Invoke(result);

            // Check game over
            if (CurrentPlayerDeck.health <= 0)
            {
                GameOver();
            }
            else
            {
                CurrentRound++;
                StartShopPhase();
            }
        }

        /// <summary>
        /// ��� �߰�
        /// </summary>
        public void AddGold(int amount)
        {
            CurrentPlayerDeck.gold += amount;
            Debug.Log($"[BackendGameManager] Gold: {CurrentPlayerDeck.gold} (+{amount})");
            OnGoldChanged?.Invoke(CurrentPlayerDeck.gold);
        }

        /// <summary>
        /// ��� �Һ�
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (CurrentPlayerDeck.gold >= amount)
            {
                CurrentPlayerDeck.gold -= amount;
                OnGoldChanged?.Invoke(CurrentPlayerDeck.gold);
                return true;
            }

            return false;
        }

        /// <summary>
        /// ������ �ޱ�
        /// </summary>
        private void TakeDamage(int damage)
        {
            CurrentPlayerDeck.health = Mathf.Max(0, CurrentPlayerDeck.health - damage);
            Debug.Log($"[BackendGameManager] Health: {CurrentPlayerDeck.health} (-{damage})");
            OnHealthChanged?.Invoke(CurrentPlayerDeck.health);
        }

        /// <summary>
        /// ���� ����
        /// </summary>
        private void GameOver()
        {
            Debug.Log("[BackendGameManager] Game Over!");
            CurrentPhase = GamePhase.Result;

            // Save stats to backend
            SaveGameResults();
        }

        /// <summary>
        /// ���� ��� ����
        /// </summary>
        private void SaveGameResults()
        {
            // TODO: Implement backend save
            Debug.Log("[BackendGameManager] Saving game results to backend...");
        }

        /// <summary>
        /// ��� �� ���� (�ӽ�)
        /// </summary>
        private List<BaseUnit> GenerateOpponentFormation()
        {
            // TODO: Implement proper opponent generation or matchmaking
            var formation = new List<BaseUnit>();

            // Generate based on current round
            int unitCount = Mathf.Min(3 + CurrentRound / 2, 6);

            for (int i = 0; i < unitCount; i++)
            {
                // Create random unit
                // This is simplified - should use proper unit creation
            }

            return formation;
        }
    }

    /// <summary>
    /// �÷��̾� �� ������
    /// </summary>
    [Serializable]
    public class PlayerDeck
    {
        public int gold;
        public int health;
        public List<BaseUnit> ownedUnits;
        public List<BaseUnit> formation;
        public int round;
    }
}