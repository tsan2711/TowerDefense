using System;
using UnityEngine;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for a single inventory item
    /// Represents a tower owned by the user
    /// </summary>
    [Serializable]
    public class InventoryItemData
    {
        /// <summary>
        /// Unique identifier for the tower (matches Tower.towerName)
        /// </summary>
        public string towerName;

        /// <summary>
        /// When this tower was unlocked (timestamp)
        /// </summary>
        public long unlockedAt;

        /// <summary>
        /// Whether this tower is currently selected for gameplay
        /// </summary>
        public bool isSelected;

        /// <summary>
        /// Tower type (matches Tower.mainTower enum value)
        /// </summary>
        public int towerType;

        /// <summary>
        /// Number of times this tower was used in gameplay
        /// </summary>
        public int usageCount;

        public InventoryItemData()
        {
        }

        public InventoryItemData(string towerName, int towerType, bool isSelected = false)
        {
            this.towerName = towerName;
            this.towerType = towerType;
            this.isSelected = isSelected;
            this.unlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.usageCount = 0;
        }
    }
}

