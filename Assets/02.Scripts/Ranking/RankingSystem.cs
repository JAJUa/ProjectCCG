/*using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BackEnd;
using SpiritAge.Core;
using SpiritAge.UI;
using SpiritAge.Utility;

namespace SpiritAge.Ranking
{
    /// <summary>
    /// 랭킹 시스템
    /// </summary>
    public class RankingSystem : AbstractSingleton<RankingSystem>
    {
        [Header("Ranking Settings")]
        [SerializeField] private int maxLeaderboardEntries = 100;
        [SerializeField] private float updateInterval = 60f;
        [SerializeField] private RankTier[] rankTiers;

        // Player ranking data
        private PlayerRankData currentPlayerRank;
        private List<LeaderboardEntry> globalLeaderboard = new List<LeaderboardEntry>();
        private List<LeaderboardEntry> friendsLeaderboard = new List<LeaderboardEntry>();
        private List<LeaderboardEntry> localLeaderboard = new List<LeaderboardEntry>();

        // Season data
        private SeasonData currentSeason;
        private DateTime lastUpdateTime;

        // Events
        public event Action<PlayerRankData> OnRankUpdated;
        public event Action<RankTier> OnTierChanged;
        public event Action<List<LeaderboardEntry>> OnLeaderboardUpdated;
        public event Action<SeasonData> OnSeasonChanged;

        protected override void OnSingletonAwake()
        {
            InitializeRanking();
            InvokeRepeating(nameof(UpdateLeaderboards), updateInterval, updateInterval);
        }

        /// <summary>
        /// 랭킹 초기화
        /// </summary>
        private void InitializeRanking()
        {
            LoadPlayerRank();
            LoadCurrentSeason();
            UpdateLeaderboards();
        }

        /// <summary>
        /// 플레이어 랭크 로드
        /// </summary>
        private void LoadPlayerRank()
        {
            currentPlayerRank = new PlayerRankData
            {
                playerId = Backend.BMember.GetUserInfo().GetReturnValue()["nickname"].ToString(),
                playerName = PlayerPrefs.GetString("PlayerNickname", "Player"),
                rankPoints = PlayerPrefs.GetInt("RankPoints", 1000),
                currentTier = GetTierByPoints(PlayerPrefs.GetInt("RankPoints", 1000)),
                wins = PlayerPrefs.GetInt("RankWins", 0),
                losses = PlayerPrefs.GetInt("RankLosses", 0),
                winStreak = PlayerPrefs.GetInt("WinStreak", 0),
                bestWinStreak = PlayerPrefs.GetInt("BestWinStreak", 0)
            };
        }

        /// <summary>
        /// 현재 시즌 로드
        /// </summary>
        private void LoadCurrentSeason()
        {
            // Load from backend or create new season
            currentSeason = new SeasonData
            {
                seasonId = DateTime.Now.Year + "_S" + ((DateTime.Now.Month - 1) / 3 + 1),
                seasonName = $"시즌 {DateTime.Now.Year}-{(DateTime.Now.Month - 1) / 3 + 1}",
                startDate = new DateTime(DateTime.Now.Year, ((DateTime.Now.Month - 1) / 3) * 3 + 1, 1),
                endDate = new DateTime(DateTime.Now.Year, ((DateTime.Now.Month - 1) / 3 + 1) * 3, DateTime.DaysInMonth(DateTime.Now.Year, ((DateTime.Now.Month - 1) / 3 + 1) * 3))
            };
        }

        // ========== Match Results ==========

        /// <summary>
        /// 매치 결과 제출
        /// </summary>
        public void SubmitMatchResult(MatchResultData result)
        {
            int pointsChange = CalculatePointsChange(result);

            currentPlayerRank.rankPoints += pointsChange;
            currentPlayerRank.rankPoints = Mathf.Max(0, currentPlayerRank.rankPoints);

            if (result.isWin)
            {
                currentPlayerRank.wins++;
                currentPlayerRank.winStreak++;
                currentPlayerRank.bestWinStreak = Mathf.Max(currentPlayerRank.bestWinStreak, currentPlayerRank.winStreak);
            }
            else
            {
                currentPlayerRank.losses++;
                currentPlayerRank.winStreak = 0;
            }

            // Check tier change
            RankTier newTier = GetTierByPoints(currentPlayerRank.rankPoints);
            if (newTier != currentPlayerRank.currentTier)
            {
                RankTier oldTier = currentPlayerRank.currentTier;
                currentPlayerRank.currentTier = newTier;
                OnTierChanged?.Invoke(newTier);

                string message = newTier.tierLevel > oldTier.tierLevel ?
                    $"승급! {newTier.tierName}" :
                    $"강등... {newTier.tierName}";

                UIManager.Instance.ShowNotification(message,
                    newTier.tierLevel > oldTier.tierLevel ? NotificationType.Success : NotificationType.Warning);
            }

            SavePlayerRank();
            UpdateRankingToBackend();

            OnRankUpdated?.Invoke(currentPlayerRank);
        }

        /// <summary>
        /// 포인트 변화 계산
        /// </summary>
        private int CalculatePointsChange(MatchResultData result)
        {
            int basePoints = result.isWin ? 25 : -20;

            // Opponent rating difference
            int ratingDiff = result.opponentRating - currentPlayerRank.rankPoints;
            float kFactor = 32f; // ELO K-factor

            float expectedScore = 1f / (1f + Mathf.Pow(10f, ratingDiff / 400f));
            float actualScore = result.isWin ? 1f : 0f;

            int pointsChange = Mathf.RoundToInt(kFactor * (actualScore - expectedScore));

            // Win streak bonus
            if (result.isWin && currentPlayerRank.winStreak > 0)
            {
                pointsChange += currentPlayerRank.winStreak * 2;
            }

            // Round bonus
            pointsChange += result.roundReached;

            return pointsChange;
        }

        /// <summary>
        /// 티어 획득
        /// </summary>
        private RankTier GetTierByPoints(int points)
        {
            foreach (var tier in rankTiers.OrderByDescending(t => t.requiredPoints))
            {
                if (points >= tier.requiredPoints)
                {
                    return tier;
                }
            }

            return rankTiers[0]; // Lowest tier
        }

        // ========== Leaderboards ==========

        /// <summary>
        /// 리더보드 업데이트
        /// </summary>
        private void UpdateLeaderboards()
        {
            UpdateGlobalLeaderboard();
            UpdateFriendsLeaderboard();
            UpdateLocalLeaderboard();

            lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 글로벌 리더보드 업데이트
        /// </summary>
        private void UpdateGlobalLeaderboard()
        {
            Backend.URank.User.GetRankList("globalRanking", maxLeaderboardEntries, (callback) =>
            {
                if (callback.IsSuccess())
                {
                    globalLeaderboard.Clear();

                    var rows = callback.GetReturnValue()["rows"];
                    foreach (LitJson.JsonData row in rows)
                    {
                        var entry = new LeaderboardEntry
                        {
                            rank = int.Parse(row["rank"].ToString()),
                            playerId = row["nickname"].ToString(),
                            playerName = row["nickname"].ToString(),
                            score = int.Parse(row["score"].ToString()),
                            additionalData = row["extraData"]?.ToString()
                        };

                        globalLeaderboard.Add(entry);
                    }

                    OnLeaderboardUpdated?.Invoke(globalLeaderboard);
                }
            });
        }

        /// <summary>
        /// 친구 리더보드 업데이트
        /// </summary>
        private void UpdateFriendsLeaderboard()
        {
            // Implementation depends on friend system
        }

        /// <summary>
        /// 로컬 리더보드 업데이트
        /// </summary>
        private void UpdateLocalLeaderboard()
        {
            // Get nearby players in ranking
            Backend.URank.User.GetRankList("globalRanking", 10, currentPlayerRank.rankPoints, (callback) =>
            {
                if (callback.IsSuccess())
                {
                    localLeaderboard.Clear();
                    // Parse and add entries
                }
            });
        }

        /// <summary>
        /// 랭킹 백엔드 업데이트
        /// </summary>
        private void UpdateRankingToBackend()
        {
            var param = new Param();
            param.Add("score", currentPlayerRank.rankPoints);
            param.Add("extraData", JsonUtility.ToJson(new
            {
                wins = currentPlayerRank.wins,
                losses = currentPlayerRank.losses,
                tier = currentPlayerRank.currentTier.tierName
            }));

            Backend.URank.User.UpdateUserScore("globalRanking", "season_" + currentSeason.seasonId, param, (callback) =>
            {
                if (!callback.IsSuccess())
                {
                    Debug.LogError($"[RankingSystem] Failed to update ranking: {callback.GetMessage()}");
                }
            });
        }

        // ========== Season Management ==========

        /// <summary>
        /// 시즌 종료 체크
        /// </summary>
        private void CheckSeasonEnd()
        {
            if (DateTime.Now > currentSeason.endDate)
            {
                EndSeason();
            }
        }

        /// <summary>
        /// 시즌 종료
        /// </summary>
        private void EndSeason()
        {
            // Calculate season rewards
            var seasonRewards = CalculateSeasonRewards();

            // Reset ranks
            ResetRanksForNewSeason();

            // Start new season
            LoadCurrentSeason();

            OnSeasonChanged?.Invoke(currentSeason);

            // Give rewards
            if (seasonRewards != null)
            {
                GiveSeasonRewards(seasonRewards);
            }
        }

        /// <summary>
        /// 시즌 보상 계산
        /// </summary>
        private SeasonRewards CalculateSeasonRewards()
        {
            return new SeasonRewards
            {
                tier = currentPlayerRank.currentTier,
                finalRank = GetPlayerRank(),
                goldReward = currentPlayerRank.currentTier.tierLevel * 100,
                specialReward = currentPlayerRank.currentTier.tierLevel >= 5 ? "special_skin" : null
            };
        }

        /// <summary>
        /// 시즌 보상 지급
        /// </summary>
        private void GiveSeasonRewards(SeasonRewards rewards)
        {
            BackendGameManager.Instance.AddGold(rewards.goldReward);

            if (!string.IsNullOrEmpty(rewards.specialReward))
            {
                // Unlock special reward
                Debug.Log($"[RankingSystem] Special reward unlocked: {rewards.specialReward}");
            }

            UIManager.Instance.ShowNotification(
                $"시즌 종료! 최종 랭크: {rewards.finalRank}\n보상: {rewards.goldReward} 골드",
                NotificationType.Success);
        }

        /// <summary>
        /// 새 시즌을 위한 랭크 리셋
        /// </summary>
        private void ResetRanksForNewSeason()
        {
            // Soft reset based on previous tier
            int resetPoints = 1000 + (currentPlayerRank.currentTier.tierLevel - 1) * 100;
            currentPlayerRank.rankPoints = resetPoints;
            currentPlayerRank.wins = 0;
            currentPlayerRank.losses = 0;
            currentPlayerRank.winStreak = 0;

            SavePlayerRank();
        }

        // ========== Utilities ==========

        /// <summary>
        /// 플레이어 랭크 저장
        /// </summary>
        private void SavePlayerRank()
        {
            PlayerPrefs.SetInt("RankPoints", currentPlayerRank.rankPoints);
            PlayerPrefs.SetInt("RankWins", currentPlayerRank.wins);
            PlayerPrefs.SetInt("RankLosses", currentPlayerRank.losses);
            PlayerPrefs.SetInt("WinStreak", currentPlayerRank.winStreak);
            PlayerPrefs.SetInt("BestWinStreak", currentPlayerRank.bestWinStreak);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 플레이어 순위 가져오기
        /// </summary>
        public int GetPlayerRank()
        {
            var playerEntry = globalLeaderboard.Find(e => e.playerId == currentPlayerRank.playerId);
            return playerEntry?.rank ?? -1;
        }

        /// <summary>
        /// 승률 계산
        /// </summary>
        public float GetWinRate()
        {
            int totalGames = currentPlayerRank.wins + currentPlayerRank.losses;
            if (totalGames == 0) return 0f;

            return (float)currentPlayerRank.wins / totalGames;
        }

        // ========== Public Getters ==========

        public PlayerRankData GetPlayerRankData() => currentPlayerRank;
        public List<LeaderboardEntry> GetGlobalLeaderboard() => new List<LeaderboardEntry>(globalLeaderboard);
        public List<LeaderboardEntry> GetFriendsLeaderboard() => new List<LeaderboardEntry>(friendsLeaderboard);
        public SeasonData GetCurrentSeason() => currentSeason;
    }

    // ========== Data Structures ==========

    [Serializable]
    public class PlayerRankData
    {
        public string playerId;
        public string playerName;
        public int rankPoints;
        public RankTier currentTier;
        public int wins;
        public int losses;
        public int winStreak;
        public int bestWinStreak;
    }

    [Serializable]
    public class RankTier
    {
        public int tierLevel;
        public string tierName;
        public int requiredPoints;
        public Color tierColor;
        public Sprite tierIcon;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerId;
        public string playerName;
        public int score;
        public string additionalData;
    }

    [Serializable]
    public class SeasonData
    {
        public string seasonId;
        public string seasonName;
        public DateTime startDate;
        public DateTime endDate;
    }

    [Serializable]
    public class MatchResultData
    {
        public bool isWin;
        public int opponentRating;
        public int roundReached;
        public int damageDealt;
        public float matchDuration;
    }

    [Serializable]
    public class SeasonRewards
    {
        public RankTier tier;
        public int finalRank;
        public int goldReward;
        public string specialReward;
    }
}*/