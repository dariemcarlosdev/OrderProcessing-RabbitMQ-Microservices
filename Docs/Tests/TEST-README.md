# Testing the OrderFlow.Core API - Pub/Sub Pattern

This guide provides step-by-step instructions for testing the RabbitMQ Publish/Subscribe pattern implementation in OrderFlow.Core.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Test Scenarios](#test-scenarios)
- [Monitoring & Verification](#monitoring--verification)
- [Advanced Testing](#advanced-testing)
- [Troubleshooting](#troubleshooting)

## Prerequisites

Before testing, ensure:
- ✅ Docker Desktop is running
- ✅ Services are started: `docker-compose up -d`
- ✅ All containers are healthy: `docker-compose ps`

### Verify Services are Running

```bash
# Check service status
docker-compose ps

# Expected output:
# NAME                 IMAGE                      STATUS
# orderflow-rabbitmq   rabbitmq:3-management      Up (healthy)
# orderflow-core       orderflow-core             Up
```

### Access Testing Interfaces

- **Swagger UI**: http://localhost:8080/swagger
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)
- **Health Check**: http://localhost:8080/health

## Quick Start

### Method 1: Using Swagger UI (Recommended for Beginners)

1. Open http://localhost:8080/swagger
2. Expand any endpoint (e.g., `POST /api/orders`)
3. Click **"Try it out"**
4. Enter test data
5. Click **"Execute"**
6. View the response

### Method 2: Using HTTP Files

Use the `http OrderFlow.Core.http` file in Visual Studio or VS Code with REST Client extension.

### Method 3: Using cURL

See examples in each test scenario below.

## Test Scenarios

### Scenario 1: Create Order Event (`order.created`)

**Purpose**: Test basic Pub/Sub flow with multiple subscribers

#### Using cURL

```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Alice Smith",
    "productName": "Wireless Mouse",
    "quantity": 2,
    "totalAmount": 49.98
  }'
```

#### Using PowerShell

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders" `
  -ContentType "application/json" `
  -Body (@{
    customerName = "Alice Smith"
    productName = "Wireless Mouse"
    quantity = 2
    totalAmount = 49.98
  } | ConvertTo-Json)
```

#### Expected Response (200 OK)

```json
{
  "success": true,
  "message": "Order created and published successfully",
  "data": {
    "order": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "customerName": "Alice Smith",
      "productName": "Wireless Mouse",
      "quantity": 2,
      "totalAmount": 49.98,
      "status": "Created",
      "createdAt": "2024-01-20T10:30:00Z"
    }
  },
  "errors": null,
  "timestamp": "2024-01-20T10:30:00Z"
}
```

#### Expected Subscribers

- ✅ **OrderProcessingSubscriber** (routing key: `order.created`)
- ✅ **NotificationSubscriber** (routing key: `order.*`)

#### Verify in Logs

```bash
docker-compose logs orderflow-core --tail 50 | grep "order.created"
```

#### Expected Log Output

```
[OrdersController] Published message with routing key order.created. OrderId: <guid>
[OrderProcessingSubscriber] Received message from queue order_processing_queue
[OrderProcessingSubscriber] Processing order for Alice Smith - Wireless Mouse
[NotificationSubscriber] Received message from queue notification_queue
[NotificationSubscriber] Sending Order Confirmation notification for order <guid>
```

---

### Scenario 2: Payment Verification Event (`payment.verified`)

**Purpose**: Test wildcard routing pattern (`payment.*`)

#### Step 1: Create an Order First

```bash
# Linux/Mac
ORDER_ID=$(curl -s -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Bob Johnson",
    "productName": "Keyboard",
    "quantity": 1,
    "totalAmount": 79.99
  }' | jq -r '.data.order.id')

echo "Order ID: $ORDER_ID"
```

```powershell
# PowerShell
$response = Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders" `
  -ContentType "application/json" `
  -Body (@{
    customerName = "Bob Johnson"
    productName = "Keyboard"
    quantity = 1
    totalAmount = 79.99
  } | ConvertTo-Json)

$ORDER_ID = $response.data.order.id
Write-Host "Order ID: $ORDER_ID"
```

#### Step 2: Verify Payment

```bash
# Replace {orderId} with the GUID from step 1
curl -X POST http://localhost:8080/api/orders/{orderId}/payment
```

```powershell
# PowerShell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders/$ORDER_ID/payment"
```

#### Expected Response (200 OK)

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

#### Expected Subscribers

- ✅ **PaymentVerificationSubscriber** (routing key: `payment.*`)
- ❌ OrderProcessingSubscriber (no match)
- ❌ ShippingSubscriber (no match)
- ❌ NotificationSubscriber (no match - doesn't match `order.*`)

#### Verify in Logs

```bash
docker-compose logs orderflow-core --tail 50 | grep "payment"
```

#### Expected Log Output

```
[OrdersController] Published message with routing key payment.verified
[PaymentVerificationSubscriber] Received message from queue payment_verification_queue
[PaymentVerificationSubscriber] Verifying payment for order <guid>
[PaymentVerificationSubscriber] Payment verification completed successfully
```

---

### Scenario 3: Shipping Event (`order.shipped`)

**Purpose**: Test specific routing to multiple subscribers

#### Using cURL

```bash
# Replace {orderId} with a valid order GUID
curl -X POST http://localhost:8080/api/orders/{orderId}/ship
```

#### Using PowerShell

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders/$ORDER_ID/ship"
```

#### Expected Response (200 OK)

```json
{
  "success": true,
  "message": "Shipping event published successfully",
  "data": {
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventType": "order.shipped"
  },
  "errors": null,
  "timestamp": "2024-01-20T10:40:00Z"
}
```

#### Expected Subscribers

- ✅ **ShippingSubscriber** (routing key: `order.shipped`)
- ✅ **NotificationSubscriber** (routing key: `order.*`)

#### Verify in Logs

```bash
docker-compose logs orderflow-core --tail 50 | grep "shipped"
```

#### Expected Log Output

```
[OrdersController] Published message with routing key order.shipped
[ShippingSubscriber] Received message from queue shipping_queue
[ShippingSubscriber] Processing shipping for order <guid>
[NotificationSubscriber] Sending Order Shipped notification
```

---

### Scenario 4: Delivery Event (`order.delivered`)

**Purpose**: Test routing to single subscriber via wildcard

#### Using cURL

```bash
curl -X POST http://localhost:8080/api/orders/{orderId}/deliver
```

#### Using PowerShell

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders/$ORDER_ID/deliver"
```

#### Expected Response (200 OK)

```json
{
  "success": true,
  "message": "Delivery event published successfully",
  "data": {
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventType": "order.delivered"
  },
  "errors": null,
  "timestamp": "2024-01-20T10:45:00Z"
}
```

#### Expected Subscribers

- ✅ **NotificationSubscriber** (routing key: `order.*`)
- ❌ All other subscribers (no matching routing keys)

#### Verify in Logs

```bash
docker-compose logs orderflow-core --tail 50 | grep "delivered"
```

---

### Scenario 5: Cancel Order Event (`order.cancelled`)

**Purpose**: Test DELETE endpoint with event publishing

#### Using cURL

```bash
curl -X DELETE http://localhost:8080/api/orders/{orderId}
```

#### Using PowerShell

```powershell
Invoke-RestMethod -Method Delete -Uri "http://localhost:8080/api/orders/$ORDER_ID"
```

#### Expected Response (200 OK)

```json
{
  "success": true,
  "message": "Order cancellation event published successfully",
  "data": {
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "eventType": "order.cancelled"
  },
  "errors": null,
  "timestamp": "2024-01-20T10:50:00Z"
}
```

#### Expected Subscribers

- ✅ **NotificationSubscriber** (routing key: `order.*`)

#### Verify in Logs

```bash
docker-compose logs orderflow-core --tail 50 | grep "cancelled"
```

---

## Monitoring & Verification

### Using RabbitMQ Management UI

1. **Navigate to**: http://localhost:15672
2. **Login**: admin / admin123

#### Check Exchange

- Go to **"Exchanges"** tab
- Find `order_exchange` (type: topic)
- Click on it to see bindings

#### Check Queues

| Queue Name | Routing Key | Expected Messages |
|-----------|-------------|-------------------|
| `order_processing_queue` | `order.created` | New orders only |
| `payment_verification_queue` | `payment.*` | Payment events only |
| `shipping_queue` | `order.shipped` | Shipping events only |
| `notification_queue` | `order.*` | ALL order events |

#### Monitor Message Flow

1. Click on **"Queues"** tab
2. For each queue, observe:
   - **Ready**: Messages waiting to be consumed
   - **Unacked**: Messages being processed
   - **Total**: Total messages processed
   - **Message rates**: Messages/second

### Using Application Logs

**Real-time monitoring:**

```bash
# All logs
docker-compose logs -f orderflow-core

# Specific subscriber
docker-compose logs -f orderflow-core | grep "OrderProcessingSubscriber"

# Specific event type
docker-compose logs -f orderflow-core | grep "order.created"

# Publisher only
docker-compose logs -f orderflow-core | grep "Published message"
```

**Structured log analysis:**

```bash
# Count messages by event type
docker-compose logs orderflow-core | grep "Published message" | grep -o "routing key [a-z.]*" | sort | uniq -c

# Count messages by subscriber
docker-compose logs orderflow-core | grep "Received message from queue" | grep -o "queue [a-z_]*" | sort | uniq -c
```

### Using Health Endpoint

```bash
# Check application and RabbitMQ health
curl http://localhost:8080/health | jq
```

**Expected output:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0118102",
  "entries": {
    "rabbitmq": {
      "status": "Healthy",
      "duration": "00:00:00.0094042"
    }
  }
}
```

---

## Advanced Testing

### Test 1: Concurrent Message Publishing

Test the system's ability to handle multiple simultaneous requests.

#### Linux/Mac

```bash
for i in {1..10}; do
  curl -X POST http://localhost:8080/api/orders \
    -H "Content-Type: application/json" \
    -d "{
      \"customerName\": \"Customer $i\",
      \"productName\": \"Product $i\",
      \"quantity\": 1,
      \"totalAmount\": 99.99
    }" &
done
wait
```

#### PowerShell

```powershell
1..10 | ForEach-Object {
  Start-Job -ScriptBlock {
    param($num)
    Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders" `
      -ContentType "application/json" `
      -Body (@{
        customerName = "Customer $num"
        productName = "Product $num"
        quantity = 1
        totalAmount = 99.99
      } | ConvertTo-Json)
  } -ArgumentList $_
}
Get-Job | Wait-Job | Receive-Job
Get-Job | Remove-Job
```

**Verification:**
- Check RabbitMQ message rates in management UI
- Verify all messages were processed
- Check for errors in logs

### Test 2: Message Persistence After Restart

Test that messages survive application restarts.

```bash
# 1. Publish messages
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Test Persistence",
    "productName": "Test Product",
    "quantity": 1,
    "totalAmount": 100.00
  }'

# 2. Stop subscribers (restart app)
docker-compose restart orderflow-core

# 3. Wait for app to restart
Start-Sleep -Seconds 10

# 4. Verify messages were redelivered
docker-compose logs orderflow-core --tail 100 | grep "Test Persistence"
```

### Test 3: Complete Order Lifecycle

Test a complete order from creation to delivery.

```bash
# 1. Create Order
ORDER_ID=$(curl -s -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "productName": "Complete Lifecycle Test",
    "quantity": 1,
    "totalAmount": 199.99
  }' | jq -r '.data.order.id')

echo "Order Created: $ORDER_ID"
Start-Sleep -Seconds 2

# 2. Verify Payment
curl -s -X POST "http://localhost:8080/api/orders/$ORDER_ID/payment"
echo "Payment Verified"
Start-Sleep -Seconds 2

# 3. Ship Order
curl -s -X POST "http://localhost:8080/api/orders/$ORDER_ID/ship"
echo "Order Shipped"
Start-Sleep -Seconds 2

# 4. Deliver Order
curl -s -X POST "http://localhost:8080/api/orders/$ORDER_ID/deliver"
echo "Order Delivered"

# 5. Check logs for complete flow
docker-compose logs orderflow-core | grep "$ORDER_ID"
```

#### PowerShell Version

```powershell
# 1. Create Order
$response = Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders" `
  -ContentType "application/json" `
  -Body (@{
    customerName = "John Doe"
    productName = "Complete Lifecycle Test"
    quantity = 1
    totalAmount = 199.99
  } | ConvertTo-Json)

$ORDER_ID = $response.data.order.id
Write-Host "Order Created: $ORDER_ID"
Start-Sleep -Seconds 2

# 2. Verify Payment
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders/$ORDER_ID/payment"
Write-Host "Payment Verified"
Start-Sleep -Seconds 2

# 3. Ship Order
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders/$ORDER_ID/ship"
Write-Host "Order Shipped"
Start-Sleep -Seconds 2

# 4. Deliver Order
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/api/orders/$ORDER_ID/deliver"
Write-Host "Order Delivered"

# 5. Check logs
docker-compose logs orderflow-core | Select-String $ORDER_ID
```

### Test 4: Routing Pattern Verification

Verify all routing patterns work correctly.

```bash
#!/bin/bash

echo "=========================================="
echo "Routing Pattern Verification Test"
echo "=========================================="
echo ""

# Create order
ORDER_ID=$(curl -s -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Routing Test",
    "productName": "Test Product",
    "quantity": 1,
    "totalAmount": 10.00
  }' | jq -r '.data.order.id')

echo "Order ID: $ORDER_ID"
sleep 3

# Test 1: order.created -> 2 subscribers
echo ""
echo "Test 1: order.created routing"
echo "------------------------------"
PROCESSING_COUNT=$(docker-compose logs orderflow-core | grep -c "order_processing_queue.*$ORDER_ID")
NOTIFICATION_COUNT=$(docker-compose logs orderflow-core | grep -c "notification_queue.*$ORDER_ID")
echo "  OrderProcessingSubscriber: $PROCESSING_COUNT messages"
echo "  NotificationSubscriber: $NOTIFICATION_COUNT messages"

# Test 2: payment.verified -> 1 subscriber
curl -s -X POST "http://localhost:8080/api/orders/$ORDER_ID/payment" > /dev/null
sleep 3

echo ""
echo "Test 2: payment.verified routing"
echo "---------------------------------"
PAYMENT_COUNT=$(docker-compose logs orderflow-core | grep -c "payment_verification_queue.*$ORDER_ID")
echo "  PaymentVerificationSubscriber: $PAYMENT_COUNT messages"

# Test 3: order.shipped -> 2 subscribers
curl -s -X POST "http://localhost:8080/api/orders/$ORDER_ID/ship" > /dev/null
sleep 3

echo ""
echo "Test 3: order.shipped routing"
echo "------------------------------"
SHIPPING_COUNT=$(docker-compose logs orderflow-core | grep -c "shipping_queue.*$ORDER_ID")
SHIPPING_NOTIF_COUNT=$(docker-compose logs orderflow-core | grep -c "notification_queue.*shipped.*$ORDER_ID")
echo "  ShippingSubscriber: $SHIPPING_COUNT messages"
echo "  NotificationSubscriber: $SHIPPING_NOTIF_COUNT messages"

echo ""
echo "=========================================="
echo "Routing Verification Complete"
echo "=========================================="
```

---

## Troubleshooting

### Issue: Messages Not Being Received

**Diagnosis:**

```bash
# Check if exchange exists
docker exec orderflow-rabbitmq rabbitmqctl list_exchanges

# Check if queues exist and have bindings
docker exec orderflow-rabbitmq rabbitmqctl list_queues name messages_ready messages_unacknowledged

# Check bindings
docker exec orderflow-rabbitmq rabbitmqctl list_bindings
```

**Solutions:**
- Verify subscribers are running: Check logs for "Subscriber started" messages
- Check routing keys match exactly
- Verify exchange and queue declarations in code

### Issue: Messages Stuck in Queue

**Diagnosis:**

```bash
# Check queue details
docker exec orderflow-rabbitmq rabbitmqctl list_queues name messages consumers

# Check consumer connections
docker exec orderflow-rabbitmq rabbitmqctl list_consumers
```

**Solutions:**
- Check if subscribers are connected
- Look for processing errors in logs
- Verify message format is correct (JSON deserialization)

### Issue: Connection Errors

**Symptoms:**
- HTTP 500 errors
- "Unable to connect to RabbitMQ" in logs

**Diagnosis:**

```bash
# Check RabbitMQ health
docker-compose ps rabbitmq

# Check RabbitMQ logs
docker-compose logs rabbitmq --tail 50

# Test connection from app container
docker exec orderflow-core ping rabbitmq
```

**Solutions:**
- Ensure RabbitMQ is healthy: `docker-compose ps`
- Verify network connectivity between containers
- Check credentials in docker-compose.yml match RabbitMQ settings

### Issue: Duplicate Messages

**Diagnosis:**
- Check if multiple instances of the same subscriber are running
- Verify consumer tags are unique

**Solutions:**
- Ensure only one instance of each subscriber service is running
- Check for restart loops in logs: `docker-compose logs orderflow-core | grep "Subscriber started"`

---

## Testing Checklist

Use this checklist to ensure comprehensive testing:

### Basic Functionality
- [ ] Create order event published and consumed
- [ ] Payment event routed to correct subscriber
- [ ] Shipping event received by multiple subscribers
- [ ] Delivery event processed
- [ ] Cancel event processed

### Routing Verification
- [ ] `order.created` reaches 2 subscribers
- [ ] `payment.*` wildcard works
- [ ] `order.*` wildcard catches all order events
- [ ] Non-matching routing keys are filtered correctly

### Reliability
- [ ] Messages persist through app restart
- [ ] Failed messages are requeued
- [ ] Acknowledgments work correctly
- [ ] Connection recovery works

### Performance
- [ ] Concurrent message publishing works
- [ ] No message loss under load
- [ ] Subscribers process messages efficiently
- [ ] RabbitMQ management UI shows healthy metrics

### Monitoring
- [ ] Application logs show publish/consume events
- [ ] RabbitMQ UI shows correct exchange/queue setup
- [ ] Health endpoint reports healthy status
- [ ] No error messages in logs

---

## Test Data Templates

### Order Creation

```json
{
  "customerName": "Jane Doe",
  "productName": "Ergonomic Chair",
  "quantity": 1,
  "totalAmount": 299.99
}
```

### Bulk Test Orders

```json
[
  {
    "customerName": "Customer 1",
    "productName": "Product A",
    "quantity": 2,
    "totalAmount": 199.98
  },
  {
    "customerName": "Customer 2",
    "productName": "Product B",
    "quantity": 1,
    "totalAmount": 49.99
  },
  {
    "customerName": "Customer 3",
    "productName": "Product C",
    "quantity": 5,
    "totalAmount": 499.95
  }
]
```

---

## Expected Subscriber Behavior Summary

| Event Type | Routing Key | Subscribers That Process It |
|-----------|-------------|----------------------------|
| Create Order | `order.created` | OrderProcessingSubscriber, NotificationSubscriber |
| Verify Payment | `payment.verified` | PaymentVerificationSubscriber |
| Ship Order | `order.shipped` | ShippingSubscriber, NotificationSubscriber |
| Deliver Order | `order.delivered` | NotificationSubscriber |
| Cancel Order | `order.cancelled` | NotificationSubscriber |

---

## References

- [Main README](README.md) - Application overview and setup
- [API Response Pattern](API-RESPONSE-PATTERN.md) - API contract documentation
- [Docker Deployment Guide](DOCKER-DEPLOYMENT.md) - Docker-specific instructions
- [RabbitMQ Topic Exchange Tutorial](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html)

---

## Support

If tests fail or you encounter issues:
1. Check application logs: `docker-compose logs orderflow-core`
2. Check RabbitMQ logs: `docker-compose logs rabbitmq`
3. Verify health endpoint: `curl http://localhost:8080/health`
4. Check RabbitMQ Management UI: http://localhost:15672
5. Refer to [DOCKER-DEPLOYMENT.md](DOCKER-DEPLOYMENT.md#troubleshooting) for troubleshooting

---

**Happy Testing! 🚀**
