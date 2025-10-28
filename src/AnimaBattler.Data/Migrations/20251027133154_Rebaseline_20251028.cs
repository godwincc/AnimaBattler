using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AnimaBattler.Data.Migrations
{
    /// <inheritdoc />
    public partial class Rebaseline_20251028 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "animas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ArchetypeId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Hp = table.Column<int>(type: "integer", nullable: false),
                    DamageMultiplier = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AssignedSkillCodes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AssignedPartCodes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_animas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "archetypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BaseHp = table.Column<int>(type: "integer", nullable: false),
                    BaseSpeed = table.Column<int>(type: "integer", nullable: false),
                    DamageMult = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    DefenseMult = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archetypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "enemies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Hp = table.Column<int>(type: "integer", nullable: false),
                    DamageMultiplier = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enemies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "enemy_skills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BaseDamage = table.Column<int>(type: "integer", nullable: false),
                    EffectValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationTurns = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enemy_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArchetypeId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BaseDamage = table.Column<int>(type: "integer", nullable: false),
                    EffectValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationTurns = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skills_archetypes_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "archetypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_enemies_Code",
                table: "enemies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enemy_skills_Code",
                table: "enemy_skills",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skills_ArchetypeId",
                table: "skills",
                column: "ArchetypeId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_ArchetypeId_Code",
                table: "skills",
                columns: new[] { "ArchetypeId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "animas");

            migrationBuilder.DropTable(
                name: "enemies");

            migrationBuilder.DropTable(
                name: "enemy_skills");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "archetypes");
        }
    }
}
