# Estrategia de Pruebas: Speckit Ticketing MVP
**Autor:** QA Senior  
**Versión:** 2.0  
**Fecha:** 2026-03-08

## 1. Misión de QA y Enfoque Estratégico
Nuestra misión es garantizar la resiliencia del flujo de compra bajo alta concurrencia. La estrategia se basa en el **QA Semaphore**, priorizando la estabilidad de los microservicios en un ecosistema de Arquitectura Hexagonal.

### 1.1 Alineación con los 7 Principios de QA
1. **Las pruebas muestran la presencia de errores:** Diseñamos tests para encontrar fallos en la persistencia y bloqueos de Redis.
2. **Pruebas tempranas (Shift-Left):** Nuestra cultura de ingeniería desplaza las pruebas lo más a la izquierda posible en el ciclo de vida (SDLC), detectando errores en la fase de diseño/dominio antes de que lleguen a la infraestructura.
3. **Agrupamiento de defectos:** Foco intensivo en `Inventory` y `Payment`.
4. **Dependencia del contexto:** No usamos los mismos tests para `Catalog` (read-heavy) que para `Inventory` (write-heavy/concurrencia).
5. **Paradoja del Pesticida:** Implementamos variabilidad en los datos de entrada para evitar que los tests se vuelvan obsoletos.

---

## 2. Cultura Shift-Left en el Pipeline

El enfoque **Shift-Left** no es solo una fase, sino una serie de capas de protección que se ejecutan antes del despliegue:

### 2.1 Pre-Commit / Local Development
- **Unit Testing:** Ejecución inmediata de la lógica de dominio.
- **Validación de OpenAPI:** Validación local de que el código coincide con el contrato `/contracts/openapi/`.

### 2.2 Continuous Integration (CI) - El Gatekeeper
Nuestro pipeline implementa el Shift-Left mediante:
- **Análisis Estático (SonarCloud):** Detección de `code smells` y deuda técnica en el momento del Pull Request.
- **Escaneo de Seguridad (Trivy):** Identificación de vulnerabilidades en librerías ANTES de generar la imagen final de Docker.
- **Feedback Loop Rápido:** Si una prueba falla en la capa de integración de componentes, el desarrollador recibe la notificación en minutos, no días.

---

## 3. Pirámide de Pruebas (Test Pyramid)

Nuestra pirámide está diseñada para maximizar el ROI de las pruebas y la eficiencia del CI:

```mermaid
graph TD
    subgraph Pyramid
    E2E[5% - System Verification: Calidad + E2E Unificado]
    INT_SYS[10% - Pruebas de Sistema: Kafka Event-Flow]
    INT_COMP[15% - Integración de Componentes: Testcontainers/Repo]
    UNIT[70% - Unitarias: Domain/Application Logic]
    end
    style UNIT fill:#4CAF50,stroke:#333
    style INT_COMP fill:#FFC107,stroke:#333
    style INT_SYS fill:#FF9800,stroke:#333
    style E2E fill:#F44336,stroke:#333
```

- **Pruebas Unitarias (Caja Blanca):** Validamos la lógica interna de los Handlers y Entidades. Usamos **Mocks** para aislar dependencias.
- **Integración de Componentes (Component Integration):** Validamos la comunicación entre el código y su base de datos/cache real usando **Testcontainers**.
- **Pruebas de Sistema (System Integration):** Validamos el flujo asíncrono entre servicios vía **Kafka** (ej: `payment-succeeded` -> `ticket-issued`).
- **Verificación de Sistema (Unified System Check):** Un solo paso de orquestación en CI que valida la infraestructura (Health) y el flujo de negocio (E2E) sobre la misma instancia de Docker Compose para optimizar tiempos.

---

## 4. Clasificación Real de Nuestras Pruebas: ¿Integración o Unitarias?

Como **QA Senior**, es vital distinguir entre lo que *parece* una prueba de integración y lo que *realmente* valida la infraestructura. Tras auditar los factories (`InventoryApiFactory`, `OrderingApiFactory`), hemos identificado la siguiente categorización:

