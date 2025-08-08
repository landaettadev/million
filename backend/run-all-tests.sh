#!/bin/bash

# Real Estate API - Complete Test Suite Runner
# This script runs all types of tests: Unit, Integration, and Performance

echo "🧪 Starting Complete Test Suite for Real Estate API"
echo "================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to check if a command was successful
test_success() {
    local exit_code=$1
    local test_type=$2
    
    if [ $exit_code -eq 0 ]; then
        echo -e "✅ ${GREEN}$test_type tests PASSED${NC}"
        return 0
    else
        echo -e "❌ ${RED}$test_type tests FAILED${NC}"
        return 1
    fi
}

all_tests_passed=true

# 1. Unit Tests
echo -e "\n🔬 ${YELLOW}Running Unit Tests...${NC}"
dotnet test RealEstate.Tests/RealEstate.Tests.csproj --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
unit_result=$?
if ! test_success $unit_result "Unit"; then
    all_tests_passed=false
fi

# 2. Integration Tests
echo -e "\n🔗 ${YELLOW}Running Integration Tests...${NC}"
dotnet test RealEstate.Tests.Integration/RealEstate.Tests.Integration.csproj --configuration Release --logger "console;verbosity=normal"
integration_result=$?
if ! test_success $integration_result "Integration"; then
    all_tests_passed=false
fi

# 3. Performance Benchmarks
echo -e "\n⚡ ${YELLOW}Running Performance Benchmarks...${NC}"
echo -e "${CYAN}Note: This may take several minutes...${NC}"

# Build performance tests in Release mode for accurate benchmarks
dotnet build RealEstate.Tests.Performance/RealEstate.Tests.Performance.csproj --configuration Release --no-restore

# Run benchmarks
dotnet run --project RealEstate.Tests.Performance/RealEstate.Tests.Performance.csproj --configuration Release -- --filter "*PropertyServiceBenchmarks*"
benchmark_result=$?
if ! test_success $benchmark_result "Performance Benchmark"; then
    all_tests_passed=false
fi

# 4. Load Tests
echo -e "\n🔥 ${YELLOW}Running Load Tests...${NC}"
echo -e "${CYAN}Note: This will test API under simulated load...${NC}"

dotnet test RealEstate.Tests.Performance/RealEstate.Tests.Performance.csproj --configuration Release --logger "console;verbosity=normal" --filter "Category=LoadTest"
load_result=$?
if ! test_success $load_result "Load"; then
    all_tests_passed=false
fi

# 5. Frontend Tests (if available)
if [ -f "../frontend/package.json" ]; then
    echo -e "\n⚛️ ${YELLOW}Running Frontend Tests...${NC}"
    cd "../frontend"
    
    # Check if test script exists
    if npm run | grep -q "test"; then
        npm test -- --watchAll=false --coverage
        frontend_result=$?
        if ! test_success $frontend_result "Frontend"; then
            all_tests_passed=false
        fi
    else
        echo -e "⚠️ ${YELLOW}No frontend test script found${NC}"
    fi
    
    cd "../backend"
fi

# Summary
echo -e "\n📊 ${CYAN}TEST SUITE SUMMARY${NC}"
echo -e "${CYAN}====================${NC}"

if [ "$all_tests_passed" = true ]; then
    echo -e "🎉 ${GREEN}ALL TESTS PASSED! 🎉${NC}"
    echo -e "${GREEN}Your application is ready for production!${NC}"
    exit 0
else
    echo -e "💥 ${RED}SOME TESTS FAILED!${NC}"
    echo -e "${RED}Please review the test results above.${NC}"
    exit 1
fi
