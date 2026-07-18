using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormaAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairWeeklyCheckInsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[WeeklyCheckIns]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [WeeklyCheckIns] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        [LocalDate] date NOT NULL,
                        [Energy] int NOT NULL,
                        [Sleep] int NOT NULL,
                        [Hunger] int NOT NULL,
                        [Recovery] int NOT NULL,
                        [Notes] nvarchar(500) NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_WeeklyCheckIns] PRIMARY KEY ([Id])
                    );

                    CREATE UNIQUE INDEX [IX_WeeklyCheckIns_UserId_LocalDate]
                        ON [WeeklyCheckIns] ([UserId], [LocalDate]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
