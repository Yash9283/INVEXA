namespace StockFlow.Models
{
    public class Sale
    {
        public int Id { get; set; }

        public string ProductName { get; set; }

        public string CustomerName { get; set; }

        public int Quantity { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime SaleDate { get; set; }
    }
}