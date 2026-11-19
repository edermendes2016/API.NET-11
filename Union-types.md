# Union types (C# 15)

O resultado de uma acusação tem exatamente quatro formas. Em C# 15 isso vira uma union:

```csharp
public sealed record CaseSolved(string Culprit, TimeOnly CrimeTime, string ClaimedLocation, string ProvenLocation, string Explanation);
public sealed record AlibiHolds(string Suspect, string Reason);
public sealed record ContradictionFound(string Suspect, TimeOnly Time, string? ClaimedLocation, string ProvenLocation,
                                       string EvidenceTitle, bool AtCrimeTime, string Hint);
public sealed record InsufficientEvidence(string Reason);

public union AccusationResult(CaseSolved, AlibiHolds, ContradictionFound, InsufficientEvidence);
```

`src/Alibi.Domain/Verdicts.cs`

## O ganho

O `switch` não precisa de `default`, e o compilador garante que os quatro casos foram tratados. Se alguém criar um quinto desfecho, o código que não trata ele deixa de compilar.

```csharp
VerdictDto dto = result switch
{
    CaseSolved s           => new("CaseSolved", "Caso encerrado!", s.Explanation, s.Culprit, ...),
    ContradictionFound x   => new("ContradictionFound", "Contradição encontrada!", ...),
    AlibiHolds a           => new("AlibiHolds", "O álibi se sustenta.", a.Reason, a.Suspect),
    InsufficientEvidence i => new("InsufficientEvidence", "Provas insuficientes.", i.Reason, ...),
};
```

`src/Alibi.Api/GameEndpoints.cs`

Os tipos de caso são records comuns, sem classe base abstrata nem hierarquia artificial. Criar o valor é uma conversão implícita: `AccusationResult r = new AlibiHolds(...)`.

## Segunda union: a resposta do suspeito

```csharp
public union SuspectReply(ScriptedReply, AiReply, Stonewall);
```

- `ScriptedReply`: fala escolhida pela busca vetorial
- `AiReply`: resposta gerada pelo Claude
- `Stonewall`: nada parecido com a pergunta; o suspeito se fecha

## Verificação

O SDK 11.0.100 compila `union` com a linguagem padrão do `net11.0`, sem `LangVersion` preview. O teste `Union_switch_is_exhaustive_without_default` documenta o padrão.
