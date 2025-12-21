-- =====================================================
-- Script: Tạo Stored Procedure báo cáo tổng tồn kho
-- Purpose: sp_BaoCaoTongTonKhoDonGian
-- Date: 2025-12-15
-- =====================================================

USE [perw];
GO

-- Tạo hoặc cập nhật stored procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_BaoCaoTongTonKhoDonGian]
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Báo cáo Tổng tồn kho của các Kho Tổng (Bảng inventories)
    SELECT
        w.name AS [Tên Kho],
        N'Kho Tổng' AS [Loại Kho],
        ISNULL(SUM(i.quantity_on_hand), 0) AS [Tổng Số Lượng Tồn],
        ISNULL(SUM(i.quantity_reserved), 0) AS [Hàng Đặt Trước]
    FROM warehouses w
    LEFT JOIN inventories i ON w.id = i.warehouse_id
    GROUP BY w.name

    UNION ALL -- Kết hợp kết quả từ Kho Tổng và Chi nhánh

    -- Báo cáo Tổng tồn kho của các Chi nhánh (Bảng branch_inventories)
    SELECT
        b.name AS [Tên Kho],
        N'Chi Nhánh' AS [Loại Kho],
        ISNULL(SUM(bi.quantity_on_hand), 0) AS [Tổng Số Lượng Tồn],
        0 AS [Hàng Đặt Trước] 
    FROM branches b
    LEFT JOIN branch_inventories bi ON b.id = bi.branch_id
    GROUP BY b.name
    
    ORDER BY [Loại Kho] DESC, [Tổng Số Lượng Tồn] DESC;
END
GO

PRINT N'✅ Stored procedure sp_BaoCaoTongTonKhoDonGian đã được tạo/cập nhật.';
GO

-- Test stored procedure
PRINT N'';
PRINT N'=== TEST STORED PROCEDURE ===';
EXEC [dbo].[sp_BaoCaoTongTonKhoDonGian];
GO
