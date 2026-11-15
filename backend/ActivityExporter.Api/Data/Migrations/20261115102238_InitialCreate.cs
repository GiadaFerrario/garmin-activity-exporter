using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActivityExporter.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GarminActivityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Sport = table.Column<string>(type: "TEXT", nullable: false),
                    TotalTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    DistanceInMeters = table.Column<double>(type: "REAL", nullable: true),
                    ElevationGain = table.Column<int>(type: "INTEGER", nullable: true),
                    AvgHR = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxHR = table.Column<int>(type: "INTEGER", nullable: true),
                    Calories = table.Column<int>(type: "INTEGER", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Splits = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_GarminActivityId",
                table: "Activities",
                column: "GarminActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_StartTimeUtc",
                table: "Activities",
                column: "StartTimeUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");
        }
    }
}
