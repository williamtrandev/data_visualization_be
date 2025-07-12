using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataVisualizationAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDatasetNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Datasets_DatasetName",
                table: "Datasets");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_DatasetName",
                table: "Datasets",
                column: "DatasetName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Datasets_DatasetName",
                table: "Datasets");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_DatasetName",
                table: "Datasets",
                column: "DatasetName",
                unique: true);
        }
    }
}
