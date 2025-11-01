using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Towers.Data
{
    /// <summary>
    /// Container ScriptableObject để chứa tất cả các TowerLibrary
    /// Mỗi level library sẽ có một TowerLibrary riêng
    /// Sử dụng để iterate và quản lý các TowerLibrary từ một nơi duy nhất
    /// </summary>
    [CreateAssetMenu(fileName = "TowerLibraryContainer.asset", menuName = "TowerDefense/Tower Library Container", order = 1)]
    public class TowerLibraryContainer : ScriptableObject
    {
        /// <summary>
        /// Dictionary chứa các TowerLibrary, key là levelId (ví dụ: "level_1", "level_2")
        /// </summary>
        [System.Serializable]
        public class TowerLibraryEntry
        {
            public string levelId;
            public TowerLibrary towerLibrary;
        }

        /// <summary>
        /// List các TowerLibrary entries
        /// </summary>
        public List<TowerLibraryEntry> libraries;

        /// <summary>
        /// Internal dictionary để truy cập nhanh
        /// </summary>
        private Dictionary<string, TowerLibrary> m_LibraryDictionary;

        /// <summary>
        /// Initialize dictionary từ list
        /// </summary>
        public void OnEnable()
        {
            RefreshDictionary();
        }

        /// <summary>
        /// Refresh dictionary từ list
        /// </summary>
        public void RefreshDictionary()
        {
            if (libraries == null)
            {
                libraries = new List<TowerLibraryEntry>();
            }

            m_LibraryDictionary = new Dictionary<string, TowerLibrary>();
            foreach (var entry in libraries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.levelId) && entry.towerLibrary != null)
                {
                    m_LibraryDictionary[entry.levelId] = entry.towerLibrary;
                }
            }
        }

        /// <summary>
        /// Get TowerLibrary by levelId
        /// </summary>
        public TowerLibrary GetLibrary(string levelId)
        {
            if (m_LibraryDictionary == null)
            {
                RefreshDictionary();
            }

            if (m_LibraryDictionary != null && m_LibraryDictionary.ContainsKey(levelId))
            {
                return m_LibraryDictionary[levelId];
            }

            return null;
        }

        /// <summary>
        /// Set or add TowerLibrary for a levelId
        /// </summary>
        public void SetLibrary(string levelId, TowerLibrary library)
        {
            if (libraries == null)
            {
                libraries = new List<TowerLibraryEntry>();
            }

            // Check if entry exists
            var existingEntry = libraries.Find(e => e != null && e.levelId == levelId);
            if (existingEntry != null)
            {
                existingEntry.towerLibrary = library;
            }
            else
            {
                libraries.Add(new TowerLibraryEntry
                {
                    levelId = levelId,
                    towerLibrary = library
                });
            }

            RefreshDictionary();
        }

        /// <summary>
        /// Get all level IDs
        /// </summary>
        public List<string> GetAllLevelIds()
        {
            if (m_LibraryDictionary == null)
            {
                RefreshDictionary();
            }

            return new List<string>(m_LibraryDictionary.Keys);
        }

        /// <summary>
        /// Get all TowerLibraries
        /// </summary>
        public List<TowerLibrary> GetAllLibraries()
        {
            if (m_LibraryDictionary == null)
            {
                RefreshDictionary();
            }

            return new List<TowerLibrary>(m_LibraryDictionary.Values);
        }

        /// <summary>
        /// Clear all libraries
        /// </summary>
        public void Clear()
        {
            if (libraries != null)
            {
                libraries.Clear();
            }

            if (m_LibraryDictionary != null)
            {
                m_LibraryDictionary.Clear();
            }
        }
    }
}
