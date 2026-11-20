using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseOrderManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenOutboxRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc_OccurredOnUtc",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredOnUtc",
                table: "OutboxMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptOnUtc",
                table: "OutboxMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptOnUtc",
                table: "OutboxMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptOnUtc_LockedUntilUtc_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptOnUtc", "LockedUntilUtc", "OccurredOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptOnUtc_LockedUntilUtc_OccurredOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastAttemptOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptOnUtc",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "LockedUntilUtc", "OccurredOnUtc" });
        }
    }
}
