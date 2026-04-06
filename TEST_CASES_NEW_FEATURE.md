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

### Escenario 1 y 3: Oferta de registro, registro exitoso y confirmación al usuario

#### CP_HU001_01 — Registro exitoso en lista de espera y visualización de confirmación

| Campo | Detalle |
|---|---|
| **Propósito** | Validar el flujo completo y exitoso de registro en la lista de espera para un usuario autenticado que encuentra una sección agotada, asegurando que se muestre la opción, se procese el registro y se presente el mensaje de confirmación. |
| **Técnica(s)** | Caso de Uso, Partición de Equivalencia |
| **Prioridad** | Alta |
| **Precondiciones** | Usuario autenticado en la plataforma. El evento "Concierto Sinfónico" existe. La sección "Platea Central" tiene 0 asientos disponibles. El usuario NO tiene una suscripción activa previa para ese contexto. |

**Pasos:**

```gherkin
Dado que soy un usuario autenticado en la plataforma Ticketing
Y accedo a la página de detalles del evento "Concierto Sinfónico"
  donde la sección "Platea Central" no tiene asientos disponibles
Cuando intento interactuar con la sección "Platea Central"
Entonces el sistema debe mostrar una opción visible y clara para
  "Unirse a la lista de espera", asociada específicamente a esa sección y evento

Cuando selecciono la opción para unirme y confirmo mi registro
Entonces el sistema debe registrar mi suscripción en la base de datos,
  asociándola a mi identificador de usuario, al evento "Concierto Sinfónico"
  y a la sección "Platea Central"
Y debo visualizar en pantalla un mensaje de confirmación, como
  "¡Te has unido a la lista de espera! Te notificaremos si se liberan asientos."
```

**Resultado esperado:**
- La opción "Unirse a la lista de espera" es visible en pantalla para la sección "Platea Central".
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
| **Precondiciones** | Usuario autenticado. El evento "Obra de Teatro Clásico" existe. La sección "Balcón Izquierdo" tiene al menos 1 asiento disponible. |

**Pasos:**

```gherkin
Dado que soy un usuario autenticado en la plataforma Ticketing
Y accedo a la página de detalles del evento "Obra de Teatro Clásico"
Y la sección "Balcón Izquierdo" tiene al menos un asiento disponible
Cuando visualizo el mapa de asientos de la sección "Balcón Izquierdo"
Entonces el sistema no debe mostrar ninguna opción para unirse a una lista de espera
Y debo poder proceder con el flujo normal de selección y compra de asientos
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
| **Precondiciones** | Usuario autenticado. Existe una suscripción activa (`status = 'active'`) en `WAITLIST_ENTRIES` para ese usuario, el evento "Concierto Sinfónico" y la sección "Platea Central". |

**Pasos:**

```gherkin
Dado que soy un usuario autenticado que ya se encuentra registrado
  en la lista de espera para la sección "Platea Central"
  del evento "Concierto Sinfónico"
Cuando accedo nuevamente a la página de detalles de dicho evento y sección
Entonces el sistema no debe mostrar la opción para "Unirse a la lista de espera"
Y en su lugar, debe mostrar un indicador visual o mensaje que confirme
  mi estado actual, como "Ya estás en la lista de espera"
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
| **Precondiciones** | Usuario no autenticado (sesión de invitado). La sección "General" del evento "Festival de Jazz" tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Dado que soy un usuario visitante (no autenticado) en la plataforma
Y accedo a la página del evento "Festival de Jazz",
  cuya sección "General" está agotada
Cuando selecciono la opción visible para unirme a la lista de espera
Entonces el sistema debe redirigirme a la página de inicio de sesión
  o solicitar la autenticación a través de un modal
Y el registro en la lista de espera no debe completarse
  hasta que mi autenticación sea exitosa
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
| **Precondiciones** | Usuario autenticado. Existe suscripción activa para la sección "Platea" del evento "Concierto de Rock". La sección "VIP" del mismo evento tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Dado que soy un usuario autenticado y ya estoy en la lista de espera
  para la sección "Platea" del evento "Concierto de Rock"
