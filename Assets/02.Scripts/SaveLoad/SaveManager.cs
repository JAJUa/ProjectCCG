/*using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BackEnd;
using SpiritAge.Core;
using SpiritAge.Utility;
using BackEnd.BackndNewtonsoft.Json;
using SpiritAge.Audio;

namespace SpiritAge.SaveLoad
{
    /// <summary>
    /// 세이브 매니저
    /// </summary>
    public class SaveManager : AbstractSingleton<SaveManager>
    {
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "spiritage_save";
        [SerializeField] private int maxSaveSlots = 3;
        [SerializeField] private bool useCloudSave = true;
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 60f;

        // Save data
        private SaveData currentSaveData;
        private float lastAutoSaveTime;

        // Events
        public event Action OnSaveComplete;
        public event Action OnLoadComplete;
        public event Action<string> OnSaveError;
        public event Action<string> OnLoadError;

        protected override void OnSingletonAwake()
        {
            InitializeSaveSystem();
        }

        private void Start()
        {
            if (autoSave)
            {
                InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
            }
        }

        /// <summary>
        /// 세이브 시스템 초기화
        /// </summary>
        private void InitializeSaveSystem()
        {
            // Create save directory if not exists
            string savePath = GetSavePath();
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // Load last save
            LoadGame(0);
        }

        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame(int slotIndex = 0)
        {
            try
            {
                // Create save data
                currentSaveData = CreateSaveData();

                // Save locally
                SaveLocal(slotIndex);

                // Save to cloud if enabled
                if (useCloudSave)
                {
                    SaveToCloud(slotIndex);
                }

                lastAutoSaveTime = Time.time;

                Debug.Log($"[SaveManager] Game saved to slot {slotIndex}");
                OnSaveComplete?.Invoke();
            }
            catch (Exception e)
            {
                string error = $"Failed to save game: {e.Message}";
                Debug.LogError($"[SaveManager] {error}");
                OnSaveError?.Invoke(error);
            }
        }

        /// <summary>
        /// 게임 로드
        /// </summary>
        public void LoadGame(int slotIndex = 0)
        {
            try
            {
                // Try to load from cloud first
                if (useCloudSave)
                {
                    LoadFromCloud(slotIndex, (success, data) =>
                    {
                        if (success && data != null)
                        {
                            currentSaveData = data;
                            ApplySaveData(currentSaveData);
                            OnLoadComplete?.Invoke();
                        }
                        else
                        {
                            // Fallback to local save
                            LoadLocal(slotIndex);
                        }
                    });
                }
                else
                {
                    LoadLocal(slotIndex);
                }
            }
            catch (Exception e)
            {
                string error = $"Failed to load game: {e.Message}";
                Debug.LogError($"[SaveManager] {error}");
                OnLoadError?.Invoke(error);
            }
        }

        /// <summary>
        /// 세이브 데이터 생성
        /// </summary>
        private SaveData CreateSaveData()
        {
            var saveData = new SaveData
            {
                version = Application.version,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

                // Player data
                playerData = new PlayerSaveData
                {
                    nickname = PlayerPrefs.GetString("PlayerNickname", "Player"),
                    level = PlayerPrefs.GetInt("PlayerLevel", 1),
                    experience = PlayerPrefs.GetInt("PlayerExperience", 0),
                    totalGamesPlayed = PlayerPrefs.GetInt("TotalGamesPlayed", 0),
                    totalWins = PlayerPrefs.GetInt("TotalWins", 0)
                },

                // Game data
                gameData = new GameSaveData
                {
                    currentRound = BackendGameManager.Instance.CurrentRound,
                    playerHealth = BackendGameManager.Instance.CurrentPlayerDeck.health,
                    playerGold = BackendGameManager.Instance.CurrentPlayerDeck.gold,
                    ownedUnitIds = GetUnitIds(BackendGameManager.Instance.CurrentPlayerDeck.ownedUnits),
                    formationIds = GetUnitIds(BackendGameManager.Instance.CurrentPlayerDeck.formation)
                },

                // Settings
                settings = new SettingsSaveData
                {
                    masterVolume = SoundManager.Instance.MasterVolume,
                    bgmVolume = SoundManager.Instance.BGMVolume,
                    sfxVolume = SoundManager.Instance.SFXVolume,
                    uiVolume = SoundManager.Instance.UIVolume,
                    isMuted = SoundManager.Instance.IsMuted
                }
            };

            return saveData;
        }

        /// <summary>
        /// 세이브 데이터 적용
        /// </summary>
        private void ApplySaveData(SaveData saveData)
        {
            if (saveData == null) return;

            // Apply player data
            if (saveData.playerData != null)
            {
                PlayerPrefs.SetString("PlayerNickname", saveData.playerData.nickname);
                PlayerPrefs.SetInt("PlayerLevel", saveData.playerData.level);
                PlayerPrefs.SetInt("PlayerExperience", saveData.playerData.experience);
                PlayerPrefs.SetInt("TotalGamesPlayed", saveData.playerData.totalGamesPlayed);
                PlayerPrefs.SetInt("TotalWins", saveData.playerData.totalWins);
            }

            // Apply game data
            if (saveData.gameData != null)
            {
                // This would need to be applied when starting a game
                // Store for later use
                currentSaveData = saveData;
            }

            // Apply settings
            if (saveData.settings != null)
            {
                SoundManager.Instance.SetMasterVolume(saveData.settings.masterVolume);
                SoundManager.Instance.SetBGMVolume(saveData.settings.bgmVolume);
                SoundManager.Instance.SetSFXVolume(saveData.settings.sfxVolume);
                SoundManager.Instance.SetUIVolume(saveData.settings.uiVolume);

                if (saveData.settings.isMuted)
                {
                    SoundManager.Instance.ToggleMute();
                }
            }

            Debug.Log($"[SaveManager] Save data applied from {saveData.timestamp}");
        }

        // ========== Local Save/Load ==========

        /// <summary>
        /// 로컬 저장
        /// </summary>
        private void SaveLocal(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            string jsonData = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);

            File.WriteAllText(filePath, jsonData);

            Debug.Log($"[SaveManager] Saved locally to {filePath}");
        }

        /// <summary>
        /// 로컬 로드
        /// </summary>
        private void LoadLocal(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);

            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                currentSaveData = JsonConvert.DeserializeObject<SaveData>(jsonData);

                ApplySaveData(currentSaveData);
                OnLoadComplete?.Invoke();

                Debug.Log($"[SaveManager] Loaded from {filePath}");
            }
            else
            {
                Debug.Log($"[SaveManager] No save file found at {filePath}");
            }
        }

        // ========== Cloud Save/Load ==========

        /// <summary>
        /// 클라우드 저장
        /// </summary>
        private void SaveToCloud(int slotIndex)
        {
            string jsonData = JsonConvert.SerializeObject(currentSaveData);

            var param = new Param();
            param.Add("slotIndex", slotIndex);
            param.Add("saveData", jsonData);
            param.Add("timestamp", currentSaveData.timestamp);

            Backend.GameData.Insert("saves", param, (callback) =>
            {
                if (callback.IsSuccess())
                {
                    Debug.Log("[SaveManager] Saved to cloud successfully");
                }
                else
                {
                    Debug.LogError($"[SaveManager] Cloud save failed: {callback.GetMessage()}");
                }
            });
        }

        /// <summary>
        /// 클라우드 로드
        /// </summary>
        private void LoadFromCloud(int slotIndex, Action<bool, SaveData> onComplete)
        {
            var whereParam = new Where();
            whereParam.Equal("slotIndex", slotIndex);

            Backend.GameData.GetMyData("saves", whereParam, (callback) =>
            {
                if (callback.IsSuccess())
                {
                    var rows = callback.GetReturnValue()["rows"];

                    if (rows.Count > 0)
                    {
                        string jsonData = rows[0]["saveData"]["S"].ToString();
                        SaveData saveData = JsonConvert.DeserializeObject<SaveData>(jsonData);

                        Debug.Log("[SaveManager] Loaded from cloud successfully");
                        onComplete?.Invoke(true, saveData);
                    }
                    else
                    {
                        Debug.Log("[SaveManager] No cloud save found");
                        onComplete?.Invoke(false, null);
                    }
                }
                else
                {
                    Debug.LogError($"[SaveManager] Cloud load failed: {callback.GetMessage()}");
                    onComplete?.Invoke(false, null);
                }
            });
        }

        // ========== Utility ==========

        /// <summary>
        /// 자동 저장
        /// </summary>
        private void AutoSave()
        {
            if (Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                SaveGame(0);
                Debug.Log("[SaveManager] Auto-saved");
            }
        }

        /// <summary>
        /// 세이브 파일 경로
        /// </summary>
        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(GetSavePath(), $"{saveFileName}_slot{slotIndex}.json");
        }

        /// <summary>
        /// 세이브 디렉토리 경로
        /// </summary>
        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, "saves");
        }

        /// <summary>
        /// 유닛 ID 목록 가져오기
        /// </summary>
        private List<string> GetUnitIds(List<BaseUnit> units)
        {
            var ids = new List<string>();
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    ids.Add(unit.Id);
                }
            }
            return ids;
        }

        /// <summary>
        /// 세이브 슬롯 정보 가져오기
        /// </summary>
        public SaveSlotInfo[] GetSaveSlots()
        {
            var slots = new SaveSlotInfo[maxSaveSlots];

            for (int i = 0; i < maxSaveSlots; i++)
            {
                string filePath = GetSaveFilePath(i);

                if (File.Exists(filePath))
                {
                    try
                    {
                        string jsonData = File.ReadAllText(filePath);
                        SaveData saveData = JsonConvert.DeserializeObject<SaveData>(jsonData);

                        slots[i] = new SaveSlotInfo
                        {
                            slotIndex = i,
                            isUsed = true,
                            timestamp = saveData.timestamp,
                            playerName = saveData.playerData.nickname,
                            round = saveData.gameData.currentRound
                        };
                    }
                    catch
                    {
                        slots[i] = new SaveSlotInfo { slotIndex = i, isUsed = false };
                    }
                }
                else
                {
                    slots[i] = new SaveSlotInfo { slotIndex = i, isUsed = false };
                }
            }

            return slots;
        }

        /// <summary>
        /// 세이브 삭제
        /// </summary>
        public void DeleteSave(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[SaveManager] Deleted save slot {slotIndex}");
            }
        }
    }

    // ========== Data Structures ==========

    [Serializable]
    public class SaveData
    {
        public string version;
        public string timestamp;
        public PlayerSaveData playerData;
        public GameSaveData gameData;
        public SettingsSaveData settings;
    }

    [Serializable]
    public class PlayerSaveData
    {
        public string nickname;
        public int level;
        public int experience;
        public int totalGamesPlayed;
        public int totalWins;
    }

    [Serializable]
    public class GameSaveData
    {
        public int currentRound;
        public int playerHealth;
        public int playerGold;
        public List<string> ownedUnitIds;
        public List<string> formationIds;
    }

    [Serializable]
    public class SettingsSaveData
    {
        public float masterVolume;
        public float bgmVolume;
        public float sfxVolume;
        public float uiVolume;
        public bool isMuted;
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public int slotIndex;
        public bool isUsed;
        public string timestamp;
        public string playerName;
        public int round;
    }
}*/