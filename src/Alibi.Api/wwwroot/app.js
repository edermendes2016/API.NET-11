"use strict";

const app = document.getElementById("app");
const dialog = document.getElementById("verdict");
const dialogBody = document.getElementById("verdict-body");

const SUGGESTIONS = [
  "Onde você estava às 21h?",
  "Conte sua noite.",
  "Qual sua relação com a vítima?",
  "Você viu algo estranho?",
  "Quem você acha que matou?",
];

const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) =>
  ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const hhmm = (t) => (t ?? "").slice(0, 5);
const shortName = (name) => name.replace(/^(Dra?\.|Prof\.)\s*/, "").split(" ")[0];
const initials = (name) => name.replace(/^(Dra?\.|Prof\.)\s*/, "").split(" ").map((p) => p[0]).slice(0, 2).join("");

// ── estado local por caso (progresso do jogador) ────────────────────────────
const store = {
  load(caseId) {
    try { return JSON.parse(localStorage.getItem(`alibi:${caseId}`)) ?? null; } catch { return null; }
  },
  save(caseId, state) {
    try { localStorage.setItem(`alibi:${caseId}`, JSON.stringify(state)); } catch { /* modo privado */ }
  },
  reset(caseId) { try { localStorage.removeItem(`alibi:${caseId}`); } catch { } },
};
const freshState = () => ({ revealed: [], chats: {}, notes: [], solved: false });

