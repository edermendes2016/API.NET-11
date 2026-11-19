# Criando um caso

Cada caso é um arquivo em `src/Alibi.Infrastructure/Seed/Cases/*.json`, carregado na subida. Esqueleto:

```json
{
  "slug": "meu-caso",
  "title": "Meu Caso",
  "difficulty": "Médio",
  "setting": "Onde e quando",
  "victim": "Nome, profissão, idade",
  "crimeScene": "Biblioteca",
  "crimeTime": "21:00",
  "summary": "O que o jogador lê no dossiê",
  "solution": "O que aconteceu de verdade",
  "locations": ["Biblioteca", "Jardim"],
  "suspects": [
    {
      "key": "ana",
      "name": "Ana Souza",
      "role": "Sócia",
      "description": "O que o jogador vê",
      "personality": "Como fala (usado pela IA)",
      "secret": "O que esconde (só a IA vê)",
      "isCulprit": true,
      "evasive": "Frase quando a pergunta não faz sentido",
      "truth": { "20:00": "Jardim", "21:00": "Biblioteca" },
      "alibi": { "20:00": { "location": "Jardim", "statement": "..." },
                 "21:00": { "location": "Jardim", "statement": "..." } },
      "statements": [
        { "topic": "21h", "ask": ["onde voce estava as 21h", "as nove horas onde estava"],
          "answer": "No jardim, claro.", "reveals": null }
      ]
    }
  ],
  "evidence": [
    { "code": "E1", "title": "Câmera", "description": "...", "suspect": "ana",
      "time": "21:00", "location": "Biblioteca", "visible": false }
  ]
}
```

## Regras (verificadas pelos testes)

- Exatamente um culpado, que estava na cena do crime no horário do crime e declarou outro lugar.
- Toda prova com suspeito precisa bater com o `truth` dele.
- Toda prova com `"visible": false` precisa ser revelada por alguma fala (`"reveals": "E1"`).
- O caso precisa ser solucionável com as próprias provas.
- As perguntas sugeridas na interface precisam encontrar resposta para todo suspeito.

Dicas: dê a pelo menos um inocente uma mentira no horário do crime (com outro motivo), e use `ask` com várias formas de perguntar a mesma coisa, porque é isso que vira o embedding.

Como as falas ficam numa tabela com índice vetorial (somente leitura no preview), um caso novo exige recriar o banco: `docker compose down -v && docker compose up --build`.
