# Hướng Dẫn Tích Hợp Warehouse Transfers & POS

## 📋 Tổng Quan

Tài liệu này hướng dẫn cài đặt và sử dụng các tính năng:
1. **Warehouse Transfers**: Chuyển hàng từ kho tổng về chi nhánh
2. **POS Checkout**: Bán hàng tại quầy chi nhánh

## 🔧 Bước 1: Cài Đặt Database

### Chạy SQL Scripts

Mở **SQL Server Management Studio** (SSMS) và thực hiện theo thứ tự:

```sql
-- Chạy lần lượt các file trong thư mục Database/
```

**Thứ tự thực hiện:**

1. **01_CreateWarehouseTransferDetails.sql**
   - Tạo bảng `warehouse_transfer_details`
   - Lưu chi tiết sản phẩm trong mỗi phiếu chuyển kho

2. **02_CreateTableTypes.sql**
   - Tạo `CartItemTableType` cho POS checkout

3. **03_CreateTriggers.sql**
   - `trg_Transfer_OnShip`: Trừ hàng khi xuất kho (pending → shipping)
   - `trg_Transfer_OnReceive`: Cộng hàng khi nhận (shipping → completed)
   - `trg_Transfer_OnReturn`: Hoàn hàng (shipping → returned)

4. **04_CreateStoredProcedures.sql**
   - `sp_ProcessTransferIssue`: Xử lý hàng lỗi/hỏng
   - `sp_POS_Checkout_Classic`: Thanh toán tại quầy

**Hoặc chạy tất cả cùng lúc:**

```bash
cd Database
sqlcmd -S localhost -d perw -U sa -P Phu@232005 -i 00_RunAll.sql
```

### Kiểm Tra Cài Đặt

```sql
-- Kiểm tra bảng
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'warehouse_transfer_details';

-- Kiểm tra triggers
SELECT name, object_name(parent_id) as table_name 
FROM sys.triggers 
WHERE name LIKE 'trg_Transfer%';

-- Kiểm tra procedures
SELECT name FROM sys.procedures 
WHERE name IN ('sp_ProcessTransferIssue', 'sp_POS_Checkout_Classic');

-- Kiểm tra table types
SELECT name FROM sys.types 
WHERE is_table_type = 1 AND name = 'CartItemTableType';
```

## 🏗️ Bước 2: Rebuild Solution

1. Mở project trong **Visual Studio**
2. **Clean Solution**: `Build > Clean Solution`
3. **Rebuild Solution**: `Build > Rebuild Solution`
4. Đảm bảo không có lỗi build

## 🚀 Bước 3: Chạy Ứng Dụng

### Khởi động Server

1. Nhấn **F5** hoặc **Debug > Start Debugging**
2. Ứng dụng sẽ mở tại `https://localhost:44377/`

### Đăng Nhập

- Sử dụng tài khoản warehouse_manager hoặc admin
- Navigate đến **Warehouse area**

## 📦 Tính Năng Warehouse Transfers

### Luồng Hoạt Động

```
1. TẠO PHIẾU (Status: pending)
   ↓
2. XUẤT KHO (Status: shipping) → Trigger trừ inventory
   ↓
3a. NHẬN HÀNG (Status: completed) → Trigger cộng branch_inventories
   HOẶC
3b. HOÀN TRẢ (Status: returned) → Trigger hoàn lại inventory
```

### Quy Trình Sử Dụng

#### 1. Tạo Phiếu Chuyển Kho

**URL:** `/Warehouse/Transfers/Create`

**Các bước:**
1. Chọn **Kho Nguồn** (warehouse)
2. Chọn **Chi Nhánh Đích** (branch)
3. Nhập ghi chú (tùy chọn)
4. Click **"Thêm Sản Phẩm"**:
   - Chọn sản phẩm từ dropdown
   - Hệ thống tự động hiển thị tồn kho
   - Nhập số lượng cần chuyển
   - Thêm ghi chú (tùy chọn)
5. Click **"Tạo Phiếu Chuyển"**

**Kết quả:**
- Phiếu được tạo với status = `pending`
- Chưa ảnh hưởng đến inventory

#### 2. Xuất Kho

**URL:** `/Warehouse/Transfers/Details/{id}`

**Các bước:**
1. Vào trang chi tiết phiếu chuyển
2. Click button **"Xuất Kho"**
3. Xác nhận

**Kết quả:**
- Status chuyển từ `pending` → `shipping`
- **Trigger tự động chạy:**
  - Kiểm tra đủ hàng trong kho tổng
  - Trừ `inventories.quantity_on_hand`
  - Nếu không đủ → Rollback và báo lỗi

#### 3a. Xác Nhận Nhận Hàng

**Tại Chi Nhánh:**
1. Vào `/Warehouse/Transfers/Details/{id}`
2. Click **"Xác Nhận Nhận Hàng"**

**Kết quả:**
- Status chuyển `shipping` → `completed`
- **Trigger tự động chạy:**
  - Cộng `branch_inventories.quantity_on_hand`
  - Tự động INSERT nếu sản phẩm chưa có trong kho chi nhánh

#### 3b. Hoàn Trả

**Nếu có sự cố:**
1. Click **"Hoàn Trả"**

**Kết quả:**
- Status chuyển `shipping` → `returned`
- **Trigger tự động chạy:**
  - Cộng lại `inventories.quantity_on_hand` tại kho tổng

