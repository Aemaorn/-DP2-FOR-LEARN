#!/bin/bash

# Script to attach SonarQube PDF report to Azure DevOps Pull Request using Azure CLI

# Default values
PDF_DIR="./sonar-reports"
COMMENT="SonarQube Analysis Report - Critical and High Issues"

# Function to display usage information
usage() {
  echo "Usage: $0 [options]"
  echo "Options:"
  echo "  -o, --organization ORG    Azure DevOps organization name (required)"
  echo "  -p, --project PROJECT     Azure DevOps project name (required)"
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
if [ -z "$ORGANIZATION" ] || [ -z "$PROJECT" ] || [ -z "$PR_ID" ]; then
  echo "Error: Missing required parameters"
  usage
fi

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
  echo "Error: Azure CLI is not installed. Please install it first."
  echo "Visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
  exit 1
fi

# Check if Azure DevOps extension is installed
if ! az extension list | grep -q "azure-devops"; then
  echo "Installing Azure DevOps extension for Azure CLI..."
  az extension add --name azure-devops
fi

# Configure Azure DevOps organization
echo "Configuring Azure DevOps organization: $ORGANIZATION"
az devops configure --defaults organization="https://dev.azure.com/$ORGANIZATION"

# Configure Azure DevOps project
echo "Configuring Azure DevOps project: $PROJECT"
az devops configure --defaults project="$PROJECT"

# If specific PDF file is provided, use it
if [ -n "$PDF_FILE" ]; then
  if [ ! -f "$PDF_FILE" ]; then
    echo "Error: PDF file not found: $PDF_FILE"
    exit 1
  fi

  PDF_PATH="$PDF_FILE"
else
  # Find the most recent PDF file in the directory
  LATEST_PDF=$(find "$PDF_DIR" -name "*.pdf" -type f -print0 | xargs -0 ls -t | head -1)

  if [ -z "$LATEST_PDF" ]; then
    echo "Error: No PDF files found in $PDF_DIR"
    exit 1
  fi

  PDF_PATH="$LATEST_PDF"
fi

echo "Attaching PDF file to PR #$PR_ID: $PDF_PATH"

# Create a temporary directory for the comment file
TEMP_DIR=$(mktemp -d)
COMMENT_FILE="$TEMP_DIR/comment.txt"

# Get the PDF filename
PDF_FILENAME=$(basename "$PDF_PATH")

# Create a more detailed comment with PDF information
DETAILED_COMMENT="$COMMENT\n\nPDF Report: $PDF_FILENAME\nGenerated: $(date '+%Y-%m-%d %H:%M:%S')\n\nPlease download the PDF report from the shared location to view the detailed analysis."

# Create the comment file with the PDF information as JSON
cat > "$COMMENT_FILE" << EOF
{
  "comments": [
    {
      "parentCommentId": 0,
      "content": "$DETAILED_COMMENT",
      "commentType": 1
    }
  ],
  "status": 1,
  "threadContext": {
    "filePath": "/",
    "leftFileEnd": null,
    "leftFileStart": null,
    "rightFileEnd": null,
    "rightFileStart": null
  }
}
EOF

# Add the PR comment with the PDF attachment
echo "Adding comment to PR #$PR_ID..."
az devops invoke --area git --resource pullRequestThreads --route-parameters \
  repositoryId="$(az repos list --query "[0].id" -o tsv)" \
  pullRequestId="$PR_ID" \
  --http-method POST \
  --api-version "6.0" \
  --in-file "$COMMENT_FILE" \
  --output json

# Check if the comment was added successfully
if [ $? -eq 0 ]; then
  echo "Comment added successfully to PR #$PR_ID"

  # Instead of trying to upload the PDF directly (which doesn't work well with the CLI),
  # we'll just inform the user that the comment was added successfully
  echo "Comment with PDF information added successfully to PR #$PR_ID"
  echo "PDF report: $PDF_PATH"
  echo ""
  echo "Note: The PDF file was not directly attached to the PR comment due to limitations"
  echo "in the Azure CLI. Please consider one of these alternatives:"
  echo "1. Upload the PDF to a shared location and update the comment with a link"
  echo "2. Use the Azure DevOps web interface to manually attach the PDF"
  echo "3. Use the Azure DevOps REST API with a Personal Access Token (PAT)"
else
  echo "Failed to add comment to PR"
  exit 1
fi

# Clean up temporary files
rm -rf "$TEMP_DIR"
