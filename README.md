# Grocery Ordering Application - Backend API

## Overview
Production-ready ASP.NET Core Web API for a Grocery Ordering System with JWT authentication, role-based authorization, and comprehensive order management.

## Technology Stack
- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server (Express for development)
- **ORM**: Entity Framework Core 8.0
- **Authentication**: JWT Token
- **API Documentation**: Swagger/OpenAPI

## Project Structure
```
GroceryOrderingApp.Backend/
├── Models/                 # Database entities
├── DTOs/                   # Data Transfer Objects
├── Controllers/            # API endpoints
├── Services/              # Business logic
├── Repositories/          # Data access layer
├── Data/                  # EF Core context & migrations
├── Program.cs             # Application startup
├── DatabaseSeeder.cs      # Initial data seeding
└── appsettings.json       # Configuration
```

## Key Features
✅ Admin, Dealer & Customer roles
✅ User management (Admin only)
✅ Shop (category alias) & Product management
✅ Shopping cart with stock validation
✅ Order placement & processing
✅ Admin delivery confirmation (with stock reduction)
✅ Order cancellation
✅ Dealer product and notification APIs
✅ Secure JWT authentication
✅ Comprehensive API documentation via Swagger

## Prerequisites
- .NET 8.0 SDK
- SQL Server (Express) or SQL Server Developer Edition
- Visual Studio 2022 or VS Code with C# extension

## Installation & Setup

### 1. Database Setup
```bash
# Open SQL Server Management Studio (SSMS) and run:
CREATE DATABASE GroceryOrderingDb;
```

### 2. Update Connection String
Edit `appsettings.json`:
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=GroceryOrderingDb;Trusted_Connection=true;Encrypt=false;"
}
```

### 3. Apply Migrations
```bash
cd GroceryOrderingApp.Backend
dotnet ef database update
```

This will:
- Create all required tables
- Seed roles (Admin, Customer)
- Seed default admin user (admin / Admin@123)
- Seed sample products and categories

### 4. Run the Application
```bash
dotnet run
```

Application will start at: `https://localhost:7001`

## API Endpoints

### Authentication
- **POST** `/api/auth/login` - Dealer/Admin login (mobile number as user id for dealer)
- **POST** `/api/auth/register` - Dealer registration

### Admin Endpoints (requires Admin role)
- **POST** `/api/admin/users` - Create new user
- **GET** `/api/admin/users` - Get all users
- **POST** `/api/admin/shops` - Create shop
- **PUT** `/api/admin/shops/{id}` - Update shop
- **DELETE** `/api/admin/shops/{id}` - Delete shop
- **POST** `/api/admin/products` - Create product
- **PUT** `/api/admin/products/{id}` - Update product
- **GET** `/api/admin/orders` - Get all orders
- **GET** `/api/admin/orders/{id}` - Get order details
- **PUT** `/api/admin/orders/{id}/deliver` - Mark as delivered (reduces stock)
- **PUT** `/api/admin/orders/{id}/cancel` - Cancel order

### Public/Customer Endpoints (no login required for order placement)
- **GET** `/api/shops` - Get active shops
- **GET** `/api/categories` - Backward-compatible categories endpoint
- **GET** `/api/products?shopId=1` - Get products by shop
- **POST** `/api/orders` - Place new order with mobile + address
- **GET** `/api/orders/my` - Get my orders (authenticated users only)
- **GET** `/api/orders/{id}` - Get order details (authenticated users only)

### Dealer Endpoints (requires Dealer role)
- **GET** `/api/dealer/shops` - Get dealer-assigned shops
- **GET** `/api/dealer/products` - Get dealer products
- **POST** `/api/dealer/products` - Add dealer product
- **PUT** `/api/dealer/products/{id}` - Update dealer product
- **DELETE** `/api/dealer/products/{id}` - Delete dealer product
- **GET** `/api/dealer/notifications` - Get order notifications
- **PUT** `/api/dealer/notifications/{id}/read` - Mark notification as read
- **GET** `/api/orders/my` - Get my orders
- **GET** `/api/orders/{id}` - Get order details

## API Documentation
Visit `https://localhost:7001/swagger` for interactive API documentation.

## Default Credentials
```
UserId: admin
Password: Admin@123
Role: Admin
```

## Database Schema

### Tables
1. **Roles** - Admin, Dealer, Customer
2. **Users** - UserId, PasswordHash, RoleId, IsActive
3. **Categories (Shops)** - Name, DealerId, IsActive
4. **Products** - Name, Description, Price, StockQuantity, CategoryId, IsActive
5. **Orders** - UserId, CustomerMobileNumber, DeliveryAddress, Status, TotalAmount
6. **OrderItems** - OrderId, ProductId, Quantity, PriceAtTime
7. **DealerNotifications** - DealerId, OrderId, Message, IsRead

## Security Features
✅ Password hashing using ASP.NET Identity PasswordHasher
✅ JWT token-based authentication
✅ Role-based authorization (Admin/Customer)
✅ HTTPS enforcement
✅ Input validation on all endpoints
✅ SQL injection prevention (EF Core)
✅ Negative stock prevention

## Deployment to Azure

### Prerequisites
- Azure subscription
- Azure SQL Database
- Azure App Service

### Steps
1. **Create Azure SQL Database**
   ```bash
   az sql server create --resource-group myResourceGroup --name myserver --admin-user adminuser --admin-password P@ssw0rd!
   az sql db create --resource-group myResourceGroup --server myserver --name GroceryOrderingDb
   ```

2. **Update Connection String**
   Set in Azure App Service Application Settings:
   ```
   ConnectionStrings__DefaultConnection=Server=tcp:myserver.database.windows.net,1433;Initial Catalog=GroceryOrderingDb;Persist Security Info=False;User ID=adminuser;Password=P@ssw0rd!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

3. **Publish to Azure**
   ```bash
   dotnet publish -c Release -o ./publish
   # Use Azure portal or VS to deploy the publish folder to App Service
   ```

4. **Update JWT Secret in Azure**
   Set in Application Settings:
   ```
   Jwt__Secret=YourSecureSecretKeyAtLeast32Characters1234567890
   ```

## Load Testing
For production readiness, test with:
- 100+ concurrent connections
- 1000+ orders per minute
- Validate stock atomicity

## Cost Optimization
- **Dev**: Free SQL Server Express
- **Production**: 
  - Azure App Service Basic: ~$13/month
  - Azure SQL Database Basic: ~$5/month
  - Total: ~$20/month

## Support & Maintenance
- Monitor application logs
- Regular database backups
- Update dependencies quarterly
- Review security patches monthly

## License
MIT
