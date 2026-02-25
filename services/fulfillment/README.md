# Fulfillment Service

Fulfillment microservice para generación y emisión de tickets en PDF con códigos QR cuando los pagos se procesan exitosamente.

## Structure

```
src/
├── Api/                 # Web API entry point
├── Application/         # Business logic & use cases
│   ├── Ports/          # Interfaces (repositories, services)
│   └── UseCases/       # MediatR handlers
├── Domain/             # Domain models & entities
│   └── Entities/
└── Infrastructure/     # Data access, Kafka, external services
    ├── Persistence/    # EF Core context, repositories
    └── Events/         # Kafka consumers & event models

tests/                  # Unit tests
```

## Running Locally

### Prerequisites
- .NET 8.0
- PostgreSQL (bc_fulfillment schema)
- Kafka (payment-succeeded & ticket-issued topics)

### Development

```bash
# From services/fulfillment/ root
dotnet build
dotnet run --project src/Api/Fulfillment.Api.csproj
```

API will listen on `http://localhost:5004`

### Configuration

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topics": {
      "PaymentSucceeded": "payment-succeeded",
      "TicketIssued": "ticket-issued"
    }
  }
}
```

## Implementation Tasks

- [ ] T034: Project structure (✓ Completed)
- [ ] T035: FulfillmentDbContext & migration
- [ ] T036: Ticket generation logic, PDF + QR
- [ ] T037: Unit tests with mocks

## Database Schema

```sql
CREATE TABLE bc_fulfillment.tickets (
  id UUID PRIMARY KEY,
  order_id UUID NOT NULL UNIQUE,
  customer_email VARCHAR(255),
  event_name VARCHAR(500),
  seat_number VARCHAR(50),
  price DECIMAL,
  currency VARCHAR(3),
  status INT,
  qr_code_data VARCHAR(1000),
  ticket_pdf_path VARCHAR(1000),
  generated_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);
```

## Kafka Events

### Consuming: payment-succeeded
```json
{
  "order_id": "uuid",
  "customer_email": "string",
  "event_id": "uuid",
  "event_name": "string",
  "seat_number": "string",
  "price": "decimal",
  "currency": "string"
}
```

### Publishing: ticket-issued
```json
{
  "order_id": "uuid",
  "ticket_id": "uuid",
  "customer_email": "string",
  "ticket_pdf_url": "string",
  "event_name": "string",
  "seat_number": "string"
}
```
