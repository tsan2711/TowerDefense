# Tower Inventory System

## Tổng quan

Hệ thống Inventory quản lý các tower mà người chơi sở hữu và cho phép chọn tối đa 3 tower để sử dụng trong gameplay. Hệ thống được thiết kế theo kiến trúc microservice và tích hợp với Firebase Firestore.

## Kiến trúc

### 1. Data Models (`Services/Data/`)

#### `InventoryItemData`
- Đại diện cho một tower trong inventory của user
- Chứa thông tin: towerName, towerType, isSelected, unlockedAt, usageCount

#### `TowerInventoryData`
- Quản lý toàn bộ inventory của user
- Chứa danh sách các tower sở hữu (`ownedTowers`)
- Hỗ trợ tối đa 3 tower được chọn (`maxSelectedTowers = 3`)
- Methods: `HasTower()`, `AddTower()`, `RemoveTower()`, `SetSelectedTowers()`, etc.

#### `InventoryConfigData`
- Cấu hình cho từng tower (unlock cost, requirements, rarity, etc.)
- Được sync từ backend/Firestore
- Methods: `CanUnlock()`, `GetUnlockStatusMessage()`

### 2. Service Interfaces (`Services/Core/`)

#### `IInventoryService`
- Quản lý inventory của user (CRUD operations)
- Events: `OnInventoryLoaded`, `OnSelectedTowersChanged`
- Key methods:
  - `LoadUserInventoryAsync(userId)` - Load inventory từ Firestore
  - `UnlockTowerAsync(userId, towerName)` - Unlock tower mới
  - `SelectTowersAsync(userId, selectedTowerNames)` - Chọn towers cho gameplay
  - `HasTower(towerName)` - Kiểm tra user có tower hay không

#### `IInventoryConfigService`
- Quản lý cấu hình của inventory items
- Events: `OnInventoryConfigLoaded`
- Key methods:
  - `LoadInventoryConfigAsync()` - Load tất cả configs từ Firestore
  - `GetTowerConfig(towerName)` - Lấy config của một tower
  - `CanUnlockTower(towerName, userLevel, userCurrency)` - Kiểm tra điều kiện unlock
  - `UpdateTowerConfigAsync(config)` - Cập nhật config (admin function)

### 3. Service Implementations (`Services/Firestore/`)

#### `InventoryService`
- Implementation của `IInventoryService`
- Collections: `userInventory` (document ID = userId)
- Caching: Lưu cache local để giảm calls đến Firestore
- Auto-initialize: Tự động tạo inventory mới cho user mới với tower mặc định

#### `InventoryConfigService`
- Implementation của `IInventoryConfigService`
- Collections: `inventoryConfig` (document ID = towerName)
- Caching: Dictionary lookup nhanh bằng towerName
- Default configs: Tự động tạo configs cho 16 towers (6 types × 3 levels + SuperTower)

### 4. Unity ScriptableObject (`Tower/TowerDefense/Towers/Data/`)

#### `TowerInventory`
- ScriptableObject chứa 3 towers được chọn cho gameplay
- Tích hợp với `TowerLibrary` để load tower references
- Methods:
  - `SetSelectedTowersFromNames(List<string>)` - Set towers từ tên
  - `SyncWithInventoryData(TowerInventoryData)` - Sync với backend data
  - `AddTower()`, `RemoveTower()`, `ClearAllTowers()`
- Validation: Tự động giới hạn max 3 towers

## Cách sử dụng

### Setup trong Unity

1. **Đăng ký services trong ServiceLocator:**

```csharp
using Services.Core;
using Services.Firestore;
using Services.Managers;

// In your ServicesBootstrap or initialization script
void RegisterInventoryServices()
{
    var locator = ServiceLocator.Instance;
    
    // Create and register InventoryService
    GameObject inventoryServiceObj = new GameObject("InventoryService");
    var inventoryService = inventoryServiceObj.AddComponent<InventoryService>();
    locator.RegisterService<IInventoryService>(inventoryService);
    
    // Create and register InventoryConfigService
    GameObject configServiceObj = new GameObject("InventoryConfigService");
    var configService = configServiceObj.AddComponent<InventoryConfigService>();
    locator.RegisterService<IInventoryConfigService>(configService);
    
    // Initialize services
    inventoryService.Initialize();
    configService.Initialize();
}
```

