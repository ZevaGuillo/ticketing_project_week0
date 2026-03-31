# Feature Specification: Demand Recovery Waitlist

**Feature Branch**: `002-demand-recovery-waitlist`  
**Created**: 2026-03-30  
**Status**: Draft  
**Input**: Business specification brief for Waitlist with Internal Notifications feature

---

## Executive Summary

The Demand Recovery Waitlist feature transforms lost conversion opportunities into recoverable revenue by capturing user purchase intent when seats are unavailable and reactivating it the moment inventory is freed. This notification system targets authenticated users who encounter sold-out events, enabling them to join a waitlist and receive email notification when their desired seat becomes available. This feature addresses a critical gap in the current platform where users simply leave when inventory is unavailable, permanently losing their purchase intent. Expected outcomes include measurable conversion uplift from recovered demand, improved sell-through rates for event organizers, and reduced inventory waste from expired reservations.

---

## Clarifications

### Session 2026-03-31

- **Q: Authentication & Authorization for Waitlist** → **A:** Option visible publicly, authentication required only to join the waitlist
- **Q: Notification Retry Logic** → **A:** Single notification attempt per seat release (no retries)

---

## Problem & Opportunity

### The Problem

The ticketing platform currently experiences permanent revenue loss whenever seat inventory becomes unavailable. This occurs in two scenarios: when events are genuinely sold out, and when active temporary reservations hold seats that may expire without purchase. In both cases, users who have demonstrated clear purchase intent by selecting seats and attempting to buy simply leave the platform. The demand is permanently lost with no recovery mechanism, representing untapped revenue and suboptimal inventory utilization.

The pain is felt across three stakeholder groups: end customers who leave frustrated without alternative options, event organizers who see lower-than-optimal sell-through rates, and the platform business that loses both transaction revenue and customer lifetime value from users who abandon without purchasing.

### The Opportunity

Instead of treating unavailability as a terminal state, the platform can capture purchase intent at the moment of frustration and transform it into a deferred conversion. When a user encounters an unavailable seat, offering them a waitlist option transforms abandonment into continued engagement. The moment inventory is freed—whether through reservation expiration, cancellation, or additional inventory release—the waitlisted user can be immediately notified and given priority access to complete their purchase.

This approach recognizes that users who reach the point of selecting specific seats have high purchase intent. By maintaining that connection rather than losing it entirely, the platform recovers demand that would otherwise be permanently lost.

---

## User Value Narrative

### End Customer

For the authenticated user who encounters an unavailable seat, the waitlist provides a constructive alternative to abandonment. Rather than leaving the platform in frustration, they can express continued interest by joining a waitlist for that specific event and seat section. They receive confirmation of their position and can continue browsing or return later with confidence that they will be notified the moment their desired seat becomes available. This transforms a negative experience (sold-out) into continued engagement with the platform.

When inventory frees up, the user receives an internal notification alerting them that their seat is available. They are granted a limited-time window to complete their purchase before the opportunity passes to the next person in queue. This creates a fair, transparent system where users who waited longest get first access.

### Event Organizer

Event organizers benefit indirectly from increased sell-through rates. Every waitlist registration represents confirmed demand that might have been lost entirely. When reservations expire and seats return to inventory, waitlisted users provide immediate demand recovery rather than allowing seats to sit unsold. This results in higher overall occupancy rates, particularly for high-demand events where temporary reservations previously created artificial scarcity.

Organizers also benefit from reduced administrative burden—waitlist users self-manage their interest rather than contacting support or being lost to competitors.

### Platform / Business

The platform recovers revenue that would otherwise be permanently lost. Users who join the waitlist remain engaged with the platform rather than abandoning to competitor services. Each conversion from waitlist to purchase represents incremental revenue at minimal acquisition cost, since the user has already demonstrated intent and completed much of the purchase funnel.

The feature also provides valuable demand signal data—waitlist volume indicates true oversubscription levels and can inform pricing, inventory release timing, and future event planning decisions.

---

## Business KPIs

