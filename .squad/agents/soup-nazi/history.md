## Core Context

- Owns test discipline, coverage honesty, and “show me the real gap” pressure across parser, checker, runtime, MCP, language server, and analyzer surfaces.
- Treats behavioral claims as unproven until executable or source-grounded evidence closes them.
- Prefers coverage matrices and regression anchors that mirror the real AST/runtime contract instead of simplified proxies.

## Live Guidance

- Real catalog metadata is the executable language contract; test and audit against it directly.
- When a design behavior is still open, hold the suite or memo at the honest boundary instead of inventing false closure.
- Sample-level and source-inventory checks catch gaps that isolated happy-path reviews miss.

## Historical Summary

- 2026-05 work established the durable posture: convert review findings into executable coverage, keep sample-file gates alive, and make branch-wide counts honest.
- Detailed batch chronology now lives in `.squad/decisions.md`; this file keeps the long-lived testing/audit stance plus the newest high-value outcomes.

## Recent Updates

### 2026-07-14T22:35:00Z — Posture-v2 coverage matrix delivered

- Authored `docs/Working/posture-v2-support/coverage-matrix-2026-07-14.md`, inventorying 107 source items against Frank's posture replay.
- Classified the inventory as 27 present-correct, 7 hedged, 68 dropped, and 5 contradicted, giving the rewrite a concrete completeness map instead of a vibe check.
- The matrix became one of the five independent strengthening inputs that fed Frank's v2 rewrite and George's later sign-off check.

## Learnings

- Coverage reviews are most useful when they separate present-correct, hedged, dropped, and contradicted lanes instead of collapsing everything into “missing.”
- A strengthening pass should inventory the whole source set first; otherwise dropped material hides behind a few persuasive examples.
- Keep the durable testing/audit heuristics here and push detailed per-batch evidence into the decision ledger or support docs.
