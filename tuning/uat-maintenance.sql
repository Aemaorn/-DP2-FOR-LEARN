-- ================================================================
-- UAT Worklist Maintenance — RUN ONLY AFTER DIAGNOSTIC REVIEW
-- ================================================================
-- These are reactive fixes for common findings:
--   - Stale ANALYZE → planner picks bad indexes
--   - Index bloat → slow scans
-- Run during low-traffic window.
-- Each VACUUM ANALYZE locks table briefly (ShareUpdateExclusiveLock).
-- ================================================================

-- A. Refresh statistics on hot worklist tables
VACUUM (ANALYZE, VERBOSE) "Procurement"."Procurement";
VACUUM (ANALYZE, VERBOSE) "Procurement"."Pw119";
VACUUM (ANALYZE, VERBOSE) "Procurement"."P79Clause2";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PPettyCash";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PPettyCashReimbursement";

VACUUM (ANALYZE, VERBOSE) "Procurement"."PpAppointAcceptors";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PpTorDraftAcceptors";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PpMedianPriceAcceptor";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PpPurchaseRequisitionAcceptors";
VACUUM (ANALYZE, VERBOSE) "Procurement"."Pw119Acceptor";
VACUUM (ANALYZE, VERBOSE) "Procurement"."P79Clause2Acceptor";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PPettyCashAcceptor";
VACUUM (ANALYZE, VERBOSE) "Procurement"."PPettyCashReimbursementAcceptor";

VACUUM (ANALYZE, VERBOSE) "ContractAgreement"."CaContractDraft";
VACUUM (ANALYZE, VERBOSE) "ContractAgreement"."CaContractDraftVendor";
VACUUM (ANALYZE, VERBOSE) "ContractAgreement"."CaContractInvitation";

VACUUM (ANALYZE, VERBOSE) "SystemUtility"."SuDelegatee";

-- B. (Optional) Reindex if bloat detected — heavy lock, off-hours only.
-- Uncomment per-table as needed:
-- REINDEX TABLE CONCURRENTLY "Procurement"."Procurement";
-- REINDEX TABLE CONCURRENTLY "Procurement"."Pw119Acceptor";
-- REINDEX TABLE CONCURRENTLY "Procurement"."P79Clause2Acceptor";

-- C. (Optional) Increase work_mem at session level for EXPLAIN ANALYZE testing.
-- Doesn't persist. For persistent change, edit postgresql.conf.
-- SET work_mem = '64MB';
-- SET random_page_cost = 1.1;  -- if SSD-backed

\echo 'Maintenance complete.'
