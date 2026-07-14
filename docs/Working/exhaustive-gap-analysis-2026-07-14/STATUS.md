# Exhaustive gap analysis — STATUS (resume surface)

**Purpose**: exhaustive cell-probe verification of Precept's specified-but-maybe-unbuilt behavior, rebuilt after the prior probe run (447 verdicts) was found untrustworthy. This bundle is the committed home for the harness, evidence, and (eventually) the cell corpus that back that rebuild.

- Plan: `~/.claude/plans/do-we-need-to-synchronous-meerkat.md`
- Reconciliation target: `docs/Working/gap-ledger-2026-07-13.md` (the 72-gap ledger — trustworthy entries this work reconciles against; do not edit that file from this bundle)

## Bundle index

| Path | What it is |
|---|---|
| `runner/Program.cs` | The C# cell runner: reads a `[{id,text}]` manifest, compiles each cell's `.precept` text through the full pipeline, emits per-cell diagnostic codes as JSON. |
| `runner/runner.csproj` | Project file for the runner. `ProjectReference` points at `../../../../src/Precept/Precept.csproj` (relative, from this bundle location) — verified to build and run correctly from here, and verified not to break `dotnet build` at the repo root. |
| `calibration/RESULTS.md` | Calibration harness results notes from the prior run. |
| `calibration/calibration-cells.json` | The calibration cell set used to validate the runner/scoring pipeline before the full run. |
| `calibration/score.py` | Scoring script: compares runner output against expected verdicts. |
| `calibration/build_cells.py` | Script that built the calibration cell set. |
| `calibration/diagnostic_code_map.tsv` | Diagnostic-code reference map used during calibration/scoring. |
| `evidence/divergences.json` | Cases from the prior full run where actual diagnostics diverged from expected. |
| `evidence/recovered-corpus.json` | The ~3,446-cell corpus recovered from the prior run's enumeration + text-extraction passes (`gid`, `id`, `exercise`, `expected`, `specCite`, `recovered_text`, `map_method`, `ambiguous`). This is a **starting DRAFT**, not yet imported/re-verified into `cells/`. |
| `evidence/exhaustive-cell-probe-register-historical-leads.md` | Historical register of probe leads from an earlier (separate) probing pass — kept as leads to cross-check, not as verified verdicts. |
| `evidence/compiler-crashes.md` | 6 cells that crashed the compiler on a D26 structural-invariant violation (`TypedErrorExpression present but no Error diagnostic`) rather than returning a diagnostic — all involve `choice of number(...)`/`choice of decimal(...)` domains with scientific-notation literals. Candidate real compiler bugs; not yet triaged or filed. |
| `assembled/all-cells.json` | Phase 1 output — the merged, globally-deduped cell corpus (4,592 cells, single JSON array, 0 id collisions). This is the Phase 2 input. |
| `assembled/<unit>.cells.json` | Per-unit cell files (nine units) — concatenated into `all-cells.json`. |
| `coverage/<unit>.md` | Per-unit block inventories (nine units) — one row per doc block with COVERED/ADDED/NO-BEHAVIOR disposition. |
| `coverage-report.md` | Phase 1 merged rollup: per-unit block accounting, completeness proof, open stop-and-fix items, honest-limit statement. |
| `STATUS.md` | This file. |

## Phase state

**Phase 0: COMPLETE** (consolidation). **Phase 1: COMPLETE** — block→cell enumeration finished; 100% of blocks accounted in all nine units (`covered + added + no-behavior == total` for every unit); 4,592 cells merged into `assembled/all-cells.json` with zero id collisions. Two units carry **open stop-and-fix items** (doc-internal contradictions needing owner/author ruling before their affected cells can be verdicted — see `coverage-report.md § Open stop-and-fix`): `primitive-types` (integer-overflow stance; `pow` negative-exponent lane scope) and `business-domain` (stale Implementation Scope vs. retired D10; the `:1576` bounds-qualifier example). These do not block enumeration; they are inputs Phase 2 must resolve. **Phase 2: NEXT** — deterministic per-cell measurement of `all-cells.json` through the runner, verdict derived from diagnostic codes.

See `coverage-report.md` for the full per-unit rollup and `assembled/all-cells.json` for the merged corpus.

## Counts

- cells enumerated: **4,592** (merged into `assembled/all-cells.json`; 3,413 `source:"draft"` + 1,179 `source:"added"`; 0 id collisions). Per-unit breakdown: spec-preamble-lexer 187, spec-parser 286, spec-typecheck 433, spec-semantics-proof 51, primitive-types 1,047, temporal 657, business-domain 852, collection 960, proof-engine-prevention 119. Blocks accounted: 888 total across the nine units.
- cells checked: 0 (Phase 2)

## Scope

Cell sources = the 5 language docs (`docs/language/precept-language-spec.md`, `docs/language/primitive-types.md`, `docs/language/temporal-type-system.md`, `docs/language/business-domain-types.md`, `docs/language/collection-types.md`) plus the prevention behaviors described in `docs/compiler/proof-engine.md`. Cross-checks against `docs/compiler/diagnostic-system.md`, `docs/language/literal-system.md` (or equivalent), and `docs/language/precept-grammar.md` (or equivalent grammar doc) for consistency. Unbuilt-but-specified behavior counts as a GAP — a doc describing behavior the compiler doesn't yet implement is not a pass, it's a finding.

## Running the runner

```
dotnet run --project runner -- <manifest.json>
```

- Input: a JSON array `[{"id": "...", "text": "..."}, ...]` where `text` is a full `.precept` definition body.
- Output: JSON array, one entry per cell, each with the per-cell error codes pulled directly from the diagnostic array the compiler returns (`errorCodes`, plus the full `allDiagnostics` list with code/severity/stage) — never a hand-written summary. Verdicts must be derived from these codes, not asserted from memory or from a prior run's notes.
