# Casos de Prueba — Lista de Espera con Notificaciones Internas
**Ticketing Platform MVP**

| Campo | Detalle |
|---|---|
| Versión | 1.0 |
| Fecha | Julio 2025 |
| Estado | En revisión |
| Proyecto | Ticketing Platform MVP |
| Relacionado con | Plan de Pruebas v1.0 |

---

## Convenciones del Documento

- **CP:** Caso de Prueba
- **HU:** Historia de Usuario
- **TTL:** Time To Live (tiempo de vida de una reserva u oportunidad)
- **FIFO:** First In, First Out (orden de registro)
- **Estado esperado de BD:** resultado verificable directamente en base de datos
- **Prioridad:** Alta / Media / Baja según impacto en el flujo de negocio

---

## HU-001 — Registro de Usuario en Lista de Espera para Eventos Agotados

### Escenario 1: Oferta de registro, registro exitoso y confirmación al usuario

#### CP_HU001_01 — Registro exitoso en lista de espera y visualización de confirmación

| Campo | Detalle |
|---|---|
| **Propósito** | Validar el flujo completo y exitoso de registro en la lista de espera para un usuario autenticado que encuentra una sección agotada, asegurando que se muestre la opción, se procese el registro y se presente el mensaje de confirmación. |
| **Técnica(s)** | Caso de Uso, Partición de Equivalencia |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado en la plataforma. El evento "Concierto Sinfónico" existe. La sección "General" tiene 0 asientos disponibles. El usuario NO tiene una suscripción activa previa para esa sección y evento. |

**Pasos:**

```gherkin
Given el usuario está autenticado en la plataforma Ticketing
And existe un evento "Concierto Sinfónico" con la sección "General" totalmente reservada
And no tengo una suscripción activa previa para esa sección y evento
When accedo a la página de detalles del evento "Concierto Sinfónico"
And hago clic en un asiento reservado de la sección "General"
Then el sistema debe mostrar una opción visible y clara para "Unirse a la lista de espera" asociada a esa sección y evento
When selecciono la opción para unirme y confirmo mi registro
Then el sistema debe registrar mi suscripción en la base de datos asociándola a mi usuario, al evento "Concierto Sinfónico" y a la sección "General"
And debo visualizar un mensaje de confirmación "¡Te has unido a la lista de espera! Te notificaremos si se liberan asientos."
```

**Resultado esperado:**
- La opción "Unirse a la lista de espera" es visible en pantalla para la sección "General".
- Se crea un registro en `WAITLIST_ENTRIES` con `status = 'active'`, vinculado al usuario, evento y sección correctos.
- Se muestra el mensaje de confirmación al usuario.

---

### Escenario 4: Validación de disponibilidad

#### CP_HU001_02 — No mostrar opción de lista de espera con asientos disponibles

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que la opción para unirse a la lista de espera no se presente si la sección de un evento tiene uno o más asientos disponibles. |
| **Técnica(s)** | Partición de Equivalencia, Análisis de Valores Límite (Disponibilidad > 0) |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado. El evento "Concierto Sinfónico" existe. La sección "General" tiene al menos 1 asiento disponible. |

**Pasos:**

```gherkin
Given el usuario está autenticado en la plataforma Ticketing
And existe un evento "Concierto Sinfónico" con la sección "General" con asientos disponibles
When accedo a la página de detalles del evento "Concierto Sinfónico"
And visualizo el mapa de asientos de la sección "General"
Then el sistema no debe mostrar ninguna opción para unirse a una lista de espera
And debo poder proceder con el flujo normal de selección y compra de asientos
```

**Resultado esperado:**
- La opción "Unirse a la lista de espera" no aparece en pantalla.
- El flujo normal de selección de asientos está disponible.

---

### Flujos alternos y de excepción

#### CP_HU001_03 — Impedir registro duplicado en la misma lista de espera

| Campo | Detalle |
|---|---|
| **Propósito** | Validar la regla de negocio que impide que un usuario se suscriba más de una vez al mismo contexto (usuario + evento + sección). |
| **Técnica(s)** | Pruebas de Transición de Estado |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado. Existe una suscripción activa (`status = 'active'`) en `WAITLIST_ENTRIES` para ese usuario, el evento "Concierto Sinfónico" y la sección "General". |

**Pasos:**

```gherkin
Given el usuario está autenticado en la plataforma Ticketing
And existe un evento "Concierto Sinfónico" con la sección "General" totalmente reservada
And no tengo una suscripción activa previa para esa sección y evento
When accedo a la página de detalles del evento "Concierto Sinfónico"
And hago clic en un asiento reservado de la sección "General"
And selecciono la opción para unirme y confirmo mi registro
Then el botón de unirse a la lista de espera debe mostrar "On Waitlist"
When accedo nuevamente a la página de detalles de dicho evento y sección
And hago clic en un asiento reservado de la sección "General"
Then el botón de unirse a la lista de espera debe mostrar "On Waitlist"
And no se debe crear un registro duplicado en WAITLIST_ENTRIES
```

**Resultado esperado:**
- No se muestra la opción de unirse nuevamente.
- Se muestra el banner de estado activo.
- No se crea un registro duplicado en `WAITLIST_ENTRIES`.

