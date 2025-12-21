-- =============================================
-- TRIGGERS: Inventory Auto-Update
-- 1. warehouse_transfer → Shipping: Trừ warehouse inventory
-- 2. purchase_order created: Trừ branch inventory
-- 3. product/product_variant deleted: Xóa khỏi cart
-- Date: 2025-12-19
-- =============================================

USE perw;
GO
-- ================================================
-- Check current schema của bảng carts
-- ================================================

-- 1. Kiểm tra columns hiện tại
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.carts')
ORDER BY c.column_id;

-- 2. Kiểm tra constraints
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.carts');

-- 3. Kiểm tra indexes
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('dbo.carts')
ORDER BY i.name, ic.key_ordinal;

-- 4. Kiểm tra triggers
SELECT 
    name AS TriggerName,
    OBJECT_NAME(parent_id) AS TableName,
    type_desc AS TriggerType
FROM sys.triggers
WHERE parent_id = OBJECT_ID('dbo.carts');

-- =============================================
-- TRIGGER 1: Warehouse Transfer → Shipping
-- Khi status chuyển thành 'Shipping', trừ tồn kho warehouse
-- =============================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_WarehouseTransfer_Shipping')
BEGIN
    DROP TRIGGER TR_WarehouseTransfer_Shipping;
    PRINT '✓ Dropped existing TR_WarehouseTransfer_Shipping';
END
GO

CREATE TRIGGER TR_WarehouseTransfer_Shipping
ON dbo.warehouse_transfers
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Chỉ xử lý khi status chuyển thành 'Shipping'
        IF EXISTS (
            SELECT 1 
            FROM inserted i
            INNER JOIN deleted d ON i.id = d.id
            WHERE i.status = 'Shipping' 
              AND (d.status IS NULL OR d.status != 'Shipping')
        )
        BEGIN
            DECLARE @transfer_id BIGINT;
            DECLARE @from_warehouse_id BIGINT;
            DECLARE @product_variant_id BIGINT;
            DECLARE @quantity INT;
            
            -- Cursor cho mỗi transfer được update sang Shipping
            DECLARE transfer_cursor CURSOR FOR
            SELECT i.id, i.from_warehouse_id
            FROM inserted i
            INNER JOIN deleted d ON i.id = d.id
            WHERE i.status = 'Shipping' 
              AND (d.status IS NULL OR d.status != 'Shipping');
            
            OPEN transfer_cursor;
            FETCH NEXT FROM transfer_cursor INTO @transfer_id, @from_warehouse_id;
            
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Trừ inventory cho từng product trong transfer
                DECLARE detail_cursor CURSOR FOR
                SELECT product_variant_id, quantity
                FROM dbo.warehouse_transfer_details
                WHERE transfer_id = @transfer_id;
                
                OPEN detail_cursor;
                FETCH NEXT FROM detail_cursor INTO @product_variant_id, @quantity;
                
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    -- Kiểm tra đủ tồn kho không
                    DECLARE @available_qty INT;
                    SELECT @available_qty = ISNULL(quantity_on_hand, 0) - ISNULL(quantity_reserved, 0)
                    FROM dbo.inventories
                    WHERE warehouse_id = @from_warehouse_id 
                      AND product_variant_id = @product_variant_id;
                    
                    IF @available_qty IS NULL OR @available_qty < @quantity
                    BEGIN
                        DECLARE @err_msg NVARCHAR(500);
                        SET @err_msg = CONCAT(
                            N'Không đủ tồn kho cho sản phẩm variant ID: ', @product_variant_id,
                            N'. Yêu cầu: ', @quantity, N', Có sẵn: ', ISNULL(@available_qty, 0)
                        );
                        RAISERROR(@err_msg, 16, 1);
                        ROLLBACK TRANSACTION;
                        RETURN;
                    END
                    
                    -- Trừ tồn kho
                    UPDATE dbo.inventories
                    SET 
                        quantity_on_hand = quantity_on_hand - @quantity,
                        updated_at = GETDATE()
                    WHERE warehouse_id = @from_warehouse_id 
                      AND product_variant_id = @product_variant_id;
                    
                    -- Ghi log transaction (sử dụng đúng cột của inventory_transactions)
                    INSERT INTO dbo.inventory_transactions (
                        product_variant_id,
                        warehouse_id,
                        order_id,
                        quantity,
                        type,
                        notes,
                        created_at
                    )
                    VALUES (
                        @product_variant_id,
                        @from_warehouse_id,
                        NULL,
                        @quantity,
                        'out',
                        CONCAT(N'Xuất kho - Chuyển hàng #', @transfer_id),
                        GETDATE()
                    );
                    
                    FETCH NEXT FROM detail_cursor INTO @product_variant_id, @quantity;
                END
                
                CLOSE detail_cursor;
                DEALLOCATE detail_cursor;
                
                FETCH NEXT FROM transfer_cursor INTO @transfer_id, @from_warehouse_id;
            END
            
            CLOSE transfer_cursor;
            DEALLOCATE transfer_cursor;
        END
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'detail_cursor') >= 0
        BEGIN
            CLOSE detail_cursor;
            DEALLOCATE detail_cursor;
        END
        IF CURSOR_STATUS('local', 'transfer_cursor') >= 0
        BEGIN
            CLOSE transfer_cursor;
            DEALLOCATE transfer_cursor;
        END
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

