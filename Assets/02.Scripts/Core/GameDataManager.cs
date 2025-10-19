using System.Collections.Generic;
using UnityEngine;
using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using SpiritAge.Utility;
using SpiritAge.Utility.Data;

namespace SpiritAge.Core
{
    /// <summary>
    /// ���� ������ ������
    /// </summary>
    public class GameDataManager : AbstractSingleton<GameDataManager>
    {
        private Dictionary<string, UnitData> unitDatabase;
        private Dictionary<string, SkillData> skillDatabase;

        protected override void OnSingletonAwake()
        {
            LoadGameData();
        }

        /// <summary>
        /// ���� ������ �ε�
        /// </summary>
        private void LoadGameData()
        {
            Debug.Log("[GameDataManager] Loading game data...");

            unitDatabase = CSVDataLoader.LoadUnitData();
            skillDatabase = CSVDataLoader.LoadSkillData();

            Debug.Log($"[GameDataManager] Loaded {unitDatabase.Count} units, {skillDatabase.Count} skills");
        }

        /// <summary>
        /// ���� ������ ��������
        /// </summary>
        public UnitData GetUnitData(string unitId)
        {
            if (unitDatabase.TryGetValue(unitId, out var data))
            {
                return data;
            }

            Debug.LogWarning($"[GameDataManager] Unit data not found: {unitId}");
            return null;
        }

        /// <summary>
        /// ��ų ������ ��������
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            if (skillDatabase.TryGetValue(skillId, out var data))
            {
                return data;
            }

            Debug.LogWarning($"[GameDataManager] Skill data not found: {skillId}");
            return null;
        }

        /// <summary>
        /// ��� ���� ������
        /// </summary>
        public List<UnitData> GetAllUnits()
        {
            return new List<UnitData>(unitDatabase.Values);
        }

        /// <summary>
        /// Ÿ�Ժ� ���� ���͸�
        /// </summary>
        public List<UnitData> GetUnitsByType(UnitType type)
        {
            var result = new List<UnitData>();

            foreach (var unit in unitDatabase.Values)
            {
                if (unit.unitType == type)
                {
                    result.Add(unit);
                }
            }

            return result;
        }
    }
}