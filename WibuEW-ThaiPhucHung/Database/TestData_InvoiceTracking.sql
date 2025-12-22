-- Script kiểm tra và thêm dữ liệu test cho chức năng theo dõi hóa đơn
USE perw;
GO

-- 1. KIỂM TRA DỮ LIỆU HIỆN TẠI
PRINT '========================================';
PRINT 'KIỂM TRA DỮ LIỆU HIỆN TẠI';
PRINT '========================================';

-- Kiểm tra số lượng đơn hàng
SELECT 
    'Tổng số đơn hàng' AS [Loại],
    COUNT(*) AS [Số lượng]
FROM purchase_orders
WHERE deleted_at IS NULL;

-- Kiểm tra đơn hàng theo trạng thái
SELECT 
    ISNULL(status, 'NULL') AS [Trạng thái],
    COUNT(*) AS [Số lượng]
FROM purchase_orders
WHERE deleted_at IS NULL
GROUP BY status;

-- Kiểm tra chi nhánh
SELECT 
    'Chi nhánh' AS [Loại],
    COUNT(*) AS [Số lượng]
FROM branches

-- Kiểm tra sản phẩm
SELECT 
    'Sản phẩm' AS [Loại],
    COUNT(*) AS [Số lượng]
FROM products
WHERE deleted_at IS NULL;

-- Kiểm tra biến thể sản phẩm
SELECT 
    'Biến thể sản phẩm' AS [Loại],
    COUNT(*) AS [Số lượng]
FROM product_variants
WHERE deleted_at IS NULL;

-- Kiểm tra phương thức thanh toán
SELECT 
    'Phương thức thanh toán' AS [Loại],
    COUNT(*) AS [Số lượng]
FROM payment_methods;

PRINT '';
PRINT '========================================';
PRINT 'THÊM DỮ LIỆU TEST (NẾU CHƯA CÓ)';
PRINT '========================================';

-- 2. THÊM DỮ LIỆU TEST NẾU CHƯA CÓ

-- Kiểm tra xem đã có dữ liệu chưa
DECLARE @orderCount INT;
SELECT @orderCount = COUNT(*) FROM purchase_orders WHERE deleted_at IS NULL;

