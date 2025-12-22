# WAREHOUSE TRANSFER WORKFLOW - CẬP NHẬT HOÀN CHỈNH

## 📋 **Tổng quan thay đổi**

### **1. Database (SQL Server)**

- **File**: `08_Update_Transfer_Status_Flow.sql`

#### **Stored Procedures Mới:**

1. `sp_Warehouse_CreateTransfer` - **Đã sửa**
   - Mặc định status = `'Shipping'` (thay vì 'Pending')

2. `sp_Warehouse_ApproveRequestToShipping` - **MỚI**
   - Warehouse duyệt phiếu từ `Requested` → `Shipping`

3. `sp_Warehouse_CancelRequest` - **MỚI**
   - Warehouse hủy phiếu `Requested` → `Cancelled`

4. `sp_Branch_ConfirmDelivery` - **MỚI**
   - Branch xác nhận nhận hàng: `Shipping` → `Delivered`
   - Cập nhật tồn kho warehouse (trừ) và branch (cộng)

---

### **2. Backend (.NET MVC)**

#### **Warehouse/Controllers/TransfersController.cs** - Đã cập nhật

| Action | Thay đổi | Stored Procedure |
|--------|----------|------------------|
| `Create` | Status mặc định `"Shipping"` | `sp_Warehouse_CreateTransfer` |
| `ApproveToShipping` | **MỚI** - Duyệt Requested → Shipping | `sp_Warehouse_ApproveRequestToShipping` |
| `CancelRequest` | **MỚI** - Hủy phiếu Requested | `sp_Warehouse_CancelRequest` |
| ~~`Approve`~~ | **ĐÃ XÓA** | - |
| ~~`Reject`~~ | **ĐÃ ĐỔI TÊN** → `CancelRequest` | - |

#### **Branch/Controllers/TransfersController.cs** - Đã cập nhật

| Action | Thay đổi | Stored Procedure |
|--------|----------|------------------|
| `ConfirmDelivery` | **MỚI** - Xác nhận Shipping → Delivered | `sp_Branch_ConfirmDelivery` |

---

## 🔄 **Workflow Mới**

### **LUỒNG 1: Branch tạo yêu cầu (Request Flow)**

```
1. [Branch] Tạo phiếu yêu cầu
   POST /Branch/Transfers/Create
   → Status: Requested

2. [Warehouse] Xử lý yêu cầu:
   A. Duyệt:
      POST /Warehouse/Transfers/ApproveToShipping
      → Status: Shipping
   
   B. Hủy:
      POST /Warehouse/Transfers/CancelRequest
      → Status: Cancelled

3. [Branch] Xác nhận nhận hàng
   POST /Branch/Transfers/ConfirmDelivery
   → Status: Delivered
   → Cập nhật inventory (trừ warehouse, cộng branch)
```

### **LUỒNG 2: Warehouse tự tạo phiếu (Direct Flow)**

```
1. [Warehouse] Tạo phiếu gửi hàng
   POST /Warehouse/Transfers/Create
   → Status: Shipping (mặc định)

2. [Branch] Xác nhận nhận hàng
   POST /Branch/Transfers/ConfirmDelivery
   → Status: Delivered
   → Cập nhật inventory
```

---

## 🔒 **Quyền hạn Role**

### **role_warehouse**

- ✅ Tạo phiếu xuất hàng (mặc định Shipping)
- ✅ Duyệt phiếu yêu cầu từ Branch (Requested → Shipping)
- ✅ Hủy phiếu yêu cầu từ Branch (Requested → Cancelled)
- ❌ KHÔNG thể xác nhận Delivered

### **role_branch**

- ✅ Tạo phiếu yêu cầu nhập hàng (Requested)
- ✅ Xác nhận đã nhận hàng (Shipping → Delivered)
- ❌ KHÔNG thể tự duyệt phiếu của mình

---

## 📊 **Status Lifecycle**

