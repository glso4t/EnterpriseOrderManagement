using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseOrderManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxFailureCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbandonedClaimCount",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MessageFailureCount",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbandonedClaimCount",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "MessageFailureCount",
                table: "OutboxMessages");
        }
    }
}