| KPI | Definition | Target Direction | Measurement Method |
|-----|------------|------------------|-------------------|
| Waitlist-to-Purchase Conversion Rate | Percentage of users who join waitlist and successfully complete a purchase when notified | ↑ | Divide total purchases from waitlist users by total waitlist entries, tracked via purchase completion events |
| Time from Seat Release to User Notification | Duration between inventory becoming available and user receiving internal notification | ↓ | Timestamp difference between reservation-expired event and waitlist-notification sent event |
| Notification Interaction Rate | Percentage of notified users who click through to attempt purchase | ↑ | Divide notification clicks by total notifications delivered |
| Duplicate Notification Rate | Percentage of users receiving more than one notification for the same seat opportunity | ↓ | Track notification events per user per seat opportunity; target below 1% |
| Waitlist Abandonment Rate | Percentage of users who join waitlist but later cancel or expire without purchase | ↓ | Track waitlist entries that reach terminal status (CANCELLED, EXPIRED) without associated purchase |
| Purchase Window Expiration Rate | Percentage of notified users who fail to complete purchase within the allocated time window | ↓ | Track notified reservations that expire without payment |
| Average Time to Convert After Notification | Average duration from user notification to completed purchase | ↓ | Measure timestamp from notification sent to payment confirmed |

---

## Key Assumptions

**User behavior assumptions**: Users will trust and use the waitlist feature rather than abandoning to competitor platforms. Users will respond to notifications within the purchase window timeframe. Authenticated users will provide accurate contact preferences for notifications.

**Market assumptions**: Demand for popular events frequently exceeds supply, creating ongoing waitlist opportunity. Reservation patterns include sufficient expiration events to feed the waitlist system. Users value the waitlist option over searching for alternative events or platforms.

**Operational assumptions**: The platform can handle notification delivery with sufficient speed to enable purchase completion before secondary market competitors. Users will have persistent session or notification access when the purchase window opens.

**Technical assumptions (business-level)**: The existing reservation and expiration infrastructure will continue functioning reliably. Internal notification delivery will be sufficiently reliable for time-sensitive purchase opportunities.

---

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| User frustration from notification spam or timing issues | High | Limit notifications per user; ensure purchase window is realistic |
| Poor conversion due to users forgetting or losing interest | Medium | Send reminder within purchase window; maintain waitlist position visibility |
| Competitive disadvantage if waitlist users are slower than secondary market | Medium | Ensure notification latency is minimal; consider dynamic purchase window sizing |
| Event organizer perception of unfair access | Low | Maintain transparent FIFO queue; communicate clearly to organizers |
| Over-reliance on waitlist reducing urgency to purchase | Low | Monitor conversion rates; adjust communication to emphasize scarcity |

---

## Scope Boundaries for MVP

### In Scope

- Internal platform notifications (in-app) only—no external channels (email, SMS, push)
- FIFO (first-in-first-out) priority model; advanced prioritization (VIP tiers, loyalty boosts) deferred
- Basic waitlist entry and exit (join, cancel, notification, purchase)
- Single notification attempt per seat availability event
- Event-level waitlist tracking
- Manual waitlist closure by event or by user request

### Out of Scope

- External notification channels (email, SMS, push notifications)
- Advanced prioritization or fairness algorithms beyond basic FIFO
- Personalized waitlist experiences or recommendations
- Analytics dashboards for waitlist performance (basic tracking included)
- Waitlist analytics beyond core KPIs
- Cross-event waitlist transfer
- Waitlist subscription management (pause/resume)
- Automated inventory release triggers based on waitlist demand
- External API access to waitlist data
- White-label or partner waitlist configurations

---

## User Scenarios & Testing

### EPIC-001: Gestión de Waitlist por Contexto de Inventario

---

### HU-001 — Registro de Usuario en Lista de Espera para Eventos Agotados (Priority: P1)

**Historia de Usuario**:  
Como usuario autenticado que intenta adquirir un asiento en un evento sin disponibilidad, quiero poder registrarme en una lista de espera asociada a ese contexto específico (evento y sección), para ser considerado cuando se liberen asientos y recibir una notificación sobre la disponibilidad.

