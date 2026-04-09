# Componente: Arquitectura de Servicios

```mermaid
flowchart TB
    subgraph Client["Frontend (Client)"]
        F[Next.js App]
    end

    subgraph Gateway["API Gateway"]
        G[Ocelot / Custom]
    end

    subgraph External["Servicios Externos"]
        P[Payment Provider]
        E[Email Provider]
    end

    subgraph Infrastructure["Infraestructura"]
        K[Kafka]
        R[Redis]
        DB[(PostgreSQL)]
    end

    subgraph Services["Microservicios"]
        subgraph Identity["Identity Service"]
            I[Auth API]
            IU[User Management]
        end

        subgraph Catalog["Catalog Service"]
            C[Events API]
            CS[Seats]
        end

        subgraph Inventory["Inventory Service"]
            INV[Reservations API]
            WL[Waitlist]
            IW[Workers]
        end

        subgraph Fulfillment["Fulfillment Service"]
            FUL[Tickets API]
            PDF[PDF Generator]
        end

        subgraph Notification["Notification Service"]
            N[Email Service]
            NS[Kafka Consumer]
        end

        subgraph Ordering["Ordering Service"]
            O[Orders API]
        end

        subgraph Payment["Payment Service"]
            PAY[Payments API]
        end
    end

    F --> G
    G -->|Auth| I
    G --> C
    G --> INV
    G --> O
    G --> PAY

    I -->|Users| DB
    C -->|Events/Seats| DB
    C --> R
    INV -->|Reservations| DB
    INV -->|Waitlist| R
    INV -->|Publish| K
    O -->|Orders| DB
    PAY -->|Payments| DB
    FUL -->|Tickets| DB

    K -->|seat-released| INV
    K -->|waitlist-opp| N
    K -->|payment-succeeded| FUL
    K -->|ticket-issued| N

    N -->|SMTP| E

    style Client fill:#e1f5fe,stroke:#01579b
    style Gateway fill:#e8f5e8,stroke:#2e7d32
    style Infrastructure fill:#fff3e0,stroke:#e65100
    style Services fill:#f3e5f5,stroke:#7b1fa2
    style External fill:#ffebee,stroke:#c62828
```

---

# Componente: Flujo de Waitlist End-to-End

```mermaid
sequenceDiagram
    participant U as Frontend
    participant G as Gateway
    participant INV as Inventory
    participant K as Kafka
    participant N as Notification
    participant DB as PostgreSQL
    participant R as Redis

    Note over U,N: FASE 1: Unión al Waitlist
    U->>G: POST /api/waitlist/join<br/>{eventId, section}
    G->>INV: X-User-Id header
    INV->>R: ZADD waitlist:{eventId}:{section}
    INV->>DB: Create WaitlistEntry
    INV->>R: ZRANK (posición)
    INV-->>G: {entryId, position, status}
    G-->>U: 201 Created

    Note over U,N: FASE 2: Seleção FIFO (cuando se libera asiento)
    K->>INV: seat-released event
    INV->>R: ZRANGEBYSCORE (FIFO)
    INV->>R: ZPOPMIN (pop usuario)
    INV->>DB: Update waitlistEntry OFFERED
    INV->>DB: Create OpportunityWindow
    INV->>K: waitlist-opportunity

    Note over U,N: FASE 3: Notificación
    K->>N: waitlist-opportunity
    N->>N: Resolve eventName
    N->>N: Generate email HTML
    N->>N: Send via SMTP
    N-->>U: Email (link compra)

    Note over U,N: FASE 4: Compra
    U->>G: GET /waitlist/opportunity/{token}
    G->>INV: Validate token
    INV-->>G: reservation details
    G-->>U: Redirect to checkout

    U->>G: POST /api/reservations<br/>{seatId, opportunityToken}
    G->>INV: Create reservation (10 min)
    INV->>DB: Reservation created
    INV-->>U: reservationId, expiresAt

    U->>G: POST /api/payments
    G->>PAY: Process payment
    PAY-->>K: payment-succeeded

    Note over U,N: FASE 5: Generación Ticket
    K->>FUL: payment-succeeded
    FUL->>FUL: Generate QR code
    FUL->>FUL: Generate PDF
    FUL->>DB: Save ticket
    FUL->>K: ticket-issued

    Note over U,N: FASE 6: Notificación Ticket
    K->>N: ticket-issued
    N->>N: Generate QR bytes
    N->>N: Build email + QR embed
    N->>N: Send with PDF attach
    N-->>U: Email con QR + PDF
```

