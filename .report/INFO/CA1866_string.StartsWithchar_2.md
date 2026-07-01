# Use 'string.StartsWith(char)' instead of 'string.StartsWith(string)' when you have a string with a single char

## Details

| Property | Value |
|----------|-------|
| **Severity** | INFO |
| **Type** | CODE_SMELL |
| **Rule** | external_roslyn:CA1866 |
| **Status** | OPEN |
| **Effort** | 0min |
| **Tags** | None |
| **Clean Code Attribute** | CONVENTIONAL |
| **Created** | 2025-09-04T10:28:52+0000 |

## Location

- **File**: `Backend/GHB.DP2.Domain/Procurement/PInvite/PInvite.cs`
- **Line**: 64

## Description

Use 'string.StartsWith(char)' instead of 'string.StartsWith(string)' when you have a string with a single char

## Issue Key

`297bf9b6-8bb4-4472-8ffd-79eb3ff8c82e`
