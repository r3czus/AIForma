using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPullupExerciseMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl" },
                values: new object[] { "Extremistpullup · domena publiczna", "image/gif", "https://commons.wikimedia.org/wiki/Special:Redirect/file/Weighted%2C_wide-grip_pullup_video.gif", "https://commons.wikimedia.org/wiki/File:Weighted,_wide-grip_pullup_video.gif" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "MediaAttribution", "MediaContentType", "MediaExternalUrl", "MediaSourceUrl" },
                values: new object[] { null, null, null, null });
        }
    }
}
