#!/usr/bin/env python3
"""
SonarQube Issue Reporter

This script automates the process of:
1. Logging into SonarQube
2. Checking for critical and high issues in new code
3. Generating a PDF report for PR comments
"""

import argparse
import os
import sys
import time
import requests
import re
import html
from datetime import datetime
from fpdf import FPDF, XPos, YPos
from html.parser import HTMLParser
import tempfile
import shutil
import base64
import urllib.request


class HTMLStripper(HTMLParser):
    """Helper class to strip HTML tags from text."""

    def __init__(self):
        super().__init__()
        self.reset()
        self.strict = False
        self.convert_charrefs = True
        self.text = []

    def handle_data(self, data):
        self.text.append(data)

    def get_text(self):
        return ''.join(self.text)


def strip_html_tags(html_text):
    """Remove HTML tags from text."""
    if not html_text:
        return ""

    # First, unescape HTML entities like &amp;
    unescaped_text = html.unescape(html_text)

    # Then strip HTML tags
    stripper = HTMLStripper()
    stripper.feed(unescaped_text)
    return stripper.get_text()


class SonarQubeReporter:
    def __init__(self, sonar_url, project_key, username, password, output_dir=None):
        """Initialize the SonarQube reporter with credentials and project info."""
        self.sonar_url = sonar_url.rstrip('/')
        self.project_key = project_key
        self.username = username
        self.password = password

        if output_dir is None:
            self.output_dir = os.getcwd()
        else:
            self.output_dir = output_dir
            os.makedirs(output_dir, exist_ok=True)

        # Create a session for making requests
        self.session = requests.Session()

    def login(self):
        """Login to SonarQube using API."""
        try:
            print(f"Logging into SonarQube at {self.sonar_url}...")

            # First, get the login page to get any cookies and CSRF token
            login_page_response = self.session.get(f"{self.sonar_url}/sessions/new")

            # Extract XSRF token from cookies if available
            xsrf_token = None
            if 'XSRF-TOKEN' in self.session.cookies:
                xsrf_token = self.session.cookies['XSRF-TOKEN']
                print("Found XSRF token in cookies")

            # Prepare headers with XSRF token if available
            headers = {}
            if xsrf_token:
                headers['X-XSRF-TOKEN'] = xsrf_token

            # Perform login
            login_data = {
                'login': self.username,
                'password': self.password
            }

            login_response = self.session.post(
                f"{self.sonar_url}/api/authentication/login",
                data=login_data,
                headers=headers
            )

            if login_response.status_code == 200:
                print("Login successful!")

                # Print cookies for debugging
                print("Session cookies:")
                for cookie_name, cookie_value in self.session.cookies.items():
                    print(f"  {cookie_name}: {cookie_value[:10]}..." if len(cookie_value) > 10 else f"  {cookie_name}: {cookie_value}")

                return True
            else:
                print(f"Login failed with status code: {login_response.status_code}")
                try:
                    print(f"Response body: {login_response.text}")
                except:
                    print("Could not print response body")
                return False
        except Exception as e:
            print(f"Login failed: {str(e)}")
            return False

    def get_project_issues(self):
        """Get project issues using the SonarQube API."""
        try:
            print(f"Getting issues for project {self.project_key}...")

            # Use the SonarQube API to get issues
            api_url = f"{self.sonar_url}/api/issues/search"
            params = {
                'components': self.project_key,  # Using 'components' as seen in the web app
                'inNewCodePeriod': 'true',  # Using 'inNewCodePeriod' as seen in the web app
                'issueStatuses': 'CONFIRMED,OPEN',
                'impactSeverities': 'BLOCKER,HIGH,MEDIUM',
                'ps': 100,  # Page size
                'facets': 'cleanCodeAttributeCategories,impactSoftwareQualities,severities,types,impactSeverities,codeVariants',
                'additionalFields': '_all'
            }

            # Prepare headers with XSRF token if available
            headers = {}
            if 'XSRF-TOKEN' in self.session.cookies:
                headers['X-XSRF-TOKEN'] = self.session.cookies['XSRF-TOKEN']

            response = self.session.get(api_url, params=params, headers=headers)

            if response.status_code == 200:
                issues_data = response.json()
                print(f"Successfully retrieved {len(issues_data.get('issues', []))} issues!")
                return issues_data.get('issues', [])
            else:
                print(f"Failed to get issues. Status code: {response.status_code}")
                try:
                    print(f"Response body: {response.text}")
                except:
                    print("Could not print response body")
                return []
        except Exception as e:
            print(f"Failed to get project issues: {str(e)}")
            return []

    def get_source_code(self, component_key, line_number):
        """Fetch source code for a specific component and line number."""
        try:
            if not line_number or line_number == 'N/A':
                return None

            # Convert line_number to integer if it's a string
            if isinstance(line_number, str):
                try:
                    line_number = int(line_number)
                except ValueError:
                    return None

            # Calculate the range of lines to fetch (5 lines before and after the issue)
            start_line = max(1, line_number - 5)
            end_line = line_number + 5

            # Use the SonarQube API to get source code
            api_url = f"{self.sonar_url}/api/sources/lines"
            params = {
                'key': component_key,
                'from': start_line,
                'to': end_line
            }

            # Prepare headers with XSRF token if available
            headers = {}
            if 'XSRF-TOKEN' in self.session.cookies:
                headers['X-XSRF-TOKEN'] = self.session.cookies['XSRF-TOKEN']

            response = self.session.get(api_url, params=params, headers=headers)

            if response.status_code == 200:
                source_data = response.json()
                if 'sources' in source_data:
                    # Format the source code with line numbers
                    source_lines = []
                    for src_line in source_data['sources']:
                        line_num = src_line.get('line', '')
                        line_code = src_line.get('code', '')

                        # Clean HTML from the source code
                        clean_code = strip_html_tags(line_code)

                        # Replace multiple spaces with a single space to avoid excessive spacing
                        clean_code = re.sub(r'\s+', ' ', clean_code).strip()

                        # Highlight the issue line
                        if line_num == line_number:
                            source_lines.append(f">> {line_num}: {clean_code}")
                        else:
                            source_lines.append(f"   {line_num}: {clean_code}")

                    return "\n".join(source_lines)
            return None
        except Exception as e:
            print(f"Failed to get source code: {str(e)}")
            return None

    def process_issues(self, issues_data):
        """Process the issues data from the API."""
        try:
            print("Processing issue information...")

            processed_issues = []
            for issue in issues_data:
                try:
                    # Extract relevant information
                    severity = issue.get('severity', 'UNKNOWN')
                    message = issue.get('message', 'No message')
                    component = issue.get('component', 'Unknown component')
                    rule = issue.get('rule', 'Unknown rule')

                    # Get component name (file name) from the full path
                    component_name = component.split(':')[-1]

                    # Extract additional details for the detailed view
                    author = issue.get('author', 'Unknown')
                    creation_date = issue.get('creationDate', 'Unknown date')
                    line = issue.get('line', 'N/A')
                    status = issue.get('status', 'Unknown')
                    type_name = issue.get('type', 'Unknown type')

                    # Get impact information if available
                    impact = {}
                    if 'impacts' in issue and issue['impacts']:
                        for impact_item in issue['impacts']:
                            software_quality = impact_item.get('softwareQuality', 'Unknown')
                            severity_level = impact_item.get('severity', 'Unknown')
                            impact[software_quality] = severity_level

                    # Get code snippets if available
                    code_snippet = None
                    if 'flows' in issue and issue['flows']:
                        for flow in issue['flows']:
                            if 'locations' in flow and flow['locations']:
                                for location in flow['locations']:
                                    if 'textRange' in location and 'msg' in location:
                                        # Get the snippet and clean HTML tags
                                        raw_snippet = location.get('msg', '')
                                        code_snippet = strip_html_tags(raw_snippet)
                                        break
                                if code_snippet:
                                    break

                    # Get source code for the issue
                    source_code = self.get_source_code(component, line)

                    processed_issues.append({
                        "severity": severity,
                        "message": message,
                        "component": component_name,
                        "rule": rule,
                        "author": author,
                        "creation_date": creation_date,
                        "line": line,
                        "status": status,
                        "type": type_name,
                        "impact": impact,
                        "code_snippet": code_snippet,
                        "source_code": source_code,
                        "full_component": component,
                        "raw_data": issue  # Store the raw data for any additional details
                    })
                except Exception as e:
                    print(f"Error processing an issue: {str(e)}")
                    continue

            print(f"Processed {len(processed_issues)} issues")
            return processed_issues
        except Exception as e:
            print(f"Failed to process issues: {str(e)}")
            return []

    def generate_pdf_report(self, issues):
        """Generate a PDF report with the extracted issues."""
        try:
            print("Generating PDF report...")

            # Create a custom PDF class with Unicode support
            class UnicodePDF(FPDF):
                def __init__(self):
                    super().__init__()
                    # Add a Unicode font that supports Thai characters
                    self.add_unicode_fonts()

                def add_unicode_fonts(self):
                    # Create a temporary directory for fonts
                    self.font_dir = tempfile.mkdtemp()

                    # Download Noto Sans Thai font which supports Thai characters
                    font_found = False

                    try:
                        # URL for Noto Sans Thai font from Google Fonts
                        font_url = "https://github.com/google/fonts/raw/main/ofl/notosansthai/NotoSansThai-Regular.ttf"
                        font_bold_url = "https://github.com/google/fonts/raw/main/ofl/notosansthai/NotoSansThai-Bold.ttf"

                        # Download the regular font
                        regular_font_path = os.path.join(self.font_dir, 'NotoSansThai-Regular.ttf')
                        print(f"Downloading Noto Sans Thai font...")
                        urllib.request.urlretrieve(font_url, regular_font_path)

                        # Download the bold font
                        bold_font_path = os.path.join(self.font_dir, 'NotoSansThai-Bold.ttf')
                        urllib.request.urlretrieve(font_bold_url, bold_font_path)

                        # Add the fonts to FPDF
                        self.add_font('NotoSansThai', '', regular_font_path, uni=True)
                        self.add_font('NotoSansThai', 'B', bold_font_path, uni=True)

                        font_found = True
                        print(f"Successfully downloaded and added Noto Sans Thai font")
                    except Exception as e:
                        print(f"Error downloading font: {str(e)}")

                    # If downloading fails, try to find DejaVu Sans font in the system
                    if not font_found:
                        dejavu_paths = [
                            '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',  # Linux
                            '/usr/share/fonts/TTF/DejaVuSans.ttf',              # Arch Linux
                            '/Library/Fonts/DejaVuSans.ttf',                    # macOS
                            'C:\\Windows\\Fonts\\DejaVuSans.ttf',               # Windows
                        ]

                        for path in dejavu_paths:
                            if os.path.exists(path):
                                # Copy the font to our temporary directory
                                dest_path = os.path.join(self.font_dir, 'DejaVuSans.ttf')
                                shutil.copy(path, dest_path)
                                self.add_font('DejaVuSans', '', dest_path, uni=True)
                                self.add_font('DejaVuSans', 'B', dest_path, uni=True)
                                self.add_font('DejaVuSans', 'I', dest_path, uni=True)
                                font_found = True
                                print(f"Using DejaVu Sans font from: {path}")
                                break

                    if not font_found:
                        print("No Unicode fonts found, using default fonts")
                        # If no Unicode fonts are found, we'll use the default fonts
                        # and handle Unicode errors by replacing problematic characters

                def cleanup(self):
                    # Clean up the temporary directory
                    if hasattr(self, 'font_dir') and os.path.exists(self.font_dir):
                        shutil.rmtree(self.font_dir)

                def handle_unicode(self, text):
                    # Handle Unicode text safely
                    if text is None:
                        return ""

                    # Convert to string if it's not already
                    text = str(text)

                    # If we have Noto Sans Thai or DejaVu Sans, we can use the text as is
                    if hasattr(self, 'font_dir'):
                        # Check if we're using a font that supports Thai
                        current_font = self.font_family.lower()
                        if 'notosansthai' in current_font or 'dejavusans' in current_font:
                            return text

                    # For other fonts, we need to be careful with non-ASCII characters
                    try:
                        # Try to use the text as is, but catch any errors
                        self.get_string_width(text)
                        return text
                    except Exception:
                        # If that fails, replace problematic characters
                        result = ""
                        for char in text:
                            try:
                                # Try each character individually
                                self.get_string_width(char)
                                result += char
                            except:
                                # Replace characters that cause errors
                                if ord(char) < 128:
                                    result += char
                                else:
                                    result += '?'
                        return result

            # Create PDF object with Unicode support
            pdf = UnicodePDF()
            pdf.add_page()

            # Set font - try Noto Sans Thai first, then DejaVu Sans, then fall back to Arial
            try:
                pdf.set_font("NotoSansThai", "B", 16)
            except:
                try:
                    pdf.set_font("DejaVuSans", "B", 16)
                except:
                    pdf.set_font("Arial", "B", 16)

            # Title
            pdf.cell(0, 10, pdf.handle_unicode(f"SonarQube Issues Report - {self.project_key}"), 0, 1, "C")
            try:
                pdf.set_font("NotoSansThai", "", 10)  # No italic for Noto Sans Thai
            except:
                try:
                    pdf.set_font("DejaVuSans", "I", 10)
                except:
                    pdf.set_font("Arial", "I", 10)
            pdf.cell(0, 10, f"Generated on {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}", 0, 1, "C")

            # Add issue count
            try:
                pdf.set_font("NotoSansThai", "B", 12)
            except:
                try:
                    pdf.set_font("DejaVuSans", "B", 12)
                except:
                    pdf.set_font("Arial", "B", 12)
            pdf.cell(0, 10, f"Total Issues: {len(issues)}", 0, 1)

            # Issues summary table
            if issues:
                try:
                    pdf.set_font("DejaVuSans", "B", 11)
                except:
                    pdf.set_font("Arial", "B", 11)
                pdf.cell(30, 10, "Severity", 1)
                pdf.cell(100, 10, "Message", 1)
                pdf.cell(60, 10, "Component", 1)
                pdf.ln()

                try:
                    pdf.set_font("DejaVuSans", "", 10)
                except:
                    pdf.set_font("Arial", "", 10)
                for i, issue in enumerate(issues):
                    # Calculate row height based on message length
                    message = pdf.handle_unicode(issue["message"])
                    message_lines = self._wrap_text(message, 95, pdf)
                    component = pdf.handle_unicode(issue["component"])
                    component_lines = self._wrap_text(component, 55, pdf)

                    # Calculate row height (each line is about 6 points high)
                    row_height = max(len(message_lines), len(component_lines), 1) * 6
                    row_height = max(row_height, 10)  # Minimum height of 10

                    # Get starting y position to calculate when to draw borders
                    start_y = pdf.get_y()

                    # Severity (fixed height)
                    pdf.cell(30, row_height, pdf.handle_unicode(issue["severity"]), 1, 0, 'L')

                    # Message with word wrapping
                    message_x = pdf.get_x()
                    pdf.multi_cell(100, 6, message, 0, 'L')

                    # Draw border around message cell
                    pdf.rect(message_x, start_y, 100, row_height)

                    # Move to position for component
                    pdf.set_xy(message_x + 100, start_y)

                    # Component with word wrapping
                    component_x = pdf.get_x()
                    pdf.multi_cell(60, 6, component, 0, 'L')

                    # Draw border around component cell
                    pdf.rect(component_x, start_y, 60, row_height)

                    # Move to next row
                    pdf.set_y(start_y + row_height)

                # Add detailed information for each issue
                pdf.add_page()
                try:
                    pdf.set_font("DejaVuSans", "B", 14)
                except:
                    pdf.set_font("Arial", "B", 14)
                pdf.cell(0, 10, "Detailed Issue Information", 0, 1, "C")
                pdf.ln(5)

                for i, issue in enumerate(issues):
                    # Issue number and title
                    try:
                        pdf.set_font("DejaVuSans", "B", 12)
                    except:
                        pdf.set_font("Arial", "B", 12)
                    rule_name = pdf.handle_unicode(issue['rule'].split(':')[-1])
                    pdf.cell(0, 10, pdf.handle_unicode(f"Issue #{i+1}: {issue['severity']} - {rule_name}"), 0, 1)

                    # Issue details
                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "Message:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)

                    # Handle multi-line messages
                    message_lines = self._wrap_text(pdf.handle_unicode(issue["message"]), 150, pdf)
                    pdf.cell(0, 8, message_lines[0], 0, 1)
                    for line in message_lines[1:]:
                        pdf.cell(40, 8, "", 0)
                        pdf.cell(0, 8, line, 0, 1)

                    # File and location
                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "File:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)
                    pdf.cell(0, 8, pdf.handle_unicode(issue["full_component"]), 0, 1)

                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "Line:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)
                    pdf.cell(0, 8, str(issue["line"]), 0, 1)

                    # Status and type
                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "Status:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)
                    pdf.cell(60, 8, pdf.handle_unicode(issue["status"]), 0)

                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "Type:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)
                    pdf.cell(0, 8, pdf.handle_unicode(issue["type"]), 0, 1)

                    # Author and creation date
                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "Author:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)
                    pdf.cell(60, 8, pdf.handle_unicode(issue["author"]), 0)

                    try:
                        pdf.set_font("DejaVuSans", "B", 10)
                    except:
                        pdf.set_font("Arial", "B", 10)
                    pdf.cell(40, 8, "Created:", 0)
                    try:
                        pdf.set_font("DejaVuSans", "", 10)
                    except:
                        pdf.set_font("Arial", "", 10)

                    # Format date if possible
                    creation_date = issue["creation_date"]
                    if creation_date != "Unknown date":
                        try:
                            # Convert ISO format to readable date
                            date_obj = datetime.fromisoformat(creation_date.replace('Z', '+00:00'))
                            creation_date = date_obj.strftime('%Y-%m-%d %H:%M')
                        except:
                            pass

                    pdf.cell(0, 8, pdf.handle_unicode(creation_date), 0, 1)

                    # Impact information if available
                    if issue["impact"]:
                        try:
                            pdf.set_font("DejaVuSans", "B", 10)
                        except:
                            pdf.set_font("Arial", "B", 10)
                        pdf.cell(40, 8, "Impact:", 0)
                        try:
                            pdf.set_font("DejaVuSans", "", 10)
                        except:
                            pdf.set_font("Arial", "", 10)

                        impact_text = ", ".join([f"{pdf.handle_unicode(quality)}: {pdf.handle_unicode(severity)}"
                                               for quality, severity in issue["impact"].items()])
                        pdf.cell(0, 8, impact_text, 0, 1)

                    # Source code preview if available
                    if issue["source_code"]:
                        pdf.ln(2)
                        try:
                            pdf.set_font("DejaVuSans", "B", 10)
                        except:
                            pdf.set_font("Arial", "B", 10)
                        pdf.cell(0, 8, "Source Code:", 0, 1)
                        try:
                            pdf.set_font("DejaVuSans-Mono", "", 9)
                        except:
                            try:
                                pdf.set_font("DejaVuSans", "", 9)
                            except:
                                pdf.set_font("Courier", "", 9)

                        # Handle multi-line source code
                        source_lines = pdf.handle_unicode(issue["source_code"]).split('\n')
                        for line in source_lines:
                            pdf.cell(10, 6, "", 0)
                            pdf.cell(0, 6, line, 0, 1)

                    # Code snippet if available
                    elif issue["code_snippet"]:
                        pdf.ln(2)
                        try:
                            pdf.set_font("DejaVuSans", "B", 10)
                        except:
                            pdf.set_font("Arial", "B", 10)
                        pdf.cell(0, 8, "Code Snippet:", 0, 1)
                        try:
                            pdf.set_font("DejaVuSans-Mono", "", 9)
                        except:
                            try:
                                pdf.set_font("DejaVuSans", "", 9)
                            except:
                                pdf.set_font("Courier", "", 9)

                        # Handle multi-line code snippets
                        snippet_lines = pdf.handle_unicode(issue["code_snippet"]).split('\n')
                        for line in snippet_lines:
                            pdf.cell(10, 6, "", 0)
                            pdf.cell(0, 6, line, 0, 1)

                    # Add separator between issues
                    pdf.ln(5)
                    pdf.cell(0, 0, "", 1, 1)  # Draw a line
                    pdf.ln(5)

                    # Add a new page after every 3 issues (except the last one)
                    if (i + 1) % 3 == 0 and i < len(issues) - 1:
                        pdf.add_page()
            else:
                try:
                    pdf.set_font("DejaVuSans", "", 12)
                except:
                    pdf.set_font("Arial", "", 12)
                pdf.cell(0, 10, "No issues found!", 0, 1)

            # Save the PDF
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            pdf_filename = f"{self.output_dir}/sonarqube_issues_{self.project_key}_{timestamp}.pdf"
            pdf.output(pdf_filename)

            # Clean up temporary font directory if it exists
            if hasattr(pdf, 'cleanup'):
                pdf.cleanup()

            print(f"PDF report generated: {pdf_filename}")
            return pdf_filename
        except Exception as e:
            print(f"Failed to generate PDF report: {str(e)}")
            return None

    def _wrap_text(self, text, width, pdf):
        """Helper function to wrap text to fit within a specified width."""
        words = text.split()
        lines = []
        line = ""

        for word in words:
            if pdf.get_string_width(line + " " + word) < width:
                line += " " + word if line else word
            else:
                lines.append(line)
                line = word

        if line:
            lines.append(line)

        return lines

    def run(self):
        """Run the full workflow."""
        try:
            if not self.login():
                return None

            # Get issues from the API
            issues_data = self.get_project_issues()
            if not issues_data:
                print("No issues found or failed to retrieve issues.")
                # Create a marker file to indicate no issues were found
                marker_file = os.path.join(self.output_dir, "NO_ISSUES_FOUND")
                with open(marker_file, "w") as f:
                    f.write("No SonarQube issues were found in the new code.")
                print(f"Created marker file: {marker_file}")
                # Return special code for no issues
                return "NO_ISSUES"

            # Process the issues
            processed_issues = self.process_issues(issues_data)
            if not processed_issues:
                print("Failed to process issues.")
                return None

            # Generate the PDF report
            pdf_path = self.generate_pdf_report(processed_issues)
            return pdf_path
        except Exception as e:
            print(f"Error in workflow: {str(e)}")
            return None


def main():
    """Main function to parse arguments and run the reporter."""
    parser = argparse.ArgumentParser(description="Generate SonarQube issues report")
    parser.add_argument("--url", required=True, help="SonarQube URL")
    parser.add_argument("--project", required=True, help="SonarQube project key")
    parser.add_argument("--username", required=True, help="SonarQube username")
    parser.add_argument("--password", required=True, help="SonarQube password")
    parser.add_argument("--output-dir", help="Output directory for the PDF report")

    args = parser.parse_args()

    reporter = SonarQubeReporter(
        args.url,
        args.project,
        args.username,
        args.password,
        args.output_dir
    )

    result = reporter.run()
    if result == "NO_ISSUES":
        print("No issues found in SonarQube analysis. Skipping report generation.")
        return 2  # Special exit code for no issues
    elif result:
        print(f"Report generated successfully: {result}")
        return 0
    else:
        print("Failed to generate report")
        return 1


if __name__ == "__main__":
    sys.exit(main())
