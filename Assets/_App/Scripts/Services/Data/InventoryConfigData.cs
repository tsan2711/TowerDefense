using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for inventory item configuration
    /// Contains unlock requirements, costs, and other metadata for towers
    /// Synced from backend/Firestore
    /// </summary>
    [Serializable]
    public class InventoryConfigData
    {
        /// <summary>
        /// Unique identifier for the tower (matches Tower.towerName)
        /// </summary>
        public string towerName;

        /// <summary>
        /// Tower type (matches Tower.mainTower enum value)
        /// </summary>
        public int towerType;

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string displayName;

        /// <summary>
        /// Description of the tower
        /// </summary>
        public string description;

        /// <summary>
        /// Currency cost to unlock this tower
        /// </summary>
        public int unlockCost;

        /// <summary>
        /// Minimum user level required to unlock
        /// </summary>
        public int requiredLevel;

        /// <summary>
        /// Required level IDs that must be completed before unlocking
        /// </summary>
        public List<string> requiredLevels;

        /// <summary>
        /// Is this tower unlocked by default for new users?
        /// </summary>
        public bool isDefaultUnlocked;

        /// <summary>
        /// Can this tower be purchased with currency?
        /// </summary>
        public bool isPurchasable;

        /// <summary>
        /// Rarity tier (0=Common, 1=Rare, 2=Epic, 3=Legendary)
        /// </summary>
        public int rarity;

        /// <summary>
        /// Sort order for display in UI
        /// </summary>
        public int sortOrder;

        /// <summary>
        /// Is this tower currently available?
        /// </summary>
        public bool isActive;

        /// <summary>
        /// Tags for filtering/categorization
        /// </summary>
        public List<string> tags;

        public InventoryConfigData()
        {
            requiredLevels = new List<string>();
            tags = new List<string>();
            isActive = true;
        }

        public InventoryConfigData(string towerName, int towerType)
        {
            this.towerName = towerName;
            this.towerType = towerType;
            this.requiredLevels = new List<string>();
            this.tags = new List<string>();
            this.isActive = true;
            this.sortOrder = 0;
            this.rarity = 0;
        }

        /// <summary>
        /// Check if tower can be unlocked based on requirements
        /// </summary>
        public bool CanUnlock(int userLevel, int userCurrency, HashSet<string> completedLevels)
        {
            if (!isActive || !isPurchasable)
            {
                return false;
            }

            // Check level requirement
            if (userLevel < requiredLevel)
            {
                return false;
            }

            // Check currency
            if (userCurrency < unlockCost)
            {
                return false;
            }

            // Check required levels
            if (requiredLevels != null && requiredLevels.Count > 0)
            {
                foreach (var levelId in requiredLevels)
                {
                    if (completedLevels == null || !completedLevels.Contains(levelId))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get unlock status message
        /// </summary>
        public string GetUnlockStatusMessage(int userLevel, int userCurrency, HashSet<string> completedLevels)
        {
            if (!isActive)
            {
                return "Tower not available";
            }

            if (!isPurchasable)
            {
                return "Cannot be purchased";
            }

            if (isDefaultUnlocked)
            {
                return "Default tower";
            }

            if (userLevel < requiredLevel)
            {
                return $"Requires level {requiredLevel}";
            }

            if (userCurrency < unlockCost)
            {
                return $"Insufficient currency (need {unlockCost})";
            }

            if (requiredLevels != null && requiredLevels.Count > 0)
            {
                foreach (var levelId in requiredLevels)
                {
                    if (completedLevels == null || !completedLevels.Contains(levelId))
                    {
                        return $"Complete {levelId} first";
                    }
                }
            }

            return "Available to unlock";
        }
    }
}


