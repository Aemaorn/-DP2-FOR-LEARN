#!/bin/bash

# SonarQube HTML Report Generator Script
# This script generates interactive HTML reports from SonarQube analysis results

set -e  # Exit on any error

# Default values
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/reports"
SONAR_URL=""
PROJECT_KEY=""
USERNAME=""
PASSWORD=""
VERSION=""
VERBOSE=false

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

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Generate interactive HTML reports from SonarQube analysis results.

OPTIONS:
    -u, --url URL           SonarQube server URL (required)
    -p, --project KEY       SonarQube project key (required)
    -U, --username USER     SonarQube username (required)
    -P, --password PASS     SonarQube password (required)
    -V, --version VER       Version number of the scanned software
    -o, --output-dir DIR    Output directory (default: $OUTPUT_DIR)
    -v, --verbose           Enable verbose output
    -h, --help              Show this help message

EXAMPLES:
    # Basic usage
    $0 -u "http://localhost:9000" -p "my-project" -U "admin" -P "admin"

    # With version and custom output directory
    $0 -u "http://sonar.company.com" -p "backend-api" -U "user" -P "pass" -V "1.2.3" -o "/tmp/reports"

    # Using environment variables
    export SONAR_URL="http://localhost:9000"
    export SONAR_PROJECT_KEY="my-project"
    export SONAR_USERNAME="admin"
    export SONAR_PASSWORD="admin"
    export SONAR_VERSION="1.0.0"
    $0

ENVIRONMENT VARIABLES:
    SONAR_URL               SonarQube server URL
    SONAR_PROJECT_KEY       SonarQube project key
    SONAR_USERNAME          SonarQube username
    SONAR_PASSWORD          SonarQube password
    SONAR_VERSION           Version number of the scanned software
    SONAR_OUTPUT_DIR        Output directory for reports

EOF
}

# Function to check if Python is available
check_python() {
    if ! command -v python3 &> /dev/null; then
        print_error "Python 3 is required but not installed."
        exit 1
    fi
    
    local python_version=$(python3 --version 2>&1 | cut -d' ' -f2 | cut -d'.' -f1-2)
    local required_version="3.6"
    
    if [ "$(printf '%s\n' "$required_version" "$python_version" | sort -V | head -n1)" != "$required_version" ]; then
        print_error "Python 3.6 or higher is required. Found: $python_version"
        exit 1
    fi
    
    print_info "Using Python $(python3 --version 2>&1 | cut -d' ' -f2)"
}

# Function to validate inputs
validate_inputs() {
    local errors=0
    
    if [ -z "$SONAR_URL" ]; then
        print_error "SonarQube URL is required"
        errors=$((errors + 1))
    fi
    
    if [ -z "$PROJECT_KEY" ]; then
        print_error "Project key is required"
        errors=$((errors + 1))
    fi
    
    if [ -z "$USERNAME" ]; then
        print_error "Username is required"
        errors=$((errors + 1))
    fi
    
    if [ -z "$PASSWORD" ]; then
        print_error "Password is required"
        errors=$((errors + 1))
    fi
    
    if [ $errors -gt 0 ]; then
        print_error "Please provide all required parameters or set environment variables"
        show_usage
        exit 1
    fi
}

# Function to create output directory
create_output_dir() {
    if [ ! -d "$OUTPUT_DIR" ]; then
        print_info "Creating output directory: $OUTPUT_DIR"
        mkdir -p "$OUTPUT_DIR"
    fi
}

# Function to run the HTML reporter
run_html_reporter() {
    local reporter_script="$SCRIPT_DIR/sonar_html_reporter.py"
    
    if [ ! -f "$reporter_script" ]; then
        print_error "HTML reporter script not found: $reporter_script"
        exit 1
    fi
    
    print_info "Generating SonarQube HTML report..."
    print_info "Project: $PROJECT_KEY"
    print_info "Output: $OUTPUT_DIR"
    
    local cmd_args=(
        "--url" "$SONAR_URL"
        "--project" "$PROJECT_KEY"
        "--username" "$USERNAME"
        "--password" "$PASSWORD"
        "--output-dir" "$OUTPUT_DIR"
    )

    # Add version if specified
    if [ -n "$VERSION" ]; then
        cmd_args+=("--version" "$VERSION")
    fi
    
    if [ "$VERBOSE" = true ]; then
        print_info "Running: python3 $reporter_script ${cmd_args[*]}"
    fi
    
    # Run the Python script
    if python3 "$reporter_script" "${cmd_args[@]}"; then
        local report_file=$(find "$OUTPUT_DIR" -name "sonarqube_report_${PROJECT_KEY}_*.html" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2- || find "$OUTPUT_DIR" -name "sonarqube_report_${PROJECT_KEY}_*.html" -type f | head -1)
        
        if [ -n "$report_file" ] && [ -f "$report_file" ]; then
            print_success "HTML report generated successfully!"
            print_info "Report location: $report_file"
            print_info "File size: $(du -h "$report_file" | cut -f1)"
        else
            print_warning "Report generated but file not found in expected location"
        fi
    else
        local exit_code=$?
        if [ $exit_code -eq 2 ]; then
            print_success "No issues found in SonarQube analysis"
        else
            print_error "Failed to generate HTML report (exit code: $exit_code)"
            exit $exit_code
        fi
    fi
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -u|--url)
            SONAR_URL="$2"
            shift 2
            ;;
        -p|--project)
            PROJECT_KEY="$2"
            shift 2
            ;;
        -U|--username)
            USERNAME="$2"
            shift 2
            ;;
        -P|--password)
            PASSWORD="$2"
            shift 2
            ;;
        -V|--version)
            VERSION="$2"
            shift 2
            ;;
        -o|--output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Use environment variables if not provided via command line
SONAR_URL="${SONAR_URL:-$SONAR_URL_ENV}"
PROJECT_KEY="${PROJECT_KEY:-$SONAR_PROJECT_KEY}"
USERNAME="${USERNAME:-$SONAR_USERNAME}"
PASSWORD="${PASSWORD:-$SONAR_PASSWORD}"
VERSION="${VERSION:-$SONAR_VERSION}"
OUTPUT_DIR="${OUTPUT_DIR:-$SONAR_OUTPUT_DIR}"

# Set default output directory if still empty
OUTPUT_DIR="${OUTPUT_DIR:-$SCRIPT_DIR/reports}"

# Main execution
main() {
    print_info "SonarQube HTML Report Generator"
    print_info "==============================="
    
    check_python
    validate_inputs
    create_output_dir
    run_html_reporter
    
    print_success "Report generation completed!"
}

# Run main function
main "$@"
