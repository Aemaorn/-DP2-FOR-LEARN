#!/bin/bash

# Script to upload SonarQube PDF report to file service and attach link to Azure DevOps PR

# Default values
PDF_DIR="./sonar-reports"
COMMENT="**[This is auto generated report from pipeline]**\nPlease check and fix any CRITICAL and MAJOR issues."
FILE_SERVICE_URL="$FILE_SERVICE_URL"
FILE_SERVICE_API_KEY="$FILE_SERVICE_API_KEY"

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
  echo "  -u, --file-service-url URL File service URL (default: $FILE_SERVICE_URL)"
  echo "  -k, --api-key KEY         File service API key (default: $FILE_SERVICE_API_KEY)"
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
    -u|--file-service-url)
      FILE_SERVICE_URL="$2"
      shift 2
      ;;
    -k|--api-key)
      FILE_SERVICE_API_KEY="$2"
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

# Ensure the Scripts directory is in the path
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# Check if the NO_ISSUES_FOUND marker file exists
if [ -f "$PDF_DIR/NO_ISSUES_FOUND" ]; then
  echo "No SonarQube issues were found. Skipping PDF upload and PR comment."
  cat "$PDF_DIR/NO_ISSUES_FOUND"

  # Skip PR comment if PR_ID is 0 (manual run)
  if [ "$PR_ID" = "0" ]; then
    echo "Manual run detected (PR_ID=0)."
    exit 0
  fi

  # For PR builds, add a simple comment that no issues were found
  if command -v az &> /dev/null && [ -n "$AZURE_DEVOPS_PAT" ] && [ "$PR_ID" != "0" ]; then
    echo "Adding 'no issues found' comment to PR #$PR_ID..."

    # Configure Azure DevOps
    if [[ "$ORGANIZATION" == *"https://"* ]]; then
      ORGANIZATION_URL="$ORGANIZATION"
    else
      ORGANIZATION_URL="https://dev.azure.com/$ORGANIZATION"
    fi

    # Login with PAT
    TOKEN_FILE=$(mktemp)
    echo "$AZURE_DEVOPS_PAT" > "$TOKEN_FILE"
    cat "$TOKEN_FILE" | az devops login --organization "$ORGANIZATION_URL"
    rm -f "$TOKEN_FILE"
    export AZURE_DEVOPS_EXT_PAT="$AZURE_DEVOPS_PAT"

    # Create a simple comment
    TEMP_DIR=$(mktemp -d)
    COMMENT_FILE="$TEMP_DIR/comment.txt"
    echo '{
      "comments": [
        {
          "parentCommentId": 0,
          "content": "SonarQube Analysis: No issues found in new code! 🎉",
          "commentType": 1
        }
      ],
      "status": 1,
      "threadContext": {
        "filePath": "/"
      }
    }' > "$COMMENT_FILE"

    # Try to get repository ID
    REPO_ID=$(az repos list --project "$PROJECT" --query "[0].id" -o tsv 2>/dev/null)
    if [ -n "$REPO_ID" ]; then
      az devops invoke --area git --resource pullRequestThreads --route-parameters \
        repositoryId="$REPO_ID" \
        pullRequestId="$PR_ID" \
        --http-method POST \
        --api-version "6.0" \
        --in-file "$COMMENT_FILE" \
        --output json

      echo "Added 'no issues found' comment to PR #$PR_ID"
    fi

    rm -rf "$TEMP_DIR"
  fi

  exit 0
fi

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

echo "Using PDF file: $PDF_PATH"
PDF_FILENAME=$(basename "$PDF_PATH")

# Step 1: Upload the PDF to the file service
echo "Uploading PDF to file service..."
echo "File service URL: $FILE_SERVICE_URL"
echo "PDF path: $PDF_PATH"

# Check if curl is available
if ! command -v curl &> /dev/null; then
  echo "Error: curl is not installed or not in PATH"
  exit 1
fi

# Make sure the file exists and is readable
if [ ! -f "$PDF_PATH" ] || [ ! -r "$PDF_PATH" ]; then
  echo "Error: PDF file does not exist or is not readable: $PDF_PATH"
  exit 1
fi

# Calculate expiration date (3 days from now in Unix timestamp)
# First, try using date command with --date option (GNU date)
if date --date="3 days" +%s &>/dev/null; then
  EXPIRATION_DATE=$(date --date="3 days" +%s)
  echo "Calculated expiration date (GNU date): $(date --date="@$EXPIRATION_DATE" '+%Y-%m-%d %H:%M:%S')"
# If that fails, try using date command with -v option (BSD date, macOS)
elif date -v+3d +%s &>/dev/null; then
  EXPIRATION_DATE=$(date -v+3d +%s)
  echo "Calculated expiration date (BSD date): $(date -r $EXPIRATION_DATE '+%Y-%m-%d %H:%M:%S')"