IF @orderCount = 0
BEGIN
    PRINT 'Đang thêm dữ liệu test...';
    
    -- Lấy ID của chi nhánh đầu tiên (hoặc tạo mới nếu chưa có)
    DECLARE @branchId BIGINT;
    SELECT TOP 1 @branchId = id FROM branches;
    
    IF @branchId IS NULL
    BEGIN
        PRINT 'Tạo chi nhánh test...';
        INSERT INTO branches (name, location, created_at)
        VALUES (N'Chi nhánh Test', N'Hà Nội', GETDATE());
        SET @branchId = SCOPE_IDENTITY();
    END
    
    -- Lấy ID của user đầu tiên (hoặc NULL nếu không có)
    DECLARE @userId BIGINT;
    SELECT TOP 1 @userId = id FROM users WHERE deleted_at IS NULL;
    
    -- Lấy ID của product variant đầu tiên
    DECLARE @variantId BIGINT;
    SELECT TOP 1 @variantId = id FROM product_variants WHERE deleted_at IS NULL;
    
    IF @variantId IS NULL
    BEGIN
        PRINT 'Cần có ít nhất 1 sản phẩm trong database!';
        PRINT 'Vui lòng chạy script 05_InsertSampleData.sql trước.';
    END
    ELSE
    BEGIN
        -- Lấy ID của payment method
        DECLARE @paymentMethodId BIGINT;
        SELECT TOP 1 @paymentMethodId = id FROM payment_methods;
        
        IF @paymentMethodId IS NULL
        BEGIN
            PRINT 'Tạo phương thức thanh toán...';
            INSERT INTO payment_methods (name, code, is_active, created_at)
            VALUES 
                (N'Tiền mặt', N'Thanh toán bằng tiền mặt', 1, GETDATE()),
                (N'Chuyển khoản', N'Thanh toán qua chuyển khoản ngân hàng', 1, GETDATE()),
                (N'VNPay', N'Thanh toán qua VNPay', 1, GETDATE());
            
            SELECT TOP 1 @paymentMethodId = id FROM payment_methods;
        END
        
        -- Thêm đơn hàng test
        PRINT 'Thêm đơn hàng test...';
        
        DECLARE @orderId1 BIGINT, @orderId2 BIGINT, @orderId3 BIGINT;
        DECLARE @price DECIMAL(18,2);
        SELECT @price = price FROM product_variants WHERE id = @variantId;
        
        -- Đơn hàng 1: Pending
        INSERT INTO purchase_orders (
            user_id, order_code, status, shipping_recipient_name, 
            shipping_recipient_phone, shipping_address,
            sub_total, shipping_fee, discount_amount, total_amount,
            branch_id, created_at
        )
        VALUES (
            @userId, 'PO' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + '001', 'pending',
            N'Nguyễn Văn A', '0901234567', N'123 Đường ABC, Quận 1, TP.HCM',
            @price * 2, 30000, 0, @price * 2 + 30000,
            @branchId, DATEADD(HOUR, -5, GETDATE())
        );
        SET @orderId1 = SCOPE_IDENTITY();
        
        -- Chi tiết đơn hàng 1
        INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
        VALUES (@orderId1, @variantId, 2, @price, @price * 2, DATEADD(HOUR, -5, GETDATE()));
        
        -- Thanh toán đơn hàng 1
        INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
        VALUES (@orderId1, @paymentMethodId, @price * 2 + 30000, 'pending', DATEADD(HOUR, -5, GETDATE()));
        
        -- Đơn hàng 2: Processing
        INSERT INTO purchase_orders (
            user_id, order_code, status, shipping_recipient_name, 
            shipping_recipient_phone, shipping_address,
            sub_total, shipping_fee, discount_amount, total_amount,
            branch_id, created_at
        )
        VALUES (
            @userId, 'PO' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + '002', 'processing',
            N'Trần Thị B', '0912345678', N'456 Đường XYZ, Quận 2, TP.HCM',
            @price * 3, 30000, 50000, @price * 3 + 30000 - 50000,
            @branchId, DATEADD(HOUR, -3, GETDATE())
        );
        SET @orderId2 = SCOPE_IDENTITY();
        
        -- Chi tiết đơn hàng 2
        INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
        VALUES (@orderId2, @variantId, 3, @price, @price * 3, DATEADD(HOUR, -3, GETDATE()));
        
        -- Thanh toán đơn hàng 2
        INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
        VALUES (@orderId2, @paymentMethodId, @price * 3 + 30000 - 50000, 'paid', DATEADD(HOUR, -3, GETDATE()));
        
        -- Đơn hàng 3: Completed (7 ngày trước)
        INSERT INTO purchase_orders (
            user_id, order_code, status, shipping_recipient_name, 
            shipping_recipient_phone, shipping_address,
            sub_total, shipping_fee, discount_amount, total_amount,
            branch_id, created_at
        )
        VALUES (
            @userId, 'PO' + FORMAT(DATEADD(DAY, -7, GETDATE()), 'yyyyMMddHHmmss') + '003', 'completed',
            N'Lê Văn C', '0923456789', N'789 Đường DEF, Quận 3, TP.HCM',
            @price * 5, 30000, 0, @price * 5 + 30000,
            @branchId, DATEADD(DAY, -7, GETDATE())
        );
        SET @orderId3 = SCOPE_IDENTITY();
        
        -- Chi tiết đơn hàng 3
        INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
        VALUES (@orderId3, @variantId, 5, @price, @price * 5, DATEADD(DAY, -7, GETDATE()));
        
        -- Thanh toán đơn hàng 3
        INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
        VALUES (@orderId3, @paymentMethodId, @price * 5 + 30000, 'completed', DATEADD(DAY, -7, GETDATE()));
        
        -- Thêm thêm vài đơn hàng completed trong 7 ngày qua
        DECLARE @i INT = 1;
        WHILE @i <= 5
        BEGIN
            DECLARE @orderIdTemp BIGINT;
            DECLARE @daysAgo INT = @i;
            
            INSERT INTO purchase_orders (
                user_id, order_code, status, shipping_recipient_name, 
                shipping_recipient_phone, shipping_address,
                sub_total, shipping_fee, discount_amount, total_amount,
                branch_id, created_at
            )
            VALUES (
                @userId, 
                'PO' + FORMAT(DATEADD(DAY, -@daysAgo, GETDATE()), 'yyyyMMddHHmmss') + RIGHT('00' + CAST(@i AS VARCHAR), 2),
                'completed',
                N'Khách hàng ' + CAST(@i AS NVARCHAR),
                '090000000' + CAST(@i AS VARCHAR),
                N'Địa chỉ ' + CAST(@i AS NVARCHAR),
                @price * (@i + 1), 30000, 0, @price * (@i + 1) + 30000,
                @branchId, DATEADD(DAY, -@daysAgo, GETDATE())
            );
            SET @orderIdTemp = SCOPE_IDENTITY();
            
            INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
            VALUES (@orderIdTemp, @variantId, @i + 1, @price, @price * (@i + 1), DATEADD(DAY, -@daysAgo, GETDATE()));
            
            INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
            VALUES (@orderIdTemp, @paymentMethodId, @price * (@i + 1) + 30000, 'completed', DATEADD(DAY, -@daysAgo, GETDATE()));
            
            SET @i = @i + 1;
        END
        
        PRINT 'Đã thêm dữ liệu test thành công!';
    END
END
ELSE
BEGIN
    PRINT 'Database đã có ' + CAST(@orderCount AS VARCHAR) + ' đơn hàng.';
END

PRINT '';
PRINT '========================================';
PRINT 'KẾT QUẢ SAU KHI THÊM DỮ LIỆU';
PRINT '========================================';

-- 3. HIỂN thị KẾT QUẢ
SELECT 
    po.id,
    po.order_code AS [Mã đơn],
    b.name AS [Chi nhánh],
    ISNULL(u.full_name, po.shipping_recipient_name) AS [Khách hàng],
    po.status AS [Trạng thái],
    po.total_amount AS [Tổng tiền],
    po.created_at AS [Ngày tạo]
FROM purchase_orders po
LEFT JOIN branches b ON po.branch_id = b.id
LEFT JOIN users u ON po.user_id = u.id
WHERE po.deleted_at IS NULL
ORDER BY po.created_at DESC;

PRINT '';
PRINT 'Thống kê:';
SELECT 
    COUNT(*) AS [Tổng đơn hàng],
    SUM(CASE WHEN status = 'pending' THEN 1 ELSE 0 END) AS [Pending],
    SUM(CASE WHEN status = 'processing' THEN 1 ELSE 0 END) AS [Processing],
    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) AS [Completed],
    SUM(CASE WHEN status = 'completed' THEN total_amount ELSE 0 END) AS [Doanh thu (Completed)]
FROM purchase_orders
WHERE deleted_at IS NULL;

PRINT '';
PRINT '========================================';
PRINT 'HOÀN TẤT!';
PRINT '========================================';
PRINT 'Bây giờ bạn có thể:';
PRINT '1. Refresh trang web (F5)';
PRINT '2. Truy cập /Admin/Orders/Index để xem danh sách đơn hàng';
PRINT '3. Truy cập /Admin/Reports/Index để xem báo cáo thống kê';
GO
