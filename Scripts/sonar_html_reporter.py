#!/usr/bin/env python3
"""
SonarQube HTML Report Generator

This script generates comprehensive HTML reports for source code scanning results
in the software delivery process. It provides:
1. Interactive HTML reports with filtering and sorting
2. Dashboard-style overview with metrics
3. Detailed issue breakdown with source code preview
4. Export capabilities for CI/CD integration
"""

import argparse
import os
import sys
import json
import time
import requests
import re
import html
from datetime import datetime
from html.parser import HTMLParser
import base64
import urllib.parse


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


def map_severity_to_ui_format(severity):
    """Map SonarQube API severity names to UI display format."""
    severity_mapping = {
        'BLOCKER': 'BLOCKER',
        'CRITICAL': 'HIGH',      # Map CRITICAL to HIGH
        'MAJOR': 'MEDIUM',       # Map MAJOR to MEDIUM
        'MINOR': 'LOW',          # Map MINOR to LOW
        'INFO': 'INFO'
    }

    return severity_mapping.get(severity, severity)


class SonarQubeHTMLReporter:
    def __init__(self, sonar_url, project_key, username, password, output_dir=None, version=None):
        """Initialize the SonarQube HTML reporter with credentials and project info."""
        self.sonar_url = sonar_url.rstrip('/')
        self.project_key = project_key
        self.username = username
        self.password = password
        self.version = version or "Not specified"

        if output_dir is None:
            self.output_dir = os.getcwd()
        else:
            self.output_dir = output_dir
            os.makedirs(output_dir, exist_ok=True)

        # Create a session for making requests
        self.session = requests.Session()

        # Initialize OWASP mapping for security vulnerabilities
        self.owasp_mapping = self._init_owasp_mapping()

    def _init_owasp_mapping(self):
        """Initialize OWASP Top 10 2021 and CWE mapping for security issues."""
        return {
            # OWASP Top 10 2021 mappings (official from https://owasp.org/www-project-top-ten/)
            'broken_access_control': {
                'owasp': 'A01:2021 – Broken Access Control',
                'description': 'Access control enforcement failures (moved up from #5)',
                'cwe': ['CWE-22', 'CWE-284', 'CWE-285', 'CWE-639']
            },
            'cryptographic_failures': {
                'owasp': 'A02:2021 – Cryptographic Failures',
                'description': 'Sensitive data exposure and cryptographic failures (previously Sensitive Data Exposure)',
                'cwe': ['CWE-311', 'CWE-312', 'CWE-319', 'CWE-326']
            },
            'injection': {
                'owasp': 'A03:2021 – Injection',
                'description': 'SQL, NoSQL, OS, and LDAP injection flaws (XSS now part of this category)',
                'cwe': ['CWE-79', 'CWE-89', 'CWE-94', 'CWE-95']
            },
            'insecure_design': {
                'owasp': 'A04:2021 – Insecure Design',
                'description': 'NEW: Design flaws and missing security controls',
                'cwe': ['CWE-209', 'CWE-256', 'CWE-501', 'CWE-522']
            },
            'security_misconfiguration': {
                'owasp': 'A05:2021 – Security Misconfiguration',
                'description': 'Security misconfiguration vulnerabilities (XXE now part of this category)',
                'cwe': ['CWE-16', 'CWE-2', 'CWE-11', 'CWE-611']
            },
            'vulnerable_components': {
                'owasp': 'A06:2021 – Vulnerable and Outdated Components',
                'description': 'Using components with known vulnerabilities (moved up from #9)',
                'cwe': ['CWE-1104', 'CWE-937']
            },
            'authentication_failures': {
                'owasp': 'A07:2021 – Identification and Authentication Failures',
                'description': 'Authentication and session management flaws (previously Broken Authentication)',
                'cwe': ['CWE-287', 'CWE-384', 'CWE-620', 'CWE-640']
            },
            'integrity_failures': {
                'owasp': 'A08:2021 – Software and Data Integrity Failures',
                'description': 'NEW: Software updates, CI/CD pipelines without integrity verification',
                'cwe': ['CWE-502', 'CWE-915', 'CWE-829']
            },
            'logging_failures': {
                'owasp': 'A09:2021 – Security Logging and Monitoring Failures',
                'description': 'Insufficient logging and monitoring (moved up from #10)',
                'cwe': ['CWE-117', 'CWE-223', 'CWE-532', 'CWE-778']
            },
            'ssrf': {
                'owasp': 'A10:2021 – Server-Side Request Forgery (SSRF)',
                'description': 'NEW: Server-Side Request Forgery vulnerabilities',
                'cwe': ['CWE-918']
            },
            # Legacy mappings for backward compatibility
            'xss': {
                'owasp': 'A03:2021 – Injection',
                'description': 'Cross-Site Scripting (XSS) vulnerabilities (now part of Injection)',
                'cwe': ['CWE-79', 'CWE-80', 'CWE-83', 'CWE-87']
            },
            'xxe': {
                'owasp': 'A05:2021 – Security Misconfiguration',
                'description': 'XML External Entities (XXE) vulnerabilities (now part of Security Misconfiguration)',
                'cwe': ['CWE-611', 'CWE-827']
            },
            'insecure_deserialization': {
                'owasp': 'A08:2021 – Software and Data Integrity Failures',
                'description': 'Insecure deserialization (now part of Software and Data Integrity Failures)',
                'cwe': ['CWE-502', 'CWE-915']
            }
        }

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
                return True
            else:
                print(f"Login failed with status code: {login_response.status_code}")
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
                'components': self.project_key,
                'inNewCodePeriod': 'true',
                'issueStatuses': 'CONFIRMED,OPEN',
                'impactSeverities': 'BLOCKER,HIGH,MEDIUM,LOW',
                'ps': 500,  # Increased page size
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
                return []
        except Exception as e:
            print(f"Failed to get project issues: {str(e)}")
            return []

    def get_project_metrics(self):
        """Get project metrics for dashboard overview."""
        try:
            print("Getting project metrics...")

            # Try different metric keys for different SonarQube versions
            metric_sets = [
                # Modern SonarQube (8.0+)
                'ncloc,lines,statements,functions,classes,files,directories,complexity,coverage,duplicated_lines_density,reliability_rating,security_rating,sqale_rating,bugs,vulnerabilities,code_smells,technical_debt,sqale_index',
                # Older SonarQube versions
                'ncloc,lines,functions,classes,files,complexity,coverage,duplicated_lines_density,bugs,vulnerabilities,code_smells',
                # Basic metrics that should be available in most versions
                'ncloc,lines,files,complexity,bugs,vulnerabilities,code_smells'
            ]

            headers = {}
            if 'XSRF-TOKEN' in self.session.cookies:
                headers['X-XSRF-TOKEN'] = self.session.cookies['XSRF-TOKEN']

            for i, metric_keys in enumerate(metric_sets):
                try:
                    api_url = f"{self.sonar_url}/api/measures/component"
                    params = {
                        'component': self.project_key,
                        'metricKeys': metric_keys
                    }

                    print(f"Trying metric set {i+1}/3...")
                    response = self.session.get(api_url, params=params, headers=headers)

                    if response.status_code == 200:
                        metrics_data = response.json()
                        measures = metrics_data.get('component', {}).get('measures', [])
                        if measures:
                            print(f"Successfully retrieved {len(measures)} metrics using metric set {i+1}")
                            return measures
                        else:
                            print(f"Metric set {i+1} returned no measures")
                    else:
                        print(f"Metric set {i+1} failed with status code: {response.status_code}")
                        if response.status_code == 404:
                            print(f"API URL: {api_url}")
                            print(f"Parameters: {params}")
                            print(f"Response: {response.text[:300]}")
                except Exception as e:
                    print(f"Error with metric set {i+1}: {str(e)}")
                    continue

            # If all metric sets fail, try to get basic project info
            print("All metric sets failed, trying basic project info...")
            try:
                api_url = f"{self.sonar_url}/api/components/show"
                params = {'component': self.project_key}
                response = self.session.get(api_url, params=params, headers=headers)

                if response.status_code == 200:
                    component_data = response.json()
                    print("Got basic project info, but no detailed metrics available")
                    return []
                else:
                    print(f"Basic project info also failed: {response.status_code}")
            except Exception as e:
                print(f"Error getting basic project info: {str(e)}")

            print("No metrics could be retrieved from SonarQube")
            return []

        except Exception as e:
            print(f"Failed to get project metrics: {str(e)}")
            return []

    def get_basic_project_stats(self, issues_data):
        """Extract basic project statistics from issues data as fallback."""
        if not issues_data:
            return []

        # Count unique files from issues
        files = set()
        for issue in issues_data:
            component = issue.get('component', '')
            if component:
                files.add(component)

        # Create basic metrics from available data
        basic_metrics = [
            {"metric": "files", "value": str(len(files))},
            {"metric": "issues", "value": str(len(issues_data))}
        ]

        print(f"Generated {len(basic_metrics)} basic metrics from issues data")
        return basic_metrics

    def get_owasp_info(self, rule_key, issue_type):
        """Get OWASP information for a given rule or issue type."""
        # Common SonarQube rule patterns that map to OWASP Top 10 2021 categories
        rule_patterns = {
            # A01:2021 – Broken Access Control
            'access': 'broken_access_control',
            'authorization': 'broken_access_control',
            'permission': 'broken_access_control',
            'privilege': 'broken_access_control',
            'path-traversal': 'broken_access_control',
            'directory-traversal': 'broken_access_control',

            # A02:2021 – Cryptographic Failures
            'crypto': 'cryptographic_failures',
            'encryption': 'cryptographic_failures',
            'hash': 'cryptographic_failures',
            'cipher': 'cryptographic_failures',
            'ssl': 'cryptographic_failures',
            'tls': 'cryptographic_failures',
            'certificate': 'cryptographic_failures',

            # A03:2021 – Injection (includes XSS)
            'sql': 'injection',
            'injection': 'injection',
            'xss': 'injection',
            'cross-site': 'injection',
            'script': 'injection',
            'command': 'injection',
            'ldap': 'injection',
            'nosql': 'injection',

            # A04:2021 – Insecure Design
            'design': 'insecure_design',
            'architecture': 'insecure_design',
            'threat-model': 'insecure_design',
            'security-pattern': 'insecure_design',

            # A05:2021 – Security Misconfiguration (includes XXE)
            'configuration': 'security_misconfiguration',
            'hardcoded': 'security_misconfiguration',
            'default': 'security_misconfiguration',
            'xml': 'security_misconfiguration',
            'xxe': 'security_misconfiguration',
            'cors': 'security_misconfiguration',

            # A06:2021 – Vulnerable and Outdated Components
            'dependency': 'vulnerable_components',
            'component': 'vulnerable_components',
            'library': 'vulnerable_components',
            'outdated': 'vulnerable_components',
            'vulnerable': 'vulnerable_components',

            # A07:2021 – Identification and Authentication Failures
            'authentication': 'authentication_failures',
            'session': 'authentication_failures',
            'password': 'authentication_failures',
            'login': 'authentication_failures',
            'token': 'authentication_failures',
            'jwt': 'authentication_failures',

            # A08:2021 – Software and Data Integrity Failures
            'deserialization': 'integrity_failures',
            'serialize': 'integrity_failures',
            'integrity': 'integrity_failures',
            'checksum': 'integrity_failures',
            'signature': 'integrity_failures',
            'ci-cd': 'integrity_failures',

            # A09:2021 – Security Logging and Monitoring Failures
            'log': 'logging_failures',
            'logging': 'logging_failures',
            'monitor': 'logging_failures',
            'audit': 'logging_failures',
            'trace': 'logging_failures',

            # A10:2021 – Server-Side Request Forgery
            'ssrf': 'ssrf',
            'request-forgery': 'ssrf',
            'server-side': 'ssrf'
        }

        # Check if this is a security vulnerability
        if issue_type.lower() != 'vulnerability':
            return None

        # Look for patterns in the rule key
        rule_lower = rule_key.lower()
        for pattern, category in rule_patterns.items():
            if pattern in rule_lower:
                return self.owasp_mapping.get(category)

        # Default for security vulnerabilities without specific mapping
        return {
            'owasp': 'Security Vulnerability',
            'description': 'Security vulnerability requiring review',
            'cwe': []
        }

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
            
            # Calculate the range of lines to fetch (10 lines before and after the issue)
            start_line = max(1, line_number - 10)
            end_line = line_number + 10
            
            # Use the SonarQube API to get source code
            api_url = f"{self.sonar_url}/api/sources/lines"
            params = {
                'key': component_key,
                'from': start_line,
                'to': end_line
            }
            
            headers = {}
            if 'XSRF-TOKEN' in self.session.cookies:
                headers['X-XSRF-TOKEN'] = self.session.cookies['XSRF-TOKEN']
            
            response = self.session.get(api_url, params=params, headers=headers)
            
            if response.status_code == 200:
                source_data = response.json()
                if 'sources' in source_data:
                    return {
                        'lines': source_data['sources'],
                        'issue_line': line_number
                    }
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
                    raw_severity = issue.get('severity', 'UNKNOWN')
                    severity = map_severity_to_ui_format(raw_severity)  # Map to UI format
                    message = issue.get('message', 'No message')
                    component = issue.get('component', 'Unknown component')
                    rule = issue.get('rule', 'Unknown rule')
                    
                    # Get component name (file name) from the full path
                    component_name = component.split(':')[-1]
                    
                    # Extract additional details
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
                    
                    # Get source code for the issue
                    source_code = self.get_source_code(component, line)

                    # Get OWASP information for security vulnerabilities
                    owasp_info = self.get_owasp_info(rule, type_name)

                    processed_issues.append({
                        "id": issue.get('key', ''),
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
                        "source_code": source_code,
                        "full_component": component,
                        "owasp_info": owasp_info,
                        "raw_data": issue
                    })
                except Exception as e:
                    print(f"Error processing an issue: {str(e)}")
                    continue
            
            print(f"Processed {len(processed_issues)} issues")

            # Sort issues by severity: BLOCKER, HIGH, MEDIUM, LOW, INFO
            severity_order = {'BLOCKER': 0, 'HIGH': 1, 'MEDIUM': 2, 'LOW': 3, 'INFO': 4}

            def get_severity_priority(issue):
                severity = issue.get('severity', 'UNKNOWN')
                return severity_order.get(severity, 999)  # Unknown severities go to the end

            processed_issues.sort(key=get_severity_priority)
            print(f"Issues sorted by severity (BLOCKER → HIGH → MEDIUM → LOW → INFO)")

            return processed_issues
        except Exception as e:
            print(f"Failed to process issues: {str(e)}")
            return []

    def generate_html_report(self, issues, metrics=None):
        """Generate a comprehensive HTML report with the extracted issues."""
        try:
            print("Generating HTML report...")

            # Calculate statistics
            stats = self._calculate_statistics(issues)

            # Generate the HTML content
            html_content = self._generate_html_template(issues, stats, metrics)

            # Save the HTML report
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            html_filename = f"{self.output_dir}/sonarqube_report_{self.project_key}_{timestamp}.html"

            with open(html_filename, 'w', encoding='utf-8') as f:
                f.write(html_content)

            print(f"HTML report generated: {html_filename}")
            return html_filename
        except Exception as e:
            print(f"Failed to generate HTML report: {str(e)}")
            return None

    def _calculate_statistics(self, issues):
        """Calculate statistics from the issues data."""
        stats = {
            'total_issues': len(issues),
            'by_severity': {},
            'by_type': {},
            'by_status': {},
            'by_component': {},
            'recent_issues': 0,
            'security_hotspots': 0,
            'vulnerabilities': 0,
            'bugs': 0,
            'code_smells': 0
        }

        # Count by severity
        for issue in issues:
            severity = issue['severity']
            stats['by_severity'][severity] = stats['by_severity'].get(severity, 0) + 1

            # Count by type
            issue_type = issue['type']
            stats['by_type'][issue_type] = stats['by_type'].get(issue_type, 0) + 1

            # Count specific issue types for dashboard
            if issue_type == 'SECURITY_HOTSPOT':
                stats['security_hotspots'] += 1
            elif issue_type == 'VULNERABILITY':
                stats['vulnerabilities'] += 1
            elif issue_type == 'BUG':
                stats['bugs'] += 1
            elif issue_type == 'CODE_SMELL':
                stats['code_smells'] += 1

            # Count by status
            status = issue['status']
            stats['by_status'][status] = stats['by_status'].get(status, 0) + 1

            # Count by component (top 10)
            component = issue['component']
            stats['by_component'][component] = stats['by_component'].get(component, 0) + 1

            # Count recent issues (last 7 days)
            try:
                if issue['creation_date'] != 'Unknown date':
                    creation_date = datetime.fromisoformat(issue['creation_date'].replace('Z', '+00:00'))
                    if (datetime.now().replace(tzinfo=creation_date.tzinfo) - creation_date).days <= 7:
                        stats['recent_issues'] += 1
            except:
                pass

        # Sort components by count and keep top 10
        stats['by_component'] = dict(sorted(stats['by_component'].items(),
                                          key=lambda x: x[1], reverse=True)[:10])

        return stats

    def _generate_issue_summary_table(self, issues):
        """Generate a summary table grouping issues by SonarQube rule."""
        # Group issues by rule only
        issue_groups = {}

        for issue in issues:
            # Use only the rule as the key for grouping
            rule_key = issue['rule']  # Keep full rule identifier
            rule_name = rule_key.split(':')[-1]  # Get just the rule name for display

            if rule_key not in issue_groups:
                issue_groups[rule_key] = {
                    'rule': rule_key,
                    'rule_name': rule_name,
                    'severity': issue['severity'],
                    'count': 0,
                    'type': issue['type'],
                    'sample_message': issue['message']  # Keep one sample message for reference
                }
            issue_groups[rule_key]['count'] += 1

        # Sort by severity priority then by count
        severity_order = {'BLOCKER': 0, 'HIGH': 1, 'MEDIUM': 2, 'LOW': 3, 'INFO': 4}
        sorted_groups = sorted(issue_groups.values(),
                             key=lambda x: (severity_order.get(x['severity'], 999), -x['count']))

        return sorted_groups

    def _generate_summary_table_html(self, issues):
        """Generate HTML for the issue summary table."""
        issue_groups = self._generate_issue_summary_table(issues)

        if not issue_groups:
            return "<p>No issues found to display in summary table.</p>"

        html_parts = ['''
            <table class="summary-table">
                <thead>
                    <tr>
                        <th>SonarQube Rule</th>
                        <th>Severity</th>
                        <th>Total</th>
                        <th>Type</th>
                        <th>Sample Issue</th>
                    </tr>
                </thead>
                <tbody>
        ''']

        for group in issue_groups:
            # Use rule name for display
            display_rule = group['rule_name']

            # Show full sample message without truncation
            sample_message = group['sample_message']

            html_parts.append(f'''
                <tr>
                    <td><strong>{html.escape(display_rule)}</strong></td>
                    <td><span class="summary-severity {group['severity']}">{group['severity']}</span></td>
                    <td><span class="summary-count">{group['count']}</span></td>
                    <td><span class="summary-type">{html.escape(group['type'])}</span></td>
                    <td class="sample-issue">{html.escape(sample_message)}</td>
                </tr>
            ''')

        html_parts.append('''
                </tbody>
            </table>
        ''')

        return ''.join(html_parts)

    def _generate_html_template(self, issues, stats, metrics):
        """Generate the complete HTML template."""

        # Convert issues to JSON for JavaScript
        issues_json = json.dumps(issues, default=str, ensure_ascii=False)
        stats_json = json.dumps(stats, default=str, ensure_ascii=False)
        metrics_json = json.dumps(metrics or [], default=str, ensure_ascii=False)

        html_template = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SonarQube Code Quality Analysis Report - {html.escape(self.project_key)}</title>
    <style>
        {self._get_css_styles()}
    </style>
