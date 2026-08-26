# Decision sheet — what the runtime fault model becomes

**Run** 2026-07-28 on top of HEAD `72a6c7ff`, branch `spike/Precept-V2-Radical-reset`. Nothing here is committed and no file was edited. No `.precept` file was compiled; no catalog, sample or compiler behaviour is cited as evidence. Every line number below was read at HEAD.

**Scope.** The nineteen register rows at `docs/working-reset/phase-2/contradiction-register.md:292–315` — `R9`, `N22`, `N23`, `R10`, `N24`, `N25`, `R14`, `N26`, `R11`, `R12`, `N27`, `R13`, `N28`, `N29`, `C1`, `C2`, `N30`, `N34`, `N35` — and the nine documents they touch. Written so that whoever executes them makes no judgement calls.

---

## 0. Headline: the rows execute as a group; the model paragraph that would have gone with them did not

The nineteen rows are safe to execute together. Nothing found in review makes the group incoherent, and nothing in it needs an owner ruling first.

What was not safe was the paragraph the previous draft of this sheet proposed dropping into all nine documents as the statement of the corrected model. It said the runtime "validates the values arriving at the two ingress points … **against the rules declared on the slot each value enters**", and claimed to contradict none of the nine.

That wording settles a question the register itself routes to the owner and marks "not to be executed" (`contradiction-register.md:505`, row `N96`). `want:143` says the opposite for an editable-field write — the field's modifier rules **and every rule that mentions the field**, because "an edit is an arbitrary incoming value that no static proof can cover, so evaluating those rules at the ingress point is exactly the premise that closes the editable write sites in the compiler's proof." Canon at HEAD carries the narrow wording at eight sites and every one of them now carries an explicit warning: `precept-language-spec.md:276–288` ("Do not build against this scope, and do not cite it as evidence"), repeated at `:1994`, `runtime-api.md:404`, `result-types.md:154`, `evaluator.md:740` and `:1387`, and `compiler-and-runtime-design.md:151` — the eighth marking, at `compiler-and-runtime-design.md:137`, was added by the fifth pass three days ago for exactly this reason.

Writing the narrow scope into nine more documents as settled would have taken an owner decision away from the owner, silently, in a pass that has nothing to do with it. §1 below is rewritten so it does not.

Two smaller things also had to change before the paragraph was safe: it counted the evaluation-time checks as two when canon counts three (`compiler-and-runtime-design.md:830`: "Ingress validation, access-mode checks and row dispatch are the three things an operation does before it executes"), and it dropped the load-time certificate check that `want:165` gives as the *reason* the runtime may be lighter, and that `precept-language-spec.md:110` and `:112` state in the same breath as "there are no runtime fault traps."

---

## 1. The model, stated once

Drop-in paragraph. It contradicts none of the nine, and it is consistent with `compiler-and-runtime-design.md:647`, `:706`, `:824`, `:826`, `:830`, `:865`, `:874` and `precept-language-spec.md:110`, `:112`, `:266`, `:294`, all of which have already been corrected.

> The runtime trusts the compiler, and it establishes that trust by checking the definition once when it loads it, against the certificate the compiler emits alongside it (`runtime-api.md` § Definition Load and the Certificate Gate; not yet built, and that section says so). Having checked it, the runtime runs no fault-prevention check while an event is being evaluated. There is no divide-by-zero test, no overflow guard, no re-check of a proven bound on a write, and no fallback for a site the compiler could not prove — under prove-or-reject a definition with an unproven obligation is refused and no executable model is produced, so there is nothing for a fallback to stand behind. What still runs during an operation are the things the compiler's proofs named as their premises and the things that decide which operation applies at all: ingress validation of values arriving at the two ingress points (event arguments, including on construction, and writes to editable fields), the access-mode check that decides whether a field may be written in the current state, and guard evaluation. Everything after that is computation. `FaultCode` survives as the compiler's own vocabulary: each member names a class of failure and, through `[StaticallyPreventable]`, names the diagnostic responsible for preventing it — a build-time integrity check over the catalogs, with nothing planted at the site and no evaluation-time path. `Fault` survives as the name for a breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator — and not as a check standing behind a proof.

Three things this paragraph deliberately does not say, each for a stated reason:

- **What ingress validation evaluates for an editable-field write.** That is `N96`, it is owned by phase 5, and the register says it is not to be executed here. Every document that already states the narrow scope keeps its wording *and* its disputed marking, untouched. No document that does not already state it acquires one.
- **What, if anything, detects an engine-invariant breach.** §5 item 1.
- **That the list of what still runs is exhaustive.** `want:171` is explicit that the runtime's governance duties are "not an exhaustive list", and the runtime keeps duties this paragraph does not enumerate — the inspect mechanism, immutable versioning, the write guarantees (`want:163`). The paragraph names what runs *during evaluation of an event*, which is the only thing `want:165` is about.

---

## 2. Every name, with a disposition

"Declaring document" is the document that states the type's shape and claims ownership of it. A consumer document must not change a declaration; it changes its own references to it.

### Goes entirely

| Name | Declaring document | Named by (consumers) |
|---|---|---|
| `FaultSiteAnnotation` | **Two claimants, both self-described as canonical**: `precept-builder.md:429–435` ("This is the canonical planting contract", `:426`) and `proof-engine.md:360–364` (blockquote headed "Canonical annotation shape"). Shapes agree. | `precept-builder.md:115, 132, 285, 426, 450, 489, 593, 605, 728`; `evaluator.md:147, 981, 1419, 1456, 2072`; `proof-engine.md:321, 352, 355, 366, 2452, 2457, 2460, 2674` |
| `FaultSiteDescriptor` | `descriptor-types.md:106–110` is the nominal owner — `precept-builder.md:899` and `evaluator.md:2194` both point there. **Two further declarations disagree with it**: `precept-builder.md:598–602` uses `SourceSpan Site` where `descriptor-types.md:110` uses `int SourceLine`, and `proof-engine.md:2227` states the transform producing `site.StartLine`, matching `descriptor-types.md`. `precept-builder.md:605`'s claim that the two records carry the same fields is true only of its own copy. | `descriptor-types.md:8, 38, 140, 157, 172, 203`; `precept-builder.md:224, 285, 304, 605, 728, 899, 981`; `evaluator.md:2194`; `proof-engine.md:107, 136, 333, 1937, 2211, 2219, 2227, 2230, 2254, 2720`; **outside the nine**: `glossary.md:78` |
| `FaultSiteLink` | `proof-engine.md:344–349` | `proof-engine.md:123, 321, 327, 339, 341, 352, 354–369, 475, 483, 1931, 1946, 2211, 2219, 2221, 2225, 2230, 2254, 2296, 2306, 2350, 2357, 2410, 2413, 2587`; `precept-builder.md:198, 426, 446, 634, 967, 989`; `evaluator.md:2072`; `soundness-and-coverage.md:269`; `diagnostic-system.md:541` |
| `ProofLedger.FaultSiteLinks` (the field) | `proof-engine.md:264`, on the record at `:259–274` | `precept-builder.md:198, 446, 634, 967`; `evaluator.md:2072`; `proof-engine.md:327, 475, 483, 2211, 2254, 2306, 2350, 2357` |
| `Precept.FaultBackstops` (the property) | `precept-builder.md:224`, on the `Precept` class at `:212–225`. No other document declares this class. | `precept-builder.md:115, 132, 284, 595, 653, 728`; `evaluator.md:317, 1443` |
| `Opcode.FaultSite` (the field) | `precept-builder.md:488–489`, the only member on the `Opcode` base at `:486–490` | `precept-builder.md:450, 451`; `evaluator.md:147, 981, 982`; restated `proof-engine.md:366` |
| `IsFaultCondition` | **Nowhere.** One reference, `evaluator.md:982`. This is the predicate that would actually perform the divide-by-zero test, and no document ever specifies it. | — |
| `CC#6` | **Nowhere.** Fourteen references and no document states what the numbering is or what CC#6 says: `evaluator.md:147, 981, 989, 1419, 1456, 2072`; `precept-builder.md:132, 426, 488, 593`; `proof-engine.md:320, 354, 2452, 2460, 2674` | — |
| `CC#12` | **Nowhere.** Three references: `evaluator.md:338` (inline comment on the `Faulted` record), `:1433`, `:1620`. Unlike CC#6, this numbering names the *outcome variant*, which survives — so what goes is the numbering and the evaluation-time production it asserts, not the variant. See "Survives re-scoped" below. | — |
| The two-argument `Faults.Create` shapes | **Nowhere.** `fault-system.md:200` declares `Create(FaultCode, params object?[])` only. `evaluator.md:147` writes `Faults.Create(annotation, context)`; `precept-builder.md:607` writes `Faults.Create(descriptor, context)`; `evaluator.md:983` writes a three-argument call. All three belong only to the planting path. | — |

