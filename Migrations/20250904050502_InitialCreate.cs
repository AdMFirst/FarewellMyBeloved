using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarewellMyBeloved.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FarewellPersonId = table.Column<int>(type: "int", nullable: true),
                    FarewellMessageId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarewellPeople",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    PortraitUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BackgroundUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarewellPeople", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModeratorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModeratorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ContentReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeratorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModeratorLogs_ContentReports_ContentReportId",
                        column: x => x.ContentReportId,
                        principalTable: "ContentReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarewellMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarewellPersonId = table.Column<int>(type: "int", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuthorEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarewellMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarewellMessages_FarewellPeople_FarewellPersonId",
                        column: x => x.FarewellPersonId,
                        principalTable: "FarewellPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_CreatedAt",
                table: "ContentReports",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_FarewellMessageId",
                table: "ContentReports",
                column: "FarewellMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_FarewellPersonId",
                table: "ContentReports",
                column: "FarewellPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_Reason",
                table: "ContentReports",
                column: "Reason");

            migrationBuilder.CreateIndex(
                name: "IX_FarewellMessages_CreatedAt",
                table: "FarewellMessages",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_FarewellMessages_FarewellPersonId",
                table: "FarewellMessages",
                column: "FarewellPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_FarewellPeople_Slug",
                table: "FarewellPeople",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModeratorLogs_Action",
                table: "ModeratorLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ModeratorLogs_ContentReportId",
                table: "ModeratorLogs",
                column: "ContentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeratorLogs_CreatedAt",
                table: "ModeratorLogs",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ModeratorLogs_ModeratorName",
                table: "ModeratorLogs",
                column: "ModeratorName");

            migrationBuilder.CreateIndex(
                name: "IX_ModeratorLogs_TargetId",
                table: "ModeratorLogs",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeratorLogs_TargetType",
                table: "ModeratorLogs",
                column: "TargetType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarewellMessages");

            migrationBuilder.DropTable(
                name: "ModeratorLogs");

            migrationBuilder.DropTable(
                name: "FarewellPeople");

            migrationBuilder.DropTable(
                name: "ContentReports");
        }
    }
}
