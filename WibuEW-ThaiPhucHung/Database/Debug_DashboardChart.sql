-- Script kiểm tra dữ liệu cho Dashboard Chart
USE perw;
GO

PRINT '=== DEBUG DASHBOARD CHART DATA ===';
PRINT '';

-- 1. Kiểm tra đơn hàng completed
PRINT '1. Tổng số đơn hàng completed:';
SELECT COUNT(*) AS TotalCompleted
FROM purchase_orders
WHERE status = 'completed';
PRINT '';

-- 2. Kiểm tra đơn hàng completed trong 7 ngày qua
PRINT '2. Đơn hàng completed trong 7 ngày qua:';
SELECT COUNT(*) AS CompletedLast7Days
FROM purchase_orders
WHERE status = 'completed'
  AND created_at >= DATEADD(DAY, -7, GETDATE());
PRINT '';

-- 3. Doanh thu theo chi nhánh (TẤT CẢ thời gian)
PRINT '3. Doanh thu theo chi nhánh (tất cả):';
SELECT 
    b.name AS BranchName,
    COUNT(po.id) AS OrderCount,
    SUM(po.total_amount) AS TotalRevenue
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.status = 'completed'
GROUP BY b.name
ORDER BY SUM(po.total_amount) DESC;
PRINT '';

-- 4. Doanh thu theo chi nhánh (7 NGÀY QUA) - Cho chart
PRINT '4. Doanh thu theo chi nhánh (7 ngày qua) - DỮ LIỆU CHO CHART:';
SELECT 
    b.name AS BranchName,
    COUNT(po.id) AS OrderCount,
    SUM(po.total_amount) AS TotalRevenue
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.status = 'completed'
  AND po.created_at >= DATEADD(DAY, -7, GETDATE())
GROUP BY b.name
ORDER BY SUM(po.total_amount) DESC;
PRINT '';

-- 5. Nếu không có dữ liệu 7 ngày, kiểm tra ngày tạo
PRINT '5. Ngày tạo của đơn hàng completed:';
SELECT TOP 10
    id,
    order_code,
    created_at,
    DATEDIFF(DAY, created_at, GETDATE()) AS DaysAgo,
    total_amount
FROM purchase_orders
WHERE status = 'completed'
ORDER BY created_at DESC;
PRINT '';

PRINT '=== KẾT LUẬN ===';
PRINT 'Nếu không có dữ liệu ở mục 4, biểu đồ sẽ trống.';
PRINT 'Giải pháp: Bỏ filter 7 ngày hoặc thêm dữ liệu test mới.';
