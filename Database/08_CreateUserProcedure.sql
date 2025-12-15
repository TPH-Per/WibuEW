-- =============================================
-- STORED PROCEDURE: Tạo SQL User khi đăng ký
-- Tạo Login Server + User Database + Gán Role
-- =============================================

USE [perw];
GO

IF OBJECT_ID('sp_System_CreateSQLUser', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_System_CreateSQLUser];
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_System_CreateSQLUser]
    @Username NVARCHAR(50), -- Tên đăng nhập (VD: 'staff_kho_01')
    @Password NVARCHAR(50), -- Mật khẩu (plain text - sẽ được SQL Server quản lý)
    @RoleType NVARCHAR(20)  -- Loại: 'Admin', 'Warehouse', 'Branch', 'Customer'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @DB_RoleName NVARCHAR(50);

    -- 1. Xác định Role Database dựa trên tham số đầu vào
    IF @RoleType = 'Admin' SET @DB_RoleName = 'Role_Admin';
    ELSE IF @RoleType = 'Warehouse' SET @DB_RoleName = 'Role_WarehouseManager';
    ELSE IF @RoleType = 'Branch' SET @DB_RoleName = 'Role_BranchManager';
    ELSE IF @RoleType = 'Customer' SET @DB_RoleName = 'Role_Customer';
    ELSE
    BEGIN
        ;THROW 50001, N'Loại quyền không hợp lệ. Chỉ chấp nhận: Admin, Warehouse, Branch, Customer', 1;
    END

    BEGIN TRY
        -- 2. Tạo SQL Login (Cấp Server)
        -- Phải dùng Dynamic SQL vì lệnh CREATE LOGIN không nhận biến
        IF NOT EXISTS (SELECT name FROM master.sys.server_principals WHERE name = @Username)
        BEGIN
            SET @SQL = 'CREATE LOGIN [' + @Username + '] WITH PASSWORD = ''' + @Password + ''', CHECK_POLICY = OFF;';
            EXEC(@SQL);
            PRINT N'✅ Đã tạo Login Server: ' + @Username;
        END
        ELSE
        BEGIN
            PRINT N'⚠️ Login đã tồn tại, bỏ qua bước tạo Login.';
        END

        -- 3. Tạo Database User (Cấp Database - nối với Login trên)
        IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = @Username)
        BEGIN
            SET @SQL = 'CREATE USER [' + @Username + '] FOR LOGIN [' + @Username + '];';
            EXEC(@SQL);
            PRINT N'✅ Đã tạo User Database: ' + @Username;
        END

        -- 4. Gán User vào Role (nếu Role tồn tại)
        IF EXISTS (SELECT name FROM sys.database_principals WHERE name = @DB_RoleName AND type = 'R')
        BEGIN
            SET @SQL = 'ALTER ROLE [' + @DB_RoleName + '] ADD MEMBER [' + @Username + '];';
            EXEC(@SQL);
            PRINT N'✅ Đã gán quyền [' + @DB_RoleName + '] cho user [' + @Username + ']';
        END
        ELSE
        BEGIN
            PRINT N'⚠️ Role [' + @DB_RoleName + '] chưa tồn tại, bỏ qua bước gán role.';
        END

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT N'❌ Lỗi khi tạo user: ' + @ErrorMessage;
        ;THROW;
    END CATCH
END
GO

PRINT N'';
PRINT N'========================================';
PRINT N'✅ STORED PROCEDURE ĐÃ ĐƯỢC TẠO';
PRINT N'========================================';
PRINT N'- sp_System_CreateSQLUser';
PRINT N'';
PRINT N'Usage:';
PRINT N'EXEC sp_System_CreateSQLUser @Username = ''customer_001'', @Password = ''MyPassword123'', @RoleType = ''Customer''';
PRINT N'';
GO
