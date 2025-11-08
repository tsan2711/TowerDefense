using System.Collections.Generic;
using UnityEngine;
using Services.Data;

namespace TowerDefense.Towers.Data
{
    /// <summary>
    /// ScriptableObject chứa inventory configuration với sprite mapping
    /// Sync từ Firestore và map iconName -> Sprite references
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig.asset", menuName = "TowerDefense/Inventory Config", order = 3)]
    public class InventoryConfigScriptableObject : ScriptableObject
    {
        [Header("Configuration Data")]
        [Tooltip("List of inventory configurations from Firestore")]
        public List<InventoryConfigData> configs = new List<InventoryConfigData>();

        [Header("Sprite Mapping")]
        [Tooltip("Mapping iconName to Sprite. Key = iconName from config, Value = Sprite asset")]
        public List<IconSpriteMapping> spriteMappings = new List<IconSpriteMapping>();

        /// <summary>
        /// Get config by tower name
        /// </summary>
        public InventoryConfigData GetConfig(string towerName)
        {
            return configs?.Find(c => c != null && c.towerName == towerName);
        }

        /// <summary>
        /// Get sprite by icon name
        /// </summary>
        public Sprite GetSprite(string iconName)
        {
            if (string.IsNullOrEmpty(iconName) || spriteMappings == null)
            {
                return null;
            }

            var mapping = spriteMappings.Find(m => m != null && m.iconName == iconName);
            return mapping?.sprite;
        }

        /// <summary>
        /// Get sprite for a tower by tower name
        /// </summary>
        public Sprite GetSpriteForTower(string towerName)
        {
            var config = GetConfig(towerName);
            if (config == null || string.IsNullOrEmpty(config.iconName))
            {
                return null;
            }

            return GetSprite(config.iconName);
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

        /// <summary>
        /// Add or update sprite mapping
        /// </summary>
        public void SetSpriteMapping(string iconName, Sprite sprite)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                return;
            }

            if (spriteMappings == null)
            {
                spriteMappings = new List<IconSpriteMapping>();
            }

            var existing = spriteMappings.Find(m => m != null && m.iconName == iconName);
            if (existing != null)
            {
                existing.sprite = sprite;
            }
            else
            {
                spriteMappings.Add(new IconSpriteMapping
                {
                    iconName = iconName,
                    sprite = sprite
                });
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Icon to Sprite mapping entry
        /// </summary>
        [System.Serializable]
        public class IconSpriteMapping
        {
            public string iconName;
            public Sprite sprite;
        }
    }
}

