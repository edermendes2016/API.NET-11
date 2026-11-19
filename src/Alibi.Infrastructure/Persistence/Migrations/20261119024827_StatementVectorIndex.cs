using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alibi.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261119024827_StatementVectorIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQL Server 2025: CREATE VECTOR INDEX (DiskANN) e VECTOR_SEARCH ainda são "preview features".
        // Atenção: enquanto em preview, uma tabela com índice vetorial fica somente-leitura —
        // por isso os casos são carregados ANTES desta migration (ver DatabaseStartup).
        migrationBuilder.Sql("ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;", suppressTransaction: true);

        migrationBuilder.CreateIndex(
            name: "IX_Statements_Embedding",
            table: "Statements",
            column: "Embedding")
            .Annotation("SqlServer:VectorIndexMetric", "cosine");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Statements_Embedding",
            table: "Statements");
    }
}
