# 📋 CHANGELOG - Phiên làm việc ngày 23/12/2025

## Tổng quan
Phiên làm việc từ **12:57** đến **17:50** tập trung vào:
1. Kiểm tra và sửa lỗi logic `ApiOrdersController` không khớp với database schema
2. Sửa lỗi CORS frontend không kết nối được backend
3. Đồng bộ Entity Model với Database

---

## 📁 CÁC FILE ĐÃ SỬA ĐỔI

### 1. `Controllers/ApiOrdersController.cs`

#### Thay đổi `GetCurrentUserId()`
| Trước | Sau |
|-------|-----|
| Trả về `int?` | Trả về `long?` |
| Cast `(int)user.id` | Trả về `user.id` trực tiếp |

#### Thay đổi `GetMyOrders()`
- Bỏ parameter `string status = null`
- Thêm `.ToList()` trước khi Select (để tránh lỗi EF với navigation properties)
- Thêm đầy đủ thông tin trong response:
  - `UserId`, `ShippingRecipientName`, `ShippingRecipientPhone`, `ShippingAddress`
  - `SubTotal`, `ShippingFee`, `DiscountAmount`
  - `BranchId`, `BranchName`
  - `UpdatedAt`
- Load `Items` từ `purchase_order_details` với safe null checks
- Load `Payment` với đầy đủ thông tin

#### Thêm mới `CreateOrder()`
- Validate request null
- Sử dụng `DbTransaction` để đảm bảo data consistency
- Kiểm tra `payment_method` tồn tại và `is_active = true`
- Gộp cart items cùng `product_variant_id` (tránh composite key violation)
- Tạo mã đơn hàng unique với format: `ORD-yyyyMMddHHmmss-XXXX`
- Safe null handling cho shipping info
- Chi tiết error logging với `DbEntityValidationException`

---

### 2. `Models/CreateOrderRequest.cs`

| Property | Trước | Sau |
|----------|-------|-----|
| `PaymentMethodId` | `int?` | `long?` |

---

### 3. `purchase_orders.cs`

**Thêm mới:**
```csharp
public Nullable<long> branch_id { get; set; }
public virtual branch branch { get; set; }
```

**Lý do:** Sync với database schema - bảng `purchase_orders` có cột `branch_id`.

---

### 4. `branch.cs`

**Thêm vào constructor:**
```csharp
this.purchase_orders = new HashSet<purchase_orders>();
```

**Thêm navigation property:**
```csharp
public virtual ICollection<purchase_orders> purchase_orders { get; set; }
```

**Lý do:** Tạo relationship hai chiều giữa `branch` và `purchase_orders`.

---

## 📄 CÁC FILE MỚI TẠO

### 1. `CORS_AND_ENTITY_FIX_GUIDE.md`
Hướng dẫn sửa lỗi CORS và đồng bộ Entity Model:
- Giải thích nguyên nhân lỗi CORS (`Network Error`)
- Các bước accept SSL certificate
- So sánh Entity vs Database schema

### 2. `API_ORDERS_FIX_SUMMARY.md`
Tổng kết các lỗi đã phát hiện và sửa trong API Orders:
- Sai kiểu dữ liệu (`int` vs `long`)
- Composite Primary Key violation
- Foreign Key violation
- Thiếu Transaction
- Entity Model không sync

### 3. `CHANGELOG_2025_12_23.md` (file này)
Ghi nhận tất cả thay đổi trong phiên làm việc.

---

## 🐛 CÁC LỖI ĐÃ SỬA

### Lỗi 1: Sai kiểu dữ liệu (Type Mismatch)
- **Vấn đề:** `GetCurrentUserId()` trả về `int?` nhưng `user.id` là `long`
- **Hậu quả:** Có thể mất dữ liệu khi cast từ `long` sang `int`
- **Sửa:** Đổi return type thành `long?`

### Lỗi 2: Navigation Property sai tên
- **Vấn đề:** Code dùng `d.product_variant` nhưng entity là `d.product_variants`
- **Sửa:** Đổi thành tên đúng

### Lỗi 3: Entity thiếu `branch_id`
- **Vấn đề:** Database có `branch_id` nhưng entity `purchase_orders` không có
- **Sửa:** Thêm property và navigation

### Lỗi 4: Composite Key Violation
- **Vấn đề:** Nếu cart có 2 items cùng `product_variant_id` → Insert order_details fail
- **Sửa:** Group cart items by `product_variant_id` trước khi insert

### Lỗi 5: Foreign Key Violation
- **Vấn đề:** Hardcode `payment_method_id = 1` mà không kiểm tra tồn tại
- **Sửa:** Kiểm tra và fallback

### Lỗi 6: Thiếu Transaction
- **Vấn đề:** Nếu insert order OK nhưng order_details fail → data inconsistent
- **Sửa:** Wrap trong `DbTransaction`

---

## 📊 SO SÁNH TRƯỚC/SAU

### `purchase_orders` Entity

| Property | Trước | Sau |
|----------|-------|-----|
| `branch_id` | ❌ Không có | ✅ `Nullable<long>` |
| `branch` navigation | ❌ Không có | ✅ `virtual branch` |

### `branch` Entity

| Property | Trước | Sau |
|----------|-------|-----|
| `purchase_orders` collection | ❌ Không có | ✅ `ICollection<purchase_orders>` |

### API Response `GetMyOrders`

| Field | Trước | Sau |
|-------|-------|-----|
| `UserId` | ❌ | ✅ |
| `ShippingRecipientName` | ❌ | ✅ |
| `ShippingRecipientPhone` | ❌ | ✅ |
| `ShippingAddress` | ❌ | ✅ |
| `SubTotal` | ❌ | ✅ |
| `ShippingFee` | ❌ | ✅ |
| `DiscountAmount` | ❌ | ✅ |
| `BranchId` | ❌ | ✅ |
| `BranchName` | ❌ | ✅ |
| `UpdatedAt` | ❌ | ✅ |
| `Items` | ❌ (bị comment) | ✅ Đầy đủ |
| `Payment` | ❌ (bị comment) | ✅ Đầy đủ |

---

## ⚠️ LƯU Ý SAU KHI SỬA

1. **Rebuild project** trong Visual Studio
2. **Restart IIS Express** để áp dụng thay đổi
3. **Accept SSL certificate** nếu gặp lỗi CORS
4. Đảm bảo database có:
   - Ít nhất 1 record trong `payment_methods` với `is_active = 1`
   - User đã đăng ký và có cart items

---

**Thời gian:** 23/12/2025, 12:57 - 17:50
**Số file đã sửa:** 4
**Số file mới tạo:** 3