**Acceptance Scenarios**:

1. **Oferta de registro en lista de espera**  
   **Given** no hay asientos disponibles en un evento o sección específica  
   **When** el usuario intenta seleccionar o comprar un asiento  
   **Then** el sistema debe mostrar una opción visible para unirse a la lista de espera  
   **And** la opción debe estar asociada al contexto actual (evento y sección)

2. **Registro exitoso**  
   **Given** el usuario está autenticado  
   **And** no tiene una suscripción previa en la lista de espera para ese contexto  
   **When** selecciona la opción de unirse y confirma su registro  
   **Then** el sistema debe registrar su suscripción en la lista de espera  
   **And** debe asociarla al contexto correspondiente (evento y sección)

3. **Confirmación al usuario**  
   **Given** el registro fue realizado correctamente  
   **When** el sistema procesa la solicitud  
   **Then** debe mostrar un mensaje de confirmación  
   **And** debe indicar que será notificado si se libera disponibilidad

4. **Validación de disponibilidad**  
   **Given** existe al menos un asiento disponible  
   **When** el usuario accede al evento o sección  
   **Then** el sistema no debe mostrar la opción de unirse a la lista de espera

**Business Rules**:
- El registro en waitlist solo es posible cuando la disponibilidad es igual a cero.
- La suscripción se define por el contexto único (usuario + evento + sección).
- El usuario debe estar autenticado para registrarse.
- El registro no implica reserva de asiento ni prioridad inmediata.
- La notificación y asignación de oportunidad se gestionan en historias posteriores.

**Why this priority**: This is the core value proposition—converting abandoned users into waitlisted users. Without this capability, no demand recovery occurs.

**Independent Test**: Can be tested by simulating sold-out events and verifying users can join waitlist and receive position confirmation.

---

### HU-002 — Visualizar Suscripción Activa en Lista de Espera en la Página del Evento (Priority: P2)

**Historia de Usuario**:  
Como usuario autenticado, quiero ver una confirmación visual de que estoy en la lista de espera directamente en la página del evento agotado, para que tenga la certeza de que mi registro fue exitoso y no necesite buscar esta información en otro lugar.

**Acceptance Scenarios**:

1. **Visualización de estado activo en contexto**  
   **Given** un usuario autenticado tiene una suscripción activa en la lista de espera para una sección específica de un evento  
   **When** el usuario navega a la página de ese evento y visualiza esa sección  
   **Then** el sistema NO debe mostrar la opción para unirse a la lista de espera  
   **And** en su lugar, el sistema debe mostrar un componente visual informativo (ej. un banner o alerta)  
   **And** este componente debe contener el texto: "Ya estás en la lista de espera para esta sección. Te notificaremos si se liberan asientos."

2. **Otro usuario visualiza el mismo evento**  
   **Given** "ClienteA" tiene una suscripción activa para el "Evento A", "Sección VIP"  
   **And** el usuario "ClienteC" está autenticado en una sesión diferente  
   **When** "ClienteC" navega a la página de detalles del "Evento A"  
   **Then** el sistema NO debe mostrar el banner informativo de suscripción a "ClienteC"  
   **And** el sistema DEBE mostrar el botón "Unirme a la lista de espera" a "ClienteC"

3. **Usuario no autenticado (invitado)**  
   **Given** un usuario invitado (no autenticado) navega por el sitio  
   **And** el "Evento A", "Sección VIP" tiene 0 asientos disponibles  
   **When** el usuario invitado navega a la página de detalles del "Evento A"  
   **Then** el sistema NO debe mostrar el banner informativo de suscripción  
   **And** el sistema DEBE mostrar el botón "Unirme a la lista de espera"

4. **La suscripción del usuario no está en estado activa**  
   **Given** "ClienteA" está autenticado  
   **And** tuvo una suscripción para el "Evento A", "Sección VIP", pero su estado ahora es expirada o utilizada  
   **And** la "Sección VIP" sigue con 0 asientos disponibles  
   **When** "ClienteA" navega a la página de detalles del "Evento A"  
   **Then** el sistema NO debe mostrar el banner informativo de suscripción activa  
   **And** el sistema DEBE mostrar nuevamente el botón "Unirme a la lista de espera"

