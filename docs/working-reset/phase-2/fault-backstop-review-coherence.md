# Review of the fault-backstop pass — whether each document still holds together

**Run** 2026-07-28, read-only, against the uncommitted fault-backstop diff. Nothing was edited by this review. Findings are the reviewer's; where one is later judged wrong, that judgement belongs in the fix pass record rather than here.

---

## Coverage

I read `git status`, the full diff (19 files), the decision sheet, the pass record, and then each edited document in its current state. I checked all six categories the brief names; what I found clean in each is stated at the end. Findings ranked most severe first.

---

# BLOCKING

### 1. `docs/compiler/proof-engine.md:101` — the document's own Overview still says the ledger carries fault-site links

> "It produces a `ProofLedger` — the complete obligation inventory with dispositions, **fault-site links**, constraint influence analysis, and initial-state satisfiability results."

The `FaultSiteLinks` field was deleted from the `ProofLedger` record 150 lines below, from the pipeline diagram, from the ledger-consumption table and from the `Prove` sketch. This sentence is the first description of the ledger a reader meets and it names a field the record no longer has. The pass's own grep (`FaultSiteLink`, count 0) cannot see it — the text is hyphenated and spaced, so the pattern misses it. This is the clearest instance of removing the machinery and leaving the summary.

### 2. `docs/runtime/runtime-api.md:83` — "fault site taxonomy" survives in Responsibilities, while the same phrase was fixed 360 lines later

> "**Does NOT OWN:** … fault system (`Fault` and **fault site taxonomy**)."

The Deliberate Exclusions bullet in this same file was rewritten from "`Fault` and fault site taxonomy" to "`Fault` and the fault classes". One of the two copies was corrected. "Fault site" is a concept that no longer exists anywhere in canon.

### 3. `docs/runtime/README.md:14` — the area map still describes `fault-system.md` in the removed terms

> "| fault-system.md | **Runtime fault codes and classification — the runtime mirror of the compiler's diagnostic system** | Draft |"

Line 37 of this same file was rewritten precisely to say the mirroring is a build-time pairing "not a pair of things happening at compile time and at run time". The table row three lines above the reading order says the opposite. Per `CLAUDE.md` the per-area README is the canonical map — a reader routing from it gets the old model.

### 4. `docs/glossary.md:87` — the reverse cross-reference was not followed

The **Fault** entry (:113) was rewritten. The **Diagnostic** entry still ends: "The **runtime mirror** is **Fault**." Two entries in one file, one changed. This is the both-directions check: the pass found every file that names `Fault` but not every file that names it *from the other side*.

### 5. `docs/compiler/proof-engine.md:18` — a Non-Negotiable Rule still names the removed output

> "**Never guess** — the engine proves an obligation, rejects it with **diagnostics/fault linkage**, or leaves it explicitly unresolved…"

"Fault linkage" is `FaultSiteLink` production, which this pass removed from the In-Scope table, the ledger, the emission code and the builder contract. It survives in the callout block the document tells implementers to read before doing anything.

### 6. `docs/compiler/diagnostic-system.md:72` — OWNS still frames the chain as running from runtime to compiler

> "- The `FaultCode` enum and `[StaticallyPreventable]` attribute — **the runtime-to-compiler linkage**"

The pass deleted the "Evaluator fails →" first link from this document's chain diagram, and repointed two cross-references away from a heading named "FaultCode — The Runtime-to-Compiler Chain" on the grounds that it does not exist. The phrase that heading came from is still in the ownership list.

### 7. Three documents now state a false fact about the code, and one live consumer was never inventoried

`descriptor-types.md:8` (Implementation state, **"Implemented"**) and `:195` ("all **4** descriptor sealed records") now claim `src/Precept/Runtime/Descriptors.cs` declares four records. It declares five — `FaultSiteDescriptor` is at `Descriptors.cs:60`. `precept-builder.md:941` and `evaluator.md:2004`, `:2140` carry the same four-item list.

Separately: `src/Precept/Pipeline/ProofLedger.cs` still declares `FaultSiteLink`, `FaultSiteAnnotation` and the `FaultSiteLinks` field, `ProofEngine.Diagnostics.cs:452` still builds them, and **`tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:292` reads `compilation.Proof.FaultSiteLinks`** to find the proof obligation behind a diagnostic squiggle — a behaviour `docs/tooling/language-server.md:607` documents. The pass record's § 8 discloses that no MCP projection inventory was taken; it does not mention the language server, and the decision sheet's § 5 item 5 ("no document names another consumer") is true of documents but there is a real one in the tooling code. A `Status` field saying "Implemented" is one `CLAUDE.md` tells readers to trust; two of these are now wrong.

