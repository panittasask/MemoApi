using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemmoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStartTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StartTime",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Tasks");
        }
    }
}
