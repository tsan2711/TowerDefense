using System;
using System.Collections.Generic;

namespace Services.Data
{
    /// <summary>
    /// Model for user level progress data
    /// Contains level completion and stars information
    /// </summary>
    [Serializable]
    public class UserLevelProgress
    {
        /// <summary>
        /// Dictionary mapping levelId to number of stars earned
        /// Key: levelId (e.g., "level_1", "level_2")
        /// Value: number of stars (0-3)
        /// </summary>
        public Dictionary<string, int> LevelStars { get; set; }

        /// <summary>
        /// Maximum level unlocked by the player (0-based index)
        /// </summary>
        public int MaxLevel { get; set; }

        public UserLevelProgress()
        {
            LevelStars = new Dictionary<string, int>();
            MaxLevel = 0;
        }

        public UserLevelProgress(Dictionary<string, int> levelStars, int maxLevel)
        {
            LevelStars = levelStars ?? new Dictionary<string, int>();
            MaxLevel = maxLevel;
        }

        /// <summary>
        /// Get stars for a specific level
        /// </summary>
        public int GetStarsForLevel(string levelId)
        {
            if (LevelStars != null && LevelStars.ContainsKey(levelId))
            {
                return LevelStars[levelId];
            }
            return 0;
        }

        /// <summary>
        /// Check if a level is completed (has stars > 0)
        /// </summary>
        public bool IsLevelCompleted(string levelId)
        {
            return GetStarsForLevel(levelId) > 0;
        }

        /// <summary>
        /// Update level progress
        /// </summary>
        public void UpdateLevel(string levelId, int stars)
        {
            if (LevelStars == null)
            {
                LevelStars = new Dictionary<string, int>();
            }

            // Keep the maximum stars earned
            if (LevelStars.ContainsKey(levelId))
            {
                LevelStars[levelId] = Math.Max(LevelStars[levelId], stars);
            }
            else
            {
                LevelStars[levelId] = stars;
            }
        }
    }
}

