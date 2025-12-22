# Tài liệu cập nhật - Chức năng theo dõi hóa đơn

**Ngày cập nhật:** 22/12/2025  
**Người thực hiện:** Antigravity AI Assistant

---

## Lịch sử cập nhật

### Lần 8 - 22/12/2025 16:51
**Sửa lỗi JavaScript Dashboard Chart**
- ✅ Fix `Uncaught SyntaxError: Unexpected number` trong Dashboard
- ✅ Sửa format số trong JavaScript: `@branch.Revenue` → `@(branch.Revenue.ToString("F2", InvariantCulture))`
- ✅ Escape tên chi nhánh: `@Html.Raw(Json.Encode(branch.BranchName))`
- ✅ Biểu đồ Chart.js hiển thị chính xác

### Lần 7 - 22/12/2025 16:22
**Sửa lỗi cuối cùng - Dashboard và Inventory**
- ✅ Làm rõ logic Inventory: Kho Tổng và Chi nhánh là 2 hệ thống RIÊNG BIỆT
- ✅ Cập nhật comment trong code để giải thích logic
- ✅ Dashboard chart sẽ hiển thị nếu có đơn completed trong 7 ngày
- ✅ Script TestData_InvoiceTracking.sql đã tạo sẵn đơn completed (dòng 185-217)

### Lần 6 - 22/12/2025 15:41
**Triển khai Quản lý Tồn kho Chi nhánh**
- ✅ Kết nối `BranchInventoriesController` với database
- ✅ Implement Index - Danh sách tồn kho tất cả chi nhánh với filter
- ✅ Implement Details - Chi tiết tồn kho từng chi nhánh
- ✅ Cập nhật `BranchInventoryViewModel` với đầy đủ properties
- ✅ Tạo Views: Index.cshtml và Details.cshtml
- ✅ Sửa `InventoriesController` - Thay stored procedure bằng LINQ query
- ✅ Tạo `TestData_Inventory.sql` để insert dữ liệu test

### Lần 5 - 22/12/2025 15:14
**Thêm biểu đồ doanh thu và filter chi nhánh**
- ✅ Implement Chart.js để vẽ biểu đồ doanh thu theo chi nhánh
- ✅ Thêm dropdown filter để lọc theo chi nhánh cụ thể
- ✅ Biểu đồ cập nhật động khi chọn chi nhánh khác
- ✅ Hiển thị tooltip với doanh thu và số đơn hàng
- ✅ Format trục Y với M (triệu) và K (nghìn)
- ✅ Sửa lỗi LINQ to Entities trong DashboardController

### Lần 4 - 22/12/2025 14:55
**Cập nhật Dashboard với dữ liệu thực và biểu đồ**
- ✅ Kết nối `DashboardController` với database
- ✅ Tính toán thống kê thực tế (doanh thu, đơn hàng, khách mới, tồn kho)
- ✅ Hiển thị đơn hàng mới nhất (top 5)
- ✅ Hiển thị sản phẩm bán chạy (top 5)
- ✅ **Thêm dữ liệu doanh thu theo chi nhánh** cho biểu đồ
- ✅ Thêm `BranchRevenueViewModel` vào `AdminReportViewModel`

### Lần 3 - 22/12/2025 14:47
**Sửa lỗi format tiền tệ và ngày giờ**
- ✅ Sửa format tiền trong bảng sản phẩm (Details.cshtml)
- ✅ Sửa format tổng tiền ở header
- ✅ Sửa format số tiền thanh toán
- ✅ Sửa format tiền trong danh sách đơn hàng (Index.cshtml)
- ✅ Sửa format ngày giờ: `@order.CreatedAt:dd/MM HH:mm` → `@order.CreatedAt.ToString("dd/MM HH:mm")`
- ✅ Thay đổi từ `.ToString("c0")` sang `string.Format("{0:N0} ₫", amount)`

### Lần 2 - 22/12/2025 14:43
**Sửa lỗi hiển thị trang chi tiết đơn hàng**
- ✅ Sửa lỗi Razor syntax ở dòng 55 (sai cú pháp `@Model.Payment?.CreatedAt:dd/MM HH:mm`)
- ✅ Thêm nút "Quay lại" để quay về danh sách đơn hàng
- ✅ Di chuyển badge trạng thái lên trên cùng để dễ nhìn

