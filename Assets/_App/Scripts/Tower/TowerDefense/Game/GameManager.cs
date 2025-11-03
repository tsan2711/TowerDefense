using Core.Data;
using Core.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using Services.Core;
using Services.Data;
using Services.Managers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TowerDefense.Game
{
	/// <summary>
	/// Game Manager - a persistent single that handles persistence, and level lists, etc.
	/// This should be initialized when the game starts.
	/// </summary>
	public class GameManager : GameManagerBase<GameManager, GameDataStore>
	{
		/// <summary>
		/// Scriptable object for list of levels
		/// </summary>
		public LevelList levelList;

		/// <summary>
		/// Set sleep timeout to never sleep
		/// </summary>
		protected override void Awake()
		{
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
			base.Awake();
		}

		/// <summary>
		/// Called after data is loaded, sync maxLevel with completed levels
		/// Note: If OnDataLoaded doesn't exist in base class, Start() will handle the sync
		/// </summary>
		protected virtual void OnDataLoaded()
		{
			SyncMaxLevel();
		}

		/// <summary>
		/// Fallback to sync maxLevel in case OnDataLoaded is not called
		/// </summary>
		protected virtual void Start()
		{
			// Sync maxLevel after data should be loaded
			if (m_DataStore != null && levelList != null)
			{
				SyncMaxLevel();
			}
			
			// Load level progress from database
			LoadLevelProgressFromDB();
			
			// Load agent configurations from database
			LoadAgentConfigurationsFromDB();
		}

		/// <summary>
		/// Syncs maxLevel with the highest completed level index
		/// </summary>
		protected void SyncMaxLevel()
		{
			if (levelList == null || m_DataStore == null)
			{
				return;
			}

			int highestCompletedIndex = -1;
			
			// Find the highest index of completed levels
			foreach (LevelSaveData levelData in m_DataStore.completedLevels)
			{
				int levelIndex = GetLevelIndex(levelData.id);
				if (levelIndex >= 0)
				{
					highestCompletedIndex = Mathf.Max(highestCompletedIndex, levelIndex);
				}
			}

			// Update maxLevel: if highest completed is index N, maxLevel should be N+1
			if (highestCompletedIndex >= 0)
			{
				m_DataStore.maxLevel = Mathf.Max(m_DataStore.maxLevel, highestCompletedIndex + 1);
			}
		}

		/// <summary>
		/// Method used for completing the level
		/// </summary>
		/// <param name="levelId">The levelId to mark as complete</param>
		/// <param name="starsEarned"></param>
		public void CompleteLevel(string levelId, int starsEarned)
		{
			if (!levelList.ContainsKey(levelId))
			{
				Debug.LogWarningFormat("[GAME] Cannot complete level with id = {0}. Not in level list", levelId);
				return;
			}

			m_DataStore.CompleteLevel(levelId, starsEarned);
			
			// Update maxLevel when level is completed
			int levelIndex = GetLevelIndex(levelId);
			if (levelIndex >= 0)
			{
				m_DataStore.UpdateMaxLevel(levelIndex);
			}
			
			// Save to local storage
			SaveData();
			
			// Save to database (Firestore)
			SaveLevelProgressToDB(levelId, starsEarned);
		}

		/// <summary>
		/// Save level progress to Firestore database
		/// </summary>
		protected async void SaveLevelProgressToDB(string levelId, int starsEarned)
		{
			try
			{
				var serviceLocator = ServiceLocator.Instance;
				var authService = serviceLocator.GetService<IAuthService>();
				if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
				{
					Debug.LogWarning("[GAME] User not authenticated, cannot save level progress to DB");
					return;
				}

				string uid = authService.CurrentUser.UID;
				int maxLevel = m_DataStore.maxLevel;

				var userDataService = serviceLocator.GetService<IUserDataService>();
				if (userDataService != null)
				{
					bool success = await userDataService.SaveLevelProgressAsync(uid, levelId, starsEarned, maxLevel);
					if (success)
					{
						Debug.Log($"[GAME] Successfully saved level progress to DB: level {levelId}, stars {starsEarned}, maxLevel {maxLevel}");
					}
					else
					{
						Debug.LogWarning($"[GAME] Failed to save level progress to DB");
					}
				}
				else
				{
					Debug.LogWarning("[GAME] UserDataService not available");
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[GAME] Error saving level progress to DB: {e.Message}");
			}
		}

		/// <summary>
		/// Gets the index of a level in the level list
		/// </summary>
		/// <param name="levelId">The level ID to find</param>
		/// <returns>The index of the level, or -1 if not found</returns>
		protected int GetLevelIndex(string levelId)
		{
			if (levelList == null)
			{
				return -1;
			}

			int levelCount = levelList.Count;
			for (int i = 0; i < levelCount; i++)
			{
				LevelItem item = levelList[i];
				if (item != null && item.id == levelId)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Gets the id for the current level
		/// </summary>
		public LevelItem GetLevelForCurrentScene()
		{
			string sceneName = SceneManager.GetActiveScene().name;

			return levelList.GetLevelByScene(sceneName);
		}

		/// <summary>
		/// Determines if a specific level is completed
		/// </summary>
		/// <param name="levelId">The level ID to check</param>
		/// <returns>true if the level is completed</returns>
		public bool IsLevelCompleted(string levelId)
		{
			if (!levelList.ContainsKey(levelId))
			{
				Debug.LogWarningFormat("[GAME] Cannot check if level with id = {0} is completed. Not in level list", levelId);
				return false;
			}

			return m_DataStore.IsLevelCompleted(levelId);
		}

		/// <summary>
		/// Gets the stars earned on a given level
		/// </summary>
		/// <param name="levelId"></param>
		/// <returns></returns>
		public int GetStarsForLevel(string levelId)
		{
			if (!levelList.ContainsKey(levelId))
			{
				Debug.LogWarningFormat("[GAME] Cannot check if level with id = {0} is completed. Not in level list", levelId);
				return 0;
			}

			return m_DataStore.GetNumberOfStarForLevel(levelId);
		}

		/// <summary>
		/// Gets the maximum level unlocked by the player
		/// </summary>
		/// <returns>The maximum level index (0-based)</returns>
		public int GetMaxLevel()
		{
			return m_DataStore.maxLevel;
		}

		/// <summary>
		/// Public method to trigger loading level progress from database
		/// Can be called when user logs in after GameManager is already initialized
		/// </summary>
		public void RefreshLevelProgressFromDB()
		{
			LoadLevelProgressFromDB();
		}

		/// <summary>
		/// Public method to trigger loading agent configurations from database
		/// Can be called when user logs in after GameManager is already initialized
		/// </summary>
		public void RefreshAgentConfigurationsFromDB()
		{
			LoadAgentConfigurationsFromDB();
		}

		/// <summary>
		/// Load level progress from Firestore database
		/// </summary>
		protected async void LoadLevelProgressFromDB()
		{
			try
			{
				var serviceLocator = ServiceLocator.Instance;
				var authService = serviceLocator.GetService<IAuthService>();
				if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
				{
					Debug.Log("[GAME] User not authenticated, skipping load level progress from DB");
					return;
				}

				string uid = authService.CurrentUser.UID;

				var userDataService = serviceLocator.GetService<IUserDataService>();
				if (userDataService != null)
				{
					var (levelStars, maxLevel) = await userDataService.LoadLevelProgressAsync(uid);
					
					// Merge DB data with local data (DB takes priority)
					if (levelStars != null && levelStars.Count > 0)
					{
						foreach (var kvp in levelStars)
						{
							string levelId = kvp.Key;
							int stars = kvp.Value;
							
							// Update local data store
							m_DataStore.CompleteLevel(levelId, stars);
							Debug.Log($"[GAME] Loaded from DB: level {levelId} = {stars} stars");
						}
					}
					
					// Update maxLevel from DB if it's higher
					if (maxLevel > 0)
					{
						m_DataStore.maxLevel = Mathf.Max(m_DataStore.maxLevel, maxLevel);
						Debug.Log($"[GAME] Loaded maxLevel from DB: {maxLevel}");
					}
					
					// Sync maxLevel with completed levels
					SyncMaxLevel();
					
					// Save merged data back to local storage
					SaveData();
					
					// Update UserInfo.LevelProgress with merged data
					if (authService.CurrentUser != null)
					{
						var userLevelProgress = authService.CurrentUser.LevelProgress;
						if (userLevelProgress == null)
						{
							userLevelProgress = new UserLevelProgress();
							authService.CurrentUser.LevelProgress = userLevelProgress;
						}
						
						// Update from merged data
						userLevelProgress.LevelStars = levelStars;
						userLevelProgress.MaxLevel = m_DataStore.maxLevel;
					}
					
					Debug.Log($"[GAME] Successfully loaded level progress from DB: {levelStars.Count} levels, maxLevel {maxLevel}");
				}
				else
				{
					Debug.LogWarning("[GAME] UserDataService not available");
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[GAME] Error loading level progress from DB: {e.Message}");
			}
		}

		/// <summary>
		/// Load agent configurations from Firestore database and sync to Resources
		/// </summary>
		protected async void LoadAgentConfigurationsFromDB()
		{
			try
			{
				var serviceLocator = ServiceLocator.Instance;
				var authService = serviceLocator.GetService<IAuthService>();
				if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
				{
					Debug.Log("[GAME] User not authenticated, skipping load agent configurations from DB");
					return;
				}

				var agentConfigService = serviceLocator.GetService<IAgentConfigurationService>();
				if (agentConfigService != null)
				{
					var agentConfigurations = await agentConfigService.LoadAgentConfigurationsAsync();
					
					// Update UserInfo.AgentConfigurations with loaded data
					if (authService.CurrentUser != null)
					{
						if (agentConfigurations != null && agentConfigurations.Count > 0)
						{
							authService.CurrentUser.AgentConfigurations = agentConfigurations;
							Debug.Log($"[GAME] Successfully loaded agent configurations from DB: {agentConfigurations.Count} agents");
							
							// Sync AgentConfiguration data from Firestore to Resources ScriptableObjects
							int syncedCount = Services.Firestore.AgentConfigurationResourceSync.SyncToResources(agentConfigurations);
							Debug.Log($"[GAME] Synced {syncedCount} AgentConfiguration ScriptableObjects to Resources");
						}
						else
						{
							Debug.LogWarning("[GAME] No agent configurations loaded from DB");
						}
					}
				}
				else
				{
					Debug.LogWarning("[GAME] AgentConfigurationService not available");
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[GAME] Error loading agent configurations from DB: {e.Message}");
			}
		}
	}
}