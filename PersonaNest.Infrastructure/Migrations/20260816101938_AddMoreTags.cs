using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PersonaNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 13, "Nostalgic", "NOSTALGIC" },
                    { 14, "Binge-Worthy", "BINGE-WORTHY" },
                    { 15, "Slow Burn", "SLOW BURN" },
                    { 16, "Plot Twist", "PLOT TWIST" },
                    { 17, "Feel-Good", "FEEL-GOOD" },
                    { 18, "Dark & Gritty", "DARK & GRITTY" },
                    { 19, "Character-Driven", "CHARACTER-DRIVEN" },
                    { 20, "Visually Stunning", "VISUALLY STUNNING" },
                    { 21, "Guilty Pleasure", "GUILTY PLEASURE" },
                    { 22, "Cult Classic", "CULT CLASSIC" },
                    { 23, "Tearjerker", "TEARJERKER" },
                    { 24, "Mind-Bending", "MIND-BENDING" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 24);
        }
    }
}
