# Plan de Pruebas — Lista de Espera con Notificaciones Internas
**Ticketing Platform MVP**

| Campo | Detalle |
|---|---|
| Versión | 1.0 |
| Fecha | Julio 2025 |
| Estado | En revisión |
| Proyecto | Ticketing Platform MVP |
| Área | QA / Aseguramiento de Calidad |

---

## 1. Introducción

### 1.1 Propósito del Documento

Este documento define la estrategia, alcance, recursos y criterios de calidad para la validación de la feature de Lista de Espera con Notificaciones Internas dentro de la Ticketing Platform MVP. Su objetivo es guiar al equipo de QA en la planificación y ejecución de las actividades de prueba, asegurando la cobertura funcional y técnica de los requerimientos especificados.

### 1.2 Contexto del Proyecto

La Ticketing Platform MVP es una plataforma de venta de boletos basada en arquitectura de microservicios (.NET 8, Next.js, PostgreSQL, Redis, Kafka). La feature bajo prueba introduce la capacidad de capturar la intención de compra de usuarios cuando no existe disponibilidad de asientos, gestionando su reactivación de forma automática cuando el inventario se libera.

### 1.3 Documentos de Referencia

- Especificación de Requisitos — Ticketing: Lista de espera con notificaciones internas (v1.0)
- Contexto de Negocio — Ticketing Platform MVP
- Diagramas de secuencia: Usuario se une a waitlist / Selección de usuario cuando se libera asiento
- Diagrama ER del sistema

---

## 2. Alcance de las Pruebas

### 2.1 Funcionalidades en Alcance

| Épica | Historias de Usuario | En Alcance | Prioridad |
|---|---|:---:|:---:|
| EPIC-001 — Gestión de Waitlist | HU-001: Registro en lista de espera / HU-002: Visualizar suscripción activa / HU-003: Cancelar suscripción | Sí | Alta |
| EPIC-002 — Detección de Liberación | HU-004: Publicar evento de liberación de asiento por expiración de reserva | Sí | Alta |
| EPIC-003 — Notificación y Reactivación | HU-005: Procesar liberación y asignar oportunidad (FIFO) / HU-006: Enviar notificación por email / HU-007: Validar oportunidad y crear reserva para checkout | Sí | Alta |

### 2.2 Funcionalidades Fuera de Alcance

- Priorización avanzada de usuarios (más allá de FIFO por timestamp)
- Integración con canales externos de notificación distintos al email (push, SMS, etc.)
- Panel de administración de lista de espera
- Analítica y personalización avanzada
- Pruebas de los flujos existentes no modificados: catálogo, pago, generación de PDF/QR

### 2.3 Tipos de Prueba Incluidos

- Pruebas Funcionales (caja negra, basadas en criterios de aceptación)
- Pruebas de Integración entre microservicios (Waitlist, Inventory, Notification vía Kafka)
- Pruebas de Reglas de Negocio
- Pruebas de Flujo End-to-End (E2E)
- Pruebas No Funcionales: rendimiento, concurrencia, disponibilidad
- Pruebas de Seguridad: autenticación y autorización

---

## 3. Objetivos de Calidad

| Criterio | Descripción |
|---|---|
| Cobertura funcional | Validar el 100% de los criterios de aceptación definidos en las 7 historias de usuario. |
| Integridad de datos | Garantizar que no se generen registros duplicados en la waitlist por usuario y contexto. |
| Consistencia FIFO | Verificar que la selección de usuarios respeta el orden de registro (timestamp ascendente). |
| Atomicidad en selección | Asegurar que un asiento liberado sea asignado a un único usuario incluso bajo concurrencia. |
| Tiempo de respuesta | Cumplir RNF-001: emisión de evento de liberación en ≤ 500ms (p95) bajo carga normal. |
| Disponibilidad | Cumplir RNF-004: disponibilidad mínima de 99.5% para los servicios de waitlist y notificación. |
| Seguridad de acceso | Confirmar que solo usuarios autenticados pueden operar sobre la lista de espera (RNF-007). |
| Trazabilidad | Verificar que se registren eventos de auditoría para registro, cancelación, selección y notificación (RNF-009). |

