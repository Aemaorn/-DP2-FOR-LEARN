# Worklist API Parallel Processing Solution

## Executive Summary

จากการวิเคราะห์พบว่า API `/api/worklist` ใช้เวลา **~60 วินาที** โดย **70%** ของเวลาอยู่ที่ Procurement queries
สาเหตุหลักคือการ execute queries แบบ **sequential** (ต่อเนื่อง) ทั้งที่สามารถทำ **parallel** ได้

**ผลลัพธ์ที่คาดหวังหลังการแก้ไข:** ลดเวลาลงจาก ~60 วินาที เหลือ ~15-20 วินาที (ลดลง 60-75%)

---

## 1. Components ที่สามารถแยกทำ Parallel ได้

### 1.1 Level 1: Main Section Processing (GetList.cs)

**Location:** `GetList.cs` Lines 311-316

**ปัจจุบัน (Sequential):**
```csharp
await this.planProgram.ProcessPlanSectionAsync(req, userContext, counts, sections, ct);
await this.planAnnouncementProgram.ProcessPlanAnnouncementSectionAsync(req, userContext, counts, sections, ct);
await this.procurementProgram.ProcessProcurementSectionsAsync(req, userContext, counts, sections, ct);
await this.contractManagementProgram.ProcessContractManagementSectionAsync(req, userContext, counts, sections, ct);
await this.contractAmendmentProgram.ProcessContractAmendmentSectionAsync(req, sections, ct);
await this.expenseDisbursementProgram.ProcessExpenseDisbursementSectionAsync(req, userContext, sections, counts, ct);
```

**ทำ Parallel ได้เพราะ:**
- แต่ละ section เป็นอิสระต่อกัน
- ไม่มี data dependency ระหว่าง sections
- ใช้ข้อมูล input เดียวกัน (`req`, `userContext`)

**Parallel Groups:**
| Group | Sections | เหตุผล |
|-------|----------|--------|
| A | Plan, PlanAnnouncement | ข้อมูลเกี่ยวกับแผน |
| B | Procurement (all steps) | ข้อมูลจัดซื้อจัดจ้าง |
| C | ContractManagement, ContractAmendment | ข้อมูลสัญญา |
| D | ExpenseDisbursement | ข้อมูลเบิกจ่าย |

---

### 1.2 Level 2: Procurement Count Queries (ProcurementProgram.cs)

**Location:** `ProcurementProgram.cs` Lines 57-61

**ปัจจุบัน (Sequential):**
```csharp
counts.PreProcurement = await baseQuery.Where(x => x.Step == ProcurementStep.PreProcurement).CountAsync(ct);
counts.Procurement = await baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct)
                   + await pw119Query.CountAsync(ct)
                   + await p79Clause2Query.CountAsync(ct)
                   + await pettyCashQuery.CountAsync(ct)
                   + await pettyCashReimbursementQuery.CountAsync(ct);
counts.ContractAgreement = await baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct)
                         + await pw119Query.CountAsync(ct)
                         + await p79Clause2Query.CountAsync(ct)
                         + await pettyCashQuery.CountAsync(ct)
                         + await pettyCashReimbursementQuery.CountAsync(ct);
```

**ทำ Parallel ได้เพราะ:**
- ทุก query เป็นอิสระต่อกัน
- ไม่มี side effects ระหว่าง queries

**รายละเอียด Query ที่ทำ Parallel ได้:**

| # | Query | เวลาโดยประมาณ |
|---|-------|---------------|
| 1 | Procurement (PreProcurement) Count | 10,021ms |
| 2 | Procurement (Procurement) Count | 10,390ms |
| 3 | Procurement (ContractAgreement) Count | 9,608ms |
| 4 | Pw119 Count | 2ms |
| 5 | P79Clause2 Count | 2ms |
| 6 | PPettyCash Count | 3ms |
| 7 | PPettyCashReimbursement Count | 2ms |

**หมายเหตุ:** Queries 4-7 ถูก execute **ซ้ำ** 2 ครั้ง (สำหรับ Procurement และ ContractAgreement) - สามารถ cache ได้

---

### 1.3 Level 3: Procurement Data Queries (ProcurementProgram.cs)

**Location:** `ProcurementProgram.cs` Lines 149-153

**ปัจจุบัน (Sequential):**
```csharp
var procurementRecords = await procurementQueryWithIncludes.ToListAsync(ct);
var pw119Records = await pw119QueryWithIncludes.ToListAsync(ct);
var p79Clause2Records = await p79Clause2QueryWithIncludes.ToListAsync(ct);
var pettyCashRecords = await pettyCashQueryWithIncludes.ToListAsync(ct);
var pettyCashReimbursementRecords = await pettyCashReimbursementQueryWithIncludes.ToListAsync(ct);
```

**ทำ Parallel ได้เพราะ:**
- ทุก query ดึงข้อมูลจากตารางแยกกัน
- ไม่มี dependency ระหว่าง queries
- ผลลัพธ์ถูก merge ทีหลังใน memory

---

### 1.4 Level 4: Contract Management Queries (ContractManagementProgram.cs)

**Location:** `ContractManagementProgram.cs` Lines 61-62, 84-114

