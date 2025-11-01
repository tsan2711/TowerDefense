# Hướng dẫn cài đặt Firebase Firestore cho Unity

## ⚠️ Trạng thái hiện tại

**Code đã được cập nhật để có thể compile mà không cần Firebase Firestore package!**

Code hiện sử dụng conditional compilation (`#if FIREBASE_FIRESTORE_AVAILABLE`) để:
- ✅ Code có thể compile ngay bây giờ mà không bị lỗi
- ✅ Các chức năng Firestore sẽ trả về empty data và log warning khi package chưa được cài đặt
- ✅ Khi package được cài đặt, script editor sẽ tự động kích hoạt chức năng Firestore

## Vấn đề (nếu chưa được fix)

Code có thể báo lỗi:
- `error CS0234: The type or namespace name 'Firestore' does not exist in the namespace 'Firebase'`
- `error CS0246: The type or namespace name 'FirebaseFirestore' could not be found`

## Nguyên nhân
Thiếu Firebase Firestore package trong project. Hiện tại project chỉ có:
- ✅ Firebase.App
- ✅ Firebase.Auth
- ✅ Firebase.AppCheck
- ✅ Firebase.Storage
- ❌ Firebase.Firestore (THIẾU)

## Giải pháp tạm thời (đã được áp dụng)

Code hiện tại đã sử dụng conditional compilation để:
1. Wrap tất cả code Firestore trong `#if FIREBASE_FIRESTORE_AVAILABLE`
2. Trả về empty data khi package chưa được cài đặt
3. Script editor tự động quản lý define symbol `FIREBASE_FIRESTORE_AVAILABLE`

**Define symbol sẽ tự động được thêm/xóa khi Unity Editor phát hiện package được cài đặt hoặc gỡ bỏ.**

## Cách khắc phục

### Bước 1: Tải Firebase Unity SDK
1. Truy cập: https://firebase.google.com/download/unity
2. Đăng nhập với tài khoản Google của bạn
3. Chọn project Firebase cần sử dụng
4. Tải về file `firebase_unity_sdk_X.X.X.zip`

### Bước 2: Import Firestore Package vào Unity
1. Giải nén file zip vừa tải
2. Trong Unity Editor:
   - Vào menu `Assets` → `Import Package` → `Custom Package...`
   - Tìm và chọn file `FirebaseFirestore.unitypackage` từ thư mục đã giải nén
   - Chọn `Import` và đợi quá trình import hoàn tất

### Bước 3: Kiểm tra
Sau khi import, kiểm tra:
- Trong `Assets/Firebase/Plugins/` phải có `Firebase.Firestore.dll`
- Trong `Assets/Firebase/Editor/` phải có `FirestoreDependencies.xml`

### Bước 4: Define Symbol tự động
Script editor (`Assets/Editor/FirebaseFirestoreChecker.cs`) sẽ tự động:
- ✅ Phát hiện khi package được cài đặt
- ✅ Tự động thêm define symbol `FIREBASE_FIRESTORE_AVAILABLE` vào Player Settings
- ✅ Tự động xóa define symbol khi package bị gỡ bỏ

**Bạn không cần làm gì thêm - Unity sẽ tự động compile lại code với Firestore support!**

### Bước 5: Kiểm tra thủ công (tùy chọn)
Nếu muốn kiểm tra trạng thái cài đặt:
- Menu: `Tools` → `Firebase` → `Check Firestore Installation`
- Hoặc kiểm tra trong Unity Console log khi mở project

### Bước 6: Reimport Scripts (nếu cần)
- Unity sẽ tự động reimport scripts
- Nếu vẫn còn lỗi, thử:
  - `Assets` → `Reimport All`
  - Hoặc đóng và mở lại Unity Editor

## Lưu ý quan trọng

### Version tương thích
- Đảm bảo version của Firebase Firestore package tương thích với các Firebase packages khác
- Hiện tại project đang dùng version 13.4.0 cho các package khác (Auth, AppCheck, Storage)
- Nên sử dụng Firebase Firestore version 13.4.0 để đảm bảo tương thích

### Dependencies
- Nếu gặp lỗi về dependencies, Unity sẽ tự động fix khi bạn chạy `FirebaseApp.CheckAndFixDependenciesAsync()`
- Unity External Dependency Manager sẽ tự động quản lý Android/iOS dependencies