---

# WORTH FIXING

### 8. `docs/runtime/fault-system.md:11` — one document is now both Upstream and Downstream

Status block: Upstream = `diagnostic-system.md`; Downstream = `result-types.md` **and `diagnostic-system.md`**. The added Downstream half ("the build-time pairing…") is true but it is the same relation already stated as Upstream. In a 16-section-template Status table that is contradictory routing metadata.

### 9. `docs/runtime/fault-system.md:43` — the Overview table header contradicts the Overview prose

The prose two lines above was rewritten to say the fault system names classes the compiler prevents and that "the pairing it carries is checked when the project is built". The table immediately below is still headed:

> `| Compiler (pipeline) | Runtime (evaluator) |`

Every row under it is now compiler-side vocabulary.

### 10. `docs/runtime/fault-system.md:304` — Q2 still runs on the removed scenario

> "**When a fault occurs**, what context is useful to a consumer? … **Input values:** what were the concrete values at fault time (**`rate = 0`**)?"

One bullet (the DiagnosticCode linkage) was removed from this list; the framing sentence and the divide-by-zero example were not. `rate = 0` is the exact evaluation-time divisor test `want:165` removes and that the same file's worked example was re-authored to eliminate. Q1 (`:294`) and `:326` ("Informational or warning-level **evaluator faults**") have the same residue.

### 11. `docs/runtime/evaluator.md:434` — `Fail` is now orphaned under the wrong banner

The `// ── Fault Backstop ──` banner was removed and `Fail` left in place. The `Evaluator` class block groups members by banner comment; `Fail` now sits after `InspectUpdate` with a blank line, reading as a fourth inspection operation:

```
    // ── Inspection Operations ─────────────────────
    internal static EventInspection InspectFire(…);
    internal static UpdateInspection InspectUpdate(…);

    internal static Fault Fail(FaultCode code, params object?[] args);
```

Either give it a neutral banner or move it next to the § Fault Production declaration it duplicates.

### 12. `docs/compiler/proof-engine.md:2369` — "What crosses:" under a decision that says nothing crosses

Decision 2 now opens "The `ProofLedger` is a compile-time artifact. **Nothing from it crosses into runtime.**" It is then followed by a heading **"What crosses:"** with exactly one bullet, and that bullet is parenthetically disclaimed as not being the proof engine's: "Constraint execution plans (from evaluator, not proof engine)". A one-item list whose only item is excluded by the sentence above it should fold into the prose.

### 13. `docs/compiler/proof-engine.md:1849` — § Proof/Fault Chain no longer contains a fault

The diagram now runs `catalog metadata → ProofRequirement → ProofObligation → {Proved: Nothing further | Unresolved: DiagnosticCode → Definition refused}`. There is no `FaultCode` node left in it. The heading, its Contents entry at `:40`, and the lead-in ("connects a catalog-declared requirement to the diagnostic that refuses the definition") describe a chain that is no longer a proof/fault chain. The pass kept the heading deliberately to avoid orphaning `:40` — the right fix is to rename both together, not to keep a name the content contradicts.

### 14. `docs/runtime/precept-builder.md:676` — the new Input Contract condition names an input the document's own `Compilation` does not carry

> "- The definition's proof certificate must verify — see § The Certificate Gate."

`Compilation` is declared at `:179` as `(TokenStream, SemanticIndex, StateGraph, ProofLedger, Diagnostics)`, and `:190` says "The builder reads all three analysis artifacts." Nothing in this document says where a certificate arrives from. The added § The Certificate Gate says the checker runs "over the proof record the compiler emits alongside the definition" — alongside, i.e. not in the input record the contract is a contract on. The section is otherwise well-judged (it fixes a real gap and refuses to settle the shape); it needs one sentence saying the certificate is not part of `Compilation` today and that where it arrives is part of the unsettled shape.

### 15. `docs/working-reset/compiler-readiness-plan.md:44` — the plan gives condition 6 a failure reason that its own two other statements contradict

> "**The condition is not yet satisfied**, because none of the 21 carries a commit hash."

Exit condition 6 (`:789`) is "Every open item carries an owner." Commit hashes are condition 2, and the board row at `:74` says so explicitly ("Condition 2 fails on the 14 unstamped rows and on the 21 uncommitted ones. **Condition 6** now fails on **1**: `N82`"). Three statements in one document, one of them attributing condition 2's requirement to condition 6.

### 16. The "21 executed" count is one too many, in both tracking documents

`R13` was **not** executed — the sheet ruled against it and the register cell says so honestly ("Ruled against and not carried out"). The plan (`:40`, `:74`, `:789`) and the pass record count it among "21 of them are now executed". Twenty were edited; one was dispositioned without an edit. The register's own R13 cell is the evidence against the count.

