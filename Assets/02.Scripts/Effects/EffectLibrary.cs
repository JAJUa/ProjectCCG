using System.Collections.Generic;
using UnityEngine;

namespace SpiritAge.Effects
{
    /// <summary>
    /// 이펙트 라이브러리
    /// </summary>
    [CreateAssetMenu(fileName = "EffectLibrary", menuName = "SpiritAge/Effect Library")]
    public class EffectLibrary : ScriptableObject
    {
        [System.Serializable]
        public class EffectEntry
        {
            public string key;
            public GameObject prefab;
            public int poolSize = 5;
            public int maxPoolSize = 20;
        }

        [SerializeField] private List<EffectEntry> effects = new List<EffectEntry>();

        public List<EffectEntry> GetAllEffects()
        {
            return effects;
        }

        public GameObject GetEffectPrefab(string key)
        {
            var entry = effects.Find(e => e.key == key);
            return entry?.prefab;
        }
    }
}