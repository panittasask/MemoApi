using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemmoApi.Migrations
{
    /// <inheritdoc />
    public partial class insertUserIdinSettings1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SettingParents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SettingChildren",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_CLIENT",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_INTERNAL",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_MAINTENANCE",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_RESEARCH",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_BLOCKED",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_DONE",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_INPROGRESS",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_TODO",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingParents",
                keyColumn: "Id",
                keyValue: "SET_PARENT_PROJECT",
                column: "UserId",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingParents",
                keyColumn: "Id",
                keyValue: "SET_PARENT_STATUS",
                column: "UserId",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SettingParents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SettingChildren");
        }
    }
}
