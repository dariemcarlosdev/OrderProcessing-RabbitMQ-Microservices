# Generic Mapping Extensions - Usage Guide

## Overview

The `MappingExtensions` class now provides **generic mapping methods** that can map any domain model to any DTO type, making the mapping logic reusable across the entire application.

## Generic Methods

### 1. `MapTo<TSource, TDestination>`

Maps a single domain model to a DTO using a provided mapping function.

**Signature:**
```csharp
public static TDestination MapTo<TSource, TDestination>(
    this TSource source, 
    Func<TSource, TDestination> mapper)
    where TSource : class
    where TDestination : class
```

**Usage Examples:**

#### **Inline Mapping**
```csharp
// Map an Order to OrderResponseDto inline
var orderDto = order.MapTo<Order, OrderResponseDto>(o => new OrderResponseDto
{
    Id = o.Id,
    CustomerName = o.CustomerName,
    ProductName = o.ProductName,
    Quantity = o.Quantity,
    TotalAmount = o.TotalAmount,
    Status = o.Status.ToString(),
    CreatedAt = o.CreatedAt
});
```

#### **Using a Static Mapper Function**
```csharp
// Define a static mapper function
public static OrderResponseDto OrderMapper(Order order)
{
    return new OrderResponseDto
    {
        Id = order.Id,
        CustomerName = order.CustomerName,
        // ... other properties
    };
}

// Use the mapper
var orderDto = order.MapTo(OrderMapper);
```

#### **Mapping to Different DTO Types**
```csharp
// Map to a summary DTO
var summary = order.MapTo<Order, OrderSummaryDto>(o => new OrderSummaryDto
{
    OrderId = o.Id,
    Total = o.TotalAmount,
    Status = o.Status.ToString()
});

// Map to a detailed DTO with additional info
var detailed = order.MapTo<Order, OrderDetailedDto>(o => new OrderDetailedDto
{
    Id = o.Id,
    CustomerName = o.CustomerName,
    ProductName = o.ProductName,
    Quantity = o.Quantity,
    TotalAmount = o.TotalAmount,
    Status = o.Status.ToString(),
    CreatedAt = o.CreatedAt,
    // Additional calculated fields
    IsExpedited = o.TotalAmount > 1000,
    EstimatedDelivery = DateTime.UtcNow.AddDays(3)
});
```

---

### 2. `MapToList<TSource, TDestination>`

Maps a collection of domain models to a collection of DTOs.

**Signature:**
```csharp
public static IEnumerable<TDestination> MapToList<TSource, TDestination>(
    this IEnumerable<TSource> source, 
    Func<TSource, TDestination> mapper)
    where TSource : class
    where TDestination : class
```

**Usage Examples:**

#### **Mapping Lists**
```csharp
// Map a list of orders to DTOs
List<Order> orders = GetOrders();

var orderDtos = orders.MapToList<Order, OrderResponseDto>(o => new OrderResponseDto
{
    Id = o.Id,
    CustomerName = o.CustomerName,
    ProductName = o.ProductName,
    Quantity = o.Quantity,
    TotalAmount = o.TotalAmount,
    Status = o.Status.ToString(),
    CreatedAt = o.CreatedAt
});

// Convert to List if needed
var dtoList = orderDtos.ToList();
```

#### **Using with LINQ**
```csharp
// Filter and map in one expression
var recentOrders = orders
    .Where(o => o.CreatedAt > DateTime.UtcNow.AddDays(-7))
    .MapToList<Order, OrderResponseDto>(o => new OrderResponseDto
    {
        Id = o.Id,
        CustomerName = o.CustomerName,
        // ... other properties
    })
    .ToList();
```

