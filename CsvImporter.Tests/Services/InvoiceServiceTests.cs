using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CsvDataLayer.Interfaces;
using CsvDataLayer.Services;
using CsvDataLayer.Entities;

namespace CsvImporter.Tests.Services
{
    [TestClass]
    public class InvoiceServiceTests
    {
        private Mock<IInvoiceRepository> _mockRepository = null!;
        private IInvoiceService _invoiceService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IInvoiceRepository>();
            _invoiceService = new InvoiceService(_mockRepository.Object);
        }

        [TestMethod]
        public async Task ValidateTotalsAsync_WhenTotalsMatch_ReturnsCorrectValues()
        {
            // Arrange
            var invoices = new List<InvoiceHeader>
            {
                new InvoiceHeader
                {
                    Id = 1,
                    InvoiceNumber = "INV001",
                    InvoiceTotalExVAT = 100.00M,
                    InvoiceLines = new List<InvoiceLine>
                    {
                        new InvoiceLine { InvoiceQuantity = 2, UnitSellingPriceExVAT = 50.00M }
                    }
                }
            };

            _mockRepository.Setup(r => r.GetAllInvoicesWithLines())
                          .ReturnsAsync(invoices);
            _mockRepository.Setup(r => r.GetTotalInvoiceAmountAsync())
                          .ReturnsAsync(100.00M);
            _mockRepository.Setup(r => r.GetTotalLineAmountAsync())
                          .ReturnsAsync(100.00M);

            // Act
            var (invoiceTotal, lineItemTotal) = await _invoiceService.ValidateTotalsAsync();

            // Assert
            const decimal tolerance = 0.01M;
            Assert.IsTrue(Math.Abs(100.00M - invoiceTotal) < tolerance, "Invoice total should be approximately 100.00");
            Assert.IsTrue(Math.Abs(100.00M - lineItemTotal) < tolerance, "Line item total should be approximately 100.00");
        }

        [TestMethod]
        public async Task ValidateTotalsAsync_WhenTotalsDontMatch_StillReturnsValues()
        {
            // Arrange
            var invoices = new List<InvoiceHeader>
            {
                new InvoiceHeader
                {
                    Id = 1,
                    InvoiceNumber = "INV001",
                    InvoiceTotalExVAT = 100.00M,
                    InvoiceLines = new List<InvoiceLine>
                    {
                        new InvoiceLine { InvoiceQuantity = 1, UnitSellingPriceExVAT = 50.00M }
                    }
                }
            };

            _mockRepository.Setup(r => r.GetAllInvoicesWithLines())
                          .ReturnsAsync(invoices);
            _mockRepository.Setup(r => r.GetTotalInvoiceAmountAsync())
                          .ReturnsAsync(100.00M);
            _mockRepository.Setup(r => r.GetTotalLineAmountAsync())
                          .ReturnsAsync(50.00M);

            // Act
            var (invoiceTotal, lineItemTotal) = await _invoiceService.ValidateTotalsAsync();

            // Assert
            Assert.AreEqual(100.00M, invoiceTotal);
            Assert.AreEqual(50.00M, lineItemTotal);
        }

        [TestMethod]
        public async Task AddInvoiceWithLines_CorrectlyAddsInvoice()
        {
            // Arrange
            var header = new InvoiceHeader
            {
                InvoiceNumber = "INV001",
                InvoiceTotalExVAT = 100.00M
            };
            var lines = new List<InvoiceLine>
            {
                new InvoiceLine 
                { 
                    LineDescription = "Test Item",
                    InvoiceQuantity = 2,
                    UnitSellingPriceExVAT = 50.00M
                }
            };

            // Act
            await _invoiceService.AddInvoiceWithLines(header, lines);

            // Assert
            _mockRepository.Verify(r => r.AddInvoiceWithLines(
                It.Is<InvoiceHeader>(h => h.InvoiceNumber == "INV001" && h.InvoiceTotalExVAT == 100.00M),
                It.Is<List<InvoiceLine>>(l => 
                    l.Count == 1 && 
                    l[0].LineDescription == "Test Item" &&
                    l[0].InvoiceQuantity == 2 &&
                    l[0].UnitSellingPriceExVAT == 50.00M)
            ), Times.Once);
        }

        private async Task<InvoiceHeader> SaveTestInvoiceAsync(InvoiceHeader header)
        {
            var existingInvoice = await _mockRepository.Object.GetInvoiceByNumberAsync(header.InvoiceNumber);
            if (existingInvoice != null)
            {
                // Update existing invoice in our test context
                existingInvoice.InvoiceTotalExVAT = header.InvoiceTotalExVAT;
                existingInvoice.InvoiceDate = header.InvoiceDate;
                existingInvoice.Address = header.Address;
                return existingInvoice;
            }

            // Simulate adding a new invoice
            header.Id = new Random().Next(1, 1000); // Simulate DB generated ID
            await _mockRepository.Object.AddInvoiceAsync(header);
            await _mockRepository.Object.SaveChangesAsync();
            return header;
        }
    }
}