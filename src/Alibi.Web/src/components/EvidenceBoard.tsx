import { shortName, type Evidence } from "../api";
import type { Note } from "../progress";

interface Props {
  evidence: Evidence[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  suspectName: string;
  notes: Note[];
  onConfront: () => void;
  onAccuse: () => void;
}

export default function EvidenceBoard({ evidence, selectedId, onSelect, suspectName, notes, onConfront, onAccuse }: Props) {
  const first = shortName(suspectName);
  const none = selectedId === null;

  return (
    <section className="panel board" aria-label="Quadro de provas">
      <h3>Provas <small className="hint">{evidence.length} encontradas</small></h3>
      <p className="hint">Interrogue bem: algumas provas só aparecem quando alguém fala demais.</p>
      <div className="evidence">
        {evidence.map((e) => (
          <button key={e.id} className={`card new ${e.id === selectedId ? "selected" : ""}`} onClick={() => onSelect(e.id)}>
            <strong>{e.title}</strong><span>{e.description}</span>
          </button>
        ))}
      </div>
      <div className="actions">
        <button className="btn ghost" disabled={none} onClick={onConfront}>Confrontar {first} com a prova</button>
        <button className="btn danger" disabled={none} onClick={onAccuse}>Acusar {first}</button>
        <p className="hint">Para fechar o caso, acuse o culpado apresentando a prova que derruba o álibi no horário do crime.</p>
      </div>
      <h3 style={{ marginTop: 16 }}>Caderno</h3>
      <ul className="notes">
        {notes.length === 0
          ? <li className="hint">Nenhuma anotação ainda.</li>
          : [...notes].reverse().map((n, i) => <li key={i} className={n.kind}>{n.text}</li>)}
      </ul>
    </section>
  );
}
