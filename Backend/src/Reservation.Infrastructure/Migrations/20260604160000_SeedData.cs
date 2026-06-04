using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Reservation.Infrastructure.Persistence;

#nullable disable

namespace Reservation.Infrastructure.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ReservationDbContext))]
    [Migration("20260604160000_SeedData")]
    public partial class SeedData : Migration
    {
        // ── Services ──────────────────────────────────────────────────────────
        private const string _dejeuner = "a1a1a1a1-0000-0000-0000-000000000001";
        private const string _diner    = "a1a1a1a1-0000-0000-0000-000000000002";
        private const string _brunch   = "a1a1a1a1-0000-0000-0000-000000000003";

        // ── Tables ────────────────────────────────────────────────────────────
        private const string _t1  = "b2b2b2b2-0000-0000-0000-000000000001"; // Salle 2p
        private const string _t2  = "b2b2b2b2-0000-0000-0000-000000000002"; // Salle 2p
        private const string _t3  = "b2b2b2b2-0000-0000-0000-000000000003"; // Salle 4p combinable
        private const string _t4  = "b2b2b2b2-0000-0000-0000-000000000004"; // Salle 4p combinable
        private const string _t5  = "b2b2b2b2-0000-0000-0000-000000000005"; // Salle 6p
        private const string _t6  = "b2b2b2b2-0000-0000-0000-000000000006"; // Terrasse 2p
        private const string _t7  = "b2b2b2b2-0000-0000-0000-000000000007"; // Terrasse 4p combinable
        private const string _t8  = "b2b2b2b2-0000-0000-0000-000000000008"; // Terrasse 6p combinable
        private const string _t9  = "b2b2b2b2-0000-0000-0000-000000000009"; // Bar 2p
        private const string _t10 = "b2b2b2b2-0000-0000-0000-000000000010"; // Bar 4p
        private const string _t11 = "b2b2b2b2-0000-0000-0000-000000000011"; // SalonPrive 12p

        // ── Customers ─────────────────────────────────────────────────────────
        private const string _marie  = "c3c3c3c3-0000-0000-0000-000000000001"; // None
        private const string _robert = "c3c3c3c3-0000-0000-0000-000000000002"; // VIP
        private const string _sophie = "c3c3c3c3-0000-0000-0000-000000000003"; // VVIP
        private const string _lucas  = "c3c3c3c3-0000-0000-0000-000000000004"; // 2 no-shows
        private const string _anne   = "c3c3c3c3-0000-0000-0000-000000000005"; // blacklistée
        private const string _thomas = "c3c3c3c3-0000-0000-0000-000000000006"; // 1 late cancel

        // ── Bookings ──────────────────────────────────────────────────────────
        private const string _bk1 = "d4d4d4d4-0000-0000-0000-000000000001";
        private const string _bk2 = "d4d4d4d4-0000-0000-0000-000000000002";
        private const string _bk3 = "d4d4d4d4-0000-0000-0000-000000000003";
        private const string _bk4 = "d4d4d4d4-0000-0000-0000-000000000004";
        private const string _bk5 = "d4d4d4d4-0000-0000-0000-000000000005";
        private const string _bk6 = "d4d4d4d4-0000-0000-0000-000000000006";
        private const string _bk7 = "d4d4d4d4-0000-0000-0000-000000000007";
        private const string _bk8 = "d4d4d4d4-0000-0000-0000-000000000008";
        private const string _bk9 = "d4d4d4d4-0000-0000-0000-000000000009";

        private const string _ts = "2026-06-04 14:00:00+00:00";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Services de restauration ───────────────────────────────────
            migrationBuilder.InsertData(
                table: "dining_services",
                columns: new[] { "id", "name", "start_time", "end_time", "last_booking_time", "duration_minutes", "max_covers", "is_active", "created_at", "updated_at" },
                columnTypes: new[] { "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "INTEGER", "INTEGER", "TEXT", "TEXT" },
                values: new object[,]
                {
                    { _dejeuner, "Déjeuner", "12:00:00", "14:30:00", "13:30:00", 90,  60, 1, _ts, _ts },
                    { _diner,    "Dîner",    "19:00:00", "22:30:00", "21:00:00", 120, 60, 1, _ts, _ts },
                    { _brunch,   "Brunch",   "10:00:00", "13:00:00", "12:00:00",  90, 40, 1, _ts, _ts },
                });

            // ── 2. Tables ─────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "tables",
                columns: new[] { "id", "number", "capacity", "min_capacity", "zone", "is_combinable", "is_active", "created_at", "updated_at" },
                columnTypes: new[] { "TEXT", "INTEGER", "INTEGER", "INTEGER", "TEXT", "INTEGER", "INTEGER", "TEXT", "TEXT" },
                values: new object[,]
                {
                    // Salle (5)
                    { _t1,  1,  2, 1, "Salle",     0, 1, _ts, _ts },
                    { _t2,  2,  2, 1, "Salle",     0, 1, _ts, _ts },
                    { _t3,  3,  4, 2, "Salle",     1, 1, _ts, _ts },
                    { _t4,  4,  4, 2, "Salle",     1, 1, _ts, _ts },
                    { _t5,  5,  6, 3, "Salle",     0, 1, _ts, _ts },
                    // Terrasse (3)
                    { _t6,  6,  2, 1, "Terrasse",  0, 1, _ts, _ts },
                    { _t7,  7,  4, 2, "Terrasse",  1, 1, _ts, _ts },
                    { _t8,  8,  6, 3, "Terrasse",  1, 1, _ts, _ts },
                    // Bar (2)
                    { _t9,  9,  2, 1, "Bar",       0, 1, _ts, _ts },
                    { _t10, 10, 4, 2, "Bar",       0, 1, _ts, _ts },
                    // Salon privé (1)
                    { _t11, 11, 12, 6, "SalonPrive", 0, 1, _ts, _ts },
                });

            // ── 3. Clients ────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "customers",
                columns: new[] { "id", "first_name", "last_name", "phone", "email", "is_blacklisted", "no_show_count", "late_cancel_count", "vip_level", "created_at", "updated_at" },
                columnTypes: new[] { "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "INTEGER", "INTEGER", "TEXT", "TEXT", "TEXT" },
                values: new object[,]
                {
                    { _marie,  "Marie",  "Dubois",  "0601010101", "marie.dubois@email.fr",   0, 0, 0, "None", _ts, _ts },
                    { _robert, "Robert", "Martin",  "0602020202", "robert.martin@email.fr",  0, 0, 0, "VIP",  _ts, _ts },
                    { _sophie, "Sophie", "Laurent", "0603030303", "sophie.laurent@email.fr", 0, 0, 0, "VVIP", _ts, _ts },
                    // 2 no-shows — le prochain déclenchera la blacklist automatique
                    { _lucas,  "Lucas",  "Bernard", "0604040404", null,                      0, 2, 0, "None", _ts, _ts },
                    // Blacklistée : 3 no-shows (RB-007 / RB-008)
                    { _anne,   "Anne",   "Petit",   "0605050505", "anne.petit@email.fr",     1, 3, 0, "None", _ts, _ts },
                    // 1 late cancel
                    { _thomas, "Thomas", "Moreau",  "0606060606", "thomas.moreau@email.fr",  0, 0, 1, "None", _ts, _ts },
                });

            // ── 4. Réservations ───────────────────────────────────────────────
            var cols = new[] {
                "id", "customer_id", "table_id", "service_id",
                "booking_date", "arrival_time", "guests_count",
                "status", "source",
                "special_requests", "has_allergy_alert", "is_celebration", "needs_high_chair",
                "late_cancel", "cancellation_reason",
                "created_at", "updated_at",
            };
            var types = new[] {
                "TEXT", "TEXT", "TEXT", "TEXT",
                "TEXT", "TEXT", "INTEGER",
                "TEXT", "TEXT",
                "TEXT", "INTEGER", "INTEGER", "INTEGER",
                "INTEGER", "TEXT",
                "TEXT", "TEXT",
            };

            // 2026-06-04 — Déjeuner — BK1 : T1, Marie — Completed (historique)
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk1, _marie, _t1, _dejeuner,
                "2026-06-04", "12:00:00", 1,
                "Completed", "Staff",
                null, 0, 0, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-04 — Dîner — BK2 : T3 Salle, Marie — Confirmed + HasAllergyAlert
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk2, _marie, _t3, _diner,
                "2026-06-04", "19:00:00", 3,
                "Confirmed", "Online",
                "Allergie aux arachides — pas de cacahuètes en amuse-bouche.", 1, 0, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-04 — Dîner — BK3 : T5 Salle, Robert VIP — Seated
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk3, _robert, _t5, _diner,
                "2026-06-04", "19:00:00", 5,
                "Seated", "Phone",
                null, 0, 0, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-04 — Dîner — BK4 : T7 Terrasse, Thomas — Confirmed
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk4, _thomas, _t7, _diner,
                "2026-06-04", "19:30:00", 3,
                "Confirmed", "Staff",
                null, 0, 0, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-04 — Dîner — BK5 : T8 Terrasse, Sophie VVIP — Pending + IsCelebration
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk5, _sophie, _t8, _diner,
                "2026-06-04", "20:00:00", 4,
                "Pending", "Online",
                "Anniversaire de mariage — prévoir quelques bougies si possible.", 0, 1, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-05 — Dîner — BK6 : T3 Salle, Marie — Pending + NeedsHighChair
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk6, _marie, _t3, _diner,
                "2026-06-05", "19:30:00", 2,
                "Pending", "Online",
                "Chaise bébé requise pour un enfant de 18 mois.", 0, 0, 1,
                0, null,
                _ts, _ts,
            });

            // 2026-06-05 — Dîner — BK7 : T11 Salon privé, Sophie VVIP — Confirmed grand groupe
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk7, _sophie, _t11, _diner,
                "2026-06-05", "20:00:00", 9,
                "Confirmed", "Phone",
                null, 0, 0, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-06 — Déjeuner — BK8 : T7 Terrasse, Lucas — Pending Phone
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk8, _lucas, _t7, _dejeuner,
                "2026-06-06", "12:30:00", 3,
                "Pending", "Phone",
                null, 0, 0, 0,
                0, null,
                _ts, _ts,
            });

            // 2026-06-10 — Dîner — BK9 : T5 Salle, Robert VIP — Pending Online (horizon VIP = 180j ✓)
            migrationBuilder.InsertData("bookings", cols, types, new object[]
            {
                _bk9, _robert, _t5, _diner,
                "2026-06-10", "19:30:00", 4,
                "Pending", "Online",
                null, 0, 0, 0,
                0, null,
                _ts, _ts,
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM bookings WHERE id IN (
                    'd4d4d4d4-0000-0000-0000-000000000001','d4d4d4d4-0000-0000-0000-000000000002',
                    'd4d4d4d4-0000-0000-0000-000000000003','d4d4d4d4-0000-0000-0000-000000000004',
                    'd4d4d4d4-0000-0000-0000-000000000005','d4d4d4d4-0000-0000-0000-000000000006',
                    'd4d4d4d4-0000-0000-0000-000000000007','d4d4d4d4-0000-0000-0000-000000000008',
                    'd4d4d4d4-0000-0000-0000-000000000009');
                DELETE FROM customers WHERE id IN (
                    'c3c3c3c3-0000-0000-0000-000000000001','c3c3c3c3-0000-0000-0000-000000000002',
                    'c3c3c3c3-0000-0000-0000-000000000003','c3c3c3c3-0000-0000-0000-000000000004',
                    'c3c3c3c3-0000-0000-0000-000000000005','c3c3c3c3-0000-0000-0000-000000000006');
                DELETE FROM tables WHERE id IN (
                    'b2b2b2b2-0000-0000-0000-000000000001','b2b2b2b2-0000-0000-0000-000000000002',
                    'b2b2b2b2-0000-0000-0000-000000000003','b2b2b2b2-0000-0000-0000-000000000004',
                    'b2b2b2b2-0000-0000-0000-000000000005','b2b2b2b2-0000-0000-0000-000000000006',
                    'b2b2b2b2-0000-0000-0000-000000000007','b2b2b2b2-0000-0000-0000-000000000008',
                    'b2b2b2b2-0000-0000-0000-000000000009','b2b2b2b2-0000-0000-0000-000000000010',
                    'b2b2b2b2-0000-0000-0000-000000000011');
                DELETE FROM dining_services WHERE id IN (
                    'a1a1a1a1-0000-0000-0000-000000000001','a1a1a1a1-0000-0000-0000-000000000002',
                    'a1a1a1a1-0000-0000-0000-000000000003');");
        }
    }
}
