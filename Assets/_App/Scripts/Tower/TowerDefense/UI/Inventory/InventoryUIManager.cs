using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TowerDefense.Towers;
using TowerDefense.Towers.Data;
using Services.Core;
using Services.Data;
using Services.Managers;
using Core.Utilities;

namespace TowerDefense.UI.Inventory
{
    /// <summary>
    /// Manages the Tower Inventory UI
    /// Displays selected towers (max 3) and all owned towers
    /// Handles selection, swapping with smooth DOTween animations
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TowerInventorySlot[] selectedSlots; // Mảng slots đã được assign sẵn trong Inspector
        [SerializeField] private Transform inventoryGridContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button closeButton;
        
        [Header("Layout Settings")]
        [SerializeField] private float slotSpacing = 10f;
        [SerializeField] private GridLayoutGroup inventoryGrid;
        
        [Header("Animation Settings")]
        [SerializeField] private float swapDuration = 0.4f;
        [SerializeField] private float swapDelay = 0.1f;
        [SerializeField] private Ease swapEase = Ease.OutCubic;
        [SerializeField] private float refreshDelay = 0.5f;
        
        [Header("Data References")]
        [SerializeField] private TowerLibraryContainer libraryContainer;
        [SerializeField] private UserInventoryScriptableObject userInventory;
        [SerializeField] private string currentLevelId = "level_1";
        
        // Services
        private IInventoryService inventoryService;
        private IAuthService authService;
        
        // Slots
        private List<TowerInventorySlot> inventorySlots = new List<TowerInventorySlot>();
        
        // State
        private TowerInventorySlot currentSelectedSlot;
        private bool isSwapping = false;
        private string currentUserId;
        
        private void Awake()
        {
            // Get services
            inventoryService = ServiceLocator.Instance?.GetService<IInventoryService>();
            authService = ServiceLocator.Instance?.GetService<IAuthService>();
            
            if (inventoryService == null)
            {
                Debug.LogError("[InventoryUIManager] IInventoryService not found in ServiceLocator!");
            }
            
            if (authService == null)
            {
                Debug.LogError("[InventoryUIManager] IAuthService not found in ServiceLocator!");
            }
            
            // Setup close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }
        }
        
