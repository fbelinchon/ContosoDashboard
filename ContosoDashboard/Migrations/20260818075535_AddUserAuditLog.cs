using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAuditLogs",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuditLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_UserAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_Action",
                table: "UserAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_AdminUserId",
                table: "UserAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_Timestamp",
                table: "UserAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogs_UserId",
                table: "UserAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAuditLogs");
        }
    }
}
