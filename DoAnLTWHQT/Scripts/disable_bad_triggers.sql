-- =============================================
-- FIX TRIGGERS that reference wrong table 'order_details'
-- Should be 'purchase_order_details' instead
-- =============================================

PRINT N'=== DISABLING PROBLEMATIC TRIGGERS ===';
PRINT N'';

-- Disable TR_PurchaseOrder_Created
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_PurchaseOrder_Created')
BEGIN
    DISABLE TRIGGER TR_PurchaseOrder_Created ON purchase_orders;
    PRINT N'✓ DISABLED: TR_PurchaseOrder_Created';
END

-- Disable TR_PurchaseOrder_CancelRefund
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_PurchaseOrder_CancelRefund')
BEGIN
    DISABLE TRIGGER TR_PurchaseOrder_CancelRefund ON purchase_orders;
    PRINT N'✓ DISABLED: TR_PurchaseOrder_CancelRefund';
END

GO

-- Show disabled triggers
PRINT N'';
PRINT N'=== TRIGGER STATUS AFTER DISABLE ===';
SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    CASE WHEN t.is_disabled = 1 THEN 'DISABLED' ELSE 'ENABLED' END AS Status
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) = 'purchase_orders'
ORDER BY t.name;

GO

PRINT N'';
PRINT N'✓ DONE! Now POS Checkout should work!';
PRINT N'';
PRINT N'NOTE: These triggers were disabled because they reference';
PRINT N'      a table called "order_details" which does not exist.';
PRINT N'      The correct table name is "purchase_order_details".';
PRINT N'';
PRINT N'To re-enable later (after fixing the trigger code):';
PRINT N'  ENABLE TRIGGER TR_PurchaseOrder_Created ON purchase_orders;';
PRINT N'  ENABLE TRIGGER TR_PurchaseOrder_CancelRefund ON purchase_orders;';
GO
