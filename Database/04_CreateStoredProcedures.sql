-- =============================================
-- Stored Procedures cho Warehouse Transfers và POS
-- =============================================

USE [perw];
GO

-- =============================================
-- 1. Procedure: Xử lý hàng lỗi/hỏng tại chi nhánh
-- =============================================

IF OBJECT_ID('sp_ProcessTransferIssue', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_ProcessTransferIssue];
GO

CREATE PROCEDURE [dbo].[sp_ProcessTransferIssue]
    @BranchID BIGINT,
    @TransferID BIGINT,       
    @VariantID BIGINT,        
    @BadQty INT,            
    @Note NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra có đủ hàng để trừ không
        IF NOT EXISTS (
            SELECT 1 
            FROM branch_inventories 
            WHERE branch_id = @BranchID 
                AND product_variant_id = @VariantID
                AND quantity_on_hand >= @BadQty
        )
        BEGIN
            RAISERROR(N'Không đủ hàng trong kho chi nhánh để xử lý.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Trừ hàng lỗi khỏi kho chi nhánh
        UPDATE branch_inventories
        SET quantity_on_hand = quantity_on_hand - @BadQty,
            updated_at = SYSDATETIME()
        WHERE branch_id = @BranchID 
            AND product_variant_id = @VariantID;

        -- Log vào bảng ghi chú hoặc inventory_transactions (tùy chọn)
        -- INSERT INTO inventory_issue_log ...

        COMMIT TRANSACTION;
        
        PRINT N'✅ Đã xử lý ' + CAST(@BadQty AS NVARCHAR) + N' sản phẩm lỗi/hỏng.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- =============================================
-- 2. Procedure: POS Checkout Classic
-- Thanh toán tại quầy, tự động trừ hàng và tạo đơn
-- =============================================

IF OBJECT_ID('sp_POS_Checkout_Classic', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_POS_Checkout_Classic];
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_POS_Checkout_Classic]
    @BranchID BIGINT,
    @UserID BIGINT,
    @PaymentMethodID BIGINT,
    @CartItems [dbo].[CartItemTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Cur_VariantID BIGINT;
    DECLARE @Cur_Qty INT;
    DECLARE @Cur_Price DECIMAL(18,2);
    DECLARE @TotalAmount DECIMAL(18,2) = 0;
    DECLARE @NewOrderID BIGINT;
    DECLARE @OrderCode NVARCHAR(50);
    DECLARE @SubTotal DECIMAL(18,2);

    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- A. Tính tổng tiền
        SELECT @TotalAmount = SUM(c.Qty * p.price)
        FROM @CartItems c
        JOIN product_variants p ON c.VariantID = p.id;

        IF @TotalAmount IS NULL OR @TotalAmount = 0
        BEGIN 
            ;THROW 50001, N'Giỏ hàng trống hoặc không hợp lệ.', 1;
        END
        
        -- B. Tạo mã đơn hàng
        SET @OrderCode = 'POS-' + CAST(@BranchID AS NVARCHAR) + '-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss');

        -- C. Tạo đơn hàng
        INSERT INTO purchase_orders (
            user_id, 
            branch_id, 
            order_code, 
            status, 
            sub_total, 
            shipping_fee, 
            discount_amount, 
            total_amount, 
            created_at
        )
        VALUES (
            @UserID, 
            @BranchID, 
            @OrderCode, 
            'completed', 
            @TotalAmount, 
            0, 
            0, 
            @TotalAmount, 
            SYSDATETIME()
        );
        
        SET @NewOrderID = SCOPE_IDENTITY();

        -- D. Duyệt từng sản phẩm trong giỏ
        DECLARE cart_cursor CURSOR FOR 
        SELECT c.VariantID, c.Qty, p.price
        FROM @CartItems c
        JOIN product_variants p ON c.VariantID = p.id;

        OPEN cart_cursor;
        FETCH NEXT FROM cart_cursor INTO @Cur_VariantID, @Cur_Qty, @Cur_Price;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Kiểm tra đủ hàng
            IF NOT EXISTS (
                SELECT 1 
                FROM branch_inventories 
                WHERE branch_id = @BranchID 
                    AND product_variant_id = @Cur_VariantID 
                    AND quantity_on_hand >= @Cur_Qty
            )
            BEGIN
                CLOSE cart_cursor; 
                DEALLOCATE cart_cursor;
                ;THROW 50002, N'Không đủ hàng trong kho chi nhánh.', 1;
            END
            
            -- Tính subtotal
            SET @SubTotal = @Cur_Qty * @Cur_Price;

            -- Insert chi tiết đơn hàng
            INSERT INTO purchase_order_details (
                order_id, 
                product_variant_id, 
                quantity, 
                price_at_purchase, 
                subtotal,
                created_at
            )
            VALUES (
                @NewOrderID, 
                @Cur_VariantID, 
                @Cur_Qty, 
                @Cur_Price, 
                @SubTotal,
                SYSDATETIME()
            );

            -- Trừ hàng khỏi kho chi nhánh
            UPDATE branch_inventories
            SET quantity_on_hand = quantity_on_hand - @Cur_Qty,
                updated_at = SYSDATETIME()
            WHERE branch_id = @BranchID 
                AND product_variant_id = @Cur_VariantID;

            FETCH NEXT FROM cart_cursor INTO @Cur_VariantID, @Cur_Qty, @Cur_Price;
        END

        CLOSE cart_cursor;
        DEALLOCATE cart_cursor;

        -- E. Tạo thanh toán
        INSERT INTO payments (
            order_id, 
            payment_method_id, 
            amount, 
            status, 
            created_at
        )
        VALUES (
            @NewOrderID, 
            @PaymentMethodID, 
            @TotalAmount, 
            'completed', 
            SYSDATETIME()
        );

        COMMIT TRANSACTION;
        
        PRINT N'✅ Thanh toán thành công. Mã đơn: ' + @OrderCode;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 
            ROLLBACK TRANSACTION;
        
        -- Close cursor nếu còn mở
        IF CURSOR_STATUS('global','cart_cursor') >= -1
        BEGIN
            CLOSE cart_cursor;
            DEALLOCATE cart_cursor;
        END
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT N'';
PRINT N'========================================';
PRINT N'✅ TẤT CẢ STORED PROCEDURES ĐÃ ĐƯỢC TẠO';
PRINT N'========================================';
PRINT N'- sp_ProcessTransferIssue';
PRINT N'- sp_POS_Checkout_Classic';
PRINT N'';
GO
