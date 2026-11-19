import { useState } from "react";
import { api, type Verdict } from "../api";
import { useProgress } from "../progress";
import Dossier from "./Dossier";
import EvidenceBoard from "./EvidenceBoard";
import { ApiError, Loading } from "./Feedback";
import Interrogation from "./Interrogation";
import { useAsync } from "./useAsync";
import VerdictDialog from "./VerdictDialog";

export interface ShownVerdict { verdict: Verdict; isAccusation: boolean; gaveUp?: boolean }

export default function CaseView({ caseId }: { caseId: number }) {
  const { data, error } = useAsync(() => api.case(caseId), [caseId]);
  const { progress, update, reset } = useProgress(caseId);
  const [suspectId, setSuspectId] = useState<number | null>(null);
  const [evidenceId, setEvidenceId] = useState<number | null>(null);
  const [shown, setShown] = useState<ShownVerdict | null>(null);

  if (error) return <ApiError message={error} />;
  if (!data) return <Loading text="Carregando o caso…" />;

  const suspect = data.suspects.find((s) => s.id === suspectId) ?? data.suspects[0];
  const known = new Set(data.evidence.map((e) => e.id));
  const evidence = [...data.evidence, ...progress.revealed.filter((e) => !known.has(e.id))];

  const record = (v: Verdict) => update((p) => ({
    ...p,
    solved: p.solved || v.outcome === "CaseSolved",
    notes: [...p.notes, {
      kind: v.outcome === "AlibiHolds" ? "ok" : v.outcome === "InsufficientEvidence" ? "" : "lie",
      text: v.message,
    }],
  }));

  const confront = async () => {
    if (evidenceId === null) return;
    const v = await api.confront(data.id, suspect.id, evidenceId);
    record(v);
    setShown({ verdict: v, isAccusation: false });
  };

  const accuse = async () => {
    if (evidenceId === null) return;
    const v = await api.accuse(data.id, suspect.id, [evidenceId]);
    record(v);
    setShown({ verdict: v, isAccusation: true });
  };

  const giveUp = async () => {
    const d = await api.solution(data.id);
    setShown({
      verdict: { outcome: "InsufficientEvidence", headline: "Solução do caso", message: "", suspect: d.culprit, debrief: d },
      isAccusation: true, gaveUp: true,
    });
  };

  return (
    <>
      <div className="case-head">
        <div>
          <h1>{data.title}</h1>
          <div className="meta">{data.setting}</div>
        </div>
        <div className="head-actions">
          <a className="btn ghost small" href="#">← Arquivo</a>
          <button className="btn ghost small" onClick={giveUp}>Desistir e ver solução</button>
          <button className="btn ghost small" onClick={() => { reset(); setEvidenceId(null); }}>Recomeçar</button>
        </div>
      </div>

      <div className="case-grid">
        <Dossier data={data} activeId={suspect.id} onSelect={setSuspectId} />
        <Interrogation
          key={suspect.id}
          caseId={data.id}
          suspect={suspect}
          crimeTime={data.crimeTime}
          messages={progress.chats[suspect.id] ?? []}
          knownEvidence={evidence}
          update={update}
        />
        <EvidenceBoard
          evidence={evidence}
          selectedId={evidenceId}
          onSelect={(id) => setEvidenceId(id === evidenceId ? null : id)}
          suspectName={suspect.name}
          notes={progress.notes}
          onConfront={confront}
          onAccuse={accuse}
        />
      </div>

      {shown && <VerdictDialog shown={shown} onClose={() => setShown(null)} />}
    </>
  );
}
