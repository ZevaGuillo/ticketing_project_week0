---
description: "Task list for feature: Autenticación Real de Usuarios + API Gateway"
---

# Tasks: Autenticación Real de Usuarios + API Gateway

**Input**: specs/001-user-auth-real/spec.md

El plan sigue ciclos TDD (Red → Green → Refactor) exclusivamente en backend (.NET). No se agregan pruebas en el frontend. Se incluye la creación del API Gateway como punto de entrada único y la refactorización de arquitectura para que todos los servicios queden detrás de él.

## Arquitectura objetivo

```
Frontend (localhost:3000)
        │
        ▼
 API Gateway (:5000)          ← único punto de entrada expuesto
  ├─ JWT validation            ← verifica token antes de enrutar
  ├─ Header injection          ← propaga X-User-Id, X-User-Role
  └─ YARP reverse proxy
        │
        ├─ /auth/*      → identity   (:5000 interno, rutas públicas)
        ├─ /catalog/*   → catalog    (:5001 interno, público)
        ├─ /inventory/* → inventory  (:5002 interno, protegido)
        ├─ /ordering/*  → ordering   (:5003 interno, protegido)
        ├─ /payment/*   → payment    (:5004 interno, protegido)
        └─ /fulfillment/* → fulfillment (:5005 interno, protegido)
```

---

## Phase 0 · Test Harness & Prereqs

- [X] T000 Actualizar plantillas xUnit/NSubstitute en `services/identity/tests/unit` y `tests/integration`.
- [X] T001 Crear fixtures reutilizables (`IdentityUserBuilder`, `TokenFactory`) en `services/identity/tests/Common/` para generar usuarios y JWT válidos/expirados.
- [X] T002 Actualizar script `services/run-auth-tests.sh` con orden: Identity → Inventory → Ordering → Gateway.

---

## Phase 1 · Identity Service — Red → Green → Refactor

### Red
- [X] T010 Añadir pruebas unitarias fallidas para `CreateUserHandler` cubriendo email único, contraseña mínima y rol por defecto (`Identity.Application.UnitTests`).
- [X] T011 Añadir pruebas unitarias fallidas para `IssueTokenHandler` cubriendo credenciales inválidas, usuario inexistente y expiración configurable.
- [X] T012 Crear pruebas de integración fallidas para `POST /auth/register` y `POST /auth/token` con `WebApplicationFactory` (`Identity.IntegrationTests`).

### Green
- [X] T013 Implementar validaciones de dominio en `Identity.Domain/User` y `CreateUserCommandHandler` hasta pasar T010.
- [X] T014 Ajustar `IssueTokenHandler` para BCrypt, claims (`sub`, `role`, `email`) y TTL dinámico hasta pasar T011.
- [X] T015 Actualizar `Identity.Api` con rutas `/auth/register` y `/auth/token`, mapeo de errores con `ProblemDetails` hasta pasar T012.

### Refactor
- [X] T016 Extraer `PasswordService` y `TokenService` como servicios de dominio; eliminar lógica duplicada de `Program.cs`. Todas las suites deben permanecer verdes.

---

## Phase 2 · API Gateway — Red → Green → Refactor

Esta phase crea el proyecto gateway desde cero como punto de entrada único.

### Red
- [X] T020 Crear proyecto `services/gateway/src/Gateway.Api/Gateway.Api.csproj` (.NET 9 Minimal API + YARP).
- [X] T021 Crear proyecto de tests `services/gateway/tests/integration/Gateway.IntegrationTests/Gateway.IntegrationTests.csproj`.
- [X] T022 Escribir prueba de integración fallida: `GET /catalog/events` sin token → 200 (ruta pública).
- [X] T023 Escribir prueba de integración fallida: `POST /inventory/reservations` sin token → 401.
- [X] T024 Escribir prueba de integración fallida: `POST /inventory/reservations` con JWT válido → 200 + header `X-User-Id` propagado downstream.
- [X] T025 Escribir prueba de integración fallida: JWT expirado → 401 con `ProblemDetails`.
- [X] T026 Escribir prueba de integración fallida: rol `User` intentando ruta admin `/admin/*` → 403.

### Green
- [X] T027 Instalar dependencias: `Yarp.ReverseProxy`, `Microsoft.AspNetCore.Authentication.JwtBearer` en `Gateway.Api.csproj`.
- [X] T028 Implementar `appsettings.json` del gateway con tabla de rutas YARP:
  - Rutas públicas: `/auth/{**catch-all}` → identity, `/catalog/{**catch-all}` → catalog.
  - Rutas protegidas (`RequireAuthorization`): `/inventory/`, `/ordering/`, `/payment/`, `/fulfillment/`.
- [X] T029 Configurar middleware JWT en gateway usando la misma `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` que Identity hasta pasar T022-T025.
- [X] T030 Implementar `ClaimsForwardingTransform`: extraer `sub` y `role` del token y añadir headers `X-User-Id` / `X-User-Role` a cada petición downstream hasta pasar T024.
- [X] T031 Añadir policy `AdminOnly` y proteger rutas `/admin/` hasta pasar T026.

