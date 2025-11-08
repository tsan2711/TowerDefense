using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TowerDefense.Towers;
using Services.Data;

namespace TowerDefense.UI.Inventory
{
    /// <summary>
    /// UI component for a single tower inventory slot
    /// Displays tower sprite and handles selection state with visual feedback
    /// </summary>
    public class TowerInventorySlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image towerIcon;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionBorder;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 1f, 0.3f, 1f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        
        [Header("Animation Settings")]
        [SerializeField] private float colorTransitionDuration = 0.2f;
        [SerializeField] private float scaleOnSelect = 1.1f;
        [SerializeField] private float scaleDuration = 0.15f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;
        
        [Header("Border Settings")]
        [SerializeField] private float borderWidth = 4f;
        [SerializeField] private Color borderSelectedColor = Color.green;
        [SerializeField] private Color borderNormalColor = new Color(1f, 1f, 1f, 0.3f);
        
        // State
        private Tower towerData;
        private InventoryItemData inventoryItem;
        private bool isSelected;
        private bool isEmpty = true;
        private Tween currentTween;
        
        // Events
        public System.Action<TowerInventorySlot> OnSlotClicked;
        
        /// <summary>
        /// Get the tower data for this slot
        /// </summary>
        public Tower TowerData => towerData;
        
        /// <summary>
        /// Get the inventory item data
        /// </summary>
        public InventoryItemData InventoryItem => inventoryItem;
        
        /// <summary>
        /// Check if slot is empty
        /// </summary>
        public bool IsEmpty => isEmpty;
        
        /// <summary>
        /// Check if slot is selected
        /// </summary>
        public bool IsSelected => isSelected;
        
        private void Awake()
        {
            // Initialize references if not set
            if (towerIcon == null)
            {
                towerIcon = transform.Find("TowerIcon")?.GetComponent<Image>();
            }
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
            if (selectionBorder == null)
            {
                selectionBorder = transform.Find("SelectionBorder")?.GetComponent<Image>();
            }
            
            // Set initial state
            SetEmpty();
        }
        
        /// <summary>
        /// Initialize slot with tower data
        /// </summary>
        public void Initialize(Tower tower, InventoryItemData item, bool selected = false)
        {
            towerData = tower;
            inventoryItem = item;
            isEmpty = (tower == null);
            isSelected = selected;
            
            if (isEmpty)
            {
                SetEmpty();
            }
            else
            {
                SetTowerVisuals();
                UpdateSelectionState(false); // No animation on init
            }
        }
        
        /// <summary>
        /// Set slot to empty state
        /// </summary>
        public void SetEmpty()
        {
            isEmpty = true;
            towerData = null;
            inventoryItem = null;
            isSelected = false;
            
            if (towerIcon != null)
            {
                towerIcon.enabled = false;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = emptyColor;
            }
            
            if (selectionBorder != null)
            {
                selectionBorder.enabled = false;
            }
        }
        
        /// <summary>
        /// Set tower icon from tower data
        /// </summary>
        private void SetTowerVisuals()
        {
            if (towerIcon != null && towerData != null)
            {
                // Get icon from first level of tower
                if (towerData.levels != null && towerData.levels.Length > 0)
                {
                    Sprite icon = towerData.levels[0].levelData.icon;
                    if (icon != null)
                    {
                        towerIcon.sprite = icon;
                        towerIcon.enabled = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// Update selection state with optional animation
        /// </summary>
        public void SetSelected(bool selected, bool animate = true)
        {
            if (isEmpty)
                return;
                
            isSelected = selected;
            UpdateSelectionState(animate);
        }
        
        /// <summary>
        /// Update visual state based on selection
        /// </summary>
        private void UpdateSelectionState(bool animate)
        {
            if (isEmpty)
                return;
            
            Color targetColor = isSelected ? selectedColor : normalColor;
            Color targetBorderColor = isSelected ? borderSelectedColor : borderNormalColor;
            float targetScale = isSelected ? scaleOnSelect : 1f;
            
            // Kill existing tweens
            currentTween?.Kill();
            
            if (animate)
            {
                // Animate background color
                if (backgroundImage != null)
                {
                    backgroundImage.DOColor(targetColor, colorTransitionDuration);
                }
                
                // Animate scale
                currentTween = transform.DOScale(targetScale, scaleDuration)
                    .SetEase(scaleEase);
                
                // Animate border
                if (selectionBorder != null)
                {
                    selectionBorder.enabled = true;
                    selectionBorder.DOColor(targetBorderColor, colorTransitionDuration);
                }
            }
            else
            {
                // Set immediately without animation
                if (backgroundImage != null)
                {
                    backgroundImage.color = targetColor;
                }
                
                transform.localScale = Vector3.one * targetScale;
                
                if (selectionBorder != null)
                {
                    selectionBorder.enabled = isSelected;
                    selectionBorder.color = targetBorderColor;
                }
            }
        }
        
        /// <summary>
        /// Handle pointer click
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isEmpty)
            {
                OnSlotClicked?.Invoke(this);
                
                // Add punch animation on click
                transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
            }
        }
        
        /// <summary>
        /// Animate slot swap/move to target position
        /// </summary>
        public Tween AnimateMoveTo(Vector3 targetPosition, float duration = 0.3f)
        {
            return transform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic);
        }
        
        /// <summary>
        /// Animate slot appearance
        /// </summary>
        public void AnimateAppearance()
        {
            if (isEmpty)
                return;
            
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
        
        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            currentTween?.Kill();
        }
    }
}

