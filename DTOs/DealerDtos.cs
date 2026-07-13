namespace GroceryOrderingApp.Backend.DTOs
{
    public class DealerNotificationDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
