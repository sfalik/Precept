# Orchestration Log Entry

### 2026-04-08T23:50:00Z — Soup Nazi exploratory MCP regression (rounds 1+2)

| Field | Value |
|-------|-------|
| **Agent routed** | Soup Nazi (Tester) |
| **Why chosen** | Soup Nazi owns test discipline and regression coverage. The exploratory round was chartered to validate the MCP regression skill methodology itself — synthesizing probes from scratch, not driving the standard sample-file-based regression pass for a specific PR. |
| **Spawn count** | 1 (sync) |

#### Spawn 1 — Exploratory compile surface coverage + runtime path coverage
| Field | Value |
|-------|-------|
| **Mode** | exploratory execution |
| **Why this mode** | Goal was methodology validation, not PR gate. Soup Nazi synthesized all inputs independently using `precept_language` as the vocabulary reference, then executed two rounds via live MCP tools. No sample files read until correcting syntax errors. |
| **Files authorized to read** | `samples/` (syntax reference only, after syntax errors encountered in test plan) |
| **File(s) agent produced** | `.squad/decisions/inbox/soup-nazi-mcp-regression-exploratory.md` |
| **Outcome** | Round 1: 18 compile probes — 15/18 passed as authored. 3 failures were test plan syntax errors (multi-line transition rows, `->when` guard placement, bare dequeue/pop), not engine bugs. Corrected versions all passed. Additional corrections: Probe 16 used wrong diagnostic code (PRECEPT008 not C13); Probe 17 had wrong expectation (zero-row terminal state produces no C50 diagnostic). Round 2: All 7 outcome kinds confirmed across 3 synthesized shapes (Approval, FeatureGate, RangeGuard): Transition, NoTransition, Rejected, ConstraintFailure, UneditableField, Update, Undefined. Verdict: PASS (engine). |

#### Key learnings captured
- Transition rows are single-line — multi-line action chains break parsing.
- `when` guard precedes the first `->`, not after it.
- `dequeue`/`pop` require `into <field>` target.
- C-prefixed constraint indices (C12, C13…) ≠ emitted PRECEPT diagnostic codes.
- C50 fires only for states with outgoing rows that can't reach another state; zero-row terminal states produce no diagnostic.
- `from any` expansion is eager at compile time — visible as separate rows in compile output.

#### Authoring notes promoted to charter
All five authoring corrections were baked into the **MCP Regression Testing** skill section in Soup Nazi's charter (`charter.md`) so they survive across sessions and agents.
