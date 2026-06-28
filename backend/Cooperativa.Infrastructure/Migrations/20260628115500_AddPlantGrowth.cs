using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooperativa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantGrowth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Daily",
                table: "ActionTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CooperativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Species = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    GrowthStage = table.Column<int>(type: "integer", nullable: false),
                    Health = table.Column<int>(type: "integer", nullable: false),
                    ActionsCount = table.Column<int>(type: "integer", nullable: false),
                    NotesCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_Cooperatives_CooperativeId",
                        column: x => x.CooperativeId,
                        principalTable: "Cooperatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ActionTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Daily",
                value: true);

            migrationBuilder.UpdateData(
                table: "ActionTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Daily",
                value: true);

            migrationBuilder.InsertData(
                table: "ActionTypes",
                columns: new[] { "Id", "Active", "Code", "Daily", "Name", "RequiredRole" },
                values: new object[] { 3, true, "ABONO", false, "Abonar", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Plants_CooperativeId_MaturedAt",
                table: "Plants",
                columns: new[] { "CooperativeId", "MaturedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DeleteData(
                table: "ActionTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "Daily",
                table: "ActionTypes");
        }
    }
}
