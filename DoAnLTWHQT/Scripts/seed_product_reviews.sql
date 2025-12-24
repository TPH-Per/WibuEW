-- =============================================
-- Script: Insert Sample Product Reviews
-- Database: perw
-- =============================================

USE perw;
GO

-- Clear existing reviews (optional)
-- DELETE FROM product_reviews;

-- Insert sample reviews
-- Note: user_id and product_id must exist in users and products tables
-- Adjust the IDs based on your actual data

-- Get some existing user and product IDs
DECLARE @user1 BIGINT = (SELECT TOP 1 id FROM users WHERE deleted_at IS NULL AND role_id = 2); -- Customer
DECLARE @user2 BIGINT = (SELECT TOP 1 id FROM users WHERE deleted_at IS NULL AND role_id = 2 AND id <> @user1);
DECLARE @user3 BIGINT = (SELECT TOP 1 id FROM users WHERE deleted_at IS NULL AND role_id = 2 AND id NOT IN (@user1, @user2));

-- If no users found, use default IDs
SET @user1 = ISNULL(@user1, 1);
SET @user2 = ISNULL(@user2, 2);
SET @user3 = ISNULL(@user3, 3);

-- Insert reviews for product 17 (and other products)
-- Product 17
IF NOT EXISTS (SELECT 1 FROM product_reviews WHERE user_id = @user1 AND product_id = 17)
BEGIN
    INSERT INTO product_reviews (user_id, product_id, rating, comment, is_approved, status, created_at)
    VALUES (@user1, 17, 5, N'Sản phẩm tuyệt vời! Chất lượng rất tốt, đóng gói cẩn thận. Sẽ mua lại lần sau.', 1, 'approved', DATEADD(DAY, -5, GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM product_reviews WHERE user_id = @user2 AND product_id = 17)
BEGIN
    INSERT INTO product_reviews (user_id, product_id, rating, comment, is_approved, status, created_at)
    VALUES (@user2, 17, 4, N'Sản phẩm đẹp, giao hàng nhanh. Tuy nhiên hộp bị móp một chút. Nhìn chung vẫn hài lòng.', 1, 'approved', DATEADD(DAY, -3, GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM product_reviews WHERE user_id = @user3 AND product_id = 17)
BEGIN
    INSERT INTO product_reviews (user_id, product_id, rating, comment, is_approved, status, created_at)
    VALUES (@user3, 17, 5, N'Mô hình chi tiết đẹp, sơn màu chuẩn. Rất đáng tiền!', 1, 'approved', DATEADD(DAY, -1, GETDATE()));
END

-- Product 18
IF NOT EXISTS (SELECT 1 FROM product_reviews WHERE user_id = @user1 AND product_id = 18)
BEGIN
    INSERT INTO product_reviews (user_id, product_id, rating, comment, is_approved, status, created_at)
    VALUES (@user1, 18, 4, N'Sản phẩm ổn, đúng như mô tả. Giao hàng hơi lâu nhưng đóng gói kỹ.', 1, 'approved', DATEADD(DAY, -7, GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM product_reviews WHERE user_id = @user2 AND product_id = 18)
BEGIN
    INSERT INTO product_reviews (user_id, product_id, rating, comment, is_approved, status, created_at)
    VALUES (@user2, 18, 5, N'Xuất sắc! Đây là figure đẹp nhất tôi từng mua. Recommend cho mọi người.', 1, 'approved', DATEADD(DAY, -2, GETDATE()));
END

-- Product 19
IF NOT EXISTS (SELECT 1 FROM product_reviews WHERE user_id = @user1 AND product_id = 19)
BEGIN
    INSERT INTO product_reviews (user_id, product_id, rating, comment, is_approved, status, created_at)
    VALUES (@user1, 19, 3, N'Sản phẩm tạm được, màu sắc hơi khác so với hình mẫu. Giá cả hợp lý.', 1, 'approved', DATEADD(DAY, -10, GETDATE()));
END

-- Verify inserted data
SELECT 
    pr.user_id,
    u.full_name AS UserName,
    pr.product_id,
    p.name AS ProductName,
    pr.rating,
    pr.comment,
    pr.is_approved,
    pr.status,
    pr.created_at
FROM product_reviews pr
LEFT JOIN users u ON pr.user_id = u.id
LEFT JOIN products p ON pr.product_id = p.id
WHERE pr.product_id IN (17, 18, 19)
ORDER BY pr.product_id, pr.created_at DESC;

PRINT 'Sample reviews inserted successfully!';
GO
