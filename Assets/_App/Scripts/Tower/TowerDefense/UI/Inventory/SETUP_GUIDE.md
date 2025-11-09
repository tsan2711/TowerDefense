# Tower Inventory UI - Setup Guide

## Hướng dẫn tạo UI trong Unity Editor

### Bước 1: Tạo Inventory Panel

1. **Tạo Canvas chính** (nếu chưa có):
   - Hierarchy > Right-click > UI > Canvas
   - Set Canvas Scaler > UI Scale Mode: "Scale With Screen Size"
   - Reference Resolution: 1920x1080

2. **Tạo Inventory Panel:**
   ```
   Canvas
   └── InventoryPanel (GameObject)
       - RectTransform: Stretch (Fill parent)
       - Add Component: CanvasGroup (để fade in/out)
       - Add Component: Image (background, màu đen alpha 0.8)
   ```

### Bước 2: Tạo Layout Containers

1. **Tạo Selected Slots Container:**
   ```
   InventoryPanel
   └── SelectedSlotsContainer (GameObject)
       - RectTransform: 
         * Anchor: Top Center
         * Width: 600, Height: 180
         * Pos Y: -100
       - Add Component: HorizontalLayoutGroup
         * Spacing: 20
         * Child Alignment: Middle Center
         * Child Force Expand: Width=false, Height=false
   ```

2. **Tạo Inventory Grid Container:**
   ```
   InventoryPanel
   └── InventoryGridContainer (GameObject)
       - RectTransform:
         * Anchor: Stretch (but leave margins)
         * Left: 100, Right: 100
         * Top: 300, Bottom: 100
       - Add Component: ScrollRect
         * Content: (assign Content GameObject below)
         * Horizontal: false
         * Vertical: true
       
       └── Viewport (GameObject)
           - Add Component: Image (enabled)
           - Add Component: Mask (Show Mask Graphic: false)
           
           └── Content (GameObject)
               - RectTransform: Anchor Top-Left with pivot (0.5, 1)
               - Add Component: GridLayoutGroup
                 * Cell Size: 150 x 150
                 * Spacing: 10 x 10
                 * Constraint: Fixed Column Count: 5
                 * Child Alignment: Upper Center
               - Add Component: Content Size Fitter
                 * Vertical Fit: Preferred Size
   ```

3. **Tạo Close Button:**
   ```
   InventoryPanel
   └── CloseButton (UI > Button)
       - RectTransform:
         * Anchor: Top Right
         * Width: 60, Height: 60
         * Pos: (-30, -30)
       - Text: "X" (hoặc icon)
   ```

### Bước 3: Tạo Tower Slot Prefab

1. **Tạo Slot GameObject:**
   ```
   TowerSlot (GameObject)
   - RectTransform: Width=150, Height=150
   - Add Component: Image
     * Color: (1, 1, 1, 0.3)
     * Image Type: Sliced (nếu có sprite 9-slice)
   - Add Component: TowerInventorySlot (Script)
   ```

2. **Tạo Background Image** (con của TowerSlot):
   ```
   └── Background (Image)
       - RectTransform: Stretch to fill parent (0 margins)
       - Color: Sẽ thay đổi theo state (normal/selected/empty)
   ```

3. **Tạo Tower Icon** (con của TowerSlot):
   ```
   └── TowerIcon (Image)
       - RectTransform: 
         * Anchor: Center
         * Width: 120, Height: 120
       - Preserve Aspect: true
       - Raycast Target: false
   ```

4. **Tạo Selection Border** (con của TowerSlot):
   ```
   └── SelectionBorder (Image)
       - RectTransform: Stretch to fill parent (margins: -4)
       - Image Type: Sliced
       - Color: Green (sẽ hiển thị khi selected)
       - Sprite: Border/outline sprite
       - Raycast Target: false
       - Start disabled (sẽ enable khi select)
   ```

5. **Configure TowerInventorySlot component:**
   - Tower Icon: Assign TowerIcon Image
   - Background Image: Assign Background Image
   - Selection Border: Assign SelectionBorder Image
   - Colors:
     * Normal Color: (1, 1, 1, 0.6)
     * Selected Color: (0.3, 1, 0.3, 1)
     * Hover Color: (1, 1, 0.5, 0.8)
     * Empty Color: (0.5, 0.5, 0.5, 0.3)
     * Border Selected Color: (0, 1, 0, 1)
     * Border Normal Color: (1, 1, 1, 0.3)
   - Animation:
     * Color Transition Duration: 0.2
     * Scale On Select: 1.1
     * Scale Duration: 0.15
     * Scale Ease: OutBack

