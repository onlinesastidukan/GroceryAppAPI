using GroceryOrderingApp.Backend.Data;
using GroceryOrderingApp.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace GroceryOrderingApp.Backend.Repositories
{
    public class DealerNotificationRepository : IDealerNotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public DealerNotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DealerNotification>> GetDealerNotificationsAsync(int dealerId)
        {
            return await _context.DealerNotifications
                .Where(n => n.DealerId == dealerId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<DealerNotification> CreateAsync(DealerNotification notification)
        {
            await _context.DealerNotifications.AddAsync(notification);
            await SaveAsync();
            return notification;
        }

        public async Task MarkAsReadAsync(int dealerId, int notificationId)
        {
            var notification = await _context.DealerNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.DealerId == dealerId);
            if (notification == null)
            {
                return;
            }

            notification.IsRead = true;
            await SaveAsync();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
