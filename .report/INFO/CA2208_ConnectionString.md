# Method CreateDbContext passes 'ConnectionString' as the paramName argument to a ArgumentNullException constructor. Replace this argument with one of the method's parameter names. Note that the provided parameter name should have the exact casing as declared on the method.

## Details

| Property | Value |
|----------|-------|
| **Severity** | INFO |
| **Type** | CODE_SMELL |
| **Rule** | external_roslyn:CA2208 |
| **Status** | OPEN |
| **Effort** | 0min |
| **Tags** | None |
| **Clean Code Attribute** | CONVENTIONAL |
| **Created** | 2025-05-14T02:28:38+0000 |

## Location

- **File**: `Backend/GHB.DP2.Infrastructure/Dp2DbContext.cs`
- **Line**: 283

## Description

Method CreateDbContext passes 'ConnectionString' as the paramName argument to a ArgumentNullException constructor. Replace this argument with one of the method's parameter names. Note that the provided parameter name should have the exact casing as declared on the method.

## Issue Key

`bd040d20-b754-4330-a907-a3b9e1e3e684`
