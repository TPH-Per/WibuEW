# STORED PROCEDURE: Branch Xác Nhận Nhận Hàng

## Mô tả

Stored procedure `sp_Branch_ConfirmDelivery` cho phép Branch role xác nhận đơn hàng đã được giao đến nơi khi status = 'Shipping'.

## File

- `Database/07_Branch_ConfirmDelivery_Procedure.sql`

## Workflow Điều Chuyển Hàng

```
[BRANCH]                     [WAREHOUSE]                    [STATUS]
   |                              |                             |
   |---(1) CreateTransferRequest->|                        'Requested'
   |                              |                             |
   |                              |<-(2) Approve/Reject         |
   |                              |                        'Approved'/'Rejected'
   |                              |                             |
   |                              |<-(3) Start Shipping         |
   |                              |                        'Shipping'
   |                              |                             |
   |<-(4) ConfirmDelivery---------|                             |
   |                              |                        'Completed'
```

## Chi tiết các bước

### Bước 1: Branch tạo yêu cầu

```sql
EXEC sp_Branch_CreateTransferRequest
    @from_warehouse_id = 1,
    @to_branch_id = 1,
    @notes = N'Yêu cầu nhập hàng tháng',
    @new_transfer_id = @transfer_id OUTPUT;
```

- Status: **'Requested'**
- Chi nhánh chỉ có thể tạo yêu cầu, không thể tự duyệt

### Bước 2: Warehouse duyệt/từ chối

```sql
-- Duyệt
EXEC sp_Warehouse_ApproveTransfer @transfer_id = 1;

-- Hoặc từ chối
EXEC sp_Warehouse_RejectTransfer 
    @transfer_id = 1,
    @reason = N'Không đủ hàng';
```

- Status: **'Approved'** hoặc **'Rejected'**

### Bước 3: Warehouse bắt đầu giao hàng

```sql
EXEC sp_Warehouse_UpdateTransferStatus
    @transfer_id = 1,
    @new_status = 'Shipping';
```

- Status: **'Shipping'**
- Hàng đang trên đường giao đến chi nhánh

### Bước 4: Branch xác nhận nhận hàng (MỚI)

```sql
EXEC sp_Branch_ConfirmDelivery
    @transfer_id = 1,
    @branch_id = 1,
    @notes = N'Đã nhận hàng đầy đủ, không có hư hỏng';
```

- **Ràng buộc**: Chỉ được thực hiện khi status = **'Shipping'**
- Status sau khi xác nhận: **'Completed'**
- Cập nhật `completed_at` = GETDATE()

## Logic của sp_Branch_ConfirmDelivery

### 1. Validation

- ✅ Kiểm tra phiếu có tồn tại
- ✅ Kiểm tra phiếu thuộc về branch này (`to_branch_id = @branch_id`)
- ✅ Kiểm tra status phải là **'Shipping'**

### 2. Cập nhật Status

```sql
UPDATE warehouse_transfers
SET 
    status = 'Completed',
    completed_at = GETDATE(),
    updated_at = GETDATE(),
    notes = CONCAT(notes, ' | ', @notes)
WHERE id = @transfer_id;
```

### 3. Cập nhật Branch Inventory

- Duyệt qua tất cả `warehouse_transfer_details` của phiếu
- Với mỗi product_variant:
  - **Nếu đã có trong branch_inventories**: Cộng thêm `quantity_on_hand`
  - **Nếu chưa có**: Tạo bản ghi mới với quantity từ phiếu

```sql
UPDATE branch_inventories
SET 
    quantity_on_hand = quantity_on_hand + @quantity,
    updated_at = GETDATE()
WHERE branch_id = @branch_id 
  AND product_variant_id = @product_variant_id;
```

## Quyền hạn (Permissions)

### Role_Branch được cấp

- ✅ `sp_Branch_CreateTransferRequest` - Tạo yêu cầu
- ✅ `sp_Branch_AddTransferRequestDetail` - Thêm chi tiết
- ✅ `sp_Branch_GetMyTransferRequests` - Xem danh sách yêu cầu
- ✅ `sp_Branch_GetTransferRequestDetails` - Xem chi tiết
- ✅ `sp_Branch_ConfirmDelivery` - **Xác nhận nhận hàng (MỚI)**

### Role_WarehouseManager được cấp

- ✅ `sp_Warehouse_ApproveTransfer` - Duyệt yêu cầu
- ✅ `sp_Warehouse_RejectTransfer` - Từ chối
- ✅ `sp_Warehouse_UpdateTransferStatus` - Cập nhật status
- ✅ `sp_Warehouse_CompleteTransfer` - Hoàn thành

## Trạng thái (Status Flow)

```
Requested → Approved → Shipping → Completed
                ↓
            Rejected
```

## Ví dụ sử dụng

### Scenario: Chi nhánh Q1 nhận hàng từ kho trung tâm

```sql
-- 1. Branch tạo yêu cầu
DECLARE @transfer_id BIGINT;
EXEC sp_Branch_CreateTransferRequest
    @from_warehouse_id = 1,     -- Kho trung tâm
    @to_branch_id = 1,           -- Chi nhánh Q1
    @notes = N'Bổ sung hàng tháng 12',
    @new_transfer_id = @transfer_id OUTPUT;

-- 2. Branch thêm sản phẩm
EXEC sp_Branch_AddTransferRequestDetail
    @transfer_id = @transfer_id,
    @product_variant_id = 5,     -- Sản phẩm A
    @quantity = 100,
    @notes = N'Bán chạy';

-- 3. Warehouse duyệt
EXEC sp_Warehouse_ApproveTransfer @transfer_id = @transfer_id;

-- 4. Warehouse bắt đầu giao hàng
EXEC sp_Warehouse_UpdateTransferStatus
    @transfer_id = @transfer_id,
    @new_status = 'Shipping';

-- 5. Chi nhánh xác nhận đã nhận hàng
EXEC sp_Branch_ConfirmDelivery
    @transfer_id = @transfer_id,
    @branch_id = 1,
    @notes = N'Đã nhận đủ 100 sản phẩm, tình trạng tốt';

-- 6. Kiểm tra kết quả
SELECT * FROM warehouse_transfers WHERE id = @transfer_id;
SELECT * FROM branch_inventories WHERE branch_id = 1 AND product_variant_id = 5;
```

## Lỗi có thể gặp

### 1. "Phiếu điều chuyển không tồn tại"

- Kiểm tra `@transfer_id` có đúng không

### 2. "Phiếu điều chuyển này không thuộc chi nhánh của bạn"

- Kiểm tra `@branch_id` có khớp với `to_branch_id` trong phiếu

### 3. "Chỉ có thể xác nhận nhận hàng khi đơn hàng đang được giao (status = Shipping)"

- Phiếu chưa ở trạng thái 'Shipping'
- Warehouse cần cập nhật status sang 'Shipping' trước

## Cài đặt

1. Chạy script:

```bash
sqlcmd -S localhost -U sa -P your_password -d perw -i "Database\07_Branch_ConfirmDelivery_Procedure.sql"
```

2. Hoặc trong SQL Server Management Studio:

- Mở file `07_Branch_ConfirmDelivery_Procedure.sql`
- Chọn database `perw`
- Execute (F5)

## Lưu ý quan trọng

⚠️ **Branch KHÔNG được quyền**:

- Tự duyệt yêu cầu của mình
- Thay đổi status trước khi nhận hàng
- Xác nhận nhận hàng khi status != 'Shipping'

✅ **Branch CHỈ được phép**:

- Tạo yêu cầu (status = 'Requested')
- Xác nhận nhận hàng (khi status = 'Shipping')
