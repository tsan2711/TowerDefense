using System;
using System.Linq;
using Core.Economy;
using Core.Health;
using Core.Utilities;
using TowerDefense.Economy;
using TowerDefense.Towers;
using TowerDefense.Towers.Data;
using TowerDefense.Game;
using UnityEngine;
using Services.Core;
using Services.Managers;
using Services.Data;
using System.Threading.Tasks;

namespace TowerDefense.Level
{
	/// <summary>
	/// The level manager - handles the level states and tracks the player's currency
	/// </summary>
	[RequireComponent(typeof(WaveManager))]
	public class LevelManager : Singleton<LevelManager>
	{
		/// <summary>
		/// The configured level intro. If this is null the LevelManager will fall through to the gameplay state (i.e. SpawningEnemies)
		/// </summary>
		public LevelIntro intro;

		/// <summary>
		/// The tower library for this level
		/// </summary>
		public TowerLibrary towerLibrary;

		/// <summary>
		/// The currency that the player starts with
		/// </summary>
		public int startingCurrency;

		/// <summary>
		/// The controller for gaining currency
		/// </summary>
		public CurrencyGainer currencyGainer;

		/// <summary>
		/// Configuration for if the player gains currency even in pre-build phase
		/// </summary>
		[Header("Setting this will allow currency gain during the Intro and Pre-Build phase")]
		public bool alwaysGainCurrency;

		/// <summary>
		/// The home bases that the player must defend
		/// </summary>
		public PlayerHomeBase[] homeBases;

		public Collider[] environmentColliders;

		/// <summary>
		/// The attached wave manager
		/// </summary>
		public WaveManager waveManager { get; protected set; }

		/// <summary>
		/// Number of enemies currently in the level
		/// </summary>
		public int numberOfEnemies { get; protected set; }

		/// <summary>
		/// The current state of the level
		/// </summary>
		public LevelState levelState { get; protected set; }

		/// <summary>
		/// The currency controller
		/// </summary>
		public Currency currency { get; protected set; }

		/// <summary>
		/// Number of home bases left
		/// </summary>
		public int numberOfHomeBasesLeft { get; protected set; }

		/// <summary>
		/// Starting number of home bases
		/// </summary>
		public int numberOfHomeBases { get; protected set; }

		/// <summary>
		/// An accessor for the home bases
		/// </summary>
		public PlayerHomeBase[] playerHomeBases
		{
			get { return homeBases; }
		}

		/// <summary>
		/// If the game is over
		/// </summary>
		public bool isGameOver
		{
			get { return (levelState == LevelState.Win) || (levelState == LevelState.Lose); }
		}

		/// <summary>
		/// Fired when all the waves are done and there are no more enemies left
		/// </summary>
		public event Action levelCompleted;

		/// <summary>
		/// Fired when all of the home bases are destroyed
		/// </summary>
		public event Action levelFailed;

		/// <summary>
		/// Fired when the level state is changed - first parameter is the old state, second parameter is the new state
		/// </summary>
		public event Action<LevelState, LevelState> levelStateChanged;

		/// <summary>
		/// Fired when the number of enemies has changed
		/// </summary>
		public event Action<int> numberOfEnemiesChanged;

		/// <summary>
		/// Event for home base being destroyed
		/// </summary>
		public event Action homeBaseDestroyed;

		/// <summary>
		/// Fired when tower library is updated/filtered
		/// </summary>
		public event Action towerLibraryUpdated;

		/// <summary>
		/// Increments the number of enemies. Called on Agent spawn
		/// </summary>
		public virtual void IncrementNumberOfEnemies()
		{
			numberOfEnemies++;
			SafelyCallNumberOfEnemiesChanged();
		}

		/// <summary>
		/// Returns the sum of all HomeBases' health
		/// </summary>
		public float GetAllHomeBasesHealth()
		{
			float health = 0.0f;
			foreach (PlayerHomeBase homebase in homeBases)
			{
				health += homebase.configuration.currentHealth;
			}
			return health;
		}

		/// <summary>
		/// Decrements the number of enemies. Called on Agent death
		/// </summary>
		public virtual void DecrementNumberOfEnemies()
		{
			numberOfEnemies--;
			SafelyCallNumberOfEnemiesChanged();
			if (numberOfEnemies < 0)
			{
				Debug.LogError("[LEVEL] There should never be a negative number of enemies. Something broke!");
				numberOfEnemies = 0;
			}

			if (numberOfEnemies == 0 && levelState == LevelState.AllEnemiesSpawned)
			{
				ChangeLevelState(LevelState.Win);
			}
		}