6. **Save as Prefab:**
   - Drag TowerSlot to Project window (Assets/Prefabs/UI/)
   - Delete from Hierarchy

### Bước 4: Configure InventoryUIManager

1. **Add InventoryUIManager component** to InventoryPanel:
   ```
   InventoryPanel
   - Add Component: InventoryUIManager
   ```

2. **Assign references:**
   - Selected Slots Container: Drag SelectedSlotsContainer
   - Inventory Grid Container: Drag Content (inside ScrollRect)
   - Slot Prefab: Drag TowerSlot prefab
   - Close Button: Drag CloseButton
   - Tower Library: Drag from Assets/Resources/TowerLibrary.asset
   - User Inventory: Drag from Assets/Resources/UserInventory.asset
   - Inventory Grid: Drag GridLayoutGroup component from Content

3. **Configure settings:**
   - Max Selected Slots: 3
   - Slot Spacing: 10
   - Swap Duration: 0.4
   - Swap Delay: 0.1
   - Swap Ease: OutCubic
   - Refresh Delay: 0.5

### Bước 5: Tạo Button mở Inventory

1. **Tạo Open Inventory Button** (trong main menu hoặc HUD):
   ```
   MainMenu/HUD
   └── InventoryButton (UI > Button)
       - Text: "Inventory" hoặc icon
       - Add Component: InventoryButton (Script)
   ```

2. **Configure InventoryButton:**
   - Inventory UI: Drag InventoryPanel (hoặc để auto find)
   - Auto Find Inventory UI: true

### Bước 6: Test

1. **Setup test data:**
   - Đảm bảo có TowerLibrary với towers
   - Đảm bảo UserInventory có ownedTowers
   - Đảm bảo ServiceLocator có IInventoryService

2. **Play mode:**
   - Click vào button "Inventory"
   - UI sẽ hiển thị:
     * 3 selected slots ở trên
     * Grid của owned towers ở dưới
   - Click vào towers để swap/select

## Cấu trúc hoàn chỉnh

```
Canvas
└── InventoryPanel
    ├── InventoryUIManager (Script)
    ├── Image (Background)
    ├── CanvasGroup
    ├── CloseButton
    ├── Title (Text - optional)
    ├── SelectedSlotsContainer
    │   └── HorizontalLayoutGroup
    └── ScrollRect
        └── Viewport
            └── Content
                └── GridLayoutGroup
```

## Sprites cần thiết

Bạn cần chuẩn bị các sprites:
1. **Slot Background**: Square sprite (150x150) với 9-slice
2. **Border/Outline**: Border sprite để làm selection border
3. **Tower Icons**: Mỗi tower cần có icon sprite (trong Tower.levels[0].levelData.icon)

## Tips

### Màu sắc theo Rarity (Optional enhancement)
Có thể thêm màu theo độ hiếm:
- Common: White/Gray
- Rare: Blue
- Epic: Purple
- Legendary: Orange/Gold

### Animation Tips
- Dùng DOTween Pro nếu muốn animation phức tạp hơn
- Có thể thêm particle effects khi select tower
- Thêm sound effects cho click/swap

### Performance
- Sử dụng Object Pooling nếu có nhiều towers (>50)
- Lazy load icons nếu cần
- Cache towerLibrary references

## Troubleshooting

### Slot không hiển thị icon:
- Kiểm tra Tower.levels[0].levelData.icon có sprite không
- Kiểm tra TowerLibrary có đúng tower name không

### API không update:
- Kiểm tra IInventoryService đã register trong ServiceLocator chưa
- Kiểm tra Firebase/Backend connection
- Check logs để xem lỗi API

### Animation không smooth:
- Đảm bảo DOTween đã được setup (DOTween Setup trong Tools menu)
- Kiểm tra frame rate
- Reduce animation duration nếu quá chậm

### Swap không hoạt động:
- Kiểm tra isSwapping flag
- Check coroutine có chạy đúng không
- Verify Event System trong scene

## Next Steps

Sau khi setup xong UI:
1. Test với nhiều towers khác nhau
2. Thêm unlock system (hiển thị locked towers)
3. Thêm tower info tooltip
4. Tích hợp với gameplay (spawn selected towers)
5. Add sound effects
6. Polish animations


