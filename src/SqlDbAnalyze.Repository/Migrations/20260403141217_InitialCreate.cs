using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlDbAnalyze.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegisteredServer",
                columns: table => new
                {
                    RegisteredServerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubscriptionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceGroupName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ServerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredServer", x => x.RegisteredServerId);
                });

            migrationBuilder.CreateTable(
                name: "CachedDtuMetric",
                columns: table => new
                {
                    CachedDtuMetricId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegisteredServerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DatabaseName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DtuPercentage = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedDtuMetric", x => x.CachedDtuMetricId);
                    table.ForeignKey(
                        name: "FK_CachedDtuMetric_RegisteredServer_RegisteredServerId",
                        column: x => x.RegisteredServerId,
                        principalTable: "RegisteredServer",
                        principalColumn: "RegisteredServerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedDtuMetric_RegisteredServerId",
                table: "CachedDtuMetric",
                column: "RegisteredServerId");

            migrationBuilder.CreateIndex(
                name: "UQ_CachedDtuMetric_Server_Database_Timestamp",
                table: "CachedDtuMetric",
                columns: new[] { "RegisteredServerId", "DatabaseName", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_RegisteredServer_SubscriptionId_ResourceGroupName_ServerName",
                table: "RegisteredServer",
                columns: new[] { "SubscriptionId", "ResourceGroupName", "ServerName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedDtuMetric");

            migrationBuilder.DropTable(
                name: "RegisteredServer");
        }
    }
}