        private void Start()
        {
            // Get current user ID
            if (authService != null && authService.CurrentUser != null)
            {
                currentUserId = authService.CurrentUser.UID;
            }
            
            // Subscribe to inventory events
            if (inventoryService != null)
            {
                inventoryService.OnInventoryLoaded += OnInventoryLoaded;
                inventoryService.OnSelectedTowersChanged += OnSelectedTowersChanged;
            }
            
            // Initialize UI
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (inventoryService != null)
            {
                inventoryService.OnInventoryLoaded -= OnInventoryLoaded;
                inventoryService.OnSelectedTowersChanged -= OnSelectedTowersChanged;
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseInventory);
            }
        }
        
        /// <summary>
        /// Initialize UI with assigned slots
        /// </summary>
        private void InitializeUI()
        {
            // Setup selected slots (đã được assign sẵn trong Inspector)
            if (selectedSlots != null && selectedSlots.Length > 0)
            {
                foreach (var slot in selectedSlots)
                {
                    if (slot != null)
                    {
                        slot.Initialize(null, null, false);
                        slot.OnSlotClicked += OnSelectedSlotClicked;
                    }
                }
                Debug.Log($"[InventoryUIManager] Initialized {selectedSlots.Length} selected slots from Inspector");
            }
            else
            {
                Debug.LogWarning("[InventoryUIManager] No selected slots assigned in Inspector!");
            }
            
            // Load and display inventory
            LoadInventoryData();
        }
        
        /// <summary>
        /// Load inventory data from service
        /// </summary>
        private async void LoadInventoryData()
        {
            if (inventoryService == null)
            {
                Debug.LogError("[InventoryUIManager] Inventory service not available!");
                return;
            }
            
            TowerInventoryData inventoryData = null;
            
            // Luôn load từ backend để đảm bảo Emp1 được kiểm tra và thêm (nếu chưa có)
            if (!string.IsNullOrEmpty(currentUserId))
            {
                Debug.Log("[InventoryUIManager] Loading inventory from backend (to ensure Emp1 is added if missing)...");
                inventoryData = await inventoryService.LoadUserInventoryAsync(currentUserId);
            }
            
            // Fallback to cache if backend load failed
            if (inventoryData == null)
            {
                Debug.LogWarning("[InventoryUIManager] Backend load failed, trying cached inventory...");
                inventoryData = inventoryService.GetCachedInventory();
            }
            
            // Fallback to ScriptableObject if service fails
            if (inventoryData == null && userInventory != null)
            {
                Debug.LogWarning("[InventoryUIManager] Using UserInventoryScriptableObject as fallback");
                inventoryData = userInventory.ToInventoryData();
            }
            
            if (inventoryData != null)
            {
                // Kiểm tra xem có Emp1 không, nếu không thì thêm
                if (inventoryData.ownedTowers != null && !inventoryData.ownedTowers.Any(t => t != null && t.towerName == "Emp1"))
                {
                    Debug.LogWarning("[InventoryUIManager] Emp1 not found in inventory, will be added on next backend sync");
                }
                
                RefreshInventoryDisplay(inventoryData);
            }
            else
            {
                Debug.LogError("[InventoryUIManager] Failed to load inventory data!");
            }
        }
        
        /// <summary>
        /// Refresh inventory display with data
        /// </summary>
        private void RefreshInventoryDisplay(TowerInventoryData inventoryData)
        {
            if (inventoryData == null || libraryContainer == null)
                return;
            
            // Debug: Log all towers in inventory
            if (inventoryData.ownedTowers != null)
            {
                Debug.Log($"[InventoryUIManager] Total towers in inventory: {inventoryData.ownedTowers.Count}");
                foreach (var tower in inventoryData.ownedTowers)
                {
                    if (tower != null)
                    {
                        Debug.Log($"[InventoryUIManager] Tower: {tower.towerName}, Selected: {tower.isSelected}, Type: {tower.towerType}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[InventoryUIManager] ownedTowers is null!");
            }
            
            // Populate sprites for all inventory items from Tower library
            PopulateSpritesForInventoryItems(inventoryData.ownedTowers);
            
            // Get unlocked tower types based on player's maxLevel (same logic as LevelManager)
            HashSet<int> unlockedTowerTypes = GetUnlockedTowerTypes();
            
            // Get selected towers - filter to only include unlocked towers
            List<InventoryItemData> selectedTowers = inventoryData.ownedTowers?
                .Where(t => t != null && t.isSelected && IsTowerUnlocked(t, unlockedTowerTypes))
                .ToList() ?? new List<InventoryItemData>();
            
            // Get unselected towers (inventory) - filter to only include unlocked towers
            List<InventoryItemData> unselectedTowers = inventoryData.ownedTowers?
                .Where(t => t != null && !t.isSelected && IsTowerUnlocked(t, unlockedTowerTypes))
                .ToList() ?? new List<InventoryItemData>();
            
            Debug.Log($"[InventoryUIManager] Selected towers (after unlock filter): {selectedTowers.Count}, Unselected towers (after unlock filter): {unselectedTowers.Count}");
            
            // Update selected slots (duyệt qua mảng đã assign sẵn và đổ dữ liệu vào)
            if (selectedSlots != null && selectedSlots.Length > 0)
            {
                for (int i = 0; i < selectedSlots.Length; i++)
                {
                    if (selectedSlots[i] != null)
                    {
                        if (i < selectedTowers.Count)
                        {
                            // Get tower data from library (use towerType for reliable lookup)
                            Tower towerData = GetTowerFromLibrary(selectedTowers[i].towerName, selectedTowers[i].towerType);
                            if (towerData == null)
                            {
                                Debug.LogError($"[InventoryUIManager] RefreshInventoryDisplay: Cannot find tower '{selectedTowers[i].towerName}' (type={selectedTowers[i].towerType}) for selected slot {i}, setting empty");
                                selectedSlots[i].SetEmpty();
                            }
                            else
                            {
                                // Ensure sprite is populated before initializing slot
                                if (selectedTowers[i].sprite == null)
                                {
                                    PopulateSpriteForInventoryItem(selectedTowers[i], towerData);
                                }
                                selectedSlots[i].Initialize(towerData, selectedTowers[i], true);
                                // Update sprite to ensure it's refreshed (only if tower exists)
                                selectedSlots[i].UpdateSprite();
                                selectedSlots[i].AnimateAppearance();
                            }
                        }
                        else
                        {
                            selectedSlots[i].SetEmpty();
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[InventoryUIManager] Selected slots array is null or empty!");
            }
            
            // Clear inventory slots (these are recreated, so sprites will be set during Initialize)
            foreach (var slot in inventorySlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            inventorySlots.Clear();
            
            // Create inventory slots
            foreach (var item in unselectedTowers)
            {
                if (inventoryGridContainer != null && slotPrefab != null)
                {
                    GameObject slotObj = Instantiate(slotPrefab, inventoryGridContainer);
                    TowerInventorySlot slot = slotObj.GetComponent<TowerInventorySlot>();
                    
                    if (slot != null)
                    {
                        // Get tower data from library (use towerType for reliable lookup)
                        Tower towerData = GetTowerFromLibrary(item.towerName, item.towerType);
                        if (towerData == null)
                        {
                            Debug.LogError($"[InventoryUIManager] RefreshInventoryDisplay: Cannot find tower '{item.towerName}' (type={item.towerType}) for inventory slot, skipping");
                            Destroy(slotObj);
                        }
                        else
                        {
                            // Ensure sprite is populated before initializing slot
                            if (item.sprite == null)
                            {
                                PopulateSpriteForInventoryItem(item, towerData);
                            }
                            slot.Initialize(towerData, item, false);
                            slot.OnSlotClicked += OnInventorySlotClicked;
                            slot.AnimateAppearance();
                            inventorySlots.Add(slot);
                        }
                    }
                }
            }
            
            Debug.Log($"[InventoryUIManager] Refreshed UI: {selectedTowers.Count} selected, {unselectedTowers.Count} in inventory");
        }
        
        /// <summary>
        /// Update sprites for all existing slots (useful when sprites need to be refreshed without recreating slots)
        /// </summary>
        public void UpdateAllSlotSprites()
        {
            // Update selected slots sprites
            if (selectedSlots != null)
            {
                foreach (var slot in selectedSlots)
                {
                    if (slot != null && !slot.IsEmpty)
                    {
                        // Ensure sprite is populated in inventory item
                        if (slot.InventoryItem != null && slot.InventoryItem.sprite == null && slot.TowerData != null)
                        {
                            PopulateSpriteForInventoryItem(slot.InventoryItem, slot.TowerData);
                        }
                        slot.UpdateSprite();
                    }
                }
            }
            
            // Update inventory slots sprites
            foreach (var slot in inventorySlots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    // Ensure sprite is populated in inventory item
                    if (slot.InventoryItem != null && slot.InventoryItem.sprite == null && slot.TowerData != null)
                    {
                        PopulateSpriteForInventoryItem(slot.InventoryItem, slot.TowerData);
                    }
                    slot.UpdateSprite();
                }
            }
        }
        
        /// <summary>
        /// Generate name variations to try when searching for a tower
        /// Handles cases like "MachineGun1" -> "MachineGun", "Emp1" -> "EMP", etc.
        /// </summary>
        private List<string> GenerateTowerNameVariations(string towerName)
        {
            List<string> variations = new List<string>();
            
            if (string.IsNullOrEmpty(towerName))
                return variations;
            
            // Add original name first
            variations.Add(towerName);
            
            // Remove trailing numbers (e.g., "MachineGun1" -> "MachineGun")
            string withoutNumbers = Regex.Replace(towerName, @"\d+$", "");
            if (withoutNumbers != towerName && !variations.Contains(withoutNumbers))
            {
                variations.Add(withoutNumbers);
            }
            
            // Try uppercase variations (e.g., "Emp" -> "EMP", "Emp1" -> "EMP")
            string upperOriginal = towerName.ToUpper();
            if (upperOriginal != towerName && !variations.Contains(upperOriginal))
            {
                variations.Add(upperOriginal);
            }
            
            string upperWithoutNumbers = withoutNumbers.ToUpper();
            if (upperWithoutNumbers != towerName && upperWithoutNumbers != withoutNumbers && !variations.Contains(upperWithoutNumbers))
            {
                variations.Add(upperWithoutNumbers);
            }
            
            // Special case mappings
            Dictionary<string, string> specialMappings = new Dictionary<string, string>
            {
                { "Emp1", "EMP" },
                { "Emp", "EMP" },
                { "MachineGun1", "MachineGun" },
                { "MachineGun", "MachineGun" }
            };
            
            foreach (var mapping in specialMappings)
            {
                if (towerName == mapping.Key && !variations.Contains(mapping.Value))
                {
                    variations.Add(mapping.Value);
                }
            }
            
            return variations;
        }
        
        /// <summary>
        /// Try to find tower in a library using name variations
        /// </summary>
        private Tower TryFindTowerInLibrary(TowerLibrary library, string originalName)
        {
            if (library == null)
                return null;
            
            List<string> variations = GenerateTowerNameVariations(originalName);
            
            foreach (string variation in variations)
            {
                if (library.TryGetValue(variation, out Tower tower))
                {
                    if (variation != originalName)
                    {
                        Debug.Log($"[InventoryUIManager] GetTowerFromLibrary: Found '{originalName}' using variation '{variation}'");
                    }
                    return tower;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get tower by MainTower type from library
        /// Searches all libraries for tower with matching mainTower enum
        /// </summary>
        private Tower GetTowerByMainTowerType(int towerType)
        {
            if (libraryContainer == null)
            {
                Debug.LogError($"[InventoryUIManager] GetTowerByMainTowerType: libraryContainer is NULL!");
                return null;
            }
            
            MainTower targetMainTower = (MainTower)towerType;
            
            // Try current level library first
            if (!string.IsNullOrEmpty(currentLevelId))
            {
                TowerLibrary library = libraryContainer.GetLibrary(currentLevelId);
                if (library != null && library.configurations != null)
                {
                    foreach (var tower in library.configurations)
                    {
                        if (tower != null && tower.mainTower == targetMainTower)
                        {
                            Debug.Log($"[InventoryUIManager] GetTowerByMainTowerType: Found tower with mainTower={targetMainTower} ('{tower.towerName}') in current library levelId={currentLevelId}");
                            return tower;
                        }
                    }
                }
            }
            
            // Fallback: Search all libraries
            List<TowerLibrary> allLibraries = libraryContainer.GetAllLibraries();
            if (allLibraries != null && allLibraries.Count > 0)
            {
                foreach (var library in allLibraries)
                {
                    if (library != null && library.configurations != null)
                    {
                        foreach (var tower in library.configurations)
                        {
                            if (tower != null && tower.mainTower == targetMainTower)
                            {
                                // Find which levelId this library belongs to
                                string foundLevelId = "unknown";
                                var allLevelIds = libraryContainer.GetAllLevelIds();
                                foreach (var levelId in allLevelIds)
                                {
                                    if (libraryContainer.GetLibrary(levelId) == library)
                                    {
                                        foundLevelId = levelId;
                                        break;
                                    }
                                }
                                Debug.Log($"[InventoryUIManager] GetTowerByMainTowerType: Found tower with mainTower={targetMainTower} ('{tower.towerName}') in fallback library levelId={foundLevelId}");
                                return tower;
                            }
                        }
                    }
                }
            }
            
            Debug.LogWarning($"[InventoryUIManager] GetTowerByMainTowerType: Tower with mainTower={targetMainTower} not found in ANY library! Total libraries: {allLibraries?.Count ?? 0}");
            return null;
        }
        
        /// <summary>
        /// Get tower data from library by name or type
        /// Priority: 1) Search by towerName with variations (most accurate), 2) Search by towerType (MainTower enum) as fallback
        /// Tries current level library first, then searches all libraries as fallback
        /// Handles name variations (e.g., "MachineGun1" -> "MachineGun", "Emp1" -> "EMP")
        /// </summary>
        private Tower GetTowerFromLibrary(string towerName, int towerType = -1)
        {
            if (libraryContainer == null)
            {
                Debug.LogError($"[InventoryUIManager] GetTowerFromLibrary: libraryContainer is NULL!");
                return null;
            }
            
            if (string.IsNullOrEmpty(towerName) && towerType < 0)
            {
                Debug.LogError($"[InventoryUIManager] GetTowerFromLibrary: Both towerName and towerType are invalid!");
                return null;
            }
            
            // Priority 1: Try finding by towerName with variations (most accurate - towerName is unique)
            if (!string.IsNullOrEmpty(towerName))
            {
                // Try current level library first
                if (!string.IsNullOrEmpty(currentLevelId))
                {
                    TowerLibrary library = libraryContainer.GetLibrary(currentLevelId);
                    if (library != null)
                    {
                        Tower tower = TryFindTowerInLibrary(library, towerName);
                        if (tower != null)
                        {
                            Debug.Log($"[InventoryUIManager] GetTowerFromLibrary: Found '{towerName}' in current library levelId={currentLevelId}");
                            return tower;
                        }
                        else
                        {
                            // Log available towers in current library for debugging
                            Debug.LogWarning($"[InventoryUIManager] GetTowerFromLibrary: '{towerName}' not found in current library levelId={currentLevelId}");
                            if (library.configurations != null)
                            {
                                string availableTowers = string.Join(", ", library.configurations.Select(t => t?.towerName ?? "NULL"));
                                Debug.Log($"[InventoryUIManager] Available towers in library '{currentLevelId}': {availableTowers}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[InventoryUIManager] GetTowerFromLibrary: Library not found for levelId: {currentLevelId}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[InventoryUIManager] GetTowerFromLibrary: currentLevelId is empty, searching all libraries...");
                }
                
                // Fallback: Search all libraries by name
                List<TowerLibrary> allLibraries = libraryContainer.GetAllLibraries();
                if (allLibraries != null && allLibraries.Count > 0)
                {
                    foreach (var library in allLibraries)
                    {
                        Tower tower = TryFindTowerInLibrary(library, towerName);
                        if (tower != null)
                        {
                            // Find which levelId this library belongs to
                            string foundLevelId = "unknown";
                            var allLevelIds = libraryContainer.GetAllLevelIds();
                            foreach (var levelId in allLevelIds)
                            {
                                if (libraryContainer.GetLibrary(levelId) == library)
                                {
                                    foundLevelId = levelId;
                                    break;
                                }
                            }
                            Debug.Log($"[InventoryUIManager] GetTowerFromLibrary: Found '{towerName}' in fallback library levelId={foundLevelId}");
                            return tower;
                        }
                    }
                }
                
                // If not found by name, try by towerType as fallback
                if (towerType >= 0)
                {
                    Debug.LogWarning($"[InventoryUIManager] GetTowerFromLibrary: '{towerName}' not found by name, trying by towerType={towerType} as fallback...");
                    Tower towerByType = GetTowerByMainTowerType(towerType);
                    if (towerByType != null)
                    {
                        Debug.LogWarning($"[InventoryUIManager] GetTowerFromLibrary: Found tower by type fallback: '{towerByType.towerName}' (mainTower={towerByType.mainTower}) for '{towerName}'");
                        return towerByType;
                    }
                }
                
                Debug.LogError($"[InventoryUIManager] GetTowerFromLibrary: '{towerName}' (towerType={towerType}) not found in ANY library by name or type!");
                return null;
            }
            
            // Priority 2: If towerName is empty, try by towerType only
            if (towerType >= 0)
            {
                Tower towerByType = GetTowerByMainTowerType(towerType);
                if (towerByType != null)
                {
                    return towerByType;
                }
            }
            
            Debug.LogError($"[InventoryUIManager] GetTowerFromLibrary: Both towerName and towerType search failed! towerName='{towerName}', towerType={towerType}");
            return null;
        }
        
        /// <summary>
        /// Populate sprite for a single inventory item from Tower library
        /// </summary>
        private void PopulateSpriteForInventoryItem(InventoryItemData item, Tower tower = null)
        {
            if (item == null || string.IsNullOrEmpty(item.towerName))
            {
                Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: item is NULL or towerName is empty!");
                return;
            }
            
            // Skip if sprite already populated
            if (item.sprite != null)
            {
                Debug.Log($"[InventoryUIManager] PopulateSpriteForInventoryItem: Sprite already populated for {item.towerName}");
                return;
            }
            
            // Use provided tower or get from library
            if (tower == null)
            {
                if (libraryContainer == null)
                {
                    Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: libraryContainer is NULL for {item.towerName}!");
                    return;
                }
                
                if (string.IsNullOrEmpty(currentLevelId))
                {
                    Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: currentLevelId is empty for {item.towerName}!");
                    return;
                }
                
                TowerLibrary library = libraryContainer.GetLibrary(currentLevelId);
                if (library == null)
                {
                    Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: Library not found for levelId={currentLevelId}, towerName={item.towerName}!");
                    return;
                }
                
                if (!library.TryGetValue(item.towerName, out tower))
                {
                    Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: Tower '{item.towerName}' not found in library for levelId={currentLevelId}!");
                    return;
                }
            }
            
            // Get sprite from tower's first level
            if (tower.levels == null || tower.levels.Length == 0)
            {
                Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: Tower {item.towerName} has no levels!");
                return;
            }
            
            if (tower.levels[0].levelData == null)
            {
                Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: Tower {item.towerName}.levels[0].levelData is NULL!");
                return;
            }
            
            if (tower.levels[0].levelData.icon == null)
            {
                Debug.LogError($"[InventoryUIManager] PopulateSpriteForInventoryItem: Tower {item.towerName}.levels[0].levelData.icon is NULL!");
                return;
            }
            
            item.sprite = tower.levels[0].levelData.icon;
            Debug.Log($"[InventoryUIManager] PopulateSpriteForInventoryItem: Successfully populated sprite for {item.towerName}");
        }
        
        /// <summary>
        /// Populate sprite for inventory items from Tower library
        /// Uses GetTowerFromLibrary which has fallback to search all libraries
        /// </summary>
        private void PopulateSpritesForInventoryItems(List<InventoryItemData> items)
        {
            if (items == null)
            {
                Debug.LogError($"[InventoryUIManager] PopulateSpritesForInventoryItems: items list is NULL!");
                return;
            }
            
            if (libraryContainer == null)
            {
                Debug.LogError($"[InventoryUIManager] PopulateSpritesForInventoryItems: libraryContainer is NULL!");
                return;
            }
            
            // Log current state
            string levelIdInfo = string.IsNullOrEmpty(currentLevelId) ? "NOT SET" : currentLevelId;
            Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: Processing {items.Count} items, currentLevelId={levelIdInfo}");
            
            // Log all available level IDs
            var allLevelIds = libraryContainer.GetAllLevelIds();
            if (allLevelIds != null && allLevelIds.Count > 0)
            {
                Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: Available levelIds: {string.Join(", ", allLevelIds)}");
            }
            else
            {
                Debug.LogWarning($"[InventoryUIManager] PopulateSpritesForInventoryItems: No levelIds found in libraryContainer!");
            }
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.towerName))
                {
                    Debug.LogError($"[InventoryUIManager] PopulateSpritesForInventoryItems: Skipping NULL item or empty towerName");
                    failCount++;
                    continue;
                }
                
                // Skip if sprite already populated
                if (item.sprite != null)
                {
                    Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: Sprite already populated for {item.towerName} (towerType={item.towerType})");
                    successCount++;
                    continue;
                }
                
                // Get tower from library (with fallback to all libraries, use towerType for reliable lookup)
                Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: Looking for tower '{item.towerName}' (towerType={item.towerType})...");
                Tower tower = GetTowerFromLibrary(item.towerName, item.towerType);
                if (tower != null)
                {
                    Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: Found tower '{tower.towerName}' (mainTower={tower.mainTower}) for item '{item.towerName}'");
                    
                    // Get sprite from tower's first level
                    if (tower.levels != null && tower.levels.Length > 0 && tower.levels[0].levelData != null)
                    {
                        if (tower.levels[0].levelData.icon != null)
                        {
                            item.sprite = tower.levels[0].levelData.icon;
                            Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: ✅ Successfully populated sprite for '{item.towerName}' -> Using sprite from tower '{tower.towerName}' (sprite name: {tower.levels[0].levelData.icon.name})");
                            successCount++;
                        }
                        else
                        {
                            Debug.LogError($"[InventoryUIManager] PopulateSpritesForInventoryItems: Tower {tower.towerName}.levels[0].levelData.icon is NULL!");
                            failCount++;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[InventoryUIManager] PopulateSpritesForInventoryItems: Tower {tower.towerName} has no levels or levelData is NULL!");
                        failCount++;
                    }
                }
                else
                {
                    Debug.LogError($"[InventoryUIManager] PopulateSpritesForInventoryItems: Tower '{item.towerName}' (towerType={item.towerType}) not found in ANY library!");
                    failCount++;
                }
            }
            
            Debug.Log($"[InventoryUIManager] PopulateSpritesForInventoryItems: Completed - Success: {successCount}, Failed: {failCount}");
        }

        /// <summary>
        /// Change library (level) and set all towers from that library to user inventory
        /// </summary>
        /// <param name="levelId">The level ID to switch to</param>
        public void ChangeLibrary(string levelId)
        {
            if (libraryContainer == null)
            {
                Debug.LogError("[InventoryUIManager] LibraryContainer is null, cannot change library");
                return;
            }

            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogError("[InventoryUIManager] levelId is null or empty");
                return;
            }

            TowerLibrary library = libraryContainer.GetLibrary(levelId);
            if (library == null)
            {
                Debug.LogError($"[InventoryUIManager] Library not found for levelId: {levelId}");
                return;
            }

            // Update current level ID
            currentLevelId = levelId;

            // Set all towers from library to user inventory
            SetLibraryTowersToUserInventory(library);

            // Refresh display
            LoadInventoryData();
        }

        /// <summary>
        /// Set all towers from a library to user inventory
        /// </summary>
        private void SetLibraryTowersToUserInventory(TowerLibrary library)
        {
            if (library == null || userInventory == null)
            {
                Debug.LogError("[InventoryUIManager] Library or UserInventory is null");
                return;
            }

            if (inventoryService == null || string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("[InventoryUIManager] InventoryService or userId not available, updating ScriptableObject only");
                SetLibraryTowersToScriptableObject(library);
                return;
            }

            // Get all towers from library
            List<Tower> libraryTowers = new List<Tower>();
            if (library.configurations != null)
            {
                libraryTowers = library.configurations.Where(t => t != null).ToList();
            }

            if (libraryTowers.Count == 0)
            {
                Debug.LogWarning("[InventoryUIManager] Library has no towers");
                return;
            }

            // Create inventory items from library towers
            List<InventoryItemData> inventoryItems = new List<InventoryItemData>();
            foreach (var tower in libraryTowers)
            {
                // Check if user already owns this tower
                InventoryItemData existingItem = userInventory.GetTower(tower.towerName);
                
                if (existingItem != null)
                {
                    // Keep existing item (preserve selection state, usage count, etc.)
                    // Populate sprite if not already set
                    if (existingItem.sprite == null && tower.levels != null && tower.levels.Length > 0 && tower.levels[0].levelData != null)
                    {
                        existingItem.sprite = tower.levels[0].levelData.icon;
                    }
                    inventoryItems.Add(existingItem);
                }
                else
                {
                    // Create new inventory item
                    InventoryItemData newItem = new InventoryItemData
                    {
                        towerName = tower.towerName,
                        towerType = (int)tower.mainTower,
                        isSelected = false,
                        unlockedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        usageCount = 0
                    };
                    
                    // Get sprite from tower's first level
                    if (tower.levels != null && tower.levels.Length > 0 && tower.levels[0].levelData != null)
                    {
                        newItem.sprite = tower.levels[0].levelData.icon;
                    }
                    
                    inventoryItems.Add(newItem);
                }
            }

            // Update user inventory ScriptableObject
            userInventory.ownedTowers = inventoryItems;
            userInventory.lastUpdated = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(userInventory);
#endif

            // Update backend via service - unlock new towers
            StartCoroutine(UnlockNewTowersToBackend(libraryTowers));

            Debug.Log($"[InventoryUIManager] Set {inventoryItems.Count} towers from library to user inventory");
        }

        /// <summary>
        /// Set library towers to ScriptableObject only (fallback when service not available)
        /// </summary>
        private void SetLibraryTowersToScriptableObject(TowerLibrary library)
        {
            if (library == null || userInventory == null)
                return;

            List<Tower> libraryTowers = new List<Tower>();
            if (library.configurations != null)
            {
                libraryTowers = library.configurations.Where(t => t != null).ToList();
            }

            List<InventoryItemData> inventoryItems = new List<InventoryItemData>();
            foreach (var tower in libraryTowers)
            {
                InventoryItemData existingItem = userInventory.GetTower(tower.towerName);
                
                if (existingItem != null)
                {
                    // Populate sprite if not already set
                    if (existingItem.sprite == null && tower.levels != null && tower.levels.Length > 0 && tower.levels[0].levelData != null)
                    {
                        existingItem.sprite = tower.levels[0].levelData.icon;
                    }
                    inventoryItems.Add(existingItem);
                }
                else
                {
                    InventoryItemData newItem = new InventoryItemData
                    {
                        towerName = tower.towerName,
                        towerType = (int)tower.mainTower,
                        isSelected = false,
                        unlockedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        usageCount = 0
                    };
                    
                    // Get sprite from tower's first level
                    if (tower.levels != null && tower.levels.Length > 0 && tower.levels[0].levelData != null)
                    {
                        newItem.sprite = tower.levels[0].levelData.icon;
                    }
                    
                    inventoryItems.Add(newItem);
                }
            }

            userInventory.ownedTowers = inventoryItems;
            userInventory.lastUpdated = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(userInventory);
#endif
        }

        /// <summary>
        /// Unlock new towers to backend
        /// </summary>
        private IEnumerator UnlockNewTowersToBackend(List<Tower> libraryTowers)
        {
            if (inventoryService == null || string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("[InventoryUIManager] Cannot unlock towers: missing service or userId");
                yield break;
            }

            if (libraryTowers == null || libraryTowers.Count == 0)
            {
                yield break;
            }

            Debug.Log($"[InventoryUIManager] Unlocking {libraryTowers.Count} towers to backend...");

            int successCount = 0;
            int failCount = 0;

            foreach (var tower in libraryTowers)
            {
                if (tower == null || string.IsNullOrEmpty(tower.towerName))
                    continue;

                // Check if user already owns this tower
                if (userInventory.HasTower(tower.towerName))
                {
                    continue; // Skip if already owned
                }

                // Unlock tower
                var task = inventoryService.UnlockTowerAsync(currentUserId, tower.towerName);
                yield return new WaitUntil(() => task.IsCompleted);

                if (task.Result)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"[InventoryUIManager] Failed to unlock tower: {tower.towerName}");
                }
            }

            if (successCount > 0)
            {
                Debug.Log($"[InventoryUIManager] ✅ Successfully unlocked {successCount} towers to backend");
            }

            if (failCount > 0)
            {
                Debug.LogError($"[InventoryUIManager] ❌ Failed to unlock {failCount} towers");
            }

            // Reload inventory after unlocking
            yield return new WaitForSeconds(0.5f);
            LoadInventoryData();
        }
        
        /// <summary>
        /// Handle click on selected slot
        /// </summary>
        private void OnSelectedSlotClicked(TowerInventorySlot slot)
        {
            if (isSwapping || slot.IsEmpty)
                return;
            
            if (currentSelectedSlot == null)
            {
                // First selection - highlight the slot
                currentSelectedSlot = slot;
                slot.SetSelected(true, true);
                Debug.Log($"[InventoryUIManager] Selected tower: {slot.TowerData.towerName}");
            }
            else if (currentSelectedSlot == slot)
            {
                // Click again on same slot - unselect tower (move to inventory)
                currentSelectedSlot.SetSelected(false, true);
                currentSelectedSlot = null;
                Debug.Log("[InventoryUIManager] Unselecting tower (moving to inventory)");
                StartCoroutine(MoveToInventory(slot));
            }
            else
            {
                // Swap with another selected slot
                StartCoroutine(SwapSelectedSlots(currentSelectedSlot, slot));
            }
        }
        
        /// <summary>
        /// Handle click on inventory slot
        /// </summary>
        private void OnInventorySlotClicked(TowerInventorySlot slot)
        {
            if (isSwapping || slot.IsEmpty)
                return;
            
            if (currentSelectedSlot == null)
            {
                // Check if there's an empty selected slot
                TowerInventorySlot emptySlot = null;
                if (selectedSlots != null)
                {
                    foreach (var s in selectedSlots)
                    {
                        if (s != null && s.IsEmpty)
                        {
                            emptySlot = s;
                            break;
                        }
                    }
                }
                
                if (emptySlot != null)
                {
                    // Move to empty slot
                    StartCoroutine(MoveToSelectedSlot(slot, emptySlot));
                }
                else
                {
                    // No empty slot, user must select a slot to swap
                    Debug.Log("[InventoryUIManager] Select a tower from selected slots to swap");
                }
            }
            else
            {
                // Swap with selected slot
                StartCoroutine(SwapBetweenSelectedAndInventory(currentSelectedSlot, slot));
            }
        }
        
        /// <summary>
        /// Swap two towers in selected slots
        /// </summary>
        private IEnumerator SwapSelectedSlots(TowerInventorySlot slot1, TowerInventorySlot slot2)
        {
            isSwapping = true;
            
            // Visual feedback
            slot1.SetSelected(false, true);
            slot2.SetSelected(false, true);
            
            // Get positions
            Vector3 pos1 = slot1.transform.position;
            Vector3 pos2 = slot2.transform.position;
            
            // Animate swap
            Sequence swapSequence = DOTween.Sequence();
            swapSequence.Append(slot1.transform.DOMove(pos2, swapDuration).SetEase(swapEase));
            swapSequence.Join(slot2.transform.DOMove(pos1, swapDuration).SetEase(swapEase));
            swapSequence.Join(slot1.transform.DOScale(1.2f, swapDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
            swapSequence.Join(slot2.transform.DOScale(1.2f, swapDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
            
            yield return swapSequence.WaitForCompletion();
            
            // Swap in hierarchy (to maintain proper order)
            int index1 = slot1.transform.GetSiblingIndex();
            int index2 = slot2.transform.GetSiblingIndex();
            slot1.transform.SetSiblingIndex(index2);
            slot2.transform.SetSiblingIndex(index1);
            
            // Reset positions
            slot1.transform.position = pos1;
            slot2.transform.position = pos2;
            
            // Update backend
            yield return StartCoroutine(UpdateSelectedTowersToBackend());
            
            currentSelectedSlot = null;
            isSwapping = false;
        }
        
        /// <summary>
        /// Move tower from inventory to empty selected slot
        /// </summary>
        private IEnumerator MoveToSelectedSlot(TowerInventorySlot inventorySlot, TowerInventorySlot selectedSlot)
        {
            isSwapping = true;
            
            // Store data
            Tower towerData = inventorySlot.TowerData;
            InventoryItemData itemData = inventorySlot.InventoryItem;
            
            // Animate move
            Vector3 targetPos = selectedSlot.transform.position;
            Sequence moveSequence = DOTween.Sequence();
            moveSequence.Append(inventorySlot.transform.DOMove(targetPos, swapDuration).SetEase(swapEase));
            moveSequence.Join(inventorySlot.transform.DOScale(1.3f, swapDuration * 0.5f));
            
            yield return moveSequence.WaitForCompletion();
            
            // Update UI
            selectedSlot.Initialize(towerData, itemData, true);
            selectedSlot.AnimateAppearance();
            
            // Remove from inventory
            inventorySlots.Remove(inventorySlot);
            Destroy(inventorySlot.gameObject);
            
            // Update backend
            yield return StartCoroutine(UpdateSelectedTowersToBackend());
            
            isSwapping = false;
        }
        
        /// <summary>
        /// Move tower from selected slot to inventory (unselect)
        /// </summary>
        private IEnumerator MoveToInventory(TowerInventorySlot selectedSlot)
        {
            isSwapping = true;
            
            // Store data
            Tower towerData = selectedSlot.TowerData;
            InventoryItemData itemData = selectedSlot.InventoryItem;
            
            if (towerData == null || itemData == null)
            {
                Debug.LogError("[InventoryUIManager] Cannot move to inventory: tower data is null");
                isSwapping = false;
                yield break;
            }
            
            // Create inventory slot
            if (inventoryGridContainer == null || slotPrefab == null)
            {
                Debug.LogError("[InventoryUIManager] Cannot move to inventory: missing inventory container or prefab");
                isSwapping = false;
                yield break;
            }
            
            GameObject slotObj = Instantiate(slotPrefab, inventoryGridContainer);
            TowerInventorySlot newInventorySlot = slotObj.GetComponent<TowerInventorySlot>();
            
            if (newInventorySlot == null)
            {
                Debug.LogError("[InventoryUIManager] Cannot move to inventory: failed to create slot");
                Destroy(slotObj);
                isSwapping = false;
                yield break;
            }
            
            // Position at selected slot first (for animation)
            slotObj.transform.position = selectedSlot.transform.position;
            
            // Initialize inventory slot
            newInventorySlot.Initialize(towerData, itemData, false);
            newInventorySlot.OnSlotClicked += OnInventorySlotClicked;
            
            // Animate move to inventory grid
            // Wait for layout to update
            yield return null;
            
            Vector3 targetPos = newInventorySlot.transform.position;
            Sequence moveSequence = DOTween.Sequence();
            moveSequence.Append(slotObj.transform.DOMove(targetPos, swapDuration).SetEase(swapEase));
            moveSequence.Join(slotObj.transform.DOScale(0.8f, swapDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
            
            yield return moveSequence.WaitForCompletion();
            
            // Add to inventory list
            inventorySlots.Add(newInventorySlot);
            newInventorySlot.AnimateAppearance();
            
            // Clear selected slot
            selectedSlot.SetEmpty();
            
            // Update backend
            yield return StartCoroutine(UpdateSelectedTowersToBackend());
            
            isSwapping = false;
            
            Debug.Log($"[InventoryUIManager] Moved tower '{itemData.towerName}' to inventory");
        }
        
        /// <summary>
        /// Swap tower between selected slot and inventory slot
        /// </summary>
        private IEnumerator SwapBetweenSelectedAndInventory(TowerInventorySlot selectedSlot, TowerInventorySlot inventorySlot)
        {
            isSwapping = true;
            
            // Visual feedback
            selectedSlot.SetSelected(false, true);
            
            // Store data
            Tower selectedTower = selectedSlot.TowerData;
            InventoryItemData selectedItem = selectedSlot.InventoryItem;
            Tower inventoryTower = inventorySlot.TowerData;
            InventoryItemData inventoryItem = inventorySlot.InventoryItem;
            
            // Get positions
            Vector3 selectedPos = selectedSlot.transform.position;
            Vector3 inventoryPos = inventorySlot.transform.position;
            
            // Create temp visual for inventory slot to animate
            GameObject tempObj = Instantiate(inventorySlot.gameObject, inventorySlot.transform.parent);
            tempObj.transform.position = inventoryPos;
            TowerInventorySlot tempSlot = tempObj.GetComponent<TowerInventorySlot>();
            
            // Animate swap
            Sequence swapSequence = DOTween.Sequence();
            swapSequence.Append(selectedSlot.transform.DOMove(inventoryPos, swapDuration).SetEase(swapEase));
            swapSequence.Join(tempSlot.transform.DOMove(selectedPos, swapDuration).SetEase(swapEase));
            swapSequence.Join(selectedSlot.transform.DOScale(0.8f, swapDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
            swapSequence.Join(tempSlot.transform.DOScale(1.2f, swapDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
            
            yield return swapSequence.WaitForCompletion();
            
            // Update data
            selectedSlot.Initialize(inventoryTower, inventoryItem, true);
            selectedSlot.transform.position = selectedPos;
            
            inventorySlot.Initialize(selectedTower, selectedItem, false);
            inventorySlot.transform.position = inventoryPos;
            
            // Cleanup temp
            Destroy(tempObj);
            
            // Update backend
            yield return StartCoroutine(UpdateSelectedTowersToBackend());
            
            currentSelectedSlot = null;
            isSwapping = false;
        }
        
        /// <summary>
        /// Update selected towers to backend via API
        /// </summary>
        private IEnumerator UpdateSelectedTowersToBackend()
        {
            if (inventoryService == null || string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("[InventoryUIManager] Cannot update backend: missing service or userId");
                yield break;
            }
            
            // Get current cached inventory to determine actual selected towers
            TowerInventoryData cachedInventory = inventoryService.GetCachedInventory();
            if (cachedInventory == null)
            {
                Debug.LogWarning("[InventoryUIManager] Cannot update backend: no cached inventory");
                yield break;
            }
            
            // Collect selected tower names from selectedSlots UI
            // IMPORTANT: Use InventoryItem.towerName (backend name like "MachineGun1", "Emp1")
            // NOT TowerData.towerName (prefab name like "EMP Generator", "Assault Cannon")
            List<string> selectedTowerNames = new List<string>();
            if (selectedSlots != null)
            {
                foreach (var slot in selectedSlots)
                {
                    if (slot != null && !slot.IsEmpty && slot.InventoryItem != null)
                    {
                        // Use InventoryItem.towerName which matches backend data
                        selectedTowerNames.Add(slot.InventoryItem.towerName);
                        Debug.Log($"[InventoryUIManager] UpdateSelectedTowersToBackend: Adding tower '{slot.InventoryItem.towerName}' to selection");
                    }
                }
            }
            
            // Update cached inventory's selected state to match UI BEFORE calling backend
            // This ensures validation will pass
            if (cachedInventory.ownedTowers != null)
            {
                foreach (var tower in cachedInventory.ownedTowers)
                {
                    tower.isSelected = selectedTowerNames.Contains(tower.towerName);
                }
            }
            
            Debug.Log($"[InventoryUIManager] Updating selected towers to backend: {string.Join(", ", selectedTowerNames)}");
            
            // Call API
            bool success = false;
            var task = inventoryService.SelectTowersAsync(currentUserId, selectedTowerNames);
            yield return new WaitUntil(() => task.IsCompleted);
            success = task.Result;
            
            if (success)
            {
                Debug.Log("[InventoryUIManager] ✅ Successfully updated selected towers");
                
                // Wait a bit before refreshing
                yield return new WaitForSeconds(refreshDelay);
                
                // Refresh display
                TowerInventoryData updatedData = inventoryService.GetCachedInventory();
                if (updatedData != null)
                {
                    RefreshInventoryDisplay(updatedData);
                }
            }
            else
            {
                Debug.LogError("[InventoryUIManager] ❌ Failed to update selected towers");
            }
        }
        
        /// <summary>
        /// Event handler when inventory is loaded
        /// </summary>
        private void OnInventoryLoaded(TowerInventoryData inventory)
        {
            Debug.Log("[InventoryUIManager] Inventory loaded event received");
            RefreshInventoryDisplay(inventory);
        }
        
        /// <summary>
        /// Get unlocked tower types based on player's maxLevel (same logic as LevelManager)
        /// Level 1: Machine Gun
        /// Level 2: Machine Gun + Rocket
        /// Level 3: Machine Gun + Rocket + Emp
        /// Level 4: Machine Gun + Rocket + Emp + Laser
        /// </summary>
        private HashSet<int> GetUnlockedTowerTypes()
        {
            HashSet<int> unlockedTypes = new HashSet<int>();
            
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
            
            Debug.Log($"[InventoryUIManager] Unlocked towers for maxLevel {maxLevel}: {string.Join(", ", unlockedTypes)}");
            return unlockedTypes;
        }
        
        /// <summary>
        /// Check if a tower is unlocked based on its towerType
        /// </summary>
        private bool IsTowerUnlocked(InventoryItemData item, HashSet<int> unlockedTowerTypes)
        {
            if (item == null || unlockedTowerTypes == null)
                return false;
            
            // Check by towerType first (most reliable)
            if (unlockedTowerTypes.Contains(item.towerType))
            {
                return true;
            }
            
            // Fallback: Try to get tower from library and check mainTower
            Tower tower = GetTowerFromLibrary(item.towerName, item.towerType);
            if (tower != null)
            {
                return unlockedTowerTypes.Contains((int)tower.mainTower);
            }
            
            // If can't determine, assume unlocked (to avoid hiding valid towers)
            Debug.LogWarning($"[InventoryUIManager] Cannot determine if tower '{item.towerName}' (towerType={item.towerType}) is unlocked, assuming unlocked");
            return true;
        }
        
        /// <summary>
        /// Event handler when selected towers changed
        /// </summary>
        private void OnSelectedTowersChanged(List<string> selectedTowers)
        {
            Debug.Log($"[InventoryUIManager] Selected towers changed: {string.Join(", ", selectedTowers)}");
        }
        
        /// <summary>
        /// Close inventory UI
        /// </summary>
        private void CloseInventory()
        {
            // Add close animation if needed
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Open inventory UI
        /// </summary>
        public void OpenInventory()
        {
            gameObject.SetActive(true);
            LoadInventoryData();
        }
    }
}