---

# Componente: Detalle Inventory Service

```mermaid
flowchart TB
    subgraph Inventory["Inventory Service"]
        API[API Layer<br/>Controllers]
        
        subgraph Handlers["Handlers/UseCases"]
            J[JoinWaitlistHandler]
            S[GetWaitlistStatus]
            V[ValidateOpportunity]
            C[CancelWaitlist]
            R[CreateReservation]
        end

        subgraph Workers["Background Workers"]
            RE[ReservationExpiryWorker]
            OE[OpportunityExpiryWorker]
        end

        subgraph Kafka["Kafka Consumers"]
            SR[SeatReleasedStrategy]
            PF[PaymentFailedStrategy]
            REv[ReservationExpiredStrategy]
        end

        subgraph Persistence["Data Layer"]
            Repo[Repositories]
            Redis[WaitlistRedisConfig]
        end
    end

    API --> Handlers
    Handlers --> Repo
    Handlers --> Redis
    Workers --> Redis
    Workers --> Repo
    Kafka --> Redis
    Kafka --> Repo

    style Inventory fill:#f3e5f5,stroke:#7b1fa2
```

---

# Componente: Detalle Notification Service

```mermaid
flowchart TB
    subgraph Notification["Notification Service"]
        K[Kafka Consumer]
        
        subgraph Strategies["Event Strategies"]
            WO[WaitlistOpportunityStrategy]
            TI[TicketIssuedStrategy]
        end

        subgraph UseCases["Use Cases"]
            SE[SendWaitlistNotification]
            ST[SendTicketNotification]
        end

        subgraph Services["Core Services"]
            QR[QR Code Service]
            ES[Email Service]
        end

        subgraph Templates["Email Templates"]
            T1[Waitlist Opportunity]
            T2[Ticket Confirmation]
        end
    end

    K --> WO
    K --> TI
    WO --> SE
    TI --> ST
    SE --> ES
    ST --> QR
    ST --> ES
    SE --> T1
    ST --> T2

    style Notification fill:#f3e5f5,stroke:#7b1fa2
```

---

# Componente: Detalle Fulfillment Service

```mermaid
flowchart TB
    subgraph Fulfillment["Fulfillment Service"]
        K[Kafka Consumer]
        
        subgraph Events["Event Handlers"]
            PS[PaymentSucceededHandler]
        end

        subgraph Generation["Ticket Generation"]
            QR[QR Code Generator]
            PG[PDF Generator]
            ST[Storage Service]
        end

        subgraph Entities["Domain Entities"]
            T[Ticket]
        end

        subgraph KafkaOut["Kafka Producer"]
            TI[ticket-issued event]
        end
    end

    K --> PS
    PS --> T
    T --> QR
    QR --> PG
    PG --> ST
    T --> TI

    style Fulfillment fill:#f3e5f5,stroke:#7b1fa2
```

---

# Componente: Topics Kafka

```mermaid
flowchart LR
    subgraph Producers
        INV[Inventory]
        PAY[Payment]
        FUL[Fulfillment]
    end

    subgraph Topics
        T1[seat-released]
        T2[waitlist-opportunity]
        T3[payment-succeeded]
        T4[payment-failed]
        T5[reservation-expired]
        T6[ticket-issued]
    end

    subgraph Consumers
        INVC[Inventory]
        NC[Notification]
        CC[Catalog]
        FULC[Fulfillment]
    end

    INV --> T1
    INV --> T2
    PAY --> T3
    PAY --> T4
    INV --> T5
    FUL --> T6

    T1 --> INVC
    T1 --> CC
    T2 --> NC
    T3 --> FULC
    T4 --> INVC
    T5 --> INVC
    T6 --> NC

    style Producers fill:#e8f5e8,stroke:#2e7d32
    style Topics fill:#fff3e0,stroke:#e65100
    style Consumers fill:#e1f5fe,stroke:#01579b
```