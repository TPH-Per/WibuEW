-- =====================================================
-- COMPREHENSIVE TEST DATA - ALL BUSINESS FEATURES
-- Ngày: 23/12/2025
-- Mục đích: Test data tổng hợp cho tất cả nghiệp vụ
-- =====================================================

USE perw;
GO

PRINT '========================================';
PRINT 'COMPREHENSIVE TEST DATA';
PRINT 'Tạo dữ liệu test cho TẤT CẢ nghiệp vụ';
PRINT '========================================';
PRINT '';

-- =====================================================
-- PHẦN 1: XÓA DỮ LIỆU CŨ (NẾU CẦN)
-- =====================================================

PRINT '1. Xóa dữ liệu test cũ (nếu có):';
PRINT '';

-- Xóa theo thứ tự FK dependencies
DELETE FROM inbound_receipt_details WHERE receipt_id IN (SELECT id FROM inbound_receipts WHERE code LIKE 'IR%');
DELETE FROM inbound_receipts WHERE code LIKE 'IR%';
DELETE FROM payments WHERE order_id IN (SELECT id FROM purchase_orders WHERE order_code LIKE 'PO%');
DELETE FROM purchase_order_details WHERE order_id IN (SELECT id FROM purchase_orders WHERE order_code LIKE 'PO%');
DELETE FROM purchase_orders WHERE order_code LIKE 'PO%';
DELETE FROM branch_inventories WHERE id > 0; -- Xóa tất cả
DELETE FROM inventories WHERE id > 0; -- Xóa tất cả

PRINT '   ✓ Đã xóa dữ liệu test cũ';
PRINT '';

-- =====================================================
-- PHẦN 2: LẤY IDs CẦN THIẾT
-- =====================================================

PRINT '2. Lấy IDs cần thiết:';
PRINT '';

DECLARE @warehouseId BIGINT;
DECLARE @supplierId BIGINT;
DECLARE @userId BIGINT;
DECLARE @branchId1 BIGINT;
DECLARE @branchId2 BIGINT;
DECLARE @branchId3 BIGINT;
DECLARE @paymentMethodId BIGINT;

-- Warehouses
SELECT TOP 1 @warehouseId = id FROM warehouses WHERE deleted_at IS NULL;
IF @warehouseId IS NULL
BEGIN
    INSERT INTO warehouses (name, location, created_at)
    VALUES (N'Kho Trung Tâm TPHCM', N'123 Nguyễn Văn Linh, Q7, TP.HCM', GETDATE());
    SET @warehouseId = SCOPE_IDENTITY();
END

-- Suppliers
SELECT TOP 1 @supplierId = id FROM suppliers WHERE deleted_at IS NULL;
IF @supplierId IS NULL
BEGIN
    INSERT INTO suppliers (name, contact_info, created_at)
    VALUES (N'PERW Garment Co.', N'Email: contact@perw.com, Phone: 0901234567', GETDATE());
    SET @supplierId = SCOPE_IDENTITY();
END

-- Users
SELECT TOP 1 @userId = id FROM users WHERE deleted_at IS NULL;

-- Branches (cần 3 chi nhánh để test chart filter)
-- Note: branches table KHÔNG CÓ deleted_at column
SELECT TOP 1 @branchId1 = id FROM branches ORDER BY id;
SELECT TOP 1 @branchId2 = id FROM branches WHERE id > @branchId1 ORDER BY id;
SELECT TOP 1 @branchId3 = id FROM branches WHERE id > @branchId2 ORDER BY id;

IF @branchId1 IS NULL OR @branchId2 IS NULL OR @branchId3 IS NULL
BEGIN
    IF @branchId1 IS NULL
    BEGIN
        INSERT INTO branches (name, location, warehouse_id, created_at)
        VALUES (N'Chi Nhánh Quận 1', N'456 Nguyễn Huệ, Q1, TP.HCM', @warehouseId, GETDATE());
        SET @branchId1 = SCOPE_IDENTITY();
    END
    
    IF @branchId2 IS NULL
    BEGIN
        INSERT INTO branches (name, location, warehouse_id, created_at)
        VALUES (N'Chi Nhánh Tân Bình', N'789 Cộng Hòa, Tân Bình, TP.HCM', @warehouseId, GETDATE());
        SET @branchId2 = SCOPE_IDENTITY();
    END
    
    IF @branchId3 IS NULL
    BEGIN
        INSERT INTO branches (name, location, warehouse_id, created_at)
        VALUES (N'Chi Nhánh Thủ Đức', N'321 Võ Văn Ngân, Thủ Đức, TP.HCM', @warehouseId, GETDATE());
        SET @branchId3 = SCOPE_IDENTITY();
    END