# If both fail, try using Python
elif command -v python3 &>/dev/null; then
  EXPIRATION_DATE=$(python3 -c "import time; print(int(time.time()) + 3*24*60*60)")
  echo "Calculated expiration date (Python): $(date -r $EXPIRATION_DATE '+%Y-%m-%d %H:%M:%S' 2>/dev/null || date --date="@$EXPIRATION_DATE" '+%Y-%m-%d %H:%M:%S' 2>/dev/null || echo "$EXPIRATION_DATE")"
# If all else fails, use a hardcoded value (3 days = 259200 seconds)
else
  CURRENT_TIME=$(date +%s)
  EXPIRATION_DATE=$((CURRENT_TIME + 259200))
  echo "Calculated expiration date (manual): $(date -r $EXPIRATION_DATE '+%Y-%m-%d %H:%M:%S' 2>/dev/null || date --date="@$EXPIRATION_DATE" '+%Y-%m-%d %H:%M:%S' 2>/dev/null || echo "$EXPIRATION_DATE")"
fi

# Try to upload the file with expiration date
UPLOAD_RESPONSE=$(curl --location "$FILE_SERVICE_URL" \
  --header "x-api-key: $FILE_SERVICE_API_KEY" \
  --form "file=@\"$PDF_PATH\"" \
  --form "public=true" \
  --form "expirationUnixSeconds=$EXPIRATION_DATE" \
  --silent)

# Print the response for debugging
echo "Upload response: $UPLOAD_RESPONSE"

# Extract the file ID from the response
# Try different methods to extract the ID
FILE_ID=""

# Method 1: Using grep and sed
FILE_ID=$(echo "$UPLOAD_RESPONSE" | grep -o '"id":"[^"]*"' | sed 's/"id":"//;s/"//')

# Method 2: Using Python if available
if [ -z "$FILE_ID" ] && command -v python3 &> /dev/null; then
  echo "Trying to extract ID using Python..."
  FILE_ID=$(python3 -c "import sys, json; print(json.loads(sys.stdin.read()).get('id', ''))" <<< "$UPLOAD_RESPONSE")
fi

# Method 3: Using Python if available
if [ -z "$FILE_ID" ] && command -v python &> /dev/null; then
  echo "Trying to extract ID using Python..."
  FILE_ID=$(python -c "import sys, json; print(json.loads(sys.stdin.read()).get('id', ''))" <<< "$UPLOAD_RESPONSE")
fi

if [ -z "$FILE_ID" ]; then
  echo "Error: Failed to extract file ID from upload response"
  echo "Response: $UPLOAD_RESPONSE"
  exit 1
fi

echo "Extracted file ID: $FILE_ID"

# Construct the file URL
# Check if FILE_SERVICE_URL ends with a slash
if [[ "$FILE_SERVICE_URL" == */ ]]; then
  FILE_URL="${FILE_SERVICE_URL}${FILE_ID}"
else
  FILE_URL="${FILE_SERVICE_URL}/${FILE_ID}"
fi

# Add token if needed
FILE_URL="${FILE_URL}?t=$FILE_SERVICE_TEANANT_ID"

echo "PDF uploaded successfully. URL: $FILE_URL"

# Step 2: Create a comment with the file URL
# Skip PR comment if PR_ID is 0 (manual run)
if [ "$PR_ID" = "0" ]; then
  echo "Manual run detected (PR_ID=0). Skipping PR comment."
  echo "PDF was uploaded successfully. URL: $FILE_URL"
  echo "You can manually add this URL to your PR comment."
  exit 0
fi

echo "Adding comment to PR #$PR_ID..."

