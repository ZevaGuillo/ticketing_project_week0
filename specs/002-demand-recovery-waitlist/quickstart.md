# Quickstart: Waitlist Feature Implementation

**Feature**: Demand Recovery Waitlist  
**Date**: 2026-03-31  
**Source**: Updated spec.md with 7 User Stories

---

## Prerequisites

- .NET 9 SDK
- Docker + Docker Compose
- PostgreSQL (via Testcontainers for local dev)
- Redis
- Kafka

---

## Build & Test

```bash
# Restore and build
cd services/inventory
dotnet restore
dotnet build

# Run unit tests (TDD)
dotnet test --filter "Category=Unit"

# Run integration tests
dotnet test --filter "Category=Integration"
```

---

## TDD Workflow (MANDATORY)

1. **Red**: Write failing test first
   ```bash
   # Create test in tests/Inventory.Unit/
   dotnet test --filter "FullyQualifiedName~WaitlistEntryTests"
   ```

2. **Green**: Implement minimal code to pass

3. **Refactor**: Clean up, keep tests green

4. **Commit**: Only when all tests pass

---

## User Stories Implementation Order

| HU | Focus | Priority |
|----|-------|----------|
| HU-001 | Join waitlist when availability = 0 | P1 |
| HU-004 | Publish SeatReleased on reservation expiry | P1 |
| HU-005 | Process release, select FIFO, assign opportunity | P1 |
| HU-006 | Send email notification | P1 |
| HU-007 | Validate opportunity, create reservation | P1 |
| HU-002 | Show banner on event page | P2 |
| HU-003 | Cancel subscription from event page | P2 |

---

## Local Development

```bash
# Start infrastructure
docker-compose up -d postgres redis kafka

# Run API
cd services/inventory/src/Inventory.Api
dotnet run
```

---

## Key Commands

| Command | Purpose |
|---------|---------|
| `dotnet test` | Run all tests |
| `dotnet test --filter "Category=Unit"` | Unit tests only |
| `dotnet test --filter "Category=Integration"` | Integration tests |
| `dotnet ef migrations add Waitlist` | Create migration |

---

## Feature Flags

- `Waitlist:Enabled` - Enable/disable waitlist feature
- `Waitlist:OpportunityTTLSeconds` - Opportunity duration (default: 600 = 10 min)
- `Waitlist:ReservationTTLMinutes` - Reservation duration (default: 15 min)

---

## Kafka Topics

| Topic | Direction | Description |
|-------|-----------|-------------|
| `reservation-expired` | Consumer | Reservation expired (reused - existing topic) |
| `waitlist.opportunity-granted` | Producer | User granted purchase opportunity |
| `notification.email` | Producer | Email notification request |

---

## Endpoints (MVP)

| Method | Endpoint | Description | HU |
|--------|----------|-------------|-----|
| POST | `/api/waitlist/join` | Join waitlist | HU-001 |
| GET | `/api/waitlist/status` | Get subscription status | HU-002 |
| DELETE | `/api/waitlist/cancel` | Cancel subscription | HU-003 |
| GET | `/api/waitlist/availability` | Check if waitlist available | HU-001 |
| GET | `/api/waitlist/opportunity/{token}` | Validate and use opportunity | HU-007 |

---

## Implementation Checklist

- [ ] RF-001: Show waitlist option when availability = 0
- [ ] RF-002: Prevent duplicate active subscriptions
- [ ] RF-004-006: Show banner for active subscriptions
- [ ] RF-007-009: Cancel subscription with modal
- [ ] RF-010-012: Publish SeatReleased on valid transitions
- [ ] RF-013-015: FIFO selection and opportunity assignment
- [ ] RF-016-018: Email notification for active users
- [ ] RF-019-023: Opportunity validation and reservation creation