**Count Queries (ปัจจุบัน Sequential):**
```csharp
counts.ContractManagement = await deliveryAcceptancePeriodQuery.CountAsync(ct)
                          + await disbursementApprovalQuery.CountAsync(ct)
                          + await contractTerminationQuery.CountAsync(ct)
                          + await contractGuaranteeReturnQuery.CountAsync(ct);
```

**Data Queries (ปัจจุบัน Sequential):**
```csharp
var deliveryAcceptanceItems = await deliveryAcceptancePeriodQuery.Select(...).ToListAsync(ct);
var disbursementApprovalItems = await disbursementApprovalsQuery.Select(...).ToListAsync(ct);
var contractTerminationItems = await contractTerminationQuery.Select(...).ToListAsync(ct);
var contractGuaranteeReturnItems = await contractGuaranteeReturnQuery.Select(...).ToListAsync(ct);
```

**ทำ Parallel ได้เพราะ:**
- ทั้ง 4 queries เป็นอิสระต่อกัน
- ดึงข้อมูลจากตารางแยกกัน

---

### 1.5 Level 5: Combined Worklist Builder (CombinedWorklistBuilder.cs)

**Location:** `CombinedWorklistBuilder.cs` Lines 330-340

**ปัจจุบัน (Sequential แม้ว่าชื่อตัวแปรจะเป็น "Task"):**
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

**ทำ Parallel ได้เพราะ:**
- ทั้ง 11 queries เป็นอิสระต่อกัน
- ไม่มี dependency ระหว่าง queries

---

## 2. แนวทางการ Implement Parallel Processing

### 2.1 ข้อจำกัดของ EF Core DbContext

**ปัญหา:** EF Core `DbContext` ไม่ thread-safe - ไม่สามารถใช้ DbContext เดียวกันใน parallel queries ได้

**วิธีแก้ไข:**

#### Option A: ใช้ IDbContextFactory (แนะนำ)

```csharp
// 1. Register ใน DI
services.AddDbContextFactory<Dp2DbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Inject ใน Constructor
public class GetWorklistSeparated : EndpointBase<...>
{
    private readonly IDbContextFactory<Dp2DbContext> dbContextFactory;

    public GetWorklistSeparated(IDbContextFactory<Dp2DbContext> dbContextFactory, ...)
    {
        this.dbContextFactory = dbContextFactory;
    }
}

// 3. สร้าง DbContext ใหม่สำหรับแต่ละ parallel task
await using var dbContext1 = await this.dbContextFactory.CreateDbContextAsync(ct);
await using var dbContext2 = await this.dbContextFactory.CreateDbContextAsync(ct);
```

#### Option B: ใช้ Separate Scopes

```csharp
// ใช้ IServiceScopeFactory
var tasks = new List<Task>();
using var scope1 = serviceScopeFactory.CreateScope();
using var scope2 = serviceScopeFactory.CreateScope();
var db1 = scope1.ServiceProvider.GetRequiredService<Dp2DbContext>();
var db2 = scope2.ServiceProvider.GetRequiredService<Dp2DbContext>();
```

---

### 2.2 Implementation สำหรับแต่ละ Level

#### Level 1: Main Section Processing

**File:** `GetList.cs`

```csharp
protected override async ValueTask<Ok<GetWorklistSeparatedResponse>> HandleRequestAsync(
    GetWorklistSeparatedRequest req,
    CancellationToken ct)
{
    var userId = UserId.From(req.UserId);
    var userContext = await this.GetUserContextAsync(userId, ct);

    if (userContext is null) return null!;

    var counts = new WorklistCountsAccumulator();
    var sections = new WorklistSections();

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

    var procurementTask = Task.Run(async () =>
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(ct);
        var program = new ProcurementProgram(db);
        return await program.ProcessProcurementSectionsAsync(req, userContext, ct);
    }, ct);

    var contractManagementTask = Task.Run(async () =>
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(ct);
        var program = new ContractManagementProgram(db);
        return await program.ProcessContractManagementSectionAsync(req, userContext, ct);
    }, ct);

    var contractAmendmentTask = Task.Run(async () =>
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(ct);
        var program = new ContractAmendmentProgram(db);
        return await program.ProcessContractAmendmentSectionAsync(req, ct);
    }, ct);

    var expenseDisbursementTask = Task.Run(async () =>
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(ct);
        var program = new ExpenseDisbursementProgram(db);
        return await program.ProcessExpenseDisbursementSectionAsync(req, userContext, ct);
    }, ct);

    // รอทุก tasks เสร็จพร้อมกัน
    await Task.WhenAll(
        planTask,
        announcementTask,
        procurementTask,
        contractManagementTask,
        contractAmendmentTask,
        expenseDisbursementTask);

    // Merge results
    var planResult = await planTask;
    var announcementResult = await announcementTask;
    // ... merge ผลลัพธ์อื่นๆ
}
```

---

#### Level 2 & 3: Procurement Parallel Queries

**File:** `ProcurementProgram.cs`

