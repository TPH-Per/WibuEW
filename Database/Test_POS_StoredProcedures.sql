-- =====================================================
-- Script: Test POS Stored Procedures
-- Purpose: Kiểm tra logic của sp_POS_Checkout_Classic, 
--          sp_POS_Refund, sp_ProcessTransferIssue
-- Date: 2025-12-15
-- =====================================================

USE [perw];
GO

PRINT N'========================================';
PRINT N'BẮT ĐẦU KIỂM TRA POS STORED PROCEDURES';
PRINT N'========================================';
PRINT '';

-- =====================================================
-- PHẦN 0: CHUẨN BỊ DỮ LIỆU TEST
-- =====================================================
PRINT N'[0] CHUẨN BỊ DỮ LIỆU TEST...';

-- Biến lưu ID test
DECLARE @TestBranchID BIGINT;
DECLARE @TestWarehouseID BIGINT;
DECLARE @TestUserID BIGINT;
DECLARE @TestPaymentMethodID BIGINT;
DECLARE @TestVariantID1 BIGINT;
DECLARE @TestVariantID2 BIGINT;

-- Lấy warehouse đầu tiên
SELECT TOP 1 @TestWarehouseID = id FROM warehouses;
IF @TestWarehouseID IS NULL
BEGIN
    PRINT N'❌ Không có warehouse. Vui lòng tạo warehouse trước.';
    RETURN;
END

-- Lấy branch đầu tiên
SELECT TOP 1 @TestBranchID = id FROM branches;
IF @TestBranchID IS NULL
BEGIN
    PRINT N'❌ Không có branch. Vui lòng tạo branch trước.';
    RETURN;
END

-- Lấy user có role branch hoặc bất kỳ user nào
SELECT TOP 1 @TestUserID = id FROM users;
IF @TestUserID IS NULL
BEGIN
    PRINT N'❌ Không có user. Vui lòng tạo user trước.';
    RETURN;
END

-- Lấy payment method đầu tiên
SELECT TOP 1 @TestPaymentMethodID = id FROM payment_methods WHERE is_active = 1;
IF @TestPaymentMethodID IS NULL
BEGIN
    -- Tạo payment method test
    INSERT INTO payment_methods (name, code, is_active, created_at)
    VALUES (N'Tiền mặt', 'CASH', 1, SYSDATETIME());
    SET @TestPaymentMethodID = SCOPE_IDENTITY();
    PRINT N'  → Đã tạo payment method test: ID = ' + CAST(@TestPaymentMethodID AS NVARCHAR);
END

-- Lấy 2 product variant có sẵn
SELECT TOP 1 @TestVariantID1 = id FROM product_variants WHERE deleted_at IS NULL;
IF @TestVariantID1 IS NULL
BEGIN
    PRINT N'❌ Không có product variant. Vui lòng tạo sản phẩm trước.';
    RETURN;
END

SELECT TOP 1 @TestVariantID2 = id FROM product_variants WHERE deleted_at IS NULL AND id != @TestVariantID1;
IF @TestVariantID2 IS NULL SET @TestVariantID2 = @TestVariantID1;

PRINT N'  → Test BranchID: ' + CAST(@TestBranchID AS NVARCHAR);
PRINT N'  → Test UserID: ' + CAST(@TestUserID AS NVARCHAR);
PRINT N'  → Test PaymentMethodID: ' + CAST(@TestPaymentMethodID AS NVARCHAR);
PRINT N'  → Test VariantID1: ' + CAST(@TestVariantID1 AS NVARCHAR);
PRINT N'  → Test VariantID2: ' + CAST(@TestVariantID2 AS NVARCHAR);
PRINT N'';

-- =====================================================
-- PHẦN 1: CHUẨN BỊ TỒN KHO CHI NHÁNH
-- =====================================================
PRINT N'[1] CHUẨN BỊ TỒN KHO CHI NHÁNH...';

