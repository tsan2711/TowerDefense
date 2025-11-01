using System;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for LevelItem from Firebase Firestore
    /// Used to sync LevelItem data from backend
    /// </summary>
    [Serializable]
    public class LevelItemData
    {
        /// <summary>
        /// The id - used in persistence
        /// </summary>
        public string id;

        /// <summary>
        /// The human readable level name
        /// </summary>
        public string name;

        /// <summary>
        /// The description of the level - flavour text
        /// </summary>
        public string description;

        /// <summary>
        /// The name of the scene to load
        /// </summary>
        public string sceneName;
    }
}

