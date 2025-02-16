using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsvDataLayer.Entities
{
    public class InvoiceHeader
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public DateTime InvoiceDate { get; set; }
        
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal InvoiceTotalExVAT { get; set; }
        
        public virtual ICollection<InvoiceLine>? InvoiceLines { get; set; } = new List<InvoiceLine>();
    }

    public class InvoiceLine
    {
        [Key]
        public int Id { get; set; }
        
        public int InvoiceHeaderId { get; set; }
        
        [Required]
        [ForeignKey("InvoiceHeaderId")]
        public virtual InvoiceHeader? InvoiceHeader { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LineDescription { get; set; } = string.Empty;
        
        public int InvoiceQuantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitSellingPriceExVAT { get; set; }
    }
}