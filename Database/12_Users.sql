USE [perw];
GO

-- =============================================
-- 1. TẠO BẢNG ROLES (Nếu chưa có)
-- =============================================
IF OBJECT_ID('dbo.roles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[roles](
        [id] [bigint] IDENTITY(1,1) PRIMARY KEY,
        [name] [nvarchar](50) NOT NULL,
        [display_name] [nvarchar](100) NULL
    );
    PRINT N'✅ Đã tạo bảng Roles.';
END
ELSE
BEGIN
    PRINT N'ℹ️ Bảng Roles đã tồn tại.';
END
GO

-- =============================================
-- 2. THÊM DỮ LIỆU ROLES (BẮT BUỘC PHẢI CÓ ID 1, 2)
-- =============================================
-- Bật chế độ cho phép nhập ID thủ công
SET IDENTITY_INSERT roles ON;

-- Role Admin (ID = 1)
IF NOT EXISTS (SELECT 1 FROM roles WHERE id = 1)
    INSERT INTO roles (id, name, display_name) VALUES (1, 'admin', N'Quản trị viên');

-- Role Quản lý chi nhánh (ID = 2)
IF NOT EXISTS (SELECT 1 FROM roles WHERE id = 2)
    INSERT INTO roles (id, name, display_name) VALUES (2, 'branch_manager', N'Quản lý chi nhánh');

-- Role Thủ kho (ID = 3)
IF NOT EXISTS (SELECT 1 FROM roles WHERE id = 3)
    INSERT INTO roles (id, name, display_name) VALUES (3, 'warehouse_manager', N'Thủ kho');

SET IDENTITY_INSERT roles OFF;
PRINT N'✅ Đã nạp dữ liệu Roles (Admin, Manager...).';
GO

-- =============================================
-- 3. SỬA BẢNG USERS (Đảm bảo có cột role_id và Khóa ngoại)
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[users]') AND type in (N'U'))
BEGIN
    -- 3.1 Nếu bảng User chưa có cột role_id thì thêm vào
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[users]') AND name = 'role_id')
    BEGIN
        -- Thêm cột role_id, mặc định là 2 (Manager) cho các user cũ
        ALTER TABLE [dbo].[users] ADD [role_id] BIGINT NOT NULL DEFAULT 2;
        PRINT N'✅ Đã bổ sung cột role_id cho Users.';
    END

    -- 3.2 Tạo khóa ngoại nối Users -> Roles
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_users_roles]'))
    BEGIN
        ALTER TABLE [dbo].[users] WITH CHECK ADD CONSTRAINT [FK_users_roles] 
        FOREIGN KEY([role_id]) REFERENCES [dbo].[roles] ([id]);
        PRINT N'✅ Đã tạo liên kết (Khóa ngoại) giữa Users và Roles.';
    END
END
GO

-- =============================================
-- 4. KIỂM TRA LẠI (Chạy câu lệnh Select của bạn)
-- =============================================
PRINT N'--- DANH SÁCH USER HIỆN TẠI ---';
SELECT 
    u.id, 
    u.full_name AS [Họ Tên], 
    u.email AS [Email], 
    u.password AS [Pass],
    r.display_name AS [Chức Vụ]
FROM users u
LEFT JOIN roles r ON u.role_id = r.id;

USE [perw];
GO

-- =============================================
-- 1. SỬA DỮ LIỆU SAI (QUAN TRỌNG NHẤT)
-- =============================================
-- Tìm tất cả User có role_id không tồn tại (hoặc null) và gán về 2 (Quản lý chi nhánh)
UPDATE users
SET role_id = 2
WHERE role_id IS NULL 
   OR role_id NOT IN (SELECT id FROM roles);

PRINT N'✅ Đã sửa xong các dòng dữ liệu role_id bị lỗi.';
GO

-- =============================================
-- 2. TẠO LẠI KHÓA NGOẠI (Giờ mới chạy được)
-- =============================================
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_users_roles]'))
BEGIN
    ALTER TABLE [dbo].[users] DROP CONSTRAINT [FK_users_roles];
END

ALTER TABLE [dbo].[users] WITH CHECK ADD CONSTRAINT [FK_users_roles] 
FOREIGN KEY([role_id]) REFERENCES [dbo].[roles] ([id]);

PRINT N'✅ Đã tạo khóa ngoại FK_users_roles thành công!';
GO

-- =============================================
-- 3. KIỂM TRA KẾT QUẢ
-- =============================================
SELECT 
    u.id, 
    u.full_name, 
    u.email, 
    u.role_id,
    r.display_name AS [Chức Vụ]
FROM users u
LEFT JOIN roles r ON u.role_id = r.id;