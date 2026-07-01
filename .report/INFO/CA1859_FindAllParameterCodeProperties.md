# Change return type of method 'FindAllParameterCodeProperties' from 'System.Collections.Generic.IEnumerable<(string Path, GHB.DP2.Domain.SystemUtility.ParameterCode Code)>' to 'System.Collections.Generic.List<(string Path, GHB.DP2.Domain.SystemUtility.ParameterCode Code)>' for improved performance

## Details

| Property | Value |
|----------|-------|
| **Severity** | INFO |
| **Type** | CODE_SMELL |
| **Rule** | external_roslyn:CA1859 |
| **Status** | OPEN |
| **Effort** | 0min |
| **Tags** | None |
| **Clean Code Attribute** | CONVENTIONAL |
| **Created** | 2025-07-11T07:37:13+0000 |

## Location

- **File**: `Backend/GHB.DP2.Application/Features/ContractAgreement/ContractDraft/Update.cs`
- **Line**: 160

## Description

Change return type of method 'FindAllParameterCodeProperties' from 'System.Collections.Generic.IEnumerable<(string Path, GHB.DP2.Domain.SystemUtility.ParameterCode Code)>' to 'System.Collections.Generic.List<(string Path, GHB.DP2.Domain.SystemUtility.ParameterCode Code)>' for improved performance

## Issue Key

`df018e56-6e94-4807-9b53-b43bba94d107`
