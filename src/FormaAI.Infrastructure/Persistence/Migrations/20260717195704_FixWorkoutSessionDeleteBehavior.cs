using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkoutSessionDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_TrainingDays_TrainingDayId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_TrainingPlans_TrainingPlanId",
                table: "WorkoutSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_TrainingDays_TrainingDayId",
                table: "WorkoutSessions",
                column: "TrainingDayId",
                principalTable: "TrainingDays",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_TrainingPlans_TrainingPlanId",
                table: "WorkoutSessions",
                column: "TrainingPlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_TrainingDays_TrainingDayId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_TrainingPlans_TrainingPlanId",
                table: "WorkoutSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_TrainingDays_TrainingDayId",
                table: "WorkoutSessions",
                column: "TrainingDayId",
                principalTable: "TrainingDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_TrainingPlans_TrainingPlanId",
                table: "WorkoutSessions",
                column: "TrainingPlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
