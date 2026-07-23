using GroceryOrderingApp.Backend.DTOs;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.Services;
using GroceryOrderingApp.Backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Dealer")]
    public class DealerController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IDealerNotificationRepository _notificationRepository;
        private readonly IOrderService _orderService;

        public DealerController(
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IDealerNotificationRepository notificationRepository,
            IOrderService orderService)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _notificationRepository = notificationRepository;
            _orderService = orderService;
        }

        [HttpGet("shops")]
        public async Task<IActionResult> GetMyShops()
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var shops = await _categoryRepository.GetShopsByDealerAsync(dealerId);
            var shopDtos = shops.Select(s => new CategoryDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                PhotoUrl = ImagePayloadOptimizer.ExpandForResponse(s.PhotoUrl),
                DealerId = s.DealerId,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(shopDtos);
        }

        [HttpPost("shops")]
        public async Task<IActionResult> CreateShop([FromBody] CreateCategoryRequest request)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Shop name is required");
            }

            var photoUrl = !string.IsNullOrWhiteSpace(request.PhotoUrl)
                ? request.PhotoUrl.Trim()
                : (!string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl.Trim() : null);

            var shop = new Models.Category
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                PhotoUrl = ImagePayloadOptimizer.CompressForStorage(photoUrl),
                DealerId = dealerId,
                IsActive = true
            };

            var created = await _categoryRepository.CreateCategoryAsync(shop);
            return Ok(new CategoryDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                PhotoUrl = ImagePayloadOptimizer.ExpandForResponse(created.PhotoUrl),
                DealerId = created.DealerId,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            });
        }

        [HttpPut("shops/{id}")]
        public async Task<IActionResult> UpdateShop(int id, [FromBody] UpdateCategoryRequest request)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var shop = await _categoryRepository.GetCategoryByIdAsync(id);
            if (shop == null || shop.DealerId != dealerId)
            {
                return NotFound("Shop not found");
            }

            var photoUrl = !string.IsNullOrWhiteSpace(request.PhotoUrl)
                ? request.PhotoUrl.Trim()
                : (!string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl.Trim() : null);

            shop.Name = request.Name?.Trim() ?? shop.Name;
            shop.Description = request.Description?.Trim();
            if (photoUrl != null)
            {
                shop.PhotoUrl = ImagePayloadOptimizer.CompressForStorage(photoUrl);
            }

            shop.IsActive = request.IsActive;
            shop.UpdatedAt = DateTime.UtcNow;
            await _categoryRepository.UpdateCategoryAsync(shop);

            return Ok(new CategoryDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                PhotoUrl = ImagePayloadOptimizer.ExpandForResponse(shop.PhotoUrl),
                DealerId = shop.DealerId,
                IsActive = shop.IsActive,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            });
        }

        [HttpDelete("shops/{id}")]
        public async Task<IActionResult> DeleteShop(int id)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var shop = await _categoryRepository.GetCategoryByIdAsync(id);
            if (shop == null || shop.DealerId != dealerId)
            {
                return NotFound("Shop not found");
            }

            await _categoryRepository.DeleteCategoryAsync(id);
            return Ok(new { message = "Shop deleted successfully" });
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetMyProducts()
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var products = await _productRepository.GetProductsByDealerAsync(dealerId);
            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryId = p.CategoryId,
                ShopId = p.CategoryId,
                PhotoUrl = ImagePayloadOptimizer.ExpandForResponse(p.PhotoUrl),
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return Ok(productDtos);
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0 || request.StockQuantity < 0)
            {
                return BadRequest("Invalid product data");
            }

            var shopId = request.ShopId > 0 ? request.ShopId : request.CategoryId;
            var shop = await _categoryRepository.GetCategoryByIdAsync(shopId);
            if (shop == null || !shop.IsActive || shop.DealerId != dealerId)
            {
                return BadRequest("Shop not found for this dealer");
            }

            var product = new Models.Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = shopId,
                PhotoUrl = ImagePayloadOptimizer.CompressForStorage(string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim()),
                IsActive = true
            };

            var createdProduct = await _productRepository.CreateProductAsync(product);
            return Ok(new ProductDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                Price = createdProduct.Price,
                StockQuantity = createdProduct.StockQuantity,
                CategoryId = createdProduct.CategoryId,
                ShopId = createdProduct.CategoryId,
                PhotoUrl = ImagePayloadOptimizer.ExpandForResponse(createdProduct.PhotoUrl),
                IsActive = createdProduct.IsActive,
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt
            });
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            if (product.Category?.DealerId != dealerId)
            {
                return Forbid();
            }

            if (request.Price < 0 || request.StockQuantity < 0)
            {
                return BadRequest("Invalid product data");
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.PhotoUrl = ImagePayloadOptimizer.CompressForStorage(string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim());
            product.IsActive = request.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateProductAsync(product);

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                ShopId = product.CategoryId,
                PhotoUrl = ImagePayloadOptimizer.ExpandForResponse(product.PhotoUrl),
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            });
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            if (product.Category?.DealerId != dealerId)
            {
                return Forbid();
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateProductAsync(product);
            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            var notifications = await _notificationRepository.GetDealerNotificationsAsync(dealerId);
            return Ok(notifications.Select(n => new DealerNotificationDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList());
        }

        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            await _notificationRepository.MarkAsReadAsync(dealerId, id);
            return Ok(new { message = "Notification marked as read" });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            try
            {
                // Get dealer's products to filter orders
                var products = await _productRepository.GetProductsByDealerAsync(dealerId);
                var dealerProductIds = products.Select(p => p.Id).ToHashSet();

                if (dealerProductIds.Count == 0)
                {
                    return Ok(new
                    {
                        totalOrders = 0,
                        pendingOrders = 0,
                        totalRevenue = 0m,
                        totalProducts = 0
                    });
                }

                // Get all orders and filter by dealer's products
                var allOrders = await _orderService.GetAllOrdersAsync();
                var dealerOrders = allOrders
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => dealerProductIds.Contains(oi.ProductId)))
                    .ToList();

                var totalOrders = dealerOrders.Count;
                var pendingOrders = dealerOrders.Count(o => string.Equals(o.Status, "Pending", StringComparison.OrdinalIgnoreCase));
                var totalRevenue = dealerOrders.Sum(o => o.TotalAmount);
                var totalProducts = products.Count;

                return Ok(new
                {
                    totalOrders,
                    pendingOrders,
                    totalRevenue,
                    totalProducts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error loading dashboard: {ex.Message}" });
            }
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetDealerOrders([FromQuery] bool includeItems = false)
        {
            if (!int.TryParse(User.FindFirst("userId")?.Value, out var dealerId))
            {
                return Unauthorized();
            }

            try
            {
                // Get dealer's products to filter orders
                var products = await _productRepository.GetProductsByDealerAsync(dealerId);
                var dealerProductIds = products.Select(p => p.Id).ToHashSet();

                if (dealerProductIds.Count == 0)
                {
                    return Ok(new List<OrderDto>());
                }

                // Get all orders and filter by dealer's products
                var allOrders = await _orderService.GetAllOrdersAsync();
                var dealerOrders = allOrders
                    .Where(o => o.OrderItems != null && o.OrderItems.Any(oi => dealerProductIds.Contains(oi.ProductId)))
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                var orderDtos = dealerOrders.Select(o => new OrderDto
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
                    Items = includeItems
                        ? o.OrderItems
                            .Where(oi => dealerProductIds.Contains(oi.ProductId))
                            .Select(oi => new OrderItemDto
                            {
                                Id = oi.Id,
                                ProductId = oi.ProductId,
                                ProductName = oi.Product?.Name ?? string.Empty,
                                Quantity = oi.Quantity,
                                PriceAtTime = oi.PriceAtTime
                            }).ToList()
                        : new List<OrderItemDto>()
                }).ToList();

                return Ok(orderDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error loading orders: {ex.Message}" });
            }
        }
    }
}
