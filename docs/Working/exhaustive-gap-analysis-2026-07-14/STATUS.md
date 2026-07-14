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
| `cells/` | Empty — Phase 1 populates this with the deterministically re-verified cell corpus (one file or entry per cell, calibration-gated, verdict derived from diagnostic codes, never from a summary). |
| `STATUS.md` | This file. |
| `coverage-report.md` | Stub — Phase 1 fills in per-doc coverage accounting. |

## Phase state

**Phase 0: COMPLETE** (this consolidation). **Phase 1: NEXT.**

## Counts

- cells enumerated: 0 (the ~3,446-cell starting DRAFT lives in `evidence/recovered-corpus.json`, not yet imported)
- cells checked: 0

## Scope

Cell sources = the 5 language docs (`docs/language/precept-language-spec.md`, `docs/language/primitive-types.md`, `docs/language/temporal-type-system.md`, `docs/language/business-domain-types.md`, `docs/language/collection-types.md`) plus the prevention behaviors described in `docs/compiler/proof-engine.md`. Cross-checks against `docs/compiler/diagnostic-system.md`, `docs/language/literal-system.md` (or equivalent), and `docs/language/precept-grammar.md` (or equivalent grammar doc) for consistency. Unbuilt-but-specified behavior counts as a GAP — a doc describing behavior the compiler doesn't yet implement is not a pass, it's a finding.

## Running the runner

```
dotnet run --project runner -- <manifest.json>
```

- Input: a JSON array `[{"id": "...", "text": "..."}, ...]` where `text` is a full `.precept` definition body.
- Output: JSON array, one entry per cell, each with the per-cell error codes pulled directly from the diagnostic array the compiler returns (`errorCodes`, plus the full `allDiagnostics` list with code/severity/stage) — never a hand-written summary. Verdicts must be derived from these codes, not asserted from memory or from a prior run's notes.