### 4.1 Pruebas de Integración "Shallow" (Componente / API)
Muchos de nuestros tests marcados como "Integration" se clasifican técnicamente como **Component Tests** o **API Functional Tests**. 
- **¿Por qué NO son integración pura?** Porque utilizan `UseInMemoryDatabase`. Esto ignora las restricciones de base de datos reales (Postgres), los drivers y las migraciones.
- **¿Qué validan?** El pipeline de ASP.NET Core (Request -> Middleware -> Routing -> Controller).
- **Contextualización:** Son pruebas de **Caja Gris**, ya que conocen la infraestructura mínima pero no la ejercitan de forma real.

### 4.2 Pruebas de Integración "Deep" (Infraestructura Real)
Para que una prueba sea considerada de **Integración Real** en nuestra estrategia, debe cumplir:
- **Testcontainers:** Levantar una instancia efímera de Postgres/Redis/Kafka en Docker.
- **Persistencia de Esquema:** Validar que los esquemas `bc_*` y sus `constraints` (Unique, Foreign Key) funcionen correctamente.
- **Conectividad:** Probar que el microservicio puede comunicarse con el broker de mensajes real.

### 4.3 Diferencia Técnica por Capas y Estrategia de Validación
| Capa | Sujeto de Prueba | ¿Qué Validamos? (Business) | ¿Qué Verificamos? (Technical) | Herramienta Clave |
| :--- | :--- | :--- | :--- | :--- |
| **Unitarias (Domain)** | Lógica Pura, Entidades, Handlers | Reglas de negocio críticas (ej: stock < 0). | Invariantes del modelo y lógica determinista. | xUnit + Moq |
| **Integración de Componentes** | API Controllers + Pipeline Middleware | Contratos de API (JSON), ruteo y códigos HTTP. | Orquestación interna: Controller -> Handler -> Repo. | `WebApplicationFactory` + In-Memory |
| **Integración de Infraestructura** | Persistence (EF Core) + Event Bus | Persistencia correcta de datos complejos. | Conectividad real y Constraints de DB (Unique/FK). | Testcontainers |
| **E2E / Sistema** | Flujo Transversal (HU-P1) | Cumplimiento de la Historia de Usuario completa. | Coreografía de eventos entre microservicios (Kafka). | Docker Compose + Scripts K8s |

### 4.4 Análisis de Nuestra Implementación Actual
Tras auditar el directorio `tests/`, es fundamental aclarar el estado real de nuestras pruebas para la defensa técnica:

1.  **Pruebas de "Integración de Componentes" (Actuales):** 
    - **Ubicación:** `/services/*/tests/integration/`
    - **Naturaleza:** Son pruebas de **Caja Gris** que validan el pipeline de ASP.NET Core. 
    - **Estatus:** **IMPLEMENTADO.** Usamos `WebApplicationFactory` con `UseInMemoryDatabase`. 
    - **Defensa:** Validamos que el request llegue al controller y se procese, pero **verificamos** solo la lógica interna, omitiendo la latencia y restricciones de una base de datos real.

2.  **Pruebas de "Integración de Infraestructura" (Futuro/Escalado):**
    - **Estado:** Se recomienda migrar a **Testcontainers** para reemplazar el In-Memory por instancias reales de Postgres en el CI.

3.  **Validación vs. Verificación en nuestro Pipeline:**
    - **Validación (¿Estamos construyendo el producto correcto?):** Se hace en la capa **E2E** y **Manual**. Validamos que el flujo de compra "Ticketing MVP" cumple con la necesidad del usuario de principio a fin.
    - **Verificación (¿Estamos construyendo el producto correctamente?):** Se hace en las capas **Unitarias** e **Integración**. Verificamos que el código cumple con las especificaciones técnicas (ej: el JSON tiene el formato exacto, el código HTTP es 201 Created).

---

## 5. Cuadro de Defensa de QA (Resumen para Jurado)
| Pregunta | Respuesta Estratégica |
| :--- | :--- |
| **¿Por qué dicen que tienen pruebas de integración si usan In-Memory?** | "Tenemos **Integración de Componentes**. Validamos la integración del framework ASP.NET con nuestro código de aplicación. No es integración de infraestructura pura, pero es altamente eficiente para validar el pipeline de ejecución sin la fragilidad de servicios externos." |
| **¿Dónde está la validación de Kafka en el CI?** | "Actualmente verificamos la **intención de emisión** mediante Mocks. En el flujo final de `system-verification.yml`, validamos la **coreografía real** observando los cambios de estado finales en las bases de datos tras la propagación de eventos." |
| **¿Cómo aseguran que el Shift-Left sea real?** | "Mediante el feedback instantáneo en los PRs. Ningún código llega a la rama `main` sin haber verificado satisfactoriamente las capas unitaria y de componentes, reduciendo el costo de corrección en un 80%." |

