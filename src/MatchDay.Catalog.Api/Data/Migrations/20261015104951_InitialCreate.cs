using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MatchDay.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stadiums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stadiums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HomeTeam = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    AwayTeam = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Competition = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    KickOff = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StadiumId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StadiumId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Rows = table.Column<int>(type: "integer", nullable: false),
                    SeatsPerRow = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.CheckConstraint("CK_Section_Layout", "\"Rows\" > 0 AND \"SeatsPerRow\" > 0");
                    table.ForeignKey(
                        name: "FK_Sections_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionPrices",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionPrices", x => new { x.MatchId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_SectionPrices_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionPrices_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Stadiums",
                columns: new[] { "Id", "City", "Name" },
                values: new object[,]
                {
                    { 1, "Plovdiv", "Trimontium Arena" },
                    { 2, "Sofia", "Vitosha Park" },
                    { 3, "Varna", "Seafront Stadium" }
                });

            migrationBuilder.InsertData(
                table: "Matches",
                columns: new[] { "Id", "AwayTeam", "Competition", "HomeTeam", "KickOff", "StadiumId" },
                values: new object[,]
                {
                    { 1, "Vitosha Sofia", "First League", "Trimontium Plovdiv", new DateTimeOffset(new DateTime(2026, 10, 31, 17, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { 2, "Danube Ruse", "First League", "Varna Mariners", new DateTimeOffset(new DateTime(2026, 11, 7, 13, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3 },
                    { 3, "Rhodope Eagles", "First League", "Vitosha Sofia", new DateTimeOffset(new DateTime(2026, 11, 21, 15, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2 },
                    { 4, "Varna Mariners", "First League", "Trimontium Plovdiv", new DateTimeOffset(new DateTime(2026, 11, 28, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { 5, "Trimontium Plovdiv", "First League", "Vitosha Sofia", new DateTimeOffset(new DateTime(2027, 3, 6, 16, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2 },
                    { 6, "Vitosha Sofia", "First League", "Varna Mariners", new DateTimeOffset(new DateTime(2027, 4, 17, 15, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3 },
                    { 7, "Danube Ruse", "First League", "Trimontium Plovdiv", new DateTimeOffset(new DateTime(2027, 5, 22, 16, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 }
                });

            migrationBuilder.InsertData(
                table: "Sections",
                columns: new[] { "Id", "Name", "Rows", "SeatsPerRow", "StadiumId" },
                values: new object[,]
                {
                    { 1, "North Stand", 18, 30, 1 },
                    { 2, "South Stand", 18, 30, 1 },
                    { 3, "Main Tribune", 12, 40, 1 },
                    { 4, "VIP Lounge", 4, 20, 1 },
                    { 5, "North Stand", 20, 32, 2 },
                    { 6, "South Stand", 20, 32, 2 },
                    { 7, "Main Tribune", 14, 40, 2 },
                    { 8, "VIP Lounge", 5, 20, 2 },
                    { 9, "North Stand", 16, 28, 3 },
                    { 10, "South Stand", 16, 28, 3 },
                    { 11, "Main Tribune", 12, 36, 3 },
                    { 12, "VIP Lounge", 4, 16, 3 }
                });

            migrationBuilder.InsertData(
                table: "SectionPrices",
                columns: new[] { "MatchId", "SectionId", "Amount" },
                values: new object[,]
                {
                    { 1, 1, 21m },
                    { 1, 2, 21m },
                    { 1, 3, 36m },
                    { 1, 4, 83m },
                    { 2, 9, 12m },
                    { 2, 10, 12m },
                    { 2, 11, 22m },
                    { 2, 12, 50m },
                    { 3, 5, 15m },
                    { 3, 6, 15m },
                    { 3, 7, 26m },
                    { 3, 8, 60m },
                    { 4, 1, 17m },
                    { 4, 2, 17m },
                    { 4, 3, 29m },
                    { 4, 4, 66m },
                    { 5, 5, 23m },
                    { 5, 6, 23m },
                    { 5, 7, 39m },
                    { 5, 8, 90m },
                    { 6, 9, 14m },
                    { 6, 10, 14m },
                    { 6, 11, 26m },
                    { 6, 12, 60m },
                    { 7, 1, 14m },
                    { 7, 2, 14m },
                    { 7, 3, 24m },
                    { 7, 4, 55m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_KickOff",
                table: "Matches",
                column: "KickOff");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StadiumId",
                table: "Matches",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionPrices_SectionId",
                table: "SectionPrices",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_StadiumId",
                table: "Sections",
                column: "StadiumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectionPrices");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Stadiums");
        }
    }
}