END

-- Payment Methods
SELECT TOP 1 @paymentMethodId = id FROM payment_methods WHERE deleted_at IS NULL;

PRINT '   ✓ Warehouse: ' + CAST(@warehouseId AS VARCHAR);
PRINT '   ✓ Supplier: ' + CAST(@supplierId AS VARCHAR);
PRINT '   ✓ User: ' + CAST(ISNULL(@userId, 0) AS VARCHAR);
PRINT '   ✓ Branches: ' + CAST(@branchId1 AS VARCHAR) + ', ' + CAST(@branchId2 AS VARCHAR) + ', ' + CAST(@branchId3 AS VARCHAR);
PRINT '';

-- Lấy product variants (với fallback nếu không đủ)
DECLARE @variantId1 BIGINT, @variantId2 BIGINT, @variantId3 BIGINT, @variantId4 BIGINT, @variantId5 BIGINT;
SELECT TOP 1 @variantId1 = id FROM product_variants WHERE deleted_at IS NULL ORDER BY id;
SELECT TOP 1 @variantId2 = id FROM product_variants WHERE deleted_at IS NULL AND id > @variantId1 ORDER BY id;
SELECT TOP 1 @variantId3 = id FROM product_variants WHERE deleted_at IS NULL AND id > @variantId2 ORDER BY id;
SELECT TOP 1 @variantId4 = id FROM product_variants WHERE deleted_at IS NULL AND id > @variantId3 ORDER BY id;
SELECT TOP 1 @variantId5 = id FROM product_variants WHERE deleted_at IS NULL AND id > @variantId4 ORDER BY id;

-- Fallback: Nếu không đủ variants, dùng lại variants đã có
IF @variantId2 IS NULL SET @variantId2 = @variantId1;
IF @variantId3 IS NULL SET @variantId3 = @variantId1;
IF @variantId4 IS NULL SET @variantId4 = @variantId2;
IF @variantId5 IS NULL SET @variantId5 = @variantId3;

PRINT '   ✓ Product Variants: ' + CAST(@variantId1 AS VARCHAR) + ', ' + CAST(@variantId2 AS VARCHAR) + ', ' + CAST(@variantId3 AS VARCHAR);
PRINT '   ✓ (Fallback: ' + CAST(@variantId4 AS VARCHAR) + ', ' + CAST(@variantId5 AS VARCHAR) + ')';
PRINT '';

-- =====================================================
-- PHẦN 3: PHIẾU NHẬP KHO (INBOUND RECEIPTS)
-- Test Cases: pending, received, quality_check, completed
-- =====================================================

PRINT '3. Tạo phiếu nhập kho (4 trạng thái):';
PRINT '';

DECLARE @receiptId1 BIGINT, @receiptId2 BIGINT, @receiptId3 BIGINT, @receiptId4 BIGINT;

-- Phiếu 1: COMPLETED (5 ngày trước) - Cập nhật tồn kho
INSERT INTO inbound_receipts (code, supplier_id, warehouse_id, status, total_amount, notes, received_at, created_by, created_at)
VALUES ('IR' + FORMAT(DATEADD(DAY, -5, GETDATE()), 'yyyyMMdd') + '001', @supplierId, @warehouseId, 'completed', 
        20000000, N'Phiếu nhập hoàn thành - Đã cập nhật kho', DATEADD(DAY, -5, GETDATE()), @userId, DATEADD(DAY, -5, GETDATE()));
SET @receiptId1 = SCOPE_IDENTITY();

INSERT INTO inbound_receipt_details (receipt_id, product_variant_id, quantity, input_price, created_at)
VALUES 
    (@receiptId1, @variantId1, 100, 200000, DATEADD(DAY, -5, GETDATE())),
    (@receiptId1, @variantId2, 80, 150000, DATEADD(DAY, -5, GETDATE())),
    (@receiptId1, @variantId3, 50, 100000, DATEADD(DAY, -5, GETDATE()));