async function api(path, body) {
  const res = await fetch(`/api${path}`, body === undefined ? {} : {
    method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.json();
}

// ── status (IA / busca vetorial) ────────────────────────────────────────────
async function loadStatus() {
  try {
    const s = await api("/status");
    document.getElementById("status").innerHTML =
      `<span class="pill">Interrogatório: <b>${s.aiMode === "claude" ? "IA (Claude)" : "offline (busca vetorial)"}</b></span>` +
      `<span class="pill">Vector search: <b>${s.vectorSearchMode === "Approximate" ? "aproximada (DiskANN)" : "exata"}</b></span>`;
  } catch { /* ignora */ }
}

// ── tela 1: arquivo de casos ────────────────────────────────────────────────
async function renderHome() {
  app.innerHTML = `<p class="hint"><span class="spinner"></span> Abrindo o arquivo…</p>`;
  const cases = await api("/cases");
  app.innerHTML = `
    <h1>Arquivo de casos</h1>
    <p class="lede">Cada suspeito declarou um álibi. A verdade está no banco de dados.
      Interrogue, colete provas e confronte depoimentos até encontrar a contradição no horário do crime.</p>
    <div class="cases">
      ${cases.map((c) => {
        const solved = store.load(c.id)?.solved;
        return `<button class="folder" data-id="${c.id}">
          <span class="stamp ${solved ? "solved" : ""}">${solved ? "RESOLVIDO" : esc(c.difficulty.toUpperCase())}</span>
          <h2>${esc(c.title)}</h2>
          <p><strong>Vítima:</strong> ${esc(c.victim)}</p>
          <p>${esc(c.setting)}</p>
        </button>`;
      }).join("")}
    </div>`;
  app.querySelectorAll(".folder").forEach((el) => el.addEventListener("click", () => {
    location.hash = `#/caso/${el.dataset.id}`;
  }));
}

// ── tela 2: investigação ────────────────────────────────────────────────────
let current = null; // { data, state, suspectId, evidenceId }

async function renderCase(caseId) {
  app.innerHTML = `<p class="hint"><span class="spinner"></span> Carregando o caso…</p>`;
  const data = await api(`/cases/${caseId}`);
  const state = store.load(caseId) ?? freshState();
  current = { data, state, suspectId: data.suspects[0].id, evidenceId: null };
  drawCase();
}

function allEvidence() {
  const { data, state } = current;
  const ids = new Set(data.evidence.map((e) => e.id));
  return [...data.evidence, ...state.revealed.filter((e) => !ids.has(e.id))];
}

function drawCase() {
  const { data, state } = current;
  const suspect = data.suspects.find((s) => s.id === current.suspectId);
  const crime = hhmm(data.crimeTime);
  const times = ["20:00", "20:30", "21:00", "21:30", "22:00"];

  app.innerHTML = `
    <div class="case-head">
      <div>
        <h1>${esc(data.title)}</h1>
        <div class="meta">${esc(data.setting)}</div>
      </div>
      <div style="display:flex;gap:8px;flex-wrap:wrap">
        <button class="btn ghost small" id="back">← Arquivo</button>
        <button class="btn ghost small" id="giveup">Desistir e ver solução</button>
        <button class="btn ghost small" id="reset">Recomeçar</button>
      </div>
    </div>

    <div class="case-grid">
      <section class="panel" aria-label="Dossiê">
        <h3>Dossiê</h3>
        <div class="dossier">
          <p>${esc(data.summary)}</p>
          <p><strong>Vítima:</strong> ${esc(data.victim)}<br>
             <strong>Cena:</strong> ${esc(data.crimeScene)} · <strong>Hora:</strong> ${crime}</p>
        </div>
        <div class="suspects" role="list">
          ${data.suspects.map((s) => `
            <button class="suspect ${s.id === suspect.id ? "active" : ""}" data-id="${s.id}" role="listitem">
              <span class="avatar">${esc(initials(s.name))}</span>
              <span>${esc(s.name)}<small>${esc(s.role)}</small></span>
            </button>`).join("")}
        </div>
      </section>

      <section class="panel" aria-label="Interrogatório">
        <h3>${esc(suspect.name)} <small class="hint">${esc(suspect.role)}</small></h3>
        <p class="hint" style="margin-bottom:6px">${esc(suspect.description)}</p>
        <details>
          <summary class="hint" style="cursor:pointer">Álibi declarado</summary>
          <ul class="timeline">
            ${times.map((t) => {
              const a = suspect.alibi.find((x) => hhmm(x.time) === t);
              return `<li class="${t === crime ? "crime" : ""}"><time>${t}</time>
                ${a ? `<span><span class="where">${esc(a.location)}</span> — <span class="say">${esc(a.statement)}</span></span>`
                    : `<span class="missing">(não disse nada sobre esse horário)</span>`}</li>`;
            }).join("")}
          </ul>
        </details>
        <div class="chat">
          <div class="chat-log" id="log" aria-live="polite"></div>
          <div class="chips">${SUGGESTIONS.map((q) => `<button class="chip">${esc(q)}</button>`).join("")}</div>
          <form class="ask" id="ask">
            <label class="sr-only" for="q">Pergunta</label>
            <input id="q" autocomplete="off" maxlength="500" placeholder="Pergunte qualquer coisa a ${esc(shortName(suspect.name))}…">
            <button class="btn" type="submit">Perguntar</button>
          </form>
        </div>
      </section>

      <section class="panel board" aria-label="Quadro de provas">
        <h3>Provas <small class="hint">${allEvidence().length} encontradas</small></h3>
        <p class="hint">Interrogue bem: algumas provas só aparecem quando alguém fala demais.</p>
        <div class="evidence" id="evidence"></div>
        <div class="actions">
          <button class="btn ghost" id="confront" disabled>Confrontar ${esc(shortName(suspect.name))} com a prova</button>
          <button class="btn danger" id="accuse" disabled>Acusar ${esc(shortName(suspect.name))}</button>
          <p class="hint">Para fechar o caso, acuse o culpado apresentando a prova que derruba o álibi no horário do crime.</p>
        </div>
        <h3 style="margin-top:16px">Caderno</h3>
        <ul class="notes" id="notes">${state.notes.length ? "" : `<li class="hint">Nenhuma anotação ainda.</li>`}</ul>
      </section>
    </div>`;

  drawLog();
  drawEvidence();
  drawNotes();

  app.querySelector("#back").onclick = () => (location.hash = "");
  app.querySelector("#reset").onclick = () => { store.reset(data.id); renderCase(data.id); };
  app.querySelector("#giveup").onclick = giveUp;
  app.querySelectorAll(".suspect").forEach((el) => el.onclick = () => { current.suspectId = +el.dataset.id; drawCase(); });
  app.querySelectorAll(".chip").forEach((el) => el.onclick = () => ask(el.textContent));
  app.querySelector("#ask").onsubmit = (e) => { e.preventDefault(); const q = app.querySelector("#q"); ask(q.value); q.value = ""; };
  app.querySelector("#confront").onclick = confront;
  app.querySelector("#accuse").onclick = accuse;
}

function drawLog() {
  const log = app.querySelector("#log");
  const msgs = current.state.chats[current.suspectId] ?? [];
  if (!msgs.length) {
    log.innerHTML = `<p class="chat-empty">Sente-se. Feche a porta. Faça a primeira pergunta.</p>`;
    return;
  }
  const label = { scripted: "depoimento (busca vetorial)", ai: "resposta gerada por IA", stonewall: "o suspeito se fechou" };
  log.innerHTML = msgs.map((m) => m.role === "system"
    ? `<div class="msg system">${esc(m.text)}</div>`
    : `<div class="msg ${m.role === "suspect" ? "reply" : m.role}">${esc(m.text)}${m.kind ? `<span class="src">${label[m.kind] ?? ""}${m.distance != null ? ` · distância ${m.distance.toFixed(2)}` : ""}</span>` : ""}</div>`
  ).join("");
  log.scrollTop = log.scrollHeight;
}

function drawEvidence() {
  const box = app.querySelector("#evidence");
  box.innerHTML = allEvidence().map((e) => `
    <button class="card ${e.id === current.evidenceId ? "selected" : ""} ${e._new ? "new" : ""}" data-id="${e.id}">
      <strong>${esc(e.title)}</strong><span>${esc(e.description)}</span>
    </button>`).join("");
  box.querySelectorAll(".card").forEach((el) => el.onclick = () => {
    current.evidenceId = current.evidenceId === +el.dataset.id ? null : +el.dataset.id;
    drawEvidence();
  });
  const has = current.evidenceId != null;
  app.querySelector("#confront").disabled = !has;
  app.querySelector("#accuse").disabled = !has;
}

function drawNotes() {
  const notes = current.state.notes;
  if (!notes.length) return;
  app.querySelector("#notes").innerHTML = notes.slice().reverse()
    .map((n) => `<li class="${n.kind}">${esc(n.text)}</li>`).join("");
}

function persist() { store.save(current.data.id, current.state); }

async function ask(question) {
  question = question.trim();
  if (!question) return;
  const sid = current.suspectId;
  const chat = (current.state.chats[sid] ??= []);
  const history = chat.filter((m) => m.role !== "system").map((m) => ({ role: m.role, text: m.text }));
  chat.push({ role: "detective", text: question });
  drawLog();
  const log = app.querySelector("#log");
  log.insertAdjacentHTML("beforeend", `<div class="msg reply" id="typing"><span class="spinner"></span></div>`);
  log.scrollTop = log.scrollHeight;

  try {
    const r = await api(`/cases/${current.data.id}/suspects/${sid}/interrogate`, { question, history });
    chat.push({ role: "suspect", text: r.text, kind: r.kind, distance: r.distance });
    const ev = r.revealedEvidence;
    if (ev && !allEvidence().some((e) => e.id === ev.id)) {
      current.state.revealed.push(ev);
      chat.push({ role: "system", text: `Nova prova: ${ev.title}` });
      ev._new = true;
    }
  } catch (err) {
    chat.push({ role: "system", text: `Falha ao interrogar: ${err.message}` });
  }
  persist();
  if (sid === current.suspectId) { drawLog(); drawEvidence(); }
  current.state.revealed.forEach((e) => delete e._new);
}

async function confront() {
  const { data, suspectId, evidenceId } = current;
  const v = await api(`/cases/${data.id}/confront`, { suspectId, evidenceId });
  note(v);
  showVerdict(v, false);
}

async function accuse() {
  const { data, suspectId, evidenceId } = current;
  const v = await api(`/cases/${data.id}/accuse`, { suspectId, evidenceIds: [evidenceId] });
  note(v);
  if (v.outcome === "CaseSolved") { current.state.solved = true; persist(); }
  showVerdict(v, true);
}

function note(v) {
  const kind = v.outcome === "ContradictionFound" || v.outcome === "CaseSolved" ? "lie" : v.outcome === "AlibiHolds" ? "ok" : "";
  current.state.notes.push({ kind, text: v.message });
  persist();
  drawNotes();
}

async function giveUp() {
  const d = await api(`/cases/${current.data.id}/solution`);
  showVerdict({ outcome: "InsufficientEvidence", headline: "Solução do caso", message: "", suspect: d.culprit, debrief: d }, true, true);
}

const OUTCOME_LABEL = {
  CaseSolved: "CaseSolved", ContradictionFound: "ContradictionFound",
  AlibiHolds: "AlibiHolds", InsufficientEvidence: "InsufficientEvidence",
};

function showVerdict(v, isAccusation, gaveUp = false) {
  const d = v.debrief;
  dialogBody.className = `verdict-body o-${v.outcome}`;
  dialogBody.innerHTML = `
    ${gaveUp ? "" : `<span class="tag">AccusationResult.${OUTCOME_LABEL[v.outcome]}</span>`}
    <div class="outcome">${esc(v.headline)}</div>
    ${v.message ? `<p>${esc(v.message)}</p>` : ""}
    ${!gaveUp && isAccusation && v.outcome !== "CaseSolved" ? `<p class="hint">A acusação não se sustentou. Continue investigando.</p>` : ""}
    ${d ? `
      <h3 style="margin-top:18px">O que aconteceu</h3>
      <p><strong>Culpado(a): ${esc(d.culprit)}.</strong> ${esc(d.solution)}</p>
      <h3 style="margin-top:18px">Linha do tempo real × álibis (FULL OUTER JOIN)</h3>
      <div class="table-wrap"><table>
        <thead><tr><th>Suspeito</th><th>Hora</th><th>Onde estava</th><th>Onde disse estar</th><th></th></tr></thead>
        <tbody>${d.crossCheck.map((r) => `<tr>
          <td>${esc(r.suspect)}</td><td>${hhmm(r.time)}</td><td>${esc(r.realLocation ?? "—")}</td>
          <td>${esc(r.claimedLocation ?? "—")}</td><td class="v-${esc(r.verdict)}">${esc(r.verdict)}</td></tr>`).join("")}
        </tbody></table></div>
      <p class="sqlnote">Gerado por <code>truth.FullJoin(claims, …)</code> no EF Core 11 → <code>FULL JOIN</code> no SQL Server.</p>` : ""}`;
  dialog.showModal();
  dialogBody.focus();
  dialog.scrollTop = 0;
}

// ── roteamento ──────────────────────────────────────────────────────────────
async function route() {
  const m = location.hash.match(/^#\/caso\/(\d+)/);
  try {
    if (m) await renderCase(+m[1]); else await renderHome();
  } catch (err) {
    app.innerHTML = `<div class="panel"><h3>Não consegui falar com a API</h3>
      <p class="hint">${esc(err.message)}. O SQL Server pode ainda estar iniciando — tente de novo em alguns segundos.</p>
      <button class="btn" onclick="location.reload()">Tentar de novo</button></div>`;
  }
  window.scrollTo(0, 0);
}
document.getElementById("home-link").onclick = (e) => { e.preventDefault(); location.hash = ""; };
window.addEventListener("hashchange", route);
loadStatus();
route();
