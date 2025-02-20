using CsvDataLayer.Data;
using CsvDataLayer.Entities;
using CsvDataLayer.Interfaces;
using CsvDataLayer.Repositories;
using CsvDataLayer.Services;
using CsvHelper;
using CsvHelper.Configuration;
using CsvImporterDomain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace CsvImporterDomain.Services
{
    public static class CsvImportService
    {
        public static IServiceProvider ConfigureServices(string connectionString)
        {
            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceService, InvoiceService>();

            var serviceProvider = services.BuildServiceProvider();

            // Ensure database is created
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
            }

            return serviceProvider;
        }

        public static async Task ProcessInvoices(IInvoiceService invoiceService, string csvFilePath)
        {
            using var fileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fileStream);
            
            if (reader.EndOfStream)
            {
                throw new ReaderException(new CsvContext(new CsvConfiguration(CultureInfo.InvariantCulture)), "CSV file is empty");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace(" ", ""),
                HeaderValidated = null,
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            };
            
            using var csv = new CsvReader(reader, config);
            
            try
            {
                // Read header first to validate
                csv.Read();
                csv.ReadHeader();
                var headerRow = csv.HeaderRecord;

                var requiredHeaders = new[]
                {
                    "Invoice Number",
                    "Invoice Date",
                    "Address",
                    "Line description",
                    "Invoice Quantity",
                    "Unit selling price ex VAT",
                    "Invoice Total Ex VAT"
                };

                var missingHeaders = requiredHeaders
                    .Where(header => !headerRow!.Select(h => h.ToLower().Replace(" ", ""))
                        .Contains(header.ToLower().Replace(" ", "")))
                    .ToList();

                if (missingHeaders.Any())
                {
                    throw new CsvHelper.MissingFieldException(csv.Context, missingHeaders.First());
                }

                csv.Context.RegisterClassMap<InvoiceCsvModelMap>();
                var records = csv.GetRecords<InvoiceCsvModel>().ToList();

                if (!records.Any())
                {
                    throw new ReaderException(csv.Context, "No valid records found in CSV file");
                }

                // Group records by invoice number
                var invoiceGroups = records.GroupBy(r => r.InvoiceNumber);

                foreach (var group in invoiceGroups)
                {
                    var firstRecord = group.First();
                    var invoiceHeader = new InvoiceHeader
                    {
                        InvoiceNumber = group.Key,
                        InvoiceDate = firstRecord.InvoiceDate,
                        Address = firstRecord.Address,
                        InvoiceTotalExVAT = firstRecord.InvoiceTotalExVAT
                    };

                    var invoiceLines = group.Select(r => new InvoiceLine
                    {
                        LineDescription = r.LineDescription,
                        InvoiceQuantity = r.InvoiceQuantity,
                        UnitSellingPriceExVAT = r.UnitSellingPriceExVAT
                    }).ToList();

                    try
                    {
                        await invoiceService.AddInvoiceWithLines(invoiceHeader, invoiceLines);
                        
                        // Calculate and display total quantity for this invoice
                        var totalQuantity = invoiceLines.Sum(l => l.InvoiceQuantity);
                        Console.WriteLine($"Imported Invoice {group.Key} - Total Quantity: {totalQuantity}");
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") ?? false)
                    {
                        Console.WriteLine($"Skipped duplicate invoice: {group.Key}");
                    }
                }

                // Verify totals match
                var allInvoices = await invoiceService.GetAllInvoicesWithLines();
                if (allInvoices == null || !allInvoices.Any())
                {
                    Console.WriteLine("No invoices found for validation");
                    return;
                }
                
                decimal headerTotal = allInvoices.Sum(h => h.InvoiceTotalExVAT);
                decimal lineItemsTotal = allInvoices
                    .Where(h => h.InvoiceLines != null && h.InvoiceLines.Any())
                    .SelectMany(h => h.InvoiceLines!)
                    .Sum(l => l.InvoiceQuantity * l.UnitSellingPriceExVAT);

                Console.WriteLine("\nValidating totals:");
                Console.WriteLine($"Sum of Invoice Headers: {headerTotal:F2}");
                Console.WriteLine($"Sum of Line Items: {lineItemsTotal:F2}");
                
                if (Math.Abs(headerTotal - lineItemsTotal) < 0.01m)
                {
                    Console.WriteLine("✓ Totals match!");
                }
                else
                {
                    Console.WriteLine("⚠ Warning: Totals do not match!");
                    Console.WriteLine($"Difference: {Math.Abs(headerTotal - lineItemsTotal):F2}");
                }
            }
            catch (CsvHelper.MissingFieldException ex)
            {
                var rowNumber = ex.Context?.Parser?.Row ?? 0;
                var rawRow = ex.Context?.Parser?.RawRecord ?? "";
                Console.WriteLine($"\nError reading CSV: Missing field in row {rowNumber}");
                Console.WriteLine($"Raw data: {rawRow}");
                Console.WriteLine($"Details: {ex.Message}");
                throw;
            }
            catch (CsvHelper.TypeConversion.TypeConverterException ex)
            {
                var rowNumber = ex.Context?.Parser?.Row ?? 0;
                var rawRow = ex.Context?.Parser?.RawRecord ?? "";
                var fieldName = ex.Context?.Reader?.HeaderRecord?[ex.Context.Reader.CurrentIndex] ?? "unknown";
                Console.WriteLine($"\nError converting data in row {rowNumber}, field '{fieldName}'");
                Console.WriteLine($"Raw data: {rawRow}");
                Console.WriteLine($"Details: {ex.Message}");
                throw;
            }
            catch (ReaderException ex)
            {
                Console.WriteLine($"\nError in CSV data: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error processing CSV: {ex.Message}");
                throw;
            }
        }
    }
}