### Lần 1 - 22/12/2025 14:21
**Triển khai chức năng theo dõi hóa đơn**
- ✅ Kết nối database cho OrdersController và ReportsController
- ✅ Implement xem lịch sử bán hàng và báo cáo thống kê

---

## Tổng quan

Đã triển khai chức năng theo dõi hóa đơn với hai nghiệp vụ chính:
1. **Xem lịch sử bán hàng** - Hiển thị danh sách đơn hàng từ database với khả năng lọc theo trạng thái
2. **Báo cáo thống kê** - Tính toán và hiển thị các chỉ số thống kê thực tế từ database

---

## Chi tiết các file đã sửa đổi

### 1. OrdersController.cs
**Đường dẫn:** `DoAnLTWHQT\Areas\Admin\Controllers\OrdersController.cs`

#### Thay đổi chính:

##### a) Thêm DbContext để kết nối database
```csharp
private readonly perwEntities db = new perwEntities();
```
- Khởi tạo instance của Entity Framework DbContext để truy vấn database

##### b) Cập nhật phương thức `Index()` - Xem lịch sử bán hàng
**Chức năng:**
- Lấy danh sách tất cả đơn hàng từ bảng `purchase_orders`
- Lọc theo trạng thái (pending, processing, completed) nếu được chọn
- Sắp xếp theo thời gian tạo (mới nhất trước)
- Loại bỏ các đơn hàng đã xóa (deleted_at != null)

**Dữ liệu hiển thị:**
- Mã đơn hàng (order_code)
- Chi nhánh (branch.name)
- Khách hàng (user.full_name hoặc shipping_recipient_name)
- Trạng thái đơn hàng (status)
- Tổng tiền (total_amount)
- Thời gian tạo (created_at)
- Chi tiết sản phẩm (purchase_order_details)
- Thông tin thanh toán (payments)

**Truy vấn database:**
```csharp
var ordersQuery = db.purchase_orders
    .Where(o => o.deleted_at == null)
    .OrderByDescending(o => o.created_at);

// Lọc theo trạng thái
if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
{
    ordersQuery = (IOrderedQueryable<purchase_orders>)ordersQuery.Where(o => o.status == status);
}
```

##### c) Cập nhật phương thức `Details()` - Xem chi tiết hóa đơn
**Chức năng:**
- Lấy thông tin chi tiết một đơn hàng cụ thể theo ID
- Hiển thị đầy đủ thông tin: sản phẩm, số lượng, giá, thanh toán
- Trả về 404 nếu không tìm thấy đơn hàng

**Truy vấn database:**
```csharp
var order = db.purchase_orders
    .Where(o => o.id == id && o.deleted_at == null)
    .FirstOrDefault();
```

##### d) Mapping dữ liệu từ Entity sang ViewModel
**Các trường được map:**
- `order_code` → OrderCode
- `branch.name` → Branch
- `user.full_name` hoặc `shipping_recipient_name` → Customer
- `status` → Status
- `total_amount` → TotalAmount
- `created_at` → CreatedAt
- `purchase_order_details` → Lines (danh sách sản phẩm)
  - `product_variants.name` → VariantName
  - `product_variants.product.name` → ProductName
  - `quantity` → Quantity
  - `price_at_purchase` → UnitPrice
- `payments` → Payment (thông tin thanh toán)
  - `payment_methods.name` → Method
  - `amount` → Amount
  - `status` → Status

