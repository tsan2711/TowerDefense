# Hướng dẫn cấu hình Firestore Rules

## Vấn đề: "Missing or insufficient permissions"

Lỗi này xảy ra khi Firestore Rules chưa được cấu hình để cho phép truy cập dữ liệu. Bạn cần cấu hình Firestore Security Rules trong Firebase Console.

## Cách sửa:

### Bước 1: Truy cập Firebase Console

1. Đi đến [Firebase Console](https://console.firebase.google.com/)
2. Chọn project của bạn: **towerdefense-2bbc5**
3. Vào **Firestore Database** → **Rules**

### Bước 2: Cấu hình Rules

Có 3 tùy chọn tùy theo môi trường:

#### Option A: Development/Testing - Không cần Authentication (CỰC KỲ KHÔNG AN TOÀN - chỉ dùng cho local testing)

**Chỉ dùng khi bạn đang test và chưa có authentication setup!**

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Cho phép đọc/ghi tất cả collections KHÔNG CẦN authentication
    match /{document=**} {
      allow read, write: if true;
    }
  }
}
```

**⚠️ CẢNH BÁO NGHIÊM TRỌNG**: 
- **KHÔNG BAO GIỜ** dùng rules này trong production!
- Chỉ dùng cho local development khi bạn chưa setup authentication
- Rules này cho phép BẤT KỲ AI truy cập dữ liệu của bạn!

#### Option B: Development/Testing - Cần Authentication (AN TOÀN HƠN - khuyến nghị cho testing)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Cho phép đọc tất cả collections khi user đã authenticated
    match /{document=**} {
      allow read, write: if request.auth != null;
    }
  }
}
```

**⚠️ LƯU Ý**: 
- Rules này yêu cầu user phải đăng nhập trước khi đọc dữ liệu
- Phù hợp cho testing với authentication đã được setup
- Vẫn chưa an toàn cho production vì cho phép mọi authenticated user write

#### Option C: Production (AN TOÀN - khuyến nghị cho production)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // AgentConfigurations collection
    match /AgentConfigurations/{agentId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && request.auth.token.admin == true;
    }
    
    // TowerLevelData collection
    match /TowerLevelData/{towerId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && request.auth.token.admin == true;
    }
    
    // LevelList collection
    match /LevelList/{document=**} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && request.auth.token.admin == true;
    }
    
    // Cho phép đọc tất cả khi đã authenticated (có thể điều chỉnh)
    match /{document=**} {
      allow read: if request.auth != null;
      allow write: if false; // Chặn write mặc định
    }
  }
}
```

### Bước 3: Publish Rules

1. Click nút **"Publish"** để lưu rules
2. Đợi vài giây để rules được áp dụng

### Bước 4: Kiểm tra Authentication (nếu dùng Option B hoặc C)

Nếu bạn chọn Option B hoặc C (yêu cầu authentication), đảm bảo rằng:
- User đã đăng nhập thành công (Firebase Auth)
- Authentication token được gửi cùng với request

**Cách kiểm tra trong Unity:**
1. Xem Unity Console logs
2. Tìm log: `[FirebaseAuthService] IsAuthenticated: True/False`
3. Nếu `False`, bạn cần đăng nhập trước khi load dữ liệu từ Firestore

**Lưu ý về flow hiện tại:**
- Dữ liệu config được load tự động SAU KHI user đăng nhập thành công
- Method `LoadConfigurationDataAfterSignIn()` trong `FirebaseAuthService` sẽ được gọi sau khi sign in
- Nếu bạn thấy lỗi permission ngay khi khởi động game, có thể:
  - User chưa đăng nhập → Cần đăng nhập trước
  - Hoặc sử dụng Option A (không cần auth) cho development/testing

## Debugging

### Kiểm tra từng bước:

1. **Kiểm tra Rules đã được publish chưa:**
   - Vào Firebase Console → Firestore → Rules
   - Xem có thông báo "Rules published" không
   - Nếu chưa, click "Publish" và đợi vài giây

2. **Kiểm tra user đã authenticated chưa (nếu dùng Option B hoặc C):**
   - Xem logs trong Unity Console
   - Tìm log: `[FirebaseAuthService] Initialized successfully`
   - Tìm log: `[FirebaseAuthService] IsAuthenticated: True/False`
   - Nếu `False`, bạn cần đăng nhập trước khi load dữ liệu
   - Dữ liệu chỉ được load SAU KHI đăng nhập thành công (trong `LoadConfigurationDataAfterSignIn()`)

3. **Kiểm tra collection names:**
   - Đảm bảo collection names trong code khớp với Firestore:
     - `AgentConfigurations` (collection)
     - `TowerLevelData` (collection)
     - `LevelList/main` (document trong collection)

4. **Kiểm tra dữ liệu có tồn tại trong Firestore:**
   - Vào Firebase Console → Firestore Database → Data
   - Kiểm tra các collections trên có dữ liệu không
   - Đặc biệt kiểm tra `LevelList` collection phải có document tên `main`

5. **Test Rules trong Firebase Console:**
   - Vào Firestore → Rules
   - Click "Rules Playground" (Simulator)
   - Test với authenticated/unauthenticated user
   - Kiểm tra xem rules có cho phép đọc không

6. **Kiểm tra logs trong Unity:**
   - Xem Unity Console để biết chính xác collection nào bị từ chối
   - Log sẽ hiển thị: `[FirebaseFirestoreService] Permission denied when loading <CollectionName>`

## Cấu trúc Collections trong Firestore

### AgentConfigurations
```
Collection: AgentConfigurations
Documents: {agentId}
Fields: (xem AgentConfigurationData.cs)
```

### TowerLevelData
```
Collection: TowerLevelData
Documents: {towerId}
Fields: (xem TowerLevelDataData.cs)
```

### LevelList
```
Collection: LevelList
Document: main
Fields: (xem LevelListData.cs)
```

## Lưu ý bảo mật

- **KHÔNG BAO GIỜ** publish rules cho phép truy cập không cần authentication trong production
- Luôn sử dụng `request.auth != null` để kiểm tra user đã authenticated
- Sử dụng custom claims (`request.auth.token.admin`) để phân quyền nếu cần
- Review rules định kỳ để đảm bảo an toàn

## Liên kết hữu ích

- [Firestore Security Rules Documentation](https://firebase.google.com/docs/firestore/security/get-started)
- [Firebase Authentication](https://firebase.google.com/docs/auth)

