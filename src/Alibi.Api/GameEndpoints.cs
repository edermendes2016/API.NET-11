using Alibi.Domain;
using Alibi.Infrastructure.Interrogation;
using Alibi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alibi.Api;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/status", (InterrogationService interrogation, VectorSearchOptions vector) =>
            new StatusDto(interrogation.AiEnabled ? "claude" : "offline", vector.Mode.ToString()));

        api.MapGet("/cases", async (AlibiDbContext db) =>
            await db.Cases.AsNoTracking().OrderBy(c => c.Id)
                .Select(c => new CaseSummaryDto(c.Id, c.Slug, c.Title, c.Difficulty, c.Victim, c.Setting))
                .ToListAsync());

        api.MapGet("/cases/{caseId:int}", async (int caseId, AlibiDbContext db) =>
        {
            var c = await db.Cases.AsNoTracking()
                .Include(x => x.Suspects).ThenInclude(s => s.Alibi)
                .Include(x => x.Evidence)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == caseId);
            if (c is null) return Results.NotFound();

            return Results.Ok(new CaseDto(
                c.Id, c.Title, c.Difficulty, c.Setting, c.Victim, c.Summary, c.CrimeScene, c.CrimeTime,
                c.Suspects.OrderBy(s => s.Id).Select(s => new SuspectDto(
                    s.Id, s.Name, s.Role, s.Description,
                    s.Alibi.OrderBy(a => a.Time).Select(a => new AlibiDto(a.Time, a.ClaimedLocation, a.Statement)).ToList()
                )).ToList(),
                c.Evidence.Where(e => e.StartsVisible).OrderBy(e => e.Code).Select(EvidenceDto.From).ToList()));
        });

        api.MapPost("/cases/{caseId:int}/suspects/{suspectId:int}/interrogate",
            async (int caseId, int suspectId, InterrogateRequest req, InterrogationService interrogation, AlibiDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Question) || req.Question.Length > 500)
                return Results.BadRequest(new { error = "Faça uma pergunta (até 500 caracteres)." });

            var reply = await interrogation.AskAsync(caseId, suspectId, req.Question, req.History ?? [], ct);
            if (reply is null) return Results.NotFound();

            // C# 15: switch exaustivo sobre a union SuspectReply
            var (kind, text, revealsId, distance) = reply switch
            {
                ScriptedReply s => ("scripted", s.Text, s.RevealsEvidenceId, (double?)s.Distance),
                AiReply a => ("ai", a.Text, a.RevealsEvidenceId, a.Distance),
                Stonewall w => ("stonewall", w.Text, (int?)null, w.Distance),
            };

            EvidenceDto? revealed = null;
            if (revealsId is int id)
                revealed = await db.Evidence.AsNoTracking().Where(e => e.Id == id).Select(e => EvidenceDto.From(e)).FirstOrDefaultAsync(ct);

            return Results.Ok(new InterrogateResponse(kind, text, distance, revealed));
        });

        api.MapPost("/cases/{caseId:int}/confront", (int caseId, ConfrontRequest req, AlibiDbContext db, CaseQueries q) =>
            EvaluateAsync(caseId, req.SuspectId, [req.EvidenceId], isFinal: false, db, q));

        api.MapPost("/cases/{caseId:int}/accuse", (int caseId, AccuseRequest req, AlibiDbContext db, CaseQueries q) =>
            EvaluateAsync(caseId, req.SuspectId, req.EvidenceIds ?? [], isFinal: true, db, q));

        // Desiste do caso: mostra a solução e o cruzamento completo (FULL JOIN) de todos os suspeitos.
        api.MapGet("/cases/{caseId:int}/solution", async (int caseId, AlibiDbContext db, CaseQueries q) =>
        {
            var c = await db.Cases.AsNoTracking().Include(x => x.Suspects).FirstOrDefaultAsync(x => x.Id == caseId);
            if (c is null) return Results.NotFound();
            return Results.Ok(await DebriefAsync(c, q));
        });
    }

    private static async Task<IResult> EvaluateAsync(int caseId, int suspectId, IReadOnlyList<int> evidenceIds,
        bool isFinal, AlibiDbContext db, CaseQueries queries)
    {
        var c = await db.Cases.AsNoTracking().Include(x => x.Suspects).FirstOrDefaultAsync(x => x.Id == caseId);
        var suspect = c?.Suspects.FirstOrDefault(s => s.Id == suspectId);
        if (c is null || suspect is null) return Results.NotFound();

        var evidence = await db.Evidence.AsNoTracking()
            .Where(e => e.CaseId == caseId && evidenceIds.Contains(e.Id))
            .Select(e => new EvidenceFact(e.Id, e.Title, e.SuspectId, e.Time, e.Location))
            .ToListAsync();

        var crossCheck = await queries.CrossCheckAsync(caseId, suspectId);

        var result = Investigation.Evaluate(
            new SuspectRef(suspect.Id, suspect.Name, suspect.IsCulprit), c.CrimeTime, evidence, crossCheck, isFinal);

        // C# 15: o compilador garante que os 4 desfechos estão tratados.
        VerdictDto dto = result switch
        {
            CaseSolved s => new("CaseSolved", "Caso encerrado!", s.Explanation, s.Culprit, Debrief: await DebriefAsync(c, queries)),
            ContradictionFound x => new("ContradictionFound", "Contradição encontrada!",
                $"{x.Suspect} disse estar em {x.ClaimedLocation ?? "(não disse)"} às {x.Time:HH\\:mm}, mas \"{x.EvidenceTitle}\" prova que estava em {x.ProvenLocation}. {x.Hint}",
                x.Suspect),
            AlibiHolds a => new("AlibiHolds", "O álibi se sustenta.", a.Reason, a.Suspect),
            InsufficientEvidence i => new("InsufficientEvidence", "Provas insuficientes.", i.Reason, suspect.Name),
        };
        return Results.Ok(dto);
    }

    private static async Task<DebriefDto> DebriefAsync(MysteryCase c, CaseQueries queries)
    {
        var names = c.Suspects.ToDictionary(s => s.Id, s => s.Name);
        var rows = await queries.CrossCheckAsync(c.Id);
        return new DebriefDto(
            c.Solution,
            c.Suspects.Single(s => s.IsCulprit).Name,
            rows.Select(r => new CrossCheckRowDto(
                names[r.SuspectId], r.Time, r.RealLocation, r.ClaimedLocation,
                r.IsLie ? "mentira" : r.IsOmission ? "omissão" : r.IsFabrication ? "inventado" : "confere")).ToList());
    }
}

