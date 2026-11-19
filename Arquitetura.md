# Arquitetura

```
src/
  Alibi.Domain/          regras puras: unions, avaliação da acusação, embedder offline
  Alibi.Infrastructure/  EF Core 11, migrations, seed dos casos, busca vetorial, IA
  Alibi.Api/             Minimal APIs; serve o frontend compilado (wwwroot)
  Alibi.Web/             frontend em React 19 + Vite + TypeScript
tests/
  Alibi.Tests/           xUnit
```

O frontend é compilado num estágio Node do mesmo Dockerfile e copiado para o `wwwroot` da API, então não existe um container separado para ele.

O domínio não conhece EF nem HTTP. A infraestrutura faz as consultas (FULL JOIN, vetores) e entrega linhas simples (`TimelineRow`) para o domínio decidir.

## Modelo de dados

| Tabela | Conteúdo | Vai para o jogador? |
|---|---|---|
| `Cases` | Título, vítima, cena, hora do crime, solução | Sim, menos a solução |
| `Suspects` | Perfil, personalidade, segredo, culpado | Perfil sim; segredo e culpa nunca |
| `Timeline` | Onde cada suspeito **realmente** estava a cada horário | Nunca |
| `Alibis` | Onde cada suspeito **diz** que estava | Sim |
| `Evidence` | Prova: suspeito + horário + local que ela demonstra | Visíveis no início, ou ao serem reveladas |
| `Statements` | Falas roteirizadas + `Embedding vector(256)` | Só a fala escolhida |

## Fluxo de uma acusação

```
POST /api/cases/{id}/accuse  { suspectId, evidenceIds }
  → CaseQueries.CrossCheckAsync      (EF Core 11 FullJoin → FULL JOIN)
  → Investigation.Evaluate           (domínio, devolve a union AccusationResult)
  → switch exaustivo → JSON
```

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/status` | Modo da IA e da busca vetorial |
| GET | `/api/cases` | Lista de casos |
| GET | `/api/cases/{id}` | Dossiê, suspeitos, álibis, provas visíveis |
| POST | `/api/cases/{id}/suspects/{sid}/interrogate` | Pergunta → resposta (+ prova revelada) |
| POST | `/api/cases/{id}/confront` | Uma prova contra um suspeito |
| POST | `/api/cases/{id}/accuse` | Acusação final |
| GET | `/api/cases/{id}/solution` | Solução + cruzamento completo |

## Inicialização do banco

A ordem importa por causa de uma limitação do SQL Server 2025 (ver [[Busca vetorial]]):

1. Migration `InitialCreate` (tabelas e coluna `vector(256)`)
2. Seed dos casos a partir de `Seed/Cases/*.json`
3. Migration `StatementVectorIndex` (liga `PREVIEW_FEATURES` e cria o índice DiskANN)
4. Uma busca de teste define se o modo vetorial é aproximado ou exato
