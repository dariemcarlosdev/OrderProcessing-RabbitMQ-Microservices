# OrderFlow.Core - Docker Deployment Guide

## Overview
OrderFlow.Core is a .NET 8 microservice application that uses RabbitMQ for message-based communication. This guide explains how to deploy the application using Docker Compose.

## Prerequisites
- Docker Desktop installed and running
- .NET 8 SDK (for local builds)

## Architecture
The application consists of two main services:
- **orderflow-core**: .NET 8 Web API with Swagger UI
- **rabbitmq**: RabbitMQ message broker with management UI

Both services communicate through a custom Docker network (`orderflow-network`).

## Quick Start

### 1. Build the Application Locally
First, publish the .NET application:
```bash
dotnet publish -c Release -o ./publish
```

### 2. Start Services with Docker Compose
```bash
docker-compose up -d
```

This will:
- Start RabbitMQ and wait for it to be healthy
- Start the OrderFlow.Core application
- Create a bridge network for inter-service communication

### 3. Verify Services are Running
```bash
docker-compose ps
```

You should see both containers running:
- `orderflow-rabbitmq`: Status `healthy`
- `orderflow-core`: Status `Up`

### 4. Access the Application

#### Swagger UI (API Documentation)
- **URL**: http://localhost:8080/swagger
- Provides interactive API documentation and testing interface

#### Health Check
- **URL**: http://localhost:8080/health
- Returns JSON with application health status and RabbitMQ connectivity

#### RabbitMQ Management UI
- **URL**: http://localhost:15672
- **Username**: `admin`
- **Password**: `admin123`
- Manage queues, exchanges, and monitor message flow

## Application Services

### API Endpoints
The application includes an Orders API with the following endpoints:
- `POST /api/orders` - Create a new order
- `GET /api/orders/{id}` - Get order by ID

### Message Subscribers
The application automatically subscribes to the following RabbitMQ queues:
1. **order_processing_queue** - Processes new orders
2. **payment_verification_queue** - Handles payment verification
3. **shipping_queue** - Manages shipping notifications
4. **notification_queue** - Sends customer notifications

## Configuration

### Environment Variables
The following environment variables are configured in `docker-compose.yml`:

#### Application Settings
- `ASPNETCORE_ENVIRONMENT`: Set to `Development`
- `ASPNETCORE_URLS`: `http://+:8080`

#### RabbitMQ Connection
- `RabbitMq__HostName`: `rabbitmq` (Docker service name)
- `RabbitMq__Port`: `5672`
- `RabbitMq__UserName`: `admin`
- `RabbitMq__Password`: `admin123`
- `RabbitMq__ExchangeName`: `order_exchange`
- `RabbitMq__ExchangeType`: `topic`

### Port Mappings
- **8080**: Application HTTP API
- **5672**: RabbitMQ AMQP protocol
- **15672**: RabbitMQ Management UI

## Docker Commands

### View Logs
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs orderflow-core
docker-compose logs rabbitmq

# Follow logs in real-time
docker-compose logs -f orderflow-core
```

### Stop Services
```bash
docker-compose down
```

### Stop and Remove Volumes
```bash
docker-compose down -v
```

### Rebuild and Restart
```bash
# Rebuild application locally
dotnet publish -c Release -o ./publish

# Rebuild Docker image
docker-compose build

# Restart services
docker-compose up -d
```

## Troubleshooting

### Container Won't Start
1. Check logs: `docker-compose logs orderflow-core`
2. Verify RabbitMQ is healthy: `docker-compose ps`
3. Ensure port 8080 is not in use: `netstat -ano | findstr :8080` (Windows)

### Cannot Connect to RabbitMQ
1. Check RabbitMQ health: `docker-compose ps`
2. Wait for RabbitMQ to be fully started (usually 10-15 seconds)
3. Check RabbitMQ logs: `docker-compose logs rabbitmq`

### Swagger UI Not Loading
1. Verify container is running: `docker-compose ps`
2. Check application logs: `docker-compose logs orderflow-core`
3. Test health endpoint: `curl http://localhost:8080/health`
4. Ensure you're accessing `http://localhost:8080/swagger` (not HTTPS)

### NuGet Restore Issues During Docker Build
If you encounter NuGet connectivity issues during Docker build:
1. Build locally first: `dotnet publish -c Release -o ./publish`
2. Use the simplified Dockerfile: `Dockerfile.simple` (already configured in docker-compose.yml)

## Network Architecture

```
┌─────────────────────────────────────────────┐
│         orderflow-network (bridge)          │
│                                             │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │   RabbitMQ   │◄────►│ OrderFlow.Core  │ │
│  │  :5672       │      │   :8080         │ │
│  │  :15672      │      │                 │ │
│  └──────────────┘      └─────────────────┘ │
│         ▲                       ▲           │
└─────────┼───────────────────────┼───────────┘
          │                       │
          │                       │
     localhost:15672         localhost:8080
     (Management UI)         (Swagger UI)
```

## Production Considerations

For production deployment, consider:
1. **Use secrets management** for RabbitMQ credentials (Azure Key Vault, Docker secrets)
2. **Enable HTTPS** with proper certificates
3. **Configure persistent volumes** for RabbitMQ data
4. **Set resource limits** in docker-compose.yml
5. **Use environment-specific configuration** files
6. **Enable monitoring and logging** (Application Insights, ELK stack)
7. **Implement health checks** at the infrastructure level (Kubernetes, Azure Container Apps)

## Files

- `docker-compose.yml`: Docker Compose orchestration configuration
- `Dockerfile.simple`: Simplified Dockerfile for containerization
- `Dockerfile`: Original multi-stage Dockerfile (for reference)
- `appsettings.json`: Application configuration
- `Program.cs`: Application startup and service registration

## Support

For issues or questions:
1. Check application logs: `docker-compose logs orderflow-core`
2. Verify RabbitMQ connectivity: `http://localhost:15672`
3. Test health endpoint: `http://localhost:8080/health`
