# Worklist API Performance Optimization Plan

## Executive Summary

| Item | Detail |
|------|--------|
| **API Endpoint** | `GET /api/worklist` |
| **Current Performance** | ~60 seconds |
| **Target Performance** | ~5-15 seconds (75-90% faster) |
| **Root Cause** | Sequential query execution + Duplicate queries |
| **Main Bottleneck** | Procurement queries (~42 seconds / 70% of total time) |

---

## Task Checklist

### Legend
- [ ] = ยังไม่ได้ทำ
- [x] = ทำเสร็จแล้ว
- [-] = ทำไม่ได้ / ยกเลิก

---

## Phase 1: Infrastructure Setup (Prerequisites)

### 1.1 Register IDbContextFactory ใน DI Container

**File:** `Program.cs` หรือ `Startup.cs`

**Description:** เพิ่ม `IDbContextFactory<Dp2DbContext>` เพื่อรองรับการสร้าง DbContext หลายตัวสำหรับ Parallel queries

**Code to add:**
```csharp
services.AddDbContextFactory<Dp2DbContext>(options =>
    options.UseNpgsql(connectionString));
```

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 1.2 ปรับ Constructor ของ GetWorklistSeparated

**File:** `Backend/GHB.DP2.Application/Features/WorkList/GetList.cs`

**Description:** Inject `IDbContextFactory<Dp2DbContext>` เข้าไปใน constructor

**Code to change:**
```csharp
// Before
public class GetWorklistSeparated : EndpointBase<...>
{
    public GetWorklistSeparated(Dp2DbContext dbContext, ...)
    {
        this.dbContext = dbContext;
    }
}

// After
public class GetWorklistSeparated : EndpointBase<...>
{
    private readonly IDbContextFactory<Dp2DbContext> dbContextFactory;

    public GetWorklistSeparated(IDbContextFactory<Dp2DbContext> dbContextFactory, ...)
    {
        this.dbContextFactory = dbContextFactory;
    }
}
```

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

## Phase 2: Cache Duplicate Queries (Quick Win)

### 2.1 Cache Count Queries ใน ProcurementProgram

**File:** `Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs`
**Lines:** ~57-61

**Description:** Queries Pw119, P79Clause2, PPettyCash, PPettyCashReimbursement ถูกเรียก 2 ครั้ง สามารถ cache ได้

**Problem (ปัจจุบัน):**
```csharp
// ครั้งที่ 1: สำหรับ counts.Procurement
counts.Procurement = await baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct)
                   + await pw119Query.CountAsync(ct)           // <-- เรียกครั้งที่ 1
                   + await p79Clause2Query.CountAsync(ct)      // <-- เรียกครั้งที่ 1
                   + await pettyCashQuery.CountAsync(ct)       // <-- เรียกครั้งที่ 1
                   + await pettyCashReimbursementQuery.CountAsync(ct);  // <-- เรียกครั้งที่ 1

// ครั้งที่ 2: สำหรับ counts.ContractAgreement (ซ้ำ!)
counts.ContractAgreement = await baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct)
                         + await pw119Query.CountAsync(ct)           // <-- เรียกครั้งที่ 2
                         + await p79Clause2Query.CountAsync(ct)      // <-- เรียกครั้งที่ 2
                         + await pettyCashQuery.CountAsync(ct)       // <-- เรียกครั้งที่ 2
                         + await pettyCashReimbursementQuery.CountAsync(ct);  // <-- เรียกครั้งที่ 2
```

**Solution (แก้ไข):**
```csharp
// เรียก count ครั้งเดียว แล้ว cache ไว้
var pw119Count = await pw119Query.CountAsync(ct);
var p79Count = await p79Clause2Query.CountAsync(ct);
var pettyCashCount = await pettyCashQuery.CountAsync(ct);
var pettyCashReimbursementCount = await pettyCashReimbursementQuery.CountAsync(ct);

// ใช้ค่าที่ cache ไว้
counts.PreProcurement = await baseQuery.Where(x => x.Step == ProcurementStep.PreProcurement).CountAsync(ct);

counts.Procurement = await baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct)
                   + pw119Count + p79Count + pettyCashCount + pettyCashReimbursementCount;

counts.ContractAgreement = await baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct)
                         + pw119Count + p79Count + pettyCashCount + pettyCashReimbursementCount;
```

