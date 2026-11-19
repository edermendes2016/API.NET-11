using Xunit.Abstractions;
using Alibi.Infrastructure.Persistence;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace Alibi.Tests;

/// <summary>Prova (sem precisar de banco) que o EF Core 11 traduz os recursos novos para o SQL esperado.</summary>
public class SqlTranslationTests(ITestOutputHelper output)
{
    private static AlibiDbContext NewContext() => new(new DbContextOptionsBuilder<AlibiDbContext>()
        .UseSqlServer("Server=unused;Database=unused;TrustServerCertificate=True").Options);

    [Fact]
    public void FullJoin_is_translated_to_FULL_JOIN()
    {
        using var db = NewContext();
        var sql = new CaseQueries(db).CrossCheckQuery(caseId: 1).ToQueryString();
        output.WriteLine(sql);

        Assert.Contains("FULL JOIN", sql);
    }

    [Fact]
    public void VectorDistance_is_translated_to_VECTOR_DISTANCE()
    {
        using var db = NewContext();
        var q = new SqlVector<float>(new float[256]);
        var sql = db.Statements
            .OrderBy(s => EF.Functions.VectorDistance("cosine", s.Embedding, q))
            .Take(3)
            .ToQueryString();
        output.WriteLine(sql);

        Assert.Contains("VECTOR_DISTANCE('cosine'", sql);
    }
}
