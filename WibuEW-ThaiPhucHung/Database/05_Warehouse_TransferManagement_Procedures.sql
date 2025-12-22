-- =============================================
-- SCRIPT: STORED PROCEDURE CHO WAREHOUSE QUẢN LÝ PHIẾU ĐIỀU CHUYỂN
-- Warehouse có toàn quyền: Tạo, sửa, duyệt, từ chối, hoàn thành
-- Date: 2025-12-15
-- =============================================

USE perw;
GO

-- =============================================
-- PHẦN 1: DROP CÁC PROCEDURE CŨ NẾU TỒN TẠI
-- =============================================

IF OBJECT_ID('dbo.sp_Warehouse_CreateTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_CreateTransfer;
GO

IF OBJECT_ID('dbo.sp_Warehouse_AddTransferDetail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_AddTransferDetail;
GO

IF OBJECT_ID('dbo.sp_Warehouse_UpdateTransferStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_UpdateTransferStatus;
GO

IF OBJECT_ID('dbo.sp_Warehouse_ApproveTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_ApproveTransfer;
GO

IF OBJECT_ID('dbo.sp_Warehouse_CompleteTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_CompleteTransfer;
GO

IF OBJECT_ID('dbo.sp_Warehouse_RejectTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_RejectTransfer;
GO

IF OBJECT_ID('dbo.sp_Warehouse_GetAllTransfers', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_GetAllTransfers;
GO

IF OBJECT_ID('dbo.sp_Warehouse_GetTransferDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Warehouse_GetTransferDetails;
GO

-- =============================================
-- PHẦN 2: TẠO STORED PROCEDURES
-- =============================================

-- ---------------------------------------------
-- SP 1: Tạo phiếu điều chuyển (Warehouse có thể set status bất kỳ)
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_CreateTransfer
    @from_warehouse_id BIGINT,
    @to_branch_id BIGINT,
    @status NVARCHAR(50) = 'Pending',  -- Warehouse có thể chọn status
    @notes NVARCHAR(500) = NULL,
    @new_transfer_id BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra warehouse có tồn tại
        IF NOT EXISTS (SELECT 1 FROM dbo.warehouses WHERE id = @from_warehouse_id)
        BEGIN
            RAISERROR(N'Kho không tồn tại (warehouse_id: %d)', 16, 1, @from_warehouse_id);
            RETURN;
        END
        
        -- Kiểm tra branch có tồn tại
        IF NOT EXISTS (SELECT 1 FROM dbo.branches WHERE id = @to_branch_id)
        BEGIN
            RAISERROR(N'Chi nhánh không tồn tại (branch_id: %d)', 16, 1, @to_branch_id);
            RETURN;
        END
        
        -- Validate status (chỉ chấp nhận các giá trị hợp lệ)
        IF @status NOT IN ('Pending', 'Requested', 'Approved', 'Processing', 'Completed', 'Cancelled', 'Rejected')
        BEGIN
            RAISERROR(N'Status không hợp lệ. Các giá trị hợp lệ: Pending, Requested, Approved, Processing, Completed, Cancelled, Rejected', 16, 1);
            RETURN;
        END
        
        -- Tạo phiếu điều chuyển
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
        
        PRINT N'✅ Đã tạo phiếu điều chuyển thành công. ID: ' + CAST(@new_transfer_id AS NVARCHAR(20));
        
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

-- ---------------------------------------------
-- SP 2: Thêm chi tiết sản phẩm vào phiếu
-- ---------------------------------------------
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

-- ---------------------------------------------
-- SP 3: Cập nhật status phiếu (Warehouse có toàn quyền)
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_UpdateTransferStatus
    @transfer_id BIGINT,
    @new_status NVARCHAR(50),
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu có tồn tại
        IF NOT EXISTS (SELECT 1 FROM dbo.warehouse_transfers WHERE id = @transfer_id)
        BEGIN
            RAISERROR(N'Phiếu điều chuyển không tồn tại (transfer_id: %d)', 16, 1, @transfer_id);
            RETURN;
        END
        
        -- Validate status
        IF @new_status NOT IN ('Pending', 'Requested', 'Approved', 'Processing', 'Completed', 'Cancelled', 'Rejected')
        BEGIN
            RAISERROR(N'Status không hợp lệ. Các giá trị hợp lệ: Pending, Requested, Approved, Processing, Completed, Cancelled, Rejected', 16, 1);
            RETURN;
        END
        
        -- Lấy status hiện tại
        DECLARE @current_status NVARCHAR(50);
        SELECT @current_status = status FROM dbo.warehouse_transfers WHERE id = @transfer_id;
        
        -- Cập nhật status
        UPDATE dbo.warehouse_transfers
        SET 
            status = @new_status,
            notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE notes END,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã cập nhật status từ [' + @current_status + N'] thành [' + @new_status + N']';
        
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

-- ---------------------------------------------
-- SP 4: Duyệt yêu cầu từ Branch (Approved)
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_ApproveTransfer
    @transfer_id BIGINT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu có tồn tại và ở trạng thái Requested
        IF NOT EXISTS (
            SELECT 1 FROM dbo.warehouse_transfers 
            WHERE id = @transfer_id AND status = 'Requested'
        )
        BEGIN
            RAISERROR(N'Phiếu không tồn tại hoặc không ở trạng thái Requested', 16, 1);
            RETURN;
        END
        
        -- Cập nhật status thành Approved
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Approved',
            notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE notes END,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã duyệt phiếu điều chuyển #' + CAST(@transfer_id AS NVARCHAR(20));
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- ---------------------------------------------
-- SP 5: Hoàn thành phiếu (Completed) + Cập nhật tồn kho
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_CompleteTransfer
    @transfer_id BIGINT,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu có tồn tại và ở trạng thái Approved hoặc Processing
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
            RAISERROR(N'Phiếu điều chuyển không tồn tại', 16, 1);
            RETURN;
        END
        
        IF @current_status NOT IN ('Approved', 'Processing')
        BEGIN
            RAISERROR(N'Phiếu phải ở trạng thái Approved hoặc Processing để hoàn thành', 16, 1);
            RETURN;
        END
        
        -- Xử lý từng sản phẩm trong chi tiết
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
            -- Kiểm tra tồn kho warehouse đủ không
            DECLARE @warehouse_qty INT;
            SELECT @warehouse_qty = ISNULL(quantity_on_hand, 0)
            FROM dbo.inventories
            WHERE warehouse_id = @from_warehouse_id AND product_variant_id = @variant_id;
            
            IF @warehouse_qty IS NULL OR @warehouse_qty < @qty
            BEGIN
                CLOSE transfer_cursor;
                DEALLOCATE transfer_cursor;
                
                DECLARE @err_msg NVARCHAR(500);
                SET @err_msg = N'Không đủ tồn kho trong warehouse cho sản phẩm (variant_id: ' 
                    + CAST(@variant_id AS NVARCHAR(20)) 
                    + N'). Cần: ' + CAST(@qty AS NVARCHAR(20)) 
                    + N', Có: ' + CAST(ISNULL(@warehouse_qty, 0) AS NVARCHAR(20));
                RAISERROR(@err_msg, 16, 1);
                RETURN;
            END
            
            -- Trừ tồn kho warehouse
            UPDATE dbo.inventories
            SET 
                quantity_on_hand = quantity_on_hand - @qty,
                updated_at = GETDATE()
            WHERE warehouse_id = @from_warehouse_id AND product_variant_id = @variant_id;
            
            -- Cộng tồn kho branch (hoặc tạo mới nếu chưa có)
            IF EXISTS (
                SELECT 1 FROM dbo.branch_inventories 
                WHERE branch_id = @to_branch_id AND product_variant_id = @variant_id
            )
            BEGIN
                UPDATE dbo.branch_inventories
                SET 
                    quantity_on_hand = quantity_on_hand + @qty,
                    updated_at = GETDATE()
                WHERE branch_id = @to_branch_id AND product_variant_id = @variant_id;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.branch_inventories (
                    branch_id, product_variant_id, quantity_on_hand, 
                    quantity_reserved, reorder_level, created_at, updated_at
                )
                VALUES (
                    @to_branch_id, @variant_id, @qty,
                    0, 10, GETDATE(), GETDATE()
                );
            END
            
            FETCH NEXT FROM transfer_cursor INTO @variant_id, @qty;
        END
        
        CLOSE transfer_cursor;
        DEALLOCATE transfer_cursor;
        
        -- Cập nhật status thành Completed
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Completed',
            notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE notes END,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã hoàn thành phiếu điều chuyển #' + CAST(@transfer_id AS NVARCHAR(20)) + N' và cập nhật tồn kho';
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        -- Close cursor nếu còn mở
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

-- ---------------------------------------------
-- SP 6: Từ chối yêu cầu (Rejected)
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_RejectTransfer
    @transfer_id BIGINT,
    @reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra phiếu có tồn tại và chưa hoàn thành
        IF NOT EXISTS (
            SELECT 1 FROM dbo.warehouse_transfers 
            WHERE id = @transfer_id AND status NOT IN ('Completed', 'Cancelled')
        )
        BEGIN
            RAISERROR(N'Phiếu không tồn tại hoặc đã hoàn thành/hủy', 16, 1);
            RETURN;
        END
        
        -- Cập nhật status thành Rejected
        UPDATE dbo.warehouse_transfers
        SET 
            status = 'Rejected',
            notes = N'[TỪ CHỐI] ' + @reason,
            updated_at = GETDATE()
        WHERE id = @transfer_id;
        
        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã từ chối phiếu điều chuyển #' + CAST(@transfer_id AS NVARCHAR(20));
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

-- ---------------------------------------------
-- SP 7: Xem tất cả phiếu điều chuyển
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_GetAllTransfers
    @warehouse_id BIGINT = NULL,
    @status NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        wt.id,
        wt.from_warehouse_id,
        w.name AS warehouse_name,
        wt.to_branch_id,
        b.name AS branch_name,
        wt.transfer_date,
        wt.status,
        wt.notes,
        wt.created_at,
        wt.updated_at,
        (SELECT COUNT(*) FROM dbo.warehouse_transfer_details WHERE transfer_id = wt.id) AS total_items,
        (SELECT SUM(quantity) FROM dbo.warehouse_transfer_details WHERE transfer_id = wt.id) AS total_quantity,
        (SELECT SUM(quantity * ISNULL(price, 0)) FROM dbo.warehouse_transfer_details WHERE transfer_id = wt.id) AS total_amount
    FROM dbo.warehouse_transfers wt
    INNER JOIN dbo.warehouses w ON wt.from_warehouse_id = w.id
    INNER JOIN dbo.branches b ON wt.to_branch_id = b.id
    WHERE 
        (@warehouse_id IS NULL OR wt.from_warehouse_id = @warehouse_id)
        AND (@status IS NULL OR wt.status = @status)
    ORDER BY wt.created_at DESC;
END;
GO

-- ---------------------------------------------
-- SP 8: Xem chi tiết phiếu điều chuyển
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Warehouse_GetTransferDetails
    @transfer_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Header
    SELECT 
        wt.id,
        wt.from_warehouse_id,
        w.name AS warehouse_name,
        wt.to_branch_id,
        b.name AS branch_name,
        wt.transfer_date,
        wt.status,
        wt.notes,
        wt.created_at,
        wt.updated_at
    FROM dbo.warehouse_transfers wt
    INNER JOIN dbo.warehouses w ON wt.from_warehouse_id = w.id
    INNER JOIN dbo.branches b ON wt.to_branch_id = b.id
    WHERE wt.id = @transfer_id;
    
    -- Details
    SELECT 
        wtd.id,
        wtd.transfer_id,
        wtd.product_variant_id,
        pv.sku,
        p.name AS product_name,
        pv.name AS variant_name,
        wtd.quantity,
        wtd.price,
        (wtd.quantity * ISNULL(wtd.price, 0)) AS line_total,
        wtd.notes,
        -- Thông tin tồn kho
        ISNULL(inv.quantity_on_hand, 0) AS warehouse_stock
    FROM dbo.warehouse_transfer_details wtd
    INNER JOIN dbo.product_variants pv ON wtd.product_variant_id = pv.id
    INNER JOIN dbo.products p ON pv.product_id = p.id
    LEFT JOIN dbo.inventories inv ON inv.product_variant_id = wtd.product_variant_id 
        AND inv.warehouse_id = (SELECT from_warehouse_id FROM dbo.warehouse_transfers WHERE id = @transfer_id)
    WHERE wtd.transfer_id = @transfer_id
    ORDER BY wtd.id;
END;
GO

PRINT N'';
PRINT N'✅ Đã tạo xong 8 stored procedures cho Warehouse';
PRINT N'';
GO

-- =============================================
-- PHẦN 3: TẠO DATABASE ROLE (nếu chưa có)
-- =============================================

PRINT N'=== KIỂM TRA VÀ TẠO ROLE ===';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_warehouse' AND type = 'R')
BEGIN
    CREATE ROLE role_warehouse;
    PRINT N'Đã tạo role: role_warehouse';
END
ELSE
BEGIN
    PRINT N'Role role_warehouse đã tồn tại';
END
GO

-- =============================================
-- PHẦN 4: CẤP QUYỀN EXECUTE CHO ROLE WAREHOUSE
-- =============================================

PRINT N'=== CẤP QUYỀN EXECUTE CHO ROLE_WAREHOUSE ===';

-- Cấp quyền execute các stored procedures
GRANT EXECUTE ON dbo.sp_Warehouse_CreateTransfer TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_AddTransferDetail TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_UpdateTransferStatus TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_ApproveTransfer TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_CompleteTransfer TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_RejectTransfer TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_GetAllTransfers TO role_warehouse;
GRANT EXECUTE ON dbo.sp_Warehouse_GetTransferDetails TO role_warehouse;

-- Quyền trên các bảng
GRANT SELECT, INSERT, UPDATE ON dbo.warehouse_transfers TO role_warehouse;
GRANT SELECT, INSERT, UPDATE ON dbo.warehouse_transfer_details TO role_warehouse;
GRANT SELECT, UPDATE ON dbo.inventories TO role_warehouse;
GRANT SELECT, INSERT, UPDATE ON dbo.branch_inventories TO role_warehouse;
GRANT SELECT ON dbo.warehouses TO role_warehouse;
GRANT SELECT ON dbo.branches TO role_warehouse;
GRANT SELECT ON dbo.product_variants TO role_warehouse;
GRANT SELECT ON dbo.products TO role_warehouse;

PRINT N'✅ Đã cấp quyền cho role_warehouse thành công';
GO

-- =============================================
-- PHẦN 5: HƯỚNG DẪN SỬ DỤNG
-- =============================================

PRINT N'';
PRINT N'========================================';
PRINT N'  HƯỚNG DẪN SỬ DỤNG CHO WAREHOUSE';
PRINT N'========================================';
PRINT N'';
PRINT N'1. Tạo phiếu điều chuyển mới:';
PRINT N'   DECLARE @new_id BIGINT;';
PRINT N'   EXEC sp_Warehouse_CreateTransfer';
PRINT N'       @from_warehouse_id = 1,';
PRINT N'       @to_branch_id = 1,';
PRINT N'       @status = ''Approved'',  -- Có thể set status bất kỳ';
PRINT N'       @notes = N''Xuất hàng trực tiếp'',';
PRINT N'       @new_transfer_id = @new_id OUTPUT;';
PRINT N'';
PRINT N'2. Thêm sản phẩm vào phiếu:';
PRINT N'   EXEC sp_Warehouse_AddTransferDetail';
PRINT N'       @transfer_id = 1,';
PRINT N'       @product_variant_id = 1,';
PRINT N'       @quantity = 50;';
PRINT N'';
PRINT N'3. Cập nhật status phiếu:';
PRINT N'   EXEC sp_Warehouse_UpdateTransferStatus';
PRINT N'       @transfer_id = 1,';
PRINT N'       @new_status = ''Processing'';';
PRINT N'';
PRINT N'4. Duyệt yêu cầu từ Branch:';
PRINT N'   EXEC sp_Warehouse_ApproveTransfer @transfer_id = 1;';
PRINT N'';
PRINT N'5. Hoàn thành phiếu (cập nhật tồn kho):';
PRINT N'   EXEC sp_Warehouse_CompleteTransfer @transfer_id = 1;';
PRINT N'';
PRINT N'6. Từ chối yêu cầu:';
PRINT N'   EXEC sp_Warehouse_RejectTransfer';
PRINT N'       @transfer_id = 1,';
PRINT N'       @reason = N''Hết hàng trong kho'';';
PRINT N'';
PRINT N'7. Xem tất cả phiếu:';
PRINT N'   EXEC sp_Warehouse_GetAllTransfers;';
PRINT N'   EXEC sp_Warehouse_GetAllTransfers @status = ''Requested'';';
PRINT N'';
PRINT N'8. Xem chi tiết phiếu:';
PRINT N'   EXEC sp_Warehouse_GetTransferDetails @transfer_id = 1;';
PRINT N'';
PRINT N'========================================';
PRINT N'  STATUS WORKFLOW';
PRINT N'========================================';
PRINT N'  Requested -> Approved -> Completed';
PRINT N'           \\-> Rejected';
PRINT N'  Pending -> Processing -> Completed';
PRINT N'         \\-> Cancelled';
PRINT N'========================================';
GO