PRINT '✓ Created TR_WarehouseTransfer_Shipping';
GO

-- =============================================
-- TRIGGER 2: Purchase Order Created
-- Khi đơn hàng được tạo, trừ tồn kho branch
-- =============================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_PurchaseOrder_Created')
BEGIN
    DROP TRIGGER TR_PurchaseOrder_Created;
    PRINT '✓ Dropped existing TR_PurchaseOrder_Created';
END
GO

CREATE TRIGGER TR_PurchaseOrder_Created
ON dbo.purchase_orders
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @order_id BIGINT;
        DECLARE @branch_id BIGINT;
        
        -- Cursor cho mỗi order mới
        DECLARE order_cursor CURSOR FOR
        SELECT i.id, i.branch_id
        FROM inserted i
        WHERE i.branch_id IS NOT NULL;
        
        OPEN order_cursor;
        FETCH NEXT FROM order_cursor INTO @order_id, @branch_id;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Trừ inventory cho từng product trong order
            DECLARE @product_variant_id BIGINT;
            DECLARE @quantity INT;
            
            DECLARE order_detail_cursor CURSOR FOR
            SELECT product_variant_id, quantity
            FROM dbo.order_details
            WHERE order_id = @order_id;
            
            OPEN order_detail_cursor;
            FETCH NEXT FROM order_detail_cursor INTO @product_variant_id, @quantity;
            
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Kiểm tra đủ tồn kho branch không
                DECLARE @branch_available_qty INT;
                SELECT @branch_available_qty = ISNULL(quantity_on_hand, 0) - ISNULL(quantity_reserved, 0)
                FROM dbo.branch_inventories
                WHERE branch_id = @branch_id 
                  AND product_variant_id = @product_variant_id;
                
                IF @branch_available_qty IS NULL OR @branch_available_qty < @quantity
                BEGIN
                    DECLARE @err_msg NVARCHAR(500);
                    SET @err_msg = CONCAT(
                        N'Chi nhánh không đủ tồn kho cho sản phẩm variant ID: ', @product_variant_id,
                        N'. Yêu cầu: ', @quantity, N', Có sẵn: ', ISNULL(@branch_available_qty, 0)
                    );
                    RAISERROR(@err_msg, 16, 1);
                    ROLLBACK TRANSACTION;
                    RETURN;
                END
                
                -- Trừ tồn kho branch
                UPDATE dbo.branch_inventories
                SET 
                    quantity_on_hand = quantity_on_hand - @quantity,
                    updated_at = GETDATE()
                WHERE branch_id = @branch_id 
                  AND product_variant_id = @product_variant_id;
                
                FETCH NEXT FROM order_detail_cursor INTO @product_variant_id, @quantity;
            END
            
            CLOSE order_detail_cursor;
            DEALLOCATE order_detail_cursor;
            
            FETCH NEXT FROM order_cursor INTO @order_id, @branch_id;
        END
        
        CLOSE order_cursor;
        DEALLOCATE order_cursor;
        
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'order_detail_cursor') >= 0
        BEGIN
            CLOSE order_detail_cursor;
            DEALLOCATE order_detail_cursor;
        END
        IF CURSOR_STATUS('local', 'order_cursor') >= 0
        BEGIN
            CLOSE order_cursor;
            DEALLOCATE order_cursor;
        END
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

PRINT '✓ Created TR_PurchaseOrder_Created';
GO

-- =============================================
-- TRIGGER 2B: Purchase Order Cancelled/Refunded
-- Khi đơn hàng bị hủy hoặc trả hàng, cộng lại tồn kho branch
-- Status: 'cancelled', 'refunded', 'returned'
-- =============================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_PurchaseOrder_CancelRefund')
BEGIN
    DROP TRIGGER TR_PurchaseOrder_CancelRefund;
    PRINT '✓ Dropped existing TR_PurchaseOrder_CancelRefund';
END
GO

