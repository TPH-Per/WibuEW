-- =============================================
-- SCRIPT: CẬP NHẬT LOGIC WAREHOUSE TRANSFER
-- 1. Warehouse tạo phiếu → status mặc định = 'Shipping'
-- 2. Branch xác nhận phiếu 'Shipping' → 'Delivered'  
-- 3. Warehouse xử lý phiếu 'Requested' (từ Branch) → 'Shipping' hoặc 'Cancelled'
-- Date: 2025-12-19
-- =============================================

USE perw;
GO

-- =============================================
-- SỬA LẠI sp_Warehouse_CreateTransfer
-- Mặc định status = 'Shipping' khi Warehouse tạo
-- =============================================
IF OBJECT_ID('dbo.sp_Warehouse_CreateTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_CreateTransfer;
GO

CREATE PROCEDURE dbo.sp_Warehouse_CreateTransfer
    @from_warehouse_id BIGINT,
    @to_branch_id BIGINT,
    @status NVARCHAR(50) = 'Shipping',  -- Mặc định là Shipping
    @notes NVARCHAR(500) = NULL,
    @new_transfer_id BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra warehouse
        IF NOT EXISTS (SELECT 1 FROM dbo.warehouses WHERE id = @from_warehouse_id)
        BEGIN
            RAISERROR(N'Kho không tồn tại (warehouse_id: %d)', 16, 1, @from_warehouse_id);
            RETURN;
        END
        
        -- Kiểm tra branch
        IF NOT EXISTS (SELECT 1 FROM dbo.branches WHERE id = @to_branch_id)
        BEGIN
            RAISERROR(N'Chi nhánh không tồn tại (branch_id: %d)', 16, 1, @to_branch_id);
            RETURN;
        END
        
        -- Validate status - Warehouse chỉ có thể tạo với status 'Shipping'
        IF @status NOT IN ('Shipping', 'Requested', 'Cancelled')
        BEGIN
            RAISERROR(N'Status không hợp lệ cho Warehouse. Chỉ chấp nhận: Shipping, Requested, Cancelled', 16, 1);
            RETURN;
        END
        
        -- Tạo phiếu
        INSERT INTO dbo.warehouse_transfers (
            from_warehouse_id,
            to_branch_id,
            transfer_date,
            status,
            notes,
            created_at,
            updated_at
        )
        VALUES (
            @from_warehouse_id,
            @to_branch_id,
            GETDATE(),
            @status,
            @notes,
            GETDATE(),
            GETDATE()
        );
        
        SET @new_transfer_id = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã tạo phiếu điều chuyển #' + CAST(@new_transfer_id AS NVARCHAR(20)) + N' với status: ' + @status;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- =============================================
-- TẠO MỚI sp_Warehouse_ApproveRequestToShipping
-- Warehouse duyệt phiếu 'Requested' từ Branch → 'Shipping'
-- =============================================
IF OBJECT_ID('dbo.sp_Warehouse_ApproveRequestToShipping', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_ApproveRequestToShipping;
GO

CREATE PROCEDURE dbo.sp_Warehouse_ApproveRequestToShipping
    @transfer_id BIGINT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu ở trạng thái 'Requested'
        DECLARE @current_status NVARCHAR(50);
        SELECT @current_status = status 
        FROM dbo.warehouse_transfers 
        WHERE id = @transfer_id;
        
        IF @current_status IS NULL
        BEGIN
            RAISERROR(N'Phiếu điều chuyển không tồn tại (ID: %d)', 16, 1, @transfer_id);
            RETURN;
        END
        
        IF @current_status != 'Requested'
        BEGIN
            RAISERROR(N'Chỉ có thể duyệt phiếu ở trạng thái Requested. Trạng thái hiện tại: %s', 16, 1, @current_status);
            RETURN;
        END
        
        -- Cập nhật status
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Shipping',
            notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE notes END,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã duyệt phiếu #' + CAST(@transfer_id AS NVARCHAR(20)) + N': Requested → Shipping';
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- =============================================
-- TẠO MỚI sp_Warehouse_CancelRequest
-- Warehouse hủy phiếu 'Requested' từ Branch
-- =============================================
IF OBJECT_ID('dbo.sp_Warehouse_CancelRequest', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_CancelRequest;
GO

CREATE PROCEDURE dbo.sp_Warehouse_CancelRequest
    @transfer_id BIGINT,
    @reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu ở trạng thái 'Requested'
        DECLARE @current_status NVARCHAR(50);
        SELECT @current_status = status 
        FROM dbo.warehouse_transfers 
        WHERE id = @transfer_id;
        
        IF @current_status IS NULL
        BEGIN
            RAISERROR(N'Phiếu điều chuyển không tồn tại (ID: %d)', 16, 1, @transfer_id);
            RETURN;
        END
        
        IF @current_status != 'Requested'
        BEGIN
            RAISERROR(N'Chỉ có thể hủy phiếu ở trạng thái Requested. Trạng thái hiện tại: %s', 16, 1, @current_status);
            RETURN;
        END
        
        -- Cập nhật status
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Cancelled',
            notes = N'[HỦY BỞI WAREHOUSE] ' + @reason,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã hủy phiếu #' + CAST(@transfer_id AS NVARCHAR(20));
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- =============================================
-- TẠO MỚI sp_Branch_ConfirmDelivery
-- Branch xác nhận nhận hàng: 'Shipping' → 'Delivered'
-- =============================================
IF OBJECT_ID('dbo.sp_Branch_ConfirmDelivery', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Branch_ConfirmDelivery;
GO

CREATE PROCEDURE dbo.sp_Branch_ConfirmDelivery
    @transfer_id BIGINT,
    @branch_id BIGINT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu
        DECLARE @current_status NVARCHAR(50);
        DECLARE @to_branch_id BIGINT;
        DECLARE @from_warehouse_id BIGINT;
        
        SELECT 
            @current_status = status,
            @to_branch_id = to_branch_id,
            @from_warehouse_id = from_warehouse_id
        FROM dbo.warehouse_transfers 
        WHERE id = @transfer_id;
        
        IF @current_status IS NULL
        BEGIN
            RAISERROR(N'Phiếu điều chuyển không tồn tại (ID: %d)', 16, 1, @transfer_id);
            RETURN;
        END
        
        -- Kiểm tra branch có quyền xác nhận không
        IF @to_branch_id != @branch_id
        BEGIN
            RAISERROR(N'Chi nhánh này không có quyền xác nhận phiếu điều chuyển này', 16, 1);
            RETURN;
        END
        
        -- Chỉ xác nhận được phiếu 'Shipping'
        IF @current_status != 'Shipping'
        BEGIN
            RAISERROR(N'Chỉ có thể xác nhận phiếu ở trạng thái Shipping. Trạng thái hiện tại: %s', 16, 1, @current_status);
            RETURN;
        END
        
        -- Xử lý tồn kho
        DECLARE @variant_id BIGINT;
        DECLARE @qty INT;
        
        DECLARE transfer_cursor CURSOR FOR
            SELECT product_variant_id, quantity
            FROM dbo.warehouse_transfer_details
            WHERE transfer_id = @transfer_id;
        
        OPEN transfer_cursor;
        FETCH NEXT FROM transfer_cursor INTO @variant_id, @qty;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Kiểm tra và trừ tồn kho warehouse
            DECLARE @warehouse_qty INT;
            SELECT @warehouse_qty = ISNULL(quantity_on_hand, 0)
            FROM dbo.inventories
            WHERE warehouse_id = @from_warehouse_id AND product_variant_id = @variant_id;
            
            IF @warehouse_qty < @qty
            BEGIN
                CLOSE transfer_cursor;
                DEALLOCATE transfer_cursor;
                
                RAISERROR(N'Không đủ tồn kho trong warehouse (variant_id: %d). Cần: %d, Có: %d', 
                    16, 1, @variant_id, @qty, @warehouse_qty);
                RETURN;
            END
            
            -- Trừ warehouse
            UPDATE dbo.inventories
            SET 
                quantity_on_hand = quantity_on_hand - @qty,
                updated_at = GETDATE()
            WHERE warehouse_id = @from_warehouse_id AND product_variant_id = @variant_id;
            
            -- Cộng branch
            IF EXISTS (
                SELECT 1 FROM dbo.branch_inventories 
                WHERE branch_id = @branch_id AND product_variant_id = @variant_id
            )
            BEGIN
                UPDATE dbo.branch_inventories
                SET 
                    quantity_on_hand = quantity_on_hand + @qty,
                    updated_at = GETDATE()
                WHERE branch_id = @branch_id AND product_variant_id = @variant_id;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.branch_inventories (
                    branch_id, product_variant_id, quantity_on_hand, 
                    quantity_reserved, reorder_level, created_at, updated_at
                )
                VALUES (
                    @branch_id, @variant_id, @qty,
                    0, 10, GETDATE(), GETDATE()
                );
            END
            
            FETCH NEXT FROM transfer_cursor INTO @variant_id, @qty;
        END
        
        CLOSE transfer_cursor;
        DEALLOCATE transfer_cursor;
        
        -- Cập nhật status
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Delivered',
            notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE notes END,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Chi nhánh đã xác nhận nhận hàng phiếu #' + CAST(@transfer_id AS NVARCHAR(20));
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        IF CURSOR_STATUS('global', 'transfer_cursor') >= -1
        BEGIN
            CLOSE transfer_cursor;
            DEALLOCATE transfer_cursor;
        END
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- =============================================
-- CẤP QUYỀN
-- =============================================
GRANT EXECUTE ON dbo.sp_Warehouse_CreateTransfer TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_ApproveRequestToShipping TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_CancelRequest TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Branch_ConfirmDelivery TO role_branch;

PRINT N'';
PRINT N'========================================';
PRINT N'  WORKFLOW MỚI';
PRINT N'========================================';
PRINT N'';
PRINT N'LUỒNG 1: Branch tạo yêu cầu';
PRINT N'  1. Branch tạo → status: Requested';
PRINT N'  2. Warehouse duyệt → sp_Warehouse_ApproveRequestToShipping → Shipping';
PRINT N'     HOẶC Warehouse hủy → sp_Warehouse_CancelRequest → Cancelled';
PRINT N'  3. Branch xác nhận → sp_Branch_ConfirmDelivery → Delivered';
PRINT N'';
PRINT N'LUỒNG 2: Warehouse tự tạo';
PRINT N'  1. Warehouse tạo → sp_Warehouse_CreateTransfer → Shipping (mặc định)';
PRINT N'  2. Branch xác nhận → sp_Branch_ConfirmDelivery → Delivered';
PRINT N'';
PRINT N'✅ Hoàn tất cập nhật logic!';
GO
