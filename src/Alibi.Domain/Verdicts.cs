namespace Alibi.Domain;

// ─────────────────────────────────────────────────────────────────────────────
//  C# 15 union types
//  Uma acusação (ou o confronto de uma prova com um depoimento) tem exatamente
//  quatro desfechos possíveis. O compilador garante que todo `switch` sobre
//  `AccusationResult` trate os quatro casos — sem `default`, sem `null`,
//  sem hierarquia de classes abstratas.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>O jogador acusou o culpado e provou a mentira no horário do crime.</summary>
public sealed record CaseSolved(
    string Culprit,
    TimeOnly CrimeTime,
    string ClaimedLocation,
    string ProvenLocation,
    string Explanation);

/// <summary>As provas confirmam o depoimento do suspeito.</summary>
public sealed record AlibiHolds(string Suspect, string Reason);

/// <summary>O suspeito mentiu — mas isso, sozinho, ainda não fecha o caso.</summary>
public sealed record ContradictionFound(
    string Suspect,
    TimeOnly Time,
    string? ClaimedLocation,
    string ProvenLocation,
    string EvidenceTitle,
    bool AtCrimeTime,
    string Hint);

/// <summary>Nenhuma prova apresentada diz respeito ao suspeito.</summary>
public sealed record InsufficientEvidence(string Reason);

public union AccusationResult(CaseSolved, AlibiHolds, ContradictionFound, InsufficientEvidence);

// Resposta de um suspeito no interrogatório: roteirizada (busca vetorial),
// gerada por IA (LLM opcional) ou o suspeito simplesmente se fecha.
public sealed record ScriptedReply(string Text, int StatementId, double Distance, int? RevealsEvidenceId);
public sealed record AiReply(string Text, int? GroundingStatementId, double? Distance, int? RevealsEvidenceId);
public sealed record Stonewall(string Text, double? Distance);

public union SuspectReply(ScriptedReply, AiReply, Stonewall);
