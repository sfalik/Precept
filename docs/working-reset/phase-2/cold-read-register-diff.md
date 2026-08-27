# The cold read, diffed against the contradiction register

**Written 2026-08-26 against HEAD `eeda4c91`.** Nothing here edits canon and nothing here is a ruling.
It matches one document against another and reports what the match shows.

## What was compared, and what the exit condition asks for

Phase 2's fourth exit condition, at
[`compiler-readiness-plan.md`](../compiler-readiness-plan.md) § *Finish line*, reads in full:

> The search procedure, re-run at HEAD by somebody who did not write it, produces no disagreement that
> is not already a row. Check: run it, diff the output against the register.

Two halves. The re-run happened, but deliberately not as a re-run of
[`search-procedure.md`](search-procedure.md): a reader was taken through the owner's document
([`docs/Working/what-i-want-2026-07-16.md`](../../Working/what-i-want-2026-07-16.md)) and then through the
runtime and compiler documents with no register, no patterns and no plan, because a search procedure
cannot find the holes in its own scope. That read is written up and verified at
[`cold-read-findings.md`](cold-read-findings.md).

**This document is the second half — the diff.** Every finding the cold read wrote up is matched, one at a
time, against [`contradiction-register.md`](contradiction-register.md): already a row, in the register in
different words or inside a wider row, or not in the register at all. Where a matching row is recorded as
executed, the document is re-read at HEAD to see whether the finding still reproduces.

**What this document does not do.** It does not declare condition 4 passed or failed, it resolves nothing,
and it opens no register row. Five things it turned up plainly want attention before anyone signs off
phase 2, and they are collected in their own section with the reason each matters; the decision on all
five is somebody else's.

---

## The numbers

The cold read wrote up **31 entries**. Three of them are not live findings and are diffed only as
bookkeeping: Part 1 finding 4 was withdrawn before verification began, Part 1 finding 2 was closed by
commit `621bde2b` and verified to leave no trace in canon, and Part 1 finding 3 turned into a
demonstration that canon *agrees* with the owner and discloses that the capability is unbuilt, leaving
behind only a documentation defect that the write-up carries separately as Part 3 item 18.

That leaves **28 live findings**. Against the register's 145 rows:

| | Count |
|---|---|
| Already a register row, same substance | 4 |
| In the register in different words, or inside a wider row that was written about something else | 4 |
| In the register, recorded as **considered and explicitly excluded** — and the register's own stated condition for un-excluding it has now been met | 1 |
| **Not in the register at all** | **19** |
| Total live findings diffed | 28 |

Of the four rows in the second group, **three are executed rows whose finding the cold read still
reproduces at HEAD**, and in two of those three the reproduction is caused by the execution rather than
surviving it.

---

## The nineteen findings that are not in the register

This is the point of the exercise, so it comes first. Each entry says what the finding is, that no row
covers it, and — as far as can be told from the register's own text — why the search did not reach it.

### Why nineteen, in one paragraph before the list

Four causes account for all nineteen, and they are properties of the register's shape rather than
mistakes by any pass.

1. **The register is built to hold one shape of disagreement.** Its rows are "canon says X, the want
   document says not-X", plus nine `ADDITION` rows for "the want document requires something canon does
   not say at all". There is no row shape for *the want document being silent where canon speaks* (one
   finding), and no row shape for *two canonical documents disagreeing about a fact with no want line
   behind it at all* (twelve findings). The `INTRA-CANON` marker exists and eleven rows carry it — but
   every one of those eleven arrived either from the 2026-07-14 input list or as a by-product of a
   want-driven family. Nothing ever went looking for canon-against-canon defects on their own account.
2. **Patterns match words, and some of these defects are arithmetic.** A document that declares 147
   diagnostic codes and states 164 three times contains no phrase a pattern could be written against.
3. **A table splits a phrase across cells.** `§ 1.0`'s family test looks for a constraint noun next to an
   evaluation verb; a table whose header row says `Constraints` and whose body row says `Evaluate` puts
   them on different lines, and no pattern in the register can join them.
