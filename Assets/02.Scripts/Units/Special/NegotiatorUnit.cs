using System.Collections;
using UnityEngine;
using DG.Tweening;
using SpiritAge.Core;
using SpiritAge.Battle;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Evolution;
using SpiritAge.Utility.Probability;

namespace SpiritAge.Units.Special
{
    /// <summary>
    /// 교섭가 유닛
    /// </summary>
    public class NegotiatorUnit : BaseUnit
    {
        [Header("Negotiator Settings")]
        [SerializeField] private GameObject goldEffectPrefab;
        [SerializeField] private GameObject diceEffectPrefab;
        [SerializeField] private int baseGoldEarn = 1;
        [SerializeField] private int maxGoldEarn = 3;

        private int totalGoldEarned = 0;
        private bool hasEvolved = false;

        public override void OnTurnEnd()
        {
            base.OnTurnEnd();

            if (EvolutionType == EvolutionType.Negotiator)
            {
                EarnGold();
                CheckGoldEvolution();
            }
            else if (EvolutionType == EvolutionType.Clockmaker)
            {
                ApplyTimeSync();
            }
            else if (EvolutionType == EvolutionType.Gambler)
            {
                RollTheDice();
            }
        }

        /// <summary>
        /// 골드 획득
        /// </summary>
        private void EarnGold()
        {
            int goldAmount = Random.Range(baseGoldEarn, maxGoldEarn + 1);
            BackendGameManager.Instance.AddGold(goldAmount);
            totalGoldEarned += goldAmount;

            Debug.Log($"[Negotiator] {Name} earned {goldAmount} gold! (Total: {totalGoldEarned})");

            // Visual feedback
            ShowGoldEffect(goldAmount);
        }

        /// <summary>
        /// 골드 진화 체크
        /// </summary>
        private void CheckGoldEvolution()
        {
            if (hasEvolved) return;

            int playerGold = BackendGameManager.Instance.CurrentPlayerDeck.gold;

            if (playerGold >= 30)
            {
                EvolveToGambler();
            }
        }

        /// <summary>
        /// 시계상으로 진화 (새로고침시)
        /// </summary>
        public void EvolveToClockmaker()
        {
            if (hasEvolved) return;

            Debug.Log($"[Negotiator] {Name} evolving to Clockmaker!");

            EvolutionType = EvolutionType.Clockmaker;
            hasEvolved = true;

            // Add time attribute
            if (!Attributes.Contains(ElementAttribute.Time))
            {
                Attributes.Add(ElementAttribute.Time);
            }

            // Visual effect
            PlayEvolutionEffect(true);

            // Apply immediate effect
            ApplyTimeSync();

            EvolutionManager.Instance.OnUnitEvolved?.Invoke(this, EvolutionType.Clockmaker);
        }

        /// <summary>
        /// 도박사로 진화
        /// </summary>
        private void EvolveToGambler()
        {
            Debug.Log($"[Negotiator] {Name} evolving to Gambler!");

            EvolutionType = EvolutionType.Gambler;
            hasEvolved = true;

            // Visual effect
            PlayEvolutionEffect(false);

            EvolutionManager.Instance.OnUnitEvolved?.Invoke(this, EvolutionType.Gambler);
        }

        /// <summary>
        /// 시간 동기화 (시계상)
        /// </summary>
        private void ApplyTimeSync()
        {
            var allies = BattleManager.Instance.GetContext().PlayerUnits;

            if (allies.Count <= 1) return;

            // Calculate average speed
            int totalSpeed = 0;
            foreach (var ally in allies)
            {
                totalSpeed += ally.Stats.Speed;
            }
            int averageSpeed = totalSpeed / allies.Count;

            // Sync all speeds
            foreach (var ally in allies)
            {
                ally.Stats.Speed = averageSpeed;
            }

            Debug.Log($"[Clockmaker] All units synchronized to speed {averageSpeed}!");

            // Visual effect
            StartCoroutine(TimeSyncEffect());
        }

