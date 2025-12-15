-- =============================================
-- MASTER SCRIPT: Chạy tất cả database scripts
-- Thứ tự: Table → Types → Triggers → Procedures
-- =============================================

PRINT N'';
PRINT N'========================================';
PRINT N'BẮT ĐẦU CÀI ĐẶT DATABASE COMPONENTS';
PRINT N'========================================';
PRINT N'';

-- Đảm bảo đang dùng đúng database
USE [perw];
GO

PRINT N'▶ Bước 1/4: Tạo bảng warehouse_transfer_details...';
:r "01_CreateWarehouseTransferDetails.sql"

PRINT N'';
PRINT N'▶ Bước 2/4: Tạo Table Types...';
:r "02_CreateTableTypes.sql"

PRINT N'';
PRINT N'▶ Bước 3/4: Tạo Triggers...';
:r "03_CreateTriggers.sql"

PRINT N'';
PRINT N'▶ Bước 4/4: Tạo Stored Procedures...';
:r "04_CreateStoredProcedures.sql"

PRINT N'';
PRINT N'========================================';
PRINT N'✅ HOÀN TẤT CÀI ĐẶT DATABASE';
PRINT N'========================================';
PRINT N'';
PRINT N'Các đối tượng đã được tạo:';
PRINT N'  ✓ Bảng: warehouse_transfer_details';
PRINT N'  ✓ Type: CartItemTableType';
PRINT N'  ✓ Trigger: trg_Transfer_OnShip';
PRINT N'  ✓ Trigger: trg_Transfer_OnReceive';
PRINT N'  ✓ Trigger: trg_Transfer_OnReturn';
PRINT N'  ✓ Procedure: sp_ProcessTransferIssue';
PRINT N'  ✓ Procedure: sp_POS_Checkout_Classic';
PRINT N'';
PRINT N'Sẵn sàng để sử dụng! 🚀';
PRINT N'';
GO