##### e) Thêm phương thức Dispose
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        db.Dispose();
    }
    base.Dispose(disposing);
}
```
- Giải phóng tài nguyên DbContext khi controller bị hủy

---

### 2. ReportsController.cs
**Đường dẫn:** `DoAnLTWHQT\Areas\Admin\Controllers\ReportsController.cs`

#### Thay đổi chính:

##### a) Thêm DbContext
```csharp
private readonly perwEntities db = new perwEntities();
```

##### b) Cập nhật phương thức `Index()` - Báo cáo thống kê
**Các chỉ số được tính toán:**

1. **Doanh thu 7 ngày qua**
   - Tính tổng `total_amount` của các đơn hàng `completed` trong 7 ngày
   - So sánh với 7 ngày trước đó để tính % tăng/giảm
   ```csharp
   var revenueSevenDays = db.purchase_orders
       .Where(o => o.created_at >= sevenDaysAgo && o.deleted_at == null && o.status == "completed")
       .Sum(o => (decimal?)o.total_amount) ?? 0;
   ```

2. **Tỷ lệ hoàn thành đơn**
   - Tính % đơn hàng `completed` / tổng đơn hàng trong 7 ngày
   ```csharp
   var completionRate = totalOrders > 0 
       ? (decimal)completedOrders / totalOrders * 100 
       : 0;
   ```

3. **Tổng số đơn hàng**
   - Đếm tất cả đơn hàng chưa bị xóa
   - Phân loại theo trạng thái: pending, processing, completed
   ```csharp
   var totalOrdersCount = db.purchase_orders
       .Where(o => o.deleted_at == null)
       .Count();
   ```

4. **Giá trị trung bình đơn hàng**
   - Tính doanh thu / số lượng đơn hàng
   ```csharp
   Value = totalOrdersCount > 0 ? FormatCurrency(revenueSevenDays / totalOrdersCount) : "0đ"
   ```

##### c) Thêm phương thức `FormatCurrency()`
**Chức năng:** Format số tiền theo đơn vị phù hợp
- Tỷ (B): >= 1,000,000,000
- Triệu (M): >= 1,000,000
- Nghìn (K): >= 1,000
- Đồng (đ): < 1,000

```csharp
private string FormatCurrency(decimal amount)
{
    if (amount >= 1_000_000_000)
        return $"{amount / 1_000_000_000:0.##}B";
    if (amount >= 1_000_000)
        return $"{amount / 1_000_000:0.##}M";
    if (amount >= 1_000)
        return $"{amount / 1_000:0.##}K";
    return $"{amount:0}đ";
}
```

##### d) Thêm phương thức Dispose
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        db.Dispose();
    }
    base.Dispose(disposing);
}
```

---

### 3. DashboardController.cs
**Đường dẫn:** `DoAnLTWHQT\Areas\Admin\Controllers\DashboardController.cs`

#### Thay đổi chính:

##### a) Thêm DbContext
```csharp
private readonly perwEntities db = new perwEntities();
```

##### b) Cập nhật phương thức `Index()` - Dashboard với dữ liệu thực

**Các thống kê được tính toán:**

1. **Doanh thu 7 ngày qua**
   - Tính tổng `total_amount` của các đơn `completed` trong 7 ngày
   - So sánh với 7 ngày trước để tính % thay đổi
   ```csharp
   var revenueSevenDays = db.purchase_orders
       .Where(o => o.created_at >= sevenDaysAgo && o.deleted_at == null && o.status == "completed")
       .Sum(o => (decimal?)o.total_amount) ?? 0;
   ```

2. **Đơn hàng hoàn thành**
   - Đếm số đơn `completed` trong 7 ngày
   - Tính tỷ lệ hoàn thành (%)
   ```csharp
   var completionRate = totalOrders > 0 
       ? (decimal)completedOrders / totalOrders * 100 
       : 0;
   ```

3. **Khách hàng mới**
   - Đếm số user được tạo trong 7 ngày qua
   ```csharp
   var newCustomers = db.users
       .Where(u => u.created_at >= sevenDaysAgo && u.deleted_at == null)
       .Count();
   ```

4. **Tồn kho cảnh báo**
   - Đếm số SKU có `quantity_on_hand < reorder_level`
   ```csharp
   var lowStockCount = db.branch_inventories
       .Where(bi => bi.quantity_on_hand < bi.reorder_level)
       .Select(bi => bi.product_variant_id)
       .Distinct()
       .Count();
   ```

##### c) Doanh thu theo chi nhánh (cho biểu đồ)
**Chức năng:** Tính doanh thu của từng chi nhánh trong 7 ngày qua

