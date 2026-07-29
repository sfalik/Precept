# Review of the fault-backstop pass — what it removed that it should not have

**Run** 2026-07-28, read-only, against the uncommitted fault-backstop diff. Nothing was edited by this review. Findings are the reviewer's; where one is later judged wrong, that judgement belongs in the fix pass record rather than here.

---

# Review of the uncommitted fault-backstop pass

Read: `git status`, the full `git diff` (19 files), `docs/working-reset/phase-2/fault-backstop-model.md` (the sheet), `docs/working-reset/phase-2/fault-backstop-pass.md` (the pass record), `docs/philosophy.md` at `545e3c19`, the `want` document lines cited (106, 141, 143, 163, 165, 171, 177, 199), and the current working-tree state of every file the diff touches. I also checked `src/` and `tools/` for the types the documents stopped describing.

---

## What came back clean

Stating these explicitly so you can tell what was checked.

**1. `Fault`, `FaultCode` and `EventOutcome.Faulted` are all still declared.** `Fault` is declared at `docs/runtime/fault-system.md:207–221`, `FaultCode` at `:111–158`, `StaticallyPreventableAttribute` at `:106`, and `EventOutcome.Faulted(Fault)` at `docs/runtime/result-types.md:103` and `docs/runtime/evaluator.md:336`. The diff changes their descriptions, never their existence. `compiler-and-runtime-design.md:826`, `:865` and `:874` still keep the family.

**2. All fifteen `FaultCode` members survive, including the three that matter most.** `OutOfRange` at `fault-system.md:150`, `LengthBoundViolation` at `:153`, `CountBoundViolation` at `:156`. `soundness-and-coverage.md:234–237` still dispositions all three as **in** with `IntervalContainment`, `LengthContainment` and `CountContainment` named. All fifteen rows of the §3.1 coverage table survive; the only edit inside it is the removal of a parenthetical on the `QualifierMismatch` row. `R13` — the one register row the sheet ruled against — was correctly not executed, and its register cell says so rather than claiming execution.

**3. `[StaticallyPreventable]` and the analyzer chain survive.** The attribute is intact in `fault-system.md`, `diagnostic-system.md`, `catalog-system.md`, `compiler-and-runtime-design.md:651` and `soundness-and-coverage.md`. PRECEPT0001 is still in both enforcement tables (`diagnostic-system.md:614`, `fault-system.md:236`) and still named in `catalog-system.md:911` and `proof-engine.md:2490` — it was re-worded, not deleted, which is what the sheet required. PRECEPT0002, 0003, 0004 and 0016 are untouched.

**4. No orphaned anchors.** Every table-of-contents entry whose heading was removed went with it (`#evaluator-usage`, `#impossible-path-failures-defense-in-depth`, `#fault-chain-integrity`, `#defense-in-depth-not-a-primary-error-path`), and every entry whose heading survived was kept (`#fault-production`, `#proof-fault-chain`, Decision 2 at `:62`, the exclusion at `:80`). No link anywhere in `docs/` outside `Working/` and `working-reset/` points at a removed heading. The `#pass-6-fault-backstop-pass` anchor was renamed in both places. The builder still says "six passes" at all seven surviving sites.

**5. The register was not tampered with beyond the Commit column.** A word-level diff of `contradiction-register.md` shows changes confined to the Commit cell of 21 rows. No site, canonical-text, resolution, reason or grep-pattern cell changed anywhere in the file. Row `N82` is untouched and still unexecuted.

**6. No invented numbers of the kind you flagged.** The builder's `Estimated LOC` stayed `600–900`; only the `+ ~50 fault backstop` term came out of the itemization, and the range was left alone. `24 Roslyn rules (PRECEPT0001–PRECEPT0024)`, `132 diagnostic codes` and `145 rows` are all unchanged. The plan's new arithmetic checks out: 97 + 14 + 21 = 132. The pass record's "183 added, 435 removed across seventeen documents" matches the diffstat of the seventeen canonical files exactly. Correcting "nineteen rows across five documents" to "nine documents" is a real fix — the nineteen rows do span nine.

**7. Almost nothing was deleted that is unrelated to the model.** I read every removed line in the seventeen canonical files. Two out-of-family removals, both defensible, are under *Noted* below. Everything else is fault-backstop machinery.

