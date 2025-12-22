-- Script debug cho Inventory calculation
USE perw;
GO

PRINT '=== DEBUG INVENTORY CALCULATION ===';
PRINT '';

-- 1. Kiểm tra dữ liệu trong warehouses
PRINT '1. Dữ liệu trong warehouses:';
SELECT 
    w.id,
    w.name,
    COUNT(i.id) AS InventoryRecords
FROM warehouses w
LEFT JOIN inventories i ON w.id = i.warehouse_id AND i.deleted_at IS NULL
GROUP BY w.id, w.name;
PRINT '';

-- 2. Kiểm tra dữ liệu trong branches
PRINT '2. Dữ liệu trong branches:';
SELECT 
    b.id,
    b.name,
    COUNT(bi.id) AS InventoryRecords
FROM branches b
LEFT JOIN branch_inventories bi ON b.id = bi.branch_id
GROUP BY b.id, b.name;
PRINT '';

-- 3. Tổng tồn kho theo warehouse (giống code)
PRINT '3. Tổng tồn kho theo WAREHOUSE (từ inventories):';
SELECT 
    w.name AS TenKho,
    'Kho Tổng' AS LoaiKho,
    SUM(i.quantity_on_hand) AS TongSoLuongTon,
    SUM(i.quantity_reserved) AS HangDatTruoc
FROM inventories i
INNER JOIN warehouses w ON i.warehouse_id = w.id
WHERE i.deleted_at IS NULL
GROUP BY w.name
ORDER BY w.name;
PRINT '';

-- 4. Tổng tồn kho theo branch (giống code)
PRINT '4. Tổng tồn kho theo BRANCH (từ branch_inventories):';
SELECT 
    b.name AS TenKho,
    'Chi nhánh' AS LoaiKho,
    SUM(bi.quantity_on_hand) AS TongSoLuongTon,
    SUM(bi.quantity_reserved) AS HangDatTruoc
FROM branch_inventories bi
INNER JOIN branches b ON bi.branch_id = b.id
GROUP BY b.name
ORDER BY b.name;
PRINT '';

-- 5. Gộp cả 2 (giống code InventoriesController)
PRINT '5. TỔNG HỢP (Warehouse + Branch):';
SELECT * FROM (
    -- Warehouses
    SELECT 
        w.name AS TenKho,
        'Kho Tổng' AS LoaiKho,
        SUM(i.quantity_on_hand) AS TongSoLuongTon,
        SUM(i.quantity_reserved) AS HangDatTruoc
    FROM inventories i
    INNER JOIN warehouses w ON i.warehouse_id = w.id
    WHERE i.deleted_at IS NULL
    GROUP BY w.name
    
    UNION ALL
    
    -- Branches
    SELECT 
        b.name AS TenKho,
        'Chi nhánh' AS LoaiKho,
        SUM(bi.quantity_on_hand) AS TongSoLuongTon,
        SUM(bi.quantity_reserved) AS HangDatTruoc
    FROM branch_inventories bi
    INNER JOIN branches b ON bi.branch_id = b.id
    GROUP BY b.name
) AS Combined
ORDER BY LoaiKho, TenKho;
PRINT '';

-- 6. Kiểm tra chi tiết inventories
PRINT '6. Chi tiết INVENTORIES (warehouse):';
SELECT 
    w.name AS Warehouse,
    pv.name AS Variant,
    i.quantity_on_hand,
    i.quantity_reserved
FROM inventories i
INNER JOIN warehouses w ON i.warehouse_id = w.id
INNER JOIN product_variants pv ON i.product_variant_id = pv.id
WHERE i.deleted_at IS NULL
ORDER BY w.name, pv.name;
PRINT '';

-- 7. Kiểm tra chi tiết branch_inventories
PRINT '7. Chi tiết BRANCH_INVENTORIES:';
SELECT 
    b.name AS Branch,
    pv.name AS Variant,
    bi.quantity_on_hand,
    bi.quantity_reserved
FROM branch_inventories bi
INNER JOIN branches b ON bi.branch_id = b.id
INNER JOIN product_variants pv ON bi.product_variant_id = pv.id
ORDER BY b.name, pv.name;
PRINT '';

PRINT '=== KẾT THÚC DEBUG ===';
