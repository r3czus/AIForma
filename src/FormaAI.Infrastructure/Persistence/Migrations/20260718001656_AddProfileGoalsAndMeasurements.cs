using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileGoalsAndMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityLevel",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "UserProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalorieToleranceKcal",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "Sex",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetWeightKg",
                table: "UserProfiles",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ArmCm",
                table: "BodyMeasurements",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChestCm",
                table: "BodyMeasurements",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HipsCm",
                table: "BodyMeasurements",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThighCm",
                table: "BodyMeasurements",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CalorieToleranceKcal",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TargetWeightKg",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ArmCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "ChestCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "HipsCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "ThighCm",
                table: "BodyMeasurements");
        }
    }
}