		/// <summary>
		/// Completes building phase, setting state to spawn enemies
		/// </summary>
		public virtual void BuildingCompleted()
		{
			ChangeLevelState(LevelState.SpawningEnemies);
		}

		/// <summary>
		/// Caches the attached wave manager and subscribes to the spawning completed event
		/// Sets the level state to intro and ensures that the number of enemies is set to 0
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			waveManager = GetComponent<WaveManager>();
			waveManager.spawningCompleted += OnSpawningCompleted;

			// Does not use the change state function as we don't need to broadcast the event for this default value
			levelState = LevelState.Intro;
			numberOfEnemies = 0;

			// Ensure currency change listener is assigned
			currency = new Currency(startingCurrency);
			currencyGainer.Initialize(currency);

			// Filter inventory to ensure it's synced with maxLevel before loading tower library
			FilterInventoryIfNeeded();

			// Load TowerLibrary từ container nếu chưa được assign
			// Must load library BEFORE subscribing to events, so library is ready when inventory loads
			LoadTowerLibraryFromContainer();

			// Subscribe to inventory events to refresh tower library when selected towers change
			// This will also trigger filter immediately if inventory is already cached
			SubscribeToInventoryEvents();

			// If there's an intro use it, otherwise fall through to gameplay
			if (intro != null)
			{
				intro.introCompleted += IntroCompleted;
			}
			else
			{
				IntroCompleted();
			}

			// Iterate through home bases and subscribe
			numberOfHomeBases = homeBases.Length;
			numberOfHomeBasesLeft = numberOfHomeBases;
			for (int i = 0; i < numberOfHomeBases; i++)
			{
				homeBases[i].died += OnHomeBaseDestroyed;
			}
		}

		/// <summary>
		/// Load TowerLibrary từ TowerLibraryContainer dựa trên levelId của scene hiện tại
		/// Note: Filtering by selected towers will be handled by OnInventoryLoaded event
		/// </summary>
		protected void LoadTowerLibraryFromContainer()
		{
			// Nếu đã có towerLibrary được assign trong Inspector, tạo runtime copy để tránh modify asset gốc
			if (towerLibrary != null)
			{
				Debug.Log("[LevelManager] TowerLibrary already assigned in Inspector, creating runtime copy to avoid modifying original asset");
				// Create runtime copy to avoid modifying the original ScriptableObject
				TowerLibrary originalLibrary = towerLibrary;
				towerLibrary = ScriptableObject.CreateInstance<TowerLibrary>();
				towerLibrary.configurations = new System.Collections.Generic.List<Tower>(originalLibrary.configurations);
				towerLibrary.OnAfterDeserialize();
				// Don't filter here - let OnInventoryLoaded event handle it
				return;
			}

			// Lấy levelId từ scene hiện tại
			string levelId = GetCurrentLevelId();
			if (string.IsNullOrEmpty(levelId))
			{
				Debug.LogWarning("[LevelManager] Cannot determine levelId for current scene, TowerLibrary will not be loaded from container");
				return;
			}

			// Load TowerLibraryContainer từ Resources hoặc cached instance
			TowerLibraryContainer container = Services.Firestore.TowerLibraryLoader.GetContainerInstance();
			if (container == null)
			{
				Debug.LogWarning("[LevelManager] TowerLibraryContainer not found, TowerLibrary will not be loaded");
				return;
			}

			// Get TowerLibrary từ container
			TowerLibrary library = container.GetLibrary(levelId);
			if (library != null)
			{
				// Create a runtime copy to avoid modifying the original ScriptableObject asset
				// This ensures filtered towers don't affect the original asset
				towerLibrary = ScriptableObject.CreateInstance<TowerLibrary>();
				towerLibrary.configurations = new System.Collections.Generic.List<Tower>(library.configurations);
				towerLibrary.OnAfterDeserialize();
				Debug.Log($"[LevelManager] Loaded TowerLibrary for level {levelId} from container with {library.configurations.Count} towers (created runtime copy)");
				// Don't filter here - let OnInventoryLoaded event handle it when inventory is ready
			}
			else
			{
				Debug.LogWarning($"[LevelManager] TowerLibrary not found in container for level {levelId}");
			}
		}
		
