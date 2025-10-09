using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tutorialPanel;
    public Text tutorialTitleText;
    public Text tutorialMessageText;
    public Button nextButton;
    public Button skipButton;
    public GameObject tutorialArrow;
    public GameObject tutorialHighlight;

    [Header("Tutorial Settings")]
    public bool showTutorialOnFirstPlay = true;
    public float messageDelay = 0.5f;

    // 튜토리얼 상태
    private bool isTutorialActive = false;
    private int currentStepIndex = 0;
    private List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    // 매니저 참조
    private BackendGameManager gameManager;
    private GameUIManager uiManager;

    void Start()
    {
        gameManager = BackendGameManager.Instance;
        uiManager = GetComponent<GameUIManager>();

        // 버튼 이벤트
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipButtonClicked);

        // 튜토리얼 단계 정의
        DefineTutorialSteps();

        // 첫 플레이 체크
        if (showTutorialOnFirstPlay && IsFirstTimePlaying())
        {
            StartTutorial();
        }
        else
        {
            tutorialPanel.SetActive(false);
        }
    }

    // ============================================
    // 튜토리얼 단계 정의
    // ============================================

    private void DefineTutorialSteps()
    {
        tutorialSteps = new List<TutorialStep>
        {
            // 1. 환영 메시지
            new TutorialStep
            {
                title = "신령의 시대에 오신 것을 환영합니다!",
                message = "이 게임은 카드 수집과 전략적 배치를 통해 상대와 겨루는 턴제 로그라이크 게임입니다.\n\n" +
                         "매 라운드마다 상점에서 캐릭터를 구매하고, 덱을 구성한 뒤 전투를 벌입니다.",
                highlightTarget = null,
                pauseGame = true
            },
            
            // 2. 골드 시스템
            new TutorialStep
            {
                title = "골드 시스템",
                message = "골드는 캐릭터를 구매하는 데 사용됩니다.\n\n" +
                         "• 승리 시: 5골드\n" +
                         "• 패배 시: 3골드\n" +
                         "• 시작 골드: 10골드",
                highlightTarget = "PlayerGoldText",
                pauseGame = true
            },
            
            // 3. 상점 시스템
            new TutorialStep
            {
                title = "상점",
                message = "상점에서는 랜덤으로 5개의 캐릭터 카드가 등장합니다.\n\n" +
                         "• 골드를 소모하여 캐릭터 구매\n" +
                         "• 1골드로 상점 새로고침 가능\n" +
                         "• 2라운드부터 신령 구매 가능",
                highlightTarget = "ShopCardContainer",
                pauseGame = true
            },
            
            // 4. 캐릭터 타입
            new TutorialStep
            {
                title = "캐릭터 타입",
                message = "캐릭터는 크게 두 가지 타입으로 나뉩니다:\n\n" +
                         "• 신도자: 일반 캐릭터, 여러 개 소유 가능\n" +
                         "• 신령: 강력한 캐릭터, 덱에 1개만 가능",
                highlightTarget = null,
                pauseGame = true
            },
            
            // 5. 진화 시스템
            new TutorialStep
            {
                title = "진화 시스템",
                message = "특정 조건을 만족하면 캐릭터가 진화합니다!\n\n" +
                         "• 검사: 스탯 임계값 달성 시\n" +
                         "• 마법사: 특정 속성 3개 이상\n" +
                         "• 연구자: 특수 조건\n" +
                         "• 교섭가: 새로고침/골드 조건",
                highlightTarget = null,
                pauseGame = true
            },
            
            // 6. 덱 편성
            new TutorialStep
            {
                title = "덱 편성",
                message = "최대 6개의 캐릭터를 배치할 수 있습니다.\n\n" +
                         "배치 위치가 중요합니다!\n" +
                         "• 앞쪽: 먼저 공격받음\n" +
                         "• 뒤쪽: 보호받지만 지원 역할",
                highlightTarget = "FormationContainer",
                pauseGame = true
            },
            
            // 7. 전투 시스템
            new TutorialStep
            {
                title = "전투",
                message = "전투는 자동으로 진행됩니다.\n\n" +
                         "• 속도가 빠른 순서대로 행동\n" +
                         "• 속도가 2배 이상이면 재행동\n" +
                         "• 가장 앞에 있는 적을 공격",
                highlightTarget = null,
                pauseGame = true
            },
            
            // 8. 버프/디버프
            new TutorialStep
            {
                title = "버프와 디버프",
                message = "다양한 상태 효과가 있습니다:\n\n" +
                         "❄ 빙결: 속도 감소\n" +
                         "🔥 화상: 지속 데미지\n" +
                         "⚡ 번개: 3중첩 시 기절\n" +
                         "👻 영혼: 즉사 취약\n" +
                         "😈 광기: 공격↑ 방어↓",
                highlightTarget = null,
                pauseGame = true
            },
            
            // 9. 승리 조건
            new TutorialStep
            {
                title = "승리 방법",
                message = "상대의 체력을 0으로 만들면 승리!\n\n" +
                         "• 시작 체력: 100\n" +
                         "• 패배 시 데미지: 10\n" +
                         "• 전략적으로 덱을 강화하세요!",
                highlightTarget = "PlayerHealthText",
                pauseGame = true
            },
            
            // 10. 시작
            new TutorialStep
            {
                title = "준비 완료!",
                message = "이제 게임을 시작할 준비가 되었습니다!\n\n" +
                         "행운을 빕니다, 신령 소환사여!",
                highlightTarget = null,
                pauseGame = true,
                isLastStep = true
            }
        };
    }

    // ============================================
    // 튜토리얼 진행
    // ============================================

    public void StartTutorial()
    {
        isTutorialActive = true;
        currentStepIndex = 0;

        tutorialPanel.SetActive(true);
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= tutorialSteps.Count)
        {
            EndTutorial();
            return;
        }

        TutorialStep step = tutorialSteps[currentStepIndex];

        // 제목과 메시지 표시
        if (tutorialTitleText != null)
            tutorialTitleText.text = step.title;

        if (tutorialMessageText != null)
            tutorialMessageText.text = step.message;

        // 하이라이트 설정
        SetupHighlight(step.highlightTarget);

        // 다음 버튼 텍스트
        if (nextButton != null)
        {
            Text buttonText = nextButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = step.isLastStep ? "시작하기!" : "다음";
            }
        }

        // 게임 일시정지
        if (step.pauseGame)
        {
            Time.timeScale = 0f;
        }
    }

    private void SetupHighlight(string targetName)
    {
        // 기존 하이라이트 제거
        if (tutorialHighlight != null)
            tutorialHighlight.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);

        // 타겟이 없으면 종료
        if (string.IsNullOrEmpty(targetName)) return;

        // 타겟 찾기
        GameObject target = GameObject.Find(targetName);
        if (target == null) return;

        // 하이라이트 표시
        if (tutorialHighlight != null)
        {
            tutorialHighlight.SetActive(true);
            tutorialHighlight.transform.position = target.transform.position;

            RectTransform highlightRect = tutorialHighlight.GetComponent<RectTransform>();
            RectTransform targetRect = target.GetComponent<RectTransform>();

            if (highlightRect != null && targetRect != null)
            {
                highlightRect.sizeDelta = targetRect.sizeDelta;
            }
        }

        // 화살표 표시
        if (tutorialArrow != null)
        {
            tutorialArrow.SetActive(true);
            Vector3 arrowPos = target.transform.position + Vector3.up * 100f;
            tutorialArrow.transform.position = arrowPos;
        }
    }

    private void OnNextButtonClicked()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    private void OnSkipButtonClicked()
    {
        EndTutorial();
    }

    private void EndTutorial()
    {
        isTutorialActive = false;
        tutorialPanel.SetActive(false);

        if (tutorialHighlight != null)
            tutorialHighlight.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);

        Time.timeScale = 1f;

        // 튜토리얼 완료 저장
        MarkTutorialComplete();
    }

    // ============================================
    // 저장/로드
    // ============================================

    private bool IsFirstTimePlaying()
    {
        return !PlayerPrefs.HasKey("TutorialComplete");
    }

    private void MarkTutorialComplete()
    {
        PlayerPrefs.SetInt("TutorialComplete", 1);
        PlayerPrefs.Save();
    }

    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialComplete");
        PlayerPrefs.Save();
    }

    // ============================================
    // 특정 단계 트리거 (고급 기능)
    // ============================================

    public void ShowContextualHint(string hintTitle, string hintMessage, string targetName = null)
    {
        if (isTutorialActive) return;

        StartCoroutine(ShowHintCoroutine(hintTitle, hintMessage, targetName));
    }

    private IEnumerator ShowHintCoroutine(string title, string message, string targetName)
    {
        tutorialPanel.SetActive(true);

        if (tutorialTitleText != null)
            tutorialTitleText.text = title;

        if (tutorialMessageText != null)
            tutorialMessageText.text = message;

        SetupHighlight(targetName);

        // 다음 버튼만 표시
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            Text buttonText = nextButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "확인";
        }

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // 버튼 클릭 대기
        bool buttonClicked = false;
        System.Action clickHandler = () => buttonClicked = true;

        if (nextButton != null)
            nextButton.onClick.AddListener(() => clickHandler());

        while (!buttonClicked)
        {
            yield return null;
        }

        tutorialPanel.SetActive(false);

        if (tutorialHighlight != null)
            tutorialHighlight.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);
    }
}

// ============================================
// 튜토리얼 단계 데이터
// ============================================

[System.Serializable]
public class TutorialStep
{
    public string title;
    [TextArea(3, 10)]
    public string message;
    public string highlightTarget; // GameObject 이름
    public bool pauseGame = true;
    public bool isLastStep = false;
}