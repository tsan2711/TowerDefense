using System;
using System.Collections.Generic;
using Services.Data;
using TowerDefense.Agents.Data;
using TowerDefense.Towers.Data;

namespace Services.Firestore
{
    /// <summary>
    /// Default game data values for initialization
    /// Contains balanced default values for all enum types (AgentType, TowerType)
    /// </summary>
    public static class DefaultGameData
    {
        /// <summary>
        /// Get default AgentConfiguration data for all AgentType enum values
        /// </summary>
        public static List<AgentConfigurationData> GetDefaultAgentConfigurations()
        {
            var configs = new List<AgentConfigurationData>();
            var enumValues = Enum.GetValues(typeof(AgentType));
            
            foreach (AgentType agentType in enumValues)
            {
                var config = new AgentConfigurationData
                {
                    type = (int)agentType,
                    agentName = GetAgentName(agentType),
                    agentDescription = GetAgentDescription(agentType)
                };
                configs.Add(config);
            }
            
            return configs;
        }

        /// <summary>
        /// Get default TowerLevelData for all TowerType enum values
        /// Values are balanced for game progression
        /// </summary>
        public static List<TowerLevelDataData> GetDefaultTowerLevelData()
        {
            var towerData = new List<TowerLevelDataData>();
            var enumValues = Enum.GetValues(typeof(TowerType));
            
            foreach (TowerType towerType in enumValues)
            {
                var data = new TowerLevelDataData
                {
                    type = (int)towerType,
                    description = GetTowerDescription(towerType),
                    upgradeDescription = GetTowerUpgradeDescription(towerType),
                    cost = GetTowerCost(towerType),
                    sell = GetTowerSellValue(towerType),
                    maxHealth = GetTowerMaxHealth(towerType),
                    startingHealth = GetTowerStartingHealth(towerType)
                };
                towerData.Add(data);
            }
            
            return towerData;
        }

        /// <summary>
        /// Get default LevelList with balanced initial levels
        /// </summary>
        public static LevelListData GetDefaultLevelList()
        {
            var levelList = new LevelListData();
            
            // Create default levels - balanced progression
            levelList.levels.Add(new LevelItemData
            {
                id = "level_1",
                name = "Tutorial",
                description = "Learn the basics of tower defense",
                sceneName = "Tutorial"
            });
            
            levelList.levels.Add(new LevelItemData
            {
                id = "level_2",
                name = "First Challenge",
                description = "Put your skills to the test",
                sceneName = "Level1"
            });
            
            levelList.levels.Add(new LevelItemData
            {
                id = "level_3",
                name = "Advanced Tactics",
                description = "Master the art of defense",
                sceneName = "Level2"
            });
            
            return levelList;
        }

        private static string GetAgentName(AgentType agentType)
        {
            switch (agentType)
            {
                case AgentType.HoverBoss:
                    return "Hover Boss";
                case AgentType.Hoverbuggy:
                    return "Hoverbuggy";
                case AgentType.Hovercopter:
                    return "Hovercopter";
                case AgentType.Hovertank:
                    return "Hovertank";
                case AgentType.SuperHoverboss:
                    return "Super Hover Boss";
                case AgentType.SuperHoverbuggy:
                    return "Super Hoverbuggy";
                case AgentType.SuperHovercopter:
                    return "Super Hovercopter";
                case AgentType.SuperHovertank:
                    return "Super Hovertank";
                default:
                    return agentType.ToString();
            }
        }

        private static string GetAgentDescription(AgentType agentType)
        {
            switch (agentType)
            {
                case AgentType.HoverBoss:
                    return "A powerful boss enemy with high health and damage";
                case AgentType.Hoverbuggy:
                    return "Fast but weak enemy unit";
                case AgentType.Hovercopter:
                    return "Flying unit with moderate speed";
                case AgentType.Hovertank:
                    return "Heavy armored unit with slow speed but high health";
                case AgentType.SuperHoverboss:
                    return "Elite boss with exceptional strength";
                case AgentType.SuperHoverbuggy:
                    return "Enhanced buggy with increased speed";
                case AgentType.SuperHovercopter:
                    return "Advanced copter with better capabilities";
                case AgentType.SuperHovertank:
                    return "Upgraded tank with superior armor";
                default:
                    return $"Default description for {agentType}";
            }
        }

        private static string GetTowerDescription(TowerType towerType)
        {
            string baseType = GetTowerBaseType(towerType);
            int level = GetTowerLevel(towerType);
            string levelStr = level > 0 ? $" Level {level}" : "";
            
            return $"{baseType} Tower{levelStr} - Balanced defensive structure";
        }

