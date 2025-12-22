-- ================================================
-- Script: Test Data for Invoice Tracking Feature
-- Database: perw
-- Description: Adds sample orders for testing Dashboard and Reports
-- ================================================

USE perw;
GO

-- ================================================
-- STEP 1: Check existing data
-- ================================================
PRINT '=== CHECKING EXISTING DATA ===';

SELECT 
    COUNT(*) AS TotalOrders,
    SUM(CASE WHEN status = 'pending' THEN 1 ELSE 0 END) AS PendingOrders,
    SUM(CASE WHEN status = 'processing' THEN 1 ELSE 0 END) AS ProcessingOrders,
    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) AS CompletedOrders
FROM purchase_orders
WHERE deleted_at IS NULL;

-- ================================================
-- STEP 2: Add sample orders if needed
-- ================================================
PRINT '=== ADDING SAMPLE ORDERS ===';

DECLARE @BranchId BIGINT;
DECLARE @UserId BIGINT;
DECLARE @UserAddressId BIGINT;
DECLARE @PaymentMethodId BIGINT;
DECLARE @ProductVariantId1 BIGINT;
DECLARE @ProductVariantId2 BIGINT;

-- Get first available branch
SELECT TOP 1 @BranchId = id FROM branches WHERE deleted_at IS NULL;

-- Get first available user
SELECT TOP 1 @UserId = id FROM users WHERE deleted_at IS NULL AND role = 'customer';

-- Get first available address for this user
SELECT TOP 1 @UserAddressId = id FROM user_addresses WHERE user_id = @UserId AND deleted_at IS NULL;

-- Get COD payment method
SELECT TOP 1 @PaymentMethodId = id FROM payment_methods WHERE name LIKE '%COD%' OR name LIKE '%ti%n m%t%';

-- Get 2 product variants
SELECT TOP 1 @ProductVariantId1 = id FROM product_variants WHERE deleted_at IS NULL ORDER BY id;
SELECT TOP 1 @ProductVariantId2 = id FROM product_variants WHERE deleted_at IS NULL AND id <> @ProductVariantId1 ORDER BY id;

-- Check if we have minimum required data
IF @BranchId IS NULL OR @UserId IS NULL OR @PaymentMethodId IS NULL OR @ProductVariantId1 IS NULL
BEGIN
    PRINT 'ERROR: Missing required data (branch, user, payment method, or product variants)';
    PRINT 'Please ensure database has at least 1 branch, 1 customer user, 1 payment method, and 1 product variant';
    RETURN;
END

PRINT 'Using Branch ID: ' + CAST(@BranchId AS VARCHAR);
PRINT 'Using User ID: ' + CAST(@UserId AS VARCHAR);
PRINT 'Using Payment Method ID: ' + CAST(@PaymentMethodId AS VARCHAR);

-- ================================================
-- STEP 3: Insert sample orders
-- ================================================

-- Order 1: Completed (7 days ago)
DECLARE @OrderId1 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id, 
    status, total_amount, 
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-001', @BranchId, @UserId, @UserAddressId,
    'completed', 500000,
    'Nguyen Van A', '0901234567', '123 Test Street', 'Quan 1', 'Phuong 1', 'Ho Chi Minh',
    DATEADD(DAY, -7, GETDATE()), DATEADD(DAY, -7, GETDATE())
);
SET @OrderId1 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId1, @ProductVariantId1, 2, 250000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId1, @PaymentMethodId, 500000, 'completed', DATEADD(DAY, -7, GETDATE()));

-- Order 2: Completed (6 days ago)
DECLARE @OrderId2 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-002', @BranchId, @UserId, @UserAddressId,
    'completed', 750000,
    'Tran Thi B', '0907654321', '456 Test Avenue', 'Quan 3', 'Phuong 5', 'Ho Chi Minh',
    DATEADD(DAY, -6, GETDATE()), DATEADD(DAY, -6, GETDATE())
);
SET @OrderId2 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId2, @ProductVariantId1, 3, 250000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId2, @PaymentMethodId, 750000, 'completed', DATEADD(DAY, -6, GETDATE()));

-- Order 3: Completed (5 days ago)
DECLARE @OrderId3 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-003', @BranchId, @UserId, @UserAddressId,
    'completed', 1200000,
    'Le Van C', '0903456789', '789 Test Road', 'Quan 5', 'Phuong 3', 'Ho Chi Minh',
    DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, -5, GETDATE())
);
SET @OrderId3 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId3, @ProductVariantId2, 4, 300000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId3, @PaymentMethodId, 1200000, 'completed', DATEADD(DAY, -5, GETDATE()));

