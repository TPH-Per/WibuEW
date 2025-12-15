# Hướng Dẫn Test POS Tại Chi Nhánh

## 🎯 Mục Tiêu

Test đầy đủ chức năng bán hàng tại quầy (POS) cho chi nhánh, bao gồm:
- Tìm kiếm sản phẩm
- Thêm vào giỏ hàng
- Thanh toán qua stored procedure
- Verify inventory được trừ chính xác

---

## 📋 Checklist Chuẩn Bị

### 1. Chạy SQL Scripts

Thực hiện theo thứ tự:

```bash
# Trong SQL Server Management Studio (SSMS)
```

**Bước 1:** Tạo table + types + triggers + procedures
```sql
-- Chạy lần lượt hoặc dùng master script
USE perw;
GO

:r "D:\hqtcsdl\FINAL\LTW-HQT-QuanLyKhoNhaSach\Database\01_CreateWarehouseTransferDetails.sql"
:r "D:\hqtcsdl\FINAL\LTW-HQT-QuanLyKhoNhaSach\Database\02_CreateTableTypes.sql"
:r "D:\hqtcsdl\FINAL\LTW-HQT-QuanLyKhoNhaSach\Database\03_CreateTriggers.sql"
:r "D:\hqtcsdl\FINAL\LTW-HQT-QuanLyKhoNhaSach\Database\04_CreateStoredProcedures.sql"
```

**Bước 2:** Insert dữ liệu mẫu
```sql
:r "D:\hqtcsdl\FINAL\LTW-HQT-QuanLyKhoNhaSach\Database\05_InsertSampleData.sql"
```

**Verify:** Kiểm tra dữ liệu đã insert
```sql
SELECT COUNT(*) AS ProductVariants FROM product_variants;
SELECT COUNT(*) AS BranchInventories FROM branch_inventories WHERE branch_id = 1;
SELECT * FROM payment_methods WHERE is_active = 1;
SELECT * FROM branches;
```

Expected results:
- ProductVariants: 10
- BranchInventories (branch 1): 10
- Payment Methods: 4
- Branches: 3

###  2. Build & Run Application

```bash
# Trong Visual Studio
1. Clean Solution (Ctrl + Shift + B > Clean)
2. Rebuild Solution (Ctrl + Shift + B > Rebuild)
3. Run (F5)
```

Application sẽ mở tại: `https://localhost:44377/`

---

## 🧪 Test Scenarios

### Test Case 1: Truy Cập Trang POS

**URL:** `https://localhost:44377/Branch/POS`

**Expected:**
- ✅ Trang load thành công
- ✅ Hiển thị tên chi nhánh: "Chi Nhánh Quận 1"
- ✅ Payment methods dropdown có data
- ✅ Giỏ hàng trống với message "Giỏ hàng trống"

**Verify trong code:**
```sql
-- Check branch exists
SELECT * FROM branches WHERE id = 1;
```

---

### Test Case 2: Tìm Kiếm Sản Phẩm

**Steps:**
1. Nhập "nhà" vào ô tìm kiếm
2. Đợi kết quả (300ms debounce)

**Expected Results:**
- ✅ Hiển thị 2 sản phẩm: "Nhà Giả Kim - Bìa Cứng" và "Nhà Giả Kim - Bìa Mềm"
- ✅ Mỗi sản phẩm hiển thị:
  - Tên
  - SKU
  - Giá
  - Badge "Còn: XX" (màu xanh)

**Backend Query:**
```sql
-- Verify search works
SELECT pv.id, pv.name, pv.sku, pv.price,
       bi.quantity_on_hand as stock
FROM product_variants pv
LEFT JOIN branch_inventories bi ON pv.id = bi.product_variant_id AND bi.branch_id = 1
WHERE pv.name LIKE N'%nhà%'
   OR pv.product.name LIKE N'%nhà%';
```

**Test Edge Cases:**
- Tìm "xyz" (không có kết quả) → "Không tìm thấy sản phẩm"
- Nhập 1 ký tự → "Nhập ít nhất 2 ký tự để tìm kiếm"

---

### Test Case 3: Thêm Sản Phẩm Vào Giỏ

**Steps:**
1. Tìm "doraemon"
2. Click vào "Doraemon Tập 1" (stock = 40)
3. Click vào "Doraemon Tập 1" lần 2
4. Click vào "Doraemon Tập 1 - Tái Bản" (stock = 35)

**Expected:**
- ✅ Sau click lần 1: Giỏ hàng có 1 SP, số lượng = 1
- ✅ Sau click lần 2: Số lượng tăng lên 2
- ✅ Sau click lần 3: Giỏ hàng có 2 SP
- ✅ Tổng tiền = (25,000 × 2) + (28,000 × 1) = 78,000 ₫

**Verify Cart State:**
- Badge "3 SP" (2 + 1)
- Tổng tiền: "78,000 ₫"

---

### Test Case 4: Điều Chỉnh Số Lượng

**Steps:**
1. Có sản phẩm trong giỏ
2. Thay đổi số lượng thành 5
3. Thay đổi số lượng thành 100 (> stock)

**Expected:**
- ✅ Khi thay đổi thành 5: Tổng tiền cập nhật
- ❌ Khi thay đổi thành 100: Alert "Không đủ hàng (còn 40)"
- ✅ Số lượng revert về giá trị cũ

---

### Test Case 5: Xóa Sản Phẩm Khỏi Giỏ

**Steps:**
1. Có 2 SP trong giỏ
2. Click nút X (remove) ở SP thứ 1