		/// <summary>
		/// Get list of unlocked tower types based on player's maxLevel
		/// Level 1: Machine Gun
		/// Level 2: Machine Gun + Rocket
		/// Level 3: Machine Gun + Rocket + Emp
		/// Level 4: Machine Gun + Rocket + Emp + Laser
		/// </summary>
		/// <returns>HashSet of unlocked MainTower enum values</returns>
		protected System.Collections.Generic.HashSet<int> GetUnlockedTowerTypes()
		{
			System.Collections.Generic.HashSet<int> unlockedTypes = new System.Collections.Generic.HashSet<int>();
			
			// Get maxLevel from GameManager
			int maxLevel = 1; // Default to level 1 (Machine Gun only)
			if (TowerDefense.Game.GameManager.instanceExists)
			{
				maxLevel = TowerDefense.Game.GameManager.instance.GetMaxLevel();
				// Ensure maxLevel is at least 1 (player always starts with Machine Gun)
				if (maxLevel < 1)
				{
					maxLevel = 1;
				}
			}
			
			// Level 1: Machine Gun (always unlocked)
			unlockedTypes.Add((int)MainTower.MachineGun);
			
			// Level 2: Add Rocket
			if (maxLevel >= 2)
			{
				unlockedTypes.Add((int)MainTower.Rocket);
			}
			
			// Level 3: Add Emp
			if (maxLevel >= 3)
			{
				unlockedTypes.Add((int)MainTower.Emp);
			}
			
			// Level 4: Add Laser
			if (maxLevel >= 4)
			{
				unlockedTypes.Add((int)MainTower.Laser);
			}
			
			Debug.Log($"[LevelManager] Unlocked towers for maxLevel {maxLevel}: {string.Join(", ", unlockedTypes)}");
			return unlockedTypes;
		}

		/// <summary>
		/// Build TowerLibrary from selected towers in inventory
		/// Only includes towers that are:
		/// 1. Unlocked based on player's maxLevel
		/// 2. Selected in user's inventory
		/// </summary>
		protected void FilterTowerLibraryBySelectedTowers()
		{
			// Get unlocked tower types based on player progress
			System.Collections.Generic.HashSet<int> unlockedTowerTypes = GetUnlockedTowerTypes();
			if (unlockedTowerTypes == null || unlockedTowerTypes.Count == 0)
			{
				Debug.LogWarning("[LevelManager] No unlocked towers, using existing tower library as fallback");
				return;
			}

			// Get selected towers from inventory service
			// Store both towerName and towerType for matching
			System.Collections.Generic.HashSet<string> selectedTowerNames = new System.Collections.Generic.HashSet<string>();
			System.Collections.Generic.Dictionary<string, int> selectedTowerNameToType = new System.Collections.Generic.Dictionary<string, int>();
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator != null)
			{
				var inventoryService = serviceLocator.GetService<Services.Core.IInventoryService>();
				if (inventoryService != null)
				{
					var inventoryData = inventoryService.GetCachedInventory();
					if (inventoryData != null && inventoryData.ownedTowers != null)
					{
						// Get only towers with isSelected = true from backend
						var selectedTowers = inventoryData.GetSelectedTowers();
						if (selectedTowers != null && selectedTowers.Count > 0)
						{
							foreach (var tower in selectedTowers)
							{
								// Double-check: Only add towers with isSelected = true
								if (tower != null && tower.isSelected && !string.IsNullOrEmpty(tower.towerName))
								{
									selectedTowerNames.Add(tower.towerName);
									selectedTowerNameToType[tower.towerName] = tower.towerType;
									Debug.Log($"[LevelManager] ✅ Selected tower from BE (isSelected=true): {tower.towerName} (Type: {tower.towerType}, MainTower: {(MainTower)tower.towerType})");
								}
								else if (tower != null && !tower.isSelected)
								{
									Debug.LogWarning($"[LevelManager] ⚠️ Skipping tower '{tower.towerName}' - isSelected=false (should not be in GetSelectedTowers())");
								}
							}
						}
						else
						{
							Debug.LogWarning("[LevelManager] No selected towers found in inventory data (selectedTowers is null or empty)");
						}
					}
					else
					{
						Debug.LogWarning("[LevelManager] Inventory data not available yet (null or empty). Tower library will not be filtered. Waiting for inventory to load...");
						return; // Don't filter if inventory not ready yet
					}
				}
				else
				{
					Debug.LogWarning("[LevelManager] InventoryService not available. Tower library will not be filtered.");
					return;
				}
			}
			else
			{
				Debug.LogWarning("[LevelManager] ServiceLocator not available. Tower library will not be filtered.");
				return;
			}

