using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMatchBE.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH "NameParts" AS (
                    SELECT
                        "Id",
                        regexp_split_to_array(
                            NULLIF(
                                BTRIM(
                                    regexp_replace(
                                        split_part("Email", '@', 1),
                                        '[^[:alnum:]]+',
                                        ' ',
                                        'g')),
                                ''),
                            '[[:space:]]+') AS "Parts"
                    FROM "Users"
                    WHERE "FirstName" IS NULL OR "LastName" IS NULL
                )
                UPDATE "Users" AS "User"
                SET
                    "FirstName" = LEFT(
                        INITCAP(COALESCE("NameParts"."Parts"[1], 'Existing')),
                        100),
                    "LastName" = LEFT(
                        INITCAP(
                            CASE
                                WHEN CARDINALITY("NameParts"."Parts") > 1
                                    THEN array_to_string(
                                        "NameParts"."Parts"[2:CARDINALITY("NameParts"."Parts")],
                                        ' ')
                                ELSE 'User'
                            END),
                        100)
                FROM "NameParts"
                WHERE "User"."Id" = "NameParts"."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");
        }
    }
}