**Business Rules**:
- El sistema debe mostrar el estado actual de la suscripción (activa, expirada, consumida).
- La visualización debe estar asociada al contexto específico.

**Why this priority**: Users need visibility into their waitlist status to make informed decisions about their purchase intent.

**Independent Test**: Can be tested by creating waitlist entries in different states and verifying correct status display in event page.

---

### HU-003 — Cancelar Suscripción Activa desde la Página del Evento (Priority: P2)

**Historia de Usuario**:  
Como usuario autenticado con una suscripción activa en la lista de espera para un evento, sección o categoría específica, quiero poder cancelar mi suscripción, para dejar de participar en la lista de espera y no recibir notificaciones relacionadas con la disponibilidad de asientos.

**Acceptance Scenarios**:

1. **Inicio de la cancelación desde la UI del evento**  
   **Given** el usuario está autenticado y visualiza la página de un evento para el cual tiene una suscripción activa  
   **And** el sistema muestra el banner informativo "Ya estás en la lista de espera..."  
   **When** el usuario hace clic en el enlace o botón "Cancelar suscripción" ubicado dentro o junto a dicho banner  
   **Then** el sistema debe mostrar un modal de confirmación con el título "Confirmar Cancelación" y el texto "¿Estás seguro de que quieres abandonar la lista de espera? Perderás tu lugar actual."  
   **And** el modal debe contener dos botones: "Confirmar" y "Cerrar"

2. **Cancelación exitosa tras confirmación**  
   **Given** el usuario tiene abierto el modal de confirmación de cancelación  
   **When** el usuario hace clic en el botón "Confirmar"  
   **Then** el sistema debe actualizar el estado de la suscripción en la base de datos a cancelled  
   **And** el sistema debe mostrar una notificación "toast" de éxito con el mensaje: "Has cancelado tu suscripción a la lista de espera."  
   **And** la interfaz de la página del evento debe actualizarse inmediatamente: el banner informativo desaparece y el botón "Unirme a la lista de espera" vuelve a estar visible y activo

3. **Abandono del flujo de cancelación**  
   **Given** el usuario tiene abierto el modal de confirmación de cancelación  
   **When** el usuario hace clic en el botón "Cerrar" o cierra el modal por otros medios (ej. tecla Esc)  
   **Then** el modal debe cerrarse  
   **And** la suscripción del usuario debe permanecer en estado activa  
   **And** la interfaz de la página del evento no debe sufrir ningún cambio

**Business Rules**:
- Solo se pueden cancelar suscripciones en estado activo.
- La cancelación elimina al usuario de la lista de espera.

**Why this priority**: User autonomy and control are important for trust. Users should be able to exit the waitlist without friction.

**Independent Test**: Can be tested by cancelling waitlist entries and verifying removal and confirmation.

---

### EPIC-002: Detección de Liberación de Inventario

---

### HU-004 — Publicar Evento de Liberación de Asiento por Expiración de Reserva (Priority: P1)

**Historia de Usuario**:  
Como sistema de inventario, quiero que la expiración de una reserva temporal de asiento genere y publique un evento de negocio, para que otros sistemas, como el de la lista de espera, puedan reaccionar a la nueva disponibilidad de inventario y maximizar las oportunidades de venta.

**Acceptance Scenarios**:

1. **Detección de transición válida**  
   **Given** un asiento se encuentra en estado "reservado"  
   **When** cambia a estado "disponible"  
   **Then** el sistema debe identificar la transición como una liberación de inventario

2. **Transición no relevante**  
   **Given** un asiento cambia de estado "disponible" a "vendido"  
   **When** se registra el cambio  
   **Then** el sistema no debe considerarlo como una liberación de inventario

