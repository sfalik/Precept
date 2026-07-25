---
status: Draft — reworked 2026-07-24 against the shared write-site surface and the 2026-07-23 review findings; see § Review record. Still UNLOCKED; the owner signs off the re-lock.
phase-target: TBD — precedes the constraint-obligation build (matrix § Storage names constraint-obligation design time as the trigger for these catalog entries)
comparable-systems-research-status: strong
sources-consulted:
  - docs/Working/obligation-discharge-matrix-2026-07-19.md **rev 11** § The cell — the 2026-07-21 citation duty ruling (`:122`) and its structural-fact refinement (`:133`)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 § The minting rule — symmetric attachment (`:228`), the three-leg mention set (`:229–232`), cross-field modifiers (`:234`), activation sites (`:237`), site identity (`:240`)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 § Vocabulary — write plan (`:53`), the whole-write-plan ruling, discharge contract, premise classes
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 § The cell — the carried-forward-write ruling (`:140`) and its citation consequence (`:147`)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 § Case shapes — the write-site-category axis (`:172–180`), the rule/Invariant row (`:63`), the editable-ingress cell (`:442`)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 § Rule validity — the transport rule's eight-writer kill leg (`:355`), `provdeps` as the type-structural discriminator (`:359–363`)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 § Amendments — the two amendment classes (`:490`)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md rev 11 — the `omit` canon conflict, ⧖ Q13 (`:294`, `:459`)
  - docs/Working/constraint-establishment-preservation-obligations-2026-07-23.md — the companion design that mints what this one counts; `PRE0167` / `PRE0168` are its codes
  - docs/Working/certificate-steps-membership-2026-07-12.md Decision 1 — the eleven certificate premise kinds
  - docs/compiler/proof-engine.md § Obligation Generation Contract, § Catalog-Driven Obligation Instantiation, § ProofRequirement Catalog DU
  - docs/language/precept-language-spec.md § 0.1 — the eleven design principles
  - docs/language/precept-language-spec.md § 3A.4 — the eight in-operation writers (`:1979–1992`) and the nine-phase operation execution order (`:1996–2006`)
  - docs/language/precept-language-spec.md § 0.7 / `:272`, `:1969` — the boundary of the guarantee; restored state is not re-governed on load
  - docs/language/precept-language-spec.md `:2069`, `:2075–2076`, `:2081–2089` — construction is fully governed and composes existing constraint forms
  - docs/language/precept-language-spec.md § Open questions `:2200`, `:2202`, `:2204` — the half-catalogued write surface, state-action multiplicity, and whether the writer set is closed
  - docs/philosophy.md — core commitments
  - research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md — Event-B, Dafny, refinement and dependent types on the assume-vs-discharge line
  - src/Precept/Language/ProofRequirementKind.cs — **thirteen** enum members (verified at HEAD 2026-07-24)
  - src/Precept/Language/ProofRequirement.cs — the ProofRequirement DU (twelve sealed subtypes at HEAD; `Modifier` has none) and ProofSubject
  - src/Precept/Language/Action.cs — `ActionSlotRole.IntoTarget`, the already-declared second write target
  - src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs — the in-tree catalog-vs-code coverage gate
  - src/Precept.Analyzers/DiagnosticCoverageScanner.cs — how that gate computes its expected set
  - precept_compile probe (2026-07-23, HEAD) — the OrderTotals witness and the modifier/rule asymmetry probe
  - local `Compiler.Compile` harness (2026-07-24, HEAD) — the per-category built-status table in § The write-site surface § 8
  - docs/Working/bugs.md — BUG-033, BUG-035, BUG-036
  - src/Precept/Language/ActionKind.cs — the fifteen action kinds, checked against the § 3A.4 writer table
  - src/Precept/Language/Actions.cs — ClearApplicable, which gates `clear` on the Optional modifier for scalars
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds — the five constraint forms and their obligation shapes, incl. edge-triggered entry/exit ensures
---

# Linkage and completeness for constraint establishment and preservation

## Review record

### 2026-07-24 (pass 2) — corrections identified, **nothing written into this document**

Recorded in `establishment-preservation-pass2-findings-2026-07-24.md`. Status stays **Draft**.

Three defects in this document were confirmed at HEAD and must be fixed in pass 3:

- **`:526` is wrong and its doc-update entry is harmful.** The `ProofRequirement` DU carries **thirteen**
  sealed subtypes, not twelve: `ModifierRequirement` (`src/Precept/Language/ProofRequirement.cs:157`)
  covers `ProofRequirementKind.Modifier` and simply breaks the `*ProofRequirement` naming convention.
  `docs/compiler/proof-engine.md:531` is therefore **correct**, and `:834`'s doc-update entry — which
  orders a "correction" to it — would edit a correct canonical statement into a false one. **Delete that
  entry.** (A rename to `ModifierProofRequirement` is a legitimate separate cleanup; it is not doc drift.)
- **`:554`** — `PRECEPT0026` does not enforce one-to-one enum↔DU correspondence; it polices switch-arm
  completeness over `[CatalogDU]` types. The correspondence is **test**-held
  (`ProofRequirementCatalogTests`, which asserts a count of 13). The companion carries the same false
  claim at its `:660`.
- **`Expected(C)` (`:469`) has no carve-out** for the `maxcount`/`mincount` desugaring exclusion the
  companion's inventory (`:690`) requires, and the four coupling points at `:819–822` do not list it.
  As written, the pair fires a false-positive `PRE0166` on **every** `maxcount` file.

Also: § Open questions live item 1 (the admissibility-condition owner question) was independently ruled
already-answered — `compiler-readiness-STATUS.md:145–157` assigns pass 2 an editorial sharpening, not a
fork. Delete it rather than carrying it into a re-lock. And the "closed in the first draft" item holding
that establishment applies to exactly two constraint kinds is what leaves the transition-moment
obligation defined nowhere in either document; the proposed fix reverses it to four kinds, which must be
argued rather than silently overwritten.

Unchanged and still owed from the shared section: `Expected(C)` must be re-derived from whatever
quantifier the companion settles on, not patched; the shared § 6 range should reconcile to the
companion's "(5)–(7)"; and § 10 exists only in the companion's copy.

### 2026-07-24 — the three findings worked, and the shared write-site surface incorporated

**Finding 1 is closed by demotion, not by repair.** The review was right that the admissibility condition has no rejection power, and the honest response is to stop claiming it does. § Semantic Rules now states the condition as a *serialization and inspectability contract discharged by construction*, and says in one line that all of this design's rejection power lives in the completeness check (`PRE0166`) and, for the author-facing failure, in the companion design's own verdicts (`PRE0167` / `PRE0168`). Decision 1 is re-stated on the axes where the two encodings actually differ — cross-proof consistency, output size, and replay cost — and its stakes drop from `high` to `medium`. Acceptance criterion 7, which described an unreachable state, is replaced by a serialization criterion that can actually fail.

The reconciliation the review did not ask for but the demotion forces: **`PRE0165` is deleted.** It fired on "a rule is used as a fact but a write can break it", which is exactly the condition the companion's `PRE0168` reports on the same file, from the same undischarged preservation obligation. The matrix's base-minimality standard is the rule that settles it: a base witness program "rejects *only* for the target obligation" (`obligation-discharge-matrix-2026-07-19.md:96`), and two codes firing on one undischarged obligation makes every base program for that obligation non-minimal by construction. So one of the two codes has to go, and it is this document's, because the obligation belongs to the companion. This design now introduces exactly one diagnostic, `PRE0166`, and it is compiler-internal.

**Finding 2 is closed at the surface level.** `Expected(C)`'s preservation leg no longer quantifies over `Handlers` but over **governed operation occasions** — construction rows, transition and stateless event rows, and editable-field doors — computed from the same declared write-site surface the companion's minting walk reads. That surface is now written down once, in § The write-site surface, and both documents range over it. The update patch is in scope as W7; state exit and entry actions are in scope as W3, folded into the enclosing row's write plan rather than treated as their own occasions. `Restore` stays in the writer list, because the transport rule's kill leg quantifies over all eight (`obligation-discharge-matrix-2026-07-19.md:355`), and stays out of the preservation product, because `Governed = false` (`precept-language-spec.md:272`, `:1969`) — the review record's earlier inclusion of `Restore` among the operations `Expected(C)` misses was loose, and excluding it is the canonical answer. Without that distinction `PRE0166` would fire on every file declaring any constraint. The self-contradiction on cross-field modifiers is resolved in favour of the matrix (`:234`): under § 2.4 a constraint modifier *is* a rule, so the `mentions → {F}` clause is deleted rather than reconciled.

**Finding 3 is closed in all four parts.** (a) Both misattributed witnesses are replaced. The qualifier-modifier hole is no longer offered as a missing-category witness — the review is right that the per-file comparison catches it, and § The write-site surface § 8 now supplies what the analyzer level actually needs: five of the eight categories mint nothing at all today, and W6 (the `omit` reset) "has neither a catalog member nor an implementation" (`precept-language-spec.md:2200`). BUG-033 is moved out of Decision 3's supporting evidence and into its counter-evidence, where the review correctly places it: two independent consumers making the identical mistake is falsifier 4's condition, and Decision 3's claimed strength is restated downward to what survives. (b) `PriorAction` moves out of the no-record group into a group of its own, carrying a **write citation** rather than a coverage record, which is what the matrix's ruling actually demands (`:147`, "a proof consuming a written value names the write it came from"); acceptance criterion 14's fourth situation (criterion 12 before the renumber) is rewritten to test that. (c) The `DeclaredPresence` split now reaches the `omit` reset and takes the conservative position — a field some state omits is treated as breakable and carries a record, with the discharge itself `Unresolved` until ⧖ Q13 is ruled, which is sound under either reading of the conflict (`obligation-discharge-matrix-2026-07-19.md:294`, `:459`). (d) "the eleven existing requirement kinds" is corrected to thirteen, with the DU-versus-enum discrepancy stated exactly.

**Matrix citations refreshed rev 10 → rev 11**, each verified to resolve at HEAD.

**Still open after this pass**: the construction case carries a *plan set* rather than a plan, because `precept-language-spec.md:2202` forbids assuming entry actions run at construction — refusal in the unsettled direction, not a settlement. Premise class (d) across a `Restore` needs a base case that `Governed = false` does not supply (§ Open questions). The `omit` post-state value remains unusable as a premise. And the § 3A.4 writer list is still asserted rather than build-enforced; that is what `Precept0030` exists to convert from silent to loud. Status stays `Draft`.

### 2026-07-23 — why this stopped being locked

Locked and then reviewed adversarially the same day. Three findings, recorded rather than patched. **All three are worked in the 2026-07-24 entry above; this entry is the historical record of what was found, not an open list.**

**1. The admissibility condition has no rejection power.** Both its conjuncts are true by construction. "Every check in the coverage record is discharged" can never be the reason for a rejection, because an undischarged obligation already rejects the file on its own under no-deferral. And "the proof's citation equals the coverage record" is trivially satisfied, since the record is a function of the fact and the same engine computes both sides — there is no independently derived second value to disagree with. So the citation duty as encoded here is a serialization annotation with no accept/reject consequence, and all the rejection power in the design lives in the completeness check.

Three things follow. Decision 1 — which fork to take on the encoding — was immaterial, because the two encodings agree in the only way that matters: neither can fail. Acceptance criterion 7 describes a state the pipeline cannot reach. And the Goal, that `OrderTotals` must not compile, is discharged entirely by the companion design; nothing here is load-bearing for it.

**2. `Expected(C)` misses two of the four mutating operations.** Its preservation leg quantifies over `Handlers`. The update patch on an editable field and `Restore` are both writers § 3A.4 enumerates, and neither belongs to a handler — the spec lists `Create`, `Fire`, `Update` and `Restore` as peer operations, and the matrix names the editable-field door as a first-class write-site category with its own case shape. So a rule over an editable field gets no expected preservation check for the edit door, the set comparison passes, and the file compiles with the rule breakable through `Update`. That is the failure class this document exists to close, reintroduced through its own quantifier.

The same section also contradicts itself four lines apart on whether a cross-field modifier mentions one field or two. The matrix is explicit that it is two. Verified at HEAD: `field Floor as decimal max Ceiling` with a handler writing `Ceiling` produces **zero** proof obligations.

**3. Both witnesses used to justify Decisions 3 and 5 are misattributed.** The qualifier-modifier hole is offered as proof that the per-file check is blind to a missing category — but it is not a missing write-site category. The site is present and demonstrably obligated for the `min` spelling on the same line of the same file, so the per-file set comparison would catch it. And BUG-033, offered as the instance motivating an independently derived expected set, is the *counterexample*: two structurally independent consumers, in different pipeline components, made the identical mistake. This document's own falsifier 4 says that condition means the independence is procedural fiction. It was met before the document was written.

**Also found**: Decision 4 contradicts a locked ruling without quoting it — the matrix rules that a proof consuming a written value names the write it came from, while the decision places `PriorAction` in the group needing no record, and acceptance criterion 12 still demands a test for that situation. `DeclaredDefault` is exempted on grounds ("it *is* an establishment fact") that the 2026-07-21 ruling treats as the definition of a value-fact. The `DeclaredPresence` split enumerates `clear` and stops, without reaching the `omit` reset, on which the matrix carries an unresolved canon conflict. And "the eleven existing requirement kinds" is wrong — the discriminated union has thirteen.

**What survived review**: Decision 2 (two catalog kinds rather than one parameterized kind) is sound and correctly grounded, with the Event-B precedent quoted faithfully. Decision 6 (compiler-internal severity) is right and its dead-rows precedent applies. Set equality rather than containment is correct under the exact-power contract, with the reason stated rather than asserted. The premise partition is total over the eleven certificate premise kinds, even though two placements are wrong. And § The cost's correction to the matrix's own rationale is accurate — it identifies that the citation duty relocates the enumeration rather than removing it, and stops one step short of finding 1.

## Goal

When this is done, a proof that leans on a fact an operation could break cannot be accepted unless the checks that make that fact true, and keep it true, are named, present, and passing — and the compiler independently knows the full list of checks that should exist, so a missing one fails the compile instead of passing silently.

**Which half of that this document owns.** The 2026-07-23 review established that the naming half has no rejection power of its own: an undischarged check already rejects the file under no-deferral, so a citation cannot be the reason anything fails. This document therefore owns two things and claims no more. The **completeness check** is its rejection power — it is what makes "the full list of checks that should exist" a computed, compared, per-compile property rather than an assertion about the generator. The **citation** is its inspectability contract — it is what makes the answer to "why was this proof allowed" readable at the point of consumption instead of reconstructible only by auditing the whole file.

The `OrderTotals` file in § The problem, which compiles clean today while resting a division on a rule nothing enforces, must not compile — but it is **the companion design's** test, not this one's. The check that rejects it is the preservation obligation the companion mints for the `Reprice` occasion, reported as `PRE0168`. What this document contributes to that file is the guarantee that the obligation was minted at all: the completeness check is what turns "the compiler forgot to obligate this write" from silence into `PRE0166`.

## Scope

**In scope.**

- The *linkage*: how a proof records which checks a fact rests on, and what a reader or a re-checker does with that record.
- The *completeness check*: how the compiler knows the full set of checks that should exist for a file, computed without asking the code that creates them.
- Both, covering all four places a proof gets a fact that an operation can break: field modifiers, rules and ensures, qualifiers whose value comes from a field, and a value written earlier in the same operation.
- The catalog entries the above requires, and what the proof certificate has to carry.
- **The write-site surface itself** (§ The write-site surface) — the eight writer categories, the four operations and their `Governed` flag, and the derivations both this design and the companion range over. Added to scope on 2026-07-24: it was previously assumed as an input by both designs, and each assumed a different one.

