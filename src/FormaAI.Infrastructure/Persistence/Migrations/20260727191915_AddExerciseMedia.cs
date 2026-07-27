using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaAttribution",
                table: "Exercises",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaContentType",
                table: "Exercises",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaExternalUrl",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaSourceUrl",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaStorageName",
                table: "Exercises",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl", "MediaStorageName" },
                values: new object[] { "Wensceslao · CC BY-SA 4.0", "image/gif", "https://commons.wikimedia.org/wiki/Special:Redirect/file/Squats.gif", "https://commons.wikimedia.org/wiki/File:Squats.gif", null });

            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl", "MediaStorageName" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl", "MediaStorageName" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl", "MediaStorageName" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl", "MediaStorageName" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl", "MediaStorageName" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaAttribution",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MediaContentType",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MediaExternalUrl",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MediaSourceUrl",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MediaStorageName",
                table: "Exercises");
        }
    }
}
