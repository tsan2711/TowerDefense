using Core.Data;
using Core.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using Services.Core;
using Services.Data;
using Services.Managers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using TowerDefense.Towers;
using TowerDefense.UI;

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
			
			// Load user inventory from database
			LoadInventoryFromDB();
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
						
						// Unlock tower for completed level
						await UnlockTowerForLevel(uid, levelId);
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
		/// Refresh level select UI to update stars display
		/// Called after level progress is loaded from database
		/// </summary>
		protected void RefreshLevelSelectUI()
		{
			// Find LevelSelectScreen in scene and refresh stars
			LevelSelectScreen levelSelectScreen = FindObjectOfType<LevelSelectScreen>();
			if (levelSelectScreen != null)
			{
				levelSelectScreen.RefreshLevelStars();
				Debug.Log("[GAME] Refreshed level select UI stars");
			}
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
				
				// Refresh level select UI if it exists
				RefreshLevelSelectUI();
				
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
		
		/// <summary>
		/// Load user inventory from Firestore database
		/// This ensures selected towers are available when entering a level
		/// </summary>
		protected async void LoadInventoryFromDB()
		{
			try
			{
				var serviceLocator = ServiceLocator.Instance;
				var authService = serviceLocator.GetService<IAuthService>();
				if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
				{
					Debug.Log("[GAME] User not authenticated, skipping load inventory from DB");
					return;
				}

				string uid = authService.CurrentUser.UID;

				var inventoryService = serviceLocator.GetService<IInventoryService>();
				if (inventoryService != null)
				{
					var inventoryData = await inventoryService.LoadUserInventoryAsync(uid);
					
					if (inventoryData != null)
					{
						// Filter inventory based on unlocked towers (maxLevel)
						await FilterInventoryByUnlockedTowers(inventoryService, uid, inventoryData);
						
						var selectedTowers = inventoryData.GetSelectedTowers();
						Debug.Log($"[GAME] Successfully loaded inventory from DB: {inventoryData.ownedTowers.Count} towers owned, {selectedTowers.Count} selected");
						
						// Log selected towers
						if (selectedTowers.Count > 0)
						{
							Debug.Log($"[GAME] Selected towers for gameplay:");
							foreach (var tower in selectedTowers)
							{
								Debug.Log($"  - {tower.towerName} (Type: {tower.towerType})");
							}
						}
						else
						{
							Debug.LogWarning("[GAME] No towers selected! Player won't have any towers in level.");
						}
					}
					else
					{
						Debug.LogWarning("[GAME] Failed to load inventory from DB");
					}
				}
				else
				{
					Debug.LogWarning("[GAME] InventoryService not available");
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[GAME] Error loading inventory from DB: {e.Message}");
			}
		}
		
		/// <summary>
		/// Public method to trigger loading inventory from database
		/// Can be called when user logs in after GameManager is already initialized
		/// </summary>
		public void RefreshInventoryFromDB()
		{
			LoadInventoryFromDB();
		}

		/// <summary>
		/// Public method to trigger filtering inventory by unlocked towers
		/// Can be called when entering a level to ensure inventory is synced with maxLevel
		/// </summary>
		public async System.Threading.Tasks.Task FilterInventoryIfNeeded()
		{
			try
			{
				var serviceLocator = ServiceLocator.Instance;
				var authService = serviceLocator?.GetService<IAuthService>();
				if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
				{
					Debug.Log("[GAME] User not authenticated, skipping filter inventory");
					return;
				}

				string uid = authService.CurrentUser.UID;
				var inventoryService = serviceLocator?.GetService<IInventoryService>();
				if (inventoryService == null)
				{
					Debug.LogWarning("[GAME] InventoryService not available, cannot filter inventory");
					return;
				}

				// Load current inventory
				var inventoryData = inventoryService.GetCachedInventory();
				if (inventoryData == null)
				{
					// Try to load from DB first
					inventoryData = await inventoryService.LoadUserInventoryAsync(uid);
				}

				if (inventoryData != null)
				{
					// Filter inventory based on unlocked towers
					await FilterInventoryByUnlockedTowers(inventoryService, uid, inventoryData);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[GAME] Error filtering inventory: {e.Message}");
			}
		}

		/// <summary>
		/// Unlock tower for completed level
		/// When player wins level 1 (index 0): Unlock Rocket
		/// When player wins level 2 (index 1): Unlock Emp
		/// When player wins level 3 (index 2): Unlock Laser
		/// </summary>
		protected async System.Threading.Tasks.Task UnlockTowerForLevel(string userId, string levelId)
		{
			try
			{
				// Get level index
				int levelIndex = GetLevelIndex(levelId);
				if (levelIndex < 0)
				{
					Debug.LogWarning($"[GAME] Cannot unlock tower: Invalid levelId {levelId}");
					return;
				}

				// Determine which tower to unlock based on level index
				// When player wins level 1 (index 0), unlock Rocket for level 2
				// When player wins level 2 (index 1), unlock Emp for level 3
				// When player wins level 3 (index 2), unlock Laser for level 4
				MainTower towerToUnlock = MainTower.MachineGun; // Default
				bool shouldUnlock = false;

				if (levelIndex == 0)
				{
					// Win level 1 -> unlock Rocket
					towerToUnlock = MainTower.Rocket;
					shouldUnlock = true;
				}
				else if (levelIndex == 1)
				{
					// Win level 2 -> unlock Emp
					towerToUnlock = MainTower.Emp;
					shouldUnlock = true;
				}
				else if (levelIndex == 2)
				{
					// Win level 3 -> unlock Laser
					towerToUnlock = MainTower.Laser;
					shouldUnlock = true;
				}

				if (!shouldUnlock)
				{
					Debug.Log($"[GAME] No tower to unlock for level index {levelIndex}");
					return;
				}

				// Load all tower prefabs to find towerName from MainTower
				Tower[] allTowerPrefabs = Resources.LoadAll<Tower>("Towers");
				if (allTowerPrefabs == null || allTowerPrefabs.Length == 0)
				{
					Debug.LogWarning("[GAME] Cannot unlock tower: No tower prefabs found in Resources/Towers/");
					return;
				}

				// Find tower prefab with matching MainTower
				Tower towerPrefab = null;
				foreach (var prefab in allTowerPrefabs)
				{
					if (prefab != null && prefab.mainTower == towerToUnlock)
					{
						towerPrefab = prefab;
						break;
					}
				}

				if (towerPrefab == null || string.IsNullOrEmpty(towerPrefab.towerName))
				{
					Debug.LogWarning($"[GAME] Cannot unlock tower: Tower prefab with MainTower.{towerToUnlock} not found");
					return;
				}

				// Get inventory service
				var serviceLocator = ServiceLocator.Instance;
				var inventoryService = serviceLocator?.GetService<IInventoryService>();
				if (inventoryService == null)
				{
					Debug.LogWarning("[GAME] Cannot unlock tower: InventoryService not available");
					return;
				}

				// Check if user already owns this tower
				if (inventoryService.HasTower(towerPrefab.towerName))
				{
					Debug.Log($"[GAME] User already owns tower {towerPrefab.towerName}, skipping unlock");
					return;
				}

				// Unlock tower
				bool success = await inventoryService.UnlockTowerAsync(userId, towerPrefab.towerName);
				if (success)
				{
					Debug.Log($"[GAME] ✅ Successfully unlocked tower {towerPrefab.towerName} (MainTower: {towerToUnlock}) for completing level {levelId} (index {levelIndex})");
				}
				else
				{
					Debug.LogWarning($"[GAME] Failed to unlock tower {towerPrefab.towerName} for user {userId}");
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[GAME] Error unlocking tower for level: {e.Message}");
			}
		}

		/// <summary>
		/// Filter inventory to only keep towers that are unlocked based on player's maxLevel
		/// Removes towers that are not unlocked and deselects them if they were selected
		/// Level 1: Machine Gun
		/// Level 2: Machine Gun + Rocket
		/// Level 3: Machine Gun + Rocket + Emp
		/// Level 4: Machine Gun + Rocket + Emp + Laser
		/// </summary>
		protected async Task FilterInventoryByUnlockedTowers(IInventoryService inventoryService, string userId, TowerInventoryData inventoryData)
		{
			if (inventoryData == null || inventoryData.ownedTowers == null)
			{
				return;
			}

			// Get unlocked tower types based on maxLevel
			int maxLevel = GetMaxLevel();
			if (maxLevel < 1)
			{
				maxLevel = 1; // Default to level 1
			}

		HashSet<int> unlockedTowerTypes = new HashSet<int>();
		List<string> unlockedTowerNames = new List<string>();
		
		unlockedTowerTypes.Add((int)MainTower.MachineGun); // Always unlocked
		unlockedTowerNames.Add("MachineGun");
		
		if (maxLevel >= 2)
		{
			unlockedTowerTypes.Add((int)MainTower.Rocket);
			unlockedTowerNames.Add("Rocket");
		}
		if (maxLevel >= 3)
		{
			unlockedTowerTypes.Add((int)MainTower.Emp);
			unlockedTowerNames.Add("Emp");
		}
		if (maxLevel >= 4)
		{
			unlockedTowerTypes.Add((int)MainTower.Laser);
			unlockedTowerNames.Add("Laser");
		}

		Debug.Log($"[GAME] Filtering inventory for maxLevel {maxLevel}. Unlocked towers ({unlockedTowerTypes.Count}): {string.Join(", ", unlockedTowerNames)}");

		// Load all tower prefabs to map towerName -> MainTower
		Debug.Log("[GAME] Loading tower prefabs from Resources/Towers/...");
		Tower[] allTowerPrefabs = Resources.LoadAll<Tower>("Towers");
		Debug.Log($"[GAME] Loaded {(allTowerPrefabs != null ? allTowerPrefabs.Length : 0)} tower prefabs");
		
		if (allTowerPrefabs == null || allTowerPrefabs.Length == 0)
		{
			Debug.LogError("[GAME] ❌ Cannot filter inventory: No tower prefabs found in Resources/Towers/");
			return;
		}

			// Create mapping: towerName -> MainTower
			Dictionary<string, MainTower> towerNameToType = new Dictionary<string, MainTower>();
			foreach (var prefab in allTowerPrefabs)
			{
				if (prefab != null && !string.IsNullOrEmpty(prefab.towerName))
				{
					towerNameToType[prefab.towerName] = prefab.mainTower;
				}
			}

		// Find towers to remove (not unlocked)
		List<string> towersToRemove = new List<string>();
		List<string> selectedTowersToDeselect = new List<string>();
		
		Debug.Log($"[GAME] Checking {inventoryData.ownedTowers.Count} towers in inventory...");

		foreach (var tower in inventoryData.ownedTowers.ToList())
		{
			if (tower == null || string.IsNullOrEmpty(tower.towerName))
			{
				continue;
			}

			// Check if tower type is unlocked
			bool isUnlocked = false;
			MainTower towerMainType = MainTower.MachineGun;
			
			if (towerNameToType.ContainsKey(tower.towerName))
			{
				towerMainType = towerNameToType[tower.towerName];
				isUnlocked = unlockedTowerTypes.Contains((int)towerMainType);
				Debug.Log($"[GAME] Checking tower: {tower.towerName} -> MainTower.{towerMainType} (value={(int)towerMainType}) -> {(isUnlocked ? "✅ UNLOCKED" : "❌ LOCKED")}");
			}
			else
			{
				// If tower name not found in prefabs, check by towerType (fallback)
				isUnlocked = unlockedTowerTypes.Contains(tower.towerType);
				Debug.LogWarning($"[GAME] Tower {tower.towerName} not found in prefabs! Checking by towerType={tower.towerType} -> {(isUnlocked ? "UNLOCKED" : "LOCKED")}");
			}

			if (!isUnlocked)
			{
				// Mark for removal
				towersToRemove.Add(tower.towerName);
				if (tower.isSelected)
				{
					selectedTowersToDeselect.Add(tower.towerName);
				}
				Debug.Log($"[GAME] ❌ Tower {tower.towerName} (MainTower.{towerMainType}, towerType={tower.towerType}) is not unlocked for maxLevel {maxLevel}, will be removed");
			}
		}

			// Remove locked towers from database
			bool inventoryChanged = false;
			foreach (var towerName in towersToRemove)
			{
				bool removed = await inventoryService.RemoveTowerAsync(userId, towerName);
				if (removed)
				{
					inventoryChanged = true;
					Debug.Log($"[GAME] Removed locked tower from database: {towerName}");
				}
				else
				{
					// If remove failed, still remove from local data
					inventoryData.RemoveTower(towerName);
					inventoryChanged = true;
					Debug.Log($"[GAME] Removed locked tower from local data: {towerName}");
				}
			}

			// Reload inventory after removing towers
			if (inventoryChanged)
			{
				inventoryData = await inventoryService.LoadUserInventoryAsync(userId);
			}

			// If any selected towers were removed, update selection
			if (selectedTowersToDeselect.Count > 0 || inventoryChanged)
			{
				var remainingSelected = inventoryData.GetSelectedTowerNames();
				if (remainingSelected.Count > 0)
				{
					// Keep only unlocked selected towers
					await inventoryService.SelectTowersAsync(userId, remainingSelected);
					Debug.Log($"[GAME] Updated selected towers after filtering: {string.Join(", ", remainingSelected)}");
				}
				else
				{
					// No towers selected, select Machine Gun if available
					var machineGunTower = inventoryData.ownedTowers.FirstOrDefault(t => 
						towerNameToType.ContainsKey(t.towerName) && 
						towerNameToType[t.towerName] == MainTower.MachineGun);
					
					if (machineGunTower != null)
					{
						await inventoryService.SelectTowersAsync(userId, new List<string> { machineGunTower.towerName });
						Debug.Log($"[GAME] Auto-selected Machine Gun as default tower");
					}
				}
			}

		if (inventoryChanged)
		{
			Debug.Log($"[GAME] Inventory filtered and saved. Removed {towersToRemove.Count} locked towers");
			
			// Sync updated inventory to ScriptableObjects
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator != null)
			{
				// Get latest inventory data
				var latestInventory = inventoryService.GetCachedInventory();
				if (latestInventory != null)
				{
			Debug.Log("[GAME] Syncing filtered inventory to ScriptableObjects...");
				try
				{
					Services.Firestore.GameDataSyncService.SyncUserInventoryToScriptableObject(latestInventory);
					Services.Firestore.GameDataSyncService.SyncUserInventoryDataToScriptableObject(latestInventory);
					Debug.Log("[GAME] ✅ Successfully synced filtered inventory to ScriptableObjects");
				}
				catch (System.Exception syncEx)
				{
					Debug.LogError($"[GAME] Error syncing filtered inventory: {syncEx.Message}");
				}
				}
			}
		}
		else
		{
			Debug.Log($"[GAME] Inventory is already in sync with unlocked towers");
		}
	}
}
}