**Out of scope.**

- The rule and ensure checks themselves — what they prove, over which sites, and how they discharge. That is the next design pass. This one says what a reference to such a check looks like and how the compiler counts them; it does not build them.
- The independent re-checker that would replay a certificate. The certificate format work already deferred it, and nothing here changes that.
- Diagnostic wording beyond the one new message this design introduces (`PRE0166`). The author-facing message for an unenforced constraint is the companion design's `PRE0168`.

**Deferred to future.**

- Repairing the specific holes this machinery makes visible — the qualifier modifiers that create no write-site check (`nonzero`, `positive`, `nonnegative`), BUG-033's missed `into` target, BUG-035's missing staleness check on a qualifier's source field, and the five writer categories that mint nothing at all (§ The write-site surface § 8). This design makes them fail loudly; fixing each is its own work, and it is the companion's minting walk that does it.
- Whether the completeness check can be relaxed for files where no proof consumes any breakable fact. It cannot be relaxed safely without an argument nobody has made yet.

## The problem

Two `.precept` files, both compiled at HEAD on 2026-07-23 through `precept_compile`.

The first shows that Precept already knows how to do half of this, for one spelling only:

```precept
precept BoundVsRule

field Total as decimal default 0 max 1000
field Other as decimal default 0

rule Other <= 1000 because "The other total is capped at the same ceiling"

event Bump(N as decimal)

on Bump
    -> set Total = Bump.N
    -> set Other = Bump.N
```

`Total` produces two checks — one that its default fits inside `max 1000`, and one at the write site, which fails and rejects the file. `Other` produces **none**. Same ceiling, same write, one written as a modifier and one as a rule.

The second is the failure this design exists to stop:

```precept
precept OrderTotals

field ItemCount as integer default 1
field UnitCost as decimal default 1.0
field Total as decimal default 0.0

rule ItemCount > 0 because "An order with no items has nothing to price"

event Reprice(NewCount as integer, NewCost as decimal)

on Reprice
    -> set ItemCount = Reprice.NewCount
    -> set UnitCost = Reprice.NewCost
    -> set Total = Reprice.NewCost / ItemCount
```

At HEAD this compiles with **zero diagnostics**, and the engine reports the divisor check as `Proved` by the strategy that reads rules as facts. The rule `ItemCount > 0` is used to prove the division safe. Nothing checks that the rule is true: `set ItemCount = Reprice.NewCount` writes it from an argument with no constraint at all. Fire `Reprice(0, 5.0)` and the engine divides by zero.

This is the shape the matrix calls out: a proof that is individually valid and replayable, resting on a fact that was never earned. It is silent, it is spread across the file, and it looks exactly like success. A certificate cannot catch it, because a certificate records work that was done and this is work that was never attempted.

Your 2026-07-21 ruling chose the fix: a proof may not use such a fact unless it **names the check that established it and every check that preserves it**, each of them passing. This design builds that.

## Two words used throughout

**A fact an operation can break** — the matrix calls this a *value-fact*. Example: `ItemCount > 0`. Some operation in the file can write `ItemCount` and make it false, so the fact has to be earned somewhere and kept true everywhere.

**A fact fixed by the declaration** — the matrix calls this *type-structural*. Example: `field Price as money in 'USD'`. No operation anywhere can change a field's declared currency; the language has no construct that retypes a slot. Your 2026-07-21 refinement says these cite their declaration and stop — there is nothing to earn and nothing to keep.

The whole design turns on which of the two a fact is, and the test is behavioural, not about how the fact is spelled: *can some operation establish or break it?*

## The write-site surface

*Incorporated 2026-07-24. This section is shared verbatim in substance with `constraint-establishment-preservation-obligations-2026-07-23.md`. It exists because both designs walk the write surface and, before this pass, walked different ones: the companion minted preservation per **handler**, and this document's `Expected(C)` quantified over **`Handlers`**. Neither quantifier reached a state-action write, an editable-field edit, or a computed recomputation — and `Minted(C) = Expected(C)` held only because both walks were wrong in the same way. Fix one and the completeness check fires on every file that has any of the three. This is the one surface both walks range over.*

### 1. Three notions, kept apart

- **Writer category** — a mechanism that places a value into a field slot during one operation. Eight of them, `precept-language-spec.md:1981–1988`. A closed list about the *language*.
- **Operation occasion** — one reachable execution of `Create` / `Fire` / `Update` / `Restore` along one route. Per the site-identity ruling, a site is an evaluation occasion, not a syntactic position (`obligation-discharge-matrix-2026-07-19.md:240`).
- **Write plan** — for one occasion, the ordered sequence of firings of every writer category that occasion encloses, in the phase order of `precept-language-spec.md:1998–2006`. This is the matrix's existing term (`:53`) and the unit the whole-write-plan ruling quantifies over.

A "write site" is derived, not declared: the triple (occasion, category, target field). The matrix's write-site-category axis (`:172–180`) is a *projection* of the writer list, not a peer list — see § 3.

### 2. The eight writer categories

Enumerated from `precept-language-spec.md` § 3A.4 *What can write during one operation* (`:1979–1988`), each with the phase it occupies (`:1998–2006`) and the operations that can enclose it (`:1996`: "A phase that does not apply to an operation is skipped; the relative order of the phases that do apply never varies by operation").

| # | Category | Spec | Phase | Enclosed by | How its target set is read |
|---|---|---|---|---|---|
| W1 | Action-chain write in a transition row or event handler | `:1981` | 5 | Create, Fire | the action's primary field target, per `ActionMeta` (`src/Precept/Language/Actions.cs`) |
| W2 | The `into` binding second target of `dequeue` / `pop` / `dequeueBy` | `:1982`, `:1990` | 5 | Create, Fire | the action's `ActionSlotRole.IntoTarget` slot (`src/Precept/Language/Action.cs:132`) |
| W3 | A state exit or entry action's action chain | `:1983` | 3 (exit), 6 (entry) | Create†, Fire | same as W1, at the `ConstructKind.StateAction` positions (`src/Precept/Language/ConstructKind.cs:39`) |
| W4 | Default materialization at construction | `:1984` | before 5 (`:2075`) | Create | every field declaring `default`, in declaration order (§ 3.5) |
| W5 | Computed-field recomputation | `:1985` | 7 | Create, Fire, Update, **Restore** | every `<-` field |
| W6 | The `omit` reset on entering a state that omits the field | `:1986` | 4 (`:2001`); at construction, the hollow build (`:2075`) | Create, Fire | the fields the entered state omits (`ConstructKind.OmitDeclaration`; `omit` as an access modifier, `catalog-system.md:495`) |
| W7 | An update patch applied to an editable field | `:1987` | 5 | Update | the fields the current state marks `editable` (`ConstructKind.AccessMode`) |
| W8 | Restore writing persisted values into slots | `:1988` | 5 | Restore | every stored field |

† W3-under-`Create` is the unsettled case. See § 6.

**Reconciliation with the matrix's write-site-category axis** (`obligation-discharge-matrix-2026-07-19.md:172–180`). The matrix axis has five values; they are not five more writers. `handler set` (`:176`) and `collection action` (`:179`) are both W1, split by `ActionKind` — a split the action catalog already owns, so the axis reads it rather than restating it. `entry hook / state action` (`:178`) is W3. `editable-field edit` (`:177`) is W7. `computed-field transitive` (`:180`) is **not a writer at all** — it is mention-set leg (c) (`:232`), which maps a constraint onto the write sites of a computed field's *inputs*; the writer that changes the computed slot itself is W5. Writing that mapping down once is load-bearing: it is the reason the two walks can read one list and produce the same product.

**Double-counting is closed by the collapse, not by deduplication.** Leg (c) reaches occasion `O` through the input's W1 firing, and W5 reaches the same occasion through the computed slot. Because obligations run over the whole write plan per occasion (matrix `:53`), both routes name the same single obligation. Had the design stayed per-write, the two walks would each have to deduplicate identically or the set comparison would fail — a second, independent reason the whole-write-plan ruling is load-bearing here.

### 3. Per-category attributes — and why preservation duty is not one of them

`Restore` is a § 3A.4 writer (W8) and must stay on the list, because the transport rule's kill leg "unions the write-targets of every firing that *may run strictly between* p and q, over all **eight** writers" (matrix `:355`) — a fact established before a restore does not survive it. But `Restore` owes no preservation obligation: restored state "is trusted as valid at the time it was persisted: hydration is fast and does not re-validate" (`precept-language-spec.md:272`), and § 3A.4 says it in one line — "(Restored state is neither: it is trusted as valid at persistence time and is re-governed only by the next operation's sweep — §0.7.)" (`:1969`). A flat writer list cannot express that.

#### Decision W-1: preservation duty is declared on the **operation**, not on the writer category

`OwesPreservation(category, occasion) = occasion.Governed`, where `Governed` is a declared per-operation attribute: true for `Create`, `Fire`, `Update`; false for `Restore`.

- **Rationale**: a per-category boolean is **ill-typed against canon**, and W5 is the proof. `precept-language-spec.md:1985` declares computed-field recomputation "recomputed in every operation, **including `Restore`**". A per-category `OwesPreservation` on W5 would have to be simultaneously true (under `Fire`) and false (under `Restore`). The duty varies over the (category, operation) pair, and the only axis it actually varies on is the operation — which is where canon puts it: `:272` is a statement about *the boundary of the guarantee*, an operation-level fact, not a fact about slot-writing mechanisms. Declaring it there makes W8's exemption **derived** (its enclosure is `{Restore}`, and `Restore` is ungoverned) rather than stipulated, so no category carries an exception clause.
- **Alternatives considered and rejected**:
  - *A per-category `OwesPreservation` boolean, false on W8.* Rejected: W5 above. It would also need a second exception the moment any future writer fires under both a governed and an ungoverned operation.
  - *Drop `Restore` from the category list, since it owes nothing.* Rejected: it would silently break the transport rule, which quantifies over all eight (matrix `:355`). Removing a writer to express "owes no preservation" conflates two different uses of one list — precisely the conflation this section exists to prevent.
  - *Keep two lists — a "writers" list for fact survival and a "preservation sites" list for obligations.* Rejected on the catalog rule: two hand-maintained lists of the same language fact drift, and Decision 3's independence requires both walks to read *one* declaration, not two.
- **Precedent**: in-tree, `ActionMeta` already separates `WriteSemantics` (`ActionWriteSemantics`) from `Effect` (`ActionEffectClass`) for exactly this reason — one action's write behaviour is read by two consumers asking different questions (`FieldNeverSet` vs sequential proof flow), and the catalog declares both rather than letting one consumer infer the other. Externally, Event-B's obligation generator produces no obligation for an initialisation outside the model's mutating events, without removing that state from the model.
- **Tradeoff accepted**: a reader of the write-site catalog alone cannot answer "does this owe preservation" — they must also read the operation catalog. Accepted because the join is one hop and mechanical, and the alternative encodes a falsehood about W5.

#### What each category declares

