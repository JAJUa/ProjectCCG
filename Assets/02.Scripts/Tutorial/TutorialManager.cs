using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpiritAge.Core;
using SpiritAge.Core.Enums;
using SpiritAge.UI;
using SpiritAge.Utility;

namespace SpiritAge.Tutorial
{
    /// <summary>
    /// Ʃ�丮�� �Ŵ���
    /// </summary>
    public class TutorialManager : AbstractSingleton<TutorialManager>
    {
        [Header("Tutorial UI")]
        [SerializeField] private GameObject tutorialCanvas;
        [SerializeField] private GameObject highlightPrefab;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private GameObject dialogueBox;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;

        [Header("Tutorial Data")]
        [SerializeField] private TutorialSequence[] tutorialSequences;
        [SerializeField] private float textSpeed = 0.05f;

        // State
        private bool isTutorialActive = false;
        private int currentSequenceIndex = 0;
        private int currentStepIndex = 0;
        private TutorialSequence currentSequence;
        private GameObject currentHighlight;
        private GameObject currentArrow;
        private Coroutine textCoroutine;

        // Progress tracking
        private HashSet<string> completedTutorials = new HashSet<string>();

        // Events
        public event Action<string> OnTutorialStarted;
        public event Action<string> OnTutorialCompleted;
        public event Action<TutorialStep> OnStepChanged;

        protected override void OnSingletonAwake()
        {
            LoadTutorialProgress();
            SetupUI();
        }

        /// <summary>
        /// Ʃ�丮�� ���൵ �ε�
        /// </summary>
        private void LoadTutorialProgress()
        {
            string savedProgress = PlayerPrefs.GetString("TutorialProgress", "");
            if (!string.IsNullOrEmpty(savedProgress))
            {
                string[] completed = savedProgress.Split(',');
                foreach (string tutorialId in completed)
                {
                    completedTutorials.Add(tutorialId);
                }
            }
        }

        /// <summary>
        /// UI ����
        /// </summary>
        private void SetupUI()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(NextStep);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(SkipTutorial);
            }