---

#### CP_HU001_04 — Requerir autenticación para unirse a la lista de espera

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que solo los usuarios autenticados puedan registrarse, cumpliendo con RNF-007. |
| **Técnica(s)** | Pruebas de Seguridad (Control de Acceso), Caso de Uso |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario no autenticado (sesión de invitado). La sección "General" del evento "Concierto Sinfónico" tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Given soy un usuario visitante (no autenticado) en la plataforma
And accedo a la página del evento "Concierto Sinfónico", cuya sección "General" está agotada
When selecciono la opción visible para unirme a la lista de espera
Then el sistema debe redirigirme a la página de inicio de sesión o solicitar la autenticación a través de un modal
And el registro en la lista de espera no debe completarse hasta que mi autenticación sea exitosa
```

**Resultado esperado:**
- El usuario es redirigido al login o se muestra un modal de autenticación.
- No se crea ningún registro en `WAITLIST_ENTRIES` hasta completar el login.

---

#### CP_HU001_05 — Permitir registro en listas de espera de diferentes secciones del mismo evento

| Campo | Detalle |
|---|---|
| **Propósito** | Confirmar que la unicidad de la suscripción se gestiona a nivel de sección, permitiendo a un usuario estar en múltiples listas dentro de un mismo evento. |
| **Técnica(s)** | Pruebas Combinatorias |
| **Prioridad** | Media |
| **Precondiciones** | Usuario autenticado. Existe suscripción activa para la sección "General" del evento "Concierto Sinfónico". La sección "VIP" del mismo evento tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Given el usuario está autenticado en la plataforma Ticketing
And está en la lista de espera para la sección "General" del evento "Concierto Sinfónico"
And la sección "VIP" del mismo evento también se encuentra agotada
When accedo a la sección "VIP" y selecciono la opción para unirme a su respectiva lista de espera
And confirmo mi registro
Then el sistema debe registrar exitosamente mi nueva suscripción para la sección "VIP"
And mi cuenta debe mantener activas ambas suscripciones de forma independiente: una para "General" y otra para "VIP" del "Concierto Sinfónico"
```

**Resultado esperado:**
- Se crean dos registros independientes en `WAITLIST_ENTRIES` para el mismo usuario y evento, con `section` diferente.
- Ambos registros tienen `status = 'active'`.

---

## HU-002 — Visualizar Suscripción Activa en Lista de Espera en la Página del Evento

### Escenario 1: Visualización de estado activo en contexto

#### CP_HU002_01 — Visualización correcta del estado de suscripción activa

| Campo | Detalle |
|---|---|
| **Propósito** | Confirmar que un usuario con una suscripción activa ve el componente informativo correcto y no la opción para unirse nuevamente. |
| **Técnica(s)** | Caso de Uso, Pruebas de Estado |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado. Existe suscripción activa en `WAITLIST_ENTRIES` para el usuario, sección "VIP" del evento "Concierto Sinfónico". |

**Pasos:**

```gherkin
Given el usuario está autenticado con una suscripción activa en la lista de espera para la sección "VIP" del evento "Concierto Sinfónico"
When navego a la página de detalles del evento "Concierto Sinfónico" y observo la sección "VIP"
Then el sistema no debe mostrar la opción para "Unirme a la lista de espera"
And en su lugar, debo visualizar un componente informativo claro, como un banner o una alerta
And este componente debe contener el texto exacto: "Ya estás en la lista de espera para esta sección. Te notificaremos si se liberan asientos."
```

**Resultado esperado:**
- No aparece el botón "Unirme a la lista de espera".
- Se muestra el banner informativo con el texto exacto especificado.

---

### Escenario 2: Otro usuario visualiza el mismo evento

#### CP_HU002_02 — Verificación de no visualización de estado para otro usuario

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el estado de suscripción de un usuario no es visible para otro usuario diferente, mostrando a este último la opción de registro estándar. |
| **Técnica(s)** | Partición de Equivalencia (Usuarios distintos), Pruebas de Seguridad (Aislamiento de datos) |
| **Prioridad** | Alta |
| **Precondiciones** | UsuarioA tiene suscripción activa para sección "VIP" del evento "Concierto Sinfónico". UsuarioB está autenticado en sesión diferente y no tiene suscripción para ese contexto. La sección "VIP" del evento tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Given el usuario "UsuarioA" tiene una suscripción activa para la sección "VIP" del evento "Concierto Sinfónico"
And yo soy el usuario "UsuarioB", estoy autenticado en una sesión diferente y no tengo suscripción para ese contexto
When navego a la página de detalles del evento "Concierto Sinfónico" y observo la sección "VIP", que está agotada
Then el sistema no debe mostrarme ningún banner informativo sobre la suscripción de "UsuarioA"
And el sistema debe mostrarme la opción para "Unirme a la lista de espera"
```

**Resultado esperado:**
- "UsuarioB" no ve el banner de suscripción activa.
- "UsuarioB" ve el botón "Unirme a la lista de espera".

---

### Escenario 3: Usuario no autenticado (invitado)

#### CP_HU002_03 — Comportamiento para usuario no autenticado

| Campo | Detalle |
|---|---|
| **Propósito** | Validar que un usuario no autenticado no ve ningún estado de suscripción y se le presenta la opción de unirse a la lista. |
| **Técnica(s)** | Partición de Equivalencia (Rol de usuario: invitado) |
| **Prioridad** | Media |
| **Precondiciones** | Sesión de invitado (sin autenticación). La sección "VIP" del evento "Concierto Sinfónico" tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Given soy un usuario visitante (no autenticado) en la plataforma
And la sección "VIP" del evento "Concierto Sinfónico" no tiene asientos disponibles
When navego a la página de detalles del evento "Concierto Sinfónico" y observo la sección "VIP"
Then el sistema no debe mostrar ningún banner informativo relacionado con suscripciones
And el sistema debe mostrar la opción para "Unirme a la lista de espera"
```