			if (selectedTowerNames.Count == 0)
			{
				Debug.LogWarning("[LevelManager] ⚠️ No towers with isSelected=true found in inventory! TowerLibrary will be empty.");
			}

			// Debug: Log selected tower names from inventory (only isSelected=true from BE)
			Debug.Log($"[LevelManager] 📋 Filtering TowerLibrary - Only towers with isSelected=true from BE ({selectedTowerNames.Count}): {string.Join(", ", selectedTowerNames)}");
			Debug.Log($"[LevelManager] 🔓 Unlocked tower types: {string.Join(", ", unlockedTowerTypes)}");
			
			// Log detailed info about each selected tower for debugging
			Debug.Log($"[LevelManager] 📊 Detailed selected towers info:");
			foreach (var kvp in selectedTowerNameToType)
			{
				Debug.Log($"[LevelManager]   - {kvp.Key} (Type: {kvp.Value}, MainTower: {(MainTower)kvp.Value}, Unlocked: {unlockedTowerTypes.Contains(kvp.Value)})");
			}

			// Get source towers to filter from
			// Strategy: Use existing towerLibrary as base, but also load from Resources to ensure all selected towers are available
			System.Collections.Generic.Dictionary<string, Tower> sourceTowersDict = new System.Collections.Generic.Dictionary<string, Tower>();
			
			// First, add towers from existing library if available
			if (towerLibrary != null && towerLibrary.configurations != null && towerLibrary.configurations.Count > 0)
			{
				foreach (var tower in towerLibrary.configurations)
				{
					if (tower != null && !string.IsNullOrEmpty(tower.towerName))
					{
						sourceTowersDict[tower.towerName] = tower;
					}
				}
				Debug.Log($"[LevelManager] Added {sourceTowersDict.Count} towers from existing tower library");
			}
			
			// Then, load all towers from Resources and add any missing ones
			// This ensures selected towers that aren't in the library can still be found
			// Resources takes priority - if a tower with the same name exists but different mainTower, replace it
			Tower[] allTowerPrefabs = Resources.LoadAll<Tower>("Towers");
			if (allTowerPrefabs != null && allTowerPrefabs.Length > 0)
			{
				Debug.Log($"[LevelManager] Loaded {allTowerPrefabs.Length} tower prefabs from Resources/Towers/");
				int addedFromResources = 0;
				int replacedFromResources = 0;
				foreach (var tower in allTowerPrefabs)
				{
					if (tower != null && !string.IsNullOrEmpty(tower.towerName))
					{
						// Log all tower names from Resources for debugging
						Debug.Log($"[LevelManager] Found tower prefab in Resources: '{tower.towerName}' (MainTower: {tower.mainTower})");
						
						// Check if tower with same name already exists
						if (sourceTowersDict.ContainsKey(tower.towerName))
						{
							var existingTower = sourceTowersDict[tower.towerName];
							// If mainTower matches, skip (duplicate)
							if (existingTower != null && existingTower.mainTower == tower.mainTower)
							{
								Debug.Log($"[LevelManager] Tower '{tower.towerName}' already in dict with same MainTower ({tower.mainTower}), skipping duplicate");
							}
							// If mainTower differs, replace with Resources version (source of truth)
							else
							{
								Debug.LogWarning($"[LevelManager] ⚠️ Tower '{tower.towerName}' exists in dict with different MainTower! Existing: {existingTower?.mainTower ?? MainTower.Emp}, Resources: {tower.mainTower}. Replacing with Resources version.");
								sourceTowersDict[tower.towerName] = tower;
								replacedFromResources++;
							}
						}
						else
						{
							// New tower, add it
							sourceTowersDict[tower.towerName] = tower;
							addedFromResources++;
						}
					}
					else
					{
						Debug.LogWarning($"[LevelManager] Found null or empty towerName in Resources prefab: {tower?.name ?? "NULL"}");
					}
				}
				Debug.Log($"[LevelManager] Added {addedFromResources} new towers, replaced {replacedFromResources} conflicting towers from Resources/Towers/ (total: {sourceTowersDict.Count})");
			}
			else
			{
				Debug.LogWarning("[LevelManager] No tower prefabs found in Resources/Towers/");
			}
			
			// Convert to list for filtering
			System.Collections.Generic.List<Tower> sourceTowers = new System.Collections.Generic.List<Tower>(sourceTowersDict.Values);
			
