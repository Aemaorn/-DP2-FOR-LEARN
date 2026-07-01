# The logging message template should not vary between calls to 'LoggerExtensions.LogDebug(ILogger, string?, params object?[])'

## Details

| Property | Value |
|----------|-------|
| **Severity** | INFO |
| **Type** | CODE_SMELL |
| **Rule** | external_roslyn:CA2254 |
| **Status** | OPEN |
| **Effort** | 0min |
| **Tags** | None |
| **Clean Code Attribute** | CONVENTIONAL |
| **Created** | 2025-05-15T10:55:54+0000 |

## Location

- **File**: `Backend/GHB.DP2.Infrastructure/Interceptors/AuditInfoInterceptor.cs`
- **Line**: 310

## Description

The logging message template should not vary between calls to 'LoggerExtensions.LogDebug(ILogger, string?, params object?[])'

## Issue Key

`760ef142-1282-4b60-b93d-7722dbef5be0`
