using Microsoft.EntityFrameworkCore;
using CsvDataLayer.Entities;

namespace CsvDataLayer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<InvoiceHeader> InvoiceHeaders { get; set; } = null!;
        public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names
            modelBuilder.Entity<InvoiceHeader>().ToTable("InvoiceHeaders");
            modelBuilder.Entity<InvoiceLine>().ToTable("InvoiceLines");

            // Configure primary keys
            modelBuilder.Entity<InvoiceHeader>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<InvoiceLine>()
                .HasKey(e => e.Id);

            // Configure relationships
            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.InvoiceHeader)
                .WithMany(ih => ih.InvoiceLines)
                .HasForeignKey(il => il.InvoiceHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique indexes
            modelBuilder.Entity<InvoiceHeader>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // Configure required fields and data types
            modelBuilder.Entity<InvoiceHeader>()
                .Property(ih => ih.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<InvoiceHeader>()
                .Property(ih => ih.Address)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<InvoiceHeader>()
                .Property(ih => ih.InvoiceTotalExVAT)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceLine>()
                .Property(il => il.LineDescription)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<InvoiceLine>()
                .Property(il => il.UnitSellingPriceExVAT)
                .HasColumnType("decimal(18,2)");
        }
    }
}