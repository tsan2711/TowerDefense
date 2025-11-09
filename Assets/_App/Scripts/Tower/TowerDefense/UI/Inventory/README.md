# Tower Inventory UI System

## Overview
Simple and smooth inventory UI system for managing tower selection in the game. Allows players to view owned towers and select up to 3 towers for gameplay with smooth DOTween animations.

## Components

### 1. TowerInventorySlot.cs
Individual slot component that displays a tower sprite with visual feedback for selection state.

**Features:**
- Displays tower icon from TowerLibrary
- Color transitions for selection state (normal, selected, hover, empty)
- Scale animations on selection
- Border highlight when selected
- Click handling
- Smooth DOTween animations

**States:**
- Empty: Slot has no tower
- Normal: Tower present but not selected
- Selected: Tower is selected (green tint + border + scale)

### 2. InventoryUIManager.cs
Main manager that handles the inventory UI layout and logic.

**Features:**
- Displays 3 selected tower slots at the top
- Grid of owned towers (inventory) below
- Click to select/deselect towers
- Swap animations between slots using DOTween
- Move towers from inventory to selected slots
- Swap towers between selected and inventory
- Automatically syncs with IInventoryService
- Calls API to update backend when selection changes

**Layout:**
```
┌─────────────────────────────────────┐
│   Selected Towers (Max 3)           │
│   [Slot 1] [Slot 2] [Slot 3]        │
├─────────────────────────────────────┤
│   Inventory Grid                    │
│   [T1] [T2] [T3] [T4]               │
│   [T5] [T6] [T7] [T8]               │
│   ...                               │
└─────────────────────────────────────┘
```

## Usage

### Setup in Unity Editor

1. **Create Inventory Panel:**
   - Create Canvas > Panel (name it "InventoryPanel")
   - Add InventoryUIManager component
   
2. **Create Selected Slots Container:**
   - Create Empty GameObject under Panel (name: "SelectedSlots")
   - Add HorizontalLayoutGroup component
   - Set spacing, padding
   
3. **Create Inventory Grid Container:**
   - Create Empty GameObject under Panel (name: "InventoryGrid")
   - Add GridLayoutGroup component
   - Set cell size, spacing, constraint
   
4. **Create Slot Prefab:**
   - Create UI > Image (name: "TowerSlot")
   - Add child Image for "TowerIcon"
   - Add child Image for "SelectionBorder"
   - Add TowerInventorySlot component
   - Assign references
   - Save as prefab
   
5. **Configure InventoryUIManager:**
   - Assign selectedSlotsContainer
   - Assign inventoryGridContainer
   - Assign slotPrefab
   - Assign towerLibrary (from Resources)
   - Assign userInventory (ScriptableObject)
   - Set animation settings

### Prefab Structure

```
TowerSlot (Prefab)
├── TowerInventorySlot (Script)
├── Image (Background)
├── TowerIcon (Image)
└── SelectionBorder (Image)
```

### Integration with Services

The UI automatically integrates with:
- `IInventoryService` - for loading and updating inventory
- `IAuthService` - for getting current user ID
- `TowerLibrary` - for tower data and sprites
- `UserInventoryScriptableObject` - fallback data source

### Opening the Inventory

```csharp
InventoryUIManager inventoryUI = FindObjectOfType<InventoryUIManager>();
if (inventoryUI != null)
{
    inventoryUI.OpenInventory();
}
```

Or create a button that calls `OpenInventory()` method.

## Interaction Flow

### Selecting a Tower
1. User clicks on an inventory slot
2. If there's an empty selected slot → tower moves to it
3. If all selected slots are full → user must select a selected tower first

### Swapping Towers
1. User clicks on a selected tower (highlights it)
2. User clicks on another tower (selected or inventory)
3. Smooth swap animation plays
4. API call updates backend
5. UI refreshes after confirmation

### Deselecting
1. User clicks on a highlighted selected tower again
2. Tower deselects (highlight removed)

## Animation Details

### Swap Animation
- Duration: 0.4s (configurable)
- Easing: OutCubic
- Effects:
  - Position interpolation between slots
  - Scale pulse (1.0 → 1.2 → 1.0)
  - Smooth color transition

### Selection Animation
- Duration: 0.15s
- Easing: OutBack
- Effects:
  - Scale up to 1.1x when selected
  - Color shift to green tint
  - Border appears with fade-in

### Appearance Animation
- Duration: 0.3s
- Easing: OutBack
- Effects:
  - Scale from 0 to 1 (pop-in effect)

## Visual Settings

### Colors (Customizable in Inspector)
- Normal Color: White with 60% opacity
- Selected Color: Green (0.3, 1, 0.3, 1)
- Hover Color: Yellow tint (1, 1, 0.5, 0.8)
- Empty Color: Gray (0.5, 0.5, 0.5, 0.3)
- Border Selected: Green
- Border Normal: White with 30% opacity

## Requirements

### Packages
- DOTween (for animations)
- TextMeshPro (optional, for labels)
- Unity UI

### Services
- IInventoryService
- IAuthService
- ServiceLocator

### Data
- TowerLibrary (ScriptableObject)
- UserInventoryScriptableObject
- Tower prefabs with icons

## Notes

- Maximum 3 towers can be selected at once
- Swapping is blocked during animation (isSwapping flag)
- Automatically loads inventory on Start
- Subscribes to inventory events for real-time updates
- Handles empty slots gracefully
- API calls are async with coroutine wrappers

## Future Enhancements

Possible additions:
- Tower info popup on hover
- Drag-and-drop instead of click
- Sound effects for swap/select
- Particle effects on selection
- Filtering/sorting of inventory
- Search functionality
- Tower unlock status display
- Rarity indicators (colors/borders)