**Resultado esperado:**
- No se muestra banner de estado.
- Se muestra el botón "Unirme a la lista de espera" (que redirigirá al login al hacer clic).

---

### Escenario 4: La suscripción del usuario no está en estado activa

#### CP_HU002_04 — Comportamiento con suscripción expirada o utilizada

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que si la suscripción de un usuario ya no está activa, el sistema revierte la interfaz para permitirle unirse nuevamente. |
| **Técnica(s)** | Pruebas de Transición de Estado |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado. Su suscripción para la sección "VIP" del evento "Concierto Sinfónico" tiene `status = 'expired'` en `WAITLIST_ENTRIES`. La sección "VIP" continúa con 0 asientos disponibles. |

**Pasos:**

```gherkin
Given el usuario está autenticado y mi suscripción previa para la sección "VIP" del evento "Concierto Sinfónico" ahora tiene un estado de "expirada"
And la sección "VIP" del evento "Concierto Sinfónico" continúa sin asientos disponibles
When navego a la página de detalles del evento "Concierto Sinfónico" y observo la sección "VIP"
Then el sistema no debe mostrar el banner informativo de suscripción activa
And el sistema debe mostrarme nuevamente la opción para "Unirme a la lista de espera"
```

**Resultado esperado:**
- El banner de suscripción activa no aparece.
- Se muestra el botón "Unirme a la lista de espera".

---

## HU-003 — Cancelar Suscripción Activa desde la Página del Evento

### Escenario 1: Inicio de la cancelación desde la UI del evento

#### CP_HU003_01 — Despliegue del modal de confirmación de cancelación

| Campo | Detalle |
|---|---|
| **Propósito** | Validar que al hacer clic en la opción de cancelar, el sistema presenta correctamente el modal de confirmación con todos sus elementos requeridos. |
| **Técnica(s)** | Caso de Uso, Pruebas de Interfaz de Usuario (UI) |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado en la página del evento. El usuario tiene una suscripción activa, por lo que el banner informativo está visible. |

**Pasos:**

```gherkin
Given el usuario está autenticado y está en la página del evento "Concierto Sinfónico"
And tengo una suscripción activa a la lista de espera, por lo que visualizo el banner informativo "Ya estás en la lista de espera..."
When hago clic en el enlace o botón "Cancelar suscripción"
Then el sistema debe mostrar un modal de confirmación
And el título del modal debe ser "Confirmar Cancelación"
And el texto del cuerpo del modal debe ser "¿Estás seguro de que quieres abandonar la lista de espera? Perderás tu lugar actual."
And el modal debe contener un botón con el texto "Confirmar" y otro con el texto "Cerrar"
```

**Resultado esperado:**
- El modal aparece con título, texto y ambos botones correctos.

---

### Escenario 2: Cancelación exitosa tras confirmación

#### CP_HU003_02 — Proceso de cancelación exitoso y actualización de la interfaz

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que tras la confirmación del usuario, la suscripción se cancela en el sistema y la interfaz se actualiza en tiempo real para reflejar el cambio. |
| **Técnica(s)** | Pruebas de Transición de Estado, Caso de Uso |
| **Prioridad** | Alta |
| **Precondiciones** | otro usuario reserva el asiento 1 de fila 1 en la sección "General" del evento "Concierto Sinfónico". El usuario está autenticado en la plataforma Ticketing. |

**Pasos:**

```gherkin
Given otro usuario reserva el asiento 1 de fila 1 en la sección "General" del evento "Concierto Sinfónico"
And el usuario está autenticado en la plataforma Ticketing
And accedo a la página de detalles del evento "Concierto Sinfónico"
When intento interactuar con un asiento reservado de la sección "General"
And selecciono la opción para unirme a la lista de espera
And debo visualizar un indicador de confirmación de lista de espera
When accedo a la página "My Waitlist"
Then debo ver mi suscripción al evento "Concierto Sinfónico" en la lista
When cancelo mi suscripción al evento "Concierto Sinfónico"
Then la suscripción al evento "Concierto Sinfónico" debe desaparecer de la lista
When recargo la página "My Waitlist"
Then no debo ver mi suscripción al evento "Concierto Sinfónico" en la lista
```

**Resultado esperado:**
- El registro en `WAITLIST_ENTRIES` tiene `status = 'cancelled'`.
- El toast de éxito se muestra con el mensaje "Has cancelado tu suscripción a la lista de espera."
- La UI refleja el cambio sin necesidad de recargar la página.

