# Research: Demand Recovery Waitlist

**Feature**: Waitlist with Internal Notifications  
**Date**: 2026-03-30  
**Source**: spec.md + CHANGELOG_SOURCES.md

---

## Decision 1: Storage Architecture

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Waitlist Queue** | Redis Sorted Sets + PostgreSQL | Redis for fast FIFO selection (ZPOPMAX), PostgreSQL for persistence and audit |
| **Source of Truth** | PostgreSQL | Ensures data durability and recovery capability |
| **Event Storage** | Kafka (reused topics) | No new topics needed; reuse `reservation-expired` |

**Alternatives Considered**:
- PostgreSQL-only with `SELECT FOR UPDATE SKIP LOCKED`: Higher latency than Redis
- Redis-only: Data loss risk on Redis failure
- New Kafka topics: Unnecessary complexity; existing topics suffice

---

## Decision 2: Concurrency Control

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **User Selection** | Lua script atomic (ZPOPMAX + ZREM) | Guarantees single worker selects user; prevents duplicates |
| **Notification Idempotency** | SHA256 idempotency key in Redis | Prevents duplicate notifications on Kafka replay |
| **Lock Strategy** | Redis distributed lock | Already exists in project (`RedisLock.AcquireLockAsync`) |

**Alternatives Considered**:
- Database advisory locks: Higher latency
- Optimistic locking: Race condition risk on high concurrency

---

## Decision 3: FIFO + Priority Model

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Default Order** | FIFO by timestamp | Simple, fair, expected by users |
| **Score Composition** | `timestamp * 1000000 + (10000 - priority * 100)` | Allows future priority boost without breaking FIFO |
| **Tie-breaker** | userId as secondary sort | Deterministic ordering |

**Alternatives Considered**:
- Pure FIFO: Too rigid for future VIP/fairness features
- Priority-only: Loses fairness principle

---

## Decision 4: Purchase Window

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Duration** | 15 minutes (existing TTL) | Already configured in reservation system |
| **Expiration Handling** | Re-select next user in queue | Automatic demand reactivation |
| **State Tracking** | `NOTIFIED_PENDING` status in DB | Enables recovery on worker failure |

---

## Decision 5: Testing Strategy (TDD)

| Test Type | Framework | Rationale |
|-----------|-----------|-----------|
| **Unit Tests** | xUnit + Moq + FluentAssertions | Fast feedback, mocks domain ports |
| **Integration Tests** | Testcontainers (PostgreSQL) | Real DB for persistence scenarios |
| **Async Tests** | Testcontainers (Kafka) | Verifies event publication/consumption |

**TDD Workflow**:
1. Write failing test first (unit or integration)
2. Implement minimal code to pass
3. Refactor while keeping tests green
4. Repeat per RF/acceptance criteria

---

## Decision 6: Integration with Existing System

| Integration Point | Approach |
|-------------------|----------|
| **Inventory Service** | Extend `CreateReservationCommandHandler` to check waitlist on reservation expiry |
| **Reservation Expiry Worker** | Reuse existing worker; add waitlist notification logic |
| **Kafka Topics** | Reuse `reservation-expired`, publish to `waitlist-notification` |
| **Notification Service** | Reuse existing internal notification infrastructure |

---

## Summary

- **No NEEDS CLARIFICATION markers** - All requirements are clear from spec and CHANGELOG_SOURCES.md
- **Architecture**: Hexagonal (ports/adapters), PostgreSQL + Redis, Kafka for async
- **Testing**: TDD mandatory with xUnit + Testcontainers
- **Existing infrastructure**: Reused wherever possible (reservation expiry, Redis locks, Kafka topics)