Y la sección "VIP" del mismo evento también se encuentra agotada
Cuando accedo a la sección "VIP" y selecciono la opción para unirme
  a su respectiva lista de espera
Y confirmo mi registro
Entonces el sistema debe registrar exitosamente mi nueva suscripción
  para la sección "VIP"
Y mi cuenta debe mantener activas ambas suscripciones de forma independiente:
  una para "Platea" y otra para "VIP" del "Concierto de Rock"
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
| **Precondiciones** | Usuario "ClienteA" autenticado. Existe suscripción activa en `WAITLIST_ENTRIES` para "ClienteA", sección "VIP" del "Evento A". |

**Pasos:**

```gherkin
Dado que soy un usuario autenticado con una suscripción activa
  en la lista de espera para la sección "VIP" del "Evento A"
Cuando navego a la página de detalles del "Evento A"
  y observo la sección "VIP"
Entonces el sistema no debe mostrar la opción para "Unirme a la lista de espera"
Y en su lugar, debo visualizar un componente informativo claro,
  como un banner o una alerta
Y este componente debe contener el texto exacto:
  "Ya estás en la lista de espera para esta sección.
  Te notificaremos si se liberan asientos."
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
| **Precondiciones** | "ClienteA" tiene suscripción activa para sección "VIP" del "Evento A". "ClienteC" está autenticado en sesión diferente y no tiene suscripción para ese contexto. La sección "VIP" del "Evento A" tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Dado que el usuario "ClienteA" tiene una suscripción activa
  para la sección "VIP" del "Evento A"
Y yo soy el usuario "ClienteC", estoy autenticado en una sesión diferente
  y no tengo suscripción para ese contexto
Cuando navego a la página de detalles del "Evento A"
  y observo la sección "VIP", que está agotada
Entonces el sistema no debe mostrarme ningún banner informativo
  sobre la suscripción de "ClienteA"
Y el sistema debe mostrarme la opción para "Unirme a la lista de espera"
```

**Resultado esperado:**
- "ClienteC" no ve el banner de suscripción activa.
- "ClienteC" ve el botón "Unirme a la lista de espera".

---

### Escenario 3: Usuario no autenticado (invitado)

#### CP_HU002_03 — Comportamiento para usuario no autenticado

| Campo | Detalle |
|---|---|
| **Propósito** | Validar que un usuario no autenticado no ve ningún estado de suscripción y se le presenta la opción de unirse a la lista. |
| **Técnica(s)** | Partición de Equivalencia (Rol de usuario: invitado) |
| **Prioridad** | Media |
| **Precondiciones** | Sesión de invitado (sin autenticación). La sección "VIP" del "Evento A" tiene 0 asientos disponibles. |

**Pasos:**

```gherkin
Dado que soy un usuario visitante (no autenticado) en la plataforma
Y la sección "VIP" del "Evento A" no tiene asientos disponibles
Cuando navego a la página de detalles del "Evento A"
  y observo la sección "VIP"
Entonces el sistema no debe mostrar ningún banner informativo
  relacionado con suscripciones
Y el sistema debe mostrar la opción para "Unirme a la lista de espera"
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
| **Precondiciones** | Usuario "ClienteA" autenticado. Su suscripción para la sección "VIP" del "Evento A" tiene `status = 'expired'` en `WAITLIST_ENTRIES`. La sección "VIP" continúa con 0 asientos disponibles. |

**Pasos:**

```gherkin
Dado que soy el usuario "ClienteA" y estoy autenticado
Y mi suscripción previa para la sección "VIP" del "Evento A"
  ahora tiene un estado de "expirada"
Y la sección "VIP" del "Evento A" continúa sin asientos disponibles
Cuando navego a la página de detalles del "Evento A"
  y observo la sección "VIP"
Entonces el sistema no debe mostrar el banner informativo
  de suscripción activa