When removing `Opcode.FaultSite`, leave `public abstract record Opcode { }` as an empty abstract base. The subtypes at `precept-builder.md:491–505` still need a common base for `ImmutableArray<Opcode>` at `:481`. This is mechanical; do not restate the union.

### Survives unchanged

| Name | Declaring document | Why it survives |
|---|---|---|
| `StaticallyPreventableAttribute` / `[StaticallyPreventable]` | `fault-system.md:104–109` (claims ownership at `:62`). Second copy `diagnostic-system.md:570–575`. | `compiler-and-runtime-design.md:647` and `:651` (both already corrected) keep it explicitly as a build-time integrity check over the catalogs. This is the one name in the family that does not change at all, and it sits next to everything that does. |
| PRECEPT0002, PRECEPT0004 | `diagnostic-system.md:665`, `:669`, in the enforcement table at `:661–672` | Both enforce the build-time pairing. PRECEPT0002 requires every `FaultCode` to name a valid `DiagnosticCode`; PRECEPT0004 requires every `Fault` to be constructed through `Faults.Create()`, which holds as long as `Fault` is a public output type with a string identity field (`diagnostic-system.md:709–711`). |
| PRECEPT0016 | **`fault-system.md:241`**, in that document's own enforcement table at `:235–242`; source file named at `:357`; listed as owned-elsewhere at `:69`. It appears **nowhere** in `diagnostic-system.md`. | Requires `FaultMeta.Code` in each switch case to match that case's own enum member. Purely a catalog-shape rule; nothing to do with evaluation. |
| `DiagnosticMeta.PreventsFault` | `diagnostic-system.md:441` | The diagnostic-side half of the same pairing. |
| The Diagnostics ↔ Faults catalog pair | `catalog-system.md:329, 391, 676–706, 911` (outside the nine) | Same. |
| `FaultMeta`, `FaultSeverity`, `Faults.GetMeta`, `Faults.All` | `fault-system.md:160–202` | Catalog metadata over the fault classes. **One qualification**: `FaultMeta.RecoveryHint` is a field of this record whose stated purpose (`fault-system.md:79`) no longer has an occasion — §5 item 4. Leave the record alone while that is open; do not treat it as fully settled. |
| `ConstraintInfluenceMap`, `ConstraintInfluenceEntry` | `precept-builder.md:614–619`; `proof-engine.md:371`, `:2235–2238` | `proof-engine.md:2243` states in its own words that this is not a re-check index, and `:2255` says it again in the ledger-consumption table. Pass 6's other half. |
| `ProofLedger` (the record itself), the three-way verdict, prove-or-reject | `proof-engine.md:259–274`; `soundness-and-coverage.md` §2 | `want:171` supports all of it. |

### Survives re-scoped

| Name | Declaring document | New scope, in words |
|---|---|---|
| `FaultCode` (the enum) | `fault-system.md:101–157` — **fifteen members**, claims ownership at `:57`. A second full copy at `diagnostic-system.md:577–617` has **thirteen** (stops at `OutOfRange = 13`, missing `LengthBoundViolation` and `CountBoundViolation`) and maps `UnexpectedNull` to `NullInNonNullableContext` at `:591` where `fault-system.md:125` maps it to `UnprovedPresenceRequirement`. This drift predates the correction. | The enum keeps all fifteen members. It stops being "the closed registry of all evaluator failure modes" (`fault-system.md:57`) and becomes the set of failure classes the compiler is responsible for preventing, each naming its preventing diagnostic. Nothing in the evaluator raises a member. |
| `Fault` (the record) | `fault-system.md:207–219`. Restated `evaluator.md:1665–1669`. | Keeps its shape. Stops being "an error the type checker should have prevented, reachable for out-of-contract data" and becomes "the name for a breach of one of the engine's own invariants," in the words `compiler-and-runtime-design.md:826` already uses. Whether anything ever produces one is §5 item 1. |
| `Faults.Create(FaultCode, params object?[])` | `fault-system.md:200` | The declared one-plus-varargs shape survives; every call site in the removed path goes. |
| `EventOutcome.Faulted(Fault)` | `result-types.md:103`, on the hierarchy at `:95–105`; the document claims ownership at `:54–56`. Restated `evaluator.md:338`; described `runtime-api.md:171`, `:906`; spec `:1963`. | The variant stays. Its description everywhere changes from "impossible path the type checker should have prevented / backstop" to "engine-invariant breach." The `CC#12` numbering goes with it wherever it appears. Its `Pipeline stage` cell at `result-types.md:115` currently reads `Backstop`; every other cell in that column names a phase from spec §3A.4. Replace it with `—`, because no §3A.4 phase produces it. |
| `Evaluator.Fail(FaultCode, params object?[])` | `evaluator.md:436–437`, `:1657–1659` | The method survives with `Faults.Create` behind it; the `// ── Fault Backstop ──` banner at `:436` and the dispatch-loop call at `:983` go. |
| **§ Fault Production** (`evaluator.md:1655–1670`) | — | **The section survives whole and keeps its table-of-contents entry at `:57`.** It declares `Fail` (`:1657–1659`), states that `Faults.Create` looks up `FaultMeta` (`:1662`), and restates the `Fault` record (`:1665–1669`) — all of which survive re-scoped. It is the duplicate block at `:1419–1429` that goes, because that one is anchored on the annotation. |
| PRECEPT0001 | `diagnostic-system.md:664` | The rule is "Fail classification — every `Fail()` uses `FaultCode`". It is a constraint on evaluator call sites, so unlike PRECEPT0002/0004/0016 its meaning depends on §5 item 1. Under item 1's option (A) it constrains a method nothing calls, which is harmless but should not be described as guaranteeing that "every evaluator failure path routes through a classified `FaultCode`". Four sentences state it that way and all four need the same re-wording: `proof-engine.md:1945`, `:2582`, `diagnostic-system.md:96`, `:767`, plus `diagnostic-system.md:787` and `:103`. **Do not delete the analyzer; do re-word its description.** |
| `ProofObligation.Site` reference-identity invariant | `proof-engine.md:320–337` | The field survives (diagnostics use its span). The blockquote's stated reason — that the builder matches by reference identity to stamp an annotation — goes with the builder match, and no document states another consumer for the "MUST NOT use `with { }` on `Site`" rule at `:337`. See §5 item 5. |
| Pass 6 | `precept-builder.md:591`, body `:591–622` | **The pass survives; its name does not.** It keeps `ConstraintInfluenceMap` construction (`:593` says so in its own words). The count stays six. Rename it **Pass 6 — Constraint Influence Pass** everywhere the old name appears: the heading `:591`, the table-of-contents entry and anchor `#pass-6-fault-backstop-pass` at `:34`, the diagram box title at `:283`, the pass-list item at `:115`, and the dependency table's first cell at `:304`. |
| `soundness-and-coverage.md` §3.1's `FaultCode` coverage axis | `soundness-and-coverage.md:212–242` | Survives whole. It measures what the *compiler* must cover; each row names the preventing diagnostic and the discharging obligation kind. Only one parenthetical inside it changes (`:232`). Two more edits land nearby but outside it — see §3 and §6's `N34`. |

---

## 3. Per document, what changes

Line numbers verified at HEAD by reading the files, not by grep alone. Where the register's number has drifted, both are given.

### `docs/runtime/precept-builder.md`