**Expected:**
- ✅ SP bị xóa khỏi giỏ
- ✅ Tổng tiền cập nhật
- ✅ Badge cập nhật số lượng

---

### Test Case 6: Thanh Toán Thành Công

**Pre-condition:**
```sql
-- Check inventory trước khi bán
SELECT product_variant_id, quantity_on_hand 
FROM branch_inventories 
WHERE branch_id = 1 AND product_variant_id IN (5, 6);

-- Expected:
-- Variant 5 (Doraemon T1): 40
-- Variant 6 (Doraemon T1 TB): 35
```

**Steps:**
1. Thêm "Doraemon Tập 1" x2 vào giỏ
2. Thêm "Doraemon Tập 1 - Tái Bản" x3 vào giỏ
3. Chọn payment method: "Tiền mặt"
4. Click "THANH TOÁN"
5. Confirm popup

**Expected:**
- ✅ Loading/processing
- ✅ Alert "✅ Thanh toán thành công!"
- ✅ Giỏ hàng clear
- ✅ Focus về ô tìm kiếm

**Verify Database:**

```sql
-- 1. Check purchase_order được tạo
SELECT TOP 1 * 
FROM purchase_orders 
WHERE branch_id = 1 
ORDER BY created_at DESC;

-- Expected:
-- order_code: POS-1-20251211HHMMSS
-- status: completed
-- total_amount: (25000 * 2) + (28000 * 3) = 134,000

-- 2. Check purchase_order_details
DECLARE @OrderID BIGINT = (SELECT TOP 1 id FROM purchase_orders ORDER BY created_at DESC);

SELECT * FROM purchase_order_details WHERE order_id = @OrderID;

-- Expected: 2 rows
-- Row 1: variant 5, qty 2, price 25000, subtotal 50000
-- Row 2: variant 6, qty 3, price 28000, subtotal 84000

-- 3. Check inventory bị trừ
SELECT product_variant_id, quantity_on_hand 
FROM branch_inventories 
WHERE branch_id = 1 AND product_variant_id IN (5, 6);

-- Expected:
-- Variant 5: 40 - 2 = 38
-- Variant 6: 35 - 3 = 32

-- 4. Check payment record
SELECT * FROM payments WHERE order_id = @OrderID;

-- Expected:
-- payment_method_id: 1 (Tiền mặt)
-- amount: 134,000
-- status: completed
```

---

### Test Case 7: Thanh Toán Thất Bại (Không Đủ Hàng)

**Pre-condition:**
```sql
-- Giảm stock về 1
UPDATE branch_inventories 
SET quantity_on_hand = 1 
WHERE branch_id = 1 AND product_variant_id = 5;
```

**Steps:**
1. Refresh page (F5)
2. Tìm "Doraemon Tập 1"
3. Thêm vào giỏ x2 (không được, max stock = 1)
4. Thêm vào giỏ x1
5. Trong input số lượng, thay đổi thành 5
6. Click "THANH TOÁN"

**Expected:**
- ✅ Alert "Không đủ hàng (còn 1)" khi thay đổi SL
- ✅ Stored procedure ROLLBACK
- ❌ Không tạo purchase_order
- ❌ Inventory không thay đổi

**Verify:**
```sql
-- Inventory không đổi
SELECT quantity_on_hand 
FROM branch_inventories 
WHERE branch_id = 1 AND product_variant_id = 5;
-- Expected: vẫn = 1
```

---

### Test Case 8: Clear Cart (Hủy)

**Steps:**
1. Thêm 3 SP vào giỏ
2. Click "Hủy"
3. Confirm popup

**Expected:**
- ✅ Giỏ hàng clear
- ✅ Tổng tiền = 0 ₫
- ✅ Message "Giỏ hàng trống"

---

## 🔧 Debug Tips

### Check AJAX Requests

**Trong Browser Console (F12):**

```javascript
// Monitor search requests
// Network tab > Filter: SearchProducts

// Monitor checkout request
// Network tab > Filter: Checkout
// Check request payload and response
```

### Check SQL Server Logs

```sql
-- Enable profiler để xem stored procedure execution
-- Or check error log

EXEC sp_readerrorlog 0, 1, N'sp_POS_Checkout_Classic';
```

### Common Issues

**1. Lỗi "CartItemTableType không tồn tại"**
```sql
-- Solution: Chạy lại script
:r "02_CreateTableTypes.sql"
```

**2. Lỗi "branch_inventories không có data"**
```sql
-- Solution: Insert sample data
:r "05_InsertSampleData.sql"
```

**3. Lỗi 404 /Branch/POS**
```
-- Solution: Check routing trong BranchAreaRegistration.cs
-- Rebuild solution
```

---

## ✅ Acceptance Criteria

Hệ thống đạt yêu cầu khi:

- [x] Search sản phẩm real-time hoạt động
- [x] Add to cart với stock validation
- [x] Tổng tiền tính chính xác
- [x] Thanh toán thành công tạo order + payment
- [x] Inventory bị trừ chính xác
- [x] Stored procedure ROLLBACK khi lỗi
- [x] UI responsive và user-friendly

---

## 📊 Performance Check

```sql
-- Check execution time của stored procedure
SET STATISTICS TIME ON;

DECLARE @Cart CartItemTableType;
INSERT INTO @Cart VALUES (5, 2), (6, 3);

EXEC sp_POS_Checkout_Classic 
    @BranchID = 1,
    @UserID = 2,
    @PaymentMethodID = 1,
    @CartItems = @Cart;

SET STATISTICS TIME OFF;

-- Expected: < 100ms
```

---

**Chúc bạn test thành công! 🚀**