---

## Blocking

### B1. A stated reason for a surviving invariant was deleted, and the replacement says no reason was ever written

`docs/compiler/proof-engine.md`, working tree `:317–320` (was `:337` at HEAD).

At HEAD the invariant paragraph ended:

> **Invariant:** The proof engine MUST NOT use `with { ... }` on `obligation.Site` … but the `Site` field must point to the original `SemanticIndex` expression. **Reference identity prevents false positives from structural equality matching identical-but-distinct expressions in different transition rows.**

That final clause is gone. In its place:

> **Why the invariant holds is unsettled.** The reason previously stated for it was that the Precept Builder matched a `TypedExpression` by reference identity to stamp a fault annotation on the opcode it compiled to. Nothing does that any more, and **no document names another consumer** that needs reference identity rather than the span. … whoever writes the certificate contract owns the question … and **should either give this invariant a reason or remove it**.

The document did state a second reason, in the sentence the pass deleted, and it is not about fault-site planting. It is about what goes wrong if you copy a `TypedExpression`: structural equality then matches an identical-but-distinct expression in a different transition row. That hazard is a property of expression identity, not of the annotation mechanism. The new text tells the next reader that the invariant has no written justification, and points them at removing it — while the justification was in the same paragraph until this diff.

The sheet made this error first (§5 item 5, and §3: "no document states another consumer for the 'MUST NOT use `with { }` on `Site`' rule at `:337`"), so the pass inherited it. But the sentence was on screen when the paragraph was rewritten.

Fix: restore the clause, or restate it, and re-word the open note to say what remains unsettled is whether any *current* consumer relies on reference identity — not that no reason was ever given.

### B2. The plan's exit condition 6 no longer records a failure, and the owners it names do not meet the condition's own bar

`docs/working-reset/compiler-readiness-plan.md:790–795`.

Before:

> As of 2026-07-28 this condition fails: the nineteen fault-backstop rows have no owning phase, and two rows found in `docs/tooling/` on the fifth pass are recorded but unassigned.

After:

> As of 2026-07-28 the nineteen fault-backstop rows and the two `docs/tooling/` rows are executed and uncommitted, so they are no longer open items; the two questions that pass left open both name an owner — whether anything constructs a `Fault` is owned by phase 11, and the reason behind the reference-identity invariant on `ProofObligation.Site` is owned by whoever writes the certificate contract.

Three problems.

- **`N82` disappeared from the condition that fails on it.** The board row at `:75` says "**Condition 6** now fails on **1**: `N82`, same-basis period ordering." The exit-condition text does not mention `N82` and no longer says the condition fails. The two statements are in the same document and disagree, and the one that *defines* the condition now reads as satisfied. The pass record's claim that "no condition is marked as passing" is true only in the narrow sense that the word "passing" does not appear; the recorded failure was removed at the defining site.
- **"Whoever writes the certificate contract" is not an admissible owner.** The condition's own text, two lines above, requires "either a later phase whose scope actually reaches it, or a ruling the owner has given that the work is not being done." A person who has not been identified is neither. Phase 4 ("Draft the certificate step list and count it") would have been a phase; it was not named.
- **Phase 11 does not have the scope for the `Fault` question.** `:1601` states phase 11's goal as "Make the frozen suite green by building the compiler, without touching the suite," building only against red tests phase 10 wrote. An undecided design question cannot have a red test written for it in phase 10, so parking it in phase 11 is the pattern condition 6 exists to catch. `fault-system.md:318` also describes phase 11 as "which builds the evaluator" — that is not how the plan describes it.

Fix: restore an explicit statement that condition 6 still fails on `N82`, and re-home the two open questions on phases whose stated scope reaches them.

### B3. Three documents now make false statements about shipped code, and the drift is unrecorded

The design change is right; these are the is-statements that came with it.

- `docs/runtime/descriptor-types.md:8` — Implementation state now reads "Implemented — `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor` defined in `Descriptors.cs`". `src/Precept/Runtime/Descriptors.cs:60` defines `FaultSiteDescriptor` as well.
- `docs/runtime/descriptor-types.md:194` — "Implemented — all 4 descriptor sealed records". `Descriptors.cs` declares five (lines 10, 23, 32, 43, 60).
- `docs/runtime/precept-builder.md:948` and `docs/compiler/proof-engine.md`'s `ProofLedger` record — both now omit `FaultSiteLink`, which `src/Precept/Pipeline/ProofLedger.cs:117` declares and `:8` carries as a field on the record.