---

### Escenario 3: Abandono del flujo de cancelación

#### CP_HU003_03 — Abandono de la cancelación usando el botón "Cerrar"

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que al usar el botón "Cerrar" del modal, el flujo se interrumpe y el estado de la suscripción y la UI permanecen sin cambios. |
| **Técnica(s)** | Caso de Uso (Flujo Alterno) |
| **Prioridad** | Media |
| **Precondiciones** | El modal de confirmación de cancelación está visible. |

**Pasos:**

```gherkin
Given he iniciado el proceso de cancelación y el modal de confirmación está visible
When hago clic en el botón "Cerrar"
Then el modal de confirmación debe cerrarse
And mi suscripción a la lista de espera debe permanecer en estado "activa" en el sistema
And la interfaz de la página del evento debe mantenerse sin cambios, mostrando el banner de suscripción activa
```

**Resultado esperado:**
- El modal se cierra.
- El registro en `WAITLIST_ENTRIES` permanece con `status = 'active'`.
- El banner informativo sigue visible.

---

#### CP_HU003_04 — Abandono de la cancelación cerrando el modal por otros medios

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que otros métodos para cerrar el modal (tecla Escape, clic fuera del área) también cancelan la operación de forma segura. |
| **Técnica(s)** | Pruebas de Usabilidad, Caso de Uso (Flujo Alterno) |
| **Prioridad** | Baja |
| **Precondiciones** | El modal de confirmación de cancelación está visible. |

**Pasos:**

```gherkin
Given he iniciado el proceso de cancelación y el modal de confirmación está visible
When presiono la tecla "Escape" en mi teclado para cerrar el modal
Then el modal de confirmación debe cerrarse
And mi suscripción a la lista de espera debe permanecer en estado "activa"
And la interfaz de la página del evento no debe sufrir ninguna modificación
```

**Resultado esperado:**
- El modal se cierra.
- El estado de la suscripción no cambia.
- La UI permanece sin cambios.

---

### Flujo de excepción

#### CP_HU003_05 — Manejo de error si la cancelación falla en el backend

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que si ocurre un error en el servidor al intentar cancelar, el usuario recibe una notificación de error y la UI no queda en un estado inconsistente. |
| **Técnica(s)** | Pruebas de Robustez, Manejo de Excepciones |
| **Prioridad** | Media |
| **Precondiciones** | El modal de confirmación está visible. El servicio de backend está configurado para retornar un error (5xx) en la solicitud de cancelación. |

**Pasos:**

```gherkin
Given he iniciado el proceso de cancelación y el modal de confirmación está visible
And el servicio de backend fallará al procesar la solicitud de cancelación
When hago clic en el botón "Confirmar"
Then el modal de confirmación debe cerrarse
And el sistema debe mostrar una notificación de error, como "No se pudo cancelar la suscripción. Por favor, inténtalo de nuevo."
And mi suscripción debe permanecer en estado "activa"
And la interfaz de la página del evento debe seguir mostrando el banner de suscripción activa
```

**Resultado esperado:**
- Se muestra el mensaje de error.
- El registro en `WAITLIST_ENTRIES` permanece con `status = 'active'`.
- La UI no queda en estado inconsistente.

---

## HU-004 — Publicar Evento de Liberación de Asiento por Expiración de Reserva

### Escenario 1: Detección de transición válida

#### CP_HU004_01 — Publicación de evento por expiración de reserva

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que la transición de un asiento del estado `reservado` a `disponible` (por expiración del TTL) genera y publica correctamente un evento `SeatReleased`. |
| **Técnica(s)** | Pruebas de Transición de Estado, Pruebas Basadas en Eventos |
| **Prioridad** | Alta |
| **Precondiciones** | El asiento tiene estado `reserved` en la base de datos. Su reserva temporal tiene un TTL configurado (puede reducirse a segundos en el ambiente de pruebas). |

**Pasos:**

```gherkin
Given existe un asiento con estado "reservado" para el evento "Concierto Sinfónico"
And su reserva temporal tiene un tiempo de vida (TTL) definido
When el tiempo de vida (TTL) de la reserva para el asiento expira sin que se complete la compra
Then el estado del asiento en la base de datos de inventario debe actualizarse a "disponible"
And un evento de negocio del tipo "SeatReleased" debe ser publicado en el topic "seat-released"
And el contenido del evento debe incluir los identificadores del asiento, del evento y de la sección correspondiente
```

**Resultado esperado:**
- El campo `status` del asiento es `'available'` en la BD.
- Se confirma la publicación de un mensaje `SeatReleased` en el topic `seat-released` con los identificadores correctos.

---

### Escenarios 2 y 3: Transiciones no relevantes y compra exitosa

#### CP_HU004_02 — No se genera evento de liberación en una compra exitosa

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que cuando un asiento pasa de `reservado` a `vendido` por una compra exitosa, el sistema NO publica un evento `SeatReleased`. |
| **Técnica(s)** | Pruebas de Transición de Estado (Flujo negativo) |
| **Prioridad** | Alta |
| **Precondiciones** | El asiento tiene estado `reserved`. El TTL de la reserva aún no ha expirado. |

