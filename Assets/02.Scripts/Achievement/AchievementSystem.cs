using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BackEnd;
using SpiritAge.Core;
using SpiritAge.UI;
using SpiritAge.Utility;
using SpiritAge.Audio;
using SpiritAge.Effects;

namespace SpiritAge.Achievement
{
    /// <summary>
    /// 업적 시스템
    /// </summary>
    public class AchievementSystem : AbstractSingleton<AchievementSystem>
    {
        [Header("Achievement Settings")]
        [SerializeField] private AchievementDatabase achievementDatabase;
        [SerializeField] private float notificationDuration = 3f;

        // Achievement tracking
        private Dictionary<string, AchievementProgress> progressTracker;
        private List<string> unlockedAchievements;

        // Events
        public event Action<AchievementData> OnAchievementUnlocked;
        public event Action<string, float> OnProgressUpdated;

        protected override void OnSingletonAwake()
        {
            InitializeAchievements();
            LoadProgress();
        }

        /// <summary>
        /// 업적 초기화
        /// </summary>
        private void InitializeAchievements()
        {
            progressTracker = new Dictionary<string, AchievementProgress>();
            unlockedAchievements = new List<string>();

            if (achievementDatabase != null)
            {
                foreach (var achievement in achievementDatabase.achievements)
                {
                    progressTracker[achievement.id] = new AchievementProgress
                    {
                        achievementId = achievement.id,
                        currentValue = 0,
                        isUnlocked = false
                    };
                }
            }
        }