```
┌─────────────┐
│  Requested  │  ← Branch tạo yêu cầu
└──────┬──────┘
       │
       ├─[Warehouse Approve]──→ ┌───────────┐
       │                          │  Shipping  │
       │                          └─────┬─────┘
       │                                │
       │                         [Branch Confirm]
       │                                │
       │                                ↓
       │                          ┌───────────┐
       │                          │ Delivered  │  ← Hoàn thành
       │                          └───────────┘
       │
       └─[Warehouse Cancel]────→ ┌───────────┐
                                  │ Cancelled  │
                                  └───────────┘


HOẶC

┌───────────┐
│  Shipping  │  ← Warehouse tạo trực tiếp
└─────┬─────┘
      │
      │ [Branch Confirm]
      │
      ↓
┌───────────┐
│ Delivered  │  ← Hoàn thành
└───────────┘
```

---

## ⚠️ **Validation & Business Rules**

### **sp_Warehouse_ApproveRequestToShipping**

- Chỉ duyệt được phiếu có status = `'Requested'`
- Phiếu phải tồn tại

### **sp_Warehouse_CancelRequest**

- Chỉ hủy được phiếu có status = `'Requested'`
- Bắt buộc nhập lý do hủy

### **sp_Branch_ConfirmDelivery**

- Chỉ xác nhận được phiếu có status = `'Shipping'`
- Branch phải là người nhận (to_branch_id == @branch_id)
- Kiểm tra tồn kho warehouse đủ hay không
- Tự động cập nhật inventory khi xác nhận

---

## 📝 **Hướng dẫn triển khai**

### **Bước 1: Chạy SQL Script**

```sql
-- Chạy file này trong SQL Server Management Studio
-- Kết nối database: perw
USE perw;
GO

-- Chạy script
:r "C:\...\Database\08_Update_Transfer_Status_Flow.sql"
GO
```

### **Bước 2: Build lại .NET Project**

```bash
# Trong Visual Studio
Build > Rebuild Solution
```

### **Bước 3: Kiểm tra quyền hạn**

```sql
-- Kiểm tra role_warehouse có quyền EXECUTE không
SELECT 
    dp.name AS RoleName,
    o.name AS ProcedureName,
    dp2.permission_name
FROM sys.database_permissions dp2
INNER JOIN sys.database_principals dp ON dp2.grantee_principal_id = dp.principal_id
INNER JOIN sys.objects o ON dp2.major_id = o.object_id
WHERE dp.name = 'role_warehouse'
  AND o.type = 'P'
ORDER BY o.name;
```

---

## 🧪 **Test Scenarios**

### **Test Case 1: Branch Request Flow**

```
1. Login as Branch Manager
2. Create Transfer Request → Verify status = 'Requested'
3. Login as Warehouse Manager  
4. Approve Request → Verify status = 'Shipping'
5. Login as Branch Manager
6. Confirm Delivery → Verify status = 'Delivered' + inventory updated
```

### **Test Case 2: Warehouse Direct Flow**

```
1. Login as Warehouse Manager
2. Create Transfer → Verify status = 'Shipping'
3. Login as Branch Manager
4. Confirm Delivery → Verify status = 'Delivered' + inventory updated
```

### **Test Case 3: Cancel Request**

```
1. Login as Branch Manager
2. Create Transfer Request → status = 'Requested'
3. Login as Warehouse Manager
4. Cancel Request → Verify status = 'Cancelled'
5. Verify Branch Manager cannot confirm cancelled transfer
```

---

## ✅ **Checklist**

- [x] SQL Script tạo 4 stored procedures mới
- [x] Cập nhật Warehouse/TransfersController
- [x] Cập nhật Branch/TransfersController
- [x] Thêm action ConfirmDelivery cho Branch
- [x] Cập nhật status mặc định từ Pending → Shipping
- [ ] **TODO**: Cập nhật Views (.cshtml) để hiển thị buttons phù hợp
- [ ] **TODO**: Test toàn bộ workflow
- [ ] **TODO**: Cập nhật documentation

---

**Ngày cập nhật**: 2025-12-19  
**Người thực hiện**: AI Assistant
