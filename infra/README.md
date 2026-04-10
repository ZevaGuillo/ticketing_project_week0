# Infrastructure — Operations Guide

## Quick Start

```bash
cd infra
docker compose up -d
```

Verify all services are healthy:
```bash
docker compose ps
```

## Architecture Overview

```
                                    ┌─────────────────┐
                                    │   Frontend      │
                                    │  (localhost:3000)│
                                    └────────┬────────┘
                                             │
                                    ┌────────▼────────┐
                                    │    Gateway      │
                                    │ (localhost:5000) │◄── Only exposed port
                                    │   (YARP)        │
                                    └────────┬────────┘
                                             │
                    ┌────────────────────────┼────────────────────────┐
                    │                        │                        │
           ┌────────▼────────┐     ┌────────▼────────┐     ┌────────▼────────┐
           │     Identity     │     │     Catalog      │     │    Inventory    │
           │  (internal:5001)  │     │  (internal:5001) │     │  (internal:5002) │
           └───────────────────┘     └───────────────────┘     └───────────────────┘
                    │                                                         │
           ┌────────▼────────┐                                       ┌────────▼────────┐
           │     Ordering     │                                       │    Payment      │
           │  (internal:5003) │                                       │  (internal:5005) │
           └───────────────────┘                                       └───────────────────┘
                    │
           ┌────────▼────────┐
           │   Fulfillment    │
           │  (internal:5004) │
           └───────────────────┘
```

**Network Model:**
- Gateway is the **only exposed service** (port 5000)
- All backend services communicate internally via Docker DNS
- No direct access to internal services from outside

## Smoke Test

Run the infrastructure smoke test to verify all services are running:

```bash
# From repo root
chmod +x smoke-test.sh
./smoke-test.sh
```

This script verifies:
- ✓ PostgreSQL is responding
- ✓ Redis is responding
- ✓ Kafka broker is responding
- ✓ Gateway health endpoint (if running)

## PostgreSQL Connection String

**Development Connection String:**
```
Server=localhost;Port=5432;Database=ticketing;User Id=postgres;Password=postgres;
```

**Inside Docker Network (service-to-service):**
```
Server=postgres;Port=5432;Database=ticketing;User Id=postgres;Password=postgres;
```

**EF Core .NET Configuration:**
```csharp
// In each service's DbContext
optionsBuilder.UseNpgsql(connectionString);
// Schema is configured per context: modelBuilder.HasDefaultSchema("bc_<service>");
```

## Schemas & Bounded Contexts

| Schema | Service | Purpose |
|--------|---------|---------|
| `bc_identity` | Identity | User/token management |
| `bc_catalog` | Catalog | Event catalog (read-side) |
| `bc_inventory` | Inventory | Seat reservations & availability |
| `bc_ordering` | Ordering | Shopping cart & orders |
| `bc_payment` | Payment | Payment records (simulated) |
| `bc_fulfillment` | Fulfillment | Ticket generation & PDF storage |
| `bc_notification` | Notification | Notification state & delivery logs |

## Services & Ports

| Service | Port | Exposure |
|--------|------|----------|
| **Gateway** | 5000 | **Public** (YARP reverse proxy) |
| PostgreSQL | 5432 | Internal only |
| Redis | 6379 | Internal only |
| Zookeeper | 2181 | Internal only |
| Kafka Broker | 9092 | Internal only |
| Identity | 5001 | Internal (via gateway) |
| Catalog | 5001 | Internal (via gateway) |
| Inventory | 5002 | Internal (via gateway) |
| Ordering | 5003 | Internal (via gateway) |
| Payment | 5005 | Internal (via gateway) |
| Fulfillment | 5004 | Internal (via gateway) |
| Notification | 5006 | Internal (via gateway) |

## Gateway Routes

| Path | Backend Service | Auth Required |
|------|----------------|--------------|
| `/auth/*` | Identity | No |
| `/catalog/*` | Catalog | No |
| `/inventory/*` | Inventory | Yes (JWT) |
| `/ordering/*` | Ordering | Yes (JWT) |
| `/payment/*` | Payment | Yes (JWT) |
| `/fulfillment/*` | Fulfillment | Yes (JWT) |
| `/admin/*` | Various | Yes (Admin role) |

## Migration & Schema Initialization

**Initial setup:**
- `init-schemas.sql` runs automatically on `docker compose up`.
- All schemas and tables created in sequence.

**Adding new migrations (EF Core):**
```bash
cd services/<service>/src
dotnet ef migrations add <MigrationName> --project Infrastructure
dotnet ef database update
```

## Shutting Down

```bash
docker compose down
# Preserve volumes:

docker compose down
# Remove all data:
docker compose down -v
```

## Troubleshooting

**Postgres connection refused:**
```bash
docker compose logs postgres
docker compose ps  # Check if postgres is healthy
```

**Redis cache not working:**
```bash
docker exec speckit-redis redis-cli ping
```

**Kafka broker issues:**
```bash
docker compose logs kafka
docker compose logs zookeeper
```

**Gateway routing issues:**
```bash
docker compose logs gateway
# Check YARP configuration in appsettings.json
```

**Reset all data:**
```bash
docker compose down -v
docker compose up -d
```
