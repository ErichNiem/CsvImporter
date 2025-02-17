using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CsvDataLayer.Interfaces;
using CsvDataLayer.Entities;
using CsvImporterDomain.Services;
using CsvImporterDomain.Models;
using System.Text;
using CsvHelper;
using CsvHelper.TypeConversion;

namespace CsvImporter.Tests.Services
{
    [TestClass]
    public class CsvImportServiceTests
    {
        private Mock<IInvoiceService> _mockInvoiceService = null!;
        private string _testCsvPath = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockInvoiceService = new Mock<IInvoiceService>();
            // Create a unique file name for each test
            _testCsvPath = Path.Combine(Path.GetTempPath(), $"test_invoices_{Path.GetRandomFileName()}.csv");
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            // Add retry logic for file cleanup
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(_testCsvPath))
                    {
                        File.Delete(_testCsvPath);
                    }
                    break;
                }
                catch (IOException)
                {
                    if (i == 2) throw; // Throw on last attempt
                    await Task.Delay(100); // Wait before retry
                }
            }
        }

        [TestMethod]
        public async Task ProcessInvoices_WithValidCsv_ProcessesSuccessfully()
        {
            // Arrange
            var csvContent = @"Invoice Number,Invoice Date,Address,Line description,Invoice Quantity,Unit selling price ex VAT,Invoice Total Ex VAT
INV001,16/02/2024 10:00,123 Test St,Test Item,2,50.00,100.00";
            
            await File.WriteAllTextAsync(_testCsvPath, csvContent, Encoding.UTF8);

            _mockInvoiceService.Setup(s => s.AddInvoiceWithLines(
                It.IsAny<InvoiceHeader>(), 
                It.IsAny<List<InvoiceLine>>()))
                .Returns(Task.CompletedTask);

            // Act
            await CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath);

            // Assert
            _mockInvoiceService.Verify(s => s.AddInvoiceWithLines(
                It.Is<InvoiceHeader>(h => 
                    h.InvoiceNumber == "INV001" && 
                    h.InvoiceTotalExVAT == 100.00M),
                It.Is<List<InvoiceLine>>(l => 
                    l.Count == 1 && 
                    l[0].LineDescription == "Test Item" &&
                    l[0].InvoiceQuantity == 2 &&
                    l[0].UnitSellingPriceExVAT == 50.00M)
            ), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task ProcessInvoices_WithInvalidFilePath_ThrowsException()
        {
            // Act
            await CsvImportService.ProcessInvoices(_mockInvoiceService.Object, "nonexistent.csv");
        }

        [TestMethod]
        public async Task ProcessInvoices_WithInvalidCsvFormat_ThrowsMissingFieldException()
        {
            // Arrange
            var invalidCsvContent = @"InvalidHeader1,InvalidHeader2
InvalidData1,InvalidData2";
            
            await File.WriteAllTextAsync(_testCsvPath, invalidCsvContent, Encoding.UTF8);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<CsvHelper.MissingFieldException>(() => 
                CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath),
                "Should throw MissingFieldException when CSV headers don't match expected format");
        }

        [TestMethod]
        public async Task ProcessInvoices_WithEmptyCsv_ThrowsReaderException()
        {
            // Arrange
            await File.WriteAllTextAsync(_testCsvPath, "", Encoding.UTF8);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ReaderException>(() => 
                CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath),
                "Should throw ReaderException when CSV file is empty");
        }

        [TestMethod]
        public async Task ProcessInvoices_WithMissingRequiredHeaders_ThrowsMissingFieldException()
        {
            // Arrange
            var invalidCsvContent = @"Invoice Number,Invoice Date
INV001,2024-02-16";
            
            await File.WriteAllTextAsync(_testCsvPath, invalidCsvContent, Encoding.UTF8);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<CsvHelper.MissingFieldException>(() => 
                CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath),
                "Should throw MissingFieldException when required headers are missing");
        }

        [TestMethod]
        public async Task ProcessInvoices_WithInvalidDateFormat_ThrowsTypeConverterException()
        {
            // Arrange
            var csvContent = @"Invoice Number,Invoice Date,Address,Line description,Invoice Quantity,Unit selling price ex VAT,Invoice Total Ex VAT
INV001,invalid-date,123 Test St,Test Item,2,50.00,100.00";
            
            await File.WriteAllTextAsync(_testCsvPath, csvContent, Encoding.UTF8);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<TypeConverterException>(() => 
                CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath),
                "Should throw TypeConverterException when date format is invalid");
        }

        [TestMethod]
        public async Task ProcessInvoices_WithInvalidDecimalFormat_ThrowsTypeConverterException()
        {
            // Arrange
            var csvContent = @"Invoice Number,Invoice Date,Address,Line description,Invoice Quantity,Unit selling price ex VAT,Invoice Total Ex VAT
INV001,16/02/2024 10:00,123 Test St,Test Item,2,invalid-price,100.00";
            
            await File.WriteAllTextAsync(_testCsvPath, csvContent, Encoding.UTF8);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<TypeConverterException>(() => 
                CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath),
                "Should throw TypeConverterException when decimal format is invalid");
        }

        [TestMethod]
        public async Task ProcessInvoices_WithInvalidQuantityFormat_ThrowsTypeConverterException()
        {
            // Arrange
            var csvContent = @"Invoice Number,Invoice Date,Address,Line description,Invoice Quantity,Unit selling price ex VAT,Invoice Total Ex VAT
INV001,16/02/2024 10:00,123 Test St,Test Item,invalid-qty,50.00,100.00";
            
            await File.WriteAllTextAsync(_testCsvPath, csvContent, Encoding.UTF8);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<TypeConverterException>(() => 
                CsvImportService.ProcessInvoices(_mockInvoiceService.Object, _testCsvPath),
                "Should throw TypeConverterException when quantity format is invalid");
        }
    }
}