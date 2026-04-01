using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                schema: "bc_inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityWindows",
                schema: "bc_inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WaitlistEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityWindows_WaitlistEntries_WaitlistEntryId",
                        column: x => x.WaitlistEntryId,
                        principalSchema: "bc_inventory",
                        principalTable: "WaitlistEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityWindows_Token",
                schema: "bc_inventory",
                table: "OpportunityWindows",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityWindows_WaitlistEntryId",
                schema: "bc_inventory",
                table: "OpportunityWindows",
                column: "WaitlistEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_EventId_Section_Status",
                schema: "bc_inventory",
                table: "WaitlistEntries",
                columns: new[] { "EventId", "Section", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_UserId_EventId_Section",
                schema: "bc_inventory",
                table: "WaitlistEntries",
                columns: new[] { "UserId", "EventId", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_UserId_Status",
                schema: "bc_inventory",
                table: "WaitlistEntries",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpportunityWindows",
                schema: "bc_inventory");

            migrationBuilder.DropTable(
                name: "WaitlistEntries",
                schema: "bc_inventory");
        }
    }
}
