using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;
using SpiritAge.Utility;

namespace SpiritAge.Audio
{
    /// <summary>
    /// 사운드 매니저
    /// </summary>
    public class SoundManager : AbstractSingleton<SoundManager>
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource[] sfxSources;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private int sfxSourceCount = 10;

        [Header("Audio Clips")]
        [SerializeField] private AudioLibrary audioLibrary;

        // Audio settings
        private float masterVolume = 1f;
        private float bgmVolume = 0.7f;
        private float sfxVolume = 1f;
        private float uiVolume = 1f;
        private bool isMuted = false;

        // SFX management
        private int currentSfxIndex = 0;
        private Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();
        private const float MIN_REPLAY_INTERVAL = 0.05f;

        protected override void OnSingletonAwake()
        {
            InitializeAudioSources();
            LoadAudioSettings();
        }

        /// <summary>
        /// 오디오 소스 초기화
        /// </summary>
        private void InitializeAudioSources()
        {
            // Create BGM source
            if (bgmSource == null)
            {
                GameObject bgmGO = new GameObject("BGM Source");
                bgmGO.transform.SetParent(transform);
                bgmSource = bgmGO.AddComponent<AudioSource>();
                bgmSource.outputAudioMixerGroup = bgmMixerGroup;
                bgmSource.loop = true;
                bgmSource.priority = 0;
            }

            // Create SFX sources
            if (sfxSources == null || sfxSources.Length == 0)
            {
                sfxSources = new AudioSource[sfxSourceCount];
                for (int i = 0; i < sfxSourceCount; i++)
                {
                    GameObject sfxGO = new GameObject($"SFX Source {i}");
                    sfxGO.transform.SetParent(transform);
                    sfxSources[i] = sfxGO.AddComponent<AudioSource>();
                    sfxSources[i].outputAudioMixerGroup = sfxMixerGroup;
                    sfxSources[i].playOnAwake = false;
                }
            }

            // Create UI source
            if (uiSource == null)
            {
                GameObject uiGO = new GameObject("UI Source");
                uiGO.transform.SetParent(transform);
                uiSource = uiGO.AddComponent<AudioSource>();
                uiSource.outputAudioMixerGroup = uiMixerGroup;
                uiSource.playOnAwake = false;
                uiSource.priority = 1;
            }
        }

        /// <summary>
        /// 오디오 설정 로드
        /// </summary>
        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
            isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

