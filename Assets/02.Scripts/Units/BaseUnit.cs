using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Data;
using SpiritAge.Stats;
using SpiritAge.Buffs;
using SpiritAge.Core;
using SpiritAge.Skills;
using SpiritAge.Battle;

namespace SpiritAge.Units
{
    /// <summary>
    /// 유닛 기본 클래스
    /// </summary>
    public abstract class BaseUnit : MonoBehaviour, ICombatUnit, IPoolable
    {
        [Header("Unit Components")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Transform visualTransform;
        [SerializeField] protected GameObject selectionIndicator;

        [Header("Animation Settings")]
        [SerializeField] protected float attackAnimDuration = 0.3f;
        [SerializeField] protected float hitAnimDuration = 0.2f;
        [SerializeField] protected float deathAnimDuration = 0.5f;

        // Core Properties
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public UnitType UnitType { get; protected set; }
        public EvolutionType EvolutionType { get; set; }
        public IUnitStats Stats { get; protected set; }
        public List<ElementAttribute> Attributes { get; protected set; }
        public List<IBuff> ActiveBuffs { get; protected set; }
        public bool IsAlive => Stats.Health > 0;

        // Events
        public event Action<BaseUnit> OnUnitDeath;
        public event Action<BaseUnit, int> OnDamageTaken;
        public event Action<BaseUnit, int> OnHealed;

        // Protected fields
        protected UnitData unitData;
        protected ISkill primarySkill;
        protected Sequence currentAnimation;

        protected virtual void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (visualTransform == null) visualTransform = transform.GetChild(0);
            if (selectionIndicator != null) selectionIndicator.SetActive(false);

            Attributes = new List<ElementAttribute>();
            ActiveBuffs = new List<IBuff>();
        }

        /// <summary>
        /// 유닛 초기화
        /// </summary>
        public virtual void Initialize(UnitData data)
        {
            unitData = data;
            Id = data.id;
            Name = data.name;
            UnitType = data.unitType;
            EvolutionType = data.evolutionType;

            // Initialize Stats
            Stats = new UnitStats(data.baseAttack, data.baseHealth, data.baseSpeed);

            // Copy attributes
            Attributes.Clear();
            Attributes.AddRange(data.attributes);

            // Load sprite
            if (!string.IsNullOrEmpty(data.spritePath))
            {
                Sprite sprite = Resources.Load<Sprite>(data.spritePath);
                if (sprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                }
            }

            // Initialize skill
            LoadSkill(data.skillId);

            UpdateVisuals();
        }

        /// <summary>
        /// 스킬 로드
        /// </summary>
        protected virtual void LoadSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;

            var skillData = GameDataManager.Instance.GetSkillData(skillId);
            if (skillData != null)
            {
                primarySkill = SkillFactory.CreateSkill(skillData);
            }
        }

        /// <summary>
        /// 공격 수행
        /// </summary>
        public virtual void PerformAttack(IUnit target)
        {
            if (!IsAlive || target == null || !target.IsAlive) return;

            int damage = CalculateDamage(target);

            // Attack animation
            PlayAttackAnimation(() =>
            {
                target.TakeDamage(damage, this);

                // Trigger on-hit skills
                primarySkill?.Execute(this, new List<IUnit> { target }, BattleManager.Instance.GetContext());

                // Apply elemental effects
                ApplyElementalEffects(target);
            });
        }

        /// <summary>
        /// 데미지 계산
        /// </summary>
        public virtual int CalculateDamage(IUnit target)
        {
            int baseDamage = Stats.Attack;

            // Apply buff modifiers
            foreach (var buff in ActiveBuffs)
            {
                if (buff.Type == BuffType.AttackBoost)
                {
                    baseDamage = Mathf.RoundToInt(baseDamage * (1f + 0.1f * buff.Stack));
                }
                else if (buff.Type == BuffType.Madness)
                {
                    baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
                }
            }

            return Mathf.Max(1, baseDamage);
        }

        /// <summary>
        /// 데미지 받기
        /// </summary>
        public virtual void TakeDamage(int damage, IUnit attacker)
        {
            if (!IsAlive) return;

            // Apply defense modifiers
            foreach (var buff in ActiveBuffs)
            {
                if (buff.Type == BuffType.DefenseBoost)
                {
                    damage = Mathf.RoundToInt(damage * (1f - 0.1f * buff.Stack));
                }
                else if (buff.Type == BuffType.Madness)
                {
                    damage = Mathf.RoundToInt(damage * 1.3f); // Take more damage with madness
                }
            }

            damage = Mathf.Max(1, damage);
            Stats.Health -= damage;

            OnDamageTaken?.Invoke(this, damage);

            if (Stats.Health <= 0)
            {
                Stats.Health = 0;
                Die();
            }
            else
            {
                PlayHitAnimation();
            }

            UpdateVisuals();
        }

