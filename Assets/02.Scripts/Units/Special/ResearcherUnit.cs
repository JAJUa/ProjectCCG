using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using SpiritAge.Battle;
using SpiritAge.Core;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Data;
using SpiritAge.Evolution;
using SpiritAge.Effects;

namespace SpiritAge.Units.Special
{
    /// <summary>
    /// 연구자 유닛
    /// </summary>
    public class ResearcherUnit : BaseUnit
    {
        [Header("Researcher Settings")]
        [SerializeField] private GameObject researchEffectPrefab;
        [SerializeField] private float researchAnimationDuration = 1f;
        [SerializeField] private ParticleSystem knowledgeParticles;

        private bool hasEvolved = false;
        private int researchProgress = 0;

        protected override void Awake()
        {
            base.Awake();

            if (knowledgeParticles == null)
            {
                knowledgeParticles = GetComponentInChildren<ParticleSystem>();
            }
        }

        public override void OnTurnStart()
        {
            base.OnTurnStart();

            if (!hasEvolved)
            {
                CheckEvolutionConditions();
            }

            // Researcher passive: gain knowledge
            researchProgress++;

            if (researchProgress % 3 == 0)
            {
                ApplyResearchBonus();
            }
        }

        /// <summary>
        /// 진화 조건 체크
        /// </summary>
        private void CheckEvolutionConditions()
        {
            var context = BattleManager.Instance.GetContext();

            // Check for Spiritist evolution (Soul attribute present)
            bool hasSoulUnit = false;
            foreach (var unit in context.PlayerUnits)
            {
                if (unit.Attributes.Contains(ElementAttribute.Soul))
                {
                    hasSoulUnit = true;
                    break;
                }
            }

            if (hasSoulUnit && EvolutionType == EvolutionType.Researcher)
            {
                EvolveToSpiritist();
                return;
            }

            // Check for Alchemist evolution (6 units in formation)
            if (context.PlayerUnits.Count >= 6 && EvolutionType == EvolutionType.Researcher)
            {
                EvolveToAlchemist();
            }
        }

        /// <summary>
        /// 영혼술사로 진화
        /// </summary>
        private void EvolveToSpiritist()
        {
            Debug.Log($"[ResearcherUnit] {Name} evolving to Spiritist!");

            EvolutionType = EvolutionType.Spiritist;
            hasEvolved = true;

            // Visual effects
            PlayEvolutionEffect(ElementAttribute.Soul);

            // Update abilities
            Attributes.Add(ElementAttribute.Soul);

            // Subscribe to death events
            var allUnits = BattleManager.Instance.GetContext().PlayerUnits;
            foreach (var unit in allUnits)
            {
                if (unit is BaseUnit baseUnit)
                {
                    baseUnit.OnUnitDeath += OnAllyDeath;
                }
            }

            EvolutionManager.Instance.OnUnitEvolved?.Invoke(this, EvolutionType.Spiritist);
        }

        /// <summary>
        /// 연금술사로 진화
        /// </summary>
        private void EvolveToAlchemist()
        {
            Debug.Log($"[ResearcherUnit] {Name} evolving to Alchemist!");

            EvolutionType = EvolutionType.Alchemist;
            hasEvolved = true;

            // Visual effects
            PlayEvolutionEffect(ElementAttribute.None);

            // Enable merging in shop
            EnableMergeAbility();

            EvolutionManager.Instance.OnUnitEvolved?.Invoke(this, EvolutionType.Alchemist);
        }

        /// <summary>
        /// 연구 보너스 적용
        /// </summary>
        private void ApplyResearchBonus()
        {
            Debug.Log($"[ResearcherUnit] Research breakthrough! Applying bonus...");

            // Random buff to all allies
            var allies = BattleManager.Instance.GetContext().PlayerUnits;
            var randomAlly = allies[Random.Range(0, allies.Count)];

            var bonus = new StatModifier(2, StatModifier.ModifierType.Flat, this);
            randomAlly.Stats.ApplyModifier(bonus);

            // Visual feedback
            if (knowledgeParticles != null)
            {
                knowledgeParticles.Emit(10);
            }
        }

        /// <summary>
        /// 아군 사망시 (영혼술사)
        /// </summary>
        private void OnAllyDeath(BaseUnit deadUnit)
        {
            if (EvolutionType != EvolutionType.Spiritist) return;
            if (!IsAlive) return;

            SummonSoul(deadUnit);
        }