### 17. `docs/working-reset/phase-2/contradiction-register.md:286` — the section intro still describes the machinery in the present tense

> "This family … **runs through six documents** and **includes** a builder pass, a descriptor type, an opcode annotation and an evaluator dispatch check. **A resolution that deletes only the prose leaves the machinery.**"

All four of those are now gone, and the plan was corrected in the same pass from "five documents" to "nine". The register's framing paragraph, sitting directly above 19 cells that now say "Executed", says the machinery is still there.

### 18. `docs/working-reset/phase-2/contradiction-register.md:315` — `N36`'s cell asserts the family has not run

> "All 13 survivors sit in the deliberately-deferred fault-backstop family — `proof-engine.md` (4), `fault-system.md` (4), `evaluator.md` (2), `precept-builder.md` (2), `soundness-and-coverage.md` (1) — which `R9`–`N35` own and **which nothing has executed** … The whole-canon count stays non-zero **until `R9`–`N35` execute**."

All 13 are now zero. `N36` is in the same table as the 19 cells the pass rewrote and was not touched.

### 19. `docs/compiler/diagnostic-system.md:578–581` — an instruction to future editors is now inside the illustrative code block

```csharp
    // … the remaining members, each naming its preventing diagnostic.
    // `docs/runtime/fault-system.md` declares the enum and is the only
    // place its membership is listed; do not keep a second copy here.
}
```

Reducing the drifted 13-member copy to a pointer is right and it is what `CLAUDE.md` § Catalog System requires. But the second and third lines are editorial policy addressed to whoever edits this file, sitting inside a C# sample that is meant to show the attribute shape. That belongs in the prose above the block.

---

# NOTED (pre-existing, or correct as written)

