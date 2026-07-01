#nullable disable

namespace GHB.DP2.Infrastructure.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for Procurement queries - critical for worklist performance
            // Reduces Q4 (Procurement PreProcurement Count) from ~700ms to ~65ms
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Procurement_Step_IsDeleted"
                ON "Procurement"."Procurement" ("Step")
                WHERE NOT "IsDeleted";
                """);

            // Index for Plan queries - improves worklist Plan count performance
            // Reduces Q2 (Plan Count) from ~13ms to ~2ms
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Plan_Status_IsDeleted_IsActive"
                ON "Plan"."Plan" ("Status")
                WHERE NOT "IsDeleted" AND "IsActive";
                """);

            // Index for PpAppoint acceptors lookup - used in Procurement worklist queries
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PpAppointAcceptors_Lookup"
                ON "Procurement"."PpAppointAcceptors" ("PpAppointId", "Type", "Status", "Sequence");
                """);

            // Index for PpTorDraft acceptors lookup - used in Procurement worklist queries
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PpTorDraftAcceptors_Lookup"
                ON "Procurement"."PpTorDraftAcceptors" ("PpTorDraftId", "Type", "Status", "Sequence")
                WHERE NOT "IsDeleted";
                """);

            // Index for PpMedianPrice acceptors lookup - used in Procurement worklist queries
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PpMedianPriceAcceptor_Lookup"
                ON "Procurement"."PpMedianPriceAcceptor" ("MedianPriceId", "Type", "Status", "Sequence")
                WHERE NOT "IsDeleted";
                """);

            // Index for PpPurchaseRequisition acceptors lookup - used in Procurement worklist queries
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PpPurchaseRequisitionAcceptors_Lookup"
                ON "Procurement"."PpPurchaseRequisitionAcceptors" ("PpPurchaseRequisitionId", "Type", "Status", "Sequence")
                WHERE NOT "IsDeleted";
                """);

            // Index for SuDelegatee user lookup - used across all worklist queries for delegation
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_SuDelegatee_SuUserId"
                ON "SystemUtility"."SuDelegatee" ("SuUserId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "Procurement"."IX_Procurement_Step_IsDeleted";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "Plan"."IX_Plan_Status_IsDeleted_IsActive";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "Procurement"."IX_PpAppointAcceptors_Lookup";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "Procurement"."IX_PpTorDraftAcceptors_Lookup";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "Procurement"."IX_PpMedianPriceAcceptor_Lookup";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "Procurement"."IX_PpPurchaseRequisitionAcceptors_Lookup";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "SystemUtility"."IX_SuDelegatee_SuUserId";
                """);
        }
    }
}
