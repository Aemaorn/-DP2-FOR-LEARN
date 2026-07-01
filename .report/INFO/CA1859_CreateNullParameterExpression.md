# Change return type of method 'CreateNullParameterExpression' from 'System.Linq.Expressions.Expression' to 'System.Linq.Expressions.ConstantExpression' for improved performance

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
| **Created** | 2025-09-11T11:58:40+0000 |

## Location

- **File**: `Backend/GHB.DP2.Application/JopService/RecurringRegistrar.cs`
- **Line**: 145

## Description

Change return type of method 'CreateNullParameterExpression' from 'System.Linq.Expressions.Expression' to 'System.Linq.Expressions.ConstantExpression' for improved performance

## Issue Key

`2f2fbee2-565c-4c57-b35b-fe466e3778ce`
