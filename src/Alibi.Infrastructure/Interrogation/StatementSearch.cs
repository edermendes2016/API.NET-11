using Alibi.Domain;
using Alibi.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alibi.Infrastructure.Interrogation;

public sealed record StatementMatch(Statement Statement, double Distance);

/// <summary>
/// Encontra a fala do suspeito mais próxima da pergunta do jogador, no próprio SQL Server 2025.
/// Modo "approximate": <c>VectorSearch()</c> do EF Core 11 (experimental, EF9105) → VECTOR_SEARCH + índice DiskANN.
/// Modo "exact": <c>EF.Functions.VectorDistance()</c> → VECTOR_DISTANCE('cosine', ...).
/// Se o modo aproximado não estiver disponível no servidor, cai automaticamente para o exato.
/// </summary>
public class StatementSearch(AlibiDbContext db, VectorSearchOptions options, ILogger<StatementSearch> logger)
{
    public async Task<IReadOnlyList<StatementMatch>> FindAsync(int suspectId, string question, int top = 3, CancellationToken ct = default)
    {
        var query = new SqlVector<float>(TextEmbedder.Embed(question));

        if (options.Mode == VectorSearchMode.Approximate)
        {
            try
            {
                if (!options.ApproximateProbed)
                {
                    options.ApproximateProbed = true;
                    logger.LogInformation("Testando VECTOR_SEARCH aproximado (experimental). Se este SQL Server não suportar, um erro de SQL é esperado logo abaixo.");
                }
                return await ApproximateAsync(suspectId, query, top, ct);
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                logger.LogWarning("VECTOR_SEARCH indisponível ({Message}). Usando VECTOR_DISTANCE exato.", ex.Message);
                options.Mode = VectorSearchMode.Exact;
            }
        }

        return await ExactAsync(suspectId, query, top, ct);
    }

    private async Task<IReadOnlyList<StatementMatch>> ExactAsync(int suspectId, SqlVector<float> query, int top, CancellationToken ct)
    {
        var rows = await db.Statements
            .Where(s => s.SuspectId == suspectId)
            .Select(s => new { Statement = s, Distance = EF.Functions.VectorDistance("cosine", s.Embedding, query) })
            .OrderBy(x => x.Distance)
            .Take(top)
            .ToListAsync(ct);
        return rows.Select(r => new StatementMatch(r.Statement, r.Distance)).ToList();
    }

#pragma warning disable EF9105 // VectorSearch é experimental no EF Core 11
    private async Task<IReadOnlyList<StatementMatch>> ApproximateAsync(int suspectId, SqlVector<float> query, int top, CancellationToken ct)
    {
        var rows = await db.Statements
            .VectorSearch(s => s.Embedding, query, "cosine")
            .Where(r => r.Value.SuspectId == suspectId)
            .OrderBy(r => r.Distance)
            .Take(top)
            .WithApproximate()
            .ToListAsync(ct);
        return rows.Select(r => new StatementMatch(r.Value, r.Distance)).ToList();
    }
#pragma warning restore EF9105
}

public enum VectorSearchMode { Approximate, Exact }

public class VectorSearchOptions
{
    public VectorSearchMode Mode { get; set; } = VectorSearchMode.Approximate;
    /// <summary>Distância de cosseno acima da qual o suspeito "não entende" a pergunta.</summary>
    public double MaxDistance { get; set; } = 0.80;
    internal bool ApproximateProbed { get; set; }
}
