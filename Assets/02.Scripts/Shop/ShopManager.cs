using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using SpiritAge.Core;
using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using SpiritAge.Units;
using SpiritAge.UI;
using SpiritAge.Evolution;
using SpiritAge.Utility;
using SpiritAge.Utility.Pooling;
using SpiritAge.Utility.Probability;

namespace SpiritAge.Shop
{
    /// <summary>
    /// 상점 시스템 매니저
    /// </summary>
    public class ShopManager : AbstractSingleton<ShopManager>
    {
        [Header("Shop Configuration")]
        [SerializeField] private int shopSlotCount = 5;
        [SerializeField] private int refreshCost = 1;
        [SerializeField] private int spiritUnlockRound = 2;

        [Header("UI References")]
        [SerializeField] private Transform shopContainer;
        [SerializeField] private Transform spiritShopContainer;
        [SerializeField] private GameObject shopCardPrefab;
        [SerializeField] private GameObject refreshButton;

        // Shop Data
        private List<ShopCard> currentShopCards = new List<ShopCard>();
        private ShopCard currentSpiritCard;
        private int refreshCount = 0;

        // Unit Pools
        private List<UnitData> followerPool;
        private List<UnitData> spiritPool;

        // Events
        public event Action<BaseUnit> OnUnitPurchased;
        public event Action<BaseUnit> OnUnitSold;
        public event Action OnShopRefreshed;

        protected override void OnSingletonAwake()
        {
            InitializeUnitPools();
            SubscribeToEvents();
        }

        /// <summary>
        /// 유닛 풀 초기화
        /// </summary>
        private void InitializeUnitPools()
        {
            followerPool = GameDataManager.Instance.GetUnitsByType(UnitType.Follower);
            spiritPool = GameDataManager.Instance.GetUnitsByType(UnitType.Spirit);

            Debug.Log($"[ShopManager] Initialized with {followerPool.Count} followers, {spiritPool.Count} spirits");
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            BackendGameManager.Instance.OnShopPhaseStart += OnShopPhaseStart;
        }

        /// <summary>
        /// 상점 페이즈 시작
        /// </summary>
        private void OnShopPhaseStart()
        {
            refreshCount = 0;
            RefreshShop(true); // First refresh is free

            // Show spirit shop if unlocked
            if (BackendGameManager.Instance.CurrentRound >= spiritUnlockRound)
            {
                ShowSpiritShop();
            }
        }

        /// <summary>
        /// 상점 새로고침
        /// </summary>
        public void RefreshShop(bool isFree = false)
        {
            // Check gold
            if (!isFree && !BackendGameManager.Instance.SpendGold(refreshCost))
            {
                Debug.Log("[ShopManager] Not enough gold to refresh!");
                UIManager.Instance.ShowNotification("골드가 부족합니다!", NotificationType.Error);
                return;
            }

            // Clear current shop
            ClearShopCards();

            // Generate new cards
            var selectedUnits = SelectRandomUnits(shopSlotCount);

            for (int i = 0; i < selectedUnits.Count; i++)
            {
                CreateShopCard(selectedUnits[i], i);
            }

            refreshCount++;
            OnShopRefreshed?.Invoke();

            // Check for negotiator evolution
            CheckRefreshEvolution();

            // Animation
            AnimateShopRefresh();
        }

        /// <summary>
        /// 랜덤 유닛 선택 (라운드 기반 가중치)
        /// </summary>
        private List<UnitData> SelectRandomUnits(int count)
        {
            var selectedUnits = new List<UnitData>();
            int currentRound = BackendGameManager.Instance.CurrentRound;

            // Create weighted pool based on round
            var weightedPool = new Dictionary<UnitData, float>();

            foreach (var unit in followerPool)
            {
                float weight = CalculateUnitWeight(unit, currentRound);
                weightedPool[unit] = weight;
            }

            // Select units
            for (int i = 0; i < count; i++)
            {
                var selected = ProbabilitySystem.WeightedRandom(weightedPool);
                if (selected != null)
                {
                    selectedUnits.Add(selected);
                    // Don't remove from pool to allow duplicates
                }
            }

            return selectedUnits;
        }

