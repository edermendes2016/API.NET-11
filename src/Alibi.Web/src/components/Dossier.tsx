import { hhmm, initials, type CaseFile } from "../api";

interface Props { data: CaseFile; activeId: number; onSelect: (id: number) => void }

export default function Dossier({ data, activeId, onSelect }: Props) {
  return (
    <section className="panel" aria-label="Dossiê">
      <h3>Dossiê</h3>
      <div className="dossier">
        <p>{data.summary}</p>
        <p>
          <strong>Vítima:</strong> {data.victim}<br />
          <strong>Cena:</strong> {data.crimeScene} · <strong>Hora:</strong> {hhmm(data.crimeTime)}
        </p>
      </div>
      <div className="suspects" role="list">
        {data.suspects.map((s) => (
          <button key={s.id} role="listitem" className={`suspect ${s.id === activeId ? "active" : ""}`} onClick={() => onSelect(s.id)}>
            <span className="avatar">{initials(s.name)}</span>
            <span>{s.name}<small>{s.role}</small></span>
          </button>
        ))}
      </div>
    </section>
  );
}
