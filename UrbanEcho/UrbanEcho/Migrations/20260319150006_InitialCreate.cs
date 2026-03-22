using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanEcho.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntersectionReport",
                columns: table => new
                {
                    IntersectionReportId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntersectionReport", x => x.IntersectionReportId);
                });

            migrationBuilder.CreateTable(
                name: "RoadEdgeReport",
                columns: table => new
                {
                    RoadEdgeReportId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadEdgeReport", x => x.RoadEdgeReportId);
                });

            migrationBuilder.CreateTable(
                name: "IntersectionReportModel",
                columns: table => new
                {
                    IntersectionReportModelId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntersectionName = table.Column<string>(type: "TEXT", nullable: false),
                    AverageTimeSpent = table.Column<double>(type: "REAL", nullable: false),
                    TotalTimeSpent = table.Column<double>(type: "REAL", nullable: false),
                    AverageSpeed = table.Column<double>(type: "REAL", nullable: false),
                    AverageWaitTime = table.Column<double>(type: "REAL", nullable: false),
                    TotalWaitTime = table.Column<double>(type: "REAL", nullable: false),
                    VehicleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lon = table.Column<double>(type: "REAL", nullable: false),
                    IntersectionReportId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntersectionReportModel", x => x.IntersectionReportModelId);
                    table.ForeignKey(
                        name: "FK_IntersectionReportModel_IntersectionReport_IntersectionReportId",
                        column: x => x.IntersectionReportId,
                        principalTable: "IntersectionReport",
                        principalColumn: "IntersectionReportId");
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RoadEdgeReportId = table.Column<int>(type: "INTEGER", nullable: false),
                    IntersectionReportId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_Reports_IntersectionReport_IntersectionReportId",
                        column: x => x.IntersectionReportId,
                        principalTable: "IntersectionReport",
                        principalColumn: "IntersectionReportId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_RoadEdgeReport_RoadEdgeReportId",
                        column: x => x.RoadEdgeReportId,
                        principalTable: "RoadEdgeReport",
                        principalColumn: "RoadEdgeReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadEdgeReportModel",
                columns: table => new
                {
                    RoadEdgeReportModelId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AverageTimeSpent = table.Column<double>(type: "REAL", nullable: false),
                    TotalTimeSpent = table.Column<double>(type: "REAL", nullable: false),
                    AverageSpeed = table.Column<double>(type: "REAL", nullable: false),
                    AverageWaitTime = table.Column<double>(type: "REAL", nullable: false),
                    TotalWaitTime = table.Column<double>(type: "REAL", nullable: false),
                    VehicleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lon = table.Column<double>(type: "REAL", nullable: false),
                    IntersectionReportModelId = table.Column<int>(type: "INTEGER", nullable: true),
                    RoadEdgeReportId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadEdgeReportModel", x => x.RoadEdgeReportModelId);
                    table.ForeignKey(
                        name: "FK_RoadEdgeReportModel_IntersectionReportModel_IntersectionReportModelId",
                        column: x => x.IntersectionReportModelId,
                        principalTable: "IntersectionReportModel",
                        principalColumn: "IntersectionReportModelId");
                    table.ForeignKey(
                        name: "FK_RoadEdgeReportModel_RoadEdgeReport_RoadEdgeReportId",
                        column: x => x.RoadEdgeReportId,
                        principalTable: "RoadEdgeReport",
                        principalColumn: "RoadEdgeReportId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntersectionReportModel_IntersectionReportId",
                table: "IntersectionReportModel",
                column: "IntersectionReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_IntersectionReportId",
                table: "Reports",
                column: "IntersectionReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_RoadEdgeReportId",
                table: "Reports",
                column: "RoadEdgeReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadEdgeReportModel_IntersectionReportModelId",
                table: "RoadEdgeReportModel",
                column: "IntersectionReportModelId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadEdgeReportModel_RoadEdgeReportId",
                table: "RoadEdgeReportModel",
                column: "RoadEdgeReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "RoadEdgeReportModel");

            migrationBuilder.DropTable(
                name: "IntersectionReportModel");

            migrationBuilder.DropTable(
                name: "RoadEdgeReport");

            migrationBuilder.DropTable(
                name: "IntersectionReport");
        }
    }
}