The machinery is live in nine places, not only in the two declarations: `ProofEngine.cs:102,176`, `ProofEngine.Diagnostics.cs:452–493` (`CreateFaultSiteLink`), `FaultSiteAnnotation` at `ProofLedger.cs:124`, the language server reading `compilation.Proof.FaultSiteLinks` at `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:292`, and tests in `test/Precept.Tests/DescriptorsTests.cs:177–224` and `FaultMapDerivationTests.cs`.

`CLAUDE.md` § Doc status conventions: "If a doc says 'Implemented' but the code disagrees, that's drift — fix the doc in the same pass and note the drift to the user." The pass record's §8 discloses only that no MCP projection inventory was taken; nothing in it mentions `src/`.

Fix: make the three Implementation State / Source Files entries honest (the types exist in code and are on the way out), and record the code drift so the removal has a place to be tracked.

### B4. Report only — the philosophy tension the pass says does not exist

`docs/philosophy.md:46`, locked at `545e3c19`, states without qualification: **"The engine does not fault."**

After this diff, six canonical places say the opposite is undecided:

- `compiler-and-runtime-design.md:826` — "whether the evaluator should detect an engine-invariant breach at all … is undecided"
- `fault-system.md:318` — "Whether the evaluator asserts, faults, or does neither … is undecided"
- `evaluator.md:1620` — "Whether the evaluator carries a detection mechanism at all … is undecided"
- `result-types.md:129` — "Nothing constructs one today; whether anything ever will is undecided"
- `runtime-api.md:171` — same
- `precept-language-spec.md:1963` — "Nothing constructs one today"

The philosophy sentence sits directly after a list of author-data fault classes ("Division by zero, arithmetic overflow, empty collection access"), so it can be read as scoped to those. But as written it is unqualified, it is in the locked document, and canon now holds open a case in which the engine faults. The pass record §8 asserts "Nothing found in this pass contradicts it"; that assertion is at least contestable and was not tested against this wording.

Not resolving it. It needs one sentence from you: either the locked sentence is scoped to author-data faults and the engine-defect case is genuinely open, or it is unqualified and Q3 has only one admissible answer.

---

## Worth fixing

### W1. `fault-system.md` Q3 — the sentence the sheet told the pass to keep was deleted, and the pass record says it stayed

The sheet, §3 and §6 `N27`, both say: "**Do not remove the whole of Q3.** … Delete `:322`, **keep the heading and `:324`**", because `:324` — "This affects how aggressively the evaluator should assert vs. gracefully fault. Decision needed at evaluator design time." — was the only record in canon of that question.

The diff deletes `:324` and renames the heading. The pass record's `N27` row states "**`:320` and `:324` stay.**" That is not what happened.

The substance is preserved, and arguably better stated — the new Q3 body ends "Whether the evaluator asserts, faults, or does neither … is undecided." So the harm is to the record, not to canon. But the record's claim should be corrected, because the next reader will check the two sites the sheet named and find neither.

### W2. A transient phase label written into a canonical document

`docs/runtime/fault-system.md:318`: "**Owned by phase 11**, which builds the evaluator". Phase labels are on `CLAUDE.md`'s transient list. The rule is written for code comments, but the rot is the same: the plan is a working document and its phase numbering has already been renumbered once. A canonical doc should say what the open question is and route to the plan, not name a phase number inside the answer. See also B2 for whether phase 11 is the right owner at all.

### W3. Three surviving sentences the pass's own sweep missed, in files it edited

Omissions rather than deletions, and the pass's grep patterns could not have caught them, but they now read against the corrected model:

- `docs/runtime/result-types.md:436` — "an evaluator-level fault is returned as the `EventOutcome.Faulted(Fault)` variant". Every other description of `Faulted` in that file was re-scoped; this one was not.
- `docs/runtime/README.md:14` — the document table still describes `fault-system.md` as "Runtime fault codes and classification — the runtime mirror of the compiler's diagnostic system", while `:37` in the same file was corrected in this diff.
- `docs/runtime/fault-system.md:326` — "Informational or warning-level **evaluator faults** are excluded…".

