#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TowerDefense.Game;
using TowerDefense.Towers;
using Services.Core;
using Services.Data;
using Services.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TowerDefense.Level.Editor
{
	/// <summary>
	/// Context menu để test các chức năng khi vào level
	/// Sử dụng: Right-click vào LevelManager trong Hierarchy > Test Level Functions
	/// </summary>
	public class LevelTestMenu : EditorWindow
	{
		private Vector2 scrollPosition;
		private int selectedMaxLevel = 1;
		private string statusMessage = "";
		private MessageType statusType = MessageType.Info;

		[MenuItem("GameObject/TowerDefense/Test Level Functions", false, 10)]
		public static void ShowWindow()
		{
			GetWindow<LevelTestMenu>("Level Test Menu");
		}

		[MenuItem("TowerDefense/Test/Level Test Menu")]
		public static void ShowWindowFromMenu()
		{
			GetWindow<LevelTestMenu>("Level Test Menu");
		}

		void OnGUI()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			
			EditorGUILayout.LabelField("Level Test Menu", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			// Status message
			if (!string.IsNullOrEmpty(statusMessage))
			{
				EditorGUILayout.HelpBox(statusMessage, statusType);
				EditorGUILayout.Space();
			}

			// GameManager status
			EditorGUILayout.LabelField("Game Manager Status", EditorStyles.boldLabel);
			if (!GameManager.instanceExists)
			{
				EditorGUILayout.HelpBox("GameManager instance không tồn tại. Vui lòng vào Play mode hoặc đảm bảo GameManager đã được khởi tạo.", MessageType.Warning);
			}
			else
			{
				var gm = GameManager.instance;
				EditorGUILayout.LabelField($"Max Level: {gm.GetMaxLevel()}");
				EditorGUILayout.LabelField($"Level List Count: {gm.levelList?.Count ?? 0}");
			}
			EditorGUILayout.Space();

			// Max Level Control
			EditorGUILayout.LabelField("Max Level Control", EditorStyles.boldLabel);
			selectedMaxLevel = EditorGUILayout.IntSlider("Set Max Level", selectedMaxLevel, 1, 5);
			EditorGUILayout.HelpBox(
				"Max Level 1: Machine Gun\n" +
				"Max Level 2: Machine Gun + Rocket\n" +
				"Max Level 3: Machine Gun + Rocket + Emp\n" +
				"Max Level 4: Machine Gun + Rocket + Emp + Laser",
				MessageType.Info);
			
			if (GUILayout.Button("Set Max Level", GUILayout.Height(30)))
			{
				SetMaxLevel(selectedMaxLevel);
			}
			EditorGUILayout.Space();

			// Tower Unlock Test
			EditorGUILayout.LabelField("Tower Unlock Test", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Unlock Rocket", GUILayout.Height(25)))
			{
				UnlockTower(MainTower.Rocket);
			}
			if (GUILayout.Button("Unlock Emp", GUILayout.Height(25)))
			{
				UnlockTower(MainTower.Emp);
			}
			if (GUILayout.Button("Unlock Laser", GUILayout.Height(25)))
			{
				UnlockTower(MainTower.Laser);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			// Inventory Operations
			EditorGUILayout.LabelField("Inventory Operations", EditorStyles.boldLabel);
			if (GUILayout.Button("Filter Inventory by MaxLevel", GUILayout.Height(30)))
			{
				FilterInventory();
			}
			if (GUILayout.Button("Reload Inventory from DB", GUILayout.Height(30)))
			{
				ReloadInventory();
			}
			if (GUILayout.Button("Show Current Inventory", GUILayout.Height(30)))
			{
				ShowInventory();
			}
			EditorGUILayout.Space();

			// Level Completion Test
			EditorGUILayout.LabelField("Level Completion Test", EditorStyles.boldLabel);
			if (GUILayout.Button("Complete Level 1 (Unlock Rocket)", GUILayout.Height(30)))
			{
				CompleteLevel(0, 3);
			}
			if (GUILayout.Button("Complete Level 2 (Unlock Emp)", GUILayout.Height(30)))
			{
				CompleteLevel(1, 3);
			}
			if (GUILayout.Button("Complete Level 3 (Unlock Laser)", GUILayout.Height(30)))
			{
				CompleteLevel(2, 3);
			}
			EditorGUILayout.Space();

		// Debug Functions
		EditorGUILayout.LabelField("Debug Functions", EditorStyles.boldLabel);
		if (GUILayout.Button("Check Current MaxLevel", GUILayout.Height(25)))
		{
			CheckCurrentMaxLevel();
		}
		if (GUILayout.Button("Show Unlocked Towers", GUILayout.Height(25)))
		{
			ShowUnlockedTowers();
		}
		EditorGUILayout.Space();

		// Utility Functions
		EditorGUILayout.LabelField("Utility Functions", EditorStyles.boldLabel);
		if (GUILayout.Button("Clear Status", GUILayout.Height(25)))
		{
			statusMessage = "";
		}
		if (GUILayout.Button("Refresh", GUILayout.Height(25)))
		{
			Repaint();
		}

		EditorGUILayout.EndScrollView();
		}

		private void SetMaxLevel(int maxLevel)
		{
			if (!Application.isPlaying)
			{
				ShowStatus("Chỉ có thể set maxLevel trong Play mode!", MessageType.Warning);
				return;
			}

			if (!GameManager.instanceExists)
			{
				ShowStatus("GameManager instance không tồn tại!", MessageType.Error);
				return;
			}

			// Complete levels to set maxLevel
			var gm = GameManager.instance;
			if (gm.levelList == null)
			{
				ShowStatus("LevelList không tồn tại!", MessageType.Error);
				return;
			}

			// Complete levels up to maxLevel - 1
			for (int i = 0; i < maxLevel - 1 && i < gm.levelList.Count; i++)
			{
				var levelItem = gm.levelList[i];
				if (levelItem != null && !string.IsNullOrEmpty(levelItem.id))
				{
					gm.CompleteLevel(levelItem.id, 3);
				}
			}

			ShowStatus($"Đã set maxLevel = {maxLevel} bằng cách complete các levels", MessageType.Info);
		}

		private void UnlockTower(MainTower towerType)
		{
			if (!Application.isPlaying)
			{
				ShowStatus("Chỉ có thể unlock tower trong Play mode!", MessageType.Warning);
				return;
			}

			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator == null)
			{
				ShowStatus("ServiceLocator không tồn tại!", MessageType.Error);
				return;
			}

			var authService = serviceLocator.GetService<IAuthService>();
			if (authService == null || !authService.IsAuthenticated || authService.CurrentUser == null)
			{
				ShowStatus("User chưa authenticated!", MessageType.Warning);
				return;
			}

			// Load tower prefab to get towerName
			Tower[] allTowerPrefabs = Resources.LoadAll<Tower>("Towers");
			if (allTowerPrefabs == null || allTowerPrefabs.Length == 0)
			{
				ShowStatus("Không tìm thấy tower prefabs trong Resources/Towers/", MessageType.Error);
				return;
			}

			Tower towerPrefab = allTowerPrefabs.FirstOrDefault(t => t != null && t.mainTower == towerType);
			if (towerPrefab == null || string.IsNullOrEmpty(towerPrefab.towerName))
			{
				ShowStatus($"Không tìm thấy tower với MainTower.{towerType}", MessageType.Error);
				return;
			}

			var inventoryService = serviceLocator.GetService<IInventoryService>();
			if (inventoryService == null)
			{
				ShowStatus("InventoryService không tồn tại!", MessageType.Error);
				return;
			}

			string userId = authService.CurrentUser.UID;
			
			// Check if already owned
			if (inventoryService.HasTower(towerPrefab.towerName))
			{
				ShowStatus($"User đã sở hữu tower {towerPrefab.towerName}", MessageType.Info);
				return;
			}

			// Unlock tower (async)
			UnlockTowerAsync(inventoryService, userId, towerPrefab.towerName, towerType);
		}

		private void FilterInventory()
		{
			if (!Application.isPlaying)
			{
				ShowStatus("Chỉ có thể filter inventory trong Play mode!", MessageType.Warning);
				return;
			}

			if (!GameManager.instanceExists)
			{
				ShowStatus("GameManager instance không tồn tại!", MessageType.Error);
				return;
			}

			var gm = GameManager.instance;
			gm.RefreshInventoryFromDB();
			ShowStatus("Đã trigger filter inventory. Kiểm tra Console để xem kết quả.", MessageType.Info);
		}

		private void ReloadInventory()
		{
			if (!Application.isPlaying)
			{
				ShowStatus("Chỉ có thể reload inventory trong Play mode!", MessageType.Warning);
				return;
			}

			if (!GameManager.instanceExists)
			{
				ShowStatus("GameManager instance không tồn tại!", MessageType.Error);
				return;
			}

			var gm = GameManager.instance;
			gm.RefreshInventoryFromDB();
			ShowStatus("Đã trigger reload inventory. Kiểm tra Console để xem kết quả.", MessageType.Info);
		}

		private void ShowInventory()
		{
			if (!Application.isPlaying)
			{
				ShowStatus("Chỉ có thể xem inventory trong Play mode!", MessageType.Warning);
				return;
			}

			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator == null)
			{
				ShowStatus("ServiceLocator không tồn tại!", MessageType.Error);
				return;
			}

			var inventoryService = serviceLocator.GetService<IInventoryService>();
			if (inventoryService == null)
			{
				ShowStatus("InventoryService không tồn tại!", MessageType.Error);
				return;
			}

			var inventory = inventoryService.GetCachedInventory();
			if (inventory == null)
			{
				ShowStatus("Inventory chưa được load!", MessageType.Warning);
				return;
			}

			var ownedTowers = inventory.ownedTowers ?? new List<InventoryItemData>();
			var selectedTowers = inventory.GetSelectedTowers();

			int ownedCount = ownedTowers != null ? ownedTowers.Count : 0;
			int selectedCount = selectedTowers != null ? selectedTowers.Count : 0;

			string message = $"Inventory:\n" +
				$"Owned: {ownedCount} towers\n" +
				$"Selected: {selectedCount} towers\n\n";

			if (ownedCount > 0)
			{
				message += "Owned Towers:\n";
				foreach (var tower in ownedTowers)
				{
					message += $"  - {tower.towerName} (Type: {tower.towerType}, Selected: {tower.isSelected})\n";
				}
			}

			Debug.Log($"[LevelTestMenu] {message}");
			ShowStatus(message, MessageType.Info);
		}

		private void CompleteLevel(int levelIndex, int stars)
		{
			if (!Application.isPlaying)
			{
				ShowStatus("Chỉ có thể complete level trong Play mode!", MessageType.Warning);
				return;
			}

			if (!GameManager.instanceExists)
			{
				ShowStatus("GameManager instance không tồn tại!", MessageType.Error);
				return;
			}

			var gm = GameManager.instance;
			if (gm.levelList == null)
			{
				ShowStatus("LevelList không tồn tại!", MessageType.Error);
				return;
			}
			
			int levelCount = gm.levelList.Count;
			if (levelIndex < 0 || levelIndex >= levelCount)
			{
				ShowStatus($"Level index {levelIndex} không hợp lệ!", MessageType.Error);
				return;
			}

			var levelItem = gm.levelList[levelIndex];
			if (levelItem == null || string.IsNullOrEmpty(levelItem.id))
			{
				ShowStatus($"Level tại index {levelIndex} không hợp lệ!", MessageType.Error);
				return;
			}

			gm.CompleteLevel(levelItem.id, stars);
			ShowStatus($"Đã complete level {levelItem.id} (index {levelIndex}) với {stars} stars. Kiểm tra Console để xem tower được unlock.", MessageType.Info);
		}

		private async void UnlockTowerAsync(IInventoryService inventoryService, string userId, string towerName, MainTower towerType)
		{
			try
			{
				bool success = await inventoryService.UnlockTowerAsync(userId, towerName);
				EditorApplication.delayCall += () =>
				{
					if (success)
					{
						ShowStatus($"✅ Đã unlock tower {towerName} (MainTower: {towerType})", MessageType.Info);
					}
					else
					{
						ShowStatus($"❌ Không thể unlock tower {towerName}", MessageType.Error);
					}
				};
			}
			catch (System.Exception e)
			{
				EditorApplication.delayCall += () =>
				{
					ShowStatus($"❌ Lỗi khi unlock tower: {e.Message}", MessageType.Error);
				};
			}
		}

	private void ShowStatus(string message, MessageType type)
	{
		statusMessage = message;
		statusType = type;
		Repaint();
	}

	private void CheckCurrentMaxLevel()
	{
		if (!Application.isPlaying)
		{
			ShowStatus("Chỉ có thể check maxLevel trong Play mode!", MessageType.Warning);
			return;
		}

		if (!GameManager.instanceExists)
		{
			ShowStatus("GameManager instance không tồn tại!", MessageType.Error);
			return;
		}

		var gm = GameManager.instance;
		int maxLevel = gm.GetMaxLevel();
		
		string message = $"Current MaxLevel: {maxLevel}\n\n";
		message += "Unlocked Towers:\n";
		message += "- Level 1: Machine Gun\n";
		
		if (maxLevel >= 2)
			message += "- Level 2: Machine Gun + Rocket\n";
		if (maxLevel >= 3)
			message += "- Level 3: Machine Gun + Rocket + Emp\n";
		if (maxLevel >= 4)
			message += "- Level 4: Machine Gun + Rocket + Emp + Laser\n";

		Debug.Log($"[LevelTestMenu] {message}");
		ShowStatus(message, MessageType.Info);
	}

	private void ShowUnlockedTowers()
	{
		if (!Application.isPlaying)
		{
			ShowStatus("Chỉ có thể xem unlocked towers trong Play mode!", MessageType.Warning);
			return;
		}

		if (!GameManager.instanceExists)
		{
			ShowStatus("GameManager instance không tồn tại!", MessageType.Error);
			return;
		}

		var serviceLocator = ServiceLocator.Instance;
		if (serviceLocator == null)
		{
			ShowStatus("ServiceLocator không tồn tại!", MessageType.Error);
			return;
		}

		var inventoryService = serviceLocator.GetService<IInventoryService>();
		if (inventoryService == null)
		{
			ShowStatus("InventoryService không tồn tại!", MessageType.Error);
			return;
		}

		var gm = GameManager.instance;
		int maxLevel = gm.GetMaxLevel();

		// Get expected unlocked towers
		List<MainTower> expectedUnlocked = new List<MainTower>();
		expectedUnlocked.Add(MainTower.MachineGun);
		if (maxLevel >= 2) expectedUnlocked.Add(MainTower.Rocket);
		if (maxLevel >= 3) expectedUnlocked.Add(MainTower.Emp);
		if (maxLevel >= 4) expectedUnlocked.Add(MainTower.Laser);

		// Get actual inventory
		var inventory = inventoryService.GetCachedInventory();
		if (inventory == null || inventory.ownedTowers == null)
		{
			ShowStatus("Inventory chưa được load!", MessageType.Warning);
			return;
		}

		// Load tower prefabs to map names
		Tower[] allTowerPrefabs = Resources.LoadAll<Tower>("Towers");
		Dictionary<string, MainTower> towerNameToType = new Dictionary<string, MainTower>();
		foreach (var prefab in allTowerPrefabs)
		{
			if (prefab != null && !string.IsNullOrEmpty(prefab.towerName))
			{
				towerNameToType[prefab.towerName] = prefab.mainTower;
			}
		}

		string message = $"MaxLevel: {maxLevel}\n\n";
		message += $"Expected Unlocked Towers: {string.Join(", ", expectedUnlocked)}\n\n";
		message += $"Actual Owned Towers ({inventory.ownedTowers.Count}):\n";

		bool hasIssue = false;
		foreach (var tower in inventory.ownedTowers)
		{
			if (towerNameToType.ContainsKey(tower.towerName))
			{
				MainTower towerType = towerNameToType[tower.towerName];
				bool shouldHave = expectedUnlocked.Contains(towerType);
				string status = shouldHave ? "✅" : "❌ SHOULD NOT HAVE";
				message += $"  {status} {tower.towerName} ({towerType})\n";
				
				if (!shouldHave)
				{
					hasIssue = true;
				}
			}
			else
			{
				message += $"  ⚠️ {tower.towerName} (Unknown type)\n";
			}
		}

		Debug.Log($"[LevelTestMenu] {message}");
		ShowStatus(message, hasIssue ? MessageType.Error : MessageType.Info);
	}
}
}
#endif

