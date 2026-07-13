using GroceryOrderingApp.Backend.Models;

namespace GroceryOrderingApp.Backend.Repositories
{
    public interface IDealerNotificationRepository
    {
        Task<List<DealerNotification>> GetDealerNotificationsAsync(int dealerId);
        Task<DealerNotification> CreateAsync(DealerNotification notification);
        Task MarkAsReadAsync(int dealerId, int notificationId);
        Task SaveAsync();
    }
}
