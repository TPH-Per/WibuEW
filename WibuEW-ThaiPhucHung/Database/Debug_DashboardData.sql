-- Script kiểm tra dữ liệu cho biểu đồ Dashboard
-- Chạy script này trong SQL Server Management Studio

USE perw;
GO

PRINT '=== KIỂM TRA DỮ LIỆU CHO DASHBOARD ===';
PRINT '';

-- 1. Kiểm tra tổng số đơn hàng completed
PRINT '1. Tổng số đơn hàng completed:';
SELECT COUNT(*) AS TotalCompletedOrders
FROM purchase_orders
WHERE deleted_at IS NULL AND status = 'completed';
PRINT '';

-- 2. Kiểm tra đơn hàng completed trong 7 ngày qua
PRINT '2. Đơn hàng completed trong 7 ngày qua:';
SELECT COUNT(*) AS CompletedLast7Days
FROM purchase_orders
WHERE deleted_at IS NULL 
  AND status = 'completed'
  AND created_at >= DATEADD(DAY, -7, GETDATE());
PRINT '';

-- 3. Kiểm tra ngày tạo của đơn hàng completed
PRINT '3. Ngày tạo của các đơn hàng completed:';
SELECT 
    id,
    order_code,
    created_at,
    DATEDIFF(DAY, created_at, GETDATE()) AS DaysAgo
FROM purchase_orders
WHERE deleted_at IS NULL AND status = 'completed'
ORDER BY created_at DESC;
PRINT '';

-- 4. Kiểm tra doanh thu theo chi nhánh (TẤT CẢ)
PRINT '4. Doanh thu theo chi nhánh (tất cả thời gian):';
SELECT 
    b.name AS BranchName,
    COUNT(po.id) AS OrderCount,
    SUM(po.total_amount) AS TotalRevenue
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.deleted_at IS NULL AND po.status = 'completed'
GROUP BY b.name
ORDER BY SUM(po.total_amount) DESC;
PRINT '';

-- 5. Kiểm tra doanh thu theo chi nhánh (7 NGÀY QUA)
PRINT '5. Doanh thu theo chi nhánh (7 ngày qua):';
SELECT 
    b.name AS BranchName,
    COUNT(po.id) AS OrderCount,
    SUM(po.total_amount) AS TotalRevenue
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.deleted_at IS NULL 
  AND po.status = 'completed'
  AND po.created_at >= DATEADD(DAY, -7, GETDATE())
GROUP BY b.name
ORDER BY SUM(po.total_amount) DESC;
PRINT '';

-- 6. Kiểm tra chi nhánh có trong database
PRINT '6. Danh sách chi nhánh:';
SELECT id, name, location
FROM branches
ORDER BY id;
PRINT '';

PRINT '=== KẾT THÚC KIỂM TRA ===';
