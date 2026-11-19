import { api } from "../api";
import { loadProgress } from "../progress";
import { ApiError, Loading } from "./Feedback";
import { useAsync } from "./useAsync";

export default function CaseList() {
  const { data: cases, error } = useAsync(api.cases, []);
  if (error) return <ApiError message={error} />;
  if (!cases) return <Loading text="Abrindo o arquivo…" />;

  return (
    <>
      <h1>Arquivo de casos</h1>
      <p className="lede">
        Cada suspeito declarou um álibi. A verdade está no banco de dados. Interrogue, colete provas e
        confronte depoimentos até encontrar a contradição no horário do crime.
      </p>
      <div className="cases">
        {cases.map((c) => {
          const solved = loadProgress(c.id).solved;
          return (
            <a key={c.id} className="folder" href={`#/caso/${c.id}`}>
              <span className={`stamp ${solved ? "solved" : ""}`}>{solved ? "RESOLVIDO" : c.difficulty.toUpperCase()}</span>
              <h2>{c.title}</h2>
              <p><strong>Vítima:</strong> {c.victim}</p>
              <p>{c.setting}</p>
            </a>
          );
        })}
      </div>
    </>
  );
}
