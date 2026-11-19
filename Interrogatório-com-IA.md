# Interrogatório com IA

## Dois modos

| Modo | Quando | Resposta |
|---|---|---|
| Offline | Sem `ANTHROPIC_API_KEY` (padrão) | A fala roteirizada mais próxima ([[Busca vetorial]]) |
| IA | Com `ANTHROPIC_API_KEY` | Claude interpreta o suspeito, baseado nas falas roteirizadas |

```bash
ANTHROPIC_API_KEY=sk-ant-... docker compose up
```

`ANTHROPIC_MODEL` troca o modelo (padrão `claude-opus-5-5`).

## O jogo continua determinístico

Nos dois modos, a busca vetorial escolhe a fala canônica. É **ela** que destrava provas (`RevealsEvidenceId`). A IA só dá voz ao personagem, então o caso é sempre solucionável e a IA não inventa pistas.

## Integração

O SDK oficial da Anthropic para C# expõe um `IChatClient` (Microsoft.Extensions.AI), registrado só quando há chave:

```csharp
if (!string.IsNullOrWhiteSpace(apiKey))
    services.AddSingleton<IChatClient>(_ =>
        new AnthropicClient { ApiKey = apiKey }.AsIChatClient(model));
```

`src/Alibi.Infrastructure/DependencyInjection.cs`

O prompt de sistema leva: perfil e personalidade, o **segredo** (com a instrução de nunca confessar), o álibi oficial, todas as falas canônicas e a fala mais relevante para a pergunta. As últimas 10 trocas da conversa vão como histórico, com `Reasoning.Effort = Low` porque é um diálogo curto.

Se a chamada falhar (rede, cota), o serviço registra o erro e devolve a resposta roteirizada.

## Status

O caminho com Claude compila e está ligado no DI, mas foi desenvolvido sem uma chave real; o modo offline é o que foi testado de ponta a ponta.
