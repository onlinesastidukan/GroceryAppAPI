using GroceryOrderingApp.Backend.Data;
using GroceryOrderingApp.Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace GroceryOrderingApp.Backend
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public DatabaseSeeder(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task SeedAsync()
        {
            // Seed Roles
            var requiredRoles = new[]
            {
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Customer" },
                new Role { Id = 3, Name = "Dealer" }
            };

            foreach (var role in requiredRoles)
            {
                if (!_context.Roles.Any(r => r.Name == role.Name))
                {
                    _context.Roles.Add(role);
                }
            }
            await _context.SaveChangesAsync();

            // Seed Admin User
            if (!_context.Users.Any(u => u.RoleId == 1))
            {
                var adminUser = new User
                {
                    UserId = "admin",
                    FullName = "Admin User",
                    MobileNumber = "9999999999",
                    Address = "Admin HQ",
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, "Admin@123");
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
            }

            if (!_context.Users.Any(u => u.RoleId == 3))
            {
                var dealerUser = new User
                {
                    UserId = "8888888888",
                    FullName = "Default Dealer",
                    MobileNumber = "8888888888",
                    Address = "Dealer Hub",
                    RoleId = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                dealerUser.PasswordHash = _passwordHasher.HashPassword(dealerUser, "Dealer@123");
                _context.Users.Add(dealerUser);
                await _context.SaveChangesAsync();
            }

            // Seed Categories with Hindi names
            if (!_context.Categories.Any())
            {
                var defaultDealerId = _context.Users.FirstOrDefault(u => u.RoleId == 3)?.Id;
                var categories = new[]
                {
                    new Category { Name = "Sabji ki dukan", DealerId = defaultDealerId, IsActive = true },        // Vegetable shop
                    new Category { Name = "Parchun ki dukan", DealerId = defaultDealerId, IsActive = true },      // Grocery/Spice shop
                    new Category { Name = "Cake ki dukan", DealerId = defaultDealerId, IsActive = true },         // Bakery shop
                    new Category { Name = "Tailor ki dukan", DealerId = defaultDealerId, IsActive = true },       // Tailoring shop
                    new Category { Name = "Fruits ki dukan", DealerId = defaultDealerId, IsActive = true }        // Fruit shop
                };
                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();
            }

            // Seed Products
            if (!_context.Products.Any())
            {
                var products = new[]
                {
                    // Sabji ki dukan (Vegetables)
                    new Product { Name = "Tomato", Description = "Fresh red tomato", Price = 40m, StockQuantity = 100, CategoryId = 1, IsActive = true },
                    new Product { Name = "Onion", Description = "Golden onion", Price = 30m, StockQuantity = 150, CategoryId = 1, IsActive = true },
                    new Product { Name = "Potato", Description = "Fresh potato", Price = 25m, StockQuantity = 200, CategoryId = 1, IsActive = true },
                    new Product { Name = "Carrot", Description = "Orange carrot", Price = 35m, StockQuantity = 80, CategoryId = 1, IsActive = true },

                    // Fruits ki dukan (Fruits)
                    new Product { Name = "Apple", Description = "Red apple", Price = 100m, StockQuantity = 50, CategoryId = 5, IsActive = true },
                    new Product { Name = "Banana", Description = "Yellow banana", Price = 25m, StockQuantity = 120, CategoryId = 5, IsActive = true },
                    new Product { Name = "Orange", Description = "Sweet orange", Price = 50m, StockQuantity = 60, CategoryId = 5, IsActive = true },
                    new Product { Name = "Mango", Description = "Sweet mango", Price = 80m, StockQuantity = 40, CategoryId = 5, IsActive = true },

                    // Parchun ki dukan (Grocery/Spices)
                    new Product { Name = "Rice (1kg)", Description = "Basmati rice", Price = 80m, StockQuantity = 150, CategoryId = 2, IsActive = true },
                    new Product { Name = "Wheat (1kg)", Description = "Whole wheat flour", Price = 40m, StockQuantity = 200, CategoryId = 2, IsActive = true },
                    new Product { Name = "Turmeric", Description = "Turmeric powder", Price = 45m, StockQuantity = 100, CategoryId = 2, IsActive = true },
                    new Product { Name = "Chili Powder", Description = "Red chili powder", Price = 50m, StockQuantity = 80, CategoryId = 2, IsActive = true },

                    // Cake ki dukan (Bakery)
                    new Product { Name = "Bread", Description = "Whole wheat bread", Price = 30m, StockQuantity = 80, CategoryId = 3, IsActive = true },
                    new Product { Name = "Cake", Description = "Vanilla cake", Price = 150m, StockQuantity = 30, CategoryId = 3, IsActive = true },
                    new Product { Name = "Cookies", Description = "Chocolate cookies", Price = 60m, StockQuantity = 100, CategoryId = 3, IsActive = true },

                    // Tailor ki dukan (Tailoring)
                    new Product { Name = "Cotton Fabric (1 meter)", Description = "Premium cotton fabric", Price = 200m, StockQuantity = 50, CategoryId = 4, IsActive = true },
                    new Product { Name = "Silk Fabric (1 meter)", Description = "Pure silk fabric", Price = 500m, StockQuantity = 25, CategoryId = 4, IsActive = true },
                    new Product { Name = "Thread", Description = "Colorful embroidery thread", Price = 20m, StockQuantity = 200, CategoryId = 4, IsActive = true }
                };
                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();
            }
        }
    }
}
