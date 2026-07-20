using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseMuscleEngagements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseMuscleEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuscleGroup = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseMuscleEngagements", x => x.Id);
                    table.CheckConstraint("CK_ExerciseMuscleEngagements_Percentage", "[Percentage] >= 1 AND [Percentage] <= 100");
                    table.ForeignKey(
                        name: "FK_ExerciseMuscleEngagements_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExerciseMuscleEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuscleGroup = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExerciseMuscleEngagements", x => x.Id);
                    table.CheckConstraint("CK_WorkoutExerciseMuscleEngagements_Percentage", "[Percentage] >= 1 AND [Percentage] <= 100");
                    table.ForeignKey(
                        name: "FK_WorkoutExerciseMuscleEngagements_WorkoutExercises_WorkoutExerciseId",
                        column: x => x.WorkoutExerciseId,
                        principalTable: "WorkoutExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMuscleEngagements_ExerciseId_MuscleGroup",
                table: "ExerciseMuscleEngagements",
                columns: new[] { "ExerciseId", "MuscleGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExerciseMuscleEngagements_WorkoutExerciseId_MuscleGroup",
                table: "WorkoutExerciseMuscleEngagements",
                columns: new[] { "WorkoutExerciseId", "MuscleGroup" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO ExerciseMuscleEngagements (Id, ExerciseId, MuscleGroup, Percentage)
                SELECT NEWID(), Id, PrimaryMuscleGroup, 100 FROM Exercises;

                INSERT INTO WorkoutExerciseMuscleEngagements (Id, WorkoutExerciseId, MuscleGroup, Percentage)
                SELECT NEWID(), workout.Id, COALESCE(exercise.PrimaryMuscleGroup, 10), 100
                FROM WorkoutExercises workout
                LEFT JOIN Exercises exercise ON exercise.Id = workout.ExerciseId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseMuscleEngagements");

            migrationBuilder.DropTable(
                name: "WorkoutExerciseMuscleEngagements");
        }
    }
}
