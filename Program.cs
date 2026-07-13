using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GroceryOrderingApp.Backend.Data;
using GroceryOrderingApp.Backend.Repositories;
using GroceryOrderingApp.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var rawDatabaseUrl = builder.Configuration["DATABASE_URL"];
var connectionString = string.IsNullOrWhiteSpace(rawDatabaseUrl)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : BuildNpgsqlConnectionString(rawDatabaseUrl);

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDealerNotificationRepository, DealerNotificationRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations and seed database at startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Applying database migrations...");
        try
        {
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations completed successfully.");
        }
        catch (Exception migrationEx)
        {
            logger.LogWarning(migrationEx, "Migration failed, attempting to ensure database schema is created...");
            // Fallback: Ensure the database schema exists
            dbContext.Database.EnsureCreated();
            logger.LogInformation("Database schema ensured via EnsureCreated.");
        }
        
        // Seed initial data
        logger.LogInformation("Seeding database with initial data...");
        var seeder = new GroceryOrderingApp.Backend.DatabaseSeeder(dbContext);
        seeder.SeedAsync().GetAwaiter().GetResult();
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database initialization. The application will continue, but database operations may fail.");
    }
}
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string BuildNpgsqlConnectionString(string input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        throw new ArgumentException("Database connection string is empty.", nameof(input));
    }

    if (input.Contains("${{", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("DATABASE_URL is a template value. Set DATABASE_URL by referencing the Postgres plugin variable in the backend service.");
    }

    if (input.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
    {
        return input;
    }

    if (input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(input);
        var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentException("DATABASE_URL is missing database name.", nameof(input));
        }

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = username,
            Password = password,
            SslMode = Npgsql.SslMode.Require,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }

    return input;
}
