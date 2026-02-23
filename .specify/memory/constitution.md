<!--
Sync Impact Report

- Version change: unspecified -> 1.1.0
- Modified principles:
	- Architecture: [PRINCIPLE_1_NAME] -> Arquitectura Hexagonal (Ports & Adapters)
	- Database: [PRINCIPLE_2_NAME] -> Base de datos: Shared PostgreSQL (schemas por bounded context)
	- Communication: [PRINCIPLE_3_NAME] -> Comunicación síncrona/asíncrona (HTTP/gRPC + Kafka)
	- Transactions/Sagas: [PRINCIPLE_4_NAME] -> Prefer local transactions; evitar sagas complejas al inicio
	- Deployment & Quality: [PRINCIPLE_5_NAME] -> Docker Compose despliegue + Testcontainers
- Added sections: standardized Development Workflow and Database constraints clarified
- Removed sections: none
- Templates requiring updates: .specify/templates/plan-template.md (⚠ pending), .specify/templates/spec-template.md (⚠ pending), .specify/templates/tasks-template.md (⚠ pending), .specify/templates/constitution-template.md (⚠ pending)
- Follow-up TODOs:
	- TODO(RATIFICATION_DATE): confirm original ratification date and replace placeholder
	- Review templates listed above and align their "Constitution Check" storage/infra guidance with the shared-Postgres decision
	- Evaluate RBAC per-schema in postgres and document recommended roles
-->

# Speckit Ticketing Constitution

## Core Principles

### Arquitectura: Hexagonal (Ports & Adapters)
La arquitectura de cada microservicio MUST seguir el patrón Hexagonal (Ports & Adapters). El dominio debe permanecer puro y aislado de detalles de infraestructura.
- Regla: Todo acceso externo (DB, mensajería, HTTP, almacenamiento) MUST realizarse a través de puertos (interfaces) e implementado por adaptadores.
- Rationale: Garantiza testabilidad del dominio, independencia tecnológica y facilidad de evolución.

### Base de datos: UNA instancia PostgreSQL compartida (shared database pattern)
Se adopta UNA sola instancia PostgreSQL compartida para el entorno de producción y desarrollo (ej: postgres:5432), con aislamiento lógico mediante schemas por bounded context (ej: catalog, inventory, ordering).
- Regla: Cada bounded context MUST usar su propio schema PostgreSQL. Los microservicios MUST acceder a la base de datos mediante repositorios definidos como puertos (IRepository) y nunca exponer SQL cru directamente fuera del adaptador de persistencia.
- Regla: En .NET usar EF Core con un `DbContext` por microservicio; un `DbContext` compartido solo si la simplicidad lo justifica y no rompe el aislamiento lógico.
- Regla: La connection string de la instancia es única en `docker-compose`; sin embargo, implantar roles/privilegios por schema es RECOMMENDED para seguridad.
- Rationale: Reduce la complejidad operativa y facilita transacciones locales; los schemas permiten separación lógica sin múltiples instancias.

### Comunicación: Síncrona y Asíncrona
- Síncrona: Usar HTTP/REST o gRPC para consultas/operaciones donde se requiere respuesta inmediata. Preferir gRPC para contratos fuertemente tipados y alto rendimiento interno.
- Asíncrona: Usar Kafka para eventos no críticos, notificaciones y patrones de eventual consistency (confirmaciones, proyecciones, integración eventual entre bounded contexts).
- Regla: Preferir diseño que permita fallos aislados y reintentos; eventos MUST ser idempotentes cuando sea posible.

### Transacciones y Orquestación (Sagas)
- Regla: No forzar sagas complejas al inicio. Favor transacciones locales ACID dentro del mismo schema/microservicio siempre que sea posible.
- Regla: Emplear sagas o coreografías solo cuando la consistencia distribuida sea necesaria y justificar su complejidad en la especificación del feature.
- Rationale: Minimizar complejidad operativa y técnica al inicio; favorecer soluciones simples y explicables.

### Despliegue: Docker Compose (entorno dev/integ)
- Regla: Despliegue base con `docker-compose` que incluya: 1 Postgres (instancia única), Redis, Kafka, y los servicios .NET del sistema.
- Rationale: Entorno reproducible y sencillo para desarrollo; producción puede usar orquestadores, pero la topología lógica (una BD compartida con schemas) se mantiene.

### Calidad y Tests
- Regla: Unit tests MUST mock puertos y casos de uso; Integration tests MUST usar Testcontainers con un solo contenedor Postgres compartido para validar integraciones reales.
- Regla: Contract tests y pruebas de integración asíncronas para flujos basados en Kafka son RECOMMENDED.
- Rationale: Balance entre velocidad de testeo (mocks) y fiabilidad (Testcontainers con Postgres real).

## Database & Security Constraints
- Naming: Schemas MUST usar prefijo del bounded context: `bc_<name>` o `schema_<bounded_context>` (consistencia obligatoria).
- Migrations: Cada microservicio mantiene sus migraciones y las aplica únicamente al schema que controla.
- Backups: La instancia PostgreSQL MUST tener backups periódicos; restauraciones y políticas operativas deben documentarse fuera de esta constitución.

## Development Workflow
- PRs MUST include: descripción del cambio, impacto en schemas (si aplica), tests unitarios y una sección "Constitution Check" que confirme cumplimiento con esta constitución.
- CI gating: El pipeline MUST ejecutar unit tests, contract tests (si existen) y al menos una suite de integración ligera contra Postgres Testcontainer antes de merge.
- Code review: Cambios de esquemas o migraciones REQUIERE al menos dos aprobaciones y un plan de despliegue/migración.

## Governance
- Enmiendas: Las modificaciones a esta constitución requieren una propuesta documentada y una revisión por el equipo técnico. Cambios menores (clarificaciones) pueden aprobarse por mayoría simple; cambios materiales (nuevo principio o redefinición) requieren consenso técnico y un plan de migración.
- Versioning policy:
	- MAJOR: Cambios incompatibles en principios fundamentales (ej. remover el patrón shared DB o cambiar la arquitectura obligatoria).
	- MINOR: Añadir un principio nuevo o expandir materialmente la guía (esta actualización → MINOR).
	- PATCH: Correcciones de redacción, typos y clarificaciones menores.
- Compliance review: Antes de merges que alteren infra o schemas, agregar una etiqueta `constitution-check` y pasar la revisión de cumplimiento.

**Version**: 1.1.0 | **Ratified**: TODO(RATIFICATION_DATE) | **Last Amended**: 2026-02-22
