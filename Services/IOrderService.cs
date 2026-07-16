using GroceryOrderingApp.Backend.Models;
using GroceryOrderingApp.Backend.Repositories;

namespace GroceryOrderingApp.Backend.Services
{
    public interface IOrderService
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<List<Order>> GetOrdersByUserAsync(int userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<List<Order>> GetActiveOrdersByMobileAsync(string mobileNumber);
        Task<Order> CreateOrderAsync(Order order, List<(int productId, int quantity)> items);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> DeliverOrderAsync(int orderId);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
