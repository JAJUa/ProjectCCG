using UnityEngine;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Enums;

namespace SpiritAge.Buffs
{
    /// <summary>
    /// ���� ���� (�ӵ� ����)
    /// </summary>
    public class FreezeBuff : BaseBuff
    {
        public override BuffType Type => BuffType.Freeze;
        private const int SPEED_REDUCTION = 2;

        public FreezeBuff() : base(2, 1) { }

        public override void OnApply(IUnit target)
        {
            base.OnApply(target);
            target.Stats.Speed = Mathf.Max(1, target.Stats.Speed - SPEED_REDUCTION * Stack);
        }

        public override void OnTick(IUnit target)
        {
            // No damage on tick
        }

        public override void OnRemove(IUnit target)
        {
            base.OnRemove(target);
            target.Stats.Speed += SPEED_REDUCTION * Stack;
        }
    }

    /// <summary>
    /// ȭ�� ���� (���� ������)
    /// </summary>
    public class BurnBuff : BaseBuff
    {
        public override BuffType Type => BuffType.Burn;
        private const int BURN_DAMAGE = 3;

        public BurnBuff() : base(3, 1) { }

        public override void OnTick(IUnit target)
        {
            int damage = BURN_DAMAGE * Stack;
            target.TakeDamage(damage, null);
            Debug.Log($"[Burn] {target.Name} takes {damage} burn damage");
        }
    }

    /// <summary>
    /// ���� ���� (3��ø�� ����)
    /// </summary>
    public class LightningBuff : BaseBuff
    {
        public override BuffType Type => BuffType.Lightning;
        private const int STUN_STACK_REQUIRED = 3;

        public LightningBuff() : base(-1, 1) { } // Permanent until triggered

        public override void OnApply(IUnit target)
        {
            base.OnApply(target);

            if (Stack >= STUN_STACK_REQUIRED)
            {
                // Apply stun and remove lightning
                target.AddBuff(new StunBuff());
                target.RemoveBuff(BuffType.Lightning);
                Debug.Log($"[Lightning] {target.Name} is stunned!");
            }
        }

        public override void OnTick(IUnit target)
        {
            // No effect on tick
        }
    }

    /// <summary>
    /// ���� ����
    /// </summary>
    public class StunBuff : BaseBuff
    {
        public override BuffType Type => BuffType.Stun;

        public StunBuff() : base(1, 1) { }

        public override void OnApply(IUnit target)
        {
            base.OnApply(target);
            target.Stats.Speed = 0;
        }

        public override void OnTick(IUnit target)
        {
            // No effect on tick
        }

        public override void OnRemove(IUnit target)
        {
            base.OnRemove(target);
            target.Stats.ResetToBase(); // Restore speed
        }
    }

    /// <summary>
    /// ��ȥ ���� (�Ϸ����)
    /// </summary>
    public class SoulBuff : BaseBuff
    {
        public override BuffType Type => BuffType.Soul;

        public SoulBuff() : base(-1, 1) { } // Permanent

        public override void OnApply(IUnit target)
        {
            base.OnApply(target);
            // Soul units are fragile - instant death on any attack
        }

        public override void OnTick(IUnit target)
        {
            // No effect on tick
        }
    }

    /// <summary>
    /// ���� ���� (���ݷ¡� ���¡�)
    /// </summary>
    public class MadnessBuff : BaseBuff
    {
        public override BuffType Type => BuffType.Madness;

        public MadnessBuff() : base(3, 1) { }

        public override void OnApply(IUnit target)
        {
            base.OnApply(target);
            // Attack boost handled in damage calculation
        }

        public override void OnTick(IUnit target)
        {
            // No effect on tick
        }
    }
}