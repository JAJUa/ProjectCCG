using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Data;
using SpiritAge.Units;
using SpiritAge.Utility;

namespace SpiritAge.Battle
{
    /// <summary>
    /// 전투 시스템 매니저
    /// </summary>
    public class BattleManager : AbstractSingleton<BattleManager>
    {
        [Header("Battle Configuration")]
        [SerializeField] private float actionDelay = 0.5f;
        [SerializeField] private float turnStartDelay = 1f;
        [SerializeField] private int maxTurns = 100;

        [Header("Battle Field")]
        [SerializeField] private Transform playerField;
        [SerializeField] private Transform enemyField;
        [SerializeField] private Transform effectsContainer;

        // Battle State
        public GamePhase CurrentPhase { get; private set; }
        private BattleContext battleContext;
        private List<BaseUnit> playerUnits = new List<BaseUnit>();
        private List<BaseUnit> enemyUnits = new List<BaseUnit>();
        private Coroutine battleCoroutine;

        // Events
        public event Action<BattleResult> OnBattleEnd;
        public event Action<int> OnTurnStart;
        public event Action<int> OnTurnEnd;
        public event Action<BaseUnit, BaseUnit, int> OnDamageDealt;

        protected override void OnSingletonAwake()
        {
            battleContext = new BattleContext();
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle(List<BaseUnit> playerFormation, List<BaseUnit> enemyFormation)
        {
            if (CurrentPhase == GamePhase.Battle)
            {
                Debug.LogWarning("[BattleManager] Battle already in progress!");
                return;
            }

            CurrentPhase = GamePhase.Battle;

            // Setup units
            playerUnits.Clear();
            enemyUnits.Clear();

            playerUnits.AddRange(playerFormation);
            enemyUnits.AddRange(enemyFormation);

            // Setup battle context
            battleContext = new BattleContext
            {
                PlayerUnits = playerUnits.Cast<IUnit>().ToList(),
                EnemyUnits = enemyUnits.Cast<IUnit>().ToList(),
                CurrentTurn = 0,
                CurrentPhase = GamePhase.Battle
            };

            // Position units
            PositionUnits(playerUnits, playerField);
            PositionUnits(enemyUnits, enemyField);

            // Start battle loop
            if (battleCoroutine != null) StopCoroutine(battleCoroutine);
            battleCoroutine = StartCoroutine(BattleLoop());
        }

        /// <summary>
        /// 전투 루프
        /// </summary>
        private IEnumerator BattleLoop()
        {
            Debug.Log("[BattleManager] === Battle Started ===");

            battleContext.CurrentTurn = 0;

            while (CurrentPhase == GamePhase.Battle && battleContext.CurrentTurn < maxTurns)
            {
                battleContext.CurrentTurn++;

                Debug.Log($"[BattleManager] Turn {battleContext.CurrentTurn} Start");
                OnTurnStart?.Invoke(battleContext.CurrentTurn);

                // Process turn start for all units
                ProcessTurnStart();

                yield return new WaitForSeconds(turnStartDelay);

                // Determine action order
                var actionOrder = DetermineActionOrder();

                // Execute actions
                foreach (var (unit, isPlayer) in actionOrder)
                {
                    if (!unit.IsAlive) continue;

                    // Check for stun
                    var stunBuff = unit.ActiveBuffs.FirstOrDefault(b => b.Type == BuffType.Stun);
                    if (stunBuff != null)
                    {
                        Debug.Log($"[BattleManager] {unit.Name} is stunned!");
                        yield return new WaitForSeconds(actionDelay);
                        continue;
                    }

                    // Perform action
                    yield return StartCoroutine(ExecuteUnitAction(unit, isPlayer));

                    // Check battle end
                    var result = CheckBattleEnd();
                    if (result != BattleResult.None)
                    {
                        EndBattle(result);
                        yield break;
                    }
                }

                // Process turn end
                ProcessTurnEnd();
                OnTurnEnd?.Invoke(battleContext.CurrentTurn);

                yield return new WaitForSeconds(actionDelay);
            }

            // Time out - Draw
            EndBattle(BattleResult.Draw);
        }

        /// <summary>
        /// 행동 순서 결정 (속도 기반, 동속 동시 공격)
        /// </summary>
        private List<(BaseUnit unit, bool isPlayer)> DetermineActionOrder()
        {
            var allUnits = new List<(BaseUnit unit, bool isPlayer, int speed)>();

            // Add alive units
            foreach (var unit in playerUnits.Where(u => u.IsAlive))
            {
                allUnits.Add((unit, true, unit.Stats.Speed));
            }

            foreach (var unit in enemyUnits.Where(u => u.IsAlive))
            {
                allUnits.Add((unit, false, unit.Stats.Speed));
            }

            // Group by speed for simultaneous actions
            var speedGroups = allUnits.GroupBy(u => u.speed)
                                      .OrderByDescending(g => g.Key)
                                      .ToList();

            var actionOrder = new List<(BaseUnit unit, bool isPlayer)>();

            foreach (var group in speedGroups)
            {
                var groupUnits = group.ToList();

                // Check for 2x speed advantage
                var minOpponentSpeed = allUnits
                    .Where(u => u.isPlayer != groupUnits[0].isPlayer)
                    .Select(u => u.speed)
                    .DefaultIfEmpty(0)
                    .Min();

                bool hasDoubleSpeed = groupUnits[0].speed >= minOpponentSpeed * 2;

                // Add units to action order
                foreach (var (unit, isPlayer, speed) in groupUnits)
                {
                    actionOrder.Add((unit, isPlayer));

                    // Add again if has 2x speed
                    if (hasDoubleSpeed && minOpponentSpeed > 0)
                    {
                        actionOrder.Add((unit, isPlayer));
                        Debug.Log($"[BattleManager] {unit.Name} has double speed! Extra action granted.");
                    }
                }
            }

            return actionOrder;
        }

        /// <summary>
        /// 유닛 행동 실행
        /// </summary>
        private IEnumerator ExecuteUnitAction(BaseUnit unit, bool isPlayer)
        {
            // Find target
            var targets = isPlayer ? enemyUnits : playerUnits;
            var target = GetFrontTarget(targets);

            if (target == null)
            {
                Debug.Log($"[BattleManager] {unit.Name} has no valid target!");
                yield break;
            }

            Debug.Log($"[BattleManager] {unit.Name} attacks {target.Name}!");

            // Perform attack
            unit.PerformAttack(target);

            // Wait for animation
            yield return new WaitForSeconds(actionDelay);

            // Check if target died
            if (!target.IsAlive)
            {
                Debug.Log($"[BattleManager] {target.Name} has been defeated!");
                OnUnitDeath(target, !isPlayer);
            }
        }

        /// <summary>
        /// 가장 앞의 타겟 찾기
        /// </summary>
        private BaseUnit GetFrontTarget(List<BaseUnit> targets)
        {
            return targets.FirstOrDefault(t => t.IsAlive);
        }

        /// <summary>
        /// 유닛 사망 처리
        /// </summary>
        private void OnUnitDeath(BaseUnit unit, bool isPlayer)
        {
            // Trigger death skills
            var team = isPlayer ? playerUnits : enemyUnits;

            foreach (var ally in team.Where(u => u.IsAlive))
            {
                // Check for Spiritist skill
                if (ally.EvolutionType == EvolutionType.Spiritist)
                {
                    // Summon soul
                    Debug.Log($"[BattleManager] {ally.Name} summons a soul of {unit.Name}!");
                    // TODO: Implement soul summoning
                }
            }
        }

        /// <summary>
        /// 턴 시작 처리
        /// </summary>
        private void ProcessTurnStart()
        {
            var allUnits = playerUnits.Concat(enemyUnits).Where(u => u.IsAlive);

            foreach (var unit in allUnits)
            {
                unit.OnTurnStart();
            }
        }

        /// <summary>
        /// 턴 종료 처리
        /// </summary>
        private void ProcessTurnEnd()
        {
            var allUnits = playerUnits.Concat(enemyUnits).Where(u => u.IsAlive);

            foreach (var unit in allUnits)
            {
                unit.OnTurnEnd();

                // Special turn end effects
                if (unit.EvolutionType == EvolutionType.Negotiator)
                {
                    int goldEarned = UnityEngine.Random.Range(1, 4);
                    Debug.Log($"[BattleManager] {unit.Name} earned {goldEarned} gold!");
                    // TODO: Add gold to player
                }
            }
        }

        /// <summary>
        /// 전투 종료 체크
        /// </summary>
        private BattleResult CheckBattleEnd()
        {
            bool playerAlive = playerUnits.Any(u => u.IsAlive);
            bool enemyAlive = enemyUnits.Any(u => u.IsAlive);

            if (!playerAlive && !enemyAlive)
            {
                return BattleResult.Draw;  // 무승부
            }
            else if (!enemyAlive)
            {
                return BattleResult.Victory;
            }
            else if (!playerAlive)
            {
                return BattleResult.Defeat;
            }

            return BattleResult.None;
        }

        /// <summary>
        /// 전투 종료
        /// </summary>
        private void EndBattle(BattleResult result)
        {
            CurrentPhase = GamePhase.Result;

            Debug.Log($"[BattleManager] === Battle Ended: {result} ===");

            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }

            OnBattleEnd?.Invoke(result);
        }

        /// <summary>
        /// 유닛 배치
        /// </summary>
        private void PositionUnits(List<BaseUnit> units, Transform field)
        {
            float spacing = 1.5f;
            float startX = -(units.Count - 1) * spacing / 2f;

            for (int i = 0; i < units.Count; i++)
            {
                units[i].transform.SetParent(field);
                units[i].transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
                units[i].transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 전투 컨텍스트 가져오기
        /// </summary>
        public BattleContext GetContext()
        {
            return battleContext;
        }
    }
}