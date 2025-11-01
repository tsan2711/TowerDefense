using System;
using UnityEngine;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for TowerLevelData from Firebase Firestore
    /// Used to sync TowerLevelData data from backend
    /// </summary>
    [Serializable]
    public class TowerLevelDataData
    {
        /// <summary>
        /// Tower type as integer (matches backend enum)
        /// </summary>
        public int type;

        /// <summary>
        /// A description of the tower for displaying on the UI
        /// </summary>
        public string description;

        /// <summary>
        /// A description of the tower upgrade for displaying on the UI
        /// </summary>
        public string upgradeDescription;

        /// <summary>
        /// The cost to upgrade to this level
        /// </summary>
        public int cost;

        /// <summary>
        /// The sell cost of the tower
        /// </summary>
        public int sell;

        /// <summary>
        /// The max health
        /// </summary>
        public int maxHealth;

        /// <summary>
        /// The starting health
        /// </summary>
        public int startingHealth;

        /// <summary>
        /// Convert to TowerType enum
        /// </summary>
        public TowerDefense.Towers.Data.TowerType GetTowerType()
        {
            return (TowerDefense.Towers.Data.TowerType)type;
        }
    }
}

