#!/usr/bin/env python3
"""
Test script for SonarQube HTML Reporter

This script tests the HTML reporter functionality with mock data
to ensure it works correctly before connecting to a real SonarQube instance.
"""

import os
import sys
import json
import tempfile
from datetime import datetime, timedelta

# Add the Scripts directory to the path so we can import the reporter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from sonar_html_reporter import SonarQubeHTMLReporter


def create_mock_issues():
    """Create mock issues data for testing."""
    base_date = datetime.now()
    
    mock_issues = [
        {
            "key": "issue-1",
            "severity": "BLOCKER",
            "message": "Potential null pointer dereference in user authentication",
            "component": "src/main/java/com/example/auth/UserService.java",
            "rule": "java:S2259",
            "author": "john.doe@example.com",
            "creation_date": (base_date - timedelta(days=1)).isoformat() + "Z",
            "line": 45,
            "status": "OPEN",
            "type": "BUG",
            "impacts": [
                {"softwareQuality": "RELIABILITY", "severity": "HIGH"}
            ]
        },
        {
            "key": "issue-2",
            "severity": "CRITICAL",
            "message": "SQL injection vulnerability detected",
            "component": "src/main/java/com/example/db/QueryBuilder.java",
            "rule": "java:S3649",
            "author": "jane.smith@example.com",
            "creation_date": (base_date - timedelta(days=3)).isoformat() + "Z",
            "line": 123,
            "status": "CONFIRMED",
            "type": "VULNERABILITY",
            "impacts": [
                {"softwareQuality": "SECURITY", "severity": "HIGH"}
            ]
        },
        {
            "key": "issue-3",
            "severity": "MAJOR",
            "message": "Unused import should be removed",
            "component": "src/main/java/com/example/util/StringUtils.java",
            "rule": "java:S1128",
            "author": "bob.wilson@example.com",
            "creation_date": (base_date - timedelta(days=5)).isoformat() + "Z",
            "line": 8,
            "status": "OPEN",
            "type": "CODE_SMELL",
            "impacts": [
                {"softwareQuality": "MAINTAINABILITY", "severity": "MEDIUM"}
            ]
        },
        {
            "key": "issue-4",
            "severity": "MINOR",
            "message": "Method name should follow camelCase convention",
            "component": "src/main/java/com/example/service/DataProcessor.java",
            "rule": "java:S100",
            "author": "alice.brown@example.com",
            "creation_date": (base_date - timedelta(days=2)).isoformat() + "Z",
            "line": 67,
            "status": "OPEN",
            "type": "CODE_SMELL",
            "impacts": [
                {"softwareQuality": "MAINTAINABILITY", "severity": "LOW"}
            ]
        },
        {
            "key": "issue-5",
            "severity": "INFO",
            "message": "Consider using StringBuilder for string concatenation",
            "component": "src/main/java/com/example/util/MessageBuilder.java",
            "rule": "java:S1643",
            "author": "charlie.davis@example.com",
            "creation_date": (base_date - timedelta(days=7)).isoformat() + "Z",
            "line": 34,
            "status": "OPEN",
            "type": "CODE_SMELL",
            "impacts": [
                {"softwareQuality": "MAINTAINABILITY", "severity": "LOW"}
            ]
        }
    ]
    
    return mock_issues


def create_mock_source_code():
    """Create mock source code data."""
    return {
        "lines": [
            {"line": 40, "code": "public class UserService {"},
            {"line": 41, "code": "    private UserRepository userRepo;"},
            {"line": 42, "code": "    "},
            {"line": 43, "code": "    public User authenticate(String username, String password) {"},
            {"line": 44, "code": "        User user = userRepo.findByUsername(username);"},
            {"line": 45, "code": "        if (user.getPassword().equals(password)) {  // Potential NPE here"},
            {"line": 46, "code": "            return user;"},
            {"line": 47, "code": "        }"},
            {"line": 48, "code": "        return null;"},
            {"line": 49, "code": "    }"},
            {"line": 50, "code": "}"}
        ],
        "issue_line": 45
    }