### Define Symbol
- Define symbol `FIREBASE_FIRESTORE_AVAILABLE` được tự động quản lý bởi `FirebaseFirestoreChecker.cs`
- Nếu cần thêm thủ công: `Edit` → `Project Settings` → `Player` → `Other Settings` → `Scripting Define Symbols`
- Thêm: `FIREBASE_FIRESTORE_AVAILABLE` (chỉ cần thiết nếu script editor không hoạt động)

### Code hiện tại
- Code đã được cập nhật để compile được mà không cần package
- Các method sẽ trả về empty data và log warning khi package chưa được cài đặt
- Sau khi cài đặt package, chức năng sẽ tự động được kích hoạt

## Troubleshooting

### Lỗi TypeInitializationException trên macOS Editor

**Lỗi:**
```
[FirebaseAuthService] Firebase type initialization failed: The type initializer for 'Firebase.FirebaseApp' threw an exception.
Inner exception: The type initializer for 'Firebase.LogUtil' threw an exception.
This usually indicates missing native libraries or incompatible SDK version.
```

**Nguyên nhân:**
- Native libraries của Firebase chưa được enable cho Unity Editor trên macOS
- Plugin importer settings chưa đúng cho `.so` hoặc `.bundle` files
- Thiếu native libraries cho platform macOS Editor

**Giải pháp:**

1. **Sử dụng script tự động sửa lỗi:**
   - Vào menu Unity: `Tools` → `Firebase` → `Fix Native Library Settings (macOS)`
   - Script sẽ tự động:
     - Enable tất cả `.so` files cho Editor platform
     - Enable tất cả `.bundle` directories cho Editor platform
     - Enable `.dll` files cho Editor (dùng trong Editor mode)
   - Sau khi chạy script, **đóng và mở lại Unity Editor**

2. **Kiểm tra thủ công:**
   - Mở Project window
   - Vào `Assets/Firebase/Plugins/x86_64/`
   - Chọn từng file `.so` và `.bundle`
   - Trong Inspector, kiểm tra:
     - ✅ **Editor** phải được check (cho macOS Editor)
     - ✅ **StandaloneOSX** phải được check (cho macOS build)
     - ❌ Bỏ check các platform khác nếu không cần

3. **Nếu vẫn không hoạt động:**
   ```bash
   # Xóa cache và rebuild
   # Trong Unity Editor:
   Assets → Reimport All
   ```
   
   Hoặc:
   - Đóng Unity Editor
   - Xóa thư mục `Library` (Unity sẽ tự động rebuild)
   - Mở lại Unity Editor

4. **Kiểm tra phiên bản Firebase SDK:**
   - Đảm bảo tất cả Firebase packages cùng version (ví dụ: 13.4.0)
   - Nếu version khác nhau, có thể gây lỗi tương thích
   - Reimport tất cả Firebase packages từ cùng một Firebase Unity SDK download

5. **Nếu thiếu native libraries:**
   - Reimport Firebase App package: `Assets` → `Import Package` → `Custom Package...`
   - Chọn `FirebaseApp.unitypackage` từ Firebase Unity SDK
   - Đảm bảo import đầy đủ, không skip các files trong `Plugins/x86_64/`

**Script tự động fix:**
- Script `FirebaseNativeLibraryFixer.cs` sẽ tự động chạy khi Unity Editor mở trên macOS
- Nếu phát hiện vấn đề, script sẽ tự động sửa (silent mode)
- Bạn có thể chạy thủ công qua menu `Tools` → `Firebase` → `Fix Native Library Settings (macOS)`

### Lỗi UnityConnectWebRequestException
Lỗi này liên quan đến Unity Services (Unity Connect), không ảnh hưởng đến Firebase. Có thể bỏ qua nếu không sử dụng Unity Cloud Services.

### Vẫn gặp lỗi compile sau khi cài đặt package
1. Kiểm tra xem `FIREBASE_FIRESTORE_AVAILABLE` có trong Scripting Define Symbols không
2. Thử: `Assets` → `Reimport All`
3. Đóng và mở lại Unity Editor
4. Xóa thư mục `Library` và để Unity rebuild (lưu ý: sẽ mất cache)

### Script editor không tự động detect package
1. Kiểm tra file `Assets/Editor/FirebaseFirestoreChecker.cs` có tồn tại không
2. Thử chạy manual check: `Tools` → `Firebase` → `Check Firestore Installation`
3. Kiểm tra Unity Console để xem có log nào từ FirebaseFirestoreChecker không

