# Prefer comparing 'Length' to 0 rather than using 'Any()', both for clarity and for performance

## Details

| Property | Value |
|----------|-------|
| **Severity** | INFO |
| **Type** | CODE_SMELL |
| **Rule** | external_roslyn:CA1860 |
| **Status** | OPEN |
| **Effort** | 0min |
| **Tags** | None |
| **Clean Code Attribute** | CONVENTIONAL |
| **Created** | 2025-07-22T13:02:44+0000 |

## Location

- **File**: `Backend/GHB.DP2.Application/EventHandlers/ContractAgreement/ContractDraftApproveEventHandler.cs`
- **Line**: 154

## Description

Prefer comparing 'Length' to 0 rather than using 'Any()', both for clarity and for performance

## Issue Key

`7fd4655f-b312-4d02-a064-1cdd1f74b76e`
