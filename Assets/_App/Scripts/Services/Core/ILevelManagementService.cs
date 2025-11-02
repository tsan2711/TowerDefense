using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;

namespace Services.Core
{
    /// <summary>
    /// Service interface for managing Level List and Level Library Config
    /// Microservice: Handles only Level Management domain
    /// </summary>
    public interface ILevelManagementService : IService
    {
        /// <summary>
        /// Event fired when LevelList is loaded
        /// </summary>
        event Action<LevelListData> OnLevelListLoaded;

        /// <summary>
        /// Event fired when LevelLibraryConfigs are loaded
        /// </summary>
        event Action<List<LevelLibraryConfigData>> OnLevelLibraryConfigsLoaded;

        /// <summary>
        /// Load LevelList from Firestore
        /// </summary>
        Task<LevelListData> LoadLevelListAsync();

        /// <summary>
        /// Load all LevelLibraryConfig from Firestore
        /// </summary>
        Task<List<LevelLibraryConfigData>> LoadLevelLibraryConfigsAsync();

        /// <summary>
        /// Load LevelLibraryConfig filtered by levelId
        /// </summary>
        Task<LevelLibraryConfigData> LoadLevelLibraryConfigByLevelIdAsync(string levelId);

        /// <summary>
        /// Load LevelLibraryConfig filtered by type
        /// </summary>
        Task<LevelLibraryConfigData> LoadLevelLibraryConfigByTypeAsync(int type);

        /// <summary>
        /// Get cached LevelList
        /// </summary>
        LevelListData GetCachedLevelList();

        /// <summary>
        /// Get cached LevelLibraryConfigs
        /// </summary>
        List<LevelLibraryConfigData> GetCachedLevelLibraryConfigs();

        /// <summary>
        /// Initialize LevelList and LevelLibraryConfig collections with default data if empty
        /// </summary>
        Task<bool> InitializeCollectionsIfEmptyAsync();
    }
}

