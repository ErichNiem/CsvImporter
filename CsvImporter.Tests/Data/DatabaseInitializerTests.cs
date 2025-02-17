using CsvDataLayer.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace CsvImporter.Tests.Data
{
    [TestClass]
    public class DatabaseInitializerTests
    {
        private string _connectionString = null!;
        private string _databaseName = null!;
        private ApplicationDbContext _context = null!;

        [TestInitialize]
        public void Setup()
        {
            // Use underscore instead of hyphen for database name, and add a random component
            _databaseName = $"TestDb_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetRandomFileName().Replace(".", "")}";
            _connectionString = $"Server=localhost;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        [TestMethod]
        public async Task EnsureDatabaseCreated_CreatesDatabase()
        {
            // Act
            await DatabaseInitializer.EnsureDatabaseCreated(_connectionString, _databaseName);

            // Assert
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName",
                connection);
            cmd.Parameters.AddWithValue("@dbName", _databaseName);

            var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.AreEqual(1, result, "Database should exist after creation");
        }

        [TestMethod]
        public async Task EnsureTablesCreated_CreatesTables()
        {
            // Arrange
            await DatabaseInitializer.EnsureDatabaseCreated(_connectionString, _databaseName);

            // Act
            await DatabaseInitializer.EnsureTablesCreated(_context);

            // Assert
            var tables = await _context.Database.SqlQuery<string>(
                FormattableStringFactory.Create(SqlConstants.GetAllTables))
                .ToListAsync();

            CollectionAssert.Contains(tables, "InvoiceHeaders");
            CollectionAssert.Contains(tables, "InvoiceLines");
        }

        [TestMethod]
        public async Task EnsureTablesCreated_TablesHaveCorrectSchema()
        {
            // Arrange
            await DatabaseInitializer.EnsureDatabaseCreated(_connectionString, _databaseName);
            await DatabaseInitializer.EnsureTablesCreated(_context);

            // Act & Assert
            var headerColumns = await GetTableColumns("InvoiceHeaders");
            var lineColumns = await GetTableColumns("InvoiceLines");

            CollectionAssert.Contains(headerColumns, "Id");
            CollectionAssert.Contains(headerColumns, "InvoiceNumber");
            CollectionAssert.Contains(headerColumns, "InvoiceTotalExVAT");

            CollectionAssert.Contains(lineColumns, "Id");
            CollectionAssert.Contains(lineColumns, "InvoiceHeaderId");
            CollectionAssert.Contains(lineColumns, "InvoiceQuantity");
            CollectionAssert.Contains(lineColumns, "UnitSellingPriceExVAT");
        }

        private async Task<List<string>> GetTableColumns(string tableName)
        {
            return await _context.Database
                .SqlQuery<string>(FormattableStringFactory.Create(
                    $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'"))
                .ToListAsync();
        }
    }
}