# Block inventory — what the script does, and what it establishes

**Status**: recovered 2026-07-25. Never reviewed by anyone.

The thing to run is [`block-inventory.workflow.js`](block-inventory.workflow.js). This page is
about it, not a second copy of it.

## What it is and where it came from

A workflow that walks nine regions of the canonical documents, one agent per region, and makes
each agent account for every block in its region. A block is a heading section, a markdown
table, or a worked-example fenced code block. Every block gets exactly one label — it states
testable behaviour, in which case the agent enumerates the cells for it, or it states none, in
which case the agent writes a one-line reason. An unaccounted block stops the run. The
per-region numbers are added up at the end and have to reconcile.

It ran on 2026-07-14 and reported 888 blocks accounted across the five language documents and
the proof-engine document. Its output is in
`docs/Working/exhaustive-gap-analysis-2026-07-14/`, which phase 1 deletes. It existed only
inside Claude Code session state — workflow record `wf_6f0ba18d-2f4` — and was recovered into
the repository before the deletion.

Neither the original run nor the recovered script has been reviewed. The 888 is what the run
said about itself.

## The script reproduces the inventory. It does not reproduce the cells.

This matters and it is easy to miss. The block inventory was this workflow's own work — nine
agents reading documents — and it is fully reproducible from the script.

The cells are not. Of the 4,592 cells in the bundle, about 3,413 arrived as a draft corpus
written by an earlier run and only about 1,179 were written by this workflow, filling gaps in
what it was handed. The script knows how to *complete* a cell corpus; it does not know how to
*produce* one. Run it expecting cells and roughly a quarter of them arrive, with nothing saying
the rest are missing.

The draft corpus is `evidence/recovered-corpus.json` in the bundle — 3,446 cells, 1.5 MB, and
the only copy. Phase 1's disposal records the bundle's git tree object before deleting it, so it
is restorable, but it is restorable from a hash written in a disposal record and not from
anywhere a reader would look.

## The chain it belongs to

Five workflows ran on 2026-07-14, in this order. All survive in
`~/.claude/projects/-home-sfalik-source-repos-Precept/*/workflows/wf_*.json`, each with its full
`.script`. Times are UTC; agent counts and durations are each record's own `agentCount` and
`durationMs`.

| Time | Name | Agents | Duration | Record | What it produced |
|---|---|---:|---:|---|---|
| 02:32 | `wf0-spec-correction-ledger` | 4 | 9 min | `8e59697f…/wf_9ef0a9a2-6b2` | The spec correction ledger reviewed before the freeze at `342e66db` |
| 04:27 | `wf1-gap-ledger` | 9 | 57 min | `8e59697f…/wf_2e5ab5e3-7a1` | `docs/Working/gap-ledger-2026-07-13.md`, the 72-gap ledger |
| 12:16 | `wf1b-cell-by-cell-matrices` | 5 | 19 min | `8e59697f…/wf_36136135-d33` | Cell-level findings appended to that ledger, closing its deferred matrix item |
| 14:02 | `exhaustive-cell-probe` | 344 | 88 min | `8e59697f…/wf_ce837e79-949` | The 3,446-cell enumeration, **and** 447 verdicts found untrustworthy within hours |
| 21:52 | `gap-analysis-phase1` | 10 | 22 min | `d73a29fc…/wf_6f0ba18d-2f4` | The 888-block inventory, and 1,179 cells filling gaps in the enumeration above |

**The draft corpus came from `exhaustive-cell-probe`, and this is settled rather than inferred.**
The bundle's own index describes `evidence/recovered-corpus.json` as "the ~3,446-cell corpus
recovered from the prior run's enumeration + text-extraction passes… a starting DRAFT"; the
count in the file is exactly 3,446; and the shared brief in `gap-analysis-phase1` calls its
input "the reusable ~3,446-cell starting draft from a prior run".

That is the run whose verdicts were thrown out. The ruling at the time separated the two
deliberately: what the probe *said about* each cell was discarded, what it *listed* was kept.
The block inventory was then handed the list. So three quarters of the bundle's cells trace back
to a failed effort — through the half of it that was deliberately salvaged, but they trace there
all the same, and nothing has re-checked them since.

## What has to be decided before it is run again

**The proof-engine unit's scope.** Its guidance tells the agent to treat four sections of
`docs/compiler/proof-engine.md` — §6 Architecture, §7 Component Mechanics, §8 Dependencies, §9
Failure Modes — as out of scope on the grounds that they describe how the engine is built
rather than what it guarantees. That instruction wrote off 64 of that unit's 112 blocks, more
than half the document and the largest write-off anywhere in the run, and it came from a plan
that no longer exists. It is left in the script and marked with a comment. Keep it and say why,
or delete it and expect the unit to grow. Do not inherit it silently.

**Whether it is run as a workflow or as loose parallel agents.** The workflow adds the
per-region numbers up itself and reports any region whose labels do not sum to its block total.
Running the nine agents by hand gets the same nine inventories and no check. That is a real
loss and it needs deliberate compensation — somebody recomputing the sums from the written
inventories, not a reader's impression that they looked complete.

The document line ranges were re-checked against HEAD on 2026-07-25 and the script already
carries the corrections. Only one had moved: the language spec grew from 2,170 lines to 2,219,
and the semantics-and-proof region runs to the end of the file, so its end moved with it. Check
them again before any later run.

## What it establishes, and what it does not

Recorded at `coverage-report.md:63` in the bundle, and it is the honest statement:

> This is the strongest available structural check against the docs, not a mathematical proof
> of exhaustiveness. The method guarantees that every block a careful reader identified in each
> truth doc received an explicit disposition, and that every non-NO-BEHAVIOR block maps to ≥1
> concrete cell. It does not guarantee that every testable behavior was extracted from every
> block. The named residual: a behavior stated only deep in prose — a single qualifying clause,
> a parenthetical, a footnote inside an otherwise-covered section — that the region reader did
> not decompose into its own cell. Block-level accounting cannot detect a behavior it never saw.

So the arithmetic proves that nothing was skipped. It does not prove that anything was read
closely enough. The countermeasure inside the script is the instruction to enumerate the whole
family rather than the instance the prose names — the original called under-enumeration the
primary failure mode of the pass — and that is pressure, not a check.

## What came out of the 2026-07-14 run besides the inventory

Eleven items the nine agents flagged, five of them contradictions inside the canonical
documents, are in [`canon-contradictions-found-2026-07-14.md`](canon-contradictions-found-2026-07-14.md).
One of the five is still live at HEAD. That file also records how it survived the rollup, which
is a failure mode of the synthesis step rather than of the walk.