---

## 4. Estrategia de Pruebas

### 4.1 Enfoque General

La estrategia sigue un modelo en capas, comenzando por la validación unitaria de cada servicio, escalando a pruebas de integración entre servicios y culminando con flujos E2E completos. Se priorizan los caminos críticos del negocio antes de los flujos alternativos.

### 4.2 Niveles de Prueba

**Nivel 1 — Pruebas Funcionales por Historia de Usuario**

Se ejecutan casos de prueba derivados de cada escenario Given/When/Then definido en los criterios de aceptación. Cubren caminos felices, caminos alternativos y escenarios de error para cada HU.

**Nivel 2 — Pruebas de Integración**

Se valida la comunicación entre microservicios a través de Kafka: la publicación del evento `SeatReleased` por parte del Inventory Service y su consumo por el Waitlist Service, así como la publicación de `WaitlistOpportunityGranted` y su consumo por el Notification Service.

**Nivel 3 — Pruebas End-to-End (E2E)**

Se simulan flujos completos desde la perspectiva del usuario: (a) usuario se registra en waitlist, la reserva de otro usuario expira, el sistema le notifica y el usuario completa la compra; (b) el tiempo de la oportunidad expira y el sistema reasigna al siguiente usuario en cola.

**Nivel 4 — Pruebas No Funcionales**

Se ejecutan pruebas de carga para validar RNF-001 y RNF-002. Se realizan pruebas de disponibilidad y reintentos automáticos para RNF-004 y RNF-005.

### 4.3 Técnicas de Prueba

- Partición de equivalencia y análisis de valores límite para estados de suscripción (`activa`, `expirada`, `consumida`, `cancelada`).
- Tablas de decisión para las transiciones de estado de asiento (`disponible → reservado → vendido / disponible`).
- Casos de prueba basados en escenarios de concurrencia (múltiples consumidores del mismo evento Kafka).
- Pruebas de mutación de estado para verificar idempotencia del `idempotency_key` en `WAITLIST_ENTRIES`.

---

## 5. Matriz de Cobertura de Requerimientos

| Req. / Historia | Descripción | Tipo de Prueba | Responsable | Prioridad |
|---|---|---|---|:---:|
| HU-001 / RF-001–003 | Registro en lista de espera (validación disponibilidad, duplicado, confirmación) | Funcional + UI | QA | Alta |
| HU-002 / RF-004–006 | Visualización del estado de suscripción activa en la página del evento | Funcional + UI | QA | Alta |
| HU-003 / RF-007–008 | Cancelación de suscripción con modal de confirmación | Funcional + UI | QA | Media |
| HU-004 / RF-009–010 | Detección y publicación del evento `SeatReleased` (solo transición reservado → disponible) | Integración | QA + Dev | Alta |
| HU-005 / RF-013–015 | Selección FIFO del usuario en lista de espera al consumir `SeatReleased` | Integración + Concurrencia | QA + Dev | Alta |
| HU-006 / RF-016–017 | Envío de email al usuario seleccionado; omisión en usuarios inactivos | Integración + E2E | QA | Alta |
| HU-007 / RF-018–022 | Gestión de ventana de oportunidad: uso, expiración, bloqueo y reasignación | Funcional + E2E | QA | Alta |
| RNF-001 | Latencia de emisión de eventos ≤ 500ms p95 bajo 1000 eventos/seg | Rendimiento | QA + DevOps | Alta |
| RNF-002 | Soporte de 1000 transacciones concurrentes con integridad de la waitlist | Carga / Concurrencia | QA + DevOps | Alta |
| RNF-004 | Disponibilidad 99.5% de servicios waitlist y notificación | Disponibilidad | DevOps | Media |
| RNF-005 | Reintentos automáticos en ≤ 60 segundos ante fallo de entrega | Resiliencia | QA + Dev | Media |
| RNF-007–008 | Solo usuarios autenticados pueden operar sobre la waitlist | Seguridad | QA | Alta |
| RNF-009 | Eventos de auditoría registrados para todas las operaciones de waitlist | Funcional | QA + Dev | Media |

