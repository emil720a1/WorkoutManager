using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAthleteUsernameAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Athletes_TelegramId",
                table: "Athletes");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Athletes",
                type: "text",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Athletes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_TelegramId",
                table: "Athletes",
                column: "TelegramId",
                unique: true,
                filter: "\"TelegramId\" != 0");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_Username",
                table: "Athletes",
                column: "Username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Athletes_TelegramId",
                table: "Athletes");

            migrationBuilder.DropIndex(
                name: "IX_Athletes_Username",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Athletes");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_TelegramId",
                table: "Athletes",
                column: "TelegramId",
                unique: true);
        }
    }
}
