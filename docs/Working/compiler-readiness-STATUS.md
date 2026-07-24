# Compiler-Readiness STATUS — the single living tracking surface

**Status**: Living — updated in place. **Last updated**: 2026-07-24.

## How to read this

This is the **one volatile surface**. It answers "where are we, what's blocked, what's
ruled, what's next" without opening another doc. It **points at** the source docs; it
does **not** duplicate their content. When state changes, update *this* file — not five.

The state used to live only in memory (`project_boundary_define_what_i_want_2026_07_16.md`)
and smeared across the matrix, its cells, the slice-3 boundary report, the gap ledger,
and the meta-plan. That is why "where are we" kept getting lost. This page fixes that.

## Big picture

Shane's **want doc** (`what-i-want-2026-07-16.md`, locked 2026-07-18) is the seed. From
it we build the **obligation-discharge matrix** (`obligation-discharge-matrix-2026-07-19.md`).
Once *populated and ratified*, the matrix **becomes** the canonical, exhaustive
definition of the proof engine (owner ruling 2026-07-19) and is promoted into `docs/`
canon. That proof engine is the capability the whole-compiler readiness build
(`compiler-readiness-meta-plan-2026-07-13.md`) implements against.

## Slice board

The matrix is populated in slices. Ratification is an owner act, not a drafting outcome.

| Slice | Goal | Status | Blocker |
|---|---|---|---|
| **1 (+1.5)** | numeric single-field (Witness Family 1); WP calculator + 13-rule normal form | ✅ **Done, live-verified** | — |
| **2** | tooling: cell schema, validator, prose generator, cell→test converter, manifest runner, corpus-measurement harness | ✅ **Done** (`60ef5a11`) | — |
| **3** | the **fault family** (ten requirement kinds) | ❌ **Failed ratification** (19/19 adversarial passes returned defects: 13 unsound, 6 needs-rework) | incomplete rule layer + open owner Q1/Q2 (below) |
| **type-requirement family** | `Dimension` + `Modifier`, re-homed per the 07-21 scope ruling | its **own work item**, owes its own denominator | after slice 3 |
| **4** | next constraint family | ⏸ **Gated** | verified-complete rule layer + weekly credit reset |

The fault-family denominator is **ten** kinds (dropped from eleven when
`IntervalContainment` re-homed from a minted kind to a discharge *strategy*, per the
locked constraint-establishment/preservation design).

## Rule layer status — the current bottleneck

