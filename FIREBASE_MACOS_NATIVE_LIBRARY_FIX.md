# Hướng dẫn khắc phục lỗi Firebase Native Library trên macOS

## Lỗi gặp phải

```
DllNotFoundException: FirebaseCppApp-13_4_0 assembly:<unknown assembly> type:<unknown type> member:(null)
```

## Nguyên nhân

Trên macOS, Firebase Unity SDK cần native library (`FirebaseCppApp-13_4_0`) nhưng:
1. File `.bundle` (định dạng native cho macOS) có thể bị thiếu
2. Plugin settings có thể chưa được cấu hình đúng cho macOS
3. Unity Editor trên macOS cần native library được enable đúng cách

## Giải pháp

### Giải pháp 1: Sử dụng script tự động (Khuyên dùng)

1. Mở Unity Editor
2. Vào menu: `Tools` → `Firebase` → `Fix Native Library Settings (macOS)`
3. Script sẽ tự động:
   - Kiểm tra các native libraries
   - Enable `.so` file cho macOS (nếu `.bundle` không có)
   - Hiển thị thông tin diagnostic

### Giải pháp 2: Sửa thủ công Plugin Settings

1. Trong Unity Editor, tìm file: `Assets/Firebase/Plugins/x86_64/FirebaseCppApp-13_4_0.so`
2. Click phải → `Reimport`
3. Click vào file → Inspector → Plugin Inspector
4. Trong phần "Platform settings":
   - ✅ Tick vào **Editor** (chọn **OSX** trong dropdown)
   - ✅ Tick vào **OSXIntel64**
   - ✅ Tick vào **OSXUniversal**
5. Click **Apply**

### Giải pháp 3: Reimport Firebase App Package

Nếu các giải pháp trên không hoạt động:

1. Xóa thư mục: `Assets/Firebase/Plugins/x86_64/`
2. Tải lại Firebase Unity SDK từ: https://firebase.google.com/download/unity
3. Import lại package `FirebaseApp.unitypackage`
4. Đảm bảo tất cả files được import, bao gồm:
   - `FirebaseCppApp-13_4_0.bundle` (nếu có)
   - `FirebaseCppApp-13_4_0.so`
   - `FirebaseCppApp-13_4_0.dll`

## Kiểm tra

Sau khi áp dụng giải pháp:

1. Đóng và mở lại Unity Editor
2. Chạy game trong Editor
3. Kiểm tra Console log - không còn lỗi `DllNotFoundException`

## Lưu ý quan trọng

### Về `.bundle` vs `.so` trên macOS

- **`.bundle`**: Định dạng native bundle của macOS, được ưa chuộng nhất
- **`.so`**: Shared library của Linux, có thể hoạt động trên macOS nhưng không phải là định dạng chuẩn
- **`.dll`**: Chỉ dùng cho Windows

### Unity Editor trên macOS

Unity Editor trên macOS có thể sử dụng:
- `.bundle` files (preferred)
- `.so` files (nếu được enable đúng cách)
- `.dylib` files (ít khi dùng với Firebase)

### Nếu vẫn gặp lỗi

1. **Kiểm tra version**: Đảm bảo tất cả Firebase packages cùng version (hiện tại: 13.4.0)
2. **Reimport tất cả**: `Assets` → `Reimport All`
3. **Xóa Library**: Xóa thư mục `Library/` và để Unity rebuild (⚠️ sẽ mất cache)
4. **Kiểm tra Build Target**: Đảm bảo bạn đang test trong Unity Editor, không phải standalone build

## Troubleshooting

### Lỗi vẫn xảy ra sau khi fix

**Kiểm tra file tồn tại:**
```bash
ls -la Assets/Firebase/Plugins/x86_64/FirebaseCppApp-13_4_0*
```

Phải có ít nhất một trong các file sau:
- `FirebaseCppApp-13_4_0.bundle` (preferred)
- `FirebaseCppApp-13_4_0.so`
- `FirebaseCppApp-13_4_0.dll` (chỉ cho Editor trên macOS)

**Kiểm tra plugin settings:**
1. Click vào file `.so` hoặc `.bundle`
2. Inspector → Plugin Importer
3. Đảm bảo "Editor" và "OSXIntel64" đã được tick

### Lỗi khi build standalone

Nếu lỗi chỉ xảy ra khi build standalone game:
1. Đảm bảo `OSXIntel64` hoặc `OSXUniversal` đã được enable
2. Kiểm tra Build Settings → Platform Settings
3. Thử build với "Development Build" để xem log chi tiết

### Apple Silicon (M1/M2/M3) Mac

Nếu bạn đang dùng Mac với chip Apple Silicon:
1. Unity Editor có thể chạy dưới Rosetta
2. Đảm bảo `OSXUniversal` được enable (bao gồm cả ARM64)
3. Hoặc thử với `OSXIntel64` nếu Unity đang chạy dưới Rosetta

## Thông tin bổ sung

Script tự động: `Assets/Editor/FirebaseNativeLibraryFixer.cs`
- Tự động chạy khi mở Unity Editor
- Có thể chạy thủ công qua menu: `Tools` → `Firebase` → `Fix Native Library Settings (macOS)`

