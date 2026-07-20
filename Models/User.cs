namespace GroceryOrderingApp.Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? FcmToken { get; set; }

        public Role? Role { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Category> DealerShops { get; set; } = new List<Category>();
        public ICollection<DealerNotification> DealerNotifications { get; set; } = new List<DealerNotification>();
    }
}
