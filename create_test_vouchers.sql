-- =========================================================
-- SCRIPT: TAO MA GIAM GIA TEST CHO POS
-- =========================================================

USE perw;
GO

-- Kiem tra table discounts ton tai
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'discounts')
BEGIN
    PRINT 'Table discounts khong ton tai!';
    RETURN;
END

-- Xoa ma test cu (neu co)
DELETE FROM discounts WHERE code = 'TEST2026';

PRINT '========================================';
PRINT 'TAO MA GIAM GIA TEST';
PRINT '========================================';
PRINT '';

-- Tao ma giam gia moi
INSERT INTO discounts (
    code,
    [type],
    value,
    is_active,
    start_at,
    end_at,
    max_uses,
    used_count,
    created_at,
    updated_at
)
VALUES (
    'TEST2026',          -- Ma voucher
    'fixed',             -- Loai: giam tien (fixed) hoac giam % (percent)
    50000,               -- Giam 50,000 VND
    1,                   -- is_active = true
    GETDATE(),           -- Bat dau tu bay gio
    DATEADD(MONTH, 1, GETDATE()), -- Het han sau 1 thang
    100,                 -- Toi da 100 luot su dung
    0,                   -- Chua su dung lan nao
    GETDATE(),
    GETDATE()
);

PRINT 'Da tao ma giam gia:';
PRINT '  Code: TEST2026';
PRINT '  Giam: 50,000 VND';
PRINT '  Hieu luc: ' + CAST(GETDATE() AS VARCHAR(30)) + ' -> ' + CAST(DATEADD(MONTH, 1, GETDATE()) AS VARCHAR(30));
PRINT '  Max uses: 100';
PRINT '';

-- Tao them vai ma khac
INSERT INTO discounts (code, [type], value, is_active, start_at, end_at, max_uses, used_count, created_at, updated_at)
VALUES 
    ('SALE10', 'percent', 10, 1, GETDATE(), DATEADD(MONTH, 6, GETDATE()), 500, 0, GETDATE(), GETDATE()),
    ('NEWYEAR', 'fixed', 100000, 1, GETDATE(), DATEADD(MONTH, 3, GETDATE()), 200, 0, GETDATE(), GETDATE()),
    ('VIP50', 'fixed', 50000, 1, GETDATE(), DATEADD(YEAR, 1, GETDATE()), NULL, 0, GETDATE(), GETDATE());

PRINT 'Da tao them 3 ma khac:';
PRINT '  SALE10 - Giam 10%';
PRINT '  NEWYEAR - Giam 100,000 VND';
PRINT '  VIP50 - Giam 50,000 VND';
PRINT '';

PRINT '========================================';
PRINT 'HOAN THANH!';
PRINT '========================================';

-- Hien thi tat ca ma giam gia dang hoat dong
SELECT 
    id,
    code,
    [type],
    value,
    is_active,
    CONVERT(VARCHAR(20), start_at, 120) as start_at,
    CONVERT(VARCHAR(20), end_at, 120) as end_at,
    max_uses,
    used_count
FROM discounts
WHERE is_active = 1
ORDER BY created_at DESC;

GO