**Pasos:**

```gherkin
Given el asiento se encuentra en estado "reservado" por un usuario
When el usuario completa exitosamente el proceso de pago para esa reserva antes de que expire el TTL
Then el estado del asiento en la base de datos debe actualizarse a "sold"
And el sistema NO debe publicar ningún evento del tipo "SeatReleased" en el topic "seat-released"
```

**Resultado esperado:**
- El campo `status` del asiento es `'sold'` en la BD.
- No se registra ningún mensaje `SeatReleased` en el topic `seat-released`.

---

#### CP_HU004_03 — No se genera evento en transición de disponible a vendido

| Campo | Detalle |
|---|---|
| **Propósito** | Confirmar que la transición `disponible → vendido` no se considera una liberación de inventario y no genera el evento. |
| **Técnica(s)** | Pruebas de Transición de Estado (Flujo negativo) |
| **Prioridad** | Alta |
| **Precondiciones** | El asiento tiene estado `available` en la BD. |

**Pasos:**

```gherkin
Given el asiento se encuentra en estado "disponible"
When un proceso de sistema o una compra directa cambia su estado a "sold"
Then el sistema NO debe publicar un evento del tipo "SeatReleased" en el topic "seat-released"
And el estado final del asiento debe ser "sold"
```

**Resultado esperado:**
- El campo `status` del asiento es `'sold'` en la BD.
- No se registra ningún mensaje `SeatReleased` en el topic `seat-released`.

---

### Flujos de excepción y robustez

#### CP_HU004_04 — Generación de evento en cancelación manual de reserva

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que si una reserva es cancelada por un proceso administrativo (no por expiración de TTL), cambiando el asiento a `disponible`, el sistema también genera el evento `SeatReleased`. |
| **Técnica(s)** | Pruebas de Transición de Estado |
| **Prioridad** | Media |
| **Precondiciones** | El asiento tiene estado `reserved` en la BD. |

**Pasos:**

```gherkin
Given el asiento se encuentra en estado "reservado"
When un administrador o un proceso de sistema cancela la reserva manualmente, liberando el asiento
Then el estado del asiento en la base de datos debe actualizarse a "disponible"
And un evento de negocio del tipo "SeatReleased" debe ser publicado en el topic "seat-released"
```

**Resultado esperado:**
- El campo `status` del asiento es `'available'` en la BD.
- Se confirma la publicación del mensaje `SeatReleased` en el topic `seat-released`.

---

## HU-005 — Procesar Liberación de Asiento y Asignar Oportunidad al Siguiente en la Cola

### Escenario 1: Asignación exitosa al primer usuario en la cola

#### CP_HU005_01 — Asignación de oportunidad al primer usuario en una lista de espera activa

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que el sistema selecciona correctamente al primer usuario elegible (FIFO), actualiza el estado de su suscripción y publica el evento de oportunidad. |
| **Técnica(s)** | Pruebas Basadas en Eventos, Pruebas de Transición de Estado, Validación FIFO |
| **Prioridad** | Alta |
| **Precondiciones** | Existen dos suscripciones activas en `WAITLIST_ENTRIES` para la sección "VIP" del evento "Concierto Sinfónico": UsuarioX con `joined_at = 10:00` y UsuarioY con `joined_at = 10:05`. Se publica un evento `SeatReleased` para ese contexto. |

**Pasos:**

```gherkin
Given el servicio de lista de espera consume un evento "SeatReleased" para la sección "VIP" del evento "Concierto Sinfónico"
And la lista de espera para ese contexto contiene a "UsuarioY" (suscrito a las 10:05) y a "UsuarioX" (suscrito a las 10:00)
And ambas suscripciones están en estado "activa"
When el evento es procesado por el sistema
Then el estado de la suscripción de "UsuarioX" en la base de datos debe cambiar de "activa" a "ofrecida"
And el estado de la suscripción de "UsuarioY" debe permanecer como "activa"
And un nuevo evento "WaitlistOpportunityGranted" debe ser publicado, conteniendo el ID de "UsuarioX", los detalles del evento y sección y un TTL para la oportunidad
```

**Resultado esperado:**
- El registro de "UsuarioX" en `WAITLIST_ENTRIES` tiene `status = 'offered'` y `notified_at` registrado.
- El registro de "UsuarioY" permanece con `status = 'active'`.
- Se confirma la publicación de `WaitlistOpportunityGranted` con los datos correctos en el topic correspondiente.

---

### Escenario 2: No hay usuarios elegibles en la lista de espera

#### CP_HU005_02 — No se realiza ninguna acción si la lista de espera está vacía

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que si no hay usuarios en la lista de espera para el contexto del asiento liberado, el sistema no realiza ninguna acción ni publica eventos innecesarios. |
| **Técnica(s)** | Pruebas de Flujo Negativo |
| **Prioridad** | Alta |
| **Precondiciones** | No existe ninguna suscripción activa en `WAITLIST_ENTRIES` para la sección "General" del evento. Se publica un evento `SeatReleased` para ese contexto. |

**Pasos:**

