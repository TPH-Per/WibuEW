# 📋 CHANGELOG - Tổng Hợp Thay Đổi Từ Khi Pull Project

> **Ngày cập nhật:** 2025-12-22 22:10
> 
> **Tổng số files thay đổi:** 51 files (Modified) + 7 files (New) + 5 files (Hôm nay)
> 
> **Tổng số dòng thay đổi:** +247 / -188

---

## 🔥 CẬP NHẬT MỚI NHẤT (2025-12-22)

### Vấn đề: Lỗi 401 khi thêm sản phẩm vào giỏ hàng

**Triệu chứng:**
```
POST https://localhost:44377/api/cart
Status: 401 Unauthorized
Response: { Success: false, Message: "Vui lòng đăng nhập" }
```

**Nguyên nhân gốc:**
1. `ApiAuthController.Login()` không tạo cookie `FormsAuthenticationTicket` sau khi login thành công
2. Các API khác (như `/api/cart`) dựa vào cookie để xác thực user
3. CORS được cấu hình ở 2 nơi → gây duplicate `Access-Control-Allow-Origin` header

---

### ✅ Đã sửa (5 files)

#### 1. `DoAnLTWHQT/Controllers/ApiAuthController.cs`

| Thay đổi | Chi tiết |
|----------|----------|
| ✅ Thêm tạo FormsAuthenticationTicket | Sau khi login thành công, tạo và set cookie `.ASPXAUTH` |
| ✅ Xóa `[EnableCors]` attribute | CORS giờ được xử lý tập trung trong Global.asax.cs |
| ✅ Thêm import `System.Web.Security` | Để sử dụng `FormsAuthenticationTicket` |

**Code thêm (sau khi verify password thành công):**
```csharp
// Tạo FormsAuthentication cookie
var userRole = dbUser.role?.name ?? "customer";
var rememberMe = request.RememberMe;

var ticket = new FormsAuthenticationTicket(
    1,                                          // version
    dbUser.email,                               // user name (email)
    DateTime.Now,                               // issue time
    DateTime.Now.AddDays(rememberMe ? 7 : 1),   // expiration
    rememberMe,                                 // persistent
    userRole,                                   // user data (role)
    FormsAuthentication.FormsCookiePath         // cookie path
);

var encryptedTicket = FormsAuthentication.Encrypt(ticket);
var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
{
    HttpOnly = true,
    Secure = FormsAuthentication.RequireSSL,
    Path = FormsAuthentication.FormsCookiePath
};

if (rememberMe) authCookie.Expires = ticket.Expiration;

HttpContext.Current.Response.Cookies.Add(authCookie);
```

---

#### 2. `DoAnLTWHQT/Models/ApiLoginRequest.cs`

| Thay đổi | Chi tiết |
|----------|----------|
| ✅ Thêm property `RememberMe` | Hỗ trợ "Ghi nhớ đăng nhập" |

**Code mới:**
```csharp
public class ApiLoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }  // MỚI
}
```

---

#### 3. `DoAnLTWHQT/Global.asax.cs`

| Thay đổi | Chi tiết |
|----------|----------|
| ✅ Hỗ trợ nhiều origins | Thêm `localhost:5173` vào danh sách allowed |
| ✅ Tránh duplicate headers | Check trước khi add header |
| ✅ Thêm header `Authorization` | Cho phép gửi Authorization header |

**Code mới:**
```csharp
protected void Application_BeginRequest(object sender, EventArgs e)
{
    var context = HttpContext.Current;
    var origin = context.Request.Headers["Origin"];

    // Danh sách các origin được phép
    var allowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173" };

    if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
    {
        // Chỉ add header nếu chưa có (tránh duplicate)
        if (string.IsNullOrEmpty(context.Response.Headers["Access-Control-Allow-Origin"]))
        {
            context.Response.AddHeader("Access-Control-Allow-Origin", origin);
            context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With, Authorization");
            context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        }
    }

    if (context.Request.HttpMethod == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        context.Response.End();
    }
}
```

---

#### 4. `DoAnLTWHQT/App_Start/WebApiConfig.cs`

| Thay đổi | Chi tiết |
|----------|----------|
| ✅ Xóa CORS configuration | CORS giờ xử lý tập trung trong Global.asax.cs |
| ✅ Comment out PreflightHandler | Đã xử lý trong Global.asax.cs |

