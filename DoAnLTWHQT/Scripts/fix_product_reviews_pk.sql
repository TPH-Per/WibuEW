-- =============================================
-- Script: Add Primary Key to product_reviews
-- Purpose: Fix Entity Framework "read-only" issue
-- =============================================

-- Check if Primary Key already exists
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'product_reviews' AND CONSTRAINT_TYPE = 'PRIMARY KEY'
)
BEGIN
    -- Add composite primary key on user_id + product_id
    ALTER TABLE product_reviews
    ADD CONSTRAINT PK_product_reviews PRIMARY KEY (user_id, product_id);
    
    PRINT 'Primary Key added successfully to product_reviews';
END
ELSE
BEGIN
    PRINT 'Primary Key already exists on product_reviews';
END
GO

-- Verify the change
SELECT 
    tc.CONSTRAINT_NAME,
    kcu.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.TABLE_NAME = 'product_reviews' 
    AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY';
GO
