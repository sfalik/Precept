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

The fault-family denominator is **provisionally ten** kinds (dropped from eleven when
`IntervalContainment` re-homed from a minted kind to a discharge *strategy*). That re-home is
Decision 2 of the constraint-establishment/preservation design — which is **unlocked**, and
whose Decision 2 the adversarial review flagged as defective (scope mismatch). So the ten-kind
count is contingent on that design re-locking; treat it as provisional until then.

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
- `presence-tolerant-message-rendering-2026-07-23.md` — refuse uniformly, **Locked 2026-07-24**, promoted (`63e4faeb`).

**Rework pass 1 ran 2026-07-24 — both docs substantially reworked (uncommitted), still `Draft`,
still NOT ready to re-lock.** Genuinely closed: the quantifier moved off *handlers* onto governed
operation occasions (state-action writes, the update patch and the editable door are now in scope —
verified live: an entry-action write on the initial state under `max 1000` rejects with no event
handler in the file); creation treated as governed per canon; `Restore` correctly excluded from
preservation while kept as a writer for fact-survival; the unsettled entry-actions-at-construction
question honoured *without* settling it (construction carries a plan set — one member with entry
actions, one without — and the obligation must discharge over both); the collapse arithmetic
(nine fault kinds) verified against `ProofRequirementKind.cs`.
**Still open — the blocker:** the new `Owes` rule has no constraint-kind term, so a residency
ensure (`in S ensure …`) would mint preservation on rows that never enter `S`. Adversarial review
reports that over-rejects a majority of the corpus (64 of 78 sample files carry `in S ensure`)
*and* fires the internal "compiler defect" diagnostic on them, because the completeness check is
set equality. Plus: a spurious construction term in residency establishment; the weakest
precondition still not total (four writer categories have no action kind); unpinned intra-firing
order for multi-target writes; and a set of citation/bookkeeping errors that disagree across the
pair. Pass 2 required before re-lock.

**Unlocked / in rework (NOT landed — do not build against):**
- `obligation-linkage-and-completeness-2026-07-23.md` — **UNLOCKED (Draft)**. Was locked for
  a few hours 2026-07-23, then re-opened by adversarial review. Recorded defects: the
  admissibility condition has no rejection power; `Expected(C)` misses `Update`/`Restore`;
  misattributed witnesses; the DU has thirteen kinds, not eleven. See its `## Review record`.
- `constraint-establishment-preservation-obligations-2026-07-23.md` — **UNLOCKED (Draft)**,
  same story. Recorded defects: Decision 2 opens a soundness hole at non-handler write sites
  (state entry/exit); scope collapses one kind and leaves `Length`/`CountContainment`; the WP
  rule is not total (11 of 15 action kinds are not `field := expr`). See its `## Review record`.

These two are the machinery that would make premise class (d) safe for the fault family
(they provide the preservation obligation). **Re-locking them is the current blocker** — see
the Resume pointer.

## Open owner questions (the real forks that gate progress)

> ⚠ **`…-cells/slice-3-boundary-report.md` is dated 2026-07-21 and is STALE as a list of owner
> questions.** The matrix moved on 07-23 and owner rulings landed on 07-21. Verified by
> independent check 2026-07-24: its ranked #1 and #3 are **not** owner questions. **Re-check
> every remaining item against the matrix before putting it to the owner** — do not read that
> report's framing as live.

| Boundary-report item | Verdict (independent check, 2026-07-24) |
|---|---|
| #1 — does the matrix author a fault-family validity argument at all? | **NOT a fork — authoring work.** Matrix `:260` *"Every generative rule carries a validity argument"* admits no argument-less arm; `:261`'s gate blocks ratification but assigns no disposition; `open` is defined (`:292`) as *"the model has not decided"* and the model **has** decided the fault discharge (`:207`). Already answered by action — five fault arguments were written 07-23. Granularity is settled too: one argument per generative rule, fault family indexed by requirement kind. |
| #2 — is premise class (d) available to the fault family? | **ALREADY RULED** by the owner 2026-07-21 (matrix `:122`): a declared constraint or field modifier *is* available as a premise for a fault obligation, but *"a proof may not consume such a fact unless it names the obligation that established the fact and every obligation that preserves it."* `:131` states the consequence outright — *"class (d) is now admitted for the fault family under this citation duty… The row is corrected in the canon-correction pass."* The `:207` row the report quotes is known-stale bookkeeping the matrix already flags for correction. **Both of the report's "two sides" are alternatives the ruling explicitly rejected** (`:125`): excluding declared facts (contradicts `want:104` + `spec:256`) and admitting the fact *bare* (the report's own stated fear) — the ruled position is the third, admit-under-citation. The citation duty *is* what closes the known false-proof: `:124` — *"that file cannot state its own proof, because there is no establishing obligation to name."* |
| #3 — is an evaluation site a syntactic position or an evaluation occasion? | **ALREADY RULED** by the owner 2026-07-21 — *"site identity as an evaluation occasion"* (matrix `:5`, rev 8). The report predates the ruling. |
| the six smaller items | Not yet re-checked. Assume stale until verified. |

