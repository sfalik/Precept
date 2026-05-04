# Frank — Cross-Cutting Execution Driver Restructure

- Date: 2026-05-04
- Requested by: Shane
- Decision surface: `docs/working/cross-cutting-decisions.md`

## What changed

I converted the top of `docs/working/cross-cutting-decisions.md` into a real execution driver.

- Added a status summary covering CC#1 through CC#26 with one current-state line per decision.
- Added a dependency map showing which canonical docs and pipeline stages each CC cluster blocks.
- Added Wave 0 through Wave 5 execution sections with assignable checklist items, ownership tags, and blocker tags.
- Preserved the prior detailed CC entries below the new driver as retained source material rather than deleting or rewriting them away.

## What each wave now means

- **Wave 0** — closed foundation; CC#1, CC#2, and CC#25 are fixed architecture.
- **Wave 1** — remaining cross-stage shape decisions that still need a single contract before multiple docs can converge.
- **Wave 2** — stage-local or lightly-coupled decisions that become mechanical once Wave 1 stops moving the shared shapes.
- **Wave 3** — burn-down of the migrated structural and catalog open questions inside the canonical docs themselves.
- **Wave 4** — final consistency pass and owner sign-off on any true residual open questions.
- **Wave 5** — delete `docs/working/` and other superseded artifacts only after the canonical set is self-sufficient.

## How Shane should use it

Open the status summary first to see which decisions are decided, which are blocked, and which canonical doc currently owns each open question.
Then work wave by wave from the checklists; the retained detailed entries are there for design context, not for coordination.
