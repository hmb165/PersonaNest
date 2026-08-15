using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonaNest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTasteProfileAiNarrative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiNarrative",
                table: "TasteProfiles",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiNarrativeGeneratedAt",
                table: "TasteProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiNarrative",
                table: "TasteProfiles");

            migrationBuilder.DropColumn(
                name: "AiNarrativeGeneratedAt",
                table: "TasteProfiles");
        }
    }
}
