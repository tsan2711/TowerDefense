# Tower Inventory UI - Quick Start

## Cách nhanh nhất để setup

### Option 1: Auto Setup (Recommended) ⚡

1. **Mở Unity Editor**
2. **Menu: GameObject > UI > Tower Inventory UI (Auto Setup)**
3. **Done!** UI đã được tạo tự động

### Option 2: Manual Setup 🔧

Xem file `SETUP_GUIDE.md` để biết chi tiết.

## Sau khi tạo UI

### 1. Assign references (nếu chưa tự động)
- Mở `InventoryPanel` trong Hierarchy
- Select component `InventoryUIManager`
- Assign:
  - `Tower Library` → Assets/.../TowerLibrary.asset
  - `User Inventory` → Assets/.../UserInventory.asset

### 2. Test trong Play Mode
- Để test, active `InventoryPanel` trong scene
- Hoặc tạo button gọi `inventoryUI.OpenInventory()`

### 3. Tạo button mở Inventory
```csharp
// Cách 1: Dùng InventoryButton component
- Tạo UI Button
- Add component: InventoryButton
- Assign Inventory UI reference

// Cách 2: Code trực tiếp
InventoryUIManager inventoryUI = FindObjectOfType<InventoryUIManager>();
inventoryUI.OpenInventory();
```

## Features

✅ Hiển thị 3 selected towers ở trên  
✅ Grid inventory towers ở dưới  
✅ Click để select/deselect  
✅ Swap animation mượt với DOTween  
✅ Auto sync với backend API  
✅ Color transition cho selection state  
✅ Scale animation khi select  

## Interactions

- **Click tower trong inventory** → Move to empty selected slot
- **Click selected tower** → Highlight (màu xanh)
- **Click selected tower đã highlight** → Deselect
- **Click tower khác khi đã select** → Swap animation
- **Click X button** → Close inventory

## Animation Settings

Có thể tuỳ chỉnh trong Inspector:
- `Swap Duration`: Thời gian swap (default: 0.4s)
- `Swap Ease`: Kiểu easing (default: OutCubic)
- `Color Transition Duration`: Màu chuyển đổi (default: 0.2s)
- `Scale On Select`: Scale khi select (default: 1.1x)

## Troubleshooting

### UI không hiển thị:
- Check Canvas có trong scene không
- Check InventoryPanel có active không

### Icons không hiển thị:
- Check Tower.levels[0].levelData.icon có sprite
- Check TowerLibrary có towers

### API không update:
- Check ServiceLocator có IInventoryService
- Check Firebase connection
- Xem Console logs

## Next Steps

1. ✅ Test với nhiều towers
2. 🎨 Customize colors theo game theme
3. 🔊 Add sound effects
4. ✨ Add particle effects (optional)
5. 🎯 Integrate với gameplay

## Support

Xem thêm:
- `README.md` - Tổng quan hệ thống
- `SETUP_GUIDE.md` - Hướng dẫn chi tiết
- `Scripts/Services/INVENTORY_SYSTEM.md` - Backend system docs


