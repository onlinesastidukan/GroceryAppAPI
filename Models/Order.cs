namespace GroceryOrderingApp.Backend.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Shipped, Delivered, Cancelled
        public decimal TotalAmount { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerMobileNumber { get; set; }

        public User? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<DealerNotification> DealerNotifications { get; set; } = new List<DealerNotification>();
    }
}
