using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpiritAge.Core;
using SpiritAge.UI;
using SpiritAge.Utility;
using SpiritAge.Achievement;

namespace SpiritAge.Quest
{
    /// <summary>
    /// 퀘스트 시스템
    /// </summary>
    public class QuestSystem : AbstractSingleton<QuestSystem>
    {
        [Header("Quest Settings")]
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private int maxActiveQuests = 3;
        [SerializeField] private int maxDailyQuests = 5;

        // Active quests
        private List<QuestProgress> activeQuests = new List<QuestProgress>();
        private List<QuestProgress> completedQuests = new List<QuestProgress>();
        private DateTime lastDailyReset;

        // Events
        public event Action<QuestData> OnQuestAccepted;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestProgress> OnProgressUpdated;
        public event Action OnDailyReset;

        protected override void OnSingletonAwake()
        {
            LoadQuestData();
            CheckDailyReset();
        }

        /// <summary>
        /// 퀘스트 데이터 로드
        /// </summary>
        private void LoadQuestData()
        {
            string savedData = PlayerPrefs.GetString("QuestProgress", "");
            if (!string.IsNullOrEmpty(savedData))
            {
                var saveData = JsonUtility.FromJson<QuestSaveData>(savedData);
                activeQuests = saveData.activeQuests;
                completedQuests = saveData.completedQuests;
                lastDailyReset = DateTime.Parse(saveData.lastDailyReset);
            }
            else
            {
                lastDailyReset = DateTime.Today;
                GenerateDailyQuests();
            }
        }