**Impact:** ประหยัด 4 queries (~8-12ms)

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

## Phase 3: Parallel Processing Implementation

### 3.1 Parallel Count Queries ใน ProcurementProgram (HIGH IMPACT)

**File:** `Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs`
**Lines:** ~57-61

**Description:** ทำ count queries 7 ตัวให้ parallel แทนที่จะ sequential

**Problem (ปัจจุบัน - Sequential):**
```csharp
counts.PreProcurement = await baseQuery.Where(x => x.Step == ProcurementStep.PreProcurement).CountAsync(ct);
counts.Procurement = await baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct) + ...;
counts.ContractAgreement = await baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct) + ...;
```

**Solution (Parallel):**
```csharp
// เริ่ม tasks ทั้งหมดพร้อมกัน (ไม่ await ทันที)
var preProcCountTask = baseQuery.Where(x => x.Step == ProcurementStep.PreProcurement).CountAsync(ct);
var procCountTask = baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct);
var contractCountTask = baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct);
var pw119CountTask = pw119Query.CountAsync(ct);
var p79CountTask = p79Clause2Query.CountAsync(ct);
var pettyCashCountTask = pettyCashQuery.CountAsync(ct);
var pettyCashReimbursementCountTask = pettyCashReimbursementQuery.CountAsync(ct);

// รอทั้งหมดพร้อมกัน
await Task.WhenAll(
    preProcCountTask, procCountTask, contractCountTask,
    pw119CountTask, p79CountTask, pettyCashCountTask, pettyCashReimbursementCountTask);

// ดึงผลลัพธ์
var preProcCount = await preProcCountTask;
var procCount = await procCountTask;
var contractCount = await contractCountTask;
var pw119Count = await pw119CountTask;
var p79Count = await p79CountTask;
var pettyCashCount = await pettyCashCountTask;
var pettyCashReimbursementCount = await pettyCashReimbursementCountTask;

counts.PreProcurement = preProcCount;
counts.Procurement = procCount + pw119Count + p79Count + pettyCashCount + pettyCashReimbursementCount;
counts.ContractAgreement = contractCount + pw119Count + p79Count + pettyCashCount + pettyCashReimbursementCount;
```

**Impact:** ลดเวลาจาก ~30 วินาที เหลือ ~10 วินาที (67% faster)

**Time Breakdown:**
| Query | Current Time |
|-------|-------------|
| Procurement Count (PreProcurement) | 10,021ms |
| Procurement Count (Procurement) | 10,390ms |
| Procurement Count (ContractAgreement) | 9,608ms |
| Pw119 Count | 2ms |
| P79Clause2 Count | 2ms |
| PPettyCash Count | 3ms |
| PPettyCashReimbursement Count | 2ms |

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 3.2 Parallel Data Queries ใน ProcurementProgram

**File:** `Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs`
**Lines:** ~149-153

**Description:** ทำ data queries 5 ตัวให้ parallel

**Problem (ปัจจุบัน - Sequential):**
```csharp
var procurementRecords = await procurementQueryWithIncludes.ToListAsync(ct);
var pw119Records = await pw119QueryWithIncludes.ToListAsync(ct);
var p79Clause2Records = await p79Clause2QueryWithIncludes.ToListAsync(ct);
var pettyCashRecords = await pettyCashQueryWithIncludes.ToListAsync(ct);
var pettyCashReimbursementRecords = await pettyCashReimbursementQueryWithIncludes.ToListAsync(ct);
```

**Solution (Parallel):**
```csharp
// เริ่ม tasks ทั้งหมดพร้อมกัน
var procurementTask = procurementQueryWithIncludes.ToListAsync(ct);
var pw119Task = pw119QueryWithIncludes.ToListAsync(ct);
var p79Clause2Task = p79Clause2QueryWithIncludes.ToListAsync(ct);
var pettyCashTask = pettyCashQueryWithIncludes.ToListAsync(ct);
var pettyCashReimbursementTask = pettyCashReimbursementQueryWithIncludes.ToListAsync(ct);

// รอทั้งหมดพร้อมกัน
await Task.WhenAll(procurementTask, pw119Task, p79Clause2Task, pettyCashTask, pettyCashReimbursementTask);

// ดึงผลลัพธ์
var procurementRecords = await procurementTask;
var pw119Records = await pw119Task;
var p79Clause2Records = await p79Clause2Task;
var pettyCashRecords = await pettyCashTask;
var pettyCashReimbursementRecords = await pettyCashReimbursementTask;
```