**Lý do:** Tránh duplicate `Access-Control-Allow-Origin` header (gây lỗi CORS)

---

#### 5. `FRONTEND_AUTH_FIX_GUIDE.md` (FILE MỚI)

File hướng dẫn chi tiết cho team Frontend cách sửa để authentication hoạt động:

**Nội dung chính:**
- Giải thích nguyên nhân lỗi 401
- Hướng dẫn thêm `withCredentials: true` vào Axios
- Ví dụ code hoàn chỉnh cho `api/index.ts`, `auth.api.ts`, `cart.api.ts`
- Cách kiểm tra cookie đã được set
- Lưu ý về HTTPS và SameSite

---

### 🐛 Bug Fixes Summary

| Bug | Nguyên nhân | Giải pháp |
|-----|-------------|-----------|
| 401 khi thêm vào giỏ hàng | API login không tạo auth cookie | Thêm code tạo `FormsAuthenticationTicket` |
| CORS `does not match 'http://localhost:3000, http://localhost:3000'` | Header duplicate từ 2 nơi | Xóa CORS từ WebApiConfig, chỉ giữ trong Global.asax.cs |

---

### 📋 Checklist Cho Team

**Backend (đã hoàn thành ✅):**
- [x] ApiAuthController tạo cookie khi login
- [x] CORS xử lý tập trung trong Global.asax.cs
- [x] Hỗ trợ cả localhost:3000 và localhost:5173
- [x] Thêm RememberMe vào ApiLoginRequest

**Frontend (cần làm ⚠️):**
- [ ] Thêm `withCredentials: true` vào Axios config
- [ ] Cập nhật login request để gửi `rememberMe`
- [ ] Build lại và test

---

---

## 📊 Tổng Quan

| Loại | Số lượng |
|------|----------|
| Files mới tạo (Untracked) | 7 files |
| Files đã sửa (Modified) | 51 files |
| Tổng cộng | 58 files |

---

## 🆕 FILES MỚI TẠO (7 files)

### 1. `DoAnLTWHQT/Controllers/ApiAuthControllerController.cs` ⭐
**Mục đích:** API Controller xử lý authentication cho Vue.js frontend

**Endpoints:**
| Method | Route | Mô tả |
|--------|-------|-------|
| POST | `/api/auth/login` | Đăng nhập, trả về user info |
| POST | `/api/auth/register` | Đăng ký tài khoản mới |
| GET | `/api/auth/check` | Kiểm tra API hoạt động |

**Features:**
- CORS enabled cho `http://localhost:5173`
- Hỗ trợ 3 loại password hash: Plain text, BCrypt, ASP.NET Crypto
- JSON response chuẩn hóa

---

### 2. `DoAnLTWHQT/App_Start/WebApiConfig.cs` ⭐
**Mục đích:** Cấu hình Web API 2

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

### 3. `DoAnLTWHQT/App_Start/PreflightRequestsHandler.cs`
**Mục đích:** Handler cho CORS preflight requests (OPTIONS)

---

### 4. `DoAnLTWHQT/Models/ApiLoginRequest.cs`
```csharp
public class ApiLoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

---

### 5. `DoAnLTWHQT/Models/ApiRegisterRequest.cs`
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

### 6. `DoAnLTWHQT/Models/ApiResponse.cs`
```csharp
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}
```

---

### 7. `DoAnLTWHQT/Models/ApiValidationResponse.cs`
```csharp
public class ApiValidationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
}
```

---

### 8. `DoAnLTWHQT/Models/UserDto.cs`
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

### 9. `test_password.cs`
**Mục đích:** File test password hashing (development only)

---

## 🔧 FILES ĐÃ SỬA ĐỔI (51 files)

### ⭐ THAY ĐỔI QUAN TRỌNG

#### 1. `DoAnLTWHQT/Global.asax.cs`
**Thay đổi chính:**
- ✅ Thêm `GlobalConfiguration.Configure(WebApiConfig.Register)` để đăng ký Web API
- ✅ Thêm `Application_BeginRequest` handler để xử lý CORS manually
- ✅ Enable `FilterConfig.RegisterGlobalFilters`

```csharp
// THÊM MỚI trong Application_Start()
GlobalConfiguration.Configure(WebApiConfig.Register);

