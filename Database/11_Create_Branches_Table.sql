USE [perw];
GO

-- =========================================================
-- PHẦN 1: DỌN DẸP CỘT 'is_active' (NẾU ĐANG TỒN TẠI)
-- =========================================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[branches]') AND name = 'is_active')
BEGIN
    -- 1.1 Tìm và xóa ràng buộc mặc định (Default Constraint) của cột is_active trước
    DECLARE @ConstraintName nvarchar(200)
    SELECT @ConstraintName = Name FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.branches')
    AND parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.branches') AND name = 'is_active')

    IF @ConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [dbo].[branches] DROP CONSTRAINT ' + @ConstraintName)
    END

    -- 1.2 Xóa cột is_active
    ALTER TABLE [dbo].[branches] DROP COLUMN [is_active];
    PRINT N'✅ Đã xóa cột [is_active] khỏi bảng branches.';
END
GO

-- =========================================================
-- PHẦN 2: BỔ SUNG CÁC CỘT CẦN THIẾT
-- =========================================================

-- 2.1 Bổ sung cột 'manager_user_id' (Khớp với Code C#)
-- (Lưu ý: Code C# dùng manager_user_id, không phải manager_id)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[branches]') AND name = 'manager_user_id')
BEGIN
    ALTER TABLE [dbo].[branches] ADD [manager_user_id] BIGINT NULL;
    PRINT N'✅ Đã thêm cột [manager_user_id].';
END
GO

-- 2.2 Bổ sung cột 'phone'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[branches]') AND name = 'phone')
BEGIN
    ALTER TABLE [dbo].[branches] ADD [phone] NVARCHAR(20) NULL;
    PRINT N'✅ Đã thêm cột [phone].';
END
GO

-- 2.3 Bổ sung cột 'warehouse_id'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[branches]') AND name = 'warehouse_id')
BEGIN
    ALTER TABLE [dbo].[branches] ADD [warehouse_id] BIGINT NULL;
    PRINT N'✅ Đã thêm cột [warehouse_id].';
END
GO

-- =========================================================
-- PHẦN 3: TẠO KHÓA NGOẠI (Foreign Keys)
-- =========================================================

-- Link tới bảng Users
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[users]') AND type in (N'U'))
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_branches_users]'))
    BEGIN
        ALTER TABLE [dbo].[branches] DROP CONSTRAINT [FK_branches_users];
    END

    ALTER TABLE [dbo].[branches] WITH CHECK ADD CONSTRAINT [FK_branches_users] 
    FOREIGN KEY([manager_user_id]) REFERENCES [dbo].[users] ([id]);
    
    PRINT N'✅ Đã cập nhật khóa ngoại FK_branches_users.';
END
GO

-- Link tới bảng Warehouses
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[warehouses]') AND type in (N'U'))
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_branches_warehouses]'))
    BEGIN
        ALTER TABLE [dbo].[branches] DROP CONSTRAINT [FK_branches_warehouses];
    END

    ALTER TABLE [dbo].[branches] WITH CHECK ADD CONSTRAINT [FK_branches_warehouses] 
    FOREIGN KEY([warehouse_id]) REFERENCES [dbo].[warehouses] ([id]);
    
    PRINT N'✅ Đã cập nhật khóa ngoại FK_branches_warehouses.';
END
GO

-- =========================================================
-- PHẦN 4: THÊM DỮ LIỆU MẪU (KHÔNG CÓ ACTIVE)
-- =========================================================

DECLARE @MainWarehouseID BIGINT = (SELECT TOP 1 id FROM warehouses); 
IF @MainWarehouseID IS NULL SET @MainWarehouseID = 1;

-- Thêm Chi nhánh Quận 1
IF NOT EXISTS (SELECT 1 FROM [dbo].[branches] WHERE [name] = N'Chi nhánh Quận 1')
BEGIN
    INSERT INTO [dbo].[branches] ([name], [location], [phone], [warehouse_id], [manager_user_id], [created_at])
    VALUES (N'Chi nhánh Quận 1', N'45 Nguyễn Huệ, Q1', '0281234567', @MainWarehouseID, NULL, SYSDATETIME());
    PRINT N'➕ Đã thêm Chi nhánh Quận 1';
END

-- Thêm Chi nhánh Hà Đông
IF NOT EXISTS (SELECT 1 FROM [dbo].[branches] WHERE [name] = N'Chi nhánh Hà Đông')
BEGIN
    INSERT INTO [dbo].[branches] ([name], [location], [phone], [warehouse_id], [manager_user_id], [created_at])
    VALUES (N'Chi nhánh Hà Đông', N'12 Văn Phú, Hà Đông', '0249876543', @MainWarehouseID, NULL, SYSDATETIME());
    PRINT N'➕ Đã thêm Chi nhánh Hà Đông';
END

-- Thêm Chi nhánh Thủ Đức
IF NOT EXISTS (SELECT 1 FROM [dbo].[branches] WHERE [name] = N'Chi nhánh Thủ Đức')
BEGIN
    INSERT INTO [dbo].[branches] ([name], [location], [phone], [warehouse_id], [manager_user_id], [created_at])
    VALUES (N'Chi nhánh Thủ Đức', N'88 Võ Văn Ngân, Thủ Đức', '0289998887', @MainWarehouseID, NULL, SYSDATETIME());
    PRINT N'➕ Đã thêm Chi nhánh Thủ Đức';
END
GO