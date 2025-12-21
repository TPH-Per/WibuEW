-- =============================================
-- SCRIPT: STORED PROCEDURE CHO BRANCH TẠO YÊU CẦU NHẬP HÀNG
-- Cho phép Branch tạo phiếu yêu cầu điều chuyển từ Kho sang Chi nhánh
-- Status luôn là 'Requested' - chỉ Warehouse mới có thể hoàn thành
-- Date: 2025-12-15
-- =============================================

USE perw;
GO

-- =============================================
-- PHẦN 1: TẠO STORED PROCEDURE
-- =============================================

-- Drop nếu đã tồn tại
IF OBJECT_ID('dbo.sp_Branch_CreateTransferRequest', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Branch_CreateTransferRequest;
GO

IF OBJECT_ID('dbo.sp_Branch_AddTransferRequestDetail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Branch_AddTransferRequestDetail;
GO

IF OBJECT_ID('dbo.sp_Branch_GetMyTransferRequests', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Branch_GetMyTransferRequests;
GO

IF OBJECT_ID('dbo.sp_Branch_GetTransferRequestDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Branch_GetTransferRequestDetails;
GO

-- ---------------------------------------------
-- SP 1: Tạo phiếu yêu cầu nhập hàng (header)
-- Ràng buộc: Status luôn là 'Requested'
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Branch_CreateTransferRequest
    @from_warehouse_id BIGINT,
    @to_branch_id BIGINT,
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
        
        -- Tạo phiếu yêu cầu với status = 'Requested' (KHÔNG CHO PHÉP GIÁ TRỊ KHÁC)
        INSERT INTO dbo.warehouse_transfers (
            from_warehouse_id,
            to_branch_id,
            transfer_date,
            status,          -- Luôn là 'Requested'
            notes,
            created_at,
            updated_at
        )
        VALUES (
            @from_warehouse_id,
            @to_branch_id,
            GETDATE(),
            'Requested',     -- RÀNG BUỘC: Luôn là 'Requested'
            @notes,
            GETDATE(),
            GETDATE()
        );
        
        SET @new_transfer_id = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
        
        PRINT N'Đã tạo yêu cầu nhập hàng thành công. ID: ' + CAST(@new_transfer_id AS NVARCHAR(20));
        
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
-- SP 2: Thêm chi tiết sản phẩm vào phiếu yêu cầu
-- Ràng buộc: Chỉ thêm được khi status = 'Requested'
-- ---------------------------------------------
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
        
        PRINT N'Đã thêm sản phẩm vào phiếu yêu cầu thành công';
        
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
-- SP 3: Xem danh sách yêu cầu của chi nhánh
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Branch_GetMyTransferRequests
    @branch_id BIGINT
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
        (SELECT SUM(quantity * ISNULL(price, 0)) FROM dbo.warehouse_transfer_details WHERE transfer_id = wt.id) AS total_amount
    FROM dbo.warehouse_transfers wt
    INNER JOIN dbo.warehouses w ON wt.from_warehouse_id = w.id
    INNER JOIN dbo.branches b ON wt.to_branch_id = b.id
    WHERE wt.to_branch_id = @branch_id
    ORDER BY wt.created_at DESC;
END;
GO

-- ---------------------------------------------
-- SP 4: Xem chi tiết phiếu yêu cầu
-- ---------------------------------------------
CREATE PROCEDURE dbo.sp_Branch_GetTransferRequestDetails
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
        wtd.notes
    FROM dbo.warehouse_transfer_details wtd
    INNER JOIN dbo.product_variants pv ON wtd.product_variant_id = pv.id
    INNER JOIN dbo.products p ON pv.product_id = p.id
    WHERE wtd.transfer_id = @transfer_id
    ORDER BY wtd.id;
END;
GO

PRINT N'';
PRINT N'✅ Đã tạo xong 4 stored procedures cho Branch';
PRINT N'';
GO

-- =============================================
-- PHẦN 2: TẠO DATABASE ROLE (nếu chưa có)
-- =============================================

PRINT N'=== KIỂM TRA VÀ TẠO ROLE ===';

-- Tạo role nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_branch' AND type = 'R')
BEGIN
    CREATE ROLE role_branch;
    PRINT N'Đã tạo role: role_branch';
END
ELSE
BEGIN
    PRINT N'Role role_branch đã tồn tại';
END
GO

-- =============================================
-- PHẦN 3: CẤP QUYỀN EXECUTE CHO ROLE BRANCH
-- =============================================

PRINT N'=== CẤP QUYỀN EXECUTE CHO ROLE_BRANCH ===';

-- Cấp quyền execute các stored procedures cho role_branch
GRANT EXECUTE ON dbo.sp_Branch_CreateTransferRequest TO role_branch;
GRANT EXECUTE ON dbo.sp_Branch_AddTransferRequestDetail TO role_branch;
GRANT EXECUTE ON dbo.sp_Branch_GetMyTransferRequests TO role_branch;
GRANT EXECUTE ON dbo.sp_Branch_GetTransferRequestDetails TO role_branch;

-- Thêm quyền SELECT trên các bảng cần thiết
GRANT SELECT ON dbo.warehouses TO role_branch;
GRANT SELECT ON dbo.branches TO role_branch;
GRANT SELECT ON dbo.warehouse_transfers TO role_branch;
GRANT SELECT ON dbo.warehouse_transfer_details TO role_branch;
GRANT SELECT ON dbo.product_variants TO role_branch;
GRANT SELECT ON dbo.products TO role_branch;

PRINT N'✅ Đã cấp quyền execute cho role_branch thành công';
GO

-- =============================================
-- PHẦN 4: TEST STORED PROCEDURES
-- =============================================

PRINT N'';
PRINT N'=== TEST STORED PROCEDURES ===';
PRINT N'';

-- Test 1: Tạo phiếu yêu cầu
DECLARE @test_transfer_id BIGINT;
DECLARE @test_warehouse_id BIGINT;
DECLARE @test_branch_id BIGINT;

-- Lấy warehouse và branch đầu tiên để test
SELECT TOP 1 @test_warehouse_id = id FROM dbo.warehouses;
SELECT TOP 1 @test_branch_id = id FROM dbo.branches;

IF @test_warehouse_id IS NOT NULL AND @test_branch_id IS NOT NULL
BEGIN
    PRINT N'Test: Tạo phiếu yêu cầu nhập hàng...';
    
    EXEC dbo.sp_Branch_CreateTransferRequest
        @from_warehouse_id = @test_warehouse_id,
        @to_branch_id = @test_branch_id,
        @notes = N'[TEST] Yêu cầu nhập hàng test',
        @new_transfer_id = @test_transfer_id OUTPUT;
    
    PRINT N'Transfer ID mới: ' + ISNULL(CAST(@test_transfer_id AS NVARCHAR(20)), 'NULL');
    
    -- Test 2: Thêm sản phẩm
    DECLARE @test_variant_id BIGINT;
    SELECT TOP 1 @test_variant_id = id FROM dbo.product_variants WHERE deleted_at IS NULL;
    
    IF @test_variant_id IS NOT NULL AND @test_transfer_id IS NOT NULL
    BEGIN
        PRINT N'Test: Thêm sản phẩm vào phiếu...';
        
        EXEC dbo.sp_Branch_AddTransferRequestDetail
            @transfer_id = @test_transfer_id,
            @product_variant_id = @test_variant_id,
            @quantity = 5,
            @notes = N'[TEST] Sản phẩm test';
    END
    
    -- Test 3: Xem danh sách
    PRINT N'';
    PRINT N'Test: Danh sách yêu cầu của chi nhánh:';
    EXEC dbo.sp_Branch_GetMyTransferRequests @branch_id = @test_branch_id;
    
    -- Test 4: Xem chi tiết
    IF @test_transfer_id IS NOT NULL
    BEGIN
        PRINT N'';
        PRINT N'Test: Chi tiết phiếu yêu cầu:';
        EXEC dbo.sp_Branch_GetTransferRequestDetails @transfer_id = @test_transfer_id;
    END
END
ELSE
BEGIN
    PRINT N'⚠️ Không có dữ liệu warehouse/branch để test. Vui lòng thêm dữ liệu.';
END
GO

-- =============================================
-- PHẦN 5: HƯỚNG DẪN SỬ DỤNG
-- =============================================

PRINT N'';
PRINT N'========================================';
PRINT N'  HƯỚNG DẪN SỬ DỤNG CHO BRANCH';
PRINT N'========================================';
PRINT N'';
PRINT N'1. Tạo phiếu yêu cầu nhập hàng:';
PRINT N'   DECLARE @new_id BIGINT;';
PRINT N'   EXEC sp_Branch_CreateTransferRequest';
PRINT N'       @from_warehouse_id = 1,';
PRINT N'       @to_branch_id = 1,';
PRINT N'       @notes = N''Yêu cầu nhập hàng tháng 12'',';
PRINT N'       @new_transfer_id = @new_id OUTPUT;';
PRINT N'   SELECT @new_id AS NewTransferID;';
PRINT N'';
PRINT N'2. Thêm sản phẩm vào phiếu:';
PRINT N'   EXEC sp_Branch_AddTransferRequestDetail';
PRINT N'       @transfer_id = 1,';
PRINT N'       @product_variant_id = 1,';
PRINT N'       @quantity = 10,';
PRINT N'       @notes = N''Sản phẩm bán chạy'';';
PRINT N'';
PRINT N'3. Xem danh sách yêu cầu của chi nhánh:';
PRINT N'   EXEC sp_Branch_GetMyTransferRequests @branch_id = 1;';
PRINT N'';
PRINT N'4. Xem chi tiết phiếu yêu cầu:';
PRINT N'   EXEC sp_Branch_GetTransferRequestDetails @transfer_id = 1;';
PRINT N'';
PRINT N'========================================';
PRINT N'  LƯU Ý QUAN TRỌNG';
PRINT N'========================================';
PRINT N'- Branch CHỈ có thể tạo yêu cầu với status = ''Requested''';
PRINT N'- Branch KHÔNG thể thay đổi status của phiếu';
PRINT N'- Chỉ Warehouse mới có quyền duyệt/từ chối/hoàn thành phiếu';
PRINT N'========================================';
GO