#### **Reusing Mapper Functions**
```csharp
// Define a reusable mapper
Func<Order, OrderResponseDto> orderMapper = o => new OrderResponseDto
{
    Id = o.Id,
    CustomerName = o.CustomerName,
    ProductName = o.ProductName,
    Quantity = o.Quantity,
    TotalAmount = o.TotalAmount,
    Status = o.Status.ToString(),
    CreatedAt = o.CreatedAt
};

// Use it for multiple collections
var todayOrders = todaysOrders.MapToList(orderMapper).ToList();
var weekOrders = weeklyOrders.MapToList(orderMapper).ToList();
```

---

## Specific Order Mapping Methods

For convenience, **Order-specific** mapping methods are still available:

### 1. `ToResponseDto()` (Extension Method)

**Recommended for Order to OrderResponseDto mapping.**

```csharp
// Simple, clean syntax
var dto = order.ToResponseDto();
```

### 2. `ToOrderResponseDto()` (Static Method)

**Use when you need a Func<Order, OrderResponseDto> reference.**

```csharp
// Pass as a function reference
var dtos = orders.MapToList(MappingExtensions.ToOrderResponseDto);
```

---

## Real-World Usage Examples

### Example 1: Controller Endpoint
```csharp
[HttpGet]
public async Task<ActionResult<ApiResponse<List<OrderResponseDto>>>> GetOrders()
{
    try
    {
        var orders = await _orderRepository.GetAllAsync();
        
        // Option 1: Using specific method
        var orderDtos = orders.Select(o => o.ToResponseDto()).ToList();
        
        // Option 2: Using generic method
        var orderDtos = orders.MapToList<Order, OrderResponseDto>(o => o.ToResponseDto()).ToList();
        
        // Option 3: Using static mapper with generic method
        var orderDtos = orders.MapToList(MappingExtensions.ToOrderResponseDto).ToList();
        
        var response = ApiResponse<List<OrderResponseDto>>.SuccessResponse(
            orderDtos,
            "Orders retrieved successfully");
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving orders");
        var errorResponse = ApiResponse<List<OrderResponseDto>>.FailureResponse(
            "An error occurred while retrieving orders",
            ex.Message);
        return StatusCode(500, errorResponse);
    }
}
```

### Example 2: Service Layer
```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    
    public async Task<List<OrderSummaryDto>> GetOrderSummariesAsync(string customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        
        // Map to summary DTO with calculated fields
        return orders.MapToList<Order, OrderSummaryDto>(o => new OrderSummaryDto
        {
            OrderId = o.Id,
            ProductName = o.ProductName,
            Total = o.TotalAmount,
            Status = o.Status.ToString(),
            DaysOld = (DateTime.UtcNow - o.CreatedAt).Days,
            IsRecent = (DateTime.UtcNow - o.CreatedAt).Days <= 7
        }).ToList();
    }
}
```

### Example 3: Complex Mapping
```csharp
// Map to a DTO that combines data from multiple sources
public OrderDetailedDto MapToDetailedDto(Order order, List<OrderItem> items, Customer customer)
{
    return order.MapTo<Order, OrderDetailedDto>(o => new OrderDetailedDto
    {
        // Order properties
        Id = o.Id,
        CustomerName = o.CustomerName,
        ProductName = o.ProductName,
        Quantity = o.Quantity,
        TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        
        // Additional data from other sources
        Items = items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Price = i.Price
        }).ToList(),
        
        CustomerEmail = customer.Email,
        CustomerPhone = customer.Phone,
        
        // Calculated fields
        EstimatedDelivery = CalculateDeliveryDate(o),
        IsExpedited = o.TotalAmount > 1000
    });
}
```

---

## Benefits of Generic Mapping

### ? **Reusability**
- One method works for all domain model ? DTO mappings
- No need to create specific extension methods for each type

### ? **Flexibility**
- Inline mapping for simple cases
- Reusable mapper functions for complex scenarios
- Easy to add calculated fields

### ? **Type Safety**
- Generic constraints ensure only reference types
- Compile-time checking of mappings
- IntelliSense support

### ? **Null Safety**
- Built-in null checks using `ArgumentNullException.ThrowIfNull`
- Prevents null reference exceptions

