# T029: Phase 1 End-to-End Smoke Test

## Overview

Task T029 implements a comprehensive end-to-end smoke test for Phase 1 that verifies all microservices (Catalog, Inventory, Ordering) work together correctly in a realistic scenario.

**Scenario**: Reservation → Add to Cart → Checkout

## Prerequisites

Before running the smoke test, ensure:

1. **.NET 8 SDK** is installed
   ```bash
   dotnet --version  # Should be 8.0.x or higher
   ```

2. **Docker Desktop** is running (required for postgres, redis, kafka)
   ```bash
   docker ps  # Should show no errors
   ```

3. **All dependencies are available**
   ```bash
   dotnet restore speckit-ticketing.sln
   ```

## Running the Smoke Test

The smoke test is fully automated via the `phase-1-smoke-test.sh` script.

### Quick Start

```bash
# From repository root
./phase-1-smoke-test.sh
```

### What the Script Does

The smoke test executes in 4 phases:

#### Phase 1: Infrastructure Setup
- Starts Docker Compose services (postgres, redis, kafka, zookeeper)
- Waits for all infrastructure components to be healthy
- Verifies database, cache, and message broker connectivity

#### Phase 2: Build and Start Services
- Builds the complete solution in Release mode
- Starts 4 .NET microservices in background:
  - **Identity** (port 5000) - Authentication service
  - **Catalog** (port 5001) - Event/Seat catalog read-side
  - **Inventory** (port 5002) - Seat reservation coordination
  - **Ordering** (port 5003) - Shopping cart & orders
- Waits for each service to be ready (health check)

#### Phase 3: End-to-End Test Execution

The script exercises the complete purchase flow:

1. **Catalog**: GET `/events/{id}/seatmap`
   - Retrieves available seats and pricing
   - Extracts a seat ID for the next step

2. **Inventory**: POST `/reservations`
   - Reserves the selected seat (15-min TTL)
   - Extracts a reservation ID

3. **Ordering**: POST `/cart/add`
   - Adds the reserved seat to the user's shopping cart
   - Creates a draft order
   - Extracts an order ID

4. **Ordering**: POST `/orders/checkout`
   - Transitions order from draft to pending state
   - Verifies checkout completes successfully

#### Phase 4: Results Summary
- Reports total passed/failed tests
- Displays service PIDs and log file locations

### Expected Output

```
🎟️  SpecKit Ticketing - Phase 1 End-to-End Smoke Test
===========================================================

[1/4] Starting Infrastructure (docker-compose)
...
  ✓ Waiting for PostgreSQL...
  ✓ Waiting for Redis...
  ✓ Waiting for Kafka...

[2/4] Building and Starting .NET Services
...
  ✓ Starting Identity Service (port 5000)...
  ✓ Starting Catalog Service (port 5001)...
  ✓ Starting Inventory Service (port 5002)...
  ✓ Starting Ordering Service (port 5003)...

[3/4] Executing End-to-End Scenario Tests
Testing E2E Flow: Catalog → Inventory → Ordering

1. Catalog: GET /events/{id}/seatmap... ✓
2. Inventory: POST /reservations... ✓
3. Ordering: POST /cart/add... ✓
4. Ordering: POST /orders/checkout... ✓

[4/4] Test Summary
...
✓ All tests passed! Phase 1 smoke test SUCCESSFUL
```

## Troubleshooting

### Services fail to start

**Check logs:**
```bash
cat /tmp/identity.log
cat /tmp/catalog.log
cat /tmp/inventory.log
cat /tmp/ordering.log
```

**Common issues:**
- Port already in use: Kill existing processes or use different ports in `launchSettings.json`
- Database connection: Ensure `docker-compose up -d` services are healthy
- Missing migrations: Migrations should run automatically on first startup

### Infrastructure services don't start

```bash
# Check Docker Compose
cd infra
docker-compose logs

# Manually start and debug
docker-compose up
```

### Health check timeouts

Services may need more time to initialize. Adjust timeout in script:
```bash
# Change this line in phase-1-smoke-test.sh:
if wait_for_url "http://localhost:5000/health" 30; then  # 30 -> 60 for more time
```

### Port conflicts

If ports are already in use, modify `launchSettings.json` for each service:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5004"  // Change port here
    }
  }
}
```

Then update the smoke test script URLs accordingly.

## Manual Testing (Without Script)

If you prefer to run services manually:

### 1. Start Infrastructure
```bash
cd infra
docker-compose up -d
```

### 2. Start Services (in separate terminals)

**Terminal 1 - Identity:**
```bash
cd services/identity/src/Identity.Api
dotnet run
```

**Terminal 2 - Catalog:**
```bash
cd services/catalog/src/Api
dotnet run
```

**Terminal 3 - Inventory:**
```bash
cd services/inventory/src/Inventory.Api
dotnet run
```

**Terminal 4 - Ordering:**
```bash
cd services/ordering/src/Api
dotnet run
```

### 3. Execute API Calls

```bash
# 1. Get seatmap (you may need an existing event ID)
curl http://localhost:5001/events/00000000-0000-0000-0000-000000000001/seatmap

# Extract seat ID from response

# 2. Reserve a seat
curl -X POST http://localhost:5002/reservations \
  -H "Content-Type: application/json" \
  -d '{"seatId":"<SEAT_ID>","customerId":"test-user-001"}'

# Extract reservation ID from response

# 3. Add to cart
curl -X POST http://localhost:5003/cart/add \
  -H "Content-Type: application/json" \
  -d '{"reservationId":"<RESERVATION_ID>","seatId":"<SEAT_ID>","price":50.00,"userId":"test-user-001"}'

# Extract order ID from response

# 4. Checkout
curl -X POST http://localhost:5003/orders/checkout \
  -H "Content-Type: application/json" \
  -d '{"orderId":"<ORDER_ID>","userId":"test-user-001"}'
```

## Cleanup

The smoke test script automatically cleans up background services on exit. For manual cleanup:

```bash
# Stop Docker Compose services
cd infra
docker-compose down

# Kill any remaining dotnet processes
pkill -f "dotnet run" || true
```

## Dependencies

T029 successfully tests all Phase 1 completed tasks:
- ✓ T010: Infrastructure smoke test (health checks for postgres, redis, kafka)
- ✓ T013: Catalog seatmap endpoint
- ✓ T019: Inventory reservation endpoint
- ✓ T025: Ordering cart and checkout endpoints

## Next Steps (Phase 2)

After successful T029, Phase 2 focuses on:
- Payment service simulation (T030-T033)
- Fulfillment & Ticket generation (T034-T037)
- Notification service (T038-T039)
- Full end-to-end flow with payments and tickets (T040-T041)
