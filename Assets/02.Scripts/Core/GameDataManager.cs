using System.Collections.Generic;
using UnityEngine;
using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using SpiritAge.Utility;
using SpiritAge.Utility.Data;

namespace SpiritAge.Core
{
    /// <summary>
    /// 게임 데이터 관리자
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
        /// 게임 데이터 로드
        /// </summary>
        private void LoadGameData()
        {
            Debug.Log("[GameDataManager] Loading game data...");

            unitDatabase = CSVDataLoader.LoadUnitData();
            skillDatabase = CSVDataLoader.LoadSkillData();

            Debug.Log($"[GameDataManager] Loaded {unitDatabase.Count} units, {skillDatabase.Count} skills");
        }

        /// <summary>
        /// 유닛 데이터 가져오기
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
        /// 스킬 데이터 가져오기
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
        /// 모든 유닛 데이터
        /// </summary>
        public List<UnitData> GetAllUnits()
        {
            return new List<UnitData>(unitDatabase.Values);
        }

        /// <summary>
        /// 타입별 유닛 필터링
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