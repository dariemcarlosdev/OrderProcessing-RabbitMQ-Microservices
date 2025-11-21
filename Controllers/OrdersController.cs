using Microsoft.AspNetCore.Mvc;
using OrderFlow.Core.Contracts.Requests;
using OrderFlow.Core.Contracts.Responses;
using OrderFlow.Core.Extensions;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Controllers;

/// <summary>
/// REST API controller for managing order operations and publishing order events.
/// </summary>
/// <remarks>
/// This controller provides HTTP endpoints for the complete order lifecycle management:
/// - Creating new orders
/// - Verifying payments
/// - Shipping orders
/// - Delivering orders
/// - Cancelling orders
/// All operations publish events to RabbitMQ, which are then processed asynchronously by
/// various subscribers. This demonstrates an event-driven architecture where the API acts
/// as a command interface, and business logic is handled by background services.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IMessagePublisher messagePublisher,
        ILogger<OrdersController> logger)
    {
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order and publishes an event to RabbitMQ.
    /// </summary>
    /// <param name="request">The request model containing order details.</param>
    /// <returns>A standardized API response containing the created order details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OrderResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.CustomerName,
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                TotalAmount = request.TotalAmount,
                Status = OrderStatus.Created,
                CreatedAt = DateTime.UtcNow
            };

            var orderEvent = new OrderEvent
            {
                OrderId = order.Id,
                EventType = OrderEventTypes.OrderCreated,
                OrderData = order,
                Timestamp = DateTime.UtcNow,
                Message = $"Order created for customer {order.CustomerName}"
            };

            // Convention-based routing key: use event type as routing key
            var routingKey = orderEvent.EventType.ToLower(); // e.g., "order.created"
            await _messagePublisher.PublishAsync(orderEvent, routingKey);

            _logger.LogInformation($"Order created successfully: {order.Id}");

            // Simplified: Map domain model directly to response DTO
            var responseDto = order.MapTo<Order, OrderResponseDto>( order => new()
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt
            });


            var response = ApiResponse<OrderResponseDto>.SuccessResponse(
                responseDto, 
                "Order created and published successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            
            var errorResponse = ApiResponse<OrderResponseDto>.FailureResponse(
                "An error occurred while creating the order",
                ex.Message);

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Publishes a payment verification event for the specified order.
    /// </summary>
    /// <param name="orderId">The ID of the order for which the payment is verified.</param>
    /// <returns>A standardized API response containing the operation result.</returns>
    [HttpPost("{orderId}/payment")]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<OrderOperationResponseDto>>> VerifyPayment(Guid orderId)
    {
        try
        {
            var orderEvent = new OrderEvent
            {
                OrderId = orderId,
                EventType = OrderEventTypes.PaymentVerified,
                Timestamp = DateTime.UtcNow,
                Message = $"Payment verified for order {orderId}"
            };
            var routingKey = orderEvent.EventType.ToLower();
            await _messagePublisher.PublishAsync(orderEvent, routingKey);

            _logger.LogInformation("Payment verified for order: {OrderId}", orderId);

            var responseData = new OrderOperationResponseDto
            {
                OrderId = orderId,
                EventType = OrderEventTypes.PaymentVerified
            };

            var response = ApiResponse<OrderOperationResponseDto>.SuccessResponse(
                responseData,
                "Payment verification event published successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            
            var errorResponse = ApiResponse<OrderOperationResponseDto>.FailureResponse(
                "An error occurred while verifying payment",
                ex.Message);

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Publishes a shipping event for the specified order.
    /// </summary>
    /// <param name="orderId">The ID of the order to be shipped.</param>
    /// <returns>A standardized API response containing the operation result.</returns>
    [HttpPost("{orderId}/ship")]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<OrderOperationResponseDto>>> ShipOrder(Guid orderId)
    {
        try
        {
            var orderEvent = new OrderEvent
            {
                OrderId = orderId,
                EventType = OrderEventTypes.OrderShipped,
                Timestamp = DateTime.UtcNow,
                Message = $"Order {orderId} has been shipped"
            };
            var routingKey = orderEvent.EventType.ToLower();
            await _messagePublisher.PublishAsync(orderEvent, routingKey);

            _logger.LogInformation("Shipping event published for order: {OrderId}", orderId);

            var responseData = new OrderOperationResponseDto
            {
                OrderId = orderId,
                EventType = OrderEventTypes.OrderShipped
            };

            var response = ApiResponse<OrderOperationResponseDto>.SuccessResponse(
                responseData,
                "Shipping event published successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order");
            
            var errorResponse = ApiResponse<OrderOperationResponseDto>.FailureResponse(
                "An error occurred while shipping the order",
                ex.Message);

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Publishes a delivery event for the specified order.
    /// </summary>
    /// <param name="orderId">The ID of the order to be delivered.</param>
    /// <returns>A standardized API response containing the operation result.</returns>
    [HttpPost("{orderId}/deliver")]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<OrderOperationResponseDto>>> DeliverOrder(Guid orderId)
    {
        try
        {
            var orderEvent = new OrderEvent
            {
                OrderId = orderId,
                EventType = OrderEventTypes.OrderDelivered,
                Timestamp = DateTime.UtcNow,
                Message = $"Order {orderId} has been delivered"
            };
            var routingKey = orderEvent.EventType.ToLower();
            await _messagePublisher.PublishAsync(orderEvent, routingKey);

            _logger.LogInformation("Delivery event published for order: {OrderId}", orderId);

            var responseData = new OrderOperationResponseDto
            {
                OrderId = orderId,
                EventType = OrderEventTypes.OrderDelivered
            };

            var response = ApiResponse<OrderOperationResponseDto>.SuccessResponse(
                responseData,
                "Delivery event published successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order");
            
            var errorResponse = ApiResponse<OrderOperationResponseDto>.FailureResponse(
                "An error occurred while delivering the order",
                ex.Message);

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Publishes a cancellation event for the specified order.
    /// </summary>
    /// <param name="orderId">The ID of the order to be cancelled.</param>
    /// <returns>A standardized API response containing the operation result.</returns>
    [HttpDelete("{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OrderOperationResponseDto>), 500)]
    public async Task<ActionResult<ApiResponse<OrderOperationResponseDto>>> CancelOrder(Guid orderId)
    {
        try
        {
            var orderEvent = new OrderEvent
            {
                OrderId = orderId,
                EventType = OrderEventTypes.OrderCancelled,
                Timestamp = DateTime.UtcNow,
                Message = $"Order {orderId} has been cancelled"
            };
            var routingKey = orderEvent.EventType.ToLower();
            await _messagePublisher.PublishAsync(orderEvent, routingKey);

            _logger.LogInformation("Cancellation event published for order: {OrderId}", orderId);

            var responseData = new OrderOperationResponseDto
            {
                OrderId = orderId,
                EventType = OrderEventTypes.OrderCancelled
            };

            var response = ApiResponse<OrderOperationResponseDto>.SuccessResponse(
                responseData,
                "Order cancellation event published successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order");
            
            var errorResponse = ApiResponse<OrderOperationResponseDto>.FailureResponse(
                "An error occurred while cancelling the order",
                ex.Message);

            return StatusCode(500, errorResponse);
        }
    }

    // Additional endpoints for order management can be added here as needed.
    // For example, endpoints for retrieving order status, updating orders, etc.
}
