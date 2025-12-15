# User Registration & Creation Setup Guide

This document contains all the necessary scripts and instructions to set up the user registration functionality.

---

## 📋 Prerequisites

- SQL Server 2019+
- Visual Studio 2022
- .NET Framework 4.8
- Database `perw` already created

---

## 🔧 Step 1: Run SQL Scripts

### 1.1. Create Database Roles (Run first)

```sql
USE [perw];
GO

-- Create database roles if not exist
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_Admin' AND type = 'R')
    CREATE ROLE [Role_Admin];
    
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_WarehouseManager' AND type = 'R')
    CREATE ROLE [Role_WarehouseManager];
    
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_BranchManager' AND type = 'R')
    CREATE ROLE [Role_BranchManager];
    
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'Role_Customer' AND type = 'R')
    CREATE ROLE [Role_Customer];

PRINT N'✅ Database roles created successfully';
GO
```

### 1.2. Create Stored Procedure for SQL User Creation

```sql
USE [perw];
GO

IF OBJECT_ID('sp_System_CreateSQLUser', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_System_CreateSQLUser];
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_System_CreateSQLUser]
    @Username NVARCHAR(50),
    @Password NVARCHAR(50),
    @RoleType NVARCHAR(20)  -- 'Admin', 'Warehouse', 'Branch', 'Customer'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @DB_RoleName NVARCHAR(50);

    -- Determine Database Role
    IF @RoleType = 'Admin' SET @DB_RoleName = 'Role_Admin';
    ELSE IF @RoleType = 'Warehouse' SET @DB_RoleName = 'Role_WarehouseManager';
    ELSE IF @RoleType = 'Branch' SET @DB_RoleName = 'Role_BranchManager';
    ELSE IF @RoleType = 'Customer' SET @DB_RoleName = 'Role_Customer';
    ELSE
    BEGIN
        ;THROW 50001, N'Invalid role type. Accepted: Admin, Warehouse, Branch, Customer', 1;
    END

    BEGIN TRY
        -- Create SQL Login (Server level)
        IF NOT EXISTS (SELECT name FROM master.sys.server_principals WHERE name = @Username)
        BEGIN
            SET @SQL = 'CREATE LOGIN [' + @Username + '] WITH PASSWORD = ''' + @Password + ''', CHECK_POLICY = OFF;';
            EXEC(@SQL);
            PRINT N'✅ Created Server Login: ' + @Username;
        END
        ELSE
        BEGIN
            PRINT N'⚠️ Login already exists, skipping.';
        END

        -- Create Database User
        IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = @Username)
        BEGIN
            SET @SQL = 'CREATE USER [' + @Username + '] FOR LOGIN [' + @Username + '];';
            EXEC(@SQL);
            PRINT N'✅ Created Database User: ' + @Username;
        END

        -- Add to Role
        IF EXISTS (SELECT name FROM sys.database_principals WHERE name = @DB_RoleName AND type = 'R')
        BEGIN
            SET @SQL = 'ALTER ROLE [' + @DB_RoleName + '] ADD MEMBER [' + @Username + '];';
            EXEC(@SQL);
            PRINT N'✅ Added user to role [' + @DB_RoleName + ']';
        END

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT N'❌ Error: ' + @ErrorMessage;
        ;THROW;
    END CATCH
END
GO

PRINT N'✅ Stored procedure sp_System_CreateSQLUser created';
GO
```

### 1.3. Test the Stored Procedure

```sql
-- Test creating a user
EXEC sp_System_CreateSQLUser 
    @Username = 'test_customer', 
    @Password = 'Test@123', 
    @RoleType = 'Customer';

-- Verify user was created
SELECT name FROM sys.database_principals WHERE name = 'test_customer';
SELECT name FROM master.sys.server_principals WHERE name = 'test_customer';
```

---

## 🔧 Step 2: Ensure NuGet Packages

Make sure these packages are in `packages.config`:

```xml
<package id="BCrypt.Net-Next" version="4.0.3" targetFramework="net48" />
<package id="System.Buffers" version="4.5.1" targetFramework="net48" />
<package id="System.Memory" version="4.5.5" targetFramework="net48" />
<package id="System.Runtime.CompilerServices.Unsafe" version="6.0.0" targetFramework="net48" />
```