4. **One finding was ruled out of the register by policy**, and that is the one in its own category
   below rather than here.

### Part 1 — disagreements between canon and the owner's document

**1. The proof engine has no obligation for "this handler preserves this rule".** The cold read's heaviest
finding. The owner's model is an induction (`what-i-want-2026-07-16.md:19`, "Every handler *preserves*
every rule, provable from four premise classes"). At HEAD, `proof-engine.md:489` enumerates the obligation
vocabulary:

> The `ProofRequirement` discriminated union has a subtype for every `ProofRequirementKind` (numeric,
> presence, dimension, modifier, qualifier-compatibility, qualifier-chain, interval/length/count
> containment, key-presence, index-bounds, dimensional-product, and assignment-qualifier — the open-field
> assignment case discharged via guard narrowing)

Thirteen kinds; neither preservation nor establishment is among them.

**No row covers this.** `grep -c ProofRequirementKind contradiction-register.md` returns **0**, and the
word *establish* appears in the register exactly once — inside `N52`'s pattern cell, quoting the sentence
the correction wrote. The adjacent rows are `N51` (delete the "No Inductive Proof" section), `N52`, `N53`
and `N54` (write the induction, the four premise classes and the symmetric write sites into the
specification), and `N81` (routed to the owner: "The induction itself has never been argued"). All of
those are about what canon **says**. None is about whether the compiler has an obligation kind that says
it.

**Why it was missed.** The register's question was whether canon states the owner's model. Canon now
does. Whether the machinery canon describes elsewhere can carry the model is a different question, and
nothing in the search procedure asks it. This is the first of the five items collected at the end,
because two executed rows wrote the claim that rests on it.

**5. Three different definitions of a dead end.** The owner, `what-i-want-2026-07-16.md:152`: "Every
non-terminal state has an exit … A state you can enter but never leave is rejected".
`compiler-and-runtime-design.md:147` agrees — "detects dead-ends (every non-terminal state must have an
outgoing path)". `graph-analyzer.md:451` does not:

> 4. DeadEndStates = { s ∈ ReachableStates : s ∉ TerminalStates AND s ∉ reverseVisited }

Two non-terminal states that transition into each other and nowhere else satisfy the first test and fail
the second. Both texts stand at HEAD, and `graph-analyzer.md:457` states the owner's definition as the
rationale for the algorithm's severity, so one sentence holds both readings.

**No row covers it.** The four dead-end rows are `C7`, `N45`, `N46` and `N49`, and all four are about
**severity**: `C7` quotes "**Severity:** Warning, not error" and resolves to Error; `N45` quotes
"emits `DeadEndState` (**Warning**, code 108)". Which states the diagnostic applies to is not their
subject. Both are executed — `graph-analyzer.md:451` now reads `Emit Diagnostic(DeadEndState, Error)` and
`:457` reads "**Severity:** Error" — and the definition disagreement sits untouched underneath the
severity correction.

**Why it was missed.** The search patterns were written from the severity words. Nothing greps an
algorithm.

**7. `on <Event> ensure` runs at ingress, where the owner routes relational event conditions to guards.**
The owner, `:143`: "For an arg, that's its modifier-rules; anything relational an event needs lives in
its guards." Canon has a third thing at ingress. `evaluator.md:1369–1373` at HEAD:

> ```
>     // on <event> ensures — event preconditions (ConstraintKind.EventPrecondition),
>     // declared against the event's arguments and evaluated before the operation runs.
>     if (index.OnEvent.TryGetValue(@event, out var eventConstraints))
> ```

and `evaluator.md:2130`, the ingress matrix: "`Fire` | no | yes | the constraints declared on each
supplied argument, **plus `on <event>`**"; `runtime-api.md:883`: "Arg ensures (`on <Event> ensure`) |
Evaluated at ingress if initial event declared".