</head>
<body>
    <div class="container">
        <header class="header">
            <h1>Code Quality Analysis Report</h1>
            <div class="project-info">
                <div class="project-details">
                    <span class="project-name">Project: {html.escape(self.project_key)}</span>
                    <span class="project-version">Version: {html.escape(self.version)}</span>
                </div>
                <span class="report-date">Report Date: {datetime.now().strftime('%B %d, %Y at %H:%M')}</span>
            </div>
        </header>

        <div class="document-info">
            <h2>Executive Summary</h2>
            <p>This document presents the results of automated code quality analysis performed using SonarQube for
            <strong>{html.escape(self.project_key)}</strong> version <strong>{html.escape(self.version)}</strong>.
            The analysis identifies potential issues, security vulnerabilities, and code quality concerns that should
            be addressed as part of the software delivery process.</p>

            <div class="owasp-info">
                <h3>🛡️ Security Framework Compliance</h3>
                <p>This analysis includes security vulnerability detection aligned with industry standards:</p>
                <ul>
                    <li><strong>OWASP Top 10</strong> - Web application security risks</li>
                    <li><strong>OWASP ASVS</strong> - Application Security Verification Standard</li>
                    <li><strong>CWE</strong> - Common Weakness Enumeration</li>
                    <li><strong>SANS Top 25</strong> - Most dangerous software errors</li>
                </ul>
                <p>Security vulnerabilities identified in this report should be prioritized according to OWASP risk rating methodology.</p>

                <div class="owasp-top10-list">
                    <h4>📋 OWASP Top 10 2021 - Web Application Security Risks</h4>
                    <div class="owasp-categories">
                        <div class="owasp-category-item">
                            <span class="category-number">A01:2021</span>
                            <span class="category-name">Broken Access Control</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A02:2021</span>
                            <span class="category-name">Cryptographic Failures</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A03:2021</span>
                            <span class="category-name">Injection</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A04:2021</span>
                            <span class="category-name">Insecure Design</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A05:2021</span>
                            <span class="category-name">Security Misconfiguration</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A06:2021</span>
                            <span class="category-name">Vulnerable and Outdated Components</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A07:2021</span>
                            <span class="category-name">Identification and Authentication Failures</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A08:2021</span>
                            <span class="category-name">Software and Data Integrity Failures</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A09:2021</span>
                            <span class="category-name">Security Logging and Monitoring Failures</span>
                        </div>
                        <div class="owasp-category-item">
                            <span class="category-number">A10:2021</span>
                            <span class="category-name">Server-Side Request Forgery (SSRF)</span>
                        </div>
                    </div>
                    <p class="owasp-note"><strong>Note:</strong> This list is based on the official OWASP Top 10 2021.
                    OWASP Top 10 2025 is expected in late summer/early fall 2025.</p>
                </div>
            </div>
        </div>

        <div class="dashboard">
            <div class="metrics-grid">
                {self._generate_code_metrics_cards(metrics)}
                <div class="metric-card total-issues">
                    <h3>Total Issues</h3>
                    <div class="metric-value">{stats['total_issues']}</div>
                </div>
                <div class="metric-card security-hotspots">
                    <h3>🔥 Security Hotspots</h3>
                    <div class="metric-value security-hotspot-count">{stats['security_hotspots']}</div>
                </div>
                <div class="metric-card vulnerabilities">
                    <h3>🛡️ Vulnerabilities</h3>
                    <div class="metric-value vulnerability-count">{stats['vulnerabilities']}</div>
                </div>
                <div class="metric-card bugs">
                    <h3>🐛 Bugs</h3>
                    <div class="metric-value bug-count">{stats['bugs']}</div>
                </div>
                <div class="metric-card code-smells">
                    <h3>👃 Code Smells</h3>
                    <div class="metric-value code-smell-count">{stats['code_smells']}</div>
                </div>
                {self._generate_owasp_summary(issues)}
                <div class="metric-card severity-breakdown">
                    <h3>By Severity</h3>
                    <div class="severity-list">
                        {self._generate_severity_breakdown(stats['by_severity'])}
                    </div>
                </div>
                <div class="metric-card type-breakdown">
                    <h3>By Type</h3>
                    <div class="type-list">
                        {self._generate_type_breakdown(stats['by_type'])}
                    </div>
                </div>
            </div>
        </div>

        <div class="controls">
            <div class="filters">
                <label for="severity-filter">Filter by Severity:</label>
                <select id="severity-filter">
                    <option value="">All Severities</option>
                    <option value="BLOCKER">Blocker</option>
                    <option value="HIGH">High</option>
                    <option value="MEDIUM">Medium</option>
                    <option value="LOW">Low</option>
                    <option value="INFO">Info</option>
                </select>

                <label for="type-filter">Filter by Type:</label>
                <select id="type-filter">
                    <option value="">All Types</option>
                    <option value="BUG">Bug</option>
                    <option value="VULNERABILITY">Vulnerability</option>
                    <option value="CODE_SMELL">Code Smell</option>
                    <option value="SECURITY_HOTSPOT">Security Hotspot</option>
                </select>

                <label for="search-filter">Search:</label>
                <input type="text" id="search-filter" placeholder="Search in messages, files...">
            </div>

            <div class="actions">
                <button onclick="exportToCSV()">Export to CSV</button>
                <button onclick="exportToJSON()">Export to JSON</button>
                <button onclick="printReport()">Print Report</button>
            </div>
        </div>

        <div class="issue-summary-section">
            <h2>Issue Summary</h2>
            {self._generate_summary_table_html(issues)}
        </div>

        <div class="issues-section">
            <h2>Detailed Issue Analysis</h2>
            <div id="issues-container">
                {self._generate_issues_html(issues)}
            </div>
        </div>

        <footer class="footer">
            <p><strong>Code Quality Analysis Report</strong></p>
            <p>Generated by SonarQube Integration System</p>
            <p>Report Date: {datetime.now().strftime('%B %d, %Y at %H:%M:%S')}</p>
        </footer>
    </div>

    <script>
        // Data for JavaScript functionality
        const issuesData = {issues_json};
        const statsData = {stats_json};
        const metricsData = {metrics_json};

        {self._get_javascript_code()}
    </script>
