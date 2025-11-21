# Docker Containerization Guide for OrderFlow.Core

> *"Containers are not just about packaging—they're about building systems that can scale, heal, and evolve."*

## ?? Table of Contents

1. [Overview](#overview)
2. [Docker Compose Architecture](#docker-compose-architecture)
3. [Service Definitions](#service-definitions)
   - [RabbitMQ Service](#rabbitmq-service)
   - [OrderFlow.Core Application Service](#orderflowcore-application-service)
4. [Networking](#networking)
5. [Volumes and Data Persistence](#volumes-and-data-persistence)
6. [Service Orchestration](#service-orchestration)
7. [Environment Variables](#environment-variables)
8. [Health Checks](#health-checks)
9. [Build Context and Dockerfile](#build-context-and-dockerfile)
10. [Port Mappings](#port-mappings)
11. [Dependency Management](#dependency-management)
12. [Container Lifecycle](#container-lifecycle)
13. [Best Practices Applied](#best-practices-applied)
14. [Troubleshooting Container Issues](#troubleshooting-container-issues)
15. [Advanced Scenarios](#advanced-scenarios)

---

## Overview

The `docker-compose.yml` file is the **orchestration blueprint** for the OrderFlow.Core distributed system. It defines:

- **2 Services**: RabbitMQ message broker and OrderFlow.Core API
- **1 Network**: Custom bridge network for inter-container communication
- **1 Volume**: Persistent storage for RabbitMQ data
- **Health Checks**: Ensuring RabbitMQ is ready before starting the application
- **Environment Configuration**: All runtime settings via environment variables

### Why Docker Compose?

Docker Compose solves the complexity of multi-container applications by:
- ? **Single Command Deployment** — `docker-compose up` starts the entire stack
- ? **Declarative Configuration** — Infrastructure as code (YAML)
- ? **Service Dependencies** — Ensures RabbitMQ starts before the app
- ? **Network Isolation** — Services communicate securely on a private network
- ? **Reproducibility** — Same environment on every machine

---

## Docker Compose Architecture

### High-Level Architecture Diagram

```
???????????????????????????????????????????????????????????????????
?                     Docker Compose Stack                        ?
?                                                                 ?
?  ?????????????????????????????  ????????????????????????????  ?
?  ?   RabbitMQ Service        ?  ?   OrderFlow.Core         ?  ?
?  ?   (rabbitmq:3-mgmt)       ?  ?   (.NET 8 API)           ?  ?
?  ?                           ?  ?                          ?  ?
?  ?  - Port: 5672 (AMQP)      ????  - Port: 8080 (HTTP)     ?  ?
?  ?  - Port: 15672 (Mgmt UI)  ?  ?  - Depends on RabbitMQ   ?  ?
?  ?  - Health: rabbitmq-diag  ?  ?  - Environment: Dev      ?  ?
?  ?  - Volume: rabbitmq_data  ?  ?  - Restart: unless-stop  ?  ?
?  ?????????????????????????????  ????????????????????????????  ?
?              ?                               ?                 ?
?              ?????????????????????????????????                 ?
?                          ?                                     ?
?                ??????????????????????                          ?
?                ?  orderflow-network ?                          ?
?                ?  (bridge driver)   ?                          ?
?                ??????????????????????                          ?
?                                                                 ?
?  ????????????????????????????????????????????????????????????  ?
?  ?  Volume: orderflow_rabbitmq_data                         ?  ?
?  ?  (Persistent storage for RabbitMQ)                       ?  ?
?  ????????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????????
         ?                                    ?
         ?                                    ?
   localhost:15672                      localhost:8080
   (RabbitMQ UI)                        (Swagger API)
```

### Component Relationships

```
???????????????????????????????????????????????????????????????
?                    Orchestration Flow                       ?
???????????????????????????????????????????????????????????????

1. docker-compose up
   ?
   ??> Create Network: orderflow-network (bridge)
   ?
   ??> Create Volume: orderflow_rabbitmq_data
   ?
   ??> Start Service: rabbitmq
   ?   ?
   ?   ??> Pull Image: rabbitmq:3-management
   ?   ??> Create Container: orderflow-rabbitmq
   ?   ??> Attach to Network: orderflow-network
   ?   ??> Mount Volume: rabbitmq_data ? /var/lib/rabbitmq
   ?   ??> Expose Ports: 5672, 15672
   ?   ??> Set Environment: RABBITMQ_DEFAULT_USER, _PASS
   ?   ??> Start Health Checks (every 10s)
   ?       ?
   ?       ??> Wait for "Healthy" status (max 5 retries)
   ?
   ??> Start Service: orderflow-core (after RabbitMQ healthy)
       ?
       ??> Build Image from Dockerfile
       ??> Create Container: orderflow-core
       ??> Attach to Network: orderflow-network
       ??> Expose Port: 8080
       ??> Set Environment: ASPNETCORE_*, RabbitMq__*
       ??> Start Application
           ?
           ??> Connect to RabbitMQ via service name: rabbitmq:5672
```

---

## Service Definitions

### RabbitMQ Service

#### Complete Configuration

```yaml
rabbitmq:
  image: rabbitmq:3-management
  container_name: orderflow-rabbitmq
  hostname: rabbitmq
  ports:
    - "5672:5672"
    - "15672:15672"
  environment:
    RABBITMQ_DEFAULT_USER: admin
    RABBITMQ_DEFAULT_PASS: admin123
  healthcheck:
    test: rabbitmq-diagnostics -q ping
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 30s
  volumes:
    - rabbitmq_data:/var/lib/rabbitmq
  networks:
    - orderflow-network
```

#### Breakdown by Section

##### 1. **Image Selection**

```yaml
image: rabbitmq:3-management
```

**What it does:**
- Pulls the official RabbitMQ image with the **management plugin** pre-installed
- Version 3.x is the stable branch with .NET client compatibility

**Why `3-management`?**
- Includes the **Web UI** for monitoring queues, exchanges, and connections
- Essential for debugging message flow in development

**Alternative Tags:**
- `rabbitmq:3` — RabbitMQ without management UI (production)
- `rabbitmq:3-alpine` — Smaller image size (~50MB less)
- `rabbitmq:3.12-management` — Specific version pinning

**Best Practice:**
```yaml
image: rabbitmq:3.12-management  # Pin versions in production
```

---

##### 2. **Container & Hostname**

```yaml
container_name: orderflow-rabbitmq
hostname: rabbitmq
```

**Container Name:**
- Assigns a **static name** to the container
- Allows direct reference: `docker logs orderflow-rabbitmq`
- Without this, Docker generates random names like `orderflowcore-rabbitmq-1`

**Hostname:**
- Sets the internal **network hostname**
- Other containers use `rabbitmq` to connect (not `localhost`)
- Critical for service discovery within Docker networks

**Example Usage in App:**
```csharp
// In appsettings.json or environment variables
"RabbitMq": {
  "HostName": "rabbitmq",  // Uses the hostname defined in docker-compose
  "Port": 5672
}
```

---

##### 3. **Port Mappings**

```yaml
ports:
  - "5672:5672"   # AMQP protocol (for app connections)
  - "15672:15672" # Management UI (for browser)
```

**Format:** `"HOST:CONTAINER"`

| Host Port | Container Port | Purpose |
|-----------|----------------|---------|
| 5672 | 5672 | **AMQP Protocol** — Message broker communication |
| 15672 | 15672 | **Management UI** — Web interface at http://localhost:15672 |

**Why Expose Ports?**
- `5672` ? Allows **OrderFlow.Core** to connect to RabbitMQ
- `15672` ? Allows **developers** to access the web UI from the host machine

**Internal vs External Access:**
- **Internal** (container-to-container): Uses `rabbitmq:5672` via Docker network
- **External** (host machine): Uses `localhost:5672`

---

##### 4. **Environment Variables**

```yaml
environment:
  RABBITMQ_DEFAULT_USER: admin
  RABBITMQ_DEFAULT_PASS: admin123
```

**What it does:**
- Overrides default RabbitMQ credentials (`guest/guest`)
- Creates a new user with **full permissions**

**Default Behavior:**
- RabbitMQ's default `guest/guest` user only works from `localhost`
- Custom credentials enable remote connections (required for Docker)

**Security Considerations:**

?? **Development:**
```yaml
environment:
  RABBITMQ_DEFAULT_USER: admin
  RABBITMQ_DEFAULT_PASS: admin123  # Hardcoded for simplicity
```

? **Production:**
```yaml
environment:
  RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
  RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASS}
```

Use **Docker secrets** or **environment files**:
```bash
# .env file
RABBITMQ_USER=admin
RABBITMQ_PASS=SecureP@ssw0rd!
```

---

##### 5. **Health Checks**

```yaml
healthcheck:
  test: rabbitmq-diagnostics -q ping
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s
```

**What it does:**
- Runs `rabbitmq-diagnostics ping` **every 10 seconds**
- Marks container as **healthy** when ping succeeds
- Retries **5 times** before marking as **unhealthy**

**Parameter Breakdown:**

| Parameter | Value | Explanation |
|-----------|-------|-------------|
| `test` | `rabbitmq-diagnostics -q ping` | Command to check if RabbitMQ is ready |
| `interval` | `10s` | Check every 10 seconds |
| `timeout` | `5s` | Wait 5 seconds for response |
| `retries` | `5` | Try 5 times before declaring failure |
| `start_period` | `30s` | Grace period before starting checks |

**Why Health Checks Matter:**

Without health checks:
```yaml
depends_on:
  - rabbitmq  # Only waits for container to start (not ready)
```

With health checks:
```yaml
depends_on:
  rabbitmq:
    condition: service_healthy  # Waits until RabbitMQ is ready
```

**Lifecycle Timeline:**
```
0s    ? Container starts
      ?
30s   ? start_period ends ? Begin health checks
      ?
30s   ? ? Check 1: Fail (RabbitMQ still initializing)
40s   ? ? Check 2: Fail
50s   ? ? Check 3: Success ? Container marked HEALTHY
      ?
      ??> OrderFlow.Core starts (dependency satisfied)
```

---

##### 6. **Volumes (Data Persistence)**

```yaml
volumes:
  - rabbitmq_data:/var/lib/rabbitmq
```

**What it does:**
- Mounts a **named volume** to persist RabbitMQ data
- Data survives container restarts and rebuilds

**Volume Mounting:**

| Source | Destination | Purpose |
|--------|-------------|---------|
| `rabbitmq_data` (named volume) | `/var/lib/rabbitmq` (container path) | RabbitMQ data directory |

**What Gets Persisted:**
- Queues, exchanges, bindings
- Messages (if marked persistent)
- User accounts and permissions
- Configuration changes

**Without Volume:**
```
1. Create queues and messages
2. docker-compose down
3. docker-compose up
   ? All data LOST (queues gone, messages gone)
```

**With Volume:**
```
1. Create queues and messages
2. docker-compose down
3. docker-compose up
   ? All data RESTORED (queues exist, messages intact)
```

**Volume Management:**
```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect orderflow_rabbitmq_data

# Remove volume (deletes data!)
docker volume rm orderflow_rabbitmq_data
```

---

##### 7. **Networks**

```yaml
networks:
  - orderflow-network
```

**What it does:**
- Attaches RabbitMQ to the custom bridge network
- Enables communication with other services (OrderFlow.Core)

**Network Isolation:**

? **Without Custom Network:**
```
orderflow-core ? tries to connect to "rabbitmq"
                 ? DNS resolution fails
                 ? Connection error
```

? **With Custom Network:**
```
orderflow-core ? resolves "rabbitmq" via Docker DNS
               ? connects to orderflow-rabbitmq container
               ? success
```

---

### OrderFlow.Core Application Service

#### Complete Configuration

```yaml
orderflow-core:
  build:
    context: .
    dockerfile: Dockerfile
  container_name: orderflow-core
  hostname: orderflow-core
  ports:
    - "8080:8080"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ASPNETCORE_HTTP_PORTS=8080
    - ASPNETCORE_URLS=http://+:8080
    - RabbitMq__HostName=rabbitmq
    - RabbitMq__Port=5672
    - RabbitMq__UserName=admin
    - RabbitMq__Password=admin123
    - RabbitMq__ExchangeName=order_exchange
    - RabbitMq__ExchangeType=topic
  depends_on:
    rabbitmq:
      condition: service_healthy
  networks:
    - orderflow-network
  restart: unless-stopped
```

#### Breakdown by Section

##### 1. **Build Configuration**

```yaml
build:
  context: .
  dockerfile: Dockerfile
```

**Build Context:**
- `.` ? Current directory (where `docker-compose.yml` lives)
- Docker sends **all files** in this directory to the build daemon

**Dockerfile:**
- Specifies which Dockerfile to use
- Default: `Dockerfile` in the context directory
- Can be changed: `dockerfile: Dockerfile.production`

**Build Process:**
```bash
# Manual build equivalent
docker build -t orderflow-core:latest -f Dockerfile .
```

**Build vs Image:**

| Use `build:` | Use `image:` |
|-------------|-------------|
| Build from source code | Pull pre-built image |
| Development environments | Production deployments |
| Custom Dockerfile | Official/public images |

**Example with Image:**
```yaml
orderflow-core:
  image: myregistry.azurecr.io/orderflow-core:v1.2.3
  # No build needed, just pull and run
```

---

##### 2. **Container & Hostname**

```yaml
container_name: orderflow-core
hostname: orderflow-core
```

**Same principles as RabbitMQ:**
- `container_name` ? Static name for commands (`docker logs orderflow-core`)
- `hostname` ? Internal DNS name on the Docker network

**Hostname Use Cases:**
- Service discovery (if you add more services)
- Logging and tracing (hostname appears in logs)
- Debugging (easily identify which container logs belong to)

---

##### 3. **Port Mappings**

```yaml
ports:
  - "8080:8080"
```

**Why Only HTTP (8080)?**

In the simplified `Dockerfile.simple`, we removed HTTPS to avoid certificate issues:

```dockerfile
# Original (with HTTPS)
EXPOSE 8080
EXPOSE 8081

# Simplified (HTTP only)
EXPOSE 8080
```

**HTTPS in Docker:**

To enable HTTPS, you'd need:
1. **Development Certificate:**
   ```bash
   dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p YourPassword
   ```

2. **Mount Certificate:**
   ```yaml
   volumes:
     - ~/.aspnet/https:/https:ro
   environment:
     - ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword
     - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
   ```

3. **Expose Port:**
   ```yaml
   ports:
     - "8080:8080"
     - "8081:8081"
   ```

**For production**, use a **reverse proxy** (Nginx, Traefik) to handle SSL termination.

---

##### 4. **Environment Variables**

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_HTTP_PORTS=8080
  - ASPNETCORE_URLS=http://+:8080
  - RabbitMq__HostName=rabbitmq
  - RabbitMq__Port=5672
  - RabbitMq__UserName=admin
  - RabbitMq__Password=admin123
  - RabbitMq__ExchangeName=order_exchange
  - RabbitMq__ExchangeType=topic
```

#### ASP.NET Core Configuration

| Variable | Value | Purpose |
|----------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Enables Swagger UI, detailed errors |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Tells Kestrel to listen on port 8080 |
| `ASPNETCORE_URLS` | `http://+:8080` | Bind to all network interfaces |

**Environment Behavior:**

```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();      // ? Enabled in Development
    app.UseSwaggerUI();
}
```

#### RabbitMQ Configuration Override

**Double Underscore (`__`) Syntax:**

```yaml
RabbitMq__HostName=rabbitmq
```

Maps to:
```json
{
  "RabbitMq": {
    "HostName": "rabbitmq"
  }
}
```

**How It Works:**

1. **appsettings.json** (default):
   ```json
   "RabbitMq": {
     "HostName": "localhost",
     "Port": 5672
   }
   ```

2. **Environment Variable Override**:
   ```yaml
   RabbitMq__HostName=rabbitmq
   ```

3. **Final Configuration** (what the app sees):
   ```json
   "RabbitMq": {
     "HostName": "rabbitmq",  // ? Overridden
     "Port": 5672
   }
   ```

**Why Override?**

| Environment | HostName |
|------------|----------|
| Local Development | `localhost` (RabbitMQ on host) |
| Docker Compose | `rabbitmq` (RabbitMQ service name) |
| Kubernetes | `rabbitmq-service.default.svc.cluster.local` |

**Configuration Hierarchy:**
```
1. appsettings.json (lowest priority)
2. appsettings.{Environment}.json
3. Environment Variables
4. Command-line arguments (highest priority)
```

---

##### 5. **Dependency Management**

```yaml
depends_on:
  rabbitmq:
    condition: service_healthy
```

**What it does:**
- **Waits** for RabbitMQ to be **healthy** before starting OrderFlow.Core
- Prevents connection failures during startup

**Dependency Strategies:**

? **Basic (Unsafe):**
```yaml
depends_on:
  - rabbitmq  # Only waits for container to START (not ready!)
```

**Problem:**
```
1. RabbitMQ container starts
2. OrderFlow.Core starts immediately
3. App tries to connect ? RabbitMQ still initializing
4. Connection fails ? App crashes or hangs
```

? **Health Check (Recommended):**
```yaml
depends_on:
  rabbitmq:
    condition: service_healthy  # Waits until health check passes
```

**Result:**
```
1. RabbitMQ container starts
2. Health checks run (30s grace period + retries)
3. RabbitMQ marked HEALTHY
4. OrderFlow.Core starts
5. App connects successfully
```

**Startup Timeline:**
```
0s    ? docker-compose up
      ?
      ??> Start rabbitmq container
      ?
30s   ? Health check grace period
      ?
50s   ? RabbitMQ HEALTHY
      ?   ??> Dependency satisfied
      ?
51s   ? Start orderflow-core container
      ?
55s   ? OrderFlow.Core connects to RabbitMQ
      ?   ??> Success!
```

---

##### 6. **Restart Policy**

```yaml
restart: unless-stopped
```

**Restart Policies:**

| Policy | Behavior |
|--------|----------|
| `no` | Never restart (default) |
| `always` | Always restart (even after `docker-compose down`) |
| `on-failure` | Restart only if exit code != 0 |
| `unless-stopped` | Always restart **unless manually stopped** |

**Why `unless-stopped`?**

? **Wanted Behavior:**
- App crashes ? Docker restarts it automatically
- RabbitMQ connection lost ? App retries, Docker restarts if needed
- Machine reboots ? Services start automatically

? **Unwanted Behavior:**
- `docker-compose down` ? Services should STOP (not restart)
- Manual stop ? Services should STAY stopped

**Example Scenarios:**

```bash
# Scenario 1: App crashes
App crashes ? Exit code 1
              ? Docker restarts container
              ? App runs again

# Scenario 2: Intentional stop
docker-compose down ? All services stop
                     ? Services stay stopped (unless-stopped respects this)

# Scenario 3: Machine reboot
Machine restarts ? Docker daemon starts
                  ? Containers restart automatically
```

**Production Recommendation:**

```yaml
restart: always  # For critical services in production
```

But consider using **orchestrators** (Kubernetes, Docker Swarm) instead for better control.

---

## Networking

### Network Definition

```yaml
networks:
  orderflow-network:
    driver: bridge
    name: orderflow-network
```

#### Bridge Network Explained

**What is a Bridge Network?**

A **bridge network** is a **private internal network** created by Docker. Containers on the same bridge can communicate with each other using **container names** as hostnames.

**Network Drivers:**

| Driver | Use Case |
|--------|----------|
| `bridge` | Default, single-host container communication |
| `host` | Container shares host network (no isolation) |
| `overlay` | Multi-host communication (Swarm, Kubernetes) |
| `macvlan` | Assign MAC addresses to containers |
| `none` | No networking (isolated container) |

#### How Bridge Networks Work

```
???????????????????????????????????????????????????????
?               Host Machine (Your Computer)          ?
?                                                     ?
?  ????????????????????????????????????????????????  ?
?  ?       orderflow-network (172.18.0.0/16)      ?  ?
?  ?                                              ?  ?
?  ?  ???????????????????   ???????????????????  ?  ?
?  ?  ?   rabbitmq      ?   ? orderflow-core  ?  ?  ?
?  ?  ?  172.18.0.2     ?   ?   172.18.0.3    ?  ?  ?
?  ?  ?                 ?   ?                 ?  ?  ?
?  ?  ?  hostname:      ?   ?  hostname:      ?  ?  ?
?  ?  ?  rabbitmq       ?????  rabbitmq:5672  ?  ?  ?
?  ?  ???????????????????   ???????????????????  ?  ?
?  ?                                              ?  ?
?  ????????????????????????????????????????????????  ?
?                       ?                             ?
?                       ?                             ?
?                  Port Mapping                       ?
?                       ?                             ?
?  ?????????????????????????????????????????????????  ?
?  ?         Host Network (localhost)              ?  ?
?  ?   - localhost:5672  ? rabbitmq:5672           ?  ?
?  ?   - localhost:8080  ? orderflow-core:8080     ?  ?
?  ?   - localhost:15672 ? rabbitmq:15672          ?  ?
?  ?????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????
```

#### DNS Resolution

**Automatic DNS:**

Docker provides automatic DNS resolution for **container names** and **service names**.

```yaml
# OrderFlow.Core connects using service name
RabbitMq__HostName=rabbitmq
```

**Behind the Scenes:**
```
1. App tries to connect to "rabbitmq"
2. Docker DNS resolves "rabbitmq" ? 172.18.0.2
3. Connection established
```

**Why Not Use IP Addresses?**

? **Bad:**
```yaml
RabbitMq__HostName=172.18.0.2  # IP can change between restarts
```

? **Good:**
```yaml
RabbitMq__HostName=rabbitmq  # Name is always consistent
```

#### Network Isolation

**Containers on the same network:**
```
rabbitmq ?? orderflow-core  ? Can communicate
```

**Containers on different networks:**
```
rabbitmq (network A) ?X? other-app (network B)  ? Cannot communicate
```

**Multiple Networks:**

```yaml
orderflow-core:
  networks:
    - orderflow-network  # Internal communication
    - public-network     # External API access
```

#### Network Commands

```bash
# List networks
docker network ls

# Inspect network
docker network inspect orderflow-network

# View connected containers
docker network inspect orderflow-network --format '{{json .Containers}}' | jq

# Connect a container to network
docker network connect orderflow-network my-container

# Disconnect
docker network disconnect orderflow-network my-container

# Remove network (only if no containers attached)
docker network rm orderflow-network
```

---

## Volumes and Data Persistence

### Volume Definition

```yaml
volumes:
  rabbitmq_data:
    name: orderflow_rabbitmq_data
```

#### Why Named Volumes?

**Named vs Anonymous Volumes:**

| Named Volume | Anonymous Volume |
|--------------|------------------|
| `rabbitmq_data` | `a1b2c3d4...` (random hash) |
| Explicit name | Auto-generated name |
| Easy to manage | Hard to identify |
| Survives `down -v` | Deleted with `down -v` |

**Named Volume Usage:**
```yaml
services:
  rabbitmq:
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  rabbitmq_data:
    name: orderflow_rabbitmq_data
```

#### Volume Storage Location

**Where is data stored?**

Docker stores volumes in:
```
# Linux
/var/lib/docker/volumes/orderflow_rabbitmq_data/_data

# Windows (WSL2)
\\wsl$\docker-desktop-data\data\docker\volumes\orderflow_rabbitmq_data\_data

# Mac
~/Library/Containers/com.docker.docker/Data/vms/0/data/docker/volumes/orderflow_rabbitmq_data/_data
```

**Access Volume Data:**
```bash
# Linux/Mac
docker volume inspect orderflow_rabbitmq_data --format '{{.Mountpoint}}'

# Windows (WSL2)
explorer.exe $(wslpath -w $(docker volume inspect orderflow_rabbitmq_data --format '{{.Mountpoint}}'))
```

#### What RabbitMQ Stores

**Inside `/var/lib/rabbitmq`:**
```
/var/lib/rabbitmq/
??? mnesia/                  # Database files
?   ??? rabbit@rabbitmq/
?   ?   ??? queues.dat       # Queue definitions
?   ?   ??? exchanges.dat    # Exchange definitions
?   ?   ??? bindings.dat     # Routing bindings
?   ?   ??? messages.dat     # Persisted messages
??? schema/                  # RabbitMQ schema
??? plugins/                 # Plugin data
```

#### Volume Lifecycle

**Scenario 1: Normal Operation**
```bash
docker-compose up     # Volume created (if doesn't exist)
                      # Data persisted to volume

docker-compose down   # Containers stopped and removed
                      # Volume remains intact

docker-compose up     # Volume reused
                      # Data restored automatically
```

**Scenario 2: Volume Removal**
```bash
docker-compose down -v  # Remove containers AND volumes
                        # All data deleted!
```

**Scenario 3: Backup**
```bash
# Backup volume to tar
docker run --rm \
  -v orderflow_rabbitmq_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/rabbitmq-backup.tar.gz -C /data .

# Restore from tar
docker run --rm \
  -v orderflow_rabbitmq_data:/data \
  -v $(pwd):/backup \
  alpine sh -c "cd /data && tar xzf /backup/rabbitmq-backup.tar.gz"
```

#### Volume vs Bind Mounts

**Volume (Recommended for production data):**
```yaml
volumes:
  - rabbitmq_data:/var/lib/rabbitmq  # Named volume
```

**Bind Mount (Recommended for configuration):**
```yaml
volumes:
  - ./config/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro  # Host file ? Container
```

**Comparison:**

| Feature | Volume | Bind Mount |
|---------|--------|------------|
| Management | Docker-managed | User-managed |
| Performance | Optimized | Depends on OS |
| Backup | `docker volume` commands | Regular file tools |
| Portability | Cross-platform | OS-dependent paths |
| Security | Isolated | Host filesystem access |

---

## Service Orchestration

### Startup Order

Docker Compose orchestrates services in this order:

```
1. Networks
   ??> Create orderflow-network

2. Volumes
   ??> Create orderflow_rabbitmq_data

3. Services (dependency order)
   ??> Start rabbitmq
   ?   ??> Wait for health check
   ?       ??> Status: HEALTHY
   ?
   ??> Start orderflow-core
       ??> Connect to rabbitmq
```

### Dependency Graph

```
???????????????????????????????????????????????
?           Service Dependencies              ?
???????????????????????????????????????????????

orderflow-core
      ?
      ? depends_on:
      ?   rabbitmq:
      ?     condition: service_healthy
      ?
      ?
  rabbitmq
      ?
      ? healthcheck:
      ?   test: rabbitmq-diagnostics -q ping
      ?   interval: 10s
      ?   retries: 5
      ?
      ?
  HEALTHY ? orderflow-core starts
```

### Parallel vs Sequential Startup

**Without Dependencies:**
```yaml
services:
  rabbitmq:
    # ...
  orderflow-core:
    # No depends_on
```

**Result:** Both start **in parallel** (race condition!)

```
Time  ? rabbitmq       ? orderflow-core
??????????????????????????????????????????
0s    ? Starting...    ? Starting...
1s    ? Initializing   ? Connecting... ? Failed
2s    ? Ready          ? Crashed
```

**With Dependencies:**
```yaml
services:
  rabbitmq:
    healthcheck: ...
  orderflow-core:
    depends_on:
      rabbitmq:
        condition: service_healthy
```

**Result:** Sequential startup (safe)

```
Time  ? rabbitmq       ? orderflow-core
??????????????????????????????????????????
0s    ? Starting...    ? Waiting...
30s   ? Health checks  ? Waiting...
50s   ? HEALTHY        ? Starting...
55s   ? Running        ? Connecting... ? Success
```

---

## Environment Variables

### Configuration Layers

ASP.NET Core uses a **layered configuration system**:

```
???????????????????????????????????????????????
?         Configuration Precedence            ?
?         (Highest to Lowest Priority)        ?
???????????????????????????????????????????????

4. Command-line arguments (highest)
   ?
3. Environment variables ? docker-compose.yml sets these
   ?
2. appsettings.{Environment}.json
   ?
1. appsettings.json (lowest)
```

### Environment Variable Formats

#### List Format

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:8080
  - RabbitMq__HostName=rabbitmq
```

#### Dictionary Format

```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Development
  ASPNETCORE_URLS: http://+:8080
  RabbitMq__HostName: rabbitmq
```

**Both are equivalent**, but list format is more common in Docker Compose.

### Environment File

**For sensitive data or many variables:**

```yaml
# docker-compose.yml
services:
  orderflow-core:
    env_file:
      - .env.development
```

```bash
# .env.development
ASPNETCORE_ENVIRONMENT=Development
RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=admin
RABBITMQ_PASS=admin123
```

**Advantages:**
- ? Keep secrets out of `docker-compose.yml`
- ? Different files for different environments (`.env.dev`, `.env.prod`)
- ? Easier to manage many variables

---

## Health Checks

### Health Check Mechanism

**How Health Checks Work:**

```
???????????????????????????????????????????????
?          Health Check Lifecycle             ?
???????????????????????????????????????????????

Container Start
    ?
    ??> start_period (30s grace period)
    ?   ??> No checks during this time
    ?
    ??> First Check (at 30s)
    ?   ??> Run: rabbitmq-diagnostics -q ping
    ?   ??> Result: Exit code 0 = Success, Non-zero = Failure
    ?
    ??> If Success:
    ?   ??> Mark container HEALTHY
    ?
    ??> If Failure:
        ??> Wait interval (10s)
        ??> Retry (max 5 times)
        ??> If all retries fail:
            ??> Mark container UNHEALTHY
```

### Health Check States

| State | Meaning |
|-------|---------|
| `starting` | Container started, within `start_period` |
| `healthy` | Health check passed |
| `unhealthy` | Health check failed after retries |

### Custom Health Check Commands

**RabbitMQ:**
```yaml
test: rabbitmq-diagnostics -q ping
```

**Alternative Checks:**
```yaml
# Check if management API responds
test: ["CMD-SHELL", "curl -f http://localhost:15672/api/overview || exit 1"]

# Check specific node
test: ["CMD", "rabbitmqctl", "node_health_check"]
```

**ASP.NET Core:**
```yaml
orderflow-core:
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
```

### Health Check Best Practices

? **Do:**
- Use health checks for critical dependencies
- Set appropriate `start_period` (allow app to initialize)
- Use `interval` that matches app startup time (not too frequent)

? **Don't:**
- Set `interval` too low (creates unnecessary load)
- Forget `timeout` (hanging checks block startup)
- Skip `start_period` (false positives during startup)

---

## Build Context and Dockerfile

### Build Context

```yaml
build:
  context: .
  dockerfile: Dockerfile
```

**What Gets Sent to Docker?**

The `context` directory and **all subdirectories**:

```
OrderFlow.Core/  ? context: .
??? Controllers/
??? Models/
??? Infrastructure/
??? Dockerfile
??? docker-compose.yml
??? OrderFlow.Core.csproj
??? ... (everything)
```

### Optimizing Build Context

**Problem:** Large context slows down builds.

**Solution:** Use `.dockerignore`:

```
# .dockerignore
**/.vs
**/.vscode
**/bin
**/obj
**/publish
**/*.user
**/node_modules
```

**Result:** Docker ignores these folders during build.

### Multi-Stage Dockerfile

**Current Dockerfile Structure:**

```dockerfile
# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ["OrderFlow.Core.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish -o /app/publish

# Stage 4: Final
FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderFlow.Core.dll"]
```

**Why Multi-Stage?**

| Benefit | Explanation |
|---------|-------------|
| **Smaller Image** | Final image only contains runtime + binaries (not SDK) |
| **Security** | No build tools in production image |
| **Speed** | Layer caching speeds up rebuilds |

**Image Size Comparison:**

| Approach | Size |
|----------|------|
| Single-stage (with SDK) | ~720 MB |
| Multi-stage (runtime only) | ~210 MB |

---

## Port Mappings

### Port Format

```yaml
ports:
  - "HOST:CONTAINER"
```

**Examples:**

```yaml
ports:
  - "8080:8080"    # Host 8080 ? Container 8080
  - "80:8080"      # Host 80 ? Container 8080
  - "8080"         # Random host port ? Container 8080
```

### Port Binding Modes

**Bind to All Interfaces:**
```yaml
ports:
  - "8080:8080"  # Accessible from anywhere
```

**Bind to Localhost Only:**
```yaml
ports:
  - "127.0.0.1:8080:8080"  # Only accessible from host machine
```

**Bind to Specific IP:**
```yaml
ports:
  - "192.168.1.100:8080:8080"
```

### Port Conflicts

**Problem:**
```bash
docker-compose up
# Error: Bind for 0.0.0.0:8080 failed: port is already allocated
```

**Solutions:**

1. **Change Host Port:**
   ```yaml
   ports:
     - "8081:8080"  # Use different host port
   ```

2. **Find Process Using Port:**
   ```bash
   # Windows
   netstat -ano | findstr :8080
   
   # Linux/Mac
   lsof -i :8080
   ```

3. **Stop Conflicting Service:**
   ```bash
   # Stop local .NET app using port 8080
   ```

---

## Dependency Management

### Dependency Conditions

Docker Compose supports multiple dependency conditions:

```yaml
depends_on:
  rabbitmq:
    condition: service_healthy        # ? Recommended
    # OR
    condition: service_started        # Basic (just waits for start)
    # OR
    condition: service_completed_successfully  # For init containers
```

### Complex Dependencies

**Multiple Dependencies:**

```yaml
orderflow-core:
  depends_on:
    rabbitmq:
      condition: service_healthy
    database:
      condition: service_healthy
    redis:
      condition: service_started
```

**Startup Order:**
```
1. Start rabbitmq, database, redis (parallel)
2. Wait for rabbitmq ? HEALTHY
3. Wait for database ? HEALTHY
4. Wait for redis ? STARTED
5. Start orderflow-core
```

### Init Containers

**Pattern for Database Migrations:**

```yaml
services:
  db-migrate:
    image: orderflow-migrations
    command: dotnet ef database update
    depends_on:
      database:
        condition: service_healthy

  orderflow-core:
    depends_on:
      db-migrate:
        condition: service_completed_successfully
```

---

## Container Lifecycle

### Common Commands

```bash
# Start services (detached)
docker-compose up -d

# Start and rebuild
docker-compose up -d --build

# View logs
docker-compose logs -f orderflow-core

# Stop services (keep containers)
docker-compose stop

# Start stopped services
docker-compose start

# Restart service
docker-compose restart orderflow-core

# Stop and remove containers
docker-compose down

# Stop and remove containers + volumes
docker-compose down -v

# Stop and remove containers + images
docker-compose down --rmi all

# View service status
docker-compose ps

# View resource usage
docker-compose stats

# Execute command in running container
docker-compose exec orderflow-core bash

# Scale service (multiple instances)
docker-compose up -d --scale orderflow-core=3
```

### Service States

```
???????????????????????????????????????????????
?           Container State Machine           ?
???????????????????????????????????????????????

          docker-compose up
                  ?
                  ?
            ????????????
            ? Created  ?
            ????????????
                  ?
                  ?
            ????????????
            ? Starting ?
            ????????????
                  ?
                  ?
            ????????????
            ? Running  ????????
            ????????????      ?
                  ?            ?
        ????????????????????????
        ?         ?            ?
        ?         ?            ?
   ?????????? ???????????     ?
   ? Paused ? ? Exited  ?     ?
   ?????????? ???????????     ?
       ?            ?          ?
       ?  restart   ?          ?
       ?????????????????????????

docker-compose down
       ?
       ?
   Removed
```

---

## Best Practices Applied

### ? 1. **Named Containers & Networks**

```yaml
container_name: orderflow-rabbitmq
hostname: rabbitmq
networks:
  - orderflow-network
```

**Why:** Easy identification, consistent DNS resolution

---

### ? 2. **Health Checks for Dependencies**

```yaml
healthcheck:
  test: rabbitmq-diagnostics -q ping
  interval: 10s
  retries: 5
```

**Why:** Prevents connection failures during startup

---

### ? 3. **Data Persistence with Named Volumes**

```yaml
volumes:
  rabbitmq_data:
    name: orderflow_rabbitmq_data
```

**Why:** Data survives container restarts

---

### ? 4. **Environment Variable Configuration**

```yaml
environment:
  - RabbitMq__HostName=rabbitmq
  - RabbitMq__UserName=admin
```

**Why:** Environment-specific settings without code changes

---

### ? 5. **Restart Policies**

```yaml
restart: unless-stopped
```

**Why:** Automatic recovery from crashes

---

### ? 6. **Port Consistency**

```yaml
ports:
  - "8080:8080"  # Same port inside and outside
```

**Why:** Easier debugging and configuration

---

### ? 7. **Multi-Stage Docker Build**

```dockerfile
FROM build AS publish
RUN dotnet publish

FROM base AS final
COPY --from=publish /app/publish .
```

**Why:** Smaller, more secure production images

---

### ? 8. **Network Isolation**

```yaml
networks:
  orderflow-network:
    driver: bridge
```

**Why:** Secure communication, DNS resolution

---

### ? 9. **Graceful Startup Order**

```yaml
depends_on:
  rabbitmq:
    condition: service_healthy
```

**Why:** Services start in the correct order

---

### ? 10. **Development vs Production Settings**

```yaml
ASPNETCORE_ENVIRONMENT: Development
```

**Why:** Different behavior for dev/prod (Swagger, error details)

---

## Troubleshooting Container Issues

### Issue 1: Container Won't Start

**Symptoms:**
```bash
docker-compose ps
# orderflow-core   Exit 1
```

**Diagnosis:**
```bash
docker-compose logs orderflow-core
```

**Common Causes:**
- Missing environment variables
- Port conflicts
- Connection to RabbitMQ failed
- Application error

**Solution:**
1. Check logs for specific error
2. Verify RabbitMQ is healthy: `docker-compose ps`
3. Check port availability: `netstat -ano | findstr :8080`

---

### Issue 2: Cannot Connect to RabbitMQ

**Symptoms:**
```
BrokerUnreachableException: None of the specified endpoints were reachable
```

**Diagnosis:**
```bash
# Check if RabbitMQ is healthy
docker-compose ps

# Check RabbitMQ logs
docker-compose logs rabbitmq

# Check network connectivity
docker-compose exec orderflow-core ping rabbitmq
```

**Common Causes:**
- RabbitMQ not healthy yet
- Wrong hostname (should be `rabbitmq`, not `localhost`)
- Network not connected
- Credentials mismatch

**Solution:**
```yaml
# Ensure health check is configured
depends_on:
  rabbitmq:
    condition: service_healthy

# Verify hostname
environment:
  - RabbitMq__HostName=rabbitmq  # NOT localhost
```

---

### Issue 3: Port Already in Use

**Symptoms:**
```
Error: Bind for 0.0.0.0:8080 failed: port is already allocated
```

**Solution:**

**Option 1: Change Host Port**
```yaml
ports:
  - "8081:8080"  # Use different host port
```

**Option 2: Stop Conflicting Process**
```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Linux/Mac
lsof -i :8080
kill -9 <PID>
```

---

### Issue 4: Volume Permission Errors

**Symptoms:**
```
Permission denied: '/var/lib/rabbitmq/...'
```

**Solution:**

**Linux/Mac:**
```yaml
rabbitmq:
  user: "999:999"  # RabbitMQ UID:GID
```

**Or use named volumes (recommended):**
```yaml
volumes:
  - rabbitmq_data:/var/lib/rabbitmq
```

---

### Issue 5: Build Fails

**Symptoms:**
```
Error: failed to solve: process "/bin/sh -c dotnet restore" did not complete successfully
```

**Common Causes:**
- NuGet connectivity issues
- Missing `.csproj` file
- Wrong Dockerfile

**Solution:**

**Use Simplified Dockerfile:**
```bash
# Build locally first
dotnet publish -c Release -o ./publish

# Use Dockerfile.simple
docker-compose -f docker-compose.yml build
```

---

## Advanced Scenarios

### Scenario 1: Multiple Environments

**Structure:**
```
??? docker-compose.yml         # Base configuration
??? docker-compose.dev.yml     # Development overrides
??? docker-compose.prod.yml    # Production overrides
```

**docker-compose.yml:**
```yaml
services:
  orderflow-core:
    build:
      context: .
    environment:
      - RabbitMq__HostName=rabbitmq
```

**docker-compose.dev.yml:**
```yaml
services:
  orderflow-core:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    ports:
      - "8080:8080"
```

**docker-compose.prod.yml:**
```yaml
services:
  orderflow-core:
    image: myregistry.azurecr.io/orderflow-core:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: always
```

**Usage:**
```bash
# Development
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up
```

---

### Scenario 2: Scaling Services

**Horizontal Scaling:**
```bash
docker-compose up -d --scale orderflow-core=3
```

**Result:**
```
orderflow-core_1
orderflow-core_2
orderflow-core_3
```

**With Load Balancer:**
```yaml
services:
  nginx:
    image: nginx
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - orderflow-core

  orderflow-core:
    # No direct port exposure (nginx handles it)
```

---

### Scenario 3: External Secrets

**Using Docker Secrets (Swarm):**
```yaml
services:
  rabbitmq:
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS_FILE: /run/secrets/rabbitmq_pass
    secrets:
      - rabbitmq_pass

secrets:
  rabbitmq_pass:
    external: true
```

**Using Azure Key Vault (via environment):**
```bash
# Fetch from Key Vault
RABBITMQ_PASS=$(az keyvault secret show --vault-name mykeyvault --name rabbitmq-pass --query value -o tsv)

# Export for docker-compose
export RABBITMQ_PASS

# Use in docker-compose.yml
environment:
  - RABBITMQ_DEFAULT_PASS=${RABBITMQ_PASS}
```

---

### Scenario 4: CI/CD Integration

**GitHub Actions Example:**
```yaml
name: Deploy with Docker Compose

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build application
        run: dotnet publish -c Release -o ./publish
      
      - name: Deploy with Docker Compose
        run: |
          docker-compose build
          docker-compose up -d
      
      - name: Health check
        run: |
          sleep 10
          curl http://localhost:8080/health
```

---

## Summary

This `docker-compose.yml` demonstrates **production-ready** container orchestration:

? **Service Dependencies** — RabbitMQ starts before the app  
? **Health Checks** — Ensures services are ready  
? **Network Isolation** — Secure internal communication  
? **Data Persistence** — Volumes prevent data loss  
? **Environment Configuration** — Easy customization  
? **Restart Policies** — Automatic recovery  
? **Logging & Monitoring** — Easy access to logs  

### Key Takeaways

1. **Health checks are critical** — Don't just wait for container start
2. **Use named volumes** — Easier to manage than anonymous volumes
3. **Custom networks** — Enable DNS resolution and isolation
4. **Environment variables** — Configuration without code changes
5. **Depends_on + health checks** — Ensure proper startup order
6. **Restart policies** — Build resilient systems

---

## Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Dockerfile Best Practices](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)
- [Docker Networking Guide](https://docs.docker.com/network/)
- [Docker Volumes Guide](https://docs.docker.com/storage/volumes/)
- [Health Check Reference](https://docs.docker.com/engine/reference/builder/#healthcheck)

---

<div align="center">

**Built with Docker Compose, RabbitMQ, and .NET 8** ??

*"Containers are not just about packaging—they're about building systems that can scale, heal, and evolve."*

</div>
