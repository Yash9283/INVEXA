using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; }

        [Required]
        public string CustomerName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        [Required]
        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime InvoiceDate { get; set; }

        public string PaymentStatus { get; set; }
    }
}