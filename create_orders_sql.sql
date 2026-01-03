-- =========================================================
-- SCRIPT: TAO 10 DON HANG CHO USER 20, BRANCH TAN PHU (ID=2)
-- =========================================================

USE perw;
GO

DECLARE @user_id BIGINT = 20;
DECLARE @branch_id BIGINT = 2;
DECLARE @num_orders INT = 10;

PRINT '========================================';
PRINT 'TAO 10 DON HANG';
PRINT 'User ID: 20';
PRINT 'Branch ID: 2 (Tan Phu)';
PRINT '========================================';
PRINT '';

-- Lay 3 san pham co gia cao nhat
DECLARE @pv1 BIGINT, @pv2 BIGINT, @pv3 BIGINT;
DECLARE @price1 DECIMAL(10,2), @price2 DECIMAL(10,2), @price3 DECIMAL(10,2);

SELECT TOP 3
    @pv1 = CASE WHEN ROW_NUMBER() OVER (ORDER BY price DESC) = 1 THEN id ELSE @pv1 END,
    @pv2 = CASE WHEN ROW_NUMBER() OVER (ORDER BY price DESC) = 2 THEN id ELSE @pv2 END,
    @pv3 = CASE WHEN ROW_NUMBER() OVER (ORDER BY price DESC) = 3 THEN id ELSE @pv3 END,
    @price1 = CASE WHEN ROW_NUMBER() OVER (ORDER BY price DESC) = 1 THEN price ELSE @price1 END,
    @price2 = CASE WHEN ROW_NUMBER() OVER (ORDER BY price DESC) = 2 THEN price ELSE @price2 END,
    @price3 = CASE WHEN ROW_NUMBER() OVER (ORDER BY price DESC) = 3 THEN price ELSE @price3 END
FROM product_variants
WHERE deleted_at IS NULL;

-- Neu khong tim thay, dung ID mac dinh
IF @pv1 IS NULL SET @pv1 = 1;
IF @pv2 IS NULL SET @pv2 = 2;
IF @pv3 IS NULL SET @pv3 = 3;
IF @price1 IS NULL SET @price1 = 100000;
IF @price2 IS NULL SET @price2 = 80000;
IF @price3 IS NULL SET @price3 = 60000;

PRINT 'San pham su dung:';
PRINT 'Variant 1: ID=' + CAST(@pv1 AS VARCHAR(10)) + ', Price=' + CAST(@price1 AS VARCHAR(20));
PRINT 'Variant 2: ID=' + CAST(@pv2 AS VARCHAR(10)) + ', Price=' + CAST(@price2 AS VARCHAR(20));
PRINT 'Variant 3: ID=' + CAST(@pv3 AS VARCHAR(10)) + ', Price=' + CAST(@price3 AS VARCHAR(20));
PRINT '';

DECLARE @i INT = 1;
DECLARE @order_id BIGINT;
DECLARE @order_code VARCHAR(50);
DECLARE @subtotal DECIMAL(15,2);
DECLARE @shipping_fee DECIMAL(15,2) = 30000;
DECLARE @total DECIMAL(15,2);

WHILE @i <= @num_orders
BEGIN
    -- Tao order code unique
    SET @order_code = 'ORD-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + '-' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 4);
    
    -- Tinh subtotal
    SET @subtotal = (@price1 * 2) + (@price2 * 1) + (@price3 * 3);
    SET @total = @subtotal + @shipping_fee;
    
    -- Tao order
    INSERT INTO purchase_orders (
        user_id, order_code, status,
        shipping_recipient_name, shipping_recipient_phone, shipping_address,
        sub_total, shipping_fee, discount_amount, total_amount,
        branch_id, created_at, updated_at
    )
    VALUES (
        @user_id, @order_code, 'pending',
        N'Nguyen Van Test - Don #' + CAST(@i AS NVARCHAR(3)),
        '0909123456',
        N'123 Duong ABC, Phuong Tan Phu, Quan 7, TP.HCM',
        @subtotal, @shipping_fee, 0, @total,
        @branch_id, GETDATE(), GETDATE()
    );
    
    SET @order_id = SCOPE_IDENTITY();
    
    -- Tao order details
    INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
    VALUES
        (@order_id, @pv1, 2, @price1, @price1 * 2, GETDATE()),
        (@order_id, @pv2, 1, @price2, @price2 * 1, GETDATE()),
        (@order_id, @pv3, 3, @price3, @price3 * 3, GETDATE());
    
    -- Tao payment
    INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
    VALUES (@order_id, 1, @total, 'pending', GETDATE());
    
    PRINT 'Don #' + CAST(@i AS VARCHAR(3)) + ' - Order ID: ' + CAST(@order_id AS VARCHAR(10)) + ' - Code: ' + @order_code + ' - Total: ' + CAST(@total AS VARCHAR(20)) + ' VND';
    
    SET @i = @i + 1;
    WAITFOR DELAY '00:00:00.050';
END

PRINT '';
PRINT '========================================';
PRINT 'HOAN THANH!';
PRINT 'Da tao: 10 don hang';
PRINT 'Tong tien moi don: ' + CAST(@total AS VARCHAR(20)) + ' VND';
PRINT '========================================';

-- Hien thi cac don vua tao
SELECT 
    id, order_code, status, shipping_recipient_name, total_amount, created_at
FROM purchase_orders
WHERE user_id = @user_id AND branch_id = @branch_id
ORDER BY created_at DESC;

GO