```csharp
public async Task<ProcurementSectionResult> ProcessProcurementSectionsAsync(
    GetWorklistSeparatedRequest req,
    UserContext userContext,
    CancellationToken ct)
{
    var baseQuery = this.BuildProcurementBaseQuery(req, userContext);
    var pw119Query = this.BuildPw119BaseQuery(req, userContext);
    var p79Clause2Query = this.BuildP79Clause2BaseQuery(req, userContext);
    var pettyCashQuery = this.BuildPPettyCashBaseQuery(req, userContext);
    var pettyCashReimbursementQuery = this.BuildPPettyCashReimbursementBaseQuery(req, userContext);

    // ===== PARALLEL COUNT QUERIES =====
    var preProcCountTask = baseQuery.Where(x => x.Step == ProcurementStep.PreProcurement).CountAsync(ct);
    var procCountTask = baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct);
    var contractCountTask = baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct);
    var pw119CountTask = pw119Query.CountAsync(ct);
    var p79CountTask = p79Clause2Query.CountAsync(ct);
    var pettyCashCountTask = pettyCashQuery.CountAsync(ct);
    var pettyCashReimbursementCountTask = pettyCashReimbursementQuery.CountAsync(ct);

    await Task.WhenAll(
        preProcCountTask, procCountTask, contractCountTask,
        pw119CountTask, p79CountTask, pettyCashCountTask, pettyCashReimbursementCountTask);

    var preProcCount = await preProcCountTask;
    var procCount = await procCountTask;
    var contractCount = await contractCountTask;
    var pw119Count = await pw119CountTask;          // Cache นี้!
    var p79Count = await p79CountTask;              // Cache นี้!
    var pettyCashCount = await pettyCashCountTask;  // Cache นี้!
    var pettyCashReimbursementCount = await pettyCashReimbursementCountTask;  // Cache นี้!

    var counts = new ProcurementCounts
    {
        PreProcurement = preProcCount,
        Procurement = procCount + pw119Count + p79Count + pettyCashCount + pettyCashReimbursementCount,
        ContractAgreement = contractCount + pw119Count + p79Count + pettyCashCount + pettyCashReimbursementCount
    };

    // ... ส่วนที่เหลือ
}
```

---

#### Level 3: Procurement Data Parallel Queries

```csharp
private static async Task<SectionResult<ProcurementItem>> CreateProcurementWithPw119AndP79Clause2AndPettyCashSectionAsync(
    IQueryable<Procurement> procurementQuery,
    IQueryable<Pw119> pw119Query,
    IQueryable<P79Clause2> p79Clause2Query,
    IQueryable<PPettyCash> pettyCashQuery,
    IQueryable<PPettyCashReimbursement> pettyCashReimbursementQuery,
    GetWorklistSeparatedRequest req,
    CancellationToken ct)
{
    // ... setup queries ...

    // ===== PARALLEL DATA QUERIES =====
    var procurementTask = procurementQueryWithIncludes.ToListAsync(ct);
    var pw119Task = pw119QueryWithIncludes.ToListAsync(ct);
    var p79Clause2Task = p79Clause2QueryWithIncludes.ToListAsync(ct);
    var pettyCashTask = pettyCashQueryWithIncludes.ToListAsync(ct);
    var pettyCashReimbursementTask = pettyCashReimbursementQueryWithIncludes.ToListAsync(ct);

    await Task.WhenAll(procurementTask, pw119Task, p79Clause2Task, pettyCashTask, pettyCashReimbursementTask);

    var procurementRecords = await procurementTask;
    var pw119Records = await pw119Task;
    var p79Clause2Records = await p79Clause2Task;
    var pettyCashRecords = await pettyCashTask;
    var pettyCashReimbursementRecords = await pettyCashReimbursementTask;

    // ... merge and paginate ...
}
```

---

#### Level 4: Contract Management Parallel Queries

**File:** `ContractManagementProgram.cs`

```csharp
public async Task ProcessContractManagementSectionAsync(
    GetWorklistSeparatedRequest req,
    UserContext userContext,
    WorklistCountsAccumulator counts,
    WorklistSections sections,
    CancellationToken ct)
{
    var deliveryAcceptancePeriodQuery = this.BuildDeliveryAcceptanceBaseQuery(req, userContext);
    var disbursementApprovalQuery = this.BuildDisbursementApprovalBaseQuery(req, userContext);
    var contractTerminationQuery = this.BuildContractTerminationBaseQuery(req, userContext);
    var contractGuaranteeReturnQuery = this.BuildContractGuaranteeReturnBaseQuery(req, userContext);

    // ===== PARALLEL COUNT QUERIES =====
    var deliveryCountTask = deliveryAcceptancePeriodQuery.CountAsync(ct);
    var disbursementCountTask = disbursementApprovalQuery.CountAsync(ct);
    var terminationCountTask = contractTerminationQuery.CountAsync(ct);
    var guaranteeCountTask = contractGuaranteeReturnQuery.CountAsync(ct);

    await Task.WhenAll(deliveryCountTask, disbursementCountTask, terminationCountTask, guaranteeCountTask);

    counts.ContractManagement = await deliveryCountTask
                              + await disbursementCountTask
                              + await terminationCountTask
                              + await guaranteeCountTask;

    // ... ส่วนที่เหลือ
}

private static async Task<SectionResult<ContractManagementItem>> CreateSectionResultAsync(...)
{
    // ===== PARALLEL DATA QUERIES =====
    var deliveryTask = deliveryAcceptancePeriodQuery.Select(...).ToListAsync(ct);
    var disbursementTask = disbursementApprovalsQuery.Select(...).ToListAsync(ct);
    var terminationTask = contractTerminationQuery.Select(...).ToListAsync(ct);
    var guaranteeTask = contractGuaranteeReturnQuery.Select(...).ToListAsync(ct);

    await Task.WhenAll(deliveryTask, disbursementTask, terminationTask, guaranteeTask);

    var deliveryAcceptanceItems = await deliveryTask;
    var disbursementApprovalItems = await disbursementTask;
    var contractTerminationItems = await terminationTask;
    var contractGuaranteeReturnItems = await guaranteeTask;

    // ... merge and paginate ...
}
```

