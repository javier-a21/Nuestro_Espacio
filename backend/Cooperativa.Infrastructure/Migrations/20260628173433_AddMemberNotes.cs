using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooperativa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoteAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoteText",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoteAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoteText",
                table: "AspNetUsers");
        }
    }
}
