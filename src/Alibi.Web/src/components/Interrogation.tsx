import { useEffect, useRef, useState, type FormEvent } from "react";
import { api, hhmm, shortName, type ChatTurn, type Evidence, type Suspect } from "../api";
import type { Message, Progress } from "../progress";

const SUGGESTIONS = [
  "Onde você estava às 21h?",
  "Conte sua noite.",
  "Qual sua relação com a vítima?",
  "Você viu algo estranho?",
  "Quem você acha que matou?",
];
const TIMES = ["20:00", "20:30", "21:00", "21:30", "22:00"];
const SOURCE = { scripted: "depoimento (busca vetorial)", ai: "resposta gerada por IA", stonewall: "o suspeito se fechou" };

interface Props {
  caseId: number;
  suspect: Suspect;
  crimeTime: string;
  messages: Message[];
  knownEvidence: Evidence[];
  update: (change: (p: Progress) => Progress) => void;
}

export default function Interrogation({ caseId, suspect, crimeTime, messages, knownEvidence, update }: Props) {
  const [question, setQuestion] = useState("");
  const [waiting, setWaiting] = useState(false);
  const log = useRef<HTMLDivElement>(null);

  useEffect(() => { log.current?.scrollTo(0, log.current.scrollHeight); }, [messages.length, waiting]);

  const push = (...added: Message[]) => update((p) => ({
    ...p, chats: { ...p.chats, [suspect.id]: [...(p.chats[suspect.id] ?? []), ...added] },
  }));

  const ask = async (text: string) => {
    text = text.trim();
    if (!text || waiting) return;
    const history: ChatTurn[] = messages.flatMap((m) => (m.role === "system" ? [] : [{ role: m.role, text: m.text }]));
    push({ role: "detective", text });
    setQuestion("");
    setWaiting(true);
    try {
      const r = await api.interrogate(caseId, suspect.id, text, history);
      const reply: Message = { role: "suspect", text: r.text, kind: r.kind, distance: r.distance };
      const ev = r.revealedEvidence;
      if (ev && !knownEvidence.some((e) => e.id === ev.id)) {
        push(reply, { role: "system", text: `Nova prova: ${ev.title}` });
        update((p) => ({ ...p, revealed: [...p.revealed, ev] }));
      } else {
        push(reply);
      }
    } catch (e) {
      push({ role: "system", text: `Falha ao interrogar: ${(e as Error).message}` });
    } finally {
      setWaiting(false);
    }
  };

  const submit = (e: FormEvent) => { e.preventDefault(); ask(question); };
  const first = shortName(suspect.name);

  return (
    <section className="panel" aria-label="Interrogatório">
      <h3>{suspect.name} <small className="hint">{suspect.role}</small></h3>
      <p className="hint" style={{ marginBottom: 6 }}>{suspect.description}</p>
      <details>
        <summary className="hint" style={{ cursor: "pointer" }}>Álibi declarado</summary>
        <ul className="timeline">
          {TIMES.map((t) => {
            const a = suspect.alibi.find((x) => hhmm(x.time) === t);
            return (
              <li key={t} className={t === hhmm(crimeTime) ? "crime" : ""}>
                <time>{t}</time>
                {a
                  ? <span><span className="where">{a.location}</span> — <span className="say">{a.statement}</span></span>
                  : <span className="missing">(não disse nada sobre esse horário)</span>}
              </li>
            );
          })}
        </ul>
      </details>

      <div className="chat">
        <div className="chat-log" ref={log} aria-live="polite">
          {messages.length === 0 && !waiting && <p className="chat-empty">Sente-se. Feche a porta. Faça a primeira pergunta.</p>}
          {messages.map((m, i) =>
            m.role === "system" ? <div key={i} className="msg system">{m.text}</div>
            : m.role === "detective" ? <div key={i} className="msg detective">{m.text}</div>
            : (
              <div key={i} className="msg reply">
                {m.text}
                <span className="src">{SOURCE[m.kind]}{m.distance != null && ` · distância ${m.distance.toFixed(2)}`}</span>
              </div>
            ))}
          {waiting && <div className="msg reply"><span className="spinner" /></div>}
        </div>
        <div className="chips">
          {SUGGESTIONS.map((s) => <button key={s} className="chip" onClick={() => ask(s)} disabled={waiting}>{s}</button>)}
        </div>
        <form className="ask" onSubmit={submit}>
          <label className="sr-only" htmlFor="q">Pergunta</label>
          <input id="q" autoComplete="off" maxLength={500} value={question}
                 onChange={(e) => setQuestion(e.target.value)} placeholder={`Pergunte qualquer coisa a ${first}…`} />
          <button className="btn" type="submit" disabled={waiting || !question.trim()}>Perguntar</button>
        </form>
      </div>
    </section>
  );
}
