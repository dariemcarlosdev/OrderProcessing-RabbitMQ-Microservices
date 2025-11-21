# OrderFlow.Core - Order Processing with RabbitMQ Pub/Sub

**Author:** Dariem Carlos Macias

> *"The art of programming is the art of organizing complexity."* — **Edsger W. Dijkstra**

## 🎯 Project Vision

**OrderFlow.Core** is a comprehensive demonstration of **event-driven architecture** and **microservices patterns** using .NET 8 and RabbitMQ. This project serves as both a learning resource and a production-ready reference implementation for building scalable, resilient, and maintainable distributed systems.

### Why This Project Exists

In modern software architecture, the ability to design systems that are:
- ✅ **Loosely coupled** — components operate independently
- ✅ **Highly scalable** — horizontal scaling without code changes
- ✅ **Resilient** — graceful failure handling and automatic recovery
- ✅ **Maintainable** — clean separation of concerns

...is not just a luxury—it's a necessity. This project demonstrates these principles through a real-world order processing system using the **Publish/Subscribe pattern** with RabbitMQ as the message broker.

### What I Learned

Building this project from the ground up was an incredible journey into the world of **distributed systems** and **event-driven architecture**. Here's what I discovered through hands-on implementation:

🔹 **Event-Driven Architecture** — I learned that decoupling components through message brokers isn't just theory—it's a powerful way to build systems that truly scale. Watching orders flow through RabbitMQ and seeing multiple subscribers process them independently was a lightbulb moment. The beauty lies in how publishers don't need to know about subscribers; they just fire events and move on.

🔹 **RabbitMQ Topic Exchange** — Initially, routing keys seemed like simple strings, but mastering wildcard patterns (`order.*`, `payment.*`) revealed their true power. I discovered how to route one event to multiple queues based on patterns, enabling sophisticated message routing without complex conditional logic. The "aha!" moment came when I realized `NotificationSubscriber` could listen to *all* order events with a single `order.*` pattern.

🔹 **Clean Architecture** — Separating concerns into distinct layers (Controllers, Infrastructure, Domain Models, Contracts) felt tedious at first, but it paid off massively during refactoring. When I needed to change the response format, I only touched DTOs—not a single domain model or subscriber. The **Single Responsibility Principle** stopped being a buzzword and became my best friend.

🔹 **Docker Orchestration** — Writing a multi-container `docker-compose.yml` taught me that deployment isn't just about running code—it's about orchestrating ecosystems. Managing service dependencies, health checks, and network isolation showed me why Docker is indispensable for modern development. Watching RabbitMQ wait for health checks before my app started was satisfying in ways I didn't expect.

🔹 **Background Services** — Implementing `BackgroundService` in .NET 8 revealed the elegance of long-running processes. My subscribers run continuously in the background, consuming messages without blocking HTTP requests. I learned about graceful shutdown, exception handling in loops, and how to prevent memory leaks in infinite loops.

🔹 **API Design Patterns** — Creating the generic `ApiResponse<T>` wrapper taught me that consistency is king. Instead of scattered response formats, every endpoint now returns a predictable structure with `success`, `message`, `data`, and `errors`. This pattern eliminated so much client-side guesswork and made API consumption a joy.

🔹 **Connection Resilience** — I learned the hard way that connections fail—RabbitMQ restarts, networks hiccup, containers restart. Implementing **exponential backoff** with retry logic (1s, 2s, 4s, 8s, 16s) transformed my brittle system into a resilient one. Automatic recovery and connection pooling became non-negotiables, not nice-to-haves.

🔹 **Health Monitoring** — Adding health checks wasn't just about green checkmarks—it was about observability. When I integrated ASP.NET Core health checks for RabbitMQ, I could finally *see* when things went wrong before users did. The `/health` endpoint became my first debugging tool, not my last resort.

---

**The Biggest Lesson?** Building distributed systems is about **embracing failure** and **designing for resilience**. Every component can fail, every network can drop, every message can be duplicated. The systems that survive aren't the ones that never fail—they're the ones that fail gracefully and recover automatically.

