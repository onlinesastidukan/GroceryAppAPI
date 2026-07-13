using GroceryOrderingApp.Backend.DTOs;
using GroceryOrderingApp.Backend.Repositories;
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

        public DealerController(
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IDealerNotificationRepository notificationRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _notificationRepository = notificationRepository;
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
                PhotoUrl = s.PhotoUrl,
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
                PhotoUrl = photoUrl,
                DealerId = dealerId,
                IsActive = true
            };

            var created = await _categoryRepository.CreateCategoryAsync(shop);
            return Ok(new CategoryDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                PhotoUrl = created.PhotoUrl,
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
                shop.PhotoUrl = photoUrl;
            }

            shop.IsActive = request.IsActive;
            shop.UpdatedAt = DateTime.UtcNow;
            await _categoryRepository.UpdateCategoryAsync(shop);

            return Ok(new CategoryDto
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                PhotoUrl = shop.PhotoUrl,
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
                PhotoUrl = p.PhotoUrl,
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
                PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim(),
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
                PhotoUrl = createdProduct.PhotoUrl,
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
            product.PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim();
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
                PhotoUrl = product.PhotoUrl,
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
    }
}
