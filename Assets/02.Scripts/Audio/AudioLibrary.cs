using System.Collections.Generic;
using UnityEngine;

namespace SpiritAge.Audio
{
    /// <summary>
    /// 오디오 라이브러리
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "SpiritAge/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class AudioEntry
        {
            public string key;
            public AudioClip clip;
            [Range(0f, 1f)] public float defaultVolume = 1f;
        }

        [SerializeField] private List<AudioEntry> audioClips = new List<AudioEntry>();
        private Dictionary<string, AudioEntry> clipDictionary;

        private void OnEnable()
        {
            BuildDictionary();
        }

        private void BuildDictionary()
        {
            clipDictionary = new Dictionary<string, AudioEntry>();
            foreach (var entry in audioClips)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    clipDictionary[entry.key] = entry;
                }
            }
        }

        public AudioClip GetClip(string key)
        {
            if (clipDictionary == null) BuildDictionary();

            if (clipDictionary.TryGetValue(key, out AudioEntry entry))
            {
                return entry.clip;
            }

            return null;
        }

        public float GetDefaultVolume(string key)
        {
            if (clipDictionary == null) BuildDictionary();

            if (clipDictionary.TryGetValue(key, out AudioEntry entry))
            {
                return entry.defaultVolume;
            }

            return 1f;
        }
    }
}