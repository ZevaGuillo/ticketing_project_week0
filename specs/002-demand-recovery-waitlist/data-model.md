# Data Model: Waitlist with Internal Notifications

**Feature**: Demand Recovery Waitlist  
**Date**: 2026-03-31  
**Source**: Updated spec.md with 7 User Stories

---

## Entities

### WaitlistEntry

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | GUID | PK, Not Null | Unique identifier |
| UserId | GUID | Not Null, FK | User reference |
| EventId | GUID | Not Null, FK | Event reference |
| Section | string | Not Null | Seat section |
| Status | enum | Not Null | ACTIVE, OFFERED, EXPIRED, CONSUMED, CANCELLED |
| JoinedAt | DateTime | Not Null | Registration timestamp |
| NotifiedAt | DateTime? | Nullable | First notification timestamp |
| CreatedAt | DateTime | Not Null | Record creation |
| UpdatedAt | DateTime | Not Null | Last update |

**State Transitions**:
- ACTIVE → OFFERED: User selected from queue (HU-005)
- OFFERED → CONSUMED: User completes purchase
- OFFERED → EXPIRED: Opportunity window expires without purchase (HU-007)
- ACTIVE → CANCELLED: User cancels subscription (HU-003)
- EXPIRED → ACTIVE: User re-joins (if still unavailable)

---

### OpportunityWindow

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | GUID | PK, Not Null | Unique identifier |
| WaitlistEntryId | GUID | Not Null, FK | Reference to waitlist entry |
| Token | string | Unique | Secure token for URL access |
| SeatId | GUID | Not Null, FK | Associated seat |
| StartsAt | DateTime | Not Null | Window start (notification time) |
| ExpiresAt | DateTime | Not Null | Window expiration (10 min per HU-005) |
| Status | enum | Not Null | OFFERED, IN_PROGRESS, USED, EXPIRED |
| UsedAt | DateTime? | Nullable | When opportunity was used |

**State Transitions**:
- OFFERED → IN_PROGRESS: User accesses opportunity link (HU-007 Scenario 1)
- IN_PROGRESS → USED: User completes purchase
- OFFERED → EXPIRED: Window expires without access
- IN_PROGRESS → EXPIRED: User starts checkout but doesn't complete

---

### ReservationExpiredEvent (Kafka Consumer)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| ReservationId | GUID | Not Null | Expired reservation reference |
| EventId | GUID | Not Null | Event reference |
| SeatId | GUID | Not Null | Seat reference |
| Section | string | Not Null | Seat section |
| ExpiredAt | DateTime | Not Null | When reservation expired |
| Reason | enum | Not Null | TTL_EXPIRED, MANUAL_CANCELLATION |

---

### WaitlistOpportunityGranted (Kafka Producer)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| OpportunityId | GUID | Not Null | Reference to OpportunityWindow |
| WaitlistEntryId | GUID | Not Null | Reference to WaitlistEntry |
| UserId | GUID | Not Null | User reference |
| EventId | GUID | Not Null | Event reference |
| SeatId | GUID | Not Null | Available seat reference |
| Section | string | Not Null | Seat section |
| OpportunityTTL | int | Not Null | TTL in seconds (600 = 10 min) |
| CreatedAt | DateTime | Not Null | Event timestamp |
| IdempotencyKey | string | Unique | SHA256 hash for deduplication |

---

## Redis Structures

### Waitlist Queue (Sorted Set)

```
Key: waitlist:{eventId}:{section}
Score: timestamp * 1000000
Value: userId
```

### Idempotency Cache

```
Key: waitlist:processed:{idempotencyKey}
Value: 1
TTL: 24 hours
```

### Opportunity Token Cache

```
Key: waitlist:opportunity:{token}
Value: opportunityId
TTL: 600 seconds (10 minutes)
```

---

## Relationships

```
User 1──∞ WaitlistEntry
Event 1──∞ WaitlistEntry
Seat 1──∞ WaitlistEntry
WaitlistEntry 1──1 OpportunityWindow
Reservation 1──1 SeatReleaseEvent
```

---

## Validation Rules (from RFs)

| RF | Rule |
|----|------|
| RF-001 | Show waitlist option only when availability = 0 |
| RF-002 | Prevent duplicate active subscriptions (user + event + section) |
| RF-004 | Show "Ya estás en la lista de espera..." banner for ACTIVE status |
| RF-007 | Show confirmation modal before cancellation |
| RF-010 | Only transition "reserved → available" triggers SeatReleased event |
| RF-012 | Ensure idempotency in event emission |
| RF-013 | Select user with oldest JoinedAt (FIFO) |
| RF-015 | Publish WaitlistOpportunityGranted with TTL |
| RF-016 | Send email to user with active account + verified email |
| RF-018 | Do NOT send email to inactive users |
| RF-019 | Validate opportunity status OFFERED and not expired |
| RF-020 | Create 15-minute reservation TTL |
| RF-021 | Block access if opportunity expired |
| RF-022 | Mark opportunity as USED after purchase |
| RF-023 | Release opportunity to next user after expiration |

---

## Schema Migration

```sql
-- Add to bc_inventory schema
CREATE TYPE waitlist_status AS ENUM ('ACTIVE', 'OFFERED', 'EXPIRED', 'CONSUMED', 'CANCELLED');
CREATE TYPE opportunity_status AS ENUM ('OFFERED', 'IN_PROGRESS', 'USED', 'EXPIRED');

CREATE TABLE waitlist_entries (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    event_id UUID NOT NULL,
    section VARCHAR(100) NOT NULL,
    status waitlist_status NOT NULL DEFAULT 'ACTIVE',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notified_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uq_waitlist_user_event_section UNIQUE (user_id, event_id, section)
);

CREATE TABLE opportunity_windows (
    id UUID PRIMARY KEY,
    waitlist_entry_id UUID NOT NULL REFERENCES waitlist_entries(id),
    seat_id UUID NOT NULL,
    token VARCHAR(255) UNIQUE NOT NULL,
    starts_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    status opportunity_status NOT NULL DEFAULT 'OFFERED',
    used_at TIMESTAMPTZ
);

CREATE INDEX idx_waitlist_event_section_status ON waitlist_entries(event_id, section, status);
CREATE INDEX idx_waitlist_user_status ON waitlist_entries(user_id, status);
CREATE INDEX idx_opportunity_waitlist ON opportunity_windows(waitlist_entry_id);
CREATE INDEX idx_opportunity_token ON opportunity_windows(token) WHERE status IN ('OFFERED', 'IN_PROGRESS');
```

---

## State Machine Diagrams

### WaitlistEntry Status

```
[ACTIVE] ──(selected)──► [OFFERED] ──(purchase)──► [CONSUMED]
    │                                           
    │──(cancel)──► [CANCELLED]                   
    │                                           
    └──(expire)──► [EXPIRED] ──(rejoin)──► [ACTIVE]
```

### OpportunityWindow Status

```
[OFFERED] ──(user access)──► [IN_PROGRESS] ──(purchase)──► [USED]
    │
    └──(expire)──► [EXPIRED] ──(reassign)──► [OFFERED] (next user)
```