Y el sistema debe mostrarme nuevamente la opción para
  "Unirme a la lista de espera"
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
| **Precondiciones** | Usuario autenticado en la página del "Evento A". El usuario tiene una suscripción activa, por lo que el banner informativo está visible. |

**Pasos:**

```gherkin
Dado que soy un usuario autenticado y estoy en la página del "Evento A"
Y tengo una suscripción activa a la lista de espera,
  por lo que visualizo el banner informativo "Ya estás en la lista de espera..."
Cuando hago clic en el enlace o botón "Cancelar suscripción"
Entonces el sistema debe mostrar un modal de confirmación
Y el título del modal debe ser "Confirmar Cancelación"
Y el texto del cuerpo del modal debe ser "¿Estás seguro de que quieres
  abandonar la lista de espera? Perderás tu lugar actual."
Y el modal debe contener un botón con el texto "Confirmar"
  y otro con el texto "Cerrar"
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
| **Precondiciones** | El modal de confirmación de cancelación está visible. |

**Pasos:**

```gherkin
Dado que he iniciado el proceso de cancelación
  y el modal de confirmación está visible
Cuando hago clic en el botón "Confirmar"
Entonces el estado de mi suscripción en la base de datos
  debe cambiar a "cancelled"
Y debo visualizar una notificación de tipo "toast" con el mensaje exacto:
  "Has cancelado tu suscripción a la lista de espera."
Y el banner informativo de suscripción activa debe desaparecer de la página
Y el botón para "Unirme a la lista de espera" debe volver
  a estar visible y funcional
```

**Resultado esperado:**
- El registro en `WAITLIST_ENTRIES` tiene `status = 'cancelled'`.
- El toast de éxito se muestra con el texto exacto.
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
Dado que he iniciado el proceso de cancelación
  y el modal de confirmación está visible
Cuando hago clic en el botón "Cerrar"
Entonces el modal de confirmación debe cerrarse
Y mi suscripción a la lista de espera debe permanecer
  en estado "activa" en el sistema
Y la interfaz de la página del evento debe mantenerse sin cambios,
  mostrando el banner de suscripción activa
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
Dado que he iniciado el proceso de cancelación
  y el modal de confirmación está visible
Cuando presiono la tecla "Escape" en mi teclado para cerrar el modal
Entonces el modal de confirmación debe cerrarse
Y mi suscripción a la lista de espera debe permanecer en estado "activa"
Y la interfaz de la página del evento no debe sufrir ninguna modificación
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
Dado que he iniciado el proceso de cancelación
  y el modal de confirmación está visible
Y el servicio de backend fallará al procesar la solicitud de cancelación
Cuando hago clic en el botón "Confirmar"
Entonces el modal de confirmación debe cerrarse
Y el sistema debe mostrar una notificación de error, como
  "No se pudo cancelar la suscripción. Por favor, inténtalo de nuevo."
Y mi suscripción debe permanecer en estado "activa"
Y la interfaz de la página del evento debe seguir mostrando
  el banner de suscripción activa
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
| **Precondiciones** | El asiento "A-12" del "Evento X" tiene `status = 'reserved'` en la base de datos. Su reserva temporal tiene un TTL configurado (puede reducirse a segundos en el ambiente de pruebas). |

**Pasos:**

```gherkin
Dado que existe un asiento con ID "A-12" en estado "reservado"
  para el "Evento X"
Y su reserva temporal tiene un tiempo de vida (TTL) definido
Cuando el tiempo de vida (TTL) de la reserva para el asiento "A-12"
  expira sin que se complete la compra
Entonces el estado del asiento "A-12" en la base de datos de inventario
  debe actualizarse a "disponible"
Y un evento de negocio del tipo "SeatReleased" debe ser publicado
  en el topic "inventory-events"
Y el contenido del evento debe incluir los identificadores del asiento ("A-12"),
  del evento ("Evento X") y de la sección correspondiente