---

#### Level 5: Combined Worklist Builder Parallel Queries

**File:** `CombinedWorklistBuilder.cs`

```csharp
public async Task<PaginatedQueryResult<CombinedWorklistItem>?> BuildCombinedWorklistAsync(
    GetWorklistSeparatedRequest req,
    UserContext userContext,
    CancellationToken ct)
{
    // ... setup queries ...

    // ===== PARALLEL DATA QUERIES =====
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

    await Task.WhenAll(
        planTask, announcementTask, procurementTask,
        pw119Task, p79Clause2Task, pettyCashTask, pettyCashReimbursementTask,
        deliveryAcceptanceTask, disbursementApprovalTask,
        contractTerminationTask, contractGuaranteeReturnTask);

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
        .Concat(expenseDisbursementItems)
        .OrderByDescending(x => x.SortAt ?? DateTimeOffset.MinValue)
        .ThenByDescending(x => x.Number)
        .ToList();

    // ... paginate ...
}
```

---

## 3. สิ่งที่ต้องทำ (Implementation Checklist)

### Phase 1: Infrastructure Setup

- [ ] **3.1** Register `IDbContextFactory<Dp2DbContext>` ใน DI Container
  - File: `Program.cs` หรือ `Startup.cs`
  ```csharp
  services.AddDbContextFactory<Dp2DbContext>(options =>
      options.UseNpgsql(connectionString));
  ```

- [ ] **3.2** ปรับ constructor ของ `GetWorklistSeparated` ให้รับ `IDbContextFactory`
  - File: `GetList.cs`

- [ ] **3.3** ปรับ constructor ของแต่ละ Program class ให้รับ `IDbContextFactory` หรือ `Dp2DbContext`

### Phase 2: Implement Parallel Processing

- [ ] **3.4** แก้ไข `GetList.cs` - Level 1 Main Sections Parallel
  - ใช้ `Task.WhenAll()` สำหรับ 6 sections

- [ ] **3.5** แก้ไข `ProcurementProgram.cs` - Level 2 Count Queries
  - ใช้ `Task.WhenAll()` สำหรับ 7 count queries
  - Cache ค่า count ของ Pw119, P79Clause2, PPettyCash, PPettyCashReimbursement

- [ ] **3.6** แก้ไข `ProcurementProgram.cs` - Level 3 Data Queries
  - ใช้ `Task.WhenAll()` สำหรับ 5 data queries

- [ ] **3.7** แก้ไข `ContractManagementProgram.cs` - Level 4 Queries
  - ใช้ `Task.WhenAll()` สำหรับ count queries
  - ใช้ `Task.WhenAll()` สำหรับ data queries

- [ ] **3.8** แก้ไข `CombinedWorklistBuilder.cs` - Level 5 Combined Queries
  - ใช้ `Task.WhenAll()` สำหรับ 11 queries

### Phase 3: Testing & Optimization

- [ ] **3.9** เพิ่ม Unit Tests สำหรับ parallel operations
- [ ] **3.10** ทดสอบ performance ก่อน/หลัง
- [ ] **3.11** Monitor connection pool usage
- [ ] **3.12** เพิ่ม logging สำหรับ query execution times

---

## 4. ข้อควรระวัง

### 4.1 DbContext Thread Safety
- **ปัญหา:** EF Core DbContext ไม่ thread-safe
- **วิธีแก้:** ใช้ `IDbContextFactory` สร้าง DbContext ใหม่สำหรับแต่ละ parallel task
- **ข้อยกเว้น:** ถ้า queries อยู่ใน method เดียวกันและใช้ **DbContext เดียวกัน** แต่ไม่มี state changes ก็สามารถ parallel ได้ (read-only queries)

### 4.2 Connection Pool Exhaustion
- **ปัญหา:** การทำ parallel มากเกินไปอาจทำให้ connection pool หมด
- **วิธีแก้:**
  - จำกัดจำนวน parallel queries (ใช้ `SemaphoreSlim`)
  - เพิ่มขนาด connection pool
  ```csharp
  "Server=...;Max Pool Size=100;..."
  ```

### 4.3 Memory Usage
- **ปัญหา:** Parallel queries load data เข้า memory พร้อมกัน
- **วิธีแก้:** Monitor memory usage และปรับ batch size ถ้าจำเป็น

### 4.4 Error Handling
- **ปัญหา:** ถ้า query หนึ่งใน parallel group fail, `Task.WhenAll` จะ throw exception
- **วิธีแก้:** ใช้ `Task.WhenAll` ร่วมกับ try-catch หรือใช้ pattern ที่ collect errors