---

## 6. Pruebas Funcionales y No Funcionales

Como parte del rigor de QA Senior, dividimos las pruebas en dos grandes dimensiones:

### 3.1 Pruebas Funcionales (El 'Qué')
Se centran en el cumplimiento de los requerimientos del negocio (historias de usuario):
- **Happy Path:** Flujo de compra exitoso (TC-P1-01 a TC-P1-06).
- **Edge Cases:** Reservas simultáneas, pagos declinados y cupones de descuento.
- **Validación de Negocio:** Asegurar que un usuario no exceda el límite de tickets permitido por evento.

### 3.2 Pruebas No Funcionales (El 'Cómo de bien')
Garantizamos los atributos de calidad del sistema mediante el pipeline de CI/CD:

#### A. Calidad de Código y Mantenibilidad (SonarCloud/SonarQube)
- **Herramienta:** `sonar-analysis.yml`.
- **Métricas:** 
    - **Quality Gate:** Cero (0) Bugs, Cero (0) Vulnerabilities (Rating A).
    - **Duplicación:** Máximo 3% para asegurar código limpio y modular.
    - **Cobertura:** Mínimo 85% para avanzar al siguiente Stage.

#### B. Seguridad y Escaneo de Vulnerabilidades (Trivy)
- **Herramienta:** `trivy` (SCA/Container Scanning).
- **Alcance:**
    - Escaneo de imágenes base de Docker para microservicios.
    - Detección de CVEs en dependencias de NuGet y NPM.
    - Búsqueda de Secretos/Hardcoded Credentials en el código.

#### C. Rendimiento y Concurrencia
- **Herramienta:** `RedisLock` + Pruebas de Carga Locales.
- **Límite:** El sistema debe manejar race-conditions de hasta 100 usuarios intentando reservar el mismo asiento en menos de 1 segundo.

---

## 4. Tipos de Pruebas y Técnicas

### 3.1 Pruebas de Caja Blanca (White-Box)
- **Nivel:** Unitario.
- **Técnica:** Cobertura de caminos (Path Coverage) en los State Machines de las Órdenes.
- **Objetivo:** Asegurar que cada `if/else` en la lógica de negocio sea ejercitado.

### 3.2 Pruebas de Caja Negra (Black-Box)
- **Nivel:** API / Integración.
- **Técnica:** **Partición de Equivalencia** y **Valores Límite**.
- **Ejemplo:** Validar que una reserva de 15:01 min sea rechazada sin conocer la implementación interna del TTL en Redis.

---

## 4. Estrategia de Integración Detallada

### 4.1 Integración de Componentes (Component Testing)
Cada microservicio es validado de forma aislada pero con infraestructura real:
- **Base de Datos:** Postgres con esquemas `bc_*`. Validamos migraciones y constraints.
- **Cache:** Redis para locks distribuidos. Validamos que el lock se libere correctamente.

### 4.2 Integración de Sistema (System Testing)
Validamos la coreografía de microservicios:
1. **Flow Check:** `Inventory` emite `ReservationCreated`.
2. **Side Effect:** `Ordering` recibe el evento y crea la orden en `Draft`.
3. **Resiliencia:** Si Kafka falla, el sistema debe reintentar el envío mediante el patrón "Outbox" o reintentos locales.

---

## 5. Gestión de Datos de Prueba
- **Datos Estáticos:** Archivos JSON para mapas de asientos predefinidos.
- **Datos Dinámicos:** Generados en tiempo de ejecución para evitar colisiones entre ejecuciones de CI/CD.
---

**Firmado:** GitHub Copilot (QA Senior AI)

---

## Apéndice: Estrategia de Pruebas para Waitlist Feature

### A.1 Contexto de la Feature

| Aspecto | Valor |
|---------|-------|
| **Feature** | Demand Recovery Waitlist |
| **HU Mapping** | 7 User Stories (HU-001 a HU-007) |
| **Reqs** | 23 Funcional Requirements (RF-001 a RF-023) |
| **Arquitectura** | Hexagonal (Ports & Adapters) |
| **TDD** | OBLIGATORIO |

