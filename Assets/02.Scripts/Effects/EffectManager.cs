using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpiritAge.Utility;
using SpiritAge.Utility.Pooling;

namespace SpiritAge.Effects
{
    /// <summary>
    /// 이펙트 매니저
    /// </summary>
    public class EffectManager : AbstractSingleton<EffectManager>
    {
        [Header("Effect Prefabs")]
        [SerializeField] private EffectLibrary effectLibrary;

        [Header("Screen Effects")]
        [SerializeField] private GameObject screenShakeCameraTarget;
        [SerializeField] private CanvasGroup screenFlashCanvas;
        [SerializeField] private Image screenFlashImage;

        // Effect tracking
        private Dictionary<string, float> lastEffectTime = new Dictionary<string, float>();
        private const float MIN_EFFECT_INTERVAL = 0.05f;

        // Camera shake
        private Tween currentShakeTween;
        private Vector3 originalCameraPosition;

        protected override void OnSingletonAwake()
        {
            InitializeEffectPools();

            if (screenShakeCameraTarget == null)
                screenShakeCameraTarget = Camera.main.gameObject;

            if (screenShakeCameraTarget != null)
                originalCameraPosition = screenShakeCameraTarget.transform.position;
        }

        /// <summary>
        /// 이펙트 풀 초기화
        /// </summary>
        private void InitializeEffectPools()
        {
            if (effectLibrary == null) return;

            foreach (var effect in effectLibrary.GetAllEffects())
            {
                PoolManager.Instance.CreatePool(
                    effect.key,
                    effect.prefab,
                    effect.poolSize,
                    effect.maxPoolSize,
                    true
                );
            }
        }

        // ========== Basic Effects ==========

        /// <summary>
        /// 이펙트 재생
        /// </summary>
        public GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation = default, float scale = 1f, float duration = 0f)
        {
            // Check interval
            if (lastEffectTime.ContainsKey(effectName))
            {
                if (Time.time - lastEffectTime[effectName] < MIN_EFFECT_INTERVAL)
                    return null;
            }

            GameObject effect = PoolManager.Instance.Spawn(effectName, position, rotation);

            if (effect == null)
            {
                Debug.LogWarning($"[EffectManager] Effect not found: {effectName}");
                return null;
            }

            // Set scale
            if (scale != 1f)
            {
                effect.transform.localScale = Vector3.one * scale;
            }

            // Auto despawn
            float despawnTime = duration > 0 ? duration : GetEffectDuration(effect);
            PoolManager.Instance.Despawn(effectName, effect, despawnTime);

            lastEffectTime[effectName] = Time.time;

            return effect;
        }

        /// <summary>
        /// 타겟 추적 이펙트
        /// </summary>
        public GameObject PlayEffectOnTarget(string effectName, Transform target, Vector3 offset = default, float duration = 0f)
        {
            if (target == null) return null;

            GameObject effect = PlayEffect(effectName, target.position + offset);

            if (effect != null)
            {
                effect.transform.SetParent(target);
                effect.transform.localPosition = offset;
            }

            return effect;
        }

