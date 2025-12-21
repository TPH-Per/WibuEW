USE [perw];
GO

-- =============================================
-- 1. CHUẨN BỊ USER (Để tránh lỗi khóa ngoại)
-- =============================================
DECLARE @ValidUserID BIGINT;
SELECT TOP 1 @ValidUserID = id FROM users;

-- Nếu chưa có User nào, tạo ngay 1 user ảo
IF @ValidUserID IS NULL
BEGIN
    INSERT INTO users (username, password, email, full_name, role_id, created_at)
    VALUES ('khachhang_test', '123456', 'test@gmail.com', N'Khách Hàng Test', 1, GETDATE());
    
    SET @ValidUserID = SCOPE_IDENTITY();
    PRINT N'⚠️ Đã tạo mới User ID: ' + CAST(@ValidUserID AS NVARCHAR(50));
END

-- =============================================
-- 2. TẠO LẠI ĐƠN HÀNG (Nếu chưa có)
-- =============================================
-- Đơn 1
IF NOT EXISTS (SELECT 1 FROM purchase_orders WHERE order_code = 'ORD-2024-001')
BEGIN
    INSERT INTO purchase_orders (user_id, order_code, status, shipping_recipient_name, shipping_recipient_phone, shipping_address, sub_total, shipping_fee, discount_amount, total_amount, created_at)
    VALUES (@ValidUserID, 'ORD-2024-001', 'completed', N'Nguyễn Văn A', '0901234567', N'123 Lê Lợi, HCM', 550000, 30000, 50000, 530000, GETDATE());
    PRINT N'✅ Đã tạo lại đơn ORD-2024-001';
END

-- Đơn 2
IF NOT EXISTS (SELECT 1 FROM purchase_orders WHERE order_code = 'ORD-2024-002')
BEGIN
    INSERT INTO purchase_orders (user_id, order_code, status, shipping_recipient_name, shipping_recipient_phone, shipping_address, sub_total, shipping_fee, discount_amount, total_amount, created_at)
    VALUES (@ValidUserID, 'ORD-2024-002', 'processing', N'Trần Thị B', '0912345678', N'456 Nguyễn Trãi, HN', 1200000, 50000, 0, 1250000, GETDATE());
    PRINT N'✅ Đã tạo lại đơn ORD-2024-002';
END
GO

-- =============================================
-- 3. TẠO PHƯƠNG THỨC THANH TOÁN (Chỉ COD & PREPAID)
-- =============================================
-- Xóa sạch dữ liệu cũ để nạp lại
DELETE FROM payments;
DELETE FROM payment_methods;

-- Bật nhập ID
IF OBJECTPROPERTY(OBJECT_ID('payment_methods'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT payment_methods ON;

INSERT INTO payment_methods (id, name, code, is_active, created_at) VALUES (1, N'Thanh toán khi nhận hàng (COD)', 'COD', 1, GETDATE());
INSERT INTO payment_methods (id, name, code, is_active, created_at) VALUES (2, N'Chuyển khoản / Thanh toán Online', 'PREPAID', 1, GETDATE());

-- Tắt nhập ID
IF OBJECTPROPERTY(OBJECT_ID('payment_methods'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT payment_methods OFF;
GO

-- =============================================
-- 4. TẠO DỮ LIỆU THANH TOÁN
-- =============================================
DECLARE @Order1_ID BIGINT = (SELECT TOP 1 id FROM purchase_orders WHERE order_code = 'ORD-2024-001');
DECLARE @Order2_ID BIGINT = (SELECT TOP 1 id FROM purchase_orders WHERE order_code = 'ORD-2024-002');

-- Đơn 1: COD
IF @Order1_ID IS NOT NULL 
    INSERT INTO payments (order_id, payment_method_id, amount, status, transaction_code, created_at) 
    VALUES (@Order1_ID, 1, 530000, 'completed', NULL, GETDATE());

-- Đơn 2: PREPAID (Chuyển khoản OCB)
IF @Order2_ID IS NOT NULL 
    INSERT INTO payments (order_id, payment_method_id, amount, status, transaction_code, created_at) 
    VALUES (@Order2_ID, 2, 1250000, 'completed', 'OCB-TRANS-9999', GETDATE());

PRINT N'🎉 ĐÃ HOÀN TẤT! Hãy chạy lại lệnh SELECT dưới đây để kiểm tra.';
GO

-- Kiểm tra kết quả
SELECT * FROM purchase_orders;
SELECT * FROM payment_methods;
SELECT * FROM payments;