# 📋 CHANGELOG - API Authentication Module

> **Tổng hợp các thay đổi từ khi pull project đến hiện tại**
> 
> Cập nhật: 2025-12-20

---

## 🆕 Files Đã Tạo Mới

### 1. `Controllers/ApiAuthControllerController.cs`
**Mục đích:** API Controller để xử lý authentication cho Vue.js frontend

**Endpoints:**
| Method | Route | Mô tả |
|--------|-------|-------|
| POST | `/api/auth/login` | Đăng nhập và trả về thông tin user |
| POST | `/api/auth/register` | Đăng ký tài khoản mới |
| GET | `/api/auth/check` | Kiểm tra API hoạt động |

**Features:**
- CORS enabled cho `http://localhost:5173` (Vue dev server)
- Attribute routing với prefix `api/auth`
- BCrypt password hashing cho đăng ký
- Trả về JSON response

---

### 2. `Models/ApiLoginRequest.cs`
```csharp
public class ApiLoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

---

### 3. `Models/ApiRegisterRequest.cs`
```csharp
public class ApiRegisterRequest
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public string PasswordConfirmation { get; set; }
}
```

---

### 4. `Models/ApiResponse.cs`
```csharp
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}
```

---

### 5. `Models/ApiValidationResponse.cs`
```csharp
public class ApiValidationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
}
```

---

### 6. `Models/UserDto.cs`
```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public int? RoleId { get; set; }
    public string RoleName { get; set; }
}
```

---

### 7. `App_Start/WebApiConfig.cs`
**Mục đích:** Cấu hình Web API với CORS và routing

```csharp
public static void Register(HttpConfiguration config)
{
    // Enable CORS
    var cors = new EnableCorsAttribute(
        origins: "http://localhost:5173",
        headers: "*",
        methods: "*"
    );
    cors.SupportsCredentials = true;
    config.EnableCors(cors);

    // Attribute routing
    config.MapHttpAttributeRoutes();

    // Convention-based routing
    config.Routes.MapHttpRoute(
        name: "DefaultApi",
        routeTemplate: "api/{controller}/{action}/{id}",
        defaults: new { id = RouteParameter.Optional }
    );
}
```

---

## 🔧 Files Đã Chỉnh Sửa

### 1. `Global.asax.cs`
**Thay đổi:** Thêm đăng ký Web API config

```csharp
// Thêm dòng này trong Application_Start()
GlobalConfiguration.Configure(WebApiConfig.Register);
```

---

### 2. `Controllers/ApiAuthControllerController.cs` - **BUG FIX (2025-12-20)**

**Vấn đề:** Login qua API báo "Email hoặc mật khẩu không chính xác" dù cùng tài khoản login qua web thành công.

**Nguyên nhân:** Phương thức `VerifyPassword` chỉ hỗ trợ BCrypt hash, trong khi database có thể chứa:
- Plain text password (cho testing)
- BCrypt hash (`$2a$`, `$2b$`, `$2y$`)
- ASP.NET Crypto hash

**Trước khi sửa:**
```csharp
private bool VerifyPassword(string inputPassword, string storedHash)
{
    try
    {
        return BCryptNet.Verify(inputPassword, storedHash);
    }
    catch
    {
        return false;
    }
}
```

**Sau khi sửa:**
```csharp
private bool VerifyPassword(string inputPassword, string storedHash)
{
    if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(inputPassword))
    {
        return false;
    }

    // 1. Plain text comparison (for testing/legacy accounts)
    if (string.Equals(storedHash, inputPassword))
    {
        return true;
    }

    // 2. BCrypt hash (starts with $2)
    if (storedHash.StartsWith("$2"))
    {
        try
        {
            return BCryptNet.Verify(inputPassword, storedHash);
        }
        catch
        {
            return false;
        }
    }

    // 3. ASP.NET Crypto hash
    try
    {
        return System.Web.Helpers.Crypto.VerifyHashedPassword(storedHash, inputPassword);
    }
    catch
    {
        return false;
    }
}
```

**Thêm using statement:**
```csharp
using System.Web.Helpers;
```

---

## 📦 NuGet Packages Cần Thiết

Các packages đã có sẵn trong project:
- `Microsoft.AspNet.WebApi.Core` (5.3.0)
- `Microsoft.AspNet.WebApi.WebHost` (5.3.0)
- `Microsoft.AspNet.WebApi.Cors` (5.3.0)
- `Microsoft.AspNet.Cors` (5.3.0)
- `BCrypt.Net-Next` (4.0.3)

---

## 🌐 Cấu Hình Server

**Lưu ý quan trọng:** Project sử dụng **HTTPS** trên port **44377**

```xml
<!-- Trong .csproj -->
<IISExpressSSLPort>44377</IISExpressSSLPort>
<IISUrl>https://localhost:44377/</IISUrl>
```

**Đúng:** `https://localhost:44377/api/auth/login`  
**Sai:** `http://localhost:44377/api/auth/login` ← Gây lỗi ECONNRESET

---

## 🧪 Test Commands

### Kiểm tra API hoạt động:
```bash
curl -k https://localhost:44377/api/auth/check
```

### Đăng nhập:
```bash
curl -k -X POST https://localhost:44377/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"warehouse@test.local\",\"password\":\"Warehouse@123\"}"
```

### Đăng ký:
```bash
curl -k -X POST https://localhost:44377/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"name\":\"testuser\",\"fullName\":\"Test User\",\"email\":\"test@example.com\",\"password\":\"123456\",\"passwordConfirmation\":\"123456\"}"
```

---

## ✅ Trạng Thái Hiện Tại

| Tính năng | Trạng thái |
|-----------|------------|
| API Login | ✅ Hoạt động |
| API Register | ✅ Hoạt động |
| CORS | ✅ Đã cấu hình |
| Multi-hash password support | ✅ Đã sửa |
| HTTPS | ✅ Bắt buộc |

---

## 📝 Ghi Chú

1. **Restart IIS Express** sau mỗi lần thay đổi code
2. Sử dụng `-k` flag với curl để bỏ qua SSL certificate validation
3. API chỉ cho phép CORS từ `http://localhost:5173` (Vue dev server)
4. Để test từ origin khác, cần cập nhật CORS config trong `WebApiConfig.cs` và `ApiAuthController.cs`