2. **Tạo TowerInventory ScriptableObject:**
   - Right-click trong Project window
   - Create → TowerDefense → Tower Inventory
   - Gán reference đến TowerLibrary

### Sử dụng trong Code

#### Load và hiển thị inventory của user:

```csharp
using Services.Core;
using Services.Managers;
using Services.Data;

IInventoryService inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();

async void LoadUserInventory(string userId)
{
    TowerInventoryData inventory = await inventoryService.LoadUserInventoryAsync(userId);
    
    if (inventory != null)
    {
        Debug.Log($"User has {inventory.ownedTowers.Count} towers");
        
        List<string> selectedTowers = inventory.GetSelectedTowerNames();
        Debug.Log($"Selected towers: {string.Join(", ", selectedTowers)}");
    }
}
```

#### Unlock tower mới:

```csharp
async void UnlockTower(string userId, string towerName)
{
    // Check config first
    IInventoryConfigService configService = ServiceLocator.Instance.GetService<IInventoryConfigService>();
    InventoryConfigData config = configService.GetTowerConfig(towerName);
    
    if (config != null && config.CanUnlock(userLevel, userCurrency, completedLevels))
    {
        bool success = await inventoryService.UnlockTowerAsync(userId, towerName);
        
        if (success)
        {
            Debug.Log($"Successfully unlocked {towerName}!");
            // Deduct currency from user
        }
    }
    else
    {
        Debug.Log($"Cannot unlock {towerName}: Requirements not met");
    }
}
```

#### Chọn towers cho gameplay:

```csharp
async void SelectTowersForGame(string userId, List<string> towerNames)
{
    // Validate max 3 towers
    if (towerNames.Count > 3)
    {
        Debug.LogError("Cannot select more than 3 towers!");
        return;
    }
    
    bool success = await inventoryService.SelectTowersAsync(userId, towerNames);
    
    if (success)
    {
        // Update TowerInventory ScriptableObject
        TowerInventoryData data = inventoryService.GetCachedInventory();
        towerInventory.SyncWithInventoryData(data);
        
        Debug.Log("Towers selected for gameplay!");
    }
}
```

#### Sync ScriptableObject với backend data:

```csharp
using TowerDefense.Towers.Data;

public TowerInventory towerInventory; // Assign in Inspector

void SyncInventoryWithBackend()
{
    IInventoryService inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();
    TowerInventoryData backendData = inventoryService.GetCachedInventory();
    
    if (backendData != null)
    {
        towerInventory.SyncWithInventoryData(backendData);
        Debug.Log($"Synced {towerInventory.SelectedCount} towers");
    }
}
```

#### Hiển thị UI shop/unlock:

```csharp
async void DisplayShop()
{
    IInventoryConfigService configService = ServiceLocator.Instance.GetService<IInventoryConfigService>();
    IInventoryService inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();
    
    List<InventoryConfigData> allConfigs = configService.GetCachedConfigs();
    
    foreach (var config in allConfigs)
    {
        bool isOwned = inventoryService.HasTower(config.towerName);
        bool canUnlock = config.CanUnlock(userLevel, userCurrency, completedLevels);
        
        // Update UI
        // - Show lock/unlock status
        // - Show price
        // - Show requirements
        // - Enable/disable purchase button
    }
}
```

### Events

Subscribe to events để cập nhật UI realtime:

```csharp
void Start()
{
    IInventoryService inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();
    
    inventoryService.OnInventoryLoaded += OnInventoryUpdated;
    inventoryService.OnSelectedTowersChanged += OnSelectionChanged;
}

void OnInventoryUpdated(TowerInventoryData inventory)
{
    Debug.Log($"Inventory updated: {inventory.ownedTowers.Count} towers");
    // Refresh UI
}

void OnSelectionChanged(List<string> selectedTowers)
{
    Debug.Log($"Selection changed: {string.Join(", ", selectedTowers)}");
    // Update gameplay UI
}

void OnDestroy()
{
    IInventoryService inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();
    if (inventoryService != null)
    {
        inventoryService.OnInventoryLoaded -= OnInventoryUpdated;
        inventoryService.OnSelectedTowersChanged -= OnSelectionChanged;
    }
}
```

