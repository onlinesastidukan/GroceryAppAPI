using GroceryOrderingApp.Backend.Models;

namespace GroceryOrderingApp.Backend.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<Category>> GetActiveCategoriesAsync();
        Task<List<Category>> GetShopsByDealerAsync(int dealerId);
        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
        Task SaveAsync();
    }
}