CREATE TRIGGER TR_PurchaseOrder_CancelRefund
ON dbo.purchase_orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Chỉ xử lý khi status chuyển thành cancelled/refunded/returned
        -- từ một status khác (không phải đã là cancelled/refunded/returned)
        IF EXISTS (
            SELECT 1 
            FROM inserted i
            INNER JOIN deleted d ON i.id = d.id
            WHERE i.status IN ('cancelled', 'refunded', 'returned', 'Cancelled', 'Refunded', 'Returned')
              AND (d.status IS NULL OR d.status NOT IN ('cancelled', 'refunded', 'returned', 'Cancelled', 'Refunded', 'Returned'))
              AND i.branch_id IS NOT NULL
        )
        BEGIN
            DECLARE @order_id BIGINT;
            DECLARE @branch_id BIGINT;
            DECLARE @old_status NVARCHAR(50);
            DECLARE @new_status NVARCHAR(50);
            
            -- Cursor cho mỗi order bị cancel/refund
            DECLARE cancel_cursor CURSOR FOR
            SELECT i.id, i.branch_id, d.status, i.status
            FROM inserted i
            INNER JOIN deleted d ON i.id = d.id
            WHERE i.status IN ('cancelled', 'refunded', 'returned', 'Cancelled', 'Refunded', 'Returned')
              AND (d.status IS NULL OR d.status NOT IN ('cancelled', 'refunded', 'returned', 'Cancelled', 'Refunded', 'Returned'))
              AND i.branch_id IS NOT NULL;
            
            OPEN cancel_cursor;
            FETCH NEXT FROM cancel_cursor INTO @order_id, @branch_id, @old_status, @new_status;
            
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Cộng lại inventory cho từng product trong order
                DECLARE @product_variant_id BIGINT;
                DECLARE @quantity INT;
                
                DECLARE refund_detail_cursor CURSOR FOR
                SELECT product_variant_id, quantity
                FROM dbo.order_details
                WHERE order_id = @order_id;
                
                OPEN refund_detail_cursor;
                FETCH NEXT FROM refund_detail_cursor INTO @product_variant_id, @quantity;
                
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    -- Kiểm tra xem đã có record trong branch_inventories chưa
                    IF EXISTS (
                        SELECT 1 FROM dbo.branch_inventories
                        WHERE branch_id = @branch_id 
                          AND product_variant_id = @product_variant_id
                    )
                    BEGIN
                        -- Cộng lại tồn kho branch
                        UPDATE dbo.branch_inventories
                        SET 
                            quantity_on_hand = quantity_on_hand + @quantity,
                            updated_at = GETDATE()
                        WHERE branch_id = @branch_id 
                          AND product_variant_id = @product_variant_id;
                    END
                    ELSE
                    BEGIN
                        -- Tạo mới record nếu chưa có
                        INSERT INTO dbo.branch_inventories (
                            branch_id,
                            product_variant_id,
                            quantity_on_hand,
                            quantity_reserved,
                            reorder_level,
                            created_at,
                            updated_at
                        )
                        VALUES (
                            @branch_id,
                            @product_variant_id,
                            @quantity,
                            0,
                            5,
                            GETDATE(),
                            GETDATE()
                        );
                    END
                    
                    FETCH NEXT FROM refund_detail_cursor INTO @product_variant_id, @quantity;
                END
                
                CLOSE refund_detail_cursor;
                DEALLOCATE refund_detail_cursor;
                
                PRINT CONCAT('✓ Hoàn trả tồn kho cho đơn hàng #', @order_id, ' (', @old_status, ' → ', @new_status, ')');
                
                FETCH NEXT FROM cancel_cursor INTO @order_id, @branch_id, @old_status, @new_status;
            END
            
            CLOSE cancel_cursor;
            DEALLOCATE cancel_cursor;
        END
        
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'refund_detail_cursor') >= 0
        BEGIN
            CLOSE refund_detail_cursor;
            DEALLOCATE refund_detail_cursor;
        END
        IF CURSOR_STATUS('local', 'cancel_cursor') >= 0
        BEGIN
            CLOSE cancel_cursor;
            DEALLOCATE cancel_cursor;
        END
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

PRINT '✓ Created TR_PurchaseOrder_CancelRefund';
GO

-- =============================================
-- TRIGGER 3: Product Variant Deleted → Remove from Cart
-- Khi product_variant bị soft-delete, xóa khỏi cart
-- =============================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_ProductVariant_Deleted_RemoveFromCart')
BEGIN
    DROP TRIGGER TR_ProductVariant_Deleted_RemoveFromCart;
    PRINT '✓ Dropped existing TR_ProductVariant_Deleted_RemoveFromCart';
