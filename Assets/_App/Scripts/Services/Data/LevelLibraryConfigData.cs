using System;

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
    }
}
