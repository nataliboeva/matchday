using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchDay.Notifications.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ETickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    MatchTitle = table.Column<string>(type: "character varying(130)", maxLength: 130, nullable: false),
                    KickOff = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Stadium = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Section = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Seats = table.Column<List<string>>(type: "text[]", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ETickets_BookingId",
                table: "ETickets",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ETickets_TicketCode",
                table: "ETickets",
                column: "TicketCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ETickets");
        }
    }
}
