using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image cardBackground;
    public Image characterIcon;
    public Text nameText;
    public Text costText;
    public Text attackText;
    public Text healthText;
    public Text speedText;
    public Text skillText;
    public GameObject attributeContainer;
    public GameObject attributeIconPrefab;

    [Header("Visual Effects")]
    public GameObject glowEffect;
    public GameObject selectionOutline;
    public ParticleSystem purchaseEffect;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color spiritColor = new Color(1f, 0.8f, 0.2f);
    public Color evolutionColor = new Color(0.5f, 0.8f, 1f);

    // 카드 데이터
    private CharacterData characterData;
    private System.Action<CardUI> onCardClicked;
    private bool isSelected = false;
    private bool isInteractable = true;

    // 애니메이션
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private float hoverScaleMultiplier = 1.1f;
    private float animationDuration = 0.2f;

    void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        if (glowEffect != null)
            glowEffect.SetActive(false);

        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    // ============================================
    // 카드 초기화
    // ============================================

    public void Initialize(CharacterData data, System.Action<CardUI> clickCallback = null)
    {
        characterData = data;
        onCardClicked = clickCallback;

        UpdateCardDisplay();
    }

    public void UpdateCardDisplay()
    {
        if (characterData == null) return;

        // 이름
        if (nameText != null)
            nameText.text = characterData.name;

        // 비용
        if (costText != null)
            costText.text = characterData.buyCost + "G";

        // 스탯
        if (attackText != null)
            attackText.text = "⚔ " + characterData.currentAttack;

        if (healthText != null)
            healthText.text = "❤ " + characterData.currentHealth;

        if (speedText != null)
            speedText.text = "⚡ " + characterData.currentSpeed;

        // 스킬
        if (skillText != null)
        {
            skillText.text = characterData.skillDescription;
            skillText.fontSize = characterData.skillDescription.Length > 50 ? 10 : 12;
        }

        // 속성 아이콘
        UpdateAttributeIcons();

        // 카드 색상
        UpdateCardColor();

        // 캐릭터 아이콘 (스프라이트가 있다면)
        // if(characterIcon != null)
        //     characterIcon.sprite = Resources.Load<Sprite>($"Icons/{characterData.id}");
    }

    // ============================================
    // 속성 아이콘 업데이트
    // ============================================

    private void UpdateAttributeIcons()
    {
        if (attributeContainer == null || attributeIconPrefab == null) return;

        // 기존 아이콘 제거
        foreach (Transform child in attributeContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // 속성 아이콘 생성
        foreach (string attribute in characterData.attributes)
        {
            GameObject icon = Instantiate(attributeIconPrefab, attributeContainer.transform);
            Image iconImage = icon.GetComponent<Image>();

            if (iconImage != null)
            {
                iconImage.color = GetAttributeColor(attribute);
            }

            Text iconText = icon.GetComponentInChildren<Text>();
            if (iconText != null)
            {
                iconText.text = GetAttributeSymbol(attribute);
            }
        }
    }

    private Color GetAttributeColor(string attribute)
    {
        switch (attribute)
        {
            case "Ice": return new Color(0.5f, 0.8f, 1f);
            case "Fire": return new Color(1f, 0.4f, 0.2f);
            case "Lightning": return new Color(1f, 1f, 0.3f);
            case "Soul": return new Color(0.7f, 0.5f, 0.9f);
            case "Time": return new Color(0.8f, 0.8f, 0.8f);
            case "Luck": return new Color(0.2f, 1f, 0.5f);
            default: return Color.white;
        }
    }

    private string GetAttributeSymbol(string attribute)
    {
        switch (attribute)
        {
            case "Ice": return "❄";
            case "Fire": return "🔥";
            case "Lightning": return "⚡";
            case "Soul": return "👻";
            case "Time": return "⏰";
            case "Luck": return "🍀";
            default: return "?";
        }
    }

    // ============================================
    // 카드 색상 업데이트
    // ============================================

    private void UpdateCardColor()
    {
        if (cardBackground == null) return;

        Color targetColor = normalColor;

        // 신령은 특별한 색상
        if (characterData.type == CharacterType.Spirit)
        {
            targetColor = spiritColor;

            if (glowEffect != null)
                glowEffect.SetActive(true);
        }

        // 진화한 캐릭터는 진화 색상
        else if (characterData.evolutionType != EvolutionType.None &&
                characterData.evolutionType != EvolutionType.Swordsman &&
                characterData.evolutionType != EvolutionType.Mage &&
                characterData.evolutionType != EvolutionType.Researcher &&
                characterData.evolutionType != EvolutionType.Negotiator)
        {
            targetColor = evolutionColor;
        }

        cardBackground.color = targetColor;
    }

    // ============================================
    // 인터랙션
    // ============================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;

        // 호버 애니메이션
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale * hoverScaleMultiplier));

        // 테두리 표시
        if (selectionOutline != null)
            selectionOutline.SetActive(true);

        // 상세 정보 표시 (툴팁 등)
        ShowDetailTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;

        if (!isSelected)
        {
            // 원래 크기로
            StopAllCoroutines();
            StartCoroutine(AnimateScale(originalScale));

            if (selectionOutline != null)
                selectionOutline.SetActive(false);
        }

        HideDetailTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;

        // 클릭 콜백 호출
        onCardClicked?.Invoke(this);

        // 클릭 효과
        PlayClickEffect();
    }

    // ============================================
    // 선택 상태
    // ============================================

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionOutline != null)
            selectionOutline.SetActive(selected);

        if (selected)
        {
            transform.localScale = originalScale * hoverScaleMultiplier;
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        // 비활성화 시 회색으로
        if (cardBackground != null)
        {
            cardBackground.color = interactable ?
                (characterData.type == CharacterType.Spirit ? spiritColor : normalColor) :
                Color.gray;
        }
    }

    // ============================================
    // 애니메이션
    // ============================================

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    public void PlayPurchaseAnimation()
    {
        StartCoroutine(PurchaseAnimationCoroutine());
    }

    private IEnumerator PurchaseAnimationCoroutine()
    {
        // 파티클 효과
        if (purchaseEffect != null)
        {
            purchaseEffect.Play();
        }

        // 카드가 위로 날아가며 사라지는 효과
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + Vector3.up * 100f;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            // 페이드 아웃
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void PlayClickEffect()
    {
        // 클릭 시 작은 펄스 효과
        StopAllCoroutines();
        StartCoroutine(PulseEffect());
    }

    private IEnumerator PulseEffect()
    {
        Vector3 pulseScale = originalScale * 1.15f;
        float pulseDuration = 0.1f;

        // 크게
        yield return StartCoroutine(AnimateScale(pulseScale));

        // 작게
        yield return StartCoroutine(AnimateScale(originalScale * hoverScaleMultiplier));
    }

    // ============================================
    // 툴팁
    // ============================================

    private void ShowDetailTooltip()
    {
        // 상세 정보 툴팁 표시
        // TooltipManager.Instance?.ShowTooltip(characterData, transform.position);

        /*Debug.Log($"[카드 정보]\n" +
                 $"이름: {characterData.name}\n" +
                 $"타입: {characterData.type}\n" +
                 $"진화: {characterData.evolutionType}\n" +
                 $"스킬: {characterData.skillDescription}");*/
    }

    private void HideDetailTooltip()
    {
        // TooltipManager.Instance?.HideTooltip();
    }

    // ============================================
    // 공개 메서드
    // ============================================

    public CharacterData GetCharacterData()
    {
        return characterData;
    }

    public void UpdateStats(int attack, int health, int speed)
    {
        if (characterData != null)
        {
            characterData.currentAttack = attack;
            characterData.currentHealth = health;
            characterData.currentSpeed = speed;
            UpdateCardDisplay();
        }
    }

    public void ShowEvolutionEffect()
    {
        StartCoroutine(EvolutionEffectCoroutine());
    }

    private IEnumerator EvolutionEffectCoroutine()
    {
        // 진화 이펙트
        if (glowEffect != null)
        {
            glowEffect.SetActive(true);
        }

        // 빛나는 효과
        Color originalColor = cardBackground.color;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 2f, 1f);

            cardBackground.color = Color.Lerp(originalColor, Color.white, t * 0.5f);

            yield return null;
        }

        cardBackground.color = evolutionColor;
        UpdateCardDisplay();

        if (glowEffect != null)
        {
            glowEffect.SetActive(false);
        }
    }
}

