# Testes e CI

```bash
dotnet test
```

30 testes xUnit, sem precisar de banco:

| Arquivo | O que cobre |
|---|---|
| `InvestigationTests` | Os quatro desfechos, omissão, confronto que nunca encerra o caso, switch exaustivo |
| `TextEmbedderTests` | Vetores normalizados e determinísticos; paráfrase mais próxima que pergunta sem relação |
| `CaseDataTests` | Para cada JSON: um culpado, provas coerentes, provas ocultas alcançáveis, caso solucionável, perguntas sugeridas com resposta |
| `SqlTranslationTests` | `FullJoin` vira `FULL JOIN` e `VectorDistance` vira `VECTOR_DISTANCE` (via `ToQueryString`) |

Os testes de dados já pegaram falhas reais de roteiro, como suspeitos sem resposta para "Você viu algo estranho?".

## CI

`.github/workflows/ci.yml` roda em push e pull request:

1. `dotnet build` e `dotnet test` com o SDK 11.0.x
2. `npm ci` e `npm run build` do frontend React (checagem de tipos do TypeScript + build do Vite)
3. `docker build` da imagem da API

## Teste manual de ponta a ponta

Feito com `docker compose up` e Playwright: abrir o caso, interrogar até revelar a prova oculta, confrontar e acusar até `CaseSolved`, e conferir a tela no celular (390 px, sem rolagem horizontal).