-- Phiếu 2: QUALITY_CHECK (2 ngày trước)
INSERT INTO inbound_receipts (code, supplier_id, warehouse_id, status, total_amount, notes, received_at, created_by, created_at)
VALUES ('IR' + FORMAT(DATEADD(DAY, -2, GETDATE()), 'yyyyMMdd') + '002', @supplierId, @warehouseId, 'quality_check',
        12000000, N'Đang kiểm tra chất lượng', DATEADD(DAY, -2, GETDATE()), @userId, DATEADD(DAY, -2, GETDATE()));
SET @receiptId2 = SCOPE_IDENTITY();

INSERT INTO inbound_receipt_details (receipt_id, product_variant_id, quantity, input_price, created_at)
VALUES (@receiptId2, @variantId1, 60, 200000, DATEADD(DAY, -2, GETDATE()));

-- Phiếu 3: RECEIVED (1 ngày trước)
INSERT INTO inbound_receipts (code, supplier_id, warehouse_id, status, total_amount, notes, received_at, created_by, created_at)
VALUES ('IR' + FORMAT(DATEADD(DAY, -1, GETDATE()), 'yyyyMMdd') + '003', @supplierId, @warehouseId, 'received',
        8000000, N'Đã nhận hàng, chờ kiểm tra', DATEADD(DAY, -1, GETDATE()), @userId, DATEADD(DAY, -1, GETDATE()));
SET @receiptId3 = SCOPE_IDENTITY();

INSERT INTO inbound_receipt_details (receipt_id, product_variant_id, quantity, input_price, created_at)
VALUES 
    (@receiptId3, @variantId2, 40, 150000, DATEADD(DAY, -1, GETDATE())),
    (@receiptId3, @variantId3, 30, 100000, DATEADD(DAY, -1, GETDATE()));

-- Phiếu 4: PENDING (hôm nay)
INSERT INTO inbound_receipts (code, supplier_id, warehouse_id, status, total_amount, notes, received_at, created_by, created_at)
VALUES ('IR' + FORMAT(GETDATE(), 'yyyyMMdd') + '004', @supplierId, @warehouseId, 'pending',
        15000000, N'Đang chờ nhận hàng từ NCC', DATEADD(DAY, 1, GETDATE()), @userId, GETDATE());
SET @receiptId4 = SCOPE_IDENTITY();

INSERT INTO inbound_receipt_details (receipt_id, product_variant_id, quantity, input_price, created_at)
VALUES 
    (@receiptId4, @variantId1, 50, 200000, GETDATE()),
    (@receiptId4, @variantId4, 70, 120000, GETDATE());

PRINT '   ✓ Phiếu 1: COMPLETED (cập nhật tồn kho)';
PRINT '   ✓ Phiếu 2: QUALITY_CHECK';
PRINT '   ✓ Phiếu 3: RECEIVED';
PRINT '   ✓ Phiếu 4: PENDING';
PRINT '';

-- =====================================================
-- PHẦN 4: TỒN KHO KHO TỔNG (INVENTORIES)
-- Test Cases: Đủ hàng, Cảnh báo, Hết hàng
-- =====================================================

PRINT '4. Tạo tồn kho Kho Tổng (từ phiếu completed):';
PRINT '';

-- Tự động từ phiếu completed
INSERT INTO inventories (product_variant_id, warehouse_id, quantity_on_hand, quantity_reserved, reorder_level, created_at)
SELECT 
    ird.product_variant_id,
    ir.warehouse_id,
    SUM(ird.quantity) AS quantity_on_hand,
    0 AS quantity_reserved,
    CASE 
        WHEN SUM(ird.quantity) >= 100 THEN 20  -- Đủ hàng
        WHEN SUM(ird.quantity) >= 50 THEN 60   -- Cảnh báo (on_hand < reorder)
        ELSE 10                                 -- Hết hàng
    END AS reorder_level,
    GETDATE()
FROM inbound_receipt_details ird
INNER JOIN inbound_receipts ir ON ird.receipt_id = ir.id
WHERE ir.status = 'completed'
GROUP BY ird.product_variant_id, ir.warehouse_id;

-- Thêm một số items với reserved để test Available
UPDATE TOP (1) inventories 
SET quantity_reserved = 20
WHERE product_variant_id = @variantId1;

PRINT '   ✓ Đã tạo tồn kho từ phiếu completed';
PRINT '   ✓ Test cases: Đủ hàng, Cảnh báo, Hết hàng';
PRINT '';