-- Xóa tồn kho cũ của variant test tại branch test (nếu có)
DELETE FROM branch_inventories 
WHERE branch_id = @TestBranchID 
    AND product_variant_id IN (@TestVariantID1, @TestVariantID2);

-- Thêm tồn kho test: 100 đơn vị cho mỗi variant
INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at)
VALUES 
    (@TestBranchID, @TestVariantID1, 100, 0, 10, SYSDATETIME()),
    (@TestBranchID, @TestVariantID2, 50, 0, 5, SYSDATETIME());

PRINT N'  → Đã thêm tồn kho: Variant1 = 100, Variant2 = 50';

-- Hiển thị tồn kho trước test
PRINT N'  → Tồn kho trước test:';
SELECT branch_id, product_variant_id, quantity_on_hand 
FROM branch_inventories 
WHERE branch_id = @TestBranchID;
PRINT N'';

-- =====================================================
-- PHẦN 2: TEST sp_POS_Checkout_Classic
-- =====================================================
PRINT N'========================================';
PRINT N'[2] TEST: sp_POS_Checkout_Classic';
PRINT N'========================================';

-- 2.1: Test checkout thành công
PRINT N'';
PRINT N'[2.1] Test checkout thành công với 2 sản phẩm...';

DECLARE @CartItems AS [dbo].[CartItemTableType];
INSERT INTO @CartItems (VariantID, Qty) VALUES (@TestVariantID1, 5);
INSERT INTO @CartItems (VariantID, Qty) VALUES (@TestVariantID2, 3);

BEGIN TRY
    EXEC [dbo].[sp_POS_Checkout_Classic]
        @BranchID = @TestBranchID,
        @UserID = @TestUserID,
        @PaymentMethodID = @TestPaymentMethodID,
        @CartItems = @CartItems;
    
    PRINT N'  ✅ Checkout thành công!';
    
    -- Kiểm tra tồn kho sau checkout
    PRINT N'  → Tồn kho sau checkout:';
    SELECT branch_id, product_variant_id, quantity_on_hand 
    FROM branch_inventories 
    WHERE branch_id = @TestBranchID;
    
    -- Kiểm tra đơn hàng được tạo
    PRINT N'  → Đơn hàng vừa tạo:';
    SELECT TOP 1 id, order_code, branch_id, total_amount, status, created_at
    FROM purchase_orders 
    WHERE branch_id = @TestBranchID
    ORDER BY id DESC;
END TRY
BEGIN CATCH
    PRINT N'  ❌ Lỗi: ' + ERROR_MESSAGE();
END CATCH

-- 2.2: Test checkout với giỏ hàng rỗng
PRINT N'';
PRINT N'[2.2] Test checkout với giỏ hàng rỗng...';

DELETE FROM @CartItems;

BEGIN TRY
    EXEC [dbo].[sp_POS_Checkout_Classic]
        @BranchID = @TestBranchID,
        @UserID = @TestUserID,
        @PaymentMethodID = @TestPaymentMethodID,
        @CartItems = @CartItems;
    
    PRINT N'  ❌ LỖI: Lẽ ra phải throw error!';
END TRY
BEGIN CATCH
    PRINT N'  ✅ Đúng! Đã catch lỗi: ' + ERROR_MESSAGE();
END CATCH

-- 2.3: Test checkout khi không đủ hàng
PRINT N'';
PRINT N'[2.3] Test checkout khi không đủ hàng (yêu cầu 999 đơn vị)...';

DELETE FROM @CartItems;
INSERT INTO @CartItems (VariantID, Qty) VALUES (@TestVariantID1, 999);

BEGIN TRY
    EXEC [dbo].[sp_POS_Checkout_Classic]
        @BranchID = @TestBranchID,
        @UserID = @TestUserID,
        @PaymentMethodID = @TestPaymentMethodID,
        @CartItems = @CartItems;
    
    PRINT N'  ❌ LỖI: Lẽ ra phải throw error vì không đủ hàng!';