### A.2 Pirámide de Pruebas para Waitlist

```mermaid
graph TD
    subgraph WaitlistPyramid
    E2E[5% - E2E: Compra completa Waitlist]
    INT_KAFKA[10% - Kafka: waitlist.opportunity-granted]
    INT_COMP[15% - Componentes: API + Handlers]
    UNIT[70% - Unitarias: WaitlistEntry, OpportunityWindow]
    end
    style UNIT fill:#4CAF50,stroke:#333
    style INT_COMP fill:#FFC107,stroke:#333
    style INT_KAFKA fill:#FF9800,stroke:#333
    style E2E fill:#F44336,stroke:#333
```

### A.3 Cobertura por User Story

| HU | Descripción | Tipo Prueba Prioritaria | Entidades Clave |
|----|-------------|------------------------|-----------------|
| HU-001 | Registro en waitlist | Unit + Component | WaitlistEntry |
| HU-002 | Visualizar suscripción activa | Component | WaitlistEntry |
| HU-003 | Cancelar suscripción | Unit + Component | WaitlistEntry |
| HU-004 | Publicar evento de liberación | Integration (Kafka) | ReservationExpiredEvent |
| HU-005 | Procesar liberación y asignar oportunidad | Unit + Integration | WaitlistEntry, OpportunityWindow |
| HU-006 | Enviar notificación por email | Integration | WaitlistOpportunityGranted |
| HU-007 | Validar oportunidad y crear reserva | Unit + Component | OpportunityWindow, Reservation |

### A.4 Estrategia TDD para Waitlist (Workflow)

```mermaid
flowchart LR
    A[RED: Escribir test] --> B[GREEN: Implementar mínimo]
    B --> C[REFACTOR: Limpiar código]
    C --> A
```

**Flujo por cada RF:**

1. **RED:** Escribir test unitario fallando para el requisito
2. **GREEN:** Implementar código mínimo para pasar
3. **REFACTOR:** Aplicar SOLID, mantener tests verdes

### A.5 Estrategias de Prueba por Capa

| Capa | Estrategia | Enfoque |
|------|-----------|---------|
| **Unit** | TDD puro | Tests para Handlers, Services, Entidades. Mocks para puertos (IRepository, IEventPublisher). Validación de reglas de negocio. |
| **Component** | API Testing | Tests de endpoints REST con WebApplicationFactory. Validación de contratos JSON y códigos HTTP. |
| **Integration** | Testcontainers | Tests de repositorios con PostgreSQL real. Validación de constraints y migraciones. |
| **Kafka** | Event-Flow Testing | Tests de consumo/publicación de eventos. Validación de idempotencia y coreografía. |
| **E2E** | Docker Compose | Flujo completo: usuario → waitlist → notificación → checkout. |

### A.6 Herramientas de Testing

| Capa | Herramienta | Uso |
|------|------------|-----|
| Unit | xUnit + Moq + FluentAssertions | Lógica de dominio, Handlers |
| Component | WebApplicationFactory + In-Memory | Endpoints API |
| Integration | Testcontainers (PostgreSQL + Redis) | Persistencia real |
| Kafka | Testcontainers + Confluent | Event flow |
| E2E | Docker Compose | Flujo completo |

### A.7 Métricas de Cobertura Objetivo

| Métrica | Target |
|---------|--------|
| Unit Test Coverage | ≥ 80% |
| Integration Test Coverage | ≥ 60% |
| All Tests Pass | 100% |
| TDD Compliance | 100% (RED → GREEN → REFACTOR) |

### A.8 Estrategias de Mitigación de Riesgos de Testing

| Riesgo de Testing | Estrategia de Mitigación |
|-------------------|--------------------------|
| Race condition en selección FIFO | Lua script atómico en Redis (operación ZPOPMAX + ZREM atómica) |
| Doble notificación por Kafka replay | Idempotency key en Redis (SHA256 payload + TTL 24h) |
| Desincronización Redis/DB | PostgreSQL como source of truth + background sync worker |
| Oportunidad otorgada a múltiples usuarios | Estado OFFERED previene re-selección, validación antes de crear reserva |
| Expiración de oportunidad no propagada | Background worker de expiración de oportunidades |

