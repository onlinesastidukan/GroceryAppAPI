namespace GroceryOrderingApp.Backend.Models
{
    public class DealerNotification
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public int OrderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Dealer { get; set; }
        public Order? Order { get; set; }
    }
}
