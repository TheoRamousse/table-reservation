using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reservation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    customer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    table_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    service_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    booking_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    arrival_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    guests_count = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    special_requests = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    has_allergy_alert = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_celebration = table.Column<bool>(type: "INTEGER", nullable: false),
                    needs_high_chair = table.Column<bool>(type: "INTEGER", nullable: false),
                    late_cancel = table.Column<bool>(type: "INTEGER", nullable: false),
                    cancellation_reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    first_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    is_blacklisted = table.Column<bool>(type: "INTEGER", nullable: false),
                    no_show_count = table.Column<int>(type: "INTEGER", nullable: false),
                    late_cancel_count = table.Column<int>(type: "INTEGER", nullable: false),
                    vip_level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dining_services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    last_booking_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    duration_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    max_covers = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dining_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    number = table.Column<int>(type: "INTEGER", nullable: false),
                    capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    min_capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    zone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    is_combinable = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tables", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_bookings_customer",
                table: "bookings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_bookings_date_service",
                table: "bookings",
                columns: new[] { "booking_date", "service_id" });

            migrationBuilder.CreateIndex(
                name: "idx_bookings_table_date",
                table: "bookings",
                columns: new[] { "table_id", "booking_date" });

            migrationBuilder.CreateIndex(
                name: "idx_customers_email",
                table: "customers",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_customers_phone",
                table: "customers",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "idx_tables_number",
                table: "tables",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "dining_services");

            migrationBuilder.DropTable(
                name: "tables");
        }
    }
}