        /// <summary>
        /// 유닛 가중치 계산
        /// </summary>
        private float CalculateUnitWeight(UnitData unit, int round)
        {
            float baseWeight = 1f;

            // Adjust weight based on unit cost and round
            if (unit.cost <= round)
            {
                baseWeight *= 2f;
            }
            else if (unit.cost > round + 2)
            {
                baseWeight *= 0.5f;
            }

            // Evolution units have lower weight
            if (unit.evolutionType != EvolutionType.None &&
                unit.evolutionType != EvolutionType.Swordsman &&
                unit.evolutionType != EvolutionType.Mage &&
                unit.evolutionType != EvolutionType.Researcher &&
                unit.evolutionType != EvolutionType.Negotiator)
            {
                baseWeight *= 0.3f;
            }

            return baseWeight;
        }

        /// <summary>
        /// 상점 카드 생성
        /// </summary>
        private void CreateShopCard(UnitData unitData, int slotIndex)
        {
            GameObject cardObj = PoolManager.Instance.Spawn("ShopCard",
                shopContainer.position + Vector3.right * slotIndex * 2f,
                Quaternion.identity);

            if (cardObj == null)
            {
                cardObj = Instantiate(shopCardPrefab, shopContainer);
            }

            var shopCard = cardObj.GetComponent<ShopCard>();
            if (shopCard == null)
            {
                shopCard = cardObj.AddComponent<ShopCard>();
            }

            shopCard.Initialize(unitData, slotIndex);
            shopCard.OnCardPurchased = HandleUnitPurchase;

            currentShopCards.Add(shopCard);
        }

        /// <summary>
        /// 신령 상점 표시
        /// </summary>
        private void ShowSpiritShop()
        {
            if (spiritShopContainer == null || spiritPool.Count == 0) return;

            // Check if player already has a spirit
            bool hasSpirit = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits
                .Any(u => u.UnitType == UnitType.Spirit);

            if (!hasSpirit && currentSpiritCard == null)
            {
                var randomSpirit = ProbabilitySystem.RandomElement(spiritPool);

                GameObject cardObj = Instantiate(shopCardPrefab, spiritShopContainer);
                currentSpiritCard = cardObj.GetComponent<ShopCard>();
                currentSpiritCard.Initialize(randomSpirit, -1);
                currentSpiritCard.OnCardPurchased = HandleSpiritPurchase;
                currentSpiritCard.SetGlow(true); // Special glow for spirit
            }
        }

        /// <summary>
        /// 유닛 구매 처리
        /// </summary>
        private void HandleUnitPurchase(ShopCard card, UnitData unitData)
        {
            // Check gold
            if (!BackendGameManager.Instance.SpendGold(unitData.cost))
            {
                UIManager.Instance.ShowNotification("골드가 부족합니다!", NotificationType.Error);
                return;
            }

            // Check formation space
            if (BackendGameManager.Instance.CurrentPlayerDeck.formation.Count >= 6)
            {
                UIManager.Instance.ShowNotification("편성 슬롯이 가득 찼습니다!", NotificationType.Warning);
                return;
            }

            // Create unit
            var unit = CreateUnit(unitData);

            // Add to player deck
            BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Add(unit);

            // Auto place in formation
            AutoPlaceInFormation(unit);

            // Remove card from shop
            currentShopCards.Remove(card);
            card.PlayPurchaseAnimation();

            OnUnitPurchased?.Invoke(unit);

            // Check evolution conditions
            EvolutionManager.Instance.CheckEvolutionConditions(unit);

            UIManager.Instance.ShowNotification($"{unitData.name} 구매 완료!", NotificationType.Success);
        }

