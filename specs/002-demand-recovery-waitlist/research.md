# Research: Demand Recovery Waitlist

**Feature**: Waitlist with Internal Notifications  
**Date**: 2026-03-31  
**Source**: spec.md (updated with 7 User Stories)

---

## Decision 1: Storage Architecture

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Waitlist Queue** | Redis Sorted Sets + PostgreSQL | Redis for fast FIFO selection (ZPOPMAX), PostgreSQL for persistence and audit |
| **Source of Truth** | PostgreSQL | Ensures data durability and recovery capability |
| **Kafka Events** | Consume from reused `reservation-expired` topic, publish `WaitlistOpportunityGranted` | New topic for waitlist opportunity events |

**Alternatives Considered**:
- PostgreSQL-only with `SELECT FOR UPDATE SKIP LOCKED`: Higher latency than Redis
- Redis-only: Data loss risk on Redis failure

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
| **Score Composition** | `timestamp * 1000000 + (priority_modifier)` | Allows future priority boost without breaking FIFO |
| **Tie-breaker** | userId as secondary sort | Deterministic ordering |

**Alternatives Considered**:
- Pure FIFO: Too rigid for future VIP/fairness features
- Priority-only: Loses fairness principle

---

## Decision 4: Opportunity Window Timing

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Opportunity TTL** | 10 minutes | Per HU-005 acceptance criteria |
| **Reservation TTL** | 15 minutes (existing) | Per HU-007, standard reservation |
| **Expiration Handling** | Re-select next user in queue | Automatic demand reactivation |
| **State Tracking** | `offered` → `in_progress` → `used/expired` | Per HU-007 acceptance criteria |

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

| Integration Point | Approach | User Story |
|-------------------|----------|------------|
| **Inventory Service** | Extend reservation expiry worker to check waitlist | HU-004 |
| **Kafka Topics** | Consume from reused `reservation-expired`, publish to new `waitlist.opportunity-granted` | HU-005 |
| **Notification Service** | Consume `WaitlistOpportunityGranted`, send email | HU-006 |
| **Checkout Flow** | Validate opportunity token, create reservation | HU-007 |

---

## Decision 7: Email Notification vs In-App (Updated from Clarifications)

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Notification Channel** | Email (via notification service) | Per HU-006 - "Enviar Notificación por Email" |
| **Retry Logic** | Single attempt, no retries | Per clarification session |
| **User Validation** | Check active account + verified email | Per HU-006 Scenario 3 |

---

## Summary

- **No NEEDS CLARIFICATION markers** - All requirements are clear from updated spec
- **Architecture**: Hexagonal (ports/adapters), PostgreSQL + Redis, Kafka for async
- **Testing**: TDD mandatory with xUnit + Testcontainers
- **User Stories**: 7 HUs mapped to specific integration points
- **Key Flow**: Seat Release → FIFO Selection → Opportunity Granted → Email → Checkout Reservation
