-- =============================================
-- Script: Tạo bảng discounts và thêm dữ liệu mẫu
-- Date: 2025-12-19
-- =============================================

USE [perw];
GO

-- 1. Tạo bảng discounts nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[discounts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[discounts] (
        [id] BIGINT IDENTITY(1,1) NOT NULL,
        [code] VARCHAR(50) NOT NULL,        -- Mã giảm giá (VD: SALE10)
        [type] VARCHAR(20) NOT NULL,        -- Loại: 'percent' hoặc 'fixed'
        [value] DECIMAL(18, 2) NOT NULL,    -- Giá trị (10 hoặc 50000)
        [min_order_amount] DECIMAL(18, 2) NULL, -- Đơn tối thiểu
        [max_uses] INT NULL,                -- Số lượt dùng tối đa
        [used_count] INT NOT NULL DEFAULT 0, -- Số lượt đã dùng
        [start_at] DATETIME2 NULL,          -- Ngày bắt đầu
        [end_at] DATETIME2 NULL,            -- Ngày kết thúc
        [is_active] BIT NOT NULL DEFAULT 1, -- Trạng thái kích hoạt
        [created_at] DATETIME2 NULL DEFAULT SYSDATETIME(),
        [updated_at] DATETIME2 NULL DEFAULT SYSDATETIME(),
        
        CONSTRAINT [PK_discounts] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_discounts_code] UNIQUE ([code]) -- Mã không được trùng
    );

    PRINT N'✅ Bảng discounts đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT N'⚠️ Bảng discounts đã tồn tại. Bỏ qua bước tạo bảng.';
END
GO

-- 2. Thêm cột discount_id vào bảng purchase_orders (nếu chưa có để link FK)
-- Dựa trên discount.cs có quan hệ ICollection<purchase_orders>
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.purchase_orders') AND name = 'discount_id'
)
BEGIN
    ALTER TABLE [dbo].[purchase_orders]
    ADD [discount_id] BIGINT NULL;

    ALTER TABLE [dbo].[purchase_orders]
    ADD CONSTRAINT [FK_purchase_orders_discounts]
    FOREIGN KEY ([discount_id]) REFERENCES [dbo].[discounts]([id]);
    
    PRINT N'✅ Đã thêm cột discount_id vào bảng purchase_orders.';
END
GO

-- 3. Thêm dữ liệu mẫu (Sample Data)
-- Xóa dữ liệu cũ nếu muốn làm mới lại từ đầu:
-- DELETE FROM [dbo].[discounts]; 

IF NOT EXISTS (SELECT 1 FROM [dbo].[discounts])
BEGIN
    INSERT INTO [dbo].[discounts] 
        ([code], [type], [value], [min_order_amount], [max_uses], [used_count], [start_at], [end_at], [is_active], [created_at], [updated_at])
    VALUES 
        -- Mã 1: Giảm ngay 20k cho đơn từ 100k (Như bạn yêu cầu)
        (
            'GIAMGIA20K',   -- Code
            'fixed',        -- Type (Giảm tiền mặt)
            20000,          -- Value (20.000 VNĐ)
            100000,         -- Min Order (100.000 VNĐ)
            100,            -- Max Uses (100 lần)
            0,              -- Used Count
            GETDATE(),      -- Start Date (Ngay bây giờ)
            DATEADD(day, 30, GETDATE()), -- End Date (30 ngày sau)
            1,              -- Active
            SYSDATETIME(), SYSDATETIME()
        ),

        -- Mã 2: Giảm ngay 50k cho đơn từ 500k
        (
            'GIAMGIA50K', 
            'fixed', 
            50000, 
            500000, 
            50, 
            5, -- Giả sử đã có 5 người dùng
            GETDATE(), 
            DATEADD(day, 30, GETDATE()), 
            1, 
            SYSDATETIME(), SYSDATETIME()
        ),

        -- Mã 3: Giảm 10% (Tối đa giảm cho vui)
        (
            'SALE10', 
            'percent',      -- Type (Phần trăm)
            10,             -- Value (10%)
            0,              -- Không yêu cầu đơn tối thiểu
            1000, 
            150, 
            DATEADD(day, -10, GETDATE()), -- Bắt đầu từ 10 ngày trước
            DATEADD(day, 20, GETDATE()),  -- Còn 20 ngày nữa
            1, 
            SYSDATETIME(), SYSDATETIME()
        ),

        -- Mã 4: Mã giảm giá Flash Sale 50% (Đã hết hạn - Để test filter)
        (
            'FLASHSALE50', 
            'percent', 
            50, 
            200000, 
            10, 
            10, -- Đã dùng hết lượt
            DATEADD(month, -1, GETDATE()), 
            DATEADD(day, -1, GETDATE()), -- Đã hết hạn hôm qua
            0, -- Inactive
            SYSDATETIME(), SYSDATETIME()
        );
            INSERT INTO [dbo].[discounts] 
        ([code], [type], [value], [min_order_amount], [max_uses], [used_count], [start_at], [end_at], [is_active], [created_at], [updated_at])
    VALUES
                (
            'FLASHSALE100', 
            'percent', 
            50, 
            200000, 
            10, 
            10, -- Đã dùng hết lượt
            DATEADD(month, -1, GETDATE()), 
            DATEADD(day, -1, GETDATE()), -- Đã hết hạn hôm qua
            0, -- Inactive
            SYSDATETIME(), SYSDATETIME()
        );

    PRINT N'✅ Đã thêm 4 mã giảm giá mẫu thành công.';
