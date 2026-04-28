using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemmoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskGroupId",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            // Backfill: existing rows are their own group
            migrationBuilder.Sql("UPDATE [Tasks] SET [TaskGroupId] = [Id] WHERE [TaskGroupId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskGroupId",
                table: "Tasks");
        }
    }
}
