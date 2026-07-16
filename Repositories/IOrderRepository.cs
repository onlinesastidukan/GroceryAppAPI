using GroceryOrderingApp.Backend.Models;

namespace GroceryOrderingApp.Backend.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<List<Order>> GetOrdersByUserAsync(int userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<List<Order>> GetActiveOrdersByMobileAsync(string mobileNumber);
        Task<Order> CreateOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task SaveAsync();
    }
}