**Impact:** ลดเวลาจาก ~12 วินาที เหลือ ~3 วินาที (75% faster)

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 3.3 Parallel Count Queries ใน ContractManagementProgram

**File:** `Backend/GHB.DP2.Application/Features/WorkList/Programs/ContractManagementProgram.cs`
**Lines:** ~61-62

**Description:** ทำ count queries 4 ตัวให้ parallel

**Problem (ปัจจุบัน - Sequential):**
```csharp
counts.ContractManagement = await deliveryAcceptancePeriodQuery.CountAsync(ct)
                          + await disbursementApprovalQuery.CountAsync(ct)
                          + await contractTerminationQuery.CountAsync(ct)
                          + await contractGuaranteeReturnQuery.CountAsync(ct);
```

**Solution (Parallel):**
```csharp
var deliveryCountTask = deliveryAcceptancePeriodQuery.CountAsync(ct);
var disbursementCountTask = disbursementApprovalQuery.CountAsync(ct);
var terminationCountTask = contractTerminationQuery.CountAsync(ct);
var guaranteeCountTask = contractGuaranteeReturnQuery.CountAsync(ct);

await Task.WhenAll(deliveryCountTask, disbursementCountTask, terminationCountTask, guaranteeCountTask);

counts.ContractManagement = await deliveryCountTask
                          + await disbursementCountTask
                          + await terminationCountTask
                          + await guaranteeCountTask;
```

**Impact:** ลดเวลา ~70%

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 3.4 Parallel Data Queries ใน ContractManagementProgram

**File:** `Backend/GHB.DP2.Application/Features/WorkList/Programs/ContractManagementProgram.cs`
**Lines:** ~84-114

**Description:** ทำ data queries 4 ตัวให้ parallel

**Problem (ปัจจุบัน - Sequential):**
```csharp
var deliveryAcceptanceItems = await deliveryAcceptancePeriodQuery.Select(...).ToListAsync(ct);
var disbursementApprovalItems = await disbursementApprovalsQuery.Select(...).ToListAsync(ct);
var contractTerminationItems = await contractTerminationQuery.Select(...).ToListAsync(ct);
var contractGuaranteeReturnItems = await contractGuaranteeReturnQuery.Select(...).ToListAsync(ct);
```

**Solution (Parallel):**
```csharp
var deliveryTask = deliveryAcceptancePeriodQuery.Select(...).ToListAsync(ct);
var disbursementTask = disbursementApprovalsQuery.Select(...).ToListAsync(ct);
var terminationTask = contractTerminationQuery.Select(...).ToListAsync(ct);
var guaranteeTask = contractGuaranteeReturnQuery.Select(...).ToListAsync(ct);

await Task.WhenAll(deliveryTask, disbursementTask, terminationTask, guaranteeTask);

var deliveryAcceptanceItems = await deliveryTask;
var disbursementApprovalItems = await disbursementTask;
var contractTerminationItems = await terminationTask;
var contractGuaranteeReturnItems = await guaranteeTask;
```

**Impact:** ลดเวลา ~70%

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 3.5 Parallel Queries ใน CombinedWorklistBuilder

**File:** `Backend/GHB.DP2.Application/Features/WorkList/Programs/CombinedWorklistBuilder.cs`
**Lines:** ~330-340

**Description:** ทำ 11 queries ให้ parallel (เมื่อ IncludeAll=true)

