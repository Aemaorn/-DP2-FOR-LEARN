#!/bin/bash

# Script to attach SonarQube PDF report to Azure DevOps Pull Request

# Default values
PDF_DIR="./sonar-reports"
COMMENT="SonarQube Analysis Report - Critical and High Issues"

# Function to display usage information
usage() {
  echo "Usage: $0 [options]"
  echo "Options:"
  echo "  -o, --organization ORG    Azure DevOps organization name (required)"
  echo "  -p, --project PROJECT     Azure DevOps project name (required)"
  echo "  -t, --token TOKEN         Azure DevOps personal access token (required)"
  echo "  -r, --pr-id ID            Pull Request ID (required)"
  echo "  -f, --pdf-file FILE       Specific PDF file to attach (optional)"
  echo "  -d, --pdf-dir DIR         Directory containing PDF reports (default: $PDF_DIR)"
  echo "  -c, --comment TEXT        Comment text (default: '$COMMENT')"
  echo "  -h, --help                Display this help message"
  exit 1
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case "$1" in
    -o|--organization)
      ORGANIZATION="$2"
      shift 2
      ;;
    -p|--project)
      PROJECT="$2"
      shift 2
      ;;
    -t|--token)
      TOKEN="$2"
      shift 2
      ;;
    -r|--pr-id)
      PR_ID="$2"
      shift 2
      ;;
    -f|--pdf-file)
      PDF_FILE="$2"
      shift 2
      ;;
    -d|--pdf-dir)
      PDF_DIR="$2"
      shift 2
      ;;
    -c|--comment)
      COMMENT="$2"
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
if [ -z "$ORGANIZATION" ] || [ -z "$PROJECT" ] || [ -z "$TOKEN" ] || [ -z "$PR_ID" ]; then
  echo "Error: Missing required parameters"
  usage
fi

# Ensure the Scripts directory is in the path
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# Check if Python virtual environment exists, create if not
VENV_DIR="$SCRIPT_DIR/.venv"
if [ ! -d "$VENV_DIR" ]; then
  echo "Creating Python virtual environment..."
  python3 -m venv "$VENV_DIR"
fi

# Activate virtual environment
source "$VENV_DIR/bin/activate"

# Install required packages if needed
if [ ! -f "$VENV_DIR/.installed" ]; then
  echo "Installing required Python packages..."
  pip install requests
  touch "$VENV_DIR/.installed"
fi

# If specific PDF file is provided, use it
if [ -n "$PDF_FILE" ]; then
  if [ ! -f "$PDF_FILE" ]; then
    echo "Error: PDF file not found: $PDF_FILE"
    exit 1
  fi
  
  echo "Attaching PDF file to PR #$PR_ID: $PDF_FILE"
  python "$SCRIPT_DIR/attach_to_pr.py" \
    --organization "$ORGANIZATION" \
    --project "$PROJECT" \
    --token "$TOKEN" \
    --pr-id "$PR_ID" \
    --pdf-path "$PDF_FILE" \
    --comment "$COMMENT"
else
  # Find the most recent PDF file in the directory
  LATEST_PDF=$(find "$PDF_DIR" -name "*.pdf" -type f -print0 | xargs -0 ls -t | head -1)
  
  if [ -z "$LATEST_PDF" ]; then
    echo "Error: No PDF files found in $PDF_DIR"
    exit 1
  fi
  
  echo "Attaching latest PDF file to PR #$PR_ID: $LATEST_PDF"
  python "$SCRIPT_DIR/attach_to_pr.py" \
    --organization "$ORGANIZATION" \
    --project "$PROJECT" \
    --token "$TOKEN" \
    --pr-id "$PR_ID" \
    --pdf-path "$LATEST_PDF" \
    --comment "$COMMENT"
fi

# Check if the script was successful
if [ $? -eq 0 ]; then
  echo "PDF report successfully attached to PR #$PR_ID"
else
  echo "Failed to attach PDF report to PR"
  exit 1
fi

# Deactivate virtual environment
deactivate
