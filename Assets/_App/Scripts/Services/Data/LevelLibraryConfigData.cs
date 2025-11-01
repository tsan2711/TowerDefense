using System;
using System.Collections.Generic;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for LevelLibraryConfig from Firebase Firestore
    /// Used to sync LevelLibraryConfig data from backend
    /// Config prefab mapping cho từng level library
    /// </summary>
    [Serializable]
    public class LevelLibraryConfigData
    {
        /// <summary>
        /// Level library type as integer (matches LevelLibraryType enum)
        /// </summary>
        public int type;

        /// <summary>
        /// Level ID từ LevelList (ví dụ: "level_1", "level_2")
        /// </summary>
        public string levelId;

        /// <summary>
        /// Tên của TowerLibrary prefab trong Resources hoặc đường dẫn prefab
        /// Ví dụ: "Level_1_TowerLibrary" hoặc "Assets/_App/Data/TowerLibrary/Level_1_TowerLibrary"
        /// </summary>
        public string towerLibraryPrefabName;

        /// <summary>
        /// List các Tower prefab types (TowerPrefabType enum as int = MainTower enum) cho level library này
        /// TowerPrefabType map với MainTower enum: Emp, Laser, MachineGun, Pylon, Rocket, SuperTower
        /// Towers sẽ được load từ Resources/Tower với tên prefab tương ứng với enum value (ví dụ: Resources/Tower/Emp)
        /// </summary>
        public List<int> towerPrefabTypes;

        /// <summary>
        /// Mô tả cho level library này
        /// </summary>
        public string description;

        /// <summary>
        /// Convert to LevelLibraryType enum
        /// </summary>
        public LevelLibraryType GetLevelLibraryType()
        {
            return (LevelLibraryType)type;
        }

        /// <summary>
        /// Convert list of tower prefab types to TowerPrefabType enum list
        /// </summary>
        public List<TowerPrefabType> GetTowerPrefabTypes()
        {
            List<TowerPrefabType> types = new List<TowerPrefabType>();
            if (towerPrefabTypes != null)
            {
                foreach (int typeValue in towerPrefabTypes)
                {
                    if (System.Enum.IsDefined(typeof(TowerPrefabType), typeValue))
                    {
                        types.Add((TowerPrefabType)typeValue);
                    }
                }
            }
            return types;
        }
    }
}
