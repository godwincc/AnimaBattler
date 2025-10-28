using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimaBattler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnergyToSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Energy",
                table: "skills",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Energy",
                table: "skills");
        }
    }
}
