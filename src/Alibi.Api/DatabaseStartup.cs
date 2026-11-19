using Alibi.Infrastructure.Interrogation;
using Alibi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Alibi.Api;

public static class DatabaseStartup
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        // O SQL Server do docker compose pode demorar alguns segundos a aceitar conexões.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AlibiDbContext>();
                var migrator = db.GetService<IMigrator>();
                var migrations = db.Database.GetMigrations().ToList();

                // 1) Esquema (tabelas + coluna VECTOR) → 2) casos → 3) índice vetorial (deixa a tabela read-only no preview)
                await migrator.MigrateAsync(migrations.First(m => m.EndsWith("_InitialCreate")));
                await CaseSeeder.SeedAsync(db, Path.Combine(AppContext.BaseDirectory, "Seed", "Cases"), logger);
                await TryCreateVectorIndexAsync(db, scope.ServiceProvider.GetRequiredService<VectorSearchOptions>(), logger);

                // Sonda: descobre já na subida se VECTOR_SEARCH (aproximado) funciona neste servidor.
                var anySuspect = await db.Suspects.Select(s => s.Id).FirstAsync();
                await scope.ServiceProvider.GetRequiredService<StatementSearch>().FindAsync(anySuspect, "onde voce estava");
                logger.LogInformation("Busca vetorial em modo {Mode}.", scope.ServiceProvider.GetRequiredService<VectorSearchOptions>().Mode);
                return;
            }
            catch (Exception ex) when (attempt < 40)
            {
                logger.LogWarning("Banco ainda indisponível (tentativa {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private static async Task TryCreateVectorIndexAsync(AlibiDbContext db, VectorSearchOptions options, ILogger logger)
    {
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            options.Mode = VectorSearchMode.Exact;
            logger.LogWarning("Índice vetorial indisponível neste SQL Server ({Message}). Busca vetorial exata (VECTOR_DISTANCE) ativada.", ex.Message);
        }
    }
}