- **`docs/runtime/fault-system.md:231`** — "The chain is enforced across **five layers**:" over a **six**-row table. Untouched by the diff, pre-existing, but it sits four lines from an edit and the pass's own count discipline would catch it.
- **`docs/runtime/precept-builder.md:112–113`** — pass-list items 4 and 5 are "Constraint plan pass" then "Execution plan pass", while the § 7 headings, the diagram, the dependency table and the note at `:305` all have Pass 4 = Execution Plan and Pass 5 = Constraint Plan. Pre-existing; the diff rewrote item 6 of this list without noticing its neighbours.
- **`docs/runtime/evaluator.md:2004–2022`** — Implementation Status runs 1–6, Resolved Design Questions resumes at 8. The pass closed the hole it made at 10 (renumbering 11–13 to 10–12) and left the pre-existing hole at 7. Not a regression, just an inconsistently applied rule.
- **`docs/runtime/result-types.md:47`, `:56`** — still "8 variants" against a 7-variant code block. The pass records this and correctly declines to fix it, and notes that removing `evaluator.md:1433` silently deleted one of the three copies of the wrong count.
- **`docs/compiler-and-runtime-design.md:872–874`** — "Commit outcomes by operation" still gives `Fault` as the invariant-breach outcome for `Create`/`Fire`, `Update` and `Restore`, while `result-types.md:136–142` gives `UpdateOutcome` no fault variant and `runtime-api.md:281` gives `RestoreOutcome` none. The sheet dispositions this as descriptive rather than a variant requirement and folds it into the open producer question. Reasonable, but the row is now more visibly odd than it was.
- **`docs/compiler/graph-analyzer.md:72`** — "malformed lifecycle structure **would surface as runtime faults during evaluation**" — a counterfactual about the stage's absence, so not false, but it presumes an evaluation-time fault mechanism the rest of canon now says does not exist.
- **`docs/tooling/mcp.md:240`** — "# Precept Proofs and Runtime Faults" is a third instance of the tool-output heading the pass deliberately left at `:242` and `:269`; the same reasoning applies and it just is not listed.
- **The two declared survivors check out.** `docs/tooling/mcp.md:110` (defense-in-depth around the MCP wrapper's own tool methods) and `docs/compiler/type-checker.md:1133` (belt-and-suspenders compile-time re-validation) are both unrelated to the evaluator. So are `philosophy.md:46`, `precept-language-spec.md:110` and `:294`, and `proof-engine.md:1686`, all of which state the negation.

### Prose

- **"the single source of truth"** appears in two *rewritten* sentences — `diagnostic-system.md:552` and `proof-engine.md:2587`. The phrase pre-dates the pass, but both sentences were being re-authored, and `CLAUDE.md` bans it as a noun phrase.
- **`proof-engine.md:308`** — "The reason **previously stated** for it was that the Precept Builder matched … **Nothing does that any more**." This announces the edit rather than describing what is true. It is the pattern the brief names, and it is in a canonical spec. It can be said forward: the invariant's only recorded reason was a builder match that no longer exists; no document names another consumer; the invariant is kept pending a reason.
- **`precept-builder.md:420`** ("**Nothing is stamped on an opcode for a fault.**") and **`evaluator.md:972`** ("**The loop runs no fault check.**") lead their paragraphs with a negation where the section otherwise describes what the pass or the loop does. Both are defensible — canon states the same negation at `precept-language-spec.md:110` and `:112` — but they read as answers to a question about the previous draft rather than as descriptions of the design.
- **`diagnostic-system.md:626`** — the added "What the chain rules out" paragraph reasons about "`DiagnosticCode.StackUnderflow`'s message template" after the walkthrough directly above it was rewritten to use a generic member `X` and no longer introduces `StackUnderflow`. The example is now introduced only glancingly ("a stack underflow in the opcode loop say") and then referred to as if declared.

---

## What I checked and found clean

1. **Orphaned structure** — I ran an anchor checker over all 17 edited files. Every Contents entry resolves to a surviving heading and every surviving heading that had an entry still has one: `evaluator.md:56` (Impossible-Path) removed with its section; `:57` (Fault Production) kept with its section; `fault-system.md:30` removed, `:35` retitled with its Q3 heading; `diagnostic-system.md:35` (Evaluator usage) removed with its subsection; `proof-engine.md:57` (Fault Chain Integrity) removed with its section, `:40` (Proof/Fault Chain) correctly kept; `precept-builder.md:34` renamed to match the renamed Pass 6 heading, and the new § The Certificate Gate has an entry. `#### FaultSiteLink` was below the Contents' depth, so its removal orphaned nothing. No diagram node, dependency row or "see § X" points at removed material — the two cross-references that pointed at headings which never existed (`evaluator.md:2103`, `fault-system.md:262`) were repointed, and both new targets exist.
2. **Counts and sequences** — "six passes" holds at all eight sites in `precept-builder.md`; the Pass 6 rename landed in all five places plus the anchor; "all 5 descriptor records" → 4 (correct against the doc, wrong against the code, see finding 7); "Three-Layer" → "Two-Layer" changed in both the heading and the announcing sentence, and nothing in canon still cites three layers; `proof-engine.md`'s three key design choices, §11's five decisions and the builder-consumption contract's renumbered 1–3 are all intact; the soundness note went from "Two known items" to one and was retitled; `catalog-system.md:295`'s "Faults (15)" is still right because the enum keeps all fifteen members. The count errors I found are at findings 15, 16 and the noted "five layers".
3. **Cross-file references, both directions** — swept `docs/language`, `docs/compiler`, `docs/runtime`, `docs/tooling`, `docs/philosophy.md`, `docs/compiler-and-runtime-design.md`, `docs/glossary.md`, `docs/contributing/`, `CONTRIBUTING.md`, `README.md`, `docs/README.md` and every per-area README, including files the diff never touched. Findings 1–6 are what that turned up; everything else resolves.
4. **Hollow tables and examples** — the divide-by-zero walkthrough was re-authored in both copies and now opens from the compiler side and closes on the step that is the point; the nine-row impossible-path table went whole rather than leaving a stub; the `FaultCode`/`GetMeta` copies were reduced to a pointer with the teaching content kept; `Opcode` correctly survives as an empty abstract base for its subtypes; `result-types.md:115`'s `Pipeline stage` cell became `—` rather than being deleted out of a column where every other cell names a § 3A.4 phase.
5. **Residue of the removed model** — I ran every term in the brief plus `fault trap`, `IsFaultCondition`, `fault site`, `runtime mirror`, `runtime face`, `runtime-to-compiler`, `three layers`, and the removed heading names. The vocabulary the pass's table tracks is genuinely at zero. What it missed are the paraphrases: "fault-site links", "fault site taxonomy", "runtime mirror" ×2, "runtime-to-compiler linkage", "diagnostics/fault linkage" — findings 1–6. Its judgement on the two survivors it does list is correct.
6. **The two re-scoped sections** — § Fault Production (`evaluator.md:1591–1614`) and § Pass 6 — Constraint Influence Pass (`precept-builder.md:557`) both still have members and neither argues for its own removal. Fault Production declares `Fail`, restates the `Fault` record, keeps "never thrown", and carries the open producer question honestly. Pass 6 keeps `ConstraintInfluenceMap`, which is a real responsibility with a real input and a real output, and the dependency row and diagram box were renamed with it. Both hold together. The re-scoped § Q3 in `fault-system.md` also holds — it closes the half the ruling settles and states the half that remains, with phase 11 named as owner.