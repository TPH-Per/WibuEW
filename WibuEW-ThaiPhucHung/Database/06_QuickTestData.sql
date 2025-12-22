-- Insert thêm product variants và inventory cho testing
USE [perw];
GO

-- Check và insert product variants nếu chưa có đủ
IF (SELECT COUNT(*) FROM product_variants) < 10
BEGIN
    PRINT N'Inserting more product variants...';
    
    -- Lấy product_id đầu tiên
    DECLARE @ProductID BIGINT = (SELECT TOP 1 id FROM products);
    
    IF @ProductID IS NOT NULL
    BEGIN
        -- Insert thêm variants
        INSERT INTO product_variants (product_id, name, sku, price, original_price, image_url, created_at, updated_at)
        SELECT TOP 10
            @ProductID,
            CONCAT(N'Sản phẩm test ', ROW_NUMBER() OVER (ORDER BY (SELECT NULL))),
            CONCAT('SKU-TEST-', ROW_NUMBER() OVER (ORDER BY (SELECT NULL))),
            CAST((RAND(CHECKSUM(NEWID())) * 100000 + 10000) AS DECIMAL(18,2)),
            CAST((RAND(CHECKSUM(NEWID())) * 150000 + 15000) AS DECIMAL(18,2)),
            '/images/test.jpg',
            SYSDATETIME(),
            SYSDATETIME()
        FROM sys.objects
        WHERE NOT EXISTS (SELECT 1 FROM product_variants WHERE sku LIKE 'SKU-TEST-%');
        
        PRINT N'✅ Inserted product variants';
    END
END

-- Insert branch inventory cho Branch 1
IF (SELECT COUNT(*) FROM branch_inventories WHERE branch_id = 1) < 10
BEGIN
    PRINT N'Inserting branch inventory...';
    
    INSERT INTO branch_inventories (branch_id,  product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
    SELECT 
        1 as branch_id,
        pv.id as product_variant_id,
        50 as quantity_on_hand,
        0 as quantity_reserved,
        10 as reorder_level,
        SYSDATETIME(),
        SYSDATETIME()
    FROM product_variants pv
    WHERE NOT EXISTS (
        SELECT 1 FROM branch_inventories 
        WHERE branch_id = 1 AND product_variant_id = pv.id
    );
    
    PRINT N'✅ Inserted branch inventory';
END

-- Verify
SELECT COUNT(*) as 'Total Product Variants' FROM product_variants;
SELECT COUNT(*) as 'Branch 1 Inventory Records' FROM branch_inventories WHERE branch_id = 1;

SELECT TOP 5 
    pv.id,
    pv.name,
    pv.sku,
    pv.price,
    bi.quantity_on_hand
FROM product_variants pv
LEFT JOIN branch_inventories bi ON pv.id = bi.product_variant_id AND bi.branch_id = 1
ORDER BY pv.id;

PRINT N'✅ Done!';
GO
