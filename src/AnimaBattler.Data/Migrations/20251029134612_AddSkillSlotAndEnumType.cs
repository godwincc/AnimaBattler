using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimaBattler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillSlotAndEnumType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseHeal",
                table: "skills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slot",
                table: "skills",
                type: "character varying(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "DamageMult",
                table: "archetypes",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateTable(
                name: "anima_skills",
                columns: table => new
                {
                    AnimaId = table.Column<long>(type: "bigint", nullable: false),
                    SkillId = table.Column<long>(type: "bigint", nullable: false),
                    IsEquipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LearnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anima_skills", x => new { x.AnimaId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_anima_skills_animas_AnimaId",
                        column: x => x.AnimaId,
                        principalTable: "animas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_anima_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anima_skills_AnimaId_OrderIndex",
                table: "anima_skills",
                columns: new[] { "AnimaId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_anima_skills_SkillId",
                table: "anima_skills",
                column: "SkillId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anima_skills");

            migrationBuilder.DropColumn(
                name: "BaseHeal",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "skills");

            migrationBuilder.AlterColumn<double>(
                name: "DamageMult",
                table: "archetypes",
                type: "double precision",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