Run in Package Manager Console:
```powershell
Install-Package BCrypt.Net-Next -Version 4.0.3
Install-Package System.Memory -Version 4.5.5
```

---

## 🔧 Step 3: Web.config Connection String

Ensure `Web.config` has correct connection string format:

```xml
<connectionStrings>
    <add name="PerwDbContext" 
         connectionString="Data Source=localhost;Initial Catalog=perw;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

⚠️ **IMPORTANT**: Use `TrustServerCertificate` (no spaces), NOT `Trust Server Certificate`

---

## 📦 Step 4: Required Files

### 4.1. ViewModel: `RegisterViewModel.cs`
Location: `DoAnLTWHQT/ViewModels/Admin/RegisterViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Ltwhqt.ViewModels.Admin
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Only letters, numbers, and underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email.")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool AgreeTerms { get; set; }
    }
}
```

### 4.2. AccountController Register Actions
Location: `DoAnLTWHQT/Controllers/AccountController.cs`

Add these using statements at the top:
```csharp
using System.Configuration;
using System.Data.SqlClient;
using BCryptNet = BCrypt.Net.BCrypt;
```

Add these actions:
```csharp
// GET: /Account/Register
[AllowAnonymous]
[HttpGet]
[Route("register")]
public ActionResult Register()
{
    if (User?.Identity?.IsAuthenticated == true)
    {
        return RedirectToAction("Index", "Home");
    }
    return View(new RegisterViewModel());
}

// POST: /Account/Register
[AllowAnonymous]
[HttpPost]
[Route("register")]
[ValidateAntiForgeryToken]
public ActionResult Register(RegisterViewModel model)
{
    try
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var normalizedUsername = model.Username.Trim().ToLowerInvariant();

        // Check duplicates
        if (_db.users.Any(u => u.email.ToLower() == normalizedEmail && u.deleted_at == null))
        {
            ModelState.AddModelError("Email", "Email already in use.");
            return View(model);
        }

        if (_db.users.Any(u => u.name.ToLower() == normalizedUsername && u.deleted_at == null))
        {
            ModelState.AddModelError("Username", "Username already in use.");
            return View(model);
        }

        // Step 1: Create SQL User
        var connectionString = ConfigurationManager.ConnectionStrings["PerwDbContext"]?.ConnectionString;
        if (!string.IsNullOrEmpty(connectionString))
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("sp_System_CreateSQLUser", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Username", normalizedUsername);
                        command.Parameters.AddWithValue("@Password", model.Password);
                        command.Parameters.AddWithValue("@RoleType", "Customer");
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"SQL Error: {sqlEx.Message}");
                throw new Exception($"Cannot create SQL User: {sqlEx.Message}");
            }
        }

        // Step 2: Hash password with BCrypt
        var hashedPassword = BCryptNet.HashPassword(model.Password, BCryptNet.GenerateSalt(12));

        // Step 3: Insert into users table
        var newUser = new user
        {
            name = normalizedUsername,
            full_name = model.FullName.Trim(),
            email = normalizedEmail,
            phone_number = model.PhoneNumber?.Trim(),
            password = hashedPassword,
            role_id = 4, // Customer
            status = "active",
            created_at = DateTime.Now,
            updated_at = DateTime.Now
        };

        _db.users.Add(newUser);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Registration successful! Please login.";
        return RedirectToAction("Login");
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", $"Error: {ex.Message}");
        return View(model);
    }
}
```

---

## 🎨 Step 5: Views

### 5.1. Register View
Location: `DoAnLTWHQT/Views/Account/Register.cshtml`

```html
@model Ltwhqt.ViewModels.Admin.RegisterViewModel
@{
    ViewBag.Title = "Register";
    Layout = "~/Views/Shared/_LayoutAuth.cshtml";
}