3. **No se genera evento en una compra exitosa**  
   **Given** un asiento específico tiene el estado reserved  
   **When** el usuario completa la compra exitosamente antes de que el TTL expire  
   **Then** el estado del asiento debe cambiar de reserved a sold  
   **And** el sistema NO debe publicar un evento SeatReleased en el topic inventory-events

**Business Rules**:
- Solo la transición "reservado → disponible" se considera liberación.
- Otras transiciones no activan procesos de reactivación.
- Cada transición debe ser registrada de forma única.

**Why this priority**: This is the trigger for the entire demand reactivation flow. Without seat release detection, no users can be notified.

**Independent Test**: Can be tested by simulating seat state transitions and verifying only valid releases trigger notification logic.

---

### EPIC-003: Notificación y Reactivación de Demanda

---

### HU-005 — Procesar Liberación de Asiento y Asignar Oportunidad al Siguiente en la Cola (Priority: P1)

**Historia de Usuario**:  
Como sistema de lista de espera, quiero consumir eventos de liberación de asientos para seleccionar al primer usuario elegible (FIFO) y cambiar el estado de su suscripción, para que se le asigne formalmente una oportunidad de compra y se inicie el flujo de notificación.

**Acceptance Scenarios**:

1. **Asignación exitosa al primer usuario en la cola**  
   **Given** el servicio consume un evento SeatReleased para un asiento en el "Evento A", "Sección VIP"  
   **And** la lista de espera para ese contexto tiene varios usuarios con suscripción activa  
   **And** "UsuarioX" es el usuario con la fecha de suscripción más antigua (FIFO)  
   **When** el evento es procesado  
   **Then** el sistema debe seleccionar únicamente a "UsuarioX"  
   **And** el estado de la suscripción de "UsuarioX" debe cambiar de activa a ofrecida  
   **And** el sistema debe publicar un nuevo evento WaitlistOpportunityGranted que contenga el userId de "UsuarioX", los detalles del evento/sección y un TTL para la oportunidad (ej. 10 minutos)

2. **No hay usuarios elegibles en la lista de espera**  
   **Given** el servicio consume un evento SeatReleased  
   **And** no hay ningún usuario con suscripción activa en la lista de espera para ese contexto  
   **When** el evento es procesado  
   **Then** el sistema no debe realizar ninguna selección ni cambiar ningún estado  
   **And** no se debe publicar ningún evento WaitlistOpportunityGranted

**Business Rules**:
- La selección se realiza en orden FIFO basado en timestamp.
- Solo se consideran suscripciones en estado activa.
- La selección es por contexto (evento/sección).

**Why this priority**: This is the critical step that connects inventory release to the right user for reactivation.

**Independent Test**: Can be tested by creating waitlist entries with different timestamps and verifying FIFO selection.

---

### HU-006 — Enviar Notificación por Email de Oportunidad de Compra (Priority: P1)

**Historia de Usuario**:  
Como sistema de notificaciones, quiero consumir eventos de oportunidad de la lista de espera para componer y enviar un correo electrónico al usuario seleccionado, para que el usuario sea informado de inmediato sobre su oportunidad de compra por tiempo limitado y pueda actuar rápidamente.

**Acceptance Scenarios**:

1. **Envío de email tras recibir evento de oportunidad**  
   **Given** el servicio de notificaciones consume un evento WaitlistOpportunityGranted del topic de Kafka  
   **And** el evento contiene userId, eventId, sectionId y un opportunityTTL  
   **And** el usuario asociado al userId tiene una cuenta activa y un correo electrónico verificado  
   **When** el evento es procesado  
   **Then** el sistema debe componer un correo electrónico dirigido al usuario  
   **And** el correo debe ser entregado a un proveedor de servicios de email para su envío

2. **Vigencia de la oportunidad**  
   **Given** el usuario recibe una notificación  
   **When** accede después de un periodo definido  
   **Then** el sistema debe validar si la oportunidad sigue vigente antes de permitir continuar con la compra

3. **No se notifica a usuarios inactivos**  
   **Given** el servicio consume un evento WaitlistOpportunityGranted  
   **But** el usuario asociado al userId tiene su cuenta en estado inactiva o suspendida  
   **When** el evento es procesado  
   **Then** el sistema NO debe enviar ningún correo electrónico  
   **And** el evento debe ser marcado como procesado para evitar reintentos

