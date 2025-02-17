using CsvDataLayer.Data;
using CsvDataLayer.Interfaces;
using CsvDataLayer.Repositories;
using CsvDataLayer.Services;
using CsvImporterDomain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

namespace CsvImporter;

public class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Get connection string and database name from App.config
            var connectionString = ConfigurationManager.ConnectionStrings["CsvImporter"]?.ConnectionString 
                ?? throw new ConfigurationErrorsException("Connection string 'CsvImporter' not found in configuration");
                
            var databaseName = ConfigurationManager.AppSettings["DatabaseName"] 
                ?? throw new ConfigurationErrorsException("DatabaseName not found in configuration");
            
            // Ensure database exists
            await DatabaseInitializer.EnsureDatabaseCreated(connectionString, databaseName);

                // Set up application services
                var services = new ServiceCollection();

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));

                services.AddScoped<IInvoiceRepository, InvoiceRepository>();
                services.AddScoped<IInvoiceService, InvoiceService>();

                var serviceProvider = services.BuildServiceProvider();

                // Ensure tables are created
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await DatabaseInitializer.EnsureTablesCreated(dbContext);

                try
                {
                    // Get CSV file path from configuration
                    var csvFilePath = ConfigurationManager.AppSettings["CsvFilePath"]
                        ?? throw new ConfigurationErrorsException("CsvFilePath not found in configuration");

                    if (!File.Exists(csvFilePath))
                    {
                        throw new FileNotFoundException($"CSV file not found at path: {csvFilePath}");
                    }

                    Console.WriteLine($"\nProcessing CSV file: {csvFilePath}");
                    var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

                    try
                    {
                        await CsvImportService.ProcessInvoices(invoiceService, csvFilePath);
                        Console.WriteLine("\nCSV processing completed successfully!");

                        // Validate totals after successful import
                        var (invoiceTotal, lineItemTotal) = await invoiceService.ValidateTotalsAsync();
                        Console.WriteLine($"\nValidation Results:");
                        Console.WriteLine($"Total Invoice Amount: {invoiceTotal:C}");
                        Console.WriteLine($"Total Line Items Amount: {lineItemTotal:C}");

                        // Check if totals match with a small tolerance for floating-point arithmetic
                        const decimal tolerance = 0.01M; // 1 penny tolerance
                        if (Math.Abs(invoiceTotal - lineItemTotal) > tolerance)
                        {
                            var difference = Math.Abs(invoiceTotal - lineItemTotal);
                            throw new InvalidOperationException(
                                $"Total mismatch detected! Difference: {difference:C}\n" +
                                $"This might indicate missing or incorrect line items."
                            );
                        }

                        Console.WriteLine("All totals match successfully!");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"\nValidation Error: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Additional Details: {ex.InnerException.Message}");
                        }
                        throw;
                    }
                    catch (DbUpdateException ex)
                    {
                        Console.WriteLine($"\nDatabase Error: Failed to save changes");
                        Console.WriteLine($"Details: {ex.InnerException?.Message ?? ex.Message}");
                        throw;
                    }
                }
                catch (ConfigurationErrorsException ex)
                {
                    Console.WriteLine($"\nConfiguration Error: {ex.Message}");
                    throw;
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"\nFile Error: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError processing CSV: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Additional Details: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFatal Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.ExitCode = 1;
            }

        // Only try to read key if we have a console window
        if (Environment.UserInteractive && !Console.IsInputRedirected)
        {
            Console.WriteLine("\nPress any key to exit...");
            try
            {
                Console.ReadKey(intercept: true);
            }
            catch (InvalidOperationException)
            {
                // Ignore if we can't read the key
            }
        }
    }
}