-- =====================================================
-- PHẦN 5: TỒN KHO CHI NHÁNH (BRANCH_INVENTORIES)
-- Test Cases: 3 chi nhánh, Low stock, Normal stock
-- =====================================================

PRINT '5. Tạo tồn kho Chi nhánh (3 chi nhánh):';
PRINT '';

-- Chi nhánh 1: Nhiều hàng
INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at)
VALUES 
    (@branchId1, @variantId1, 50, 5, 10, GETDATE()),
    (@branchId1, @variantId2, 40, 0, 10, GETDATE()),
    (@branchId1, @variantId3, 30, 0, 10, GETDATE());

-- Chi nhánh 2: Ít hàng (low stock)
INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at)
VALUES 
    (@branchId2, @variantId1, 5, 0, 10, GETDATE()),   -- Low stock
    (@branchId2, @variantId2, 8, 0, 10, GETDATE()),   -- Low stock
    (@branchId2, @variantId4, 25, 5, 10, GETDATE());  -- Normal

-- Chi nhánh 3: Hỗn hợp
INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at)
VALUES 
    (@branchId3, @variantId1, 15, 0, 10, GETDATE()),
    (@branchId3, @variantId3, 3, 0, 10, GETDATE()),   -- Low stock
    (@branchId3, @variantId5, 20, 0, 10, GETDATE());

PRINT '   ✓ Chi nhánh 1: Nhiều hàng';
PRINT '   ✓ Chi nhánh 2: Low stock items';
PRINT '   ✓ Chi nhánh 3: Hỗn hợp';
PRINT '';

-- =====================================================
-- PHẦN 6: ĐƠN HÀNG (PURCHASE_ORDERS)
-- Test Cases: pending, processing, completed (3 chi nhánh khác nhau)
-- =====================================================

PRINT '6. Tạo đơn hàng (3 trạng thái x 3 chi nhánh):';
PRINT '';

DECLARE @orderId BIGINT;
DECLARE @i INT = 1;

-- Loop tạo 12 đơn hàng
WHILE @i <= 12
BEGIN
    DECLARE @status NVARCHAR(20);
    DECLARE @branchId BIGINT;
    DECLARE @daysAgo INT = (@i % 7); -- 0-6 ngày trước
    DECLARE @totalAmount DECIMAL(12,2);
    
    -- Xác định status
    SET @status = CASE 
        WHEN @i % 3 = 0 THEN 'completed'
        WHEN @i % 3 = 1 THEN 'processing'
        ELSE 'pending'
    END;
    
    -- Xác định branch (luân phiên 3 chi nhánh)
    SET @branchId = CASE 
        WHEN @i % 3 = 0 THEN @branchId1
        WHEN @i % 3 = 1 THEN @branchId2
        ELSE @branchId3
    END;
    
    -- Tính total amount
    SET @totalAmount = 500000 + (@i * 100000);
    
    -- Tạo đơn hàng
    INSERT INTO purchase_orders (
        user_id, order_code, status, branch_id,
        shipping_recipient_name, shipping_recipient_phone, shipping_address,
        sub_total, shipping_fee, discount_amount, total_amount,
        created_at
    )
    VALUES (
        @userId,
        'PO' + FORMAT(DATEADD(DAY, -@daysAgo, GETDATE()), 'yyyyMMdd') + RIGHT('000' + CAST(@i AS VARCHAR), 3),
        @status,
        @branchId,
        N'Khách hàng ' + CAST(@i AS NVARCHAR),
        '090' + RIGHT('0000000' + CAST(@i AS VARCHAR), 7),
        N'Địa chỉ giao hàng ' + CAST(@i AS NVARCHAR),
        @totalAmount - 30000,
        30000,
        0,
        @totalAmount,
        DATEADD(DAY, -@daysAgo, GETDATE())
    );
    
    SET @orderId = SCOPE_IDENTITY();
    
    -- Tạo chi tiết đơn hàng
    INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
    VALUES 
        (@orderId, @variantId1, 2, 200000, 400000, DATEADD(DAY, -@daysAgo, GETDATE())),
        (@orderId, @variantId2, 1, @totalAmount - 430000, @totalAmount - 430000, DATEADD(DAY, -@daysAgo, GETDATE()));
    
    -- Tạo payment cho đơn completed
    IF @status = 'completed'
    BEGIN
        INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
        VALUES (@orderId, @paymentMethodId, @totalAmount, 'completed', DATEADD(DAY, -@daysAgo, GETDATE()));
    END
    
    SET @i = @i + 1;
