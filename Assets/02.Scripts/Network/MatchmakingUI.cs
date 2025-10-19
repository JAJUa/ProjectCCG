/*using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpiritAge.Core.Enums;
using SpiritAge.UI;

namespace SpiritAge.Network
{
    /// <summary>
    /// ��ġ����ŷ UI
    /// </summary>
    public class MatchmakingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject matchmakingPanel;
        [SerializeField] private Button rankMatchButton;
        [SerializeField] private Button casualMatchButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text timerText;
        [SerializeField] private GameObject searchingAnimation;

        [Header("Match Found UI")]
        [SerializeField] private GameObject matchFoundPanel;
        [SerializeField] private Text opponentNameText;
        [SerializeField] private Image opponentAvatar;
        [SerializeField] private Text countdownText;

        // State
        private bool isSearching = false;
        private float searchStartTime;
        private Coroutine searchCoroutine;

        private void Awake()
        {
            SetupButtons();
            SubscribeToEvents();
        }

        /// <summary>
        /// ��ư ����
        /// </summary>
        private void SetupButtons()
        {
            if (rankMatchButton != null)
            {
                rankMatchButton.onClick.RemoveAllListeners();
                rankMatchButton.onClick.AddListener(() => StartMatchmaking(MatchType.Rank));
            }

            if (casualMatchButton != null)
            {
                casualMatchButton.onClick.RemoveAllListeners();
                casualMatchButton.onClick.AddListener(() => StartMatchmaking(MatchType.Casual));
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(CancelMatchmaking);
                cancelButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// �̺�Ʈ ����
        /// </summary>
        private void SubscribeToEvents()
        {
            var network = NetworkManager.Instance;
            if (network != null)
            {
                network.OnConnected += OnConnectedToServer;
                network.OnMatchFound += OnMatchFound;
                network.OnMatchEnded += OnMatchEnded;
            }
        }

        /// <summary>
        /// ��ġ����ŷ ����
        /// </summary>
        private void StartMatchmaking(MatchType matchType)
        {
            if (isSearching) return;

            isSearching = true;
            searchStartTime = Time.time;

            // Connect to server if not connected
            if (!NetworkManager.Instance.IsConnected)
            {
                NetworkManager.Instance.ConnectToMatchServer();
                StartCoroutine(WaitForConnectionAndStartMatch(matchType));
            }
            else
            {
                NetworkManager.Instance.StartMatchmaking(matchType);
                ShowSearchingUI(matchType);
            }
        }

        /// <summary>
        /// ���� ��� �� ��Ī ����
        /// </summary>
        private IEnumerator WaitForConnectionAndStartMatch(MatchType matchType)
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (!NetworkManager.Instance.IsConnected && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (NetworkManager.Instance.IsConnected)
            {
                NetworkManager.Instance.StartMatchmaking(matchType);
                ShowSearchingUI(matchType);
            }
            else
            {
                UIManager.Instance.ShowNotification("���� ���� ����", NotificationType.Error);
                isSearching = false;
            }
        }

        /// <summary>
        /// ��ġ����ŷ ���
        /// </summary>
        private void CancelMatchmaking()
        {
            if (!isSearching) return;

            NetworkManager.Instance.CancelMatchmaking();
            HideSearchingUI();

            isSearching = false;

            UIManager.Instance.ShowNotification("��ġ����ŷ ��ҵ�", NotificationType.Info);
        }

        /// <summary>
        /// �˻� UI ǥ��
        /// </summary>
        private void ShowSearchingUI(MatchType matchType)
        {
            if (matchmakingPanel != null)
                matchmakingPanel.SetActive(true);

            if (rankMatchButton != null)
                rankMatchButton.gameObject.SetActive(false);

            if (casualMatchButton != null)
                casualMatchButton.gameObject.SetActive(false);

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(true);

            if (statusText != null)
                statusText.text = $"{matchType} ��ġ �˻� ��...";

            if (searchingAnimation != null)
            {
                searchingAnimation.SetActive(true);
                searchingAnimation.transform.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.Linear);
            }

            // Start timer coroutine
            if (searchCoroutine != null)
                StopCoroutine(searchCoroutine);
            searchCoroutine = StartCoroutine(UpdateSearchTimer());
        }

        /// <summary>
        /// �˻� UI �����
        /// </summary>
        private void HideSearchingUI()
        {
            if (rankMatchButton != null)
                rankMatchButton.gameObject.SetActive(true);

            if (casualMatchButton != null)
                casualMatchButton.gameObject.SetActive(true);

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(false);

            if (searchingAnimation != null)
            {
                DOTween.Kill(searchingAnimation.transform);
                searchingAnimation.SetActive(false);
            }

            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
        }

        /// <summary>
        /// �˻� Ÿ�̸� ������Ʈ
        /// </summary>
        private IEnumerator UpdateSearchTimer()
        {
            while (isSearching)
            {
                float elapsed = Time.time - searchStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);

                if (timerText != null)
                    timerText.text = $"{minutes:00}:{seconds:00}";

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// ���� �����
        /// </summary>
        private void OnConnectedToServer()
        {
            Debug.Log("[MatchmakingUI] Connected to matchmaking server");
        }

        /// <summary>
        /// ��ġ ã��
        /// </summary>
        private void OnMatchFound(MatchInfo match)
        {
            isSearching = false;
            HideSearchingUI();

            // Show match found UI
            StartCoroutine(ShowMatchFoundSequence());
        }

        /// <summary>
        /// ��ġ �߰� ������
        /// </summary>
        private IEnumerator ShowMatchFoundSequence()
        {
            if (matchFoundPanel != null)
            {
                matchFoundPanel.SetActive(true);

                // Set opponent info
                if (opponentNameText != null)
                    opponentNameText.text = NetworkManager.Instance.OpponentNickname;

                // Countdown
                for (int i = 3; i > 0; i--)
                {
                    if (countdownText != null)
                    {
                        countdownText.text = i.ToString();
                        countdownText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
                    }

                    yield return new WaitForSeconds(1f);
                }

                matchFoundPanel.SetActive(false);
            }

            // Transition to battle
            UIManager.Instance.TransitionToPhase(GamePhase.Battle);
        }

        /// <summary>
        /// ��ġ ����
        /// </summary>
        private void OnMatchEnded(string reason)
        {
            isSearching = false;
            HideSearchingUI();

            if (matchFoundPanel != null)
                matchFoundPanel.SetActive(false);

            UIManager.Instance.ShowNotification($"��ġ ����: {reason}", NotificationType.Info);
        }

        private void OnDestroy()
        {
            var network = NetworkManager.Instance;
            if (network != null)
            {
                network.OnConnected -= OnConnectedToServer;
                network.OnMatchFound -= OnMatchFound;
                network.OnMatchEnded -= OnMatchEnded;
            }

            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
            }

            DOTween.Kill(searchingAnimation?.transform);
            DOTween.Kill(countdownText?.transform);
        }
    }
}*/