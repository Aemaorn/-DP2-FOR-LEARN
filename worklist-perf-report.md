# Worklist Performance Optimization Report

**Date**: 2026-05-23
**Branch**: `improve/worklist-perf`
**Commits**: `1dc7f41fd`, `496778d2a`, `69921aacf` (+ pending pre-compute IDs)

## TL;DR

| Endpoint | Baseline | After All Changes | Improvement |
|----------|---------|-------------------|-------------|
| `/api/worklist?includeProcurement=true` (uncached) | ~5–8s | **~480-626ms** | ~10× faster |
| `/api/worklist?includeProcurement=true` (cached) | n/a | ~25ms | cache hit |
| `/api/worklist?includePreProcurement=true` (uncached) | 2.82s | **~1.04s** | ~2.7× faster |
| `/api/worklist?includeContractAgreement=true` (warm-app cold-cache) | ~1.37s | **~1.1s** (median of 4) | ~20% faster after projection refactor |
| `/api/worklist?includeContractManagement=true` (warm) | ~908ms | **~506ms** | 44% faster |
| `/api/worklist?includeContractAmendment=true` (uncached) | ~1.46s | **~778ms** | 47% faster |
| `/api/worklist?includeAll=true` (uncached) | 1.0–2.2s | **~699ms** | ~3× faster |

## Cross-Reference: Prior Plan in `tuning/`

Team previously created an analysis plan in `tuning/CPlan.md` + `tuning/CSolution.md`. None of those items were marked done. Status after this work:

| `tuning/CPlan.md` Item | Status | Notes |
|------------------------|--------|-------|
| 1.1 Register `IDbContextFactory` in DI | Done | Already in `ServiceCollectionExtensions.cs:66` via `AddPooledDbContextFactory<Dp2ReadOnlyDbContext>` |
| 1.2 Update endpoint constructor | Done | `GetWorklistSeparated` already injects `IDbContextFactory` |
| 2.1 Cache count queries (procurement) | Done | Counts now share scope with list query within each Project/Hydrate path; no duplicate predicate eval |
| 3.1 Parallel count queries (ProcurementProgram) | Done | `ProjectProcurementForComboParallel` runs list + count on 2 contexts via `Task.WhenAll` |
| 3.2 Parallel data queries (ProcurementProgram) | Done | 5 entity types projected in parallel; 5 hydration tasks in parallel via factory |
| 3.3 Parallel count queries (ContractManagementProgram) | Done | 3 counts run on 3 dedicated contexts via `Task.WhenAll` (commit `6cdf7828e`) |
| 3.4 Parallel data queries (ContractManagementProgram) | Done | 3 main lists + 3 lookup dictionaries (Plans/Vendors/POAs) parallel across 6 contexts (commit `6cdf7828e`) |
| 3.5 Parallel queries (CombinedWorklistBuilder) | Done | All 12 sources parallel via dedicated contexts; Procurement source now pre-computes accessible IDs once then runs 2 parallel hydrations |
| 3.6 Parallel main sections (GetList.cs) | Done | Already present (`Task.WhenAll` of all `Execute*ProgramAsync`) |
| 4.1 DB indexing | Done | Migration `20260523032006_AddProcurementWorklistIndexes` adds equivalent indexes (different naming convention) |
| 5.1 Performance testing before/after | Done | Manual; see TL;DR table |
| 5.3 Connection pool monitoring | Pending | Pooled factory should suffice for current load |

The 60s baseline in `tuning/CPlan.md` likely reflected an earlier code state. Current uncached baseline measured at start of this work was 1–8s (depending on path), and the 60s figure is no longer reproducible on the current branch state.

## Scope

Performance refactor of the worklist API surface in `Backend/GHB.DP2.Application/Features/WorkList/`. Targets the procurement combo section (5 entity types: `Procurement`, `Pw119`, `P79Clause2`, `PPettyCash`, `PPettyCashReimbursement`) and the combined-all view (12 sources).

## Problems Identified