```csharp
var revenueByBranch = db.purchase_orders
    .Where(o => o.deleted_at == null && o.status == "completed" && o.created_at >= sevenDaysAgo)
    .GroupBy(o => o.branch.name)
    .Select(g => new BranchRevenueViewModel
    {
        BranchName = g.Key ?? "Không xác định",
        Revenue = g.Sum(o => o.total_amount),
        OrderCount = g.Count()
    })
    .OrderByDescending(b => b.Revenue)
    .ToList();
```

**Dữ liệu trả về:**
- `BranchName` - Tên chi nhánh
- `Revenue` - Tổng doanh thu
- `OrderCount` - Số lượng đơn hàng

##### d) Đơn hàng mới nhất (Top 5)
```csharp
TopOrders = db.purchase_orders
    .Where(o => o.deleted_at == null)
    .OrderByDescending(o => o.created_at)
    .Take(5)
    .Select(o => new PurchaseOrderListItemViewModel { ... })
    .ToList()
```

##### e) Sản phẩm bán chạy (Top 5)
**Logic:** Group theo sản phẩm, tính tổng số lượng bán, sắp xếp giảm dần

```csharp
BestSellers = db.purchase_order_details
    .Where(d => d.purchase_orders.deleted_at == null && 
                d.purchase_orders.status == "completed" &&
                d.purchase_orders.created_at >= sevenDaysAgo)
    .GroupBy(d => new 
    { 
        ProductId = d.product_variants.product.id,
        ProductName = d.product_variants.product.name,
        CategoryName = d.product_variants.product.category.name,
        SupplierName = d.product_variants.product.supplier.name
    })
    .Select(g => new { ... })
    .OrderByDescending(p => p.TotalQuantity)
    .Take(5)
    .ToList()
```

##### f) Thêm phương thức FormatCurrency()
```csharp
private string FormatCurrency(decimal amount)
{
    if (amount >= 1_000_000_000)
        return $"{amount / 1_000_000_000:0.##}B";
    if (amount >= 1_000_000)
        return $"{amount / 1_000_000:0.##}M";
    if (amount >= 1_000)
        return $"{amount / 1_000:0.##}K";
    return $"{amount:0}đ";
}
```

##### g) Thêm phương thức Dispose
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        db.Dispose();
    }
    base.Dispose(disposing);
}
```

---

### 4. Details.cshtml & Index.cshtml
**Đường dẫn:** 
- `DoAnLTWHQT\Areas\Admin\Views\Orders\Details.cshtml`
- `DoAnLTWHQT\Areas\Admin\Views\Orders\Index.cshtml`

#### Thay đổi chính:

##### a) Sửa lỗi Razor syntax
**Lỗi:** Dùng sai cú pháp `@Model.Property:format`

**Ví dụ lỗi:**
```html
<!-- SAI -->
<td>@order.TotalAmount.ToString("c0")</td>
<td>@order.CreatedAt:dd/MM HH:mm</td>
<span>@Model.Payment?.CreatedAt:dd/MM HH:mm</span>
```

**Đã sửa:**
```html
<!-- ĐÚNG -->
<td>@string.Format("{0:N0} ₫", order.TotalAmount)</td>
<td>@order.CreatedAt.ToString("dd/MM HH:mm")</td>
@if (Model.Payment != null)
{
    <span>@Model.Payment.CreatedAt.ToString("dd/MM HH:mm")</span>
}
```

##### b) Thêm nút "Quay lại" (Details.cshtml)
```html
<div class="d-flex justify-content-between align-items-center mb-3">
    <div>
        <a href="@Url.Action("Index", "Orders")" class="btn btn-outline-secondary btn-sm">
            <i class="bi bi-arrow-left"></i> Quay lại
        </a>
    </div>
    <div class="text-end">
        <span class="badge bg-light text-dark text-uppercase">@Model.Status</span>
    </div>
</div>
```

##### c) Format tiền tệ đồng nhất
**Tất cả số tiền đều dùng format:**
```csharp
@string.Format("{0:N0} ₫", amount)
```

**Kết quả:** `20.000 ₫` (có dấu phẩy ngăn cách hàng nghìn)

---

### 5. AdminReportViewModel.cs
**Đường dẫn:** `DoAnLTWHQT\ViewModels\Admin\AdminReportViewModel.cs`

#### Thay đổi chính:

##### a) Thêm property BranchRevenues
```csharp
public class AdminReportViewModel
{
    // ... existing properties ...
    