```

**Resultado esperado:**
- El campo `status` del asiento "A-12" es `'available'` en la BD.
- Se confirma la publicación de un mensaje `SeatReleased` en el topic `inventory-events` con los identificadores correctos.

---

### Escenarios 2 y 3: Transiciones no relevantes y compra exitosa

#### CP_HU004_02 — No se genera evento de liberación en una compra exitosa

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que cuando un asiento pasa de `reservado` a `vendido` por una compra exitosa, el sistema NO publica un evento `SeatReleased`. |
| **Técnica(s)** | Pruebas de Transición de Estado (Flujo negativo) |
| **Prioridad** | Alta |
| **Precondiciones** | El asiento "B-05" tiene `status = 'reserved'`. El TTL de la reserva aún no ha expirado. |

**Pasos:**

```gherkin
Dado que el asiento "B-05" se encuentra en estado "reservado" por un usuario
Cuando el usuario completa exitosamente el proceso de pago
  para esa reserva antes de que expire el TTL
Entonces el estado del asiento "B-05" en la base de datos
  debe actualizarse a "sold"
Y el sistema NO debe publicar ningún evento del tipo "SeatReleased"
  en el topic "inventory-events"
```

**Resultado esperado:**
- El campo `status` del asiento "B-05" es `'sold'` en la BD.
- No se registra ningún mensaje `SeatReleased` en el topic `inventory-events`.

---

#### CP_HU004_03 — No se genera evento en transición de disponible a vendido

| Campo | Detalle |
|---|---|
| **Propósito** | Confirmar que la transición `disponible → vendido` no se considera una liberación de inventario y no genera el evento. |
| **Técnica(s)** | Pruebas de Transición de Estado (Flujo negativo) |
| **Prioridad** | Alta |
| **Precondiciones** | El asiento "C-10" tiene `status = 'available'` en la BD. |

**Pasos:**

```gherkin
Dado que el asiento "C-10" se encuentra en estado "disponible"
Cuando un proceso de sistema o una compra directa
  cambia su estado a "sold"
Entonces el sistema NO debe publicar un evento del tipo "SeatReleased"
  en el topic "inventory-events"
Y el estado final del asiento "C-10" debe ser "sold"
```

**Resultado esperado:**
- El campo `status` del asiento "C-10" es `'sold'` en la BD.
- No se registra ningún mensaje `SeatReleased` en el topic `inventory-events`.

---

### Flujos de excepción y robustez

#### CP_HU004_04 — Generación de evento en cancelación manual de reserva

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que si una reserva es cancelada por un proceso administrativo (no por expiración de TTL), cambiando el asiento a `disponible`, el sistema también genera el evento `SeatReleased`. |
| **Técnica(s)** | Pruebas de Transición de Estado |
| **Prioridad** | Media |
| **Precondiciones** | El asiento "D-08" tiene `status = 'reserved'` en la BD. |

**Pasos:**

```gherkin
Dado que el asiento "D-08" se encuentra en estado "reservado"
Cuando un administrador o un proceso de sistema
  cancela la reserva manualmente, liberando el asiento
Entonces el estado del asiento "D-08" en la base de datos
  debe actualizarse a "disponible"
Y un evento de negocio del tipo "SeatReleased" debe ser publicado
  en el topic "inventory-events"
```

**Resultado esperado:**
- El campo `status` del asiento "D-08" es `'available'` en la BD.
- Se confirma la publicación del mensaje `SeatReleased` en el topic `inventory-events`.

---

## HU-005 — Procesar Liberación de Asiento y Asignar Oportunidad al Siguiente en la Cola

### Escenario 1: Asignación exitosa al primer usuario en la cola

#### CP_HU005_01 — Asignación de oportunidad al primer usuario en una lista de espera activa

| Campo | Detalle |
|---|---|
| **Propósito** | Verificar que el sistema selecciona correctamente al primer usuario elegible (FIFO), actualiza el estado de su suscripción y publica el evento de oportunidad. |
| **Técnica(s)** | Pruebas Basadas en Eventos, Pruebas de Transición de Estado, Validación FIFO |
| **Prioridad** | Alta |
| **Precondiciones** | Existen dos suscripciones activas en `WAITLIST_ENTRIES` para la sección "VIP" del "Evento A": "UsuarioX" con `joined_at = 10:00` y "UsuarioY" con `joined_at = 10:05`. Se publica un evento `SeatReleased` para ese contexto en el topic `inventory-events`. |

**Pasos:**

```gherkin
Dado que el servicio de lista de espera consume un evento "SeatReleased"
  para la sección "VIP" del "Evento A"
