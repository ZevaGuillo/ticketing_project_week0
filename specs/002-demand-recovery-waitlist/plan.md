# Implementation Plan: Demand Recovery Waitlist

**Branch**: `002-demand-recovery-waitlist` | **Date**: 2026-03-30 | **Spec**: spec.md
**Input**: Feature specification from `/specs/002-demand-recovery-waitlist/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

**Feature**: Waitlist with Internal Notifications  
**Primary Requirement**: Capture purchase intent when seats are unavailable and reactivate it when inventory is freed  
**Technical Approach**: Extend existing `bc_inventory` schema with new waitlist tables, use Redis Sorted Sets for FIFO queue, reuse Kafka `reservation-expired` event for triggering notifications

## Phase 0: Research Complete

All architectural decisions documented in `research.md`:
- Storage: PostgreSQL (persistence) + Redis (queue)
- Concurrency: Lua script for atomic user selection
- Testing: TDD with xUnit + Testcontainers
- Integration: Reuse existing reservation expiry infrastructure

## Phase 1: Design Complete

Generated artifacts:
- `data-model.md`: Entity definitions and validation rules
- `contracts/waitlist-notification.json`: Kafka event contract
- `quickstart.md`: Development guide with TDD workflow

## Constitution Check (Post-Design)

| Gate | Status | Notes |
|------|--------|-------|
| Hexagonal Architecture | ✓ PASS | Domain isolated; new ports/adapters for waitlist |
| Shared PostgreSQL | ✓ PASS | Uses `bc_inventory` schema |
| Kafka Async | ✓ PASS | Reuses `reservation-expired`, new `waitlist-notification` |
| Redis Locks/TTL | ✓ PASS | Uses existing Redis infrastructure |
| TDD Required | ✓ PASS | Unit + Integration tests required |
| Observability | ✓ PASS | Structured logs, OTel traces |

## Technical Context

**Language/Version**: .NET 9  
**Primary Dependencies**: EF Core 9+, Npgsql, Confluent.Kafka 2.x, MediatR, FluentValidation, Serilog, OpenTelemetry  
**Storage**: PostgreSQL (shared instance with schema `bc_inventory`), Redis (locks/caché)  
**Testing**: xUnit + FluentAssertions + Moq + Testcontainers (TDD REQUIRED)  
**Target Platform**: Linux server  
**Project Type**: Web service / Microservice  
**Performance Goals**: <60s latency for notification delivery, <1% duplicate notifications  
**Constraints**: FIFO ordering, idempotent events, time-limited opportunity window (15 min)  
**Scale/Scope**: MVP - single bounded context (waitlist), internal notifications only  

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Hexagonal Architecture | ✓ PASS | Domain isolated via ports/adapters |
| Shared PostgreSQL with schemas | ✓ PASS | Uses `bc_inventory` schema |
| Kafka for async events | ✓ PASS | Reuses existing topics (`reservation-expired`) |
| Redis for locks/TTL | ✓ PASS | Uses existing Redis infrastructure |
| TDD Required | ✓ PASS | Unit tests + integration tests with Testcontainers |
| Observability | ✓ PASS | Structured logs, OTel traces |

## Project Structure

### Documentation (this feature)

```text
specs/002-demand-recovery-waitlist/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
services/inventory/
├── src/
│   ├── Inventory.Domain/           # Domain layer (hexagonal core)
│   ├── Inventory.Application/      # Use cases, commands, queries
│   ├── Inventory.Infrastructure/   # Adapters (DB, Kafka, Redis)
│   └── Inventory.Api/              # Minimal API endpoints
└── tests/
    ├── Inventory.Unit/              # Unit tests (TDD - FIRST)
    ├── Inventory.Integration/       # Integration tests (Testcontainers)
    └── Inventory.Contract/          # Contract tests (if needed)
```

**Structure Decision**: Extend existing `services/inventory` bounded context with new waitlist functionality. Use existing hexagonal structure (Domain → Application → Infrastructure → API).

### TDD Workflow (MANDATORY)

1. **Red**: Write failing test first (unit test)
2. **Green**: Implement minimal code to pass test
3. **Refactor**: Clean up code while keeping tests green
4. **Repeat** for each RF/acceptance criteria

**Test Coverage Requirements**:
- Unit tests: 80%+ coverage for new code
- Integration tests: All use cases with Testcontainers
- All tests must pass before commit

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations - all Constitution gates pass. The feature extends existing infrastructure without introducing new complexity.
