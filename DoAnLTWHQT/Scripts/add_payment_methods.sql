-- =============================================
-- Script to add payment methods: Cash and QR
-- Run this in SQL Server Management Studio
-- =============================================

-- Insert Cash payment method
IF NOT EXISTS (SELECT 1 FROM payment_methods WHERE code = 'cash')
BEGIN
    INSERT INTO payment_methods (name, code, is_active, created_at, updated_at)
    VALUES (N'Tiền mặt', 'cash', 1, GETDATE(), GETDATE());
    PRINT N'Added: Tiền mặt (cash)';
END
ELSE
BEGIN
    UPDATE payment_methods SET is_active = 1, updated_at = GETDATE() WHERE code = 'cash';
    PRINT N'Updated: Tiền mặt (cash) - activated';
END

-- Insert QR Code / Bank Transfer payment method
IF NOT EXISTS (SELECT 1 FROM payment_methods WHERE code = 'qr')
BEGIN
    INSERT INTO payment_methods (name, code, is_active, created_at, updated_at)
    VALUES (N'Thanh toán QR / Chuyển khoản', 'qr', 1, GETDATE(), GETDATE());
    PRINT N'Added: Thanh toán QR / Chuyển khoản';
END
ELSE
BEGIN
    UPDATE payment_methods SET is_active = 1, updated_at = GETDATE() WHERE code = 'qr';
    PRINT N'Updated: Thanh toán QR - activated';
END

-- Show all active payment methods
SELECT id, name, code, is_active 
FROM payment_methods 
WHERE is_active = 1;

PRINT N'Payment methods setup completed!';
GO
