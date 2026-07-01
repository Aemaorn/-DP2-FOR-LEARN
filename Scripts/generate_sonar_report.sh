#!/bin/bash

# Script to generate SonarQube issue reports for PR comments
# This script should be run after SonarQube analysis completes

# Default values
SONAR_URL="http://192.168.20.97:9001"
OUTPUT_DIR="./sonar-reports"

# Function to display usage information
usage() {
  echo "Usage: $0 [options]"
  echo "Options:"
  echo "  -p, --project PROJECT_KEY  SonarQube project key (required)"
  echo "  -u, --username USERNAME    SonarQube username (required)"
  echo "  -w, --password PASSWORD    SonarQube password (required)"
  echo "  -f, --password-file FILE   File containing SonarQube password (alternative to -w)"
  echo "  -s, --server URL           SonarQube server URL (default: $SONAR_URL)"
  echo "  -o, --output-dir DIR       Output directory for reports (default: $OUTPUT_DIR)"
  echo "  -h, --help                 Display this help message"
  exit 1
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case "$1" in
    -p|--project)
      PROJECT_KEY="$2"
      shift 2
      ;;
    -u|--username)
      USERNAME="$2"
      shift 2
      ;;
    -w|--password)
      # Store password in a variable, escaping special characters
      PASSWORD="$2"
      shift 2
      ;;
    -f|--password-file)
      # Read password from file
      if [ -f "$2" ]; then
        PASSWORD=$(cat "$2")
      else
        echo "Error: Password file not found: $2"
        exit 1
      fi
      shift 2
      ;;
    -s|--server)
      SONAR_URL="$2"
      shift 2
      ;;
    -o|--output-dir)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    -h|--help)
      usage
      ;;
    *)
      echo "Unknown option: $1"
      usage
      ;;
  esac
done

# Check required parameters
if [ -z "$PROJECT_KEY" ] || [ -z "$USERNAME" ] || [ -z "$PASSWORD" ]; then
  echo "Error: Missing required parameters"
  usage
fi

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Ensure the Scripts directory is in the path
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# Check for Python availability
PYTHON_CMD=""
if command -v python3 &> /dev/null; then
  PYTHON_CMD="python3"
elif command -v python &> /dev/null; then
  PYTHON_CMD="python"
fi

if [ -z "$PYTHON_CMD" ]; then
  echo "Error: Python is not installed or not in PATH"
  exit 1
fi

echo "Using Python command: $PYTHON_CMD"

# Check for pip availability
PIP_CMD=""
if command -v pip3 &> /dev/null; then
  PIP_CMD="pip3"
elif command -v pip &> /dev/null; then
  PIP_CMD="pip"
fi

if [ -z "$PIP_CMD" ]; then
  echo "Warning: pip is not installed or not in PATH"
  echo "Attempting to use $PYTHON_CMD -m pip instead"
  PIP_CMD="$PYTHON_CMD -m pip"
fi

echo "Using pip command: $PIP_CMD"

# Try to create a virtual environment, but continue without it if it fails
VENV_DIR="$SCRIPT_DIR/.venv"
USE_VENV=true

if [ ! -d "$VENV_DIR" ]; then
  echo "Creating Python virtual environment..."
  $PYTHON_CMD -m venv "$VENV_DIR" 2>/dev/null || {
    echo "Virtual environment creation failed, continuing without it..."
    USE_VENV=false
  }
fi

# Activate virtual environment if it was created successfully
if [ "$USE_VENV" = true ] && [ -f "$VENV_DIR/bin/activate" ]; then
  echo "Activating virtual environment..."
  source "$VENV_DIR/bin/activate"

  # Update PIP_CMD and PYTHON_CMD to use the virtual environment
  PYTHON_CMD="python"
  PIP_CMD="pip"
else
  echo "Running without virtual environment..."
  USE_VENV=false
fi

# Install required packages
echo "Installing required Python packages..."
$PIP_CMD install requests fpdf2 || {
  echo "Failed to install packages with $PIP_CMD, trying with $PYTHON_CMD -m pip..."
  $PYTHON_CMD -m pip install requests fpdf2
}

# Run the Python script
echo "Generating SonarQube issues report for $PROJECT_KEY..."

# Create a temporary password file to avoid shell interpretation issues
TEMP_PASSWORD_FILE=$(mktemp)
echo "$PASSWORD" > "$TEMP_PASSWORD_FILE"

# Run the Python script with the appropriate Python command
$PYTHON_CMD "$SCRIPT_DIR/sonar_issue_reporter.py" \
  --url "$SONAR_URL" \
  --project "$PROJECT_KEY" \
  --username "$USERNAME" \
  --password "$(cat $TEMP_PASSWORD_FILE)" \
  --output-dir "$OUTPUT_DIR"

# Store the exit code
PYTHON_EXIT_CODE=$?

# Remove the temporary password file
rm -f "$TEMP_PASSWORD_FILE"

# Check the exit code
if [ $PYTHON_EXIT_CODE -eq 0 ]; then
  echo "Report generated successfully in $OUTPUT_DIR"
  echo "You can now attach this report to your PR comments"
  # Create a marker file to indicate report was generated
  touch "$OUTPUT_DIR/REPORT_GENERATED"
elif [ $PYTHON_EXIT_CODE -eq 2 ]; then
  echo "No SonarQube issues found. Skipping report generation."
  # Create a marker file to indicate no issues were found
  echo "No SonarQube issues were found in the new code." > "$OUTPUT_DIR/NO_ISSUES_FOUND"
  # Exit with success but special code for no issues
  exit 2
else
  echo "Failed to generate report"
  exit 1
fi

# Deactivate virtual environment if it was used
if [ "$USE_VENV" = true ]; then
  echo "Deactivating virtual environment..."
  deactivate || true
fi
