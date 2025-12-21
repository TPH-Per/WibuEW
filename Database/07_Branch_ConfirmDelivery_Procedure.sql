-- =============================================
-- STORED PROCEDURE: Branch xác nhận nhận hàng
-- Chi nhánh xác nhận đơn hàng đã được giao đến nơi
-- Chỉ cho phép khi status = 'Shipping'
-- =============================================

USE perw;
GO

-- =============================================
-- SP: Branch xác nhận nhận hàng (Confirm Delivery)
-- Ràng buộc: Chỉ xác nhận được khi status = 'Shipping'
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
        
        -- 1. Kiểm tra phiếu có tồn tại và thuộc branch này
        DECLARE @current_status NVARCHAR(50);
        DECLARE @from_warehouse_id BIGINT;
        DECLARE @to_branch_id BIGINT;
        
        SELECT 
            @current_status = status,
            @from_warehouse_id = from_warehouse_id,
            @to_branch_id = to_branch_id
        FROM dbo.warehouse_transfers 
        WHERE id = @transfer_id;
        
        IF @current_status IS NULL
        BEGIN
            RAISERROR(N'Phiếu điều chuyển không tồn tại (transfer_id: %d)', 16, 1, @transfer_id);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- 2. Kiểm tra phiếu có thuộc branch này không
        IF @to_branch_id != @branch_id
        BEGIN
            RAISERROR(N'Phiếu điều chuyển này không thuộc chi nhánh của bạn', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- 3. Kiểm tra status phải là 'Shipping'
        IF @current_status != 'Shipping'
        BEGIN
            DECLARE @error_msg NVARCHAR(500);
            SET @error_msg = N'Chỉ có thể xác nhận nhận hàng khi đơn hàng đang được giao (status = Shipping). Status hiện tại: ' + @current_status;
            RAISERROR(@error_msg, 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        -- 4. Cập nhật status sang 'Completed'
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Completed',
            completed_at = GETDATE(),
            updated_at = GETDATE(),
            notes = CASE 
                WHEN @notes IS NOT NULL THEN CONCAT(ISNULL(notes, ''), ' | ', @notes)
                ELSE notes
            END
        WHERE id = @transfer_id;
        
        -- 5. Cập nhật branch inventory (tăng số lượng tồn kho chi nhánh)
        DECLARE @product_variant_id BIGINT;
        DECLARE @quantity INT;
        
        DECLARE detail_cursor CURSOR FOR
        SELECT product_variant_id, quantity
        FROM dbo.warehouse_transfer_details
        WHERE transfer_id = @transfer_id;
        
        OPEN detail_cursor;
        FETCH NEXT FROM detail_cursor INTO @product_variant_id, @quantity;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Kiểm tra xem đã có bản ghi inventory cho branch chưa
            IF EXISTS (
                SELECT 1 FROM dbo.branch_inventories 
                WHERE branch_id = @branch_id AND product_variant_id = @product_variant_id
            )
            BEGIN
                -- Cập nhật tồn kho hiện có
                UPDATE dbo.branch_inventories
                SET 
                    quantity_on_hand = quantity_on_hand + @quantity,
                    updated_at = GETDATE()
                WHERE branch_id = @branch_id AND product_variant_id = @product_variant_id;
            END
            ELSE
            BEGIN
                -- Tạo bản ghi mới
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
                    5, -- Default reorder level cho branch
                    GETDATE(),
                    GETDATE()
                );
            END
            
            FETCH NEXT FROM detail_cursor INTO @product_variant_id, @quantity;
        END
        
        CLOSE detail_cursor;
        DEALLOCATE detail_cursor;
        
        -- 6. Giảm inventory của warehouse (nếu chưa giảm ở bước shipping)
        -- Lưu ý: Nếu đã giảm ở warehouse khi chuyển sang 'Shipping', bỏ qua bước này
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã xác nhận nhận hàng thành công. Tồn kho chi nhánh đã được cập nhật.';
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT N'✅ Đã tạo sp_Branch_ConfirmDelivery';
GO

-- =============================================
-- Gán quyền cho role_branch
-- =============================================
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_branch' AND type = 'R')
BEGIN
    GRANT EXECUTE ON dbo.sp_Branch_ConfirmDelivery TO role_branch;
    PRINT N'✅ Đã gán quyền EXECUTE cho role_branch';
END
ELSE
BEGIN
    PRINT N'⚠️ Role role_branch chưa tồn tại. Vui lòng tạo role trước.';
END
GO

-- =============================================
-- TEST PROCEDURE
-- =============================================
PRINT N'';
PRINT N'=== HƯỚNG DẪN SỬ DỤNG ===';
PRINT N'';
PRINT N'-- Xác nhận nhận hàng:';
PRINT N'EXEC sp_Branch_ConfirmDelivery';
PRINT N'    @transfer_id = 1,';
PRINT N'    @branch_id = 1,';
PRINT N'    @notes = N''Đã nhận hàng đầy đủ, không có hư hỏng''';
PRINT N'';
PRINT N'-- Kiểm tra kết quả:';
PRINT N'SELECT * FROM warehouse_transfers WHERE id = 1;';
PRINT N'SELECT * FROM branch_inventories WHERE branch_id = 1;';
PRINT N'';
GO
