using System.Collections.Generic;
using UnityEngine;
using SpiritAge.Core.Interfaces;
using SpiritAge.Core.Data;
using UnityEditor;

namespace SpiritAge.Stats
{
    /// <summary>
    /// 유닛 스탯 시스템
    /// </summary>
    public class UnitStats : IUnitStats
    {
        // Base stats
        private int baseAttack;
        private int baseHealth;
        private int baseSpeed;

        // Current stats
        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Speed { get; set; }

        // Modifiers
        private List<StatModifier> attackModifiers = new List<StatModifier>();
        private List<StatModifier> healthModifiers = new List<StatModifier>();
        private List<StatModifier> speedModifiers = new List<StatModifier>();

        public UnitStats(int attack, int health, int speed)
        {
            baseAttack = attack;
            baseHealth = health;
            baseSpeed = speed;

            ResetToBase();
        }

        /// <summary>
        /// 스탯을 기본값으로 리셋
        /// </summary>
        public void ResetToBase()
        {
            Attack = CalculateStat(baseAttack, attackModifiers);
            MaxHealth = CalculateStat(baseHealth, healthModifiers);
            Health = MaxHealth;
            Speed = CalculateStat(baseSpeed, speedModifiers);
        }

        /// <summary>
        /// 수정자 적용
        /// </summary>
        public void ApplyModifier(StatModifier modifier)
        {
            // Determine which stat to modify based on context
            // This is simplified - in practice you'd have stat type in modifier
            attackModifiers.Add(modifier);
            RecalculateStats();
        }

        /// <summary>
        /// 수정자 제거
        /// </summary>
        public void RemoveModifier(StatModifier modifier)
        {
            attackModifiers.Remove(modifier);
            healthModifiers.Remove(modifier);
            speedModifiers.Remove(modifier);
            RecalculateStats();
        }

        /// <summary>
        /// 스탯 재계산
        /// </summary>
        private void RecalculateStats()
        {
            int oldMaxHealth = MaxHealth;

            Attack = CalculateStat(baseAttack, attackModifiers);
            MaxHealth = CalculateStat(baseHealth, healthModifiers);
            Speed = CalculateStat(baseSpeed, speedModifiers);

            // Adjust current health if max health changed
            if (MaxHealth != oldMaxHealth)
            {
                float healthPercent = (float)Health / oldMaxHealth;
                Health = Mathf.RoundToInt(MaxHealth * healthPercent);
            }
        }

        /// <summary>
        /// 스탯 계산
        /// </summary>
        private int CalculateStat(int baseValue, List<StatModifier> modifiers)
        {
            float finalValue = baseValue;

            // Sort modifiers by order
            modifiers.Sort((a, b) => a.order.CompareTo(b.order));

            // Apply flat modifiers first
            foreach (var mod in modifiers)
            {
                if (mod.type == StatModifier.ModifierType.Flat)
                {
                    finalValue += mod.value;
                }
            }

            // Apply percent add
            float percentAdd = 0;
            foreach (var mod in modifiers)
            {
                if (mod.type == StatModifier.ModifierType.PercentAdd)
                {
                    percentAdd += mod.value;
                }
            }
            finalValue *= (1 + percentAdd);

            // Apply percent multiply
            foreach (var mod in modifiers)
            {
                if (mod.type == StatModifier.ModifierType.PercentMultiply)
                {
                    finalValue *= (1 + mod.value);
                }
            }

            return Mathf.RoundToInt(finalValue);
        }
    }
}