### W4. Two incidental facts deleted with no replacement

Both in `fault-system.md`, both statements about what a returned `Faulted` outcome means, which is still a live case under the open Q3:

- Right-Sizing: "every fault is fatal **(the transition is aborted, no state changes are committed)**" — the parenthetical is gone.
- Deliberate Exclusions: "**a fault aborts the current transition immediately**, and there is no multi-stage fault pipeline to suppress" — the first clause is gone.

Neither is about backstops. They were the only statement in the fault documents of what happens when `Faulted` is the outcome. If they were meant to go, that should be deliberate.

### W5. The register's index table has no entry for this pass's non-row edits

`contradiction-register.md:39–41` states the rule: "**Every edit made to a canonical document during phase 2 is reachable from this table**, including the ones that are not rows," and the commit-indexed table at `:43` is where the not-a-row edits are made reachable. This pass made roughly fourteen such edits — `runtime-api.md:277` and `:910`, the new `precept-builder.md` § The Certificate Gate, the `diagnostic-system.md` enum and `GetMeta` copies, `proof-engine.md:2446`, `:2296`, `:2587`, `:2140`, `diagnostic-system.md:541`, `glossary.md:78` and `:113`, `runtime/README.md:37`, `anti-patterns.md:336`, `catalog-system.md:701–702`, `precept-language-spec.md:1963`, and two cross-reference repoints — and added no index row. They are reachable only through the 21 row cells that link the pass document. Add the index entry when the commit exists.

---

## Noted

### N1. Two changes that are not about fault backstops

`runtime-api.md:277` and `:910` replace "migration on schema change is the caller's responsibility" with the post-MVP restore-path account. This is a `want:141` correction (verified: want:141's parenthetical reads "Post-MVP, the restore path itself expands to handle schema evolution — data written under an older definition"), the sheet asked for it, and the two already-corrected siblings match — `evaluator.md:1754` and `precept-language-spec.md:1969`. The correction is right. It is simply a different subject riding in this commit, and the pass record's §4 cites `evaluator.md:1754` where the sheet had the stale `:1806` — the record's number is the correct one.

### N2. New canonical content that no register row covers

Both are honestly marked and both track `runtime-api.md:250–260`, which already declares the mechanism unbuilt:

- `precept-builder.md` § The Certificate Gate — a new section with a contents entry, a new Input Contract bullet, a re-worded Recovery Path, and a re-worded always-valid guarantee at `:844`. The sheet listed this under "What this does not decide" (§5 item 6); the pass wrote a section that records the question rather than answering it, which is within that, but it is new canon rather than a correction.
- `diagnostic-system.md` § "What the chain rules out" — the new paragraph arguing an engine-defect code cannot be added under PRECEPT0002 as it stands, ending with the question marked undecided.

### N3. The `diagnostic-system.md` enum copies were reduced, not reconciled

`:577–617` (thirteen members) and `:632–645` (thirteen switch arms) are now a two-member excerpt plus a pointer at `fault-system.md`. The sheet marked this optional and offered either reduction or reconciliation at fifteen. Reduction is the better choice under `CLAUDE.md` § Catalog System's anti-mirroring rule, and it removes the `UnexpectedNull → NullInNonNullableContext` mis-mapping that disagreed with the declaring document. Recorded because it is a judgement the sheet left open, not an instruction it gave.

### N4. Vocabulary sweep confirmed

I re-ran it independently over `docs/language docs/compiler docs/runtime docs/tooling docs/philosophy.md docs/compiler-and-runtime-design.md docs/glossary.md docs/contributing`. `FaultSiteAnnotation`, `FaultSiteDescriptor`, `FaultSiteLink`, `FaultBackstops`, `CC#6`, `CC#12`, `IsFaultCondition`, `backstop`, `impossible path`, `Three-Layer Enforcement` all return zero. `defense-in-depth` returns one (`mcp.md:110`, the MCP wrapper's own catch-all) and `belt-and-suspenders` one (`type-checker.md:1133`, a compile-time re-validation). Both survivors are correctly identified in the pass record and neither is a runtime path.