<div class="card shadow-sm border-0">
    <div class="card-body p-5">
        <h2 class="fw-bold mb-4 text-center">Create Account</h2>

        @if (!ViewData.ModelState.IsValid)
        {
            <div class="alert alert-danger">
                @Html.ValidationSummary(true, "Please check the form:")
            </div>
        }

        @using (Html.BeginForm("Register", "Account", FormMethod.Post))
        {
            @Html.AntiForgeryToken()

            <div class="row">
                <div class="col-md-6 mb-3">
                    <label class="form-label">Username *</label>
                    @Html.TextBoxFor(m => m.Username, new { @class = "form-control", placeholder = "e.g. john_doe", required = "required" })
                    @Html.ValidationMessageFor(m => m.Username, "", new { @class = "text-danger small" })
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label">Full Name *</label>
                    @Html.TextBoxFor(m => m.FullName, new { @class = "form-control", placeholder = "John Doe", required = "required" })
                </div>
            </div>

            <div class="mb-3">
                <label class="form-label">Email *</label>
                @Html.TextBoxFor(m => m.Email, new { @class = "form-control", type = "email", placeholder = "john@example.com", required = "required" })
            </div>

            <div class="mb-3">
                <label class="form-label">Phone Number</label>
                @Html.TextBoxFor(m => m.PhoneNumber, new { @class = "form-control", type = "tel", placeholder = "0901234567" })
            </div>

            <div class="row">
                <div class="col-md-6 mb-3">
                    <label class="form-label">Password *</label>
                    @Html.PasswordFor(m => m.Password, new { @class = "form-control", placeholder = "••••••••", required = "required", minlength = "6" })
                </div>
                <div class="col-md-6 mb-3">
                    <label class="form-label">Confirm Password *</label>
                    @Html.PasswordFor(m => m.ConfirmPassword, new { @class = "form-control", placeholder = "••••••••", required = "required" })
                </div>
            </div>

            <div class="form-check mb-4">
                @Html.CheckBoxFor(m => m.AgreeTerms, new { @class = "form-check-input", required = "required" })
                <label class="form-check-label">I agree to the Terms of Service</label>
            </div>

            <button type="submit" class="btn btn-success btn-lg w-100">Register</button>

            <div class="text-center mt-3">
                Already have an account? <a href="@Url.Content("~/login")">Login</a>
            </div>
        }
    </div>
</div>
```

---

## ✅ Step 6: Testing

### Test Customer Registration:
1. Go to: `http://localhost:PORT/register`
2. Fill in the form
3. Submit
4. Check:
   - `users` table in database for new entry
   - SQL Server > Security > Logins for new SQL Login
   - SQL Server > Database > perw > Security > Users for new DB User

### Test Admin User Creation:
1. Login as admin
2. Go to: `http://localhost:PORT/Admin/Users/Create`
3. Fill in the form with any role
4. Submit
5. Check same as above

---

## 🔍 Troubleshooting

### Error: "Keyword not supported: trust server certificate"
**Solution**: Change `Trust Server Certificate` to `TrustServerCertificate` in Web.config

### Error: "Could not find stored procedure"
**Solution**: Run the SQL script to create `sp_System_CreateSQLUser`

### Error: "System.Memory assembly not found"
**Solution**: Run `Install-Package System.Memory -Version 4.5.5`

### SQL User not created but users table entry exists
**Cause**: Insufficient permissions to create SQL Login
**Solution**: Connect with `sa` account or grant `securityadmin` role

---

## 📊 Database Tables Used

```sql
-- users table structure
CREATE TABLE users (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    full_name NVARCHAR(255) NOT NULL,
    email NVARCHAR(255) NOT NULL UNIQUE,
    email_verified_at DATETIME2,
    password NVARCHAR(255) NOT NULL,  -- BCrypt hashed
    remember_token NVARCHAR(100),
    role_id BIGINT NOT NULL,          -- FK to roles
    warehouse_id BIGINT,              -- FK to warehouses
    phone_number NVARCHAR(255),
    status NVARCHAR(255) NOT NULL DEFAULT 'active',
    created_at DATETIME2,
    updated_at DATETIME2,
    deleted_at DATETIME2
);

-- roles table structure
CREATE TABLE roles (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,       -- admin, warehouse_manager, branch_manager, client
    description NVARCHAR(MAX),
    created_at DATETIME2,
    updated_at DATETIME2,
    deleted_at DATETIME2
);
```

---

## 📝 Summary

| Component | Location |
|-----------|----------|
| SQL Stored Procedure | Run in SSMS |
| RegisterViewModel | `ViewModels/Admin/RegisterViewModel.cs` |
| AccountController | `Controllers/AccountController.cs` |
| Register View | `Views/Account/Register.cshtml` |
| Admin User Form | `Areas/Admin/Views/Users/Form.cshtml` |
| Admin UsersController | `Areas/Admin/Controllers/UsersController.cs` |

---

**Last Updated**: 2025-12-15
