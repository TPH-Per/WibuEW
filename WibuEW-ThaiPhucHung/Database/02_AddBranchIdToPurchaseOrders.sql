-- =====================================================
-- Script: Add branch_id to purchase_orders
-- Purpose: Link each purchase order to a specific branch
-- Date: 2025-12-15
-- =====================================================

USE [perw];
GO

-- Add branch_id column to purchase_orders table
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.purchase_orders') 
    AND name = 'branch_id'
)
BEGIN
    ALTER TABLE [dbo].[purchase_orders]
    ADD [branch_id] BIGINT NULL;
    
    PRINT 'Column branch_id added to purchase_orders table.';
END
ELSE
BEGIN
    PRINT 'Column branch_id already exists in purchase_orders table.';
END
GO

-- Add foreign key constraint to branches table
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_purchase_orders_branches'
)
BEGIN
    ALTER TABLE [dbo].[purchase_orders]
    ADD CONSTRAINT [FK_purchase_orders_branches]
    FOREIGN KEY ([branch_id]) REFERENCES [dbo].[branches]([id]);
    
    PRINT 'Foreign key FK_purchase_orders_branches created.';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_purchase_orders_branches already exists.';
END
GO

-- Optional: Create index for better performance
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_purchase_orders_branch_id' 
    AND object_id = OBJECT_ID('dbo.purchase_orders')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_purchase_orders_branch_id]
    ON [dbo].[purchase_orders] ([branch_id]);
    
    PRINT 'Index IX_purchase_orders_branch_id created.';
END
GO

PRINT 'Script completed successfully.';
GO
