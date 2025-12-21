-- =============================================
-- STEP 1: Find triggers that reference 'order_details'
-- =============================================
PRINT N'=== LOOKING FOR TRIGGERS THAT REFERENCE order_details ===';

SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    m.definition AS TriggerDefinition
FROM sys.triggers t
INNER JOIN sys.sql_modules m ON t.object_id = m.object_id
WHERE m.definition LIKE '%order_details%'
   OR m.definition LIKE '%order_detail%';

GO

-- =============================================
-- STEP 2: List ALL triggers on related tables
-- =============================================
PRINT N'';
PRINT N'=== ALL TRIGGERS ON RELATED TABLES ===';

SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    t.is_disabled AS IsDisabled,
    t.create_date AS CreatedDate
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) IN ('purchase_orders', 'purchase_order_details', 'payments', 'branch_inventories')
ORDER BY OBJECT_NAME(t.parent_id), t.name;

GO

-- =============================================
-- STEP 3: Disable problematic triggers (if found)
-- Uncomment the lines below after identifying the trigger name
-- =============================================
PRINT N'';
PRINT N'=== TO DISABLE A TRIGGER, RUN: ===';
PRINT N'DISABLE TRIGGER [trigger_name] ON [table_name];';
PRINT N'';

-- Example: If you find a trigger named 'trg_purchase_orders_something'
-- DISABLE TRIGGER trg_purchase_orders_something ON purchase_orders;

-- =============================================
-- STEP 4: Show stored procedure definition to verify
-- =============================================
PRINT N'';
PRINT N'=== SP sp_POS_Checkout_Classic DEFINITION CHECK ===';

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_POS_Checkout_Classic')
BEGIN
    PRINT N'✓ Stored procedure exists';
    
    -- Check if it references wrong table
    SELECT 
        CASE 
            WHEN m.definition LIKE '%order_details%' AND m.definition NOT LIKE '%purchase_order_details%' 
            THEN N'⚠ WARNING: SP still references order_details (not purchase_order_details)'
            ELSE N'✓ SP correctly references purchase_order_details'
        END AS Status
    FROM sys.sql_modules m
    INNER JOIN sys.procedures p ON m.object_id = p.object_id
    WHERE p.name = 'sp_POS_Checkout_Classic';
END
ELSE
BEGIN
    PRINT N'⚠ Stored procedure does NOT exist - need to create it';
END

GO

PRINT N'';
PRINT N'=== NEXT STEPS ===';
PRINT N'1. Look at the trigger list above';
PRINT N'2. Find any trigger that references "order_details"';
PRINT N'3. Either DISABLE it or FIX it to use "purchase_order_details"';
PRINT N'4. Then run the main fix script again';
GO
