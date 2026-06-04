using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reservation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClosedDayAndTableLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "closed_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    closed_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_closed_days", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "table_locks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    booking_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    primary_table_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    secondary_table_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_table_locks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_closed_days_date",
                table: "closed_days",
                column: "closed_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_table_locks_booking",
                table: "table_locks",
                column: "booking_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "closed_days");

            migrationBuilder.DropTable(
                name: "table_locks");
        }
    }
}
