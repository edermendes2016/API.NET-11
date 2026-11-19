using Alibi.Domain;
using Microsoft.EntityFrameworkCore;

namespace Alibi.Infrastructure.Persistence;

public class CaseQueries(AlibiDbContext db)
{
    /// <summary>
    /// EF Core 11 — <c>FullJoin</c> (operador LINQ novo do .NET 10/11, traduzido para FULL OUTER JOIN).
    /// Cruza a linha do tempo REAL com os álibis DECLARADOS por (suspeito, horário).
    /// Linhas de um lado só também aparecem: horário omitido pelo suspeito ou álibi inventado.
    /// </summary>
    public IQueryable<CrossCheckProjection> CrossCheckQuery(int caseId, int? suspectId = null)
    {
        var truth = db.Timeline.Where(t => t.CaseId == caseId && (suspectId == null || t.SuspectId == suspectId));
        var claims = db.Alibis.Where(a => a.CaseId == caseId && (suspectId == null || a.SuspectId == suspectId));

        return truth
            .FullJoin(
                claims,
                t => new { t.SuspectId, t.Time },
                a => new { a.SuspectId, a.Time },
                (t, a) => new CrossCheckProjection
                {
                    SuspectId = t != null ? t.SuspectId : a!.SuspectId,
                    Time = t != null ? t.Time : a!.Time,
                    RealLocation = t != null ? t.Location : null,
                    ClaimedLocation = a != null ? a.ClaimedLocation : null,
                    ClaimedStatement = a != null ? a.Statement : null,
                })
            .OrderBy(r => r.SuspectId).ThenBy(r => r.Time);
    }

    public async Task<List<TimelineRow>> CrossCheckAsync(int caseId, int? suspectId = null, CancellationToken ct = default) =>
        (await CrossCheckQuery(caseId, suspectId).ToListAsync(ct))
            .Select(r => new TimelineRow(r.SuspectId, r.Time, r.RealLocation, r.ClaimedLocation, r.ClaimedStatement))
            .ToList();
}

public sealed class CrossCheckProjection
{
    public int SuspectId { get; init; }
    public TimeOnly Time { get; init; }
    public string? RealLocation { get; init; }
    public string? ClaimedLocation { get; init; }
    public string? ClaimedStatement { get; init; }
}
