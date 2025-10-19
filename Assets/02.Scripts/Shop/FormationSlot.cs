using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using SpiritAge.Core.Interfaces;
using SpiritAge.Units;
using SpiritAge.Core;

namespace SpiritAge.Shop
{
    /// <summary>
    /// 편성 슬롯 (드롭 가능)
    /// </summary>
    public class FormationSlot : MonoBehaviour, IDropSlot
    {
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex;
        [SerializeField] private Transform unitContainer;
        [SerializeField] private GameObject highlightEffect;

        private BaseUnit currentUnit;

        public int SlotIndex => slotIndex;
        public bool IsEmpty => currentUnit == null;

        private void Awake()
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(false);
        }

        /// <summary>
        /// 드롭 가능 여부 체크
        /// </summary>
        public bool CanAcceptDrop(IDraggable draggable)
        {
            var dragUnit = draggable as DraggableUnit;
            if (dragUnit == null) return false;

            // Check if slot is empty or can swap
            return IsEmpty || CanSwapUnits(dragUnit.GetUnit());
        }

        /// <summary>
        /// 드롭 받기
        /// </summary>
        public void OnDropReceived(IDraggable draggable)
        {
            var dragUnit = draggable as DraggableUnit;
            if (dragUnit == null) return;

            var unit = dragUnit.GetUnit();

            if (IsEmpty)
            {
                // Place unit in empty slot
                PlaceUnit(unit);
            }
            else
            {
                // Swap units
                SwapUnits(unit);
            }

            OnDropExit(draggable);
        }

        /// <summary>
        /// 드롭 호버
        /// </summary>
        public void OnDropHover(IDraggable draggable)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(true);
                highlightEffect.transform.DOScale(1.1f, 0.2f).SetLoops(-1, LoopType.Yoyo);
            }
        }

        /// <summary>
        /// 드롭 종료
        /// </summary>
        public void OnDropExit(IDraggable draggable)
        {
            if (highlightEffect != null)
            {
                DOTween.Kill(highlightEffect.transform);
                highlightEffect.transform.localScale = Vector3.one;
                highlightEffect.SetActive(false);
            }
        }

        /// <summary>
        /// 유닛 배치
        /// </summary>
        private void PlaceUnit(BaseUnit unit)
        {
            currentUnit = unit;
            unit.transform.SetParent(unitContainer);
            unit.transform.localPosition = Vector3.zero;
            unit.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // Update formation
            UpdateFormation();
        }

        /// <summary>
        /// 유닛 교체
        /// </summary>
        private void SwapUnits(BaseUnit newUnit)
        {
            var oldUnit = currentUnit;

            // Find new unit's previous slot
            var previousSlot = FindUnitSlot(newUnit);

            // Perform swap
            if (previousSlot != null)
            {
                previousSlot.PlaceUnit(oldUnit);
            }

            PlaceUnit(newUnit);
        }

        /// <summary>
        /// 유닛 교체 가능 여부
        /// </summary>
        private bool CanSwapUnits(BaseUnit unit)
        {
            // Check game rules for swapping
            return true;
        }

        /// <summary>
        /// 유닛이 있는 슬롯 찾기
        /// </summary>
        private FormationSlot FindUnitSlot(BaseUnit unit)
        {
            var slots = FindObjectsOfType<FormationSlot>();
            foreach (var slot in slots)
            {
                if (slot.currentUnit == unit)
                    return slot;
            }
            return null;
        }

        /// <summary>
        /// 편성 업데이트
        /// </summary>
        private void UpdateFormation()
        {
            // Update BackendGameManager's formation
            var formation = new System.Collections.Generic.List<BaseUnit>();
            var slots = FindObjectsOfType<FormationSlot>();
            System.Array.Sort(slots, (a, b) => a.slotIndex.CompareTo(b.slotIndex));

            foreach (var slot in slots)
            {
                if (!slot.IsEmpty)
                {
                    formation.Add(slot.currentUnit);
                }
            }

            BackendGameManager.Instance.CurrentPlayerDeck.formation = formation;
        }

        /// <summary>
        /// 슬롯 비우기
        /// </summary>
        public void ClearSlot()
        {
            currentUnit = null;
        }
    }
}