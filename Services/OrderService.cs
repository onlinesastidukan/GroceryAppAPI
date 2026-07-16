using GroceryOrderingApp.Backend.Models;
using GroceryOrderingApp.Backend.Repositories;

namespace GroceryOrderingApp.Backend.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _orderRepository.GetOrderByIdAsync(id);
        }

        public async Task<List<Order>> GetOrdersByUserAsync(int userId)
        {
            return await _orderRepository.GetOrdersByUserAsync(userId);
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _orderRepository.GetAllOrdersAsync();
        }


        public async Task<List<Order>> GetActiveOrdersByMobileAsync(string mobileNumber)
        {
            return await _orderRepository.GetActiveOrdersByMobileAsync(mobileNumber);
        }
        public async Task<Order> CreateOrderAsync(Order order, List<(int productId, int quantity)> items)
        {
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var (productId, quantity) in items)
            {
                var product = await _productRepository.GetProductByIdAsync(productId);
                if (product == null || !product.IsActive)
                    throw new InvalidOperationException($"Product {productId} not found or inactive");

                if (product.StockQuantity < quantity)
                    throw new InvalidOperationException($"Insufficient stock for product {productId}");

                totalAmount += product.Price * quantity;
                orderItems.Add(new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    PriceAtTime = product.Price
                });
            }

            order.TotalAmount = totalAmount;
            order.OrderItems = orderItems;
            order.Status = "Pending";

            return await _orderRepository.CreateOrderAsync(order);
        }

        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            if (!ValidStatuses.Contains(status))
                return false;

            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return false;

            var previousStatus = order.Status;
            order.Status = status;
            await _orderRepository.UpdateOrderAsync(order);

            // Reduce stock only when transitioning TO Delivered for the first time.
            if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) &&
                !previousStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                        await _productRepository.UpdateProductAsync(product);
                    }
                }
            }

            return true;
        }

        public async Task<bool> DeliverOrderAsync(int orderId)
            => await UpdateOrderStatusAsync(orderId, "Delivered");

        public async Task<bool> CancelOrderAsync(int orderId)
            => await UpdateOrderStatusAsync(orderId, "Cancelled");
    }
}
