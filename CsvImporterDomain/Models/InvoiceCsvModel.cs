namespace CsvImporterDomain.Models
{
    public class InvoiceCsvModel
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal InvoiceTotalExVAT { get; set; }
        public string LineDescription { get; set; } = string.Empty;
        public int InvoiceQuantity { get; set; }
        public decimal UnitSellingPriceExVAT { get; set; }
    }
}