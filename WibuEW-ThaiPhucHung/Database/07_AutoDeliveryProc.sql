-- =============================================
-- AUTO DELIVERY PROCEDURE
-- Tự động giao hàng từ kho tổng xuống chi nhánh
-- =============================================

USE [perw];
GO

IF OBJECT_ID('sp_Auto_DeliverTransfer', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_Auto_DeliverTransfer];
GO

CREATE PROCEDURE [dbo].[sp_Auto_DeliverTransfer]
    @TransferID BIGINT,
    @AutoComplete BIT = 1  -- 1 = Auto complete, 0 = Chỉ ship
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentStatus NVARCHAR(20);
    DECLARE @FromWarehouse BIGINT;
    DECLARE @ToBranch BIGINT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Lấy thông tin phiếu chuyển
        SELECT 
            @CurrentStatus = status,
            @FromWarehouse = from_warehouse_id,
            @ToBranch = to_branch_id
        FROM warehouse_transfers
        WHERE id = @TransferID;
        
        -- Validate phiếu chuyển tồn tại
        IF @CurrentStatus IS NULL
        BEGIN
            ;THROW 50001, N'Không tìm thấy phiếu chuyển kho.', 1;
        END
        
        -- Validate có chi tiết sản phẩm
        IF NOT EXISTS (SELECT 1 FROM warehouse_transfer_details WHERE transfer_id = @TransferID)
        BEGIN
            ;THROW 50003, N'Phiếu chuyển không có sản phẩm nào. Vui lòng thêm sản phẩm trước.', 1;
        END
        
        -- Validate status
        IF @CurrentStatus NOT IN ('pending', 'shipping')
        BEGIN
            DECLARE @ErrMsg NVARCHAR(200) = N'Phiếu chuyển đang ở trạng thái: ' + @CurrentStatus + N'. Không thể giao hàng.';
            ;THROW 50002, @ErrMsg, 1;
        END
        
        -- Step 1: Nếu đang pending → chuyển sang shipping (trigger sẽ trừ hàng kho tổng)
        IF @CurrentStatus = 'pending'
        BEGIN
            PRINT N'📤 Bước 1: Xuất hàng từ kho tổng...';
            
            UPDATE warehouse_transfers
            SET status = 'shipping',
                transfer_date = SYSDATETIME(),
                updated_at = SYSDATETIME()
            WHERE id = @TransferID;
            
            PRINT N'✅ Đã xuất hàng khỏi kho tổng';
        END
        
        -- Step 2: Nếu AutoComplete = 1 → chuyển sang completed (trigger sẽ cộng hàng vào chi nhánh)
        IF @AutoComplete = 1
        BEGIN
            PRINT N'📦 Bước 2: Giao hàng đến chi nhánh...';
            
            UPDATE warehouse_transfers
            SET status = 'completed',
                updated_at = SYSDATETIME()
            WHERE id = @TransferID;
            
            PRINT N'✅ Đã giao hàng thành công đến chi nhánh';
        END
        
        COMMIT TRANSACTION;
        
        -- Success message
        DECLARE @SuccessMsg NVARCHAR(500);
        IF @AutoComplete = 1
            SET @SuccessMsg = N'✅ Giao hàng thành công! Phiếu chuyển #' + CAST(@TransferID AS NVARCHAR) + N' đã hoàn thành.';
        ELSE
            SET @SuccessMsg = N'✅ Đã xuất hàng! Phiếu chuyển #' + CAST(@TransferID AS NVARCHAR) + N' đang trên đường giao.';
            
        PRINT @SuccessMsg;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- =============================================
-- Test Procedure
-- =============================================

PRINT N'';
PRINT N'========================================';
PRINT N'✅ STORED PROCEDURE ĐÃ ĐƯỢC TẠO';
PRINT N'========================================';
PRINT N'- sp_Auto_DeliverTransfer';
PRINT N'';
PRINT N'Usage:';
PRINT N'EXEC sp_Auto_DeliverTransfer @TransferID = 1, @AutoComplete = 1';
PRINT N'';
GO
