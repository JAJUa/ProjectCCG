using System.Collections.Generic;
using UnityEngine;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Data;
using SpiritAge.Utility.Probability;

namespace SpiritAge.Skills
{
    /// <summary>
    /// 스킬 팩토리
    /// </summary>
    public static class SkillFactory
    {
        public static ISkill CreateSkill(SkillData data)
        {
            // Create skill based on skill ID or type
            switch (data.id)
            {
                case "berserker_cleave":
                    return new BerserkerCleaveSkill(data);
                case "gambler_luck":
                    return new GamblerLuckSkill(data);
                case "spiritist_summon":
                    return new SpiritistSummonSkill(data);
                // Add more skills...
                default:
                    return new GenericSkill(data);
            }
        }
    }

    /// <summary>
    /// 광전사 광역 공격 스킬
    /// </summary>
    public class BerserkerCleaveSkill : BaseSkill
    {
        public override SkillTriggerType TriggerType => SkillTriggerType.OnAttack;

        public BerserkerCleaveSkill(SkillData data) : base(data) { }

        public override void Execute(IUnit caster, List<IUnit> targets, BattleContext context)
        {
            if (targets.Count == 0) return;

            // Deal splash damage to up to 2 enemies
            int splashDamage = Mathf.RoundToInt(caster.Stats.Attack * data.value1); // value1 = 0.5 for 50%
            int hitCount = 0;

            foreach (var target in targets)
            {
                if (target.IsAlive && hitCount < 2)
                {
                    target.TakeDamage(splashDamage, caster);
                    hitCount++;
                    Debug.Log($"[Berserker] Cleave damage {splashDamage} to {target.Name}");
                }
            }
        }
    }

    /// <summary>
    /// 도박사 확률 스킬
    /// </summary>
    public class GamblerLuckSkill : BaseSkill
    {
        public override SkillTriggerType TriggerType => SkillTriggerType.OnAttack;

        public GamblerLuckSkill(SkillData data) : base(data) { }

        public override void Execute(IUnit caster, List<IUnit> targets, BattleContext context)
        {
            if (targets.Count == 0) return;

            float procChance = data.value1; // e.g., 0.3 for 30%

            if (ProbabilitySystem.Check(procChance))
            {
                int extraDamage = Random.Range(1, 4);
                targets[0].TakeDamage(extraDamage, caster);
                Debug.Log($"[Gambler] Lucky strike! Extra {extraDamage} damage");

                // Recursive check for chain luck
                if (ProbabilitySystem.Check(procChance))
                {
                    Execute(caster, targets, context);
                }
            }
        }
    }

    /// <summary>
    /// 영혼술사 소환 스킬
    /// </summary>
    public class SpiritistSummonSkill : BaseSkill
    {
        public override SkillTriggerType TriggerType => SkillTriggerType.OnAllyDeath;

        public SpiritistSummonSkill(SkillData data) : base(data) { }

        public override void Execute(IUnit caster, List<IUnit> targets, BattleContext context)
        {
            // Summon soul unit
            Debug.Log($"[Spiritist] {caster.Name} summons a soul!");
            // TODO: Implement soul unit creation
        }
    }

    /// <summary>
    /// 일반 스킬
    /// </summary>
    public class GenericSkill : BaseSkill
    {
        public override SkillTriggerType TriggerType => data.triggerType;

        public GenericSkill(SkillData data) : base(data) { }

        public override void Execute(IUnit caster, List<IUnit> targets, BattleContext context)
        {
            Debug.Log($"[Skill] {caster.Name} executes {Name}");
        }
    }
}