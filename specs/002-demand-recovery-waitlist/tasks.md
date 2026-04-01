# Tasks: Demand Recovery Waitlist

**Feature**: Demand Recovery Waitlist  
**Branch**: `002-demand-recovery-waitlist`  
**TDD Approach**: REQUIRED (RED → GREEN → REFACTOR)

---

## Phase 1: Setup & Infrastructure

- [X] T001 Create EF Core migration for waitlist tables in services/inventory/src/Inventory.Infrastructure/Persistence/Migrations/
- [X] T002 [P] Configure Kafka consumer for reservation-expired topic in services/inventory/src/Inventory.Infrastructure/Messaging/
- [X] T002b [P] Configure Kafka DLQ topic for failed messages in services/inventory/src/Inventory.Infrastructure/Messaging/
- [X] T003 [P] Configure Kafka producer for waitlist.opportunity-granted topic in services/inventory/src/Inventory.Infrastructure/Messaging/
- [X] T004 Add Redis configuration for waitlist queue (Sorted Set) in services/inventory/src/Inventory.Infrastructure/Configuration/

---

## Phase 2: Foundational (Domain & Application Layer)

- [X] T005 Create WaitlistStatus enum in services/inventory/src/Inventory.Domain/Enums/
- [X] T006 Create OpportunityStatus enum in services/inventory/src/Inventory.Domain/Enums/
- [X] T007 Create WaitlistEntry entity in services/inventory/src/Inventory.Domain/Entities/
- [X] T008 Create OpportunityWindow entity in services/inventory/src/Inventory.Domain/Entities/
- [X] T009 [P] Create IWaitlistRepository interface in services/inventory/src/Inventory.Domain/Ports/
- [X] T010 [P] Create IWaitlistService interface in services/inventory/src/Inventory.Domain/Ports/
- [X] T011 Create ReservationExpiredEvent entity in services/inventory/src/Inventory.Domain/Events/
- [X] T012 Create WaitlistOpportunityGrantedEvent entity in services/inventory/src/Inventory.Domain/Events/

---

## Phase 3: HU-001 - Join Waitlist (P1)

**Story Goal**: User can register in waitlist when seat availability is zero

**Independent Test Criteria**: User joins waitlist → receives confirmation with position

### Tests (TDD - RED first)

- [X] T013 [US1] Write failing unit test for JoinWaitlistCommandHandler in services/inventory/tests/Inventory.Unit/Commands/
- [X] T014 [US1] Write failing unit test for GetWaitlistStatusQuery in services/inventory/tests/Inventory.Unit/Queries/

### Implementation

- [X] T015 [US1] Implement JoinWaitlistCommand in services/inventory/src/Inventory.Application/Commands/
- [X] T016 [US1] Implement JoinWaitlistCommandHandler in services/inventory/src/Inventory.Application/Handlers/
- [X] T017 [US1] Implement WaitlistRepository in services/inventory/src/Inventory.Infrastructure/Persistence/
- [X] T018 [US1] Create POST /api/waitlist/join endpoint in services/inventory/src/Inventory.Api/Endpoints/
- [X] T019 [US1] Add Redis Sorted Set integration for waitlist queue in services/inventory/src/Inventory.Infrastructure/Services/

### Component Tests

- [X] T020 [US1] Write component test for POST /api/waitlist/join endpoint in services/inventory/tests/Inventory.Integration/

---

## Phase 4: HU-004 - Publish Release Event (P1)

**Story Goal**: System publishes event when reservation expires and seat becomes available

**Independent Test Criteria**: reservation-expired event → WaitlistOpportunityGranted event published

### Tests (TDD - RED first)

- [X] T021 [US4] Write failing unit test for ReservationExpiredEventConsumer in services/inventory/tests/Inventory.Unit/Messaging/

### Implementation

- [X] T022 [US4] Implement ReservationExpiredEventConsumer in services/inventory/src/Inventory.Infrastructure/Messaging/
- [X] T022b [US4] Implement WaitlistSelectionHandler to process reservation-expired event in services/inventory/src/Inventory.Application/Handlers/
- [X] T023 [US4] Add idempotency check for event processing in services/inventory/src/Inventory.Infrastructure/Services/
- [X] T023b [US4] Configure Kafka DLQ for failed messages in services/inventory/src/Inventory.Infrastructure/Messaging/

### Integration Tests

- [X] T024 [US4] Write integration test for Kafka event consumption in services/inventory/tests/Inventory.Integration/

---

## Phase 5: HU-005 - Process Release & Assign Opportunity (P1)

**Story Goal**: System selects user (FIFO) and assigns purchase opportunity

**Independent Test Criteria**: Seat released → User selected → Opportunity created with TTL

### Tests (TDD - RED first)

- [X] T025 [US5] Write failing unit test for ProcessWaitlistSelectionService in services/inventory/tests/Inventory.Unit/Services/

### Implementation

- [X] T026 [US5] Implement WaitlistSelectionService with FIFO logic in services/inventory/src/Inventory.Infrastructure/Services/
- [X] T027 [US5] Implement Lua script for atomic user selection (ZPOPMAX + ZREM) in services/inventory/src/Inventory.Infrastructure/Services/
- [X] T027b [US5] Add distributed lock for atomic selection in services/inventory/src/Inventory.Infrastructure/Services/
- [X] T027c [US5] Add Redis idempotency cache for processed events in services/inventory/src/Inventory.Infrastructure/Services/
- [X] T028 [US5] Implement WaitlistOpportunityGrantedEvent publisher using IKafkaProducer in services/inventory/src/Inventory.Application/Handlers/
- [X] T029 [US5] Implement opportunity window creation with 10-min TTL in services/inventory/src/Inventory.Infrastructure/Persistence/

