import { useCallback, useEffect, useState } from "react";
import type { Evidence, ReplyKind } from "./api";

// Progresso do jogador por caso, guardado no navegador (localStorage).
export type Message =
  | { role: "detective"; text: string }
  | { role: "suspect"; text: string; kind: ReplyKind; distance: number | null }
  | { role: "system"; text: string };

export interface Note { kind: "lie" | "ok" | ""; text: string }

export interface Progress {
  revealed: Evidence[];
  chats: Record<number, Message[]>;
  notes: Note[];
  solved: boolean;
}

const empty = (): Progress => ({ revealed: [], chats: {}, notes: [], solved: false });
const key = (caseId: number) => `alibi:${caseId}`;

export function loadProgress(caseId: number): Progress {
  try {
    const raw = localStorage.getItem(key(caseId));
    return raw ? { ...empty(), ...JSON.parse(raw) } : empty();
  } catch {
    return empty();
  }
}

export function useProgress(caseId: number) {
  const [progress, setProgress] = useState<Progress>(() => loadProgress(caseId));

  useEffect(() => setProgress(loadProgress(caseId)), [caseId]);

  const update = useCallback((change: (p: Progress) => Progress) => {
    setProgress((prev) => {
      const next = change(prev);
      try { localStorage.setItem(key(caseId), JSON.stringify(next)); } catch { /* modo privado */ }
      return next;
    });
  }, [caseId]);

  const reset = useCallback(() => {
    try { localStorage.removeItem(key(caseId)); } catch { /* ignora */ }
    setProgress(empty());
  }, [caseId]);

  return { progress, update, reset };
}
