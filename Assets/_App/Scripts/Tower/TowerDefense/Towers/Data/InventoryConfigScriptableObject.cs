using System.Collections.Generic;
using UnityEngine;
using Services.Data;

namespace TowerDefense.Towers.Data
{
    /// <summary>
    /// ScriptableObject chứa inventory configuration
    /// Sync từ Firestore, chứa unlock requirements, costs, và metadata cho towers
    /// Note: Sprite được lấy trực tiếp từ Tower.levels[0].levelData.icon, không cần mapping ở đây
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig.asset", menuName = "TowerDefense/Inventory Config", order = 3)]
    public class InventoryConfigScriptableObject : ScriptableObject
    {
        [Header("Configuration Data")]
        [Tooltip("List of inventory configurations from Firestore")]
        public List<InventoryConfigData> configs = new List<InventoryConfigData>();

        /// <summary>
        /// Get config by tower name
        /// </summary>
        public InventoryConfigData GetConfig(string towerName)
        {
            return configs?.Find(c => c != null && c.towerName == towerName);
        }

        /// <summary>
        /// Sync configs from Firestore data
        /// </summary>
        public void SyncConfigs(List<InventoryConfigData> firestoreConfigs)
        {
            if (firestoreConfigs == null)
            {
                configs = new List<InventoryConfigData>();
                return;
            }

            configs = new List<InventoryConfigData>(firestoreConfigs);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}