1. **In-memory pagination** — both `ProcurementProgram.CreateProcurementWithPw119AndP79Clause2AndPettyCashSectionAsync` and `CombinedWorklistBuilder` loaded ALL matching records into memory, then sorted and sliced. For users with thousands of accessible records, this was the dominant cost.
2. **Sequential queries on shared `DbContext`** — `DbContext` is not thread-safe, so the original code awaited each `ToListAsync()` in series.
3. **Counts hardcoded to zero** — `ProcessProcurementSectionsAsync` returned `(0, 0, 0)` as section counts; clients showed "0 procurement" regardless of actual data.
4. **`AsSplitQuery` N+1 round-trips on hydration** — Procurement entity has 11+ collection includes → 12 sequential split queries × ~50ms = ~600ms just for hydration.
5. **Repeated access predicate evaluation** — `ProcurementAccessHelper`'s giant OR-tree (delegations, acceptors, assignees across many tables) ran for every list/count query (4× in combined view's Procurement source).
6. **Missing indexes** — heavy filtering on `Procurement.ProcessType`, `Pw119.Status`, `P79Clause2.Status`, `PPettyCash.Status`, and acceptor `UserId+Status+Type` had no covering index.

## Changes

### Commit 1 — `1dc7f41fd` perf: speed up procurement worklist combo section

**File**: [Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs)

- `ProcessProcurementSectionsAsync` accepts `IDbContextFactory<Dp2ReadOnlyDbContext>` (matches `ExpenseDisbursementProgram` pattern).
- Section counts now flow from `Section.Page.TotalRecords` instead of being hardcoded to zero.
- `CreateProcurementWithPw119AndP79Clause2AndPettyCashSectionAsync` refactored into 3 phases:
  - Phase A — 5 parallel **lightweight projections** (1 row = `(Type, Id, SortKey)`) per entity, each on its own `Dp2ReadOnlyDbContext` via `Task.WhenAll`.
  - Phase B — merge projections, sort by `ProcurementNumber`, slice to final page.
  - Phase C — hydrate full entity data only for page IDs via `WHERE Id IN (…)`.
- `CreateContractAgreementSectionAsync` uses `PaginatedList.CreateAsync` for server-side procurement pagination before vendor expansion.

**File**: `Backend/GHB.DP2.Infrastructure/Migrations/20260523032006_AddProcurementWorklistIndexes.cs`

| Index | Columns |
|-------|---------|
| `IX_Procurement_ProcessType_Step` | `(ProcessType, Step)` WHERE NOT IsDeleted |
| `IX_Procurement_ProcurementNumber_Desc` | `(ProcurementNumber DESC)` WHERE NOT IsDeleted |
| `IX_Pw119_Status` / `IX_P79Clause2_Status` / `IX_PPettyCash_Status` / `IX_PPettyCashReimbursement_Status` | `(Status)` |
| `IX_*_CreatedBy` (Pw119, P79Clause2, PPettyCash, PPettyCashReimbursement) | `(CreatedBy)` |
| `IX_*Acceptor_UserId_Status_Type` (Pw119Acceptor, P79Clause2Acceptor, PPettyCashAcceptor, PPettyCashReimbursementAcceptor) | `(UserId, Status, Type)` WHERE NOT IsDeleted |

### Commit 2 — `496778d2a` perf: speed up combined worklist via DB-side pagination

**File**: [Backend/GHB.DP2.Application/Features/WorkList/Helpers/CombinedWorklistBuilder.cs](Backend/GHB.DP2.Application/Features/WorkList/Helpers/CombinedWorklistBuilder.cs)

- All 12 `Execute*QueryAsync` methods now return `(List<CombinedWorklistItem> Items, int Total)`.
- Per-source DB-side `OrderBy + Take(pageNumber × pageSize)` + `CountAsync`, eliminating in-memory load of every match.
- `BuildCombinedWorklistAsync` merges per-source results, slices page, sums per-source counts for accurate `Total`.
- `ConcurrentBag` removed (each task owns its list).
- `ExecuteProcurementQueryAsync` (heaviest source) initially split into 4 parallel queries on dedicated `DbContext` instances.

### Commit 3 — `69921aacf` perf: projection-based hydration for procurement worklist combo

**File**: [Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs)

- `HydrateComboPageAsync` runs 5 hydration tasks in parallel (separate `DbContext` per type).
- Each `Hydrate*Async` replaces `.Include()…AsSplitQuery()` with a single `.Select(new {…})` LATERAL projection.
  - **Procurement**: anonymous type carries Plan/Department/SupplyMethod refs + status of each child collection + first-PrincipleApproval rental duration via subselects → single SQL round-trip instead of 12 split queries (saves ~500ms).
  - **Pw119 / P79Clause2 / PettyCash / PettyCashReimbursement**: project directly to `ProcurementItem`.