### Refactor
- [X] T032 Extraer `ClaimsForwardingTransform` a clase propia en `Gateway.Api/Transforms/`. Mantener suites verdes.
- [X] T033 Centralizar constantes de claims y nombres de headers en `Gateway.Api/Constants/GatewayHeaders.cs`.

---

## Phase 3 · Inventory Service — Red → Green → Refactor

### Red
- [ ] T040 Escribir pruebas unitarias fallidas para `CreateReservationCommandHandler` exigiendo `userId` no vacío (`Inventory.UnitTests`).
- [ ] T041 Crear pruebas de integración fallidas: `POST /reservations` recibe header `X-User-Id` y persiste el `userId` en `Reservation` (`Inventory.IntegrationTests`).
- [ ] T042 Definir prueba de contrato Kafka fallida verificando que el evento `reservation-created` incluya campo `userId`.

### Green
- [ ] T043 Actualizar `CreateReservationCommandHandler` para leer `userId` del header `X-User-Id` y pasar T040/T041.
- [ ] T044 Añadir `userId` a la entidad `Reservation` + migración EF Core y pasar T041/T042.
- [ ] T045 Eliminar autenticación JWT directa de `Inventory.Api` (ahora la valida el gateway); el servicio solo lee headers de confianza.

### Refactor
- [ ] T046 Extraer `UserContextMiddleware` que lea `X-User-Id`/`X-User-Role` y los exponga via `IUserContext` en toda la aplicación.

---

## Phase 4 · Ordering Service — Red → Green → Refactor

### Red
- [ ] T050 Añadir pruebas unitarias fallidas para `CreateDraftOrderCommandHandler` que rechacen `userId` vacío (`Ordering.Application.UnitTests`).
- [ ] T051 Crear pruebas de integración fallidas: `POST /orders` sin header `X-User-Id` → 400 (`Ordering.IntegrationTests`).
- [ ] T052 Crear prueba de consumidor Kafka fallida verificando que al consumir `reservation-created` se mapee `userId` a la orden.

### Green
- [ ] T053 Ajustar `Ordering.Domain/Order` y handlers para exigir `userId` hasta pasar T050.
- [ ] T054 Leer `X-User-Id` desde headers en `Ordering.Api` y pasarlo al command hasta pasar T051.
- [ ] T055 Mapear `userId` desde evento `reservation-created` a la orden draft hasta pasar T052.
- [ ] T056 Eliminar autenticación JWT directa de `Ordering.Api`; validación queda en gateway.

### Refactor
- [ ] T057 Compartir `UserContextMiddleware` e `IUserContext` entre Inventory y Ordering extrayéndolos a `services/shared/UserContext/`.

---

## Phase 5 · Refactorización de Infraestructura

Tareas de plomería para que el gateway sea el único punto expuesto.

- [ ] T060 Actualizar `infra/docker-compose.yml`:
  - Agregar servicio `gateway` (puerto `5000:5000`, imagen build desde `services/gateway`).
  - Cambiar todos los servicios internos de ports expuestos a solo red interna (quitar mapeos de host para identity, catalog, inventory, ordering, payment, fulfillment, notification).
  - Agregar variable de entorno `ASPNETCORE_URLS` y nombres DNS Docker en config YARP del gateway.
- [ ] T061 Crear `services/gateway/src/Gateway.Api/Dockerfile` siguiendo el mismo patrón que los servicios existentes.
- [ ] T062 Añadir `Gateway.Api` a la solución `speckit-ticketing.sln`.
- [ ] T063 Actualizar `infra/README.md` con el nuevo diagrama de red y lista de puertos.

---

## Phase 6 · Observabilidad & Edge Cases Backend

### Red
- [ ] T070 Añadir pruebas unitarias verificando logging de intentos fallidos de autenticación en Identity (`TestLogger`).
- [ ] T071 Añadir prueba de integración en Gateway: token con `exp` pasado → 401 con mensaje instructivo.
- [ ] T072 Añadir prueba de integración en Gateway: token con firma inválida → 401.

### Green
- [ ] T073 Implementar logging estructurado (Serilog) + contador de fallos en `Identity.Application` hasta pasar T070.
- [ ] T074 Afinar validación JWT en Gateway middleware para pasar T071/T072 con `ProblemDetails` normalizados.

### Refactor
- [ ] T075 Centralizar `appsettings` de JWT, Serilog y OpenTelemetry compartidos en `services/shared/Config/`.

---

## Dependencies & Execution Order

```
Phase 0  →  Phase 1 (Identity)
                  ↓
            Phase 2 (Gateway)  ←  requiere Identity para emitir tokens de prueba
                  ↓
       Phase 3 (Inventory) + Phase 4 (Ordering)  [pueden ir en paralelo]
                  ↓
            Phase 5 (Infra refactor)
                  ↓
            Phase 6 (Observabilidad)
```

- No avanzar a Green sin tener los tests en Red confirmados (`dotnet test` falla).
- Inventory y Ordering deben **eliminar** su propia validación JWT; la responsabilidad queda exclusivamente en el gateway.
- El frontend solo necesita conocer el endpoint del gateway (`http://localhost:5000`); cambiar la variable de entorno `NEXT_PUBLIC_API_URL`.
