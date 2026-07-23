using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.Models;
using GroceryOrderingApp.Backend.DTOs;
using GroceryOrderingApp.Backend.Helpers;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCategories([FromQuery] bool includeImage = false)
        {
            var categories = await _categoryRepository.GetActiveCategoriesAsync();
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                PhotoUrl = includeImage ? ImagePayloadOptimizer.ExpandForResponse(c.PhotoUrl) : null,
                DealerId = c.DealerId,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            return Ok(categoryDtos);
        }
    }
}
