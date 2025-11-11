using TowerDefense.Level;
using TowerDefense.Towers;
using UnityEngine;

namespace TowerDefense.UI.HUD
{
	/// <summary>
	/// UI component that displays towers that can be built on this level.
	/// </summary>
	public class BuildSidebar : MonoBehaviour
	{
		/// <summary>
		/// The prefab spawned for each button
		/// </summary>
		public TowerSpawnButton towerSpawnButton;

		/// <summary>
		/// List of spawned buttons for cleanup
		/// </summary>
		private System.Collections.Generic.List<TowerSpawnButton> spawnedButtons = new System.Collections.Generic.List<TowerSpawnButton>();

		/// <summary>
		/// Initialize the tower spawn buttons
		/// </summary>
		protected virtual void Start()
		{
			if (!LevelManager.instanceExists)
			{
				Debug.LogError("[BuildSidebar] No level manager for tower list");
				return;
			}

			// Subscribe to tower library updates
			LevelManager.instance.towerLibraryUpdated += RefreshTowerButtons;
			
			// Initial refresh (may be empty if towerLibrary not ready yet)
			RefreshTowerButtons();
		}

		/// <summary>
		/// Refresh tower buttons based on current tower library
		/// </summary>
		private void RefreshTowerButtons()
		{
			if (!LevelManager.instanceExists)
			{
				return;
			}

			// Clear existing buttons
			ClearButtons();

			// Create buttons for each tower in library
			if (LevelManager.instance.towerLibrary != null)
			{
				Debug.Log($"[BuildSidebar] Refreshing tower buttons - TowerLibrary has {LevelManager.instance.towerLibrary.configurations?.Count ?? 0} towers");
				foreach (Tower tower in LevelManager.instance.towerLibrary)
				{
					if (tower != null)
					{
						TowerSpawnButton button = Instantiate(towerSpawnButton, transform);
						button.InitializeButton(tower);
						button.buttonTapped += OnButtonTapped;
						button.draggedOff += OnButtonDraggedOff;
						spawnedButtons.Add(button);
						Debug.Log($"[BuildSidebar] ✅ Created button for tower: {tower.towerName}");
					}
				}
			}
			else
			{
				Debug.LogWarning("[BuildSidebar] TowerLibrary is null, cannot create buttons");
			}
		}

		/// <summary>
		/// Clear all existing buttons
		/// </summary>
		private void ClearButtons()
		{
			foreach (TowerSpawnButton button in spawnedButtons)
			{
				if (button != null)
				{
					button.buttonTapped -= OnButtonTapped;
					button.draggedOff -= OnButtonDraggedOff;
					Destroy(button.gameObject);
				}
			}
			spawnedButtons.Clear();
		}

		/// <summary>
		/// Sets the GameUI to build mode with the <see cref="towerData"/>
		/// </summary>
		/// <param name="towerData"></param>
		void OnButtonTapped(Tower towerData)
		{
			var gameUI = GameUI.instance;
			if (gameUI.isBuilding)
			{
				gameUI.CancelGhostPlacement();
			}
			gameUI.SetToBuildMode(towerData);
		}

		/// <summary>
		/// Sets the GameUI to build mode with the <see cref="towerData"/> 
		/// </summary>
		/// <param name="towerData"></param>
		void OnButtonDraggedOff(Tower towerData)
		{
			if (!GameUI.instance.isBuilding)
			{
				GameUI.instance.SetToDragMode(towerData);
			}
		}

		/// <summary>
		/// Unsubscribes from all the tower spawn buttons
		/// </summary>
		void OnDestroy()
		{
			// Unsubscribe from LevelManager event
			if (LevelManager.instanceExists)
			{
				LevelManager.instance.towerLibraryUpdated -= RefreshTowerButtons;
			}

			// Clear all buttons
			ClearButtons();
		}

		/// <summary>
		/// Called by start wave button in scene
		/// </summary>
		public void StartWaveButtonPressed()
		{
			if (LevelManager.instanceExists)
			{
				LevelManager.instance.BuildingCompleted();
			}
		}

		/// <summary>
		/// Debug button to add currency
		/// </summary>
		/// <param name="amount">How much to add</param>
		public void AddCurrency(int amount)
		{
			if (LevelManager.instanceExists)
			{
				LevelManager.instance.currency.AddCurrency(amount);
			}
		}
	}
}