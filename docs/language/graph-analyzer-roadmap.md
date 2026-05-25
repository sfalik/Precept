# Graph Analyzer Roadmap — Deferred Modifiers

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Roadmap |
| Implementation state | Deferred — not in MVP; ship additively after the runtime gate opens |
| Scope | Compile-time graph analyses that back the 10 deferred state / event / field modifiers below |
| Related | [Precept Language Spec § 0.5](precept-language-spec.md#05-graph-analyzer-design-contract) · [Graph Analyzer Stage](../compiler/graph-analyzer.md) |

---

## Why a separate roadmap

The shipped graph-analyzer surface (spec § 0.5) covers what the runtime MVP needs: reachability, terminal detection, dead-end detection, dominator analysis for `required`, reverse-reachability for `irreversible`, and structural outcome diagnostics. Every modifier listed in this roadmap is a **pure compile-time graph property** — none of them require runtime support and none of them block runtime work. They are deliberately deferred so the runtime gate is not held up by analyses that can ship additively as Compiler 1.1.

The decision to defer was recorded in the 2026-05-24 Compiler Readiness triage. Authors who need stronger structural guarantees today can supply hand-written `rule` / `ensure` declarations that capture the same intent; the deferred modifiers are an ergonomics improvement, not a missing safety guarantee.

---

## Deferred modifiers and the analyses they require

### State modifiers

| Modifier | Meaning | Required analysis |
|---|---|---|
| `guarded` | Every incoming transition into this state carries a `when` guard | Incoming-edge analysis per state |
| `entry` | The event fires only from `initial` (the state's first reachable position) | Incoming-edge analysis (single source = initial) |
| `isolated` | The event fires from exactly one state | Incoming-edge cardinality per event |
| `universal` | The event fires from every reachable non-terminal state | Incoming-edge cardinality per event over the reachable set |

### Field / row modifiers

| Modifier | Meaning | Required analysis |
|---|---|---|
| `sealed after <State>` | No mutation to this field after the named state is entered | Reachability from the named state forward + row-partition analysis on assignments |
| `writeonce` | Field is set at most once across all reachable transition rows | Row-partition analysis across the reachable graph |

### Outcome-shape modifiers

| Modifier | Meaning | Required analysis |
|---|---|---|
| `advancing` | Every success outcome is a state transition (no `no transition`) | Outcome-type analysis per (state, event) pair |
| `settling` | Every success outcome is `no transition` (no state change) | Outcome-type analysis per (state, event) pair |
| `completing` | Transitions only into terminal states | Outcome-type analysis + terminal classification |
| `absorbing` | Event handlers never transition out of the current state | Outcome-type analysis per (state, event) pair |

---

## Why these were deferred (not dropped)

- **None require runtime support.** Each modifier is a structural claim about the static graph; metadata exposure on descriptors (if needed later by tooling) is additive and does not affect the runtime contract.
- **None replace existing diagnostics.** Reject-only pairs, events-that-never-succeed, dead-ends, and unreachables are already reported. The deferred modifiers strengthen the *author-declared intent* layer, letting the compiler cross-check intent against graph structure.
- **Author workarounds exist.** Until each modifier ships, authors can supply the same intent through `rule` / `ensure` / explicit `when` guards. The deferred work is an ergonomics + clarity improvement, not a missing safety guarantee.

---

## Ship order (proposed)

A future Compiler 1.1 pass can take these in any order, but a sensible ramp is:

1. **Outcome-shape modifiers** (`advancing`, `settling`, `completing`, `absorbing`) — they share outcome-type analysis and have the smallest surface.
2. **Incoming-edge modifiers** (`guarded`, `entry`, `isolated`, `universal`) — they share incoming-edge analysis machinery.
3. **Field / row modifiers** (`sealed after`, `writeonce`) — they require new row-partition machinery and are the largest piece.

Each modifier ships with: catalog entry (`Modifiers.cs`), graph-analyzer extension, diagnostic + scenario tests, language-server completion / hover, MCP descriptor exposure, and spec § 0.5 promotion (moving the row out of this roadmap and into the shipped contract).

---

## Why `milestone` is **not** here

The previously-documented `milestone` modifier was an undocumented synonym of the already-shipped `required` modifier — same dominator analysis, no distinct semantics. It was dropped from the spec (2026-05-24) rather than deferred. A future "milestone tracking / reporting" feature would be meaningfully different and can claim the name without collision when it is actually designed.

---

## Cross-references

- [Precept Language Spec § 0.5](precept-language-spec.md#05-graph-analyzer-design-contract) — shipped graph-analyzer contract
- [Graph Analyzer Stage](../compiler/graph-analyzer.md) — implementation stage doc
- [`ModifierKind.cs`](../../src/Precept/Language/ModifierKind.cs) — shipped modifier catalog
- [`Modifiers.cs`](../../src/Precept/Language/Modifiers.cs) — shipped modifier metadata
