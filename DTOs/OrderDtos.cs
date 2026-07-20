using System.Text.Json.Serialization;

namespace GroceryOrderingApp.Backend.DTOs
{
    public class CreateOrderRequest
    {
        public string? CustomerName { get; set; }
        
        public string? CustomerMobileNumber { get; set; }
        
        // Alias for mobile compatibility
        [JsonPropertyName("mobileNumber")]
        public string? MobileNumber
        {
            get => CustomerMobileNumber;
            set => CustomerMobileNumber = value;
        }
        
        public string? DeliveryAddress { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; } = string.Empty;

        [JsonPropertyName("userMobileNumber")]
        public string UserMobileNumber { get; set; } = string.Empty;

        [JsonPropertyName("userAddress")]
        public string UserAddress { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("deliveryAddress")]
        public string? DeliveryAddress { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customerMobileNumber")]
        public string? CustomerMobileNumber { get; set; }

        [JsonPropertyName("items")]
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("priceAtTime")]
        public decimal PriceAtTime { get; set; }
    }
}
