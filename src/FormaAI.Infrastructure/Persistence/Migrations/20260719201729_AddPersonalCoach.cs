using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalCoach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShortened",
                table: "WorkoutSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                table: "WorkoutSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRemindersPerDay",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "MeasurementRemindersEnabled",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursEnd",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(7, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursStart",
                table: "UserProfiles",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(22, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "TrainingRemindersEnabled",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WeeklySummaryRemindersEnabled",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ExerciseProgressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedWeightKg = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MinReps = table.Column<int>(type: "int", nullable: false),
                    MaxReps = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    AcceptedWeightKg = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseProgressions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionDayReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionDayReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Pose = table.Column<int>(type: "int", nullable: false),
                    StorageName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressPhotos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    P256Dh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingScheduleExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TrainingDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NewDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScheduleExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WeekStarting = table.Column<DateOnly>(type: "date", nullable: false),
                    Energy = table.Column<int>(type: "int", nullable: false),
                    Sleep = table.Column<int>(type: "int", nullable: false),
                    Hunger = table.Column<int>(type: "int", nullable: false),
                    Recovery = table.Column<int>(type: "int", nullable: false),
                    Stress = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviationReasons = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseProgressions_UserId_ExerciseId_Decision",
                table: "ExerciseProgressions",
                columns: new[] { "UserId", "ExerciseId", "Decision" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_UserId_LocalDate_EventKey",
                table: "NotificationDeliveries",
                columns: new[] { "UserId", "LocalDate", "EventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionDayReviews_UserId_LocalDate",
                table: "NutritionDayReviews",
                columns: new[] { "UserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_UserId_LocalDate_Pose",
                table: "ProgressPhotos",
                columns: new[] { "UserId", "LocalDate", "Pose" });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_Endpoint",
                table: "PushSubscriptions",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScheduleExceptions_UserId_OriginalDate_TrainingDayId",
                table: "TrainingScheduleExceptions",
                columns: new[] { "UserId", "OriginalDate", "TrainingDayId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviews_UserId_WeekStarting",
                table: "WeeklyReviews",
                columns: new[] { "UserId", "WeekStarting" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseProgressions");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "NutritionDayReviews");

            migrationBuilder.DropTable(
                name: "ProgressPhotos");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "TrainingScheduleExceptions");

            migrationBuilder.DropTable(
                name: "WeeklyReviews");

            migrationBuilder.DropColumn(
                name: "IsShortened",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "MaxRemindersPerDay",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MeasurementRemindersEnabled",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "QuietHoursEnd",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "QuietHoursStart",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TrainingRemindersEnabled",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeeklySummaryRemindersEnabled",
                table: "UserProfiles");
        }
    }
}