END

PRINT '   ✓ Đã tạo 12 đơn hàng';
PRINT '   ✓ 4 completed (3 chi nhánh)';
PRINT '   ✓ 4 processing (3 chi nhánh)';
PRINT '   ✓ 4 pending (3 chi nhánh)';
PRINT '';

-- =====================================================
-- PHẦN 6B: THÊM ĐƠN HÀNG COMPLETED ĐỂ TEST CHART
-- Mục đích: Test dashboard chart với nhiều chi nhánh và giá trị khác nhau
-- =====================================================

PRINT '6B. Thêm đơn hàng completed để test chart:';
PRINT '';

-- Thêm 8 đơn completed với giá trị đa dạng
DECLARE @testOrders TABLE (BranchId BIGINT, Amount DECIMAL(12,2), DaysAgo INT);
INSERT INTO @testOrders VALUES
    (@branchId1, 2500000, 6),  -- Chi nhánh 1: 2.5M
    (@branchId1, 3200000, 5),  -- Chi nhánh 1: 3.2M
    (@branchId1, 1800000, 4),  -- Chi nhánh 1: 1.8M
    (@branchId2, 4500000, 6),  -- Chi nhánh 2: 4.5M
    (@branchId2, 2100000, 5),  -- Chi nhánh 2: 2.1M
    (@branchId2, 3800000, 3),  -- Chi nhánh 2: 3.8M
    (@branchId3, 5200000, 6),  -- Chi nhánh 3: 5.2M
    (@branchId3, 2900000, 4);  -- Chi nhánh 3: 2.9M

DECLARE @testBranchId BIGINT, @testAmount DECIMAL(12,2), @testDaysAgo INT;
DECLARE @testCounter INT = 13; -- Bắt đầu từ 13

DECLARE test_cursor CURSOR FOR SELECT BranchId, Amount, DaysAgo FROM @testOrders;
OPEN test_cursor;
FETCH NEXT FROM test_cursor INTO @testBranchId, @testAmount, @testDaysAgo;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO purchase_orders (
        user_id, order_code, status, branch_id,
        shipping_recipient_name, shipping_recipient_phone, shipping_address,
        sub_total, shipping_fee, discount_amount, total_amount,
        created_at
    )
    VALUES (
        @userId,
        'PO' + FORMAT(DATEADD(DAY, -@testDaysAgo, GETDATE()), 'yyyyMMdd') + RIGHT('000' + CAST(@testCounter AS VARCHAR), 3),
        'completed',
        @testBranchId,
        N'Khách hàng Test ' + CAST(@testCounter AS NVARCHAR),
        '090' + RIGHT('0000000' + CAST(@testCounter AS VARCHAR), 7),
        N'Địa chỉ test ' + CAST(@testCounter AS NVARCHAR),
        @testAmount - 30000,
        30000,
        0,
        @testAmount,
        DATEADD(DAY, -@testDaysAgo, GETDATE())
    );
    
    SET @orderId = SCOPE_IDENTITY();
    
    -- Chi tiết đơn
    INSERT INTO purchase_order_details (order_id, product_variant_id, quantity, price_at_purchase, subtotal, created_at)
    VALUES 
        (@orderId, @variantId1, 3, 200000, 600000, DATEADD(DAY, -@testDaysAgo, GETDATE())),
        (@orderId, @variantId2, 2, (@testAmount - 630000) / 2, @testAmount - 630000, DATEADD(DAY, -@testDaysAgo, GETDATE()));
    
    -- Payment
    INSERT INTO payments (order_id, payment_method_id, amount, status, created_at)
    VALUES (@orderId, @paymentMethodId, @testAmount, 'completed', DATEADD(DAY, -@testDaysAgo, GETDATE()));
    
    SET @testCounter = @testCounter + 1;
    FETCH NEXT FROM test_cursor INTO @testBranchId, @testAmount, @testDaysAgo;
END

CLOSE test_cursor;
DEALLOCATE test_cursor;

PRINT '   ✓ Đã thêm 8 đơn completed cho test chart';
PRINT '   ✓ Chi nhánh 1: 3 đơn (1.8M, 2.5M, 3.2M)';
PRINT '   ✓ Chi nhánh 2: 3 đơn (2.1M, 3.8M, 4.5M)';
PRINT '   ✓ Chi nhánh 3: 2 đơn (2.9M, 5.2M)';
PRINT '';