// ============================================
// 전투 유닛 UI
// ============================================

public class BattleUnitUI : MonoBehaviour
{
    [Header("UI References")]
    public Image unitIcon;
    public Text nameText;
    public Slider healthBar;
    public Text healthText;
    public Transform buffContainer;
    public GameObject buffIconPrefab;

    [Header("Effects")]
    public GameObject damageTextPrefab;
    public GameObject healEffectPrefab;
    public GameObject deathEffectPrefab;
    public Animator animator;

    private CharacterData characterData;
    private int maxHealth;

    public void Initialize(CharacterData data)
    {
        characterData = data;
        maxHealth = data.currentHealth;

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (characterData == null) return;

        // 이름
        if (nameText != null)
            nameText.text = characterData.name;

        // 체력 바
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = characterData.currentHealth;
        }

        if (healthText != null)
            healthText.text = $"{characterData.currentHealth}/{maxHealth}";

        // 버프 아이콘
        UpdateBuffIcons();

        // 사망 체크
        if (characterData.currentHealth <= 0)
        {
            PlayDeathAnimation();
        }
    }

    private void UpdateBuffIcons()
    {
        if (buffContainer == null) return;

        // 기존 버프 아이콘 제거
        foreach (Transform child in buffContainer)
        {
            Destroy(child.gameObject);
        }

        // 버프 아이콘 생성
        foreach (var buff in characterData.buffs)
        {
            GameObject icon = Instantiate(buffIconPrefab, buffContainer);
            Image iconImage = icon.GetComponent<Image>();

            if (iconImage != null)
            {
                iconImage.color = GetBuffColor(buff.type);
            }

            Text stackText = icon.GetComponentInChildren<Text>();
            if (stackText != null && buff.stack > 1)
            {
                stackText.text = buff.stack.ToString();
            }
        }
    }

    private Color GetBuffColor(BuffType type)
    {
        switch (type)
        {
            case BuffType.Freeze: return new Color(0.5f, 0.8f, 1f);
            case BuffType.Burn: return new Color(1f, 0.4f, 0.2f);
            case BuffType.Lightning: return new Color(1f, 1f, 0.3f);
            case BuffType.Soul: return new Color(0.7f, 0.5f, 0.9f);
            case BuffType.Madness: return new Color(1f, 0.2f, 0.2f);
            case BuffType.Stun: return new Color(0.5f, 0.5f, 0.5f);
            default: return Color.white;
        }
    }

    public void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void PlayDamageAnimation(int damage)
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        ShowDamageText(damage);
    }

    private void ShowDamageText(int damage)
    {
        if (damageTextPrefab != null)
        {
            GameObject damageObj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
            Text damageText = damageObj.GetComponentInChildren<Text>();
            if (damageText != null)
            {
                damageText.text = "-" + damage;
            }

            Destroy(damageObj, 1f);
        }
    }

    public void PlayHealAnimation(int healAmount)
    {
        if (healEffectPrefab != null)
        {
            GameObject healObj = Instantiate(healEffectPrefab, transform.position, Quaternion.identity);
            Destroy(healObj, 2f);
        }

        ShowHealText(healAmount);
    }

    private void ShowHealText(int heal)
    {
        if (damageTextPrefab != null)
        {
            GameObject healObj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
            Text healText = healObj.GetComponentInChildren<Text>();
            if (healText != null)
            {
                healText.text = "+" + heal;
                healText.color = Color.green;
            }

            Destroy(healObj, 1f);
        }
    }

    private void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        if (deathEffectPrefab != null)
        {
            GameObject deathObj = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(deathObj, 2f);
        }

        // 페이드 아웃
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}