```csharp
try
{
    await Task.WhenAll(task1, task2, task3);
}
catch (Exception)
{
    // Handle individual task exceptions
    if (task1.IsFaulted) logger.LogError(task1.Exception, "Task1 failed");
    if (task2.IsFaulted) logger.LogError(task2.Exception, "Task2 failed");
    throw;
}
```

---

## 5. Performance Improvement Estimates

| Level | Component | Current Time | After Parallel | Improvement |
|-------|-----------|--------------|----------------|-------------|
| 1 | Main Sections | ~60s (sequential) | ~15s (max of all) | 75% |
| 2 | Procurement Counts | ~30s | ~10s (max query) | 67% |
| 3 | Procurement Data | ~12s | ~3s | 75% |
| 4 | Contract Management | ~1s | ~0.3s | 70% |
| 5 | Combined Worklist | ~15s | ~5s | 67% |

**Overall Expected Improvement:** จาก ~60 วินาที เหลือ ~15-20 วินาที (**60-75% faster**)

---

## 6. Architecture Diagram

```
                        GetWorklistSeparated
                        (Main Handler)
                               |
                               v
                     GetUserContextAsync (Sequential)
                     ต้องทำก่อน - เป็น dependency ของทุก sections
                               |
                               v
+-----------------------------------------------------------------------------+
|                     LEVEL 1: Parallel Section Processing                     |
|  +-----------------------------------------------------------------------+  |
|  |  Task.WhenAll()                                                       |  |
|  |  |-- PlanProgram.ProcessPlanSectionAsync                              |  |
|  |  |-- PlanAnnouncementProgram.ProcessPlanAnnouncementSectionAsync      |  |
|  |  |-- ProcurementProgram.ProcessProcurementSectionsAsync -----------+  |  |
|  |  |-- ContractManagementProgram.ProcessContractManagementSection ---+  |  |
|  |  |-- ContractAmendmentProgram.ProcessContractAmendmentSection      |  |  |
|  |  +-- ExpenseDisbursementProgram.ProcessExpenseDisbursementSection  |  |  |
|  +-----------------------------------------------------------------------+  |
+-----------------------------------------------------------------------------+
                               |
              +----------------+----------------+
              |                |                |
              v                v                v
+---------------------+ +---------------------+ +---------------------+
| LEVEL 2-3:          | | LEVEL 4:            | | LEVEL 5:            |
| Procurement Parallel| | ContractMgmt        | | CombinedWorklist    |
|                     | | Parallel            | | Parallel            |
| Count Queries:      | |                     | |                     |
| |-- PreProc Count   | | |-- Delivery Count  | | |-- Plan Query      |
| |-- Proc Count      | | |-- Disbursement    | | |-- Announcement    |
| |-- Contract Count  | | |-- Termination     | | |-- Procurement     |
| |-- Pw119 Count     | | +-- Guarantee       | | |-- Pw119           |
| |-- P79 Count       | |                     | | |-- P79Clause2      |
| |-- PettyCash       | | Data Queries:       | | |-- PettyCash       |
| +-- Reimbursement   | | |-- Delivery Data   | | |-- Reimbursement   |
|                     | | |-- Disbursement    | | |-- Delivery        |
| Data Queries:       | | |-- Termination     | | |-- Disbursement    |
| |-- Procurement     | | +-- Guarantee       | | |-- Termination     |
| |-- Pw119           | |                     | | |-- Guarantee       |
| |-- P79Clause2      | |                     | | +-- ExpenseDisb     |
| |-- PettyCash       | |                     | |                     |
| +-- Reimbursement   | |                     | |                     |
+---------------------+ +---------------------+ +---------------------+
              |                |                |
              +----------------+----------------+
                               |
                               v
                     Merge Results & Return Response
```

---

## 7. Summary

| หัวข้อ | รายละเอียด |
|--------|------------|
| **ปัญหาหลัก** | Sequential query execution |
| **Components ที่ parallel ได้** | 5 Levels, 30+ queries |
| **เทคนิคที่ใช้** | `Task.WhenAll()`, `IDbContextFactory` |
| **Expected Improvement** | 60-75% faster (60s -> 15-20s) |
| **ข้อควรระวังหลัก** | DbContext thread safety, Connection pool |
| **Files ที่ต้องแก้ไข** | 5 files (GetList.cs, ProcurementProgram.cs, ContractManagementProgram.cs, CombinedWorklistBuilder.cs, Program.cs/Startup.cs) |

---

## 8. Quick Start: ลำดับการ Implement

**แนะนำให้เริ่มจาก Level ที่มี impact สูงสุดก่อน:**

1. **Level 2-3 (ProcurementProgram.cs)** - แก้ไขง่าย, impact สูง (~30s saved)
2. **Level 5 (CombinedWorklistBuilder.cs)** - แก้ไขง่าย, impact ปานกลาง (~10s saved)
3. **Level 4 (ContractManagementProgram.cs)** - แก้ไขง่าย, impact ต่ำ (~0.7s saved)
4. **Level 1 (GetList.cs)** - ต้อง setup infrastructure ก่อน แต่ให้ผลดีที่สุด

