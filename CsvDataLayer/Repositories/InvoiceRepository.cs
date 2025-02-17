using Microsoft.EntityFrameworkCore;
using CsvDataLayer.Data;
using CsvDataLayer.Entities;
using CsvDataLayer.Interfaces;

namespace CsvDataLayer.Repositories
{
    public class InvoiceRepository(ApplicationDbContext context) : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context = context;

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
                throw new InvalidOperationException($"Invoice number {header.InvoiceNumber} already exists");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.InvoiceHeaders.AddAsync(header);
                
                foreach (var line in lines)
                {
                    line.InvoiceHeader = header; // Set the navigation property instead of just the ID
                }
                await _context.InvoiceLines.AddRangeAsync(lines);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<InvoiceHeader>> GetAllInvoicesWithLines()
        {
            return await _context.InvoiceHeaders
                .Include(h => h.InvoiceLines)
                .ToListAsync();
        }
    }
}