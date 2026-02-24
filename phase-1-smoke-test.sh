#!/bin/bash

# Phase 1 Smoke Test: End-to-End Reservation → Cart → Checkout
# Verifies: Infra + Catalog + Inventory + Ordering services working together

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$SCRIPT_DIR/infra"

echo "🎟️  SpecKit Ticketing - Phase 1 End-to-End Smoke Test"
echo "=========================================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Track results
FAILED=0
PASSED=0
SERVICES_STARTED=()
PID_LIST=()

# Cleanup function
cleanup() {
    echo ""
    echo -e "${YELLOW}🧹 Cleaning up background services...${NC}"
    for pid in "${PID_LIST[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null || true
        fi
    done
    echo -e "${GREEN}✓ Cleanup complete${NC}"
}

trap cleanup EXIT

# Function to wait for a URL to respond
wait_for_url() {
    local url=$1
    local max_attempts=$2
    local attempt=0
    
    while [ $attempt -lt "$max_attempts" ]; do
        if curl -s "$url" > /dev/null 2>&1; then
            return 0
        fi
        attempt=$((attempt + 1))
        sleep 1
    done
    return 1
}

# Function to wait for PostgreSQL specifically
wait_for_postgres() {
    local max_attempts=$1
    local attempt=0
    
    while [ $attempt -lt "$max_attempts" ]; do
        if docker exec speckit-postgres pg_isready -U postgres > /dev/null 2>&1; then
            return 0
        fi
        attempt=$((attempt + 1))
        sleep 1
    done
    return 1
}

# Function to check result
check_result() {
    local name=$1
    local result=$2
    
    if [ $result -eq 0 ]; then
        echo -e "${GREEN}✓ PASS${NC} - $name"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗ FAIL${NC} - $name"
        FAILED=$((FAILED + 1))
    fi
}

# ============================================================
# PHASE 1: Start Infrastructure
# ============================================================
echo -e "${BLUE}[1/4] Starting Infrastructure (docker-compose)${NC}"
echo ""

cd "$INFRA_DIR"
echo -n "Starting Docker Compose services... "
docker-compose up -d > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

# Wait for all services to be healthy
echo "Waiting for infrastructure to be ready..."
sleep 5  # Give services time to start

# Check PostgreSQL
echo -n "  - Waiting for PostgreSQL... "
if wait_for_postgres 30; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ TIMEOUT${NC}"
    FAILED=$((FAILED + 1))
fi

# Check Redis
echo -n "  - Waiting for Redis... "
if docker exec speckit-redis redis-cli ping > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
fi

# Check Kafka
echo -n "  - Waiting for Kafka... "
if docker exec speckit-kafka kafka-broker-api-versions --bootstrap-server localhost:9092 > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
fi

echo ""

# ============================================================
# PHASE 2: Build and Start .NET Services
# ============================================================
echo -e "${BLUE}[2/4] Building and Starting .NET Services${NC}"
echo ""

cd "$SCRIPT_DIR"

# Build solution
echo -n "Building solution... "
if dotnet build speckit-ticketing.sln -c Release > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
    exit 1
fi

# Start Identity Service (required for JWT tokens)
echo "Starting services in background..."
echo -n "  - Starting Identity Service (port 5000)... "
cd "$SCRIPT_DIR/services/identity/src/Identity.Api"
dotnet run --configuration Release > /tmp/identity.log 2>&1 &
IDENTITY_PID=$!
PID_LIST+=($IDENTITY_PID)
SERVICES_STARTED+=("Identity (PID: $IDENTITY_PID)")

if wait_for_url "http://localhost:5000/health" 30; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ TIMEOUT${NC}"
    FAILED=$((FAILED + 1))
fi

# Start Catalog Service
echo -n "  - Starting Catalog Service (port 5001)... "
cd "$SCRIPT_DIR/services/catalog/src/Api"
dotnet run --configuration Release > /tmp/catalog.log 2>&1 &
CATALOG_PID=$!
PID_LIST+=($CATALOG_PID)
SERVICES_STARTED+=("Catalog (PID: $CATALOG_PID)")

if wait_for_url "http://localhost:5001/health" 30; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ TIMEOUT${NC}"
    FAILED=$((FAILED + 1))
fi

# Start Inventory Service
echo -n "  - Starting Inventory Service (port 5002)... "
cd "$SCRIPT_DIR/services/inventory/src/Inventory.Api"
dotnet run --configuration Release > /tmp/inventory.log 2>&1 &
INVENTORY_PID=$!
PID_LIST+=($INVENTORY_PID)
SERVICES_STARTED+=("Inventory (PID: $INVENTORY_PID)")

if wait_for_url "http://localhost:5002/health" 30; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ TIMEOUT${NC}"
    FAILED=$((FAILED + 1))
fi

# Start Ordering Service
echo -n "  - Starting Ordering Service (port 5003)... "
cd "$SCRIPT_DIR/services/ordering/src/Api"
dotnet run --configuration Release > /tmp/ordering.log 2>&1 &
ORDERING_PID=$!
PID_LIST+=($ORDERING_PID)
SERVICES_STARTED+=("Ordering (PID: $ORDERING_PID)")

if wait_for_url "http://localhost:5003/health" 30; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗ TIMEOUT${NC}"
    FAILED=$((FAILED + 1))
fi

echo ""
sleep 5  # Allow services to fully initialize

# ============================================================
# PHASE 3: Execute E2E Tests
# ============================================================
echo -e "${BLUE}[3/4] Executing End-to-End Scenario Tests${NC}"
echo ""

