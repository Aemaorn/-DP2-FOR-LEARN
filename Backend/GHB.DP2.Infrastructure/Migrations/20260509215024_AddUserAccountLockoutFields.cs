#nullable disable

namespace GHB.DP2.Infrastructure.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddUserAccountLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                schema: "SystemUtility",
                table: "SuUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<System.DateTimeOffset>(
                name: "LockoutEnd",
                schema: "SystemUtility",
                table: "SuUser",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                schema: "SystemUtility",
                table: "SuUser");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                schema: "SystemUtility",
                table: "SuUser");
        }
    }
}