---

## 6. Flujos End-to-End Prioritarios

### Flujo E2E-01 — Conversión Exitosa desde Lista de Espera

| Campo | Detalle |
|---|---|
| Precondición | Evento A, Sección VIP: 0 asientos disponibles, 1 asiento con reserva activa (expira en T). |
| Actores | UsuarioA (en waitlist), UsuarioB (con reserva activa que expirará). |
| Pasos | 1. UsuarioA se registra en waitlist para Evento A / Sección VIP. 2. La reserva de UsuarioB expira (TTL alcanzado). 3. Inventory Service publica `SeatReleased` en Kafka. 4. Waitlist Service consume el evento y selecciona a UsuarioA (FIFO). 5. Waitlist Service publica `WaitlistOpportunityGranted`. 6. Notification Service envía email a UsuarioA con enlace y token. 7. UsuarioA hace clic en el enlace dentro del TTL de la oportunidad. 8. Sistema valida el token, crea reserva temporal (15 min) y redirige al checkout. 9. UsuarioA completa el pago exitosamente. 10. Sistema emite boleto (QR) y confirma la orden. |
| Resultado Esperado | La orden queda en estado `paid`. El ticket es generado. La suscripción de UsuarioA queda en estado `consumed`. |

### Flujo E2E-02 — Expiración de Oportunidad y Reasignación

| Campo | Detalle |
|---|---|
| Precondición | Evento A, Sección VIP: UsuarioA y UsuarioB en waitlist (en ese orden). Asiento con reserva activa. |
| Pasos | 1. Reserva expira. Sistema selecciona a UsuarioA y publica `WaitlistOpportunityGranted`. 2. UsuarioA recibe email pero NO hace clic dentro del TTL de la oportunidad. 3. Sistema detecta expiración de la oportunidad de UsuarioA. 4. Sistema libera la oportunidad y la reasigna a UsuarioB (siguiente en FIFO). 5. UsuarioB recibe su notificación y completa la compra. |
| Resultado Esperado | La suscripción de UsuarioA queda como `expirada`. La de UsuarioB queda como `consumed`. La orden es completada por UsuarioB. |

### Flujo E2E-03 — Concurrencia (Doble Consumo del Mismo Evento Kafka)

| Campo | Detalle |
|---|---|
| Escenario | Dos instancias del Waitlist Service consumen simultáneamente el mismo evento `SeatReleased`. |
| Resultado Esperado | Solo una instancia logra procesar el evento (validado mediante `idempotency_key` y bloqueos Redis). El asiento se asigna a un único usuario. No se generan notificaciones duplicadas. |

---

## 7. Criterios de Entrada y Salida

### 7.1 Criterios de Entrada (inicio de pruebas)

- El ambiente de pruebas está desplegado y operativo (Docker Compose con todos los servicios levantados).
- Los servicios de Kafka, Redis y PostgreSQL están accesibles y configurados con los esquemas `bc_` correspondientes.
- Los endpoints de la Waitlist API están documentados y responden correctamente en ambiente de pruebas.
- Los datos de prueba base (eventos, secciones, usuarios de prueba) están cargados en la base de datos.
- El equipo de desarrollo ha completado el code review y la integración inicial de la feature en el branch de pruebas.

### 7.2 Criterios de Salida (cierre de pruebas)

- El 100% de los casos de prueba de prioridad Alta han sido ejecutados.
- Al menos el 90% de los casos de prueba de prioridad Media han sido ejecutados.
- 0 defectos críticos abiertos (que bloqueen el flujo principal de negocio).
- Todos los requerimientos no funcionales prioritarios (RNF-001, RNF-002, RNF-007) han sido validados.
- El reporte final de pruebas ha sido revisado y aprobado por el responsable de QA y el Product Owner.

