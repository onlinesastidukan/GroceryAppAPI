using GroceryOrderingApp.Backend.Models;

namespace GroceryOrderingApp.Backend.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetProductByIdAsync(int id);
        Task<List<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<List<Product>> GetActiveProductsByCategoryAsync(int categoryId);
        Task<List<Product>> GetProductsByDealerAsync(int dealerId);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task SaveAsync();
    }
}
