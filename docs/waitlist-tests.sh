#!/bin/bash

# Waitlist API Test Script
# Usage: ./waitlist-tests.sh

set -e

GATEWAY="${GATEWAY:-http://localhost:5000}"
EVENT_ID="e40b63e0-446c-46d5-8d47-b5d911d7376f"
USER_1="66f8963f-868c-4903-9cb7-7c0b3402de1c"  # guiller.zeva16@gmail.com
USER_2="3c2c02f0-5c0f-463d-bf0b-192ee60b4d1d"  # guillo@gmail.com

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}=== Waitlist API Tests ===${NC}"
echo ""

# Function to test endpoint
test_endpoint() {
    local name="$1"
    local expected_status="$2"
    local command="$3"
    
    echo -n "Testing: $name ... "
    
    if eval "$command" > /tmp/response.txt 2>&1; then
        local status=$(grep -o '"status":[^{]*' /tmp/response.txt | head -1 || echo "")
        if [[ "$status" == *"$expected_status"* ]] || [[ "$expected_status" == "any" ]]; then
            echo -e "${GREEN}PASS${NC}"
            return 0
        else
            echo -e "${RED}FAIL${NC} (unexpected status)"
            cat /tmp/response.txt
            return 1
        fi
    else
        if [[ "$expected_status" == "error" ]]; then
            echo -e "${GREEN}PASS${NC} (expected error)"
            return 0
        else
            echo -e "${RED}FAIL${NC}"
            cat /tmp/response.txt
            return 1
        fi
    fi
}

echo -e "${YELLOW}--- Test 1: Join Waitlist (User 1) ---${NC}"
curl -s -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_1" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"General\"}"
echo ""
echo ""

echo -e "${YELLOW}--- Test 2: Get Waitlist Status (User 1) ---${NC}"
curl -s -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_1"
echo ""
echo ""

echo -e "${YELLOW}--- Test 3: Join Waitlist (User 2 - Different Section) ---${NC}"
curl -s -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_2" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"VIP\"}"
echo ""
echo ""

echo -e "${YELLOW}--- Test 4: Get Waitlist Status (User 2) ---${NC}"
curl -s -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=VIP" \
  -H "X-User-Id: $USER_2"
echo ""
echo ""

echo -e "${YELLOW}--- Test 5: Get Status (User 1 - Different Section - Not in Waitlist) ---${NC}"
curl -s -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=VIP" \
  -H "X-User-Id: $USER_1"
echo ""
echo ""

echo -e "${YELLOW}--- Test 6: Join Duplicate Waitlist (Should Conflict) ---${NC}"
curl -s -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: $USER_1" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"General\"}"
echo ""
echo ""

echo -e "${YELLOW}--- Test 7: Missing X-User-Id Header (Should Error) ---${NC}"
curl -s -X POST "$GATEWAY/api/waitlist/join" \
  -H "Content-Type: application/json" \
  --data-raw "{\"eventId\":\"$EVENT_ID\",\"section\":\"General\"}"
echo ""
echo ""

echo -e "${YELLOW}--- Test 8: Cancel Waitlist (User 1) ---${NC}"
curl -s -X DELETE "$GATEWAY/api/waitlist/cancel?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_1"
echo ""
echo ""

echo -e "${YELLOW}--- Test 9: Verify Cancelled ---${NC}"
curl -s -X GET "$GATEWAY/api/waitlist/status?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_1"
echo ""
echo ""

echo -e "${YELLOW}--- Test 10: Cancel Non-Existent Waitlist ---${NC}"
curl -s -X DELETE "$GATEWAY/api/waitlist/cancel?eventId=$EVENT_ID&section=General" \
  -H "X-User-Id: $USER_1"
echo ""
echo ""

echo -e "${YELLOW}=== All Tests Completed ===${NC}"