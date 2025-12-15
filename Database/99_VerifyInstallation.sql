-- =============================================
-- QUICK TEST: Verify All Components Installed
-- =============================================

USE [perw];
GO

PRINT N'';
PRINT N'========================================';
PRINT N'KIỂM TRA CÀI ĐẶT DATABASE';
PRINT N'========================================';
PRINT N'';

-- 1. Check Tables
PRINT N'1. Bảng warehouse_transfer_details:';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'warehouse_transfer_details')
    PRINT N'   ✅ OK'
ELSE
    PRINT N'   ❌ CHƯA CÓ - Chạy 01_CreateWarehouseTransferDetails.sql';
PRINT N'';

-- 2. Check Table Types
PRINT N'2. Table Type CartItemTableType:';
IF EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CartItemTableType')
    PRINT N'   ✅ OK'
ELSE
    PRINT N'   ❌ CHƯA CÓ - Chạy 02_CreateTableTypes.sql';
PRINT N'';

-- 3. Check Triggers
PRINT N'3. Triggers:';
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Transfer_OnShip')
    PRINT N'   ✅ trg_Transfer_OnShip'
ELSE
    PRINT N'   ❌ trg_Transfer_OnShip - Chạy 03_CreateTriggers.sql';

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Transfer_OnReceive')
    PRINT N'   ✅ trg_Transfer_OnReceive'
ELSE
    PRINT N'   ❌ trg_Transfer_OnReceive - Chạy 03_CreateTriggers.sql';

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Transfer_OnReturn')
    PRINT N'   ✅ trg_Transfer_OnReturn'
ELSE
    PRINT N'   ❌ trg_Transfer_OnReturn - Chạy 03_CreateTriggers.sql';
PRINT N'';

-- 4. Check Stored Procedures
PRINT N'4. Stored Procedures:';
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_ProcessTransferIssue')
    PRINT N'   ✅ sp_ProcessTransferIssue'
ELSE
    PRINT N'   ❌ sp_ProcessTransferIssue - Chạy 04_CreateStoredProcedures.sql';

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_POS_Checkout_Classic')
    PRINT N'   ✅ sp_POS_Checkout_Classic'
ELSE
    PRINT N'   ❌ sp_POS_Checkout_Classic - Chạy 04_CreateStoredProcedures.sql';
PRINT N'';

-- 5. Check Sample Data
PRINT N'5. Dữ Liệu Mẫu:';
DECLARE @ProductCount INT = (SELECT COUNT(*) FROM product_variants);
DECLARE @BranchInvCount INT = (SELECT COUNT(*) FROM branch_inventories WHERE branch_id = 1);

PRINT N'   Products: ' + CAST(@ProductCount AS NVARCHAR);
IF @ProductCount >= 10
    PRINT N'   ✅ Đã có sản phẩm'
ELSE
    PRINT N'   ⚠ Chỉ có ' + CAST(@ProductCount AS NVARCHAR) + ' sản phẩm - Chạy 05_InsertSampleData.sql';

PRINT N'   Branch Inventory (Branch 1): ' + CAST(@BranchInvCount AS NVARCHAR);
IF @BranchInvCount >= 10
    PRINT N'   ✅ Đã có inventory'
ELSE
    PRINT N'   ⚠ Chỉ có ' + CAST(@BranchInvCount AS NVARCHAR) + ' records - Chạy 05_InsertSampleData.sql';

PRINT N'';
PRINT N'========================================';
PRINT N'KẾT THÚC KIỂM TRA';
PRINT N'========================================';
PRINT N'';
GO
