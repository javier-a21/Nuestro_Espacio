using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooperativa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyBloom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyBlooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CooperativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    Weather = table.Column<int>(type: "integer", nullable: false),
                    Streak = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyBlooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyBlooms_Cooperatives_CooperativeId",
                        column: x => x.CooperativeId,
                        principalTable: "Cooperatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyBlooms_CooperativeId_Date",
                table: "DailyBlooms",
                columns: new[] { "CooperativeId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyBlooms");
        }
    }
}