END
GO

CREATE TRIGGER TR_ProductVariant_Deleted_RemoveFromCart
ON dbo.product_variants
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Khi deleted_at được set (từ NULL → có giá trị)
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.deleted_at IS NOT NULL 
          AND d.deleted_at IS NULL
    )
    BEGIN
        -- Soft-delete cart items chứa product variant đã bị delete
        UPDATE c
        SET 
            c.deleted_at = GETDATE(),
            c.updated_at = GETDATE()
        FROM dbo.carts c
        INNER JOIN inserted i ON c.product_variant_id = i.id
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.deleted_at IS NOT NULL 
          AND d.deleted_at IS NULL
          AND c.deleted_at IS NULL;
          
        DECLARE @affected_rows INT = @@ROWCOUNT;
        IF @affected_rows > 0
        BEGIN
            PRINT CONCAT('✓ Removed ', @affected_rows, ' cart items for deleted product variants');
        END
    END
END;
GO

PRINT '✓ Created TR_ProductVariant_Deleted_RemoveFromCart';
GO

-- =============================================
-- TRIGGER 4: Product Deleted → Remove Variants from Cart
-- Khi product bị soft-delete, xóa tất cả variants khỏi cart
-- =============================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Product_Deleted_RemoveFromCart')
BEGIN
    DROP TRIGGER TR_Product_Deleted_RemoveFromCart;
    PRINT '✓ Dropped existing TR_Product_Deleted_RemoveFromCart';
END
GO

CREATE TRIGGER TR_Product_Deleted_RemoveFromCart
ON dbo.products
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Khi deleted_at được set (từ NULL → có giá trị)
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.deleted_at IS NOT NULL 
          AND d.deleted_at IS NULL
    )
    BEGIN
        -- Soft-delete cart items chứa product variants của product đã bị delete
        UPDATE c
        SET 
            c.deleted_at = GETDATE(),
            c.updated_at = GETDATE()
        FROM dbo.carts c
        INNER JOIN dbo.product_variants pv ON c.product_variant_id = pv.id
        INNER JOIN inserted i ON pv.product_id = i.id
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.deleted_at IS NOT NULL 
          AND d.deleted_at IS NULL
          AND c.deleted_at IS NULL;
          
        DECLARE @affected_rows INT = @@ROWCOUNT;
        IF @affected_rows > 0
        BEGIN
            PRINT CONCAT('✓ Removed ', @affected_rows, ' cart items for deleted products');
        END
        
        -- Cũng soft-delete các product_variants
        UPDATE pv
        SET 
            pv.deleted_at = GETDATE(),
            pv.updated_at = GETDATE()
        FROM dbo.product_variants pv
        INNER JOIN inserted i ON pv.product_id = i.id
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.deleted_at IS NOT NULL 
          AND d.deleted_at IS NULL
          AND pv.deleted_at IS NULL;
    END
END;
GO

PRINT '✓ Created TR_Product_Deleted_RemoveFromCart';
GO

-- =============================================
-- SUMMARY
-- =============================================

PRINT '';
PRINT '========================================';
PRINT '✅ TẤT CẢ TRIGGERS ĐÃ ĐƯỢC TẠO';
PRINT '========================================';
PRINT '';
PRINT '1. TR_WarehouseTransfer_Shipping';
PRINT '   → Khi status = ''Shipping'', trừ warehouse inventory';
PRINT '';
PRINT '2. TR_PurchaseOrder_Created';
PRINT '   → Khi đơn hàng được tạo, trừ branch inventory';
PRINT '';
PRINT '3. TR_PurchaseOrder_CancelRefund';
PRINT '   → Khi đơn hàng cancelled/refunded/returned, cộng lại branch inventory';
PRINT '';
PRINT '4. TR_ProductVariant_Deleted_RemoveFromCart';
PRINT '   → Khi product_variant bị xóa, remove khỏi cart';
PRINT '';
PRINT '5. TR_Product_Deleted_RemoveFromCart';
PRINT '   → Khi product bị xóa, remove variants khỏi cart';
PRINT '';
PRINT '========================================';
GO

-- =============================================
-- VERIFY TRIGGERS
-- =============================================

SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    t.is_disabled AS IsDisabled,
    t.create_date AS CreatedAt
FROM sys.triggers t
WHERE t.name IN (
    'TR_WarehouseTransfer_Shipping',
    'TR_PurchaseOrder_Created',
    'TR_PurchaseOrder_CancelRefund',
    'TR_ProductVariant_Deleted_RemoveFromCart',
    'TR_Product_Deleted_RemoveFromCart'
)
ORDER BY t.name;
GO
