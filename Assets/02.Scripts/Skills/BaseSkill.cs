using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Interfaces;
using System.Collections.Generic;

namespace SpiritAge.Skills
{
    /// <summary>
    /// 기본 스킬 클래스
    /// </summary>
    public abstract class BaseSkill : ISkill
    {
        protected SkillData data;

        public string Id => data.id;
        public string Name => data.name;
        public string Description => data.description;
        public abstract SkillTriggerType TriggerType { get; }

        protected BaseSkill(SkillData data)
        {
            this.data = data;
        }

        public abstract void Execute(IUnit caster, List<IUnit> targets, BattleContext context);

        public virtual bool CanExecute(IUnit caster, BattleContext context)
        {
            return caster.IsAlive;
        }
    }
}