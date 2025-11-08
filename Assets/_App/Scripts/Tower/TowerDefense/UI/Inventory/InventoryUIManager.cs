using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private Transform selectedSlotsContainer;
        [SerializeField] private Transform inventoryGridContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button closeButton;
        
        [Header("Layout Settings")]
        [SerializeField] private int maxSelectedSlots = 3;
        [SerializeField] private float slotSpacing = 10f;
        [SerializeField] private GridLayoutGroup inventoryGrid;
        
        [Header("Animation Settings")]
        [SerializeField] private float swapDuration = 0.4f;
        [SerializeField] private float swapDelay = 0.1f;
        [SerializeField] private Ease swapEase = Ease.OutCubic;
        [SerializeField] private float refreshDelay = 0.5f;
        
        [Header("Data References")]
        [SerializeField] private TowerLibrary towerLibrary;
        [SerializeField] private UserInventoryScriptableObject userInventory;
        
        // Services
        private IInventoryService inventoryService;
        private Services.Auth.IAuthService authService;
        
        // Slots
        private List<TowerInventorySlot> selectedSlots = new List<TowerInventorySlot>();
        private List<TowerInventorySlot> inventorySlots = new List<TowerInventorySlot>();
        
        // State
        private TowerInventorySlot currentSelectedSlot;
        private bool isSwapping = false;
        private string currentUserId;
        
        private void Awake()
        {
            // Get services
            inventoryService = ServiceLocator.Instance?.GetService<IInventoryService>();
            authService = ServiceLocator.Instance?.GetService<Services.Auth.IAuthService>();
            
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
        /// Initialize UI with empty slots
        /// </summary>
        private void InitializeUI()
        {
            // Create selected slots (3 slots)
            CreateSelectedSlots();
            
            // Load and display inventory
            LoadInventoryData();
        }
        
        /// <summary>
        /// Create empty selected tower slots
        /// </summary>
        private void CreateSelectedSlots()
        {
            if (selectedSlotsContainer == null || slotPrefab == null)
            {
                Debug.LogError("[InventoryUIManager] Missing references for selected slots!");
                return;
            }
            
            // Clear existing
            foreach (var slot in selectedSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            selectedSlots.Clear();
            
            // Create new slots
            for (int i = 0; i < maxSelectedSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, selectedSlotsContainer);
                TowerInventorySlot slot = slotObj.GetComponent<TowerInventorySlot>();
                
                if (slot != null)
                {
                    slot.Initialize(null, null, false);
                    slot.OnSlotClicked += OnSelectedSlotClicked;
                    selectedSlots.Add(slot);
                }
            }
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
            
            // Try to get cached first
            TowerInventoryData inventoryData = inventoryService.GetCachedInventory();
            
            // If no cache, load from backend
            if (inventoryData == null && !string.IsNullOrEmpty(currentUserId))
            {
                Debug.Log("[InventoryUIManager] Loading inventory from backend...");
                inventoryData = await inventoryService.LoadUserInventoryAsync(currentUserId);
            }
            
            // Fallback to ScriptableObject if service fails
            if (inventoryData == null && userInventory != null)
            {
                Debug.LogWarning("[InventoryUIManager] Using UserInventoryScriptableObject as fallback");
                inventoryData = userInventory.ToInventoryData();
            }
            
            if (inventoryData != null)
            {
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
            if (inventoryData == null || towerLibrary == null)
                return;
            
            // Clear inventory slots
            foreach (var slot in inventorySlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            inventorySlots.Clear();
            
            // Get selected towers
            List<InventoryItemData> selectedTowers = inventoryData.ownedTowers?
                .Where(t => t != null && t.isSelected)
                .ToList() ?? new List<InventoryItemData>();
            
            // Get unselected towers (inventory)
            List<InventoryItemData> unselectedTowers = inventoryData.ownedTowers?
                .Where(t => t != null && !t.isSelected)
                .ToList() ?? new List<InventoryItemData>();
            
            // Update selected slots
            for (int i = 0; i < maxSelectedSlots; i++)
            {
                if (i < selectedSlots.Count)
                {
                    if (i < selectedTowers.Count)
                    {
                        // Get tower data from library
                        Tower towerData = GetTowerFromLibrary(selectedTowers[i].towerName);
                        selectedSlots[i].Initialize(towerData, selectedTowers[i], true);
                        selectedSlots[i].AnimateAppearance();
                    }
                    else
                    {
                        selectedSlots[i].SetEmpty();
                    }
                }
            }
            
            // Create inventory slots
            foreach (var item in unselectedTowers)
            {
                if (inventoryGridContainer != null && slotPrefab != null)
                {
                    GameObject slotObj = Instantiate(slotPrefab, inventoryGridContainer);
                    TowerInventorySlot slot = slotObj.GetComponent<TowerInventorySlot>();
                    
                    if (slot != null)
                    {
                        Tower towerData = GetTowerFromLibrary(item.towerName);
                        slot.Initialize(towerData, item, false);
                        slot.OnSlotClicked += OnInventorySlotClicked;
                        slot.AnimateAppearance();
                        inventorySlots.Add(slot);
                    }
                }
            }
            
            Debug.Log($"[InventoryUIManager] Refreshed UI: {selectedTowers.Count} selected, {unselectedTowers.Count} in inventory");
        }
        
        /// <summary>
        /// Get tower data from library by name
        /// </summary>
        private Tower GetTowerFromLibrary(string towerName)
        {
            if (towerLibrary == null || string.IsNullOrEmpty(towerName))
                return null;
            
            towerLibrary.TryGetValue(towerName, out Tower tower);
            return tower;
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
                // First selection
                currentSelectedSlot = slot;
                slot.SetSelected(true, true);
                Debug.Log($"[InventoryUIManager] Selected tower: {slot.TowerData.towerName}");
            }
            else if (currentSelectedSlot == slot)
            {
                // Deselect
                currentSelectedSlot.SetSelected(false, true);
                currentSelectedSlot = null;
                Debug.Log("[InventoryUIManager] Deselected tower");
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
                TowerInventorySlot emptySlot = selectedSlots.FirstOrDefault(s => s.IsEmpty);
                
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
            
            // Collect selected tower names
            List<string> selectedTowerNames = new List<string>();
            foreach (var slot in selectedSlots)
            {
                if (!slot.IsEmpty && slot.TowerData != null)
                {
                    selectedTowerNames.Add(slot.TowerData.towerName);
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

