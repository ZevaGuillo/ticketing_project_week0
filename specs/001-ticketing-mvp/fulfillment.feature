# language: es
Característica: Fulfillment y Emisión de Tickets
  Como sistema de ticketing
  Quiero generar y entregar tickets en PDF cuando un pago es procesado exitosamente
  Para que los clientes reciban su confirmación de reserva con código QR

  Antecedentes:
    Dado que existe un servicio de Fulfillment corriendo
    Y existe Kafka configurado con topic "payment-succeeded"
    Y existe base de datos "bc_fulfillment" con tabla "tickets"

  Escenario: Generar ticket cuando pago es exitoso (T036)
    Dado que un pago se procesó exitosamente con datos:
      | order_id | 22222222-2222-2222-2222-222222222222 |
      | customer_email | user@example.com |
      | event_name | Concierto Foo Fighters |
      | seat_number | A-15 |
      | price | 150.00 |
      | currency | USD |
    Cuando el mensaje "payment-succeeded" llega a Kafka
    Entonces se crea un registro "Ticket" en bc_fulfillment con:
      | order_id | 22222222-2222-2222-2222-222222222222 |
      | status | GENERATED |
      | ticket_pdf_path | /tickets/<order_id>.pdf |
      | qr_code_data | <order_id>:<seat_number>:<event_id> |
    Y se publica mensaje "ticket-issued" en Kafka con:
      | order_id | 22222222-2222-2222-2222-222222222222 |
      | ticket_pdf_url | /tickets/<order_id>.pdf |
      | customer_email | user@example.com |

  Escenario: Generar PDF con QR code válido (T036)
    Dado que tengo datos de una reserva exitosa
    Cuando genero un PDF con librería PdfSharpCore y QR con QRCoder
    Entonces el PDF contiene:
      | Elemento | Contenido |
      | Título | Ticket de Entrada |
      | Información evento | Nombre, fecha, ubicación |
      | Seat | Sección y número de asiento |
      | Precio | Monto pagado |
      | QR Code | Código decodificable |
    Y el archivo se guarda en ruta accesible
    Y el tamaño del PDF es mayor a 50 KB

  Escenario: Manejo de error al generar ticket (T036)
    Dado que llega un pago-exitoso con datos incompletos
    Cuando intento generar el ticket
    Entonces la generación falla gracefully
    Y se registra el error en logs
    Y NO se publica "ticket-issued"
    Y el ticket se marca con estado "FAILED"

  Escenario: Idempotencia - no generar ticket duplicado (T036)
    Dado que ya existe un ticket para order_id "22222222-2222-2222-2222-222222222222"
    Cuando vuelve a llegar el mismo mensaje "payment-succeeded"
    Entonces se retorna el ticket existente
    Y NO se genera un nuevo PDF
    Y NO se publica ticket-issued otra vez

  Escenario: Unit test - Validar entidad Ticket (T037)
    Dado que tengo datos válidos para crear un Ticket:
      | Field | Value |
      | order_id | GUID válido |
      | generated_at | Timestamp válido |
      | qr_code_data | String no vacío |
    Cuando creo una instancia de Ticket
    Entonces la entidad es válida
    Y todos los campos están poblados correctamente

  Escenario: Unit test - Generar QR code sin dependencias externas (T037)
    Dado que tengo data: "22222222-2222-2222-2222-222222222222:A-15:event123"
    Cuando genero QR usando mock de QRCoder
    Entonces se devuelve matriz de bits simulada
    Y el contenido es codificable en formato QR estándar

  Escenario: Unit test - Generar PDF sin dependencias externas (T037)
    Dado que tengo datos de evento y seat
    Y PdfSharpCore está mocked
    Cuando genero PDF
    Entonces se llama al constructor PdfDocument correctamente
    Y se agregan páginas con el contenido esperado
    Y se devuelve un stream válido
    Y el mock valida todas las llamadas esperadas