### ? **Maintainability**
- Clear, declarative mapping logic
- Easy to modify mappings in one place
- Self-documenting code

---

## When to Use Each Approach

| Scenario | Recommended Method | Example |
|----------|-------------------|---------|
| **Single Order mapping** | `ToResponseDto()` | `var dto = order.ToResponseDto();` |
| **List of Orders** | `Select` + `ToResponseDto()` | `orders.Select(o => o.ToResponseDto())` |
| **Custom DTO** | `MapTo<T, T>` inline | `order.MapTo<Order, CustomDto>(...)` |
| **Reusable mapper** | `MapTo` + static method | `orders.MapToList(MyMapper)` |
| **Collection mapping** | `MapToList<T, T>` | `orders.MapToList<Order, Dto>(...)` |
| **Complex mapping** | `MapTo` with function | `order.MapTo(o => new Dto { ... })` |

---

## Best Practices

### ? **DO:**
- Use `ToResponseDto()` for simple Order to OrderResponseDto mappings
- Use `MapTo` for custom or one-off mappings
- Use `MapToList` when mapping collections
- Create static mapper functions for reusable complex mappings
- Add null checks in custom mapper functions

### ? **DON'T:**
- Mix mapping logic with business logic
- Create mapper functions inside loops
- Use reflection-based mapping (performance cost)
- Forget to handle null values in custom mappers

---

## Performance Considerations

### **Inline Mapping (Fastest)**
```csharp
// No function allocation overhead
var dto = order.ToResponseDto();
```

### **Function Reference (Fast)**
```csharp
// Function pointer, minimal overhead
var dtos = orders.MapToList(MappingExtensions.ToOrderResponseDto);
```

### **Lambda Expression (Slightly Slower)**
```csharp
// Lambda allocation per call
var dtos = orders.MapToList<Order, OrderResponseDto>(o => new OrderResponseDto { ... });
```

**Recommendation:** For hot paths (called frequently), use `ToResponseDto()` or static mapper functions. For cold paths, use inline lambdas for clarity.

---

## Migration from Old Pattern

### **Before (Specific Methods Only):**
```csharp
public static OrderResponseDto ToResponseDto(this Order order) { ... }
public static CustomerResponseDto ToResponseDto(this Customer customer) { ... }
public static ProductResponseDto ToResponseDto(this Product product) { ... }
// ... one method per type pair
```

### **After (Generic + Specific):**
```csharp
// Generic method for any type
public static TDto MapTo<TSource, TDto>(this TSource source, Func<TSource, TDto> mapper) { ... }

// Keep specific methods for convenience
public static OrderResponseDto ToResponseDto(this Order order) { ... }

// Now you can also:
var customDto = order.MapTo<Order, CustomDto>(o => new CustomDto { ... });
```

---

## Future Enhancements

The generic mapping pattern opens doors for:

1. **AutoMapper Integration**
   ```csharp
   public static TDto MapTo<TSource, TDto>(this TSource source)
       where TSource : class
       where TDto : class
   {
       return AutoMapper.Map<TDto>(source);
   }
   ```

2. **Async Mapping** (for mapping with database lookups)
   ```csharp
   public static async Task<TDto> MapToAsync<TSource, TDto>(
       this TSource source, 
       Func<TSource, Task<TDto>> asyncMapper)
   ```

3. **Validation During Mapping**
   ```csharp
   public static TDto MapToWithValidation<TSource, TDto>(
       this TSource source, 
       Func<TSource, TDto> mapper,
       Action<TDto> validator)
   ```

---

## Summary

The generic mapping extensions provide:
- ? **Flexibility** - Map any type to any other type
- ? **Reusability** - One method for all mappings
- ? **Type Safety** - Compile-time checking
- ? **Performance** - Minimal overhead
- ? **Simplicity** - Clean, declarative syntax
- ? **Backward Compatibility** - Specific methods still available

Use the **generic methods** for flexibility and the **specific methods** for convenience!

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-21  
**Author:** Dariem Carlos Macias
