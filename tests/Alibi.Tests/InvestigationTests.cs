using Alibi.Domain;

namespace Alibi.Tests;

public class InvestigationTests
{
    private static readonly TimeOnly Crime = new(21, 0);
    private static readonly SuspectRef Culprit = new(1, "Clarice", IsCulprit: true);
    private static readonly SuspectRef Innocent = new(2, "Helena", IsCulprit: false);

    private static readonly TimelineRow[] Rows =
    [
        new(1, new(20, 30), "Biblioteca", "Biblioteca", "lendo"),
        new(1, new(21, 0), "Escritório", "Biblioteca", "lendo"),
        new(1, new(21, 30), "Trilha", null, null),                 // omissão
        new(2, new(21, 0), "Jardim", "Salão", "no bolo"),
    ];

    [Fact]
    public void Accusing_culprit_with_evidence_at_crime_time_solves_the_case()
    {
        var result = Investigation.Evaluate(Culprit, Crime, [new(10, "Fechadura", 1, new(21, 0), "Escritório")], Rows, isFinalAccusation: true);

        var solved = Assert.IsType<CaseSolved>(result.Value);
        Assert.Equal("Escritório", solved.ProvenLocation);
        Assert.Equal("Biblioteca", solved.ClaimedLocation);
    }

    [Fact]
    public void Confronting_never_closes_the_case()
    {
        var result = Investigation.Evaluate(Culprit, Crime, [new(10, "Fechadura", 1, new(21, 0), "Escritório")], Rows, isFinalAccusation: false);

        var c = Assert.IsType<ContradictionFound>(result.Value);
        Assert.True(c.AtCrimeTime);
    }

    [Fact]
    public void Innocent_liar_is_a_contradiction_not_a_solution()
    {
        var result = Investigation.Evaluate(Innocent, Crime, [new(11, "Garçom", 2, new(21, 0), "Jardim")], Rows, isFinalAccusation: true);

        Assert.IsType<ContradictionFound>(result.Value);
    }

    [Fact]
    public void Evidence_that_matches_the_statement_means_the_alibi_holds()
    {
        var result = Investigation.Evaluate(Culprit, Crime, [new(12, "Ficha", 1, new(20, 30), "Biblioteca")], Rows, isFinalAccusation: true);

        Assert.IsType<AlibiHolds>(result.Value);
    }

    [Fact]
    public void Omitted_time_slot_counts_as_contradiction()
    {
        var result = Investigation.Evaluate(Culprit, Crime, [new(13, "GPS", 1, new(21, 30), "Trilha")], Rows, isFinalAccusation: true);

        var c = Assert.IsType<ContradictionFound>(result.Value);
        Assert.Null(c.ClaimedLocation);
        Assert.False(c.AtCrimeTime);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 99 })]
    public void Evidence_about_someone_else_is_insufficient(int[] ids)
    {
        var evidence = ids.Select(id => new EvidenceFact(id, "Laudo", 2, new(21, 0), "Jardim")).ToList();

        var result = Investigation.Evaluate(Culprit, Crime, evidence, Rows, isFinalAccusation: true);

        Assert.IsType<InsufficientEvidence>(result.Value);
    }

    [Fact]
    public void Union_switch_is_exhaustive_without_default()
    {
        AccusationResult r = new AlibiHolds("x", "y");
        // Se um novo caso for adicionado à union, isto deixa de compilar sem tratamento.
        var label = r switch
        {
            CaseSolved => "solved",
            AlibiHolds => "holds",
            ContradictionFound => "lie",
            InsufficientEvidence => "weak",
        };
        Assert.Equal("holds", label);
    }
}
