using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calgary311.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceRequestId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AgencyResponsible = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    CommunityCode = table.Column<string>(type: "TEXT", nullable: true),
                    CommunityName = table.Column<string>(type: "TEXT", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_CommunityName",
                table: "ServiceRequests",
                column: "CommunityName");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_RequestedDate",
                table: "ServiceRequests",
                column: "RequestedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ServiceName",
                table: "ServiceRequests",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ServiceRequestId",
                table: "ServiceRequests",
                column: "ServiceRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Status",
                table: "ServiceRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRequests");
        }
    }
}
