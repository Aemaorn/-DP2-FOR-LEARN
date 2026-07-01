#!/bin/bash

# Full SonarQube Analysis Script
# This script runs complete SonarQube analysis and uploads results to SonarQube server
# Usage: ./sonar-analysis.sh [options]

set -e

# Default values
PROJECT_KEY=""
SONAR_HOST_URL=""
SONAR_TOKEN=""
BUILD_CONFIG="Release"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/../Backend" && pwd)"
COVERAGE_FILE="$(pwd)/coverage.xml"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

show_help() {
    echo "Full SonarQube Analysis Script"
    echo ""
    echo "Usage: $0 --project-key <key> --host-url <url> --token <token> [options]"
    echo ""
    echo "Required:"
    echo "  --project-key <key>     SonarQube project key"
    echo "  --host-url <url>        SonarQube server URL"
    echo "  --token <token>         SonarQube authentication token"
    echo ""
    echo "Optional:"
    echo "  --coverage              Run with test coverage"
    echo "  --help                  Show this help message"
    echo ""
    echo "Environment Variables (alternative to command line):"
    echo "  SONAR_PROJECT_KEY       SonarQube project key"
    echo "  SONAR_HOST_URL          SonarQube server URL"
    echo "  SONAR_TOKEN             SonarQube authentication token"
    echo ""
    echo "Examples:"
    echo "  $0 --project-key myproject --host-url https://sonar.example.com --token abc123"
    echo "  $0 --project-key myproject --host-url https://sonar.example.com --token abc123 --coverage"
}

# Parse command line arguments
RUN_COVERAGE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --project-key)
            PROJECT_KEY="$2"
            shift 2
            ;;
        --host-url)
            SONAR_HOST_URL="$2"
            shift 2
            ;;
        --token)
            SONAR_TOKEN="$2"
            shift 2
            ;;
        --coverage)
            RUN_COVERAGE=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Check for environment variables if not provided via command line
if [ -z "$PROJECT_KEY" ] && [ -n "$SONAR_PROJECT_KEY" ]; then
    PROJECT_KEY="$SONAR_PROJECT_KEY"
fi

if [ -z "$SONAR_HOST_URL" ] && [ -n "$SONAR_HOST_URL" ]; then
    SONAR_HOST_URL="$SONAR_HOST_URL"
fi

if [ -z "$SONAR_TOKEN" ] && [ -n "$SONAR_TOKEN" ]; then
    SONAR_TOKEN="$SONAR_TOKEN"
fi

# Validate required parameters
if [ -z "$PROJECT_KEY" ] || [ -z "$SONAR_HOST_URL" ] || [ -z "$SONAR_TOKEN" ]; then
    print_error "Missing required parameters!"
    echo ""
    show_help
    exit 1
fi

echo "========================================"
echo "SonarQube Full Analysis"
echo "========================================"
print_info "Project Key: $PROJECT_KEY"
print_info "SonarQube URL: $SONAR_HOST_URL"
print_info "Coverage: $([ "$RUN_COVERAGE" = true ] && echo "Enabled" || echo "Disabled")"
print_info "Backend Directory: $BACKEND_DIR"
echo "========================================"

# Change to backend directory
cd "$BACKEND_DIR"

# Install SonarQube tools
print_info "Installing SonarQube tools..."
dotnet tool install --global dotnet-sonarscanner --ignore-failed-sources || true
if [ "$RUN_COVERAGE" = true ]; then
    dotnet tool install --global dotnet-coverage --ignore-failed-sources || true
fi
print_success "SonarQube tools installed"

# Begin SonarQube analysis
print_info "Starting SonarQube analysis..."
if [ "$RUN_COVERAGE" = true ]; then
    dotnet-sonarscanner begin \
        /k:"$PROJECT_KEY" \
        /d:sonar.host.url="$SONAR_HOST_URL" \
        /d:sonar.token="$SONAR_TOKEN" \
        /d:sonar.cs.vscoveragexml.reportsPaths="$COVERAGE_FILE"
else
    dotnet-sonarscanner begin \
        /k:"$PROJECT_KEY" \
        /d:sonar.host.url="$SONAR_HOST_URL" \
        /d:sonar.token="$SONAR_TOKEN"
fi

# Restore packages
print_info "Restoring packages..."
dotnet restore GHB.DP2.sln /p:EnableSonarQubeAnalysis=true

# Build
print_info "Building solution..."
dotnet build GHB.DP2.sln --configuration $BUILD_CONFIG --no-restore /p:EnableSonarQubeAnalysis=true

# Run tests with or without coverage
if [ "$RUN_COVERAGE" = true ]; then
    print_info "Running tests with coverage..."
    dotnet-coverage collect \
        "dotnet test GHB.DP2.sln --no-build --configuration $BUILD_CONFIG /p:EnableSonarQubeAnalysis=true" \
        -f xml \
        -o "$COVERAGE_FILE"
    print_success "Tests with coverage completed"
else
    print_info "Running tests..."
    dotnet test GHB.DP2.sln --no-build --configuration $BUILD_CONFIG /p:EnableSonarQubeAnalysis=true
    print_success "Tests completed"
fi

# End SonarQube analysis
print_info "Finalizing SonarQube analysis..."
dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo "========================================"
print_success "SonarQube analysis completed!"
print_info "Results should be available at: $SONAR_HOST_URL"
print_info "Project: $PROJECT_KEY"
echo "========================================"