def create_mock_metrics():
    """Create mock project metrics."""
    return [
        {"metric": "ncloc", "value": "15420"},
        {"metric": "lines", "value": "18750"},
        {"metric": "statements", "value": "12340"},
        {"metric": "functions", "value": "1250"},
        {"metric": "classes", "value": "180"},
        {"metric": "files", "value": "95"},
        {"metric": "directories", "value": "25"},
        {"metric": "complexity", "value": "2847"},
        {"metric": "coverage", "value": "78.5"},
        {"metric": "duplicated_lines_density", "value": "3.2"},
        {"metric": "reliability_rating", "value": "2"},
        {"metric": "security_rating", "value": "1"},
        {"metric": "sqale_rating", "value": "3"},
        {"metric": "bugs", "value": "12"},
        {"metric": "vulnerabilities", "value": "3"},
        {"metric": "code_smells", "value": "156"},
        {"metric": "technical_debt", "value": "2h 30min"},
        {"metric": "sqale_index", "value": "150"}
    ]


class MockSonarQubeHTMLReporter(SonarQubeHTMLReporter):
    """Mock version of the HTML reporter for testing."""

    def __init__(self, output_dir=None):
        # Initialize with dummy values
        super().__init__(
            sonar_url="http://localhost:9000",
            project_key="test-project",
            username="test",
            password="test",
            output_dir=output_dir,
            version="1.2.3-SNAPSHOT"
        )
        self.mock_issues = create_mock_issues()
        self.mock_metrics = create_mock_metrics()
    
    def login(self):
        """Mock login - always successful."""
        print("Mock login successful!")
        return True
    
    def get_project_issues(self):
        """Return mock issues."""
        print(f"Mock: Retrieved {len(self.mock_issues)} issues")
        return self.mock_issues
    
    def get_project_metrics(self):
        """Return mock metrics."""
        print(f"Mock: Retrieved {len(self.mock_metrics)} metrics")
        return self.mock_metrics
    
    def get_source_code(self, component_key, line_number):
        """Return mock source code."""
        if line_number and line_number != 'N/A':
            return create_mock_source_code()
        return None


