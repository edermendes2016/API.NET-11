# Busca vetorial

Quando o jogador pergunta "Às nove da noite, você estava onde?", o jogo precisa achar a fala roteirizada mais parecida, sem depender de palavras exatas. Isso é feito dentro do SQL Server 2025.

## Coluna e índice

```csharp
public SqlVector<float> Embedding { get; set; }

e.Property(s => s.Embedding).HasColumnType("vector(256)");
e.HasVectorIndex(s => s.Embedding, "IX_Statements_Embedding").HasMetric("cosine"); // experimental (EF9105)
```

## Duas formas de buscar

**Aproximada** (experimental, `VectorSearch` do EF Core 11, usa o índice DiskANN):

```csharp
db.Statements
  .VectorSearch(s => s.Embedding, query, "cosine")
  .Where(r => r.Value.SuspectId == suspectId)
  .OrderBy(r => r.Distance)
  .Take(3)
  .WithApproximate();
```

**Exata** (`VECTOR_DISTANCE`):

```csharp
db.Statements
  .Where(s => s.SuspectId == suspectId)
  .OrderBy(s => EF.Functions.VectorDistance("cosine", s.Embedding, query))
  .Take(3);
```

`src/Alibi.Infrastructure/Interrogation/StatementSearch.cs`

## O que funciona hoje (testado em novembro de 2026)

| Item | Situação |
|---|---|
| Coluna `vector(256)` | Funciona |
| `VECTOR_DISTANCE` exato | Funciona, é o modo usado no Docker |
| `CREATE VECTOR INDEX` (DiskANN) | Funciona, mas exige `PREVIEW_FEATURES = ON` |
| `VectorSearch()` do EF Core 11 | **Não roda** na imagem `mssql/server:2025-latest` (CU11) |

O EF Core 11 gera a sintaxe nova `SELECT TOP(n) WITH APPROXIMATE ... FROM VECTOR_SEARCH(...)`. O SQL Server 2025 CU11 ainda só aceita a sintaxe antiga com `TOP_N` dentro de `VECTOR_SEARCH` e responde `Incorrect syntax near 'APPROXIMATE'`.

Por isso a API tenta o modo aproximado uma vez na subida e, se falhar, troca para o exato. O modo em uso aparece no topo da página e em `GET /api/status`. Quando o SQL Server aceitar a sintaxe nova, nada no código precisa mudar.

Para o tamanho do jogo (cerca de 80 falas), a busca exata é instantânea.

## Limitação do preview: tabela somente leitura

Enquanto o índice vetorial está em preview, a tabela que tem o índice não aceita INSERT, UPDATE nem DELETE (`Data modification statement failed because table 'Statements' has a vector index on it`). Por isso o seed roda **antes** da migration que cria o índice. Para adicionar um caso novo, recrie o banco (`docker compose down -v`).

## Embeddings sem chave de API

Para o jogo rodar com zero configuração, os vetores são gerados por `TextEmbedder` (`src/Alibi.Domain/TextEmbedder.cs`): normalização em português (acentos, horários como "21h"/"nove"/"21:00", alguns sinônimos), radicais das palavras e trigramas de caracteres, tudo projetado em 256 dimensões por feature hashing e normalizado.

Não é um modelo de linguagem. É determinístico, rápido e suficiente para o vocabulário de um interrogatório. Trocar por um modelo de embeddings real exige só gerar vetores de outra dimensão e ajustar `vector(N)`.
