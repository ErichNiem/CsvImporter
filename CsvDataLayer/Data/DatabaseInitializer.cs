using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.CompilerServices;

namespace CsvDataLayer.Data
{
    public static class DatabaseInitializer
    {
        public static async Task EnsureDatabaseCreated(string masterConnectionString, string databaseName)
        {
            using var connection = new SqlConnection(masterConnectionString);
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("Successfully connected to SQL Server.");

                var createDbCommand = new SqlCommand(
                    string.Format(SqlConstants.CreateDatabaseIfNotExists, databaseName),
                    connection);
                await createDbCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"Ensured {databaseName} database exists.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to SQL Server: {ex.Message}");
                throw;
            }
        }

        public static async Task EnsureTablesCreated(ApplicationDbContext dbContext)
        {
            Console.WriteLine("\nCreating tables...");

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(SqlConstants.CreateInvoiceHeadersTable);
                await dbContext.Database.ExecuteSqlRawAsync(SqlConstants.CreateInvoiceLinesTable);

                var tables = await dbContext.Database.SqlQuery<string>(
                    FormattableStringFactory.Create(SqlConstants.GetAllTables))
                    .ToListAsync();

                Console.WriteLine("\nVerifying created tables:");
                foreach (var table in tables)
                {
                    Console.WriteLine($"- Found table: {table}");
                }

                if (tables.Count != 0)
                {
                    Console.WriteLine("Tables created successfully!");
                }
                else
                {
                    throw new Exception("No tables were found after creation attempt.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError creating tables: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}