def test_html_generation():
    """Test HTML report generation with mock data."""
    print("Testing SonarQube HTML Reporter")
    print("=" * 40)
    
    # Create temporary directory for output
    with tempfile.TemporaryDirectory() as temp_dir:
        print(f"Output directory: {temp_dir}")
        
        # Create mock reporter
        reporter = MockSonarQubeHTMLReporter(output_dir=temp_dir)
        
        # Test the full workflow
        result = reporter.run()
        
        if result and result != "NO_ISSUES":
            print(f"\n✅ Test PASSED!")
            print(f"HTML report generated: {result}")
            
            # Verify the file exists and has content
            if os.path.exists(result):
                file_size = os.path.getsize(result)
                print(f"File size: {file_size:,} bytes")
                
                # Read and validate HTML content
                with open(result, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Basic validation checks
                checks = [
                    ("HTML structure", "<!DOCTYPE html>" in content),
                    ("Title present", "<title>" in content),
                    ("CSS styles", "<style>" in content),
                    ("JavaScript", "<script>" in content),
                    ("Issues data", "issuesData" in content),
                    ("Dashboard", "dashboard" in content),
                    ("Filters", "severity-filter" in content),
                    ("Issue cards", "issue-card" in content),
                    ("Version info", "Version: 1.2.3-SNAPSHOT" in content),
                    ("Lines of Code", "Lines of Code" in content),
                    ("Code metrics", "15,420" in content),  # Formatted LOC number
                    ("OWASP reference", "OWASP Top 10" in content),
                    ("Security framework", "Security Framework Compliance" in content),
                    ("OWASP security card", "OWASP Security" in content),
                    ("OWASP Top 10 list", "OWASP Top 10 2021 - Web Application Security Risks" in content),
                    ("A01 Broken Access Control", "A01:2021" in content and "Broken Access Control" in content),
                    ("A10 SSRF", "A10:2021" in content and "Server-Side Request Forgery" in content),
                    ("Severity ordering", _check_severity_ordering(content)),
                    ("Detailed issue sorting", _check_detailed_issue_sorting(content))
                ]
                
                print("\nValidation Results:")
                print("-" * 20)
                all_passed = True
                for check_name, passed in checks:
                    status = "✅ PASS" if passed else "❌ FAIL"
                    print(f"{check_name:20} {status}")
                    if not passed:
                        all_passed = False
                
                if all_passed:
                    print(f"\n🎉 All validation checks passed!")
                    print(f"📄 Report available at: {result}")
                    
                    # Try to open in browser if available
                    try:
                        import webbrowser
                        print(f"\n🌐 Opening report in browser...")
                        webbrowser.open(f"file://{result}")
                    except:
                        print(f"\n💡 Open this file in your browser to view the report:")
                        print(f"   file://{result}")
                else:
                    print(f"\n❌ Some validation checks failed!")
                    return False
            else:
                print(f"❌ Test FAILED: Report file not found")
                return False
        else:
            print(f"❌ Test FAILED: Report generation failed")
            return False
    
    return True


def test_no_issues_scenario():
    """Test the no issues scenario."""
    print("\nTesting No Issues Scenario")
    print("=" * 30)
    
    with tempfile.TemporaryDirectory() as temp_dir:
        # Create reporter with no issues
        reporter = MockSonarQubeHTMLReporter(output_dir=temp_dir)
        reporter.mock_issues = []  # No issues
        
        result = reporter.run()
        
        if result == "NO_ISSUES":
            print("✅ No issues scenario handled correctly")
            
            # Find the generated HTML file
            html_files = [f for f in os.listdir(temp_dir) if f.endswith('.html')]
            if html_files:
                html_file = os.path.join(temp_dir, html_files[0])
                with open(html_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if "Analysis Complete - No Issues Found" in content:
                    print("✅ No issues HTML content is correct")
                    return True
                else:
                    print("❌ No issues HTML content is incorrect")
                    print("Content preview:", content[:500])
                    return False
            else:
                print("❌ No HTML file generated for no issues scenario")
                return False
        else:
            print("❌ No issues scenario not handled correctly")
            return False


def _check_severity_ordering(content):
    """Check if severity items appear in correct order: CRITICAL, MAJOR, MINOR."""
    import re

    # Find all severity items in the HTML
    severity_pattern = r'<div class="severity-item severity-(\w+)">'
    matches = re.findall(severity_pattern, content)

    if not matches:
        return True  # No severity items found, pass the test

    # Check if the order starts with CRITICAL, MAJOR, MINOR (in that sequence)
    expected_order = ['CRITICAL', 'MAJOR', 'MINOR']

    # Find the positions of expected severities
    positions = {}
    for i, severity in enumerate(matches):
        if severity in expected_order and severity not in positions:
            positions[severity] = i

    # Check if they appear in the correct order
    for i in range(len(expected_order) - 1):
        current = expected_order[i]
        next_severity = expected_order[i + 1]

        if current in positions and next_severity in positions:
            if positions[current] >= positions[next_severity]:
                return False  # Wrong order

    return True


def _check_detailed_issue_sorting(content):
    """Check if detailed issues appear in correct severity order: CRITICAL, MAJOR, MINOR."""
    import re

    # Find all issue cards with their severity
    issue_pattern = r'<div class="issue-card[^>]*>.*?<span class="severity[^>]*>([^<]+)</span>'
    matches = re.findall(issue_pattern, content, re.DOTALL)

    if len(matches) < 2:
        return True  # Not enough issues to check ordering

    # Define severity priority (lower number = higher priority)
    severity_priority = {'CRITICAL': 0, 'MAJOR': 1, 'MINOR': 2, 'INFO': 3, 'BLOCKER': 4}

    # Check if issues are in correct order
    for i in range(len(matches) - 1):
        current_severity = matches[i].strip()
        next_severity = matches[i + 1].strip()

        current_priority = severity_priority.get(current_severity, 999)
        next_priority = severity_priority.get(next_severity, 999)

        # If current issue has lower priority than next, it's wrong order
        if current_priority > next_priority:
            return False

    return True


def main():
    """Run all tests."""
    print("SonarQube HTML Reporter Test Suite")
    print("=" * 50)
    
    tests = [
        ("HTML Generation Test", test_html_generation),
        ("No Issues Scenario Test", test_no_issues_scenario)
    ]
    
    passed = 0
    total = len(tests)
    
    for test_name, test_func in tests:
        print(f"\n🧪 Running: {test_name}")
        try:
            if test_func():
                passed += 1
                print(f"✅ {test_name} PASSED")
            else:
                print(f"❌ {test_name} FAILED")
        except Exception as e:
            print(f"❌ {test_name} FAILED with exception: {e}")
    
    print(f"\n" + "=" * 50)
    print(f"Test Results: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 All tests passed! The HTML reporter is working correctly.")
        return 0
    else:
        print("❌ Some tests failed. Please check the implementation.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