        /// <summary>
        /// 신령 구매 처리
        /// </summary>
        private void HandleSpiritPurchase(ShopCard card, UnitData unitData)
        {
            // Check gold
            if (!BackendGameManager.Instance.SpendGold(unitData.cost))
            {
                UIManager.Instance.ShowNotification("골드가 부족합니다!", NotificationType.Error);
                return;
            }

            // Remove existing spirit
            var existingSpirit = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits
                .FirstOrDefault(u => u.UnitType == UnitType.Spirit);

            if (existingSpirit != null)
            {
                BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Remove(existingSpirit);
                BackendGameManager.Instance.CurrentPlayerDeck.formation.Remove(existingSpirit);
                Destroy(existingSpirit.gameObject);
            }

            // Create new spirit
            var spirit = CreateUnit(unitData);

            // Add to player deck (spirit always at front)
            BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Add(spirit);
            BackendGameManager.Instance.CurrentPlayerDeck.formation.Insert(0, spirit);

            // Remove card
            currentSpiritCard = null;
            card.PlayPurchaseAnimation();

            OnUnitPurchased?.Invoke(spirit);

            UIManager.Instance.ShowNotification($"신령 {unitData.name} 획득!", NotificationType.Success);
        }

        /// <summary>
        /// 유닛 생성
        /// </summary>
        private BaseUnit CreateUnit(UnitData data)
        {
            GameObject unitObj = PoolManager.Instance.Spawn("Unit");

            if (unitObj == null)
            {
                unitObj = new GameObject($"Unit_{data.id}");
            }

            BaseUnit unit = unitObj.GetComponent<BaseUnit>();
            if (unit == null)
            {
                // Add appropriate unit component based on type
                switch (data.evolutionType)
                {
                    case EvolutionType.Swordsman:
                    case EvolutionType.BerserkerSwordsman:
                    case EvolutionType.GuardianSwordsman:
                    case EvolutionType.WindSwordsman:
                        unit = unitObj.AddComponent<SwordsmanUnit>();
                        break;

                    case EvolutionType.Mage:
                    case EvolutionType.IceMage:
                    case EvolutionType.FireMage:
                    case EvolutionType.LightningMage:
                        unit = unitObj.AddComponent<MageUnit>();
                        break;

                    default:
                        unit = unitObj.AddComponent<BaseUnit>();
                        break;
                }
            }

            unit.Initialize(data);

            // Add draggable component
            var draggable = unitObj.GetComponent<DraggableUnit>();
            if (draggable == null)
            {
                draggable = unitObj.AddComponent<DraggableUnit>();
            }

            return unit;
        }

        /// <summary>
        /// 자동 편성 배치
        /// </summary>
        private void AutoPlaceInFormation(BaseUnit unit)
        {
            var formation = BackendGameManager.Instance.CurrentPlayerDeck.formation;

            if (unit.UnitType == UnitType.Spirit)
            {
                // Spirit always at front
                formation.Insert(0, unit);
            }
            else
            {
                // Regular units at back
                formation.Add(unit);
            }

            // Update visual placement
            UpdateFormationVisuals();
        }

