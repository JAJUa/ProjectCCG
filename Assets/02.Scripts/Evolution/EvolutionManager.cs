using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using SpiritAge.Core;
using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Interfaces;
using SpiritAge.Units;
using SpiritAge.Utility;
using SpiritAge.Utility.Pooling;
using SpiritAge.UI;

namespace SpiritAge.Evolution
{
    /// <summary>
    /// 진화 시스템 매니저
    /// </summary>
    public class EvolutionManager : AbstractSingleton<EvolutionManager>
    {
        [Header("Evolution Settings")]
        [SerializeField] private int swordsmanAttackThreshold = 15;
        [SerializeField] private int swordsmanHealthThreshold = 20;
        [SerializeField] private int swordsmanSpeedThreshold = 10;
        [SerializeField] private int elementalCountRequired = 3;
        [SerializeField] private int gamblerGoldThreshold = 30;

        [Header("Effects")]
        [SerializeField] private GameObject evolutionEffectPrefab;

        // Evolution conditions registry
        private Dictionary<EvolutionType, List<IEvolutionCondition>> evolutionConditions;

        // Events
        public event Action<BaseUnit, EvolutionType> OnUnitEvolved;

        protected override void OnSingletonAwake()
        {
            InitializeEvolutionConditions();
        }

        /// <summary>
        /// 진화 조건 초기화
        /// </summary>
        private void InitializeEvolutionConditions()
        {
            evolutionConditions = new Dictionary<EvolutionType, List<IEvolutionCondition>>();

            // Swordsman evolutions
            evolutionConditions[EvolutionType.BerserkerSwordsman] = new List<IEvolutionCondition>
            {
                new StatThresholdCondition("Attack", swordsmanAttackThreshold)
            };

            evolutionConditions[EvolutionType.GuardianSwordsman] = new List<IEvolutionCondition>
            {
                new StatThresholdCondition("Health", swordsmanHealthThreshold)
            };

            evolutionConditions[EvolutionType.WindSwordsman] = new List<IEvolutionCondition>
            {
                new StatThresholdCondition("Speed", swordsmanSpeedThreshold)
            };

            // Mage evolutions
            evolutionConditions[EvolutionType.IceMage] = new List<IEvolutionCondition>
            {
                new ElementalCountCondition(ElementAttribute.Ice, elementalCountRequired)
            };

            evolutionConditions[EvolutionType.FireMage] = new List<IEvolutionCondition>
            {
                new ElementalCountCondition(ElementAttribute.Fire, elementalCountRequired)
            };

            evolutionConditions[EvolutionType.LightningMage] = new List<IEvolutionCondition>
            {
                new ElementalCountCondition(ElementAttribute.Lightning, elementalCountRequired)
            };

            // Researcher evolutions
            evolutionConditions[EvolutionType.Spiritist] = new List<IEvolutionCondition>
            {
                new CustomCondition("Has soul attribute unit", (unit, context) =>
                {
                    return context.OwnedUnits.Any(u => u.Attributes.Contains(ElementAttribute.Soul));
                })
            };

            evolutionConditions[EvolutionType.Alchemist] = new List<IEvolutionCondition>
            {
                new CustomCondition("6 units in formation", (unit, context) =>
                {
                    return context.Formation.Count >= 6;
                })
            };

            // Negotiator evolutions
            evolutionConditions[EvolutionType.Gambler] = new List<IEvolutionCondition>
            {
                new CustomCondition("30+ gold", (unit, context) =>
                {
                    return context.PlayerGold >= gamblerGoldThreshold;
                })
            };
        }

        /// <summary>
        /// 진화 조건 체크
        /// </summary>
        public void CheckEvolutionConditions(BaseUnit unit)
        {
            if (unit == null || !CanEvolve(unit)) return;

            var context = CreateGameContext();

            // Check each possible evolution for this unit type
            var possibleEvolutions = GetPossibleEvolutions(unit.EvolutionType);

            foreach (var evolution in possibleEvolutions)
            {
                if (CheckEvolutionCondition(unit, evolution, context))
                {
                    EvolveUnit(unit, evolution);
                    break; // Only one evolution per check
                }
            }
        }

