# CHANGELOG SOURCES - Sistema de Waitlist + Notificaciones

**Fecha:** 2026-03-25 | **Feature:** Waitlist + Notificaciones Event-Driven

---

## 1. Análisis del Proyecto (Contexto)

| Aspecto | Estado Actual |
|---------|--------------|
| Stack | .NET 9, Kafka, Redis, PostgreSQL |
| Arquitectura | Hexagonal + CQRS + Event-Driven |
| Punto de integración | `InventoryService.CreateReservationCommandHandler` |
| Schema disponible | `bc_inventory`, `bc_notification` |

---

## 2. Propuesta IA

### Modelo de Datos

```csharp
WaitlistEntry:
  - Id, EventId, UserId, SeatId?, UserEmail
  - JoinedAt, Priority, Status, NotifiedAt, ExpiresAt
```

### Flujo Propuesto

1. Usuario intenta reservar → Sin asientos → Agregar a waitlist
2. Asiento liberado → Notificar siguiente usuario (prioridad por tiempo)
3. Usuario tiene 15min para completar reserva o pierde turno

---

## 3. Investigación Humano

*(COMPLETADO - Se encontró redundancia con implementación existente)*

> **NOTA:** Tras investigar el codebase, varios de los puntos sugeridos en la propuesta IA **ya existen implementados** en el proyecto. Por lo tanto, la propuesta se vuelve parcialmente redundante.

| Pregunta | Respuesta |
|---------|-----------|
| ¿Patrón de waitlist recomendado? | El proyecto ya implementa **Reservation + Expiration Pattern** en `InventoryService`. El patrón de waitlist es **nuevo** (funcionalidad adicional), pero la base de reservas con TTL ya existe. |
| ¿Cómo manejar concurrencia? | **YA EXISTE:** `CreateReservationCommandHandler` usa `RedisLock.AcquireLockAsync` (línea 42) + optimistic locking con campo `ExpiresAt` y status. |
| ¿Redis o PostgreSQL para waitlist? | El proyecto usa **PostgreSQL como source of truth** + Redis para locks. Para waitlist, se podría usar Redis Sorted Sets para prioridad + PostgreSQL para persistencia. |
| ¿Nuevos topics Kafka o reutilizar existentes? | **YA EXISTEN:** `reservation-created`, `reservation-expired`, `payment-succeeded`, `payment-failed`. No need crear nuevos topics para waitlist, se pueden reutilizar o extender los existentes. |
| ¿Background job o Redis TTL? | **YA EXISTE:** `ReservationExpiryWorker` (BackgroundService) que hace poll cada 1 minuto para expirar reservas y publicar `reservation-expired`. TTL de 15 min almacenado en BD. |

---

## 4. Cuadro Comparativo

| Aspecto | Propuesta IA | Investigación Humano |
|---------|-------------|---------------------|
| Modelo de datos | Entidad `WaitlistEntry` en BD relacional | **NUEVO:** Waitlist es funcionalidad nueva (no existe actualmente). Base de reservas ya existe en `Reservation` entity. |
| Almacenamiento | PostgreSQL | **PARCIALMENTE EXISTE:** PostgreSQL ya usado para reservas. Redis ya usado para locks. Se propone híbrida (Redis Sorted Sets para waitlist + PostgreSQL). |
| Eventos Kafka | Nuevos topics | **REDUNDANTE:** Topics `reservation-created` y `reservation-expired` ya existen y funcionan. Se pueden reutilizar para waitlist. |
| Prioridad | FIFO + prioridad manual | **NUEVO:** Waitlist necesita priorización (FIFO). Se puede implementar con Redis Sorted Sets o campo `Priority` en BD. |
| Expiración | TTL en BD | **REDUNDANTE:** `ReservationExpiryWorker` ya implementa background job con poll interval de 1 min. TTL de 15 min ya existe. |
---

## 5. Decisión

| Aspecto | Decisión | Justificación |
|----------|----------|---------------|
| Modelo | Extender `Reservation` existente + nueva entidad `WaitlistEntry` | La entidad Reservation ya existe y funciona. Waitlist es funcionalidad nueva que usa la misma base. |
| Storage | PostgreSQL (reservations) + Redis (locks) - mismo patrón | Ya está implementado y funciona. Para waitlist, se puede usar el mismo enfoque. |
| Eventos | **Reutilizar** topics existentes | `reservation-created` y `reservation-expired` ya existen. No crear nuevos topics para evitar complejidad. |
| Expiración | **YA EXISTE** - No es necesario implementar | `ReservationExpiryWorker` ya hace el trabajo. |
