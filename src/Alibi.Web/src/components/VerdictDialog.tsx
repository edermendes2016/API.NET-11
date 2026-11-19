import { useEffect, useRef } from "react";
import { hhmm } from "../api";
import type { ShownVerdict } from "./CaseView";

export default function VerdictDialog({ shown, onClose }: { shown: ShownVerdict; onClose: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null);
  const body = useRef<HTMLDivElement>(null);
  const { verdict: v, isAccusation, gaveUp } = shown;
  const d = v.debrief;

  useEffect(() => {
    dialog.current?.showModal();
    body.current?.focus();
    if (dialog.current) dialog.current.scrollTop = 0;
  }, [shown]);

  return (
    <dialog ref={dialog} className="verdict" onClose={onClose}>
      <div ref={body} tabIndex={-1} className={`verdict-body o-${v.outcome}`}>
        {!gaveUp && <span className="tag">AccusationResult.{v.outcome}</span>}
        <div className="outcome">{v.headline}</div>
        {v.message && <p>{v.message}</p>}
        {!gaveUp && isAccusation && v.outcome !== "CaseSolved" &&
          <p className="hint">A acusação não se sustentou. Continue investigando.</p>}
        {d && (
          <>
            <h3 style={{ marginTop: 18 }}>O que aconteceu</h3>
            <p><strong>Culpado(a): {d.culprit}.</strong> {d.solution}</p>
            <h3 style={{ marginTop: 18 }}>Linha do tempo real × álibis (FULL OUTER JOIN)</h3>
            <div className="table-wrap">
              <table>
                <thead><tr><th>Suspeito</th><th>Hora</th><th>Onde estava</th><th>Onde disse estar</th><th /></tr></thead>
                <tbody>
                  {d.crossCheck.map((r, i) => (
                    <tr key={i}>
                      <td>{r.suspect}</td><td>{hhmm(r.time)}</td><td>{r.realLocation ?? "—"}</td>
                      <td>{r.claimedLocation ?? "—"}</td><td className={`v-${r.verdict}`}>{r.verdict}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="sqlnote">Gerado por <code>truth.FullJoin(claims, …)</code> no EF Core 11 → <code>FULL JOIN</code> no SQL Server.</p>
          </>
        )}
      </div>
      <form method="dialog"><button className="btn">Voltar à investigação</button></form>
    </dialog>
  );
}