Y la lista de espera para ese contexto contiene a "UsuarioY"
  (suscrito a las 10:05) y a "UsuarioX" (suscrito a las 10:00)
Y ambas suscripciones están en estado "activa"
Cuando el evento es procesado por el sistema
Entonces el estado de la suscripción de "UsuarioX" en la base de datos
  debe cambiar de "activa" a "ofrecida"
Y el estado de la suscripción de "UsuarioY" debe permanecer como "activa"
Y un nuevo evento "WaitlistOpportunityGranted" debe ser publicado,
  conteniendo el ID de "UsuarioX", los detalles del "Evento A" / "Sección VIP"
  y un TTL para la oportunidad
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
| **Precondiciones** | No existe ninguna suscripción activa en `WAITLIST_ENTRIES` para la sección "General" del "Evento B". Se publica un evento `SeatReleased` para ese contexto. |

**Pasos:**

```gherkin
Dado que el servicio de lista de espera consume un evento "SeatReleased"
  para la sección "General" del "Evento B"
Y no existe ninguna suscripción activa en la lista de espera
  para ese contexto específico
Cuando el evento es procesado por el sistema
Entonces no se debe modificar el estado de ninguna suscripción
  en la base de datos
Y no se debe publicar ningún evento del tipo "WaitlistOpportunityGranted"
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
| **Precondiciones** | En `WAITLIST_ENTRIES` para la sección "Platea" del "Evento C" existen: "UsuarioA" con `joined_at = 09:00` y `status = 'cancelled'`, y "UsuarioB" con `joined_at = 09:05` y `status = 'active'`. |

**Pasos:**

```gherkin
Dado que el servicio consume un evento "SeatReleased"
  para la sección "Platea" del "Evento C"
Y la lista de espera contiene a "UsuarioA"
  (suscrito a las 09:00 con estado "cancelled")
  y a "UsuarioB" (suscrito a las 09:05 con estado "activa")
Cuando el evento es procesado
Entonces el sistema debe ignorar a "UsuarioA" y seleccionar a "UsuarioB"
Y el estado de la suscripción de "UsuarioB" debe cambiar a "ofrecida"
Y el estado de la suscripción de "UsuarioA" debe permanecer como "cancelled"
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
| **Precondiciones** | Existen suscripciones activas en `WAITLIST_ENTRIES` para el "Evento Y", sección "General". La lista de espera para el "Evento Z", sección "General" está vacía. Se publica `SeatReleased` para "Evento Z" / sección "General". |

**Pasos:**

```gherkin
Dado que el servicio consume un evento "SeatReleased"
  para el "Evento Z", sección "General"
Y existe una lista de espera con usuarios activos
  para el "Evento Y", sección "General"
Y la lista de espera para el "Evento Z", sección "General" está vacía
Cuando el evento es procesado
Entonces el sistema no debe seleccionar a ningún usuario
  de la lista de espera del "Evento Y"
Y no se debe publicar ningún evento "WaitlistOpportunityGranted"
```

**Resultado esperado:**
- Las suscripciones del "Evento Y" permanecen sin cambios en `WAITLIST_ENTRIES`.
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
| **Precondiciones** | Se publica un evento `WaitlistOpportunityGranted` con `userId = "user123"`, `eventId = "EventoA"`, `sectionId = "VIP"` y `opportunityTTL = "10 minutos"` en el topic de Kafka. El usuario "user123" tiene `status = 'active'` y correo `"cliente@email.com"` verificado. |

**Pasos:**