---

## 8. Ambiente de Pruebas y Datos

### 8.1 Configuración del Ambiente

| Componente | Descripción / Versión | Responsable |
|---|---|---|
| Orquestación | Docker Compose (desarrollo e infraestructura) | DevOps / Dev |
| Frontend | Next.js (Tailwind, Shadcn UI) — ambiente local o staging | Dev |
| Backend | .NET 8 — Arquitectura Hexagonal — todos los microservicios | Dev |
| Base de datos | PostgreSQL con esquemas `bc_inventory`, `bc_waitlist`, `bc_notification`, `bc_identity` | Dev / DBA |
| Caché / Bloqueos | Redis (Sorted Sets para FIFO, Distributed Locks para concurrencia) | Dev |
| Mensajería | Apache Kafka — topics: `inventory-events`, `waitlist-notification` | Dev |
| Email (simulado) | SMTP local (Mailhog o equivalente) para capturar emails sin envío real | QA |
| Monitoreo | Logs estructurados accesibles para verificar auditoría (RNF-009) | Dev / QA |

### 8.2 Datos de Prueba Requeridos

- **Usuarios de prueba:** Mínimo 5 cuentas activas con email verificado, 1 cuenta inactiva/suspendida.
- **Eventos:** Al menos 2 eventos con diferentes configuraciones de secciones (VIP, General).
- **Estado de inventario:** Scripts para forzar asientos en estado `reservado` con TTL configurable para simular expiración controlada.
- **Waitlist preexistente:** Scripts para insertar registros directamente en `WAITLIST_ENTRIES` con timestamps específicos para validar orden FIFO.
- **Tokens de oportunidad:** Mecanismo para generar y manipular tokens con TTL vencido (para pruebas de expiración de oportunidad).

---

## 9. Riesgos y Mitigaciones

| ID | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|:---:|:---:|---|
| R-01 | Dificultad para reproducir condiciones de concurrencia en ambiente local | Alta | Alto | Usar scripts de carga (k6 o Artillery) para simular múltiples productores/consumidores simultáneos de Kafka. |
| R-02 | TTLs de Redis y reservas son difíciles de controlar en tiempo real durante pruebas | Alta | Alto | Implementar endpoints de utilidad en ambiente de pruebas para forzar expiración instantánea o configurar TTLs cortos. |
| R-03 | Kafka puede introducir latencia variable que afecte las pruebas de tiempo | Media | Alto | Definir tolerancias en los SLAs de prueba y ejecutar pruebas de latencia en condiciones de carga controlada. |
| R-04 | La idempotencia del `idempotency_key` puede no cubrir todos los escenarios de duplicación | Media | Alto | Diseñar casos de prueba específicos con múltiples consumidores del mismo evento y verificar en BD. |
| R-05 | Cambios de último momento en los criterios de aceptación durante la ejecución | Media | Medio | Gestionar cambios mediante el Product Owner con actualización formal del plan antes de re-ejecutar. |
| R-06 | Ambiente de pruebas inestable o no disponible | Baja | Alto | Coordinar con DevOps un ambiente dedicado. Definir un checklist de salud del ambiente previo a cada sesión. |

---

## 10. Cronograma Estimado

Las estimaciones asumen dedicación parcial del equipo de QA (50%) y disponibilidad del ambiente desde el inicio del Sprint.

| Fase | Actividades | Duración Est. | Responsable |
|---|---|:---:|---|
| Fase 1 | Preparación: revisión de criterios de aceptación, diseño de casos de prueba, setup del ambiente, carga de datos. | 3 días | QA Lead |
| Fase 2 | Ejecución funcional: pruebas de HU-001 a HU-007, validación de reglas de negocio y flujos E2E-01 y E2E-02. | 5 días | QA |
| Fase 3 | Pruebas de integración y concurrencia: validación de flujos Kafka, prueba E2E-03, pruebas de carga (RNF-001, RNF-002). | 3 días | QA + Dev |
| Fase 4 | Pruebas de seguridad, auditoría y disponibilidad (RNF-004, RNF-005, RNF-007–009). | 2 días | QA + DevOps |
| Fase 5 | Retesting de defectos, regresión parcial de flujos existentes afectados, generación de reporte final. | 2 días | QA |
| **TOTAL** | | **~15 días hábiles** | |

