# Hướng Dẫn Tính Năng Đăng Ký (Register)

## Tổng Quan
Tính năng đăng ký cho phép người dùng mới tạo tài khoản Customer để sử dụng hệ thống.

## Các Thay Đổi Đã Thực Hiện

### 1. Database - Stored Procedure
**File:** `Database/08_CreateUserProcedure.sql`

```sql
-- Tạo SQL Login/User thực sự trong SQL Server
EXEC sp_System_CreateSQLUser 
    @Username = 'customer_001', 
    @Password = 'MyPassword123', 
    @RoleType = 'Customer'
```

**Chức năng:**
- Tạo SQL Server Login (cấp Server)
- Tạo Database User (cấp Database)
- Gán user vào Role tương ứng (Role_Customer, Role_Admin, etc.)

### 2. ViewModel
**File:** `DoAnLTWHQT/ViewModels/Admin/RegisterViewModel.cs`

**Các trường:**
| Field | Type | Validation |
|-------|------|------------|
| Username | string | Required, 3-50 ký tự, chỉ chữ/số/underscore |
| FullName | string | Required, 2-100 ký tự |
| Email | string | Required, Email format |
| PhoneNumber | string | Optional, Phone format |
| Password | string | Required, 6-50 ký tự |
| ConfirmPassword | string | Required, phải khớp Password |
| AgreeTerms | bool | Required = true |

### 3. Controller Actions
**File:** `DoAnLTWHQT/Controllers/AccountController.cs`

**Thêm mới:**
- `GET /register` - Hiển thị form đăng ký
- `POST /register` - Xử lý đăng ký

**Quy trình đăng ký:**
1. Validate model
2. Kiểm tra email/username trùng lặp
3. Gọi `sp_System_CreateSQLUser` để tạo SQL User (password plain text)
4. Hash password bằng BCrypt cho bảng `users`
5. Insert user mới với `role_id = 4` (Customer)
6. Redirect về Login với thông báo thành công

### 4. View
**File:** `DoAnLTWHQT/Views/Account/Register.cshtml`

**Tính năng:**
- Form đăng ký responsive
- Validation client-side với jQuery Validate
- Modal điều khoản sử dụng
- Link quay về trang đăng nhập

### 5. Cập nhật Login
**File:** `DoAnLTWHQT/Views/Account/Login.cshtml`

- Thêm link "Đăng ký ngay" ở cuối form

## Cách Cài Đặt

### Bước 1: Chạy SQL Script
```sql
-- Trong SQL Server Management Studio
USE [perw];
GO

-- Chạy file 08_CreateUserProcedure.sql
:r "Database/08_CreateUserProcedure.sql"
```

### Bước 2: (Tùy chọn) Tạo Database Roles
Nếu chưa có các roles, tạo thêm:
```sql
-- Tạo roles nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_Customer')
    CREATE ROLE [Role_Customer];
    
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_Admin')
    CREATE ROLE [Role_Admin];
    
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_WarehouseManager')
    CREATE ROLE [Role_WarehouseManager];
    
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_BranchManager')
    CREATE ROLE [Role_BranchManager];
```

### Bước 3: Build và Run
```bash
# Build project
msbuild DoAnLTWHQT.csproj /p:Configuration=Debug

# Hoặc nhấn F5 trong Visual Studio
```

## Cách Sử Dụng

### Đăng ký tài khoản mới:
1. Truy cập `http://localhost:port/register`
2. Điền đầy đủ thông tin:
   - Tên đăng nhập (chỉ chữ, số, underscore)
   - Họ tên đầy đủ
   - Email
   - Số điện thoại (tùy chọn)
   - Mật khẩu (tối thiểu 6 ký tự)
   - Xác nhận mật khẩu
3. Đồng ý điều khoản sử dụng
4. Nhấn "Đăng ký"
5. Sau khi thành công, sẽ chuyển về trang đăng nhập

### Đăng nhập:
- Sử dụng **Email** và **Mật khẩu** đã đăng ký

## Lưu Ý Quan Trọng

### Về SQL User
- Stored procedure `sp_System_CreateSQLUser` tạo SQL Login với **password plain text** để SQL Server quản lý
- Yêu cầu quyền `securityadmin` hoặc `sysadmin` để tạo Login
- Nếu không có quyền, việc tạo SQL user sẽ fail nhưng user vẫn được tạo trong bảng `users`

### Về Password Hash
- Password trong bảng `users` được hash bằng **BCrypt** với salt 12 rounds
- Khi đăng nhập, hệ thống verify password với BCrypt hash

### Về Role mặc định
- Tất cả user đăng ký qua web sẽ có `role_id = 4` (Customer)
- Admin có thể thay đổi role trong Admin → Users

## Database Changes

### Bảng `users` - Không thay đổi cấu trúc
Sử dụng các cột có sẵn:
- `name` - Tên đăng nhập
- `full_name` - Họ tên đầy đủ
- `email` - Email
- `phone_number` - Số điện thoại
- `password` - BCrypt hash
- `role_id` - 4 (Customer)
- `status` - "active"
- `created_at`, `updated_at` - Timestamps

## Files Changed Summary

| File | Action | Description |
|------|--------|-------------|
| `Database/08_CreateUserProcedure.sql` | **NEW** | Stored procedure tạo SQL User |
| `ViewModels/Admin/RegisterViewModel.cs` | **NEW** | ViewModel cho form đăng ký |
| `Controllers/AccountController.cs` | **MODIFIED** | Thêm Register GET/POST actions |
| `Views/Account/Register.cshtml` | **NEW** | View trang đăng ký |
| `Views/Account/Login.cshtml` | **MODIFIED** | Thêm link đến trang đăng ký |
| `REGISTER_GUIDE.md` | **NEW** | File hướng dẫn này |

## Troubleshooting

### 1. Lỗi "Loại quyền không hợp lệ"
- Đảm bảo RoleType là một trong: `Admin`, `Warehouse`, `Branch`, `Customer`

### 2. Lỗi permission khi tạo SQL Login
- User kết nối database cần quyền `securityadmin`
- Hoặc bỏ qua lỗi này, user vẫn được tạo trong bảng `users`

### 3. Email/Username đã tồn tại
- Hệ thống sẽ hiển thị lỗi validation
- Chọn email hoặc username khác

### 4. Mật khẩu không khớp
- Nhập lại mật khẩu xác nhận cho khớp với mật khẩu
