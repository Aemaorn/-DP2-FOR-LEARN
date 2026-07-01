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
| **Created** | 2025-09-10T09:36:43+0000 |

## Location

- **File**: `Backend/GHB.DP2.Domain/Plan/Plan.cs`
- **Line**: 216

## Description

Use 'string.StartsWith(char)' instead of 'string.StartsWith(string)' when you have a string with a single char

## Issue Key

`ff4277e5-0e5e-4b65-892d-699ee6f88907`
