using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI.Inventory
{
    /// <summary>
    /// Example script showing how to use the Inventory UI system
    /// Attach this to a GameObject with buttons to test functionality
    /// </summary>
    public class InventoryUIExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryUIManager inventoryUI;
        [SerializeField] private Button openButton;
        [SerializeField] private Button refreshButton;
        
        [Header("Auto Find")]
        [SerializeField] private bool autoFindInventoryUI = true;
        
        private void Awake()
        {
            // Auto find InventoryUIManager if not assigned
            if (autoFindInventoryUI && inventoryUI == null)
            {
                inventoryUI = FindObjectOfType<InventoryUIManager>(true);
                
                if (inventoryUI != null)
                {
                    Debug.Log("[InventoryUIExample] Found InventoryUIManager");
                }
                else
                {
                    Debug.LogWarning("[InventoryUIExample] InventoryUIManager not found in scene!");
                }
            }
            
            // Setup button listeners
            if (openButton != null)
            {
                openButton.onClick.AddListener(OpenInventory);
            }
            
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(RefreshInventory);
            }
        }
        
        private void OnDestroy()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(OpenInventory);
            }
            
            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(RefreshInventory);
            }
        }
        
        /// <summary>
        /// Open the inventory UI
        /// </summary>
        public void OpenInventory()
        {
            if (inventoryUI != null)
            {
                Debug.Log("[InventoryUIExample] Opening inventory...");
                inventoryUI.OpenInventory();
            }
            else
            {
                Debug.LogError("[InventoryUIExample] InventoryUIManager not assigned!");
            }
        }
        
        /// <summary>
        /// Refresh inventory data
        /// </summary>
        public void RefreshInventory()
        {
            if (inventoryUI != null)
            {
                Debug.Log("[InventoryUIExample] Refreshing inventory...");
                // Close and reopen to refresh
                inventoryUI.gameObject.SetActive(false);
                inventoryUI.OpenInventory();
            }
        }
        
        /// <summary>
        /// Example: Open inventory from code anywhere
        /// </summary>
        public static void OpenInventoryStatic()
        {
            InventoryUIManager inventoryUI = FindObjectOfType<InventoryUIManager>();
            if (inventoryUI != null)
            {
                inventoryUI.OpenInventory();
            }
            else
            {
                Debug.LogWarning("InventoryUIManager not found in scene!");
            }
        }
        
        // Example: Call from UnityEvent or Button OnClick
        // Just drag this script component into the Button's OnClick event
        // and select InventoryUIExample.OpenInventory()
    }
}

