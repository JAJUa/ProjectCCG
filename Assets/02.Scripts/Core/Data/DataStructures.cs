using SpiritAge.Core.Enums;
using System;
using System.Collections.Generic;
using SpiritAge.Core.Interfaces;

namespace SpiritAge.Core.Data
{
    /// <summary>
    /// ���� ������
    /// </summary>
    [Serializable]
    public class UnitData
    {
        public string id;
        public string name;
        public UnitType unitType;
        public EvolutionType evolutionType;
        public int cost;
        public int baseAttack;
        public int baseHealth;
        public int baseSpeed;
        public List<ElementAttribute> attributes = new List<ElementAttribute>();
        public string skillId;
        public string description;
        public string spritePath;
    }

    /// <summary>
    /// ��ų ������
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public string id;
        public string name;
        public string description;
        public SkillTriggerType triggerType;
        public float value1;
        public float value2;
        public float value3;
        public string effectPrefabPath;
    }

    /// <summary>
    /// ���� ������
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        public enum ModifierType
        {
            Flat = 0,
            PercentAdd = 1,
            PercentMultiply = 2
        }

        public float value;
        public ModifierType type;
        public object source;
        public int order;

        public StatModifier(float value, ModifierType type, object source = null, int order = 0)
        {
            this.value = value;
            this.type = type;
            this.source = source;
            this.order = order;
        }
    }

    /// <summary>
    /// ���� ���ؽ�Ʈ
    /// </summary>
    public class BattleContext
    {
        public List<IUnit> PlayerUnits { get; set; }
        public List<IUnit> EnemyUnits { get; set; }
        public int CurrentTurn { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public Dictionary<string, object> CustomData { get; set; }

        public BattleContext()
        {
            PlayerUnits = new List<IUnit>();
            EnemyUnits = new List<IUnit>();
            CustomData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// ���� ���ؽ�Ʈ
    /// </summary>
    public class GameContext
    {
        public int CurrentRound { get; set; }
        public int PlayerGold { get; set; }
        public int PlayerHealth { get; set; }
        public List<IUnit> OwnedUnits { get; set; }
        public List<IUnit> Formation { get; set; }
        public Dictionary<string, int> Statistics { get; set; }

        public GameContext()
        {
            OwnedUnits = new List<IUnit>();
            Formation = new List<IUnit>();
            Statistics = new Dictionary<string, int>();
        }
    }
}