            ApplyVolumeSettings();
        }

        /// <summary>
        /// 오디오 설정 저장
        /// </summary>
        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 볼륨 설정 적용
        /// </summary>
        private void ApplyVolumeSettings()
        {
            if (masterMixer != null)
            {
                float masterDB = isMuted ? -80f : Mathf.Log10(masterVolume) * 20;
                float bgmDB = Mathf.Log10(bgmVolume) * 20;
                float sfxDB = Mathf.Log10(sfxVolume) * 20;
                float uiDB = Mathf.Log10(uiVolume) * 20;

                masterMixer.SetFloat("MasterVolume", masterDB);
                masterMixer.SetFloat("BGMVolume", bgmDB);
                masterMixer.SetFloat("SFXVolume", sfxDB);
                masterMixer.SetFloat("UIVolume", uiDB);
            }
        }

        // ========== BGM Control ==========

        /// <summary>
        /// BGM 재생
        /// </summary>
        public void PlayBGM(string clipName, float fadeInTime = 1f)
        {
            AudioClip clip = GetAudioClip(clipName);
            if (clip == null) return;

            if (bgmSource.isPlaying && bgmSource.clip == clip) return;

            StartCoroutine(FadeBGM(clip, fadeInTime));
        }

        /// <summary>
        /// BGM 페이드
        /// </summary>
        private IEnumerator FadeBGM(AudioClip newClip, float fadeTime)
        {
            // Fade out current
            if (bgmSource.isPlaying)
            {
                float startVolume = bgmSource.volume;
                float elapsed = 0f;

                while (elapsed < fadeTime / 2)
                {
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeTime / 2));
                    yield return null;
                }

                bgmSource.Stop();
            }

            // Change clip
            bgmSource.clip = newClip;
            bgmSource.Play();

            // Fade in
            float targetVolume = bgmVolume;
            float elapsedIn = 0f;

            while (elapsedIn < fadeTime / 2)
            {
                elapsedIn += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsedIn / (fadeTime / 2));
                yield return null;
            }

            bgmSource.volume = targetVolume;
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM(float fadeOutTime = 1f)
        {
            if (!bgmSource.isPlaying) return;

            if (fadeOutTime <= 0)
            {
                bgmSource.Stop();
            }
            else
            {
                bgmSource.DOFade(0f, fadeOutTime).OnComplete(() => bgmSource.Stop());
            }
        }

        // ========== SFX Control ==========

        /// <summary>
        /// SFX 재생
        /// </summary>
        public void PlaySFX(string clipName, float volumeScale = 1f, float pitch = 1f)
        {
            // Check replay interval
            if (lastPlayTime.ContainsKey(clipName))
            {
                if (Time.time - lastPlayTime[clipName] < MIN_REPLAY_INTERVAL)
                    return;
            }

            AudioClip clip = GetAudioClip(clipName);
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = sfxVolume * volumeScale;
            source.pitch = pitch;
            source.Play();

            lastPlayTime[clipName] = Time.time;
        }

        /// <summary>
        /// 3D SFX 재생
        /// </summary>
        public void PlaySFXAtPosition(string clipName, Vector3 position, float volumeScale = 1f)
        {
            AudioClip clip = GetAudioClip(clipName);
            if (clip == null) return;

            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;

            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.outputAudioMixerGroup = sfxMixerGroup;
            tempSource.volume = sfxVolume * volumeScale;
            tempSource.spatialBlend = 1f; // 3D sound
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            tempSource.maxDistance = 20f;
            tempSource.Play();

            Destroy(tempGO, clip.length);
        }

        /// <summary>
        /// 랜덤 SFX 재생
        /// </summary>
        public void PlayRandomSFX(string[] clipNames, float volumeScale = 1f)
        {
            if (clipNames == null || clipNames.Length == 0) return;

            int randomIndex = UnityEngine.Random.Range(0, clipNames.Length);
            PlaySFX(clipNames[randomIndex], volumeScale);
        }

        /// <summary>
        /// 사용 가능한 SFX 소스 가져오기
        /// </summary>
        private AudioSource GetAvailableSFXSource()
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                int index = (currentSfxIndex + i) % sfxSources.Length;
                if (!sfxSources[index].isPlaying)
                {
                    currentSfxIndex = (index + 1) % sfxSources.Length;
                    return sfxSources[index];
                }
            }

            // All sources busy, use the oldest one
            currentSfxIndex = (currentSfxIndex + 1) % sfxSources.Length;
            sfxSources[currentSfxIndex].Stop();
            return sfxSources[currentSfxIndex];
        }

        // ========== UI Sound Control ==========

        /// <summary>
        /// UI 사운드 재생
        /// </summary>
        public void PlayUISound(string clipName, float volumeScale = 1f)
        {
            AudioClip clip = GetAudioClip(clipName);
            if (clip == null) return;

            uiSource.PlayOneShot(clip, uiVolume * volumeScale);
        }

        /// <summary>
        /// 버튼 클릭 사운드
        /// </summary>
        public void PlayButtonClick()
        {
            PlayUISound("ui_button_click");
        }

        /// <summary>
        /// 성공 사운드
        /// </summary>
        public void PlaySuccess()
        {
            PlayUISound("ui_success");
        }

        /// <summary>
        /// 에러 사운드
        /// </summary>
        public void PlayError()
        {
            PlayUISound("ui_error");
        }

        // ========== Audio Clip Management ==========

        /// <summary>
        /// 오디오 클립 가져오기
        /// </summary>
        private AudioClip GetAudioClip(string clipName)
        {
            if (audioLibrary == null)
            {
                Debug.LogWarning($"[SoundManager] Audio library is null");
                return null;
            }

            AudioClip clip = audioLibrary.GetClip(clipName);
            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] Audio clip not found: {clipName}");
            }

            return clip;
        }

        // ========== Volume Control ==========

        /// <summary>
        /// 마스터 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveAudioSettings();
        }

        /// <summary>
        /// BGM 볼륨 설정
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            bgmSource.volume = bgmVolume;
            ApplyVolumeSettings();
            SaveAudioSettings();
        }

        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveAudioSettings();
        }

        /// <summary>
        /// UI 볼륨 설정
        /// </summary>
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveAudioSettings();
        }

        /// <summary>
        /// 음소거 토글
        /// </summary>
        public void ToggleMute()
        {
            isMuted = !isMuted;
            ApplyVolumeSettings();
            SaveAudioSettings();
        }

        // ========== Public Properties ==========

        public float MasterVolume => masterVolume;
        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;
        public float UIVolume => uiVolume;
        public bool IsMuted => isMuted;
    }
}