        private static string GetTowerUpgradeDescription(TowerType towerType)
        {
            string baseType = GetTowerBaseType(towerType);
            int level = GetTowerLevel(towerType);
            
            if (level >= 3)
            {
                return $"Maximum level reached for {baseType}";
            }
            
            return $"Upgrade to {baseType} Level {level + 1} for increased power";
        }

        private static string GetTowerBaseType(TowerType towerType)
        {
            if (towerType.ToString().StartsWith("Emp"))
                return "EMP";
            if (towerType.ToString().StartsWith("Laser"))
                return "Laser";
            if (towerType.ToString().StartsWith("MachineGun"))
                return "Machine Gun";
            if (towerType.ToString().StartsWith("Pylon"))
                return "Pylon";
            if (towerType.ToString().StartsWith("Rocket"))
                return "Rocket";
            if (towerType == TowerType.SuperTower)
                return "Super";
            
            return towerType.ToString();
        }

        private static int GetTowerLevel(TowerType towerType)
        {
            string name = towerType.ToString();
            if (name.EndsWith("1")) return 1;
            if (name.EndsWith("2")) return 2;
            if (name.EndsWith("3")) return 3;
            if (towerType == TowerType.SuperTower) return 4;
            return 0;
        }

        // Balanced cost progression: Level 1 towers cheaper, Level 3 more expensive, Super most expensive
        private static int GetTowerCost(TowerType towerType)
        {
            int baseCost = 50;
            int level = GetTowerLevel(towerType);
            
            // Cost increases with level
            int costMultiplier = level > 0 ? level * 30 : 50;
            
            // SuperTower is special
            if (towerType == TowerType.SuperTower)
                return 500;
            
            // Different tower types have different base costs
            if (towerType.ToString().StartsWith("Emp"))
                baseCost = 40;
            else if (towerType.ToString().StartsWith("Laser"))
                baseCost = 60;
            else if (towerType.ToString().StartsWith("MachineGun"))
                baseCost = 50;
            else if (towerType.ToString().StartsWith("Pylon"))
                baseCost = 70;
            else if (towerType.ToString().StartsWith("Rocket"))
                baseCost = 80;
            
            return baseCost + costMultiplier;
        }

        // Sell value is typically 50-70% of cost
        private static int GetTowerSellValue(TowerType towerType)
        {
            int cost = GetTowerCost(towerType);
            return (int)(cost * 0.6f); // 60% sell back value
        }

        // Health increases with level
        private static int GetTowerMaxHealth(TowerType towerType)
        {
            int baseHealth = 100;
            int level = GetTowerLevel(towerType);
            
            if (towerType == TowerType.SuperTower)
                return 500;
            
            return baseHealth + (level * 50);
        }

        private static int GetTowerStartingHealth(TowerType towerType)
        {
            return GetTowerMaxHealth(towerType); // Towers start at full health
        }

        /// <summary>
        /// Check if an enum value exists in the enum type
        /// Used to validate data from backend
        /// </summary>
        public static bool IsValidAgentType(int typeValue)
        {
            return Enum.IsDefined(typeof(AgentType), typeValue);
        }

        /// <summary>
        /// Check if an enum value exists in the enum type
        /// Used to validate data from backend
        /// </summary>
        public static bool IsValidTowerType(int typeValue)
        {
            return Enum.IsDefined(typeof(TowerType), typeValue);
        }

        /// <summary>
        /// Get default LevelLibraryConfig data for all LevelLibraryType enum values
        /// Maps level IDs from LevelList to TowerLibrary prefab names
        /// </summary>
        public static List<LevelLibraryConfigData> GetDefaultLevelLibraryConfigs()
        {
            var configs = new List<LevelLibraryConfigData>();
            var enumValues = Enum.GetValues(typeof(LevelLibraryType));
            
            foreach (LevelLibraryType libraryType in enumValues)
            {
                var config = new LevelLibraryConfigData
                {
                    type = (int)libraryType,
                    levelId = GetLevelIdForLibraryType(libraryType),
                    towerLibraryPrefabName = GetTowerLibraryPrefabName(libraryType),
                    towerPrefabTypes = GetDefaultTowerPrefabTypesForLibrary(libraryType),
                    description = GetLevelLibraryDescription(libraryType)
                };
                configs.Add(config);
            }
            
            return configs;
        }

