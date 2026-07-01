#nullable disable

namespace GHB.DP2.Infrastructure.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddProcurementWorklistIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Procurement: ProcessType filter + sort key
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Procurement_ProcessType_Step"
                ON "Procurement"."Procurement" ("ProcessType", "Step")
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Procurement_ProcurementNumber_Desc"
                ON "Procurement"."Procurement" ("ProcurementNumber" DESC)
                WHERE NOT "IsDeleted";
                """);

            // Sibling entity status filters
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Pw119_Status"
                ON "Procurement"."Pw119" ("Status");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_P79Clause2_Status"
                ON "Procurement"."P79Clause2" ("Status");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PPettyCash_Status"
                ON "Procurement"."PPettyCash" ("Status");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PPettyCashReimbursement_Status"
                ON "Procurement"."PPettyCashReimbursement" ("Status");
                """);

            // Created-by lookups for Draft/Rejected/Edit predicate
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Pw119_CreatedBy"
                ON "Procurement"."Pw119" ("CreatedBy");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_P79Clause2_CreatedBy"
                ON "Procurement"."P79Clause2" ("CreatedBy");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PPettyCash_CreatedBy"
                ON "Procurement"."PPettyCash" ("CreatedBy");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PPettyCashReimbursement_CreatedBy"
                ON "Procurement"."PPettyCashReimbursement" ("CreatedBy");
                """);

            // Acceptor user-lookup composite indexes for nested .Any() checks
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Pw119Acceptor_UserId_Status_Type"
                ON "Procurement"."Pw119Acceptor" ("UserId", "Status", "Type")
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_P79Clause2Acceptor_UserId_Status_Type"
                ON "Procurement"."P79Clause2Acceptor" ("UserId", "Status", "Type")
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PPettyCashAcceptor_UserId_Status_Type"
                ON "Procurement"."PPettyCashAcceptor" ("UserId", "Status", "Type")
                WHERE NOT "IsDeleted";
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_PPettyCashReimbursementAcceptor_UserId_Status_Type"
                ON "Procurement"."PPettyCashReimbursementAcceptor" ("UserId", "Status", "Type")
                WHERE NOT "IsDeleted";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_Procurement_ProcessType_Step";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_Procurement_ProcurementNumber_Desc";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_Pw119_Status";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_P79Clause2_Status";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_PPettyCash_Status";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_PPettyCashReimbursement_Status";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_Pw119_CreatedBy";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_P79Clause2_CreatedBy";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_PPettyCash_CreatedBy";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_PPettyCashReimbursement_CreatedBy";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_Pw119Acceptor_UserId_Status_Type";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_P79Clause2Acceptor_UserId_Status_Type";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_PPettyCashAcceptor_UserId_Status_Type";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "Procurement"."IX_PPettyCashReimbursementAcceptor_UserId_Status_Type";""");
        }
    }
}