- `ProjectProcurementForComboParallel` now uses 2 contexts (list + count) running in parallel, halving the heaviest projection step from ~1.2s sequential to ~0.7s parallel.

### Pending — `ExecuteProcurementQueryAsync` pre-compute accessible IDs

**File**: [Backend/GHB.DP2.Application/Features/WorkList/Helpers/CombinedWorklistBuilder.cs](Backend/GHB.DP2.Application/Features/WorkList/Helpers/CombinedWorklistBuilder.cs)

Profiling showed the combined view's 4 procurement queries each evaluated `ProcurementAccessHelper`'s OR-tree predicate (~900ms per evaluation). Refactored to:

- **Step 1** — Single query: `BuildProcurementBaseQuery(...).Select(p => new { p.Id, p.IsContractDraft, p.SortAt }).ToListAsync()` — evaluates the predicate **once** and returns lightweight tuples.
- **Step 2** — Compute `cdIds` (top-N ContractDraft) + `ncdIds` (top-N non-ContractDraft) in memory; counts from same data.
- **Step 3** — Two parallel hydration queries via `WHERE Id IN (cdIds)` / `WHERE Id IN (ncdIds)` — cheap PK-index lookups.

Introduces a named `NcdProcurementProjection` class so the projection survives EF expression-tree restrictions.

Net effect on `includeAll=true`: 2.19s → **699ms** uncached.

## Reuse / Conventions Followed

- `IDbContextFactory<Dp2ReadOnlyDbContext>` is the canonical "parallel-safe DbContext" mechanism (already used by `ExpenseDisbursementProgram` and `GetWorklistSeparated`).
- `PaginatedList<T>.CreateAsync` from `Codehard.Common.DomainModel` for server-side `Skip/Take + Count`.
- Strongly-typed IDs (`ProcurementId.From`, `Pw119Id.From`, …) for `WHERE Id IN (…)` predicates — `Guid` lists don't translate.
- `.AsNoTracking()` on all read paths.
- `.NotCacheable()` retained on Procurement queries that go through `ProcurementAccessHelper` so access checks always re-evaluate (avoids stale permission caching).

## Out of Scope / Future Work

| Item | Reason |
|------|--------|
| Refactor `ProcurementAccessHelper` OR-tree itself | Touches dozens of permission rules; high regression risk. Pre-computing accessible IDs once per request is the cheaper alternative (now applied to combined view). |
| Apply pre-compute pattern to `ProcurementProgram.CreateProcurementWithPw119AndP79Clause2AndPettyCashSectionAsync` | Section path uses 5 different `Build*BaseQuery` (Pw119, P79, PettyCash, etc.) so pre-compute would need 5 ID lists. Worth doing if section path becomes bottleneck. |
| Restore `.NotCacheable()` decisions on non-procurement worklist queries | Worklist relies on EF Second-Level Cache (`CacheAllQueries` 30s sliding). Restoring `NotCacheable()` everywhere makes data always fresh but doubles uncached cost. Needs UX/product decision. |
| Compiled queries (`EF.CompileAsyncQuery`) | Marginal gain (~10–30ms per query); revisit if response time still matters. |
| Materialized worklist view | Multi-day effort; only worth it if even ~500ms uncached is intolerable. |

## ContractAgreement Projection Refactor — Done

### Result

| Metric | Baseline (Include + AsSplitQuery) | After (Flat Projection) |
|--------|-----------------------------------|-------------------------|
| Endpoint | `GET /api/worklist?includeContractAgreement=true&pageSize=10` | same |
| Warm-app cold-cache (4 runs) | 1.37s | **0.94s / 1.09s / 1.21s / 1.23s** (median ~1.1s) |
| Cache hit (EFCache 30s) | ~25ms | ~33ms |
| SQL queries per request | 1 (Step 1) + 12 (split queries on Procurement Include chain) | 1 (Step 1) + 2 (vendor projection + procurement projection in parallel) |
| Cartesian risk on parallel collections (`ContractInvitations × PrincipleApprovals × Vendors`) | yes | none (LATERAL subselects) |

### What Changed

