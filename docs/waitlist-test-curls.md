# Waitlist API - Test Curls

## Variables
```bash
EVENT_ID="e40b63e0-446c-46d5-8d47-b5d911d7376f"
USER_ID_1="66f8963f-868c-4903-9cb7-7c0b3402de1c"  # guiller.zeva16@gmail.com
USER_ID_2="3c2c02f0-5c0f-463d-bf0b-192ee60b4d1d"  # guillo@gmail.com
GATEWAY="http://localhost:5000"
```

## 1. Join Waitlist - Unirse a lista de espera
```bash
curl -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_ID_1" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"General\"}"
```

## 2. Get Waitlist Status - Consultar estado
```bash
curl -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_ID_1"
```

## 3. Join Waitlist (Different User)
```bash
curl -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_ID_2" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"General\"}"
```

## 4. Cancel Waitlist - Cancelar suscripción
```bash
curl -X DELETE "$GATEWAY/api/waitlist/cancel?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_ID_1"
```

## 5. Join Different Section
```bash
curl -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_ID_1" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"VIP\"}"
```

## 6. Get Status for Different User
```bash
curl -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_ID_2"
```

## 7. Error - Missing X-User-Id Header
```bash
curl -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"General\"}"
```

## 8. Error - Invalid User (not in waitlist)
```bash
curl -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: 00000000-0000-0000-0000-000000000000"
```