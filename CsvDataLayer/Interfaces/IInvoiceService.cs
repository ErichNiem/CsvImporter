using CsvDataLayer.Entities;

namespace CsvDataLayer.Interfaces
{
    public interface IInvoiceService
    {
        Task ImportInvoicesAsync(IEnumerable<InvoiceImportDto> records);
        Task<IEnumerable<InvoiceDetailDto>> GetInvoiceDetailsAsync();
        Task<(decimal InvoiceTotal, decimal LineItemTotal)> ValidateTotalsAsync();
        Task AddInvoiceWithLines(InvoiceHeader header, List<InvoiceLine> lines);
        Task<List<InvoiceHeader>> GetAllInvoicesWithLines();
    }

    public sealed record InvoiceImportDto(
        string InvoiceNumber,
        DateTime InvoiceDate,
        string Address,
        decimal InvoiceTotalExVAT,
        string LineDescription,
        int InvoiceQuantity,
        decimal UnitSellingPriceExVAT
    );

    public sealed record InvoiceDetailDto(
        string InvoiceNumber,
        int TotalQuantity
    );
}