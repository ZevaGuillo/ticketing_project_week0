# Quickstart: Waitlist Feature Implementation

**Feature**: Demand Recovery Waitlist  
**Date**: 2026-03-30

---

## Prerequisites

- .NET 9 SDK
- Docker + Docker Compose
- PostgreSQL (via Testcontainers for local dev)
- Redis

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

## TDD Workflow

1. **Red**: Write failing test first
   ```bash
   # Create test in tests/Inventory.Unit/
   dotnet test --filter "FullyQualifiedName~WaitlistEntryTests"
   ```

2. **Green**: Implement minimal code to pass

3. **Refactor**: Clean up, keep tests green

4. **Commit**: Only when all tests pass

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
| `dotnet ef migrations add` | Create migration |

---

## Feature Flags

- `Waitlist:Enabled` - Enable/disable waitlist feature
- `Waitlist:NotificationEnabled` - Enable notifications
- `Waitlist:PurchaseWindowMinutes` - Opportunity duration (default: 15)

---

## Endpoints (MVP)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/waitlist/join` | Join waitlist |
| GET | `/api/waitlist/status` | Get subscription status |
| DELETE | `/api/waitlist/cancel` | Cancel subscription |
| GET | `/api/waitlist/availability` | Check if waitlist available |

---

## Next Steps

1. Implement RF-001 (show waitlist option when availability = 0)
2. Write unit tests for JoinWaitlistCommand
3. Write integration tests for waitlist entry persistence
