using System.Collections.Generic;
using UnityEngine;
using SpiritAge.Core.Enums;
using SpiritAge.Core.Data;

namespace SpiritAge.Core.Interfaces
{
    /// <summary>
    /// 유닛의 기본 인터페이스
    /// </summary>
    public interface IUnit
    {
        string Id { get; }
        string Name { get; }
        UnitType UnitType { get; }
        EvolutionType EvolutionType { get; set; }
        IUnitStats Stats { get; }
        List<ElementAttribute> Attributes { get; }
        void Initialize(UnitData data);
        void TakeDamage(int damage, IUnit attacker);
        void Heal(int amount);
        bool IsAlive { get; }
    }

    /// <summary>
    /// 유닛 스탯 인터페이스
    /// </summary>
    public interface IUnitStats
    {
        int Attack { get; set; }
        int Health { get; set; }
        int MaxHealth { get; set; }
        int Speed { get; set; }
        void ApplyModifier(StatModifier modifier);
        void RemoveModifier(StatModifier modifier);
        void ResetToBase();
    }

    /// <summary>
    /// 전투 가능한 유닛 인터페이스
    /// </summary>
    public interface ICombatUnit : IUnit
    {
        void PerformAttack(IUnit target);
        void OnTurnStart();
        void OnTurnEnd();
        int CalculateDamage(IUnit target);
        List<IBuff> ActiveBuffs { get; }
        void AddBuff(IBuff buff);
        void RemoveBuff(BuffType type);
    }

    /// <summary>
    /// 버프/디버프 인터페이스
    /// </summary>
    public interface IBuff
    {
        BuffType Type { get; }
        int Duration { get; set; }
        int Stack { get; set; }
        bool IsExpired { get; }
        void OnApply(IUnit target);
        void OnTick(IUnit target);
        void OnRemove(IUnit target);
    }

    /// <summary>
    /// 스킬 인터페이스
    /// </summary>
    public interface ISkill
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        SkillTriggerType TriggerType { get; }
        void Execute(IUnit caster, List<IUnit> targets, BattleContext context);
        bool CanExecute(IUnit caster, BattleContext context);
    }

    /// <summary>
    /// 진화 조건 인터페이스
    /// </summary>
    public interface IEvolutionCondition
    {
        bool CheckCondition(IUnit unit, GameContext context);
        string GetDescription();
    }

    /// <summary>
    /// 드래그 가능한 객체 인터페이스
    /// </summary>
    public interface IDraggable
    {
        void OnDragStart(Vector3 position);
        void OnDrag(Vector3 position);
        void OnDragEnd(Vector3 position);
        bool CanDrag();
    }

    /// <summary>
    /// 드롭 가능한 슬롯 인터페이스
    /// </summary>
    public interface IDropSlot
    {
        bool CanAcceptDrop(IDraggable draggable);
        void OnDropReceived(IDraggable draggable);
        void OnDropHover(IDraggable draggable);
        void OnDropExit(IDraggable draggable);
    }

    /// <summary>
    /// 풀링 가능한 객체 인터페이스
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
        void ResetState();
    }
}