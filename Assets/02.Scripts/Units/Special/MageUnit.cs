using UnityEngine;
using DG.Tweening;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Data;
using SpiritAge.Buffs;
using SpiritAge.Battle;

namespace SpiritAge.Units
{
    /// <summary>
    /// ¸¶¹ý»ç À¯´Ö
    /// </summary>
    public class MageUnit : BaseUnit
    {
        [Header("Mage Settings")]
        [SerializeField] private GameObject spellEffectPrefab;
        [SerializeField] private LineRenderer spellBeam;
        [SerializeField] private float spellCastTime = 0.5f;

        protected override void Awake()
        {
            base.Awake();

            if (spellBeam != null)
            {
                spellBeam.enabled = false;
            }
        }

        protected override void PlayAttackAnimation(System.Action onComplete = null)
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();

            // Spell casting animation
            currentAnimation.Append(visualTransform.DOScale(1.2f, spellCastTime * 0.5f))
                          .Join(ShowSpellCharge())
                          .Append(visualTransform.DOScale(1f, spellCastTime * 0.5f))
                          .OnComplete(() =>
                          {
                              CastSpell();
                              onComplete?.Invoke();
                          });
        }

        private Tween ShowSpellCharge()
        {
            // Create charging effect based on element
            Color chargeColor = GetElementColor();

            return spriteRenderer.DOColor(chargeColor, spellCastTime * 0.5f)
                                .SetLoops(2, LoopType.Yoyo);
        }

        private void CastSpell()
        {
            if (spellEffectPrefab == null) return;

            Vector3 spellPos = transform.position + Vector3.up;
            GameObject effect = Instantiate(spellEffectPrefab, spellPos, Quaternion.identity);

            // Customize effect based on evolution
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = GetElementColor();
            }

            Destroy(effect, 2f);
        }

        private Color GetElementColor()
        {
            switch (EvolutionType)
            {
                case EvolutionType.IceMage:
                    return new Color(0.5f, 0.8f, 1f);
                case EvolutionType.FireMage:
                    return new Color(1f, 0.4f, 0.2f);
                case EvolutionType.LightningMage:
                    return new Color(1f, 1f, 0.3f);
                default:
                    return Color.magenta;
            }
        }

        public override void OnTurnStart()
        {
            base.OnTurnStart();

            // Mage passive: boost attack when allies use magic
            var alliedUnits = BattleManager.Instance.GetContext().PlayerUnits;
            int magicUserCount = 0;

            foreach (var unit in alliedUnits)
            {
                if (unit is MageUnit && unit != this)
                {
                    magicUserCount++;
                }
            }

            if (magicUserCount > 0)
            {
                var tempBoost = new StatModifier(magicUserCount * 2, StatModifier.ModifierType.Flat, this, 100);
                Stats.ApplyModifier(tempBoost);
                Debug.Log($"[MageUnit] {Name} gains +{magicUserCount * 2} attack from allied mages!");
            }
        }

        protected override void ApplyElementalEffects(IUnit target)
        {
            base.ApplyElementalEffects(target);

            // Apply team-wide elemental effects for evolved mages
            if (EvolutionType == EvolutionType.IceMage ||
                EvolutionType == EvolutionType.FireMage ||
                EvolutionType == EvolutionType.LightningMage)
            {
                var context = BattleManager.Instance.GetContext();
                var enemies = IsPlayerUnit() ? context.EnemyUnits : context.PlayerUnits;

                // Apply to front enemy
                if (enemies.Count > 0 && enemies[0].IsAlive)
                {
                    switch (EvolutionType)
                    {
                        case EvolutionType.IceMage:
                            enemies[0].AddBuff(new FreezeBuff());
                            break;
                        case EvolutionType.FireMage:
                            enemies[0].AddBuff(new BurnBuff());
                            break;
                        case EvolutionType.LightningMage:
                            enemies[0].AddBuff(new LightningBuff());
                            break;
                    }
                }
            }
        }

        private bool IsPlayerUnit()
        {
            var context = BattleManager.Instance.GetContext();
            return context.PlayerUnits.Contains(this);
        }
    }
}