**Problem (ปัจจุบัน - Sequential แม้ชื่อตัวแปรจะเป็น "Task"):**
```csharp
var planTask = await planQuery.ToListAsync(ct);                        // ไม่ใช่ Task จริง!
var announcementTask = await announcementQuery.ToListAsync(ct);
var procurementTask = await procurementQuery.ToListAsync(ct);
var pw119Task = await pw119Query.ToListAsync(ct);
var p79Clause2Task = await p79Clause2Query.ToListAsync(ct);
var pettyCashTask = await pettyCashQuery.ToListAsync(ct);
var pettyCashReimbursementTask = await pettyCashReimbursementQuery.ToListAsync(ct);
var deliveryAcceptanceTask = await deliveryAcceptanceQuery.ToListAsync(ct);
var disbursementApprovalTask = await disbursementApprovalQuery.ToListAsync(ct);
var contractTerminationTask = await contractTerminationQuery.ToListAsync(ct);
var contractGuaranteeReturnTask = await contractGuaranteeReturnQuery.ToListAsync(ct);
```

**Solution (Parallel):**
```csharp
// เริ่ม tasks ทั้งหมดพร้อมกัน (ไม่ await ทันที)
var planTask = planQuery.ToListAsync(ct);
var announcementTask = announcementQuery.ToListAsync(ct);
var procurementTask = procurementQuery.ToListAsync(ct);
var pw119Task = pw119Query.ToListAsync(ct);
var p79Clause2Task = p79Clause2Query.ToListAsync(ct);
var pettyCashTask = pettyCashQuery.ToListAsync(ct);
var pettyCashReimbursementTask = pettyCashReimbursementQuery.ToListAsync(ct);
var deliveryAcceptanceTask = deliveryAcceptanceQuery.ToListAsync(ct);
var disbursementApprovalTask = disbursementApprovalQuery.ToListAsync(ct);
var contractTerminationTask = contractTerminationQuery.ToListAsync(ct);
var contractGuaranteeReturnTask = contractGuaranteeReturnQuery.ToListAsync(ct);

// รอทั้งหมดพร้อมกัน
await Task.WhenAll(
    planTask, announcementTask, procurementTask,
    pw119Task, p79Clause2Task, pettyCashTask, pettyCashReimbursementTask,
    deliveryAcceptanceTask, disbursementApprovalTask,
    contractTerminationTask, contractGuaranteeReturnTask);

// ดึงผลลัพธ์และ combine
var combinedItems = (await planTask)
    .Concat(await announcementTask)
    .Concat(await procurementTask)
    .Concat(await pw119Task)
    .Concat(await p79Clause2Task)
    .Concat(await pettyCashTask)
    .Concat(await pettyCashReimbursementTask)
    .Concat(await deliveryAcceptanceTask)
    .Concat(await disbursementApprovalTask)
    .Concat(await contractTerminationTask)
    .Concat(await contractGuaranteeReturnTask)
    .OrderByDescending(x => x.SortAt ?? DateTimeOffset.MinValue)
    .ThenByDescending(x => x.Number)
    .ToList();
```

**Impact:** ลดเวลาจาก ~15 วินาที เหลือ ~5 วินาที (67% faster)

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 3.6 Parallel Main Sections ใน GetList.cs (Optional - ต้องทำ Phase 1 ก่อน)

**File:** `Backend/GHB.DP2.Application/Features/WorkList/GetList.cs`
**Lines:** ~311-316

**Description:** ทำ 6 sections ให้ parallel (ต้องใช้ IDbContextFactory)

**Problem (ปัจจุบัน - Sequential):**
```csharp
await this.planProgram.ProcessPlanSectionAsync(req, userContext, counts, sections, ct);
await this.planAnnouncementProgram.ProcessPlanAnnouncementSectionAsync(req, userContext, counts, sections, ct);
await this.procurementProgram.ProcessProcurementSectionsAsync(req, userContext, counts, sections, ct);
await this.contractManagementProgram.ProcessContractManagementSectionAsync(req, userContext, counts, sections, ct);
await this.contractAmendmentProgram.ProcessContractAmendmentSectionAsync(req, sections, ct);
await this.expenseDisbursementProgram.ProcessExpenseDisbursementSectionAsync(req, userContext, sections, counts, ct);
```

