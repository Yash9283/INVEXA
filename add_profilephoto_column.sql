-- Add ProfilePhoto column to Admins table
USE StockFlowDB;
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Admins' AND COLUMN_NAME = 'ProfilePhoto'
)
BEGIN
    ALTER TABLE Admins
    ADD ProfilePhoto NVARCHAR(255) NULL;
    
    PRINT 'ProfilePhoto column added successfully!';
END
ELSE
BEGIN
    PRINT 'ProfilePhoto column already exists.';
END
GO