```gherkin
Given el servicio de lista de espera consume un evento "SeatReleased" para la sección "General" del evento "Concierto Sinfónico"
And no existe ninguna suscripción activa en la lista de espera para ese contexto específico
When el evento es procesado por el sistema
Then no se debe modificar el estado de ninguna suscripción en la base de datos
And no se debe publicar ningún evento del tipo "WaitlistOpportunityGranted"
```

**Resultado esperado:**
- No hay cambios de estado en `WAITLIST_ENTRIES`.
- No se registra ningún mensaje `WaitlistOpportunityGranted` en el topic de notificaciones.

---

### Validación de reglas de negocio

#### CP_HU005_03 — Selección del siguiente usuario activo si el primero en la cola no es elegible

| Campo | Detalle |
|---|---|
| **Propósito** | Validar que el sistema ignora a los usuarios cuyas suscripciones no están en estado `activa` y selecciona correctamente al siguiente usuario elegible en la cola FIFO. |
| **Técnica(s)** | Pruebas de Transición de Estado, Partición de Equivalencia (Estados de suscripción) |
| **Prioridad** | Alta |
| **Precondiciones** | En `WAITLIST_ENTRIES` para la sección "VIP" del evento existen: UsuarioA con `joined_at = 09:00` y `status = 'cancelled'`, y UsuarioB con `joined_at = 09:05` y `status = 'active'`. |

**Pasos:**

```gherkin
Given el servicio consume un evento "SeatReleased" para la sección "VIP" del evento "Concierto Sinfónico"
And la lista de espera contiene a "UsuarioA" (suscrito a las 09:00 con estado "cancelled") y a "UsuarioB" (suscrito a las 09:05 con estado "activa")
When el evento es procesado
Then el sistema debe ignorar a "UsuarioA" y seleccionar a "UsuarioB"
And el estado de la suscripción de "UsuarioB" debe cambiar a "ofrecida"
And el estado de la suscripción de "UsuarioA" debe permanecer como "cancelled"
```

**Resultado esperado:**
- El registro de "UsuarioB" en `WAITLIST_ENTRIES` tiene `status = 'offered'`.
- El registro de "UsuarioA" permanece con `status = 'cancelled'`.
- Se publica `WaitlistOpportunityGranted` con el ID de "UsuarioB".

---

#### CP_HU005_04 — Aislamiento de contexto entre diferentes eventos

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que un evento de liberación de asiento para un evento/sección no afecte a las listas de espera de otros contextos. |
| **Técnica(s)** | Pruebas de Aislamiento de Datos |
| **Prioridad** | Alta |
| **Precondiciones** | Existen suscripciones activas en `WAITLIST_ENTRIES` para el evento "Concierto A", sección "General". La lista de espera para el evento "Concierto B", sección "General" está vacía. Se publica `SeatReleased` para "Concierto B" / sección "General". |

**Pasos:**

```gherkin
Given el servicio consume un evento "SeatReleased" para el evento "Concierto B", sección "General"
And existe una lista de espera con usuarios activos para el evento "Concierto A", sección "General"
And la lista de espera para el evento "Concierto B", sección "General" está vacía
When el evento es procesado
Then el sistema no debe seleccionar a ningún usuario de la lista de espera del evento "Concierto A"
And no se debe publicar ningún evento "WaitlistOpportunityGranted"
```

**Resultado esperado:**
- Las suscripciones del evento "Concierto A" permanecen sin cambios en `WAITLIST_ENTRIES`.
- No se publica ningún `WaitlistOpportunityGranted`.

---

## HU-006 — Enviar Notificación por Email de Oportunidad de Compra

### Escenario 1: Envío de email tras recibir evento de oportunidad

#### CP_HU006_01 — Envío exitoso de notificación a un usuario activo

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que el sistema consume un evento de oportunidad, compone el correo electrónico correctamente y lo entrega al proveedor de servicios para su envío cuando el usuario es elegible. |
| **Técnica(s)** | Pruebas Basadas en Eventos, Caso de Uso (Flujo Principal) |
| **Prioridad** | Alta |
| **Precondiciones** | Se publica un evento `WaitlistOpportunityGranted` con datos del usuario, evento y sección. El usuario tiene cuenta activa y correo verificado. |

**Pasos:**

```gherkin
Given el servicio de notificaciones consume un evento "WaitlistOpportunityGranted" del topic de Kafka
And el evento contiene el userId, eventId, sectionId y un opportunityTTL
And el usuario tiene una cuenta en estado "activa" con el correo electrónico verificado
When el evento es procesado por el servicio de notificaciones
Then el sistema debe componer un correo electrónico dirigido al correo del usuario
And el contenido del correo debe incluir el nombre del evento, la sección y la duración de la oportunidad
And el sistema debe registrar la entrega exitosa de la solicitud de envío al proveedor de servicios de email
```

**Resultado esperado:**
- Se registra un nuevo mail en `EMAIL_NOTIFICATIONS` con `status = 'sent'` y `recipient_email` correcto.
- El SMTP local (Mailhog) captura el email con el contenido correcto.

---

### Escenario 2: Vigencia de la oportunidad

