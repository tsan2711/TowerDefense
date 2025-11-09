using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI.Inventory
{
    /// <summary>
    /// Simple button component to open/close the inventory UI
    /// Attach this to any UI button to make it open the inventory
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class InventoryButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryUIManager inventoryUI;
        
        [Header("Settings")]
        [SerializeField] private bool autoFindInventoryUI = true;
        
        private Button button;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            
            if (autoFindInventoryUI && inventoryUI == null)
            {
                inventoryUI = FindObjectOfType<InventoryUIManager>(true); // Include inactive
            }
            
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
        
        private void OnButtonClick()
        {
            if (inventoryUI != null)
            {
                inventoryUI.OpenInventory();
            }
            else
            {
                Debug.LogError("[InventoryButton] InventoryUIManager reference is missing!");
            }
        }
    }
}


