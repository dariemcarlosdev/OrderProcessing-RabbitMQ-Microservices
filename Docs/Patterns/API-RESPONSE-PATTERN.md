# API Response Pattern Documentation

## Overview

The OrderFlow.Core API uses a standardized `ApiResponse<T>` wrapper pattern for all endpoints, providing consistent response structure, clear success/failure indication, and better error handling.

## Response Structure

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* actual response data */ },
  "errors": null,
  "timestamp": "2024-01-20T10:30:00Z"
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `success` | boolean | Indicates whether the operation was successful |
| `message` | string | Human-readable message describing the result |
| `data` | T (generic) | The actual response payload (type varies by endpoint) |
| `errors` | string[] | List of error messages (only present on failure) |
| `timestamp` | DateTime | UTC timestamp when the response was generated |

## Success Response Example

### Create Order Success
**Request:** `POST /api/orders`
```json
{
  "customerName": "John Doe",
  "productName": "Laptop",
  "quantity": 1,
  "totalAmount": 1299.99
}
```

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Order created and published successfully",
  "data": {
    "order": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "customerName": "John Doe",
      "productName": "Laptop",
      "quantity": 1,
      "totalAmount": 1299.99,
      "status": "Created",
      "createdAt": "2024-01-20T10:30:00Z"
    }
  },
  "errors": null,
  "timestamp": "2024-01-20T10:30:00Z"
}
```

### Payment Verification Success
**Request:** `POST /api/orders/{orderId}/payment`

**Response:** `200 OK`
```json
{
  "success": true,
  "message": "Payment verification event published successfully",
  "data": {
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventType": "payment.verified"
  },
  "errors": null,
  "timestamp": "2024-01-20T10:35:00Z"
}
```

## Error Response Example

### Validation Error
**Response:** `500 Internal Server Error`
```json
{
  "success": false,
  "message": "An error occurred while creating the order",
  "data": null,
  "errors": [
    "Connection to RabbitMQ failed: Unable to connect to broker"
  ],
  "timestamp": "2024-01-20T10:30:00Z"
}
```

## All Endpoints Response Types

### 1. Create Order
- **Endpoint:** `POST /api/orders`
- **Success Response:** `ApiResponse<CreateOrderResponseDto>`
- **Data Structure:**
  ```typescript
  {
    order: {
      id: string (GUID),
      customerName: string,
      productName: string,
      quantity: number,
      totalAmount: number,
      status: string,
      createdAt: DateTime
    }
  }
  ```

### 2. Verify Payment
- **Endpoint:** `POST /api/orders/{orderId}/payment`
- **Success Response:** `ApiResponse<OrderOperationResponseDto>`
- **Data Structure:**
  ```typescript
  {
    orderId: string (GUID),
    eventType: string
  }
  ```

### 3. Ship Order
- **Endpoint:** `POST /api/orders/{orderId}/ship`
- **Success Response:** `ApiResponse<OrderOperationResponseDto>`
- **Data Structure:** Same as Verify Payment

### 4. Deliver Order
- **Endpoint:** `POST /api/orders/{orderId}/deliver`
- **Success Response:** `ApiResponse<OrderOperationResponseDto>`
- **Data Structure:** Same as Verify Payment

### 5. Cancel Order
- **Endpoint:** `DELETE /api/orders/{orderId}`
- **Success Response:** `ApiResponse<OrderOperationResponseDto>`
- **Data Structure:** Same as Verify Payment

## Client-Side Usage

### JavaScript/TypeScript Example

```typescript
interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
  timestamp: string;
}

interface CreateOrderResponseDto {
  order: OrderResponseDto;
}

interface OrderResponseDto {
  id: string;
  customerName: string;
  productName: string;
  quantity: number;
  totalAmount: number;
  status: string;
  createdAt: string;
}

// Example: Creating an order
async function createOrder(orderData: any): Promise<OrderResponseDto | null> {
  try {
    const response = await fetch('/api/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(orderData)
    });

    const apiResponse: ApiResponse<CreateOrderResponseDto> = await response.json();

    if (apiResponse.success) {
      console.log('Success:', apiResponse.message);
      return apiResponse.data!.order;
    } else {
      console.error('Failed:', apiResponse.message);
      console.error('Errors:', apiResponse.errors);
      return null;
    }
  } catch (error) {
    console.error('Request failed:', error);
    return null;
  }
}
```

### C# Example

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }
}

// Example: Consuming the API
public async Task<OrderResponseDto?> CreateOrderAsync(CreateOrderRequestDto request)
{
    var response = await _httpClient.PostAsJsonAsync("/api/orders", request);
    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponseDto>>();

    if (apiResponse?.Success == true)
    {
        Console.WriteLine($"Success: {apiResponse.Message}");
        return apiResponse.Data?.Order;
    }
    else
    {
        Console.WriteLine($"Failed: {apiResponse?.Message}");
        if (apiResponse?.Errors != null)
        {
            foreach (var error in apiResponse.Errors)
            {
                Console.WriteLine($"Error: {error}");
            }
        }
        return null;
    }
}
```

## Benefits of This Pattern

? **Consistency**: All endpoints return the same structure  
? **Predictability**: Clients know exactly what to expect  
? **Error Handling**: Standardized error reporting with detailed messages  
? **Type Safety**: Generic type parameter ensures compile-time checking  
? **Debugging**: Timestamp helps with troubleshooting  
? **Extensibility**: Easy to add metadata (correlation IDs, trace IDs, etc.)  
? **API Documentation**: Better Swagger/OpenAPI documentation generation  

## Adding New Endpoints

When adding new endpoints, follow this pattern:

```csharp
[HttpPost("example")]
[ProducesResponseType(typeof(ApiResponse<YourDataDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<YourDataDto>), 500)]
public async Task<ActionResult<ApiResponse<YourDataDto>>> ExampleEndpoint([FromBody] YourRequestDto request)
{
    try
    {
        // Your business logic here
        var data = new YourDataDto { /* ... */ };
        
        var response = ApiResponse<YourDataDto>.SuccessResponse(
            data, 
            "Operation completed successfully");
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in example endpoint");
        
        var errorResponse = ApiResponse<YourDataDto>.FailureResponse(
            "An error occurred",
            ex.Message);
        
        return StatusCode(500, errorResponse);
    }
}
```

## HTTP Status Codes

| Status Code | When Used | Response Structure |
|-------------|-----------|-------------------|
| 200 OK | Successful operation | `ApiResponse<T>` with `success: true` |
| 400 Bad Request | Validation errors | `ApiResponse<T>` with `success: false` |
| 404 Not Found | Resource not found | `ApiResponse<T>` with `success: false` |
| 500 Internal Server Error | Server errors | `ApiResponse<T>` with `success: false` |

## Version History

- **v1.0** - Initial implementation with generic ApiResponse pattern
- All endpoints migrated to use consistent response structure
- Added ProducesResponseType attributes for better Swagger documentation
