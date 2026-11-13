using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleTimeSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Schedules",
                newName: "StartAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndAt",
                table: "Schedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "Bookings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ScheduleId",
                table: "Bookings",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Schedules_ScheduleId",
                table: "Bookings",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Schedules_ScheduleId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ScheduleId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "Schedules",
                newName: "Date");
        }
    }
}
