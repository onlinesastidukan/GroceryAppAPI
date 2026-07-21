using GroceryOrderingApp.Backend.DTOs;
using GroceryOrderingApp.Backend.Models;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc; using Microsoft.Extensions.Logging;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IDealerNotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<User> _passwordHasher;         private readonly ILogger<OrdersController> _logger;
        private readonly INotificationService _notificationService;

        public OrdersController(
            IOrderService orderService,
            IDealerNotificationRepository notificationRepository,
            IUserRepository userRepository,             ILogger<OrdersController> logger,
            INotificationService notificationService)         {
            _orderService = orderService;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
            _notificationService = notificationService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)             {                 _logger.LogWarning("CreateOrder rejected: empty items. Mobile={Mobile}, AddressProvided={HasAddress}", request.CustomerMobileNumber, !string.IsNullOrWhiteSpace(request.DeliveryAddress));                 return BadRequest("Order must contain at least one item");             }

            var userIdClaim = User.FindFirst("userId")?.Value;
            int userId;
            if (!int.TryParse(userIdClaim, out userId))
            {
                if (string.IsNullOrWhiteSpace(request.CustomerMobileNumber) || string.IsNullOrWhiteSpace(request.DeliveryAddress))                 {                     _logger.LogWarning("CreateOrder guest validation failed. MobilePresent={HasMobile}, AddressPresent={HasAddress}", !string.IsNullOrWhiteSpace(request.CustomerMobileNumber), !string.IsNullOrWhiteSpace(request.DeliveryAddress));                     return BadRequest("CustomerMobileNumber and DeliveryAddress are required for guest orders");                 }

                var customerUser = await GetOrCreateGuestCustomerAsync(request);
                userId = customerUser.Id;
            }

            try
            {
                var order = new Order
                {
                    UserId = userId,
                    DeliveryAddress = request.DeliveryAddress?.Trim(),
                    CustomerName = request.CustomerName?.Trim(),
                    CustomerMobileNumber = request.CustomerMobileNumber?.Trim()
                };
                var items = request.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

                _logger.LogInformation("CreateOrder request accepted. Mobile={Mobile}, ItemCount={ItemCount}, UserId={UserId}", request.CustomerMobileNumber, items.Count, userId);                 var createdOrder = await _orderService.CreateOrderAsync(order, items);
                var fullOrder = await _orderService.GetOrderByIdAsync(createdOrder.Id);
                if (fullOrder == null)
                {
                    _logger.LogError("CreateOrder failed after service call: fullOrder null. Mobile={Mobile}, UserId={UserId}", request.CustomerMobileNumber, userId);                     return BadRequest("Order creation failed");
                }

                await CreateDealerNotificationsAsync(fullOrder);

                var orderDto = new OrderDto
                {
                    Id = fullOrder.Id,
                    UserId = fullOrder.UserId,
                    OrderDate = fullOrder.OrderDate,
                    Status = fullOrder.Status,
                    TotalAmount = fullOrder.TotalAmount,
                    DeliveryAddress = fullOrder.DeliveryAddress,
                    CustomerName = fullOrder.CustomerName,
                    CustomerMobileNumber = fullOrder.CustomerMobileNumber,
                    Items = fullOrder.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product?.Name ?? "",
                        Quantity = oi.Quantity,
                        PriceAtTime = oi.PriceAtTime
                    }).ToList()
                };

                _logger.LogInformation("CreateOrder succeeded. OrderId={OrderId}, Mobile={Mobile}, Total={Total}", fullOrder.Id, request.CustomerMobileNumber, fullOrder.TotalAmount);                 return Created($"/api/orders/{fullOrder.Id}", orderDto);
            }
            catch (InvalidOperationException ex)             {                 _logger.LogWarning(ex, "CreateOrder business validation failed. Mobile={Mobile}, ItemCount={ItemCount}", request.CustomerMobileNumber, request.Items?.Count ?? 0);                 return BadRequest(ex.Message);             }             catch (Exception ex)             {                 _logger.LogError(ex, "CreateOrder unexpected error. Mobile={Mobile}, ItemCount={ItemCount}", request.CustomerMobileNumber, request.Items?.Count ?? 0);                 return StatusCode(500, "Order creation failed due to an internal error");             }
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var orders = await _orderService.GetOrdersByUserAsync(userId);
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                DeliveryAddress = o.DeliveryAddress,
                CustomerName = o.CustomerName,
                CustomerMobileNumber = o.CustomerMobileNumber,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    PriceAtTime = oi.PriceAtTime
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }


        [HttpGet("mobile/{mobileNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveOrdersByMobile(string mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
            {
                return BadRequest("Mobile number is required");
            }

            var orders = await _orderService.GetActiveOrdersByMobileAsync(mobileNumber);
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserFullName = o.User?.FullName ?? string.Empty,
                UserMobileNumber = o.User?.MobileNumber ?? string.Empty,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                DeliveryAddress = o.DeliveryAddress,
                CustomerName = o.CustomerName,
                CustomerMobileNumber = o.CustomerMobileNumber,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    Quantity = oi.Quantity,
                    PriceAtTime = oi.PriceAtTime
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != userId)
                return NotFound("Order not found");

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserFullName = order.User?.FullName ?? string.Empty,
                UserMobileNumber = order.User?.MobileNumber ?? string.Empty,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                DeliveryAddress = order.DeliveryAddress,
                CustomerName = order.CustomerName,
                CustomerMobileNumber = order.CustomerMobileNumber,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    PriceAtTime = oi.PriceAtTime
                }).ToList()
            };

            return Ok(orderDto);
        }

        private async Task<User> GetOrCreateGuestCustomerAsync(CreateOrderRequest request)
        {
            var mobile = request.CustomerMobileNumber!.Trim();
            var existingUser = await _userRepository.GetUserByMobileNumberAsync(mobile);
            if (existingUser != null)
            {
                existingUser.Address = request.DeliveryAddress!.Trim();
                if (!string.IsNullOrWhiteSpace(request.CustomerName))
                {
                    existingUser.FullName = request.CustomerName.Trim();
                }

                await _userRepository.UpdateUserAsync(existingUser);
                return existingUser;
            }

            var guestCustomer = new User
            {
                UserId = mobile,
                FullName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Guest Customer" : request.CustomerName.Trim(),
                MobileNumber = mobile,
                Address = request.DeliveryAddress!.Trim(),
                RoleId = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            guestCustomer.PasswordHash = _passwordHasher.HashPassword(guestCustomer, $"{mobile}!Guest");
            return await _userRepository.CreateUserAsync(guestCustomer);
        }

        private async Task CreateDealerNotificationsAsync(Order fullOrder)
        {
            var dealerIds = fullOrder.OrderItems
                .Select(oi => oi.Product?.Category?.DealerId)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Distinct()
                .ToList();

            foreach (var dealerId in dealerIds)
            {
                await _notificationRepository.CreateAsync(new DealerNotification
                {
                    DealerId = dealerId,
                    OrderId = fullOrder.Id,
                    Message = $"New order #{fullOrder.Id} placed.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                // Send FCM push notification
                try
                {
                    var customerName = string.IsNullOrWhiteSpace(fullOrder.CustomerName)
                        ? "Customer"
                        : fullOrder.CustomerName;

                    await _notificationService.SendOrderNotificationAsync(
                        dealerId,
                        fullOrder.Id,
                        customerName,
                        fullOrder.TotalAmount);

                    _logger.LogInformation("FCM push sent for OrderId={OrderId}, DealerId={DealerId}", fullOrder.Id, dealerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send FCM push for OrderId={OrderId}, DealerId={DealerId}", fullOrder.Id, dealerId);
                }
            }
        }
    }
}








