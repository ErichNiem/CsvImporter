using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CsvDataLayer.Data
{
    public static class DatabaseInitializer
    {
        public static async Task EnsureDatabaseCreated(string connectionString, string databaseName)
        {
            // Validate database name
            if (!IsValidDatabaseName(databaseName))
            {
                throw new ArgumentException("Invalid database name. Database names must start with a letter or underscore, " +
                    "and can only contain letters, numbers, and underscores.", nameof(databaseName));
            }

            // Create a connection string to master database
            var builder = new SqlConnectionStringBuilder(connectionString);
            var originalDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            var masterConnectionString = builder.ConnectionString;

            using var connection = new SqlConnection(masterConnectionString);
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("Successfully connected to SQL Server master database.");

                var createDbCommand = new SqlCommand(
                    string.Format(SqlConstants.CreateDatabaseIfNotExists, databaseName),
                    connection);
                await createDbCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"Ensured database '{databaseName}' exists.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring database exists: {ex.Message}");
                throw;
            }
        }

        private static bool IsValidDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                return false;

            // SQL Server database naming rules
            var pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            return Regex.IsMatch(databaseName, pattern);
        }

        public static async Task EnsureTablesCreated(ApplicationDbContext dbContext)
        {
            Console.WriteLine("\nChecking database schema...");

            try
            {
                // Instead of creating the database, just check if we can connect
                if (!await dbContext.Database.CanConnectAsync())
                {
                    throw new InvalidOperationException("Cannot connect to database. Please ensure database is created first.");
                }

                try
                {
                    // Get pending migrations
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations.");
                        await dbContext.Database.MigrateAsync();
                        Console.WriteLine("Applied pending migrations successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Database schema is up to date.");
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("pending changes"))
                {
                    // If we have pending model changes, just create the schema without recreating the database
                    Console.WriteLine("Detected pending model changes. Creating database schema...");
                    await dbContext.Database.EnsureCreatedAsync();
                }

                // Verify tables exist
                var tables = await dbContext.Database.SqlQuery<string>(
                    FormattableStringFactory.Create(SqlConstants.GetAllTables))
                    .ToListAsync();

                if (!tables.Any())
                {
                    throw new Exception("No tables were found after migration attempt.");
                }

                Console.WriteLine("\nVerified tables:");
                foreach (var table in tables)
                {
                    Console.WriteLine($"- {table}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError updating database schema: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}