---

## 11. Roles y Responsabilidades

| Rol | Perfil sugerido | Responsabilidades |
|---|---|---|
| QA Lead | Analista QA Senior | Elaborar y mantener este plan. Diseñar casos de prueba. Coordinar ejecución. Generar reporte final. Escalar defectos críticos. |
| QA | Analista QA | Ejecutar casos de prueba funcionales y E2E. Registrar defectos. Realizar retesting. |
| Dev | Desarrollador Backend / Full Stack | Dar soporte en pruebas de integración. Exponer utilidades de prueba (forzar TTL). Atender defectos. |
| DevOps | Ingeniero DevOps | Gestionar el ambiente de pruebas. Ejecutar pruebas de carga. Monitorear disponibilidad de servicios. |
| Product Owner | PO / PM | Aprobar el plan y los criterios de salida. Validar prioridades de defectos. Firmar el cierre de pruebas. |

---

## 12. Gestión de Defectos

### 12.1 Clasificación de Severidad

| Severidad | Impacto | Ejemplos en esta feature |
|---|---|---|
| Crítico (S1) | Bloquea flujo | Un usuario puede registrarse en waitlist cuando hay disponibilidad. Dos usuarios reciben la misma oportunidad de compra. El sistema no publica `SeatReleased` al expirar una reserva. |
| Alto (S2) | Afecta flujo principal | La selección FIFO no respeta el orden correcto. El email se envía a usuarios con cuenta inactiva. La oportunidad no expira cuando el TTL se cumple. |
| Medio (S3) | Funcionalidad parcialmente afectada | El mensaje de confirmación de registro no se muestra correctamente. El modal de cancelación no incluye el texto exacto especificado. |
| Bajo (S4) | Estético o menor | Problemas de alineación en el banner informativo. Ortografía en los mensajes de notificación. |

### 12.2 Flujo de Gestión

- Los defectos son registrados en el sistema de seguimiento del proyecto con los campos: ID, Historia relacionada, Severidad, Descripción, Pasos para reproducir, Resultado actual, Resultado esperado, Ambiente, Evidencia.
- Los defectos S1 y S2 deben ser comunicados inmediatamente al Dev Lead y al Product Owner.
- El retesting de un defecto corregido debe ser realizado por el mismo QA que lo reportó.
- Un defecto se cierra únicamente cuando el QA verifica la corrección en el ambiente de pruebas.

---

## 13. Métricas de Seguimiento

| Métrica | Descripción |
|---|---|
| Cobertura de ejecución | % de casos de prueba ejecutados sobre el total planificado, por prioridad. |
| Tasa de paso (Pass Rate) | % de casos de prueba que pasan sobre el total ejecutado. |
| Densidad de defectos | Número de defectos por historia de usuario o por módulo. |
| Tasa de defectos críticos | % de defectos S1/S2 sobre el total de defectos reportados. |
| Tiempo de resolución de defectos | Tiempo promedio entre el reporte de un defecto y su cierre, por severidad. |
| Métricas de impacto (negocio) | Alineadas con el documento de requisitos: tasa de conversión de waitlist, tasa de duplicación de notificaciones, tasa de expiración de oportunidades. |

---

## 14. Aprobaciones

| Rol | Nombre | Fecha | Firma |
|---|---|---|---|
| QA Lead | | | |
| Product Owner | | | |
| Dev Lead | | | |

---

*Este documento es un artefacto vivo. Cualquier cambio en el alcance o los requerimientos de la feature debe reflejarse en una versión actualizada de este plan, aprobada nuevamente por los roles indicados.*