            if (tutorialCanvas != null)
            {
                tutorialCanvas.SetActive(false);
            }
        }

        // ========== Tutorial Control ==========

        /// <summary>
        /// Ʃ�丮�� ����
        /// </summary>
        public void StartTutorial(string tutorialId)
        {
            if (isTutorialActive)
            {
                Debug.LogWarning("[TutorialManager] Tutorial already active");
                return;
            }

            TutorialSequence sequence = GetSequence(tutorialId);
            if (sequence == null)
            {
                Debug.LogError($"[TutorialManager] Tutorial not found: {tutorialId}");
                return;
            }

            if (!sequence.canRepeat && completedTutorials.Contains(tutorialId))
            {
                Debug.Log($"[TutorialManager] Tutorial already completed: {tutorialId}");
                return;
            }

            currentSequence = sequence;
            currentSequenceIndex = Array.IndexOf(tutorialSequences, sequence);
            currentStepIndex = 0;
            isTutorialActive = true;

            OnTutorialStarted?.Invoke(tutorialId);

            if (tutorialCanvas != null)
            {
                tutorialCanvas.SetActive(true);
            }

            ShowCurrentStep();
        }

        /// <summary>
        /// �ڵ� Ʃ�丮�� Ʈ����
        /// </summary>
        public void TriggerTutorial(TutorialTrigger trigger, object context = null)
        {
            foreach (var sequence in tutorialSequences)
            {
                if (sequence.trigger == trigger && !completedTutorials.Contains(sequence.id))
                {
                    if (CheckConditions(sequence, context))
                    {
                        StartTutorial(sequence.id);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// ���� üũ
        /// </summary>
        private bool CheckConditions(TutorialSequence sequence, object context)
        {
            switch (sequence.trigger)
            {
                case TutorialTrigger.FirstShop:
                    return BackendGameManager.Instance.CurrentRound == 1 &&
                           BackendGameManager.Instance.CurrentPhase == GamePhase.Shop;

                case TutorialTrigger.FirstBattle:
                    return BackendGameManager.Instance.CurrentPhase == GamePhase.Battle;

                case TutorialTrigger.FirstEvolution:
                    return context is EvolutionType;

                default:
                    return true;
            }
        }

        /// <summary>
        /// ���� �ܰ� ǥ��
        /// </summary>
        private void ShowCurrentStep()
        {
            if (currentSequence == null || currentStepIndex >= currentSequence.steps.Length)
            {
                CompleteTutorial();
                return;
            }

            TutorialStep step = currentSequence.steps[currentStepIndex];
            OnStepChanged?.Invoke(step);

            // Clear previous highlights
            ClearHighlights();

            // Show dialogue
            ShowDialogue(step);

            // Highlight target
            if (!string.IsNullOrEmpty(step.targetPath))
            {
                HighlightTarget(step.targetPath, step.highlightType);
            }

            // Handle special actions
            ExecuteStepAction(step);
        }

        /// <summary>
        /// ��ȭ ǥ��
        /// </summary>
        private void ShowDialogue(TutorialStep step)
        {
            if (dialogueBox != null)
            {
                dialogueBox.SetActive(true);

                // Position dialogue
                RectTransform dialogueRect = dialogueBox.GetComponent<RectTransform>();
                if (dialogueRect != null)
                {
                    switch (step.dialoguePosition)
                    {
                        case DialoguePosition.Top:
                            dialogueRect.anchorMin = new Vector2(0.5f, 1f);
                            dialogueRect.anchorMax = new Vector2(0.5f, 1f);
                            dialogueRect.anchoredPosition = new Vector2(0, -100);
                            break;

                        case DialoguePosition.Bottom:
                            dialogueRect.anchorMin = new Vector2(0.5f, 0f);
                            dialogueRect.anchorMax = new Vector2(0.5f, 0f);
                            dialogueRect.anchoredPosition = new Vector2(0, 100);
                            break;

                        case DialoguePosition.Center:
                            dialogueRect.anchorMin = new Vector2(0.5f, 0.5f);
                            dialogueRect.anchorMax = new Vector2(0.5f, 0.5f);
                            dialogueRect.anchoredPosition = Vector2.zero;
                            break;
                    }
                }

                // Set speaker name
                if (speakerNameText != null)
                {
                    speakerNameText.text = step.speakerName;
                }

                // Animate text
                if (textCoroutine != null)
                {
                    StopCoroutine(textCoroutine);
                }
                textCoroutine = StartCoroutine(TypewriterEffect(step.dialogueText));
            }
        }

        /// <summary>
        /// Ÿ���� ȿ��
        /// </summary>
        private IEnumerator TypewriterEffect(string text)
        {
            if (dialogueText == null) yield break;

            dialogueText.text = "";

            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(textSpeed);
            }

            textCoroutine = null;
        }

        /// <summary>
        /// Ÿ�� ���̶���Ʈ
        /// </summary>
        private void HighlightTarget(string targetPath, HighlightType type)
        {
            GameObject target = GameObject.Find(targetPath);
            if (target == null)
            {
                Debug.LogWarning($"[TutorialManager] Target not found: {targetPath}");
                return;
            }

            switch (type)
            {
                case HighlightType.Glow:
                    CreateGlowHighlight(target);
                    break;

                case HighlightType.Arrow:
                    CreateArrowPointer(target);
                    break;

                case HighlightType.Circle:
                    CreateCircleHighlight(target);
                    break;

                case HighlightType.Darken:
                    CreateDarkenHighlight(target);
                    break;
            }
        }

        /// <summary>
        /// �۷ο� ���̶���Ʈ ����
        /// </summary>
        private void CreateGlowHighlight(GameObject target)
        {
            if (highlightPrefab == null) return;

            currentHighlight = Instantiate(highlightPrefab, target.transform);
            currentHighlight.transform.localPosition = Vector3.zero;

            // Pulsing animation
            currentHighlight.transform.DOScale(1.2f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// ȭ��ǥ ������ ����
        /// </summary>
        private void CreateArrowPointer(GameObject target)
        {
            if (arrowPrefab == null) return;

            Vector3 offset = new Vector3(0, 100, 0);
            currentArrow = Instantiate(arrowPrefab);
            currentArrow.transform.position = target.transform.position + offset;

            // Bouncing animation
            currentArrow.transform.DOMoveY(
                currentArrow.transform.position.y - 20, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }

        /// <summary>
        /// ���� ���̶���Ʈ ����
        /// </summary>
        private void CreateCircleHighlight(GameObject target)
        {
            // Create circular mask around target
            // Implementation depends on UI system
        }

        /// <summary>
        /// ��Ӱ� ���̶���Ʈ ����
        /// </summary>
        private void CreateDarkenHighlight(GameObject target)
        {
            // Darken everything except target
            // Implementation requires overlay canvas
        }

        /// <summary>
        /// ���̶���Ʈ ����
        /// </summary>
        private void ClearHighlights()
        {
            if (currentHighlight != null)
            {
                DOTween.Kill(currentHighlight.transform);
                Destroy(currentHighlight);
            }

            if (currentArrow != null)
            {
                DOTween.Kill(currentArrow.transform);
                Destroy(currentArrow);
            }
        }

        /// <summary>
        /// �ܰ� �׼� ����
        /// </summary>
        private void ExecuteStepAction(TutorialStep step)
        {
            switch (step.action)
            {
                case TutorialAction.PauseGame:
                    Time.timeScale = 0f;
                    break;

                case TutorialAction.DisableInput:
                    // Disable specific input
                    break;

                case TutorialAction.ShowReward:
                    // Show reward preview
                    break;

                case TutorialAction.ForceAction:
                    // Force specific action
                    break;
            }
        }

        /// <summary>
        /// ���� �ܰ�
        /// </summary>
        public void NextStep()
        {
            // Skip text animation if still typing
            if (textCoroutine != null)
            {
                StopCoroutine(textCoroutine);
                if (dialogueText != null && currentSequence != null)
                {
                    dialogueText.text = currentSequence.steps[currentStepIndex].dialogueText;
                }
                textCoroutine = null;
                return;
            }

            currentStepIndex++;
            ShowCurrentStep();
        }

        /// <summary>
        /// Ʃ�丮�� �ǳʶٱ�
        /// </summary>
        public void SkipTutorial()
        {
            if (!currentSequence.canSkip)
            {
                UIManager.Instance.ShowNotification("�� Ʃ�丮���� �ǳʶ� �� �����ϴ�", NotificationType.Warning);
                return;
            }

            CompleteTutorial();
        }

        /// <summary>
        /// Ʃ�丮�� �Ϸ�
        /// </summary>
        private void CompleteTutorial()
        {
            if (currentSequence == null) return;

            string tutorialId = currentSequence.id;

            // Mark as completed
            if (!completedTutorials.Contains(tutorialId))
            {
                completedTutorials.Add(tutorialId);
                SaveTutorialProgress();
            }

            // Give rewards
            if (currentSequence.rewardGold > 0)
            {
                BackendGameManager.Instance.AddGold(currentSequence.rewardGold);
            }

            // Clean up
            ClearHighlights();

            if (dialogueBox != null)
            {
                dialogueBox.SetActive(false);
            }

            if (tutorialCanvas != null)
            {
                tutorialCanvas.SetActive(false);
            }

            // Reset time scale
            Time.timeScale = 1f;

            isTutorialActive = false;
            currentSequence = null;

            OnTutorialCompleted?.Invoke(tutorialId);

            UIManager.Instance.ShowNotification("Ʃ�丮�� �Ϸ�!", NotificationType.Success);
        }

        /// <summary>
        /// Ʃ�丮�� ���൵ ����
        /// </summary>
        private void SaveTutorialProgress()
        {
            string progress = string.Join(",", completedTutorials);
            PlayerPrefs.SetString("TutorialProgress", progress);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// ������ ��������
        /// </summary>
        private TutorialSequence GetSequence(string id)
        {
            foreach (var sequence in tutorialSequences)
            {
                if (sequence.id == id)
                {
                    return sequence;
                }
            }
            return null;
        }

        /// <summary>
        /// Ʃ�丮�� �Ϸ� ����
        /// </summary>
        public bool IsCompleted(string tutorialId)
        {
            return completedTutorials.Contains(tutorialId);
        }

        /// <summary>
        /// ��� Ʃ�丮�� ����
        /// </summary>
        [ContextMenu("Reset All Tutorials")]
        public void ResetAllTutorials()
        {
            completedTutorials.Clear();
            PlayerPrefs.DeleteKey("TutorialProgress");
            PlayerPrefs.Save();
            Debug.Log("[TutorialManager] All tutorials reset");
        }
    }

    // ========== Data Structures ==========

    [Serializable]
    public class TutorialSequence
    {
        public string id;
        public string name;
        public TutorialTrigger trigger;
        public bool canSkip = true;
        public bool canRepeat = false;
        public TutorialStep[] steps;
        public int rewardGold;
        public string rewardItem;
    }

    [Serializable]
    public class TutorialStep
    {
        public string speakerName = "���̵�";
        [TextArea(3, 5)]
        public string dialogueText;
        public string targetPath;
        public HighlightType highlightType;
        public DialoguePosition dialoguePosition;
        public TutorialAction action;
        public float waitTime = 0f;
    }

    public enum TutorialTrigger
    {
        Manual,
        GameStart,
        FirstShop,
        FirstBattle,
        FirstEvolution,
        FirstVictory,
        FirstDefeat,
        RoundNumber,
        Custom
    }

    public enum HighlightType
    {
        None,
        Glow,
        Arrow,
        Circle,
        Darken
    }

    public enum DialoguePosition
    {
        Top,
        Bottom,
        Center,
        Custom
    }

    public enum TutorialAction
    {
        None,
        PauseGame,
        DisableInput,
        ShowReward,
        ForceAction,
        PlayAnimation
    }
}