        /// <summary>
        /// 편성 비주얼 업데이트
        /// </summary>
        private void UpdateFormationVisuals()
        {
            var slots = FindObjectsOfType<FormationSlot>()
                .OrderBy(s => s.SlotIndex)
                .ToArray();

            var formation = BackendGameManager.Instance.CurrentPlayerDeck.formation;

            for (int i = 0; i < formation.Count && i < slots.Length; i++)
            {
                formation[i].transform.SetParent(slots[i].transform);
                formation[i].transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// 유닛 판매
        /// </summary>
        public void SellUnit(BaseUnit unit)
        {
            if (!BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Contains(unit))
            {
                Debug.LogWarning("[ShopManager] Trying to sell unit not owned!");
                return;
            }

            int sellPrice = CalculateSellPrice(unit);

            // Special sell effects
            CheckSellEffects(unit, ref sellPrice);

            // Remove from deck
            BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Remove(unit);
            BackendGameManager.Instance.CurrentPlayerDeck.formation.Remove(unit);

            // Add gold
            BackendGameManager.Instance.AddGold(sellPrice);

            // Destroy unit
            PoolManager.Instance.Despawn("Unit", unit.gameObject);

            OnUnitSold?.Invoke(unit);

            UIManager.Instance.ShowNotification($"{unit.Name} 판매: +{sellPrice} 골드", NotificationType.Info);
        }

        /// <summary>
        /// 판매 가격 계산
        /// </summary>
        private int CalculateSellPrice(BaseUnit unit)
        {
            var unitData = GameDataManager.Instance.GetUnitData(unit.Id);
            return Mathf.Max(1, unitData.cost / 2);
        }

        /// <summary>
        /// 판매 특수 효과 체크
        /// </summary>
        private void CheckSellEffects(BaseUnit unit, ref int sellPrice)
        {
            // Check for special sell skills
            if (unit.Name.Contains("몰락한 귀족"))
            {
                sellPrice += 3;
                Debug.Log("[ShopManager] Noble bonus gold on sell!");
            }
            else if (unit.Name.Contains("근육질 노예"))
            {
                // Boost spirit stats
                var spirit = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits
                    .FirstOrDefault(u => u.UnitType == UnitType.Spirit);

                if (spirit != null)
                {
                    var modifier = new StatModifier(2, StatModifier.ModifierType.Flat, unit);
                    spirit.Stats.ApplyModifier(modifier);
                    Debug.Log($"[ShopManager] {spirit.Name} stats permanently increased!");
                }
            }
        }

        /// <summary>
        /// 새로고침 진화 체크
        /// </summary>
        private void CheckRefreshEvolution()
        {
            if (refreshCount == 1)
            {
                var negotiators = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits
                    .Where(u => u.EvolutionType == EvolutionType.Negotiator);

                foreach (var negotiator in negotiators)
                {
                    EvolutionManager.Instance.EvolveUnit(negotiator, EvolutionType.Clockmaker);
                }
            }
        }

        /// <summary>
        /// 상점 카드 제거
        /// </summary>
        private void ClearShopCards()
        {
            foreach (var card in currentShopCards)
            {
                if (card != null)
                {
                    PoolManager.Instance.Despawn("ShopCard", card.gameObject);
                }
            }
            currentShopCards.Clear();
        }

        /// <summary>
        /// 상점 새로고침 애니메이션
        /// </summary>
        private void AnimateShopRefresh()
        {
            float delay = 0f;
            foreach (var card in currentShopCards)
            {
                card.transform.localScale = Vector3.zero;
                card.transform.DOScale(1f, 0.3f)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack);
                delay += 0.1f;
            }
        }

        /// <summary>
        /// 연금술사 합성
        /// </summary>
        public bool MergeUnits(BaseUnit unit1, BaseUnit unit2)
        {
            // Check for alchemist
            bool hasAlchemist = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits
                .Any(u => u.EvolutionType == EvolutionType.Alchemist);

            if (!hasAlchemist)
            {
                UIManager.Instance.ShowNotification("연금술사가 필요합니다!", NotificationType.Warning);
                return false;
            }

            // Check if same type
            if (unit1.EvolutionType != unit2.EvolutionType ||
                unit1.UnitType != UnitType.Follower)
            {
                UIManager.Instance.ShowNotification("같은 타입의 신도자만 합성 가능합니다!", NotificationType.Warning);
                return false;
            }

            // Merge stats
            unit1.Stats.Attack += unit2.Stats.Attack / 2;
            unit1.Stats.MaxHealth += unit2.Stats.MaxHealth / 2;
            unit1.Stats.Health = unit1.Stats.MaxHealth;
            unit1.Stats.Speed += unit2.Stats.Speed / 2;

            // Merge attributes
            foreach (var attr in unit2.Attributes)
            {
                if (!unit1.Attributes.Contains(attr))
                {
                    unit1.Attributes.Add(attr);
                }
            }

            // Remove unit2
            BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Remove(unit2);
            BackendGameManager.Instance.CurrentPlayerDeck.formation.Remove(unit2);
            PoolManager.Instance.Despawn("Unit", unit2.gameObject);

            // Effects
            PlayMergeEffect(unit1.transform.position);

            UIManager.Instance.ShowNotification("합성 성공!", NotificationType.Success);

            return true;
        }

        /// <summary>
        /// 합성 이펙트
        /// </summary>
        private void PlayMergeEffect(Vector3 position)
        {
            var effect = PoolManager.Instance.Spawn("MergeEffect", position, Quaternion.identity);
            if (effect != null)
            {
                PoolManager.Instance.Despawn("MergeEffect", effect, 2f);
            }
        }

        private void OnDestroy()
        {
            if (BackendGameManager.Instance != null)
            {
                BackendGameManager.Instance.OnShopPhaseStart -= OnShopPhaseStart;
            }
        }
    }
}