        /// <summary>
        /// 유닛이 진화 가능한지 체크
        /// </summary>
        private bool CanEvolve(BaseUnit unit)
        {
            // Check if unit is base type that can evolve
            return unit.EvolutionType == EvolutionType.Swordsman ||
                   unit.EvolutionType == EvolutionType.Mage ||
                   unit.EvolutionType == EvolutionType.Researcher ||
                   unit.EvolutionType == EvolutionType.Negotiator;
        }

        /// <summary>
        /// 가능한 진화 목록 가져오기
        /// </summary>
        private List<EvolutionType> GetPossibleEvolutions(EvolutionType baseType)
        {
            switch (baseType)
            {
                case EvolutionType.Swordsman:
                    return new List<EvolutionType>
                    {
                        EvolutionType.BerserkerSwordsman,
                        EvolutionType.GuardianSwordsman,
                        EvolutionType.WindSwordsman
                    };

                case EvolutionType.Mage:
                    return new List<EvolutionType>
                    {
                        EvolutionType.IceMage,
                        EvolutionType.FireMage,
                        EvolutionType.LightningMage
                    };

                case EvolutionType.Researcher:
                    return new List<EvolutionType>
                    {
                        EvolutionType.Spiritist,
                        EvolutionType.Alchemist
                    };

                case EvolutionType.Negotiator:
                    return new List<EvolutionType>
                    {
                        EvolutionType.Clockmaker,
                        EvolutionType.Gambler
                    };

                default:
                    return new List<EvolutionType>();
            }
        }

