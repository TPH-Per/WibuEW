-- =============================================
-- Tạo Table-Valued Parameter Types
-- Dùng cho stored procedures
-- =============================================

USE [perw];
GO

-- Table Type cho POS Checkout
IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'CartItemTableType')
BEGIN
    CREATE TYPE [dbo].[CartItemTableType] AS TABLE (
        [VariantID] BIGINT NOT NULL,
        [Qty] INT NOT NULL
    );
    
    PRINT N'✅ Table Type CartItemTableType đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT N'⚠️ Table Type CartItemTableType đã tồn tại.';
END
GO