			// Debug: Log all available tower prefabs
			Debug.Log($"[LevelManager] Available tower prefabs to filter ({sourceTowers.Count}):");
			foreach (var towerPrefab in sourceTowers)
			{
				if (towerPrefab != null)
				{
					bool isUnlocked = unlockedTowerTypes.Contains((int)towerPrefab.mainTower);
					bool isSelected = selectedTowerNames.Contains(towerPrefab.towerName);
					Debug.Log($"  - {towerPrefab.towerName} (MainTower: {towerPrefab.mainTower}, Unlocked: {isUnlocked}, Selected: {isSelected})");
				}
			}

			// Create or update tower library
			if (towerLibrary == null)
			{
				towerLibrary = ScriptableObject.CreateInstance<TowerLibrary>();
				towerLibrary.configurations = new System.Collections.Generic.List<Tower>();
				Debug.Log("[LevelManager] Created new runtime TowerLibrary");
			}
			else
			{
				// Clear existing configurations
				if (towerLibrary.configurations == null)
				{
					towerLibrary.configurations = new System.Collections.Generic.List<Tower>();
				}
				else
				{
					towerLibrary.configurations.Clear();
				}
			}

			// Add tower prefabs that are BOTH unlocked AND selected
			// Matching strategy:
			// 1. First try exact towerName match (inventory towerName vs prefab towerName)
			// 2. If no match, try matching by MainTower type (inventory towerType vs prefab mainTower)
			//    This handles cases where inventory has "MachineGun1" but prefab has "Assault Cannon"
			//    IMPORTANT: Only match ONE prefab per inventory tower to avoid duplicates
			int addedCount = 0;
			int skippedUnlocked = 0;
			int skippedNotSelected = 0;
			
			// Track which inventory towers have been matched to avoid duplicate matches
			System.Collections.Generic.HashSet<string> matchedInventoryTowers = new System.Collections.Generic.HashSet<string>();
			
			foreach (var towerPrefab in sourceTowers)
			{
				if (towerPrefab == null)
				{
					continue;
				}
				
				bool isUnlocked = unlockedTowerTypes.Contains((int)towerPrefab.mainTower);
				bool isSelected = false;
				string matchedInventoryName = null;
				
				// Strategy 1: Try exact towerName match (case-insensitive)
				foreach (var selectedName in selectedTowerNames)
				{
					if (string.Equals(towerPrefab.towerName, selectedName, System.StringComparison.OrdinalIgnoreCase))
					{
						// Check if this inventory tower has already been matched
						if (matchedInventoryTowers.Contains(selectedName))
						{
							Debug.LogWarning($"[LevelManager] ⚠️ Inventory tower '{selectedName}' already matched, skipping duplicate match for prefab '{towerPrefab.towerName}'");
							continue;
						}
						
						isSelected = true;
						matchedInventoryName = selectedName;
						matchedInventoryTowers.Add(selectedName);
						Debug.Log($"[LevelManager] ✅ Matched by towerName: Prefab='{towerPrefab.towerName}' == Inventory='{selectedName}'");
						break;
					}
				}
				
				// Strategy 2: If no name match, try matching by MainTower type
				// This handles cases like: Inventory has "MachineGun1" (towerType=2) but Prefab has "Assault Cannon" (mainTower=MachineGun=2)
				// IMPORTANT: Only match ONE prefab per inventory tower to avoid duplicates
				if (!isSelected)
				{
					foreach (var kvp in selectedTowerNameToType)
					{
						string inventoryTowerName = kvp.Key;
						int inventoryTowerType = kvp.Value;
						
						// Skip if this inventory tower has already been matched
						if (matchedInventoryTowers.Contains(inventoryTowerName))
						{
							continue;
						}
						
						// Check if prefab's mainTower matches inventory's towerType
						if ((int)towerPrefab.mainTower == inventoryTowerType)
						{
							isSelected = true;
							matchedInventoryName = inventoryTowerName;
							matchedInventoryTowers.Add(inventoryTowerName);
							Debug.Log($"[LevelManager] ✅ Matched by MainTower type: Prefab='{towerPrefab.towerName}' (MainTower: {towerPrefab.mainTower}) == Inventory='{inventoryTowerName}' (towerType: {inventoryTowerType})");
							break;
						}
					}
				}
				
				// If still not matched, log for debugging
				if (!isSelected)
				{
					Debug.Log($"[LevelManager] ❌ Tower '{towerPrefab.towerName}' (MainTower: {towerPrefab.mainTower}) not matched. Selected inventory: [{string.Join(", ", selectedTowerNames)}]");
				}
				
				if (!isUnlocked)
				{
					skippedUnlocked++;
					Debug.Log($"[LevelManager] Skipping '{towerPrefab.towerName}' - not unlocked (MainTower: {towerPrefab.mainTower}, Unlocked types: [{string.Join(", ", unlockedTowerTypes)}])");
					continue;
				}
				
				if (!isSelected)
				{
					skippedNotSelected++;
					continue;
				}
				
				towerLibrary.configurations.Add(towerPrefab);
				addedCount++;
				Debug.Log($"[LevelManager] ✅ Added selected tower to library: {towerPrefab.towerName} (MainTower: {towerPrefab.mainTower}, Matched with inventory: {matchedInventoryName})");
			}
			
