-- ================================================================
-- UAT Worklist Performance Diagnostics (DBeaver-compatible)
-- ================================================================
-- DBeaver: open file -> Alt+X (run script) OR highlight block + Ctrl+Enter
-- Tip: Right-click result -> "Export resultset" -> CSV/Excel for sharing
-- ================================================================

-- ================================================================
-- 1. Migration history (last 10)
-- ================================================================
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;


-- ================================================================
-- 2. Performance indexes presence (Procurement schema)
-- ================================================================
SELECT schemaname, tablename, indexname
FROM pg_indexes
WHERE schemaname = 'Procurement'
  AND indexname LIKE 'IX_%'
ORDER BY indexname;


-- ================================================================
-- 3. Data volumes (worklist-related tables)
-- ================================================================
SELECT
    'Procurement.Procurement'         AS tablename, COUNT(*) AS row_count FROM "Procurement"."Procurement"
UNION ALL SELECT 'Procurement.Pw119',                        COUNT(*) FROM "Procurement"."Pw119"
UNION ALL SELECT 'Procurement.P79Clause2',                   COUNT(*) FROM "Procurement"."P79Clause2"
UNION ALL SELECT 'Procurement.PPettyCash',                   COUNT(*) FROM "Procurement"."PPettyCash"
UNION ALL SELECT 'Procurement.PPettyCashReimbursement',      COUNT(*) FROM "Procurement"."PPettyCashReimbursement"
UNION ALL SELECT 'Procurement.PpAppointAcceptors',           COUNT(*) FROM "Procurement"."PpAppointAcceptors"
UNION ALL SELECT 'Procurement.PpTorDraftAcceptors',          COUNT(*) FROM "Procurement"."PpTorDraftAcceptors"
UNION ALL SELECT 'Procurement.PpPurchaseRequisitionAcceptors', COUNT(*) FROM "Procurement"."PpPurchaseRequisitionAcceptors"
UNION ALL SELECT 'Procurement.Pw119Acceptor',                COUNT(*) FROM "Procurement"."Pw119Acceptor"
UNION ALL SELECT 'Procurement.P79Clause2Acceptor',           COUNT(*) FROM "Procurement"."P79Clause2Acceptor"
UNION ALL SELECT 'Procurement.PPettyCashAcceptor',           COUNT(*) FROM "Procurement"."PPettyCashAcceptor"
UNION ALL SELECT 'Procurement.PPettyCashReimbursementAcceptor', COUNT(*) FROM "Procurement"."PPettyCashReimbursementAcceptor"
UNION ALL SELECT 'ContractAgreement.CaContractDraft',        COUNT(*) FROM "ContractAgreement"."CaContractDraft"
UNION ALL SELECT 'ContractAgreement.CaContractDraftVendor',  COUNT(*) FROM "ContractAgreement"."CaContractDraftVendor"
UNION ALL SELECT 'ContractAgreement.CaContractInvitation',   COUNT(*) FROM "ContractAgreement"."CaContractInvitation"
UNION ALL SELECT 'Plan.Plan',                                COUNT(*) FROM "Plan"."Plan"
UNION ALL SELECT 'SystemUtility.SuDelegatee',                COUNT(*) FROM "SystemUtility"."SuDelegatee"
ORDER BY row_count DESC;


-- ================================================================
-- 4. PostgreSQL config (memory + planner)
-- ================================================================
SELECT name, setting, unit
FROM pg_settings
WHERE name IN (
    'work_mem',
    'shared_buffers',
    'effective_cache_size',
    'max_parallel_workers_per_gather',
    'max_connections',
    'random_page_cost',
    'seq_page_cost'
)
ORDER BY name;


-- ================================================================
-- 5. Connection pool / activity snapshot
-- ================================================================
SELECT state, COUNT(*) AS conns
FROM pg_stat_activity
WHERE datname = current_database()
GROUP BY state
ORDER BY conns DESC;


-- ================================================================
-- 6. ANALYZE freshness for hot tables
-- ================================================================
SELECT
    schemaname || '.' || relname AS tablename,
    n_live_tup,
    n_dead_tup,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
WHERE schemaname IN ('Procurement', 'ContractAgreement', 'ContractManagement', 'Plan')
  AND relname IN (
      'Procurement','Pw119','P79Clause2','PPettyCash','PPettyCashReimbursement',
      'PpAppointAcceptors','PpTorDraftAcceptors','PpPurchaseRequisitionAcceptors','PpMedianPriceAcceptor',
      'Pw119Acceptor','P79Clause2Acceptor','PPettyCashAcceptor','PPettyCashReimbursementAcceptor',
      'CaContractDraft','CaContractDraftVendor','CaContractInvitation',
      'CmDeliveryAcceptancePeriod','CmContractTermination','CmContractGuaranteeReturn',
      'Plan')
ORDER BY GREATEST(COALESCE(last_analyze, '1970-01-01'::timestamp), COALESCE(last_autoanalyze, '1970-01-01'::timestamp)) ASC;


-- ================================================================
-- 7. Index sizes (top 20)
-- ================================================================
SELECT
    pi.schemaname, pi.tablename, pi.indexname,
    pg_size_pretty(pg_relation_size(pui.indexrelid)) AS index_size
FROM pg_indexes pi
JOIN pg_stat_user_indexes pui ON pui.indexrelname = pi.indexname AND pui.schemaname = pi.schemaname
WHERE pi.schemaname IN ('Procurement', 'ContractAgreement', 'ContractManagement', 'Plan')
ORDER BY pg_relation_size(pui.indexrelid) DESC
LIMIT 20;


-- ================================================================
-- 8. Table sizes (top 15)
-- ================================================================
SELECT
    schemaname || '.' || relname AS tablename,
    pg_size_pretty(pg_total_relation_size(relid)) AS total_size,
    pg_size_pretty(pg_relation_size(relid))       AS table_size,
    n_live_tup
FROM pg_stat_user_tables
WHERE schemaname IN ('Procurement', 'ContractAgreement', 'ContractManagement', 'Plan')
ORDER BY pg_total_relation_size(relid) DESC
LIMIT 15;