Slice 3 failed because its cells cite **validity arguments** (the "why this discharge is
truth-preserving" cases) that were never written, or were written and then refuted.
State of the layer (matrix § Rule validity, latest rev):

- **Held** — the transport rule (route-indexed frame-and-kill) and type-structural
  qualifier context (12 adversarial rounds each), plus the constraint-family arguments,
  plus index-bounds decomposition / string-length interval / qualifier-chain equality /
  dimensional-product / key-presence.
- **Refuted (2026-07-23)** — five arguments written that day, all fell in one review
  pass; kept in the matrix under a REFUTED banner so nothing ratifies against them.
- **No argument at all** — four fault kinds: `Numeric`, `Presence`, `CountContainment`,
  `AssignmentQualifier`.

**Standing discipline (do not violate):** a soundness argument needs **adversarial
rounds**. The 07-22 pair took 12 rounds each and held; five written with zero rounds
all fell within hours. Never lock an argument written in a single pass.

## Decision ledger — DO NOT RE-OPEN

A `## Decision` / `### N.` block with four legs is **authority**. A ledger "fork" that a
locked Decision already answers is **not a fork** — grep the Decisions first. (This is
the D14 lesson: on 2026-07-24 OF4 was briefly ruled "outcome (a)" before the owner
caught that locked temporal Decision #14 already answered it; the ruling was withdrawn.)

**Gap-ledger forks** (`gap-ledger-2026-07-13.md` § OWNER DECISIONS — that section is a
stale 07-13 snapshot with a ⚠ header; the per-fork stamps below it are current):

| Fork | Ruling | When |
|---|---|---|
| OF1 message-hole **presence** | refuse uniformly — a hole is a read; locked + promoted to canon | 2026-07-24 (`72312285`, `63e4faeb`) |
| OF1 message-hole **value fault** | enroll, caught at compile time | 2026-07-23 (`62479cae`) |
| OF2 function-arg constraints | **not a fork** — spec answers it (build, not decision) | 2026-07-23 |
| OF3 `notempty` on collections | ruled (string-only cardinality; element-routed) | 2026-06-03 |
| OF4 period ordering | **D14 stands — no ordering on `period`.** The 07-24 "outcome (a)" was withdrawn; `collection-types.md:533` drift reconciled down to D14 | 2026-07-24 (`e33be4b6`, `d9bc218b`) |
| OF5 `integer` overflow | fixed-width 64-bit today; overflow *model* deferred post-MVP | 2026-07-14 (`90f0792f`) |
| OF6 currency case | **uppercase-only** (D13b); case-insensitive impl is drift (BUG-062) | 2026-07-24 (`d74740cf`) |

**Matrix ruling batches** (recorded in the matrix with rationale/rejected-alternative/tradeoff):
- **2026-07-19** — matrix-becomes-canon at ratification; discharge contract is exact (iff);
  respellability yes/no per family; ratification + amendment protocols.
- **2026-07-20** — five rulings (whole-handler write plans; three-legged mention set;
  ensures as peer constraint forms; dead rows are Error; class (e) retired) + fold
  literal division.
- **2026-07-21** — six rulings, incl. **an evaluation site is an evaluation occasion,
  not a syntactic position** (slice-3 Q3, ruled); premise citation duty + structural-fact
  refinement; approximate lane (NaN prevented, infinity deferred); eleven-kind fault
  family with `Dimension`/`Modifier` re-homed.

**Locked designs (do not build against — they carry their own status):**
- `obligation-linkage-and-completeness-2026-07-23.md` — **Locked 2026-07-23**.
- `constraint-establishment-preservation-obligations-2026-07-23.md` — **Locked 2026-07-23**.
- `presence-tolerant-message-rendering-2026-07-23.md` — refuse uniformly, **Locked 2026-07-24**, promoted (`63e4faeb`).

## Open owner questions (the real forks that gate progress)

From `…-cells/slice-3-boundary-report.md` (ranked by how much each unblocks):

1. **Q1 — does the matrix author a fault-family validity argument at all?** All written
   arguments are for constraint-family weakest-preconditions; the most-cited one
   disclaims division, and 11 of 19 fault files are division shapes. Blocks ~175 cells.
2. **Q2 — is premise class (d) (a rule holding in the pre-state) available to the fault
   family?** It is the exact path the known false-proof runs through; widening to (d)
   without a preservation obligation writes that bug into the definition. Blocks ~40 cells.
3. The six smaller boundary questions (temporal-deferral scope; two premise classes with
   no vocabulary home; self-discharge inside one expression; two-sided index bounds;
   whether `mincount 1` discharges without its own establishment; base minimality).

## Resume pointer — ordered next steps

1. **Finish the rule layer** — author the four argument-less kinds (`Numeric`, `Presence`,
   `CountContainment`, `AssignmentQualifier`) + redo the five refuted, **with adversarial
   rounds**.
2. **Rule Q1 and Q2** (owner) — they govern the shape of those arguments and ~215 cells.
3. **Regenerate the denominators** — the ten-kind fault family + the type-requirement family.
4. **Re-author + re-ratify slice 3** (owner read, per the ratification protocol).
5. **Slice 4** — gated on a verified-complete rule layer + credit reset.

The establishment/preservation `/design` fork (which premise-(d) hinges on) is **done** —
the two designs above are locked, so it is no longer a blocker.

## Pointers (what each source doc owns)

| Doc | Owns |
|---|---|
| `obligation-discharge-matrix-2026-07-19.md` | the definition itself + § Rule validity + ratification protocol |
| `…-cells/slice-3-boundary-report.md` | the ranked slice-3 questions (plain prose) |
| `…-cells/README.md` + `*.cells.json` | the cell data + schema + regeneration commands |
| `gap-ledger-2026-07-13.md` | fork dispositions (⚠ § OWNER DECISIONS is a stale snapshot; read the per-fork stamps) |
| `compiler-readiness-meta-plan-2026-07-13.md` | the whole-compiler methodology spine (Stage 0 spec-freeze ✅ → Stage 1 gap ledger ✅ → acceptance→RED tests → build) |
| `project_boundary_define_what_i_want_2026_07_16.md` (memory) | the history/arc |