**หมายเหตุ:** Level 2-5 สามารถทำได้โดยไม่ต้อง setup `IDbContextFactory` ถ้า queries ทั้งหมดใช้ DbContext เดียวกันและเป็น read-only (AsNoTracking)

---

## 9. Duplicate Queries ที่เรียกซ้ำ (Cache Optimization)

นอกจากการทำ Parallel แล้ว ยังพบว่ามี **queries ที่ถูกเรียกซ้ำหลายครั้ง** ซึ่งสามารถเรียกครั้งเดียวแล้ว cache ผลลัพธ์ไว้ใช้ได้

### 9.1 Duplicate Count Queries ใน ProcurementProgram.cs

**Location:** `ProcurementProgram.cs` Lines 57-61

**ปัญหา:** Queries เหล่านี้ถูกเรียก **2 ครั้ง**:

```csharp
// ครั้งที่ 1: สำหรับ counts.Procurement
counts.Procurement = await baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct)
                   + await pw119Query.CountAsync(ct)           // <-- เรียกครั้งที่ 1
                   + await p79Clause2Query.CountAsync(ct)      // <-- เรียกครั้งที่ 1
                   + await pettyCashQuery.CountAsync(ct)       // <-- เรียกครั้งที่ 1
                   + await pettyCashReimbursementQuery.CountAsync(ct);  // <-- เรียกครั้งที่ 1

// ครั้งที่ 2: สำหรับ counts.ContractAgreement (เรียกซ้ำอีก!)
counts.ContractAgreement = await baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct)
                         + await pw119Query.CountAsync(ct)           // <-- เรียกครั้งที่ 2 ซ้ำ!
                         + await p79Clause2Query.CountAsync(ct)      // <-- เรียกครั้งที่ 2 ซ้ำ!
                         + await pettyCashQuery.CountAsync(ct)       // <-- เรียกครั้งที่ 2 ซ้ำ!
                         + await pettyCashReimbursementQuery.CountAsync(ct);  // <-- เรียกครั้งที่ 2 ซ้ำ!
```

**ผลกระทบ:** 4 queries x 2 ครั้ง = **8 queries ที่ไม่จำเป็น** (ประหยัดได้ 4 queries)

**วิธีแก้ไข:**

```csharp
public async Task ProcessProcurementSectionsAsync(...)
{
    // ... build queries ...

    // ===== เรียก count ครั้งเดียว แล้ว cache ไว้ =====
    var pw119Count = await pw119Query.CountAsync(ct);
    var p79Count = await p79Clause2Query.CountAsync(ct);
    var pettyCashCount = await pettyCashQuery.CountAsync(ct);
    var pettyCashReimbursementCount = await pettyCashReimbursementQuery.CountAsync(ct);

    // ===== ใช้ค่าที่ cache ไว้ =====
    counts.PreProcurement = await baseQuery.Where(x => x.Step == ProcurementStep.PreProcurement).CountAsync(ct);

    counts.Procurement = await baseQuery.Where(x => x.Step == ProcurementStep.Procurement).CountAsync(ct)
                       + pw119Count           // ใช้ค่าที่ cache
                       + p79Count             // ใช้ค่าที่ cache
                       + pettyCashCount       // ใช้ค่าที่ cache
                       + pettyCashReimbursementCount;  // ใช้ค่าที่ cache

    counts.ContractAgreement = await baseQuery.Where(x => x.Step == ProcurementStep.ContractAgreement).CountAsync(ct)
                             + pw119Count           // ใช้ค่าที่ cache (ไม่ต้องเรียกซ้ำ!)
                             + p79Count             // ใช้ค่าที่ cache
                             + pettyCashCount       // ใช้ค่าที่ cache
                             + pettyCashReimbursementCount;  // ใช้ค่าที่ cache
}
```

---

### 9.2 Duplicate Queries ระหว่าง Section Processing และ CombinedWorklistBuilder

**ปัญหา:** เมื่อ `IncludeAll=true` จะมีการเรียก queries ซ้ำกัน:

| Query | เรียกใน Section | เรียกใน CombinedWorklistBuilder | รวม |
|-------|-----------------|--------------------------------|-----|
| Plan | PlanProgram | CombinedWorklistBuilder | 2x |
| PlanAnnouncement | PlanAnnouncementProgram | CombinedWorklistBuilder | 2x |
| Procurement | ProcurementProgram | CombinedWorklistBuilder | 2x |
| Pw119 | ProcurementProgram | CombinedWorklistBuilder | 2x |
| P79Clause2 | ProcurementProgram | CombinedWorklistBuilder | 2x |
| PPettyCash | ProcurementProgram | CombinedWorklistBuilder | 2x |
| PPettyCashReimbursement | ProcurementProgram | CombinedWorklistBuilder | 2x |
| DeliveryAcceptance | ContractManagementProgram | CombinedWorklistBuilder | 2x |
| DisbursementApproval | ContractManagementProgram | CombinedWorklistBuilder | 2x |
| ContractTermination | ContractManagementProgram | CombinedWorklistBuilder | 2x |
| ContractGuaranteeReturn | ContractManagementProgram | CombinedWorklistBuilder | 2x |
| ExpenseDisbursement | ExpenseDisbursementProgram | CombinedWorklistBuilder | 2x |

