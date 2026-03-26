using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MemmoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SettingParents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingParents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingChildren",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingChildren", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingChildren_SettingParents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SettingParents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SettingParents",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Key", "Name", "UpdateBy", "UpdateDate" },
                values: new object[,]
                {
                    { "SET_PARENT_PROJECT", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "project", "Project", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_PARENT_STATUS", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "status", "Status", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "SettingChildren",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Key", "Name", "ParentId", "UpdateBy", "UpdateDate" },
                values: new object[,]
                {
                    { "SET_CHILD_PROJECT_CLIENT", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "client", "Client", "SET_PARENT_PROJECT", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_PROJECT_INTERNAL", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "internal", "Internal", "SET_PARENT_PROJECT", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_PROJECT_MAINTENANCE", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "maintenance", "Maintenance", "SET_PARENT_PROJECT", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_PROJECT_RESEARCH", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "research", "Research", "SET_PARENT_PROJECT", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_STATUS_BLOCKED", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "blocked", "Blocked", "SET_PARENT_STATUS", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_STATUS_DONE", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "done", "Done", "SET_PARENT_STATUS", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_STATUS_INPROGRESS", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "inprogress", "In Progress", "SET_PARENT_STATUS", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SET_CHILD_STATUS_TODO", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "todo", "To Do", "SET_PARENT_STATUS", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettingChildren_ParentId",
                table: "SettingChildren",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettingChildren");

            migrationBuilder.DropTable(
                name: "SettingParents");
        }
    }
}
