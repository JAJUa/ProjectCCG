using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Battle;

namespace SpiritAge.Units
{
    /// <summary>
    /// 드래그 가능한 유닛 컴포넌트
    /// </summary>
    public class DraggableUnit : MonoBehaviour, IDraggable, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Drag Settings")]
        [SerializeField] private float dragScale = 1.1f;
        [SerializeField] private float dragAlpha = 0.8f;
        [SerializeField] private LayerMask dropLayerMask = -1;

        private BaseUnit unit;
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 originalPosition;
        private Transform originalParent;
        private int originalSiblingIndex;
        private bool isDragging = false;

        public event System.Action<DraggableUnit> OnDragStarted;
        public event System.Action<DraggableUnit> OnDragEnded;

        private void Awake()
        {
            unit = GetComponent<BaseUnit>();
            canvas = GetComponentInParent<Canvas>();
            rectTransform = GetComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public bool CanDrag()
        {
            // Check if unit can be dragged (e.g., during formation phase)
            return BattleManager.Instance.CurrentPhase == GamePhase.Formation;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag()) return;

            OnDragStart(transform.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector3 worldPoint;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out worldPoint))
            {
                OnDrag(worldPoint);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging) return;

            OnDragEnd(transform.position);
        }

        public void OnDragStart(Vector3 position)
        {
            if (!CanDrag()) return;

            isDragging = true;
            originalPosition = transform.position;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();

            // Visual feedback
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = dragAlpha;
            transform.DOScale(dragScale, 0.2f);

            // Move to top layer
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling();

            OnDragStarted?.Invoke(this);
        }

        public void OnDrag(Vector3 position)
        {
            if (!isDragging) return;

            transform.position = position;

            // Check for valid drop targets
            CheckDropTargets(position);
        }

        public void OnDragEnd(Vector3 position)
        {
            if (!isDragging) return;

            isDragging = false;

            // Try to drop
            IDropSlot dropSlot = GetDropSlot(position);

            if (dropSlot != null && dropSlot.CanAcceptDrop(this))
            {
                dropSlot.OnDropReceived(this);
            }
            else
            {
                // Return to original position
                ReturnToOriginalPosition();
            }

            // Reset visual
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            transform.DOScale(1f, 0.2f);

            OnDragEnded?.Invoke(this);
        }

        private void CheckDropTargets(Vector3 position)
        {
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero, 0f, dropLayerMask);

            if (hit.collider != null)
            {
                var dropSlot = hit.collider.GetComponent<IDropSlot>();
                dropSlot?.OnDropHover(this);
            }
        }

        private IDropSlot GetDropSlot(Vector3 position)
        {
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero, 0f, dropLayerMask);

            if (hit.collider != null)
            {
                return hit.collider.GetComponent<IDropSlot>();
            }

            return null;
        }

        private void ReturnToOriginalPosition()
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
        }

        public BaseUnit GetUnit()
        {
            return unit;
        }
    }
}