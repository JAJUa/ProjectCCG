using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using BackEnd;
using SpiritAge.Core.Enums;
using SpiritAge.UI;
using SpiritAge.Utility;

namespace SpiritAge.Core
{
    /// <summary>
    /// 게임 플로우 총괄 컨트롤러
    /// </summary>
    public class GameFlowController : AbstractSingleton<GameFlowController>
    {
        [Header("Game Settings")]
        [SerializeField] private bool autoStartGame = false;
        [SerializeField] private float phaseTransitionDelay = 1f;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameScene = "Game";

        // Game State
        private bool isGameActive = false;
        private bool isTransitioning = false;

        // Events
        public event Action OnGameStart;
        public event Action OnGameEnd;
        public event Action<GamePhase> OnPhaseChanged;

        protected override void OnSingletonAwake()
        {
            // Initialize DOTween
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(200, 100);

            // Initialize Backend
            InitializeBackend();
        }

        private void Start()
        {
            if (autoStartGame)
            {
                StartCoroutine(AutoStartSequence());
            }
        }

        /// <summary>
        /// 백엔드 초기화
        /// </summary>
        private void InitializeBackend()
        {
            /*
            Backend.Initialize(true, () =>
            {
                Debug.Log("[GameFlow] Backend initialized successfully");

                // Guest login for testing
                Backend.BMember.GuestLogin((callback) =>
                {
                    if (callback.IsSuccess())
                    {
                        Debug.Log("[GameFlow] Guest login successful");
                        LoadPlayerData();
                    }
                    else
                    {
                        Debug.LogError($"[GameFlow] Guest login failed: {callback.GetMessage()}");
                    }
                });
            });*/
        }

        /// <summary>
        /// 플레이어 데이터 로드
        /// </summary>
        private void LoadPlayerData()
        {
            // Load from backend or create new
            // For now, just initialize
            Debug.Log("[GameFlow] Player data loaded");
        }

        /// <summary>
        /// 자동 시작 시퀀스
        /// </summary>
        private IEnumerator AutoStartSequence()
        {
            yield return new WaitForSeconds(2f);
            StartNewGame();
        }

        // ========== Game Flow Control ==========

        /// <summary>
        /// 새 게임 시작
        /// </summary>
        public void StartNewGame()
        {
            if (isGameActive)
            {
                Debug.LogWarning("[GameFlow] Game already active!");
                return;
            }

            Debug.Log("[GameFlow] Starting new game...");

            isGameActive = true;
            OnGameStart?.Invoke();

            // Initialize game systems
            BackendGameManager.Instance.StartGame();

            // Start with shop phase
            StartCoroutine(GameLoopCoroutine());
        }

        /// <summary>
        /// 게임 루프 코루틴
        /// </summary>
        private IEnumerator GameLoopCoroutine()
        {
            while (isGameActive)
            {
                // Shop Phase
                yield return StartPhaseCoroutine(GamePhase.Shop);
                yield return WaitForPhaseEnd(GamePhase.Shop);

                // Formation Phase (if needed)
                if (ShouldShowFormationPhase())
                {
                    yield return StartPhaseCoroutine(GamePhase.Formation);
                    yield return WaitForPhaseEnd(GamePhase.Formation);
                }

                // Battle Phase
                yield return StartPhaseCoroutine(GamePhase.Battle);
                yield return WaitForPhaseEnd(GamePhase.Battle);

                // Check game over
                if (CheckGameOver())
                {
                    EndGame();
                    yield break;
                }

                // Next round
                yield return new WaitForSeconds(phaseTransitionDelay);
            }
        }

        /// <summary>
        /// 페이즈 시작 코루틴
        /// </summary>
        private IEnumerator StartPhaseCoroutine(GamePhase phase)
        {
            isTransitioning = true;

            Debug.Log($"[GameFlow] Starting {phase} phase");
            OnPhaseChanged?.Invoke(phase);

            // UI transition
            UIManager.Instance.TransitionToPhase(phase);

            yield return new WaitForSeconds(0.5f);

            // Phase-specific initialization
            switch (phase)
            {
                case GamePhase.Shop:
                    BackendGameManager.Instance.StartShopPhase();
                    break;

                case GamePhase.Formation:
                    BackendGameManager.Instance.StartFormationPhase();
                    break;

                case GamePhase.Battle:
                    BackendGameManager.Instance.StartBattlePhase();
                    break;
            }

            isTransitioning = false;
        }

        /// <summary>
        /// 페이즈 종료 대기
        /// </summary>
        private IEnumerator WaitForPhaseEnd(GamePhase phase)
        {
            // Wait for phase to complete
            while (BackendGameManager.Instance.CurrentPhase == phase)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 편성 페이즈 표시 여부
        /// </summary>
        private bool ShouldShowFormationPhase()
        {
            // Show formation phase every 3 rounds or when player has many units
            int round = BackendGameManager.Instance.CurrentRound;
            int unitCount = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Count;

            return (round % 3 == 0) || (unitCount >= 8);
        }

        /// <summary>
        /// 게임 오버 체크
        /// </summary>
        private bool CheckGameOver()
        {
            return BackendGameManager.Instance.CurrentPlayerDeck.health <= 0;
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void EndGame()
        {
            if (!isGameActive) return;

            Debug.Log("[GameFlow] Game ended");

            isGameActive = false;
            OnGameEnd?.Invoke();

            // Save game data
            SaveGameData();

            // Show results
            ShowGameResults();
        }

        /// <summary>
        /// 게임 데이터 저장
        /// </summary>
        private void SaveGameData()
        {
            // Save to backend
            var gameData = new Param();
            gameData.Add("rounds", BackendGameManager.Instance.CurrentRound);
            gameData.Add("finalHealth", BackendGameManager.Instance.CurrentPlayerDeck.health);
            gameData.Add("timestamp", DateTime.Now.ToString());

            Backend.GameData.Insert("game_results", gameData, (callback) =>
            {
                if (callback.IsSuccess())
                {
                    Debug.Log("[GameFlow] Game data saved successfully");
                }
                else
                {
                    Debug.LogError($"[GameFlow] Failed to save game data: {callback.GetMessage()}");
                }
            });
        }

        /// <summary>
        /// 게임 결과 표시
        /// </summary>
        private void ShowGameResults()
        {
            UIManager.Instance.TransitionToPhase(GamePhase.Result);

            // TODO: Show detailed results
            // - Final round reached
            // - Units collected
            // - Damage dealt
            // - Gold earned
        }

        // ========== Scene Management ==========

        /// <summary>
        /// 메인 메뉴로
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (isGameActive)
            {
                EndGame();
            }

            StartCoroutine(LoadSceneCoroutine(mainMenuScene));
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            if (isGameActive)
            {
                EndGame();
            }

            StartCoroutine(LoadSceneCoroutine(gameScene, () => StartNewGame()));
        }

        /// <summary>
        /// 씬 로드 코루틴
        /// </summary>
        private IEnumerator LoadSceneCoroutine(string sceneName, Action onComplete = null)
        {
            // Fade out
            yield return UIManager.Instance.FadeOut(0.5f);

            // Load scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Fade in
            yield return UIManager.Instance.FadeIn(0.5f);

            onComplete?.Invoke();
        }

        // ========== Public Methods ==========

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            Time.timeScale = 0f;
            // TODO: Show pause menu
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        /// <summary>
        /// 게임 속도 설정
        /// </summary>
        public void SetGameSpeed(float speed)
        {
            Time.timeScale = Mathf.Clamp(speed, 0.5f, 3f);
        }
    }
}