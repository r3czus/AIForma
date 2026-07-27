using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMealCopyOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CopyOperationId",
                table: "Meals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meals_UserId_CopyOperationId",
                table: "Meals",
                columns: new[] { "UserId", "CopyOperationId" },
                unique: true,
                filter: "[CopyOperationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Meals_UserId_CopyOperationId",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "CopyOperationId",
                table: "Meals");
        }
    }
}