</body>
</html>"""

        return html_template

    def _get_css_styles(self):
        """Return CSS styles for the HTML report."""
        return """
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Times New Roman', Times, serif;
            line-height: 1.8;
            color: #2c3e50;
            background-color: #ffffff;
            font-size: 14px;
        }

        .container {
            max-width: 1000px;
            margin: 0 auto;
            padding: 40px;
            background: white;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
            min-height: 100vh;
        }

        .header {
            border-bottom: 3px solid #2c3e50;
            padding-bottom: 30px;
            margin-bottom: 40px;
            text-align: center;
            background: white;
        }

        .header h1 {
            font-size: 2.2em;
            margin-bottom: 15px;
            color: #2c3e50;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        .project-info {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-top: 20px;
            padding-top: 15px;
            border-top: 1px solid #bdc3c7;
        }

        .project-details {
            display: flex;
            flex-direction: column;
            gap: 5px;
        }

        .project-name {
            font-size: 1.1em;
            font-weight: bold;
            color: #34495e;
        }

        .project-version {
            font-size: 1.0em;
            font-weight: bold;
            color: #e74c3c;
            background: #f8f9fa;
            padding: 4px 8px;
            border-radius: 4px;
            border: 1px solid #dee2e6;
        }

        .report-date {
            font-size: 0.9em;
            color: #7f8c8d;
            font-style: italic;
        }

        .document-info {
            background: #f8f9fa;
            padding: 30px;
            margin-bottom: 40px;
            border-left: 5px solid #2c3e50;
            page-break-inside: avoid;
        }

        .document-info h2 {
            color: #2c3e50;
            font-size: 1.4em;
            margin-bottom: 15px;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .document-info p {
            color: #495057;
            line-height: 1.8;
            text-align: justify;
        }

        .owasp-info {
            background: #fff3cd;
            border: 1px solid #ffeaa7;
            border-radius: 8px;
            padding: 20px;
            margin-top: 25px;
        }

        .owasp-info h3 {
            color: #856404;
            margin-top: 0;
            margin-bottom: 15px;
            font-size: 1.2em;
        }

        .owasp-info ul {
            margin: 15px 0;
            padding-left: 25px;
        }

        .owasp-info li {
            margin-bottom: 8px;
            color: #6c5700;
        }

        .owasp-info strong {
            color: #495057;
        }

        .owasp-top10-list {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 20px;
            margin-top: 20px;
        }

        .owasp-top10-list h4 {
            color: #2c3e50;
            margin-top: 0;
            margin-bottom: 15px;
            font-size: 1.1em;
            border-bottom: 2px solid #3498db;
            padding-bottom: 8px;
        }

        .owasp-categories {
            display: grid;
            gap: 8px;
            margin: 15px 0;
        }

        .owasp-category-item {
            display: grid;
            grid-template-columns: 80px 1fr auto;
            align-items: center;
            padding: 10px 15px;
            background: white;
            border: 1px solid #e9ecef;
            border-radius: 5px;
            transition: all 0.2s ease;
        }

        .owasp-category-item:hover {
            border-color: #3498db;
            box-shadow: 0 2px 4px rgba(52, 152, 219, 0.1);
        }

        .category-number {
            font-weight: bold;
            color: #2c3e50;
            font-size: 0.9em;
        }

        .category-name {
            color: #495057;
            font-weight: 500;
        }

        .category-change {
            font-size: 0.8em;
            padding: 3px 8px;
            border-radius: 12px;
            font-weight: bold;
        }

        .category-change.up {
            background: #d4edda;
            color: #155724;
        }

        .category-change.down {
            background: #f8d7da;
            color: #721c24;
        }

        .category-change.new {
            background: #fff3cd;
            color: #856404;
        }

        .owasp-note {
            font-size: 0.85em;
            color: #6c757d;
            font-style: italic;
            margin-top: 15px;
            margin-bottom: 0;
        }

        .owasp-reference {
            background: #e8f4fd;
            border: 1px solid #bee5eb;
            border-radius: 6px;
            padding: 15px;
            margin: 15px 0;
            border-left: 4px solid #17a2b8;
        }

        .owasp-reference strong {
            color: #0c5460;
        }

        .owasp-summary {
            border-left: 4px solid #17a2b8;
        }

        .owasp-summary .metric-value {
            color: #17a2b8;
        }

        .owasp-categories {
            margin-top: 10px;
            font-size: 0.85em;
        }

        .owasp-category {
            background: #e8f4fd;
            padding: 4px 8px;
            margin: 2px 0;
            border-radius: 3px;
            color: #0c5460;
        }

        .footer {
            margin-top: 50px;
            padding-top: 30px;
            border-top: 2px solid #e9ecef;
            text-align: center;
            color: #6c757d;
            font-size: 0.85em;
            page-break-inside: avoid;
        }

        .footer p {
            margin: 5px 0;
        }

        .dashboard {
            margin-bottom: 40px;
            page-break-inside: avoid;
        }

        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 25px;
            margin-bottom: 40px;
        }

        .metric-card {
            background: #f8f9fa;
            padding: 25px;
            border: 2px solid #e9ecef;
            border-radius: 8px;
            text-align: center;
            page-break-inside: avoid;
        }

        .metric-card h3 {
            color: #2c3e50;
            margin-bottom: 15px;
            font-size: 1.0em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .metric-value {
            font-size: 2.5em;
            font-weight: bold;
            color: #e74c3c;
            margin-bottom: 10px;
        }

        .code-metric .metric-value {
            color: #2c3e50;
        }

        .security-hotspots {
            border-left: 4px solid #fd7e14;
        }

        .security-hotspot-count {
            color: #fd7e14;
        }

        .vulnerabilities {
            border-left: 4px solid #dc3545;
        }

        .vulnerability-count {
            color: #dc3545;
        }

        .bugs {
            border-left: 4px solid #e74c3c;
        }

        .bug-count {
            color: #e74c3c;
        }

        .code-smells {
            border-left: 4px solid #28a745;
        }

        .code-smell-count {
            color: #28a745;
        }

        .metric-description {
            font-size: 0.75em;
            color: #6c757d;
            font-style: italic;
            margin-top: 5px;
        }

        .severity-list, .type-list {
            font-size: 0.85em;
            text-align: left;
        }

        .severity-item, .type-item {
            display: flex;
            justify-content: space-between;
            margin: 8px 0;
            padding: 8px 12px;
            border-radius: 4px;
            border: 1px solid #dee2e6;
        }

        .severity-BLOCKER { background-color: #dc3545; color: white; font-weight: bold; }
        .severity-HIGH { background-color: #fd7e14; color: white; font-weight: bold; }
        .severity-MEDIUM { background-color: #ffc107; color: #212529; font-weight: bold; }
        .severity-LOW { background-color: #17a2b8; color: white; }
        .severity-INFO { background-color: #6c757d; color: white; }

        /* Legacy severity classes for backward compatibility */
        .severity-CRITICAL { background-color: #fd7e14; color: white; font-weight: bold; }
        .severity-MAJOR { background-color: #ffc107; color: #212529; font-weight: bold; }
        .severity-MINOR { background-color: #17a2b8; color: white; }

        .controls {
            background: #f8f9fa;
            padding: 25px;
            border: 2px solid #e9ecef;
            border-radius: 8px;
            margin-bottom: 40px;
            page-break-inside: avoid;
        }

        .filters {
            display: flex;
            gap: 25px;
            align-items: center;
            flex-wrap: wrap;
            margin-bottom: 20px;
        }

        .filters label {
            font-weight: bold;
            color: #2c3e50;
            font-size: 0.9em;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .filters select, .filters input {
            padding: 10px 15px;
            border: 2px solid #dee2e6;
            border-radius: 4px;
            font-size: 13px;
            font-family: inherit;
            background: white;
        }

        .filters select:focus, .filters input:focus {
            outline: none;
            border-color: #2c3e50;
        }

        .actions {
            display: flex;
            gap: 15px;
            flex-wrap: wrap;
            justify-content: center;
        }

        .actions button {
            padding: 12px 25px;
            background: #2c3e50;
            color: white;
            border: 2px solid #2c3e50;
            border-radius: 4px;
            cursor: pointer;
            font-size: 13px;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            transition: all 0.3s;
        }

        .actions button:hover {
            background: white;
            color: #2c3e50;
        }

        .issue-summary-section {
            background: white;
            padding: 30px;
            border: 2px solid #e9ecef;
            border-radius: 8px;
            margin-bottom: 30px;
            page-break-inside: avoid;
        }

        .issue-summary-section h2 {
            margin-bottom: 25px;
            color: #2c3e50;
            font-size: 1.6em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 1px;
            border-bottom: 2px solid #2c3e50;
            padding-bottom: 10px;
        }

        .summary-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            font-size: 0.9em;
        }

        .summary-table th {
            background-color: #2c3e50;
            color: white;
            padding: 12px 15px;
            text-align: left;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            border: 1px solid #34495e;
        }

        .summary-table td {
            padding: 10px 15px;
            border: 1px solid #dee2e6;
            vertical-align: middle;
        }

        .summary-table tr:nth-child(even) {
            background-color: #f8f9fa;
        }

        .summary-table tr:hover {
            background-color: #e9ecef;
        }

        .summary-severity {
            padding: 4px 10px;
            border-radius: 4px;
            font-size: 0.8em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            text-align: center;
            min-width: 70px;
            display: inline-block;
        }

        .summary-severity.BLOCKER {
            background-color: #dc3545;
            color: white;
        }

        .summary-severity.HIGH {
            background-color: #fd7e14;
            color: white;
        }

        .summary-severity.MEDIUM {
            background-color: #ffc107;
            color: #212529;
        }

        .summary-severity.LOW {
            background-color: #17a2b8;
            color: white;
        }

        .summary-severity.INFO {
            background-color: #6c757d;
            color: white;
        }

        .summary-count {
            font-weight: bold;
            font-size: 1.1em;
            text-align: center;
        }

        .summary-type {
            font-size: 0.8em;
            color: #6c757d;
            text-transform: uppercase;
            font-weight: 500;
        }

        .sample-issue {
            font-size: 0.85em;
            color: #495057;
            line-height: 1.4;
            word-wrap: break-word;
            white-space: normal;
            max-width: 300px;
        }

        .issues-section {
            background: white;
            padding: 30px;
            border: 2px solid #e9ecef;
            border-radius: 8px;
            page-break-inside: avoid;
        }

        .issues-section h2 {
            margin-bottom: 30px;
            color: #2c3e50;
            font-size: 1.8em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 1px;
            border-bottom: 2px solid #2c3e50;
            padding-bottom: 10px;
        }

        .issue-card {
            border: 2px solid #dee2e6;
            border-radius: 6px;
            margin-bottom: 25px;
            overflow: hidden;
            page-break-inside: avoid;
            background: white;
        }

        .issue-card:hover {
            border-color: #2c3e50;
            box-shadow: 0 4px 12px rgba(44,62,80,0.1);
        }

        .issue-header {
            padding: 20px;
            background: #f8f9fa;
            border-bottom: 2px solid #dee2e6;
            cursor: pointer;
        }

        .issue-title {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
        }

        .issue-severity {
            padding: 6px 12px;
            border-radius: 4px;
            font-size: 0.75em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .issue-message {
            margin: 15px 0;
            font-weight: 600;
            font-size: 1.05em;
            color: #2c3e50;
            line-height: 1.6;
        }

        .issue-meta {
            display: flex;
            gap: 25px;
            font-size: 0.85em;
            color: #6c757d;
            flex-wrap: wrap;
        }

        .issue-meta strong {
            color: #495057;
        }

        .issue-details {
            padding: 25px;
            display: none;
            background: #ffffff;
            border-top: 1px solid #dee2e6;
        }

        .issue-details.expanded {
            display: block;
        }

        .issue-info {
            margin-bottom: 20px;
            line-height: 1.8;
        }

        .issue-info strong {
            color: #2c3e50;
            font-weight: bold;
        }

        .source-code {
            background: #f8f9fa;
            color: #2c3e50;
            padding: 20px;
            border: 2px solid #dee2e6;
            border-radius: 6px;
            font-family: 'Courier New', 'Monaco', monospace;
            font-size: 0.85em;
            overflow-x: auto;
            margin: 20px 0;
            line-height: 1.6;
        }

        .source-line {
            display: block;
            padding: 3px 0;
            border-left: 3px solid transparent;
            padding-left: 10px;
        }

        .source-line.highlight {
            background: #fff3cd;
            border-left-color: #ffc107;
            color: #856404;
            font-weight: bold;
        }

        .hidden {
            display: none !important;
        }

        /* Print Styles */
        @media print {
            body {
                background: white;
                font-size: 12px;
            }

            .container {
                box-shadow: none;
                padding: 20px;
            }

            .controls {
                display: none;
            }

            .issue-card {
                page-break-inside: avoid;
                margin-bottom: 20px;
            }

            .issue-details {
                display: block !important;
            }
        }

        @media (max-width: 768px) {
            .container {
                padding: 15px;
            }

            .project-info {
                flex-direction: column;
                gap: 10px;
                text-align: center;
            }

            .filters {
                flex-direction: column;
                align-items: stretch;
                gap: 15px;
            }

            .filters > * {
                width: 100%;
            }

            .issue-meta {
                flex-direction: column;
                gap: 10px;
            }

            .actions {
                justify-content: stretch;
            }

            .actions button {
                flex: 1;
            }

            .owasp-category-item {
                grid-template-columns: 1fr;
                gap: 5px;
                text-align: center;
            }

            .category-change {
                justify-self: center;
            }
        }
        """

    def _generate_severity_breakdown(self, severity_counts):
        """Generate HTML for severity breakdown in correct order: BLOCKER, HIGH, MEDIUM, LOW, INFO."""
        html_parts = []

        # Define the correct severity order to match SonarQube UI
        severity_order = ['BLOCKER', 'HIGH', 'MEDIUM', 'LOW', 'INFO']

        # Generate HTML in the correct order
        for severity in severity_order:
            if severity in severity_counts:
                count = severity_counts[severity]
                html_parts.append(f'''
                    <div class="severity-item severity-{severity}">
                        <span>{severity}</span>
                        <span>{count}</span>
                    </div>
                ''')

        # Add any other severities that might exist (INFO, BLOCKER, etc.)
        for severity, count in severity_counts.items():
            if severity not in severity_order:
                html_parts.append(f'''
                    <div class="severity-item severity-{severity}">
                        <span>{severity}</span>
                        <span>{count}</span>
                    </div>
                ''')

        return ''.join(html_parts)

    def _generate_code_metrics_cards(self, metrics):
        """Generate HTML cards for code metrics like LOC, complexity, etc."""
        if not metrics:
            # Return a message card indicating metrics are not available
            return '''
                <div class="metric-card code-metric">
                    <h3>Code Metrics</h3>
                    <div class="metric-value">N/A</div>
                    <div class="metric-description">Metrics not available from SonarQube</div>
                </div>
            '''

        # Convert metrics list to dictionary for easier access
        metrics_dict = {}
        for metric in metrics:
            metrics_dict[metric.get('metric', '')] = metric.get('value', '0')

        print(f"Available metrics: {list(metrics_dict.keys())}")

        # Define the metrics we want to display with their labels and descriptions
        metric_definitions = [
            {
                'key': 'ncloc',
                'label': 'Lines of Code',
                'description': 'Non-comment lines of code',
                'format': 'number'
            },
            {
                'key': 'lines',
                'label': 'Total Lines',
                'description': 'Total lines including comments',
                'format': 'number'
            },
            {
                'key': 'files',
                'label': 'Files',
                'description': 'Number of files',
                'format': 'number'
            },
            {
                'key': 'functions',
                'label': 'Functions',
                'description': 'Number of functions',
                'format': 'number'
            },
            {
                'key': 'classes',
                'label': 'Classes',
                'description': 'Number of classes',
                'format': 'number'
            },
            {
                'key': 'complexity',
                'label': 'Complexity',
                'description': 'Cyclomatic complexity',
                'format': 'number'
            },
            {
                'key': 'coverage',
                'label': 'Test Coverage',
                'description': 'Unit test coverage',
                'format': 'percentage'
            },
            {
                'key': 'duplicated_lines_density',
                'label': 'Duplication',
                'description': 'Duplicated lines density',
                'format': 'percentage'
            }
        ]

        html_parts = []
        for metric_def in metric_definitions:
            key = metric_def['key']
            if key in metrics_dict and metrics_dict[key] != '0':
                value = metrics_dict[key]

                # Format the value based on type
                if metric_def['format'] == 'percentage':
                    formatted_value = f"{value}%"
                elif metric_def['format'] == 'number':
                    # Format large numbers with commas
                    try:
                        num_value = int(float(value))
                        formatted_value = f"{num_value:,}"
                    except:
                        formatted_value = value
                else:
                    formatted_value = value

                html_parts.append(f'''
                    <div class="metric-card code-metric">
                        <h3>{metric_def['label']}</h3>
                        <div class="metric-value">{formatted_value}</div>
                        <div class="metric-description">{metric_def['description']}</div>
                    </div>
                ''')

        return ''.join(html_parts)

    def _generate_owasp_summary(self, issues):
        """Generate OWASP security summary for vulnerabilities."""
        owasp_counts = {}
        vulnerability_count = 0

        for issue in issues:
            if issue.get('type', '').lower() == 'vulnerability':
                vulnerability_count += 1
                owasp_info = issue.get('owasp_info')
                if owasp_info:
                    owasp_category = owasp_info.get('owasp', 'Unknown')
                    owasp_counts[owasp_category] = owasp_counts.get(owasp_category, 0) + 1

        if vulnerability_count == 0:
            return '''
                <div class="metric-card owasp-summary">
                    <h3>🛡️ OWASP Security</h3>
                    <div class="metric-value">✅</div>
                    <div class="metric-description">No security vulnerabilities found</div>
                </div>
            '''

        # Generate top OWASP categories
        top_categories = sorted(owasp_counts.items(), key=lambda x: x[1], reverse=True)[:3]
        categories_html = ""
        for category, count in top_categories:
            short_category = category.split('–')[0].strip() if '–' in category else category
            categories_html += f"<div class='owasp-category'>{short_category}: {count}</div>"

        return f'''
            <div class="metric-card owasp-summary">
                <h3>🛡️ OWASP Security</h3>
                <div class="metric-value">{vulnerability_count}</div>
                <div class="metric-description">Security vulnerabilities</div>
                <div class="owasp-categories">
                    {categories_html}
                </div>
            </div>
        '''

    def _generate_type_breakdown(self, type_counts):
        """Generate HTML for type breakdown."""
        html_parts = []
        for issue_type, count in type_counts.items():
            html_parts.append(f'''
                <div class="type-item">
                    <span>{html.escape(issue_type)}</span>
                    <span>{count}</span>
                </div>
            ''')
        return ''.join(html_parts)

    def _generate_issues_html(self, issues):
        """Generate HTML for issues list."""
        if not issues:
            return '<p>No issues found!</p>'

        html_parts = []
        for i, issue in enumerate(issues):
            # Format creation date
            creation_date = issue['creation_date']
            if creation_date != 'Unknown date':
                try:
                    date_obj = datetime.fromisoformat(creation_date.replace('Z', '+00:00'))
                    creation_date = date_obj.strftime('%Y-%m-%d %H:%M')
                except:
                    pass

            # Generate source code preview
            source_code_html = ""
            if issue['source_code']:
                source_code_html = self._generate_source_code_html(issue['source_code'])

            # Generate impact information
            impact_html = ""
            if issue['impact']:
                impact_items = [f"{quality}: {severity}" for quality, severity in issue['impact'].items()]
                impact_html = f"<strong>Impact:</strong> {', '.join(impact_items)}<br>"

            # Generate OWASP information for security vulnerabilities
            owasp_html = ""
            if issue.get('owasp_info'):
                owasp_info = issue['owasp_info']
                cwe_list = ", ".join(owasp_info.get('cwe', [])) if owasp_info.get('cwe') else "N/A"
                owasp_html = f'''
                    <div class="owasp-reference">
                        <strong>🛡️ Security Framework Reference:</strong><br>
                        <strong>OWASP:</strong> {html.escape(owasp_info.get('owasp', 'N/A'))}<br>
                        <strong>Description:</strong> {html.escape(owasp_info.get('description', 'N/A'))}<br>
                        <strong>CWE:</strong> {html.escape(cwe_list)}<br>
                    </div>
                '''

            html_parts.append(f'''
                <div class="issue-card" data-severity="{issue['severity']}" data-type="{issue['type']}" data-component="{html.escape(issue['component'])}" data-message="{html.escape(issue['message'])}">
                    <div class="issue-header" onclick="toggleIssueDetails({i})">
                        <div class="issue-title">
                            <span class="issue-severity severity-{issue['severity']}">{issue['severity']}</span>
                            <span class="issue-type">{html.escape(issue['type'])}</span>
                        </div>
                        <div class="issue-message">{html.escape(issue['message'])}</div>
                        <div class="issue-meta">
                            <span><strong>File:</strong> {html.escape(issue['component'])}</span>
                            <span><strong>Line:</strong> {issue['line']}</span>
                            <span><strong>Rule:</strong> {html.escape(issue['rule'].split(':')[-1])}</span>
                        </div>
                    </div>
                    <div class="issue-details" id="issue-details-{i}">
                        <div class="issue-info">
                            <strong>Full Path:</strong> {html.escape(issue['full_component'])}<br>
                            <strong>Status:</strong> {html.escape(issue['status'])}<br>
                            <strong>Author:</strong> {html.escape(issue['author'])}<br>
                            <strong>Created:</strong> {creation_date}<br>
                            {impact_html}
                            <strong>Rule:</strong> {html.escape(issue['rule'])}<br>
                            {owasp_html}
                        </div>
                        {source_code_html}
                    </div>
                </div>
            ''')

        return ''.join(html_parts)

    def _generate_source_code_html(self, source_code_data):
        """Generate HTML for source code preview."""
        if not source_code_data or 'lines' not in source_code_data:
            return ""

        lines = source_code_data['lines']
        issue_line = source_code_data.get('issue_line', 0)

        html_parts = ['<div class="source-code">']

        for line_data in lines:
            line_num = line_data.get('line', '')
            line_code = strip_html_tags(line_data.get('code', ''))

            # Escape HTML in the code
            line_code = html.escape(line_code)

            # Check if this is the issue line
            css_class = "source-line highlight" if line_num == issue_line else "source-line"

            html_parts.append(f'<span class="{css_class}">{line_num:4d}: {line_code}</span>')

        html_parts.append('</div>')
        return ''.join(html_parts)

    def _get_javascript_code(self):
        """Return JavaScript code for interactive functionality."""
        return """
        // Toggle issue details
        function toggleIssueDetails(index) {
            const details = document.getElementById(`issue-details-${index}`);
            details.classList.toggle('expanded');
        }

        // Filter functionality
        function filterIssues() {
            const severityFilter = document.getElementById('severity-filter').value;
            const typeFilter = document.getElementById('type-filter').value;
            const searchFilter = document.getElementById('search-filter').value.toLowerCase();

            const issueCards = document.querySelectorAll('.issue-card');

            issueCards.forEach(card => {
                const severity = card.dataset.severity;
                const type = card.dataset.type;
                const component = card.dataset.component.toLowerCase();
                const message = card.dataset.message.toLowerCase();

                let show = true;

                // Apply severity filter
                if (severityFilter && severity !== severityFilter) {
                    show = false;
                }

                // Apply type filter
                if (typeFilter && type !== typeFilter) {
                    show = false;
                }

                // Apply search filter
                if (searchFilter && !component.includes(searchFilter) && !message.includes(searchFilter)) {
                    show = false;
                }

                card.classList.toggle('hidden', !show);
            });
        }

        // Export to CSV
        function exportToCSV() {
            const headers = ['Severity', 'Type', 'Message', 'Component', 'Line', 'Rule', 'Status', 'Author', 'Created'];
            const csvContent = [headers.join(',')];

            issuesData.forEach(issue => {
                const row = [
                    issue.severity,
                    issue.type,
                    `"${issue.message.replace(/"/g, '""')}"`,
                    issue.component,
                    issue.line,
                    issue.rule,
                    issue.status,
                    issue.author,
                    issue.creation_date
                ];
                csvContent.push(row.join(','));
            });

            downloadFile('sonarqube_issues.csv', csvContent.join('\\n'), 'text/csv');
        }

        // Export to JSON
        function exportToJSON() {
            const jsonContent = JSON.stringify({
                project: '{self.project_key}',
                generated: new Date().toISOString(),
                statistics: statsData,
                issues: issuesData
            }, null, 2);

            downloadFile('sonarqube_issues.json', jsonContent, 'application/json');
        }

        // Download file helper
        function downloadFile(filename, content, contentType) {
            const blob = new Blob([content], { type: contentType });
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        }

        // Print report
        function printReport() {
            window.print();
        }

        // Initialize event listeners
        document.addEventListener('DOMContentLoaded', function() {
            document.getElementById('severity-filter').addEventListener('change', filterIssues);
            document.getElementById('type-filter').addEventListener('change', filterIssues);
            document.getElementById('search-filter').addEventListener('input', filterIssues);
        });
        """

    def run(self):
        """Run the full workflow to generate HTML report."""
        try:
            if not self.login():
                return None

            # Get issues from the API
            issues_data = self.get_project_issues()
            if not issues_data:
                print("No issues found or failed to retrieve issues.")
                # Create a simple HTML report for no issues
                html_content = self._generate_no_issues_html()
                timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
                html_filename = f"{self.output_dir}/sonarqube_report_{self.project_key}_{timestamp}.html"

                with open(html_filename, 'w', encoding='utf-8') as f:
                    f.write(html_content)

                print(f"No issues report generated: {html_filename}")
                return "NO_ISSUES"

            # Get project metrics
            metrics = self.get_project_metrics()

            # Process the issues
            processed_issues = self.process_issues(issues_data)
            if not processed_issues:
                print("Failed to process issues.")
                return None

            # If no metrics were retrieved, try to get basic stats from issues
            if not metrics:
                print("Using fallback metrics from issues data...")
                metrics = self.get_basic_project_stats(issues_data)

            # Generate the HTML report
            html_path = self.generate_html_report(processed_issues, metrics)
            return html_path
        except Exception as e:
            print(f"Error in workflow: {str(e)}")
            return None

    def _generate_no_issues_html(self):
        """Generate a professional HTML report when no issues are found."""
        return f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Code Quality Analysis Report - {html.escape(self.project_key)}</title>
    <style>
        body {{
            font-family: 'Times New Roman', Times, serif;
            line-height: 1.8;
            color: #2c3e50;
            background-color: #ffffff;
            margin: 0;
            padding: 40px;
        }}
        .container {{
            max-width: 800px;
            margin: 0 auto;
            background: white;
            padding: 50px;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
            border: 2px solid #e9ecef;
            border-radius: 8px;
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #2c3e50;
            padding-bottom: 30px;
            margin-bottom: 40px;
        }}
        .success-icon {{
            font-size: 4em;
            color: #28a745;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #2c3e50;
            margin-bottom: 15px;
            font-size: 2.2em;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .project-name {{
            font-size: 1.2em;
            color: #34495e;
            margin-bottom: 20px;
            font-weight: bold;
        }}
        .message {{
            font-size: 1.1em;
            margin-bottom: 30px;
            text-align: justify;
            line-height: 1.8;
            color: #495057;
        }}
        .report-date {{
            color: #7f8c8d;
            font-size: 0.9em;
            font-style: italic;
            text-align: center;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 2px solid #e9ecef;
            text-align: center;
            color: #6c757d;
            font-size: 0.85em;
        }}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <div class="success-icon">✅</div>
            <h1>Code Quality Analysis Report</h1>
            <div class="project-name">Project: {html.escape(self.project_key)}</div>
        </div>

        <div class="content">
            <h2 style="color: #28a745; text-align: center; margin-bottom: 20px;">Analysis Complete - No Issues Found</h2>
            <div class="message">
                The automated code quality analysis has been completed successfully with excellent results.
                No critical issues, security vulnerabilities, or code quality concerns were identified in the
                analyzed codebase. This indicates that the code meets the established quality standards and
                is ready for the next phase of the software delivery process.
            </div>
        </div>

        <div class="footer">
            <p><strong>Code Quality Analysis Report</strong></p>
            <p>Generated by SonarQube Integration System</p>
            <div class="report-date">
                Report Date: {datetime.now().strftime('%B %d, %Y at %H:%M:%S')}
            </div>
        </div>
    </div>
</body>
</html>"""


def main():
    """Main function to parse arguments and run the HTML reporter."""
    parser = argparse.ArgumentParser(description="Generate SonarQube HTML issues report")
    parser.add_argument("--url", required=True, help="SonarQube URL")
    parser.add_argument("--project", required=True, help="SonarQube project key")
    parser.add_argument("--username", required=True, help="SonarQube username")
    parser.add_argument("--password", required=True, help="SonarQube password")
    parser.add_argument("--version", help="Version number of the scanned software")
    parser.add_argument("--output-dir", help="Output directory for the HTML report")

    args = parser.parse_args()

    reporter = SonarQubeHTMLReporter(
        args.url,
        args.project,
        args.username,
        args.password,
        args.output_dir,
        args.version
    )

    result = reporter.run()
    if result == "NO_ISSUES":
        print("No issues found in SonarQube analysis. Generated clean report.")
        return 0
    elif result:
        print(f"HTML report generated successfully: {result}")
        return 0
    else:
        print("Failed to generate HTML report")
        return 1


if __name__ == "__main__":
    sys.exit(main())