**Business Rules**:
- Solo usuarios seleccionados reciben notificación.
- La notificación debe estar asociada a un contexto específico.
- No se deben notificar usuarios inactivos.

**Why this priority**: This is the communication step that informs users about available seats and enables purchase completion.

**Independent Test**: Can be tested by creating waitlist entries, triggering seat release, and verifying email notifications are sent.

---

### HU-007 — Validar Oportunidad de Compra y Crear Reserva para Checkout (Priority: P1)

**Historia de Usuario**:  
Como un usuario que ha hecho clic en el enlace de notificación de la lista de espera, quiero que el sistema valide mi oportunidad de compra y me reserve el asiento automáticamente, para que pueda proceder al pago de forma segura y sin el riesgo de que alguien más compre el asiento.

**Acceptance Scenarios**:

1. **Acceso exitoso con oportunidad válida**  
   **Given** un usuario accede a la URL de oportunidad con un token válido  
   **And** la oportunidad asociada al token está en estado offered y su TTL no ha expirado  
   **When** el sistema procesa la solicitud  
   **Then** el sistema debe cambiar el estado de la oportunidad a in_progress (o similar) para prevenir su reutilización  
   **And** el sistema debe crear una reserva temporal estándar (TTL de 15 minutos) para el asiento asociado a nombre del usuario  
   **And** el usuario debe ser redirigido a la página de checkout con el asiento ya en su carrito

2. **Acceso dentro del tiempo permitido**  
   **Given** el usuario accede dentro del tiempo límite establecido  
   **When** intenta continuar con la compra  
   **Then** el sistema debe permitirle iniciar el proceso de compra  
   **And** debe reconocer su prioridad frente a otros usuarios

3. **Acceso fuera del tiempo permitido**  
   **Given** el tiempo límite ha expirado  
   **When** el usuario intenta utilizar la oportunidad  
   **Then** el sistema no debe permitir continuar con la compra bajo esa prioridad  
   **And** debe informarle que la oportunidad ha expirado

4. **Consumo de la oportunidad**  
   **Given** el usuario accede dentro del tiempo permitido  
   **And** inicia el proceso de compra  
   **When** se confirma su uso  
   **Then** el sistema debe marcar la oportunidad como utilizada  
   **And** no debe permitir reutilizarla

5. **Liberación tras expiración**  
   **Given** la oportunidad no fue utilizada dentro del tiempo límite  
   **When** expira el tiempo asignado  
   **Then** el sistema debe liberar la oportunidad  
   **And** permitir que otros usuarios en la lista de espera puedan ser considerados

**Business Rules**:
- Cada oportunidad de compra tiene un tiempo limitado de validez
- El tiempo comienza desde el momento en que se notifica al usuario.
- Solo durante ese periodo el usuario tiene prioridad.

**Why this priority**: This validates the entire waitlist value chain—from waitlist entry through notification to revenue generation.

**Independent Test**: Can be tested by completing purchases within and outside the time window to verify reservation and waitlist state transitions.

---

### Edge Cases

- **What happens when multiple seats become available simultaneously?** The system should notify users in queue order, one seat per notification, allowing fair sequential access.
- **How does the system handle a user who receives notification but the seat is taken by another user before they complete purchase?** The user should be notified that the seat is no longer available and offered the option to remain on the waitlist for the next availability.
- **What happens when an event is cancelled or rescheduled?** Waitlist entries should be automatically cancelled and users notified of the event status change.
- **How does the system handle extremely long waitlists?** Users should be informed of their approximate position and estimated wait time based on historical conversion rates.

---

## Requirements

### Functional Requirements

#### HU-001: Registro en Lista de Espera

- **RF-001**: El sistema mostrará una opción para unirse a la lista de espera cuando no exista disponibilidad de asientos en un evento o sección.
- **RF-002**: El sistema validará que el usuario no tenga una suscripción activa previa para el mismo contexto.
- **RF-003**: El sistema registrará la suscripción del usuario asociada al evento y sección correspondientes.

