-- =============================================
-- COMPLETE FIX for sp_POS_Checkout_Classic
-- WITH VOUCHER/DISCOUNT SUPPORT
-- Status: qr_atBranch or cash_atBranch
-- =============================================

-- First, check if the stored procedure exists and drop it
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_POS_Checkout_Classic]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_POS_Checkout_Classic]
GO

-- Ensure CartItemTableType exists
IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'CartItemTableType')
BEGIN
    CREATE TYPE [dbo].[CartItemTableType] AS TABLE
    (
        VariantID BIGINT,
        Qty INT
    )
END
GO

-- Create the stored procedure with DISCOUNT SUPPORT
CREATE PROCEDURE [dbo].[sp_POS_Checkout_Classic]
    @BranchID BIGINT,
    @UserID BIGINT,
    @PaymentMethodID BIGINT,
    @CartItems dbo.CartItemTableType READONLY,
    @PaymentType NVARCHAR(50) = 'cash_atBranch',
    @DiscountID BIGINT = NULL,
    @DiscountAmount DECIMAL(18, 2) = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OrderID BIGINT;
    DECLARE @SubTotal DECIMAL(18, 2) = 0;
    DECLARE @TotalAmount DECIMAL(18, 2) = 0;
    DECLARE @OrderCode NVARCHAR(50);
    DECLARE @ErrorMessage NVARCHAR(500);
    DECLARE @OrderStatus NVARCHAR(50);
    
    -- Set order status based on payment type
    SET @OrderStatus = @PaymentType;
    
    -- Generate unique order code: POS-YYYYMMDD-HHMMSS-RANDOM
    SET @OrderCode = 'POS-' + FORMAT(GETDATE(), 'yyyyMMdd-HHmmss') + '-' + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS NVARCHAR(10));
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Calculate subtotal from cart items
        SELECT @SubTotal = SUM(c.Qty * pv.price)
        FROM @CartItems c
        INNER JOIN product_variants pv ON pv.id = c.VariantID;
        
        IF @SubTotal IS NULL OR @SubTotal <= 0
        BEGIN
            RAISERROR(N'Giỏ hàng không hợp lệ hoặc không có sản phẩm.', 16, 1);
            RETURN;
        END
        
        -- Calculate total with discount
        SET @TotalAmount = @SubTotal - ISNULL(@DiscountAmount, 0);
        IF @TotalAmount < 0 SET @TotalAmount = 0;
        
        -- Check stock availability for all items
        IF EXISTS (
            SELECT 1 
            FROM @CartItems c
            LEFT JOIN branch_inventories bi ON bi.product_variant_id = c.VariantID AND bi.branch_id = @BranchID
            WHERE ISNULL(bi.quantity_on_hand, 0) < c.Qty
        )
        BEGIN
            RAISERROR(N'Không đủ hàng trong kho chi nhánh cho một số sản phẩm.', 16, 1);
            RETURN;
        END
        
        -- Create the purchase order with DISCOUNT
        INSERT INTO purchase_orders (
            user_id,
            branch_id,
            order_code,
            status,
            shipping_recipient_name,
            shipping_recipient_phone,
            shipping_address,
            sub_total,
            shipping_fee,
            discount_amount,
            total_amount,
            discount_id,
            created_at,
            updated_at
        )
        VALUES (
            @UserID,
            @BranchID,
            @OrderCode,
            @OrderStatus,
            N'Khách lẻ tại quầy',
            N'',
            N'Mua tại quầy',
            @SubTotal,
            0,
            ISNULL(@DiscountAmount, 0),
            @TotalAmount,
            @DiscountID,
            GETDATE(),
            GETDATE()
        );
        
        SET @OrderID = SCOPE_IDENTITY();
        
        -- Insert order details
        INSERT INTO purchase_order_details (
            order_id,
            product_variant_id,
            quantity,
            price_at_purchase,
            subtotal,
            created_at
        )
        SELECT 
            @OrderID,
            c.VariantID,
            c.Qty,
            pv.price,
            c.Qty * pv.price,
            GETDATE()
        FROM @CartItems c
        INNER JOIN product_variants pv ON pv.id = c.VariantID;
        
        -- Deduct inventory from branch
        UPDATE bi
        SET bi.quantity_on_hand = bi.quantity_on_hand - c.Qty,
            bi.updated_at = GETDATE()
        FROM branch_inventories bi
        INNER JOIN @CartItems c ON c.VariantID = bi.product_variant_id
        WHERE bi.branch_id = @BranchID;
        
        -- Create payment record
        INSERT INTO payments (
            order_id,
            payment_method_id,
            amount,
            status,
            transaction_code,
            created_at,
            updated_at
        )
        VALUES (
            @OrderID,
            @PaymentMethodID,
            @TotalAmount,
            'Paid',
            @OrderCode,
            GETDATE(),
            GETDATE()
        );
        
        COMMIT TRANSACTION;
        
        -- Return success with order info
        SELECT @OrderID AS OrderID, @OrderCode AS OrderCode, @SubTotal AS SubTotal, @DiscountAmount AS DiscountAmount, @TotalAmount AS TotalAmount, N'Thanh toán thành công!' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @ErrorMessage = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

PRINT N'✓ Stored procedure sp_POS_Checkout_Classic created with VOUCHER support!';
GO

-- =============================================
-- UPDATE EXISTING PAYMENT METHODS (COD and PREPAID)
-- =============================================

-- Update COD to "Tiền mặt tại quầy"
IF EXISTS (SELECT 1 FROM payment_methods WHERE code = 'COD')
BEGIN
    UPDATE payment_methods 
    SET name = N'Tiền mặt tại quầy', 
        is_active = 1, 
        updated_at = GETDATE() 
    WHERE code = 'COD';
    PRINT N'✓ Updated: COD -> Tiền mặt tại quầy';
END
ELSE
BEGIN
    INSERT INTO payment_methods (name, code, is_active, created_at, updated_at)
    VALUES (N'Tiền mặt tại quầy', 'COD', 1, GETDATE(), GETDATE());
    PRINT N'✓ Added: Tiền mặt tại quầy (COD)';
END

-- Update PREPAID to "Thanh toán QR / Chuyển khoản"
IF EXISTS (SELECT 1 FROM payment_methods WHERE code = 'PREPAID')
BEGIN
    UPDATE payment_methods 
    SET name = N'Thanh toán QR / Chuyển khoản', 
        is_active = 1, 
        updated_at = GETDATE() 
    WHERE code = 'PREPAID';
    PRINT N'✓ Updated: PREPAID -> Thanh toán QR / Chuyển khoản';
END
ELSE
BEGIN
    INSERT INTO payment_methods (name, code, is_active, created_at, updated_at)
    VALUES (N'Thanh toán QR / Chuyển khoản', 'PREPAID', 1, GETDATE(), GETDATE());
    PRINT N'✓ Added: Thanh toán QR / Chuyển khoản (PREPAID)';
END
GO

-- Show all active payment methods
PRINT N'';
PRINT N'=== ACTIVE PAYMENT METHODS ===';
SELECT id, name, code, is_active 
FROM payment_methods 
WHERE is_active = 1
ORDER BY id;

PRINT N'';
PRINT N'✓ ALL DONE! Restart IIS Express and test /Branch/POS';
PRINT N'✓ Now supports: Voucher/Discount codes!';
GO
