-- ================================================================
-- UAT Worklist Performance Diagnostics
-- ================================================================
-- Run each block, save output, send back for analysis
-- ================================================================

\echo '======================================'
\echo '1. Migration history (last 10)'
\echo '======================================'

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;

\echo ''
\echo '======================================'
\echo '2. Performance indexes presence'
\echo '======================================'

SELECT schemaname, tablename, indexname
FROM pg_indexes
WHERE schemaname = 'Procurement'
  AND indexname LIKE 'IX_%'
ORDER BY indexname;

\echo ''
\echo '======================================'
\echo '3. Data volumes (worklist-related tables)'
\echo '======================================'

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

\echo ''
\echo '======================================'
\echo '4. PostgreSQL config (memory + planner)'
\echo '======================================'

SHOW work_mem;
SHOW shared_buffers;
SHOW effective_cache_size;
SHOW max_parallel_workers_per_gather;
SHOW max_connections;
SHOW random_page_cost;
SHOW seq_page_cost;

\echo ''
\echo '======================================'
\echo '5. Connection pool / activity snapshot'
\echo '======================================'

SELECT state, COUNT(*) AS conns
FROM pg_stat_activity
WHERE datname = current_database()
GROUP BY state
ORDER BY conns DESC;

\echo ''
\echo '======================================'
\echo '6. ANALYZE freshness for hot tables'
\echo '======================================'

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

\echo ''
\echo '======================================'
\echo '7. Index sizes (sanity check, top 20)'
\echo '======================================'

SELECT
    schemaname, tablename, indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_indexes pi
JOIN pg_stat_user_indexes pui ON pui.indexrelname = pi.indexname AND pui.schemaname = pi.schemaname
WHERE pi.schemaname IN ('Procurement', 'ContractAgreement', 'ContractManagement', 'Plan')
ORDER BY pg_relation_size(indexrelid) DESC
LIMIT 20;

\echo ''
\echo '======================================'
\echo '8. Table sizes (top 15)'
\echo '======================================'

SELECT
    schemaname || '.' || relname AS tablename,
    pg_size_pretty(pg_total_relation_size(relid)) AS total_size,
    pg_size_pretty(pg_relation_size(relid))       AS table_size,
    n_live_tup
FROM pg_stat_user_tables
WHERE schemaname IN ('Procurement', 'ContractAgreement', 'ContractManagement', 'Plan')
ORDER BY pg_total_relation_size(relid) DESC
LIMIT 15;

\echo ''
\echo '======================================'
\echo 'Done. Save output and share back.'
\echo '======================================'
