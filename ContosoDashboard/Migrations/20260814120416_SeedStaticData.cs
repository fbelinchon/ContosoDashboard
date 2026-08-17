using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoDashboard.Migrations
{
    /// <inheritdoc />
    public partial class SeedStaticData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Announcements",
                keyColumn: "AnnouncementId",
                keyValue: 1,
                columns: new[] { "ExpiryDate", "PublishDate" },
                values: new object[] { new DateTime(2026, 1, 31, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "ProjectMembers",
                keyColumn: "ProjectMemberId",
                keyValue: 1,
                column: "AssignedDate",
                value: new DateTime(2025, 12, 2, 1, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "ProjectMembers",
                keyColumn: "ProjectMemberId",
                keyValue: 2,
                column: "AssignedDate",
                value: new DateTime(2025, 12, 2, 1, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "ProjectId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "StartDate", "TargetCompletionDate", "UpdatedDate" },
                values: new object[] { new DateTime(2025, 12, 2, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 12, 2, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 2, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "DueDate", "UpdatedDate" },
                values: new object[] { new DateTime(2025, 12, 2, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 12, 12, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 12, 12, 1, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "DueDate", "UpdatedDate" },
                values: new object[] { new DateTime(2025, 12, 7, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 6, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 3,
                columns: new[] { "CreatedDate", "DueDate", "UpdatedDate" },
                values: new object[] { new DateTime(2025, 12, 12, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 11, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2025, 12, 12, 1, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Local));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Announcements",
                keyColumn: "AnnouncementId",
                keyValue: 1,
                columns: new[] { "ExpiryDate", "PublishDate" },
                values: new object[] { new DateTime(2026, 9, 13, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(7982), new DateTime(2026, 8, 14, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(7875) });

            migrationBuilder.UpdateData(
                table: "ProjectMembers",
                keyColumn: "ProjectMemberId",
                keyValue: 1,
                column: "AssignedDate",
                value: new DateTime(2026, 7, 15, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6989));

            migrationBuilder.UpdateData(
                table: "ProjectMembers",
                keyColumn: "ProjectMemberId",
                keyValue: 2,
                column: "AssignedDate",
                value: new DateTime(2026, 7, 15, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(7100));

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "ProjectId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "StartDate", "TargetCompletionDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 15, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(4161), new DateTime(2026, 7, 15, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(3641), new DateTime(2026, 10, 13, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(3834), new DateTime(2026, 8, 14, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(4272) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "DueDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 15, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(5921), new DateTime(2026, 7, 25, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(5470), new DateTime(2026, 7, 25, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6028) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "DueDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 20, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6138), new DateTime(2026, 8, 19, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6137), new DateTime(2026, 8, 14, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6139) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 3,
                columns: new[] { "CreatedDate", "DueDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 25, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6141), new DateTime(2026, 8, 24, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6140), new DateTime(2026, 7, 25, 12, 3, 9, 114, DateTimeKind.Utc).AddTicks(6141) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 14, 12, 3, 9, 113, DateTimeKind.Utc).AddTicks(8701));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 14, 12, 3, 9, 113, DateTimeKind.Utc).AddTicks(9079));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 14, 12, 3, 9, 113, DateTimeKind.Utc).AddTicks(9081));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 14, 12, 3, 9, 113, DateTimeKind.Utc).AddTicks(9083));
        }
    }
}
