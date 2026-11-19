namespace Alibi.Domain;

/// <summary>
/// Uma linha do cruzamento (FULL OUTER JOIN) entre a linha do tempo real
/// e o álibi declarado. Qualquer um dos lados pode faltar:
///  - só <see cref="RealLocation"/>: o suspeito omitiu aquele horário;
///  - só <see cref="ClaimedLocation"/>: o suspeito declarou algo que não aconteceu.
/// </summary>
public sealed record TimelineRow(
    int SuspectId,
    TimeOnly Time,
    string? RealLocation,
    string? ClaimedLocation,
    string? ClaimedStatement)
{
    public bool IsOmission => RealLocation is not null && ClaimedLocation is null;
    public bool IsFabrication => RealLocation is null && ClaimedLocation is not null;
    public bool IsLie => RealLocation is not null && ClaimedLocation is not null
                         && !Investigation.SamePlace(RealLocation, ClaimedLocation);
}

/// <summary>O que uma prova demonstra: "o suspeito X estava no local Y às HH:mm".</summary>
public sealed record EvidenceFact(int Id, string Title, int? SuspectId, TimeOnly? Time, string? Location);

public sealed record SuspectRef(int Id, string Name, bool IsCulprit);

public static class Investigation
{
    public static bool SamePlace(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Avalia provas contra o depoimento de um suspeito.
    /// <paramref name="crossCheck"/> vem do FULL JOIN (linha do tempo real × álibi).
    /// Com <paramref name="isFinalAccusation"/> = false (confronto), nunca encerra o caso.
    /// </summary>
    public static AccusationResult Evaluate(
        SuspectRef accused,
        TimeOnly crimeTime,
        IReadOnlyCollection<EvidenceFact> evidence,
        IReadOnlyCollection<TimelineRow> crossCheck,
        bool isFinalAccusation)
    {
        var relevant = evidence
            .Where(e => e.SuspectId == accused.Id && e.Time is not null && e.Location is not null)
            .ToList();

        if (relevant.Count == 0)
            return new InsufficientEvidence(evidence.Count == 0
                ? "Você não apresentou nenhuma prova."
                : $"Nenhuma das provas apresentadas diz respeito a {accused.Name}.");

        var contradictions = new List<ContradictionFound>();
        var confirmations = new List<string>();

        foreach (var e in relevant)
        {
            var row = crossCheck.FirstOrDefault(r => r.SuspectId == accused.Id && r.Time == e.Time);

            // A prova só vale se a linha do tempo real a corrobora.
            if (row?.RealLocation is null || !SamePlace(row.RealLocation, e.Location!))
                continue;

            if (row.ClaimedLocation is null || !SamePlace(row.ClaimedLocation, e.Location!))
            {
                var atCrime = e.Time == crimeTime;
                contradictions.Add(new ContradictionFound(
                    accused.Name, e.Time!.Value, row.ClaimedLocation, e.Location!, e.Title, atCrime,
                    Hint(accused, atCrime, isFinalAccusation, row.ClaimedLocation is null)));
            }
            else
            {
                confirmations.Add($"às {e.Time:HH\\:mm} {accused.Name} realmente estava em {e.Location} (\"{e.Title}\")");
            }
        }

        if (contradictions.Count == 0)
            return confirmations.Count == 0
                ? new InsufficientEvidence($"As provas não permitem afirmar nada sobre o depoimento de {accused.Name}.")
                : new AlibiHolds(accused.Name, "O álibi se sustenta: " + string.Join("; ", confirmations) + ".");

        var decisive = contradictions.FirstOrDefault(c => c.AtCrimeTime) ?? contradictions[0];

        if (isFinalAccusation && accused.IsCulprit && decisive.AtCrimeTime)
            return new CaseSolved(
                accused.Name, decisive.Time, decisive.ClaimedLocation ?? "(omitiu o horário)",
                decisive.ProvenLocation,
                $"{accused.Name} disse estar em {decisive.ClaimedLocation ?? "lugar nenhum"} às {decisive.Time:HH\\:mm}, " +
                $"mas \"{decisive.EvidenceTitle}\" prova que estava em {decisive.ProvenLocation} — a cena do crime.");

        return decisive;
    }

    private static string Hint(SuspectRef s, bool atCrime, bool final, bool omission)
    {
        var lie = omission ? $"{s.Name} omitiu onde estava nesse horário" : $"{s.Name} mentiu";
        return (atCrime, final, s.IsCulprit) switch
        {
            (true, false, _) => $"{lie} justamente no horário do crime. Talvez seja hora de acusar.",
            (true, true, true) => $"{lie} no horário do crime.",
            (true, true, false) => $"{lie} no horário do crime, mas estava longe da cena — escondia outra coisa.",
            (false, true, true) => $"{lie}, mas não no horário do crime. Falta a prova decisiva.",
            (false, _, _) => $"{lie}, mas fora do horário do crime. Mentira não é assassinato.",
        };
    }
}