        private static string GetLevelIdForLibraryType(LevelLibraryType libraryType)
        {
            switch (libraryType)
            {
                case LevelLibraryType.Level_1:
                    return "level_1";
                case LevelLibraryType.Level_2:
                    return "level_2";
                case LevelLibraryType.Level_3:
                    return "level_3";
                case LevelLibraryType.Level_4:
                    return "level_4";
                case LevelLibraryType.Level_5:
                    return "level_5";
                case LevelLibraryType.Tutorial:
                    return "tutorial";
                default:
                    return libraryType.ToString().ToLower().Replace("_", "");
            }
        }

        private static string GetTowerLibraryPrefabName(LevelLibraryType libraryType)
        {
            // Map to Resources path or prefab name
            // Assumes prefabs are in Resources folder or use full path
            switch (libraryType)
            {
                case LevelLibraryType.Level_1:
                    return "Level_1_TowerLibrary";
                case LevelLibraryType.Level_2:
                    return "Level_2_TowerLibrary";
                case LevelLibraryType.Level_3:
                    return "Level_3_TowerLibrary";
                case LevelLibraryType.Level_4:
                    return "Level_4_TowerLibrary";
                case LevelLibraryType.Level_5:
                    return "Level_5_TowerLibrary";
                case LevelLibraryType.Tutorial:
                    return "Tutorial_TowerLibrary";
                default:
                    return $"{libraryType}_TowerLibrary";
            }
        }

        private static string GetLevelLibraryDescription(LevelLibraryType libraryType)
        {
            switch (libraryType)
            {
                case LevelLibraryType.Level_1:
                    return "Tower library configuration for Level 1";
                case LevelLibraryType.Level_2:
                    return "Tower library configuration for Level 2";
                case LevelLibraryType.Level_3:
                    return "Tower library configuration for Level 3";
                case LevelLibraryType.Level_4:
                    return "Tower library configuration for Level 4";
                case LevelLibraryType.Level_5:
                    return "Tower library configuration for Level 5";
                case LevelLibraryType.Tutorial:
                    return "Tower library configuration for Tutorial level";
                default:
                    return $"Tower library configuration for {libraryType}";
            }
        }

        /// <summary>
        /// Get default tower prefab types list for a specific level library
        /// Returns list of TowerPrefabType enum values as integers (MainTower)
        /// </summary>
        private static List<int> GetDefaultTowerPrefabTypesForLibrary(LevelLibraryType libraryType)
        {
            var towerTypes = new List<int>();
            
            switch (libraryType)
            {
                case LevelLibraryType.Tutorial:
                    // Tutorial: Basic towers only
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    break;
                    
                case LevelLibraryType.Level_1:
                    // Level 1: Basic towers
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.Laser);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    break;
                    
                case LevelLibraryType.Level_2:
                    // Level 2: More towers
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.Laser);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    towerTypes.Add((int)TowerPrefabType.Pylon);
                    break;
                    
                case LevelLibraryType.Level_3:
                    // Level 3: Most towers
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.Laser);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    towerTypes.Add((int)TowerPrefabType.Pylon);
                    towerTypes.Add((int)TowerPrefabType.Rocket);
                    break;
                    
                case LevelLibraryType.Level_4:
                    // Level 4: Almost all towers
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.Laser);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    towerTypes.Add((int)TowerPrefabType.Pylon);
                    towerTypes.Add((int)TowerPrefabType.Rocket);
                    break;
                    
                case LevelLibraryType.Level_5:
                    // Level 5: All towers available
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.Laser);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    towerTypes.Add((int)TowerPrefabType.Pylon);
                    towerTypes.Add((int)TowerPrefabType.Rocket);
                    towerTypes.Add((int)TowerPrefabType.SuperTower);
                    break;
                    
                default:
                    // Default: Basic towers
                    towerTypes.Add((int)TowerPrefabType.Emp);
                    towerTypes.Add((int)TowerPrefabType.MachineGun);
                    break;
            }
            
            return towerTypes;
        }

        /// <summary>
        /// Check if an enum value exists in the enum type
        /// Used to validate data from backend
        /// </summary>
        public static bool IsValidLevelLibraryType(int typeValue)
        {
            return Enum.IsDefined(typeof(LevelLibraryType), typeValue);
        }

        /// <summary>
        /// Check if an enum value exists in TowerPrefabType enum
        /// Used to validate data from backend
        /// </summary>
        public static bool IsValidTowerPrefabType(int typeValue)
        {
            return Enum.IsDefined(typeof(TowerPrefabType), typeValue);
        }
    }
}

