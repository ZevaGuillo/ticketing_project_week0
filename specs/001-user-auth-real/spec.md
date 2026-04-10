# Feature Specification: Autenticación Real de Usuarios

**Feature Branch**: `001-user-auth-real`  
**Created**: 2026-03-30  
**Status**: Draft  
**Input**: "Implementar autenticación real de usuarios: login, registro, protección de reserva, distinción usuario real/invitado, redirecciones y UI".

---

## User Scenarios & Testing *(mandatory)*
- Q: ¿Dónde debe almacenarse el token de sesión JWT en el frontend?
	→ A: En localStorage, con TTL corto y refresh token para no dañar la UX.


### P1 · Registro y Login de Usuarios
**Story**: Como visitante quiero registrarme con email y contraseña para convertir mis acciones de compra en transacciones asociadas a mi identidad.

**Why**: Sin registro ni login no existe trazabilidad ni control sobre reservas y pagos.
1. ¿Se requiere refresh token o basta reautenticar cada 2 horas? (coordinar con backend).
2. ¿Debe mostrarse un indicador persistente en la cabecera con el email del usuario y botón de logout?
**Acceptance Scenarios**
1. Dado un visitante sin cuenta, cuando completa `/register` con datos válidos, entonces se crea el usuario y se muestra confirmación para iniciar sesión.
2. Dado un usuario registrado, cuando envía credenciales válidas en `/login`, entonces obtiene token válido y la UI refleja que está autenticado.
3. Dado un usuario registrado, cuando envía credenciales inválidas, entonces recibe mensaje de error y permanece como invitado.

---

### P2 · Reserva Protegida por Sesión
**Story**: Como usuario quiero que reservar asientos o pagar requiera sesión activa para evitar compras anónimas y abusos.

**Why**: Asegura la asociación de cada reserva con un `userId` real y habilita soporte, cancelaciones y reporting.

**Independent Test**: Intentar reservar sin sesión debe redirigir a login; repetir tras autenticarse debe completar la reserva.

**Acceptance Scenarios**
1. Dado un visitante, cuando pulsa "Reserve & Add to Cart", entonces es redirigido a `/login` con la ruta original preservada para volver después.
2. Dado un usuario autenticado, cuando reserva un asiento, entonces la reserva se crea con su `userId` y aparece en el carrito.

---

### P3 · Acceso Público al Catálogo
**Story**: Como visitante quiero explorar eventos y asientos disponibles sin iniciar sesión para decidir si me interesa crear una cuenta.

**Why**: Mantiene baja fricción y conserva el comportamiento actual del landing.

**Independent Test**: Abrir `/` y `/events/[id]` desde incógnito debe funcionar sin advertencias de login.

**Acceptance Scenario**
1. Dado un visitante, cuando navega la lista y detalle de eventos, entonces visualiza precios, secciones y disponibilidad sin restricciones.

---

### Edge Cases
- Registro con email existente debe mostrar error específico y no crear cuenta duplicada.
- Expiración del token obliga a reautenticarse cuando se intenta una acción protegida.
- Logout manual limpia token, reservas locales y regresa al estado invitado.
- Fallos de red durante login/registro deben mostrar mensajes recuperables para reintentar.

---

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: Formularios `/register` y `/login` deben validar email y contraseña (mínimo 8 caracteres) con feedback inline.
- **FR-002**: El frontend debe usar `POST /users` y `POST /token` del servicio Identity vía HTTPS.
- **FR-003**: `auth-context` debe exponer estados `guest`, `authenticating`, `authenticated`, `error`, además de `user`, `token`, `login()`, `logout()`.
- **FR-004**: Reservas, creación de órdenes y pagos deben rechazar peticiones sin token válido tanto en frontend como backend.
- **FR-005**: Acciones protegidas sin sesión redirigen a `/login?redirectTo=<ruta original>`.
- **FR-006**: Debe existir logout explícito accesible en la UI principal que limpie storage y estado del carrito.
- **FR-007**: Mensajes diferenciados para email duplicado, credenciales inválidas, expiración de sesión y errores del servidor.
- **FR-008**: Las reservas persistidas deben almacenar el `userId` del token en vez de un UUID generado en el cliente.
- **FR-009**: El catálogo público (home, lista/detalle de eventos) debe seguir siendo accesible sin autenticación.

### Technical Requirements
- Usar JWT emitido por Identity; preferir almacenamiento en cookie `httpOnly` + memoria o solo memoria si no hay BFF.
- Todas las llamadas protegidas deben adjuntar `Authorization: Bearer <token>` y manejar respuestas 401/403 forzando logout.
- El contexto de carrito debe depender del `userId` autenticado y limpiar su estado al perder la sesión.
- Reutilizar la lógica de expiración utilizada por el panel admin para consistencia.

### Key Entities
- **Usuario**: `id`, `email`, `passwordHash`, `role`, timestamps.
- **Sesión**: `accessToken`, `expiresAt`, `redirectAfterLogin`, opcional `refreshToken`.
- **Invitado**: estado efímero sin identificador persistido, acceso únicamente de lectura.

---

## Success Criteria *(mandatory)*
- **SC-001**: ≥95% de usuarios nuevos completan registro + login en menos de 120 segundos en pruebas internas.
- **SC-002**: 100% de reservas confirmadas contienen `userId` no nulo en base de datos.
- **SC-003**: 0 compras anónimas posteriores al release.
- **SC-004**: ≥90% de errores de autenticación registrados en logs muestran mensaje al usuario.
- **SC-005**: Páginas públicas siguen cargando en <1.5s sin requerir token.

---

## Assumptions
- Autenticación basada en email/contraseña; SSO/OAuth quedan fuera de esta iteración.
- Servicio Identity (BCrypt + JWT) ya funciona y está disponible para el frontend.
- Recuperación de contraseña y verificación de email no se incluyen en este release.
- Backend confía en el token recibido y validará firma/expiración en puertas de entrada.

---

## Open Questions
1. ¿Se almacenará el token únicamente en memoria (mejor seguridad) o también en `localStorage` para persistir sesiones tras refresh?
2. ¿Se requiere refresh token o basta reautenticar cada 2 horas? (coordinar con backend).
3. ¿Debe mostrarse un indicador persistente en la cabecera con el email del usuario y botón de logout?