-- Order 4: Completed (4 days ago)
DECLARE @OrderId4 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-004', @BranchId, @UserId, @UserAddressId,
    'completed', 900000,
    'Pham Thi D', '0909876543', '321 Test Lane', 'Quan 7', 'Phuong 2', 'Ho Chi Minh',
    DATEADD(DAY, -4, GETDATE()), DATEADD(DAY, -4, GETDATE())
);
SET @OrderId4 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId4, @ProductVariantId1, 2, 250000),
       (@OrderId4, @ProductVariantId2, 1, 400000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId4, @PaymentMethodId, 900000, 'completed', DATEADD(DAY, -4, GETDATE()));

-- Order 5: Completed (3 days ago)
DECLARE @OrderId5 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-005', @BranchId, @UserId, @UserAddressId,
    'completed', 1500000,
    'Hoang Van E', '0906543210', '654 Test Boulevard', 'Quan 10', 'Phuong 4', 'Ho Chi Minh',
    DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -3, GETDATE())
);
SET @OrderId5 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId5, @ProductVariantId2, 5, 300000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId5, @PaymentMethodId, 1500000, 'completed', DATEADD(DAY, -3, GETDATE()));

-- Order 6: Completed (2 days ago)
DECLARE @OrderId6 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-006', @BranchId, @UserId, @UserAddressId,
    'completed', 2000000,
    'Vo Thi F', '0902345678', '987 Test Plaza', 'Quan 11', 'Phuong 6', 'Ho Chi Minh',
    DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -2, GETDATE())
);
SET @OrderId6 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId6, @ProductVariantId1, 5, 250000),
       (@OrderId6, @ProductVariantId2, 2, 375000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId6, @PaymentMethodId, 2000000, 'completed', DATEADD(DAY, -2, GETDATE()));

-- Order 7: Processing (1 day ago)
DECLARE @OrderId7 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-007', @BranchId, @UserId, @UserAddressId,
    'processing', 650000,
    'Do Van G', '0908765432', '147 Test Court', 'Quan 12', 'Phuong 7', 'Ho Chi Minh',
    DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, -1, GETDATE())
);
SET @OrderId7 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId7, @ProductVariantId1, 2, 325000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId7, @PaymentMethodId, 650000, 'pending', DATEADD(DAY, -1, GETDATE()));

-- Order 8: Pending (today)
DECLARE @OrderId8 BIGINT;
INSERT INTO purchase_orders (
    order_code, branch_id, user_id, user_address_id,
    status, total_amount,
    shipping_recipient_name, shipping_phone, shipping_address, shipping_district, shipping_ward, shipping_city,
    created_at, updated_at
) VALUES (
    'ORD-TEST-008', @BranchId, @UserId, @UserAddressId,
    'pending', 850000,
    'Bui Thi H', '0905432109', '258 Test Drive', 'Binh Thanh', 'Phuong 8', 'Ho Chi Minh',
    GETDATE(), GETDATE()
);
SET @OrderId8 = SCOPE_IDENTITY();

INSERT INTO purchase_order_details (purchase_order_id, product_variant_id, quantity, price_at_purchase)
VALUES (@OrderId8, @ProductVariantId2, 2, 425000);

INSERT INTO payments (purchase_order_id, payment_method_id, amount, status, created_at)
VALUES (@OrderId8, @PaymentMethodId, 850000, 'pending', GETDATE());

PRINT 'Successfully added 8 test orders (6 completed, 1 processing, 1 pending)';

-- ================================================
-- STEP 4: Verify inserted data
-- ================================================
PRINT '';
PRINT '=== VERIFICATION ===';

SELECT 
    COUNT(*) AS TotalOrders,
    SUM(CASE WHEN status = 'pending' THEN 1 ELSE 0 END) AS PendingOrders,
    SUM(CASE WHEN status = 'processing' THEN 1 ELSE 0 END) AS ProcessingOrders,
    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) AS CompletedOrders,
    SUM(total_amount) AS TotalRevenue
FROM purchase_orders
WHERE deleted_at IS NULL
    AND order_code LIKE 'ORD-TEST-%';

PRINT '';
PRINT '=== SAMPLE ORDERS ===';
SELECT TOP 10
    order_code,
    status,
    total_amount,
    FORMAT(created_at, 'dd/MM/yyyy HH:mm') AS created_at
FROM purchase_orders
WHERE deleted_at IS NULL
ORDER BY created_at DESC;

PRINT '';
PRINT '=== TEST DATA SCRIPT COMPLETED ===';
GO
