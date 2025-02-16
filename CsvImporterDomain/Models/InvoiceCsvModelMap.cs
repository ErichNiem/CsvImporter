using CsvHelper.Configuration;
using System.Globalization;

namespace CsvImporterDomain.Models
{
    public sealed class InvoiceCsvModelMap : ClassMap<InvoiceCsvModel>
    {
        public InvoiceCsvModelMap()
        {
            Map(m => m.InvoiceNumber).Name("Invoice Number");
            Map(m => m.InvoiceDate).Name("Invoice Date")
                .TypeConverterOption.Format("dd/MM/yyyy HH:mm");
            Map(m => m.Address).Name("Address");
            Map(m => m.InvoiceTotalExVAT).Name("Invoice Total Ex VAT")
                .TypeConverter<DecimalConverter>();
            Map(m => m.LineDescription).Name("Line description");
            Map(m => m.InvoiceQuantity).Name("Invoice Quantity")
                .TypeConverterOption.NumberStyles(NumberStyles.Integer);
            Map(m => m.UnitSellingPriceExVAT).Name("Unit selling price ex VAT")
                .TypeConverter<DecimalConverter>();
        }
    }
}