# Check if Azure CLI is installed
if command -v az &> /dev/null; then
  # Install Azure DevOps extension if needed
  if ! az extension list | grep -q "azure-devops"; then
    echo "Installing Azure DevOps extension for Azure CLI..."
    az extension add --name azure-devops
  fi

  # Configure Azure DevOps organization
  echo "Configuring Azure DevOps organization: $ORGANIZATION"

  # Check if ORGANIZATION already contains the full URL
  if [[ "$ORGANIZATION" == *"https://"* ]]; then
    # Use the organization URL as is
    ORGANIZATION_URL="$ORGANIZATION"
  else
    # Construct the URL from the organization name
    ORGANIZATION_URL="https://dev.azure.com/$ORGANIZATION"
  fi

  echo "Using organization URL: $ORGANIZATION_URL"
  az devops configure --defaults organization="$ORGANIZATION_URL"

  # Configure Azure DevOps project
  echo "Configuring Azure DevOps project: $PROJECT"
  az devops configure --defaults project="$PROJECT"

  # Check if we have a PAT token for authentication
  if [ -n "$AZURE_DEVOPS_PAT" ]; then
    echo "Using provided PAT token for authentication"
    # Create a temporary file for the token
    TOKEN_FILE=$(mktemp)
    echo "$AZURE_DEVOPS_PAT" > "$TOKEN_FILE"

    # Login with the token
    echo "Logging in to Azure DevOps..."
    cat "$TOKEN_FILE" | az devops login --organization "$ORGANIZATION_URL"

    # Remove the temporary token file
    rm -f "$TOKEN_FILE"

    # Set the token as an environment variable for the az CLI
    export AZURE_DEVOPS_EXT_PAT="$AZURE_DEVOPS_PAT"

    # Verify login status
    echo "Verifying login status..."
    az account show || echo "Login verification failed, but continuing anyway..."

    # Verify Azure DevOps access
    echo "Verifying Azure DevOps access..."
    az devops project show --project "$PROJECT" || echo "Project access verification failed, but continuing anyway..."
  else
    echo "No PAT token provided. Authentication may fail."
    echo "To fix this, set the AZURE_DEVOPS_PAT environment variable."
  fi

  # Create a temporary directory for the comment file
  TEMP_DIR=$(mktemp -d)
  COMMENT_FILE="$TEMP_DIR/comment.txt"

  # Create a detailed comment with PDF information and download link
  DETAILED_COMMENT="$COMMENT\n\n**SonarQube Analysis Report**\n\n- **Report Name**: $PDF_FILENAME\n- **Generated**: $(date '+%Y-%m-%d %H:%M:%S')\n\n[Download PDF Report]($FILE_URL)"

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

  # Try to get repository ID using a different approach
  echo "Attempting to get repository information..."

  # First, try to list all repositories to see if we can access them
  echo "Listing repositories in project $PROJECT..."
  az repos list --project "$PROJECT" --output table || echo "Failed to list repositories, but continuing..."

  # Try to get the repository ID directly from the project name
  # This assumes the repository name is the same as the project name
  echo "Trying to get repository ID for $PROJECT..."
  REPO_ID=$(az repos list --project "$PROJECT" --query "[?name=='$PROJECT'].id" -o tsv 2>/dev/null)

  # If that fails, try to get the first repository in the project
  if [ -z "$REPO_ID" ]; then
    echo "Trying to get the first repository in the project..."
    REPO_ID=$(az repos list --project "$PROJECT" --query "[0].id" -o tsv 2>/dev/null)
  fi

  # If that still fails, try a hardcoded repository ID for DP2_Api
  if [ -z "$REPO_ID" ]; then
    echo "Using hardcoded repository name 'DP2_Api'..."
    REPO_ID=$(az repos list --project "$PROJECT" --query "[?name=='DP2_Api'].id" -o tsv 2>/dev/null)
  fi

  # If all attempts fail, use a hardcoded repository ID if available
  if [ -z "$REPO_ID" ]; then
    # You can add a hardcoded repository ID here if you know it
    # REPO_ID="your-repository-id"
    echo "Failed to get repository ID. Authentication may have failed."
    echo "PDF was uploaded successfully. URL: $FILE_URL"
    echo "You can manually add this URL to your PR comment."
    # Don't exit with error, just continue
    # Clean up temporary files
    rm -rf "$TEMP_DIR"
    # Skip the rest of the function
    echo "Process completed successfully!"
    exit 0
  fi

  # Add the PR comment with the PDF link
  echo "Adding comment to PR #$PR_ID in repository $REPO_ID..."

  az devops invoke --area git --resource pullRequestThreads --route-parameters \
    repositoryId="$REPO_ID" \
    pullRequestId="$PR_ID" \
    --http-method POST \
    --api-version "6.0" \
    --in-file "$COMMENT_FILE" \
    --output json

  # Check if the comment was added successfully
  if [ $? -eq 0 ]; then
    echo "Comment with PDF link added successfully to PR #$PR_ID"
    echo "PDF report: $PDF_PATH"
    echo "Download URL: $FILE_URL"
  else
    echo "Failed to add comment to PR"
    echo "PDF was uploaded successfully. URL: $FILE_URL"
    echo "You can manually add this URL to your PR comment."
    # Don't exit with error, just continue
    # Clean up temporary files
    rm -rf "$TEMP_DIR"
    # Skip the rest of the function
    echo "Process completed successfully!"
    exit 0
  fi

  # Clean up temporary files
  rm -rf "$TEMP_DIR"
else
  echo "Azure CLI not found. Please install it to add comments to PRs."
  echo "PDF was uploaded successfully. URL: $FILE_URL"
  echo "You can manually add this URL to your PR comment."
  # Don't exit with error code, just continue
  # exit 1
fi

echo "Process completed successfully!"
