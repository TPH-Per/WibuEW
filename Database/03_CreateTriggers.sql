-- =============================================
-- Triggers cho Warehouse Transfers
-- Tự động cập nhật inventory khi thay đổi status
-- =============================================

USE [perw];
GO

-- =============================================
-- 1. TRIGGER XUẤT KHO: Pending → Shipping
-- Trừ hàng khỏi kho tổng (inventories)
-- =============================================

IF OBJECT_ID('trg_Transfer_OnShip', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Transfer_OnShip];
GO

CREATE TRIGGER [dbo].[trg_Transfer_OnShip] 
ON [dbo].[warehouse_transfers] 
AFTER UPDATE 
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ chạy khi status đổi từ 'pending' sang 'shipping'
    IF NOT EXISTS (
        SELECT 1 
        FROM inserted i 
        JOIN deleted d ON i.id = d.id 
        WHERE d.status = 'pending' AND i.status = 'shipping'
    ) 
        RETURN;
    
    DECLARE @ID BIGINT, @WH BIGINT;
    SELECT @ID = id, @WH = from_warehouse_id FROM inserted;
    
    -- Kiểm tra đủ hàng trong kho tổng trước khi trừ
    IF EXISTS (
        SELECT 1 
        FROM warehouse_transfer_details d 
        JOIN inventories i ON d.product_variant_id = i.product_variant_id 
            AND i.warehouse_id = @WH 
        WHERE d.transfer_id = @ID 
            AND i.quantity_on_hand < d.quantity
    )
    BEGIN 
        RAISERROR(N'Kho tổng không đủ hàng để xuất.', 16, 1);
        ROLLBACK TRANSACTION; 
        RETURN; 
    END

    -- Trừ hàng khỏi kho tổng
    UPDATE i 
    SET quantity_on_hand = i.quantity_on_hand - d.quantity, 
        updated_at = SYSDATETIME()
    FROM inventories i 
    JOIN warehouse_transfer_details d ON i.product_variant_id = d.product_variant_id
    WHERE d.transfer_id = @ID 
        AND i.warehouse_id = @WH;

    PRINT N'✅ Đã trừ hàng khỏi kho tổng (Transfer ID: ' + CAST(@ID AS NVARCHAR) + N')';
END
GO

-- =============================================
-- 2. TRIGGER NHẬN HÀNG: Shipping → Completed
-- Cộng hàng vào kho chi nhánh (branch_inventories)
-- =============================================

IF OBJECT_ID('trg_Transfer_OnReceive', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Transfer_OnReceive];
GO

CREATE TRIGGER [dbo].[trg_Transfer_OnReceive] 
ON [dbo].[warehouse_transfers] 
AFTER UPDATE 
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ chạy khi status đổi từ 'shipping' sang 'completed'
    IF NOT EXISTS (
        SELECT 1 
        FROM inserted i 
        JOIN deleted d ON i.id = d.id 
        WHERE d.status = 'shipping' AND i.status = 'completed'
    ) 
        RETURN;

    DECLARE @ID BIGINT, @BR BIGINT;
    SELECT @ID = id, @BR = to_branch_id FROM inserted;

    -- Cập nhật số lượng nếu sản phẩm đã tồn tại trong kho chi nhánh
    UPDATE bi
    SET bi.quantity_on_hand = bi.quantity_on_hand + d.quantity,
        bi.updated_at = SYSDATETIME()
    FROM branch_inventories bi
    INNER JOIN warehouse_transfer_details d ON bi.product_variant_id = d.product_variant_id
    WHERE d.transfer_id = @ID      
        AND bi.branch_id = @BR;

    -- Insert mới nếu sản phẩm chưa có trong kho chi nhánh
    INSERT INTO branch_inventories (
        branch_id, 
        product_variant_id, 
        quantity_on_hand, 
        quantity_reserved, 
        reorder_level, 
        created_at,
        updated_at
    )
    SELECT 
        @BR, 
        d.product_variant_id, 
        d.quantity, 
        0,  
        5, 
        SYSDATETIME(),
        SYSDATETIME()
    FROM warehouse_transfer_details d
    WHERE d.transfer_id = @ID
        AND NOT EXISTS (
            SELECT 1 
            FROM branch_inventories bi 
            WHERE bi.branch_id = @BR 
                AND bi.product_variant_id = d.product_variant_id
        );

    PRINT N'✅ Đã cộng hàng vào kho chi nhánh (Transfer ID: ' + CAST(@ID AS NVARCHAR) + N')';
END
GO

-- =============================================
-- 3. TRIGGER HOÀN TRẢ: Shipping → Returned
-- Hoàn lại hàng về kho tổng (inventories)
-- =============================================

IF OBJECT_ID('trg_Transfer_OnReturn', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Transfer_OnReturn];
GO

CREATE TRIGGER [dbo].[trg_Transfer_OnReturn] 
ON [dbo].[warehouse_transfers] 
AFTER UPDATE 
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ chạy khi status đổi từ 'shipping' sang 'returned'
    IF NOT EXISTS (
        SELECT 1 
        FROM inserted i 
        JOIN deleted d ON i.id = d.id 
        WHERE d.status = 'shipping' AND i.status = 'returned'
    ) 
        RETURN;

    DECLARE @ID BIGINT, @WH BIGINT;
    SELECT @ID = id, @WH = from_warehouse_id FROM inserted;

    -- Cộng lại hàng vào kho tổng
    UPDATE i 
    SET quantity_on_hand = i.quantity_on_hand + d.quantity, 
        updated_at = SYSDATETIME()
    FROM inventories i 
    JOIN warehouse_transfer_details d ON i.product_variant_id = d.product_variant_id
    WHERE d.transfer_id = @ID 
        AND i.warehouse_id = @WH;

    PRINT N'✅ Đã hoàn trả hàng về kho tổng (Transfer ID: ' + CAST(@ID AS NVARCHAR) + N')';
END
GO

PRINT N'';
PRINT N'========================================';
PRINT N'✅ TẤT CẢ TRIGGERS ĐÃ ĐƯỢC TẠO THÀNH CÔNG';
PRINT N'========================================';
PRINT N'- trg_Transfer_OnShip (Pending → Shipping)';
PRINT N'- trg_Transfer_OnReceive (Shipping → Completed)';
PRINT N'- trg_Transfer_OnReturn (Shipping → Returned)';
PRINT N'';
GO
