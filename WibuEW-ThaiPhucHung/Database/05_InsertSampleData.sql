-- =============================================
-- Insert Sample Data cho Testing
-- Thứ tự: Roles → Users → Warehouses → Branches → Categories → Suppliers → Products → Variants → Payment Methods → Inventories
-- =============================================

USE [perw];
GO

PRINT N'Bắt đầu insert dữ liệu mẫu...';
PRINT N'';

-- =============================================
-- 1. Payment Methods
-- =============================================
IF NOT EXISTS (SELECT 1 FROM payment_methods WHERE code = 'CASH')
BEGIN
    INSERT INTO payment_methods (name, code, is_active, created_at, updated_at)
    VALUES 
        (N'Tiền mặt', 'CASH', 1, SYSDATETIME(), SYSDATETIME()),
        (N'Chuyển khoản', 'BANK_TRANSFER', 1, SYSDATETIME(), SYSDATETIME()),
        (N'Thẻ tín dụng', 'CREDIT_CARD', 1, SYSDATETIME(), SYSDATETIME()),
        (N'Ví điện tử', 'E_WALLET', 1, SYSDATETIME(), SYSDATETIME());
    
    PRINT N'✅ Đã insert Payment Methods';
END
GO

-- =============================================
-- 2. Warehouses
-- =============================================
IF NOT EXISTS (SELECT 1 FROM warehouses WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT warehouses ON;
    
    INSERT INTO warehouses (id, name, location, created_at, updated_at)
    VALUES 
        (1, N'Kho Trung Tâm Hà Nội', N'Số 1 Đại Cồ Việt, Hai Bà Trưng, Hà Nội', SYSDATETIME(), SYSDATETIME()),
        (2, N'Kho Trung Tâm TP.HCM', N'Số 268 Lý Thường Kiệt, Quận 10, TP.HCM', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT warehouses OFF;
    
    PRINT N'✅ Đã insert Warehouses';
END
GO

-- =============================================
-- 3. Branches
-- =============================================
IF NOT EXISTS (SELECT 1 FROM branches WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT branches ON;
    
    INSERT INTO branches (id, name, warehouse_id, location, created_at, updated_at)
    VALUES 
        (1, N'Chi Nhánh Quận 1', 1, N'Số 123 Nguyễn Huệ, Quận 1, TP.HCM', SYSDATETIME(), SYSDATETIME()),
        (2, N'Chi Nhánh Hà Đông', 1, N'Số 456 Quang Trung, Hà Đông, Hà Nội', SYSDATETIME(), SYSDATETIME()),
        (3, N'Chi Nhánh Thủ Đức', 2, N'Số 789 Võ Văn Ngân, Thủ Đức, TP.HCM', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT branches OFF;
    
    PRINT N'✅ Đã insert Branches';
END
GO

-- =============================================
-- 4. Categories
-- =============================================
IF NOT EXISTS (SELECT 1 FROM categories WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT categories ON;
    
    INSERT INTO categories (id, name, slug, created_at, updated_at)
    VALUES 
        (1, N'Sách', 'sach', SYSDATETIME(), SYSDATETIME()),
        (2, N'Sách Văn Học', 'sach-van-hoc', SYSDATETIME(), SYSDATETIME()),
        (3, N'Sách Kỹ Năng', 'sach-ky-nang', SYSDATETIME(), SYSDATETIME()),
        (4, N'Sách Thiếu Nhi', 'sach-thieu-nhi', SYSDATETIME(), SYSDATETIME()),
        (5, N'Văn Phòng Phẩm', 'van-phong-pham', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT categories OFF;
    
    PRINT N'✅ Đã insert Categories';
END
ELSE
BEGIN
    PRINT N'⚠ Categories đã tồn tại, bỏ qua';
END
GO

-- =============================================
-- 5. Suppliers
-- =============================================
IF NOT EXISTS (SELECT 1 FROM suppliers WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT suppliers ON;
    
    INSERT INTO suppliers (id, name, contact_info, created_at, updated_at)
    VALUES 
        (1, N'NXB Kim Đồng', N'SĐT: 024-3943-4730, Email: info@nxbkimdong.com.vn', SYSDATETIME(), SYSDATETIME()),
        (2, N'NXB Trẻ', N'SĐT: 028-3930-4943, Email: contact@nxbtre.com.vn', SYSDATETIME(), SYSDATETIME()),
        (3, N'Công ty Thiên Long', N'SĐT: 024-3872-6153, Email: sales@thienlong.vn', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT suppliers OFF;
    
    PRINT N'✅ Đã insert Suppliers';
END
GO

-- =============================================
-- 6. Products
-- =============================================
IF NOT EXISTS (SELECT 1 FROM products WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT products ON;
    
    INSERT INTO products (id, category_id, supplier_id, name, description, slug, status, created_at, updated_at)
    VALUES 
        (1, 2, 1, N'Nhà Giả Kim', N'Tác phẩm nổi tiếng của Paulo Coelho', 'nha-gia-kim', 'published', SYSDATETIME(), SYSDATETIME()),
        (2, 2, 2, N'Đắc Nhân Tâm', N'Sách kỹ năng sống của Dale Carnegie', 'dac-nhan-tam', 'published', SYSDATETIME(), SYSDATETIME()),
        (3, 4, 1, N'Doraemon Tập 1', N'Truyện tranh thiếu nhi Doraemon', 'doraemon-tap-1', 'published', SYSDATETIME(), SYSDATETIME()),
        (4, 3, 2, N'Tuổi Trẻ Đáng Giá Bao Nhiêu', N'Sách kỹ năng cho tuổi trẻ', 'tuoi-tre-dang-gia-bao-nhieu', 'published', SYSDATETIME(), SYSDATETIME()),
        (5, 5, 3, N'Bút Bi Thiên Long', N'Bút bi văn phòng chất lượng cao', 'but-bi-thien-long', 'published', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT products OFF;
    
    PRINT N'✅ Đã insert Products';
END
GO

-- =============================================
-- 7. Product Variants
-- =============================================
IF NOT EXISTS (SELECT 1 FROM product_variants WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT product_variants ON;
    
    INSERT INTO product_variants (id, product_id, name, sku, price, original_price, image_url, created_at, updated_at)
    VALUES 
        -- Nhà Giả Kim
        (1, 1, N'Nhà Giả Kim - Bìa Cứng', 'NKG-BC-001', 89000, 120000, '/images/nha-gia-kim.jpg', SYSDATETIME(), SYSDATETIME()),
        (2, 1, N'Nhà Giả Kim - Bìa Mềm', 'NKG-BM-001', 65000, 85000, '/images/nha-gia-kim-mem.jpg', SYSDATETIME(), SYSDATETIME()),
        
        -- Đắc Nhân Tâm
        (3, 2, N'Đắc Nhân Tâm - Bản Đặc Biệt', 'DNT-DB-001', 95000, 130000, '/images/dac-nhan-tam.jpg', SYSDATETIME(), SYSDATETIME()),
        (4, 2, N'Đắc Nhân Tâm - Bản Thường', 'DNT-BT-001', 70000, 95000, '/images/dac-nhan-tam-thuong.jpg', SYSDATETIME(), SYSDATETIME()),
        
        -- Doraemon
        (5, 3, N'Doraemon Tập 1', 'DRM-T1-001', 25000, 30000, '/images/doraemon-1.jpg', SYSDATETIME(), SYSDATETIME()),
        (6, 3, N'Doraemon Tập 1 - Tái Bản', 'DRM-T1-TB-001', 28000, 35000, '/images/doraemon-1-tb.jpg', SYSDATETIME(), SYSDATETIME()),
        
        -- Tuổi Trẻ
        (7, 4, N'Tuổi Trẻ Đáng Giá Bao Nhiêu', 'TTDGBN-001', 79000, 110000, '/images/tuoi-tre.jpg', SYSDATETIME(), SYSDATETIME()),
        
        -- Bút
        (8, 5, N'Bút Bi Thiên Long - Xanh', 'BUT-TL-X-001', 5000, 7000, '/images/but-xanh.jpg', SYSDATETIME(), SYSDATETIME()),
        (9, 5, N'Bút Bi Thiên Long - Đỏ', 'BUT-TL-D-001', 5000, 7000, '/images/but-do.jpg', SYSDATETIME(), SYSDATETIME()),
        (10, 5, N'Bút Bi Thiên Long - Đen', 'BUT-TL-DE-001', 5000, 7000, '/images/but-den.jpg', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT product_variants OFF;
    
    PRINT N'✅ Đã insert Product Variants';
END
GO

-- =============================================
-- 8. Inventories (Kho Trung Tâm)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM inventories WHERE warehouse_id = 1)
BEGIN
    INSERT INTO inventories (product_variant_id, warehouse_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
    VALUES 
        -- Kho Trung Tâm Hà Nội
        (1, 1, 100, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (2, 1, 150, 0, 15, SYSDATETIME(), SYSDATETIME()),
        (3, 1, 80, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (4, 1, 120, 0, 12, SYSDATETIME(), SYSDATETIME()),
        (5, 1, 200, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (6, 1, 180, 0, 18, SYSDATETIME(), SYSDATETIME()),
        (7, 1, 90, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (8, 1, 500, 0, 50, SYSDATETIME(), SYSDATETIME()),
        (9, 1, 500, 0, 50, SYSDATETIME(), SYSDATETIME()),
        (10, 1, 500, 0, 50, SYSDATETIME(), SYSDATETIME()),
        
        -- Kho Trung Tâm TP.HCM
        (1, 2, 80, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (2, 2, 120, 0, 15, SYSDATETIME(), SYSDATETIME()),
        (3, 2, 70, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (4, 2, 100, 0, 12, SYSDATETIME(), SYSDATETIME()),
        (5, 2, 150, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (6, 2, 140, 0, 18, SYSDATETIME(), SYSDATETIME()),
        (7, 2, 75, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (8, 2, 400, 0, 50, SYSDATETIME(), SYSDATETIME()),
        (9, 2, 400, 0, 50, SYSDATETIME(), SYSDATETIME()),
        (10, 2, 400, 0, 50, SYSDATETIME(), SYSDATETIME());
    
    PRINT N'✅ Đã insert Inventories';
END
GO

-- =============================================
-- 9. Branch Inventories (Kho Chi Nhánh)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM branch_inventories WHERE branch_id = 1)
BEGIN
    INSERT INTO branch_inventories (branch_id, product_variant_id, quantity_on_hand, quantity_reserved, reorder_level, created_at, updated_at)
    VALUES 
        -- Chi Nhánh Quận 1
        (1, 1, 25, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (1, 2, 30, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (1, 3, 20, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (1, 4, 25, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (1, 5, 40, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (1, 6, 35, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (1, 7, 18, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (1, 8, 100, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (1, 9, 100, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (1, 10, 100, 0, 20, SYSDATETIME(), SYSDATETIME()),
        
        -- Chi Nhánh Hà Đông
        (2, 1, 20, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (2, 2, 25, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (2, 3, 15, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (2, 4, 20, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (2, 5, 30, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (2, 6, 28, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (2, 7, 15, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (2, 8, 80, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (2, 9, 80, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (2, 10, 80, 0, 20, SYSDATETIME(), SYSDATETIME()),
        
        -- Chi Nhánh Thủ Đức
        (3, 1, 15, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (3, 2, 20, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (3, 3, 12, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (3, 4, 15, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (3, 5, 25, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (3, 6, 22, 0, 10, SYSDATETIME(), SYSDATETIME()),
        (3, 7, 12, 0, 5, SYSDATETIME(), SYSDATETIME()),
        (3, 8, 60, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (3, 9, 60, 0, 20, SYSDATETIME(), SYSDATETIME()),
        (3, 10, 60, 0, 20, SYSDATETIME(), SYSDATETIME());
    
    PRINT N'✅ Đã insert Branch Inventories';
END
GO

-- =============================================
-- 10. Users (Thêm user test)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM users WHERE id = 1)
BEGIN
    SET IDENTITY_INSERT users ON;
    
    -- Giả sử roles đã có: 1=admin, 2=warehouse_manager, 3=branch_manager, 4=client
    INSERT INTO users (id, name, full_name, email, password, role_id, warehouse_id, phone_number, status, created_at, updated_at)
    VALUES 
        (1, 'admin', N'Quản Trị Viên', 'admin@bookstore.vn', '$2a$11$abc123...', 1, NULL, '0901234567', 'active', SYSDATETIME(), SYSDATETIME()),
        (2, 'branch1_staff', N'Nhân Viên CN Quận 1', 'staff.q1@bookstore.vn', '$2a$11$abc123...', 3, NULL, '0902345678', 'active', SYSDATETIME(), SYSDATETIME()),
        (3, 'branch2_staff', N'Nhân Viên CN Hà Đông', 'staff.hadong@bookstore.vn', '$2a$11$abc123...', 3, NULL, '0903456789', 'active', SYSDATETIME(), SYSDATETIME());
    
    SET IDENTITY_INSERT users OFF;
    
    PRINT N'✅ Đã insert Users';
END
GO

PRINT N'';
PRINT N'========================================';
PRINT N'✅ HOÀN THÀNH INSERT DỮ LIỆU MẪU';
PRINT N'========================================';
PRINT N'';
PRINT N'Tổng kết:';
PRINT N'  • Payment Methods: 4';
PRINT N'  • Warehouses: 2';
PRINT N'  • Branches: 3';
PRINT N'  • Categories: 5';
PRINT N'  • Suppliers: 3';
PRINT N'  • Products: 5';
PRINT N'  • Product Variants: 10';
PRINT N'  • Inventories: 20 (2 kho)';
PRINT N'  • Branch Inventories: 30 (3 chi nhánh)';
PRINT N'  • Users: 3';
PRINT N'';
PRINT N'Hệ thống sẵn sàng để test POS! 🛒';
PRINT N'';
GO