        /// <summary>
        /// 영혼 소환
        /// </summary>
        private void SummonSoul(BaseUnit originalUnit)
        {
            Debug.Log($"[Spiritist] Summoning soul of {originalUnit.Name}!");

            // Create soul unit
            GameObject soulGO = new GameObject($"Soul_{originalUnit.Name}");
            SoulUnit soul = soulGO.AddComponent<SoulUnit>();

            // Copy basic stats (weakened)
            var soulData = new UnitData
            {
                id = $"soul_{originalUnit.Id}",
                name = $"{originalUnit.Name} (영혼)",
                unitType = UnitType.Follower,
                evolutionType = EvolutionType.None,
                baseAttack = Mathf.Max(1, originalUnit.Stats.Attack / 2),
                baseHealth = 1, // Souls are fragile
                baseSpeed = originalUnit.Stats.Speed,
                attributes = new List<ElementAttribute> { ElementAttribute.Soul }
            };

            soul.Initialize(soulData);

            // Add to battle
            var context = BattleManager.Instance.GetContext();
            context.PlayerUnits.Add(soul);

            // Position soul
            soul.transform.position = originalUnit.transform.position;
            soul.transform.SetParent(originalUnit.transform.parent);

            // Visual effect
            PlaySummonEffect(soul.transform.position);
        }

        /// <summary>
        /// 합성 능력 활성화 (연금술사)
        /// </summary>
        private void EnableMergeAbility()
        {
            // This would enable merge UI in shop
            BackendGameManager.Instance.CurrentPlayerDeck.hasAlchemist = true;

            Debug.Log("[Alchemist] Merge ability unlocked!");
        }

        /// <summary>
        /// 진화 이펙트 재생
        /// </summary>
        private void PlayEvolutionEffect(ElementAttribute element)
        {
            if (researchEffectPrefab != null)
            {
                GameObject effect = Instantiate(researchEffectPrefab, transform.position, Quaternion.identity);

                // Customize based on evolution
                ParticleSystem particles = effect.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    var main = particles.main;

                    if (element == ElementAttribute.Soul)
                    {
                        main.startColor = new Color(0.7f, 0.5f, 0.9f); // Purple for soul
                    }
                    else
                    {
                        main.startColor = new Color(1f, 0.8f, 0.2f); // Gold for alchemy
                    }
                }

                Destroy(effect, 2f);
            }

            // Scale animation
            visualTransform.DOPunchScale(Vector3.one * 0.5f, researchAnimationDuration, 5);
        }

        /// <summary>
        /// 소환 이펙트 재생
        /// </summary>
        private void PlaySummonEffect(Vector3 position)
        {
            // Create summon portal effect
            EffectManager.Instance.PlayEffect("SoulSummonEffect", position);
        }

        protected override void PlayAttackAnimation(System.Action onComplete = null)
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();

            // Book/scroll casting animation
            currentAnimation.Append(visualTransform.DORotate(new Vector3(0, 360, 0), attackAnimDuration))
                          .Join(spriteRenderer.DOColor(Color.magenta, attackAnimDuration * 0.5f))
                          .Append(spriteRenderer.DOColor(Color.white, attackAnimDuration * 0.5f))
                          .OnComplete(() => onComplete?.Invoke());
        }
    }

    /// <summary>
    /// 영혼 유닛
    /// </summary>
    public class SoulUnit : BaseUnit
    {
        [Header("Soul Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float floatAmplitude = 0.2f;
        [SerializeField] private float floatFrequency = 2f;

        private Tween floatTween;

        protected override void Awake()
        {
            base.Awake();

            // Make translucent
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0.7f;
                spriteRenderer.color = color;
            }
        }

        public override void Initialize(UnitData data)
        {
            base.Initialize(data);

            // Add soul buff
            AddBuff(new Buffs.SoulBuff());

            // Fade in animation
            spriteRenderer.DOFade(0.7f, fadeInDuration).From(0f);

            // Floating animation
            StartFloating();
        }

        /// <summary>
        /// 부유 애니메이션
        /// </summary>
        private void StartFloating()
        {
            floatTween = visualTransform.DOMoveY(
                visualTransform.position.y + floatAmplitude,
                1f / floatFrequency)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public override void TakeDamage(int damage, IUnit attacker)
        {
            // Souls die in one hit regardless of damage
            Stats.Health = 0;
            Die();
        }

        protected override void Die()
        {
            floatTween?.Kill();

            // Dissipate effect
            spriteRenderer.DOFade(0f, 0.5f);
            visualTransform.DOScale(0f, 0.5f).OnComplete(() =>
            {
                base.Die();
            });
        }

        private void OnDestroy()
        {
            floatTween?.Kill();
        }
    }
}