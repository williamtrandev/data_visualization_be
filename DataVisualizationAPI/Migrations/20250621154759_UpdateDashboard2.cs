using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataVisualizationAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDashboard2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundColor",
                table: "DashboardItem");

            migrationBuilder.DropColumn(
                name: "BorderColor",
                table: "DashboardItem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackgroundColor",
                table: "DashboardItem",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BorderColor",
                table: "DashboardItem",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