**Solution (Parallel with IDbContextFactory):**
```csharp
// สร้าง parallel tasks สำหรับแต่ละ section
var planTask = Task.Run(async () =>
{
    await using var db = await this.dbContextFactory.CreateDbContextAsync(ct);
    var program = new PlanProgram(db);
    return await program.ProcessPlanSectionAsync(req, userContext, ct);
}, ct);

var announcementTask = Task.Run(async () =>
{
    await using var db = await this.dbContextFactory.CreateDbContextAsync(ct);
    var program = new PlanAnnouncementProgram(db);
    return await program.ProcessPlanAnnouncementSectionAsync(req, userContext, ct);
}, ct);

// ... เพิ่ม tasks อื่นๆ

// รอทุก tasks เสร็จพร้อมกัน
await Task.WhenAll(planTask, announcementTask, ...);

// Merge results
```

**Prerequisites:**
- [ ] Phase 1.1 (Register IDbContextFactory) ต้องทำก่อน
- [ ] Phase 1.2 (ปรับ Constructor) ต้องทำก่อน

**Impact:** ลดเวลาจาก ~60 วินาที เหลือ ~15 วินาที (75% faster)

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

## Phase 4: Database Indexing (DBA Task)

### 4.1 เพิ่ม Index สำหรับ Procurement Access Patterns

**Description:** เพิ่ม indexes เพื่อเร่ง query ที่ใช้เวลานาน

**SQL Commands:**
```sql
-- Primary bottleneck: Procurement table access patterns
CREATE INDEX CONCURRENTLY idx_procurement_step_isdeleted
ON "Procurement"."Procurement" ("Step", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_procurement_processtype_isdeleted
ON "Procurement"."Procurement" ("ProcessType", "IsDeleted");

-- Acceptor tables with common query patterns
CREATE INDEX CONCURRENTLY idx_ppappoint_acceptors_lookup
ON "Procurement"."PpAppointAcceptors" ("PpAppointId", "Type", "Status", "Sequence");

CREATE INDEX CONCURRENTLY idx_pptordraft_acceptors_lookup
ON "Procurement"."PpTorDraftAcceptors" ("PpTorDraftId", "Type", "Status", "Sequence", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_ppmedianprice_acceptors_lookup
ON "Procurement"."PpMedianPriceAcceptor" ("MedianPriceId", "Type", "Status", "Sequence", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_pppurchaserequisition_acceptors_lookup
ON "Procurement"."PpPurchaseRequisitionAcceptors" ("PpPurchaseRequisitionId", "Type", "Status", "Sequence", "IsDeleted");

-- Assignee tables with ORDER BY patterns
CREATE INDEX CONCURRENTLY idx_pptordraft_assignee_lookup
ON "Procurement"."PpTorDraftAssignee" ("PpTorDraftId", "Type", "Sequence" DESC, "IsDeleted");

CREATE INDEX CONCURRENTLY idx_ppmedianprice_assignee_lookup
ON "Procurement"."PpMedianPriceAssignee" ("MedianPriceId", "Type", "Sequence" DESC, "IsDeleted");

-- Committee tables
CREATE INDEX CONCURRENTLY idx_ppappoint_tordraft_committee
ON "Procurement"."PpAppointTorDraftCommittee" ("PpAppointId", "SuUserId", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_ppappoint_medianprice_committee
ON "Procurement"."PpAppointMedianPriceCommittee" ("PpAppointId", "SuUserId", "IsDeleted");

-- Delegatee lookup
CREATE INDEX CONCURRENTLY idx_sudelegatee_suuserid
ON "SystemUtility"."SuDelegatee" ("SuUserId");
```

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] idx_procurement_step_isdeleted | | | |
| [ ] idx_procurement_processtype_isdeleted | | | |
| [ ] idx_ppappoint_acceptors_lookup | | | |
| [ ] idx_pptordraft_acceptors_lookup | | | |
| [ ] idx_ppmedianprice_acceptors_lookup | | | |
| [ ] idx_pppurchaserequisition_acceptors_lookup | | | |
| [ ] idx_pptordraft_assignee_lookup | | | |
| [ ] idx_ppmedianprice_assignee_lookup | | | |
| [ ] idx_ppappoint_tordraft_committee | | | |
| [ ] idx_ppappoint_medianprice_committee | | | |
| [ ] idx_sudelegatee_suuserid | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

## Phase 5: Testing & Verification

### 5.1 Performance Testing Before/After