**No row covers it**, and the register's one mention of `EventPrecondition` is inside `N93`'s quotation of
the catalog's activation buckets. What makes this worth reading rather than filing is `S4`, executed in
`5e853c0e`, whose reason cell says flatly: "**Rules and ensures are not evaluated at runtime under the
want.**" Canon at HEAD evaluates event ensures at ingress, and says so in three places. Whether that is a
contradiction of `S4` or a case `S4`'s sentence was never meant to cover is exactly what the cold read
declined to decide, and it is not decided here either — the owner's document never uses the word
`ensure` at all, which is the next finding.

**Why it was missed.** `S4`'s pattern is `grep -rn "check rules and ensures" $CANON` — one deleted
sentence in the specification. Nothing looked for where ensures are still evaluated.

### Part 2 — places canon is silent on something the owner requires

**The `ensure` construct is absent from the owner's document.** `grep -i ensure` over
`what-i-want-2026-07-16.md` returns nothing, while `runtime-api.md:421` lists
`ConstraintKind { Invariant, StateResident, StateEntry, StateExit, EventPrecondition }`.

**Not in the register, and structurally could not be.** Every row shape the register has runs from the
want document towards canon. A construct canon carries that the want document never mentions produces no
row of any kind — not a contradiction row, because canon does not contradict a silence; not an
`ADDITION`, because the want document requires nothing.

**Where the certificate checker's compiler stage sits.** The requirement is written three times
(`soundness-and-coverage.md:159` and `:55–58`, `proof-engine.md:2534`). `docs/compiler/README.md:17` calls
the pipeline six stages and lists six, none of them a checker.

**No row covers it.** `C5` removed the deferral — its resolution is "The checker is in scope, including
its runtime load-time half" — and `N66` records that `soundness-and-coverage.md` § 5a (certificate format)
is a stub routed to phases 4 and 5. Neither asks where the compiler-side stage goes. This is the one item
the cold read reported as having no owner anywhere, and it was given a home on 2026-08-26 in
`docs/compiler/README.md` § Open questions.

**Why it was missed.** `C5`'s pattern is `grep -rn "no live re-checker" $CANON`. A stage that has never
been written about produces no text, and an absence search has to be aimed at a specific phrase to find
it — `N52`, `N53`, `N68` and `N69` are all absence searches aimed at a phrase from the want document, and
"where the stage sits" is not a phrase the want document contains.

**What artefact carries the certificate out of the compiler.** The owner, `:199`: "Alongside the compiled
definition, the compiler emits a **certificate**". `compiler-and-runtime-design.md:687` lists
`Compilation`'s seven members and none is a certificate; `precept-builder.md:865` names the question in
canon's own voice.

**No row covers it** — the string `Compilation` does not appear in the register at all. The adjacent row
is `N68`, the `ADDITION` that found zero occurrences of "certificat" anywhere under `docs/runtime/` and
required the load-time gate be written into `runtime-api.md`. It was executed in `c3a9be21`, which added
§ *Definition Load and the Certificate Gate*. Writing the gate raised the question of what the gate is
handed, and that question is now parked in `docs/runtime/README.md` § Open questions — but it entered
canon after the register was built and never became a row.

### Part 3 — canonical documents contradicting each other

Twelve of the eighteen intra-canon items have no row of any kind. They are listed compactly because they
are ordinary defects rather than design questions; each was re-checked at HEAD.

**2. `UpdateOutcome`'s variant names do not overlap between two accounts.**
`compiler-and-runtime-design.md:881` gives `FieldWriteCommitted`, `AccessDenied`, `InvalidInput`, `Fault`;
`result-types.md:136–142` and `evaluator.md:355–360` give `Updated`, `ConstraintsFailed`,
`FieldNotEditable`, `InvalidFields`. Not one name is shared. *(Missed because no want line is behind it —
the register touched `compiler-and-runtime-design.md:883` through `N15` and `N20`, both of which were
removing the three `*ConstraintsFailed` names, not reading the rest of the row.)*