    // Dữ liệu cho biểu đồ doanh thu theo chi nhánh
    public IList<BranchRevenueViewModel> BranchRevenues { get; set; } = new List<BranchRevenueViewModel>();
}
```

##### b) Thêm BranchRevenueViewModel
```csharp
// ViewModel cho doanh thu theo chi nhánh
public class BranchRevenueViewModel
{
    public string BranchName { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
```

---

### 6. TestData_InvoiceTracking.sql
**Đường dẫn:** `Database\TestData_InvoiceTracking.sql`

#### Mục đích:
Script SQL để kiểm tra và thêm dữ liệu test vào database

#### Chức năng:
1. Kiểm tra dữ liệu hiện tại trong database
2. Thêm dữ liệu test nếu chưa có:
   - 8 đơn hàng (1 pending, 1 processing, 6 completed)
   - Chi tiết sản phẩm cho mỗi đơn
   - Thông tin thanh toán
3. Hiển thị kết quả sau khi thêm

#### Cách sử dụng:
```sql
-- Chạy trong SQL Server Management Studio
-- Database: perw
```



## Các bảng database sử dụng

### 1. purchase_orders
**Mục đích:** Lưu thông tin đơn hàng
**Các trường sử dụng:**
- `id` - ID đơn hàng
- `order_code` - Mã đơn hàng
- `user_id` - ID khách hàng
- `branch_id` - ID chi nhánh
- `status` - Trạng thái (pending, processing, completed)
- `total_amount` - Tổng tiền
- `shipping_recipient_name` - Tên người nhận (nếu không có user)
- `created_at` - Thời gian tạo
- `deleted_at` - Thời gian xóa (soft delete)

### 2. purchase_order_details
**Mục đích:** Lưu chi tiết sản phẩm trong đơn hàng
**Các trường sử dụng:**
- `order_id` - ID đơn hàng
- `product_variant_id` - ID biến thể sản phẩm
- `quantity` - Số lượng
- `price_at_purchase` - Giá tại thời điểm mua

### 3. payments
**Mục đích:** Lưu thông tin thanh toán
**Các trường sử dụng:**
- `id` - ID thanh toán
- `order_id` - ID đơn hàng
- `payment_method_id` - ID phương thức thanh toán
- `amount` - Số tiền
- `status` - Trạng thái thanh toán
- `created_at` - Thời gian tạo

### 4. product_variants
**Mục đích:** Lưu thông tin biến thể sản phẩm
**Các trường sử dụng:**
- `id` - ID biến thể
- `product_id` - ID sản phẩm
- `name` - Tên biến thể (ví dụ: "Size 39", "Màu đỏ")

### 5. product
**Mục đích:** Lưu thông tin sản phẩm
**Các trường sử dụng:**
- `id` - ID sản phẩm
- `name` - Tên sản phẩm

### 6. branch
**Mục đích:** Lưu thông tin chi nhánh
**Các trường sử dụng:**
- `id` - ID chi nhánh
- `name` - Tên chi nhánh

### 7. user
**Mục đích:** Lưu thông tin người dùng/khách hàng
**Các trường sử dụng:**
- `id` - ID người dùng
- `full_name` - Họ tên

### 8. payment_methods
**Mục đích:** Lưu thông tin phương thức thanh toán
**Các trường sử dụng:**
- `id` - ID phương thức
- `name` - Tên phương thức (COD, VNPay, Momo, etc.)

---

## Nghiệp vụ đã triển khai

### 1. Xem lịch sử bán hàng
**URL:** `/Admin/Orders/Index?status={status}`

**Tham số:**
- `status` (optional): "all", "pending", "processing", "completed"

**Chức năng:**
- Hiển thị danh sách tất cả đơn hàng
- Lọc theo trạng thái
- Sắp xếp theo thời gian (mới nhất trước)
- Hiển thị thông tin: mã đơn, chi nhánh, khách hàng, trạng thái, tổng tiền, thời gian

### 2. Xem chi tiết hóa đơn
**URL:** `/Admin/Orders/Details/{id}`

**Chức năng:**
- Hiển thị thông tin đầy đủ của một đơn hàng
- Danh sách sản phẩm với số lượng và giá
- Thông tin thanh toán
- Thông tin khách hàng và chi nhánh

### 3. Báo cáo thống kê
**URL:** `/Admin/Reports/Index`

**Chức năng:**
- Hiển thị 4 chỉ số chính:
  1. Doanh thu 7 ngày (với % thay đổi so với tuần trước)
  2. Tỷ lệ hoàn thành đơn (%)
  3. Tổng số đơn hàng (phân loại theo trạng thái)
  4. Giá trị trung bình đơn hàng

---

## Lưu ý kỹ thuật

### 1. Soft Delete
- Tất cả truy vấn đều lọc `deleted_at == null` để chỉ lấy dữ liệu chưa bị xóa

### 2. Null Safety
- Sử dụng null-conditional operator (`?.`) và null-coalescing operator (`??`) để xử lý giá trị null
- Ví dụ: `o.user?.full_name ?? o.shipping_recipient_name ?? "Khách lẻ"`

### 3. Performance
- Sử dụng LINQ to Entities để tạo SQL query hiệu quả
- Chỉ load dữ liệu cần thiết thông qua projection

### 4. Date Handling
- Chuyển đổi `DateTime` sang `DateTimeOffset` để hiển thị đúng timezone
- Sử dụng `DateTime.Now` để tính toán khoảng thời gian

### 5. Decimal Calculations
- Sử dụng `(decimal?)` cast để tránh lỗi khi Sum() trên collection rỗng
- Format số tiền theo đơn vị phù hợp (B/M/K/đ)

---

## Kết luận

Đã hoàn thành việc triển khai chức năng theo dõi hóa đơn và cập nhật Dashboard với:

### Controllers
- ✅ **OrdersController** - Kết nối database thực tế, xem lịch sử bán hàng với lọc theo trạng thái
- ✅ **ReportsController** - Báo cáo thống kê tự động tính toán từ database
- ✅ **DashboardController** - Dashboard với dữ liệu thực + dữ liệu cho biểu đồ doanh thu theo chi nhánh

### Views
- ✅ **Details.cshtml** - Sửa lỗi Razor syntax, thêm nút "Quay lại", format tiền tệ đúng
- ✅ **Index.cshtml** - Sửa format tiền tệ và ngày giờ

### ViewModels
- ✅ **AdminReportViewModel** - Thêm `BranchRevenues` property
- ✅ **BranchRevenueViewModel** - ViewModel mới cho dữ liệu biểu đồ

### Database
- ✅ **TestData_InvoiceTracking.sql** - Script thêm dữ liệu test

### Tính năng đã triển khai
1. ✅ Xem lịch sử đơn hàng với filter theo trạng thái
2. ✅ Xem chi tiết hóa đơn với đầy đủ thông tin
3. ✅ Báo cáo thống kê 4 chỉ số chính
4. ✅ Dashboard với dữ liệu thực:
   - 4 widget thống kê
   - Top 5 đơn hàng mới nhất
   - Top 5 sản phẩm bán chạy
   - **Dữ liệu doanh thu theo chi nhánh** (sẵn sàng cho biểu đồ)
5. ✅ Format tiền tệ đồng nhất: `20.000 ₫`
6. ✅ Format ngày giờ: `22/12 14:30`
7. ✅ Xử lý an toàn với null values
8. ✅ Dispose DbContext đúng cách

### Dữ liệu cho biểu đồ
Model `BranchRevenues` đã sẵn sàng trong Dashboard với:
- `BranchName` - Tên chi nhánh
- `Revenue` - Doanh thu (decimal)
- `OrderCount` - Số đơn hàng (int)

Có thể sử dụng Chart.js hoặc thư viện khác để vẽ biểu đồ từ dữ liệu này.

Hệ thống giờ đây có thể hiển thị dữ liệu thực tế từ database và cung cấp thông tin hữu ích cho việc quản lý bán hàng.