        /// <summary>
        /// 치유
        /// </summary>
        public virtual void Heal(int amount)
        {
            if (!IsAlive) return;

            int healAmount = Mathf.Min(amount, Stats.MaxHealth - Stats.Health);
            Stats.Health += healAmount;

            OnHealed?.Invoke(this, healAmount);
            PlayHealAnimation();
            UpdateVisuals();
        }

        /// <summary>
        /// 원소 효과 적용
        /// </summary>
        protected virtual void ApplyElementalEffects(IUnit target) //AddBuff??
        {
            foreach (var attribute in Attributes)
            {
                switch (attribute)
                {
                    case ElementAttribute.Ice:
                        target.AddBuff(new FreezeBuff());
                        break;
                    case ElementAttribute.Fire:
                        target.AddBuff(new BurnBuff());
                        break;
                    case ElementAttribute.Lightning:
                        target.AddBuff(new LightningBuff());
                        break;
                }
            }
        }

        /// <summary>
        /// 버프 추가
        /// </summary>
        public virtual void AddBuff(IBuff buff)
        {
            var existing = ActiveBuffs.Find(b => b.Type == buff.Type);

            if (existing != null)
            {
                existing.Stack += buff.Stack;
                existing.Duration = Mathf.Max(existing.Duration, buff.Duration);
            }
            else
            {
                ActiveBuffs.Add(buff);
                buff.OnApply(this);
            }

            UpdateVisuals();
        }

        /// <summary>
        /// 버프 제거
        /// </summary>
        public virtual void RemoveBuff(BuffType type)
        {
            var buff = ActiveBuffs.Find(b => b.Type == type);
            if (buff != null)
            {
                buff.OnRemove(this);
                ActiveBuffs.Remove(buff);
                UpdateVisuals();
            }
        }

        /// <summary>
        /// 턴 시작
        /// </summary>
        public virtual void OnTurnStart()
        {
            // Process buff ticks
            var buffsToRemove = new List<IBuff>();

            foreach (var buff in ActiveBuffs)
            {
                buff.OnTick(this);

                if (buff.Duration > 0)
                {
                    buff.Duration--;
                    if (buff.IsExpired)
                    {
                        buffsToRemove.Add(buff);
                    }
                }
            }

            foreach (var buff in buffsToRemove)
            {
                RemoveBuff(buff.Type);
            }
        }

        /// <summary>
        /// 턴 종료
        /// </summary>
        public virtual void OnTurnEnd()
        {
            // Turn end logic
        }

        /// <summary>
        /// 유닛 사망
        /// </summary>
        protected virtual void Die()
        {
            OnUnitDeath?.Invoke(this);
            PlayDeathAnimation(() =>
            {
                gameObject.SetActive(false);
            });
        }

        // ========== Animations ==========

        protected virtual void PlayAttackAnimation(Action onComplete = null)
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(visualTransform.DOPunchPosition(Vector3.right * 0.3f, attackAnimDuration, 5))
                          .OnComplete(() => onComplete?.Invoke());
        }

        protected virtual void PlayHitAnimation()
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(visualTransform.DOShakePosition(hitAnimDuration, 0.2f, 10))
                          .Join(spriteRenderer.DOColor(Color.red, hitAnimDuration * 0.5f))
                          .Append(spriteRenderer.DOColor(Color.white, hitAnimDuration * 0.5f));
        }

        protected virtual void PlayHealAnimation()
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(visualTransform.DOScale(1.2f, 0.2f))
                          .Join(spriteRenderer.DOColor(Color.green, 0.2f))
                          .Append(visualTransform.DOScale(1f, 0.2f))
                          .Join(spriteRenderer.DOColor(Color.white, 0.2f));
        }

        protected virtual void PlayDeathAnimation(Action onComplete = null)
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();
            currentAnimation.Append(visualTransform.DOScale(0f, deathAnimDuration).SetEase(Ease.InBack))
                          .Join(spriteRenderer.DOFade(0f, deathAnimDuration))
                          .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// 비주얼 업데이트
        /// </summary>
        protected virtual void UpdateVisuals()
        {
            // Update health bar, buff icons, etc.
        }

        // ========== IPoolable Implementation ==========

        public virtual void OnSpawn()
        {
            gameObject.SetActive(true);
            visualTransform.localScale = Vector3.one;
            spriteRenderer.color = Color.white;
            spriteRenderer.DOFade(1f, 0f);
        }

        public virtual void OnDespawn()
        {
            currentAnimation?.Kill();
            ActiveBuffs.Clear();
        }

        public virtual void ResetState()
        {
            Stats?.ResetToBase();
            ActiveBuffs?.Clear();
            OnUnitDeath = null;
            OnDamageTaken = null;
            OnHealed = null;
        }

        protected virtual void OnDestroy()
        {
            currentAnimation?.Kill();
        }
    }
}