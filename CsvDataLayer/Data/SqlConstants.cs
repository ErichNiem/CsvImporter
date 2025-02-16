namespace CsvDataLayer.Data
{
    public static class SqlConstants
    {
        public const string CreateInvoiceHeadersTable = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceHeaders]') AND type in (N'U'))
            BEGIN
                CREATE TABLE InvoiceHeaders (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
                    InvoiceDate DATETIME2 NOT NULL,
                    Address NVARCHAR(500) NOT NULL,
                    InvoiceTotalExVAT DECIMAL(18,2) NOT NULL
                );
                PRINT 'Created InvoiceHeaders table';
            END";

        public const string CreateInvoiceLinesTable = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceLines]') AND type in (N'U'))
            BEGIN
                CREATE TABLE InvoiceLines (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    InvoiceHeaderId INT NOT NULL,
                    LineDescription NVARCHAR(500) NOT NULL,
                    InvoiceQuantity INT NOT NULL,
                    UnitSellingPriceExVAT DECIMAL(18,2) NOT NULL,
                    CONSTRAINT FK_InvoiceLines_InvoiceHeaders 
                        FOREIGN KEY (InvoiceHeaderId) 
                        REFERENCES InvoiceHeaders(Id) 
                        ON DELETE CASCADE
                );
                PRINT 'Created InvoiceLines table';
            END";

        public const string GetAllTables = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

        public const string CreateDatabaseIfNotExists = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{0}') CREATE DATABASE {0};";
    }
}