### Xử Lý Hàng Lỗi/Hỏng

**Nếu phát hiện hàng lỗi sau khi nhận:**

```csharp
// Gọi từ controller
EXEC sp_ProcessTransferIssue 
    @BranchID = 1,
    @TransferID = 123,
    @VariantID = 456,
    @BadQty = 2,
    @Note = N'Hàng bị hỏng trong quá trình vận chuyển'
```

**Kết quả:**
- Trừ số lượng hỏng từ `branch_inventories`

## 🛒 Tính Năng POS Checkout

### Quy Trình Bán Hàng Tại Quầy

**URL:** `/POS/Index`

#### Bước 1: Tìm Sản Phẩm
1. Nhập tên hoặc SKU vào ô tìm kiếm
2. Hệ thống hiển thị kết quả real-time
3. Click vào sản phẩm để thêm vào giỏ

#### Bước 2: Quản Lý Giỏ Hàng
- Tự động hiển thị trong panel bên phải
- Điều chỉnh số lượng
- Xóa sản phẩm nếu cần
- Tổng tiền tự động tính

#### Bước 3: Thanh Toán
1. Chọn phương thức thanh toán
2. Click **"THANH TOÁN"**
3. Xác nhận

**Stored Procedure Tự Động:**
```sql
sp_POS_Checkout_Classic
```

**Logic xử lý:**
1. Tính tổng tiền
2. Kiểm tra đủ hàng trong `branch_inventories`
3. Tạo `purchase_orders` (status = completed)
4. Tạo `purchase_order_details`
5. Trừ `branch_inventories.quantity_on_hand`
6. Tạo `payments` (status = completed)
7. Return OrderCode

**Nếu lỗi:**
- Rollback toàn bộ transaction
- Return error message

## 🔍 Testing & Verification

### Test Case 1: Chuyển Kho Thành Công

```sql
-- 1. Kiểm tra tồn kho ban đầu
SELECT warehouse_id, product_variant_id, quantity_on_hand 
FROM inventories WHERE warehouse_id = 1 AND product_variant_id = 101;

-- 2. Tạo phiếu chuyển 10 SP (variant 101) từ kho 1 sang chi nhánh 2
-- (Thực hiện qua UI)

-- 3. Xuất kho → Kiểm tra inventory giảm
SELECT quantity_on_hand FROM inventories 
WHERE warehouse_id = 1 AND product_variant_id = 101;
-- Expect: Giảm 10

-- 4. Nhận hàng → Kiểm tra branch_inventories tăng
SELECT quantity_on_hand FROM branch_inventories 
WHERE branch_id = 2 AND product_variant_id = 101;
-- Expect: Tăng 10
```

### Test Case 2: Không Đủ Hàng

```sql
-- Tạo phiếu chuyển số lượng > tồn kho
-- Khi xuất kho → Expect: Rollback + Error message
```

### Test Case 3: Hoàn Trả

```sql
-- 1. Xuất kho (inventory giảm)
-- 2. Hoàn trả (inventory tăng lại)
-- 3. Verify: inventory = giá trị ban đầu
```

### Test Case 4: POS Checkout

```sql
-- 1. Kiểm tra branch_inventories trước bán
SELECT quantity_on_hand FROM branch_inventories 
WHERE branch_id = 1 AND product_variant_id = 101;

-- 2. Bán 3 sản phẩm qua POS

-- 3. Verify branch_inventories giảm 3
-- 4. Verify purchase_orders có record mới
-- 5. Verify payments có record mới
```

## ⚠️ Lưu Ý Quan Trọng

### Triggers

> **Triggers sẽ TỰ ĐỘNG chạy khi UPDATE status**. Controllers chỉ cần:
> ```csharp
> transfer.status = "shipping";
> db.SaveChanges(); // Trigger tự động trừ inventory
> ```

### Transaction Handling

> Stored procedures đã có `BEGIN TRANSACTION` / `COMMIT`.  
> **KHÔNG** wrap thêm transaction ở controller.

### Status Transitions

Chỉ cho phép chuyển status theo lưu đồ:

```
pending → shipping → completed
             ↓
          returned
```

Không hợp lệ:
- pending → completed ❌
- completed → shipping ❌
- returned → completed ❌

### Error Handling

Tất cả lỗi từ triggers/procedures sẽ:
- Tự động ROLLBACK
- Throw exception với message rõ ràng
- Controllers catch và hiển thị cho user

## 🐛 Troubleshooting

### Lỗi: "warehouse_transfer_details không tồn tại"
**Giải pháp:** Chạy `01_CreateWarehouseTransferDetails.sql`

### Lỗi: "CartItemTableType không tồn tại"
**Giải pháp:** Chạy `02_CreateTableTypes.sql`

### Trigger không chạy
**Kiểm tra:**
```sql
SELECT * FROM sys.triggers WHERE name LIKE 'trg_Transfer%';
```

### Inventory không cập nhật
**Debug:**
1. Kiểm tra status có thay đổi đúng không
2. Xem trigger có enabled không
3. Check SQL Server error log

## 📞 Hỗ Trợ

Nếu gap vấn đề:
1. Kiểm tra SQL Server error log
2. Kiểm tra Visual Studio Output window
3. Enable SQL profiler để trace queries
4. Review implementation_plan.md

---

**Chúc bạn triển khai thành công! 🚀**