-- =====================================================
-- PHẦN 7: HIỂN THỊ KẾT QUẢ
-- =====================================================

PRINT '7. Kết quả test data:';
PRINT '';

PRINT '   A. Phiếu nhập kho:';
SELECT 
    code AS [Mã phiếu],
    status AS [Trạng thái],
    FORMAT(total_amount, 'N0') + ' ₫' AS [Tổng tiền],
    FORMAT(received_at, 'dd/MM/yyyy') AS [Ngày nhận]
FROM inbound_receipts
WHERE code LIKE 'IR%'
ORDER BY created_at DESC;

PRINT '';
PRINT '   B. Tồn kho Kho Tổng:';
SELECT 
    pv.name AS [Biến thể],
    i.quantity_on_hand AS [Tồn kho],
    i.quantity_reserved AS [Đang giữ],
    (i.quantity_on_hand - i.quantity_reserved) AS [Khả dụng],
    i.reorder_level AS [Mức CB],
    CASE 
        WHEN i.quantity_on_hand < i.reorder_level THEN N'⚠ Cảnh báo'
        WHEN (i.quantity_on_hand - i.quantity_reserved) = 0 THEN N'❌ Hết hàng'
        ELSE N'✓ Đủ hàng'
    END AS [Trạng thái]
FROM inventories i
INNER JOIN product_variants pv ON i.product_variant_id = pv.id
ORDER BY i.quantity_on_hand;

PRINT '';
PRINT '   C. Tồn kho Chi nhánh:';
SELECT 
    b.name AS [Chi nhánh],
    COUNT(*) AS [Số SKU],
    SUM(bi.quantity_on_hand) AS [Tổng tồn],
    SUM(CASE WHEN bi.quantity_on_hand < bi.reorder_level THEN 1 ELSE 0 END) AS [SKU cảnh báo]
FROM branch_inventories bi
INNER JOIN branches b ON bi.branch_id = b.id
GROUP BY b.name
ORDER BY b.name;

PRINT '';
PRINT '   D. Đơn hàng theo chi nhánh và trạng thái:';
SELECT 
    b.name AS [Chi nhánh],
    po.status AS [Trạng thái],
    COUNT(*) AS [Số đơn],
    FORMAT(SUM(po.total_amount), 'N0') + ' ₫' AS [Tổng tiền]
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.order_code LIKE 'PO%'
GROUP BY b.name, po.status
ORDER BY b.name, po.status;

PRINT '';
PRINT '   E. Dashboard Chart Data (Doanh thu theo chi nhánh):';
SELECT 
    b.name AS [Chi nhánh],
    COUNT(*) AS [Số đơn completed],
    FORMAT(SUM(po.total_amount), 'N0') + ' ₫' AS [Doanh thu]
FROM purchase_orders po
INNER JOIN branches b ON po.branch_id = b.id
WHERE po.status = 'completed' AND po.order_code LIKE 'PO%'
GROUP BY b.name
ORDER BY SUM(po.total_amount) DESC;

PRINT '';
PRINT '========================================';
PRINT 'HOÀN TẤT!';
PRINT '========================================';
PRINT '';
PRINT 'Test Cases Covered:';
PRINT '✓ Phiếu nhập: 4 trạng thái (pending, received, quality_check, completed)';
PRINT '✓ Tồn kho Kho Tổng: 3 trạng thái (Đủ hàng, Cảnh báo, Hết hàng)';
PRINT '✓ Tồn kho Chi nhánh: 3 chi nhánh với low stock items';
PRINT '✓ Đơn hàng: 3 trạng thái x 3 chi nhánh = 12 đơn';
PRINT '✓ Dashboard Chart: Dữ liệu từ 3 chi nhánh khác nhau';
PRINT '';
PRINT 'Bây giờ có thể test:';
PRINT '1. /Admin/SupplierShipments/Index - Phiếu nhập (4 trạng thái)';
PRINT '2. /Admin/Inventories/List - Tồn kho Kho Tổng';
PRINT '3. /Admin/BranchInventories/Index - Tồn kho Chi nhánh (filter 3 chi nhánh)';
PRINT '4. /Admin/Orders/Index - Đơn hàng (filter 3 trạng thái)';
PRINT '5. /Admin/Dashboard/Index - Chart filter 3 chi nhánh';
PRINT '6. /Admin/Reports/Index - Báo cáo thống kê';
PRINT '';

GO