This project transformed my understanding from "I know the theory" to "I've lived the battle scars." Every bug taught me something new, every refactor made me appreciate clean architecture, and every successful deployment reinforced that **good software isn't written—it's rewritten**.

---

This project is designed for **software engineers, architects, and students** who want to go beyond tutorials and build something real. If you're ready to learn by doing, dive in—the journey is worth it.

---

## 📚 Table of Contents

1. [Architecture](#architecture)
   - [Publisher](#publisher)
   - [Subscribers](#subscribers-background-services)
2. [Prerequisites](#prerequisites)
3. [Quick Start with Docker Compose](#quick-start-with-docker-compose-recommended)
4. [Alternative: Local Development Setup](#alternative-local-development-setup)
5. [How the Pub/Sub Pattern Works](#how-the-pubsub-pattern-works-in-this-project)
   - [Overview](#overview)
   - [Key Components](#key-components)
   - [Message Flow Example](#message-flow-example)
   - [Topic Exchange Routing](#topic-exchange-routing)
   - [Dependency Injection Setup](#dependency-injection-setup)
   - [Key Advantages](#key-advantages-of-this-pattern)
   - [Error Handling](#error-handling)
6. [API Endpoints](#api-endpoints)
7. [Testing the Pub/Sub Pattern](#testing-the-pubsub-pattern)
8. [Routing Keys](#routing-keys)
9. [Project Structure](#project-structure)
10. [Key Features](#key-features)
11. [Monitoring](#monitoring)
12. [Troubleshooting](#troubleshooting)
13. [Configuration](#configuration)
14. [Production Deployment](#production-deployment)
15. [📖 Documentation](#-documentation)
16. [Next Steps](#next-steps)
17. [Resources](#resources)
18. [License](#license)

---

# OrderFlow.Core - Order Processing with RabbitMQ Pub/Sub

A .NET 8 Web API project demonstrating order processing using the Publish/Subscribe pattern with RabbitMQ as the message broker.

## Architecture

This project implements a complete order processing system using the Publish/Subscribe pattern with the following components:

### Publisher
- **OrdersController**: REST API endpoints that publish order events to RabbitMQ

### Subscribers (Background Services)
1. **OrderProcessingSubscriber**: Processes new orders (routing key: `order.created`)
2. **PaymentVerificationSubscriber**: Verifies payments (routing key: `payment.*`)
3. **ShippingSubscriber**: Handles shipping (routing key: `order.shipped`)
4. **NotificationSubscriber**: Sends notifications for all order events (routing key: `order.*`)

## Prerequisites

- .NET 8 SDK
- Docker Desktop (for containerized deployment)

## Quick Start with Docker Compose (Recommended)

The easiest way to run the entire application stack:

### 1. Build the Application
```bash
dotnet publish -c Release -o ./publish
```

### 2. Start All Services
```bash
docker-compose up -d
```

This will:
- Start RabbitMQ with management UI
- Wait for RabbitMQ to be healthy
- Start the OrderFlow.Core application
- Create a bridge network for service communication

### 3. Access the Application

- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health
- **RabbitMQ Management UI**: http://localhost:15672
  - Username: `admin`
  - Password: `admin123`

### 4. View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f orderflow-core
```

### 5. Stop Services
```bash
docker-compose down
```

📖 **For detailed Docker deployment instructions, troubleshooting, and production considerations:**
- [Docker Deployment Guide](Docs/Containerization/DOCKER-DEPLOYMENT.md)
- [Docker Containerization Deep Dive](Docs/Containerization/DOCKER-CONTAINERIZE-README.md)

## Alternative: Local Development Setup

If you prefer to run the application locally without Docker:

### 1. Start RabbitMQ Only with Docker
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=admin123 \
  rabbitmq:3-management
```

### 2. Wait for RabbitMQ to be Ready
```bash
# Check if RabbitMQ is healthy (wait until "Ping succeeded")
docker exec rabbitmq rabbitmq-diagnostics ping
```

### 3. Update Configuration
Ensure `appsettings.json` matches your RabbitMQ settings:

```json
"RabbitMq": {
  "HostName": "localhost",
  "Port": 5672,
  "UserName": "admin",
  "Password": "admin123",
  "ExchangeName": "order_exchange",
  "ExchangeType": "topic"
}
```

### 4. Run the Application
```bash
dotnet build
dotnet run
```

The application will be available at:
- **Swagger UI**: https://localhost:7279/swagger (or http://localhost:5246/swagger)
- **Health Check**: https://localhost:7279/health

## How the Pub/Sub Pattern Works in This Project

This section explains the complete message flow and the role of each component in the publish/subscribe architecture.

### Overview

The project implements an **event-driven architecture** using RabbitMQ's **Topic Exchange** pattern. When an order-related action occurs (create, payment, shipping, etc.), an event is published to RabbitMQ.
Multiple subscribers listen for specific event types and process them independently and asynchronously.

### Key Components

#### 1. Configuration Layer

**`RabbitMqSettings`** (`Configuration/RabbitMqSettings.cs`)
- Contains all RabbitMQ connection parameters (hostname, port, credentials)
- Defines exchange name (`order_exchange`) and type (`topic`)
- Loaded from `appsettings.json` during application startup
- Injected into all RabbitMQ-related services via dependency injection

#### 2. Domain Models

**`Order`** (`Models/Order.cs`)
- Core domain entity representing an order
- Contains customer info, product details, quantity, amount, status, and timestamps
- Status transitions through the `OrderStatus` enum: Created → Processing → PaymentVerified → Shipped → Delivered (or Cancelled)

**`OrderEvent`** (`Models/OrderEvent.cs`)
- Wrapper class for all order-related events
- Contains OrderId, EventType, OrderData (the full Order object), Timestamp, and Message
- Serialized to JSON for transmission through RabbitMQ

**`OrderEventTypes`** (`Models/OrderEventTypes.cs`)
- Static class defining all event type constants
- Ensures consistency across publishers and subscribers
- Examples: `order.created`, `payment.verified`, `order.shipped`, etc.

#### 3. Infrastructure Layer - Connection Management

**`IRabbitMqConnectionFactory`** (`Infrastructure/RabbitMQ/IRabbitMqConnectionFactory.cs`)
- Interface for abstracting RabbitMQ connection creation
- Enables testability and loose coupling

**`RabbitMqConnectionFactory`** (`Infrastructure/RabbitMQ/RabbitMqConnectionFactory.cs`)
- Concrete implementation that creates RabbitMQ connections
- Implements retry logic with exponential backoff (5 attempts)
- Configures automatic recovery and network recovery intervals
- Uses `RabbitMqSettings` to establish connections
- Provides comprehensive logging for connection events and errors

#### 4. Publishing Layer

**`IMessagePublisher`** (`Infrastructure/RabbitMQ/IMessagePublisher.cs`)
- Interface defining the contract for message publishing
- Single method: `PublishAsync(OrderEvent, routingKey)`

**`RabbitMqPublisher`** (`Infrastructure/RabbitMQ/RabbitMqPublisher.cs`)
- Implements `IMessagePublisher` and handles actual message publishing
- **Initialization**: Creates connection, channel, and declares the exchange
- **Publishing Process**:
  1. Serializes `OrderEvent` to JSON
  2. Creates message properties (persistent, content-type, timestamp)
  3. Publishes to `order_exchange` with specified routing key
  4. Logs all operations for monitoring
- **Features**: Auto-recovery on channel closure, persistent message delivery, proper resource cleanup

**`OrdersController`** (`Controllers/OrdersController.cs`)
- REST API controller that acts as the **Event Publisher**
- Receives HTTP requests and publishes corresponding events
- **Endpoints**:
  - `POST /api/orders` → Publishes `order.created` event
  - `POST /api/orders/{id}/payment` → Publishes `payment.verified` event
  - `POST /api/orders/{id}/ship` → Publishes `order.shipped` event
  - `POST /api/orders/{id}/deliver` → Publishes `order.delivered` event
  - `DELETE /api/orders/{id}` → Publishes `order.cancelled` event
- Creates `Order` and `OrderEvent` objects from requests
- Uses `IMessagePublisher` to publish events to RabbitMQ
- Returns consistent `ApiResponse<T>` wrapper for all endpoints

> 📖 **Learn more about the API Response Pattern:** [API Response Pattern Guide](Docs/Patterns/API-RESPONSE-PATTERN.md)

#### 5. Subscription Layer

**`RabbitMqSubscriberBase`** (`Infrastructure/RabbitMQ/RabbitMqSubscriberBase.cs`)
- Abstract base class for all subscribers
- Extends `BackgroundService` to run continuously in the background
- **Initialization**:
  1. Creates connection and channel via `IRabbitMqConnectionFactory`
  2. Declares the exchange (`order_exchange`)
  3. Declares queue with durable settings
  4. Binds queue to exchange using routing key pattern
  5. Sets QoS to process one message at a time
- **Message Processing**:
  1. Receives messages via `EventingBasicConsumer`
  2. Deserializes JSON to `OrderEvent` object
  3. Calls abstract `ProcessMessageAsync` method (implemented by derived classes)
  4. Acknowledges message on success (`BasicAck`)
  5. Requeues message on error (`BasicNack` with requeue=true)
- **Derived classes must define**: `QueueName`, `RoutingKey`, and `ProcessMessageAsync`

#### 6. Concrete Subscribers (Background Services)

**`OrderProcessingSubscriber`** (`Services/Subscribers/OrderProcessingSubscriber.cs`)
- **Queue**: `order_processing_queue`
- **Routing Key**: `order.created`
- **Purpose**: Processes newly created orders
- **Actions**: Validates order details, checks inventory, calculates costs
- **Triggered by**: POST /api/orders

**`PaymentVerificationSubscriber`** (`Services/Subscribers/PaymentVerificationSubscriber.cs`)
- **Queue**: `payment_verification_queue`
- **Routing Key**: `payment.*` (wildcard - matches all payment events)
- **Purpose**: Verifies payment transactions
- **Actions**: Integrates with payment gateways, validates amounts, handles fraud detection
- **Triggered by**: POST /api/orders/{id}/payment

**`ShippingSubscriber`** (`Services/Subscribers/ShippingSubscriber.cs`)
- **Queue**: `shipping_queue`
- **Routing Key**: `order.shipped`
- **Purpose**: Handles shipping logistics
- **Actions**: Integrates with carriers, generates labels, calculates delivery times
- **Triggered by**: POST /api/orders/{id}/ship

**`NotificationSubscriber`** (`Services/Subscribers/NotificationSubscriber.cs`)
- **Queue**: `notification_queue`
- **Routing Key**: `order.*` (wildcard - matches ALL order events)
- **Purpose**: Sends customer notifications for any order event
- **Actions**: Sends emails, SMS, push notifications based on event type
- **Triggered by**: ALL order endpoints (creates notifications for every event)

### Message Flow Example

Let's trace what happens when you create a new order:

```
1. Client Request
   └─> POST /api/orders { "customerName": "John", "productName": "Laptop", ... }

2. OrdersController (Publisher)
   ├─> Creates Order object (ID, Status=Created, Timestamp)
   ├─> Creates OrderEvent object (OrderId, EventType="order.created", OrderData)
   └─> Calls RabbitMqPublisher.PublishAsync(orderEvent, "order.created")

3. RabbitMqPublisher
   ├─> Serializes OrderEvent to JSON
   ├─> Creates AMQP message with persistent properties
   └─> Publishes to "order_exchange" with routing key "order.created"

4. RabbitMQ Topic Exchange
   ├─> Receives message with routing key "order.created"
   ├─> Evaluates routing key against queue bindings:
   │   ├─> Matches "order.created" pattern → Routes to order_processing_queue
   │   └─> Matches "order.*" pattern → Routes to notification_queue
   └─> Delivers message to both queues (Pub/Sub pattern!)

5. Subscribers (Parallel Processing)
   ├─> OrderProcessingSubscriber
   │   ├─> Receives from order_processing_queue
   │   ├─> Deserializes JSON to OrderEvent
   │   ├─> Calls ProcessMessageAsync()
   │   ├─> Executes order processing logic
   │   └─> Acknowledges message (BasicAck)
   │
   └─> NotificationSubscriber
       ├─> Receives from notification_queue
       ├─> Deserializes JSON to OrderEvent
       ├─> Calls ProcessMessageAsync()
       ├─> Determines notification type: "Order Confirmation"
       ├─> Sends notification to customer
       └─> Acknowledges message (BasicAck)

6. Response
   └─> OrdersController returns HTTP 200 with Order details
```

### Topic Exchange Routing

The **Topic Exchange** uses pattern matching for routing:

| Event Published | Routing Key | Matched Subscribers |
|----------------|-------------|---------------------|
| Order Created | `order.created` | OrderProcessingSubscriber (`order.created`)<br>NotificationSubscriber (`order.*`) |
| Payment Verified | `payment.verified` | PaymentVerificationSubscriber (`payment.*`) |
| Order Shipped | `order.shipped` | ShippingSubscriber (`order.shipped`)<br>NotificationSubscriber (`order.*`) |
| Order Delivered | `order.delivered` | NotificationSubscriber (`order.*`) |
| Order Cancelled | `order.cancelled` | NotificationSubscriber (`order.*`) |

**Wildcard Patterns**:
- `*` (star) matches exactly one word (e.g., `order.*` matches `order.created`, `order.shipped`)
- `#` (hash) matches zero or more words (not used in this project, but available)

### Dependency Injection Setup

In `Program.cs`, all components are registered:

```csharp
// Configuration
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

// Infrastructure
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

// Subscribers (Background Services)
builder.Services.AddHostedService<OrderProcessingSubscriber>();
builder.Services.AddHostedService<PaymentVerificationSubscriber>();
builder.Services.AddHostedService<ShippingSubscriber>();
builder.Services.AddHostedService<NotificationSubscriber>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq");
```

### Key Advantages of This Pattern

1. **Loose Coupling**: Publishers don't know about subscribers, and vice versa
2. **Scalability**: Add new subscribers without modifying existing code
3. **Asynchronous Processing**: HTTP requests return immediately; heavy processing happens in background
4. **Multiple Consumers**: Same event can trigger multiple business processes
5. **Reliability**: Persistent messages and acknowledgments ensure no data loss
6. **Flexibility**: Topic routing allows sophisticated event filtering
7. **Resilience**: Automatic recovery handles connection failures gracefully with retry logic

### Error Handling

- **Publisher**: Logs errors and throws exceptions (returns 500 to client)
- **Subscribers**: Catch exceptions, log errors, and requeue messages for retry
- **Connection Failures**: Automatic recovery enabled with 10-second intervals
- **Connection Retry**: Exponential backoff with 5 retry attempts (1s, 2s, 4s, 8s, 16s)
- **Message Failures**: Messages stay in queue until successfully processed

## API Endpoints

All endpoints return a consistent `ApiResponse<T>` wrapper with the following structure:

```json
{
  "success": true,
  "message": "Order created successfully",
  "data": { ... },
  "errors": []
}
```

> 📖 **Learn more about the API Response Pattern:** [API Response Pattern Guide](Docs/Patterns/API-RESPONSE-PATTERN.md)

### Create Order
```http
POST /api/orders
Content-Type: application/json

{
  "customerName": "John Doe",
  "productName": "Laptop",
  "quantity": 1,
  "totalAmount": 1299.99
}
```

### Verify Payment
```http
POST /api/orders/{orderId}/payment
```

### Ship Order
```http
POST /api/orders/{orderId}/ship
```

### Deliver Order
```http
POST /api/orders/{orderId}/deliver
```

### Cancel Order
```http
DELETE /api/orders/{orderId}
```

### Health Check
```http
GET /health
```

Returns JSON with application health status and RabbitMQ connectivity.

## Testing the Pub/Sub Pattern

### Using Docker Compose (Recommended)

1. **Start all services:**
   ```bash
   docker-compose up -d
   ```

2. **Wait for services to be ready:**
   ```bash
   docker-compose ps
   ```

3. **Access Swagger UI:**
   - Navigate to http://localhost:8080/swagger
   - Use the interactive UI to test endpoints

4. **Create a new order:**
   - Execute POST `/api/orders` with sample data
   - Check the logs to see message flow:
   ```bash
   docker-compose logs -f orderflow-core
   ```

5. **Observe in the logs:**
   - **Publisher**: Message published to RabbitMQ
   - **OrderProcessingSubscriber**: Processing the order
   - **NotificationSubscriber**: Sending notification

6. **Monitor in RabbitMQ UI:**
   - Visit http://localhost:15672 (admin/admin123)
   - Check Exchanges → `order_exchange`
   - Check Queues → See all 4 queues and message flow

> 📖 **For comprehensive testing scenarios and automated test scripts:** [Testing Guide](Docs/Tests/TEST-README.md)

### Using Local Development

1. Start RabbitMQ (see [Alternative: Local Development Setup](#alternative-local-development-setup))
2. Start the application with `dotnet run`
3. Use Swagger UI at https://localhost:7279/swagger
4. Check console logs to see message flow

## Routing Keys

The project uses topic exchange with the following routing patterns:

- `order.created` → OrderProcessingSubscriber, NotificationSubscriber
- `payment.verified` → PaymentVerificationSubscriber
- `order.shipped` → ShippingSubscriber, NotificationSubscriber
- `order.delivered` → NotificationSubscriber
- `order.cancelled` → NotificationSubscriber

## Project Structure

```
OrderFlow.Core/
├── Configuration/
│   └── RabbitMqSettings.cs
├── Contracts/
│   ├── Requests/
│   │   └── CreateOrderRequestDto.cs
│   └── Responses/
│       ├── ApiResponse.cs
│       ├── CreateOrderResponseDto.cs
│       ├── OrderOperationResponseDto.cs
│       └── OrderResponseDto.cs
├── Controllers/
│   └── OrdersController.cs
├── Docs/
│   ├── Containerization/
│   │   ├── DOCKER-CONTAINERIZE-README.md
│   │   └── DOCKER-DEPLOYMENT.md
│   ├── Patterns/
│   │   └── API-RESPONSE-PATTERN.md
│   └── Tests/
│       └── TEST-README.md
├── Infrastructure/
│   └── RabbitMQ/
│       ├── IRabbitMqConnectionFactory.cs
│       ├── RabbitMqConnectionFactory.cs
│       ├── IMessagePublisher.cs
│       ├── RabbitMqPublisher.cs
│       └── RabbitMqSubscriberBase.cs
├── Models/
│   ├── Order.cs
│   ├── OrderEvent.cs
│   └── OrderEventTypes.cs
├── Services/
│   └── Subscribers/
│       ├── OrderProcessingSubscriber.cs
│       ├── PaymentVerificationSubscriber.cs
│       ├── ShippingSubscriber.cs
│       └── NotificationSubscriber.cs
├── docker-compose.yml
├── Dockerfile
├── Dockerfile.simple
├── .dockerignore
├── Program.cs
├── appsettings.json
└── README.md
```

## Key Features

- ✅ Topic-based routing with RabbitMQ
- ✅ Multiple subscribers for different order events
- ✅ Automatic message acknowledgment
- ✅ Persistent messages
- ✅ Automatic connection recovery with retry logic
- ✅ Structured logging
- ✅ Clean architecture with separation of concerns
- ✅ Background services for continuous message consumption
- ✅ Docker Compose support for easy deployment
- ✅ Health checks for monitoring
- ✅ Swagger UI for API documentation
- ✅ Consistent API response wrapper pattern
- ✅ Comprehensive documentation

## Monitoring

### Application Logs
```bash
# Docker Compose
docker-compose logs -f orderflow-core

# Local Development
Check console output
```

### RabbitMQ Management UI
Visit http://localhost:15672 to monitor:
- Exchange creation and bindings
- Queue status and message rates
- Consumer connections
- Message flow and routing

### Health Endpoint
Check http://localhost:8080/health for:
- Overall application health status
- RabbitMQ connection status
- Response time metrics

## Troubleshooting

### Common Issues

#### "Unable to connect to RabbitMQ"
- **Docker Compose**: Ensure RabbitMQ is healthy with `docker-compose ps`
- **Local**: Check RabbitMQ is running with `docker ps`
- Verify port 5672 is accessible
- Check credentials match in `appsettings.json` or docker-compose environment variables

#### "Swagger UI not loading"
- **Docker**: Access http://localhost:8080/swagger (not HTTPS)
- **Local**: Try both HTTP and HTTPS URLs
- Check application logs for startup errors

#### "Container won't start"
- Check logs: `docker-compose logs orderflow-core`
- Verify publish folder exists: `ls ./publish`
- Rebuild: `dotnet publish -c Release -o ./publish && docker-compose up -d --build`

> 📖 **For detailed troubleshooting and Docker-specific issues:** [Docker Deployment Guide](Docs/Containerization/DOCKER-DEPLOYMENT.md#troubleshooting)

## Configuration

### Docker Compose Environment
Configuration is managed via environment variables in `docker-compose.yml`:
- RabbitMQ hostname: `rabbitmq` (Docker service name)
- Credentials: admin/admin123

> 📖 **For in-depth docker-compose.yml explanation:** [Docker Containerization Guide](Docs/Containerization/DOCKER-CONTAINERIZE-README.md)

### Local Development Environment
Configuration is loaded from `appsettings.json`:
- RabbitMQ hostname: `localhost`
- Port: 5672
- Credentials: admin/admin123

## Production Deployment

For production deployment considerations, including:
- Secret management
- HTTPS configuration
- Resource limits
- Monitoring and logging
- Health checks
- Scaling strategies

> 📖 **See the Production Considerations section in:** [Docker Deployment Guide](Docs/Containerization/DOCKER-DEPLOYMENT.md#production-considerations)

---

## 📖 Documentation

This project includes comprehensive documentation covering all aspects of the system:

### 🐳 **Containerization & Deployment**
- **[Docker Deployment Guide](Docs/Containerization/DOCKER-DEPLOYMENT.md)**
  - Quick start with Docker Compose
  - Environment configuration
  - Port mappings and networking
  - Troubleshooting container issues
  - Production deployment strategies

- **[Docker Containerization Deep Dive](Docs/Containerization/DOCKER-CONTAINERIZE-README.md)**
  - Complete docker-compose.yml breakdown
  - Service orchestration explained
  - Networking architecture
  - Volume management and persistence
  - Health checks and dependencies
  - Container lifecycle management
  - Advanced scenarios and best practices

### 🎨 **Patterns & Architecture**
- **[API Response Pattern Guide](Docs/Patterns/API-RESPONSE-PATTERN.md)**
  - Consistent response wrapper structure
  - Generic `ApiResponse<T>` implementation
  - Error handling strategies
  - Success and failure response examples
  - Best practices for API design

### 🧪 **Testing**
- **[Comprehensive Testing Guide](Docs/Tests/TEST-README.md)**
  - Quick start testing with Swagger UI
  - Detailed test scenarios for each event type
  - RabbitMQ Management UI monitoring
  - Automated test scripts
  - Troubleshooting test failures
  - Performance and load testing

### 📚 **Documentation Index**

| Topic | Document | Description |
|-------|----------|-------------|
| **Quick Start** | [Main README](README.md) | Getting started, architecture overview |
| **Docker Deployment** | [DOCKER-DEPLOYMENT.md](Docs/Containerization/DOCKER-DEPLOYMENT.md) | Step-by-step deployment guide |
| **Docker Architecture** | [DOCKER-CONTAINERIZE-README.md](Docs/Containerization/DOCKER-CONTAINERIZE-README.md) | Deep dive into docker-compose.yml |
| **API Patterns** | [API-RESPONSE-PATTERN.md](Docs/Patterns/API-RESPONSE-PATTERN.md) | API response design patterns |
| **Testing** | [TEST-README.md](Docs/Tests/TEST-README.md) | Comprehensive testing guide |

---

## Next Steps

Consider adding:
- ✅ **Connection resilience**: ✓ Implemented with retry logic and exponential backoff
- ✅ **Health checks**: ✓ Implemented with ASP.NET Core health checks
- ✅ **Docker support**: ✓ Full Docker Compose orchestration
- ✅ **API Response Pattern**: ✓ Consistent response wrapper implemented
- ✅ **Comprehensive Documentation**: ✓ Multiple guides covering all aspects
- **Database persistence**: Store orders in a database (SQL Server, PostgreSQL)
- **Dead letter queues**: Handle permanently failed messages
- **Message retry policies**: Enhanced backoff strategies with dead letter exchange
- **Circuit breaker pattern**: Prevent cascading failures (Polly library)
- **Unit and integration tests**: Test publishers and subscribers
- **Metrics and observability**: Prometheus, Grafana, or Application Insights
- **API authentication**: Add JWT or OAuth2 authentication
- **Rate limiting**: Protect API endpoints from abuse
- **Idempotency**: Handle duplicate message processing
- **Event sourcing**: Full event history and replay capability

## Resources

### Official Documentation
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [.NET RabbitMQ Client](https://www.rabbitmq.com/dotnet-api-guide.html)
- [ASP.NET Core Background Services](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Topic Exchange Tutorial](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html)

### Project Documentation
- [Docker Deployment Guide](Docs/Containerization/DOCKER-DEPLOYMENT.md)
- [Docker Containerization Deep Dive](Docs/Containerization/DOCKER-CONTAINERIZE-README.md)
- [API Response Pattern Guide](Docs/Patterns/API-RESPONSE-PATTERN.md)
- [Testing Guide](Docs/Tests/TEST-README.md)

---

## 🎓 Learning Path

If you're new to these concepts, here's a suggested learning path:

1. **Start Simple** — Run the project locally and test the `/api/orders` endpoint
2. **Observe the Flow** — Watch the logs and RabbitMQ Management UI
3. **Understand Routing** — Experiment with different routing keys
4. **Read the Docs** — Explore the [comprehensive documentation](Docs/) for deep dives
5. **Test Thoroughly** — Use the [Testing Guide](Docs/Tests/TEST-README.md) for scenarios
6. **Deploy** — Use Docker Compose to experience the full stack
7. **Containerize** — Learn from [Docker Containerization Guide](Docs/Containerization/DOCKER-CONTAINERIZE-README.md)
8. **Optimize** — Implement dead letter queues and retry policies

---

## 🤝 Contributing

This project is open for contributions! Whether you want to:
- 🐛 Fix bugs or improve documentation
- ✨ Add new features or patterns
- 📚 Share your learnings or use cases
- 💡 Suggest architectural improvements

Feel free to open an issue or submit a pull request. All contributions are welcome!

---

## 💬 Feedback & Support

Have questions or suggestions? Feel free to:
- 📧 Open an issue on GitHub
- 💭 Share your thoughts on implementation patterns
- 🌟 Star this repository if you find it helpful

---

## 🙏 Acknowledgments

This project stands on the shoulders of giants:
- The **RabbitMQ** team for building an incredible message broker
- The **.NET team** for the amazing ASP.NET Core framework
- The **Docker** community for simplifying deployment
- **Edsger W. Dijkstra** for teaching us to organize complexity
- Every developer who has shared knowledge through open source

---

## License

This is a demonstration project for learning purposes.

---

<div align="center">

### 💙 Built with Love & Passion for Clean Code

*"Code is like humor. When you have to explain it, it's bad."* — **Cory House**

This project was crafted with care to demonstrate **best practices**, **clean architecture**, and the **joy of building distributed systems**. Whether you're a student learning event-driven architecture, an engineer building production systems, or an architect evaluating patterns—I hope this project serves you well.

**May your messages always route correctly, your queues never overflow, and your connections always recover gracefully.** 🚀

---

**Happy Coding!** ⌨️✨

*If this project helped you in your journey, consider giving it a ⭐ star and sharing it with others who might benefit.*

---

### 👨‍💻 About the Author

**Dariem Carlos Macias**  
Software Engineer | Distributed Systems Enthusiast

This project represents my journey into the world of event-driven architecture, microservices patterns, and production-ready system design. Every line of code, every architectural decision, and every piece of documentation was crafted with the goal of not just building software, but building understanding.

*"The best way to learn is to teach, and the best way to teach is through code that speaks for itself."*

---

**— Built with .NET 8, RabbitMQ, Docker, and a lot of ☕**

</div>
