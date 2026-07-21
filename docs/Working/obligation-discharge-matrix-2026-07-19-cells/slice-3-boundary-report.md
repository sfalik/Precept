# Slice 3 — the fault family: what got defined, and what it is blocked on

**Status**: Draft — 2026-07-21. For the owner. Companion to `docs/Working/obligation-discharge-matrix-2026-07-19.md` (the matrix) and to the nineteen `fault-*.cells.json` files in this folder.

Read the two paragraphs below first; everything after them is the evidence.

**None of the nineteen fault cell files is ratifiable as it stands.** Nineteen independent adversarial verification passes returned thirteen "unsound" verdicts and six "needs rework". Zero passed. The compile evidence in those files is, with a handful of named exceptions, genuine and reproducible — the verifiers re-ran roughly a thousand witness programs and nearly all of them behaved exactly as recorded. What fails is the definitional layer: a large majority of the cells marked `defined` rest on a discharge rule the matrix does not currently state, and several rest on one the matrix explicitly excludes.

**The single most valuable thing you can do with this report is answer the questions in "What the matrix does not answer".** Those are not editorial cleanups. Ranked as they are below, the first two questions between them govern essentially every `defined` cell in the fault family; nothing downstream ratifies until they are ruled on, and re-deriving after a ruling is much cheaper than re-authoring a second time.

---

## What got defined

**202 cells are recorded as data** across nineteen files: 175 `defined`, 25 `open`, 2 `empty`. No cell is `deferred` and none is `conflicted`.

The honest denominator is harder, and this is the first thing that needs saying plainly.

- The **fault axis enumeration** is mechanical and pinned by tests: 101 catalog-declared proof-requirement sites (74 in Operations, 18 in Types, 7 in Actions, 2 in Functions), produced by walking the four requirement-declaring catalogs. `dotnet run --project tools/Precept.MatrixTools -- fault-axis` reproduces it; twelve tests in `test/Precept.MatrixTools.Tests/FaultAxisEnumeratorTests.cs` pin the total, the per-catalog split, the per-kind split, and eight representative rows.
- The **evaluation-site axis** is 23 categories, derived from the grammar rather than from a sample sweep. The cell data uses 21 of them; `stateless-hook-action-operand` and `choice-value-expression` appear in no cell's coordinates.
- The naive product of those two axes is 2,323 coordinates. The disposition pass that preceded authoring reported a finer product of **9,292** coordinates with 992 defined, 441 deferred, 760 open, 7,047 empty and 52 conflicted.

**That 9,292-cell disposition map is not in the repository.** It exists only as counts in the disposition pass's summary. So the defensible statement is: 202 cells are recorded as checkable data; a further several thousand coordinates were dispositioned in a pass whose output was not persisted and therefore cannot be validated, cited, or swept when a ruling lands. Several cell files lean on that map in prose — three of them fold twenty or more evaluation-site categories into a handful of "representative" cells and point at it — so a non-trivial share of the claimed coverage currently rests on a document that is not here. Group 17 alone folds 20 categories into 4 representatives, leaving 48 of its stated 60 coordinates with no disposition the validator can see, while every sibling file enumerates its categories one cell each.

