-- Script to add warehouse_id column to users table
-- This is needed because EDMX expects this column but database doesn't have it

-- Step 1: Add the column
ALTER TABLE [dbo].[users]
ADD [warehouse_id] [bigint] NULL;
GO

-- Step 2: Add foreign key constraint (optional, but recommended for data integrity)
ALTER TABLE [dbo].[users]
ADD CONSTRAINT [FK_users_warehouses] 
FOREIGN KEY ([warehouse_id]) 
REFERENCES [dbo].[warehouses] ([id]);
GO

PRINT 'warehouse_id column added to users table successfully!';