## Firestore Structure

### Collection: `userInventory`

```json
{
  "userId": "user123",
  "maxSelectedTowers": 3,
  "lastUpdated": 1699123456,
  "ownedTowers": [
    {
      "towerName": "MachineGun1",
      "towerType": 2,
      "isSelected": true,
      "unlockedAt": 1699000000,
      "usageCount": 15
    },
    {
      "towerName": "Laser1",
      "towerType": 1,
      "isSelected": true,
      "unlockedAt": 1699050000,
      "usageCount": 8
    }
  ]
}
```

### Collection: `inventoryConfig`

```json
{
  "towerName": "Laser2",
  "towerType": 1,
  "displayName": "Laser II",
  "description": "Enhanced laser with increased damage",
  "unlockCost": 3000,
  "requiredLevel": 10,
  "requiredLevels": ["level_5", "level_6"],
  "isDefaultUnlocked": false,
  "isPurchasable": true,
  "rarity": 2,
  "iconName": "laser2_icon",
  "sortOrder": 4,
  "isActive": true,
  "tags": ["laser", "energy", "precision"]
}
```

## Tower Types (MainTower enum)

- 0: Emp
- 1: Laser
- 2: MachineGun
- 3: Pylon
- 4: Rocket
- 5: SuperTower

## Rarity Levels

- 0: Common
- 1: Rare
- 2: Epic
- 3: Legendary

## Best Practices

1. **Always load configs first** trước khi hiển thị shop/inventory UI
2. **Cache data locally** để giảm Firestore reads
3. **Validate trước khi unlock** để tránh lãng phí currency
4. **Subscribe to events** để UI luôn sync với backend
5. **Sync TowerInventory ScriptableObject** khi user login hoặc inventory thay đổi
6. **Handle offline cases** bằng cách cache data locally

## Extensibility

Để thêm features mới:

1. **Custom unlock conditions**: Mở rộng `InventoryConfigData.CanUnlock()`
2. **Tower upgrades**: Thêm field `upgradeLevel` vào `InventoryItemData`
3. **Tower rental**: Thêm field `rentalExpiry` để tower có thời hạn
4. **Tower crafting**: Thêm collection `craftingRecipes` và service mới
5. **Achievement integration**: Track `usageCount` và trigger achievements

## Troubleshooting

### "Service not initialized"
- Đảm bảo `FirebaseInitializationService` đã initialize trước
- Check Firebase config và permissions

### "Permission denied"
- Cấu hình Firestore Rules trong Firebase Console
- Đảm bảo user đã authenticate

### "Tower not found in TowerLibrary"
- Kiểm tra `towerName` khớp với tên trong TowerLibrary
- Verify TowerLibrary asset đã được assign trong TowerInventory

### Inventory không sync
- Check events đã được subscribe đúng chưa
- Verify `TowerInventory.SyncWithInventoryData()` được gọi sau khi load

## Performance Tips

1. Load configs **một lần** khi app start, cache locally
2. Load user inventory **khi login**, không reload mỗi scene
3. Sử dụng `GetCached...()` methods thay vì async calls khi có thể
4. Batch operations: Update multiple towers cùng lúc thay vì từng tower
5. Firestore indexes: Tạo composite indexes nếu query phức tạp

## Security Notes

- Admin functions (UpdateTowerConfigAsync) cần authentication & authorization
- Validate unlock costs ở backend, không tin client
- Firestore Rules phải kiểm tra user chỉ có thể edit inventory của mình
- Không để client tự set `isDefaultUnlocked` hoặc unlock cost

---

**Created by:** Tower Defense Inventory System
**Version:** 1.0.0
**Last Updated:** November 2025