[ProcurementProgram.cs:127-196 (CreateContractAgreementSectionAsync)](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs#L127-L196):

- **Step 1** unchanged: get accessible procurement IDs + ProcessType + sort keys via `BuildProcurementBaseQuery` (this remains the dominant cost ~700ms due to `ProcurementAccessHelper` OR-tree).
- **Step 2 rewritten**:
  - Query A — `ProjectContractAgreementVendorsAsync`: flat `.SelectMany` projection over `ContractDrafts.Vendors` with all 25+ vendor + procurement + POAC fields including 4 nullable budget descriptions for in-memory `ResolveBudgetDescription` fallback.
  - Query B — reuse existing `HydrateProcurementAsync` for non-ContractDraft fallback path + ContractDraft procurements with zero vendors.
  - Both queries run in parallel via `IDbContextFactory` + `Task.WhenAll` on dedicated `Dp2ReadOnlyDbContext` instances.
- **In-memory mapping** preserves `pageMeta` order. `BuildContractAgreementVendorItem` inlines the budget description fallback chain + vendor name concatenation.
- **Removed dead code**: `ExpandContractDraftVendors`, `MapProcurement`, `FormatRentalDuration`, `ResolveChildStatus`, `ResolveBudgetDescription` (local copy) — no remaining callers after refactor.

### Why Gain Modest (~20% not ~50%+)

Profiling confirmed:

- **Step 1 dominates** (~700ms): `ProcurementAccessHelper.Build*` OR-tree predicate evaluation on every request. Touches dozens of permission tables. This was untouched by the projection refactor — Step 1 still has to enumerate all accessible procurements just to know which IDs land on the page.
- **Step 2 savings bounded by small dataset**: ContractAgreement step has 6 procurements total (per current sample). Include chain cartesian explosion would be much worse on 1000+ procurements; current data size hides most of the gain.
- **EF Core 9 split-query overhead is ~50ms per round-trip on warm app**, so 12 split queries ≈ 600ms theoretical save. Actual save measured ~270ms — close to the theoretical bound given the small data.

### Verification

- ✓ `dotnet build` clean (0 errors)
- ✓ Response JSON byte-identical to baseline ([tuning/baseline-contract-agreement.json](tuning/baseline-contract-agreement.json) vs [tuning/after-contract-agreement.json](tuning/after-contract-agreement.json), normalized via `jq -S | sort_by(.id,.contractDraftVendorId)`)
- ✓ Both branches exercised: 5 ContractDraft vendor rows + 2 non-ContractDraft (`ContractInvitation`) rows + 1 multi-vendor procurement (id `0198c68c` expanded into 2 vendor rows)
- ✓ Counts match: `totalRecords=6, counts.contractAgreement=6, data.length=7`
- ✓ Cache-hit path unchanged (~33ms)

### Follow-up Worth Considering

- Apply pre-compute accessible IDs pattern to `CreateContractAgreementSectionAsync` Step 1 if `ProcurementAccessHelper` cost becomes the next bottleneck. Same approach already used in `CombinedWorklistBuilder.ExecuteProcurementQueryAsync`.

---

## Next: Planned Refactor — ContractAgreement Projection

### Baseline (2026-05-23, cold cache after container restart)

| Metric | Value |
|--------|-------|
| Endpoint | `GET /api/worklist?includeContractAgreement=true&pageSize=10` |
| `total_time` | **4.89s** (cold; warm ≈ 1.37s) |
| `totalRecords` | 6 procurements |
| Rows returned | 7 (1 procurement with 2 vendor expansions, id `0198c68c`) |
| Branches exercised | `ContractDraft` vendor expansion (5 rows) + `ContractInvitation` non-vendor (2 rows) |
| Types | `Procurement` + `Rent` |
| Sample saved | [tuning/baseline-contract-agreement.json](tuning/baseline-contract-agreement.json) |

### Problem

`CreateContractAgreementSectionAsync` ([ProcurementProgram.cs:165-183](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs#L165-L183)) still uses `.Include()` chain 4 levels deep + `.AsSplitQuery()`:

```
Procurement → Plan, Department, SupplyMethod×3, PrincipleApprovals → RentTypeCodeInfo,
              PrincipleApprovalRentals, ContractInvitations,
              ContractDrafts → Vendors → ContractInvitationVendors → PurchaseOrderApprovalContract
```

- Multiple split round-trips (one per collection include).
- Cartesian risk on parallel collections (`ContractInvitations` + `PrincipleApprovals` + `Vendors`).
- `ExpandContractDraftVendors` ([line 198-240](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs#L198-L240)) drills into `vendor.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor.EstablishmentName` post-hydration → forces all those refs into the include tree.
- `ResolveBudgetDescription` ([line 260-283](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs#L260-L283)) walks 4 nullable budget sources (`PrincipleApprovalRentalBudget`, `Budget`, `PpPurchaseRequisitionBudget`, `PPurchaseOrderApprovalBudget`) on the POAC.

### Refactor Plan

**Approach**: replace `.Include` chain with flat DB-side projection. Two queries split by branch, both on the same `pageIds` set:

1. **Query A — ContractDraft vendor expansion**
   ```csharp
   ctxA.Procurements.AsNoTracking()
     .Where(x => pageIds.Contains(x.Id) && x.ProcessType == ProcessType.ContractDraft)
     .SelectMany(x => x.ContractDrafts.SelectMany(cd => cd.Vendors.Select(v => new VendorRow {
         // Procurement fields
         Id = x.Id.Value, Type = x.Type,
         PlanNumber = x.Plan != null ? x.Plan.PlanNumber.Value : null,
         PlanTypeName = x.Plan != null ? x.Plan.Type.ToString() : null,
         ProcurementNumber = x.ProcurementNumber.HasValue ? x.ProcurementNumber.Value.Value : null,
         Name = x.Name, Budget = x.Budget,
         DepartmentName = x.Department.Name,
         SupplyMethodLabel = x.SupplyMethod.Label,
         SupplyMethodTypeLabel = x.SupplyMethodType != null ? x.SupplyMethodType.Label : "",
         SupplyMethodSpecialTypeLabel = x.SupplyMethodSpecialType != null ? x.SupplyMethodSpecialType.Label : "",
         FirstApprovalRentTypeLabel = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => p.RentTypeCodeInfo.Label).FirstOrDefault(),
         FirstApprovalRentalDurationYear/Month/Day = ... .FirstOrDefault(),
         FirstApprovalTotalRental = ... .FirstOrDefault(),
         Step = x.Step, ProcessType = x.ProcessType,
         // Vendor fields
         VendorId = v.Id.Value,
         VendorContractNumber = v.ContractNumber,
         VendorContractName = v.ContractName,
         VendorBudget = v.Budget,
         VendorStatus = v.Status,
         // POAC fields for vendorName resolution
         EntrepreneurEstablishmentName = v.CIV.POAC.Entrepreneur != null ? v.CIV.POAC.Entrepreneur.SuVendor.EstablishmentName : null,
         PARentEntrepreneurEstablishmentName = v.CIV.POAC.PrincipleApprovalRentalEntrepreneurs != null ? v.CIV.POAC.PrincipleApprovalRentalEntrepreneurs.Vendor.EstablishmentName : null,
         // BudgetDescription 4-level fallback (all nullable strings)
         BudgetDesc_PARental = v.CIV.POAC.PrincipleApprovalRentalBudget != null ? v.CIV.POAC.PrincipleApprovalRentalBudget.Description : null,
         BudgetDesc_Budget   = v.CIV.POAC.Budget != null ? v.CIV.POAC.Budget.Description : null,
         BudgetDesc_PR       = v.CIV.POAC.PpPurchaseRequisitionBudget != null ? v.CIV.POAC.PpPurchaseRequisitionBudget.Description : null,
         BudgetDesc_POA      = v.CIV.POAC.PPurchaseOrderApprovalBudget != null ? v.CIV.POAC.PPurchaseOrderApprovalBudget.Description : null,
     })))
     .ToListAsync(ct);
   ```

2. **Query B — non-ContractDraft fallback** (`MapProcurement` path)
   - Same flat projection of procurement-only fields (no vendor expansion).
   - Reuse projection shape from `HydrateProcurementAsync` (combo section) since identical output structure.

3. **In-memory final mapping**
   - For each `VendorRow` from Query A:
     - Apply `ResolveBudgetDescription(type, BudgetDesc_*)` fallback chain.
     - Compose `vendorName`: `ContractNumber + (EntrepreneurName != null ? " : " + EntrepreneurName : "")`.
     - Pick `status = VendorStatus.ToString()`.
     - Pick `budget = VendorBudget`.
   - For each row from Query B: apply `ResolveChildStatus` logic + `FormatRentalDuration`.
   - Merge + preserve `pageIds` order.

4. **Parallel execution**
   - Run Query A + Query B via `Task.WhenAll` on 2 dedicated `Dp2ReadOnlyDbContext` (factory).

### Expected Gain

- **Eliminate**: 4-level Include chain, AsSplitQuery N+1 round-trips, cartesian explosion from parallel collections.
- **Target cold timing**: ~4.9s → **~600-1000ms** (similar shape to procurement combo refactor: 5-8s → 480ms).
- **Memory**: page-only (≤ pageSize × ~3-5 vendors max) instead of full entity graphs.

### Risks + Mitigations

| Risk | Mitigation |
|------|------------|
| `SelectMany` over conditional ContractDraft branch doesn't translate in EF | Split into 2 queries by `ProcessType` filter — simpler, EF-translatable. |
| BudgetDescription 4-level nullable navigation through `v.CIV.POAC.*` may produce LEFT JOIN bloat | If query plan degrades, fall back to nullable sub-`.Select(...).FirstOrDefault()` per source. |
| Output row-order or field divergence vs current | Diff [baseline-contract-agreement.json](tuning/baseline-contract-agreement.json) row-by-row against post-refactor output. Same `pageIds` order preserved. |
| `Plan.Type.ToString()` / `PlanNumber.Value` can throw `Nullable object must have a value` (seen before) | Null guard: `x.Plan != null ? x.Plan.X : null`. |
| EF expression-tree restrictions on anonymous types vs `Task<T>` | Use named `record VendorRow` + `record NonVendorRow` to satisfy strongly-typed `Task<List<T>>`. |

### Verification Plan

1. `dotnet build` clean (0 errors).
2. Restart container → cold curl → save to `tuning/after-contract-agreement.json`.
3. Diff vs `tuning/baseline-contract-agreement.json` (jq sort by id+vendorId → compare).
4. Compare `total_time` cold + warm (run 2× consecutively).
5. EXPLAIN ANALYZE the new SQL — verify uses `IX_Procurement_ProcessType_Step` + PK lookups.
6. Smoke test `/wl` page in browser → ContractAgreement tab renders identical.

### Files to Touch

- [Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs](Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs) — refactor `CreateContractAgreementSectionAsync` only; leave `ExpandContractDraftVendors` + `ResolveBudgetDescription` as private helpers for in-memory mapping.
- No migration changes.

---

## Unrelated Findings (Out of Scope)

While profiling `?includeContractAmendment=true`, the runtime threw `Cannot convert string value 'Reviewed' from the database to any value in the mapped 'ContractDraftVendorEditStatus' enum.` ([Backend/GHB.DP2.Domain/ContractManagement/CaContractDraftVendorEdit/CaContractDraftVendorEdit.cs:19](Backend/GHB.DP2.Domain/ContractManagement/CaContractDraftVendorEdit/CaContractDraftVendorEdit.cs#L19)). The DB holds a `Reviewed` status that the enum does not declare. Pre-existing schema/code drift; flag to the ContractAmendment owners. Not addressed in this PR.

## Verification Checklist

- [x] `dotnet build Backend/GHB.DP2.sln` — 0 errors, 0 warnings
- [x] EF migration applied successfully (`20260523032006_AddProcurementWorklistIndexes` in `__EFMigrationsHistory`)
- [x] All `IX_*` indexes present in `Procurement` schema
- [x] `/api/worklist?includeProcurement=true&pageSize=10` returns 200 with same shape as before, ~480-626ms uncached
- [x] `/api/worklist?includeAll=true&pageSize=10` returns 200 with non-zero counts, ~699ms uncached
- [x] Docker image rebuilds + container starts without `PendingModelChangesWarning`
- [ ] Manual UI smoke test on `/wl` page across all tabs (pending)
- [ ] Load test under realistic concurrency (pending)

## Files Changed

```
Backend/GHB.DP2.Application/Features/WorkList/GetList.cs
Backend/GHB.DP2.Application/Features/WorkList/Helpers/CombinedWorklistBuilder.cs
Backend/GHB.DP2.Application/Features/WorkList/Programs/ProcurementProgram.cs
Backend/GHB.DP2.Infrastructure/Migrations/20260523032006_AddProcurementWorklistIndexes.cs
Backend/GHB.DP2.Infrastructure/Migrations/20260523032006_AddProcurementWorklistIndexes.Designer.cs
Backend/GHB.DP2.Infrastructure/Migrations/Dp2DbContextModelSnapshot.cs
```
