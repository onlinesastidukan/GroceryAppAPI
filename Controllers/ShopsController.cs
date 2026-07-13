using GroceryOrderingApp.Backend.DTOs;
using GroceryOrderingApp.Backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopsController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;

        public ShopsController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveShops()
        {
            var shops = await _categoryRepository.GetActiveCategoriesAsync();
            var shopDtos = shops.Select(c => new CategoryDto
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

            return Ok(shopDtos);
        }
    }
}
