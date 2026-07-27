using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSupersets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntervalSeconds",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersetGroupId",
                table: "WorkoutExercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupersetPosition",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntervalSeconds",
                table: "PlannedExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersetGroupId",
                table: "PlannedExercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupersetPosition",
                table: "PlannedExercises",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntervalSeconds",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "SupersetGroupId",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "SupersetPosition",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "IntervalSeconds",
                table: "PlannedExercises");

            migrationBuilder.DropColumn(
                name: "SupersetGroupId",
                table: "PlannedExercises");

            migrationBuilder.DropColumn(
                name: "SupersetPosition",
                table: "PlannedExercises");
        }
    }
}