### Integration Tests

- [X] T030 [US5] Write integration test for FIFO selection in services/inventory/tests/Inventory.Integration/

---

## Phase 6: HU-006 - Email Notification (P1)

**Story Goal**: Selected user receives email notification with purchase opportunity

**Independent Test Criteria**: WaitlistOpportunityGranted event → Email sent to user

### Tests (TDD - RED first)

- [ ] T031 [US6] Write failing unit test for WaitlistNotificationHandler in services/inventory/tests/Inventory.Unit/Handlers/

### Implementation

- [ ] T032 [US6] Implement WaitlistNotificationHandler in services/inventory/src/Inventory.Application/Handlers/
- [ ] T033 [US6] Add user status validation (active account + verified email) in services/inventory/src/Inventory.Application/Handlers/
- [ ] T034 [US6] Create email composition logic in services/inventory/src/Inventory.Application/Services/
- [ ] T034b [US6] Add retry policy with exponential backoff for failed notifications in services/inventory/src/Inventory.Application/Handlers/

---

## Phase 7: HU-007 - Validate Opportunity & Create Reservation (P1)

**Story Goal**: User accesses opportunity → System validates → Creates reservation

**Independent Test Criteria**: Valid token + opportunity → 15-min reservation created

### Tests (TDD - RED first)

- [ ] T035 [US7] Write failing unit test for ValidateOpportunityCommandHandler in services/inventory/tests/Inventory.Unit/Commands/

### Implementation

- [ ] T036 [US7] Implement ValidateOpportunityCommand in services/inventory/src/Inventory.Application/Commands/
- [ ] T037 [US7] Implement ValidateOpportunityCommandHandler in services/inventory/src/Inventory.Application/Handlers/
- [ ] T038 [US7] Add opportunity status validation (OFFERED, not expired) in services/inventory/src/Inventory.Application/Handlers/
- [ ] T039 [US7] Create reservation with 15-min TTL in services/inventory/src/Inventory.Application/Services/
- [ ] T040 [US7] Create GET /api/waitlist/opportunity/{token} endpoint in services/inventory/src/Inventory.Api/Endpoints/

---

## Phase 7b: Opportunity Expiration & Re-selection (CRITICAL)

**Story Goal**: When opportunity expires → release for next user in queue

**Independent Test Criteria**: Opportunity expires → Next user selected → New opportunity created

### Implementation

- [ ] T040b [US7] Implement OpportunityExpiryWorker (BackgroundService) in services/inventory/src/Inventory.Infrastructure/Workers/
- [ ] T040c [US7] Add automatic re-selection trigger in OpportunityExpiryWorker in services/inventory/src/Inventory.Infrastructure/Workers/
- [ ] T040d [US7] Add Redis-DB consistency check after re-selection in services/inventory/src/Inventory.Infrastructure/Workers/

---

## Phase 8: HU-002 & HU-003 - UI Components (P2)

**Story Goal**: User can view waitlist status and cancel subscription

**Independent Test Criteria**: User sees banner (if active) or join button (if not)

### Implementation

- [ ] T041 [US2] Implement GetWaitlistStatusQuery in services/inventory/src/Inventory.Application/Queries/
- [ ] T042 [US2] Create GET /api/waitlist/status endpoint in services/inventory/src/Inventory.Api/Endpoints/
- [ ] T043 [US3] Implement CancelWaitlistCommand in services/inventory/src/Inventory.Application/Commands/
- [ ] T044 [US3] Implement CancelWaitlistCommandHandler in services/inventory/src/Inventory.Application/Handlers/
- [ ] T045 [US3] Create DELETE /api/waitlist/cancel endpoint in services/inventory/src/Inventory.Api/Endpoints/

---

## MVP Scope

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational)
    ↓
Phase 3 (HU-001) ←→ Phase 4 (HU-004)  ←→ Phase 6 (HU-006)
    ↓                    ↓                    ↓
Phase 5 (HU-005) ───────────────────────────────→
    ↓
Phase 7 (HU-007)
    ↓
Phase 7b (Opportunity Expiry & Re-selection)
    ↓
Phase 8 (HU-002, HU-003)
```

---

## Parallel Execution Opportunities

| Task Set | Parallel Tasks |
|----------|----------------|
| Setup | T002, T002b, T003, T004 |
| Foundational | T009, T010 |
| HU-004 parallel with HU-001 | T021-T024 with T013-T020 |
| Expiry & Re-selection | T040b, T040c, T040d |
| UI Components | T041-T045 |

---

## MVP Scope

**Recommended MVP**: Phase 3 (HU-001) + Phase 4 (HU-004) + Phase 5 (HU-005) + Phase 6 (HU-006) + Phase 7 (HU-007) + Phase 7b (Expiry Worker)

This delivers complete waitlist-to-purchase flow:
1. User joins waitlist
2. Seat releases → user selected (FIFO)
3. Email notification sent
4. User validates opportunity → reservation created
5. Opportunity expires → next user selected automatically

**Total MVP Tasks**: T001-T040d (exclude T041-T045)
