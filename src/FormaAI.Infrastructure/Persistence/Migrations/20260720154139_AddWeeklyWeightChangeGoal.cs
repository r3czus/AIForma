using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyWeightChangeGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyWeightChangeKg",
                table: "UserProfiles",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0.5m);

            migrationBuilder.Sql("UPDATE [UserProfiles] SET [WeeklyWeightChangeKg] = 0 WHERE [Goal] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyWeightChangeKg",
                table: "UserProfiles");
        }
    }
}