**4. Restore either recomputes computed fields or leaves them stale.** Recompute:
`compiler-and-runtime-design.md:842` ("it does recompute computed fields"), `:912`, `runtime-api.md:277`,
`evaluator.md:2138`. Stale: `evaluator.md:1975–1977` — "those values may be stale … `FromJson` is a
hydration primitive, not a recomputation pipeline. Freshness enforcement is a caller concern."
*(Missed because § 2's rows are all about whether restore re-governs, not about what it recomputes.)*

**5. `evaluator.md`'s inspection summary table still shows the constraint pass the rest of the document
says was removed.** At HEAD, `evaluator.md:1933–1936`:

> | Mode | Guard | Actions | Constraints | Disposition |
> | Commit | Evaluate | Execute | Evaluate | Fail/succeed |
> | Inspect | Evaluate | Execute (on copy) | Evaluate | Report all |

The same document denies the pass at `:694`, `:717`, `:912`, `:1680`, `:2034` and `:2126`. **This is a
survivor of the register's largest family**, and it is the third of the five items collected at the end.
*(Missed for a measurable reason: § 1.0's family test was run in Python against the canonical set at HEAD
for this diff and returns 35 lines, ten of them in `evaluator.md` — `:141`, `:387`, `:411`, `:603`,
`:717`, `:784`, `:1680`, `:1750`, `:2034`, `:2128` — and none in the 1900s. The pattern needs a
constraint noun and an evaluation verb near each other; this table puts the noun in the header row and the
verb in the body rows.)*

**8. `precept-builder.md` numbers its own passes two ways.** The list at `:110–117` puts the constraint
plan pass fourth and the execution plan pass fifth; the diagram at `:267–279`, the dependency table at
`:300–301`, `:306` ("pass numbers now reflect execution order") and the section headings at `:420` and
`:505` all transpose them. *(Missed: no want line, and `N22`'s work in this document was scoped to Pass 6.)*

**9. `FieldDescriptor` has two different shapes, and a property both documents say the evaluator needs is
in neither.** `descriptor-types.md:78–85` declares seven members, `precept-builder.md:322–331` eight, and
neither carries `AccessModes`, which `evaluator.md:1333` reads and `:1347`, `:2016` and `:2030` all
discuss. *(Missed: `N26` was the only row in `descriptor-types.md` and it was removing
`FaultSiteDescriptor`.)* **One thing here has moved since the read**: the cold read flagged an uncommitted
edit that would have deleted the only line recording the missing property. Commit `7773112e` did the
opposite — the record now sits at `descriptor-types.md:109`, beside the printed type: "**The
`FieldDescriptor` printed above is missing `AccessModes`, and it should not be.**" The two shapes still
disagree.

**10. `Compilation` has two memberships.** `compiler-and-runtime-design.md:687` lists seven members
including `ConstructManifest`; `precept-builder.md:181–190` declares five, omits `ConstructManifest`, and
renames three; `:193` then says "The builder reads all **three** analysis artifacts" about a record it has
just printed with four. *(Missed: no want line, and no row names `Compilation`.)*

**12. `diagnostic-system.md` lists 147 codes, claims 164, and calls the list closed.** Recounted at HEAD
for this diff: the enum block at `:216–416` declares **147** members with no duplicate value, the highest
value is **164**, and seventeen numbers in the range have no member (112–118, 120–126, 130–132). The
document states 164 at `:419`, `:448` and `:457`, and calls the set closed at `:421`, `:57`, `:67` and
`:96`. *(Missed: arithmetic, not phrasing.)*

**13. Four codes are discussed by name and are not in the list** — PRE0113 (`:203`), PRE0114 (`:201`,
`:203`, `:205`), PRE0115 (`:419`), PRE0116 (`:752`). *(Same reason.)*

**14. `DiagnosticCategory` is both owned and rejected by the same document.** Owned at `:71`, `:437`,
`:473`, `:476–485`, `:800` and in five switch arms at `:452–456`. Rejected at `:772`: "**Category field
orthogonal to stage:** Rejected: `DiagnosticStage` is the only classification axis needed at DSL scale."
*(Same reason.)*

**15. The Roslyn rule range is given twice, differently.** `:81` — "Roslyn rules
PRECEPT0001–PRECEPT0031". `:646` — "24 Roslyn rules (PRECEPT0001–PRECEPT0024)." *(Same reason.)*

**16. The diagnostic count differs between documents.** `runtime-api.md:672` — "132 diagnostic codes";
`diagnostic-system.md:419` — "164 active diagnostic codes". *(Same reason.)*

**17. `StructuralSinkState` — the diagnostic document assigns a code to the graph analyzer that the graph
analyzer's own document does not know exists.** `diagnostic-system.md:321` declares
`StructuralSinkState = 119` under the Graph header and names it at `:187` and `:193`;
`grep -rn StructuralSinkState docs/compiler/graph-analyzer.md` returns nothing. **Adjacency worth
stating**: `N49` quotes `diagnostic-system.md:191`, the tracked-drift sentence, which lists
`StructuralSinkState` among six flipped codes — so the identifier passed through the register, inside a
row about severity, and the fact that one of the six is unknown to the document that would emit it was not
what that row was reading for.

**18. `graph-analyzer.md` § 4's forwarding-fact table omits a subtype `proof-engine.md` says is in the
same hierarchy.** `graph-analyzer.md:261` calls the table below it "the current contract consumed by the
proof engine" and lists five subtypes; `proof-engine.md:449` says `UnreachableRowFact` is a sixth,
produced by the proof engine into the same union. *(Missed: found by the cold read only while verifying
Part 1 finding 3; no row and no pattern goes near the forwarding-fact hierarchy.)*

---

## The findings already in the register

### Same substance, same thing, and the row's own state matches what the cold read found

**Whether the premise list replaces the constraint plan index** → rows **`N69`**, **`N10`** and **`N80`**.
`N69` is an `ADDITION` quoting `want:210` verbatim — "the premise list **is** the runtime's validation
surface … derived from the proof, never maintained in parallel" — and its reason cell says "Canon's
`ConstraintPlanIndex` (N10) is exactly the parallel structure this forbids." `N69` is executed
(`c3a9be21`); `N10` is **not executed**, routed to `N80`, "**Waiting on Shane**". The cold read found
`runtime-api.md:258` stating the requirement, `precept-builder.md:505–516` describing the index, and
`precept-builder.md:812`, `:866` and `catalog-system.md:1444` all recording that nothing reconciles them.
That is exactly the state `N80` says the row is in. **No discrepancy.**

**Which type carries what inspection reports** → rows **`N70`**, **`N79`** and **`N8`**. `N70` is the
`ADDITION` for `want:211`, recorded as **not executed** with the reason: "`c3a9be21` did not add the
statement; it recorded the gap as open, in canon's own voice, at `result-types.md:250` and
`evaluator.md:912` … This row stays open behind `N79`." The cold read quoted those two exact lines.
**No discrepancy.**

**`result-types.md` describes a post-mutation evaluation the same document says does not exist** (Part 3
item 6) → the same family. § 1.0's triage table lists `result-types.md:173`, `:210`, `:227`, `:248`,
`runtime-api.md:414`, `evaluator.md:388` and `:412` under the heading "**Rides on `N79`** — the three
`ConstraintResult` arrays", with the note that the descriptions "move when `N79` is decided, not before".
The cold read reached `:227` and `:248` and quoted `:250`'s open note against them. **No discrepancy** —
though **one half of this item is new**: `result-types.md:229` documents `HypotheticalResult` as a member
of `UpdateInspection`, and the record printed at `:220–223` declares only `Fields`, `Constraints` and
`Events`. That is a different defect at the same site, and no row or triage line covers it.

**The three record types that should come out of the code** (Part 3 item 11) → the § 3 preamble and rows
**`R9`**, **`N26`** and **`C1`**. All three rows are executed in `c9193ec8`, and all three had their
patterns rewritten on 2026-07-28 precisely because the identifiers survive in the disclosures: `N26`'s
cell reads "The bare identifier returns **5** at HEAD and all five are the disclosures that
`Descriptors.cs` still declares the record and that its removal has not been made". The cold read found
those five disclosures and (uniquely in the read) checked `src/` and confirmed the types are declared.
**No discrepancy** — the rows and the read agree that the documents are correct and the code has not
changed.

### In the register in different words, or inside a row written about something else

**Who owns schema evolution on restore** (Part 1 finding 6) → adjacent to **`N16`** and **`N21`**, and
covered by neither. `N16`'s site is `compiler-and-runtime-design.md:718` and its quoted canonical text is
"`Restore` itself does not reject them … **but the next operation's post-mutation sweep will**". Its
pattern is `grep -rn "the next operation's post-mutation sweep will" $CANON_WIDE` — one clause of a long
paragraph. The correction landed (`25894ae3`); at HEAD that paragraph reads "no later operation re-governs
them either". **What the same paragraph still says, and what no row ever quoted**, is:

> **This is a known gap — definition migration is out of scope for v1.** … automated migration is
> deferred. Host applications that need to handle definition evolution must manage the migration
> externally.

Against `runtime-api.md:277`, `runtime-api.md:906` and `evaluator.md:1757`, which all say the same thing
the other way: "Handling data written under an older definition is the post-MVP job of the restore path
itself, through migration." `N21` did correct a migration sentence — but in the specification
(`precept-language-spec.md:1946`), about the ordering of migration against constraint evaluation. **The
wording difference is the whole story**: one row's pattern quoted a clause about the sweep, another's
quoted a clause about constraint evaluation, and the sentence between them assigning the work to the host
matched neither. The fourth of the five items collected at the end.

**`EventOutcome` described three ways, and a variant used but never declared** (Part 3 item 1) → row
**`N4`** sits on the exact line. `N4`'s site is `runtime-api.md:168` and its canonical text is
"\| `Created` \| … \| A mutation row matched and all post-mutation constraints passed. \|", resolved to
"construction succeeds when the construction row executes". Executed in `c3a9be21`; at HEAD `:168` reads
"A mutation row matched and its action chain ran to completion." **The row's subject was the sweep clause
in the cell, not the variant in it.** At HEAD `runtime-api.md:106` says nine variants,
`result-types.md:47` and `:56` say eight, the declaration at `:95–104` and the table at `:109–115` have
seven, and `Created` — used at `runtime-api.md:145`, `:160`, `:168`, `:175`, `:182`, `:245`, `:246`,
`:890` and `:891` — appears in no declaration block in any runtime document.

**`ConstraintsFailed` declared in one document and declared impossible in another** (Part 3 item 3) →
rows **`S7`** and **`N15`**, both executed, and **the contradiction is a product of the two of them**.
`S7` is a `RE-SCOPE` row and its pattern cell states the intent plainly: "`grep -rn "ConstraintsFailed"
$CANON` returns 15 and always will: `UpdateOutcome.ConstraintsFailed` survives on purpose as an ingress
failure, labelled as one at `result-types.md:148` and `:152`." `N15` removed three `*ConstraintsFailed`
outcomes from `compiler-and-runtime-design.md`, and the text `25894ae3` wrote in their place is
`:863` at HEAD:

> There is no `*ConstraintsFailed` outcome in any of these families, and **there cannot be one**: a
> definition only compiles if every handler was proven to preserve every rule…

against `result-types.md:139`, which still declares
`public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations) : UpdateOutcome;`,
`:148` which describes when it is produced, and `evaluator.md:743` which returns it. The second of the
five items collected at the end.

**`result-types.md` says faults throw in one place and never throw in two others** (Part 3 item 7) → row
**`N9`** is on the same comparator table, one row away. `N9`'s canonical text is "\| OPA `deny[msg]` \|
Collect-all violation set \| ConstraintResult lists, not short-circuit. \|" at `result-types.md:369`. At
HEAD, `:369` is the **Temporal Workflow** row — "Faults throw (not return)" — and the OPA row `N9`
corrected has moved to `:375`. So the register reached this table, corrected one row of it for a want
reason, and the row below it saying faults throw, against `:51` ("never thrown") and `:72`, was not what
the pass was reading for.

### In the register, considered and explicitly excluded — and the exclusion's own condition has now been met

**Time-referencing rules.** The cold read found that the owner deliberately left this open at
`what-i-want-2026-07-16.md:219` ("Do rules that reference the current moment exist at all … Deliberately
left open"), and that the language documents have already answered the first half: `temporal-type-system.md:560–562`
declares `now()`, `:577–578` gives `rule now() - IncidentAt <= '72 hours'` as a worked usage pattern, and
`:568` states the consequence under a locked decision number. Meanwhile `grep -n "now()"` over
`docs/compiler/*.md`, `docs/runtime/*.md` and `docs/compiler-and-runtime-design.md` returns zero.

**The register holds this, in § 14.4 *Not contradictions*:**

> | want:219, want:220 | Questions the want document itself declares open. Canon saying something about
> either is canon answering an open question, which goes to "Waiting on Shane", not into this register. |

That is a precise rule and the cold read has now met its condition. Canon **does** say something about
want:219 — it declares the function, gives a rule using it as an example, and locks a decision about its
determinism. Under § 14.4's own words that makes it a Waiting-on-Shane item, and it is not on the plan's
§ 4 list. The fifth of the five items collected at the end.

**Why the search did not turn this up.** It is not a search failure at all. The exclusion was written on
the reasoning that an open question in the want document cannot be contradicted, which is true of a
silence and false the moment canon speaks — and nobody looked at whether canon spoke, because the
exclusion closed the question before that check.

---

## Five things worth reading before anyone signs

Collected rather than buried, with the reason each one matters. **None of them is resolved here and none
should be read as a recommendation about the exit condition.**

1. **Two executed rows wrote a sentence that the compiler has no vocabulary to state.** `S2`'s reason
   cell — "Every handler was proven to preserve every rule at compile time, so there is no post-mutation
   pass/fail decision left to make" — and `S9`'s — "An invalid configuration never persists because every
   handler is proven to preserve every rule" — are the justification for removing the runtime's last rule
   check. That claim now stands in canon at `proof-engine.md:2170` and
   `compiler-and-runtime-design.md:151`. `proof-engine.md:489` lists thirteen obligation kinds and
   neither preservation nor establishment is one, and `proof-engine.md:2614` admits in canon's own voice
   that the pre-state premise and the symmetric write-site enumeration are "gaps against the target".
   Whether that is a hole to fill in phase 5 or a sentence that overstates what was proven, the two are
   currently reconciled nowhere.
2. **Two executed rows now contradict each other.** `S7` deliberately kept `UpdateOutcome.ConstraintsFailed`
   as an ingress failure; `N15`'s executed text says no such outcome exists or can exist. Both rows are
   marked executed with commit hashes, both did what their resolution column said, and the result is a
   flat disagreement between `result-types.md:139`/`:148`/`:152` and
   `compiler-and-runtime-design.md:863` at HEAD. Condition 3 cannot see this: each row's pattern passes.
3. **The largest family in the register left a table stating the removed model.** `evaluator.md:1933–1936`
   shows `Constraints: Evaluate` for both Commit and Inspect. Nineteen rows and four passes worked on this
   family, § 1.0 exists specifically to catch exactly this class of survivor, and the family test — run
   for this diff and returning 35 lines — cannot reach a table that splits the noun from the verb across
   rows.
4. **A paragraph corrected by `N16` still assigns schema migration to the host**, while three runtime
   documents assign it to the restore path post-MVP. Both wordings are current at HEAD, and the owner's
   line (`:141`) matches the runtime documents.
5. **§ 14.4's exclusion of want:219 has had its own condition met.** Canon speaks about time-referencing
   rules in `temporal-type-system.md` and is silent about them in every compiler and runtime document. By
   the register's own rule that is a Waiting-on-Shane item, and it is not on the list.

---

## The reverse check — register rows the cold read did not reach

The cold read is one pass by one reader; the register is the product of a written procedure and five
passes. The size of what the read did not reach is worth knowing before anyone treats a single cold read
as a substitute for either.

**Counted mechanically at HEAD**: the register carries **150 identifiers and 145 rows**, and **90 of
those rows name a runtime, compiler or architecture document in their Site column** (`docs/runtime/*`,
`docs/compiler/*`, `docs/compiler-and-runtime-design.md`) — that count excludes the `ADDITION` rows, whose
Site cell names an absence rather than a file, so the true footprint in the read's territory is somewhat
larger. The cold read's 28 findings touch the subject matter of about **18** of them.

**For executed rows this is the expected result and not a measure of anything.** An executed row's defect
is gone from the document; a cold reader finding it again would mean the correction had been undone. The
read found no such regression, with the two exceptions in the list above — and both of those are
corrections that landed as written, not corrections that came undone.

**The number that carries information is the open rows.** The register records **12 identifiers as not
executed**: `N8`, `N10`, `N66`, `N70`, `N79`, `N80`, `N81`, `N96`, `N100`, `R13`, and `P5`/`P6` (which are
recorded as never having been rows). Of those:

- **Reached independently by the cold read: 5** — `N8`, `N10`, `N70`, `N79`, `N80`, all through the two
  findings about the premise list and about what inspection reports.
- **Adjacent but not the same: 1** — `N81` ("The induction itself has never been argued") sits next to the
  read's heaviest finding, which is that no obligation kind exists for the induction's step. Related, not
  identical.
- **Not reached: 4 in the read's territory** — `N66` (the certificate-format section is a stub routed to
  phases 4 and 5), `N100` (the `ProofStrategy` enum numbers every strategy from 6 upward differently from
  the section headings in the same document), `R13` (a quoting rule about three `FaultCode` members,
  ruled against and disposed of without an edit), and `N96` (the editable-write premise), whose canon
  description is now history because the owner ruled the narrowing out on 2026-08-26 — which the cold read
  did independently confirm, from the other direction, by finding no trace of it left.
- **Out of scope: 2** — `P5` and `P6` are philosophy sites, which the read did not cover.

**What this says.** One reader with no patterns found nineteen things the procedure had not, and missed
four open questions the procedure holds — including one, `N100`, that a reader would have to be counting
enum values against section headings to notice. The two shapes catch different things and neither
subsumes the other. That is the case for having run the cold read, and equally the case against treating
one as a replacement for the register.

---

## What this diff establishes, and what it does not

**Establishes.** Every one of the cold read's 28 live findings has been matched against the register one
at a time. Nineteen are in no row. Four are rows with the same substance and the row's recorded state
matches what the document says at HEAD. Four are inside rows written about something else, and three of
those four are executed rows whose finding still reproduces at HEAD — two of them *because* of how the
rows executed rather than in spite of it. One is in § 14.4 as an explicit exclusion whose stated
condition has since been met. Every claim above was re-checked against the document at HEAD `eeda4c91`
rather than taken from the cold read's own text; where a line number moved since the read was written
against `621bde2b`, the HEAD number is the one given.

**Does not establish.** Whether nineteen new findings is a lot or a little for a register of 145 rows —
there is no baseline to say. Whether any of the nineteen deserves a row, which is a question about what
phase 2 is for and not a question this diff can answer. Whether the register is complete: nothing can
establish that, and the register says so itself under *Honest note on completeness*. And whether exit
condition 4 is met — the condition asks for the search procedure re-run and the output diffed, and what
happened instead was a differently shaped read, now diffed. Both halves have produced something to read.
Whether that is what the condition was asking for is the owner's call, and it has not been made here.

---

## Cross-references

- The read this diffs: [`cold-read-findings.md`](cold-read-findings.md)
- The register: [`contradiction-register.md`](contradiction-register.md), with the family test at § 1.0 and
  the exclusions at § 14
- The procedure the condition names: [`search-procedure.md`](search-procedure.md)
- The condition itself: [`compiler-readiness-plan.md`](../compiler-readiness-plan.md) § *Finish line*,
  condition 4
- The owner's document: [`what-i-want-2026-07-16.md`](../../Working/what-i-want-2026-07-16.md)
