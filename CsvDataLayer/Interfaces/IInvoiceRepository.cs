using CsvDataLayer.Entities;

namespace CsvDataLayer.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<InvoiceHeader>> GetAllInvoicesAsync();
        Task<InvoiceHeader?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task AddInvoiceAsync(InvoiceHeader invoice);
        Task<decimal> GetTotalInvoiceAmountAsync();
        Task<decimal> GetTotalLineAmountAsync();
        Task SaveChangesAsync();
        Task AddInvoiceWithLines(InvoiceHeader header, List<InvoiceLine> lines);
        Task<List<InvoiceHeader>> GetAllInvoicesWithLines();
    }
}