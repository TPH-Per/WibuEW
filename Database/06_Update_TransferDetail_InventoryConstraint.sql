-- =============================================
-- SCRIPT: CẬP NHẬT STORED PROCEDURES - THÊM RÀNG BUỘC TỒN KHO
-- Kiểm tra số lượng yêu cầu không vượt quá số lượng tồn kho warehouse
-- Date: 2025-12-15
-- =============================================

USE perw;
GO

-- =============================================
-- 1. CẬP NHẬT SP WAREHOUSE: sp_Warehouse_AddTransferDetail
-- Thêm kiểm tra tồn kho warehouse trước khi thêm chi tiết
-- =============================================

IF OBJECT_ID('dbo.sp_Warehouse_AddTransferDetail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_AddTransferDetail;
GO

CREATE PROCEDURE dbo.sp_Warehouse_AddTransferDetail
    @transfer_id BIGINT,
    @product_variant_id BIGINT,
    @quantity INT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Kiểm tra phiếu có tồn tại
        IF NOT EXISTS (SELECT 1 FROM dbo.warehouse_transfers WHERE id = @transfer_id)
        BEGIN
            RAISERROR(N'Phiếu điều chuyển không tồn tại (transfer_id: %d)', 16, 1, @transfer_id);
            RETURN;
        END
        
        -- Kiểm tra product_variant có tồn tại
        IF NOT EXISTS (SELECT 1 FROM dbo.product_variants WHERE id = @product_variant_id)
        BEGIN
            RAISERROR(N'Biến thể sản phẩm không tồn tại (product_variant_id: %d)', 16, 1, @product_variant_id);
            RETURN;
        END
        
        -- Kiểm tra số lượng hợp lệ
        IF @quantity <= 0
        BEGIN
            RAISERROR(N'Số lượng phải lớn hơn 0', 16, 1);
            RETURN;
        END
        
        -- *** MỚI: LẤY WAREHOUSE_ID TỪ PHIẾU TRANSFER ***
        DECLARE @warehouse_id BIGINT;
        SELECT @warehouse_id = from_warehouse_id FROM dbo.warehouse_transfers WHERE id = @transfer_id;
        
        -- *** MỚI: KIỂM TRA TỒN KHO WAREHOUSE ***
        DECLARE @available_qty INT;
        SELECT @available_qty = ISNULL(quantity_on_hand, 0) - ISNULL(quantity_reserved, 0)
        FROM dbo.inventories 
        WHERE warehouse_id = @warehouse_id AND product_variant_id = @product_variant_id;
        
        -- Nếu không có bản ghi tồn kho
        IF @available_qty IS NULL
        BEGIN
            SET @available_qty = 0;
        END
        
        -- Tính tổng số lượng đã thêm vào phiếu này cho cùng sản phẩm
        DECLARE @already_in_transfer INT;
        SELECT @already_in_transfer = ISNULL(SUM(quantity), 0)
        FROM dbo.warehouse_transfer_details 
        WHERE transfer_id = @transfer_id AND product_variant_id = @product_variant_id;
        
        -- Kiểm tra số lượng yêu cầu + đã thêm không vượt quá tồn kho
        IF (@already_in_transfer + @quantity) > @available_qty
        BEGIN
            DECLARE @error_msg NVARCHAR(500);
            SET @error_msg = N'Số lượng yêu cầu (' + CAST(@quantity AS NVARCHAR(20)) + 
                N') vượt quá tồn kho khả dụng (' + CAST(@available_qty - @already_in_transfer AS NVARCHAR(20)) + N')';
            RAISERROR(@error_msg, 16, 1);
            RETURN;
        END
        
        -- Lấy giá của sản phẩm
        DECLARE @price DECIMAL(18, 2);
        SELECT @price = price FROM dbo.product_variants WHERE id = @product_variant_id;
        
        -- Thêm chi tiết
        INSERT INTO dbo.warehouse_transfer_details (
            transfer_id,
            product_variant_id,
            quantity,
            price,
            notes,
            created_at,
            updated_at
        )
        VALUES (
            @transfer_id,
            @product_variant_id,
            @quantity,
            @price,
            @notes,
            GETDATE(),
            GETDATE()
        );
        
        PRINT N'✅ Đã thêm sản phẩm vào phiếu điều chuyển thành công';
        
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT N'✅ Đã cập nhật sp_Warehouse_AddTransferDetail với ràng buộc tồn kho';
GO

-- =============================================
-- 2. CẬP NHẬT SP BRANCH: sp_Branch_AddTransferRequestDetail
-- Thêm kiểm tra tồn kho warehouse trước khi thêm chi tiết
-- =============================================

IF OBJECT_ID('dbo.sp_Branch_AddTransferRequestDetail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Branch_AddTransferRequestDetail;
GO

CREATE PROCEDURE dbo.sp_Branch_AddTransferRequestDetail
    @transfer_id BIGINT,
    @product_variant_id BIGINT,
    @quantity INT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Kiểm tra phiếu yêu cầu có tồn tại và status = 'Requested'
        IF NOT EXISTS (
            SELECT 1 FROM dbo.warehouse_transfers 
            WHERE id = @transfer_id AND status = 'Requested'
        )
        BEGIN
            RAISERROR(N'Phiếu yêu cầu không tồn tại hoặc đã được xử lý (transfer_id: %d)', 16, 1, @transfer_id);
            RETURN;
        END
        
        -- Kiểm tra product_variant có tồn tại
        IF NOT EXISTS (SELECT 1 FROM dbo.product_variants WHERE id = @product_variant_id)
        BEGIN
            RAISERROR(N'Biến thể sản phẩm không tồn tại (product_variant_id: %d)', 16, 1, @product_variant_id);
            RETURN;
        END
        
        -- Kiểm tra số lượng hợp lệ
        IF @quantity <= 0
        BEGIN
            RAISERROR(N'Số lượng phải lớn hơn 0', 16, 1);
            RETURN;
        END
        
        -- *** MỚI: LẤY WAREHOUSE_ID TỪ PHIẾU TRANSFER ***
        DECLARE @warehouse_id BIGINT;
        SELECT @warehouse_id = from_warehouse_id FROM dbo.warehouse_transfers WHERE id = @transfer_id;
        
        -- *** MỚI: KIỂM TRA TỒN KHO WAREHOUSE ***
        DECLARE @available_qty INT;
        SELECT @available_qty = ISNULL(quantity_on_hand, 0) - ISNULL(quantity_reserved, 0)
        FROM dbo.inventories 
        WHERE warehouse_id = @warehouse_id AND product_variant_id = @product_variant_id;
        
        -- Nếu không có bản ghi tồn kho
        IF @available_qty IS NULL
        BEGIN
            SET @available_qty = 0;
        END
        
        -- Tính tổng số lượng đã thêm vào phiếu này cho cùng sản phẩm
        DECLARE @already_in_transfer INT;
        SELECT @already_in_transfer = ISNULL(SUM(quantity), 0)
        FROM dbo.warehouse_transfer_details 
        WHERE transfer_id = @transfer_id AND product_variant_id = @product_variant_id;
        
        -- Kiểm tra số lượng yêu cầu + đã thêm không vượt quá tồn kho
        IF (@already_in_transfer + @quantity) > @available_qty
        BEGIN
            DECLARE @error_msg NVARCHAR(500);
            SET @error_msg = N'Số lượng yêu cầu (' + CAST(@quantity AS NVARCHAR(20)) + 
                N') vượt quá tồn kho khả dụng (' + CAST(@available_qty - @already_in_transfer AS NVARCHAR(20)) + 
                N'). Vui lòng liên hệ Kho để biết thêm chi tiết.';
            RAISERROR(@error_msg, 16, 1);
            RETURN;
        END
        
        -- Lấy giá của sản phẩm
        DECLARE @price DECIMAL(18, 2);
        SELECT @price = price FROM dbo.product_variants WHERE id = @product_variant_id;
        
        -- Thêm chi tiết
        INSERT INTO dbo.warehouse_transfer_details (
            transfer_id,
            product_variant_id,
            quantity,
            price,
            notes,
            created_at,
            updated_at
        )
        VALUES (
            @transfer_id,
            @product_variant_id,
            @quantity,
            @price,
            @notes,
            GETDATE(),
            GETDATE()
        );
        
        PRINT N'✅ Đã thêm sản phẩm vào phiếu yêu cầu thành công';
        
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT N'✅ Đã cập nhật sp_Branch_AddTransferRequestDetail với ràng buộc tồn kho';
GO

-- =============================================
-- 3. TEST RÀNG BUỘC
-- =============================================

PRINT N'';
PRINT N'=== TEST RÀNG BUỘC TỒN KHO ===';
PRINT N'';

-- Hiển thị tồn kho hiện tại
PRINT N'Tồn kho warehouse hiện tại:';
SELECT TOP 5
    i.warehouse_id,
    w.name AS warehouse_name,
    i.product_variant_id,
    pv.name AS variant_name,
    i.quantity_on_hand,
    i.quantity_reserved,
    (i.quantity_on_hand - i.quantity_reserved) AS available
FROM dbo.inventories i
INNER JOIN dbo.warehouses w ON i.warehouse_id = w.id
INNER JOIN dbo.product_variants pv ON i.product_variant_id = pv.id
ORDER BY i.warehouse_id, i.product_variant_id;

PRINT N'';
PRINT N'=== HƯỚNG DẪN TEST ===';
PRINT N'1. Thử thêm sản phẩm với số lượng > tồn kho:';
PRINT N'   - Sẽ nhận được lỗi: "Số lượng yêu cầu vượt quá tồn kho khả dụng"';
PRINT N'';
PRINT N'2. Thử thêm sản phẩm không có trong kho:';
PRINT N'   - Sẽ nhận được lỗi: "Số lượng yêu cầu vượt quá tồn kho khả dụng (0)"';
PRINT N'';
GO
