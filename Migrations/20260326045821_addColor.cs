using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemmoApi.Migrations
{
    /// <inheritdoc />
    public partial class addColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "SettingChildren",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_CLIENT",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_INTERNAL",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_MAINTENANCE",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_PROJECT_RESEARCH",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_BLOCKED",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_DONE",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_INPROGRESS",
                column: "Color",
                value: "");

            migrationBuilder.UpdateData(
                table: "SettingChildren",
                keyColumn: "Id",
                keyValue: "SET_CHILD_STATUS_TODO",
                column: "Color",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "SettingChildren");
        }
    }
}
