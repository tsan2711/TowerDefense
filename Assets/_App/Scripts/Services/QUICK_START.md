# Quick Start Guide - Firebase Authentication Service

## Bước 1: Setup Services Bootstrap trong Scene

1. Tạo một GameObject trống tên là `ServicesBootstrap`
2. Thêm component `ServicesBootstrap` vào GameObject đó
3. Component sẽ tự động khởi tạo tất cả services trong `Awake()`

## Bước 2: Setup UI Login Manager

1. Trong scene, tạo Canvas (nếu chưa có): Hierarchy → UI → Canvas
2. Trong Canvas, tạo các UI elements:
   - **Buttons**: 
     - `GoogleSignInButton` - Button để đăng nhập bằng Google
     - `EmailSignInButton` - Button để đăng nhập bằng Email
     - `SignUpButton` - Button để đăng ký tài khoản mới
     - `SignOutButton` - Button để đăng xuất
   - **InputFields**:
     - `EmailInputField` - InputField cho email
     - `PasswordInputField` - InputField cho password (Content Type: Password)
   - **Text**:
     - `StatusText` - Text hiển thị trạng thái/error messages
     - `UserInfoText` - Text hiển thị thông tin user khi đã đăng nhập
   - **Panels (Optional)**:
     - `LoginPanel` - GameObject chứa các UI elements của login (sẽ ẩn khi đã login)
     - `UserInfoPanel` - GameObject chứa thông tin user (sẽ hiện khi đã login)

3. Tạo GameObject cho Login Manager:
   - Hierarchy → Create Empty → đặt tên `UILoginManager`
   - Add Component → `UILoginManager` (Services.UI)

4. Assign các UI references vào UILoginManager component:
   - Kéo thả các buttons, input fields, text, và panels từ Hierarchy vào các slots tương ứng trong Inspector

## Bước 3: Sử dụng Auth Service trong code

```csharp
using Services.Core;
using Services.Managers;

public class YourScript : MonoBehaviour
{
    private IAuthService authService;

    void Start()
    {
        // Lấy auth service từ ServiceLocator
        authService = ServiceLocator.Instance.GetService<IAuthService>();
        
        // Đăng ký events
        authService.OnSignInSuccess += OnSignInSuccess;
        authService.OnSignInFailed += OnSignInFailed;
    }

    async void SignInWithGoogle()
    {
        var result = await authService.SignInWithGoogleAsync();
        if (result.Success)
        {
            Debug.Log($"Logged in: {result.User.Email}");
        }
    }

    async void SignInWithEmail(string email, string password)
    {
        var result = await authService.SignInWithEmailAsync(email, password);
        if (result.Success)
        {
            Debug.Log($"Logged in: {result.User.Email}");
        }
    }

    void OnSignInSuccess(UserInfo user)
    {
        Debug.Log($"User signed in: {user.Email}");
    }

    void OnSignInFailed(string error)
    {
        Debug.LogError($"Sign in failed: {error}");
    }
}
```

## Bước 4: Tích hợp Google Sign-In (Optional)

### Option 1: Sử dụng Google Sign-In Unity Plugin

1. Cài đặt plugin từ Asset Store hoặc Unity Package Manager
2. Cập nhật `GoogleSignInHelper.GetGoogleCredentialAsync()` với code thực tế

### Option 2: Sử dụng Firebase Web-based (cho WebGL)

Implement OAuth flow trong `GoogleSignInHelper`

## Cấu trúc Services

```
Services/
├── Core/               # Interfaces - không cần chỉnh sửa
├── Auth/               # Firebase Auth implementation
├── Managers/           # ServiceLocator và Bootstrap
└── UI/                 # UI Managers (UILoginManager)
```

## Thêm Service mới

Khi cần thêm service mới (ví dụ: Analytics, Storage, etc.):

1. Tạo interface trong `Core/` (ví dụ: `IAnalyticsService.cs`)
2. Tạo implementation trong thư mục riêng (ví dụ: `Analytics/FirebaseAnalyticsService.cs`)
3. Đăng ký trong `ServicesBootstrap.InitializeServices()`

Xem `README.md` để có hướng dẫn chi tiết hơn.

