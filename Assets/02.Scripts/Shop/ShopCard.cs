using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using SpiritAge.Core;
using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Interfaces;

namespace SpiritAge.Shop
{
    /// <summary>
    /// ���� ī�� UI
    /// </summary>
    public class ShopCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPoolable
    {
        [Header("UI Components")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image unitIcon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text costText;
        [SerializeField] private Text attackText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private GameObject glowEffect;
        [SerializeField] private GameObject soldOutOverlay;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.7f);
        [SerializeField] private Color spiritColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color cannotAffordColor = new Color(0.5f, 0.5f, 0.5f);

        private UnitData unitData;
        private int slotIndex;
        private bool isPurchased = false;
        private bool canAfford = true;

        public Action<ShopCard, UnitData> OnCardPurchased;

        private Tween hoverTween;

        /// <summary>
        /// ī�� �ʱ�ȭ
        /// </summary>
        public void Initialize(UnitData data, int index)
        {
            unitData = data;
            slotIndex = index;
            isPurchased = false;

            UpdateDisplay();
            CheckAffordability();

            if (soldOutOverlay != null)
                soldOutOverlay.SetActive(false);
        }

        /// <summary>
        /// ���÷��� ������Ʈ
        /// </summary>
        private void UpdateDisplay()
        {
            if (unitData == null) return;

            // Name and cost
            if (nameText != null) nameText.text = unitData.name;
            if (costText != null) costText.text = $"{unitData.cost}G";

            // Stats
            if (attackText != null) attackText.text = unitData.baseAttack.ToString();
            if (healthText != null) healthText.text = unitData.baseHealth.ToString();
            if (speedText != null) speedText.text = unitData.baseSpeed.ToString();

            // Description
            if (descriptionText != null) descriptionText.text = unitData.description;

            // Load sprite
            if (!string.IsNullOrEmpty(unitData.spritePath) && unitIcon != null)
            {
                Sprite sprite = Resources.Load<Sprite>(unitData.spritePath);
                if (sprite != null) unitIcon.sprite = sprite;
            }

            // Card color based on type
            UpdateCardColor();
        }

        /// <summary>
        /// ī�� ���� ������Ʈ
        /// </summary>
        private void UpdateCardColor()
        {
            if (cardBackground == null) return;

            Color targetColor = normalColor;

            if (unitData.unitType == UnitType.Spirit)
            {
                targetColor = spiritColor;
                SetGlow(true);
            }
            else if (!canAfford)
            {
                targetColor = cannotAffordColor;
            }

            cardBackground.color = targetColor;
        }

        /// <summary>
        /// ���� ���� ���� üũ
        /// </summary>
        private void CheckAffordability()
        {
            int playerGold = BackendGameManager.Instance.CurrentPlayerDeck.gold;
            canAfford = playerGold >= unitData.cost;
            UpdateCardColor();
        }

        /// <summary>
        /// �۷ο� ȿ�� ����
        /// </summary>
        public void SetGlow(bool enable)
        {
            if (glowEffect != null)
            {
                glowEffect.SetActive(enable);

                if (enable)
                {
                    glowEffect.transform.DOScale(1.1f, 1f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
                else
                {
                    DOTween.Kill(glowEffect.transform);
                }
            }
        }

        // ========== Input Events ==========

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isPurchased || !canAfford) return;

            OnCardPurchased?.Invoke(this, unitData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isPurchased) return;

            // Hover animation
            hoverTween?.Kill();
            hoverTween = transform.DOScale(1.05f, 0.2f).SetEase(Ease.OutQuad);

            if (cardBackground != null && canAfford)
                cardBackground.color = hoverColor;

            // Show detailed tooltip
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isPurchased) return;

            // Reset scale
            hoverTween?.Kill();
            hoverTween = transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);

            UpdateCardColor();

            // Hide tooltip
            HideTooltip();
        }

        /// <summary>
        /// ���� ǥ��
        /// </summary>
        private void ShowTooltip()
        {
            // TODO: Implement tooltip system
            Debug.Log($"[ShopCard] Showing tooltip for {unitData.name}");
        }

        /// <summary>
        /// ���� �����
        /// </summary>
        private void HideTooltip()
        {
            // TODO: Implement tooltip system
        }

        /// <summary>
        /// ���� �ִϸ��̼�
        /// </summary>
        public void PlayPurchaseAnimation()
        {
            isPurchased = true;

            Sequence purchaseSeq = DOTween.Sequence();
            purchaseSeq.Append(transform.DOScale(1.2f, 0.2f))
                      .Append(transform.DOScale(0f, 0.3f).SetEase(Ease.InBack))
                      .Join(GetComponent<CanvasGroup>().DOFade(0f, 0.3f))
                      .OnComplete(() =>
                      {
                          if (soldOutOverlay != null)
                              soldOutOverlay.SetActive(true);

                          gameObject.SetActive(false);
                      });
        }

        // ========== IPoolable Implementation ==========

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            isPurchased = false;
        }

        public void OnDespawn()
        {
            hoverTween?.Kill();
            SetGlow(false);
        }

        public void ResetState()
        {
            unitData = null;
            slotIndex = -1;
            isPurchased = false;
            canAfford = true;
            OnCardPurchased = null;
        }

        private void OnDestroy()
        {
            hoverTween?.Kill();
            DOTween.Kill(glowEffect?.transform);
        }
    }
}