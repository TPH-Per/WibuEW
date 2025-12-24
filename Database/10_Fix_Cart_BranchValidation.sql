USE perw;
GO

-- ================================================
-- Fix Cart Branch Validation Trigger
-- Date: 2025-12-24
-- Note: Column in carts table is 'branchid' (no underscore)
-- ================================================

PRINT '=== Fixing Cart Branch Validation Trigger ===';

-- Drop existing trigger
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Cart_ValidateConsistency')
BEGIN
    DROP TRIGGER TR_Cart_ValidateConsistency;
    PRINT '  - Dropped existing trigger TR_Cart_ValidateConsistency';
END
GO

-- Create corrected trigger
CREATE TRIGGER TR_Cart_ValidateBranchConsistency
ON dbo.carts
AFTER INSERT, UPDATE
AS 
BEGIN
    SET NOCOUNT ON;
    
    -- Skip if no rows affected
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    
    -- Kiểm tra mỗi user chỉ có items từ 1 branch trong giỏ hàng active
    DECLARE @UserId BIGINT;
    DECLARE @BranchCount INT;
    
    -- Get affected user IDs
    DECLARE user_cursor CURSOR FOR 
        SELECT DISTINCT user_id FROM inserted WHERE deleted_at IS NULL;
    
    OPEN user_cursor;
    FETCH NEXT FROM user_cursor INTO @UserId;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Count distinct branches for this user's active cart items
        SELECT @BranchCount = COUNT(DISTINCT branchid)
        FROM dbo.carts
        WHERE user_id = @UserId
          AND deleted_at IS NULL;
        
        IF @BranchCount > 1
        BEGIN
            CLOSE user_cursor;
            DEALLOCATE user_cursor;
            
            RAISERROR(N'Giỏ hàng chỉ được chứa sản phẩm từ một chi nhánh. Vui lòng xóa sản phẩm từ chi nhánh khác trước khi thêm.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
        
        FETCH NEXT FROM user_cursor INTO @UserId;
    END
    
    CLOSE user_cursor;
    DEALLOCATE user_cursor;
END;
GO

PRINT '✓ Created trigger TR_Cart_ValidateBranchConsistency';
GO

-- Create stored procedure for adding to cart with branch validation
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_Cart_AddItem')
BEGIN
    DROP PROCEDURE sp_Cart_AddItem;
    PRINT '  - Dropped existing sp_Cart_AddItem';
END
GO

CREATE PROCEDURE sp_Cart_AddItem
    @UserId BIGINT,
    @ProductVariantId BIGINT,
    @BranchId BIGINT,
    @Quantity INT,
    @Price DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 1. Kiểm tra variant tồn tại
        IF NOT EXISTS (SELECT 1 FROM product_variants WHERE id = @ProductVariantId AND deleted_at IS NULL)
        BEGIN
            ;THROW 50001, N'Sản phẩm không tồn tại hoặc đã bị xóa.', 1;
        END
        
        -- 2. Kiểm tra branch tồn tại
        IF NOT EXISTS (SELECT 1 FROM branches WHERE id = @BranchId)
        BEGIN
            ;THROW 50002, N'Chi nhánh không tồn tại.', 1;
        END
        
        -- 3. Kiểm tra tồn kho tại branch
        DECLARE @AvailableStock INT;
        SELECT @AvailableStock = ISNULL(quantity_on_hand, 0)
        FROM branch_inventories
        WHERE product_variant_id = @ProductVariantId AND branch_id = @BranchId;
        
        IF @AvailableStock IS NULL OR @AvailableStock < @Quantity
        BEGIN
            DECLARE @StockMsg NVARCHAR(200) = N'Sản phẩm không có đủ tồn kho tại chi nhánh này. Hiện có: ' + CAST(ISNULL(@AvailableStock, 0) AS NVARCHAR);
            ;THROW 50003, @StockMsg, 1;
        END
        
        -- 4. Kiểm tra nếu user đã có items từ branch khác trong cart
        DECLARE @ExistingBranchId BIGINT;
        SELECT TOP 1 @ExistingBranchId = branchid
        FROM carts
        WHERE user_id = @UserId AND deleted_at IS NULL;
        
        IF @ExistingBranchId IS NOT NULL AND @ExistingBranchId <> @BranchId
        BEGIN
            DECLARE @BranchName NVARCHAR(100);
            SELECT @BranchName = name FROM branches WHERE id = @ExistingBranchId;
            DECLARE @BranchMsg NVARCHAR(300) = N'Giỏ hàng của bạn đang có sản phẩm từ chi nhánh "' + ISNULL(@BranchName, N'Khác') + N'". Vui lòng xóa trước khi thêm sản phẩm từ chi nhánh khác.';
            ;THROW 50004, @BranchMsg, 1;
        END
        
        -- 5. Kiểm tra nếu item đã tồn tại trong cart (cùng variant + cùng branch)
        DECLARE @ExistingCartId BIGINT;
        SELECT @ExistingCartId = id
        FROM carts
        WHERE user_id = @UserId 
          AND product_variant_id = @ProductVariantId 
          AND branchid = @BranchId
          AND deleted_at IS NULL;
        
        IF @ExistingCartId IS NOT NULL
        BEGIN
            -- Update quantity
            UPDATE carts
            SET quantity = quantity + @Quantity,
                updated_at = SYSDATETIME()
            WHERE id = @ExistingCartId;
            
            SELECT @ExistingCartId AS CartItemId, 'updated' AS Action;
        END
        ELSE
        BEGIN
            -- Insert new item
            INSERT INTO carts (user_id, product_variant_id, branchid, quantity, price, created_at)
            VALUES (@UserId, @ProductVariantId, @BranchId, @Quantity, @Price, SYSDATETIME());
            
            SELECT SCOPE_IDENTITY() AS CartItemId, 'created' AS Action;
        END
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        ;THROW;
    END CATCH
END;
GO

PRINT '✓ Created stored procedure sp_Cart_AddItem';
GO

-- Verification
PRINT '';
PRINT '=== Verification ===';

SELECT 
    t.name AS TriggerName,
    'Trigger' AS Type,
    CASE WHEN t.is_disabled = 0 THEN 'Enabled' ELSE 'Disabled' END AS Status
FROM sys.triggers t
WHERE t.name = 'TR_Cart_ValidateBranchConsistency'
UNION ALL
SELECT 
    p.name AS ProcName,
    'Procedure' AS Type,
    'Active' AS Status
FROM sys.procedures p
WHERE p.name = 'sp_Cart_AddItem';

PRINT '';
PRINT '✓ Cart Branch Validation setup complete!';
GO
