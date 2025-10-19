using UnityEngine;
using DG.Tweening;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;

namespace SpiritAge.Units
{
    /// <summary>
    /// °Ë»ç À¯´Ö
    /// </summary>
    public class SwordsmanUnit : BaseUnit
    {
        [Header("Swordsman Settings")]
        [SerializeField] private GameObject slashEffectPrefab;
        [SerializeField] private float cleaveAngle = 45f;

        protected override void PlayAttackAnimation(System.Action onComplete = null)
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();

            // Different animations based on evolution
            switch (EvolutionType)
            {
                case EvolutionType.BerserkerSwordsman:
                    // Wide sweep attack
                    currentAnimation.Append(visualTransform.DORotate(new Vector3(0, 0, -cleaveAngle), attackAnimDuration * 0.5f))
                                  .Append(visualTransform.DORotate(new Vector3(0, 0, cleaveAngle), attackAnimDuration * 0.5f))
                                  .Join(ShowSlashEffect(true))
                                  .OnComplete(() => onComplete?.Invoke());
                    break;

                case EvolutionType.WindSwordsman:
                    // Quick multi-hit
                    currentAnimation.Append(visualTransform.DOPunchPosition(Vector3.right * 0.5f, attackAnimDuration * 0.3f, 10))
                                  .Append(visualTransform.DOPunchPosition(Vector3.right * 0.5f, attackAnimDuration * 0.3f, 10))
                                  .Join(ShowSlashEffect(false))
                                  .OnComplete(() => onComplete?.Invoke());
                    break;

                default:
                    base.PlayAttackAnimation(onComplete);
                    break;
            }
        }

        private Tween ShowSlashEffect(bool isWide)
        {
            if (slashEffectPrefab == null) return null;

            GameObject effect = Instantiate(slashEffectPrefab, transform.position + Vector3.right, Quaternion.identity);

            if (isWide)
            {
                effect.transform.localScale = Vector3.one * 1.5f;
            }

            Destroy(effect, 1f);
            return effect.transform.DOScale(0f, 1f);
        }

        public override int CalculateDamage(IUnit target)
        {
            int damage = base.CalculateDamage(target);

            // Guardian gets bonus based on health
            if (EvolutionType == EvolutionType.GuardianSwordsman)
            {
                damage += Mathf.RoundToInt(Stats.MaxHealth * 0.1f);
            }

            return damage;
        }
    }
}