        /// <summary>
        /// 주사위 굴리기 (도박사)
        /// </summary>
        private void RollTheDice()
        {
            // Random events
            float roll = Random.value;

            if (roll < 0.3f) // 30% chance for bonus
            {
                int bonusType = Random.Range(0, 3);

                switch (bonusType)
                {
                    case 0: // Extra gold
                        int extraGold = Random.Range(1, 6);
                        BackendGameManager.Instance.AddGold(extraGold);
                        Debug.Log($"[Gambler] Lucky roll! +{extraGold} gold!");
                        ShowGoldEffect(extraGold);
                        break;

                    case 1: // Random buff
                        var randomAlly = ProbabilitySystem.RandomElement(
                            BattleManager.Instance.GetContext().PlayerUnits);
                        if (randomAlly != null)
                        {
                            randomAlly.Stats.Attack += 3;
                            Debug.Log($"[Gambler] Lucky roll! {randomAlly.Name} gets +3 attack!");
                        }
                        break;

                    case 2: // Extra action
                        StartCoroutine(ExtraActionCoroutine());
                        break;
                }

                // Visual feedback
                ShowDiceEffect(true);
            }
            else
            {
                ShowDiceEffect(false);
            }
        }

        /// <summary>
        /// 추가 행동 코루틴
        /// </summary>
        private IEnumerator ExtraActionCoroutine()
        {
            Debug.Log($"[Gambler] Lucky roll! Extra action!");

            yield return new WaitForSeconds(0.5f);

            // Perform extra attack
            var enemies = BattleManager.Instance.GetContext().EnemyUnits;
            if (enemies.Count > 0 && enemies[0].IsAlive)
            {
                PerformAttack(enemies[0]);
            }
        }

        public override int CalculateDamage(IUnit target)
        {
            int damage = base.CalculateDamage(target);

            if (EvolutionType == EvolutionType.Gambler)
            {
                // Random damage multiplier
                if (ProbabilitySystem.CheckPercent(30f))
                {
                    damage = Random.Range(damage, damage * 3);
                    Debug.Log($"[Gambler] Critical gamble! Damage: {damage}");
                }
            }

            return damage;
        }

        /// <summary>
        /// 골드 이펙트 표시
        /// </summary>
        private void ShowGoldEffect(int amount)
        {
            if (goldEffectPrefab != null)
            {
                GameObject effect = Instantiate(goldEffectPrefab,
                    transform.position + Vector3.up,
                    Quaternion.identity);

                TextMesh text = effect.GetComponentInChildren<TextMesh>();
                if (text != null)
                {
                    text.text = $"+{amount}G";
                }

                effect.transform.DOMoveY(transform.position.y + 2f, 1f);
                effect.transform.DOScale(0f, 1f);

                Destroy(effect, 1f);
            }
        }

        /// <summary>
        /// 주사위 이펙트 표시
        /// </summary>
        private void ShowDiceEffect(bool success)
        {
            if (diceEffectPrefab != null)
            {
                GameObject dice = Instantiate(diceEffectPrefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.identity);

                // Dice roll animation
                dice.transform.DORotate(new Vector3(360, 360, 0), 0.5f, RotateMode.FastBeyond360);
                dice.transform.DOJump(transform.position + Vector3.up, 1f, 1, 0.5f);

                // Color based on success
                SpriteRenderer sr = dice.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = success ? Color.green : Color.red;
                }

                Destroy(dice, 1f);
            }
        }

        /// <summary>
        /// 시간 동기화 이펙트
        /// </summary>
        private IEnumerator TimeSyncEffect()
        {
            var allies = BattleManager.Instance.GetContext().PlayerUnits;

            foreach (var ally in allies)
            {
                if (ally is BaseUnit unit)
                {
                    // Clock hands effect
                    unit.transform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 진화 이펙트
        /// </summary>
        private void PlayEvolutionEffect(bool isClockmaker)
        {
            if (isClockmaker)
            {
                // Clock/time effect
                visualTransform.DORotate(new Vector3(0, 0, 720), 1f, RotateMode.FastBeyond360);
                spriteRenderer.DOColor(new Color(0.8f, 0.8f, 0.8f), 0.5f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                // Dice/gambling effect
                visualTransform.DOShakePosition(1f, 0.3f, 20);
                spriteRenderer.DOColor(Color.green, 0.5f).SetLoops(2, LoopType.Yoyo);
            }
        }

        protected override void PlayAttackAnimation(System.Action onComplete = null)
        {
            currentAnimation?.Kill();

            currentAnimation = DOTween.Sequence();

            if (EvolutionType == EvolutionType.Gambler)
            {
                // Dice throw animation
                currentAnimation.Append(visualTransform.DOJump(
                    visualTransform.position + Vector3.right * 0.5f, 0.5f, 1, attackAnimDuration))
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                base.PlayAttackAnimation(onComplete);
            }
        }
    }
}