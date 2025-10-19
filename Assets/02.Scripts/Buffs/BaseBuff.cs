using UnityEngine;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;

namespace SpiritAge.Buffs
{
    /// <summary>
    /// 기본 버프 클래스
    /// </summary>
    public abstract class BaseBuff : IBuff
    {
        public abstract BuffType Type { get; }
        public int Duration { get; set; }
        public int Stack { get; set; }
        public bool IsExpired => Duration == 0;

        protected BaseBuff(int duration = 3, int stack = 1)
        {
            Duration = duration;
            Stack = stack;
        }

        public virtual void OnApply(IUnit target)
        {
            Debug.Log($"[Buff] {Type} applied to {target.Name} (Stack: {Stack})");
        }

        public abstract void OnTick(IUnit target);

        public virtual void OnRemove(IUnit target)
        {
            Debug.Log($"[Buff] {Type} removed from {target.Name}");
        }
    }
}