**ผลกระทบ:** **12 queries ถูกเรียกซ้ำ 2 ครั้ง** เมื่อ `IncludeAll=true`

**วิธีแก้ไข:** ใช้ Shared Result Pattern

```csharp
// สร้าง class เก็บ cached results
public class WorklistQueryResults
{
    public List<Plan>? Plans { get; set; }
    public int? PlanCount { get; set; }

    public List<PlanAnnouncement>? Announcements { get; set; }
    public int? AnnouncementCount { get; set; }

    public List<Procurement>? Procurements { get; set; }
    public int? ProcurementPreProcCount { get; set; }
    public int? ProcurementProcCount { get; set; }
    public int? ProcurementContractCount { get; set; }

    public List<Pw119>? Pw119s { get; set; }
    public int? Pw119Count { get; set; }

    // ... เพิ่มสำหรับ entities อื่นๆ
}

// ใน GetWorklistSeparated
protected override async ValueTask<Ok<GetWorklistSeparatedResponse>> HandleRequestAsync(...)
{
    var cachedResults = new WorklistQueryResults();

    // ถ้า IncludeAll=true ให้ดึงข้อมูลครั้งเดียว แล้วใช้ทั้ง Section และ Combined
    if (req.IncludeAll)
    {
        // ดึงข้อมูลทั้งหมดครั้งเดียว
        cachedResults.Plans = await planQuery.ToListAsync(ct);
        cachedResults.PlanCount = cachedResults.Plans.Count;

        cachedResults.Pw119s = await pw119Query.ToListAsync(ct);
        cachedResults.Pw119Count = cachedResults.Pw119s.Count;

        // ... ดึง entities อื่นๆ
    }

    // ส่ง cachedResults ไปให้แต่ละ program ใช้
    await this.planProgram.ProcessPlanSectionAsync(req, userContext, counts, sections, cachedResults, ct);
    // ...

    // CombinedWorklistBuilder ก็ใช้ cachedResults เดียวกัน
    var combined = await this.combinedWorklistBuilder.BuildCombinedWorklistAsync(req, userContext, cachedResults, ct);
}
```

---

### 9.3 Duplicate Data Fetch ใน ExpenseDisbursement

**Location:** `CombinedWorklistBuilder.cs` Lines 267-304

**ปัญหา:** ExpenseDisbursement ต้องดึงข้อมูลจาก source tables หลายตัว:

```csharp
// ดึง ExpenseDisbursement entities
var expenseDisbursementEntities = await this.expenseDisbursementProgram
    .BuildExpenseDisbursementBaseQuery(req, userContext, departmentSourceIds)
    .ToListAsync(ct);

// แยก IDs ตาม SourceType
var w119Ids = new HashSet<Guid>();
var clause79Ids = new HashSet<Guid>();
// ...

// ดึง source data อีกครั้ง!
var sourceLookups = await this.expenseDisbursementProgram.BuildSourceLookupsAsync(
    w119Ids, clause79Ids, disbursementIds, guaranteeReturnIds, pettyCashReimbursementIds, ct);
```

**ปัญหาคือ:** ข้อมูล W119, P79Clause2, DisbursementApproval, ContractGuaranteeReturn ถูกดึงไปแล้วใน queries ก่อนหน้า แต่ยังต้องดึงอีกครั้งสำหรับ ExpenseDisbursement

**วิธีแก้ไข:**

```csharp
// ใช้ข้อมูลที่ดึงไปแล้วจาก cached results
public async Task<PaginatedQueryResult<CombinedWorklistItem>?> BuildCombinedWorklistAsync(
    GetWorklistSeparatedRequest req,
    UserContext userContext,
    WorklistQueryResults cachedResults,  // รับ cached results
    CancellationToken ct)
{
    // ...

    // สร้าง lookup จาก cached data แทนการ query ใหม่
    var sourceLookups = new SourceLookups
    {
        W119Lookup = cachedResults.Pw119s?.ToDictionary(x => x.Id.Value),
        Clause79Lookup = cachedResults.P79Clause2s?.ToDictionary(x => x.Id.Value),
        DisbursementLookup = cachedResults.DisbursementApprovals?.ToDictionary(x => x.Id.Value),
        // ...
    };
}
```

---

### 9.4 Summary: Duplicate Queries ที่พบ