        /// <summary>
        /// 진행도 로드
        /// </summary>
        private void LoadProgress()
        {
            // Load from PlayerPrefs or Backend
            string savedData = PlayerPrefs.GetString("AchievementProgress", "");

            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var loadedProgress = JsonUtility.FromJson<AchievementSaveData>(savedData);

                    foreach (var progress in loadedProgress.progressList)
                    {
                        if (progressTracker.ContainsKey(progress.achievementId))
                        {
                            progressTracker[progress.achievementId] = progress;

                            if (progress.isUnlocked)
                            {
                                unlockedAchievements.Add(progress.achievementId);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AchievementSystem] Failed to load progress: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 진행도 저장
        /// </summary>
        private void SaveProgress()
        {
            var saveData = new AchievementSaveData
            {
                progressList = progressTracker.Values.ToList()
            };

            string jsonData = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("AchievementProgress", jsonData);
            PlayerPrefs.Save();

            // Also save to Backend
            SaveToBackend(saveData);
        }

        /// <summary>
        /// 백엔드 저장
        /// </summary>
        private void SaveToBackend(AchievementSaveData data)
        {
            var param = new Param();
            param.Add("achievements", JsonUtility.ToJson(data));
            param.Add("timestamp", DateTime.Now.ToString());

            Backend.GameData.Insert("achievements", param, (callback) =>
            {
                if (!callback.IsSuccess())
                {
                    Debug.LogError($"[AchievementSystem] Backend save failed: {callback.GetMessage()}");
                }
            });
        }

        // ========== Progress Tracking ==========

        /// <summary>
        /// 진행도 추가
        /// </summary>
        public void AddProgress(string achievementId, float amount = 1f)
        {
            if (!progressTracker.ContainsKey(achievementId))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement not found: {achievementId}");
                return;
            }

            var progress = progressTracker[achievementId];

            if (progress.isUnlocked) return;

            var achievement = GetAchievementData(achievementId);
            if (achievement == null) return;

            progress.currentValue += amount;
            progress.currentValue = Mathf.Min(progress.currentValue, achievement.targetValue);

            float percentage = progress.currentValue / achievement.targetValue;
            OnProgressUpdated?.Invoke(achievementId, percentage);

            Debug.Log($"[AchievementSystem] {achievementId} progress: {progress.currentValue}/{achievement.targetValue}");

            // Check for unlock
            if (progress.currentValue >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }

            SaveProgress();
        }

        /// <summary>
        /// 진행도 설정
        /// </summary>
        public void SetProgress(string achievementId, float value)
        {
            if (!progressTracker.ContainsKey(achievementId)) return;

            var progress = progressTracker[achievementId];
            if (progress.isUnlocked) return;

            var achievement = GetAchievementData(achievementId);
            if (achievement == null) return;

            progress.currentValue = Mathf.Min(value, achievement.targetValue);

            float percentage = progress.currentValue / achievement.targetValue;
            OnProgressUpdated?.Invoke(achievementId, percentage);

            if (progress.currentValue >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }

            SaveProgress();
        }

        /// <summary>
        /// 업적 해금
        /// </summary>
        private void UnlockAchievement(string achievementId)
        {
            if (unlockedAchievements.Contains(achievementId)) return;

            var achievement = GetAchievementData(achievementId);
            if (achievement == null) return;

            progressTracker[achievementId].isUnlocked = true;
            progressTracker[achievementId].unlockedDate = DateTime.Now.ToString();
            unlockedAchievements.Add(achievementId);

            Debug.Log($"[AchievementSystem] Achievement Unlocked: {achievement.name}!");

            // Give rewards
            GiveRewards(achievement);

            // Show notification
            ShowAchievementNotification(achievement);

            // Fire event
            OnAchievementUnlocked?.Invoke(achievement);

            // Check for chain achievements
            CheckChainAchievements(achievement);

            SaveProgress();
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
        private void GiveRewards(AchievementData achievement)
        {
            if (achievement.goldReward > 0)
            {
                BackendGameManager.Instance.AddGold(achievement.goldReward);
            }

            if (achievement.experienceReward > 0)
            {
                // Add experience
                PlayerPrefs.SetInt("PlayerExperience",
                    PlayerPrefs.GetInt("PlayerExperience", 0) + achievement.experienceReward);
            }

            if (!string.IsNullOrEmpty(achievement.itemReward))
            {
                // Give item
                Debug.Log($"[AchievementSystem] Item reward: {achievement.itemReward}");
            }
        }

        /// <summary>
        /// 업적 알림 표시
        /// </summary>
        private void ShowAchievementNotification(AchievementData achievement)
        {
            string message = $"업적 달성!\n{achievement.name}";
            UIManager.Instance.ShowNotification(message, NotificationType.Success);

            // Play sound
            SoundManager.Instance.PlaySuccess();

            // Screen effect
            EffectManager.Instance.FlashScreen(Color.yellow, 0.3f);
        }

        /// <summary>
        /// 연계 업적 체크
        /// </summary>
        private void CheckChainAchievements(AchievementData achievement)
        {
            // Check if this achievement unlocks others
            foreach (var other in achievementDatabase.achievements)
            {
                if (other.prerequisiteIds.Contains(achievement.id))
                {
                    bool allPrerequisitesMet = true;
                    foreach (var prereq in other.prerequisiteIds)
                    {
                        if (!unlockedAchievements.Contains(prereq))
                        {
                            allPrerequisitesMet = false;
                            break;
                        }
                    }

                    if (allPrerequisitesMet)
                    {
                        // Enable this achievement
                        Debug.Log($"[AchievementSystem] Chain achievement enabled: {other.name}");
                    }
                }
            }
        }

        // ========== Queries ==========

        /// <summary>
        /// 업적 데이터 가져오기
        /// </summary>
        public AchievementData GetAchievementData(string achievementId)
        {
            if (achievementDatabase == null) return null;

            return achievementDatabase.achievements.Find(a => a.id == achievementId);
        }

        /// <summary>
        /// 진행도 가져오기
        /// </summary>
        public AchievementProgress GetProgress(string achievementId)
        {
            if (progressTracker.TryGetValue(achievementId, out var progress))
            {
                return progress;
            }
            return null;
        }

        /// <summary>
        /// 업적 해금 여부
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            return unlockedAchievements.Contains(achievementId);
        }

        /// <summary>
        /// 전체 진행률
        /// </summary>
        public float GetOverallProgress()
        {
            if (achievementDatabase == null || achievementDatabase.achievements.Count == 0)
                return 0f;

            return (float)unlockedAchievements.Count / achievementDatabase.achievements.Count;
        }

        /// <summary>
        /// 카테고리별 업적 가져오기
        /// </summary>
        public List<AchievementData> GetAchievementsByCategory(AchievementCategory category)
        {
            if (achievementDatabase == null) return new List<AchievementData>();

            return achievementDatabase.achievements
                .Where(a => a.category == category)
                .ToList();
        }

        // ========== Debug ==========

        [ContextMenu("Unlock All Achievements")]
        public void UnlockAllAchievements()
        {
            foreach (var achievement in achievementDatabase.achievements)
            {
                UnlockAchievement(achievement.id);
            }
        }

        [ContextMenu("Reset All Progress")]
        public void ResetAllProgress()
        {
            InitializeAchievements();
            SaveProgress();
            Debug.Log("[AchievementSystem] All progress reset");
        }
    }

    // ========== Data Structures ==========

    [Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public float currentValue;
        public bool isUnlocked;
        public string unlockedDate;
    }

    [Serializable]
    public class AchievementSaveData
    {
        public List<AchievementProgress> progressList;
    }

    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "SpiritAge/Achievement Database")]
    public class AchievementDatabase : ScriptableObject
    {
        public List<AchievementData> achievements = new List<AchievementData>();
    }

    [Serializable]
    public class AchievementData
    {
        public string id;
        public string name;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public AchievementCategory category;
        public AchievementTier tier;
        public float targetValue;
        public bool isHidden;

        [Header("Rewards")]
        public int goldReward;
        public int experienceReward;
        public string itemReward;

        [Header("Requirements")]
        public List<string> prerequisiteIds = new List<string>();
    }

    public enum AchievementCategory
    {
        General,
        Combat,
        Collection,
        Evolution,
        Economy,
        Social,
        Special
    }

    public enum AchievementTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }
}