        /// <summary>
        /// 라인 이펙트
        /// </summary>
        public void PlayLineEffect(string effectName, Vector3 start, Vector3 end, float duration = 1f)
        {
            GameObject effect = PlayEffect(effectName, start);

            if (effect != null)
            {
                LineRenderer lineRenderer = effect.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, start);
                    lineRenderer.SetPosition(1, end);

                    // Animate
                    DOTween.Sequence()
                        .Append(DOTween.To(() => lineRenderer.widthMultiplier,
                            x => lineRenderer.widthMultiplier = x, 0f, duration))
                        .SetEase(Ease.InQuad);
                }
            }
        }

        /// <summary>
        /// 트레일 이펙트
        /// </summary>
        public void PlayTrailEffect(string effectName, Vector3[] path, float speed = 10f)
        {
            GameObject effect = PlayEffect(effectName, path[0]);

            if (effect != null)
            {
                effect.transform.DOPath(path, path.Length / speed, PathType.CatmullRom)
                .SetEase(Ease.InOutQuad)
                    .OnComplete(() => PoolManager.Instance.Despawn(effectName, effect));
            }
        }

        // ========== Combat Effects ==========

        /// <summary>
        /// 데미지 텍스트
        /// </summary>
        public void ShowDamageText(Vector3 position, int damage, DamageType type = DamageType.Normal)
        {
            string effectName = GetDamageTextEffect(type);
            GameObject damageText = PlayEffect(effectName, position);

            if (damageText != null)
            {
                TextMesh textMesh = damageText.GetComponentInChildren<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = damage.ToString();
                    textMesh.color = GetDamageColor(type);
                }

                // Animate
                damageText.transform.DOMoveY(position.y + 2f, 1f);
                damageText.transform.DOScale(0f, 1f).SetEase(Ease.InBack);
            }
        }

        /// <summary>
        /// 힐 텍스트
        /// </summary>
        public void ShowHealText(Vector3 position, int healAmount)
        {
            GameObject healText = PlayEffect("HealText", position);

            if (healText != null)
            {
                TextMesh textMesh = healText.GetComponentInChildren<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = $"+{healAmount}";
                    textMesh.color = Color.green;
                }

                // Animate
                healText.transform.DOMoveY(position.y + 2f, 1f);
                healText.transform.DOScale(1.2f, 0.3f)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() => healText.transform.DOScale(0f, 0.3f));
            }
        }

        /// <summary>
        /// 공격 이펙트
        /// </summary>
        public void PlayAttackEffect(Vector3 attackerPos, Vector3 targetPos, AttackType type)
        {
            switch (type)
            {
                case AttackType.Melee:
                    PlayEffect("SlashEffect", targetPos);
                    break;

                case AttackType.Ranged:
                    PlayProjectileEffect("ArrowEffect", attackerPos, targetPos);
                    break;

                case AttackType.Magic:
                    PlayEffect("MagicImpact", targetPos);
                    break;
            }
        }

        /// <summary>
        /// 투사체 이펙트
        /// </summary>
        public void PlayProjectileEffect(string effectName, Vector3 start, Vector3 target, float speed = 10f)
        {
            GameObject projectile = PlayEffect(effectName, start);

            if (projectile != null)
            {
                // Look at target
                projectile.transform.LookAt(target);

                // Move to target
                projectile.transform.DOMove(target, Vector3.Distance(start, target) / speed)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        PlayEffect("ImpactEffect", target);
                        PoolManager.Instance.Despawn(effectName, projectile);
                    });
            }
        }

        // ========== Screen Effects ==========

        /// <summary>
        /// 화면 흔들기
        /// </summary>
        public void ShakeScreen(float intensity = 1f, float duration = 0.5f)
        {
            if (screenShakeCameraTarget == null) return;

            currentShakeTween?.Kill();
            screenShakeCameraTarget.transform.position = originalCameraPosition;

            currentShakeTween = screenShakeCameraTarget.transform
                .DOShakePosition(duration, intensity, 10, 90, false, true)
                .OnComplete(() => screenShakeCameraTarget.transform.position = originalCameraPosition);
        }

        /// <summary>
        /// 화면 플래시
        /// </summary>
        public void FlashScreen(Color color, float duration = 0.2f)
        {
            if (screenFlashCanvas == null || screenFlashImage == null) return;

            screenFlashImage.color = color;
            screenFlashCanvas.alpha = 1f;
            screenFlashCanvas.DOFade(0f, duration);
        }

        /// <summary>
        /// 슬로우 모션
        /// </summary>
        public void SetSlowMotion(float timeScale = 0.5f, float duration = 1f)
        {
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, timeScale, 0.2f)
                .OnComplete(() =>
                {
                    DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, 0.2f)
                        .SetDelay(duration);
                });
        }

        // ========== Utility ==========

        /// <summary>
        /// 이펙트 지속시간 가져오기
        /// </summary>
        private float GetEffectDuration(GameObject effect)
        {
            // Check particle system
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                return ps.main.duration + ps.main.startLifetime.constantMax;
            }

            // Check animation
            Animator animator = effect.GetComponent<Animator>();
            if (animator != null)
            {
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    return clipInfo[0].clip.length;
                }
            }

            // Default duration
            return 2f;
        }

        private string GetDamageTextEffect(DamageType type)
        {
            switch (type)
            {
                case DamageType.Critical: return "CriticalDamageText";
                case DamageType.Elemental: return "ElementalDamageText";
                default: return "DamageText";
            }
        }

        private Color GetDamageColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Critical: return Color.yellow;
                case DamageType.Elemental: return Color.cyan;
                default: return Color.white;
            }
        }

        private void OnDestroy()
        {
            currentShakeTween?.Kill();
        }
    }

    // ========== Enums ==========

    public enum DamageType
    {
        Normal,
        Critical,
        Elemental
    }

    public enum AttackType
    {
        Melee,
        Ranged,
        Magic
    }
}