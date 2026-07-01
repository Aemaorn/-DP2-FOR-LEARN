# SonarQube Issue Reporter

This tool automates the process of:
1. Logging into SonarQube
2. Checking for critical and high issues in new code
3. Generating a PDF report for PR comments

## Prerequisites

- Python 3.6+
- Chrome browser installed
- SonarQube server running and accessible

## Installation

The script will automatically set up a Python virtual environment and install the required dependencies when you run it for the first time.

Required Python packages (installed automatically):
- selenium
- webdriver-manager
- fpdf2
- requests

## Usage

### Using the Shell Script

```bash
./generate_sonar_report.sh --project "YOUR_PROJECT_KEY" --username "YOUR_USERNAME" --password "YOUR_PASSWORD"
```

### Options

- `-p, --project PROJECT_KEY`: SonarQube project key (required)
- `-u, --username USERNAME`: SonarQube username (required)
- `-w, --password PASSWORD`: SonarQube password (required)
- `-f, --password-file FILE`: File containing SonarQube password (alternative to -w)
- `-s, --server URL`: SonarQube server URL (default: http://192.168.20.97:9001)
- `-o, --output-dir DIR`: Output directory for reports (default: ./sonar-reports)
- `-h, --help`: Display help message

**Note**: If your password contains special characters (like `!`, `$`, `@`, etc.), use the `--password-file` option or enclose the password in single quotes to prevent shell interpretation.

### Example

```bash
# For backend project
./generate_sonar_report.sh --project "GHB-DP2-Backend" --username "admin" --password "admin123"

# For frontend project
./generate_sonar_report.sh --project "GHB-DP2-Frontend" --username "admin" --password "admin123"

# Using password with special characters
./generate_sonar_report.sh --project "DP2_Api" --username "admin" --password 'u%LJ!SPBmA7cGfSRJ@c6ag$n5#mwNv^4'

# Using password file
echo "your_complex_password" > password.txt
./generate_sonar_report.sh --project "DP2_Api" --username "admin" --password-file password.txt
rm password.txt  # Remove the password file after use
```

## Integration with CI/CD

You can integrate this tool with your CI/CD pipeline by adding a step after the SonarQube analysis:

```yaml
- task: Bash@3
  displayName: 'Generate SonarQube Issues Report'
  inputs:
    filePath: 'Scripts/generate_sonar_report.sh'
    arguments: '--project "$(ProjectName)" --username "$(SonarQubeUsername)" --password "$(SonarQubePassword)"'
```

## Adding the Report to PR Comments

After generating the report, you can add it to your PR comments using the provided script.

### Using the Attachment Script

#### Option 1: Using REST API (requires PAT)

```bash
./attach_to_azure_pr.sh --organization "YourOrg" --project "YourProject" --token "YourPAT" --pr-id "123"
```

#### Option 2: Using Azure CLI (recommended)

This approach uses the Azure CLI, which handles authentication more reliably:

```bash
./attach_to_azure_pr_cli.sh --organization "YourOrg" --project "YourProject" --pr-id "123"
```

**Note**: This requires the Azure CLI to be installed and authenticated. If you haven't authenticated yet, run `az login` first.

#### Option 3: Upload to File Service and Add Link (best)

This approach uploads the PDF to your file service and adds a download link to the PR comment:

```bash
./attach_to_azure_pr_with_upload.sh --organization "YourOrg" --project "YourProject" --pr-id "123"
```

**Benefits**:
- PDF is accessible via a direct download link
- No authentication issues with Azure DevOps
- Works with any PDF size
- Provides a better user experience

### Options

#### Common Options (All Scripts)

- `-o, --organization ORG`: Azure DevOps organization name (required)
- `-p, --project PROJECT`: Azure DevOps project name (required)
- `-r, --pr-id ID`: Pull Request ID (required)
- `-f, --pdf-file FILE`: Specific PDF file to attach (optional)
- `-d, --pdf-dir DIR`: Directory containing PDF reports (default: ./sonar-reports)
- `-c, --comment TEXT`: Comment text (default: 'SonarQube Analysis Report - Critical and High Issues')
- `-h, --help`: Display help message

#### REST API Script Options (`attach_to_azure_pr.sh`)

- `-t, --token TOKEN`: Azure DevOps personal access token (required)

#### File Service Upload Options (`attach_to_azure_pr_with_upload.sh`)

- `-u, --file-service-url URL`: File service URL (default: https://file-service.ch-core.staging.codehard.co.th/api/files/)
- `-k, --api-key KEY`: File service API key (default: fs_RaDTest_Qy3uZpKhwLxqPnAfHSR9kr)

### Examples

#### Using REST API (Option 1)

```bash
# Attach the latest PDF report
./attach_to_azure_pr.sh --organization "MyCompany" --project "DP2" --token "abcdef123456" --pr-id "42"

# Attach a specific PDF report
./attach_to_azure_pr.sh --organization "MyCompany" --project "DP2" --token "abcdef123456" --pr-id "42" --pdf-file "./sonar-reports/sonarqube_issues_DP2-Frontend_20250515_154506.pdf"
```

#### Using Azure CLI (Option 2)

```bash
# Attach the latest PDF report
./attach_to_azure_pr_cli.sh --organization "MyCompany" --project "DP2" --pr-id "42"

# Attach a specific PDF report
./attach_to_azure_pr_cli.sh --organization "MyCompany" --project "DP2" --pr-id "42" --pdf-file "./sonar-reports/sonarqube_issues_DP2-Frontend_20250515_154506.pdf"
```

#### Using File Service Upload (Option 3 - Recommended)

```bash
# Upload and attach the latest PDF report
./attach_to_azure_pr_with_upload.sh --organization "MyCompany" --project "DP2" --pr-id "42"

# Upload and attach a specific PDF report
./attach_to_azure_pr_with_upload.sh --organization "MyCompany" --project "DP2" --pr-id "42" --pdf-file "./sonar-reports/sonarqube_issues_DP2-Frontend_20250515_154506.pdf"

# Use a custom file service URL and API key
./attach_to_azure_pr_with_upload.sh --organization "MyCompany" --project "DP2" --pr-id "42" --file-service-url "https://my-file-service.com/api/files/" --api-key "my-api-key"
```

### Creating a Personal Access Token (PAT)

To use this script, you need an Azure DevOps Personal Access Token with the following permissions:
- Pull Request (Read & Write)
- Code (Read)

To create a PAT:
1. Go to Azure DevOps and click on your profile picture in the top right
2. Select "Personal access tokens"
3. Click "New Token"
4. Give it a name (e.g., "SonarQube Report Bot")
5. Set the organization and expiration
6. Under "Scopes", select "Custom defined"
7. Check "Pull Request (Read & Write)" and "Code (Read)"
8. Click "Create"
9. Copy the token (you won't be able to see it again)

### PAT Format and Usage

The PAT should be used exactly as provided by Azure DevOps, without any modifications. For example:
```
2R4JsPdh1MRtKxLN1HXuyL0YkPglNNKF9Lt2ydElTTdcLLYwTuS5JQQJ99BEACA
```

When using the token in the script:
```bash
./attach_to_azure_pr.sh --organization "codehard" --project "GHB-DP-2" --token "YOUR_PAT" --pr-id "16409"
```

### Organization and Project Names

- **Organization Name**: This is your Azure DevOps organization name (e.g., "codehard")
- **Project Name**: This is your Azure DevOps project name (e.g., "GHB-DP-2")

Make sure to use the exact names as they appear in your Azure DevOps URL:
```
https://dev.azure.com/{organization}/{project}
```

### Troubleshooting Authentication Issues

If you encounter a 401 Unauthorized error:

1. **Verify PAT Permissions**: Ensure your PAT has "Pull Request (Read & Write)" and "Code (Read)" permissions
2. **Check PAT Expiration**: Make sure your PAT hasn't expired
3. **Verify Organization/Project Names**: Double-check the organization and project names
4. **Try with Full URL**: If using just the organization name doesn't work, try with the full URL:
   ```bash
   ./attach_to_azure_pr.sh --organization "https://dev.azure.com/codehard" --project "GHB-DP-2" --token "YOUR_PAT" --pr-id "16409"
   ```
5. **Check for Special Characters**: If your PAT contains special characters, enclose it in single quotes:
   ```bash
   ./attach_to_azure_pr.sh --organization "codehard" --project "GHB-DP-2" --token 'YOUR_PAT' --pr-id "16409"
   ```

## Troubleshooting

- If you encounter issues with Chrome not starting, make sure Chrome is installed and accessible.
- If the login fails, check your SonarQube credentials and server URL.
- For other issues, check the console output for error messages.