END TRY
BEGIN CATCH
    PRINT N'  ✅ Đúng! Đã catch lỗi: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- PHẦN 3: TEST sp_POS_Refund
-- =====================================================
PRINT N'';
PRINT N'========================================';
PRINT N'[3] TEST: sp_POS_Refund';
PRINT N'========================================';

-- Lấy order ID vừa tạo
DECLARE @TestOrderID BIGINT;
SELECT TOP 1 @TestOrderID = id 
FROM purchase_orders 
WHERE branch_id = @TestBranchID 
ORDER BY id DESC;

IF @TestOrderID IS NULL
BEGIN
    PRINT N'  ❌ Không có đơn hàng để test refund.';
END
ELSE
BEGIN
    PRINT N'  → Test Order ID: ' + CAST(@TestOrderID AS NVARCHAR);
    
    -- Tồn kho trước refund
    PRINT N'  → Tồn kho trước refund:';
    SELECT branch_id, product_variant_id, quantity_on_hand 
    FROM branch_inventories 
    WHERE branch_id = @TestBranchID AND product_variant_id = @TestVariantID1;
    
    -- 3.1: Test refund thành công
    PRINT N'';
    PRINT N'[3.1] Test refund 2 đơn vị Variant1...';
    
    BEGIN TRY
        EXEC [dbo].[sp_POS_Refund]
            @BranchID = @TestBranchID,
            @OriginalOrderID = @TestOrderID,
            @VariantID = @TestVariantID1,
            @Qty = 2,
            @Reason = N'Khách đổi ý';
        
        PRINT N'  ✅ Refund thành công!';
        
        -- Tồn kho sau refund
        PRINT N'  → Tồn kho sau refund (phải tăng 2):';
        SELECT branch_id, product_variant_id, quantity_on_hand 
        FROM branch_inventories 
        WHERE branch_id = @TestBranchID AND product_variant_id = @TestVariantID1;
        
        -- Kiểm tra payment refund
        PRINT N'  → Payment refund (amount âm):';
        SELECT TOP 1 id, order_id, amount, status, created_at
        FROM payments 
        WHERE order_id = @TestOrderID AND status = 'refunded'
        ORDER BY id DESC;
    END TRY
    BEGIN CATCH
        PRINT N'  ❌ Lỗi: ' + ERROR_MESSAGE();
    END CATCH
    
    -- 3.2: Test refund với order không thuộc branch
    PRINT N'';
    PRINT N'[3.2] Test refund với order không thuộc branch...';
    
    BEGIN TRY
        EXEC [dbo].[sp_POS_Refund]
            @BranchID = 99999, -- Branch không tồn tại
            @OriginalOrderID = @TestOrderID,
            @VariantID = @TestVariantID1,
            @Qty = 1,
            @Reason = N'Test';
        
        -- Lưu ý: SP hiện tại chỉ PRINT, không THROW error
        PRINT N'  ⚠️ Cảnh báo: SP chỉ print message, không throw error!';
    END TRY
    BEGIN CATCH
        PRINT N'  ✅ Đã catch lỗi: ' + ERROR_MESSAGE();
    END CATCH
END

-- =====================================================
-- PHẦN 4: TEST sp_ProcessTransferIssue
-- =====================================================
PRINT N'';
PRINT N'========================================';
PRINT N'[4] TEST: sp_ProcessTransferIssue';
PRINT N'========================================';

-- Tồn kho trước xử lý hàng lỗi
PRINT N'  → Tồn kho trước xử lý hàng lỗi:';
SELECT branch_id, product_variant_id, quantity_on_hand 
FROM branch_inventories 
WHERE branch_id = @TestBranchID AND product_variant_id = @TestVariantID2;

-- 4.1: Test xử lý hàng lỗi thành công
PRINT N'';
PRINT N'[4.1] Test xử lý 5 đơn vị hàng lỗi Variant2...';

