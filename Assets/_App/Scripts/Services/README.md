# Services Architecture - Microservice Pattern

Cấu trúc microservice cho Unity dự án, hỗ trợ scale dễ dàng khi thêm các service mới.

## Cấu trúc thư mục

```
Services/
├── Core/                    # Interfaces và models chung
│   ├── IService.cs         # Base interface cho tất cả services
│   └── IAuthService.cs     # Interface cho authentication service
│
├── Auth/                    # Authentication service implementation
│   ├── FirebaseAuthService.cs
│   └── GoogleSignInHelper.cs
│
├── Managers/                # Service management
│   ├── ServiceLocator.cs   # Quản lý tất cả services
│   └── ServicesBootstrap.cs # Khởi tạo services
│
└── Examples/               # Ví dụ sử dụng
    └── AuthServiceExample.cs
```

## Cách sử dụng

### 1. Khởi tạo Services

Thêm `ServicesBootstrap` component vào GameObject trong scene đầu tiên của bạn:

```csharp
// Tự động khởi tạo trong Awake
GameObject bootstrapGO = new GameObject("ServicesBootstrap");
bootstrapGO.AddComponent<ServicesBootstrap>();
```

### 2. Sử dụng Auth Service

```csharp
using Services.Core;
using Services.Managers;

// Lấy auth service
IAuthService authService = ServiceLocator.Instance.GetService<IAuthService>();

// Đăng ký event listeners
authService.OnSignInSuccess += (user) => {
    Debug.Log($"User signed in: {user.Email}");
};

// Đăng nhập với Google
var result = await authService.SignInWithGoogleAsync();

// Đăng nhập với Email/Password
var result = await authService.SignInWithEmailAsync("email@example.com", "password");

// Kiểm tra trạng thái
if (authService.IsAuthenticated)
{
    UserInfo user = authService.CurrentUser;
}

// Lấy token để gọi API
string token = await authService.GetAuthTokenAsync();

// Đăng xuất
await authService.SignOutAsync();
```

## Thêm Service mới

Để thêm một service mới (ví dụ: Analytics Service):

### 1. Tạo Interface

```csharp
// Services/Core/IAnalyticsService.cs
public interface IAnalyticsService : IService
{
    void LogEvent(string eventName, Dictionary<string, object> parameters);
}
```

### 2. Tạo Implementation

```csharp
// Services/Analytics/FirebaseAnalyticsService.cs
public class FirebaseAnalyticsService : MonoBehaviour, IAnalyticsService
{
    public bool IsInitialized { get; private set; }
    
    public void Initialize()
    {
        // Initialize Firebase Analytics
        IsInitialized = true;
    }
    
    public void LogEvent(string eventName, Dictionary<string, object> parameters)
    {
        // Implementation
    }
    
    public void Shutdown()
    {
        // Cleanup
    }
}
```

### 3. Đăng ký trong ServicesBootstrap

```csharp
private void InitializeAnalyticsService()
{
    if (ServiceLocator.Instance.IsServiceRegistered<IAnalyticsService>())
        return;

    GameObject analyticsGO = new GameObject("FirebaseAnalyticsService");
    var analyticsService = analyticsGO.AddComponent<FirebaseAnalyticsService>();
    ServiceLocator.Instance.RegisterService<IAnalyticsService>(analyticsService);
}

// Gọi trong InitializeServices()
InitializeAnalyticsService();
```

## Lưu ý

### Google Sign-In Integration

Bạn cần tích hợp Google Sign-In plugin cho Unity. Có 2 options:

1. **Google Sign-In Unity Plugin** (Khuyên dùng cho mobile)
   - Cài đặt từ Unity Package Manager hoặc Asset Store
   - Implement trong `GoogleSignInHelper.GetGoogleCredentialAsync()`

2. **Firebase Web-based Google Sign-In** (Cho WebGL)
   - Sử dụng OAuth flow qua browser
   - Cần xử lý callback

### Firebase Configuration

Đảm bảo file `google-services.json` (Android) và `GoogleService-Info.plist` (iOS) được đặt trong `StreamingAssets/`.

## Best Practices

1. **Always check service initialization**: `if (service.IsInitialized)`
2. **Use async/await**: Tất cả service methods đều là async
3. **Handle errors**: Luôn xử lý exceptions và check `AuthResult.Success`
4. **Subscribe to events**: Sử dụng events để react với state changes
5. **Service Locator**: Luôn sử dụng ServiceLocator để access services, không tạo instance trực tiếp

## Ví dụ hoàn chỉnh

Xem file `AuthServiceExample.cs` để có ví dụ đầy đủ với UI integration.