| # | Location | Query | จำนวนครั้งที่เรียก | ควรเรียก | ประหยัดได้ |
|---|----------|-------|-------------------|----------|-----------|
| 1 | ProcurementProgram | Pw119 Count | 2 | 1 | 1 query |
| 2 | ProcurementProgram | P79Clause2 Count | 2 | 1 | 1 query |
| 3 | ProcurementProgram | PPettyCash Count | 2 | 1 | 1 query |
| 4 | ProcurementProgram | PPettyCashReimbursement Count | 2 | 1 | 1 query |
| 5 | Section + Combined | Plan Query | 2 | 1 | 1 query |
| 6 | Section + Combined | Announcement Query | 2 | 1 | 1 query |
| 7 | Section + Combined | Procurement Query | 2 | 1 | 1 query |
| 8 | Section + Combined | Pw119 Query | 2 | 1 | 1 query |
| 9 | Section + Combined | P79Clause2 Query | 2 | 1 | 1 query |
| 10 | Section + Combined | PPettyCash Query | 2 | 1 | 1 query |
| 11 | Section + Combined | PPettyCashReimbursement Query | 2 | 1 | 1 query |
| 12 | Section + Combined | DeliveryAcceptance Query | 2 | 1 | 1 query |
| 13 | Section + Combined | DisbursementApproval Query | 2 | 1 | 1 query |
| 14 | Section + Combined | ContractTermination Query | 2 | 1 | 1 query |
| 15 | Section + Combined | ContractGuaranteeReturn Query | 2 | 1 | 1 query |
| 16 | Section + Combined | ExpenseDisbursement Query | 2 | 1 | 1 query |
| 17 | ExpenseDisbursement | Source Lookups (W119, P79, etc.) | 1-5 | 0 (ใช้ cached) | 1-5 queries |

**รวม:** ประหยัดได้ **16-21 queries** จากการ cache

---

### 9.5 Implementation Priority สำหรับ Cache Optimization

| Priority | Item | Effort | Impact | Files |
|----------|------|--------|--------|-------|
| **1** | Cache Pw119/P79/PettyCash counts | Low | Medium | ProcurementProgram.cs |
| **2** | Share results ระหว่าง Sections และ Combined | Medium | High | GetList.cs, CombinedWorklistBuilder.cs |
| **3** | Reuse source lookups ใน ExpenseDisbursement | Medium | Medium | ExpenseDisbursementProgram.cs, CombinedWorklistBuilder.cs |

---

### 9.6 Complete Cache Implementation Example

```csharp
// Step 1: สร้าง shared cache class
public class WorklistCache
{
    // Counts (เรียกครั้งเดียว)
    public int Pw119Count { get; set; }
    public int P79Clause2Count { get; set; }
    public int PettyCashCount { get; set; }
    public int PettyCashReimbursementCount { get; set; }

    // Data (เรียกครั้งเดียว ถ้า IncludeAll=true)
    public List<CombinedWorklistItem>? PlanItems { get; set; }
    public List<CombinedWorklistItem>? AnnouncementItems { get; set; }
    public List<CombinedWorklistItem>? ProcurementItems { get; set; }
    public List<CombinedWorklistItem>? Pw119Items { get; set; }
    public List<CombinedWorklistItem>? P79Clause2Items { get; set; }
    public List<CombinedWorklistItem>? PettyCashItems { get; set; }
    public List<CombinedWorklistItem>? PettyCashReimbursementItems { get; set; }
    public List<CombinedWorklistItem>? DeliveryAcceptanceItems { get; set; }
    public List<CombinedWorklistItem>? DisbursementApprovalItems { get; set; }
    public List<CombinedWorklistItem>? ContractTerminationItems { get; set; }
    public List<CombinedWorklistItem>? ContractGuaranteeReturnItems { get; set; }
    public List<CombinedWorklistItem>? ExpenseDisbursementItems { get; set; }
}

// Step 2: ใน GetWorklistSeparated
protected override async ValueTask<Ok<GetWorklistSeparatedResponse>> HandleRequestAsync(...)
{
    var cache = new WorklistCache();

    // ดึง counts ครั้งเดียว (parallel)
    var pw119CountTask = pw119Query.CountAsync(ct);
    var p79CountTask = p79Clause2Query.CountAsync(ct);
    var pettyCashCountTask = pettyCashQuery.CountAsync(ct);
    var pettyCashReimbursementCountTask = pettyCashReimbursementQuery.CountAsync(ct);

    await Task.WhenAll(pw119CountTask, p79CountTask, pettyCashCountTask, pettyCashReimbursementCountTask);

    cache.Pw119Count = await pw119CountTask;
    cache.P79Clause2Count = await p79CountTask;
    cache.PettyCashCount = await pettyCashCountTask;
    cache.PettyCashReimbursementCount = await pettyCashReimbursementCountTask;

    // ถ้า IncludeAll=true ดึงข้อมูลครั้งเดียวแล้วใช้ทั้ง Sections และ Combined
    if (req.IncludeAll)
    {
        // ดึง data ทั้งหมด (parallel)
        // ... แล้วเก็บใน cache
    }

    // ส่ง cache ไปให้แต่ละ program
    await this.procurementProgram.ProcessProcurementSectionsAsync(req, userContext, counts, sections, cache, ct);

    // Combined ก็ใช้ cache เดียวกัน
    var combined = await this.combinedWorklistBuilder.BuildCombinedWorklistAsync(req, userContext, cache, ct);
}
```

---

## 10. Combined Optimization Summary

| Optimization Type | Queries Saved | Time Saved |
|------------------|---------------|------------|
| **Parallel Processing** | N/A (same queries, faster) | ~40-45s |
| **Cache Duplicate Counts** | 4 queries | ~0.02s |
| **Cache Duplicate Data (IncludeAll)** | 12 queries | ~5-10s |
| **Reuse Source Lookups** | 1-5 queries | ~1-2s |
| **TOTAL** | 17-21 queries | **~45-57s** |

**Final Expected Performance:** จาก ~60 วินาที เหลือ ~5-15 วินาที (**75-90% faster**)
