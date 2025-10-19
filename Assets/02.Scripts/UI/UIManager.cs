using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpiritAge.Battle;
using SpiritAge.Core;
using SpiritAge.Core.Enums;
using SpiritAge.Shop;
using SpiritAge.Units;
using SpiritAge.Utility;
using SpiritAge.Utility.Pooling;

namespace SpiritAge.UI
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// UI 시스템 매니저
    /// </summary>
    public class UIManager : AbstractSingleton<UIManager>
    {
        [Header("Main UI Panels")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject formationPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject resultPanel;

        [Header("HUD Elements")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text roundText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Transform buffIconContainer;

        [Header("Shop UI")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Transform shopCardContainer;
        [SerializeField] private Transform spiritCardContainer;

        [Header("Battle UI")]
        [SerializeField] private Transform playerFieldUI;
        [SerializeField] private Transform enemyFieldUI;
        [SerializeField] private Text turnCounterText;
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private GameObject healTextPrefab;

        [Header("Notification")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;

        [Header("Effects")]
        [SerializeField] private float fadeSpeed = 0.5f;
        [SerializeField] private float notificationDuration = 2f;

        // Current state
        private GamePhase currentPhase = GamePhase.None;
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private bool isShowingNotification = false;

        protected override void OnSingletonAwake()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // Hide all panels initially
            SetAllPanelsActive(false);

            // Setup buttons
            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveAllListeners();
                refreshButton.onClick.AddListener(OnRefreshButtonClicked);
            }

            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            }

            // Initialize HUD
            UpdateHUD();
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            // Backend events
            var backend = BackendGameManager.Instance;
            if (backend != null)
            {
                backend.OnGoldChanged += UpdateGoldDisplay;
                backend.OnHealthChanged += UpdateHealthDisplay;
                backend.OnShopPhaseStart += () => TransitionToPhase(GamePhase.Shop);
                backend.OnFormationPhaseStart += () => TransitionToPhase(GamePhase.Formation);
                backend.OnBattlePhaseStart += () => TransitionToPhase(GamePhase.Battle);
                backend.OnBattleComplete += OnBattleComplete;
            }

            // Battle events
            var battle = BattleManager.Instance;
            if (battle != null)
            {
                battle.OnTurnStart += UpdateTurnCounter;
                battle.OnDamageDealt += ShowDamageText;
            }
        }

        /// <summary>
        /// 페이즈 전환
        /// </summary>
        public void TransitionToPhase(GamePhase newPhase)
        {
            if (currentPhase == newPhase) return;

            StartCoroutine(PhaseTransitionCoroutine(newPhase));
        }

        /// <summary>
        /// 페이즈 전환 코루틴
        /// </summary>
        private IEnumerator PhaseTransitionCoroutine(GamePhase newPhase)
        {
            // Fade out current panel
            yield return FadeOutCurrentPanel();

            currentPhase = newPhase;

            // Show new panel
            GameObject targetPanel = GetPanelForPhase(newPhase);
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                yield return FadeInPanel(targetPanel);
            }

            // Update UI for new phase
            UpdatePhaseSpecificUI();
        }

        /// <summary>
        /// 현재 패널 페이드 아웃
        /// </summary>
        private IEnumerator FadeOutCurrentPanel()
        {
            GameObject currentPanel = GetPanelForPhase(currentPhase);
            if (currentPanel != null)
            {
                CanvasGroup canvasGroup = GetOrAddCanvasGroup(currentPanel);

                yield return canvasGroup.DOFade(0f, fadeSpeed).WaitForCompletion();
                currentPanel.SetActive(false);
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// 패널 페이드 인
        /// </summary>
        private IEnumerator FadeInPanel(GameObject panel)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(panel);
            canvasGroup.alpha = 0f;

            yield return canvasGroup.DOFade(1f, fadeSpeed).WaitForCompletion();
        }

        /// <summary>
        /// 페이즈별 패널 가져오기
        /// </summary>
        private GameObject GetPanelForPhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Lobby: return lobbyPanel;
                case GamePhase.Shop: return shopPanel;
                case GamePhase.Formation: return formationPanel;
                case GamePhase.Battle: return battlePanel;
                case GamePhase.Result: return resultPanel;
                default: return null;
            }
        }

        /// <summary>
        /// 캔버스 그룹 가져오기 또는 추가
        /// </summary>
        private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
        {
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = obj.AddComponent<CanvasGroup>();
            }
            return canvasGroup;
        }

        /// <summary>
        /// 페이즈별 UI 업데이트
        /// </summary>
        private void UpdatePhaseSpecificUI()
        {
            switch (currentPhase)
            {
                case GamePhase.Shop:
                    UpdateShopUI();
                    break;
                case GamePhase.Formation:
                    UpdateFormationUI();
                    break;
                case GamePhase.Battle:
                    UpdateBattleUI();
                    break;
            }

            UpdateHUD();
        }

        // ========== HUD Updates ==========

        /// <summary>
        /// HUD 업데이트
        /// </summary>
        private void UpdateHUD()
        {
            var backend = BackendGameManager.Instance;
            if (backend == null) return;

            UpdateGoldDisplay(backend.CurrentPlayerDeck.gold);
            UpdateHealthDisplay(backend.CurrentPlayerDeck.health);
            UpdateRoundDisplay(backend.CurrentRound);
        }

        /// <summary>
        /// 골드 표시 업데이트
        /// </summary>
        private void UpdateGoldDisplay(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"💰 {gold}G";

                // Punch animation
                goldText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
            }
        }

        /// <summary>
        /// 체력 표시 업데이트
        /// </summary>
        private void UpdateHealthDisplay(int health)
        {
            if (healthText != null)
            {
                healthText.text = $"❤️ {health}";
            }

            if (healthBar != null)
            {
                healthBar.DOValue(health / 100f, 0.5f);

                // Change color based on health
                Image fillImage = healthBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (health > 60)
                        fillImage.color = Color.green;
                    else if (health > 30)
                        fillImage.color = Color.yellow;
                    else
                        fillImage.color = Color.red;
                }
            }
        }

        /// <summary>
        /// 라운드 표시 업데이트
        /// </summary>
        private void UpdateRoundDisplay(int round)
        {
            if (roundText != null)
            {
                roundText.text = $"Round {round}";
            }
        }

        // ========== Shop UI ==========

        /// <summary>
        /// 상점 UI 업데이트
        /// </summary>
        private void UpdateShopUI()
        {
            // Enable refresh button
            if (refreshButton != null)
            {
                refreshButton.interactable = true;

                // Update refresh cost display
                Text costText = refreshButton.GetComponentInChildren<Text>();
                if (costText != null)
                {
                    costText.text = "새로고침 (1G)";
                }
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭
        /// </summary>
        private void OnRefreshButtonClicked()
        {
            ShopManager.Instance.RefreshShop();

            // Button feedback
            refreshButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5);
        }

        /// <summary>
        /// 준비 버튼 클릭
        /// </summary>
        private void OnReadyButtonClicked()
        {
            BackendGameManager.Instance.StartBattlePhase();
        }

        // ========== Formation UI ==========

        /// <summary>
        /// 편성 UI 업데이트
        /// </summary>
        private void UpdateFormationUI()
        {
            // Update formation slots
            // Show unit positions
        }

        // ========== Battle UI ==========

        /// <summary>
        /// 전투 UI 업데이트
        /// </summary>
        private void UpdateBattleUI()
        {
            // Setup battle field UI
        }

        /// <summary>
        /// 턴 카운터 업데이트
        /// </summary>
        private void UpdateTurnCounter(int turn)
        {
            if (turnCounterText != null)
            {
                turnCounterText.text = $"Turn {turn}";
                turnCounterText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }

        /// <summary>
        /// 데미지 텍스트 표시
        /// </summary>
        private void ShowDamageText(BaseUnit attacker, BaseUnit target, int damage)
        {
            if (damageTextPrefab == null || target == null) return;

            Vector3 worldPos = target.transform.position + Vector3.up * 2f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            GameObject damageObj = PoolManager.Instance.Spawn("DamageText", screenPos, Quaternion.identity);
            if (damageObj == null)
            {
                damageObj = Instantiate(damageTextPrefab, screenPos, Quaternion.identity, battlePanel.transform);
            }

            Text damageText = damageObj.GetComponentInChildren<Text>();
            if (damageText != null)
            {
                damageText.text = $"-{damage}";
                damageText.color = Color.red;
            }

            // Animate
            damageObj.transform.DOMoveY(screenPos.y + 50f, 1f);
            damageObj.GetComponent<CanvasGroup>().DOFade(0f, 1f);

            PoolManager.Instance.Despawn("DamageText", damageObj, 1f);
        }

        /// <summary>
        /// 전투 완료
        /// </summary>
        private void OnBattleComplete(BattleResult result)
        {
            TransitionToPhase(GamePhase.Result);
            ShowBattleResult(result);
        }

        /// <summary>
        /// 전투 결과 표시
        /// </summary>
        private void ShowBattleResult(BattleResult result)
        {
            string message = "";
            NotificationType type = NotificationType.Info;

            switch (result)
            {
                case BattleResult.Victory:
                    message = "승리!";
                    type = NotificationType.Success;
                    break;
                case BattleResult.Defeat:
                    message = "패배...";
                    type = NotificationType.Error;
                    break;
                case BattleResult.Draw:
                    message = "무승부!";
                    type = NotificationType.Warning;
                    break;
            }

            ShowNotification(message, type);
        }

        // ========== Notifications ==========

        /// <summary>
        /// 알림 표시
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            var data = new NotificationData
            {
                message = message,
                type = type,
                duration = notificationDuration
            };

            notificationQueue.Enqueue(data);

            if (!isShowingNotification)
            {
                StartCoroutine(ProcessNotificationQueue());
            }
        }

        /// <summary>
        /// 알림 큐 처리
        /// </summary>
        private IEnumerator ProcessNotificationQueue()
        {
            isShowingNotification = true;

            while (notificationQueue.Count > 0)
            {
                var data = notificationQueue.Dequeue();
                yield return ShowNotificationCoroutine(data);
            }

            isShowingNotification = false;
        }

        /// <summary>
        /// 알림 표시 코루틴
        /// </summary>
        private IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            GameObject notificationObj = PoolManager.Instance.Spawn("Notification");

            if (notificationObj == null && notificationPrefab != null)
            {
                notificationObj = Instantiate(notificationPrefab, notificationContainer);
            }

            if (notificationObj != null)
            {
                // Setup notification
                Text messageText = notificationObj.GetComponentInChildren<Text>();
                if (messageText != null)
                {
                    messageText.text = data.message;
                }

                Image background = notificationObj.GetComponent<Image>();
                if (background != null)
                {
                    background.color = GetNotificationColor(data.type);
                }

                // Animate in
                notificationObj.transform.localScale = Vector3.zero;
                notificationObj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

                yield return new WaitForSeconds(data.duration);

                // Animate out
                notificationObj.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                yield return new WaitForSeconds(0.3f);

                PoolManager.Instance.Despawn("Notification", notificationObj);
            }
        }

        /// <summary>
        /// 알림 색상 가져오기
        /// </summary>
        private Color GetNotificationColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return new Color(0.2f, 0.8f, 0.2f);
                case NotificationType.Warning: return new Color(0.8f, 0.8f, 0.2f);
                case NotificationType.Error: return new Color(0.8f, 0.2f, 0.2f);
                default: return new Color(0.2f, 0.2f, 0.8f);
            }
        }

        /// <summary>
        /// 모든 패널 활성화/비활성화
        /// </summary>
        private void SetAllPanelsActive(bool active)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(active);
            if (shopPanel != null) shopPanel.SetActive(active);
            if (formationPanel != null) formationPanel.SetActive(active);
            if (battlePanel != null) battlePanel.SetActive(active);
            if (resultPanel != null) resultPanel.SetActive(active);
        }

        /// <summary>
        /// 알림 데이터
        /// </summary>
        private struct NotificationData
        {
            public string message;
            public NotificationType type;
            public float duration;
        }
    }
}