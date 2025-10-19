using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpiritAge.Utility.Probability
{
    /// <summary>
    /// 확률 시스템 유틸리티
    /// </summary>
    public static class ProbabilitySystem
    {
        /// <summary>
        /// 확률 체크 (0~1)
        /// </summary>
        public static bool Check(float probability)
        {
            return Random.value < probability;
        }

        /// <summary>
        /// 퍼센트 확률 체크 (0~100)
        /// </summary>
        public static bool CheckPercent(float percent)
        {
            return Random.Range(0f, 100f) < percent;
        }

        /// <summary>
        /// 가중치 기반 랜덤 선택
        /// </summary>
        public static T WeightedRandom<T>(Dictionary<T, float> weights)
        {
            if (weights == null || weights.Count == 0) return default(T);

            float totalWeight = weights.Sum(x => x.Value);
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            return weights.Last().Key;
        }

        /// <summary>
        /// 리스트에서 랜덤 요소 선택
        /// </summary>
        public static T RandomElement<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 리스트에서 중복 없이 N개 랜덤 선택
        /// </summary>
        public static List<T> RandomSample<T>(List<T> list, int count)
        {
            if (list == null || list.Count == 0) return new List<T>();

            var shuffled = new List<T>(list);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int randomIndex = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
            }

            return shuffled.Take(Math.Min(count, shuffled.Count)).ToList();
        }

        /// <summary>
        /// 가우시안 분포 랜덤 (정규분포)
        /// </summary>
        public static float GaussianRandom(float mean, float stdDev)
        {
            float u1 = 1f - Random.value;
            float u2 = 1f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        /// <summary>
        /// 크리티컬 확률 계산
        /// </summary>
        public static bool CheckCritical(float critRate, out float critMultiplier)
        {
            critMultiplier = 1f;
            if (CheckPercent(critRate))
            {
                critMultiplier = 1.5f + Random.Range(0f, 0.5f); // 1.5x ~ 2x
                return true;
            }
            return false;
        }
    }
}