`WriteSiteMeta` — a discriminated union, per `catalog-system.md` ("When members fall into groups with genuinely different fields, the meta type is a **discriminated union**"), because the *target derivation* genuinely differs in shape per category (an action slot, a modifier scan, a state's omit list, "every stored field"). Fields common to every subtype:

- `Kind` — the closed 8-member enum.
- `Phase` — the § 3A.4 phase ordinal, which is what orders a plan.
- `Enclosure` — the subset of `{Create, Fire, Update, Restore}` under which the category can fire.
- `DeclaredAt` — the existing catalog or spec surface that owns the category's own semantics (§ 7).
- `BuiltStatus` — whether the pipeline consumes it today; the allow-list key for the build-time analyzer (Decision 5). Populated in § 8.

`OperationSurfaceMeta` — a 4-member catalog: `Kind`, `Governed`, and whether the operation is an establishment occasion.

Everything the two walks need is then **derived** from those two declarations plus the mention set:

```
WritePlan(O)      = firings of every cat with O ∈ Enclosure(cat), ordered by Phase
Targets(O)        = ⋃ { targets of cat at O | cat ∈ WritePlan(O) }
Owes(C, O)        = O.Governed  ∧  Targets(O) ∩ mentions(C) ≠ ∅
```

No third knob. That matters: every independent attribute is a place the two walks can disagree, and the completeness check's whole value is that they cannot.

**The establishment role needs no separate attribute** — it is `Enclosure` intersected with the establishment occasions (§ 5). W4 contributes only at construction because its enclosure is `{Create}`; W7 never contributes because `Update` is not an establishment occasion; W8 never, for the same reason plus `Governed = false`.

### 4. The preservation unit — occasions, not handlers

#### Decision W-2: `Preserve` is keyed to the **governed operation occasion**

Both designs previously keyed to a handler. Replaced, in both, by

```
Preserve-units(C) = { O ∈ Occasions(file) | Owes(C, O) }
```

where `Occasions(file)` is:

- one occasion per **construction row** (`Create`); reject rows carry no data obligation (matrix `:184`);
- one occasion per **transition row** and per stateless **event row** (`Fire`); reject rows excluded, as above;
- one occasion per **editable-field door** — the (state, editable field) pairs the access modes declare (`Update`); the matrix already treats this as a first-class category with its own case shape and its own discharge, ingress evaluation (matrix `:177`, `:442`);
- **no occasion for `Restore`** — `Governed = false`, so `Owes` is false for every constraint.

The last bullet is the one that keeps the completeness check quiet. `Restore` is available on every file. If `Expected(C)` included it (it is a § 3A.4 writer) while the minting walk did not, `PRE0166` would fire on **every file declaring any constraint**. That failure mode is why the attribute in § 3 has to exist, and it is the concrete cost of getting it wrong.

A state exit or entry action is **not its own occasion**. It is phase 3 or phase 6 of the enclosing row's plan (`:2000`, `:2003`), so the entry-action write folds into that row's single obligation. This is what closes the companion's Review-record finding 1: the `to Closed -> set Total = 5000` witness, which today rejects with `PRE0078` while the compiler reports `eventHandlers: []`, is covered because the write belongs to the plan of the row that enters `Closed` — no handler required.

- **Rationale**: the occasion is the unit the *runtime* commits, and the obligation must be keyed to the unit whose post-state is observable. The working copy is promoted or discarded whole (`precept-language-spec.md:1967`), so the only configurations a constraint can be true or false *of* are pre-state and post-state of one occasion — which is exactly the whole-write-plan ruling (matrix `:53`). The handler was never that unit; it is one *phase* of it (phase 5, `:2002`), sitting between the exit actions at phase 3 and the entry actions at phase 6. Keying to the handler therefore did not just miss some writers, it keyed to a sub-part of the atomic unit, which is why the miss was systematic rather than accidental.
- **Alternatives considered and rejected**:
  - *Keep the handler key and add the missing categories as extra keys.* Rejected: it re-introduces per-write keying through the back door, since an entry action and its enclosing row would mint two obligations over one commit and the intermediate state between them would become observable to the proof — which the whole-write-plan ruling forbids.
  - *Key to the (state, event) pair rather than the row.* Rejected on the site-identity ruling: "One written expression reached by several routes mints one fault obligation *per route*" (matrix `:240`). Rows on the same pair are different routes with different guard facts; collapsing them loses the per-route premises.
  - *Make the editable-`Update` door a modifier on the enclosing state's occasions rather than an occasion.* Rejected: an `Update` encloses no dispatch, no exit actions and no entry actions, so its write plan is genuinely different in shape — and the matrix already gives it its own case shape and its own discharge mechanism, ingress evaluation (`:442`).
- **Precedent**: Event-B's preservation obligations are per *event*, where an event is the atomic state change — the analogue of a Precept occasion, not of a Precept handler; § Language Design Grounding carries the term mapping. In-tree, the matrix's own editable-ingress cell (`:442`) already treats the edit door as a peer unit with its own obligation and its own discharge, so this decision aligns the quantifier with a case shape the matrix had already written.
- **Tradeoff accepted**: `Occasions(file)` is larger and less uniform than `Handlers` — three structurally different kinds of occasion, one of which (construction) can carry a plan *set* rather than a plan (§ 6). Both walks pay that complexity, and a bug in the occasion enumeration is a bug in both simultaneously, which is the residue Decision 3 already records. Accepted because the alternative is a quantifier that provably does not cover the write surface, and the coupling is at least named and testable (§ Dependencies).

#### Decision W-3: both walks call the same function

The minting walk and `Expected(C)` differ only in *where they get the category list*, never in what they compute from it. The companion's minting rule becomes

```
  C ∈ Constraints(file)   O ∈ Occasions(file)   Owes(C, O)
  ──────────────────────────────────────────────────────────
     ConstraintPreservationProofRequirement(C, O)  minted
```

and this design's expected set becomes

```
Expected(C) = { Establish(C, e) | e ∈ EstablishmentSites(C) }
            ∪ { Preserve(C, O)  | O ∈ Occasions(file), Owes(C, O) }
```

Same product, two derivations from one declaration — which is exactly the independence Decision 3 requires, and it now holds over a surface that actually covers the write plan.

- **Rationale**: the matrix requires the expectation be derived without asking the minting code (`:117`), and the *only* way to satisfy that while still guaranteeing agreement is for both walks to compute the same function of the same declarations. If they compute different functions, `Minted(C) = Expected(C)` becomes an accident that holds until one side is edited; if they call the same *code*, the independence requirement is violated outright. Two derivations of one declared surface is the narrow path between those, and it is why the surface must have no attributes beyond the three in § 3 — every extra knob is a place the two functions can differ.
- **Alternatives considered and rejected**:
  - *One shared function, called by both.* Rejected as the circularity the matrix names (`:117`): the check would agree with itself and see nothing. Note this is the alternative that would be *easiest* to build and hardest to argue against on engineering grounds; it is refused on the locked constraint, not on taste.
  - *Two functions over two declarations — a "writers" catalog for minting and a "sites" catalog for expectation.* Rejected on the catalog rule against parallel copies, and because the drift between two hand-maintained lists would surface as `PRE0166` on ordinary files, i.e. as a false compiler-defect report.
  - *Leave the two walks as they are and reconcile them with tests.* Rejected on the matrix's own tests-do-not-cover-this passage (`:118`) and on the observed outcome: they had already drifted into two different quantifiers, and no test caught it — the 2026-07-23 review did.
- **Precedent**: `PRECEPT0027` is the in-tree shape — one catalog (`DiagnosticCode`), two consumers (the pipeline's emission sites and the scanner's expected set), compared. Externally the survey found no comparator, which § Architecture Grounding records as the design's one genuine novelty.
- **Tradeoff accepted**: the two designs are now coupled on four specific points (§ Dependencies, *Companion*), and a divergence on any of them produces a false-positive `PRE0166` rather than a silent miss. Accepted deliberately — a loud wrong answer is the right failure mode for a check whose purpose is to make silence impossible — but it is a real cost and falsifier 6 is written to detect it.

**One correction this document owes itself.** Its § Semantic Rules previously said a field modifier's expected set uses "`mentions` replaced by `{F}`", four lines after stating that a desugared cross-field modifier mentions both fields. The matrix is explicit: `field Floor as decimal max Ceiling` "desugars to a constraint mentioning **both** fields, so a write to `Ceiling` carries the obligation just as a write to `Floor` does" (matrix `:234`). Under § 2.4 a constraint modifier *is* a rule, so a modifier has no separate rule — `mentions(·)` applies unchanged and the `{F}` clause is deleted.

### 5. Establishment sites

```
EstablishmentSites(C) = { the construction occasion(s) }                for ConstraintKind.Invariant
                      = { construction } ∪ { every row entering S }     for StateResident on S
                      = ∅                                              for StateEntry, StateExit, EventPrecondition
```

The three edge-triggered and ingress-class kinds have no separate establishment site: the transition-moment obligation *is* the check (matrix `:63–67`; "nothing is owed while merely resident"), and an event precondition is discharged by ingress evaluation. Residency establishment at **every** entry, not only the initial state, is the 2026-07-20 activation-sites ruling (matrix `:237`); the matrix records that today's engine folds residency ensures against defaults for the initial state only (`:69`) — implementation gap, not a definitional limit.

### 6. The construction case

Canon is not ambiguous about *whether* creation is governed. It is ambiguous about *one phase* of the construction plan. The surface is written so that the second ambiguity never touches the first.

**What the surface asserts.**

1. `Create` is a governed operation. Construction "fire[s] the initial event with the caller's args through the standard pipeline — same guards, same mutations, same ensures, same constraint checking as any other event" (`:2076`), and the entity "is governed from birth by the same type constraints, rules, and state-scoped ensures that apply to its initial state" (`:2069`). Establishment at construction is implementation against canon, not a design choice.
2. Every constraint that applies at the initial state must be **established** over the construction plan. The composing sets are canon and need no new form: arg ensures, field constraints, global rules, entry ensures, residency ensures (`:2081–2088`), and "No special 'construction constraint' form is needed. `to <InitialState> ensure` is the natural construction-time rule" (`:2089`).
3. The construction plan contains, in order: W4 default materialization and the hollow build's omit application — "Build a hollow version (defaults applied, initial state set, omitted fields structurally absent)" (`:2075`) — then W1 and W2 from the initial event's action chain (phase 5), then W5 recomputation (phase 7), then constraint evaluation (phase 8).
4. Under prove-or-reject, a constraint whose truth at creation cannot be established **rejects**. That is the engine working. Corpus rejections are measured and listed, never used as an argument to weaken (1)–(3).

**What the surface deliberately does not assert.**

5. **Whether W3 (entry actions) is in the construction plan.** `:2202` states the standing default: "Until these are ruled, no analysis may assume an ordering among several state actions on one state, nor that entry actions run at construction." The surface therefore carries, for the construction occasion, a **plan set** rather than a plan: `{ plan-without-W3 }` when the initial state declares no entry actions, and `{ plan-without-W3, plan-with-W3 }` when it does. Establishment holds iff it holds over **every** member. This is still one obligation per (constraint, occasion) — the whole-write-plan ruling is untouched; the obligation's proof condition is a conjunction over the set.
6. **Any order among several state actions on one state.** Same sentence at `:2202`. Where a state carries *k* state actions, the plan set carries one plan per admissible ordering and the obligation must discharge over all of them. The definitional statement is the enumeration; an order-independent abstraction is a legitimate implementation of it only if shown to accept exactly the same programs, and that equivalence is not asserted here.
7. **Self-transitions.** `:2202` leaves entry actions on a self-transition unsettled in the same breath; the same plan-set treatment applies to a self-transition row, with no further claim.
8. **What the `omit` reset leaves in the slot.** W6's *targets* are declared (the omitted fields). Its *post-state value* is conflicted canon — "canon asserts both reset-to-default and structurally-absent semantics" (matrix `:294`, `:459`, ⧖ Q13). No discharge rule may assume either reading; a proof needing W6's post-state value is `Unresolved` until the conflict is ruled.

The direction of (5)–(8) is refusal, not admission: a file whose safety depends on which reading is true does not compile. That is sound under either ruling, and when the owner rules, the plan set collapses to one member — a deletion, not a re-derivation.

### 7. Where it lives

**A suitable surface exists for three of the eight categories and must be reused, not forked.**

- `ActionMeta` already carries `WriteSemantics` (`ActionWriteSemantics`) and per-slot roles (`src/Precept/Language/Action.cs`). W1's target derivation is `ActionMeta`'s existing primary target; W3's is the same catalog read at the `ConstructKind.StateAction` position.
- W2's target is **already declared**: `ActionSlotRole.IntoTarget` (`Action.cs:132`), populated for `ActionSyntaxShape.CollectionInto` and `CollectionIntoBy`. The catalog knows about the second target; the pipeline does not consume it (§ 8). W2 must read that slot role, not re-enumerate which actions have an `into`.
- W6's target derivation reads `ConstructKind.OmitDeclaration` and the `omit` access modifier (`catalog-system.md:495`); W7's reads `ConstructKind.AccessMode`.

**A surface does not exist for the writer categories themselves, and the spec says so.** `precept-language-spec.md:1992`: "Actions are catalogued and their write semantics are catalog metadata; the other five writers are not catalogued at all — the `omit` reset in particular has no catalog member. The intended end state is that every writer is catalog-declared and that the exhaustiveness analyzer used elsewhere for catalog coverage is applied to the write surface." The `WriteSiteCategories` catalog entry in § Inventory is therefore the *already-intended* end state, not a new invention — and this section is what it declares.

**No catalog surface exists for the four operations.** `OperationKind` / `Operations.cs` is the typed-operator catalog, unrelated. `Create` / `Fire` / `Update` / `Restore` appear only in the runtime and in spec prose (`:1996`). `OperationSurfaceMeta` is a new 4-member catalog and is the smallest new surface this foundation requires.

**Prose vs metadata split.** In the catalog: the eight categories, their phase, their enclosure, their target derivation; the four operations and their `Governed` flag. Staying design prose: the derivations in § 3 (`WritePlan`, `Targets`, `Owes`), because they are functions over declared facts, not declared facts. Written nowhere: a second copy of the action list, the `into`-bearing action set, or the omit/editable field sets — all four already have catalog owners.

**Consequence for `MentionSetLegs`.** Legs (a) and (b) are scans of a constraint's own expression; leg (c) is the computed-field dataflow. Only leg (c) touches this surface, and it touches it as a *mapping onto W1/W2 targets*, not as a category. § Inventory says so, or a reader will expect a ninth category.

### 8. Built status per category

Recorded as built-status on the surface, per the spec-finalization posture: these are not defects of the definition and no bugs are filed for them. All witnesses compiled at HEAD on 2026-07-24 through the local harness (`Compiler.Compile`, printing `Diagnostics` + `Proof.Obligations`).

| Category | Minted today? | Witness |
|---|---|---|
| W1, modifier spelling | **yes** | `field Total as decimal default 0 max 1000` + `event Start(N as decimal) initial` / `on Start -> set Total = Start.N` → `IntervalContainmentProofRequirement` `Unresolved`, `NumericOverflow` error |
| W1, rule spelling | **no** | same file with `rule Total <= 1000 because "capped"` instead of `max` → `HasErrors=False`, **zero obligations** |
| W1, collection action | **yes** | `field Tags as list of string maxcount 2` / `append Tags "a"` → `CountContainmentProofRequirement` `Unresolved`, `CountBoundViolation` |
| W2 `into` target | **no** | `field D as integer default 0 max 10` / `dequeue Q into D` → only the default check on `D`; the compiler additionally warns `FieldNeverSet` on `D`, i.e. it does not treat the `into` slot as a write site at all (the BUG-033 shape) |
| W3 state action, modifier spelling | **yes** | `to Closed -> set Total = 5000` under `max 1000` → `IntervalContainment` `Unresolved` with no enclosing handler (the companion's Review-record witness, reproduced) |
| W3 state action, rule spelling | **no** | same file with `rule Total <= 1000` → `HasErrors=False`, **zero obligations** |
| W4 default materialization | **partial** | modifier spelling mints a real obligation (`NumericProofRequirement` "Default value of 'Total' must satisfy 'max'", `Proved`, strategy `Literal`); rule spelling produces a **diagnostic with no obligation** — `field Total as decimal default 5000` + `rule Total <= 1000` → `DefaultViolatesRule` error, **zero obligations** |
| W5 computed recomputation | **no** | `field Doubled as integer <- A * 2` + `rule Doubled <= 10` + `on Bump -> set A = Bump.N` (unconstrained arg) → `HasErrors=False`, **zero obligations** |
| W6 `omit` reset | **no** | `precept-language-spec.md:1992` — "the `omit` reset in particular has no catalog member"; `:2200` adds it "has neither a catalog member nor an implementation" |
| W7 update patch | **no** | `field Total as decimal default 0 nonnegative max 1000 editable` → only default checks; and `field Total as decimal default 0 editable` + `rule Total <= 1000` → `HasErrors=False`, **zero obligations** |
| W8 restore injection | n/a | owes nothing (§ 3); its only live use is the transport rule's fact-survival quantification |

Read across: **five of the eight categories mint nothing at all today, and two of the three that do, do so only for the modifier spelling.** That is the size of the gap both designs are closing, stated once. It is also the evidence base Decision 5's build-time level actually needs — see that decision.

### 9. Open dependencies this surface names and does not close

1. **Whether the eight-writer list is complete.** Asserted by the spec, not kept by the build (`:1992`, `:2204`). Every soundness argument resting on this surface inherits it. `Precept0030` (Decision 5) is what converts it from silent to loud.
2. **Entry actions at construction and state-action ordering** (`:2202`). Handled by the plan set (§ 6), not settled.
3. **The `omit` semantics conflict** (matrix `:294`, `:459`, ⧖ Q13). W6's targets are declared; its post-state value is not usable as a premise.
4. **Premise class (d) across a `Restore`.** `Restore` is ungoverned, so it owes no preservation — but the induction still needs a base case for a restored entity. Canon supplies one, and it is narrower than it looks: "the prior committed state satisfied the rules in effect **when it was written**" (`:272`). Under an unchanged definition that is the base case; under a changed definition it is not, and `:1958` points forward to migration logic that does not exist. Named here, not resolved; it belongs to whoever writes the establishment validity argument.
5. **The residency fact as a premise** (matrix `:203`). Untouched by this surface; the companion's Decision 4 keeps everything independent of it.

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | The failure this closes is one level up from `OrderTotals`: not "a rule was declared and not enforced" — the companion refuses that — but "the compiler silently failed to obligate the write in the first place", which no amount of discharge rigour detects. The completeness check makes that structurally impossible per compile rather than caught by whoever notices. | N/A | Files that compile today stop compiling — but through the companion's verdicts, not through anything in this document; see § The cost. |
| 2. One file, complete rules | Y | The expected set of checks is computed from the one `.precept` file's own declarations — the mention set and the write-site categories — with no external input (spec § 0.1 #2, "All proof facts … derive from a single file"). | N/A | N/A |
| 3. Deterministic semantics | Y | The expected set is a deterministic function of the file: same file, same set, same verdict; the ordering rule in Decision 3 makes the recorded set byte-identical across runs. | N/A | N/A |
| 4. Full inspectability | Y | The reason a fact was allowed becomes visible: a proof shows which checks earned it, and the coverage record shows the whole set (spec § 0.1 #4, "what the engine could not prove must all be surfaceable"). | N/A | Certificates and the proof output get larger. |
| 5. Keyword-anchored readability | N | N/A — nothing here is authored syntax; no token, keyword, or layout changes. | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | N | N/A — no type or domain-meaning surface is touched. | N/A | N/A |
| 7. Compile-time-first static checking | Y | The completeness check runs at compile time over an enumerable per-file product and rejects rather than guesses (spec § 0.1 #7, "proves what it can, rejects what it can prove invalid, and does not guess"). | N/A | Compile does strictly more work; see § Operational dimensions. |
| 8. Approximation honesty | N | N/A — no approximate/exact boundary is touched. | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | N/A — no constraint surface changes; existing rationale text is carried unchanged in citations. | N/A | N/A |
| 10. Totality | Y | `OrderTotals` is a live counterexample to "a precept that compiles without diagnostics has no unproven arithmetic faults" (spec § 0.1 #10). Restoring the principle needs both halves: the companion's preservation obligation refuses the file, and the completeness check is what guarantees that obligation was minted rather than skipped. Totality over a generator nobody checks is a claim, not a property. | N/A | N/A |
| 11. Static completeness | Y | The bridge from compiler to evaluator (spec § 0.1 #11) can break in two ways: the obligation exists and is undischarged — the companion's `PRE0167`/`PRE0168` — or the obligation was never created. Only the second is this document's, and it is the one no discharge rule can see. | The principle says every fault class "is linked to a compiler diagnostic that prevents it"; § The write-site surface § 8 measures how far that link currently reaches — five of eight writer categories mint nothing at all. | N/A |

**Companion commitments.** *Stateless-first-class*: nothing here needs a state machine — the mention set, the write-site categories, and the coverage record are all defined over fields and operation occasions, and a stateless precept exercises most of the machinery (`Create` and `Fire` occasions, W1, W2, W4, W5; a stateless file has no W3, W6, or W7 instances, and their absence is an empty product, not a special case). The state-dependent pieces — the state-entry establishment site and the residency ensure — simply have no instances in a stateless file. *Domain-expert-primary-author*: after the 2026-07-24 rework this design has **no** author-facing diagnostic. The author-facing message for an unenforced rule is the companion's `PRE0168`; this document's only diagnostic, `PRE0166`, is compiler-internal by Decision 6, and the coverage records are bookkeeping the author reads (through hover and MCP) but never writes.

## Language Design Grounding

*Scope note*: this design introduces no authored syntax — no token, keyword, construct, modifier, type, operator, accessor, or expression form. It does add catalog member names and certificate content, which reach authors through diagnostics, hover, and MCP output, so the grounding is carried anyway, following the precedent set by the certificate-format design for the same reason.

**General language design.** The question this design answers is one every verification system with mutable state has had to answer: when a proof uses a declared property of a value, what has to be true about that property for the proof to count?

The surveyed answer is uniform and is recorded in `research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md`. Event-B is the closest comparator because its proof-obligation generator ships a soundness argument and draws exactly this line. A purely typing invariant produces nothing:

> "At this stage the model results in no proof obligations since the invariant inv1 is nothing stronger than a typing constraint."

An invariant over a mutable variable produces establishment plus one preservation obligation per mutating event:

> "The resulting model now gives rise to 6 proof obligations in total; 3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4."

Dafny gives both poles inside one language — a subset type is checked once at the assignment boundary and then assumed, a `newtype` re-imposes the predicate on every operation — and the survey's reading is that neither is "assume forever with no establishment": both have a real establishment site.

What Precept takes: the establish-plus-preserve-per-mutating-site shape, and the refusal to manufacture obligations for facts nothing can change. One term needs care in the transfer — Event-B's "per mutating **event**" is per-event because an Event-B event *is* the atomic state change. Precept's peer unit is the **governed operation occasion**, not the event handler, because in Precept one occasion can enclose writes from several categories (a row's action chain, the state's exit and entry actions, the `omit` reset, computed recomputation) and commits them atomically. Reading Event-B's "event" as Precept's "handler" is precisely the mistake both designs made before 2026-07-24; § The write-site surface fixes the mapping. What Precept diverges on, and this is the substantive divergence, is **who guarantees the generator was complete**. Rodin's guarantee comes from a once-proved meta-theory about its obligation generator. Precept cannot borrow that: it has no such meta-theory, it has measured instances of its generator being wrong (five of the eight writer categories mint nothing at HEAD, § The write-site surface § 8; and the qualifier modifiers produce no write-site check while `min`/`max` on the same field does); and the matrix's 2026-07-20 constraint requires the expected set be derived without asking the generating code. So Precept adds a per-file cross-check that no surveyed system performs. § Architecture Grounding defends that addition.

`research/language/README.md` has no domain entry for proof-obligation generation or certificate structure — the language-research index covers authored surface. The architecture-side survey above is the on-point in-tree research, and it is cited rather than duplicated.

**Precept-specific application.** Principles 10 and 11 are the ones directly at stake: both assert that a clean compile implies no runtime faults, and `OrderTotals` falsifies both today. Principle 2 constrains where the expected set may come from — the single file, plus the catalog, which `catalog-system.md` establishes as the language specification in machine-readable form rather than an external input. Principle 3 constrains the recorded set to be deterministic. No deliberate exclusion in the spec is touched: the spec has no statement about obligation-set completeness at all, which is the gap the 2026-07-20 note found.

## Audience and Teachability

*Scope note, revised 2026-07-24*: this design has **no** author-facing diagnostic. `PRE0165` was deleted — see § Review record — because its condition and the companion's `PRE0168` are the same condition on the same obligation, and a base witness program must reject "*only* for the target obligation" (`obligation-discharge-matrix-2026-07-19.md:96`). What is author-visible here is the **coverage record**, which reaches the author through hover and `precept_compile` output rather than through a message. `PRE0166` is compiler-internal by Decision 6 and is taught to the maintainer, not the author.

**Worked example — what the author sees, and who says it.** The domain expert writes an ordinary priced-order file and gets rejected:

```precept
precept OrderTotals

field ItemCount as integer default 1
field UnitCost as decimal default 1.0
field Total as decimal default 0.0

rule ItemCount > 0 because "An order with no items has nothing to price"

event Reprice(NewCount as integer, NewCost as decimal)

on Reprice
    -> set ItemCount = Reprice.NewCount
    -> set UnitCost = Reprice.NewCost
    -> set Total = Reprice.NewCost / ItemCount
```

The message they get is the companion's `PRE0168` — "'Reprice' can leave the rule 'ItemCount > 0' false" — and the fix is one word: constrain the argument the rule's field is written from.

```precept
event Reprice(NewCount as integer positive, NewCost as decimal)
```

Nothing in this design writes that message. What this design contributes to that same file is the answer to a question the message does not answer: *how do I know the compiler looked at every place `ItemCount` can be written?* That is the coverage record.

**The author-visible surface: the coverage record.** Rendered on hover over the rule, and carried on the obligation in `precept_compile` output:

```
rule ItemCount > 0
  established at:  construction (Reprice, initial)      ✓ proved
  preserved at:    on Reprice                            ✗ unresolved  ← PRE0168 here
                   Update door: none (ItemCount not editable)
                   entry/exit actions: none
```

Three properties make this serve the domain expert rather than the compiler. It is a **list, not a verdict** — the author reads which places the compiler considered, which is the thing they cannot otherwise see. It uses **their vocabulary** — "on Reprice", "construction", "the Update door" are all things they wrote or chose not to write. And it names the **empty** rows rather than omitting them, because "the compiler checked the update door and there isn't one" and "the compiler forgot about update doors" look identical when the row is dropped, and telling those two apart is the entire point of the design.

**Error message — the one this design does introduce.** `PRE0166` is compiler-internal (Decision 6) and is deliberately not written in the author's vocabulary, because acting on it is not the author's job:

```
PRE0166: internal — the obligation set for this file is incomplete.
         The rule at line 6 ('ItemCount > 0') mentions 'ItemCount', and the
         occasion 'on Reprice' (write-site category W1, action-chain write)
         writes it, but no preservation check was created for that pair.
         This is a compiler defect, not an error in this file. Please report it.
```

**10-minute teaching path** (for the author, on the surface that is actually theirs).

1. `docs/language/precept-language-spec.md` § 2.4 — constraint modifiers on fields and arguments (3 min).
2. `samples/loan-application.precept` — a file where arguments carry constraints and rules rest on them (3 min).
3. Hover a rule in VS Code and read its coverage record — the establish row, the preserve rows, and the empty rows (2 min).
4. `docs/language/precept-language-spec.md` § 0.7 — why a constraint on an incoming value is what makes a rule keep holding (2 min).

## Semantic Rules

Three definitions, one rejection rule, and one serialization contract. Notation is deliberately light; each line is also stated in words.

**What a derivation consumes.** For a derivation `π` that discharges some obligation, `Facts(π)` is the set of facts `π` uses that some operation in the file could establish or break. Facts fixed by a declaration are not members — by the 2026-07-21 refinement they are context, not consumed premises (matrix `:133`).

**The expected set.** For a constraint `C` — a `rule`, any of the four `ensure` forms, or a constraint modifier, which § 2.4 makes a spelling of a rule rather than a separate form — the set of checks that must exist is

```
Expected(C)  =  { Establish(C, e) | e ∈ EstablishmentSites(C) }
             ∪  { Preserve(C, O)  | O ∈ Occasions(file),  Owes(C, O) }
```

in words: one establishment check at each site where the constraint first has to be true, and one preservation check for each **governed operation occasion** whose write plan touches any field the constraint mentions.

Every term is defined in § The write-site surface and nowhere else, which is the point of that section: `Occasions(file)` is § 4 (construction rows, transition and stateless event rows, editable-field doors; no `Restore` occasion), `Owes(C, O) = O.Governed ∧ Targets(O) ∩ mentions(C) ≠ ∅` is § 3, `Targets(O)` unions the targets of every writer category the occasion encloses (§ 2), and `EstablishmentSites(C)` is § 5.

Three consequences of the 2026-07-24 rework are worth stating flat, because the pre-rework text got each of them wrong:

- **The quantifier is the occasion, not the handler.** A state entry or exit action is not its own occasion; it is phase 3 or phase 6 of the enclosing row's write plan, so its writes are in `Targets(O)` for that row (§ 4). The editable-`Update` door *is* its own occasion, so a rule over an editable field now gets an expected preservation check for the edit door — the hole the 2026-07-23 review found.
- **There is no per-write keying, and that is the locked ruling, not a simplification.** The runtime mutates a working copy committed atomically (`precept-language-spec.md:1967`), so mid-plan states are not reachable configurations (matrix `:53`). One check per (constraint, occasion), whatever the plan contains.
- **A field modifier has no separate shape.** The pre-rework text said "the same shape applies with `mentions` replaced by `{F}`" four lines after stating that a desugared cross-field modifier mentions two fields. That clause is deleted. `field Floor as decimal max Ceiling` "desugars to a constraint mentioning **both** fields, so a write to `Ceiling` carries the obligation just as a write to `Floor` does" (matrix `:234`), and `mentions(·)` applies to a modifier unchanged. For a qualifier fact whose value comes from a field, `mentions` is that fact's provenance source fields, which the matrix's transport rule defines as `provdeps` (`:359–363`).

**Completeness — this is the rejection rule.** Let `Minted(C)` be the set of checks the compiler actually created for `C`. The file is complete when

```
∀ C ∈ Constraints(file) .  Minted(C) = Expected(C)
```

Inequality emits `PRE0166` and rejects. Set equality, not containment, in both directions: a missing check is the hole this design exists to close, and a surplus check means the minting rule and the expected-set rule disagree, which is equally a defect — under the exact-power contract a compiler that rejects more than the definition licenses is nonconforming just as one that accepts more is.

**Citation — this is a serialization contract, not a rejection rule.** A derivation `π` records, for each `φ ∈ Facts(π)`, a reference to `Coverage(φ)`: the single per-file record for the constraint or modifier that φ comes from, holding `Expected(·)` and each member's disposition.

```
π well-formed  ⟺  ∀ φ ∈ Facts(π) .  cites(π, φ) = Coverage(φ)
```

*Discharged by construction, and this document does not pretend otherwise.* The 2026-07-23 review established that the obvious stronger reading — "π is admissible only when it cites, and only when every cited check is discharged" — has no rejection power, because both conjuncts are true by construction. An undischarged check already rejects the file under no-deferral (`PRE0167`/`PRE0168` from the companion), so the second conjunct can never be the reason for a rejection. And the record is a function of the fact, computed by the same engine that computes the citation, so there is no independently derived second value for the first conjunct to disagree with. Writing it as an admissibility condition described a state the pipeline cannot reach.

What the citation actually buys is stated positively, and it is not nothing:

1. **Inspectability at the point of consumption** (Principle 4). Without it, "which checks earned the fact this proof used" is answerable only by re-deriving the coverage record from the file. With it, it is a field on the derivation, and it is what the hover surface in § Audience and Teachability renders.
2. **Cross-proof consistency is unrepresentable rather than merely unchecked.** Twelve proofs consuming `ItemCount > 0` reference one record. Under the rejected per-proof-list encoding they would carry twelve lists that must agree, and nothing checks that they do — see Decision 1.
3. **A replay hook for the deferred re-checker.** The re-checker is out of scope, but when it exists it needs the fact-to-coverage edge to exist in the artifact, not to be recomputable in principle.

**Where the rejection power actually lives.** All of it, in two places, neither of which is the citation. `PRE0166` — the completeness check above — rejects a file whose obligation set does not match the expectation. `PRE0167` and `PRE0168` — the companion design's establishment and preservation verdicts — reject a file whose obligations exist but do not discharge. This document is load-bearing for the first and for nothing in the second.

**Soundness preservation.**

*Principle 7 (compile-time-first).* The check adds no guessing: `Expected(C)` is a finite product over declarations in the file, computed by enumeration, and inequality rejects. Nothing is deferred. The one place the product is not a single plan is the construction occasion, where `:2202` forbids assuming entry actions fire; that is handled by a plan set whose obligation is a conjunction over members (§ The write-site surface § 6), which refuses rather than guesses.

*Principle 10 (totality).* This is the principle currently violated, not merely threatened — `OrderTotals` compiles with an unproven division. Restoring it needs both documents. The companion makes the derivation that proves the division rest on a preservation obligation that must itself discharge; this document makes the existence of that obligation a checked property rather than a property of whether the minting walk happened to reach the site. Totality asserted over an unchecked generator is a claim about the compiler's author, not about the compiler.

*Principle 11 (static completeness).* The bridge from compiler to evaluator can break in two ways, and they are split cleanly across the two designs. A fact whose checks exist but do not pass is the companion's; a fact whose checks were never created is this one's, and the completeness check closes it. What remains outside the claim is the correctness of `Expected` itself — it is only as good as the write-site enumeration in § 3A.4, which is asserted rather than build-enforced (`precept-language-spec.md:1992`, `:2204`), with one verified error already recorded as BUG-033. That dependency is named rather than papered over; § The write-site surface § 9 and § Open questions both carry it, and Decision 5's analyzer is the partial closure.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Three pieces, three layers, and the split is the point.

The *establishment and preservation checks* are catalog metadata — two new `ProofRequirement` subtypes, following the rule the proof-engine doc already states: "The proof engine does NOT maintain its own list of what needs to be proved." They belong beside the existing requirement kinds, not in engine code.

*The count, stated exactly, because the pre-rework text said "eleven" and that was wrong in a way worth being precise about.* `ProofRequirementKind` has **thirteen** members at HEAD (`src/Precept/Language/ProofRequirementKind.cs`, verified 2026-07-24): `Numeric`, `Presence`, `Dimension`, `Modifier`, `QualifierCompatibility`, `QualifierChain`, `IntervalContainment`, `LengthContainment`, `CountContainment`, `KeyPresence`, `IndexBounds`, `DimensionalProduct`, `AssignmentQualifier`. The `ProofRequirement` DU carries **twelve** sealed subtypes — `Modifier` has no dedicated subtype, so `docs/compiler/proof-engine.md`'s claim that the DU "has a subtype for every `ProofRequirementKind`" is drift and is listed in § Doc-update enumeration. Neither figure is eleven. Eleven is a different number in this vocabulary and appears twice nearby, which is how the error survived review: eleven is the *certificate premise* kinds (Decision 4, and correct there), and eleven is also the fault-family denominator at matrix rev 8 — which rev 10 dropped to ten when `IntervalContainment` re-homed to a discharge strategy, and which rev 11 disturbs again. Nothing in this design does arithmetic against the fault-family figure; where it names a count it names the thirteen-member enum.

The *expected-set rule* is catalog metadata too, and this is the load-bearing placement. The three legs of the mention set and the eight write-site categories are declared facts about the language, and the expected set is a function of them. Putting the rule in the catalog is what makes the expectation independent of the minting code: the minting path reads the catalog to decide where to create a check, and the completeness path reads the same catalog to decide where one should exist, but they are two derivations from one declaration rather than one derivation checking itself. If instead the expected set were computed by re-running the minting walk, it would agree with itself and see nothing — the circularity the 2026-07-20 note names.

The *completeness check* is a proof-engine output, not a new pipeline stage. It consumes the semantic index and the proof ledger, both of which the proof engine already holds, and produces a verdict. A stage boundary would buy nothing and would need its own artifact type.

There is a fourth piece at a different level entirely, and conflating it with the third is a mistake worth naming — one this document made until 2026-07-24. The per-file check answers *did this file get the checks it should have*. It cannot answer *is a whole category of check wired at all*, and the reason is narrower than the pre-rework text claimed, so it is worth stating exactly.

Two distinct failures hide behind "a category is unwired", and only one of them is the analyzer's.

- **Declared but unwired** — the category is in `WriteSiteCategories`, so `Expected` includes it, but no pipeline code ever creates a check for it. The per-file check *does* see this, on any file that exercises the category: `Expected` has the member, `Minted` does not, `PRE0166` fires. What it does not do is see it *reliably* — it sees it only when someone writes such a file. `samples/` is not a proof that no such file exists, and a category can sit unwired indefinitely while every file anyone happens to compile avoids it. The analyzer converts that from "silent until a user trips it" into "the compiler does not build". § The write-site surface § 8 measures the size of this class today: **five of the eight writer categories mint nothing at all**, and W6 is the extreme case — `precept-language-spec.md:2200` says the `omit` reset "has neither a catalog member nor an implementation".
- **Undeclared** — the category is missing from the catalog too. Neither walk includes it, the comparison passes, and the analyzer cannot see it either, because an analyzer checking "every declared member has a creation site" has nothing to iterate over. **No mechanism in this design catches this.** It is closed only by the catalog being derived from the spec and reviewed against it, and it is the residue Decision 3's tradeoff and § Open questions both carry. Saying the analyzer catches it would be false.

The first belongs in a Roslyn analyzer, following the existing `PRECEPT0027` gate, whose descriptor reads:

> "Every DiagnosticCode member must have at least one emission site in the pipeline (Diagnostics.Create, CIDiagnosticCode assignment, or ProofEngine dispatch), or be listed in the Gate 1 allow-list with a tracking comment."

The same shape applies here: every requirement kind and every write-site category the catalog declares must have at least one live creation site in the pipeline.

**Cross-component propagation.**

- *Runtime (parser, type checker, evaluator, diagnostics)*: the type checker gains no new stamping responsibility in this pass — the two new requirement subtypes are created by the constraint-obligation work that follows. Diagnostics gain **one** code, `PRE0166` (§ Inventory; `PRE0165` was deleted in the 2026-07-24 rework and the author-facing message is the companion's `PRE0168`). The evaluator is untouched. The parser and lexer are untouched.
- *Tooling (syntax highlighting, completions, hover, semantic tokens)*: hover over a constraint gains the coverage record — which operation occasions must preserve it, which of them are empty, and whether each populated one passes. No highlighting, completion, or semantic-token change; nothing authored changes.
- *MCP (vocabulary, DTOs, tool output)*: `precept_compile`'s obligation projection gains the coverage record and the citation field; `precept_proofs` gains the same. Both are additive fields on existing DTOs. The two new requirement-kind names enter the MCP vocabulary through the existing catalog formatter with no formatter change.

**Breaking changes.** Yes, three, all internal to a pre-release project with no external consumers.

1. The certificate format gains a required citation field. Certificates issued before this change do not replay. The matrix's amendment protocol already anticipates this and calls it a major definition version.
2. `ProofRequirementKind` gains two members. The analyzer that enforces one-to-one correspondence between the enum and the DU (`PRECEPT0026`) makes this a compile-time-checked change rather than a silent one.
3. Files that compile today stop compiling. This is the substance of the change, not a side effect; § The cost states it.

### External architectural precedent

The architectural problem is: *how does an obligation-generating verifier guarantee it generated all the obligations?*

Event-B's Rodin answers it with a once-proved meta-theory about the generator, and the model's own closed structure. The number of obligations is a function of the model — one initialisation obligation per invariant, one preservation obligation per (invariant, mutating event) pair — which is the same product shape as `Expected(C)` above. Precept takes the product shape directly.

Precept does **not** take the once-proved-generator guarantee, and the divergence is deliberate. Three reasons. First, Precept has no such meta-theory and writing one is not on any plan. Second, Precept has verified instances of its generator being wrong in exactly this way, and they are not isolated: a field declared `nonzero`, `positive`, or `nonnegative` produces no write-site check while `min`/`max` on the same field does, so the only spelling that discharges a downstream proof is the one nothing enforces — and the per-category measurement in § The write-site surface § 8 shows five of the eight writer categories minting nothing at all, with the rule spelling minting nothing even in the three categories that work. A meta-theory would not have caught any of that; it would have been a meta-theory about a generator with these holes. Third, the matrix's own 2026-07-20 constraint requires the expectation come from somewhere other than the minting code, which is a stronger requirement than Rodin meets.

The honest statement is therefore: **the per-file completeness cross-check is novel relative to the surveyed comparators.** The survey found no system that computes an independent expected-obligation set and compares it against what its generator produced; the surveyed systems all rely on generator correctness. The novelty is warranted because Precept's guarantee is stronger than theirs in the one relevant respect — a Precept file that compiles is claimed to have *no* unproven fault, which makes a single missed obligation a breach of the headline promise rather than an unproved lemma the user can see is unproved.

What makes the novelty affordable rather than reckless is that Precept has an in-tree precedent for the *technique* at a different level. `PRECEPT0027` already computes an expected set from a catalog (every `DiagnosticCode` member) and compares it against what the pipeline actually does (every emission site the scanner finds), with an explicit allow-list for the known gaps. The scanner's own header states the three emission shapes it must recognise, including the catalog-mediated ones — which is the same lesson this design has to absorb: an expected-versus-actual check is only as good as its model of what counts as "actual". That precedent is why the design puts the expected-set rule in the catalog and the category check in an analyzer, rather than inventing both from scratch.

## Inventory of what will be built

**Catalog.**

- `ProofRequirementKind.ConstraintEstablishment` and `ProofRequirementKind.ConstraintPreservation` — two new members (`src/Precept/Language/ProofRequirementKind.cs`).
- `ConstraintEstablishmentProofRequirement` and `ConstraintPreservationProofRequirement` — two new sealed subtypes of the `ProofRequirement` DU (`src/Precept/Language/ProofRequirement.cs`), each carrying the constraint identity, the site, and the mentioned-field set the site was derived from.
- `ProofRequirementMeta` entries for both, each naming its diagnostic code (`src/Precept/Language/ProofRequirements.cs`).
- `WriteSiteCategories` — a new catalog declaring the eight writers § 3A.4 enumerates (W1–W8, § The write-site surface § 2), each with its `Kind`, `Phase`, `Enclosure`, target derivation, `DeclaredAt`, and `BuiltStatus`. `WriteSiteMeta` is a **discriminated union**, not a flat record: the target derivation genuinely differs in shape per category (an action slot, a modifier scan, a state's omit list, "every stored field"). This is the catalog the expected-set walk reads and the analyzer checks against. New file `src/Precept/Language/WriteSiteCategories.cs`; inventory entry in `docs/language/catalog-system.md`.
  - *Reuse, not fork.* W1 and W3 read `ActionMeta`'s existing primary target; W2 reads `ActionSlotRole.IntoTarget` (`src/Precept/Language/Action.cs:132`), which the catalog already declares and the pipeline does not consume; W6 reads `ConstructKind.OmitDeclaration` and the `omit` access modifier (`catalog-system.md:495`); W7 reads `ConstructKind.AccessMode`. The catalog must not re-enumerate the action list, the `into`-bearing action set, or the omit/editable field sets — all four already have owners.
- `OperationSurfaceMeta` — a new 4-member catalog for `Create` / `Fire` / `Update` / `Restore`, carrying `Governed` and whether the operation is an establishment occasion. Required by Decision W-1: preservation duty is an operation attribute, not a writer-category attribute, because W5 fires under both a governed and an ungoverned operation (`precept-language-spec.md:1985`). No such surface exists today — `OperationKind` / `Operations.cs` is the typed-operator catalog and is unrelated; the four operations appear only in runtime code and spec prose (`:1996`). This is the smallest genuinely new catalog the design adds.
- `MentionSetLegs` — the three legs as catalog metadata rather than prose, so the expected-set walk and any future consumer derive from one declaration. Same file or a sibling; decided at build time. **Legs (a) and (b) are scans of a constraint's own expression; leg (c) is the computed-field dataflow and is the only leg that touches the write-site surface — and it touches it as a mapping onto W1/W2 targets, not as a ninth category.** The matrix's `computed-field transitive` axis value (`obligation-discharge-matrix-2026-07-19.md:180`) is leg (c), not a writer; the writer that changes a computed slot is W5. Stating that here is what stops a reader expecting nine categories.

**Supporting types.**

- `CoverageRecord` — per (constraint or modifier, file) the expected set, the minted set, and each member's disposition (`src/Precept/Pipeline/CoverageRecord.cs`). Members are keyed by occasion, and **empty occasion classes are represented, not dropped** — "the update door was considered and there is none" and "update doors were never considered" must not render identically (§ Audience and Teachability).
- `CertificatePremise` gains a required `Coverage` reference on the value-fact-bearing kinds (`DeclaredBound`, `DeclaredQualifier` when field-sourced, `RuleCondition`, `EnsureCondition`, and `DeclaredPresence` on its breakable arm), and the field is absent by construction on the rest (`src/Precept/Language/CertificatePremise.cs`).
- `CertificatePremise.PriorAction` gains a required **write citation** — the (occasion, writer category, action occurrence) triple naming the write that established the fact — which is a different object from a `Coverage` reference and is what the matrix's ruling actually demands (`obligation-discharge-matrix-2026-07-19.md:147`). See Decision 4.

**Pipeline.**

- `ProofEngine.Completeness.cs` — the expected-set walk and the set comparison, producing coverage records and the completeness verdict.
- `ProofLedger` gains the coverage records so downstream consumers read them without recomputation.

**Diagnostics.**

- `PRE0166` — the obligation set for this file is incomplete (compiler-internal; see Decision 6). **This is the only diagnostic this design introduces.**
- ~~`PRE0165`~~ — **deleted 2026-07-24.** Its condition ("a constraint is used as a fact but a write can break it") is the companion design's `PRE0168` condition, reported from the same undischarged preservation obligation on the same file. Base minimality (`obligation-discharge-matrix-2026-07-19.md:96` — a base program "rejects *only* for the target obligation") makes two codes on one obligation a standing violation, so one is a dead duplicate; the one that dies is this document's, because the obligation belongs to the companion. Recorded here rather than silently removed so the code number is not reused for something else.

**Analyzer.**

- `Precept0030ObligationCategoryCoverage` — every requirement kind and every write-site category the catalog declares has at least one live creation site in the pipeline, with an allow-list carrying a tracking comment per known gap, following `PRECEPT0027`. Its initial allow-list is not hypothetical: § The write-site surface § 8 measures **five of eight categories minting nothing** at HEAD, so W2, W5, W6, W7 and the rule-spelling arms of W1/W3/W4 are the day-one entries, each keyed by the category's `BuiltStatus`.

**Tooling.**

- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` — coverage record projection on the obligation DTO.
- Language server hover for a constraint — the coverage record summary.

**Tests.**

- `test/Precept.Tests/ObligationCompletenessTests.cs` — the expected-set walk against hand-computed products; the surplus direction; **one test per writer category W1–W7** asserting the category contributes to `Expected` for a constraint mentioning its target, since the pre-rework quantifier silently omitted four of them.
- `test/Precept.Tests/CoverageCitationTests.cs` — the citation is present and equals the record on every derivation consuming a value-fact, and is absent on every derivation consuming only type-structural context. These are serialization assertions, not admissibility assertions; see Decision 1.
- `test/Precept.Analyzers.Tests/Precept0030Tests.cs` — a requirement kind or write-site category with no creation site is flagged; an allow-listed one is not.
- Corpus run: every file in `samples/` reports a complete obligation set, or is listed with the reason it does not. Note that `omit` declarations appear in **25** files under `samples/` (verified 2026-07-24), so W6 — the category with neither a catalog member nor an implementation — is exercised by the corpus rather than hypothetical, and the corpus run is where its absence will first be measured.

## Decisions

### Decision 1: A proof cites one coverage record per fact, not a list of individual checks — as a serialization choice, with no rejection consequence either way

**Stakes**: medium *(reduced from `high` on 2026-07-24)*

**The stakes correction is part of the decision, not a footnote.** The pre-rework text ranked this `high` because it read the fork as choosing how a proof gets rejected. It does not. Both encodings record the same computed facts, and neither can fail: an undischarged check rejects the file before any citation is consulted, and the citation is computed by the same engine that computes the record it would be compared against. The fork is real, but it lives on three narrower axes — **cross-proof consistency, output size, and replay cost** — and the decision is re-argued on those.

- **Rationale**: on all three axes the record wins, and the first is the only one that is a correctness property rather than an efficiency property.
  1. *Cross-proof consistency becomes unrepresentable rather than unchecked.* The same constraint is consumed by many proofs. Under per-proof lists, two proofs consuming `ItemCount > 0` can carry different preservation lists, and nothing in the pipeline compares them — a new consistency obligation created by the encoding and discharged by nobody. One record makes the disagreeing state not expressible.
  2. *Output size.* A rule mentioning three fields, written across eight occasions, consumed at twelve fault sites carries ninety-six entries under per-proof lists and eight under the record. Certificates are already the largest artifact the compiler emits, and Principle 4's inspectability is served by output a human can read.
  3. *Replay cost for the deferred re-checker.* A re-checker validating per-proof lists re-derives the expected set once per consumption; validating records, once per constraint. The re-checker is out of scope, but the artifact shape is being locked now and it is what the re-checker will have to read.
  It also matches the certificate format Precept already locked, where premises are recorded once and steps refer to them as children rather than restating them.
- **Tradeoff accepted**: the certificate is no longer literally self-contained at the point of consumption — reading why a proof was allowed takes one hop to the coverage record. The mitigation is that the hop is within the same compilation output, not across files or tools, and hover renders the record inline. Accepted more readily after the demotion than before it, because the hop is now a readability cost on an inspectability feature rather than a hop inside a rejection path.
- **Alternatives considered**:
  - *Each proof lists the individual checks it depends on.* Rejected on axes 1–3 above. It reads closer to the 2026-07-21 ruling's wording, and pre-rework this decision claimed the two encodings were equivalent on rejection. The honest statement is stronger and simpler: **neither encoding has any rejection consequence at all**, so the ruling's wording cannot be decisive between them, and the choice falls to the three axes where they genuinely differ.
  - *No citation at all; rely on the completeness check alone.* Rejected — but the pre-rework reason was wrong and is replaced. It said a proof consuming a fact whose constraint "was never even enumerated has nothing to fail against"; false, because such a file fails the completeness check on `Minted(C) ≠ Expected(C)` whether or not any proof cites anything. The real reason is Principle 4: without the citation, "which checks earned this fact" is not a property of the artifact, only a property recomputable from the file, and the hover and MCP surfaces in § Audience and Teachability have nothing to render. This decision now rests on an inspectability argument, which is what it always was.
  - *Citation by constraint name only, with no record object.* Rejected: a name renders but carries no dispositions, so hover would show which constraint a fact came from and not whether its checks passed — the half of the question the author actually asks.
- **Precedent**: the locked certificate format — "Steps form a tree/DAG over earlier steps and premises", with steps citing children drawn from the premise list rather than restating premise content. Externally, DRAT cites clauses of the original formula by index rather than by value, and the analogy is now the right one rather than a decorative one: DRAT's indexing is also a serialization choice about artifact size and re-derivation cost, not a soundness condition of the proof system.
- **Sources consulted for this decision**:
  - `docs/Working/certificate-steps-membership-2026-07-12.md:154-158` — "Step Sⱼ ::= one CertificateStepKind member, citing children ⊆ {P₁…Pₙ} ∪ {S₁…Sⱼ₋₁}, carrying its own Conclusion"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:122` — "a proof may not consume such a fact unless it **names the obligation that established the fact and every obligation that preserves it**, each itself discharged" *(rev 11 line; was cited as `:118` against rev 10)*
  - `docs/Working/certificate-steps-membership-2026-07-12.md:354` — "DRAT's premise base being the original formula's clauses cited by index (mirror, cited above)"
- **Strongest counter-evidence**: not the ruling's wording — the 2026-07-24 demotion neutralises that objection, since a duty with no rejection consequence cannot be violated by either serialization. The live objection is sharper and points at the ruling itself: **if the citation cannot reject, the 2026-07-21 ruling did not get what it asked for.** Its stated purpose was to convert a whole-file property into something "a single certificate exhibits", and a serialization annotation does not do that on its own. Response, in two parts. First, this is conceded and recorded — § The cost already carries the correction to the ruling's rationale, and the 2026-07-24 rework extends it: the duty relocates where the enumeration is *consumed*, and the enumeration itself is what rejects. Second, the ruling is not thereby empty: it is what forces the coverage record to exist as an artifact, and the completeness check attaches to that record. Ruling and check together do what the ruling alone was described as doing. Whether the owner wants the ruling's rationale sentence amended, or wants a stronger admissibility condition designed that *can* fail, is an owner question this document does not settle — see § Open questions.
- **Reversibility**: `Easy`. Pre-release with no external certificate consumers; switching to per-proof lists is a change to how the same computed set is serialized — and, after the demotion, provably a change with no effect on which files compile.
- **Blast radius**: catalogs — none directly (a supporting type). Docs — `docs/compiler/proof-engine.md`, `docs/Working/certificate-steps-membership-2026-07-12.md`, `docs/tooling/mcp.md`. Samples — none. External consumers — none (pre-release).

### Decision 2: Establishment and preservation are two catalog kinds, not one parameterized kind

**Stakes**: high

- **Rationale**: they differ in every respect a requirement subtype exists to capture. Their site sets are disjoint and computed differently — establishment sites are construction and state entries (§ The write-site surface § 5), preservation sites are **governed operation occasions** whose write plan touches the mention set (§ 4). Their proof conditions have different shapes — establishment asks whether a configuration satisfies the constraint, preservation asks whether the weakest precondition of a write plan holds. Their diagnostics say different things to the author. CLAUDE.md's catalog rule is explicit that varying shapes get a DU base plus sealed subtypes rather than a flat record with nullable fields, and a single kind carrying "site set, which is one of two kinds" with half its fields unused per instance is exactly the shape that rule forbids. *(2026-07-24: the site-set half of this rationale was stated against the handler quantifier; the occasion quantifier makes it stronger, not weaker — establishment sites and preservation occasions remain disjoint, and the preservation side now genuinely varies in shape across construction rows, transition rows, and editable doors.)*
- **Tradeoff accepted**: two catalog members instead of one, and two diagnostic codes instead of one, for what an author may perceive as one concern ("my rule isn't enforced"). Accepted because the two failures have genuinely different repairs — an establishment failure is fixed at the default or the entering transition, a preservation failure at the writing occasion — and a single message would have to name both. *(Note that both codes are the companion design's `PRE0167` and `PRE0168`; after the 2026-07-24 rework this document introduces no author-facing code of its own, so the tradeoff is inherited rather than incurred here.)*
- **Alternatives considered**:
  - *One `ConstraintObligation` kind with a site-kind discriminator field.* Rejected on the CLAUDE.md DU rule and on the practical consequence: every consumer would switch on the discriminator to decide which fields are meaningful, which is the enum-identity dispatch the catalog rules name as a violation.
  - *Fold both into the existing `IntervalContainment` kind for the modifier case and add only a rule kind.* Rejected: it would make the modifier and rule spellings structurally different in the catalog, which is the asymmetry this whole line of work exists to remove — the spec's position is that a modifier is sugar for a rule.
- **Precedent**: Event-B generates two distinct obligation kinds for exactly this split, an initialisation obligation and a per-event preservation obligation, rather than one parameterized kind — "3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4." In-tree, the `ProofRequirement` DU already separates `LengthContainment`, `CountContainment` and `IntervalContainment`, which are three shapes of the same idea distinguished because their payloads differ.
- **Sources consulted for this decision**:
  - `CLAUDE.md` § Catalog System — "**Use discriminated unions for varying shapes.** Don't paper over shape differences with nullable fields on a flat record — use a DU base + sealed subtypes."
  - `research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md:93` — the six-obligation split quoted above
  - `src/Precept/Language/ProofRequirement.cs:11-25` — `ProofSubject` as a closed supporting DU; `ProofRequirement.cs` DU base at `:535` per `docs/compiler/proof-engine.md:535`
  - `docs/compiler/proof-engine.md:531` — the current DU membership, which has "no constraint-establishment or constraint-preservation subtype"
- **Strongest counter-evidence**: the matrix speaks of "the constraint families" and "the constraint-obligation machinery" in the singular throughout, which could be read as one mechanism. Response: it also names them as two families explicitly — "constraint establishment (want :18), constraint preservation (want :19)" — and the singular usage is about the build, not the catalog shape.
- **Reversibility**: `Hard` post-ship, `Easy` now. The member names reach MCP vocabulary and diagnostic codes; merging them later would rename public surface. Pre-release means no external consumer holds them yet.
- **Blast radius**: catalogs — `ProofRequirementKind`, `ProofRequirement`, `ProofRequirements` meta. Docs — `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU, `docs/language/catalog-system.md`, `docs/compiler/diagnostic-system.md`. Samples — none. External consumers — none.

### Decision 3: The expected set is computed from catalog-declared write-site categories and mention-set legs, never by re-running the minting walk

**Stakes**: high

- **Rationale**: this is the whole content of the independence requirement, and the matrix states it as a constraint on the design rather than a preference — "A completeness check that computes 'sites that should have minted' from the same code that decides where to mint is circular — it agrees with itself and sees nothing … The expectation has to come from the catalog and the spec, where write-site categories and the mention set are declared" (`obligation-discharge-matrix-2026-07-19.md:117`). Compliance with a locked constraint is the first leg and does not need a witness. The second leg does: the catalog is the right source *among* the independent sources, because it is the language specification in machine-readable form rather than an implementation. Concretely — the write-site categories are declared once, the minting path reads them to decide where to create a check, and the completeness path reads them to decide where one must exist. Two consumers, one declaration.

  **A witness this rationale used to carry, and no longer does.** The pre-rework text offered the qualifier-modifier hole as an instance the independent expectation would catch and a self-referential one would not. That is wrong, and the 2026-07-23 review was right to say so: `nonzero` on a field is a constraint like any other, so `Expected` includes its write-site checks whether or not the minting code creates any, and a *self-referential* expectation is exactly what fails to see it. The instance is a witness for the value of independence in general — it is not a witness for anything specific to the catalog as the source, which is what it was being used to prove. The measured evidence that does the work is now in § The write-site surface § 8: five of eight writer categories mint nothing, so the gap between "what the language declares" and "what the pipeline does" is the normal case rather than an anomaly, and any expectation derived from the pipeline inherits all five gaps silently.
- **Tradeoff accepted**: the check is only as good as the catalog. If a write-site category is missing from `WriteSiteCategories`, both walks are blind to it in the same way, and the check reports completeness on an incomplete model. That is a real residue, it is **not** closed by Decision 5's analyzer — an analyzer that iterates declared members cannot see an undeclared one, and § Architecture Grounding now says so instead of implying otherwise — and it is not closable from inside this design at all. It is why the § 3A.4 writer list being asserted rather than build-enforced stays an open dependency (`precept-language-spec.md:2204`). What the design does buy is a change of failure mode: from "a category is silently unwired inside procedural code" to "a category is absent from a declared, reviewable list that is cross-checked against the spec by a human", which is worse than a mechanical guarantee and better than nothing. Naming that gap precisely is the point; § The write-site surface § 9 item 1 carries it.
- **Alternatives considered**:
  - *Compute the expectation by walking the minting code and collecting its decision points.* Rejected as circular, in the specific sense the 2026-07-20 note names.
  - *Compute it from a hand-maintained list independent of the catalog.* Rejected: a second hand-maintained list drifts, and Precept's whole architecture exists to avoid parallel copies of what a catalog already knows.
  - *Compute it from the spec text directly at build time.* Rejected as not mechanizable — § 3A.4's writer table is prose, and the catalog is the machine-readable form the project already commits to for exactly this purpose.
- **Precedent**: `PRECEPT0027` does precisely this at the diagnostic level — the expected set is the `DiagnosticCode` enum (the catalog), the actual set is what a semantic scan of the pipeline finds, and the two are compared at build time with an explicit allow-list for gaps. Externally, the closest analogue is a coverage-driven rather than proof-driven guarantee, and the survey found none; see § Architecture Grounding on the novelty.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:117` — "A completeness check that computes 'sites that should have minted' from the same code that decides where to mint is circular — it agrees with itself and sees nothing" *(rev 11 line; was cited as `:113` against rev 10)*
  - `src/Precept.Analyzers/DiagnosticCoverageScanner.cs:8-27` — "Computes three sets from a compilation: All DiagnosticCode enum members (the catalog), Emitted codes (real emission contexts), Test-referenced codes"
  - `docs/language/precept-language-spec.md:1979-1988` — the eight-writer table; `:1990` — "Any analysis that derives write sites from an action's primary field target alone will miss it, and a fact about `D` will appear to survive"
  - `docs/language/precept-language-spec.md:1992` — "the other five writers are not catalogued at all … The intended end state is that every writer is catalog-declared", which makes `WriteSiteCategories` the spec's own intended end state rather than this design's invention
  - `CLAUDE.md` § Catalog System — "**Catalog before code.** A new keyword, type, operator, modifier, or construct goes in the appropriate catalog entry first."
- **Strongest counter-evidence — and it is a live instance, not a hypothetical.** The catalog and the minting code are written by the same author, so "independence" is procedural rather than logical: a mistaken mental model produces a matching mistake in both. **BUG-033 is that instance, and it belongs here rather than in the rationale.** The pre-rework text offered BUG-033 as motivation *for* the independently derived expected set; the 2026-07-23 review is right that it is the counterexample. Two structurally independent consumers, in different pipeline components, both failed to treat the `into` binding as a write site — and § The write-site surface § 8 confirms it is worse than a single miss: the catalog *already declares* `ActionSlotRole.IntoTarget` (`src/Precept/Language/Action.cs:132`), so the declaration was present, reviewable, and read by neither consumer correctly. Falsifier 4's condition ("the independence is procedural fiction") was met before this document was written.

  Response, and it concedes most of the objection. The claim this decision can defend is **not** "two derivations catch each other's mistakes" — BUG-033 refutes that. It is narrower and survives BUG-033 intact: a *declaration* omission is reviewable, greppable, and analyzer-checkable, while a *procedural* omission spread across the minting walk is none of those. The catalog does not make the author right; it makes the author's model visible enough to be argued with. That is a real property and it is all that is claimed. Two things follow honestly. First, the residue is unclosable from inside this design and is recorded in § Open questions rather than mitigated. Second, `Precept0030` is doing more work than "belt and braces" — it is the only mechanism here that does not depend on the author having been right twice, because it compares the declaration against the pipeline mechanically. If the owner wants genuine independence, the only shapes that supply it are spec-derived generation of the catalog or a second implementer, both named and neither taken.
- **Reversibility**: `Easy`. The expected-set walk is one file; changing where it sources from does not touch the catalog shape.
- **Blast radius**: catalogs — new `WriteSiteCategories`, `OperationSurfaceMeta`, and `MentionSetLegs`. Docs — `docs/language/catalog-system.md` (inventory), `docs/compiler/proof-engine.md`, `docs/language/precept-language-spec.md` § 3A.4 (the writer table gains a pointer to its catalog form) and § Open questions `:2200`/`:2204` (which name the catalog gap this closes). Samples — none. External consumers — none.

### Decision 4: The citation duty partitions the eleven certificate premise kinds into four groups, and only one of them carries a coverage record

**Stakes**: medium

*Reworked 2026-07-24. The pre-rework version had three groups and put `PriorAction` in the wrong one; the fourth group is the repair, not an elaboration.* The count **eleven** here is the *certificate premise* vocabulary (`certificate-steps-membership-2026-07-12.md` Decision 1) and is correct — it is not the thirteen-member `ProofRequirementKind` enum, which § Architecture Grounding now states separately. Two different elevens and a thirteen sit within a few paragraphs of each other in this design space, and conflating them is what produced the pre-rework error.

The eleven premise kinds fall out against the behavioural test:

| Group | Premise kinds | What a proof must show | Artifact |
|---|---|---|---|
| 1. Fixed by declaration | `DeclaredModifier`; `DeclaredQualifier` where `provdeps = ∅`; `DeclaredPresence` where the field is required **and no state omits it** | Cite the declaration. Empty establishment and preservation sets, per the 2026-07-21 refinement (matrix `:133`). | none |
| 2. A condition on this route | `GuardCondition`, `RejectRowGuard` | Nothing to establish or preserve — these are facts about the route, and their survival to the consumption point is the transport rule's frame-and-kill (matrix `:355`). | none |
| 3. Established by a write in this operation | `PriorAction` | Cite the **write** that established it. Nothing preserves it — the transport rule's kill leg governs its survival — but it does have an establishing event, and that event is namable. | write citation |
| 4. Breakable by an operation | `DeclaredBound`, `RuleCondition`, `EnsureCondition`, `DeclaredQualifier` where `provdeps ≠ ∅`, and `DeclaredPresence` on its breakable arm (§ below) | Cite the coverage record. | `Coverage` |

`ReachabilityFact` sits outside all four: it is derived from the state graph rather than from a value, and no operation writes a state graph.

**Why `PriorAction` needs its own group — the correction.** The pre-rework text put it in group 2 with the route conditions, on the reasoning that nothing preserves it. That half is right; the other half contradicted a locked ruling the decision did not quote. The matrix's 2026-07-21 carried-forward ruling is explicit that the fact "is not declared by the author anywhere — it is established by the write itself" (`:140`), and states the consequence in one line: "It is also subject to the citation duty above: **a proof consuming a written value names the write it came from**" (`:147`). A group whose entry reads "no coverage record" and stops therefore under-states the duty for `PriorAction`: the ruling asks for a citation, just not for a coverage record.

The two artifacts are genuinely different objects and collapsing them would be wrong in both directions. A `Coverage` reference points at a per-file record holding `Expected(C)` and its dispositions — a *set* of checks over the whole file. A write citation points at one (occasion, writer category, action occurrence) triple — a *single event* inside one operation, with no set and no dispositions, because there is nothing to enumerate: the fact is established once, at that write, and killed rather than preserved. Giving `PriorAction` a coverage record would manufacture an `Expected` set for a fact that has no establishment *sites* in the plural and no preservation obligations at all, which is precisely the vacuous-obligation over-generation the survey records every system avoiding.

**Why `DeclaredDefault` is no longer exempt.** The pre-rework text placed it outside the partition because it "*is* an establishment fact rather than a fact needing one". The review is right that this is backwards: under the 2026-07-21 refinement, being established by an operation is the *definition* of a value-fact, not an exemption from the duty. A default is materialized by W4 at construction (`precept-language-spec.md:1984`) and can be overwritten by any later governed occasion, so a proof consuming "`Total` defaults to 0" as a fact about the current configuration is consuming something an operation can break. `DeclaredDefault` therefore lands in **group 4** and carries a coverage record like any other breakable fact. Where a proof consumes the default as a fact about the *construction configuration specifically* — which is what `ScanRulesAgainstDefaults` does today — the establishment leg discharges it at the construction occasion and the preservation legs are the ordinary ones; nothing special is needed, which is the point.

**Why `DeclaredPresence` splits, and where the split now reaches.** The certificate format defines it as quoting whichever declaration aspect guarantees the field always has a value — "default or required shape" — and those aspects answer the behavioural test differently. Two writers bear on it, not one:

- *`clear`.* The catalog gates it: `ClearApplicable` lists the seven collection types plus `new ModifiedTypeTarget(null, [ModifierKind.Optional])`, so on the scalar side `clear` reaches a field only when it carries `optional`. A required field's presence is unbreakable by `clear`; an `optional` field's is breakable the moment the file contains a `clear` on it.
- *The `omit` reset (W6).* The pre-rework text enumerated `clear` and stopped, which is the review's finding. `omit` is a writer in its own right (`precept-language-spec.md:1986`), it fires at phase 4 on entering a state that omits the field, and it is **not** gated on `optional` — a required field omitted by some state is reached by it.

The `omit` leg cannot be settled here, because canon is conflicted about what the reset leaves in the slot: "canon asserts both reset-to-default and structurally-absent semantics" (matrix `:294`, `:459`, ⧖ Q13). **The position taken is the conservative one**: a field that any state omits has its `DeclaredPresence` classified into group 4 and carries a coverage record, and the presence obligation at the W6-bearing occasion discharges as `Unresolved` — the file is refused — until Q13 is ruled. This is sound under either eventual reading. If the reset restores the default, the refusal was a power-narrowing that a later ruling widens (a *minor* definition version, monotone, matrix `:490`); if the reset makes the field structurally absent, the refusal was correct and the alternative would have been an unsound `Proved`. The asymmetry is decisive: one reading costs precision on files nobody has written yet, the other costs a soundness hole in a design whose § The cost calls itself a soundness correction.

- **Rationale**: the four situations the owner scoped are not four mechanisms — they are premise kinds landing in the same group. Field modifiers are `DeclaredBound`; rules and ensures are `RuleCondition` and `EnsureCondition`; field-sourced qualifiers are the value-fact arm of `DeclaredQualifier`; a value written earlier in the operation is `PriorAction`. Three of the four share one linkage mechanism (the coverage record) and the fourth has its own (the write citation), which is what the matrix's two rulings — the citation duty at `:122` and the carried-forward ruling at `:147` — jointly require. The partition is derived rather than invented: it is the behavioural test applied to a vocabulary that already exists and was already closed.
- **Tradeoff accepted**: two citation artifacts instead of one, and two of the eleven kinds (`DeclaredQualifier`, `DeclaredPresence`) split by a property of the instance rather than by kind, so a consumer cannot tell from the premise kind alone which artifact is required. Accepted on both counts. On the artifacts: collapsing them would either manufacture a vacuous `Expected` set for `PriorAction` or drop the write citation the matrix requires, and neither is available. On the instance split: the alternative — splitting the premise kinds — would break the certificate format's one-kind-per-author-construct rule, since the author writes the same thing in both cases. The discriminators are already defined elsewhere (`provdeps` empty or not, matrix `:359–363`; and for presence, whether any state omits the field or `clear` can reach it), so no new discriminator is invented here.
- **Precedent**: the survey's cross-system finding that every system maintains exactly two populations and never conflates them — "every surveyed system maintains the two populations precisely because conflating them either over-generates vacuous obligations or under-checks mutable invariants." The four-way split here is that two-way split plus route conditions, which are not facts about values at all, plus in-operation writes, which are facts about values with an establishing event and no preservation. In-tree, the matrix itself already keeps establishment-by-write separate from establishment-by-obligation: the carried-forward ruling calls it "a licensed provenance in its own right, distinct from the four premise classes" (`:140`).
- **Sources consulted for this decision**:
  - `docs/Working/certificate-steps-membership-2026-07-12.md:337-347` — the eleven premise kinds; `:337` "**DeclaredBound** — a numeric/length/count bound modifier on a field or arg"; `:345` "**PriorAction** — an earlier action in the same transition chain whose catalog-declared effect is used"; `:347` "**DeclaredPresence** — quotes the declaration aspect that guarantees the field always has a value (default or required shape)"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:133` — "The duty applies to a consumed fact **iff** some operation can establish or break it — a value-fact." *(rev 11 line; was cited as `:127` against rev 10)*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:140`, `:147` — the carried-forward ruling and "a proof consuming a written value names the write it came from"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:294`, `:459` — the `omit` canon conflict, ⧖ Q13
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:490` — power-widening as the monotone, minor-version amendment class, which is what a later Q13 ruling would be
  - `research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md:151` — the alternatives-rejected passage quoted above
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:359-363` — `provdeps(φ) = ∅` as the type-structural discriminator
  - `src/Precept/Language/Actions.cs:50-60` — `ClearApplicable` = the seven collection types plus `new ModifiedTypeTarget(null, [ModifierKind.Optional])`
  - `src/Precept/Language/ActionKind.cs:7-35` — the fifteen action kinds, `Clear = 8` under the comment "Universal (collections + optional scalars)"
  - `docs/language/precept-language-spec.md:1984`, `:1986` — W4 default materialization and the W6 `omit` reset as writers
- **Strongest counter-evidence**: the conservative `omit` position refuses files that a plausible reading of canon makes safe, and it does so on a field kind — required, present, never `clear`ed — whose author has every reason to believe presence is guaranteed. If the reset turns out to restore the default, this design will have rejected working precepts for however long Q13 stays open, and `omit` appears in 25 sample files. Response: the cost is real and is not hidden, but it is bounded to constraints whose *proof* needs W6's post-state value, not to every file using `omit` — a file that omits a field no constraint mentions is untouched. And the alternative is not "assume the safe reading"; it is "assume a reading canon does not license", which is what the design exists to stop. § The write-site surface § 9 item 3 records this as an owner-facing dependency, and the corpus run in § Inventory is where its true size gets measured rather than estimated.
- **Reversibility**: `Easy` for the `omit` position — a Q13 ruling in the reset-to-default direction is a one-line reclassification and a power-widening. `Hard` for the group-3 write citation once certificates ship, since it is a required field on a premise kind.
- **Blast radius**: catalogs — none (the partition is over an existing vocabulary). Docs — `docs/Working/certificate-steps-membership-2026-07-12.md` (the premise partition and the two added reference fields), `docs/compiler/proof-engine.md`, `docs/tooling/mcp.md`. Samples — none directly; 25 files carry `omit` and are the corpus-run subjects. External consumers — none (pre-release).
### Decision 5: Completeness is checked at two levels — per file at compile time, and per category at build time

**Stakes**: high

- **Rationale**: the two questions are different and neither check answers the other. *Did this file get every check it should have* is a per-file question, enumerable as a finite product over the file's own declarations, and belongs at compile time so every compile answers it. *Is this category of check wired into the compiler at all* is a question about the compiler's own source. The per-file check can answer it only **incidentally and only on a file that exercises the category**, and the difference between "answers it" and "answers it when someone happens to write the triggering file" is the whole content of this decision.

  **The witness, re-grounded 2026-07-24.** The pre-rework text offered the qualifier-modifier hole here, claiming the per-file check "would have reported completeness on a file whose `nonzero` divisor was never obligated". That is false, and the 2026-07-23 review said so: `nonzero` is a constraint, `Expected` includes its write-site checks whichever way it is spelled, `Minted` does not, and `PRE0166` fires on the very first such file. The evidence that actually supports two levels is the measurement in § The write-site surface § 8: at HEAD, **five of the eight writer categories mint nothing at all**, and W6 — the `omit` reset — "has neither a catalog member nor an implementation" (`precept-language-spec.md:2200`). Each of those five is a category that will sit unwired until a file exercising it is compiled and someone reads the resulting `PRE0166`. That is a discovery mechanism gated on user behaviour. The analyzer converts it into a build failure the day the category is declared and the creation site is missing — no file required, no user required, no waiting.

  The honest boundary, which § Architecture Grounding now also states: this covers **declared-but-unwired**. A category missing from the catalog entirely is invisible to the analyzer too, because there is nothing to iterate. Neither level closes that, and no level added later would; it is closed by the catalog being derived from the spec and reviewed, and it is Decision 3's recorded residue.
- **Tradeoff accepted**: two mechanisms to maintain, in two languages, with two failure surfaces. Accepted because collapsing them means giving up one of the two questions, and both are load-bearing today — five unwired categories for the analyzer level, and a per-file product that has to be right on every compile for the compile-time level.
- **Alternatives considered**:
  - *Per-file check only.* Rejected on the discovery-gating argument above. The corpus is not a substitute: `samples/` is a set of files someone wrote to demonstrate the language, not a covering set over the write-site product, and treating a clean corpus run as evidence that every category is wired is exactly the "tests check the cases someone thought of" failure the matrix names.
  - *Build-time analyzer only.* Rejected: an analyzer can confirm a category has at least one creation site; it cannot confirm that a particular file's twelfth occasion got its check. The per-file product is where an instance goes missing.
  - *Tests only, for both.* Rejected on the 2026-07-20 constraint — "Tests check the cases someone thought of; the property here is enumerable per file … so it can be checked on every compile rather than sampled."
- **Precedent**: `PRECEPT0027` is the in-tree instance of the build-time half, with its allow-list discipline — and the allow-list matters more here than there, because five day-one entries is a large allow-list and a large allow-list quietly becomes permanent. The per-file half has no external precedent; see § Architecture Grounding.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:118` — "Tests check the cases someone thought of; the property here is enumerable per file …" *(rev 11 line; was cited as `:114` against rev 10)*
  - `src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs:21-25` — "Every DiagnosticCode member must have at least one emission site in the pipeline … or be listed in the Gate 1 allow-list with a tracking comment."
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:120` — "Prior art in this codebase points at build-time checking (catalog-declared positions plus a layered checker) rather than a pipeline stage" *(rev 11 line; was cited as `:116` against rev 10)*
  - `docs/language/precept-language-spec.md:2200` — the `omit` reset "has neither a catalog member nor an implementation"
  - § The write-site surface § 8 — the per-category built-status measurement, harness-verified at HEAD 2026-07-24
- **Strongest counter-evidence**: with the qualifier-modifier witness withdrawn, there is no *verified instance* of a defect the analyzer catches and the per-file check does not — only the argument that the per-file check's coverage is gated on which files get compiled. A reviewer can fairly say the second level is being justified by a hypothetical. Response: partly conceded, and the concession is recorded in falsifier 3, which is written to detect exactly this. What resists the objection is that the gating is not hypothetical — five categories are unwired *now*, and W6 in particular has no implementation at all, so on the day `WriteSiteCategories` lands the analyzer fails and the per-file check does not fail until a constraint mentions an omitted field. That is a difference in *when*, which is the claim, rather than a difference in *whether*, which is not.
- **Reversibility**: `Easy` for the analyzer half (add or remove a build-time gate), `Hard` for the per-file half once diagnostics ship against it.
- **Blast radius**: catalogs — none beyond Decision 3's. Docs — `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`, `docs/contributing/`. Samples — every sample must pass the per-file check or be listed. External consumers — none.

### Decision 6: A completeness failure fails the compile as a compiler-internal error, distinct from the author-facing message

**Stakes**: medium

*The author-facing message this is "distinct from" is the companion design's `PRE0168`, not `PRE0165`. `PRE0165` was deleted on 2026-07-24 as a dead duplicate of `PRE0168` (§ Inventory), which changes nothing in this decision's substance — the two audiences and the two repairs are the same two — but it does mean the distinction is now **across** the two designs rather than inside this one, and base minimality (matrix `:96`) is what forces the deduplication.*

- **Rationale**: the two failures have different audiences and different repairs. "A rule is used as a fact that a write can break" is the author's problem, it is reported by the companion's `PRE0168`, and § Audience and Teachability shows it written in their vocabulary. An incomplete obligation set is not the author's problem: they wrote a legal file and the compiler failed to enumerate its own work. That must not be reported as though they made a mistake, and it must not be silently tolerated either, because tolerating it is the hole. So it fails the compile as `PRE0166` with a message that names the missing triple — constraint, occasion, writer category — and says plainly that this is a compiler defect to report.
- **Tradeoff accepted**: an author can hit a message they cannot act on. Accepted because the alternative is worse in both directions — a warning would let the file ship with the guarantee broken, and dressing it as an author error would send them looking for a mistake they did not make.
- **Alternatives considered**:
  - *Report it as an author diagnostic.* Rejected: it is not an author error and no author edit reliably fixes it.
  - *Warning severity.* Rejected: a file that compiles with an incomplete obligation set is exactly the "compiles but the guarantee has an asterisk on it" two-tier trustworthiness the want doc forbids, and which the dead-rows ruling already refused for the same reason.
  - *Silent internal assertion in debug builds only.* Rejected: the failure is silent in production builds, which is the current situation.
- **Precedent**: the 2026-07-20 dead-rows ruling took the same position on a structurally analogous question — a definition that misrepresents itself is an error, not a warning, because warning severity creates the two-tier guarantee. Externally, Rodin surfaces an un-discharged obligation as a model-level failure rather than a tool warning.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:217` — "Warning severity would also create exactly the two-tier trustworthiness the want doc forbids — a file that compiles but whose guarantee has an asterisk on it." *(rev 11 line; was cited as `:211` against rev 10)*
  - `docs/compiler/proof-engine.md` § Obligation Generation Contract — "A missing obligation means a constraint that an author declared is silently not enforced — exactly the prevention-vs-detection failure mode Precept exists to prevent in user code."
- **Reversibility**: `Hard` once the code ships — severity changes on a shipped diagnostic are observable behaviour. `Easy` now (pre-release).
- **Blast radius**: catalogs — `DiagnosticCode` gains `PRE0166` and loses the reserved `PRE0165`. Docs — `docs/compiler/diagnostic-system.md`. Samples — none. External consumers — none.


## The cost

This design shrinks the set of files that compile — but almost none of that shrinkage is *this* document's. The files that stop compiling stop because a constraint's establishment or preservation obligation is undischarged, and those obligations and their verdicts belong to the companion design. What this document adds to the rejected set is narrower and stranger: files whose obligation set does not match the expectation, which is a compiler defect surfacing as a rejection rather than an author error. On current evidence that set is not small — § The write-site surface § 8 measures five of eight writer categories minting nothing, so until those are wired, `PRE0166` is the expected outcome on any file exercising them, and the corpus run in § Inventory is where the true size gets measured rather than estimated.

Under the matrix's amendment protocol this is a **soundness correction**, not a widening: a major definition version, certificate replay breaks for affected cells, and it is routed to the owner with the witness program. Both requirements are met — `OrderTotals` in § The problem is the witness, compiled at HEAD on 2026-07-23 with zero diagnostics and a `Proved` divisor.

One correction is owed to the matrix itself, and the 2026-07-23 review pushed it one step further than the pre-rework text took it. The 2026-07-21 ruling's rationale says the citation duty "converts that whole-file property into something a single certificate exhibits, so the gap is visible at the point of consumption rather than inferable only by auditing the entire file." The pre-rework correction said that was *half* right — a citation makes the link local, but "every check that preserves it" is still a whole-file claim, so a checker trusting a citation must verify the cited set is complete, which needs the independently derived expectation. That correction stands and the review confirmed it. What the review added is the last step: **the citation does not merely need the enumeration alongside it — the citation contributes no rejection power at all**, because both of its conjuncts hold by construction (§ Semantic Rules). The ruling stands, and it is not thereby empty: it is what forces the coverage record to exist as an artifact, and the completeness check attaches to that record. But its rationale sentence describes an effect the duty alone does not have, and the correction offered at promotion should say so plainly rather than softening it to "overstates".

## Acceptance criteria

*Rewritten 2026-07-24. Criteria 1 and 7 tested things this design does not do; criteria 4, 5 and 12 were written against the handler quantifier.*

1. `OrderTotals` (§ The problem) fails to compile. The diagnostic is the companion's `PRE0168`, not a code from this design — asserted explicitly, so that a future reintroduction of a duplicate author-facing code here fails a test rather than passing review.
2. `OrderTotals` with `NewCount as integer positive` compiles clean, and the divisor derivation's certificate carries a citation to the coverage record for `ItemCount > 0`, whose members are all discharged.
3. `BoundVsRule` (§ The problem) rejects on **both** fields, not just `Total` — the rule-spelled ceiling produces the same rejection as the modifier-spelled one.
4. For a file with one rule mentioning two fields and three **occasions** writing them, the expected set computed by the walk equals the hand-computed product, verified by a test asserting the exact member list. At least one of the three occasions is not an event handler.
5. **One test per writer category W1–W7**: a constraint mentioning the category's target field yields an `Expected` member at the occasion enclosing that category. W7 (the editable-`Update` door) and W3 (state entry/exit actions) are the two the pre-rework quantifier silently omitted and are the load-bearing rows.
6. **`Restore` produces no `Expected` member for any constraint**, on a file with fields, rules, and a stored computed field — the direct test of Decision W-1. A regression here fires `PRE0166` on every file in the corpus, so this criterion is also the corpus's canary.
7. A test that deliberately suppresses minting for one write site causes the compile to fail with `PRE0166` naming that constraint, occasion, and writer category — the check catches a missing check.
8. A test that deliberately mints one extra check causes the same failure in the surplus direction.
9. **Serialization, not admissibility**: every derivation consuming a value-fact carries a citation equal to that fact's coverage record, and every derivation consuming only type-structural context carries none. Asserted as a property of the emitted artifact. *(This replaces the pre-rework criterion 7, which asserted that a derivation citing an undischarged record "is not admitted" — a state the pipeline cannot reach, since the undischarged member already rejects the file.)*
10. `Precept0030ObligationCategoryCoverage` fails the build when a requirement kind or write-site category has no creation site, and passes when it is allow-listed with a tracking comment. The initial allow-list has five write-site entries (§ Inventory) and each carries a tracking comment.
11. Every file in `samples/` reports a complete obligation set, or appears in a committed list naming the reason it does not — no silent exclusions. The 25 files carrying `omit` are called out separately, since W6 is the category with no implementation.
12. Coverage records are byte-identical across two runs on the same file (determinism, Principle 3).
13. `precept_compile` output carries the coverage record for every constraint — including its **empty** occasion classes — and `precept_proofs` carries the citation on every derivation that consumes a breakable fact.
14. The four situations each have at least one end-to-end test, with the fourth testing the right artifact: a field modifier and a rule (coverage record), a field-sourced qualifier (coverage record, `provdeps ≠ ∅` arm), and a value written earlier in the same operation — which asserts a **write citation** naming the establishing write, per Decision 4 group 3, *not* a coverage record.
15. A constraint mentioning a field that some state omits produces a coverage record whose W6-bearing member is `Unresolved`, and the file is rejected — the conservative ⧖ Q13 position, asserted so that a later ruling has to change a test deliberately rather than by accident.
16. On a file whose initial state declares entry actions, the construction establishment obligation discharges only if it discharges over **both** members of the plan set (with and without W3), per `precept-language-spec.md:2202`.

## Dependencies

**Upstream.**

- The matrix's transport rule (`:355`) and its type-structural qualifier argument (`:359–363`) — this design consumes both (Decision 4's route-condition group and the `provdeps` discriminator). Note that both were among the arguments rev 11 marks as refuted by adversarial review; this design consumes their *statements*, and if either is re-derived differently the Decision 4 partition needs re-checking.
- The eight-writer enumeration in `precept-language-spec.md` § 3A.4 — the expected set is computed against it. Complete as a category list (§ Open questions), but asserted rather than build-enforced (`:2204`), with BUG-033 a verified instance of it being consumed wrongly. `WriteSiteCategories` and `Precept0030` together close the declared-but-unwired half and not the undeclared half (Decision 5).
- The certificate format's premise vocabulary — Decision 4 partitions it and adds two reference fields to it.
- **`precept-language-spec.md:2202`** — the standing default that no analysis may assume entry actions run at construction. The construction plan set (§ The write-site surface § 6) exists to honour it; a ruling in either direction collapses the set to one member.
- **⧖ Q13, the `omit` canon conflict** (matrix `:294`, `:459`) — Decision 4's conservative presence position and W6's unusable post-state value both hang on it.

**Companion — the coupling that must be checked, not assumed.**

This design and `constraint-establishment-preservation-obligations-2026-07-23.md` are **not independent documents**; the completeness check compares a set this document computes against a set that document mints, and they agree only if four things match exactly. Each is a place the pair can silently break:

1. **The occasion set.** Both must range over the same `Occasions(file)` — construction rows, transition and stateless event rows, editable-field doors, and **no** `Restore` occasion (§ The write-site surface § 4).
2. **`Owes`.** Both must use `O.Governed ∧ Targets(O) ∩ mentions(C) ≠ ∅`, with `Governed` read from `OperationSurfaceMeta` and `Targets` unioned over the enclosed writer categories (§ 3).
3. **The mention set.** Both must apply all three legs unchanged to constraint modifiers — no `{F}` special case on either side (§ 4, and matrix `:234`).
4. **The establishment site set.** Both must use § 5's three-way split by constraint kind, and both must treat the construction occasion as carrying a **plan set** when the initial state declares entry actions (§ 6).

If the companion diverges on any of the four, `Minted(C) ≠ Expected(C)` on ordinary files and `PRE0166` fires as a false positive — the completeness check reporting a compiler defect that is actually a design disagreement. That failure mode is loud rather than silent, which is the right way round, but it is a coupling and it is named here so it can be checked at build time rather than discovered.

**Downstream.**

- The rule and ensure obligations themselves — the companion design. It creates the checks this one counts and cites.
- Repair of the qualifier-modifier hole, BUG-033, and BUG-035 — each becomes a loud failure once this ships, rather than a silent one.
- Slice 3's re-ratification, which needs a rule layer that can state establishment and preservation.

## Doc-update enumeration

- `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU — the two new subtypes, **and** the correction that the DU does not in fact have "a subtype for every `ProofRequirementKind`" (`Modifier` has none at HEAD); § Obligation Generation Contract — the completeness rule as a fifth contract item; § Contracts and Guarantees § Obligation Completeness — replace the current claim with the checked one.
- `docs/language/catalog-system.md` — inventory entries for `WriteSiteCategories`, `OperationSurfaceMeta`, and `MentionSetLegs`; `CoverageRecord` under supporting types.
- `docs/compiler/diagnostic-system.md` — `PRE0166` only. `PRE0165` is not added; if it was reserved anywhere, the reservation is released and the number is not reused.
- `docs/language/precept-language-spec.md` § 3A.4 — the writer table gains a pointer to its catalog form, and `:1992`'s "not yet enforced" note is updated to name `WriteSiteCategories` as the surface that enforces it; § Open questions `:2200` and `:2204` — updated to say which half is closed (declared-but-unwired) and which is not (undeclared); § 0.7 — the composition paragraph gains the completeness leg.
- `docs/tooling/mcp.md` — the coverage record on the obligation DTO, including its empty occasion classes.
- `docs/tooling/language-server.md` — constraint hover gains the coverage summary.
- `docs/Working/obligation-discharge-matrix-2026-07-19.md` § The cell — the rationale correction in § The cost, in its strengthened 2026-07-24 form; § Vocabulary — coverage record and write citation as defined terms; § Case shapes — a pointer from the five-value write-site-category axis (`:172–180`) to the eight-category surface it projects.
- `docs/Working/certificate-steps-membership-2026-07-12.md` — the four-way premise partition, the added `Coverage` reference on group 4, and the added write citation on `PriorAction`.
- `docs/contributing/` — the analyzer's allow-list discipline.
- `README.md` — no change; it makes no claim about obligation completeness.

## Operational dimensions

**Security.** N/A — no source-text ingestion surface changes. The completeness walk consumes an already-parsed semantic index.

**Observability.** The two failure modes are deliberately distinguishable and now live in two documents. The companion's `PRE0168` names the author's rule and the breaking occasion. `PRE0166` names the constraint, the occasion, and the **writer category** that should have produced a check and did not, and says plainly that it is a compiler defect. Naming the category is what makes the diagnostic actionable for the maintainer — "W7, update patch" points at a specific unwired surface, where "no check at this site" points at nothing. The coverage record is surfaced through `precept_compile`, `precept_proofs`, and constraint hover, so "which occasions must preserve this rule, which were empty, and did the rest pass" is answerable without reading compiler output.

**Evolvability.** N/A — no dependency on an external standard. The catalog entries this design adds are internal.

## Falsifiers

1. If the per-file completeness walk adds more than 10ms to the median sample compile, the enumerate-every-compile decision is wrong for the product's latency budget and the check should move to a build-time or opt-in gate. (Baseline: the full corpus currently compiles in about 44ms.)
2. If more than three files in `samples/` need a coverage exclusion entry to pass acceptance criterion 11 **for a reason other than an unwired writer category**, the expected-set rule is over-generating and the mention-set legs or write-site categories are modelled too broadly. The carve-out matters: five categories are unwired at HEAD, so exclusions attributable to those are evidence of the known gap, not of over-generation, and conflating the two would let a modelling error hide behind the build-status backlog.
3. If a real defect is found in obligation minting that the per-file check does not catch **and** the build-time analyzer does not catch, the two-level split in Decision 5 is missing a level and the design needs a third. Note that Decision 5's counter-evidence already concedes there is no verified instance of a defect only the analyzer catches; if none appears once both levels are live, the honest response is to re-argue Decision 5 rather than keep the second level on the strength of the argument alone.
4. If the completeness check and the minting walk are ever found to have been fixed together in one edit to make a test pass, the independence in Decision 3 is procedural fiction and the expectation needs a genuinely separate source — spec-derived generation, or a second implementer. **This falsifier's condition was already met once**, by BUG-033, before this document was written; Decision 3's counter-evidence now says so and narrows the claim accordingly. A second occurrence should be read as confirmation rather than as a surprise.
5. If a domain expert shown `PRE0166` attempts to edit their file in response, the compiler-internal framing has failed and the message needs to be routed away from the author entirely.
6. If `Expected(C)` and the companion's minted set diverge on an ordinary sample file — a false-positive `PRE0166` traceable to the two designs disagreeing rather than to a compiler gap — then the four coupling points in § Dependencies are not sufficient to keep the walks in step, and the shared surface needs to be a single executed function rather than a shared definition two walks implement.

## Open questions

Four were carried in the first draft and closed there. The 2026-07-24 rework closed three review findings and, in doing so, opened four genuine questions it does not settle. Both sets are recorded; the first set is history, the second is live.

### Live after the 2026-07-24 rework

**Does the owner want an admissibility condition that can actually reject?** Decision 1's demotion is honest about what the citation buys, but it leaves the 2026-07-21 ruling's stated purpose only partly served: the ruling asked for the whole-file property to become something a certificate exhibits, and a serialization annotation plus a separate whole-file check is not quite that. Two responses are available and this document does not choose between them — amend the ruling's rationale sentence to describe what the duty actually does, or design a stronger condition that has independent failure modes (the obvious candidate: require the citation to be re-derivable by a checker that does *not* share the engine's computation, which is the deferred re-checker's job and would make the condition non-vacuous). The second is a real design pass, not an edit.

**Premise class (d) across a `Restore`.** `Restore` is ungoverned, so it owes no preservation — but the induction that premise class (d) rests on still needs a base case for a restored entity. Canon supplies one and it is narrower than it looks: "the prior committed state satisfied the rules in effect **when it was written**" (`precept-language-spec.md:272`). Under an unchanged definition that is the base case; under a changed definition it is not, and `:1958` points forward to migration logic that does not exist. Named, not resolved. It belongs to whoever writes the establishment validity argument, and it may be an owner question.

**⧖ Q13 — what the `omit` reset leaves in the slot.** Decision 4 takes the conservative position and acceptance criterion 15 pins it, so the design is sound under either ruling. What is not known is the *cost*: `omit` appears in 25 sample files, and how many carry a constraint mentioning an omitted field is measured by the corpus run, not by this document.

**Whether the corpus can pass at all before the five unwired categories are built.** Acceptance criterion 11 asks every sample to report a complete obligation set. On current built-status that is not achievable — five categories mint nothing — so either the criterion is satisfied by a long, honest exclusion list, or this design's acceptance is gated on the companion's implementation reaching those categories. Which of the two is intended is a sequencing question for the plan, and naming it here stops it being discovered during execution.

### Closed in the first draft

**The coverage-record encoding against the 2026-07-21 ruling** — closed as an engineering call in Decision 1, though for different reasons after the rework: the encodings do not "check the same two things", they check nothing that can fail, and the choice rests on cross-proof consistency, output size, and replay cost.

**Completeness of the § 3A.4 writer list** — checked, and the list is complete as an enumeration of writer *categories*. `ActionKind` has exactly fifteen members (`Set`, `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Clear`, `Append`, `AppendBy`, `Insert`, `RemoveAt`, `Put`, `EnqueueBy`, `DequeueBy`), matching the table's "fifteen kinds"; the `into` target, the state-action positions, default materialization, computed-field recomputation, the `omit` reset, the update patch, and `Restore` account for the rest. Nothing else in the language places a value into a field slot — an argument default writes an argument, not a field, and `Inspect` is non-mutating. What is *not* verified, and never was the same claim, is that the compiler consumes all eight; § The write-site surface § 8 now measures exactly how far short it falls. That is the write-surface dependency the design carries, and Decision 5's analyzer closes the declared-but-unwired half of it.

**The establishment site set for event, entry, and exit ensures** — already answered by the matrix, and the first draft recorded it as open through insufficient reading. Entry and exit ensures are transition-moment obligations, explicitly edge-triggered — "nothing is owed while merely resident" — so the obligation at the moment *is* the check and there is no separate establishment site. An event ensure is ingress-class and discharged by ingress evaluation, which the matrix lists as a discharge mechanism rather than something needing establishment. Establishment as a distinct site set therefore applies to exactly two constraint kinds: `rule`, at construction, and the residency `ensure`, at construction plus every entry into its anchor state. § The write-site surface § 5 is written this way.

**Whether `DeclaredPresence` is breakable** — checked against the action catalog and it splits, like `DeclaredQualifier`. The 2026-07-24 rework found the split was drawn too narrowly (it reached `clear` and not the `omit` reset) and widened it; Decision 4 carries both legs and the ⧖ Q13 dependency the second one introduces.