#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace TowerDefense.UI.Inventory
{
    /// <summary>
    /// Editor utility to quickly setup Inventory UI in the scene
    /// Usage: GameObject > UI > Tower Inventory UI (Auto Setup)
    /// </summary>
    public class InventoryUISetup : MonoBehaviour
    {
        [MenuItem("GameObject/UI/Tower Inventory UI (Auto Setup)", false, 0)]
        static void CreateInventoryUI(MenuCommand menuCommand)
        {
            // Create Canvas if needed
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Setup canvas scaler
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                Debug.Log("[InventoryUISetup] Created Canvas");
            }
            
            // Create Inventory Panel
            GameObject panelObj = new GameObject("InventoryPanel");
            GameObjectUtility.SetParentAndAlign(panelObj, canvas.gameObject);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            
            // Stretch to fill
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            
            // Add background
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
            
            // Add canvas group for fading
            panelObj.AddComponent<CanvasGroup>();
            
            // Add InventoryUIManager
            InventoryUIManager manager = panelObj.AddComponent<InventoryUIManager>();
            
            // Create Title (optional)
            GameObject titleObj = new GameObject("Title");
            GameObjectUtility.SetParentAndAlign(titleObj, panelObj);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(400, 60);
            titleRect.anchoredPosition = new Vector2(0, -20);
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "TOWER INVENTORY";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            
            // Create Close Button
            GameObject closeButtonObj = new GameObject("CloseButton");
            GameObjectUtility.SetParentAndAlign(closeButtonObj, panelObj);
            RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(60, 60);
            closeRect.anchoredPosition = new Vector2(-20, -20);
            
            Image closeBg = closeButtonObj.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button closeButton = closeButtonObj.AddComponent<Button>();
            
            GameObject closeTextObj = new GameObject("Text");
            GameObjectUtility.SetParentAndAlign(closeTextObj, closeButtonObj);
            RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;
            closeTextRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 32;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;
            
            // Create Selected Slots Container
            GameObject selectedContainer = new GameObject("SelectedSlotsContainer");
            GameObjectUtility.SetParentAndAlign(selectedContainer, panelObj);
            RectTransform selectedRect = selectedContainer.AddComponent<RectTransform>();
            selectedRect.anchorMin = new Vector2(0.5f, 1f);
            selectedRect.anchorMax = new Vector2(0.5f, 1f);
            selectedRect.pivot = new Vector2(0.5f, 1f);
            selectedRect.sizeDelta = new Vector2(600, 180);
            selectedRect.anchoredPosition = new Vector2(0, -100);
            
            HorizontalLayoutGroup selectedLayout = selectedContainer.AddComponent<HorizontalLayoutGroup>();
            selectedLayout.spacing = 20;
            selectedLayout.childAlignment = TextAnchor.MiddleCenter;
            selectedLayout.childForceExpandWidth = false;
            selectedLayout.childForceExpandHeight = false;
            selectedLayout.childControlWidth = false;
            selectedLayout.childControlHeight = false;
            
            // Create Scroll View for Inventory
            GameObject scrollViewObj = new GameObject("InventoryScrollView");
            GameObjectUtility.SetParentAndAlign(scrollViewObj, panelObj);
            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(100, 100); // Left, Bottom
            scrollRect.offsetMax = new Vector2(-100, -300); // Right, Top
            
            ScrollRect scrollComponent = scrollViewObj.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            scrollComponent.movementType = ScrollRect.MovementType.Clamped;
            
            // Create Viewport
            GameObject viewportObj = new GameObject("Viewport");
            GameObjectUtility.SetParentAndAlign(viewportObj, scrollViewObj);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            Image viewportMask = viewportObj.AddComponent<Image>();
            viewportMask.color = new Color(1, 1, 1, 0.01f); // Nearly transparent
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            scrollComponent.viewport = viewportRect;
            
            // Create Content
            GameObject contentObj = new GameObject("Content");
            GameObjectUtility.SetParentAndAlign(contentObj, viewportObj);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);
            contentRect.anchoredPosition = Vector2.zero;
            
            GridLayoutGroup gridLayout = contentObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150, 150);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            
            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollComponent.content = contentRect;
            
            // Create Slot Prefab if not exists
            GameObject slotPrefab = CreateSlotPrefab();
            
            // Assign references to manager
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("selectedSlotsContainer").objectReferenceValue = selectedContainer.transform;
            serializedManager.FindProperty("inventoryGridContainer").objectReferenceValue = contentObj.transform;
            serializedManager.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            serializedManager.FindProperty("closeButton").objectReferenceValue = closeButton;
            serializedManager.FindProperty("inventoryGrid").objectReferenceValue = gridLayout;
            serializedManager.FindProperty("maxSelectedSlots").intValue = 3;
            serializedManager.FindProperty("swapDuration").floatValue = 0.4f;
            serializedManager.FindProperty("swapDelay").floatValue = 0.1f;
            serializedManager.ApplyModifiedProperties();
            
            // Try to find and assign TowerLibrary and UserInventory
            TryAssignScriptableObjects(manager);
            
            // Register undo
            Undo.RegisterCreatedObjectUndo(panelObj, "Create Inventory UI");
            
            // Select the created object
            Selection.activeGameObject = panelObj;
            
            Debug.Log("[InventoryUISetup] ✅ Inventory UI created successfully!");
            Debug.Log("[InventoryUISetup] Please assign TowerLibrary and UserInventory references in InventoryUIManager component.");
        }
        
        static GameObject CreateSlotPrefab()
        {
            // Check if prefab already exists
            string prefabPath = "Assets/_App/Prefabs/UI/TowerSlot.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                Debug.Log("[InventoryUISetup] Using existing TowerSlot prefab");
                return existingPrefab;
            }
            
            // Create new slot prefab
            GameObject slotObj = new GameObject("TowerSlot");
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(150, 150);
            
            // Background
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(1, 1, 1, 0.3f);
            
            // TowerIcon
            GameObject iconObj = new GameObject("TowerIcon");
            GameObjectUtility.SetParentAndAlign(iconObj, slotObj);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(120, 120);
            iconRect.anchoredPosition = Vector2.zero;
            
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            
            // Selection Border
            GameObject borderObj = new GameObject("SelectionBorder");
            GameObjectUtility.SetParentAndAlign(borderObj, slotObj);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-4, -4);
            borderRect.offsetMax = new Vector2(4, 4);
            
            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = Color.green;
            borderImage.raycastTarget = false;
            borderObj.SetActive(false);
            
            // Add TowerInventorySlot component
            TowerInventorySlot slotComponent = slotObj.AddComponent<TowerInventorySlot>();
            
            // Assign references using reflection (since fields are private SerializeField)
            SerializedObject serializedSlot = new SerializedObject(slotComponent);
            serializedSlot.FindProperty("towerIcon").objectReferenceValue = iconImage;
            serializedSlot.FindProperty("backgroundImage").objectReferenceValue = slotBg;
            serializedSlot.FindProperty("selectionBorder").objectReferenceValue = borderImage;
            
            // Set colors
            serializedSlot.FindProperty("normalColor").colorValue = new Color(1f, 1f, 1f, 0.6f);
            serializedSlot.FindProperty("selectedColor").colorValue = new Color(0.3f, 1f, 0.3f, 1f);
            serializedSlot.FindProperty("hoverColor").colorValue = new Color(1f, 1f, 0.5f, 0.8f);
            serializedSlot.FindProperty("emptyColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            serializedSlot.FindProperty("borderSelectedColor").colorValue = Color.green;
            serializedSlot.FindProperty("borderNormalColor").colorValue = new Color(1f, 1f, 1f, 0.3f);
            
            serializedSlot.ApplyModifiedProperties();
            
            // Save as prefab
            string directory = "Assets/_App/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slotObj, prefabPath);
            DestroyImmediate(slotObj);
            
            Debug.Log($"[InventoryUISetup] Created TowerSlot prefab at: {prefabPath}");
            return prefab;
        }
        
        static void TryAssignScriptableObjects(InventoryUIManager manager)
        {
            // Try to find TowerLibrary
            string[] towerLibraryGuids = AssetDatabase.FindAssets("t:TowerLibrary");
            if (towerLibraryGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(towerLibraryGuids[0]);
                TowerDefense.Towers.Data.TowerLibrary towerLibrary = AssetDatabase.LoadAssetAtPath<TowerDefense.Towers.Data.TowerLibrary>(path);
                
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("towerLibrary").objectReferenceValue = towerLibrary;
                serializedManager.ApplyModifiedProperties();
                
                Debug.Log($"[InventoryUISetup] Assigned TowerLibrary: {path}");
            }
            
            // Try to find UserInventoryScriptableObject
            string[] userInventoryGuids = AssetDatabase.FindAssets("t:UserInventoryScriptableObject");
            if (userInventoryGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(userInventoryGuids[0]);
                TowerDefense.Towers.Data.UserInventoryScriptableObject userInventory = AssetDatabase.LoadAssetAtPath<TowerDefense.Towers.Data.UserInventoryScriptableObject>(path);
                
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("userInventory").objectReferenceValue = userInventory;
                serializedManager.ApplyModifiedProperties();
                
                Debug.Log($"[InventoryUISetup] Assigned UserInventory: {path}");
            }
        }
    }
}
#endif


