using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alibi.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261119024825_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Cases",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Setting = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Victim = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CrimeScene = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CrimeTime = table.Column<TimeOnly>(type: "time", nullable: false),
                Solution = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Suspects",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CaseId = table.Column<int>(type: "int", nullable: false),
                Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Personality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Secret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsCulprit = table.Column<bool>(type: "bit", nullable: false),
                EvasiveLine = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suspects", x => x.Id);
                table.ForeignKey(
                    name: "FK_Suspects_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Alibis",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CaseId = table.Column<int>(type: "int", nullable: false),
                SuspectId = table.Column<int>(type: "int", nullable: false),
                Time = table.Column<TimeOnly>(type: "time", nullable: false),
                ClaimedLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Statement = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Alibis", x => x.Id);
                table.ForeignKey(
                    name: "FK_Alibis_Suspects_SuspectId",
                    column: x => x.SuspectId,
                    principalTable: "Suspects",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Evidence",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CaseId = table.Column<int>(type: "int", nullable: false),
                Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SuspectId = table.Column<int>(type: "int", nullable: true),
                Time = table.Column<TimeOnly>(type: "time", nullable: true),
                Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                StartsVisible = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Evidence", x => x.Id);
                table.ForeignKey(
                    name: "FK_Evidence_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Evidence_Suspects_SuspectId",
                    column: x => x.SuspectId,
                    principalTable: "Suspects",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Statements",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CaseId = table.Column<int>(type: "int", nullable: false),
                SuspectId = table.Column<int>(type: "int", nullable: false),
                Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                RevealsEvidenceId = table.Column<int>(type: "int", nullable: true),
                Embedding = table.Column<SqlVector<float>>(type: "vector(256)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Statements", x => x.Id);
                table.ForeignKey(
                    name: "FK_Statements_Suspects_SuspectId",
                    column: x => x.SuspectId,
                    principalTable: "Suspects",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "Timeline",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CaseId = table.Column<int>(type: "int", nullable: false),
                SuspectId = table.Column<int>(type: "int", nullable: false),
                Time = table.Column<TimeOnly>(type: "time", nullable: false),
                Location = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Timeline", x => x.Id);
                table.ForeignKey(
                    name: "FK_Timeline_Suspects_SuspectId",
                    column: x => x.SuspectId,
                    principalTable: "Suspects",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Alibis_CaseId_SuspectId_Time",
            table: "Alibis",
            columns: new[] { "CaseId", "SuspectId", "Time" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Alibis_SuspectId",
            table: "Alibis",
            column: "SuspectId");

        migrationBuilder.CreateIndex(
            name: "IX_Cases_Slug",
            table: "Cases",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Evidence_CaseId",
            table: "Evidence",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_Evidence_SuspectId",
            table: "Evidence",
            column: "SuspectId");

        migrationBuilder.CreateIndex(
            name: "IX_Statements_SuspectId",
            table: "Statements",
            column: "SuspectId");

        migrationBuilder.CreateIndex(
            name: "IX_Suspects_CaseId",
            table: "Suspects",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_Timeline_CaseId_SuspectId_Time",
            table: "Timeline",
            columns: new[] { "CaseId", "SuspectId", "Time" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Timeline_SuspectId",
            table: "Timeline",
            column: "SuspectId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Alibis");

        migrationBuilder.DropTable(
            name: "Evidence");

        migrationBuilder.DropTable(
            name: "Statements");

        migrationBuilder.DropTable(
            name: "Timeline");

        migrationBuilder.DropTable(
            name: "Suspects");

        migrationBuilder.DropTable(
            name: "Cases");
    }
}
