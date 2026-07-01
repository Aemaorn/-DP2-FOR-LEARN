#!/usr/bin/env python3
"""
Attach SonarQube PDF Report to Azure DevOps Pull Request

This script uploads a PDF file to Azure DevOps and adds it as an attachment to a PR comment.
"""

import argparse
import os
import sys
import base64
import json
import requests
from datetime import datetime


class AzureDevOpsClient:
    def __init__(self, organization, project, personal_access_token):
        """Initialize the Azure DevOps client with credentials and project info."""
        # Remove any trailing slashes from organization name
        self.organization = organization.rstrip('/')

        # If organization contains the full URL, extract just the org name
        if self.organization.startswith('https://'):
            # Extract organization name from URL
            parts = self.organization.split('/')
            if 'dev.azure.com' in parts:
                idx = parts.index('dev.azure.com')
                if len(parts) > idx + 1:
                    self.organization = parts[idx + 1]
            else:
                # Try to handle visualstudio.com URLs
                for i, part in enumerate(parts):
                    if part.endswith('.visualstudio.com'):
                        self.organization = part.split('.')[0]
                        break

        self.project = project
        self.personal_access_token = personal_access_token
        self.base_url = f"https://dev.azure.com/{self.organization}/{self.project}"

        print(f"Using Azure DevOps API URL: {self.base_url}")

        # Create auth header with PAT
        auth_str = f":{personal_access_token}"
        self.auth_header = {
            'Authorization': f'Basic {base64.b64encode(auth_str.encode()).decode()}'
        }

        # Print first few characters of encoded auth header for debugging
        encoded_auth = base64.b64encode(auth_str.encode()).decode()
        print(f"Auth header (first 10 chars): {encoded_auth[:10]}...")

    def get_pull_request(self, pull_request_id):
        """Get pull request details."""
        url = f"{self.base_url}/_apis/git/pullrequests/{pull_request_id}?api-version=6.0"
        print(f"Getting pull request details from URL: {url}")

        try:
            # First attempt with standard headers
            response = requests.get(url, headers=self.auth_header)

            if response.status_code == 200:
                return response.json()
            else:
                print(f"Failed to get pull request. Status code: {response.status_code}")
                print(f"Response: {response.text}")

                # If authentication failed, try with Basic Auth directly
                if response.status_code == 401:
                    print("Trying alternative authentication method for PR details...")
                    from requests.auth import HTTPBasicAuth
                    auth = HTTPBasicAuth('', self.personal_access_token)
                    response = requests.get(url, auth=auth)

                    if response.status_code == 200:
                        return response.json()
                    else:
                        print(f"Alternative authentication also failed. Status code: {response.status_code}")
                        print(f"Response: {response.text}")

                # Try a different API URL format (for older Azure DevOps instances)
                print("Trying alternative API URL format...")
                alt_url = f"https://{self.organization}.visualstudio.com/{self.project}/_apis/git/pullrequests/{pull_request_id}?api-version=6.0"
                print(f"Alternative URL: {alt_url}")
                response = requests.get(alt_url, headers=self.auth_header)

                if response.status_code == 200:
                    return response.json()
                else:
                    print(f"Alternative URL also failed. Status code: {response.status_code}")
                    print(f"Response: {response.text}")

                return None
        except Exception as e:
            print(f"Exception during pull request retrieval: {str(e)}")
            return None

    def add_comment_with_attachment(self, pull_request_id, comment_text, file_path):
        """Add a comment with an attachment to a pull request."""
        # First, upload the file to get an attachment ID
        attachment_id = self._upload_attachment(pull_request_id, file_path)
        if not attachment_id:
            return False

        # Now create a comment with the attachment
        repository_id = self._get_repository_id(pull_request_id)
        if not repository_id:
            return False

        url = f"{self.base_url}/_apis/git/repositories/{repository_id}/pullRequests/{pull_request_id}/threads?api-version=6.0"

        # Create comment with attachment
        comment_with_attachment = {
            "comments": [
                {
                    "parentCommentId": 0,
                    "content": comment_text,
                    "commentType": 1
                }
            ],
            "status": 1,  # Active
            "threadContext": {
                "filePath": "/",  # Root of the repository
                "leftFileEnd": None,
                "leftFileStart": None,
                "rightFileEnd": None,
                "rightFileStart": None
            },
            "pullRequestThreadContext": {
                "changeTrackingId": 0,
                "iterationContext": {
                    "firstComparingIteration": 1,
                    "secondComparingIteration": 1
                }
            },
            "properties": {
                "Microsoft.TeamFoundation.Discussion.UniqueID": {
                    "$type": "System.String",
                    "$value": f"SonarQube-Report-{datetime.now().strftime('%Y%m%d%H%M%S')}"
                },
                "Microsoft.TeamFoundation.Discussion.SupportsMarkdown": {
                    "$type": "System.Boolean",
                    "$value": "True"
                },
                "Microsoft.TeamFoundation.Discussion.Attachments": {
                    "$type": "System.String",
                    "$value": json.dumps([{
                        "attachmentId": attachment_id,
                        "fileName": os.path.basename(file_path),
                        "fileSize": os.path.getsize(file_path)
                    }])
                }
            }
        }

        response = requests.post(url, headers={**self.auth_header, 'Content-Type': 'application/json'},
                                json=comment_with_attachment)

        if response.status_code in [200, 201]:
            print(f"Successfully added comment with attachment to PR #{pull_request_id}")
            return True
        else:
            print(f"Failed to add comment. Status code: {response.status_code}")
            print(f"Response: {response.text}")
            return False

    def _upload_attachment(self, pull_request_id, file_path):
        """Upload a file as an attachment and return the attachment ID."""
        if not os.path.exists(file_path):
            print(f"File not found: {file_path}")
            return None

        repository_id = self._get_repository_id(pull_request_id)
        if not repository_id:
            return None

        # Construct the URL for uploading attachments
        url = f"{self.base_url}/_apis/git/repositories/{repository_id}/pullRequests/{pull_request_id}/attachments?fileName={os.path.basename(file_path)}&api-version=6.0"
        print(f"Uploading attachment to URL: {url}")

        # Read the file content
        with open(file_path, 'rb') as file:
            file_content = file.read()
            file_size = len(file_content)
            print(f"File size: {file_size} bytes")

        # Prepare headers
        headers = {**self.auth_header, 'Content-Type': 'application/octet-stream'}
        print(f"Request headers: {', '.join([f'{k}: {v[:10]}...' if k == 'Authorization' else f'{k}: {v}' for k, v in headers.items()])}")

        # Try alternative authentication method if the first one fails
        try:
            # First attempt with standard headers
            response = requests.post(url, headers=headers, data=file_content)

            if response.status_code in [200, 201]:
                attachment_data = response.json()
                return attachment_data.get('attachmentId')
            else:
                print(f"Failed to upload attachment. Status code: {response.status_code}")
                print(f"Response: {response.text}")

                # If authentication failed, try with Basic Auth directly
                if response.status_code == 401:
                    print("Trying alternative authentication method...")
                    from requests.auth import HTTPBasicAuth
                    auth = HTTPBasicAuth('', self.personal_access_token)
                    response = requests.post(url, auth=auth, headers={'Content-Type': 'application/octet-stream'}, data=file_content)

                    if response.status_code in [200, 201]:
                        attachment_data = response.json()
                        return attachment_data.get('attachmentId')
                    else:
                        print(f"Alternative authentication also failed. Status code: {response.status_code}")
                        print(f"Response: {response.text}")

                return None
        except Exception as e:
            print(f"Exception during attachment upload: {str(e)}")
            return None

    def _get_repository_id(self, pull_request_id):
        """Get the repository ID from the pull request."""
        pr_data = self.get_pull_request(pull_request_id)
        if pr_data:
            return pr_data.get('repository', {}).get('id')
        return None


def main():
    """Main function to parse arguments and run the client."""
    parser = argparse.ArgumentParser(description="Attach SonarQube PDF Report to Azure DevOps Pull Request")
    parser.add_argument("--organization", required=True, help="Azure DevOps organization name")
    parser.add_argument("--project", required=True, help="Azure DevOps project name")
    parser.add_argument("--token", required=True, help="Azure DevOps personal access token")
    parser.add_argument("--pr-id", required=True, help="Pull Request ID", type=int)
    parser.add_argument("--pdf-path", required=True, help="Path to the PDF report file")
    parser.add_argument("--comment", default="SonarQube Analysis Report",
                        help="Comment text to include with the attachment")

    args = parser.parse_args()

    # Validate PDF file exists
    if not os.path.exists(args.pdf_path):
        print(f"Error: PDF file not found at {args.pdf_path}")
        return 1

    # Create Azure DevOps client
    client = AzureDevOpsClient(args.organization, args.project, args.token)

    # Add comment with attachment
    success = client.add_comment_with_attachment(args.pr_id, args.comment, args.pdf_path)

    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