public sealed record StatusDto(string AiMode, string VectorSearchMode);
public sealed record CaseSummaryDto(int Id, string Slug, string Title, string Difficulty, string Victim, string Setting);
public sealed record CaseDto(int Id, string Title, string Difficulty, string Setting, string Victim, string Summary,
    string CrimeScene, TimeOnly CrimeTime, List<SuspectDto> Suspects, List<EvidenceDto> Evidence);
public sealed record SuspectDto(int Id, string Name, string Role, string Description, List<AlibiDto> Alibi);
public sealed record AlibiDto(TimeOnly Time, string Location, string Statement);
public sealed record EvidenceDto(int Id, string Code, string Title, string Description)
{
    public static EvidenceDto From(Evidence e) => new(e.Id, e.Code, e.Title, e.Description);
}
public sealed record InterrogateRequest(string Question, List<ChatTurn>? History);
public sealed record InterrogateResponse(string Kind, string Text, double? Distance, EvidenceDto? RevealedEvidence);
public sealed record ConfrontRequest(int SuspectId, int EvidenceId);
public sealed record AccuseRequest(int SuspectId, List<int>? EvidenceIds);
public sealed record VerdictDto(string Outcome, string Headline, string Message, string Suspect, DebriefDto? Debrief = null);
public sealed record DebriefDto(string Solution, string Culprit, List<CrossCheckRowDto> CrossCheck);
public sealed record CrossCheckRowDto(string Suspect, TimeOnly Time, string? RealLocation, string? ClaimedLocation, string Verdict);
