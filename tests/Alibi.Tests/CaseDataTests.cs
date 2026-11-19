using Alibi.Domain;
using Alibi.Infrastructure.Persistence;

namespace Alibi.Tests;

/// <summary>Garante que todo caso em Seed/Cases é solucionável e coerente.</summary>
public class CaseDataTests
{
    public static TheoryData<string> CaseFiles()
    {
        var data = new TheoryData<string>();
        foreach (var f in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Seed", "Cases"), "*.json").Order())
            data.Add(Path.GetFileName(f));
        return data;
    }

    private static CaseSeeder.CaseDoc Load(string file) =>
        CaseSeeder.Load(Path.Combine(AppContext.BaseDirectory, "Seed", "Cases", file));

    [Fact]
    public void There_are_three_cases() => Assert.Equal(3, CaseFiles().Count);

    [Theory, MemberData(nameof(CaseFiles))]
    public void Case_has_exactly_one_culprit_who_was_at_the_crime_scene(string file)
    {
        var c = Load(file);
        var culprit = Assert.Single(c.Suspects, s => s.IsCulprit);
        Assert.Equal(c.CrimeScene, culprit.Truth[c.CrimeTime]);
        Assert.NotEqual(c.CrimeScene, culprit.Alibi[c.CrimeTime].Location);
    }

    [Theory, MemberData(nameof(CaseFiles))]
    public void Every_evidence_is_consistent_with_the_real_timeline(string file)
    {
        var c = Load(file);
        foreach (var e in c.Evidence.Where(e => e.Suspect is not null))
        {
            var suspect = c.Suspects.Single(s => s.Key == e.Suspect);
            Assert.Equal(e.Location, suspect.Truth[e.Time!]);
        }
    }

    [Theory, MemberData(nameof(CaseFiles))]
    public void Hidden_evidence_can_be_unlocked_by_some_statement(string file)
    {
        var c = Load(file);
        var reveals = c.Suspects.SelectMany(s => s.Statements).Select(s => s.Reveals).Where(r => r is not null).ToHashSet();
        foreach (var e in c.Evidence.Where(e => !e.Visible))
            Assert.Contains(e.Code, reveals);
    }

    [Theory, MemberData(nameof(CaseFiles))]
    public void Case_is_solvable_with_its_own_evidence(string file)
    {
        var c = Load(file);
        var ids = c.Suspects.Select((s, i) => (s, id: i + 1)).ToDictionary(x => x.s.Key, x => x.id);
        var culprit = c.Suspects.Single(s => s.IsCulprit);
        var crimeTime = TimeOnly.Parse(c.CrimeTime);

        var rows = culprit.Truth.Keys.Union(culprit.Alibi.Keys).Select(t => new TimelineRow(
            ids[culprit.Key], TimeOnly.Parse(t),
            culprit.Truth.GetValueOrDefault(t), culprit.Alibi.GetValueOrDefault(t)?.Location, null)).ToList();

        var evidence = c.Evidence.Select((e, i) => new EvidenceFact(i + 1, e.Title,
            e.Suspect is null ? null : ids[e.Suspect], e.Time is null ? null : TimeOnly.Parse(e.Time), e.Location)).ToList();

        var result = Investigation.Evaluate(new SuspectRef(ids[culprit.Key], culprit.Name, true), crimeTime, evidence, rows, true);

        Assert.IsType<CaseSolved>(result.Value);
    }

    [Theory, MemberData(nameof(CaseFiles))]
    public void Suggested_questions_find_a_scripted_answer_for_every_suspect(string file)
    {
        var c = Load(file);
        foreach (var s in c.Suspects)
        {
            var embeddings = s.Statements.Select(st => TextEmbedder.Embed($"{st.Topic} {string.Join(" | ", st.Ask)}")).ToList();
            foreach (var q in new[] { "Onde você estava às 21h?", "Conte sua noite.", "Você viu algo estranho?", "Quem você acha que matou?" })
            {
                var best = embeddings.Min(e => TextEmbedder.CosineDistance(TextEmbedder.Embed(q), e));
                Assert.True(best <= 0.8, $"{s.Name}: '{q}' sem resposta (distância {best:F2})");
            }
        }
    }
}
