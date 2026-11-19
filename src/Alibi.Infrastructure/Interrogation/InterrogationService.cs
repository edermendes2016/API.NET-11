using System.Text;
using Alibi.Domain;
using Alibi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Alibi.Infrastructure.Interrogation;

public sealed record ChatTurn(string Role, string Text);

/// <summary>
/// Interrogatório: a busca vetorial sempre escolhe a fala roteirizada mais próxima
/// (é ela que destrava provas — o jogo é determinístico). Se houver um LLM configurado,
/// ele interpreta o suspeito usando essas falas como base; sem LLM, a fala roteirizada é a resposta.
/// </summary>
public class InterrogationService(
    AlibiDbContext db,
    StatementSearch search,
    VectorSearchOptions options,
    ILogger<InterrogationService> logger,
    IChatClient? chat = null)
{
    public bool AiEnabled => chat is not null;

    public async Task<SuspectReply?> AskAsync(int caseId, int suspectId, string question,
        IReadOnlyList<ChatTurn> history, CancellationToken ct = default)
    {
        var suspect = await db.Suspects.AsNoTracking()
            .Include(s => s.Alibi)
            .FirstOrDefaultAsync(s => s.Id == suspectId && s.CaseId == caseId, ct);
        if (suspect is null) return null;

        var matches = await search.FindAsync(suspectId, question, 3, ct);
        var best = matches.FirstOrDefault(m => m.Distance <= options.MaxDistance);

        if (chat is not null)
        {
            try
            {
                var text = await AskLlmAsync(suspect, matches, history, question, ct);
                return new AiReply(text, best?.Statement.Id, best?.Distance, best?.Statement.RevealsEvidenceId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "LLM falhou; usando resposta roteirizada.");
            }
        }

        return best is null
            ? new Stonewall(suspect.EvasiveLine, matches.FirstOrDefault()?.Distance)
            : new ScriptedReply(best.Statement.Answer, best.Statement.Id, best.Distance, best.Statement.RevealsEvidenceId);
    }

    private async Task<string> AskLlmAsync(Suspect s, IReadOnlyList<StatementMatch> matches,
        IReadOnlyList<ChatTurn> history, string question, CancellationToken ct)
    {
        var allStatements = await db.Statements.AsNoTracking().Where(x => x.SuspectId == s.Id).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"Você é {s.Name}, {s.Role}, suspeito num jogo de mistério. Fale sempre em português do Brasil, em primeira pessoa, no máximo 3 frases.");
        sb.AppendLine($"Quem você é: {s.Description}");
        sb.AppendLine($"Personalidade: {s.Personality}");
        sb.AppendLine($"Seu segredo (NUNCA revele nem confesse diretamente; minta para protegê-lo): {s.Secret}");
        sb.AppendLine("Seu álibi oficial, que você sustenta sempre:");
        foreach (var a in s.Alibi.OrderBy(a => a.Time))
            sb.AppendLine($"- {a.Time:HH\\:mm}: {a.ClaimedLocation} — {a.Statement}");
        sb.AppendLine("Falas canônicas do personagem (fonte da verdade do jogo; não contradiga e não invente fatos novos, horários, provas ou testemunhas):");
        foreach (var st in allStatements)
            sb.AppendLine($"- [{st.Topic}] {st.Answer}");
        if (matches.Count > 0)
            sb.AppendLine($"A fala mais relevante para a pergunta atual é: \"{matches[0].Statement.Answer}\". Baseie sua resposta nela.");
        sb.AppendLine("Se o detetive perguntar algo fora do caso, desvie mantendo o personagem. Nunca saia do personagem nem mencione que é uma IA.");

        var messages = new List<ChatMessage> { new(ChatRole.System, sb.ToString()) };
        foreach (var turn in history.TakeLast(10))
            messages.Add(new ChatMessage(turn.Role == "detective" ? ChatRole.User : ChatRole.Assistant, turn.Text));
        messages.Add(new ChatMessage(ChatRole.User, question));

        var response = await chat!.GetResponseAsync(messages, new ChatOptions
        {
            MaxOutputTokens = 1024,
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low },
        }, ct);
        return response.Text.Trim();
    }
}