#### HU-002: Visualización de Suscripción en Página del Evento

- **RF-004**: El sistema mostrará un banner informativo "Ya estás en la lista de espera..." cuando el usuario autenticado tenga suscripción activa.
- **RF-005**: El sistema no mostrará el banner a otros usuarios sin suscripción activa.
- **RF-006**: El sistema mostrará el botón "Unirme a la lista de espera" a usuarios sin suscripción activa.

#### HU-003: Cancelación de Suscripción

- **RF-007**: El sistema mostrará un modal de confirmación antes de cancelar la suscripción.
- **RF-008**: El sistema actualizará el estado a cancelled tras confirmación del usuario.
- **RF-009**: El sistema mostrará una notificación toast de éxito tras cancelación.

#### HU-004: Publicar Evento de Liberación

- **RF-010**: El sistema publicará un evento SeatReleased solo en transición "reservado → disponible".
- **RF-011**: El sistema NO publicará evento en transición "reservado → vendido".
- **RF-012**: El sistema garantizará idempotencia en la emisión de eventos.

#### HU-005: Procesar Liberación y Asignar Oportunidad

- **RF-013**: El sistema seleccionará usuarios activos en orden FIFO.
- **RF-014**: El sistema cambiará el estado de suscripción de activa a ofrecida.
- **RF-015**: El sistema publicará un evento WaitlistOpportunityGranted con TTL.

#### HU-006: Enviar Notificación por Email

- **RF-016**: El sistema enviará email al usuario seleccionado tras recibir evento de oportunidad.
- **RF-017**: El sistema validará que el usuario tenga cuenta activa y email verificado.
- **RF-018**: El sistema no enviará email a usuarios inactivos.

#### HU-007: Validar Oportunidad y Crear Reserva

- **RF-019**: El sistema validará la vigencia de la oportunidad antes de crear reserva.
- **RF-020**: El sistema creará una reserva temporal de 15 minutos.
- **RF-021**: El sistema bloqueará el acceso si la oportunidad expiró.
- **RF-022**: El sistema marcará la oportunidad como utilizada tras consumo.
- **RF-023**: El sistema liberará la oportunidad tras expiración para siguiente usuario.

### Key Entities

- **Waitlist Subscription**: Represents a user's request to be notified when seats become available for a specific context (user + event + section). Contains user identification, event reference, section, registration timestamp, status (active, offered, expired, consumed, cancelled), notification timestamps.
- **Reservation Expired Event**: Represents the transition of a seat from reserved to available state (consumed from existing Kafka topic). Contains seat identification, event reference, section, release timestamp.
- **Opportunity Window**: Represents the time-limited opportunity granted to a user after notification, containing start timestamp, expiration timestamp, status (offered, in_progress, used, expired), token.

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users who join waitlist convert to purchases at a rate of at least 15% within 30 days of joining.
- **SC-002**: Time from seat release to user notification averages less than 60 seconds under normal operating conditions.
- **SC-003**: At least 70% of notified users interact with the notification (click-through) within the purchase window.
- **SC-004**: Duplicate notification rate remains below 1% of total notifications sent.
- **SC-005**: Purchase window expiration rate stays below 25%, indicating users have sufficient time to complete purchases.
- **SC-006**: Average time from notification to purchase completion is under 5 minutes for successful conversions.
- **SC-007**: Waitlist abandonment rate (users who cancel or expire without purchase) stays below 40%.

---

## Assumptions

- Users have persistent login sessions or can easily re-authenticate when notified of seat availability.
- The existing reservation expiration infrastructure reliably triggers events when seats become available.
- Purchase window timing (currently 15 minutes) is sufficient for users to complete transactions.
- Internal notification delivery is reliable enough for time-sensitive purchase opportunities.
- Users prefer in-app notifications over external channels for MVP, reducing complexity and spam risk.
- FIFO ordering is acceptable to users; more sophisticated prioritization can be added in future iterations.