#### CP_HU006_02 — Contenido del email debe especificar la vigencia de la oportunidad

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el correo electrónico generado contenga explícitamente el tiempo límite de la oportunidad. |
| **Técnica(s)** | Pruebas de Contenido, Caso de Uso |
| **Prioridad** | Media |
| **Precondiciones** | Se procesa un evento `WaitlistOpportunityGranted` con un TTL definido. El usuario destinatario tiene cuenta activa. |

**Pasos:**

```gherkin
Given el sistema está procesando un evento "WaitlistOpportunityGranted" con un TTL definido
When se compone el correo electrónico de notificación para el usuario
Then el cuerpo del correo debe contener un texto claro que indique la vigencia, como "Tienes X minutos para completar tu compra."
And el correo debe incluir un enlace único y seguro que dirija al usuario al flujo de compra
```

**Resultado esperado:**
- El email capturado por el SMTP local contiene el texto de vigencia y un enlace único con token.

---

#### CP_HU006_03 — Validación de oportunidad expirada al acceder desde el enlace

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que el sistema valida correctamente la vigencia de la oportunidad cuando el usuario intenta usarla después de que expiró. |
| **Técnica(s)** | Pruebas de Integración, Pruebas de Transición de Estado |
| **Prioridad** | Alta |
| **Precondiciones** | El usuario recibió una notificación con un TTL definido. Han transcurrido más tiempo que el TTL desde el envío. |

**Pasos:**

```gherkin
Given un usuario recibió una notificación de oportunidad con un TTL de X minutos
When el usuario hace clic en el enlace de compra del correo electrónico después de que han transcurrido más tiempo que el TTL
Then el sistema debe validar que la oportunidad ha expirado
And debe mostrar al usuario un mensaje informativo, como "Lo sentimos, tu oportunidad de compra ha expirado."
And no debe permitirle continuar con el proceso de compra
```

**Resultado esperado:**
- El sistema responde con un mensaje de oportunidad expirada.
- No se crea ninguna reserva de asiento.

---

### Escenario 3: No se notifica a usuarios inactivos

#### CP_HU006_04 — No se envía notificación a un usuario con cuenta inactiva

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el sistema descarta el envío de notificaciones si la cuenta del usuario destinatario está inactiva. |
| **Técnica(s)** | Partición de Equivalencia (Estado de cuenta), Pruebas de Flujo Negativo |
| **Prioridad** | Alta |
| **Precondiciones** | Se publica un evento `WaitlistOpportunityGranted` para un usuario. El usuario tiene `status = 'inactive'` en la BD. |

**Pasos:**

```gherkin
Given el servicio de notificaciones consume un evento "WaitlistOpportunityGranted" para un usuario
And la cuenta del usuario se encuentra en estado "inactiva" en la base de datos
When el evento es procesado
Then el sistema NO debe componer ni intentar enviar ningún correo electrónico
And el evento debe ser marcado como procesado o descartado para evitar reintentos de envío
```

**Resultado esperado:**
- No se registra ningún email en `EMAIL_NOTIFICATIONS` para ese usuario.
- El SMTP local (Mailhog) no captura ningún email dirigido a ese usuario.
- El evento queda marcado como procesado.

---

#### CP_HU006_05 — No se envía notificación a un usuario con cuenta suspendida

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el sistema descarta el envío de notificaciones si la cuenta del usuario destinatario está suspendida. |
| **Técnica(s)** | Partición de Equivalencia (Estado de cuenta), Pruebas de Flujo Negativo |
| **Prioridad** | Alta |
| **Precondiciones** | Se publica un evento `WaitlistOpportunityGranted` para un usuario. El usuario tiene `status = 'suspended'` en la BD. |

**Pasos:**

```gherkin
Given el servicio de notificaciones consume un evento "WaitlistOpportunityGranted" para un usuario
And la cuenta del usuario se encuentra en estado "suspendida" en la base de datos
When el evento es procesado
Then el sistema NO debe enviar ningún correo electrónico al usuario
And el evento debe ser gestionado como completado para no generar reintentos
```

**Resultado esperado:**
- No se registra ningún email en `EMAIL_NOTIFICATIONS` para ese usuario.
- El evento queda marcado como completado sin envío.

---

## HU-007 — Validar Oportunidad de Compra y Crear Reserva para Checkout

### Escenarios 1 y 2: Acceso exitoso con oportunidad válida

#### CP_HU007_01 — Acceso y reserva exitosos con una oportunidad válida

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que un usuario que accede con un token de oportunidad válido y vigente es redirigido al checkout con un asiento reservado a su nombre. |
| **Técnica(s)** | Caso de Uso (Flujo Principal), Pruebas de Transición de Estado |
| **Prioridad** | Alta |
| **Precondiciones** | otro usuario reserva el asiento 1 de fila 1 en la sección "VIP" del evento "Concierto Sinfónico". El usuario está autenticado en la plataforma Ticketing. |

**Pasos:**

```gherkin
Given otro usuario reserva el asiento 1 de fila 1 en la sección "VIP" del evento "Concierto Sinfónico"
And el usuario está autenticado en la plataforma Ticketing
And accedo a la página de detalles del evento "Concierto Sinfónico"
When intento interactuar con un asiento reservado de la sección "VIP"
And selecciono la opción para unirme a la lista de espera
And debo visualizar un indicador de confirmación de lista de espera
When la oportunidad de compra se habilita para el usuario
And el usuario selecciona el asiento ofrecido de la sección "VIP"
Then debe visualizarse el panel de oportunidad de compra para el asiento
```