Two owner-ruled exclusions are honoured throughout: **arithmetic overflow** (deferred pending the number-model decision) and **temporal arithmetic**. Thirteen sites (441 coordinates by the disposition pass's count) were deferred under the temporal ruling — but see question 4 below, because the boundary of that exclusion is not written anywhere and the deferral was applied under the wider of two readings.

---

## What the enumeration might have missed

The catalog walk is a fixed point, not a reading: each of `Operations.All`, `Functions.All`, `Actions.All`, `Types.All` is built as `Enum.GetValues<TKind>()` over an exhaustive `GetMeta` switch whose default arm throws, so no catalog member can be silently absent. An independent textual cross-check — grepping the eight concrete `ProofRequirement` constructor names across the four catalog files — returns exactly 74 / 18 / 7 / 2, agreeing site-for-site. `ProofRequirements` is a declarable slot on exactly four metadata types, and a repo-wide grep shows every other mention is a consumer.

What would falsify it, stated so you can judge rather than trust:

1. **Five of the thirteen requirement kinds have no catalog declaration site at all** — Presence, IntervalContainment, LengthContainment, CountContainment, AssignmentQualifier. Every instance of those is constructed in pipeline code. If the fault family is meant to include them, 101 is the wrong denominator *by construction*, not by omission. This gap is pinned by a test so it cannot quietly change, and the corpus measurement makes its size concrete: of 1,045 obligations HEAD minted across `samples/`, 464 — 44% — are of kinds with no catalog site.
2. **Seven distinct dynamic minting mechanisms** live outside the catalog walk: optional-field presence minted during the expression walk; the action-attached dynamic generator on `set`; collection element write-site containment; default-value obligations driven off the *Modifiers* catalog through `ProofSatisfactions` rather than `ProofRequirements` (the largest body of requirement construction outside the catalogs); computed-field result bounds; count containment on every collection mutation; and type-checker-stamped assignment qualifiers, which make a `TypedAction`'s requirements a superset of its `ActionMeta`'s. A catalog walk sees none of them.
3. **A fifth `ProofRequirements` slot** on a new metadata type would be invisible to the walk and nothing would complain.
4. **Overflow has no catalog-declared site whatsoever.** HEAD emits `NumericOverflow` — I reproduced it on `samples/Test2.precept` — but no `ProofRequirement` anywhere declares an overflow precondition. So overflow is structurally outside the enumerated product, and the deferral and the catalog-vs-pipeline scope question are entangled.

Two smaller items the enumeration raises rather than answers: `mid(str, start, length)` documents a positivity precondition in its own catalog description but declares no requirement (is that a 102nd site, or a stale description?); and `pow` declares its non-negative-exponent requirement only on the integer overload, with nothing stated for the decimal and number overloads.

The evaluation-site axis is argued off the grammar's top-level dispatch table and is closed under that argument, with one deliberate inclusion (`operand-free-action-precondition`, for `dequeue`/`pop`, which have a precondition and no operand) recorded rather than excluded. Its known soft spot is that it treats a site as a *syntactic position*; the alternative reading — one site per evaluation occasion — is question 3 below.

---

## What the matrix does not answer

These are ranked by how much work each one unblocks. Every one of them is a question the matrix's own text cannot decide, and every one was hit independently by multiple authoring groups.

### 1. Is there a validity argument for the fault family at all? — blocks essentially every defined cell

The matrix's § Validity arguments states seven arguments by name, and the schema pins that list closed. **All seven were written for constraint-family weakest preconditions.** None is about a catalog-stamped safety precondition. Every fault cell that cites one is therefore citing an argument that does not reach its case, and the matrix's own ratification gate says "no cell ratifies whose derivation cites an argument-less rule."

It is worse than a generic mismatch in two specific ways:

- The most-cited argument, *Arg-bound interval arithmetic*, ends by stating its own exclusions: "an RHS containing division rounds even in `decimal` … so division shapes sit outside this rule as stated", and "the argument covers the exact lanes (`integer`, `decimal`) only". **Every cell in eleven of the nineteen files is a division shape**, and several are on the `number` lane or on money/quantity operands. The cited authority explicitly disclaims the case it is cited for. Only two files disclosed this exclusion; the rest disclosed a different, narrower mismatch or none.
- *Arg-bound interval arithmetic* is also written for premise class (b) — event-arg ingress governance. Most fault discharges consume a **field modifier** (class (a)), whose truth at a later evaluation site depends on establishment at construction plus preservation at every write, which is a different soundness burden with a different argument.

**The question**: does the matrix author a fault-family validity argument (and if so, one per premise class, or one covering the interval-exclusion derivation generally)? Or is the honest disposition for these cells `open` until it does? A ruling either way is a small piece of writing that re-validates roughly 175 cells.

### 2. Is premise class (d) — a business rule holding in the pre-state — available to the fault family? — blocks ~40 cells and touches the known unsoundness

The matrix's per-family case-shape table gives the fault family's discharge column as **"premise classes (b)/(c)/(a) per catalog `ProofSatisfactions`"**. Class (d) is not listed. Eight of the nineteen files nevertheless declare (d) applicable, with full contract entries, suggestion schemas, discharge witnesses and near-misses, across roughly forty cells. Some flagged the tension; several did not.

This matters more than an ordinary vocabulary question, because class (d) is exactly the path the verified false-proof defect runs through (`authored-expressiveness-gaps.md` Part 3): a rule stating a field is positive, a handler dividing by it, and a different handler setting it to zero — compiles clean, obligation reported Proved via the compositional-constraint strategy. Widening the fault family to (d) without a preservation obligation on the rule would write that defect into the definition. The corpus measurement shows six live instances of that shape in `samples/` today.

There is a real argument on the other side — `want :104` says faults get "the identical premise-and-certificate treatment" — which is why this needs your ruling rather than a cleanup.

### 3. Is an evaluation site a syntactic position, or an evaluation occasion? — re-opens every defined cell if ruled the other way

The whole slice treats a site as a syntactic position, following the matrix's sentence that "each evaluation site in a plan mints its own fault obligations". The alternative — one site per occasion, so a rule condition evaluated at an editable-field ingress is a different site from the same condition at a handler checkpoint — changes premise availability materially, because ingress evaluation is a discharge mechanism the matrix rules on separately. The matrix's sentence settles the in-plan case and says nothing about the cross-occasion case. A ruling the other way re-opens all 175 defined cells.

### 4. What exactly does the temporal-arithmetic deferral cover?

The cited canon (`primitive-types.md:688`, `temporal-type-system.md:624`) defers **representable-range overflow only**. Whether it also covers period-dimension checks on `date ± period`, and divide-by-zero where the divisor merely happens to be a temporal value, is unwritten. Thirteen sites were deferred under the wider reading; under the narrower one, nine of them would be defined and two would join the open qualifier region. Two of the deferred groups are visibly weak members — `PriceTimesPeriod` / `PriceTimesDuration` carry a dimension-match requirement, not a value question.

### 5. Two premise classes the vocabulary has no home for

- **Type-qualifier facts.** Thirty-six catalog sites — every currency-match, unit-match, dimension-match requirement — are discharged by facts about a value's currency or unit. Those facts live in a *type reference* (`money in 'USD'`), not in a modifier, and none of the four premise classes covers a declaration attribute of that shape. Is there a fifth class, does class (a) widen beyond modifiers, or are these type-checking rather than proof surface?
- **Collection-contents facts.** `append … by` requires that the ordering key not already exist. All four premise classes are declaration- or guard-scoped; none ranges over a collection's current contents.

### 6. Self-discharge inside one expression

Three shapes, none written down anywhere:

- Is an earlier conjunct of a guard a premise for a later one? `when Parts != 0 and Total / Parts > 1` is the spelling an author reaches for.
- Do the branches of `if C then A else B` get `C` (or its negation) as a premise? This one affects every category, since conditional expressions appear in all of them.
- Does a *conditional rule's* activation guard discharge a fault inside the rule's own condition (`rule X / Y > 1 when Y != 0`)? The matrix's class (c) is the *handler's* guard, and a rule activation guard is not one. Likewise, is the failure context — the constraint is false, and for a conditional rule its `when` is true — a premise inside a `because` message?

### 7. Two-sided index-bounds discharge

Proving `0 <= i < count` consumes both an interval on the index and a cardinality fact about the collection. The matrix's only written fault decision procedure computes one operand's interval. Whether the two-premise combination is licensed, and by which derivation, is unstated. Two groups split on it: group 18 marked its transition-row and state-hook cells `defined` on conjunct-wise decomposition while opening its construction-row cells for exactly that missing rule.

### 8. Does `mincount 1` discharge a collection non-empty obligation, and is it earned?

Two files answer this in opposite directions. `fault-13` records `mincount 1` as a discharge with `builtStatus: proven-today`; `fault-16` records the same spelling as a *near-miss that must still reject*, and opens five cells on the ground that no discharge exists. Canon (`collection-types.md`) and HEAD both say it discharges. But `fault-16`'s reason for doubting it is a genuine catch and points at a live hole: `field Readings as log of decimal mincount 1` with no default and no write site compiles clean while `.last` is read on a provably-empty collection. So the real question is whether `mincount` may be consumed as a premise without its own establishment obligation.

### 9. Smaller, but still unanswered

- **Base minimality.** Groups applied it inconsistently — one withheld three bases entirely rather than record a base that co-mints a second obligation, while a dozen others record `noOtherDiagnostics: true` on bases that emit `FieldNeverSet` warnings. What does the matrix require?
- **Schema vocabulary gaps.** The `application` object has loci only for a row guard and event-arg declarations, so no field-modifier or top-level-rule addition can be applied mechanically — which is why a large fraction of fault witnesses cannot be converted into tests. The discharge `kind` enum likewise has no member for a field modifier or a constraint declaration, so those are recorded as `arg-modifier` and `other`.

---

## What the verification found

Nineteen independent verification passes, each re-running the group's witnesses from the recorded source at HEAD `e1a14d91`. **Thirteen returned "unsound", six returned "needs rework", none passed.** The findings below are the blocking and major ones; each was evidenced by a re-run or a source citation, not argued.

### Findings that make a recorded answer wrong

- **The false-proof hazard is broader than the brief assumed.** The slice's ground rules exempted modifier-based discharges from the known rule-premise defect. Two groups showed that exemption is false. A `nonzero` field whose construction row writes 0, divided by at a state hook, compiles with zero diagnostics while the tool's own calculator prints `Divisor:nonzero: WP = false` for that row. Same for `nonnegative` under `sqrt`: `field Reading as number nonnegative` with `set Reading = -5` then `sqrt(Reading)` compiles clean, and swapping `nonnegative` for `min 0` makes it reject — **the only spelling that discharges is the one nothing enforces**. Qualifier modifiers (`nonzero`, `positive`, `nonnegative`) mint no write-site obligation; bound modifiers (`min`, `max`) do. Consequence: a substantial number of discharges recorded `builtStatus: proven-today` on a clean compile are resting on the same defect the brief ruled out, one premise class over.
- **Two groups' central measurement does not reproduce.** Group 17 records in all twelve cells, as live-verified, that a verbatim `when Index >= 0 and Index < F.count` guard does *not* discharge the index-bounds obligation at HEAD. It does. The `.at` accessor carries two obligations that HEAD reports under the same diagnostic code, distinguished only by whether the message names the index expression or a literal placeholder; the group read both as one. That misreading is the sole basis of two missing-rule findings and one open question the group routed to you, and it also means all three of that group's near-misses weaken the wrong conjunct. Group 13 records, also live-verified, that no minimal base is constructible for the same reason; a minimal base is constructible.
- **Six discharge witnesses do not produce an accepting program.** Group 3's six class-(d) discharges add `rule Parts != 0` while leaving `field Parts … default 0` in place, so the added rule is falsified by the default configuration — the calculator reports `rule[0]: WP = false` for all six. Group 11's two event-arg-modifier discharges add `positive` to a field the construction row sets to zero, same failure, and record `expected: accept`.
- **Two groups' bases carry a second, undischarged obligation.** Group 9's transition-row-guard bases write the divisor from an unconstrained arg inside the very row being guarded, so the `positive` addition mints an uncovered preservation obligation; the paired near-misses then reject for two reasons and isolate nothing.
- **One recorded band member is not sound**, so it does not measure the band (group 18: a guard bounding the index above with nothing bounding it below).

### Findings about coverage

- **Group 6 covers 4 of the 10 primitive divide/modulo catalog kinds** with no cell, no disposition and no equivalence-class note for the other six — 12 silent cells, against a sibling group that enumerates all ten.
- **Four business-domain divide-by-zero sites are absent from every cell file**: `MoneyDividePeriod`, `MoneyDivideDuration`, `QuantityDividePeriod`, `QuantityDivideDuration`. Each carries the identical "Divisor must be non-zero" requirement. If the temporal deferral covers them, they still owe four `deferred` cells.
- **Groups 3, 13 and 17 record only a fraction of their stated product** — group 13 records 15 cells for a 45-coordinate group, disposing of the other two site categories in a prose note pointing at the uncommitted disposition map, while its own text says the applicable-class set differs at those coordinates.

### Findings about internal consistency

Four coordinates now carry two different answers in two different files:

| Coordinate | One file says | The other says |
|---|---|---|
| `ensure-activation-guard`, fault family | class (a) only, (d) does not instantiate (fault-3) | classes (a) and (d) (fault-14) |
| the five guard positions, fault family | (a) and (d) (fault-3) | no (d) anywhere (fault-9) |
| construction-row-action-operand | class (c) does not exist there (fault-2) | classes (b) and (c) (fault-8) — and fault-8 is right; a construction-row guard does discharge |
| `mincount 1` on a collection accessor | a proven discharge (fault-13) | a near-miss that must reject (fault-16) |

Each of these is a case where the matrix decides nothing, one group resolved it silently, and a sibling resolved it the other way.

### The verification machinery itself is not yet running on this family

`convert-cells` produces **zero executable rows for all nineteen fault families**. All 202 cells are skipped, each with a named reason: 174 for "no mechanized canonical WP key is recorded" (the calculator produces canonical keys for constraint weakest preconditions, not for catalog-stamped fault preconditions), 25 because the cell is `open`, 2 because it is `empty`, 1 for an absent base. So none of the 189 bases, 273 discharges or 273 near-misses recorded in the cell data reaches the runner. The matrix names the cell-generated test matrix as the *only* verification of the reject half of every contract. Today it verifies none of it. The family-1 manifest still runs and still produces its rows; nothing regressed — the fault family simply never entered the machinery.

---

## What could not be verified, and why

- **The rule-premise discharges.** Every discharge whose premise is a business rule is recorded `builtStatus: unresolved-today` with the false-proof reason named, and no clean compile is cited as confirming one. That discipline held across all nineteen files — the verifiers checked it specifically and found no violation. Those discharges are unverified by design and stay that way until question 2 is ruled on.
- **The modifier-premise discharges, retroactively.** Per the finding above, discharges that consume a `nonzero` / `positive` / `nonnegative` field modifier at a state hook or a constraint site are exposed to the same defect and should be treated as unverified too, regardless of what their `builtStatus` currently says. This affects groups 2, 4 and 12 at minimum; a sweep is needed once the ruling lands rather than a per-file patch now.
- **Every recorded proof strategy.** The compile tool prints diagnostics and weakest-precondition traces; it never prints which `ProofStrategy` discharged a fault obligation. So every `strategy` field on a fault discharge is a source-reading inference sitting inside a live-verified record. One is provably wrong: eleven cells in group 7 name `IntervalContainment` for an obligation carrying a `NumericProofRequirement`, which the interval prover declines outright.
- **Two evaluation-site categories were never measured** — `type-qualifier-expression` and `collection-inner-type-modifier-value-expression` — and are recorded as unmeasured rather than inferred. A related caution: for the event-arg and collection-inner-type positions the expression is not type-checked *at all* at HEAD (a reference to an undeclared field there produces zero diagnostics), so "no obligation minted" at those positions is confounded.
- **Ten evaluation positions mint nothing at HEAD**, each confirmed by a compiled snippet against a structurally identical position that does mint: the four guard positions plus rule-activation and ensure-activation guards, both interpolation positions, the field `default` value expression, and the two constraint-modifier value expressions. This slice treats all ten as build gaps — the definition commits, the compiler does not yet raise. If any is meant to be a definitional exclusion, those cells change disposition, not just built status. The most pointed one: the reject-message interpolation is the exact site `what-i-want-2026-07-16.md:72` uses to argue the fault axis has to exist at all.

---

## What the corpus measurement does and does not support

Full detail in `corpus-measurement-fault-family.md`; the scoping there carries the matrix's ratification-protocol limits forward unchanged. In summary:

HEAD mints **1,045 proof obligations** across the 78 samples. 1,043 are proved; the two unresolved ones are both in `Test2.precept`, the one sample deliberately kept failing.

**Agreement with the corpus is necessary, not sufficient, and the corpus is never citable as coverage.** The corpus is filtered by a test asserting every sample compiles, so programs demonstrating expressiveness loss could not be in it; a measured "no lost expressiveness" is the expected reading whether the true number is zero or large. These are documentation samples this project wrote, not field data. And one limit bites harder for faults than for rules: **the ledger records what HEAD minted, not what the definition owes** — a site the engine does not mint at contributes nothing, and this slice measured ten such positions. Reading a zero here as "no obligation exists" inverts the finding.

What it does support, as existence proofs and named gaps:

- The interesting derivations are barely exercised. Of 30 divide-by-zero obligations in the entire corpus, **18 discharge by folding a literal divisor** — no authored premise at all. Four discharge from a guard, two from a declared modifier, six through the compositional-constraint strategy.
- Those six compositional-constraint discharges are the corpus's only instances of a rule discharging a fault obligation, and they are exactly the shape the verified defect makes unsound. They count as proved below because that is what the ledger says — not because the discharge is sound.
- Divide-by-zero is minted in exactly three contexts corpus-wide: transition rows (23), constraints (4), computed fields (3). Never in a guard, an interpolation hole, or a declaration position — independently corroborating the ten-position minting gap.
- Collection non-empty discharges through a guard 17 times and through in-chain growth once. **Never through a declared `mincount`** — so the corpus supplies no evidence either way on question 8.
- Three declared requirement kinds are minted zero times: `Dimension`, `CountContainment`, `KeyPresence`. That is a fact about the corpus, not the catalog.
- 44% of minted obligations (464 of 1,045) are of the five kinds with no catalog declaration site — the concrete size of the enumeration's blind spot.

---

## What slice 4 needs before it can start

In order, and the first two are hard prerequisites:

1. **Rulings on questions 1 and 2.** Between them they govern every `defined` fault cell. Authoring more cells on top of an unlicensed validity-argument citation and an unlicensed premise class would multiply the rework rather than reduce it.
2. **A ruling on question 3** (site identity), because it is the one that re-opens everything if it goes the other way. Cheap to answer now, expensive to answer later.
3. **A decision on the fault family's denominator.** Either the 9,292-cell disposition map is regenerated as committed data — so totality is checkable and a landed ruling can be swept mechanically — or the coverage claim is restated as "202 recorded cells" and the folded categories are enumerated as real cells. The current state, where a meaningful share of coverage rests on an uncommitted document, cannot ratify.
4. **Two pieces of tooling, both small and both currently blocking verification:**
   - a canonical WP key for catalog-stamped fault obligations, without which `convert-cells` yields zero rows and the reject side of every fault contract stays untestable;
   - `application` loci for a field-modifier addition and a top-level-rule addition, without which most fault witnesses cannot be applied mechanically.
5. **A remediation pass over the four contradicting coordinates** in the consistency table, once questions 1, 2 and 8 are ruled — not before, since three of the four are downstream of those rulings.
6. **A re-measurement of group 17** with the two `.at` obligations separated, which will likely retire that group's headline missing-rule finding entirely.

Nothing in this report claims a fault cell is ratifiable. Nineteen of nineteen files came back with substantive defects, and the defects cluster on two unanswered questions rather than on nineteen independent authoring mistakes — which is the encouraging reading, but only if those two questions get answered before the next slice starts.