			Debug.Log($"[LevelManager] Filter summary: Added={addedCount}, Skipped (not unlocked)={skippedUnlocked}, Skipped (not selected)={skippedNotSelected}");
			
			// Check if all selected towers were matched
			if (matchedInventoryTowers.Count < selectedTowerNames.Count)
			{
				var unmatchedTowers = selectedTowerNames.Where(name => !matchedInventoryTowers.Contains(name)).ToList();
				Debug.LogWarning($"[LevelManager] ⚠️ WARNING: {unmatchedTowers.Count} selected tower(s) from BE were NOT matched to any prefab: {string.Join(", ", unmatchedTowers)}");
				Debug.LogWarning($"[LevelManager] ⚠️ Expected {selectedTowerNames.Count} towers, but only {matchedInventoryTowers.Count} were matched. Missing: {string.Join(", ", unmatchedTowers)}");
			}
			else if (matchedInventoryTowers.Count == selectedTowerNames.Count)
			{
				Debug.Log($"[LevelManager] ✅ All {selectedTowerNames.Count} selected towers from BE were successfully matched!");
			}

			// Rebuild dictionary after building library
			towerLibrary.OnAfterDeserialize();

			if (addedCount > 0)
			{
				Debug.Log($"[LevelManager] ✅ Built TowerLibrary with {addedCount} selected towers (Expected: {selectedTowerNames.Count} from BE):");
				foreach (var tower in towerLibrary.configurations)
				{
					Debug.Log($"  - {tower.towerName} (MainTower: {tower.mainTower})");
				}
				
				// Warn if count doesn't match
				if (addedCount != selectedTowerNames.Count)
				{
					Debug.LogWarning($"[LevelManager] ⚠️ MISMATCH: TowerLibrary has {addedCount} towers but BE has {selectedTowerNames.Count} selected towers!");
				}
			}
			else
			{
				Debug.LogWarning($"[LevelManager] ⚠️ No towers added to library! Unlocked types: {string.Join(", ", unlockedTowerTypes)}, Selected towers: {string.Join(", ", selectedTowerNames)}");
			}

