using Alibi.Infrastructure.Interrogation;
using Alibi.Infrastructure.Persistence;
using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alibi.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultClaudeModel = "claude-opus-5-5";

    public static IServiceCollection AddAlibiInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Alibi")
            ?? throw new InvalidOperationException("ConnectionStrings:Alibi não configurada.");

        services.AddDbContext<AlibiDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5)));
        services.AddScoped<CaseQueries>();
        services.AddScoped<StatementSearch>();
        services.AddScoped<InterrogationService>();

        var vectorOptions = new VectorSearchOptions();
        config.GetSection("VectorSearch").Bind(vectorOptions);
        services.AddSingleton(vectorOptions);

        // IA opcional: só liga se houver ANTHROPIC_API_KEY. Sem chave, o jogo roda 100% offline.
        var apiKey = config["ANTHROPIC_API_KEY"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var model = config["ANTHROPIC_MODEL"] is { Length: > 0 } m ? m : DefaultClaudeModel;
            services.AddSingleton<IChatClient>(_ => new AnthropicClient { ApiKey = apiKey }.AsIChatClient(model));
        }

        return services;
    }
}
