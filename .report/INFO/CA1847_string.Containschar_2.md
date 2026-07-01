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
| **Created** | 2025-09-11T11:58:40+0000 |

## Location

- **File**: `Backend/GHB.DP2.Application/Features/SystemUtility/SuUser/SignIn.cs`
- **Line**: 67

## Description

Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching for a single character

## Issue Key

`9313efda-c3ec-42ed-b18e-8172382bd10e`