        /// <summary>
        /// 퀘스트 데이터 저장
        /// </summary>
        private void SaveQuestData()
        {
            var saveData = new QuestSaveData
            {
                activeQuests = activeQuests,
                completedQuests = completedQuests,
                lastDailyReset = lastDailyReset.ToString()
            };

            string jsonData = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("QuestProgress", jsonData);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 일일 리셋 체크
        /// </summary>
        private void CheckDailyReset()
        {
            if (DateTime.Today > lastDailyReset.Date)
            {
                ResetDailyQuests();
            }
        }

        /// <summary>
        /// 일일 퀘스트 리셋
        /// </summary>
        private void ResetDailyQuests()
        {
            Debug.Log("[QuestSystem] Daily reset triggered");

            // Remove incomplete daily quests
            activeQuests.RemoveAll(q =>
            {
                var questData = GetQuestData(q.questId);
                return questData != null && questData.type == QuestType.Daily;
            });

            // Generate new daily quests
            GenerateDailyQuests();

            lastDailyReset = DateTime.Today;
            SaveQuestData();

            OnDailyReset?.Invoke();
        }

        /// <summary>
        /// 일일 퀘스트 생성
        /// </summary>
        private void GenerateDailyQuests()
        {
            if (questDatabase == null) return;

            var dailyQuests = questDatabase.quests
                .Where(q => q.type == QuestType.Daily)
                .OrderBy(x => Guid.NewGuid())
                .Take(maxDailyQuests)
                .ToList();

            foreach (var quest in dailyQuests)
            {
                AcceptQuest(quest.id);
            }

            Debug.Log($"[QuestSystem] Generated {dailyQuests.Count} daily quests");
        }

        // ========== Quest Management ==========

        /// <summary>
        /// 퀘스트 수락
        /// </summary>
        public bool AcceptQuest(string questId)
        {
            // Check if already active
            if (activeQuests.Any(q => q.questId == questId))
            {
                Debug.LogWarning($"[QuestSystem] Quest already active: {questId}");
                return false;
            }

            // Check max active quests
            var questData = GetQuestData(questId);
            if (questData.type != QuestType.Daily && activeQuests.Count >= maxActiveQuests)
            {
                Debug.LogWarning("[QuestSystem] Max active quests reached");
                return false;
            }

            // Check prerequisites
            if (!CheckPrerequisites(questData))
            {
                Debug.LogWarning($"[QuestSystem] Prerequisites not met for: {questId}");
                return false;
            }

            // Create progress
            var progress = new QuestProgress
            {
                questId = questId,
                currentValue = 0,
                acceptedDate = DateTime.Now.ToString(),
                isCompleted = false
            };

            activeQuests.Add(progress);
            OnQuestAccepted?.Invoke(questData);

            Debug.Log($"[QuestSystem] Quest accepted: {questData.name}");
            SaveQuestData();

            return true;
        }

        /// <summary>
        /// 퀘스트 진행도 업데이트
        /// </summary>
        public void UpdateProgress(string questId, float amount = 1f)
        {
            var progress = activeQuests.Find(q => q.questId == questId);
            if (progress == null || progress.isCompleted) return;

            var questData = GetQuestData(questId);
            if (questData == null) return;

            progress.currentValue += amount;
            progress.currentValue = Mathf.Min(progress.currentValue, questData.targetValue);

            OnProgressUpdated?.Invoke(progress);

            Debug.Log($"[QuestSystem] {questId} progress: {progress.currentValue}/{questData.targetValue}");

            // Check completion
            if (progress.currentValue >= questData.targetValue)
            {
                CompleteQuest(questId);
            }

            SaveQuestData();
        }

        /// <summary>
        /// 퀘스트 타입별 진행도 업데이트
        /// </summary>
        public void UpdateProgressByType(QuestObjective objective, float amount = 1f)
        {
            var relevantQuests = activeQuests.Where(q =>
            {
                var data = GetQuestData(q.questId);
                return data != null && data.objective == objective && !q.isCompleted;
            }).ToList();

            foreach (var quest in relevantQuests)
            {
                UpdateProgress(quest.questId, amount);
            }
        }

        /// <summary>
        /// 퀘스트 완료
        /// </summary>
        private void CompleteQuest(string questId)
        {
            var progress = activeQuests.Find(q => q.questId == questId);
            if (progress == null || progress.isCompleted) return;

            var questData = GetQuestData(questId);
            if (questData == null) return;

            progress.isCompleted = true;
            progress.completedDate = DateTime.Now.ToString();

            // Give rewards
            GiveRewards(questData);

            // Move to completed
            activeQuests.Remove(progress);
            completedQuests.Add(progress);

            OnQuestCompleted?.Invoke(questData);

            // Unlock achievement if linked
            if (!string.IsNullOrEmpty(questData.linkedAchievementId))
            {
                AchievementSystem.Instance.AddProgress(questData.linkedAchievementId, 1);
            }

            UIManager.Instance.ShowNotification($"퀘스트 완료: {questData.name}", NotificationType.Success);
            SaveQuestData();
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
        private void GiveRewards(QuestData quest)
        {
            if (quest.goldReward > 0)
            {
                BackendGameManager.Instance.AddGold(quest.goldReward);
            }

            if (quest.experienceReward > 0)
            {
                int currentExp = PlayerPrefs.GetInt("PlayerExperience", 0);
                PlayerPrefs.SetInt("PlayerExperience", currentExp + quest.experienceReward);
            }

            if (!string.IsNullOrEmpty(quest.itemReward))
            {
                // Give item
                Debug.Log($"[QuestSystem] Item reward: {quest.itemReward}");
            }
        }

        /// <summary>
        /// 선행 조건 체크
        /// </summary>
        private bool CheckPrerequisites(QuestData quest)
        {
            foreach (string prereqId in quest.prerequisiteQuestIds)
            {
                if (!completedQuests.Any(q => q.questId == prereqId))
                {
                    return false;
                }
            }

            // Check level requirement
            int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            if (playerLevel < quest.requiredLevel)
            {
                return false;
            }

            return true;
        }

        // ========== Queries ==========

        /// <summary>
        /// 퀘스트 데이터 가져오기
        /// </summary>
        public QuestData GetQuestData(string questId)
        {
            if (questDatabase == null) return null;
            return questDatabase.quests.Find(q => q.id == questId);
        }

        /// <summary>
        /// 활성 퀘스트 목록
        /// </summary>
        public List<QuestProgress> GetActiveQuests()
        {
            return new List<QuestProgress>(activeQuests);
        }

        /// <summary>
        /// 타입별 퀘스트 가져오기
        /// </summary>
        public List<QuestData> GetQuestsByType(QuestType type)
        {
            if (questDatabase == null) return new List<QuestData>();
            return questDatabase.quests.Where(q => q.type == type).ToList();
        }

        /// <summary>
        /// 퀘스트 포기
        /// </summary>
        public void AbandonQuest(string questId)
        {
            var progress = activeQuests.Find(q => q.questId == questId);
            if (progress == null) return;

            var questData = GetQuestData(questId);
            if (questData != null && questData.type == QuestType.Main)
            {
                Debug.LogWarning("[QuestSystem] Cannot abandon main quest");
                return;
            }

            activeQuests.Remove(progress);
            SaveQuestData();

            Debug.Log($"[QuestSystem] Quest abandoned: {questId}");
        }
    }

    // ========== Data Structures ==========

    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public float currentValue;
        public string acceptedDate;
        public bool isCompleted;
        public string completedDate;
    }

    [Serializable]
    public class QuestSaveData
    {
        public List<QuestProgress> activeQuests;
        public List<QuestProgress> completedQuests;
        public string lastDailyReset;
    }

    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "SpiritAge/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestData> quests = new List<QuestData>();
    }

    [Serializable]
    public class QuestData
    {
        public string id;
        public string name;
        [TextArea(2, 4)]
        public string description;
        public QuestType type;
        public QuestObjective objective;
        public float targetValue;
        public int requiredLevel;

        [Header("Rewards")]
        public int goldReward;
        public int experienceReward;
        public string itemReward;
        public string linkedAchievementId;

        [Header("Requirements")]
        public List<string> prerequisiteQuestIds = new List<string>();
    }

    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Weekly,
        Event
    }

    public enum QuestObjective
    {
        WinBattles,
        CollectGold,
        BuyUnits,
        SellUnits,
        EvolveUnit,
        ReachRound,
        UseSkills,
        DefeatEnemies,
        CompleteWithHealth,
        Custom
    }
}