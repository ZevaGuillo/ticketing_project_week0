#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=========================================="
echo "Running Auth Test Suite"
echo "=========================================="
echo ""

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

TOTAL_FAILED=0

run_tests() {
    local project_path=$1
    local project_name=$2
    
    echo -e "${YELLOW}Running $project_name tests...${NC}"
    
    if dotnet test "$project_path" --verbosity normal --no-restore; then
        echo -e "${GREEN}✓ $project_name tests passed${NC}"
        echo ""
    else
        echo -e "${RED}✗ $project_name tests failed${NC}"
        TOTAL_FAILED=$((TOTAL_FAILED + 1))
        echo ""
    fi
}

echo "Step 1: Identity Service Tests"
echo "------------------------------"
run_tests "identity/tests/unit/Identity.UnitTests/Identity.UnitTests.csproj" "Identity Unit"
run_tests "identity/tests/integration/Identity.IntegrationTests/Identity.IntegrationTests.csproj" "Identity Integration"

echo "Step 2: Inventory Service Tests"
echo "--------------------------------"
if [ -d "inventory/tests" ]; then
    for test_dir in inventory/tests/unit/*/; do
        if [ -f "${test_dir}*.csproj" ]; then
            run_tests "${test_dir}" "Inventory Unit"
        fi
    done
    
    for test_dir in inventory/tests/integration/*/; do
        if [ -f "${test_dir}*.csproj" ]; then
            run_tests "${test_dir}" "Inventory Integration"
        fi
    done
else
    echo -e "${YELLOW}⚠ Inventory tests directory not found, skipping${NC}"
    echo ""
fi

echo "Step 3: Ordering Service Tests"
echo "-------------------------------"
if [ -d "ordering/tests" ]; then
    for test_dir in ordering/tests/unit/*/; do
        if [ -f "${test_dir}*.csproj" ]; then
            run_tests "${test_dir}" "Ordering Unit"
        fi
    done
    
    for test_dir in ordering/tests/integration/*/; do
        if [ -f "${test_dir}*.csproj" ]; then
            run_tests "${test_dir}" "Ordering Integration"
        fi
    done
else
    echo -e "${YELLOW}⚠ Ordering tests directory not found, skipping${NC}"
    echo ""
fi

echo "Step 4: Gateway Service Tests"
echo "------------------------------"
if [ -d "gateway/tests" ]; then
    for test_dir in gateway/tests/unit/*/; do
        if [ -f "${test_dir}*.csproj" ]; then
            run_tests "${test_dir}" "Gateway Unit"
        fi
    done
    
    for test_dir in gateway/tests/integration/*/; do
        if [ -f "${test_dir}*.csproj" ]; then
            run_tests "${test_dir}" "Gateway Integration"
        fi
    done
else
    echo -e "${YELLOW}⚠ Gateway tests directory not found, skipping${NC}"
    echo ""
fi

echo "=========================================="
echo "Test Suite Complete"
echo "=========================================="

if [ $TOTAL_FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All test suites passed!${NC}"
    exit 0
else
    echo -e "${RED}✗ $TOTAL_FAILED test suite(s) failed${NC}"
    exit 1
fi