# Store results
E2E_RESULT=0

# Use a default event ID (from seeding) or get it dynamically
echo "Testing E2E Flow: Catalog → Inventory → Ordering"
echo ""

# Step 1: Get event seatmap from Catalog
echo -n "1. Catalog: GET /events/{id}/seatmap... "
EVENT_RESPONSE=$(curl -s -X GET "http://localhost:5001/events/00000000-0000-0000-0000-000000000001/seatmap" \
    -H "Content-Type: application/json")

if echo "$EVENT_RESPONSE" | grep -q "seatId"; then
    echo -e "${GREEN}✓${NC}"
    PASSED=$((PASSED + 1))
    SEAT_ID=$(echo "$EVENT_RESPONSE" | grep -o '"seatId":"[^"]*' | head -1 | cut -d'"' -f4)
    echo "   Extracted seat ID: $SEAT_ID"
else
    echo -e "${RED}✗${NC}"
    FAILED=$((FAILED + 1))
    echo "   Response: $EVENT_RESPONSE"
    E2E_RESULT=1
fi

# Step 2: Reserve the seat in Inventory
if [ ! -z "$SEAT_ID" ]; then
    echo -n "2. Inventory: POST /reservations... "
    RESERVATION_RESPONSE=$(curl -s -X POST "http://localhost:5002/reservations" \
        -H "Content-Type: application/json" \
        -d "{\"seatId\":\"$SEAT_ID\",\"customerId\":\"test-customer-001\"}")
    
    if echo "$RESERVATION_RESPONSE" | grep -q "reservationId"; then
        echo -e "${GREEN}✓${NC}"
        PASSED=$((PASSED + 1))
        RESERVATION_ID=$(echo "$RESERVATION_RESPONSE" | grep -o '"reservationId":"[^"]*' | head -1 | cut -d'"' -f4)
        echo "   Extracted reservation ID: $RESERVATION_ID"
    else
        echo -e "${RED}✗${NC}"
        FAILED=$((FAILED + 1))
        echo "   Response: $RESERVATION_RESPONSE"
        E2E_RESULT=1
    fi
else
    echo -e "${YELLOW}⊘ SKIP${NC} - No seat ID from Catalog"
fi

# Step 3: Add reserved seat to cart in Ordering
if [ ! -z "$RESERVATION_ID" ]; then
    echo -n "3. Ordering: POST /cart/add... "
    CART_RESPONSE=$(curl -s -X POST "http://localhost:5003/cart/add" \
        -H "Content-Type: application/json" \
        -d "{\"reservationId\":\"$RESERVATION_ID\",\"seatId\":\"$SEAT_ID\",\"price\":50.00,\"userId\":\"test-user-001\"}")
    
    if echo "$CART_RESPONSE" | grep -q "id"; then
        echo -e "${GREEN}✓${NC}"
        PASSED=$((PASSED + 1))
        ORDER_ID=$(echo "$CART_RESPONSE" | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)
        echo "   Extracted order ID: $ORDER_ID"
    else
        echo -e "${RED}✗${NC}"
        FAILED=$((FAILED + 1))
        echo "   Response: $CART_RESPONSE"
        E2E_RESULT=1
    fi
else
    echo -e "${YELLOW}⊘ SKIP${NC} - No reservation ID from Inventory"
fi

# Step 4: Checkout order in Ordering
if [ ! -z "$ORDER_ID" ]; then
    echo -n "4. Ordering: POST /orders/checkout... "
    CHECKOUT_RESPONSE=$(curl -s -X POST "http://localhost:5003/orders/checkout" \
        -H "Content-Type: application/json" \
        -d "{\"orderId\":\"$ORDER_ID\",\"userId\":\"test-user-001\"}")
    
    if echo "$CHECKOUT_RESPONSE" | grep -q "pending\|completed"; then
        echo -e "${GREEN}✓${NC}"
        PASSED=$((PASSED + 1))
        echo "   Order status: $(echo $CHECKOUT_RESPONSE | grep -o '"status":"[^"]*' | head -1 | cut -d'"' -f4)"
    else
        echo -e "${RED}✗${NC}"
        FAILED=$((FAILED + 1))
        echo "   Response: $CHECKOUT_RESPONSE"
        E2E_RESULT=1
    fi
else
    echo -e "${YELLOW}⊘ SKIP${NC} - No order ID from Cart"
fi

echo ""

# ============================================================
# PHASE 4: Summary
# ============================================================
echo -e "${BLUE}[4/4] Test Summary${NC}"
echo ""

echo "Services started:"
for svc in "${SERVICES_STARTED[@]}"; do
    echo "  - $svc"
done
echo ""

echo "Test Results:"
echo "  ✓ Passed: $PASSED"
echo "  ✗ Failed: $FAILED"
echo ""

# Final result
TOTAL=$((PASSED + FAILED))
echo "=========================================================="

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All tests passed! Phase 1 smoke test SUCCESSFUL${NC}"
    exit 0
else
    echo -e "${RED}✗ $FAILED/$TOTAL test(s) failed - Phase 1 smoke test FAILED${NC}"
    echo ""
    echo "Logs:"
    echo "  - Identity: /tmp/identity.log"
    echo "  - Catalog: /tmp/catalog.log"
    echo "  - Inventory: /tmp/inventory.log"
    echo "  - Ordering: /tmp/ordering.log"
    exit 1
fi
