#!/bin/bash

# Build script for GHB DP2 Backend
# Usage: ./build.sh [options]
# Options:
#   --sonar, -s          Enable SonarQube analysis (slower build)
#   --fast, -f           Fast build without SonarQube analysis (default)
#   --clean, -c          Clean before build
#   --test, -t           Run tests after build
#   --coverage           Run tests with coverage (implies --sonar)
#   --help, -h           Show this help message

set -e  # Exit on any error

# Default values
ENABLE_SONAR=false
CLEAN_BUILD=false
RUN_TESTS=false
RUN_COVERAGE=false
BUILD_CONFIG="Release"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/../Backend" && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
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

# Function to show help
show_help() {
    echo "Build script for GHB DP2 Backend"
    echo ""
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --sonar, -s          Enable SonarQube analysis (slower build)"
    echo "  --fast, -f           Fast build without SonarQube analysis (default)"
    echo "  --clean, -c          Clean before build"
    echo "  --test, -t           Run tests after build"
    echo "  --coverage           Run tests with coverage (implies --sonar)"
    echo "  --help, -h           Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                   # Fast build (default)"
    echo "  $0 --fast --test     # Fast build with tests"
    echo "  $0 --sonar --test    # Build with SonarQube analysis and tests"
    echo "  $0 --coverage        # Build with SonarQube analysis and test coverage"
    echo "  $0 --clean --sonar   # Clean build with SonarQube analysis"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --sonar|-s)
            ENABLE_SONAR=true
            shift
            ;;
        --fast|-f)
            ENABLE_SONAR=false
            shift
            ;;
        --clean|-c)
            CLEAN_BUILD=true
            shift
            ;;
        --test|-t)
            RUN_TESTS=true
            shift
            ;;
        --coverage)
            RUN_COVERAGE=true
            ENABLE_SONAR=true  # Coverage requires SonarQube tools
            RUN_TESTS=true
            shift
            ;;
        --help|-h)
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

# Print build configuration
echo "========================================"
echo "GHB DP2 Backend Build Script"
echo "========================================"
print_info "Build Configuration: $BUILD_CONFIG"
print_info "SonarQube Analysis: $([ "$ENABLE_SONAR" = true ] && echo "Enabled" || echo "Disabled")"
print_info "Clean Build: $([ "$CLEAN_BUILD" = true ] && echo "Yes" || echo "No")"
print_info "Run Tests: $([ "$RUN_TESTS" = true ] && echo "Yes" || echo "No")"
print_info "Test Coverage: $([ "$RUN_COVERAGE" = true ] && echo "Yes" || echo "No")"
print_info "Backend Directory: $BACKEND_DIR"
echo "========================================"

# Change to backend directory
cd "$BACKEND_DIR"

# Clean if requested
if [ "$CLEAN_BUILD" = true ]; then
    print_info "Cleaning previous build artifacts..."
    dotnet clean GHB.DP2.sln --configuration $BUILD_CONFIG
    print_success "Clean completed"
fi

# Install SonarQube tools if needed
if [ "$ENABLE_SONAR" = true ]; then
    print_info "Installing SonarQube tools..."
    dotnet tool install --global dotnet-sonarscanner --ignore-failed-sources || true
    if [ "$RUN_COVERAGE" = true ]; then
        dotnet tool install --global dotnet-coverage --ignore-failed-sources || true
    fi
    print_success "SonarQube tools installed"
fi

# Restore packages
print_info "Restoring NuGet packages..."
if [ "$ENABLE_SONAR" = true ]; then
    dotnet restore GHB.DP2.sln /p:EnableSonarQubeAnalysis=true
else
    dotnet restore GHB.DP2.sln /p:EnableSonarQubeAnalysis=false
fi
print_success "Package restore completed"

# Build
print_info "Building solution..."
if [ "$ENABLE_SONAR" = true ]; then
    print_warning "Building with SonarQube analysis enabled (this may take longer)..."
    dotnet build GHB.DP2.sln --configuration $BUILD_CONFIG --no-restore /p:EnableSonarQubeAnalysis=true
else
    print_info "Building with fast mode (SonarQube analysis disabled)..."
    dotnet build GHB.DP2.sln --configuration $BUILD_CONFIG --no-restore /p:EnableSonarQubeAnalysis=false
fi
print_success "Build completed"

# Run tests if requested
if [ "$RUN_TESTS" = true ]; then
    print_info "Running tests..."
    if [ "$RUN_COVERAGE" = true ]; then
        print_info "Running tests with coverage..."
        dotnet-coverage collect \
            "dotnet test GHB.DP2.sln --no-build --configuration $BUILD_CONFIG /p:EnableSonarQubeAnalysis=true" \
            -f xml \
            -o "coverage.xml"
        print_success "Tests with coverage completed"
    else
        if [ "$ENABLE_SONAR" = true ]; then
            dotnet test GHB.DP2.sln --no-build --configuration $BUILD_CONFIG /p:EnableSonarQubeAnalysis=true
        else
            dotnet test GHB.DP2.sln --no-build --configuration $BUILD_CONFIG /p:EnableSonarQubeAnalysis=false
        fi
        print_success "Tests completed"
    fi
fi

# Final message
echo "========================================"
if [ "$ENABLE_SONAR" = true ]; then
    print_success "Build completed with SonarQube analysis"
    print_info "Note: SonarQube analysis was enabled, which may have increased build time"
    print_info "For faster builds, use: $0 --fast"
else
    print_success "Fast build completed"
    print_info "SonarQube analysis was disabled for faster build times"
    print_info "For code quality analysis, use: $0 --sonar"
fi
echo "========================================"
