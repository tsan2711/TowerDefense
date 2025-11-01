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
    }
}