			// Notify UI that tower library has been updated
			towerLibraryUpdated?.Invoke();
			Debug.Log($"[LevelManager] 📢 TowerLibrary updated event fired (UI should refresh)");
		}

		/// <summary>
		/// Get current level ID từ scene name hoặc GameManager
		/// </summary>
		protected string GetCurrentLevelId()
		{
			// Try to get from GameManager
			if (TowerDefense.Game.GameManager.instanceExists)
			{
				var levelItem = TowerDefense.Game.GameManager.instance.GetLevelForCurrentScene();
				if (levelItem != null && !string.IsNullOrEmpty(levelItem.id))
				{
					return levelItem.id;
				}
			}

			// Fallback: Try to infer from scene name
			string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			if (sceneName == "Tutorial")
			{
				return "level_1"; // Tutorial maps to level_1
			}

			// Try to match scene name to level pattern
			if (sceneName.StartsWith("Level"))
			{
				// Extract number from scene name (e.g., "Level1" -> "level_1", "Level2" -> "level_2")
				string numberStr = sceneName.Replace("Level", "");
				if (int.TryParse(numberStr, out int levelNumber))
				{
					return $"level_{levelNumber}";
				}
			}

			return null;
		}

		/// <summary>
		/// Filter inventory to ensure it's synced with maxLevel before loading tower library
		/// This ensures player only has unlocked towers in their inventory
		/// </summary>
		protected async void FilterInventoryIfNeeded()
		{
			if (!TowerDefense.Game.GameManager.instanceExists)
			{
				Debug.LogWarning("[LevelManager] GameManager not available, cannot filter inventory");
				return;
			}

			try
			{
				await TowerDefense.Game.GameManager.instance.FilterInventoryIfNeeded();
				Debug.Log("[LevelManager] Inventory filtered successfully");
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[LevelManager] Error filtering inventory: {e.Message}");
			}
		}

		/// <summary>
		/// Updates the currency gain controller
		/// </summary>
		protected virtual void Update()
		{
			if (alwaysGainCurrency ||
			    (!alwaysGainCurrency && levelState != LevelState.Building && levelState != LevelState.Intro))
			{
				currencyGainer.Tick(Time.deltaTime);
			}
		}

		/// <summary>
		/// Subscribe to inventory service events to refresh tower library when selected towers change
		/// Also ensures inventory is reloaded from backend to get latest selected towers
		/// </summary>
		protected void SubscribeToInventoryEvents()
		{
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator != null)
			{
				var inventoryService = serviceLocator.GetService<Services.Core.IInventoryService>();
				if (inventoryService != null)
				{
					inventoryService.OnInventoryLoaded += OnInventoryLoaded;
					inventoryService.OnSelectedTowersChanged += OnSelectedTowersChanged;
					Debug.Log("[LevelManager] Subscribed to inventory events (OnInventoryLoaded, OnSelectedTowersChanged)");
					
					// Always reload inventory from backend when entering level to ensure we have latest selected towers
					// This is important because user may have changed selection in inventory UI before entering level
					ReloadInventoryFromBackend();
				}
			}
		}

		/// <summary>
		/// Reload inventory from backend to ensure we have latest selected towers
		/// This is called when entering level to sync with backend data
		/// </summary>
		protected async void ReloadInventoryFromBackend()
		{
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator == null)
			{
				Debug.LogWarning("[LevelManager] ServiceLocator not available, cannot reload inventory");
				// Fallback: try to use cached inventory if available
				TryFilterWithCachedInventory();
				return;
			}

			var authService = serviceLocator.GetService<Services.Core.IAuthService>();
			if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
			{
				Debug.LogWarning("[LevelManager] User not authenticated, using cached inventory if available");
				// Fallback: try to use cached inventory if available
				TryFilterWithCachedInventory();
				return;
			}

			var inventoryService = serviceLocator.GetService<Services.Core.IInventoryService>();
			if (inventoryService == null)
			{
				Debug.LogWarning("[LevelManager] InventoryService not available, using cached inventory if available");
				// Fallback: try to use cached inventory if available
				TryFilterWithCachedInventory();
				return;
			}

			try
			{
				string uid = authService.CurrentUser.UID;
				Debug.Log($"[LevelManager] Reloading inventory from backend for user {uid} to ensure latest selected towers");
				
				// Reload inventory from backend - this will update cached inventory and fire OnInventoryLoaded event
				var inventoryData = await inventoryService.LoadUserInventoryAsync(uid);
				
				if (inventoryData != null)
				{
					Debug.Log($"[LevelManager] ✅ Successfully reloaded inventory from backend: {inventoryData.ownedTowers.Count} towers owned");
					
					// Filter inventory by unlocked towers (based on maxLevel)
					if (TowerDefense.Game.GameManager.instanceExists)
					{
						await TowerDefense.Game.GameManager.instance.FilterInventoryIfNeeded();
					}
					
					// FilterTowerLibraryBySelectedTowers() will be called by OnInventoryLoaded event
					// But we also call it here to ensure it happens even if event doesn't fire
					FilterTowerLibraryBySelectedTowers();
				}
				else
				{
					Debug.LogWarning("[LevelManager] Failed to reload inventory from backend, using cached inventory");
					TryFilterWithCachedInventory();
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[LevelManager] Error reloading inventory from backend: {e.Message}");
				// Fallback: try to use cached inventory if available
				TryFilterWithCachedInventory();
			}
		}

		/// <summary>
		/// Try to filter tower library using cached inventory as fallback
		/// </summary>
		protected void TryFilterWithCachedInventory()
		{
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator == null)
			{
				return;
			}

			var inventoryService = serviceLocator.GetService<Services.Core.IInventoryService>();
			if (inventoryService == null)
			{
				return;
			}

			var cachedInventory = inventoryService.GetCachedInventory();
			if (cachedInventory != null && cachedInventory.ownedTowers != null && cachedInventory.ownedTowers.Count > 0)
			{
				Debug.Log("[LevelManager] Using cached inventory to filter tower library (fallback)");
				FilterTowerLibraryBySelectedTowers();
			}
			else
			{
				Debug.LogWarning("[LevelManager] No cached inventory available, tower library will not be filtered. Waiting for inventory to load...");
			}
		}

		/// <summary>
		/// Unsubscribes from events
		/// </summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (waveManager != null)
			{
				waveManager.spawningCompleted -= OnSpawningCompleted;
			}
			if (intro != null)
			{
				intro.introCompleted -= IntroCompleted;
			}

			// Unsubscribe from inventory events
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator != null)
			{
				var inventoryService = serviceLocator.GetService<Services.Core.IInventoryService>();
				if (inventoryService != null)
				{
					inventoryService.OnInventoryLoaded -= OnInventoryLoaded;
					inventoryService.OnSelectedTowersChanged -= OnSelectedTowersChanged;
				}
			}

			// Iterate through home bases and unsubscribe
			for (int i = 0; i < numberOfHomeBases; i++)
			{
				homeBases[i].died -= OnHomeBaseDestroyed;
			}
		}

		/// <summary>
		/// Event handler when inventory is loaded
		/// Refreshes tower library to match loaded inventory
		/// </summary>
		protected void OnInventoryLoaded(TowerInventoryData inventory)
		{
			if (inventory != null && inventory.ownedTowers != null)
			{
				Debug.Log($"[LevelManager] Inventory loaded: {inventory.ownedTowers.Count} towers owned, refreshing tower library");
				// Refresh tower library with loaded inventory
				FilterTowerLibraryBySelectedTowers();
			}
		}

		/// <summary>
		/// Event handler when selected towers in inventory change
		/// Refreshes tower library to match new selection
		/// </summary>
		protected void OnSelectedTowersChanged(System.Collections.Generic.List<string> selectedTowerNames)
		{
			Debug.Log($"[LevelManager] Selected towers changed: {string.Join(", ", selectedTowerNames)}");
			// Refresh tower library with new selected towers
			FilterTowerLibraryBySelectedTowers();
		}

		/// <summary>
		/// Fired when Intro is completed or immediately, if no intro is specified
		/// </summary>
		protected virtual void IntroCompleted()
		{
			ChangeLevelState(LevelState.Building);
		}

		/// <summary>
		/// Fired when the WaveManager has finished spawning enemies
		/// </summary>
		protected virtual void OnSpawningCompleted()
		{
			ChangeLevelState(LevelState.AllEnemiesSpawned);
		}

		/// <summary>
		/// Changes the state and broadcasts the event
		/// </summary>
		/// <param name="newState">The new state to transitioned to</param>
		protected virtual void ChangeLevelState(LevelState newState)
		{
			// If the state hasn't changed then return
			if (levelState == newState)
			{
				return;
			}

			LevelState oldState = levelState;
			levelState = newState;
			if (levelStateChanged != null)
			{
				levelStateChanged(oldState, newState);
			}
			
			switch (newState)
			{
				case LevelState.SpawningEnemies:
					waveManager.StartWaves();
					break;
				case LevelState.AllEnemiesSpawned:
					// Win immediately if all enemies are already dead
					if (numberOfEnemies == 0)
					{
						ChangeLevelState(LevelState.Win);
					}
					break;
				case LevelState.Lose:
					SafelyCallLevelFailed();
					break;
				case LevelState.Win:
					SafelyCallLevelCompleted();
					break;
			}
		}

		/// <summary>
		/// Fired when a home base is destroyed
		/// </summary>
		protected virtual void OnHomeBaseDestroyed(DamageableBehaviour homeBase)
		{
			// Decrement the number of home bases
			numberOfHomeBasesLeft--;

			// Call the destroyed event
			if (homeBaseDestroyed != null)
			{
				homeBaseDestroyed();
			}

			// If there are no home bases left and the level is not over then set the level to lost
			if ((numberOfHomeBasesLeft == 0) && !isGameOver)
			{
				ChangeLevelState(LevelState.Lose);
			}
		}

		/// <summary>
		/// Calls the <see cref="levelCompleted"/> event
		/// </summary>
		protected virtual void SafelyCallLevelCompleted()
		{
			if (levelCompleted != null)
			{
				levelCompleted();
			}
		}

		/// <summary>
		/// Calls the <see cref="numberOfEnemiesChanged"/> event
		/// </summary>
		protected virtual void SafelyCallNumberOfEnemiesChanged()
		{
			if (numberOfEnemiesChanged != null)
			{
				numberOfEnemiesChanged(numberOfEnemies);
			}
		}

		/// <summary>
		/// Calls the <see cref="levelFailed"/> event
		/// </summary>
		protected virtual void SafelyCallLevelFailed()
		{
			if (levelFailed != null)
			{
				levelFailed();
			}
		}
	}
}