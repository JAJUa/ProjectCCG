using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using SpiritAge.Core.Data;
using SpiritAge.Core.Enums;
using static UnityEngine.Playables.FrameData;

namespace SpiritAge.Utility.Data
{
    /// <summary>
    /// CSV 데이터 로더
    /// </summary>
    public static class CSVDataLoader
    {
        private const string DATA_PATH = "Data/";
        private const string UNIT_DATA_FILE = "units";
        private const string SKILL_DATA_FILE = "skills";

        /// <summary>
        /// 유닛 데이터 로드
        /// </summary>
        public static Dictionary<string, UnitData> LoadUnitData()
        {
            var unitDict = new Dictionary<string, UnitData>();

            TextAsset csvFile = Resources.Load<TextAsset>($"{DATA_PATH}{UNIT_DATA_FILE}");
            if (csvFile == null)
            {
                Debug.LogError($"[CSVDataLoader] Cannot find unit data file: {DATA_PATH}{UNIT_DATA_FILE}");
                return unitDict;
            }

            string[] lines = csvFile.text.Split('\n');

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = ParseCSVLine(lines[i]);
                if (values.Length < 10) continue;

                try
                {
                    var data = new UnitData
                    {
                        id = values[0].Trim(),
                        name = values[1].Trim(),
                        unitType = ParseEnum<UnitType>(values[2]),
                        evolutionType = ParseEnum<EvolutionType>(values[3]),
                        cost = int.Parse(values[4]),
                        baseAttack = int.Parse(values[5]),
                        baseHealth = int.Parse(values[6]),
                        baseSpeed = int.Parse(values[7]),
                        attributes = ParseAttributes(values[8]),
                        skillId = values[9].Trim(),
                        description = values.Length > 10 ? values[10].Trim() : "",
                        spritePath = values.Length > 11 ? values[11].Trim() : ""
                    };

                    unitDict[data.id] = data;
                    Debug.Log($"[CSVDataLoader] Loaded unit: {data.name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CSVDataLoader] Error parsing unit data at line {i}: {e.Message}");
                }
            }

            Debug.Log($"[CSVDataLoader] Loaded {unitDict.Count} units");
            return unitDict;
        }

        /// <summary>
        /// 스킬 데이터 로드
        /// </summary>
        public static Dictionary<string, SkillData> LoadSkillData()
        {
            var skillDict = new Dictionary<string, SkillData>();

            TextAsset csvFile = Resources.Load<TextAsset>($"{DATA_PATH}{SKILL_DATA_FILE}");
            if (csvFile == null)
            {
                Debug.LogError($"[CSVDataLoader] Cannot find skill data file: {DATA_PATH}{SKILL_DATA_FILE}");
                return skillDict;
            }

            string[] lines = csvFile.text.Split('\n');

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = ParseCSVLine(lines[i]);
                if (values.Length < 7) continue;

                try
                {
                    var data = new SkillData
                    {
                        id = values[0].Trim(),
                        name = values[1].Trim(),
                        description = values[2].Trim(),
                        triggerType = ParseEnum<SkillTriggerType>(values[3]),
                        value1 = float.Parse(values[4]),
                        value2 = float.Parse(values[5]),
                        value3 = float.Parse(values[6]),
                        effectPrefabPath = values.Length > 7 ? values[7].Trim() : ""
                    };

                    skillDict[data.id] = data;
                    Debug.Log($"[CSVDataLoader] Loaded skill: {data.name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CSVDataLoader] Error parsing skill data at line {i}: {e.Message}");
                }
            }

            Debug.Log($"[CSVDataLoader] Loaded {skillDict.Count} skills");
            return skillDict;
        }

        /// <summary>
        /// CSV 라인 파싱 (콤마 처리)
        /// </summary>
        private static string[] ParseCSVLine(string line)
        {
            var pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
            var values = Regex.Split(line, pattern);

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i].Trim('"');
            }

            return values;
        }

        /// <summary>
        /// Enum 파싱
        /// </summary>
        private static T ParseEnum<T>(string value) where T : struct, Enum
        {
            value = value.Trim();
            if (Enum.TryParse<T>(value, true, out T result))
            {
                return result;
            }
            return default(T);
        }

        /// <summary>
        /// 속성 목록 파싱
        /// </summary>
        private static List<ElementAttribute> ParseAttributes(string value)
        {
            var attributes = new List<ElementAttribute>();
            if (string.IsNullOrWhiteSpace(value)) return attributes;

            var parts = value.Split('|');
            foreach (var part in parts)
            {
                if (Enum.TryParse<ElementAttribute>(part.Trim(), true, out var attr))
                {
                    attributes.Add(attr);
                }
            }

            return attributes;
        }
    }
}