END
ELSE
BEGIN
    PRINT N'⚠️ Dữ liệu mẫu đã tồn tại trong bảng discounts.';
END
GO

select * from discounts

USE [perw];
GO

-- =============================================
-- 1. GỠ BỎ RÀNG BUỘC KHÓA NGOẠI (Để xóa được bảng cũ)
-- =============================================
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_purchase_orders_discounts]'))
BEGIN
    ALTER TABLE [dbo].[purchase_orders] DROP CONSTRAINT [FK_purchase_orders_discounts];
END
GO

-- =============================================
-- 2. XÓA BẢNG CŨ & TẠO BẢNG MỚI
-- =============================================
IF OBJECT_ID('dbo.discounts', 'U') IS NOT NULL DROP TABLE dbo.discounts;
GO

CREATE TABLE [dbo].[discounts] (
    [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [code] VARCHAR(50) NOT NULL UNIQUE,     -- Mã Voucher
    [value] DECIMAL(18, 2) NOT NULL,        -- Giá trị giảm (VND)
    -- ĐÃ BỎ CỘT [type] và [min_order_amount]
    
    [max_uses] INT NULL,                    -- Số lượt dùng tối đa
    [used_count] INT NOT NULL DEFAULT 0,    -- Số lượt đã dùng
    
    [start_at] DATETIME2 NULL,              -- Ngày bắt đầu
    [end_at] DATETIME2 NULL,                -- Ngày kết thúc
    [is_active] BIT NOT NULL DEFAULT 1,     -- Kích hoạt
    
    [created_at] DATETIME2 NULL DEFAULT SYSDATETIME(),
    [updated_at] DATETIME2 NULL DEFAULT SYSDATETIME()
);
GO

-- =============================================
-- 3. TẠO LẠI KHÓA NGOẠI VỚI BẢNG ĐƠN HÀNG
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.purchase_orders') AND name = 'discount_id')
BEGIN
    ALTER TABLE [dbo].[purchase_orders] WITH CHECK ADD CONSTRAINT [FK_purchase_orders_discounts] 
    FOREIGN KEY([discount_id]) REFERENCES [dbo].[discounts] ([id]);
END
GO

-- =============================================
-- 4. THÊM DỮ LIỆU MẪU (Chỉ giảm tiền mặt)
-- =============================================
INSERT INTO [dbo].[discounts] ([code], [value], [max_uses], [used_count], [start_at], [end_at], [is_active])
VALUES 
-- Mã 1: Giảm 20k
('GIAM20K', 20000, 100, 5, GETDATE(), DATEADD(day, 30, GETDATE()), 1),

-- Mã 2: Giảm 50k
('GIAM50K', 50000, 50, 2, GETDATE(), DATEADD(day, 15, GETDATE()), 1),

-- Mã 3: Giảm 100k (Voucher khủng)
('TET2025', 100000, 10, 0, GETDATE(), DATEADD(day, 60, GETDATE()), 1);

PRINT N'✅ Đã cập nhật bảng Discounts (Bỏ type và min_order).';
SELECT * FROM discounts;

select * from purchase_orders