// THÊM MỚI method
protected void Application_BeginRequest(object sender, EventArgs e)
{
    var context = HttpContext.Current;
    var origin = context.Request.Headers["Origin"];

    if (origin == "http://localhost:3000")
    {
        context.Response.AddHeader("Access-Control-Allow-Origin", origin);
        context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
        context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    }

    if (context.Request.HttpMethod == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        context.Response.End();
    }
}
```

---

#### 2. `DoAnLTWHQT/DoAnLTWHQT.csproj`
**Thay đổi:** +31 dòng
- Thêm reference các file mới (Models, Controllers, App_Start)
- Thêm các NuGet packages cho Web API

---

#### 3. `DoAnLTWHQT/Web.config`
**Thay đổi:** +9 dòng / -1 dòng
- Cập nhật cấu hình cho Web API

---

#### 4. `DoAnLTWHQT/packages.config`
**Thay đổi:** +7 packages mới cho Web API và CORS

---

#### 5. `DoAnLTWHQT/Controllers/AccountController.cs`
**Thay đổi:** Refactor code (~37 dòng thay đổi)

---

### 📁 CÁC CONTROLLERS KHÁC (Minor changes - cleanup/refactor)

| File | Thay đổi |
|------|----------|
| `Areas/Admin/Controllers/BranchInventoriesController.cs` | 2 dòng |
| `Areas/Admin/Controllers/BranchesController.cs` | 4 dòng |
| `Areas/Admin/Controllers/CategoriesController.cs` | 4 dòng |
| `Areas/Admin/Controllers/DashboardController.cs` | 1 dòng |
| `Areas/Admin/Controllers/DiscountsController.cs` | 4 dòng |
| `Areas/Admin/Controllers/InventoriesController.cs` | 2 dòng |
| `Areas/Admin/Controllers/OrdersController.cs` | 2 dòng |
| `Areas/Admin/Controllers/PaymentsController.cs` | 2 dòng |
| `Areas/Admin/Controllers/ProductReviewsController.cs` | 2 dòng |
| `Areas/Admin/Controllers/ProductVariantsController.cs` | 9 dòng |
| `Areas/Admin/Controllers/ProductsController.cs` | 4 dòng |
| `Areas/Admin/Controllers/ReportsController.cs` | 2 dòng |
| `Areas/Admin/Controllers/SupplierShipmentsController.cs` | 4 dòng |
| `Areas/Admin/Controllers/SuppliersController.cs` | 7 dòng |
| `Areas/Admin/Controllers/UsersController.cs` | 4 dòng |
| `Areas/Admin/Controllers/WarehouseTransfersController.cs` | 4 dòng |
| `Areas/Branch/Controllers/DiscountsController.cs` | 2 dòng |
| `Areas/Branch/Controllers/InventoryController.cs` | 2 dòng |
| `Areas/Branch/Controllers/OrdersController.cs` | 2 dòng |
| `Areas/Branch/Controllers/POSController.cs` | 14 dòng |
| `Areas/Branch/Controllers/PaymentsController.cs` | 2 dòng |
| `Areas/Branch/Controllers/PreOrdersController.cs` | 2 dòng |
| `Areas/Branch/Controllers/ReportsController.cs` | 2 dòng |
| `Areas/Branch/Controllers/TransfersController.cs` | 2 dòng |
| `Areas/Warehouse/Controllers/AdjustmentsController.cs` | 2 dòng |
| `Areas/Warehouse/Controllers/DashboardController.cs` | 1 dòng |
| `Areas/Warehouse/Controllers/InventoryController.cs` | 2 dòng |
| `Areas/Warehouse/Controllers/ReservationsController.cs` | 2 dòng |
| `Areas/Warehouse/Controllers/ShipmentsController.cs` | 12 dòng |
| `Areas/Warehouse/Controllers/TransactionsController.cs` | 2 dòng |
| `Areas/Warehouse/Controllers/TransfersController.cs` | 54 dòng |

---

### 📁 AREA REGISTRATIONS

| File | Thay đổi |
|------|----------|
| `Areas/Admin/AdminAreaRegistration.cs` | 8 dòng |
| `Areas/Branch/BranchAreaRegistration.cs` | 8 dòng |
| `Areas/Warehouse/WarehouseAreaRegistration.cs` | 8 dòng |

---

### 📁 APP_START

| File | Thay đổi |
|------|----------|
| `App_Start/BundleConfig.cs` | 1 dòng (remove) |
| `App_Start/FilterConfig.cs` | 1 dòng (remove) |

---

### 📁 VIEWMODELS

| File | Thay đổi |
|------|----------|
| `ViewModels/Admin/AdminManagementViewModels.cs` | 2 dòng |
| `ViewModels/Admin/AdminReportViewModel.cs` | 2 dòng |
| `ViewModels/Admin/LoginViewModel.cs` | 2 dòng |
| `ViewModels/Branch/BranchDashboardViewModel.cs` | 2 dòng |
| `ViewModels/Branch/BranchManagementViewModels.cs` | 1 dòng |
| `ViewModels/Warehouse/InboundReceiptViewModels.cs` | 66 dòng |
| `ViewModels/Warehouse/WarehouseDashboardViewModel.cs` | 2 dòng |
| `ViewModels/Warehouse/WarehouseManagementViewModels.cs` | 2 dòng |

---

### 📁 SECURITY & OTHER

| File | Thay đổi |
|------|----------|
| `Security/CustomPrincipal.cs` | 50 dòng (refactor) |
| `Properties/AssemblyInfo.cs` | 1 dòng |

---

## 📦 NUGET PACKAGES ĐÃ THÊM

```xml
<package id="Microsoft.AspNet.WebApi" version="5.3.0" />
<package id="Microsoft.AspNet.WebApi.Client" version="6.0.0" />
<package id="Microsoft.AspNet.WebApi.Core" version="5.3.0" />
<package id="Microsoft.AspNet.WebApi.WebHost" version="5.3.0" />
<package id="Microsoft.AspNet.WebApi.Cors" version="5.3.0" />
<package id="Microsoft.AspNet.Cors" version="5.3.0" />
<package id="BCrypt.Net-Next" version="4.0.3" />
```

---

## 🐛 BUG FIXES

### Fix 1: Password Verification trong API (2025-12-20)

**Vấn đề:** Login qua API thất bại với message "Email hoặc mật khẩu không chính xác" dù cùng tài khoản login qua web thành công.

**Nguyên nhân:** `ApiAuthController.VerifyPassword()` chỉ hỗ trợ BCrypt hash

**Giải pháp:** Cập nhật để hỗ trợ 3 loại password:
1. Plain text (cho testing)
2. BCrypt hash (`$2a$`, `$2b$`, `$2y$`)
3. ASP.NET Crypto hash

---

### Fix 2: HTTPS vs HTTP (2025-12-20)

**Vấn đề:** `Error: read ECONNRESET` khi gọi API

**Nguyên nhân:** Project sử dụng HTTPS trên port 44377, client gọi bằng HTTP

**Giải pháp:** Sử dụng `https://localhost:44377/api/auth/login`

