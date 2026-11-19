import { useEffect, useState } from "react";
import { api, type Status } from "./api";
import CaseList from "./components/CaseList";
import CaseView from "./components/CaseView";

function useHashRoute() {
  const read = () => {
    const m = location.hash.match(/^#\/caso\/(\d+)/);
    return m ? Number(m[1]) : null;
  };
  const [caseId, setCaseId] = useState<number | null>(read);
  useEffect(() => {
    const onChange = () => { setCaseId(read()); window.scrollTo(0, 0); };
    window.addEventListener("hashchange", onChange);
    return () => window.removeEventListener("hashchange", onChange);
  }, []);
  return caseId;
}

export default function App() {
  const caseId = useHashRoute();
  const [status, setStatus] = useState<Status | null>(null);

  useEffect(() => { api.status().then(setStatus).catch(() => {}); }, []);

  return (
    <>
      <header className="topbar">
        <a className="brand" href="#">ÁLIBI<span>arquivo de casos</span></a>
        {status && (
          <div className="status">
            <span className="pill">Interrogatório: <b>{status.aiMode === "claude" ? "IA (Claude)" : "offline (busca vetorial)"}</b></span>
            <span className="pill">Vector search: <b>{status.vectorSearchMode === "Approximate" ? "aproximada (DiskANN)" : "exata"}</b></span>
          </div>
        )}
      </header>

      <main>{caseId === null ? <CaseList /> : <CaseView key={caseId} caseId={caseId} />}</main>

      <footer className="foot">
        .NET 11 · C# 15 union types · EF Core 11 FullJoin · SQL Server 2025 VECTOR · React ·{" "}
        <a href="/docs" target="_blank" rel="noopener">API (Scalar)</a>
      </footer>
    </>
  );
}