BEGIN TRY
    EXEC [dbo].[sp_ProcessTransferIssue]
        @BranchID = @TestBranchID,
        @TransferID = 1, -- Giả định transfer ID
        @VariantID = @TestVariantID2,
        @BadQty = 5,
        @Note = N'Hàng hỏng trong quá trình vận chuyển';
    
    PRINT N'  ✅ Xử lý hàng lỗi thành công!';
    
    -- Tồn kho sau xử lý
    PRINT N'  → Tồn kho sau xử lý (phải giảm 5):';
    SELECT branch_id, product_variant_id, quantity_on_hand 
    FROM branch_inventories 
    WHERE branch_id = @TestBranchID AND product_variant_id = @TestVariantID2;
END TRY
BEGIN CATCH
    PRINT N'  ❌ Lỗi: ' + ERROR_MESSAGE();
END CATCH

-- 4.2: Test xử lý hàng lỗi vượt quá tồn kho
PRINT N'';
PRINT N'[4.2] Test xử lý hàng lỗi vượt quá tồn kho (999 đơn vị)...';

BEGIN TRY
    EXEC [dbo].[sp_ProcessTransferIssue]
        @BranchID = @TestBranchID,
        @TransferID = 1,
        @VariantID = @TestVariantID2,
        @BadQty = 999,
        @Note = N'Test quá số lượng';
    
    PRINT N'  ❌ LỖI: Lẽ ra phải throw error!';
END TRY
BEGIN CATCH
    PRINT N'  ✅ Đúng! Đã catch lỗi: ' + ERROR_MESSAGE();
END CATCH

-- =====================================================
-- PHẦN 5: TỔNG KẾT
-- =====================================================
PRINT N'';
PRINT N'========================================';
PRINT N'[5] TỔNG KẾT';
PRINT N'========================================';

PRINT N'  → Tồn kho cuối cùng:';
SELECT branch_id, product_variant_id, quantity_on_hand, updated_at
FROM branch_inventories 
WHERE branch_id = @TestBranchID;

PRINT N'  → Đơn hàng test:';
SELECT id, order_code, branch_id, total_amount, status
FROM purchase_orders 
WHERE branch_id = @TestBranchID
ORDER BY id DESC;

PRINT N'  → Payments test:';
SELECT p.id, p.order_id, p.amount, p.status, pm.name as payment_method
FROM payments p
JOIN payment_methods pm ON p.payment_method_id = pm.id
WHERE p.order_id IN (SELECT id FROM purchase_orders WHERE branch_id = @TestBranchID)
ORDER BY p.id DESC;

PRINT N'';
PRINT N'========================================';
PRINT N'HOÀN TẤT KIỂM TRA POS STORED PROCEDURES';
PRINT N'========================================';

-- =====================================================
-- PHẦN 6: DỌN DẸP (TÙY CHỌN)
-- =====================================================
-- Uncomment các dòng dưới nếu muốn xóa dữ liệu test

/*
PRINT N'';
PRINT N'[6] DỌN DẸP DỮ LIỆU TEST...';

-- Xóa payments
DELETE FROM payments WHERE order_id IN (
    SELECT id FROM purchase_orders WHERE branch_id = @TestBranchID AND order_code LIKE 'POS-%'
);

-- Xóa order details
DELETE FROM purchase_order_details WHERE order_id IN (
    SELECT id FROM purchase_orders WHERE branch_id = @TestBranchID AND order_code LIKE 'POS-%'
);

-- Xóa orders
DELETE FROM purchase_orders WHERE branch_id = @TestBranchID AND order_code LIKE 'POS-%';

-- Xóa branch inventories test
DELETE FROM branch_inventories 
WHERE branch_id = @TestBranchID 
    AND product_variant_id IN (@TestVariantID1, @TestVariantID2);

PRINT N'  ✅ Đã dọn dẹp dữ liệu test.';
*/

GO
