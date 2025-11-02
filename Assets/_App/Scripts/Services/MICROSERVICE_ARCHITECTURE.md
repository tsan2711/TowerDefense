# Microservice Architecture Documentation

## Tổng quan

Dự án đã được refactor để tuân thủ **Microservice Architecture Pattern**, mỗi service chỉ phục vụ một domain cụ thể.

## Kiến trúc hiện tại

### Services theo Domain

#### 1. **IAgentConfigurationService** / **AgentConfigurationService**
- **Domain**: Agent Configuration
- **Collection**: `AgentConfigurations`
- **Chức năng**:
  - Load AgentConfiguration data
  - Filter by type
  - Initialize collection với default data

#### 2. **ITowerDataService** / **TowerDataService**
- **Domain**: Tower Level Data
- **Collection**: `TowerLevelData`
- **Chức năng**:
  - Load TowerLevelData
  - Filter by type
  - Initialize collection với default data

#### 3. **ILevelManagementService** / **LevelManagementService**
- **Domain**: Level Management
- **Collections**: 
  - `LevelList` (single document: "main")
  - `LevelLibraryConfig`
- **Chức năng**:
  - Load LevelList
  - Load LevelLibraryConfig
  - Filter by levelId or type
  - Initialize collections với default data

#### 4. **IUserDataService** / **UserDataService**
- **Domain**: User Data
- **Collection**: `users`
- **Chức năng**:
  - Save/Update user data
  - Load user data by UID

### Base Classes

#### **FirestoreServiceBase**
- Base class cho tất cả Firestore services
- Xử lý Firebase initialization chung
- Cung cấp shared logic

### Service Registration

Tất cả services được đăng ký trong `ServicesBootstrap`:

```csharp
// Microservices - following microservice architecture pattern
InitializeAgentConfigurationService();
InitializeTowerDataService();
InitializeLevelManagementService();
InitializeUserDataService();
```

## Firestore Collections Structure

### Tuân thủ Microservice Pattern

Mỗi collection phục vụ một domain cụ thể:

1. **AgentConfigurations**
   - Document ID: padded type (e.g., "00", "01", "02")
   - Fields: `type`, `agentName`, `agentDescription`

2. **TowerLevelData**
   - Document ID: padded type (e.g., "00", "01", ..., "15")
   - Fields: `type`, `description`, `upgradeDescription`, `cost`, `sell`, `maxHealth`, `startingHealth`

3. **LevelList**
   - Document ID: "main"
   - Fields: `levels` (array of level items)

4. **LevelLibraryConfig**
   - Document ID: padded type (e.g., "00", "01")
   - Fields: `type`, `levelId`, `towerLibraryPrefabName`, `towerPrefabTypes`, `description`

5. **users**
   - Document ID: user UID
   - Fields: `uid`, `email`, `displayName`, `photoURL`, `providerId`, `createdAt`, `updatedAt`, `lastLoginAt`

## Backward Compatibility

### Legacy IFirestoreService

`IFirestoreService` và `FirebaseFirestoreService` vẫn được giữ lại để:
- Backward compatibility với code cũ
- Migration dần dần

**Note**: Khuyến nghị sử dụng các microservices riêng biệt thay vì `IFirestoreService`.

## Best Practices

### 1. Service Independence
- Mỗi service độc lập, không phụ thuộc lẫn nhau
- Chỉ chia sẻ base class cho initialization logic

### 2. Single Responsibility
- Mỗi service chỉ xử lý một domain cụ thể
- Không có logic cross-domain trong service

### 3. Service Locator Pattern
- Sử dụng `ServiceLocator` để quản lý services
- Dễ dàng inject dependencies

### 4. Parallel Loading
- Load data từ các services song song để tối ưu performance
- Sử dụng `Task.WhenAll` trong orchestrator

## Migration Guide

### Từ IFirestoreService sang Microservices

**Trước (Legacy)**:
```csharp
IFirestoreService firestoreService = ServiceLocator.Instance.GetService<IFirestoreService>();
await firestoreService.LoadAgentConfigurationsAsync();
```

**Sau (Microservice)**:
```csharp
IAgentConfigurationService agentService = ServiceLocator.Instance.GetService<IAgentConfigurationService>();
await agentService.LoadAgentConfigurationsAsync();
```

## Lợi ích của Microservice Architecture

1. **Scalability**: Dễ dàng thêm service mới cho domain mới
2. **Maintainability**: Mỗi service nhỏ, dễ maintain
3. **Testability**: Dễ test từng service độc lập
4. **Separation of Concerns**: Logic được tách biệt rõ ràng
5. **Performance**: Có thể optimize từng service riêng biệt

## Future Enhancements

Có thể thêm các microservices mới:
- `IAnalyticsService` - Analytics tracking
- `ICloudStorageService` - File storage
- `ILeaderboardService` - Leaderboard management
- `IPurchaseService` - In-app purchases
- etc.

## Kết luận

Hệ thống hiện tại đã tuân thủ **Microservice Architecture Pattern**:
- ✅ Tách biệt services theo domain
- ✅ Collections được tổ chức phù hợp
- ✅ Service independence
- ✅ Single Responsibility Principle
- ✅ Easy to scale and maintain

