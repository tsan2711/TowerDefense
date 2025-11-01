using System;
using System.Collections.Generic;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for LevelList from Firebase Firestore
    /// Used to sync LevelList data from backend
    /// </summary>
    [Serializable]
    public class LevelListData
    {
        /// <summary>
        /// List of levels
        /// </summary>
        public List<LevelItemData> levels;

        public LevelListData()
        {
            levels = new List<LevelItemData>();
        }
    }
}

