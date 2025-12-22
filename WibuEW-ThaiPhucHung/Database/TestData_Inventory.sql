-- Script tạo dữ liệu test cho Inventory Management
-- Chạy trong SQL Server Management Studio

USE perw;
GO

PRINT '=== TẠO DỮ LIỆU TEST CHO INVENTORY ===';
PRINT '';

-- Kiểm tra dữ liệu hiện tại
PRINT '1. Kiểm tra dữ liệu hiện tại:';
SELECT 
    --(SELECT COUNT(*) FROM inventory) AS InventoryCount,
    (SELECT COUNT(*) FROM branch_inventories) AS BranchInventoryCount,
    (SELECT COUNT(*) FROM warehouses) AS WarehouseCount,
    (SELECT COUNT(*) FROM branches) AS BranchCount;
PRINT '';

-- Thêm warehouse nếu chưa có
IF NOT EXISTS (SELECT 1 FROM warehouses WHERE name = 'Kho Trung Tâm')
BEGIN
    PRINT '2. Thêm warehouse...';
    INSERT INTO warehouses (name, location, created_at, updated_at)
    VALUES ('Kho Trung Tâm', 'Quận 1, TP.HCM', GETDATE(), GETDATE());
    PRINT '   ✓ Đã thêm warehouse';
END
ELSE
BEGIN
    PRINT '2. Warehouse đã tồn tại';
END
PRINT '';

-- Lấy IDs
DECLARE @warehouseId BIGINT = (SELECT TOP 1 id FROM warehouses WHERE name = 'Kho Trung Tâm');
DECLARE @branch1Id BIGINT = (SELECT TOP 1 id FROM branches ORDER BY id);
DECLARE @branch2Id BIGINT = (SELECT id FROM branches WHERE id > @branch1Id ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY);

PRINT '3. IDs được sử dụng:';
PRINT '   Warehouse ID: ' + CAST(@warehouseId AS VARCHAR);
PRINT '   Branch 1 ID: ' + CAST(@branch1Id AS VARCHAR);
PRINT '   Branch 2 ID: ' + CAST(ISNULL(@branch2Id, 0) AS VARCHAR);
PRINT '';

-- Lấy product variant IDs
DECLARE @variant1 BIGINT = (SELECT TOP 1 id FROM product_variants ORDER BY id);
DECLARE @variant2 BIGINT = (SELECT id FROM product_variants WHERE id > @variant1 ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY);
DECLARE @variant3 BIGINT = (SELECT id FROM product_variants WHERE id > @variant2 ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY);
DECLARE @variant4 BIGINT = (SELECT id FROM product_variants WHERE id > @variant3 ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY);
DECLARE @variant5 BIGINT = (SELECT id FROM product_variants WHERE id > @variant4 ORDER BY id OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY);

-- Thêm inventory (tồn kho tại kho trung tâm)
PRINT '4. Thêm inventory (kho trung tâm)...';

-- Xóa dữ liệu cũ nếu có
DELETE FROM inventory WHERE warehouse_id = @warehouseId;