---

## 🌐 CẤU HÌNH SERVER

```
Protocol: HTTPS
Port: 44377
URL: https://localhost:44377/
```

**Lưu ý:** Luôn sử dụng HTTPS, không dùng HTTP!

---

## 🧪 TEST COMMANDS

### Kiểm tra API:
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

## ✅ TRẠNG THÁI HIỆN TẠI

| Module | Trạng thái |
|--------|------------|
| Web Authentication (Forms) | ✅ Hoạt động |
| API Authentication | ✅ Hoạt động |
| CORS Configuration | ✅ Đã cấu hình |
| Multi-hash Password Support | ✅ Đã implement |
| Admin Area | ✅ Hoạt động |
| Branch Area | ✅ Hoạt động |
| Warehouse Area | ✅ Hoạt động |

---

## 📝 GHI CHÚ QUAN TRỌNG

1. **Restart IIS Express** sau mỗi lần thay đổi code C#
2. Sử dụng `-k` flag với curl để bỏ qua SSL certificate validation
3. API CORS chỉ cho phép từ:
   - `http://localhost:5173` (Vue dev server - trong WebApiConfig)
   - `http://localhost:3000` (trong Global.asax.cs)
4. Để test từ origin khác, cập nhật CORS config
5. Build command: `msbuild DoAnLTWHQT.csproj /t:Build /p:Configuration=Debug`

---

> **Ghi chú:** File này được tạo tự động bởi AI Assistant để theo dõi các thay đổi trong dự án.
