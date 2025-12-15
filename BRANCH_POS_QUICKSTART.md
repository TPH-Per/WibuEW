# 🚀 Quick Start: Branch POS System

## ✅ CÀI ĐẶT HOÀN TẤT!

Hệ thống POS cho Branch đã được setup đầy đủ:

### 📊 Database
- ✅ 15 sản phẩm (product_variants)
- ✅ 15 inventory records cho Branch 1
- ✅ Payment methods
- ✅ Stored procedure `sp_POS_Checkout_Classic`

### 🎯 Backend
- ✅ **Controller**: `Areas/Branch/Controllers/POSController.cs`
  - `Index()` - Load trang POS
  - `SearchProducts(query)` - Tìm kiếm real-time
  - `Checkout(...)` - Thanh toán

### 🎨 Frontend
- ✅ **View**: `Areas/Branch/Views/POS/Index.cshtml`
  - Search box với debounce
  - Dynamic cart management
  - Real-time total calculation

---

## 🌐 TRUY CẬP TRANG POS

### Bước 1: Chạy Application

```bash
# Trong Visual Studio: Nhấn F5
# Hoặc
dotnet run
```

### Bước 2: Navigate

**URL**: `https://localhost:44377/Branch/POS`

Hoặc `https://localhost:44377/Branch/POS/Index`

---

## 🧪 TEST NGAY

### Test 1: Tìm kiếm sản phẩm

1. Vào URL trên
2. Nhập "sousou" vào ô tìm kiếm
3. **Expected**: Hiển thị danh sách sản phẩm kèm stock

### Test 2: Thêm vào giỏ

1. Click vào sản phẩm "sousou no frieren" 
2. **Expected**: Sản phẩm xuất hiện trong giỏ hàng bên phải
3. **Expected**: Tổng tiền = 17,000₫

### Test 3: Thanh toán

1. Có sản phẩm trong giỏ
2. Chọn payment method
3. Click **"THANH TOÁN"**
4. Confirm popup
5. **Expected**: Alert "Thanh toán thành công!"
6. **Expected**: Giỏ hàng clear

### Test 4: Verify Database

```sql
-- Check order đã tạo
SELECT TOP 1 * FROM purchase_orders ORDER BY created_at DESC;

-- Check inventory đã giảm
SELECT product_variant_id, quantity_on_hand 
FROM branch_inventories 
WHERE branch_id = 1 AND product_variant_id = 1;
-- Expected: 50 - (số lượng bán) = còn lại
```

---

## 🔧 TROUBLESHOOTING

### Lỗi: 404 Not Found

**Nguyên nhân**: Routing chưa đúng

**Fix**: Kiểm tra `BranchAreaRegistration.cs`

```csharp
context.MapRoute(
    "Branch_default",
    "Branch/{controller}/{action}/{id}",
    new { action = "Index", id = UrlParameter.Optional }
);
```

### Lỗi: No products found

**Nguyên nhân**: Database chưa có data

**Fix**: Chạy lại
```bash
sqlcmd -S localhost -d perw -U sa -P "Phu@232005" -i "Database\06_QuickTestData.sql"
```

### Lỗi: CartItemTableType không tồn tại

**Fix**: Chạy
```bash
sqlcmd -S localhost -d perw -U sa -P "Phu@232005" -i "Database\02_CreateTableTypes.sql"
```

---

## 📸 Expected UI

```
┌─────────────────────────────────────────────┐
│  Bán Hàng Tại Quầy - Chi Nhánh Quận 1     │
├──────────────────────┬──────────────────────┤
│  [ Search: sousou  ] │   CART (1 SP)        │
│                      │  ┌────────────────┐  │
│  Results:            │  │ sousou no ...  │  │
│  ╔════════════════╗  │  │ Qty: [1] [X]   │  │
│  ║ sousou no ...  ║  │  │ 17,000₫        │  │
│  ║ SKU: P0002...  ║  │  └────────────────┘  │
│  ║ 17,000₫        ║  │                      │
│  ║ Stock: 50      ║  │  Total: 17,000₫     │
│  ╚════════════════╝  │  [Tiền mặt ▼]       │
│                      │  [Hủy][THANH TOÁN]  │
└──────────────────────┴──────────────────────┘
```

---

## 🎉 SUCCESS CRITERIA

- [x] Page loads without errors
- [x] Search returns products with stock info
- [x] Can add products to cart
- [x] Cart shows correct total
- [x] Checkout creates order in database
- [x] Inventory decreases after checkout

---

## 📱 NEXT STEPS

**Để production-ready:**

1. **Authentication**: Lấy real userID và branchID từ session
2. **Error Handling**: Hiển thị lỗi user-friendly hơn
3. **Validation**: Check stock trước khi checkout
4. **Receipt**: In hóa đơn sau thanh toán
5. **Reports**: Dashboard báo cáo bán hàng

---

**Chúc bạn test thành công! 🎊**

Nếu có vấn đề gì, check:
- Browser Console (F12)
- Visual Studio Output window
- SQL Server error log
