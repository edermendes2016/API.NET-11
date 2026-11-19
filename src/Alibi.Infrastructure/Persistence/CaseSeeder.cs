using System.Text.Json;
using System.Text.Json.Serialization;
using Alibi.Domain;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alibi.Infrastructure.Persistence;

/// <summary>Carrega os casos de Seed/Cases/*.json para o banco (idempotente, por slug).</summary>
public static class CaseSeeder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task SeedAsync(AlibiDbContext db, string casesDirectory, ILogger logger, CancellationToken ct = default)
    {
        foreach (var file in Directory.GetFiles(casesDirectory, "*.json").Order())
        {
            var doc = Load(file);

            if (await db.Cases.AnyAsync(c => c.Slug == doc.Slug, ct))
                continue;

            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var mysteryCase = new MysteryCase
            {
                Slug = doc.Slug, Title = doc.Title, Difficulty = doc.Difficulty, Setting = doc.Setting,
                Victim = doc.Victim, Summary = doc.Summary, CrimeScene = doc.CrimeScene,
                CrimeTime = TimeOnly.Parse(doc.CrimeTime), Solution = doc.Solution,
            };
            db.Cases.Add(mysteryCase);
            await db.SaveChangesAsync(ct);

            var suspects = new Dictionary<string, Suspect>();
            foreach (var s in doc.Suspects)
            {
                var suspect = new Suspect
                {
                    CaseId = mysteryCase.Id, Key = s.Key, Name = s.Name, Role = s.Role, Description = s.Description,
                    Personality = s.Personality, Secret = s.Secret, IsCulprit = s.IsCulprit, EvasiveLine = s.Evasive,
                };
                db.Suspects.Add(suspect);
                suspects[s.Key] = suspect;
            }
            await db.SaveChangesAsync(ct);

            var evidence = new Dictionary<string, Evidence>();
            foreach (var e in doc.Evidence)
            {
                var ev = new Evidence
                {
                    CaseId = mysteryCase.Id, Code = e.Code, Title = e.Title, Description = e.Description,
                    SuspectId = e.Suspect is null ? null : suspects[e.Suspect].Id,
                    Time = e.Time is null ? null : TimeOnly.Parse(e.Time),
                    Location = e.Location, StartsVisible = e.Visible,
                };
                db.Evidence.Add(ev);
                evidence[e.Code] = ev;
            }
            await db.SaveChangesAsync(ct);

            foreach (var s in doc.Suspects)
            {
                var suspectId = suspects[s.Key].Id;
                foreach (var (time, location) in s.Truth)
                    db.Timeline.Add(new TimelineEvent { CaseId = mysteryCase.Id, SuspectId = suspectId, Time = TimeOnly.Parse(time), Location = location });

                foreach (var (time, claim) in s.Alibi)
                    db.Alibis.Add(new AlibiClaim
                    {
                        CaseId = mysteryCase.Id, SuspectId = suspectId, Time = TimeOnly.Parse(time),
                        ClaimedLocation = claim.Location, Statement = claim.Statement,
                    });

                foreach (var st in s.Statements)
                {
                    var prompt = string.Join(" | ", st.Ask);
                    db.Statements.Add(new Statement
                    {
                        CaseId = mysteryCase.Id, SuspectId = suspectId, Topic = st.Topic, Prompt = prompt, Answer = st.Answer,
                        RevealsEvidenceId = st.Reveals is null ? null : evidence[st.Reveals].Id,
                        Embedding = new SqlVector<float>(TextEmbedder.Embed($"{st.Topic} {prompt}")),
                    });
                }
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            });
            logger.LogInformation("Caso '{Title}' carregado ({Suspects} suspeitos, {Evidence} provas).",
                doc.Title, doc.Suspects.Count, doc.Evidence.Count);
        }
    }

    public static CaseDoc Load(string file) =>
        JsonSerializer.Deserialize<CaseDoc>(File.ReadAllText(file), Json)
        ?? throw new InvalidOperationException($"Caso inválido: {file}");

    public sealed record CaseDoc(
        string Slug, string Title, string Difficulty, string Setting, string Victim, string CrimeScene,
        string CrimeTime, string Summary, string Solution, List<string> Locations,
        List<SuspectDoc> Suspects, List<EvidenceDoc> Evidence);

    public sealed record SuspectDoc(
        string Key, string Name, string Role, string Description, string Personality, string Secret,
        bool IsCulprit, string Evasive, Dictionary<string, string> Truth,
        Dictionary<string, AlibiDoc> Alibi, List<StatementDoc> Statements);

    public sealed record AlibiDoc(string Location, string Statement);
    public sealed record StatementDoc(string Topic, List<string> Ask, string Answer, string? Reveals);
    public sealed record EvidenceDoc(string Code, string Title, string Description, string? Suspect, string? Time, string? Location, bool Visible);
}
