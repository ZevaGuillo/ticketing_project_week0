# Implementation Plan: Demand Recovery Waitlist

**Branch**: `002-demand-recovery-waitlist` | **Date**: 2026-03-31 | **Spec**: spec.md
**Input**: Feature specification from `/specs/002-demand-recovery-waitlist/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

**Feature**: Waitlist with Internal Notifications  
**Primary Requirement**: Capture purchase intent when seats are unavailable and reactivate it when inventory is freed, via email notifications and checkout reservation flow  
**Technical Approach**: Extend existing `bc_inventory` schema with new waitlist tables, use Redis Sorted Sets for FIFO queue, consume `reservation-expired` Kafka events (reused topic), publish `WaitlistOpportunityGranted` events, integrate with notification service for email

## Technical Context

**Language/Version**: .NET 9  
**Primary Dependencies**: EF Core 9+, Npgsql, Confluent.Kafka 2.x, MediatR, FluentValidation, Serilog, OpenTelemetry  
**Storage**: PostgreSQL (shared instance with schema `bc_inventory`), Redis (locks/caché)  
**Testing**: xUnit + FluentAssertions + Moq + Testcontainers (TDD REQUIRED)  
**Target Platform**: Linux server  
**Project Type**: Web service / Microservice  
**Performance Goals**: <60s latency for notification delivery, <1% duplicate notifications  
**Constraints**: FIFO ordering, idempotent events, time-limited opportunity window (10 min opportunity TTL, 15 min reservation TTL)  
**Kafka Integration**: Consume from reused `reservation-expired` topic, publish to new `waitlist.opportunity-granted` topic  
**Scale/Scope**: MVP - single bounded context (waitlist), email notifications via notification service

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Hexagonal Architecture | ✓ PASS | Domain isolated via ports/adapters |
| Shared PostgreSQL with schemas | ✓ PASS | Uses `bc_inventory` schema |
| Kafka for async events | ✓ PASS | Consumes `reservation-expired` (reused), publishes `WaitlistOpportunityGranted` |
| Redis for locks/TTL | ✓ PASS | Uses existing Redis infrastructure |
| TDD Required | ✓ PASS | Unit tests + integration tests with Testcontainers |
| Observability | ✓ PASS | Structured logs, OTel traces |

## User Stories (Updated)

| Epic | HU | Description | Priority |
|------|-----|-------------|----------|
| EPIC-001 | HU-001 | Registro de Usuario en Lista de Espera para Eventos Agotados | P1 |
| EPIC-001 | HU-002 | Visualizar Suscripción Activa en Lista de Espera en la Página del Evento | P2 |
| EPIC-001 | HU-003 | Cancelar Suscripción Activa desde la Página del Evento | P2 |
| EPIC-002 | HU-004 | Publicar Evento de Liberación de Asiento por Expiración de Reserva | P1 |
| EPIC-003 | HU-005 | Procesar Liberación de Asiento y Asignar Oportunidad al Siguiente en la Cola | P1 |
| EPIC-003 | HU-006 | Enviar Notificación por Email de Oportunidad de Compra | P1 |
| EPIC-003 | HU-007 | Validar Oportunidad de Compra y Crear Reserva para Checkout | P1 |

## Project Structure

### Documentation (this feature)

```text
specs/002-demand-recovery-waitlist/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── waitlist-opportunity-granted.json
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
services/
├── inventory/
│   ├── src/
│   │   ├── Inventory.Domain/           # Domain layer (hexagonal core)
│   │   │   ├── Entities/                # WaitlistEntry, OpportunityWindow
│   │   │   ├── Enums/                   # WaitlistStatus, OpportunityStatus
│   │   │   └── Events/                  # Domain events
│   │   ├── Inventory.Application/       # Use cases, commands, queries
│   │   │   ├── Commands/                # JoinWaitlist, CancelWaitlist
│   │   │   ├── Queries/                 # GetWaitlistStatus
│   │   │   └── Handlers/                # Command/Query handlers
│   │   ├── Inventory.Infrastructure/   # Adapters (DB, Kafka, Redis)
│   │   │   ├── Persistence/             # EF Core, Repositories
│   │   │   ├── Messaging/               # Kafka consumers/producers
│   │   │   └── Services/                # WaitlistService
│   │   └── Inventory.Api/              # Minimal API endpoints
│   │       └── Endpoints/               # Waitlist endpoints
│   └── tests/
│       ├── Inventory.Unit/              # Unit tests (TDD - FIRST)
│       └── Inventory.Integration/       # Integration tests (Testcontainers)
└── notification/
    └── [existing - will consume WaitlistOpportunityGranted]
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

## Phase 0: Research Complete

All architectural decisions documented in `research.md`:
- Storage: PostgreSQL (persistence) + Redis (queue)
- Concurrency: Lua script for atomic user selection
- Testing: TDD with xUnit + Testcontainers
- Integration: Consume from reused `reservation-expired` Kafka topic, publish `WaitlistOpportunityGranted`

## Phase 1: Design Complete

Generated artifacts:
- `data-model.md`: Entity definitions and validation rules
- `contracts/waitlist-opportunity-granted.json`: Kafka event contract
- `quickstart.md`: Development guide with TDD workflow

## Constitution Check (Post-Design)

| Gate | Status | Notes |
|------|--------|-------|
| Hexagonal Architecture | ✓ PASS | Domain isolated; new ports/adapters for waitlist |
| Shared PostgreSQL | ✓ PASS | Uses `bc_inventory` schema |
| Kafka Async | ✓ PASS | Consumes `reservation-expired` (reused), publishes `WaitlistOpportunityGranted` |
| Redis Locks/TTL | ✓ PASS | Uses existing Redis infrastructure |
| TDD Required | ✓ PASS | Unit + Integration tests required |
| Observability | ✓ PASS | Structured logs, OTel traces |

## Complexity Tracking

No violations - all Constitution gates pass. The feature extends existing infrastructure without introducing new complexity.
