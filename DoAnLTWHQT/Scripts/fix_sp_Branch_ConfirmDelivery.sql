-- =====================================================
-- sp_Branch_ConfirmDelivery
-- Chi nhánh xác nhận đã nhận hàng: Shipping → Delivered
-- Cập nhật tồn kho chi nhánh
-- =====================================================
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_Branch_ConfirmDelivery')
    DROP PROCEDURE sp_Branch_ConfirmDelivery;
GO

CREATE PROCEDURE sp_Branch_ConfirmDelivery
    @transfer_id BIGINT,
    @branch_id BIGINT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu tồn tại và thuộc branch này
        DECLARE @current_status NVARCHAR(50);
        DECLARE @to_branch_id BIGINT;
        
        SELECT @current_status = status, @to_branch_id = to_branch_id
        FROM warehouse_transfers
        WHERE id = @transfer_id;
        
        IF @current_status IS NULL
        BEGIN
            RAISERROR(N'Không tìm thấy phiếu transfer', 16, 1);
            RETURN;
        END
        
        IF @to_branch_id <> @branch_id
        BEGIN
            RAISERROR(N'Phiếu này không thuộc chi nhánh của bạn', 16, 1);
            RETURN;
        END
        
        -- Kiểm tra status phải là Shipping (case-insensitive)
        IF LOWER(@current_status) <> 'shipping'
        BEGIN
            RAISERROR(N'Chỉ có thể xác nhận phiếu đang giao (Shipping)', 16, 1);
            RETURN;
        END
        
        -- Cập nhật status thành Delivered
        UPDATE warehouse_transfers 
        SET status = 'Delivered',
            notes = CASE WHEN @notes IS NOT NULL THEN ISNULL(notes, '') + ' | Confirmed: ' + @notes ELSE notes END,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        -- Cập nhật tồn kho chi nhánh cho từng sản phẩm trong phiếu
        -- Chỉ cập nhật các product_variant còn tồn tại
        MERGE INTO branch_inventories AS target
        USING (
            SELECT wtd.product_variant_id, SUM(wtd.quantity) AS quantity
            FROM warehouse_transfer_details wtd
            INNER JOIN product_variants pv ON wtd.product_variant_id = pv.id
            WHERE wtd.transfer_id = @transfer_id
              AND pv.deleted_at IS NULL
            GROUP BY wtd.product_variant_id
        ) AS source
        ON target.branch_id = @branch_id AND target.product_variant_id = source.product_variant_id
        WHEN MATCHED THEN
            UPDATE SET quantity_on_hand = target.quantity_on_hand + source.quantity,
                       updated_at = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
            VALUES (@branch_id, source.product_variant_id, source.quantity, 0, 10, GETDATE(), GETDATE());
        
        COMMIT TRANSACTION;
        
        -- Return success
        SELECT 1 AS success;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

PRINT 'Created sp_Branch_ConfirmDelivery successfully';
GO