INSERT INTO inventory (product_variant_id, warehouse_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
VALUES
    (@variant1, @warehouseId, 100, 10, 20, GETDATE(), GETDATE()),
    (@variant2, @warehouseId, 150, 15, 30, GETDATE(), GETDATE()),
    (@variant3, @warehouseId, 80, 5, 25, GETDATE(), GETDATE()),
    (@variant4, @warehouseId, 200, 20, 40, GETDATE(), GETDATE()),
    (@variant5, @warehouseId, 50, 5, 30, GETDATE(), GETDATE());

PRINT '   ✓ Đã thêm ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records vào inventory';
PRINT '';

-- Thêm branch_inventories (tồn kho tại chi nhánh)
PRINT '5. Thêm branch_inventories (chi nhánh)...';

-- Xóa dữ liệu cũ nếu có
DELETE FROM branch_inventories WHERE branch_id IN (@branch1Id, @branch2Id);

-- Chi nhánh 1
INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
VALUES
    (@branch1Id, @variant1, 25, 3, 10, GETDATE(), GETDATE()),
    (@branch1Id, @variant2, 30, 5, 15, GETDATE(), GETDATE()),
    (@branch1Id, @variant3, 15, 2, 10, GETDATE(), GETDATE()),
    (@branch1Id, @variant4, 40, 8, 20, GETDATE(), GETDATE()),
    (@branch1Id, @variant5, 8, 1, 15, GETDATE(), GETDATE()); -- Low stock

DECLARE @branch1Count INT = @@ROWCOUNT;

-- Chi nhánh 2 (nếu có)
IF @branch2Id IS NOT NULL
BEGIN
    INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
    VALUES
        (@branch2Id, @variant1, 20, 2, 10, GETDATE(), GETDATE()),
        (@branch2Id, @variant2, 35, 7, 15, GETDATE(), GETDATE()),
        (@branch2Id, @variant3, 18, 3, 10, GETDATE(), GETDATE()),
        (@branch2Id, @variant4, 45, 10, 20, GETDATE(), GETDATE()),
        (@branch2Id, @variant5, 12, 2, 15, GETDATE(), GETDATE());
    
    PRINT '   ✓ Đã thêm ' + CAST(@branch1Count + @@ROWCOUNT AS VARCHAR) + ' records vào branch_inventories';
END
ELSE
BEGIN
    PRINT '   ✓ Đã thêm ' + CAST(@branch1Count AS VARCHAR) + ' records vào branch_inventories (chỉ có 1 chi nhánh)';
END
PRINT '';

-- Kiểm tra kết quả
PRINT '6. Kết quả sau khi thêm:';
SELECT 
    (SELECT COUNT(*) FROM inventory WHERE warehouse_id = @warehouseId) AS InventoryCount,
    (SELECT COUNT(*) FROM branch_inventories WHERE branch_id IN (@branch1Id, @branch2Id)) AS BranchInventoryCount;
PRINT '';

-- Hiển thị dữ liệu mẫu
PRINT '7. Dữ liệu mẫu - Inventory (Kho Trung Tâm):';
SELECT TOP 5
    pv.name AS VariantName,
    p.name AS ProductName,
    i.quantity_on_hand AS OnHand,
    i.quantity_reserved AS Reserved,
    i.reorder_level AS ReorderLevel
FROM inventory i
INNER JOIN product_variants pv ON i.product_variant_id = pv.id
INNER JOIN product p ON pv.product_id = p.id
WHERE i.warehouse_id = @warehouseId
ORDER BY p.name;
PRINT '';

PRINT '8. Dữ liệu mẫu - Branch Inventories:';
SELECT TOP 10
    b.name AS BranchName,
    p.name AS ProductName,
    pv.name AS VariantName,
    bi.quantity_on_hand AS OnHand,
    bi.quantity_reserved AS Reserved,
    bi.reorder_level AS ReorderLevel,
    CASE 
        WHEN bi.quantity_on_hand < bi.reorder_level THEN 'LOW STOCK'
        ELSE 'OK'
    END AS Status
FROM branch_inventories bi
INNER JOIN branches b ON bi.branch_id = b.id
INNER JOIN product_variants pv ON bi.product_variant_id = pv.id
INNER JOIN product p ON pv.product_id = p.id
WHERE bi.branch_id IN (@branch1Id, @branch2Id)
ORDER BY b.name, p.name;
PRINT '';

PRINT '=== HOÀN THÀNH ===';
PRINT 'Bạn có thể test các URL sau:';
PRINT '- /Admin/BranchInventories/Index';
PRINT '- /Admin/BranchInventories/Details/' + CAST(@branch1Id AS VARCHAR);
IF @branch2Id IS NOT NULL
    PRINT '- /Admin/BranchInventories/Details/' + CAST(@branch2Id AS VARCHAR);
