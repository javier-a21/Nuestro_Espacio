using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooperativa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EventHandled",
                table: "RoomStates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EventSince",
                table: "RoomStates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventStage",
                table: "RoomStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeatherEvent",
                table: "RoomStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventHandled",
                table: "RoomStates");

            migrationBuilder.DropColumn(
                name: "EventSince",
                table: "RoomStates");

            migrationBuilder.DropColumn(
                name: "EventStage",
                table: "RoomStates");

            migrationBuilder.DropColumn(
                name: "WeatherEvent",
                table: "RoomStates");
        }
    }
}
