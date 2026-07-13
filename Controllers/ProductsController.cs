using Microsoft.AspNetCore.Mvc;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.DTOs;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory([FromQuery] int? categoryId = null, [FromQuery] int? shopId = null)
        {
            List<Models.Product> products;
            var effectiveCategoryId = shopId.GetValueOrDefault() > 0 ? shopId : categoryId;

            // If categoryId is not provided (or invalid), return all active products.
            if (effectiveCategoryId.HasValue && effectiveCategoryId.Value > 0)
            {
                products = await _productRepository.GetActiveProductsByCategoryAsync(effectiveCategoryId.Value);
            }
            else
            {
                products = (await _productRepository.GetAllProductsAsync())
                    .Where(p => p.IsActive)
                    .ToList();
            }

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
                IsActive = p.IsActive
            }).ToList();

            return Ok(productDtos);
        }
    }
}