```gherkin
Dado que el servicio de notificaciones consume un evento
  "WaitlistOpportunityGranted" del topic de Kafka
Y el evento contiene el userId "user123", eventId "EventoA",
  sectionId "VIP" y un opportunityTTL de "10 minutos"
Y el usuario "user123" tiene una cuenta en estado "activa"
  con el correo electrónico verificado "cliente@email.com"
Cuando el evento es procesado por el servicio de notificaciones
Entonces el sistema debe componer un correo electrónico
  dirigido a "cliente@email.com"
Y el contenido del correo debe incluir el nombre del evento,
  la sección y la duración de la oportunidad ("10 minutos")
Y el sistema debe registrar la entrega exitosa de la solicitud
  de envío al proveedor de servicios de email
```

**Resultado esperado:**
- Se registra un nuevo mail en `EMAIL_NOTIFICATIONS` con `status = 'sent'` y `recipient_email = 'cliente@email.com'`.
- El SMTP local (Mailhog) captura el email con el contenido correcto.

---

### Escenario 2: Vigencia de la oportunidad

#### CP_HU006_02 — Contenido del email debe especificar la vigencia de la oportunidad

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el correo electrónico generado contenga explícitamente el tiempo límite de la oportunidad. |
| **Técnica(s)** | Pruebas de Contenido, Caso de Uso |
| **Prioridad** | Media |
| **Precondiciones** | Se procesa un evento `WaitlistOpportunityGranted` con `opportunityTTL = "15 minutos"`. El usuario destinatario tiene cuenta activa. |

**Pasos:**

```gherkin
Dado que el sistema está procesando un evento "WaitlistOpportunityGranted"
  con un TTL de "15 minutos"
Cuando se compone el correo electrónico de notificación para el usuario
Entonces el cuerpo del correo debe contener un texto claro que indique
  la vigencia, como "Tienes 15 minutos para completar tu compra."
Y el correo debe incluir un enlace único y seguro
  que dirija al usuario al flujo de compra
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
| **Precondiciones** | El usuario recibió una notificación con un TTL de 10 minutos. Han transcurrido más de 10 minutos desde el envío. |

**Pasos:**

```gherkin
Dado que un usuario recibió una notificación de oportunidad
  con un TTL de 10 minutos
Cuando el usuario hace clic en el enlace de compra del correo electrónico
  después de que han transcurrido 12 minutos
Entonces el sistema debe validar que la oportunidad ha expirado
Y debe mostrar al usuario un mensaje informativo, como
  "Lo sentimos, tu oportunidad de compra ha expirado."
Y no debe permitirle continuar con el proceso de compra
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
| **Precondiciones** | Se publica un evento `WaitlistOpportunityGranted` para `userId = "user456"`. El usuario "user456" tiene `status = 'inactive'` en la BD. |

**Pasos:**

```gherkin
Dado que el servicio de notificaciones consume un evento
  "WaitlistOpportunityGranted" para el userId "user456"
Y la cuenta del usuario "user456" se encuentra en estado "inactiva"
  en la base de datos
Cuando el evento es procesado
Entonces el sistema NO debe componer ni intentar enviar
  ningún correo electrónico
Y el evento debe ser marcado como procesado o descartado
  para evitar reintentos de envío
```

**Resultado esperado:**
- No se registra ningún email en `EMAIL_NOTIFICATIONS` para "user456".
- El SMTP local (Mailhog) no captura ningún email dirigido a ese usuario.
- El evento queda marcado como procesado.

---

#### CP_HU006_05 — No se envía notificación a un usuario con cuenta suspendida

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el sistema descarta el envío de notificaciones si la cuenta del usuario destinatario está suspendida. |
| **Técnica(s)** | Partición de Equivalencia (Estado de cuenta), Pruebas de Flujo Negativo |
| **Prioridad** | Alta |
| **Precondiciones** | Se publica un evento `WaitlistOpportunityGranted` para `userId = "user789"`. El usuario "user789" tiene `status = 'suspended'` en la BD. |

**Pasos:**

