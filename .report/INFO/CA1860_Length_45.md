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
| **Created** | 2025-07-17T05:21:13+0000 |

## Location

- **File**: `Backend/GHB.DP2.Application/EventHandlers/ContractAgreement/ContractDraftApproveEventHandler.cs`
- **Line**: 66

## Description

Prefer comparing 'Length' to 0 rather than using 'Any()', both for clarity and for performance

## Issue Key

`ae50d2b5-8689-44ab-9a00-f077a75dde52`
