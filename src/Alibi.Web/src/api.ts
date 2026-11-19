// Tipos espelham os DTOs de src/Alibi.Api/GameEndpoints.cs

export type Outcome = "CaseSolved" | "AlibiHolds" | "ContradictionFound" | "InsufficientEvidence";
export type ReplyKind = "scripted" | "ai" | "stonewall";

export interface Status { aiMode: "claude" | "offline"; vectorSearchMode: "Approximate" | "Exact" }
export interface CaseSummary { id: number; slug: string; title: string; difficulty: string; victim: string; setting: string }
export interface Alibi { time: string; location: string; statement: string }
export interface Suspect { id: number; name: string; role: string; description: string; alibi: Alibi[] }
export interface Evidence { id: number; code: string; title: string; description: string }
export interface CaseFile {
  id: number; title: string; difficulty: string; setting: string; victim: string; summary: string;
  crimeScene: string; crimeTime: string; suspects: Suspect[]; evidence: Evidence[];
}
export interface ChatTurn { role: "detective" | "suspect"; text: string }
export interface InterrogateResponse { kind: ReplyKind; text: string; distance: number | null; revealedEvidence: Evidence | null }
export interface CrossCheckRow { suspect: string; time: string; realLocation: string | null; claimedLocation: string | null; verdict: string }
export interface Debrief { solution: string; culprit: string; crossCheck: CrossCheckRow[] }
export interface Verdict { outcome: Outcome; headline: string; message: string; suspect: string; debrief: Debrief | null }

async function request<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`/api${path}`, body === undefined ? undefined : {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.json() as Promise<T>;
}

export const api = {
  status: () => request<Status>("/status"),
  cases: () => request<CaseSummary[]>("/cases"),
  case: (id: number) => request<CaseFile>(`/cases/${id}`),
  interrogate: (caseId: number, suspectId: number, question: string, history: ChatTurn[]) =>
    request<InterrogateResponse>(`/cases/${caseId}/suspects/${suspectId}/interrogate`, { question, history }),
  confront: (caseId: number, suspectId: number, evidenceId: number) =>
    request<Verdict>(`/cases/${caseId}/confront`, { suspectId, evidenceId }),
  accuse: (caseId: number, suspectId: number, evidenceIds: number[]) =>
    request<Verdict>(`/cases/${caseId}/accuse`, { suspectId, evidenceIds }),
  solution: (caseId: number) => request<Debrief>(`/cases/${caseId}/solution`),
};

export const hhmm = (t: string) => t.slice(0, 5);
export const shortName = (name: string) => name.replace(/^(Dra?\.|Prof\.)\s*/, "").split(" ")[0];
export const initials = (name: string) =>
  name.replace(/^(Dra?\.|Prof\.)\s*/, "").split(" ").map((p) => p[0]).slice(0, 2).join("");
