using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAppPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultRepetitions",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "DefaultRestSeconds",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<int>(
                name: "DefaultSets",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "MealReminderMinutesBefore",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MealRemindersEnabled",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MealSchedule",
                table: "UserProfiles",
                type: "nvarchar(800)",
                maxLength: 800,
                nullable: false,
                defaultValue: "Śniadanie~07:00~1|Lunch~12:00~1|Obiad~15:00~1|Przekąska~18:00~1|Kolacja~21:00~1");

            migrationBuilder.AddColumn<string>(
                name: "ThemePreference",
                table: "UserProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.AddColumn<int>(
                name: "TrainingActivityLevel",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeekStartsOn",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WorkActivityLevel",
                table: "UserProfiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRepetitions",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultRestSeconds",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultSets",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MealReminderMinutesBefore",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MealRemindersEnabled",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MealSchedule",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ThemePreference",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TrainingActivityLevel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeekStartsOn",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WorkActivityLevel",
                table: "UserProfiles");
        }
    }
}
