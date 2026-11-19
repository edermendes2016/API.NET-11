using Microsoft.Data.SqlTypes;

namespace Alibi.Infrastructure.Persistence;

public class MysteryCase
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public required string Difficulty { get; set; }
    public required string Setting { get; set; }
    public required string Victim { get; set; }
    public required string Summary { get; set; }
    public required string CrimeScene { get; set; }
    public TimeOnly CrimeTime { get; set; }
    public required string Solution { get; set; }
    public List<Suspect> Suspects { get; set; } = [];
    public List<Evidence> Evidence { get; set; } = [];
}

public class Suspect
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public required string Description { get; set; }
    public required string Personality { get; set; }
    /// <summary>Só o LLM enxerga — nunca vai para o cliente.</summary>
    public required string Secret { get; set; }
    public bool IsCulprit { get; set; }
    public required string EvasiveLine { get; set; }
    public List<AlibiClaim> Alibi { get; set; } = [];
    public List<Statement> Statements { get; set; } = [];
}

/// <summary>O que REALMENTE aconteceu (oculto do jogador).</summary>
public class TimelineEvent
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int SuspectId { get; set; }
    public TimeOnly Time { get; set; }
    public required string Location { get; set; }
}

/// <summary>O que o suspeito DIZ que aconteceu.</summary>
public class AlibiClaim
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int SuspectId { get; set; }
    public TimeOnly Time { get; set; }
    public required string ClaimedLocation { get; set; }
    public required string Statement { get; set; }
}

public class Evidence
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int? SuspectId { get; set; }
    public TimeOnly? Time { get; set; }
    public string? Location { get; set; }
    public bool StartsVisible { get; set; }
}

/// <summary>Fala roteirizada de um suspeito, indexada por embedding (coluna VECTOR do SQL Server 2025).</summary>
public class Statement
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int SuspectId { get; set; }
    public required string Topic { get; set; }
    public required string Prompt { get; set; }
    public required string Answer { get; set; }
    public int? RevealsEvidenceId { get; set; }
    public SqlVector<float> Embedding { get; set; }
}
