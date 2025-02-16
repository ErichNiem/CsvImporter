using CsvDataLayer.Entities;
using CsvDataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CsvDataLayer.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _repository;

        public InvoiceService(IInvoiceRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task ImportInvoicesAsync(IEnumerable<InvoiceImportDto> records)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            try
            {
                foreach (var record in records)
                {
                    if (string.IsNullOrEmpty(record.InvoiceNumber))
                        throw new InvalidOperationException($"Invoice number cannot be null or empty");

                    var invoice = await _repository.GetInvoiceByNumberAsync(record.InvoiceNumber);

                    if (invoice == null)
                    {
                        invoice = new InvoiceHeader
                        {
                            InvoiceNumber = record.InvoiceNumber,
                            InvoiceDate = record.InvoiceDate,
                            Address = record.Address ?? throw new InvalidOperationException($"Address is required for invoice {record.InvoiceNumber}"),
                            InvoiceTotalExVAT = record.InvoiceTotalExVAT,
                            InvoiceLines = new List<InvoiceLine>()
                        };
                        await _repository.AddInvoiceAsync(invoice);
                    }

                    if (string.IsNullOrEmpty(record.LineDescription))
                        throw new InvalidOperationException($"Line description cannot be null or empty for invoice {record.InvoiceNumber}");

                    var invoiceLine = invoice.InvoiceLines?
                        .FirstOrDefault(l => l.LineDescription == record.LineDescription);

                    if (invoiceLine == null)
                    {
                        invoiceLine = new InvoiceLine
                        {
                            LineDescription = record.LineDescription,
                            InvoiceQuantity = record.InvoiceQuantity,
                            UnitSellingPriceExVAT = record.UnitSellingPriceExVAT,
                            InvoiceHeader = invoice
                        };
                        invoice.InvoiceLines?.Add(invoiceLine);
                    }
                }

                await _repository.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to save changes to the database. Please ensure all required fields are provided.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while importing invoices: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<InvoiceDetailDto>> GetInvoiceDetailsAsync()
        {
            try
            {
                var invoices = await _repository.GetAllInvoicesAsync();
                return invoices.Select(invoice => new InvoiceDetailDto(
                    invoice.InvoiceNumber,
                    invoice.InvoiceLines?.Sum(l => l.InvoiceQuantity) ?? 0
                ));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve invoice details", ex);
            }
        }

        public async Task<(decimal InvoiceTotal, decimal LineItemTotal)> ValidateTotalsAsync()
        {
            try
            {
                var invoiceTotal = await _repository.GetTotalInvoiceAmountAsync();
                var lineItemTotal = await _repository.GetTotalLineAmountAsync();
                return (invoiceTotal, lineItemTotal);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to validate invoice totals", ex);
            }
        }

        public async Task AddInvoiceWithLines(InvoiceHeader header, List<InvoiceLine> lines)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            try
            {
                await _repository.AddInvoiceWithLines(header, lines);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to add invoice with lines to the database", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while adding invoice: {ex.Message}", ex);
            }
        }

        public async Task<List<InvoiceHeader>> GetAllInvoicesWithLines()
        {
            try
            {
                return await _repository.GetAllInvoicesWithLines();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve invoices with their lines", ex);
            }
        }
    }
}