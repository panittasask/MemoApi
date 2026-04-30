using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemmoApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkNoteOptionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "WorkNotes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "WorkNotes");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "WorkNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "WorkNotes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "WorkNotes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "WorkNotes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