**Correction to the boundary report:** its claim that *"the schema pins that list closed"* is
false — `cell.schema.json:237` says the enum *"mirrors that section and **grows with it**."*

**A fifth claimed owner decision — also already answered (independent check, 2026-07-24).** The
rework pass reported that the 2026-07-21 citation duty's *rejection power* needed an owner ruling
(amend the rationale vs. un-defer the certificate re-checker). Neither is open: the ruling never
claimed rejection power — matrix `:122` says an uncited consumption *"does not typecheck as a proof
at all"*, a well-formedness claim, and `:124`'s rationale is visibility (*"the gap is visible at the
point of consumption"*), grounded in proof-carrying code (`:126`). The demotion was already
**owner-accepted 2026-07-23** at `:129`: *"The duty relocates where that enumeration is consumed; it
does not remove the need for one. The ruling itself is unaffected."* And the re-checker exit is a
closed deferral — `compiler-readiness-plan-2026-07-12-architecture.md:43` *"Decision B RETRACTED —
the independent re-checker is not an MVP decision"*, `:192` *"there is no MVP decision to make
here… the earlier B1/B2 fork is therefore void."* It is already a later phase, and when it lands it
makes the condition non-vacuous with no decision required. **Disposition: sharpen `:129`'s wording
during Pass 2 — an editorial obligation, not a fork.**

> **Standing lesson: agent-authored artifacts systematically over-produce "owner decisions."** Four
> of four claimed forks checked this session were already settled (boundary-report #1/#2/#3, and the
> citation-duty question above). **Route every claimed owner decision through an independent
> canon-check before it reaches the owner.**

**Net: all three ranked "owner questions" are already answered — none gates the rule layer.**
What remains is authoring work (expensive — the two arguments that held took twelve adversarial
rounds each), plus two mechanical follow-ups and one sequencing dependency:

- **Canon-correction owed since rev 8**: the fault row at matrix `:207` still lists premise
  classes (b)/(c)/(a) and must be corrected to admit (d) under the citation duty.
- **Re-ratify the ~40 (d)-citing fault cells** *after* the two designs re-lock.
- **Sequencing (not a fork)**: `ProofRequirement` carries no establishment or preservation
  subtype yet (matrix `:125`), so a (d) citation currently has nothing to name — those cells are
  *definitionally licensed but not yet buildable or ratifiable* until the two designs land.

## Resume pointer — ordered next steps

0. **Re-lock the two establishment/preservation designs** (the current blocker) — fix the
   Review-record defects in both, re-run adversarial review, owner sign-off. Until this
   lands, premise class (d) is not safely available and the fault arguments below cannot be
   authored soundly.
1. **Finish the rule layer** — author the four argument-less kinds (`Numeric`, `Presence`,
   `CountContainment`, `AssignmentQualifier`) + redo the five refuted, **with adversarial
   rounds**.
2. ~~Rule Q1/Q2 (owner)~~ — **struck 2026-07-24.** Q1 is authoring work, not a decision
   (independent check; see the table above); Q3 was already ruled 07-21; Q2 is under check and
   is most likely conditional-on-machinery, i.e. sequencing. Nothing here is known to need an
   owner ruling.
3. **Regenerate the denominators** — the ten-kind fault family + the type-requirement family.
4. **Re-author + re-ratify slice 3** (owner read, per the ratification protocol).
5. **Slice 4** — gated on a verified-complete rule layer + credit reset.

The establishment/preservation `/design` fork (which premise-(d) hinges on) is **NOT done** —
both designs were locked for a few hours 2026-07-23, then re-opened by adversarial review and
are Draft with recorded defects. Step 0 above is that re-lock.

## Pointers (what each source doc owns)

| Doc | Owns |
|---|---|
| `obligation-discharge-matrix-2026-07-19.md` | the definition itself + § Rule validity + ratification protocol |
| `…-cells/slice-3-boundary-report.md` | the ranked slice-3 questions (plain prose) |
| `…-cells/README.md` + `*.cells.json` | the cell data + schema + regeneration commands |
| `gap-ledger-2026-07-13.md` | fork dispositions (⚠ § OWNER DECISIONS is a stale snapshot; read the per-fork stamps) |
| `compiler-readiness-meta-plan-2026-07-13.md` | the whole-compiler methodology spine (Stage 0 spec-freeze ✅ → Stage 1 gap ledger ✅ → acceptance→RED tests → build) |
| `project_boundary_define_what_i_want_2026_07_16.md` (memory) | the history/arc |
