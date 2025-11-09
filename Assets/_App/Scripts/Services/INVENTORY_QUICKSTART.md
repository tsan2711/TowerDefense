# Tower Inventory System - Quick Start

## Tóm tắt

Hệ thống quản lý inventory cho Tower Defense game với các tính năng chính:
- ✅ User có thể sở hữu nhiều towers
- ✅ Chọn tối đa **3 towers** để sử dụng trong gameplay
- ✅ Unlock towers với currency và requirements
- ✅ Config từ backend (Firestore)
- ✅ Sync real-time với events

## Files đã tạo

### Core Services
```
Services/Core/
  ├── IInventoryService.cs              # Interface cho user inventory
  └── IInventoryConfigService.cs        # Interface cho tower configs

Services/Firestore/
  ├── InventoryService.cs               # Implementation - quản lý inventory
  └── InventoryConfigService.cs         # Implementation - quản lý configs
```

### Data Models
```
Services/Data/
  ├── InventoryItemData.cs              # Model cho 1 tower item
  ├── TowerInventoryData.cs             # Model cho toàn bộ inventory
  └── InventoryConfigData.cs            # Model cho tower configuration
```

### Unity Integration
```
Tower/TowerDefense/Towers/Data/
  └── TowerInventory.cs                 # ScriptableObject chứa 3 towers selected

Services/Examples/
  └── InventoryExample.cs               # Example script minh họa cách dùng
```

### Documentation
```
Services/
  ├── INVENTORY_SYSTEM.md               # Full documentation
  └── INVENTORY_QUICKSTART.md           # File này
```

## Setup nhanh (3 bước)

### 1. Đăng ký Services

Thêm vào `ServicesBootstrap.cs` hoặc script initialization:

```csharp
using Services.Firestore;
using Services.Managers;

// Create InventoryService
GameObject invServiceObj = new GameObject("InventoryService");
var invService = invServiceObj.AddComponent<InventoryService>();
ServiceLocator.Instance.RegisterService<IInventoryService>(invService);

// Create InventoryConfigService
GameObject configServiceObj = new GameObject("InventoryConfigService");
var configService = configServiceObj.AddComponent<InventoryConfigService>();
ServiceLocator.Instance.RegisterService<IInventoryConfigService>(configService);

// Initialize
ServiceLocator.Instance.InitializeAllServices();
```

### 2. Tạo TowerInventory Asset

1. Right-click trong Project → Create → TowerDefense → Tower Inventory
2. Đặt tên: `PlayerTowerInventory`
3. Gán reference `Tower Library` trong Inspector

### 3. Sử dụng trong Code

```csharp
using Services.Core;
using Services.Managers;

// Get services
var invService = ServiceLocator.Instance.GetService<IInventoryService>();
var configService = ServiceLocator.Instance.GetService<IInventoryConfigService>();

// Load inventory
var inventory = await invService.LoadUserInventoryAsync(userId);

// Unlock tower
await invService.UnlockTowerAsync(userId, "Laser1");

// Select towers (max 3)
await invService.SelectTowersAsync(userId, new List<string> { 
    "MachineGun1", "Laser1", "Rocket1" 
});

// Sync with ScriptableObject
towerInventory.SyncWithInventoryData(invService.GetCachedInventory());
```

## Firestore Collections

System tự động tạo 2 collections:

### `userInventory` (per user)
- Document ID = userId
- Chứa: ownedTowers, selectedTowers, maxSelectedTowers

### `inventoryConfig` (global)
- Document ID = towerName
- Chứa: unlockCost, requiredLevel, rarity, description, etc.
- Auto-initialize với 16 towers mặc định

## Firestore Rules Example

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // User inventory - chỉ owner mới đọc/ghi được
    match /userInventory/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
    
    // Config - tất cả đọc được, chỉ admin ghi được
    match /inventoryConfig/{towerName} {
      allow read: if request.auth != null;
      allow write: if request.auth.token.admin == true;
    }
  }
}
```

## API Methods

### IInventoryService
```csharp
// Load & Query
LoadUserInventoryAsync(userId)          // Load từ Firestore
GetCachedInventory()                    // Get cached local
HasTower(towerName)                     // Check ownership
GetAvailableTowers()                    // List owned towers
GetSelectedTowers()                     // List selected towers (max 3)

// Modify
UnlockTowerAsync(userId, towerName)     // Unlock tower mới
RemoveTowerAsync(userId, towerName)     // Remove tower
SelectTowersAsync(userId, towerNames)   // Select towers (max 3)
InitializeUserInventoryAsync(userId)    // Init new user

// Events
OnInventoryLoaded                       // Fired when loaded
OnSelectedTowersChanged                 // Fired when selection changed
```

### IInventoryConfigService
```csharp
// Load & Query
LoadInventoryConfigAsync()              // Load tất cả configs
LoadTowerConfigAsync(towerName)         // Load 1 tower config
GetTowerConfig(towerName)               // Get cached config
GetUnlockCost(towerName)                // Get cost
CanUnlockTower(...)                     // Check requirements

// Admin
UpdateTowerConfigAsync(config)          // Update config
InitializeCollectionIfEmptyAsync()      // Create default configs

// Events
OnInventoryConfigLoaded                 // Fired when loaded
```

## Default Towers

System tự động tạo configs cho 16 towers:

| Tower Type | Levels | Default Unlocked |
|------------|--------|------------------|
| MachineGun | 1-3    | Level 1 ✓        |
| Laser      | 1-3    | ✗                |
| Rocket     | 1-3    | ✗                |
| EMP        | 1-3    | ✗                |
| Pylon      | 1-3    | ✗                |
| SuperTower | 1      | ✗                |

## Testing

Sử dụng `InventoryExample.cs`:

1. Add component vào GameObject
2. Assign `TowerInventory` trong Inspector
3. Set `testUserId`
4. Gọi public methods từ Inspector hoặc code

Methods có sẵn:
- `LoadInventory()` - Load inventory
- `LoadConfigurations()` - Load configs
- `UnlockTower()` - Unlock tower test
- `SelectFirstThreeTowers()` - Auto select 3 towers
- `CheckUnlockRequirements()` - Check requirements
- `DisplayOwnedTowers()` - Show owned towers
- `InitializeNewUser()` - Init new user

## Next Steps

1. ✅ Setup services trong ServiceLocator
2. ✅ Tạo TowerInventory asset
3. ✅ Test với InventoryExample
4. 🔲 Tạo UI cho shop/inventory
5. 🔲 Integrate với gameplay
6. 🔲 Add analytics tracking
7. 🔲 Configure Firestore Rules

## Support & Documentation

📖 Full docs: `INVENTORY_SYSTEM.md`
💡 Example: `Examples/InventoryExample.cs`
🔧 Services architecture: `MICROSERVICE_ARCHITECTURE.md`

---

**Version:** 1.0.0 | **Created:** November 2025


