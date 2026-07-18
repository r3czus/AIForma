using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyCheckIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Energy = table.Column<int>(type: "int", nullable: false),
                    Sleep = table.Column<int>(type: "int", nullable: false),
                    Hunger = table.Column<int>(type: "int", nullable: false),
                    Recovery = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyCheckIns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCheckIns_UserId_LocalDate",
                table: "WeeklyCheckIns",
                columns: new[] { "UserId", "LocalDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyCheckIns");
        }
    }
}
