using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.Models;
using GroceryOrderingApp.Backend.DTOs;
using GroceryOrderingApp.Backend.Services;
using Microsoft.AspNetCore.Identity;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderService _orderService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AdminController(
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IOrderService orderService)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderService = orderService;
            _passwordHasher = new PasswordHasher<User>();
        }

        // Users Management
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.MobileNumber) ||
                string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest("Password, FullName, MobileNumber, and Address are required");
            }

            var userId = !string.IsNullOrWhiteSpace(request.UserId)
                ? request.UserId.Trim()
                : request.MobileNumber.Trim();
            var existingUser = await _userRepository.GetUserByUserIdAsync(userId);
            if (existingUser == null)
            {
                existingUser = await _userRepository.GetUserByMobileNumberAsync(request.MobileNumber.Trim());
            }
            if (existingUser != null)
                return BadRequest("UserId or MobileNumber already exists");

            var role = request.Role?.Trim().ToLowerInvariant() switch
            {
                "admin" => "Admin",
                "dealer" => "Dealer",
                _ => "Customer"
            };
            var roleId = role switch
            {
                "Admin" => 1,
                "Dealer" => 3,
                _ => 2
            };

            var user = new User
            {
                UserId = userId,
                FullName = request.FullName,
                MobileNumber = request.MobileNumber,
                Address = request.Address,
                RoleId = roleId,
                CreatedBy = int.Parse(User.FindFirst("userId")?.Value ?? "0"),
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            var createdUser = await _userRepository.CreateUserAsync(user);

            var userDto = new UserDto
            {
                Id = createdUser.Id,
                UserId = createdUser.UserId,
                Role = role,
                FullName = createdUser.FullName,
                MobileNumber = createdUser.MobileNumber,
                Address = createdUser.Address,
                CreatedAt = createdUser.CreatedAt,
                IsActive = createdUser.IsActive,
                CreatedBy = createdUser.CreatedBy
            };

            return Created($"/api/admin/users/{createdUser.Id}", userDto);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                UserId = u.UserId,
                Role = u.Role?.Name ?? "Customer",
                FullName = u.FullName,
                MobileNumber = u.MobileNumber,
                Address = u.Address,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                CreatedBy = u.CreatedBy
            }).ToList();

            return Ok(userDtos);
        }

        // Categories Management
        [HttpGet("categories")]
        [HttpGet("shops")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryRepository.GetAllCategoriesAsync();
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                PhotoUrl = c.PhotoUrl,
                DealerId = c.DealerId,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
            return Ok(categoryDtos);
        }

        [HttpPost("categories")]
        [HttpPost("shops")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Category name is required");

            // Accept photoUrl or imageUrl alias
            var photoUrl = !string.IsNullOrWhiteSpace(request.PhotoUrl)
                ? request.PhotoUrl.Trim()
                : (!string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl.Trim() : null);

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                PhotoUrl = photoUrl,
                DealerId = request.DealerId,
                IsActive = true
            };
            var createdCategory = await _categoryRepository.CreateCategoryAsync(category);

            var categoryDto = new CategoryDto
            {
                Id = createdCategory.Id,
                Name = createdCategory.Name,
                Description = createdCategory.Description,
                PhotoUrl = createdCategory.PhotoUrl,
                DealerId = createdCategory.DealerId,
                IsActive = createdCategory.IsActive
            };

            return Created($"/api/admin/categories/{createdCategory.Id}", categoryDto);
        }

        [HttpDelete("categories/{id}")]
        [HttpDelete("shops/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new { success = false, message = "Category not found" });

            await _categoryRepository.DeleteCategoryAsync(id);
            return Ok(new { success = true, message = "Category deleted" });
        }

        [HttpPut("categories/{id}")]
        [HttpPut("shops/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound("Category not found");

            // Accept photoUrl or imageUrl alias
            var photoUrl = !string.IsNullOrWhiteSpace(request.PhotoUrl)
                ? request.PhotoUrl.Trim()
                : (!string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl.Trim() : null);

            category.Name = request.Name;
            category.Description = request.Description;
            // Only overwrite PhotoUrl if caller supplied one; preserve existing otherwise
            if (photoUrl != null) category.PhotoUrl = photoUrl;
            category.DealerId = request.DealerId;
            category.IsActive = request.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _categoryRepository.UpdateCategoryAsync(category);

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                PhotoUrl = category.PhotoUrl,
                DealerId = category.DealerId,
                IsActive = category.IsActive,
                UpdatedAt = category.UpdatedAt
            };

            return Ok(categoryDto);
        }

        [HttpPatch("categories/{id}")]
        [HttpPatch("shops/{id}")]
        public async Task<IActionResult> PatchCategory(int id, [FromBody] UpdateCategoryRequest request)
            => await UpdateCategory(id, request);

        // Products Management
        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0 || request.StockQuantity < 0)
                return BadRequest("Invalid product data");

            var categoryId = request.ShopId > 0 ? request.ShopId : request.CategoryId;
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
                return BadRequest("Shop not found");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = categoryId,
                PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim(),
                IsActive = true
            };

            var createdProduct = await _productRepository.CreateProductAsync(product);

            var productDto = new ProductDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                Price = createdProduct.Price,
                StockQuantity = createdProduct.StockQuantity,
                CategoryId = createdProduct.CategoryId,
                ShopId = createdProduct.CategoryId,
                PhotoUrl = createdProduct.PhotoUrl,
                IsActive = createdProduct.IsActive
            };

            return Created($"/api/admin/products/{createdProduct.Id}", productDto);
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
                return NotFound("Product not found");

            if (request.Price < 0 || request.StockQuantity < 0)
                return BadRequest("Invalid product data");

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.CategoryId = request.ShopId > 0 ? request.ShopId : request.CategoryId;
            product.PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim();
            product.IsActive = request.IsActive;

            await _productRepository.UpdateProductAsync(product);

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                ShopId = product.CategoryId,
                PhotoUrl = product.PhotoUrl,
                IsActive = product.IsActive
            };

            return Ok(productDto);
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            // Soft delete to preserve historical order references.
            product.IsActive = false;
            await _productRepository.UpdateProductAsync(product);

            return Ok(new { message = "Product deleted successfully" });
        }

        // Orders Management
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserFullName = o.User?.FullName ?? o.User?.UserId ?? string.Empty,
                UserMobileNumber = o.User?.MobileNumber ?? string.Empty,
                UserAddress = o.User?.Address ?? string.Empty,
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

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound("Order not found");

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserFullName = order.User?.FullName ?? order.User?.UserId ?? string.Empty,
                UserMobileNumber = order.User?.MobileNumber ?? string.Empty,
                UserAddress = order.User?.Address ?? string.Empty,
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

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Status is required");

            var success = await _orderService.UpdateOrderStatusAsync(id, request.Status);
            if (!success)
                return BadRequest($"Cannot update order status to '{request.Status}'");

            return Ok(new { message = $"Order status updated to '{request.Status}'" });
        }

        [HttpPut("orders/{id}/deliver")]
        public async Task<IActionResult> DeliverOrder(int id)
        {
            var success = await _orderService.DeliverOrderAsync(id);
            if (!success)
                return BadRequest("Cannot deliver this order");

            return Ok(new { message = "Order delivered successfully" });
        }

        [HttpPut("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var success = await _orderService.CancelOrderAsync(id);
            if (!success)
                return BadRequest("Cannot cancel this order");

            return Ok(new { message = "Order cancelled successfully" });
        }
    }
}
