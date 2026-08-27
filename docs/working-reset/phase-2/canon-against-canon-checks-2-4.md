# Canon against canon, checks 2 to 4

**Run 2026-08-27** against HEAD `85206254`. Companion to
[`canon-against-canon-check-1-declared-types.md`](canon-against-canon-check-1-declared-types.md), which
explains why these are mechanical checks rather than greps or a reader.

---

## Check 2 — counts a document states, against the set and against each other

[`checks/stated-counts.py`](checks/stated-counts.py). Two reports. **A** de-camel-cases every declared
enum name into the phrase prose uses for it — `DiagnosticCode` becomes *diagnostic code* — and compares
every number canon states for that phrase against the enum's actual member count. **B** takes a list of
countable nouns and reports each one canon gives more than one number.

### New findings

**1. `ConstraintKind` declares five members and the specification names four kinds, in a different
taxonomy.** `runtime-api.md:421` declares `ConstraintKind { Invariant, StateResident, StateEntry,
StateExit, EventPrecondition }`. `precept-language-spec.md:1961` opens "The four constraint kinds have
distinct attribution" and lists event ensures, rules, state ensures, and transition rejections. The two
lists are not the same list with one missing: the specification folds the enum's three state anchors into
a single "state ensures", and it adds transition rejections, which is not a `ConstraintKind` at all. Both
sentences present as the enumeration.

**2. Fourteen catalogs against fifteen.** `precept-grammar.md:63` and `:808` both say "The 14 catalogs";
`catalog-system.md:129` says "Fifteen catalogs in three groups (12 language-definition + 2 failure-mode +
1 tooling-adjacent)". `CLAUDE.md` names `catalog-system.md` as the canonical inventory and instructs other
documents to reference catalogs by name rather than by count, precisely so this cannot happen. It
happened.

**3. `DiagnosticStage` declares seven values and is described as five.** `diagnostic-system.md:144`
declares `Lex, Parse, Type, Graph, Proof, Bind, Tooling`. `catalog-system.md:169` — "`DiagnosticStage` |
5 values, classification axis for diagnostics, no per-member metadata".

### Findings it reproduced, which is how the check earns trust

`DiagnosticCode` declares **147** members; `diagnostic-system.md:419` states **164 active diagnostic
codes**; `runtime-api.md:672` states **132**. And `EventOutcome` is given **8 variants** at
`result-types.md:47` and `:56` and **9** at `runtime-api.md:106`. Both were already cold-read findings.

### What it discards, and why report B has to be read rather than trusted

A noun legitimately carries different numbers in different scopes — "the graph analyzer reads from two
catalogs" is about that stage, not about the inventory. Report B lists every noun with more than one
number and nothing decides for you which are scoped. Report A is tighter, because an enum's member count
is a fact and not a scope.

---

## Check 3 — identifiers used as though declared, that nothing declares

[`checks/undeclared-identifiers.py`](checks/undeclared-identifiers.py). Builds the set of everything canon
declares in a C# fence — type names, enum members, record members — then reports PascalCase identifiers
used in a declaration-shaped position that are in none of them.

**This check produces a different and weaker class of finding, and it is worth saying so plainly.** What
it finds is *completeness gaps*, not disagreements: `SlotLayout.ComputedSlots` is read six times in
`evaluator.md` and `SlotLayout` is never printed with that member; `DispatchIndex.Rows` the same;
`ConstructSlotKind.GuardClause` and `.ComputeExpression` are matched on in two documents and the enum is
never declared with them. Nothing contradicts anything. A reader is told a member exists and never shown
the type that carries it.

**One correction the check needed before it was worth reading**: its first run treated every fenced block
as C#, so an author's field and event names in `.precept` examples — `Floor`, `SKU`, `ReviewerName` —
came back as undeclared identifiers. It now reads `csharp` fences only.

Kept, and not turned into rows, because a completeness gap is not two documents disagreeing. It belongs to
whoever writes the corrected statement of the runtime.

---

## Check 4 — a diagnostic code against the stage document that would emit it

[`checks/code-stage-attribution.py`](checks/code-stage-attribution.py). The `DiagnosticCode` enum is
grouped by banner comments naming the emitting stage, and every stage has a canonical document.
`diagnostic-system.md:160` states the contract this rests on: "Each code is owned by exactly one producing
component, and the meta `Stage` names that component." So a code its own stage document has never heard of
is two canonical documents disagreeing about who emits it.

**The ratio is what makes a gap mean anything**, and the check prints it per stage:

| Stage | Codes named in the stage's own document |
|---|---|
| lex | 8 of 8 |
| namebinder | 1 of 1 |
| proof | 9 of 9 |
| runtime | 2 of 2 |
| tooling | 1 of 1 |
| **graph** | **7 of 8** |
| parse | 0 of 9 |
| type | 10 of 109 |

A stage naming every code it emits and missing one is a finding. A stage naming almost none never
enumerated them, and its misses say nothing — `parser.md` and `type-checker.md` are both in that group,
which is why 109 of 147 "missing" is not 109 defects.

**One finding, already known**: `StructuralSinkState = 119`, declared under the Graph banner at
`diagnostic-system.md:321`, appears nowhere in `graph-analyzer.md`. **And nothing new** — of the five
stages whose documents do enumerate, four are complete. That is worth recording: the check confirms the
known defect is the only one of its kind.

**A false positive this check produced and how it was caught**, because the next person to run it should
know. It first reported `UndeclaredArg = 107` against the proof engine. The code sits under a `NameBinder`
banner, which was not in the stage map, and the banner parser left the previous stage in place instead of
failing — so every code under an unmapped banner was silently attributed to whichever stage came before
it. `name-binder.md` names `UndeclaredArg` twice. The map now carries the name binder, an unrecognised
banner clears the stage rather than inheriting it, and the check prints any banner it could not map.

---

## What the four checks leave

Across all four checks, **nineteen genuine canon-against-canon findings**. **Five were already known**,
and reproducing them is the evidence the checks work: `FieldDescriptor`'s two shapes, 147 codes stated as
164, 164 stated as 132 in another document, `EventOutcome` given eight variants and nine, and
`StructuralSinkState`. **Fourteen are new** — eleven from check 1 and three from check 2.

None of the fourteen can be resolved by choosing a wording. Each needs somebody to say which statement is
right, which makes them rows whose resolution needs a decision rather than rows a correction pass
executes. Four of check 1's are one decision rather than four: what a descriptor carries to point at
source, answered `SourceSpan Source` in one document and `int SourceLine` in the other, in five places.

**What none of this establishes** is that the canonical documents hold no other contradictions. Four
shapes of fact are now checked mechanically. Everything canon states in prose and nowhere in a code fence
or a number is still only as covered as a reader made it.