**Resultado esperado:**
- El registro en `OPPORTUNITY_WINDOWS` tiene `status = 'in_progress'` o similar.
- Se crea un registro en `RESERVATIONS` con el TTL estándar de 15 minutos.
- El usuario es redirigido automáticamente a la página de checkout.
- La página de checkout debe mostrar el asiento reservado en mi carrito de compra.

---

### Escenario 3: Acceso fuera del tiempo permitido

#### CP_HU007_02 — Acceso denegado por oportunidad expirada

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el sistema impida el acceso al flujo de compra si la oportunidad del usuario ya ha expirado. |
| **Técnica(s)** | Pruebas de Flujo Negativo, Análisis de Valores Límite (TTL) |
| **Prioridad** | Alta |
| **Precondiciones** | El usuario tiene un token de oportunidad. El TTL de la oportunidad ya ha expirado. |

**Pasos:**

```gherkin
Given accedo a la URL de oportunidad con un token válido
But el tiempo límite (TTL) de mi oportunidad ya ha expirado
When el sistema procesa mi solicitud
Then no se debe crear ninguna reserva de asiento a mi nombre
And no debo ser redirigido a la página de checkout
And en su lugar, debo visualizar una página o mensaje que indique claramente: "Lo sentimos, tu oportunidad de compra ha expirado."
```

**Resultado esperado:**
- No se crea ningún registro en `RESERVATIONS`.
- El usuario ve el mensaje de oportunidad expirada.

---

### Escenario 4: Consumo de la oportunidad

#### CP_HU007_03 — Impedir reutilización de una oportunidad ya en progreso

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que una vez que una oportunidad está en estado `in_progress`, el mismo token no puede ser usado nuevamente. |
| **Técnica(s)** | Pruebas de Transición de Estado, Pruebas de Seguridad |
| **Prioridad** | Alta |
| **Precondiciones** | El usuario ya accedió exitosamente a su URL de oportunidad. La oportunidad tiene `status = 'in_progress'`. |

**Pasos:**

```gherkin
Given ya he accedido exitosamente a la URL de oportunidad y mi oportunidad está en estado "in_progress"
When intento acceder a la misma URL de oportunidad por segunda vez (por ejemplo, desde otro navegador o pestaña)
Then el sistema debe validar que la oportunidad ya fue reclamada
And debe mostrar un mensaje informativo como: "Esta oportunidad de compra ya está siendo procesada."
And no debe crear una segunda reserva ni redirigirme nuevamente al checkout
```

**Resultado esperado:**
- No se crea una segunda reserva en `RESERVATIONS`.
- Se muestra el mensaje informativo correspondiente.

---

### Escenario 5: Liberación tras expiración

#### CP_HU007_04 — Liberación automática de la oportunidad por expiración del TTL

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el sistema actualiza el estado de una oportunidad a `expired` y desencadena el proceso para notificar al siguiente usuario en la cola cuando el TTL vence sin ser utilizada. |
| **Técnica(s)** | Pruebas de Sistema (Procesos Temporizados), Pruebas de Transición de Estado |
| **Prioridad** | Alta |
| **Precondiciones** | Una oportunidad fue asignada a un usuario con `status = 'offered'` y TTL definido. El TTL puede configurarse en segundos en el ambiente de pruebas. Existe al menos un usuario más en la cola para ese contexto. |

**Pasos:**

```gherkin
Given una oportunidad de compra con un TTL de X minutos fue asignada a un usuario y su estado es "offered"
When transcurren los X minutos sin que el usuario acceda a la URL de oportunidad
Then el estado de esa oportunidad en la base de datos debe cambiar automáticamente a "expired"
And el sistema debe iniciar el proceso para seleccionar al siguiente usuario elegible en la lista de espera para ese mismo contexto
```

**Resultado esperado:**
- El registro de la oportunidad pasa a `status = 'expired'` en la BD.
- Se publica un nuevo evento de selección o el Waitlist Service procesa la reasignación.
- El siguiente usuario en la cola recibe su oportunidad.

---

## Resumen de Cobertura

| Historia | Total de CPs | Alta | Media | Baja |
|---|:---:|:---:|:---:|:---:|
| HU-001 | 5 | 3 | 1 | 1 |
| HU-002 | 4 | 3 | 1 | 0 |
| HU-003 | 5 | 2 | 2 | 1 |
| HU-004 | 4 | 3 | 1 | 0 |
| HU-005 | 4 | 4 | 0 | 0 |
| HU-006 | 5 | 4 | 1 | 0 |
| HU-007 | 4 | 4 | 0 | 0 |
| **TOTAL** | **31** | **23** | **6** | **2** |

---

*Este documento debe mantenerse sincronizado con la Especificación de Requisitos y el Plan de Pruebas v1.0. Cualquier cambio en los criterios de aceptación de una historia implica la revisión de los casos de prueba correspondientes.*