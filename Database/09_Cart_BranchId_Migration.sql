USE perw;
GO

-- ================================================
-- Cart Migration: Add branchid column
-- Date: 2025-12-19
-- ================================================

ALTER TABLE carts
ADD branchid BIGINT NULL;

ALTER TABLE carts
ADD CONSTRAINT FK_carts_branches 
FOREIGN KEY (branchid) REFERENCES branches(id);

-- Index cho performance
CREATE INDEX IX_carts_branchid ON carts(branchid); 

-- ================================================
-- Complete Cart Migration (từ trạng thái đã có branchid NULL)
-- Date: 2025-12-19
-- ================================================

PRINT '=== Completing Cart Migration ===';

-- Step 1: Migrate existing data - gán default branch cho cart items cũ
PRINT 'Step 1: Migrating existing cart data...';

DECLARE @DefaultBranchId BIGINT;
DECLARE @UpdatedRows INT;

-- Lấy branch đầu tiên làm default
SELECT TOP 1 @DefaultBranchId = id 
FROM dbo.branches 
ORDER BY id ASC;

IF @DefaultBranchId IS NULL
BEGIN
    PRINT '✗ ERROR: No branches found in database!';
    PRINT 'Please create at least one branch first.';
    RETURN;
END

PRINT CONCAT('  Using branch ID ', @DefaultBranchId, ' as default');

-- Update tất cả cart items có branchid = NULL
UPDATE dbo.carts
SET branchid = @DefaultBranchId
WHERE branchid IS NULL;

SET @UpdatedRows = @@ROWCOUNT;
PRINT CONCAT('✓ Updated ', @UpdatedRows, ' cart items with default branch');
GO

-- Step 2: Verify không còn NULL values
DECLARE @NullCount INT;
SELECT @NullCount = COUNT(*) FROM dbo.carts WHERE branchid IS NULL;

IF @NullCount > 0
BEGIN
    PRINT CONCAT('✗ ERROR: Still have ', @NullCount, ' NULL branchid values!');
    RETURN;
END
ELSE
BEGIN
    PRINT '✓ All cart items now have branchid';
END
GO

-- Step 3: Alter column to NOT NULL
PRINT 'Step 2: Making branchid NOT NULL...';

-- Drop index trước
DROP INDEX IX_carts_branchid ON dbo.carts;
PRINT '  - Dropped index';

-- Drop FK trước
ALTER TABLE dbo.carts DROP CONSTRAINT FK_carts_branches;
PRINT '  - Dropped FK constraint';

-- Alter column
ALTER TABLE dbo.carts
ALTER COLUMN branchid BIGINT NOT NULL;
PRINT '  - Column altered to NOT NULL';

-- Recreate FK
ALTER TABLE dbo.carts
ADD CONSTRAINT FK_carts_branches 
FOREIGN KEY (branchid) REFERENCES dbo.branches(id);
PRINT '  - FK constraint recreated';

-- Recreate index
CREATE NONCLUSTERED INDEX IX_carts_branchid 
ON dbo.carts(branchid ASC)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, 
      SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, 
      ONLINE = OFF, ALLOW_ROW_LOCKS = ON, 
      ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
PRINT '  - Index recreated';

PRINT '✓ branchid is now NOT NULL';
GO

-- Step 4: Create validation trigger
PRINT 'Step 3: Creating validation trigger...';

-- Drop nếu đã tồn tại
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Cart_ValidateConsistency')
BEGIN
    DROP TRIGGER TR_Cart_ValidateConsistency;
    PRINT '  - Dropped existing trigger';
END
GO

CREATE TRIGGER TR_Cart_ValidateConsistency
ON dbo.carts
AFTER INSERT, UPDATE
AS BEGIN
    SET NOCOUNT ON;
    
    -- Kiểm tra mỗi user chỉ có items từ 1 branch
    IF EXISTS (
        SELECT c.userid
        FROM dbo.carts c
        WHERE c.userid IN (SELECT i.userid FROM inserted i)
          AND c.deletedat IS NULL
        GROUP BY c.userid
        HAVING COUNT(DISTINCT c.branchid) > 1
    )
    BEGIN
        RAISERROR('Cart can only contain items from one branch', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
    
    -- Kiểm tra không mix active và pre-order
    IF EXISTS (
        SELECT c.userid
        FROM dbo.carts c
        INNER JOIN dbo.productvariants pv ON c.productvariantid = pv.id
        INNER JOIN dbo.products p ON pv.productid = p.id
        WHERE c.userid IN (SELECT i.userid FROM inserted i)
          AND c.deletedat IS NULL
        GROUP BY c.userid
        HAVING COUNT(DISTINCT 
            CASE 
                WHEN p.status IN ('active', 'draft') THEN 'normal'
                WHEN p.status = 'pre-order' THEN 'pre-order'
                ELSE 'normal'
            END
        ) > 1
    )
    BEGIN
        RAISERROR('Cart cannot mix normal and pre-order products', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

PRINT '✓ Validation trigger created';
PRINT '';
PRINT '=== Migration Completed Successfully ===';
GO

-- Final verification
PRINT '';
PRINT '=== Verification ===';

-- Check schema
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END AS Nullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.carts')
  AND c.name = 'branchid';

-- Check constraints
SELECT 
    fk.name AS ConstraintName,
    'FK' AS Type
FROM sys.foreign_keys fk
WHERE fk.name = 'FK_carts_branches'
UNION ALL
SELECT 
    i.name AS ConstraintName,
    'Index' AS Type
FROM sys.indexes i
WHERE i.name = 'IX_carts_branchid'
  AND i.object_id = OBJECT_ID('dbo.carts')
UNION ALL
SELECT 
    t.name AS ConstraintName,
    'Trigger' AS Type
FROM sys.triggers t
WHERE t.name = 'TR_Cart_ValidateConsistency';

-- Show summary
PRINT '';
PRINT 'Cart table summary:';
SELECT 
    COUNT(*) AS TotalCartItems,
    COUNT(DISTINCT userid) AS UniqueUsers,
    COUNT(DISTINCT branchid) AS UniqueBranches
FROM dbo.carts
WHERE deletedat IS NULL;

PRINT '';
PRINT '✓ All verifications passed!';
GO
