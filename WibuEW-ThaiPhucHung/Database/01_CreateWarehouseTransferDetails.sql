-- =============================================
-- Tạo bảng warehouse_transfer_details
-- Lưu chi tiết sản phẩm trong mỗi phiếu chuyển kho
-- =============================================

USE [perw];
GO

-- Kiểm tra và tạo bảng nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[warehouse_transfer_details]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[warehouse_transfer_details] (
        [id] BIGINT IDENTITY(1,1) NOT NULL,
        [transfer_id] BIGINT NOT NULL,
        [product_variant_id] BIGINT NOT NULL,
        [quantity] INT NOT NULL,
        [notes] NVARCHAR(500) NULL,
        [created_at] DATETIME2 NULL DEFAULT SYSDATETIME(),
        [updated_at] DATETIME2 NULL DEFAULT SYSDATETIME(),
        [deleted_at] DATETIME2 NULL,
        
        CONSTRAINT [PK_warehouse_transfer_details] PRIMARY KEY ([id]),
        CONSTRAINT [FK_warehouse_transfer_details_transfers] 
            FOREIGN KEY ([transfer_id]) REFERENCES [warehouse_transfers]([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_warehouse_transfer_details_product_variants] 
            FOREIGN KEY ([product_variant_id]) REFERENCES [product_variants]([id]),
        CONSTRAINT [CHK_warehouse_transfer_details_quantity] CHECK ([quantity] > 0)
    );

    -- Indexes
    CREATE INDEX [IX_warehouse_transfer_details_transfer_id] 
        ON [warehouse_transfer_details]([transfer_id]);
        
    CREATE INDEX [IX_warehouse_transfer_details_product_variant_id] 
        ON [warehouse_transfer_details]([product_variant_id]);

    PRINT N'✅ Bảng warehouse_transfer_details đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT N'⚠️ Bảng warehouse_transfer_details đã tồn tại.';
END
GO
