using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PersonaNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Themes",
                columns: new[] { "Id", "Description", "IsDefault", "Name", "PrimaryDimHex", "PrimaryHex" },
                values: new object[,]
                {
                    { 9, "Rich and thoughtful.", false, "Indigo", "#4547c4", "#6366f1" },
                    { 10, "Soft and expressive.", false, "Rose", "#c2293f", "#f43f5e" },
                    { 11, "Sharp and lively.", false, "Lime", "#65990f", "#84cc16" },
                    { 12, "Crisp and modern.", false, "Cyan", "#0589a0", "#06b6d4" },
                    { 13, "Playful and vivid.", false, "Fuchsia", "#ab2cc0", "#d946ef" },
                    { 14, "Muted and understated.", false, "Slate", "#47536b", "#64748b" },
                    { 15, "Bright and cheerful.", false, "Yellow", "#b58607", "#eab308" },
                    { 16, "Light and airy.", false, "Sky", "#0b7fb5", "#0ea5e9" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