**Description:** ทดสอบ performance ก่อนและหลังแก้ไข

| Test Case | Before (ms) | After (ms) | Improvement |
|-----------|-------------|------------|-------------|
| IncludeAll=true, pageSize=10 | | | |
| IncludeAll=false, pageSize=10 | | | |
| IncludeAll=true, pageSize=50 | | | |

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 5.2 Functional Testing

**Description:** ตรวจสอบว่า API ยังทำงานถูกต้องหลังแก้ไข

| Test Case | Expected | Actual | Pass/Fail |
|-----------|----------|--------|-----------|
| Count ของแต่ละ section ถูกต้อง | | | |
| Data pagination ถูกต้อง | | | |
| Sorting ถูกต้อง | | | |
| CombinedWorklist รวมข้อมูลครบ | | | |
| ไม่มี duplicate items | | | |

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

### 5.3 Monitor Connection Pool Usage

**Description:** ตรวจสอบว่าไม่มี connection pool exhaustion

**Check Items:**
- [ ] Check `Max Pool Size` ใน connection string
- [ ] Monitor active connections ระหว่าง peak usage
- [ ] ไม่มี timeout errors

| Status | Assigned To | Date | Notes |
|--------|-------------|------|-------|
| [ ] | | | |

**Developer Comments:**
```
ทำได้/ทำไม่ได้:
เหตุผล:
ปัญหาที่พบ:
```

---

## Summary: Implementation Priority

| Priority | Task | Effort | Impact | Dependencies |
|----------|------|--------|--------|--------------|
| **1 (Quick Win)** | 2.1 Cache Count Queries | Low | Medium | None |
| **2 (High Impact)** | 3.1 Parallel Count Queries in Procurement | Medium | High | None |
| **3** | 3.2 Parallel Data Queries in Procurement | Medium | High | None |
| **4** | 3.5 Parallel Queries in CombinedWorklist | Medium | Medium | None |
| **5** | 3.3, 3.4 Contract Management Parallel | Low | Low | None |
| **6** | 4.1 Database Indexes | Low | Medium | DBA access |
| **7 (Optional)** | 1.1, 1.2, 3.6 Full Parallel Sections | High | High | Architecture change |

---

## Important Notes

### DbContext Thread Safety
- EF Core DbContext **ไม่** thread-safe
- **ในกรณีที่ query ทั้งหมดใช้ DbContext เดียวกัน:** สามารถ parallel ได้ถ้าเป็น read-only queries (AsNoTracking)
- **ถ้าต้องการ full parallel:** ต้องใช้ `IDbContextFactory` สร้าง DbContext ใหม่สำหรับแต่ละ parallel task

### Connection Pool
- ถ้า parallel มากเกินไป อาจทำให้ connection pool หมด
- Default Max Pool Size = 100
- Monitor และปรับถ้าจำเป็น

### Error Handling
```csharp
try
{
    await Task.WhenAll(task1, task2, task3);
}
catch (Exception)
{
    if (task1.IsFaulted) logger.LogError(task1.Exception, "Task1 failed");
    if (task2.IsFaulted) logger.LogError(task2.Exception, "Task2 failed");
    throw;
}
```

---

## Files Reference

| File | Purpose |
|------|---------|
| `Backend/GHB.DP2.Application/Features/WorkList/GetList.cs` | Main endpoint handler |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/PlanProgram.cs` | Plans section |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/PlanAnnouncementProgram.cs` | Announcements section |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs` | Procurement section (CRITICAL) |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/ContractManagementProgram.cs` | Contract management |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/ContractAmendmentProgram.cs` | Contract amendments |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/ExpenseDisbursementProgram.cs` | Expense disbursement |
| `Backend/GHB.DP2.Application/Features/WorkList/Programs/CombinedWorklistBuilder.cs` | Combined view |
| `Backend/GHB.DP2.Application/Features/WorkList/AccessHelpers/ProcurementAccessHelper.cs` | Access control logic |

---

## Sign-off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Developer | | | |
| Code Reviewer | | | |
| QA | | | |
| Tech Lead | | | |

---

## Change Log

| Date | Version | Changes | By |
|------|---------|---------|-----|
| | 1.0 | Initial plan created | |
| | | | |
| | | | |
