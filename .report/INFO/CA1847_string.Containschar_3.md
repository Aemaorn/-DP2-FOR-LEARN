# Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching for a single character

## Details

| Property | Value |
|----------|-------|
| **Severity** | INFO |
| **Type** | CODE_SMELL |
| **Rule** | external_roslyn:CA1847 |
| **Status** | OPEN |
| **Effort** | 0min |
| **Tags** | None |
| **Clean Code Attribute** | CONVENTIONAL |
| **Created** | 2025-05-14T02:28:38+0000 |

## Location

- **File**: `Backend/GHB.DP2.Infrastructure/Services/ActiveDirectory/ActiveDirectoryService.cs`
- **Line**: 31

## Description

Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching for a single character

## Issue Key

`b2504418-5105-4676-97b5-9e6c77406e76`