```gherkin
Dado que el servicio de notificaciones consume un evento
  "WaitlistOpportunityGranted" para el userId "user789"
Y la cuenta del usuario "user789" se encuentra en estado "suspendida"
Cuando el evento es procesado
Entonces el sistema NO debe enviar ningún correo electrónico al usuario
Y el evento debe ser gestionado como completado
  para no generar reintentos
```

**Resultado esperado:**
- No se registra ningún email en `EMAIL_NOTIFICATIONS` para "user789".
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
| **Precondiciones** | El usuario tiene un token de oportunidad válido. La oportunidad asociada al token tiene `status = 'offered'` y el TTL no ha expirado. El asiento correspondiente está disponible. |

**Pasos:**

```gherkin
Dado que soy un usuario que ha recibido una oportunidad de compra
  y accedo a la URL con un token válido
Y la oportunidad asociada a mi token está en estado "offered"
  y su TTL no ha expirado
Cuando el sistema procesa mi solicitud de acceso
Entonces el estado de mi oportunidad en la base de datos
  debe cambiar de "offered" a "in_progress"
Y el sistema debe crear una nueva reserva temporal para el asiento
  correspondiente, con el TTL estándar de 15 minutos
Y debo ser redirigido automáticamente a la página de checkout
Y la página de checkout debe mostrar el asiento reservado
  en mi carrito de compra
```

**Resultado esperado:**
- El registro en `NOTIFICATION_TOKENS` tiene `is_used = true` o la oportunidad pasa a `in_progress`.
- Se crea un registro en `RESERVATIONS` con `status = 'NOTIFIED_PENDING'` y `expires_at = NOW() + 15 min`.
- El usuario es redirigido a `/checkout` con el asiento en carrito.

---

### Escenario 3: Acceso fuera del tiempo permitido

#### CP_HU007_02 — Acceso denegado por oportunidad expirada

| Campo | Detalle |
|---|---|
| **Propósito** | Asegurar que el sistema impida el acceso al flujo de compra si la oportunidad del usuario ya ha expirado. |
| **Técnica(s)** | Pruebas de Flujo Negativo, Análisis de Valores Límite (TTL) |
| **Prioridad** | Alta |
| **Precondiciones** | El usuario tiene un token de oportunidad. El TTL de la oportunidad ya ha expirado (`expires_at < NOW()`). |

**Pasos:**

```gherkin
Dado que accedo a la URL de oportunidad con un token válido
Pero el tiempo límite (TTL) de mi oportunidad ya ha expirado
Cuando el sistema procesa mi solicitud
Entonces no se debe crear ninguna reserva de asiento a mi nombre
Y no debo ser redirigido a la página de checkout
Y en su lugar, debo visualizar una página o mensaje que indique claramente:
  "Lo sentimos, tu oportunidad de compra ha expirado."
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
Dado que ya he accedido exitosamente a la URL de oportunidad
  y mi oportunidad está en estado "in_progress"
Cuando intento acceder a la misma URL de oportunidad por segunda vez
  (por ejemplo, desde otro navegador o pestaña)
Entonces el sistema debe validar que la oportunidad ya fue reclamada
Y debe mostrar un mensaje informativo como:
  "Esta oportunidad de compra ya está siendo procesada."
Y no debe crear una segunda reserva
  ni redirigirme nuevamente al checkout
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
| **Precondiciones** | Una oportunidad fue asignada a un usuario con `status = 'offered'` y TTL de 10 minutos. El TTL puede configurarse en segundos en el ambiente de pruebas. Existe al menos un usuario más en la cola para ese contexto. |

**Pasos:**

```gherkin
Dado que una oportunidad de compra con un TTL de 10 minutos
  fue asignada a un usuario y su estado es "offered"
Cuando transcurren los 10 minutos sin que el usuario
  acceda a la URL de oportunidad
Entonces el estado de esa oportunidad en la base de datos
  debe cambiar automáticamente a "expired"
Y el sistema debe iniciar el proceso para seleccionar
  al siguiente usuario elegible en la lista de espera
  para ese mismo contexto
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