# Method HandleRequestAsync passes 'ProcurementId' as the paramName argument to a ArgumentException constructor. Replace this argument with one of the method's parameter names. Note that the provided parameter name should have the exact casing as declared on the method.

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
| **Created** | 2025-09-04T10:28:52+0000 |

## Location

- **File**: `Backend/GHB.DP2.Application/Features/Dashboard/Timeline.cs`
- **Line**: 58

## Description

Method HandleRequestAsync passes 'ProcurementId' as the paramName argument to a ArgumentException constructor. Replace this argument with one of the method's parameter names. Note that the provided parameter name should have the exact casing as declared on the method.

## Issue Key

`b774b1d1-0452-4c08-81c3-c279a91b0f59`
