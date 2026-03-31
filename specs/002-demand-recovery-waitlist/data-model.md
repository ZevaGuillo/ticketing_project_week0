# Data Model: Waitlist with Internal Notifications

**Feature**: Demand Recovery Waitlist  
**Date**: 2026-03-30

---

## Entities

### WaitlistEntry

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | GUID | PK, Not Null | Unique identifier |
| UserId | GUID | Not Null, FK | User reference |
| EventId | GUID | Not Null, FK | Event reference |
| Section | string | Not Null | Seat section |
| Status | enum | Not Null | ACTIVE, EXPIRED, CONSUMED, CANCELLED |
| JoinedAt | DateTime | Not Null | Registration timestamp |
| NotifiedAt | DateTime? | Nullable | First notification timestamp |
| ExpiresAt | DateTime? | Nullable | Opportunity expiration |
| CreatedAt | DateTime | Not Null | Record creation |
| UpdatedAt | DateTime | Not Null | Last update |

**State Transitions**:
- ACTIVE → CONSUMED: User completes purchase
- ACTIVE → EXPIRED: Opportunity window expires without purchase
- ACTIVE → CANCELLED: User cancels subscription
- EXPIRED → ACTIVE: User re-joins (if still unavailable)

---

### InventoryReleaseEvent

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | GUID | PK, Not Null | Unique identifier |
| SeatId | GUID | Not Null, FK | Seat reference |
| EventId | GUID | Not Null, FK | Event reference |
| Section | string | Not Null | Seat section |
| ReleasedAt | DateTime | Not Null | Release timestamp |
| IdempotencyKey | string | Unique | SHA256(userId + eventId + seatId + timestamp) |

---

### OpportunityWindow

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | GUID | PK, Not Null | Unique identifier |
| WaitlistEntryId | GUID | Not Null, FK | Reference to waitlist entry |
| StartsAt | DateTime | Not Null | Window start (notification time) |
| ExpiresAt | DateTime | Not Null | Window expiration |
| Status | enum | Not Null | PENDING, USED, EXPIRED |
| UsedAt | DateTime? | Nullable | When opportunity was used |

---

## Redis Structures

### Waitlist Queue (Sorted Set)

```
Key: waitlist:{eventId}:{section}
Score: timestamp * 1000000 + priority_modifier
Value: userId
```

### Idempotency Cache

```
Key: notification:processed:{idempotencyKey}
Value: 1
TTL: 24 hours
```

---

## Relationships

```
User 1──∞ WaitlistEntry
Event 1──∞ WaitlistEntry
Seat 1──∞ InventoryReleaseEvent
WaitlistEntry 1──1 OpportunityWindow
```

---

## Validation Rules (from RFs)

| RF | Rule |
|----|------|
| RF-001 | Show waitlist option only when availability = 0 |
| RF-002 | Prevent duplicate active subscriptions (user + event + section) |
| RF-009 | Only transition "reserved → available" triggers release |
| RF-011 | Generate single notification per release (idempotency) |
| RF-014 | FIFO ordering by timestamp |
| RF-018 | Purchase window duration: 15 minutes |
| RF-020 | Block purchase if window expired |

---

## Schema Migration

```sql
-- Add to bc_inventory schema
CREATE TYPE waitlist_status AS ENUM ('ACTIVE', 'EXPIRED', 'CONSUMED', 'CANCELLED');
CREATE TYPE opportunity_status AS ENUM ('PENDING', 'USED', 'EXPIRED');

CREATE TABLE waitlist_entries (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    event_id UUID NOT NULL,
    section VARCHAR(100) NOT NULL,
    status waitlist_status NOT NULL DEFAULT 'ACTIVE',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notified_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uq_waitlist_user_event_section UNIQUE (user_id, event_id, section)
);

CREATE TABLE opportunity_windows (
    id UUID PRIMARY KEY,
    waitlist_entry_id UUID NOT NULL REFERENCES waitlist_entries(id),
    starts_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    status opportunity_status NOT NULL DEFAULT 'PENDING',
    used_at TIMESTAMPTZ
);

CREATE INDEX idx_waitlist_event_section_status ON waitlist_entries(event_id, section, status);
CREATE INDEX idx_waitlist_user_status ON waitlist_entries(user_id, status);
CREATE INDEX idx_opportunity_waitlist ON opportunity_windows(waitlist_entry_id);
```
