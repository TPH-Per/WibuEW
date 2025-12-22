-- Script đơn giản để kiểm tra và fix Dashboard Chart
USE perw;
GO

PRINT '=== KIỂM TRA DASHBOARD CHART ===';
PRINT '';

-- 1. Kiểm tra đơn hàng completed
PRINT '1. Số đơn hàng completed:';
SELECT COUNT(*) AS Total FROM purchase_orders WHERE status = 'completed';
PRINT '';

-- 2. Doanh thu theo chi nhánh (dữ liệu cho chart)
PRINT '2. Doanh thu theo chi nhánh:';
SELECT 
    b.name AS BranchName,
    COUNT(po.id) AS OrderCount,
    SUM(po.total_amount) AS Revenue
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.status = 'completed'
GROUP BY b.name
ORDER BY SUM(po.total_amount) DESC;
PRINT '';

-- 3. Nếu không có dữ liệu, update đơn hàng hiện có
DECLARE @completedCount INT;
SELECT @completedCount = COUNT(*) FROM purchase_orders WHERE status = 'completed';

IF @completedCount = 0
BEGIN
    PRINT '3. Không có đơn completed. Đang update...';
    
    -- Update 5 đơn đầu tiên thành completed
    UPDATE TOP (5) purchase_orders
    SET status = 'completed',
        created_at = DATEADD(DAY, -ABS(CHECKSUM(NEWID()) % 7), GETDATE())
    WHERE status != 'completed';
    
    PRINT '   ✓ Đã update 5 đơn thành completed';
    
    -- Hiển thị lại kết quả
    PRINT '';
    PRINT '4. Doanh thu sau khi update:';
    SELECT 
        b.name AS BranchName,
        COUNT(po.id) AS OrderCount,
        SUM(po.total_amount) AS Revenue
    FROM purchase_orders po
    INNER JOIN branches b ON po.branch_id = b.id
    WHERE po.status = 'completed'
    GROUP BY b.name
    ORDER BY SUM(po.total_amount) DESC;
END
ELSE
BEGIN
    PRINT '3. Đã có ' + CAST(@completedCount AS VARCHAR) + ' đơn completed - OK!';
END

PRINT '';
PRINT '=== KẾT QUẢ ===';
PRINT 'Nếu có dữ liệu ở mục 2 hoặc 4, biểu đồ sẽ hiển thị.';
PRINT 'Rebuild solution và refresh trang Dashboard (F5).';