        /// <summary>
        /// 특정 진화 조건 체크
        /// </summary>
        private bool CheckEvolutionCondition(BaseUnit unit, EvolutionType targetEvolution, GameContext context)
        {
            if (!evolutionConditions.ContainsKey(targetEvolution))
                return false;

            var conditions = evolutionConditions[targetEvolution];

            // All conditions must be met
            foreach (var condition in conditions)
            {
                if (!condition.CheckCondition(unit, context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 유닛 진화
        /// </summary>
        public void EvolveUnit(BaseUnit unit, EvolutionType newEvolution)
        {
            if (unit == null) return;

            Debug.Log($"[EvolutionManager] {unit.Name} evolving to {newEvolution}!");

            // Store old type
            var oldType = unit.EvolutionType;

            // Apply evolution
            unit.EvolutionType = newEvolution;

            // Apply evolution bonuses
            ApplyEvolutionBonuses(unit, newEvolution);

            // Update visuals
            UpdateEvolutionVisuals(unit, newEvolution);

            // Play effect
            PlayEvolutionEffect(unit);

            // Fire event
            OnUnitEvolved?.Invoke(unit, newEvolution);

            // Show notification
            UIManager.Instance.ShowNotification($"{unit.Name}이(가) 진화했습니다!", NotificationType.Success);
        }

        /// <summary>
        /// 진화 보너스 적용
        /// </summary>
        private void ApplyEvolutionBonuses(BaseUnit unit, EvolutionType evolution)
        {
            // Base stat increase
            var baseBonus = new StatModifier(5, StatModifier.ModifierType.Flat, this);
            unit.Stats.ApplyModifier(baseBonus);

            // Evolution-specific bonuses
            switch (evolution)
            {
                case EvolutionType.BerserkerSwordsman:
                    // Attack focus
                    var atkBonus = new StatModifier(10, StatModifier.ModifierType.Flat, this);
                    unit.Stats.ApplyModifier(atkBonus);
                    break;

                case EvolutionType.GuardianSwordsman:
                    // Health focus
                    var hpBonus = new StatModifier(15, StatModifier.ModifierType.Flat, this);
                    unit.Stats.ApplyModifier(hpBonus);
                    break;

                case EvolutionType.WindSwordsman:
                    // Speed doubled
                    var spdBonus = new StatModifier(1f, StatModifier.ModifierType.PercentMultiply, this);
                    unit.Stats.ApplyModifier(spdBonus);
                    break;

                case EvolutionType.IceMage:
                    if (!unit.Attributes.Contains(ElementAttribute.Ice))
                        unit.Attributes.Add(ElementAttribute.Ice);
                    break;

                case EvolutionType.FireMage:
                    if (!unit.Attributes.Contains(ElementAttribute.Fire))
                        unit.Attributes.Add(ElementAttribute.Fire);
                    break;

                case EvolutionType.LightningMage:
                    if (!unit.Attributes.Contains(ElementAttribute.Lightning))
                        unit.Attributes.Add(ElementAttribute.Lightning);
                    break;
            }
        }

        /// <summary>
        /// 진화 비주얼 업데이트
        /// </summary>
        private void UpdateEvolutionVisuals(BaseUnit unit, EvolutionType evolution)
        {
            // TODO: Load evolution-specific sprite
            // TODO: Update particle effects
            // TODO: Update UI elements
        }

        /// <summary>
        /// 진화 이펙트 재생
        /// </summary>
        private void PlayEvolutionEffect(BaseUnit unit)
        {
            if (evolutionEffectPrefab != null)
            {
                var effect = PoolManager.Instance.Spawn("EvolutionEffect",
                    unit.transform.position,
                    Quaternion.identity);

                if (effect == null)
                {
                    effect = Instantiate(evolutionEffectPrefab, unit.transform.position, Quaternion.identity);
                }

                // Animation
                unit.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5)
                    .SetEase(Ease.OutElastic);

                // Destroy effect after delay
                PoolManager.Instance.Despawn("EvolutionEffect", effect, 2f);
            }
        }

        /// <summary>
        /// 게임 컨텍스트 생성
        /// </summary>
        private GameContext CreateGameContext()
        {
            return new GameContext
            {
                CurrentRound = BackendGameManager.Instance.CurrentRound,
                PlayerGold = BackendGameManager.Instance.CurrentPlayerDeck.gold,
                PlayerHealth = BackendGameManager.Instance.CurrentPlayerDeck.health,
                OwnedUnits = BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits.Cast<IUnit>().ToList(),
                Formation = BackendGameManager.Instance.CurrentPlayerDeck.formation.Cast<IUnit>().ToList()
            };
        }
    }

    // ========== Evolution Conditions Implementation ==========

    /// <summary>
    /// 스탯 임계값 조건
    /// </summary>
    public class StatThresholdCondition : IEvolutionCondition
    {
        private string statName;
        private int threshold;

        public StatThresholdCondition(string stat, int value)
        {
            statName = stat;
            threshold = value;
        }

        public bool CheckCondition(IUnit unit, GameContext context)
        {
            switch (statName.ToLower())
            {
                case "attack": return unit.Stats.Attack >= threshold;
                case "health": return unit.Stats.MaxHealth >= threshold;
                case "speed": return unit.Stats.Speed >= threshold;
                default: return false;
            }
        }

        public string GetDescription()
        {
            return $"{statName} >= {threshold}";
        }
    }

    /// <summary>
    /// 원소 개수 조건
    /// </summary>
    public class ElementalCountCondition : IEvolutionCondition
    {
        private ElementAttribute element;
        private int requiredCount;

        public ElementalCountCondition(ElementAttribute elem, int count)
        {
            element = elem;
            requiredCount = count;
        }

        public bool CheckCondition(IUnit unit, GameContext context)
        {
            int count = context.Formation.Count(u => u.Attributes.Contains(element));
            return count >= requiredCount;
        }

        public string GetDescription()
        {
            return $"{element} attribute units >= {requiredCount}";
        }
    }

    /// <summary>
    /// 커스텀 조건
    /// </summary>
    public class CustomCondition : IEvolutionCondition
    {
        private string description;
        private Func<IUnit, GameContext, bool> checkFunc;

        public CustomCondition(string desc, Func<IUnit, GameContext, bool> check)
        {
            description = desc;
            checkFunc = check;
        }

        public bool CheckCondition(IUnit unit, GameContext context)
        {
            return checkFunc?.Invoke(unit, context) ?? false;
        }

        public string GetDescription()
        {
            return description;
        }
    }
}