Removals: `:115` (pass-list item 6's first clause; the `ConstraintInfluenceMap` clause stays, and the item is renamed); `:132` responsibility row (whole row — its surviving content is the row below it at `:133`); `:154` LOC itemization, drop "+ ~50 fault backstop"; `:198` and `:634` two tables that both state `ProofLedger`'s contents, drop `FaultSiteLinks` from each; `:224` the `FaultBackstops` property; `:284` and `:285` two of the diagram box's three bullets (`:286` stays); `:304` dependency row — Depends-On becomes `ProofLedger` alone, Produces becomes `ConstraintInfluenceMap` alone, and the row label is renamed; `:426–436` the planting contract and the `FaultSiteAnnotation` declaration; `:438–453` the "Planting pseudocode" heading and the `CompileExpression` block; `:488–489` the `FaultSite` field and its comment; `:593` first sentence and the residual-bookkeeping clause; `:595` the `FaultBackstops` materialization paragraph; `:597–607` the `FaultSiteDescriptor` declaration, the relationship paragraph and the "When reached, they fire" paragraph; `:653` drop `FaultBackstops` from the evaluator's consumed list; `:728` the output-contract invariant bullet; `:899` and `:981` drop `FaultSiteDescriptor`; `:967` cross-reference row; `:989` drop `FaultSiteLink`.

Structural: `:283` box title renamed (the box itself stays, with one bullet); `:34` table-of-contents entry text and anchor renamed; `:591` heading renamed.

Unchanged, and easy to remove by mistake: `:26`, `:108`, `:232`, `:234`, `:306`, `:980` all say "six passes" or state the execution order `1 → 2 → 3 → 4 → 5 → 6`. The count does not change.

Not covered by any register row, and needed: `precept-builder.md` owns `Precept.From` and never mentions the certificate. §9 Failure Modes (`:675–708`) enumerates the `HasErrors` guard and builder invariant violations, §10 Input Contract (`:713–717`) requires three things a tampered definition satisfies, the Recovery Path at `:700–708` tells the caller to fix compilation errors — wrong advice for a tampered definition with no diagnostics — and `:883–889` guarantees "a `Precept` is always valid" on the strength of the `HasErrors` guard alone. `runtime-api.md:254` says `Precept.From` runs the certificate gate. That inconsistency is inside the nine. See §5 item 6.

### `docs/runtime/evaluator.md`

Removals: `:19` the non-negotiable rule (note it already hedges — "compiler proofs are **intended to** eliminate runtime fault paths"); `:56` table-of-contents entry for § Impossible-Path Failures; `:147` responsibility row; `:317` drop "fault backstops" from the model-contents cell; `:436` the `// ── Fault Backstop ──` banner (keep `Fail` at `:437`); `:981–983` the dispatch check; `:989` the two-layer paragraph — its first half is a complete correct sentence on its own and should be kept as one sentence, without "the first line of defense" and without the CC#6 label; `:1419–1429` the annotation-anchored fault-production block (its content survives verbatim at `:1662–1669`); `:1433` the CC#12 paragraph; `:1443` drop `FaultBackstops`; `:1456` the `FaultSiteAnnotation` internal-dependency row; `:1620` in-domain table row; `:1624–1638` the section heading and its nine-row table; `:1640–1652` the `FaultCode` excerpt and its lead-in; `:1677` the recovery row; `:1683` the "Produces a `Fault` via `Faults.Create` (for impossible paths)" bullet; `:2072` resolved question 10; `:2194` drop `FaultSiteDescriptor`.

**Keep `:1431`** ("Faults are **never thrown** — they are returned as structured outcome variants"). It is true under the corrected model and it is the only statement of it in this document. Moving it into § Fault Production keeps it anchored; leaving it where it is orphans it under a removed paragraph.

> **Corrected 2026-08-11.** The test applied here is whether the sentence is true. It is — vacuously, while nothing constructs a `Fault`. What was not asked is whether stating it flatly presumes the answer to the producer question this same pass was filing as open, and it does: a reader meets an unqualified statement that a fault is returned rather than thrown. The move into § Fault Production was right and stands. The sentence itself was made conditional on 2026-08-11 — see [`fault-outcome-wording.md`](fault-outcome-wording.md).

**Keep `:57` and § Fault Production (`:1655–1670`) entire.** See §2.

`:1708` reads "Every `Fault` produced by the evaluator carries a `FaultCode` with a `[StaticallyPreventable]` attribute. If the compiler emits no errors, the evaluator should never fault." The first sentence goes — it describes a production path that no longer exists. The second is rewritten rather than kept as-is: "should never" hedges a guarantee stated absolutely everywhere else, and this document's `:19` was flagged for exactly that hedge. Replace with a flat statement that a compiled definition has no fault path at evaluation time.

`:2157` cross-references `docs/compiler/proof-engine.md §Fault Backstops`. **That heading does not exist at HEAD** — `proof-engine.md`'s fault headings are `#### FaultSiteLink` (`:339`), `### Proof/Fault Chain` (`:1900`), `### Fault Chain Integrity` (`:2408`), `### Roslyn-Enforced FaultCode↔DiagnosticCode Correspondence` (`:2562`). Repoint it to `docs/compiler/diagnostic-system.md § FaultCode → DiagnosticCode Chain`, which is where `proof-engine.md:2724` already sends readers.

Structural: the Resolved Design Questions list at `:2066–2078` is numbered 8, 9, 10, 11, 12, 13. Removing item 10 leaves a hole. Renumber to 8–12, or state in the list's lead-in that the numbering is historical.

Unchanged, and at risk from a pass primed on deletion rows: `:891–893` (undetermined values on the inspect path — this paragraph *relies* on there being no fault at evaluation time and is correct as written); `:725–735` (the access-mode check, which is a live evaluation-time refusal — `want:106` and `want:163` keep it); `:737–742` (ingress governance, including its disputed marking, which stays exactly as it is); `:1685` ("the only exception paths are truly exceptional… corrupted `Precept` model" — see §5 item 1); `:1710` ("Every constraint declared on an incoming value is checked as it enters" — this is ingress completeness and `want:165` keeps it by name); `:1731` (the bucket index's two surviving purposes); `:1797–1808` Decision 5, already corrected against `want:141`; `:2068` (resolved question 8, already corrected).

### `docs/runtime/descriptor-types.md`

`:8` drop `FaultSiteDescriptor` from the implementation-state list; `:38` drop "fault sites" from the Overview's list of what descriptors represent; `:106–110` the declaration; `:140` the downstream bullet's third clause; `:157` the contract bullet; `:172` the Innovation bullet; `:203` "all 5" becomes "all 4".

Structural: `:172` is one of exactly three bullets under **Innovation** (`:170–172`). Removing it leaves that section making a claim about first-class runtime identity with no proof-related example. Removing it is right; note the section thins.

### `docs/runtime/fault-system.md`

`:30` table-of-contents entry for § Defense-in-depth; `:35` table-of-contents entry for Q3; `:246–261` the divide-by-zero example — remove step 1 and re-open the walkthrough from the compiler side (step 5's "no `Precept` produced → evaluator never runs" is the point of the example and survives); `:263` the justification clause; `:284–286` the whole "Defense-in-depth, not a primary error path" section; `:322`, the paragraph inside Q3 that restates `:286` almost verbatim.

**Do not remove the whole of Q3 (`:320–324`).** `:324` — "This affects how aggressively the evaluator should assert vs. gracefully fault. Decision needed at evaluator design time." — is the only place in canon that records the assert-versus-fault question, and that question is §5 item 2. Delete `:322`, keep the heading and `:324`, and re-open the question from the corrected side.

Re-scope, not delete: `:7`, `:8`, `:11`, `:42`, `:51`, `:57`, `:60`, `:66`, `:73`, `:95`, `:101`, `:227–231`, `:290`, `:344` all describe `FaultCode`/`Fault` as the evaluator's failure modes. Each becomes the compiler-side reading. `:230`'s chain diagram — `Evaluator fails → FaultCode member → …` — loses its first arrow; the rest of the chain stands.

`:211–213` carries a TODO naming "linkage back to the DiagnosticCode that should have prevented it" as an expected field. Under the corrected model that linkage is compile-time only. Fix the TODO or drop that clause. Same for `:315`, the matching bullet in Q2.

`:340` calls `diagnostic-system.md` the mirror document where "the full enforcement chain, end-to-end example… are documented there", while carrying its own copy of the example. Both copies need the same edit; see the `diagnostic-system.md` entry.

**Do not remove `:149`, `:153`, `:156`.** See §6 on `R13`.

### `docs/runtime/runtime-api.md`

`:670` heading "Three-Layer Enforcement Model" becomes two layers; **`:672` — "Precept enforces field constraints across three layers:" — becomes two in the same edit**, or the file ends up with a two-layer heading over a sentence announcing three; `:678` item 3 goes and the numbering closes up; `:171` and `:906` re-describe `Faulted`.

Not in any register row, and needed: `:277` and `:910` both say migration on schema change "is the caller's responsibility". `want:141`'s parenthetical says the opposite — "Post-MVP, the restore path itself expands to handle schema evolution". `evaluator.md:1806` and `precept-language-spec.md:1969` have both already been corrected to match. This is the third sibling and it is a straight `want:141` correction, not a design question. Add it.

`:250–260` (the certificate gate) is already correct and already discloses "Not yet built" at `:260`. Leave it. `:281` and `:283` (restore trust, access-mode bypass) are already correct. Leave them. `:400–404` (the Update pipeline and its disputed marking) is untouched by this correction.

### `docs/runtime/result-types.md`

`:51`, `:63`, `:72`, `:129` all re-describe `Faulted`/`Fault`. `:115`'s `Pipeline stage` cell becomes `—`.

Unchanged: `:123` (the open question on `InvalidArgs`'s shape), `:154` (the disputed marking) and `:252` (the open question on what the `ConstraintResult` arrays mean now) are all honest and correctly declared open. Do not resolve them.

Pre-existing drift, not this correction's business, but note it while the file is open: `:47` and `:56` say eight `EventOutcome` variants; the code block at `:95–105` lists seven and omits `Created`, which `runtime-api.md:168` and `precept-language-spec.md:1960` both require. `:371` says "Faults throw (not return)" against `:51`'s "never thrown". The eight-variant claim also appears at `evaluator.md:1433`, which this pass removes for other reasons.

### `docs/compiler/proof-engine.md`

Removals: `:11` Downstream status row, drop "fault backstops"; `:123` responsibility row; `:136` out-of-scope row; `:264` the `FaultSiteLinks` field; `:322–335` the reference-identity blockquote's matching mechanism (keep `:320`, `:321` re-worded, and `:337` — see §5 item 5); `:339–369` the `FaultSiteLink` subsection including the structural-elision note; `:475` and `:483` two pipeline-diagram lines; `:1918` "No runtime check needed" branch and `:1925–1940` the `FaultCode → FaultSiteLink → FaultSiteDescriptor` chain of the Proof/Fault diagram; `:1946` invariant 3's second half; `:2211` builder-consumption row's first clause; `:2219–2230` the `FaultSiteDescriptor` half of the builder-consumption contract; `:2254` the transform row; `:2306`, `:2350` and `:2357` the emission code's `faultSiteLinks` list, its `CreateFaultSiteLink` call and its constructor argument; `:2410–2415` the Fault Chain Integrity contract's body; `:2457` Decision 2's "Defense-in-depth / belt-and-suspenders" bullet; `:2460` Decision 2's "What crosses" first bullet; `:2678` the second reason under "Why not pass obligations to runtime?"; `:2720` cross-reference row.

**Five things that are not deletions, and each of them would break something if deleted:**

- **`:107`, key design choice 2.** It reads "Proof ledger does NOT cross the compile-runtime boundary — only `FaultSiteDescriptor` residue (defense-in-depth backstops) crosses into runtime. The proof engine is purely a compile-time analysis stage." Only the middle clause is false; the first and last are the corrected model's own claim. Re-word to "nothing crosses". Deleting the item would also break a numbered list of three (`:106–108`) with no renumbering instruction.
- **`:2450–2466`, Decision 2.** Its title — "ProofLedger Does NOT Cross Compile-Runtime Boundary" — is true and stays. Its "What does not cross" list at `:2463–2466` (`ProofObligation` records, strategy evidence, obligation coverage statistics) is entirely true and stays. Delete `:2457` and `:2460`, re-word `:2452`. The alternative — deleting the decision whole — removes the compile-runtime boundary statement from the document, leaves the table-of-contents entry at `:62` orphaned, and leaves §11's decisions numbered 1, 3, 4, 5.
- **`:2672–2679`, "No Runtime Obligation Checking".** Same shape. The heading is true, `:2674`'s first sentence is true, `:2677` and `:2679` are true. Delete `:2674`'s second sentence and `:2678`. Deleting the section orphans the table-of-contents entry at `:80`.
- **`:2446`**, the third of three items under **Trade-off accepted** in Decision 1 — "Defense-in-depth backstops catch any runtime failures." Decision 1 is a locked decision and `CLAUDE.md` requires a stated trade-off. Two of three reasons survive; this one becomes false. **Replace it, do not delete it.** The honest replacement is that an obligation no admissible strategy can discharge rejects the definition — a harder trade-off than the one currently claimed. **No register row covers this.**
- **`:2408`, § Fault Chain Integrity.** The heading and its table-of-contents entry at `:57` go together with the body at `:2410–2415`; that one *is* a whole-section removal and the entry must go with it. Named here so it is not confused with the three above.

Three sentences use `CreateFaultSiteLink` as evidence that a diagnostic code lives in exactly one catalog field: `:2296`, `:2587`, and — in the other document — `diagnostic-system.md:541`. The argument survives on `CreateDiagnostic` alone; **rewrite all three**, or the argument for single-siting the code disappears along with its example.

`:2140` maps unresolved proof-site shapes to diagnostic codes and mentions the corresponding `FaultCode`. The diagnostic selection survives; the `FaultCode` mention re-scopes to the catalog pairing.

Structural: the table-of-contents entry at `:57` goes with § Fault Chain Integrity. **`:40` ("Proof/Fault Chain") stays** — the section at `:1900` survives with a thinner diagram; deleting the entry would make a surviving section unreachable from the contents. The diagram at `:1904–1940` loses four boxes. The `ProofLedger` record at `:259–274` loses a field.

`:1737` is already corrected (register `C4`, committed `d32d8e26`) and shows the target wording: "the runtime evaluates no constraint against the finished working copy and carries no fault trap at evaluation time."

### `docs/compiler/soundness-and-coverage.md`

`:113–114` the parenthetical "(The `[StaticallyPreventable]` evaluator traps are defense-in-depth for out-of-contract entry, not an author-facing opt-out.)" — the *point* of the parenthetical is that there is no escape hatch, and that point survives in the sentences above it at `:109–112`; the parenthetical does not, because there are no traps. This sits in § "Why there is no author-visible escape hatch", not in §3.1.

`:232` the `QualifierMismatch` row's parenthetical about runtime fault-identity collapse. This is the only edit inside §3.1.

`:266–271` the runtime-only-residue note, which sits in §3.2. It has two items. Item 2 (the `FaultSiteLink` identity collapse) goes with `FaultSiteLink`. **Item 1 survives untouched** — it is a diagnostic message borrowing the wrong template, a compile-time defect with nothing to do with this family. The note's framing sentence "Two known items" becomes one.

Everything else in §3.1 (`:212–242`) and §3.2 (`:244–264`) stays. §3.1 is compile-time coverage accounting; `want:165` does not touch it, and all fifteen rows keep their dispositions.

### `docs/compiler/diagnostic-system.md`

`:552` the conservative-backstop clause (register `N35` cites `:548`, which at HEAD is the coverage-scanner paragraph — a different sentence); the rest of `:552`, including the collection-safety collapse, survives.

Not covered by any register row, and the plainest instance of the removed model in the whole set:

- `:649–657` "### Evaluator usage" — a code block whose entire content is `if (IsZero(right)) return Fail(FaultCode.DivisionByZero);`. Remove the subsection **and its table-of-contents entry at `:35`**.
- `:558` "Precept's philosophy: no runtime errors. The evaluator must never produce a failure that the compiler could have prevented." Re-scope: the first clause is right, the second describes the evaluator producing failures.
- `:563` the chain diagram, whose first link is "Evaluator fails →". Same treatment as `fault-system.md:230`.
- `:713–730` the divide-by-zero example, a second copy of `fault-system.md:246–261`. Same edit.
- `:96` the inputs bullet; `:103` the `Fault` output bullet ("when a runtime fault occurs"); `:767` and `:787`, both of which describe the chain's guarantee as covering "every evaluator failure mode".
- `:541` the `CreateFaultSiteLink` sentence (see the proof-engine entry).

`:577–617` is the thirteen-member `FaultCode` copy and `:632–645` the matching thirteen-case `GetMeta` copy. `fault-system.md` owns the enum (`:57`, `:62`); these copies have drifted independently of this correction. Reduce them to a pointer at `fault-system.md`, or bring them in line at fifteen. On `UnexpectedNull`'s preventing diagnostic, ownership settles it without a tally: `fault-system.md:125` is the declaring document and says `UnprovedPresenceRequirement`; `diagnostic-system.md:591` is a consumer copy and says `NullInNonNullableContext`. `soundness-and-coverage.md:226` independently agrees with the declaring document. This is pre-existing drift; fixing it is optional and, if the executor prefers to keep the pass clean, may be filed as a separate note rather than folded in.

`:682–690` walks through adding a hypothetical `FaultCode.StackUnderflow`. It survives as written today, but see §5 item 3.

### Documents outside the nine that the register does not cover

- **`docs/glossary.md:113`** — "Faults are emitted when invariants the compiler couldn't prove are violated at runtime." A one-sentence statement of exactly what `want:171` removes, in the document a reader consults when a term is unclear. Must change.
- **`docs/glossary.md:78`** — the Descriptor entry lists "fault site" as one of the program elements a descriptor names. Goes with `FaultSiteDescriptor`.
- **`docs/runtime/README.md:37`** — "Fault system mirrors diagnostic system… same code namespace conventions, same severity model." Survives, but the mirror is now compile-time only; the sentence reads as a runtime pairing.
- **`docs/contributing/anti-patterns.md:336`** — "Only obligations the proof engine genuinely cannot discharge become runtime fault-site backstops (defense-in-depth, not primary error path)." Directly contradicts prove-or-reject; must change.
- **`docs/tooling/mcp.md:242`** (`## Runtime Faults` in `precept_proofs` output) and **`:269`** (`## Prevents Fault` in `precept_diagnostic`). Both survive; the first's heading is now describing compiler vocabulary, not runtime behaviour.
- **`docs/language/catalog-system.md:329, 391, 676–706, 911`** — the Diagnostics ↔ Faults pair, its diagram edge and its analyzer row. All survive per `compiler-and-runtime-design.md:647`. `:695` states `RecoveryHint` is "populated on every member" — see §5 item 4.
- **`docs/language/precept-language-spec.md:1963`** — `| Faulted | Evaluator impossible path / runtime fault. |` in the construction-outcome table. The spec has been corrected everywhere else; this row was missed.
- **`docs/compiler-and-runtime-design.md:882`** — the already-corrected document names `Fault` as `Restore`'s invariant-breach outcome, and `runtime-api.md:281` says `RestoreOutcome` carries `Restored` and `RestoreInvalidInput` "and nothing else". `:881` names `Fault` for `Update`, and `result-types.md:136–142` gives `UpdateOutcome` four variants, none of them a fault. See the note under §5 item 1.

---

## 4. The re-scope boundary

Apply this to each sentence, table cell, diagram node and code block; it decides all of them the same way everywhere.

**Ask: does this text describe a check *for a fault* that happens while an event is being evaluated?**

- **Yes → it goes.** Anything that plants, carries, reaches, tests, fires, routes to, or falls back on a fault while an operation is executing. This includes the words *backstop*, *defense-in-depth*, *belt-and-suspenders*, *trap*, *second layer*, *impossible path*, *out-of-contract data reaching a check*, and any sentence whose subject is "the evaluator" and whose verb is a form of checking **for a fault**.
- **It is a check that happens during evaluation but is not a fault check → it stays.** Ingress validation, the access-mode check, and guard evaluation all run during an operation and `want:165` keeps all three by name or by consequence. `evaluator.md:1710`, `:2068`, `:1731`, `:725–735` are the sentences most at risk from a pass reading only the first bullet, and every one of them survives untouched.
- **It describes something that happens when the project is built, when a definition is compiled, or when a definition is loaded → it stays.** The pairing between a fault class and the diagnostic that prevents it; the analyzers that enforce that pairing at build time; the exhaustive-switch completeness argument; the coverage accounting that uses fault classes as an axis; the diagnostic selection that reads a code from catalog metadata; and the certificate check `Precept.From` performs at load (`runtime-api.md:250–260`), which is the check `want:165` gives as the reason the rest may be dropped.
- **None of the above — it says only that a type or an outcome variant exists → it stays, re-described.** `Fault`, `FaultCode`, `EventOutcome.Faulted` remain declared. Their descriptions change from "a failure the compiler should have prevented, reachable for out-of-contract data" to "a breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator." Do not attach a producing condition to them; nothing in the corrected set names one.

Two replacements to use verbatim rather than re-inventing per document:

- Where a document justified a trap by out-of-contract data, the canonical replacement already exists at `precept-language-spec.md:294`: data that did not enter through a declared operation is outside the guarantee, is not detected, and "there are no runtime fault traps waiting to catch such a value at evaluation time."
- Where a document said the compile gate is "the first line of defense", the surviving statement is `evaluator.md:989`'s own first half, standing alone: the type checker and proof engine emit diagnostics for unresolved obligations, and no `Precept` is built until the author fixes them.

**One thing this boundary explicitly does not decide**, and no executor should let it: what ingress validation evaluates for an editable-field write. That was true when this was written and it is still true, but the sentences it referred to are gone. **Corrected 2026-08-26:** the owner ruled the "and nothing wider" narrowing out of canon, so the nine sentences that carried it were edited to drop the qualifier, and the eight disputed markings were deleted with them. The eight marked pairs were at `precept-language-spec.md:274`/`:276`, `:1992`/`:1994`, `runtime-api.md:402`/`:404`, `result-types.md:152`/`:154`, `evaluator.md:738`/`:740`, `:1381`/`:1383` *(recorded here as `:1385`/`:1387`, which was already wrong before the removal)*, and `compiler-and-runtime-design.md:137` and `:151`. The ninth site, `compiler/proof-engine.md:2597`, said the same thing in the words "and nothing beyond them" and never carried a marking at all. Canon now states only the undisputed half — a value entering from outside is held to the constraints declared on the slot it enters — and says nothing about whether an editable write evaluates anything further. Do not read the removal as an answer in either direction: the question stays open and stays with phase 5 (`phase-5/editable-write-cascade.md`, `phase-5/editable-write-design-space.md`).

---

## 5. What this does not decide

Six items, down from eight. Each is a design question, not a contradiction. Two of the previous eight were closed by reading, and the reasons are given below so nobody re-opens them.

**1. What, if anything, the evaluator detects and reports as a `Fault`.** Two positions are written in canonical documents and they do not quite agree. `compiler-and-runtime-design.md:706` and `:824` name the Evaluator as the producer of `Fault`, "engine-invariant breach only", and `:874`/`:880–882` put it on the outcome grid for all three operations. Against that, `precept-builder.md:690` and `evaluator.md:1685` say internal invariant violations throw `InvalidOperationException` and a corrupted model is an unrecoverable exception that should not be caught. `evaluator.md:989` names the population as "force-builds, catalog evolution, or compiler bugs" — force-builds cannot exist under prove-or-reject, catalog evolution is what `runtime-api.md:254`'s certificate gate refuses at load, and a compiler bug is undetectable by the mechanism being removed, because the annotation is planted only where the compiler *admitted* it could not prove something (`precept-builder.md:426`, `proof-engine.md:2413`).

So the question is narrow: **the family stays** — `compiler-and-runtime-design.md:706`, `:824`, `:826`, `:865`, `:874` already say so, in a document already corrected once, and reversing that is not on the table here. What is open is whether anything constructs a `Fault`, or whether `Fault` and `Faulted` are declared classifications with no producer while engine bugs stay exceptions.

- *(A) No producer.* Stated honestly as such. **Cost:** an outcome variant nothing constructs, and `compiler-and-runtime-design.md:706`/`:824` need "produced by the Evaluator" softened. **Benefit:** requires no new mechanism.
- *(B) A detection mechanism the evaluator actually carries.* The most convergent candidate would be the four type-checker-owned classes — `TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, `FunctionArityMismatch` — dispositioned **explicitly-out** at `soundness-and-coverage.md:224–228` and `:238–242` because the type checker and name binder decide them before the proof engine runs, and independently marked as compiler-stage-owned at `evaluator.md:1630–1636`'s third column. **Cost, stated plainly:** `want:165`'s closing clause is against it — "What runs at runtime are the declared premises (ingress validation of args and editable fields, guards) that the compiler's proofs depend on, and nothing beyond them." An evaluation-time test for a type mismatch is not a declared premise. Choosing (B) means setting that clause aside deliberately, not reading around it.

`want:165` and `want:171` do not mention engine defects at all, which is why the question exists; but `want:165` does bear on (B), and that is stated here rather than left for the owner to notice.

Two consequences ride on this and close with it: whether `UpdateOutcome` and `RestoreOutcome` gain a fault variant (`compiler-and-runtime-design.md:881`/`:882` name `Fault` for both; `result-types.md:136–142` and `runtime-api.md:281` give neither one), and how PRECEPT0001 is described (§2). Neither is a separate decision.

**2. Whether an assertion about the engine's own internal state counts as a fault-prevention check under `want:165`.** `want:165`'s three examples are all checks about the author's data — divisors, overflow, proven bounds on writes. A slot-index range assertion or a stack-depth assertion is a different kind of thing. `fault-system.md:324` is canon's own record of this question ("how aggressively the evaluator should assert vs. gracefully fault"), which is why that line is kept rather than deleted. One sentence from the owner. Item 1's option (B) cannot be evaluated without it.

**3. Whether a new engine-invariant `FaultCode` member can ever be added.** `diagnostic-system.md:682–690` walks through adding `FaultCode.StackUnderflow` — an unambiguous engine bug — and PRECEPT0002 forces it to declare `[StaticallyPreventable(DiagnosticCode.StackUnderflow)]`, then forces `DiagnosticCode.StackUnderflow` to exist with a stage, a severity and a message template that some pipeline stage emits against source text. For an engine-invariant code no such authoring diagnostic can exist. Options: the enum is closed at the fifteen members it has, all of which do have real preventing diagnostics, and no engine-invariant member is ever added (in which case nothing changes); or PRECEPT0002 is relaxed for a new category. Only bites if item 1 lands on (B) or a new member is proposed.

**4. Where `FaultMeta.RecoveryHint` belongs.** Its stated purpose (`fault-system.md:79`) is telling the precept author how to fix the definition so a runtime fault stops happening, and its example (`:183`) is `"Guard the transition with 'when Divisor != 0'…"`. If nothing raises a fault at evaluation time, the hint has no occasion. `catalog-system.md:695` says it is populated on every member. Options: keep it as authoring-time guidance carried through `precept_diagnostic` and `precept_proofs`; move the guidance to the diagnostic, where authoring guidance already lives; or delete the field. Each changes a catalog and the MCP formatters.

**5. Whether the reference-identity invariant on `ProofObligation.Site` still has a consumer.** `proof-engine.md:337` states the proof engine MUST NOT copy a `TypedExpression` into `Site`. The only stated reason is the builder's `ReferenceEqualityComparer` match against `FaultSiteLinks`, which goes. Diagnostic emission needs the span, not the reference. **No document names another consumer.** Do not delete the invariant silently and do not preserve it with a reason nobody has written; this needs a value nobody has.

**6. What `Precept.From` does when the certificate does not verify.** `runtime-api.md:254` says it "refuses to govern under a definition whose certificate does not verify", and `:260` already declares the whole mechanism unbuilt and the checker's shape unsettled — so this does not block the nineteen rows. It is here because `precept-builder.md` owns the method and does not know about it: two failure modes (`:679–690`), an input contract a tampered definition satisfies (`:713–717`), recovery advice that is wrong for one (`:700–708`), and a guarantee at `:883–889` that a `Precept` is always valid resting on the `HasErrors` guard alone. That inconsistency between two of the nine will keep re-appearing until somebody answers whether verification failure is an exception or a structured result, whether it names the step that failed to replay, and whether verification is cached. Note that `want:177` is unqualified — "compile-time rejections and runtime refusals alike must explain themselves clearly" — so an unexplained failure is not among the admissible answers whichever shape is chosen.

**Closed by reading, recorded so they are not re-opened:**

- *Whether the `Update`/`Restore` outcome hierarchies gain a fault variant* — folded into item 1, where it belongs. `compiler-and-runtime-design.md:881`'s other three cells (`FieldWriteCommitted`, `AccessDenied`, `InvalidInput`) name no variant that exists in `result-types.md:136–142` either, so that row is descriptive rather than a variant requirement, and `runtime-api.md:281`'s "and nothing else" is scoped to constraint-failure outcomes on restore. No separate decision.
- *What `Fault`'s remaining open questions mean now* (`fault-system.md:211–213`, `:309–318`) — these have no answer until item 1 does, and saying so is not a separate question.

---

## 6. Where the register's nineteen rows are wrong or incomplete

### Follow as written

**N23, R10, N27, N28, N30, C1, N34, N35** — the sites and the resolutions are right. Corrections to line numbers and additions are below; the instruction itself stands.

### Follow with a correction

**R9** — instruction right. Add the sites it does not name: `precept-builder.md:428–436` (the `FaultSiteAnnotation` declaration and its comment) and `:438–453` (the "Planting pseudocode" heading and the `CompileExpression` block). Correcting only the two sentences at `:426` and `:607` leaves the record declaration and the code standing.

**N22** — instruction right, and it is the row that gets the hard part right (the pass survives, half of it). Add: the LOC itemization at `:154`, the pass-list item at `:115`, the responsibility row at `:132`, the anchor rename in the table of contents at `:34`, the diagram box title at `:283`, and the dependency-row label at `:304` — the last two are places the old name survives that the row's own site list touches for content but not for name. State explicitly that the pass count stays six and that `:26`, `:108`, `:232`, `:234`, `:306` and `:980` do not change — that is the mistake this row is most likely to produce.

**N23** — instruction right for `precept-builder.md`. Extend: `Precept.FaultBackstops` is also named at `evaluator.md:317` and `:1443`, and neither is covered by `N23` or `N24`. `precept-builder.md:653` and `evaluator.md:1443` are two copies of the same list and must change together.

**N24** — instruction right; **all five line numbers have drifted by about 22 to 24.** `:437` should be `:436` (the banner; `:437` is the `Fail` declaration, which survives). `:1005` is now `:981–983`; `:1013` is now `:989`; `:1443` is now `:1419`; `:2094` is now `:2072`. Add `:1456`. Add `:1433` and `:338`, which carry the parallel `CC#12` numbering the row never mentions.

**N25** — instruction says "the section survives re-scoped." **That is right about `Faulted` and wrong about the section it names.** § Impossible-Path Failures (`:1624–1652`) is a heading, a nine-row table and an enum excerpt, and all of it goes: the table's third column ("Should Be Prevented By") names an author premise or a compiler stage for every one of the nine rows, and none of them describes a compiler or builder defect. What survives re-scoped is a *different* section — § Fault Production (`:1655–1670`), which the row does not mention at all and which declares `Fail` and restates the `Fault` record. Line numbers: `:1644`→`:1624`, `:1648`→`:1628`, `:2179`→`:2157`. Add `:1620`, `:1640–1652`, `:1677`, `:1683`, and `:1419–1429`.

**R14** — instruction right for the division row, and **the identical reasoning removes all nine rows**, not one. Line `:1654`→`:1630`.

**N26** — extend to `descriptor-types.md:8`, `:38`, `:157`, `:203`, to the three consumer sites `precept-builder.md:899`, `:981` and `evaluator.md:2194`, and to `glossary.md:78`.

**R11** — the removal is right, and the example needs re-authoring, not just a deleted step: with step 1 gone the walkthrough starts nowhere. Also note the same example exists a second time at `diagnostic-system.md:713–730` and no row covers it. Line `:248`→`:247` (the step-1 text; the example block runs `:246–261`).

**R12** — instruction right; no correction.

**N27** — instruction right, with one narrowing. `:284–286` goes whole. Of Q3, only `:322` goes; `:320` and `:324` stay, because `:324` is the only record in canon of the assert-versus-fault question, which §5 item 2 routes to the owner. Add `:35`, the table-of-contents entry for Q3 — the row correctly names `:30` for the other removed section's entry and missing its sibling is an inconsistency inside one file's instruction.

**N28** — line `:660`→`:678`. Extend: the heading at `:670` says "Three-Layer Enforcement Model" and the sentence directly under it at `:672` says "across three layers". Both become two. Removing item 3 without both leaves a three-layer announcement over two items.

**N29** — the substance is right and it is the only row in the family that says "re-scope, not delete". Lines: `runtime-api.md:887`→`:906`; `result-types.md:117`→`:115`. Two additions: `result-types.md:115`'s `Pipeline stage` cell reads `Backstop`, which is not a §3A.4 phase and needs `—` rather than deletion; and the same re-scope is needed at `result-types.md:51`, `:63`, `:129`, `runtime-api.md:171`, `evaluator.md:338`, `fault-system.md:66`, `:95`, and `precept-language-spec.md:1963`, none of which `N29` names. `evaluator.md:1620` is the one place where the row's own logic points the other way — it is a row in a table headed "All in-domain failures" (`:1608`), and an engine defect is not an in-domain failure, so it goes rather than being re-scoped. Say so explicitly; otherwise the two instructions collide.

**C1** — instruction right. Its own note already flags the line numbers as stale; they are. `:346`→`:352`, `:342`→`:341`, `:2210`→`:2211`, `:2435`→`:2452`, `:2667`→`:2674`. `:107` and `:123` still resolve, but `:107` is a re-wording rather than a deletion (see §3).

**C2** — the quoted "belt-and-suspenders" text is Decision 2's, now at `:2457`, not `:2451` as the row's own correction says. Three additions the row does not cover: `:2460` (Decision 2's "What crosses" first bullet, which names `FaultSiteAnnotation`); `:2678`, the same claim in the Deliberate Exclusions section; and **`:2446`**, item 3 of Decision 1's *Trade-off accepted*, which must be **replaced** rather than deleted because `CLAUDE.md` requires a locked decision to state its accepted trade-off. And the row must not be read as licence to delete Decision 2 or the "No Runtime Obligation Checking" exclusion whole — both carry true statements of the compile-runtime boundary and both have table-of-contents entries (`:62`, `:80`) that would be orphaned. See §3.

**N30** — instruction right; `:1932`→`:1937–1938`, `:1912`→`:1918`. The diagram loses four boxes, not one branch. The section heading at `:1900` and its table-of-contents entry at `:40` both stay.

**N34** — instruction right; `:94`→`:113`, `:199`→`:232`, `:236`→`:266–271`. Two corrections to the row's reason field, which says "the coverage matrix encodes the backstop model in its column definitions, so the matrix's own shape changes." **It does not.** §3.1's columns are `FaultCode` / preventing `DiagnosticCode` / discharging obligation kind / disposition — all compile-time. The matrix keeps its shape and all fifteen rows. And only one of the three edits is inside §3.1 at all: `:232` is; `:113` sits in "Why there is no author-visible escape hatch" above §3.1, and `:266–271` is in §3.2. The note at `:266–271` has two items, of which only item 2 goes — item 1 is a diagnostic-message defect unrelated to this family.

**N35** — line `:548`→`:552`; at HEAD `:548` is the coverage-scanner paragraph and quoting it would send an executor to the wrong sentence. Extend substantially: `diagnostic-system.md` also carries `:558`, `:563`, `:649–657` (plus its table-of-contents entry at `:35`), `:713–730`, `:96`, `:103`, `:767`, `:787`, `:541`, and the drifted thirteen-member copies at `:577–617` and `:632–645`. `:649–657` is a code block containing the exact divide-by-zero test `want:165` names, and no row covers it.

### Describes an edit that should not be made

**R13** — **do not remove `OutOfRange`, `LengthBoundViolation` or `CountBoundViolation` from the `FaultCode` enumeration.** The row's reasoning is that these "come out of the fault enumeration with S11". S11 and its family (S12, S13, S14, SC-1, N37–N40 in the register's §4) were executed as **wording** corrections, not removals. N38's resolution is "Keep the `Proof` classification; re-word"; N40's cell says outright that the quoted phrase survives because the obligation is real and only its classification changed; S11 removed a phrase from a prose fault list and touched no enum.

Removing the three members would break the compile-time coverage accounting: `soundness-and-coverage.md:234`, `:235` and `:236` disposition all three as **in** with named discharging obligation kinds (`IntervalContainment`, `LengthContainment`, `CountContainment`), and `proof-engine.md:1737` names `CountBoundViolation`'s diagnostic as the emitting one for count containment. Those are three live prevention paths and the matrix is where they are counted.

What R13 is pointing at is real. `precept-language-spec.md:112` says a value straying outside a declared bound "is not on that list" of fault classes, and `compiler-and-runtime-design.md:136` states the rule that actually holds: containment "is not a fault gate and should not be quoted as one." That is a **quoting rule, not a membership rule**, and it is how R13 should be executed: never cite these three members as evidence that a declared bound is fault-prevented, and where a document does, re-word it to rule preservation exactly as N38 and N40 did elsewhere. The residual tension — three bound-containment members sitting in an enum the spec says bounds are not on — is a classification question the spec has already answered in the only way that matters here (don't quote them that way), so it needs no owner ruling and gets none.

---

## Two things nobody has a value for

Stated rather than guessed:

- The reference-identity invariant at `proof-engine.md:337` has no remaining stated consumer once the builder match goes (§5 item 5).
- Whether anything constructs a `Fault` (§5 item 1). Everything in this sheet is executable without the answer; nothing in it presumes one.

**Pass 6's new name is not on this list.** The previous draft both supplied *Pass 6 — Constraint Influence Pass* and said nobody has the name, which is a contradiction an executor cannot act on. The name is taken here, derived from `precept-builder.md:593`'s own statement of the surviving responsibility, and it is used in all five places the old name appears.

---

## What the critics found

Three critiques were run against the previous draft of this sheet. Every finding is answered below. Where a critic cited a line I could not confirm, that is recorded as a finding about the critique, with the line that shows it.

### Findings that changed the sheet

**The model paragraph settled an owner-routed question (against-the-want #1).** Confirmed and acted on — it is now §0 and the paragraph is rewritten. Verified: `contradiction-register.md:505` carries row `N96` marked "Not executed, and not to be executed. Routed to the owner… **Owned by phase 5, not phase 2**"; `precept-language-spec.md:276–288` says "Do not build against this scope, and do not cite it as evidence"; `want:143` states the wide reading and gives the reason. The critic's verdict of "not safe to execute" was right about the paragraph and wrong about the nineteen rows, which is why §0 says so both ways.

**The paragraph omitted the certificate (against-the-want #3).** Confirmed. `precept-language-spec.md:110` reads "There are no runtime fault traps… What makes that safe is the proof certificate the runtime verifies once, when it loads the definition — not a redundant check at evaluation time", and `:112` repeats it. `want:165` gives the same pairing; `want:199` says the runtime "does not blindly trust the compiler either." §1 now names the load-time check, and §4's retention bucket now has a load-time category.

**The paragraph counted two checks where canon counts three (against-the-want #4).** Confirmed by two already-corrected documents: `compiler-and-runtime-design.md:830` — "Ingress validation, access-mode checks and row dispatch are the three things an operation does before it executes" — and `runtime-api.md:400`, which lists "access mode check → type validation → ingress validation". `evaluator.md:731–733` performs the check and `result-types.md:149` declares `FieldNotEditable` with "Access mode check" as its pipeline stage. §1 now names it; §3 lists `evaluator.md:725–735` as unchanged-and-at-risk. *The critic cited `result-types.md:150` for `FieldNotEditable`; that line is the `InvalidFields` row. The correct line is `:149`.*

**§4's removal rule was unqualified about the word "checking" (against-the-want #2).** Confirmed and fixed. The bucket now says "a check **for a fault**", and the second bullet exists to catch what the first would otherwise sweep up. *The critic cited `evaluator.md:1709` for "Every constraint declared on an incoming value is checked as it enters"; that sentence is at `:1710`. `:1709` is the inspection–commit guarantee. `:2068` and `:1731` were cited correctly.*

**Three different answers for § Fault Production (coherence #1).** Confirmed — this was the one place the sheet was not executable. `evaluator.md:57` is the contents entry, `:1655` the heading, `:1657–1659` the `Fail` block, `:1665–1669` the `Fault` record. The resolution: **the section survives whole**, entry and all, and what goes is the duplicate at `:1419–1429`, which states the same thing anchored on the annotation. `:1662–1669` carries the surviving version verbatim, so nothing is lost.

**Deleting `proof-engine.md:107`, Decision 2 and the No-Runtime-Obligation-Checking exclusion whole (coherence #2 and #3).** Confirmed on every point. `:107` is item 2 of a numbered list of three at `:106–108` and its first and last clauses are true. Decision 2 runs `:2450–2466` and its "What does not cross" list at `:2463–2466` is entirely true. The exclusion's heading is at `:2672` and `:2674`'s first sentence is true. Contents entries at `:62` and `:80` would have been orphaned; §11's decisions would have read 1, 3, 4, 5. §3 now specifies clause-level edits for all three.

**Orphaned contents entries (coherence #4, #5).** Confirmed: `diagnostic-system.md:35` is `- [Evaluator usage](#evaluator-usage)`; `fault-system.md:35` is the Q3 entry. Both added. The second is now moot in part, because Q3 is no longer deleted whole (see overreach #3).

**The reverse orphan at `proof-engine.md:40` (coherence #6).** Confirmed: `:40` points at `### Proof/Fault Chain` at `:1900`, which survives. The entry stays; only `:57` goes.

**`runtime-api.md:672` (coherence #7).** Confirmed and added.

**Pass 6's name surviving in two more places (coherence #8).** Confirmed: `precept-builder.md:283` is the diagram box title "Pass 6: Fault Backstop Pass", `:304` is "| Pass 6 (Fault Backstop) |". Both added to the rename list, which now runs to five sites.

**Two definitions of "descriptor" still listing fault sites (coherence #9).** Confirmed: `descriptor-types.md:38` and `glossary.md:78` both enumerate "fault sites" among what descriptors represent. Both added.

**`evaluator.md:1433` and the CC#12 numbering (coherence #10, overreach #4).** Confirmed and the more serious of the two. `:1433` states the removed model as plainly as anything in the nine — "Impossible-path failures are modeled as structured `Fault` values raised via `Faults.Create` and surfaced as `EventOutcome.Faulted(Fault fault)` — the 8th `EventOutcome` variant. The evaluator's `Fail()` path now returns a `Faulted` outcome rather than an unhandled exception." It also asserts eight variants against a seven-variant code block at `result-types.md:95–105`. `CC#12` appears at `:338`, `:1433` and `:1620` and the sheet never mentioned it. All three are now in §2 and §3, and `:1426–1429` is folded into the same removal so nothing is stranded. **`:1431` is kept** — "Faults are never thrown" is true under the corrected model. *(Corrected 2026-08-11: true, but stated flatly it presumes the answer to the open producer question. Made conditional on 2026-08-11 — see [`fault-outcome-wording.md`](fault-outcome-wording.md).)*

**A third copy of the `CreateFaultSiteLink` argument (coherence #11).** Confirmed: `proof-engine.md:2296` carries it as well as `:2587` and `diagnostic-system.md:541`. All three now say "rewrite".

**PRECEPT0001 is about evaluator call sites (coherence #12).** Confirmed: `diagnostic-system.md:664` reads "Fail classification — every `Fail()` uses `FaultCode`". Its four descriptions in terms of evaluator failure paths — `proof-engine.md:1945`, `:2582`, `diagnostic-system.md:96`, `:767`, plus `:787` and `:103` — were on no list. PRECEPT0001 now has its own row in §2 with the qualification that its meaning follows §5 item 1, and the six sentences are on the change list.

**The resolved-questions numbering (coherence #13).** Confirmed: `evaluator.md:2066–2078` runs 8, 9, 10, 11, 12, 13. Renumbering instruction added.

**`evaluator.md:1620` — delete or re-scope (coherence #14).** Confirmed as an unstated collision between §2/`N29` (re-scope) and §3 (delete). Resolved explicitly in favour of deletion, with the reason: `:1608` heads the table "All in-domain failures produce structured outcome variants", and an engine defect is not an in-domain failure. `:1683` was also unnamed and is now on the list.

**PRECEPT0016's declaring document (coherence #15).** Confirmed by grep: PRECEPT0016 appears **zero** times in `diagnostic-system.md`. It lives only in `fault-system.md`, declared at `:241`, source file at `:357`, listed as externally-owned at `:69`. §2 corrected — and the ownership direction the sheet stated for that one analyzer was backwards.

**`precept-builder.md:283–286` (coherence #16).** Confirmed: `:283` is the box title, the bullets are `:284`, `:285`, `:286`. The instruction is now "keep the box, rename the title, drop `:284` and `:285`".

**Where the soundness edits land (coherence #17).** Confirmed: §3.1 runs `:212–242`, §3.2 starts at `:244`. `:113` is above §3.1 in the escape-hatch subsection; `:266–271` is inside §3.2. Only `:232` is inside §3.1. §3 and §6 both corrected.

**Execution presupposes item 1's answer (overreach #1).** Partly right and acted on. §5 item 1 is now narrowed: the *family stays* — `compiler-and-runtime-design.md:706`, `:824`, `:826`, `:865`, `:874` say so in an already-corrected document — and only the producer question is open. That removes the dependency: every re-description edit in §3 is correct under either remaining answer.

**Item 1 was a three-way fork with two closed branches (overreach #2).** Confirmed and folded into the above. Option (C), deleting the family, would reverse a correction already executed, so it is no longer offered.

**Deleting the whole of Q3 erases an open question (overreach #3).** Confirmed and acted on. `fault-system.md:324` reads "This affects how aggressively the evaluator should assert vs. gracefully fault. Decision needed at evaluator design time" — the only record in canon of the question §5 item 2 asks. Only `:322` goes now, and register row `N27` cites `:322` rather than the section, so this was an expansion the sheet made rather than one the register asked for.

**`UpdateOutcome`/`RestoreOutcome` did not need an owner (overreach #5).** Confirmed and folded into item 1. `compiler-and-runtime-design.md:881`'s other three cells name nothing that exists in `result-types.md:136–142` (`Updated`, `ConstraintsFailed`, `FieldNotEditable`, `InvalidFields`), so the row is descriptive. `runtime-api.md:281`'s "and nothing else" is scoped to constraint-failure outcomes — verified by reading the full line, which opens "There is no constraint-failure outcome on restore."

**Eight open items were really two or three (overreach #6).** Confirmed by the sheet's own words. Two are closed and recorded as closed; the list is six, and item 1 now carries its two consequences inside it rather than as separate rows.

**Item 8 was already declared open and out of scope (overreach #7).** Confirmed: `runtime-api.md:260` reads "**Not yet built.** No certificate is emitted, no checker exists, and `Precept.From` performs no such gate today." Kept as item 6 but re-framed: it does not block the nineteen rows, and it is here because two of the nine disagree with each other about the same method. The `want:154` argument the previous sheet used is dropped — the critic is right that `want:154` is about event dispatch inside a governed precept, and `want:177` ("compile-time rejections and runtime refusals alike must explain themselves clearly") is the applicable line and is unqualified.

**The `UnexpectedNull` tally (overreach #8).** Confirmed on all three counts. `fault-system.md` *is* the declaring document (`:57`, `:62`), so ownership settles it and no vote is needed; the sheet's own rule already said a consumer must not change a declaration. And the line is `:591`, not `:585` — `:585` is the `TypeMismatch` attribute. §3 now settles it by ownership and marks the fix optional, since it is pre-existing drift.

**The Pass 6 name contradiction (overreach #9).** Confirmed — the sheet both supplied a name and said nobody had one. The name is taken, once, and the "nobody has a value" section no longer lists it.

**`FaultMeta` marked "survives unchanged" while §5 questions one of its fields (overreach #10).** Confirmed. The §2 row now carries the qualification.

**R13's third reason does not hold (overreach #11).** Confirmed. `compiler-and-runtime-design.md:865` says directly that "many diagnostics have no fault counterpart", and `diagnostic-system.md:682–690` shows the analyzer running from `FaultCode` to `DiagnosticCode`, not the reverse — so removing fault members would break no diagnostic. The reason is struck. The conclusion stands on the coverage-accounting reason, and R13 is now re-expressed as the quoting rule `compiler-and-runtime-design.md:136` already states.

**"Line numbers verified at HEAD" was too broad a claim (overreach #12).** Confirmed on all three examples. The reference-identity invariant is at `proof-engine.md:337`, not `:335` — the blockquote runs `:320`–`:337` and the invariant is its last line. The blockquote opens at `:320`, not `:319`. The `UnexpectedNull` mapping is at `diagnostic-system.md:591`, not `:585`. All corrected, and the opening of §3 now says the numbers were verified by reading rather than by grep.

### Findings where a critic was wrong, and the line that shows it

- **against-the-want #2 cited `evaluator.md:1709`** for "Every constraint declared on an incoming value is checked as it enters." That sentence is at **`:1710`**; `:1709` is "Inspection–commit agreement". The substance of the finding is right and was acted on; the citation was off by one.
- **against-the-want #4 cited `result-types.md:150`** as declaring `FieldNotEditable`. `:150` is the `InvalidFields` row; `FieldNotEditable` is at **`:149`**, with `Access mode check` as its pipeline stage. Again the substance holds.
- **against-the-want #6** asked for `evaluator.md:1708`'s hedge to be re-worded rather than half-deleted. Right, and adopted — but the finding treats the hedge as the sheet's own wording. It is the document's; the sheet's error was keeping the sentence unchanged, not writing it.

### Findings I did not act on

None. Every finding either changed the sheet or is answered above with the reason and the line.

### What none of the three critiques covered, and this sheet does not either

Nobody checked the nine documents against `docs/tooling/mcp.md`'s DTOs or `tools/Precept.Mcp/CatalogFormatters.cs`. §3 flags `mcp.md:242` and `:269` as surviving-but-re-described, and §5 item 4 notes that a `RecoveryHint` decision would move the formatters, but no inventory of the MCP projection was taken. If `Fault` or `FaultCode` is projected anywhere else in the tooling documents, this pass will not have found it.

---

### Files referenced (absolute)

`/home/sfalik/source/repos/Precept/docs/Working/what-i-want-2026-07-16.md`, `/home/sfalik/source/repos/Precept/docs/working-reset/phase-2/contradiction-register.md`, `/home/sfalik/source/repos/Precept/docs/working-reset/phase-2/fifth-pass-corrections.md`, `/home/sfalik/source/repos/Precept/docs/runtime/precept-builder.md`, `/home/sfalik/source/repos/Precept/docs/runtime/evaluator.md`, `/home/sfalik/source/repos/Precept/docs/runtime/descriptor-types.md`, `/home/sfalik/source/repos/Precept/docs/runtime/fault-system.md`, `/home/sfalik/source/repos/Precept/docs/runtime/runtime-api.md`, `/home/sfalik/source/repos/Precept/docs/runtime/result-types.md`, `/home/sfalik/source/repos/Precept/docs/runtime/README.md`, `/home/sfalik/source/repos/Precept/docs/compiler/proof-engine.md`, `/home/sfalik/source/repos/Precept/docs/compiler/soundness-and-coverage.md`, `/home/sfalik/source/repos/Precept/docs/compiler/diagnostic-system.md`, `/home/sfalik/source/repos/Precept/docs/compiler-and-runtime-design.md`, `/home/sfalik/source/repos/Precept/docs/language/precept-language-spec.md`, `/home/sfalik/source/repos/Precept/docs/language/catalog-system.md`, `/home/sfalik/source/repos/Precept/docs/glossary.md`, `/home/sfalik/source/repos/Precept/docs/tooling/mcp.md`, `/home/sfalik/source/repos/Precept/docs/contributing/anti-patterns.md`

No file was created, edited or deleted; no git command was run.