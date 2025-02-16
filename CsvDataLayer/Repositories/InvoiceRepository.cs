using Microsoft.EntityFrameworkCore;
using CsvDataLayer.Data;
using CsvDataLayer.Entities;
using CsvDataLayer.Interfaces;

namespace CsvDataLayer.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;

        public InvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InvoiceHeader>> GetAllInvoicesAsync()
        {
            return await _context.InvoiceHeaders
                .Include(h => h.InvoiceLines)
                .ToListAsync();
        }

        public async Task<InvoiceHeader?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return await _context.InvoiceHeaders
                .Include(h => h.InvoiceLines)
                .FirstOrDefaultAsync(h => h.InvoiceNumber == invoiceNumber);
        }

        public async Task AddInvoiceAsync(InvoiceHeader invoice)
        {
            await _context.InvoiceHeaders.AddAsync(invoice);
        }

        public async Task<decimal> GetTotalInvoiceAmountAsync()
        {
            return await _context.InvoiceHeaders.SumAsync(i => i.InvoiceTotalExVAT);
        }

        public async Task<decimal> GetTotalLineAmountAsync()
        {
            var invoices = await _context.InvoiceHeaders
                .Include(h => h.InvoiceLines)
                .ToListAsync();

            return invoices.Sum(i => i.InvoiceLines?.Sum(l => l.InvoiceQuantity * l.UnitSellingPriceExVAT) ?? 0);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddInvoiceWithLines(InvoiceHeader header, List<InvoiceLine> lines)
        {
            var existingInvoice = await _context.InvoiceHeaders
                .FirstOrDefaultAsync(h => h.InvoiceNumber == header.InvoiceNumber);

            if (existingInvoice != null)
            {
                throw new DbUpdateException("Duplicate invoice number found");
            }

            await _context.InvoiceHeaders.AddAsync(header);
            await _context.SaveChangesAsync();

            foreach (var line in lines)
            {
                line.InvoiceHeaderId = header.Id;
            }
            await _context.InvoiceLines.AddRangeAsync(lines);
            await _context.SaveChangesAsync();
        }

        public async Task<List<InvoiceHeader>> GetAllInvoicesWithLines()
        {
            return await _context.InvoiceHeaders
                .Include(h => h.InvoiceLines)
                .ToListAsync();
        }
    }
}