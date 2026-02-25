# Fulfillment Service Architecture

## Data Flow
```
Payment Service (payment-succeeded)
    ↓
    └─ Kafka Topic: payment-succeeded
         ↓
         └─ Fulfillment Consumer
              ├─ Parse message
              ├─ Validate payment data
              ├─ Create Ticket entity
              ├─ Generate PDF + QR
              ├─ Save to DB (bc_fulfillment)
              ├─ Publish ticket-issued
              └─ Return (success/failure)
                   ↓
         Kafka Topic: ticket-issued
              ↓
              └─ Notification Service (consumes)
```

## Domain Model

### Ticket Entity
- `id`: UUID (PK)
- `order_id`: UUID (FK, unique)
- `customer_email`: string (from payment-succeeded)
- `event_name`: string
- `seat_number`: string
- `price`: decimal
- `currency`: string
- `status`: enum (PENDING, GENERATED, FAILED, DELIVERED)
- `qr_code_data`: string (format: order_id:seat:event_id)
- `ticket_pdf_path`: string (relative path to storage)
- `generated_at`: DateTime
- `created_at`: DateTime
- `updated_at`: DateTime

### PaymentSucceededEvent (Kafka message structure)
```json
{
  "order_id": "uuid",
  "customer_email": "string",
  "event_id": "uuid",
  "event_name": "string",
  "seat_number": "string",
  "price": "decimal",
  "currency": "string",
  "payment_id": "uuid",
  "timestamp": "datetime-iso"
}
```

### TicketIssuedEvent (Kafka message structure)
```json
{
  "order_id": "uuid",
  "ticket_id": "uuid",
  "customer_email": "string",
  "ticket_pdf_url": "string",
  "event_name": "string",
  "seat_number": "string",
  "timestamp": "datetime-iso"
}
```

## Technical Stack

### Libraries
- **PDF Generation**: PdfSharpCore (NuGet)
- **QR Code**: QRCoder (NuGet)
- **Image handling**: System.Drawing.Common (for QR to image)
- **Kafka**: Confluent.Kafka
- **Testing**: xUnit, Moq

### Database
- Schema: bc_fulfillment
- Tables:
  - tickets
  - ticket_audit (for tracking status changes)

### File Storage
- Local filesystem: `/app/data/tickets/` (persisted via Docker volume)
- Relative path: `tickets/{order_id}.pdf`

## Service Dependencies
- PostgreSQL (bc_fulfillment schema)
- Kafka (payment-succeeded & ticket-issued topics)
- Services that produce payment-succeeded: Payment Service
- Services that consume ticket-issued: Notification Service

## Implementation Phases

### Phase 1: Project Setup (T034)
- Create Fulfillment.sln with project structure
- Setup Hexagonal architecture
- Configure dependency injection

### Phase 2: Database & Domain (T035)
- Create FulfillmentDbContext
- Define Ticket entity
- Create initial migration
- Seed test data (optional)

### Phase 3: Core Business Logic (T036)
- Implement Ticket generation logic
- Integrate PDF + QR generation
- Implement Kafka consumer
- Publish ticket-issued events
- Handle idempotency

### Phase 4: Testing (T037)
- Unit tests for domain logic
- Mock PDF/QR libraries
- Test consumer error handling
- Test idempotency
