# Phase 2 — the review findings on the fault-backstop pass, applied

**Run** 2026-07-28 on top of HEAD `72a6c7ff`, branch `spike/Precept-V2-Radical-reset`. Nothing here is
committed; the tree is left dirty. No `.precept` file was compiled, and no catalog, sample or compiler
behaviour is cited as evidence for a documentation claim. `docs/philosophy.md` was not edited. Nothing
under `docs/archive/`, `src/` or `tools/` was edited. No sub-agents were spawned.

**What this pass is.** Two independent reviews went over the uncommitted fault-backstop diff —
[`fault-backstop-review-deletions.md`](fault-backstop-review-deletions.md), on what was removed that
should not have been, and [`fault-backstop-review-coherence.md`](fault-backstop-review-coherence.md), on
which documents no longer hold together. Both confirmed the substance: the deletions were right and the
removal is not reopened here. Between them they found a tail of things to fix, and this pass applies it.

**The largest single group is one pattern.** The original pass's vocabulary sweep returned zero for every
exact type name and missed the paraphrases — "fault-site links" spaced and hyphenated, "fault site
taxonomy", "the runtime mirror", "the runtime-to-compiler linkage", "diagnostics/fault linkage". Six of
the coherence review's seven blocking findings are that one pattern. § 4 below re-runs the sweep with the
paraphrases included.

**One instruction carried through every edit made here.** Added text describes what is now true rather
than announcing what changed. "The reason previously stated for it was … nothing does that any more" is
wrong for a canonical specification; the same content states forward as "reference identity is the only
thing that tells two identical-but-distinct expressions apart."

---

## 1. The finding the owner ruled on

### Deletions review `B4` — the philosophy tension

**The finding.** `docs/philosophy.md:46` states "The engine does not fault" without qualification, while
six canonical places now say whether the evaluator detects an **engine defect** is undecided:
`compiler-and-runtime-design.md:826`, `fault-system.md` Q3, `evaluator.md` § Fault Production,
`result-types.md` § `Faulted` semantics, `runtime-api.md:171`, `precept-language-spec.md:1963`. The review
declined to resolve it and asked the owner for one sentence.

**Answered by the owner, 2026-07-28.** His words: *"obviously we can't say that piece of software has no
bugs. it's aspirational."* `philosophy.md:8` already says the same in the document's own voice — it states
commitments rather than the current build, and gaps are tracked in the compiler and runtime documents. A
bug in Precept is outside the guarantee, not a case the guarantee has to model.

**What was done.** Nothing to `philosophy.md`, which is locked and was amended earlier the same day on the
owner's approval, and nothing to the six notes on that account. The ruling is recorded on the plan's § 4
row, struck through so it is not lost, and in [`fault-backstop-pass.md`](fault-backstop-pass.md) § 8 in
place of that document's untested claim that nothing in the pass contradicted the philosophy.

**While in the six notes, each was checked for whether it names an engine defect clearly** rather than
reading as though the guarantee itself were soft. Five were already unambiguous — `result-types.md`,
`runtime-api.md` and `precept-language-spec.md` all say "a defect in the compiler, the builder or the
evaluator"; `compiler-and-runtime-design.md:826` says the same; `fault-system.md` Q3 describes the
evaluator "checking that it has not been handed a model that violates its own construction invariants".
**One was touched**: `evaluator.md` § Fault Production's open note said "engine-invariant breach" and
"corrupted model" without ever saying whose defect it is. It now opens "The case in question is a defect in
Precept itself — in the compiler, the builder or the evaluator — not anything an author can write in a
definition."

---

## 2. The deletions review, finding by finding

### `B1` — a stated reason for a surviving invariant was deleted (blocking)

**The finding.** At HEAD the reference-identity invariant on `ProofObligation.Site` gave two reasons. The
pass removed the one about the builder's match and also deleted the other — "Reference identity prevents
false positives from structural equality matching identical-but-distinct expressions in different
transition rows" — while its replacement text told the reader no reason was ever written and pointed at
removing the invariant.

**What was done.** `docs/compiler/proof-engine.md` § ProofObligation. The reason is restated forward: the
same text written into two transition rows produces two `TypedExpression` values that compare equal
structurally, so a consumer matching on structure attributes an obligation to whichever it meets first,
and copying the `Site` with `with { }` discards the distinction. The open note is re-worded to say what is
actually unsettled — whether any consumer needs the distinction — and to name the certificate checker's
specification as where that closes, because a checker that replays a discharge has to find the expression
the discharge was about. No phase number is written into the canonical document; the owner is recorded in
the plan (see `B2`).

**Where a reviewer was wrong, with the evidence.** The coherence review's finding 7 offers
`tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:292` as a live consumer the sheet and the
deletions review had missed, and the deletions review's `B3` repeats it. It is a live consumer of
`ProofLedger.FaultSiteLinks`. It is **not** a consumer of reference identity, so it does not bear on the
question this invariant leaves open. The code matches on spans throughout:
`RichHoverFactory.cs:293` filters with `SymbolNavigation.Contains(link.Site, position)` — `link.Site` is
the `SourceSpan`, not the expression — and `:275` and `:303` select obligations by
`obligation.Site.Span == site.Span` and `Overlaps(obligation.Site.Span, diagnostic.Span)`. Nothing in that
file compares a `TypedExpression` by reference. The invariant's open half is therefore still open in
exactly the terms the fix states it in.

### `B2` — the plan's exit condition 6 (blocking)

**The finding.** Three problems. The condition text no longer records that it fails, while the board row
says it fails on `N82`. "Whoever writes the certificate contract" is a person nobody has named, and the
condition's own text requires a named phase whose scope reaches the item, an owner ruling, or a named
exception. And phase 11's stated scope is building against red tests phase 10 wrote, so an undecided
design question cannot live there.

**What was done, and it took reading every phase entry rather than picking the nearest.**
`docs/working-reset/compiler-readiness-plan.md`:

- **Exit condition 6 states its failures again**, explicitly: on `N82`, and on one open item.
- **The reference-identity question is re-homed on phase 5.** Phase 5's *What it produces* list includes
  "the specification of the certificate checker: what it walks, what it asks the catalog for each thing it
  finds, and what it does when the certificate does not cover something", and § What closes when puts the
  certificate step vocabulary's closure there. A checker that replays a discharge has to identify the
  expression the discharge was about, so whether `Site` must be a reference or may be a span is decided
  inside that specification. That is a phase whose stated scope actually reaches it.
- **The `Fault`-producer question has no owning phase, and that is written down rather than resolved.**
  Every phase entry was read. Phase 3 closes the language; phase 4 drafts and counts certificate step
  kinds; phase 5 settles obligation families and the discharge procedure; phases 6 and 7 argue and
  generate the grid; phase 8 is a confirmation; phase 9 fills the catalogs; phase 10 turns claims into
  failing tests; phase 11 builds the compiler against those tests plus the certificate checker and the
  runtime's load-time call to it; phase 12 confirms the compiler is finished. § What closes when stops at
  "the compiler". **Nothing in this plan builds the evaluator**, and whether the evaluator detects a
  breach of its own construction invariants is an evaluator design decision. So the honest result is that
  the item needs one of the condition's other two forms — an owner ruling that the work is not being done,
  or a named exception — and it is on § 4 as the former, with the question put as *who owns it*, not
  *what is the answer*.
- **The board row and § 1 agree with the condition text again.** Both now say condition 6 fails on one row
  and one open item.
- **The canonical document lost its phase label.** `fault-system.md` Q3 said "**Owned by phase 11**, which
  builds the evaluator". Phase numbers are transient material by `CLAUDE.md` § Transient vs Canonical
  references, and this one was also wrong. Q3 now says the decision belongs with the work that builds the
  evaluator and that the two accounts in canon have to be reconciled first. This also closes `W2`.

### `B3` — three documents make false statements about shipped code (blocking)

**The finding.** `descriptor-types.md:8` and `:194` claim four descriptor records where
`src/Precept/Runtime/Descriptors.cs` declares five; `precept-builder.md:948` and `proof-engine.md`'s
`ProofLedger` record omit `FaultSiteLink`, which `src/Precept/Pipeline/ProofLedger.cs` declares and
carries as a field. `CLAUDE.md` § Doc status conventions requires drift to be fixed in the same pass and
reported.

**What was done. No code was changed.** Six documentation sites now say the types exist in code, are
described nowhere in the design, and are on their way out:

| Site | What it now says |
|---|---|
| `descriptor-types.md` Status | `Descriptors.cs` also declares a fifth record, `FaultSiteDescriptor`, which this document no longer describes |
| `descriptor-types.md` § Source Files | The four records this document describes plus a fifth it does not, with why there is no such residue to plant, and that the removal has not been made |
| `precept-builder.md` § 16 `Descriptors.cs` row | The file also declares `FaultSiteDescriptor`, which no builder pass produces |
| `precept-builder.md` § 16 `ProofLedger.cs` row | The file also declares `FaultSiteLink` and `FaultSiteAnnotation` and the record still carries `FaultSiteLinks`; no builder pass reads any of the three |
| `proof-engine.md` § Output | A note under the `ProofLedger` record: the shipped record is one field wider, the language server reads it, and the record shown is the shape the ledger takes once that comes out |
| `proof-engine.md` § 1 Status | The Implementation state row routes a reader to that note |
| `evaluator.md` Implementation Status and § Source Files | `FaultSiteDescriptor` is declared and the evaluator does not read it |

**The removal is tracked** on the plan's § 5 as its own row, naming every place it touches:
`Descriptors.cs:60`, `ProofLedger.cs`, `ProofEngine.Diagnostics.cs:452`, `RichHoverFactory.cs:292`, and
the tests in `test/Precept.Tests/DescriptorsTests.cs` and `FaultMapDerivationTests.cs`. The drift is also
disclosed in [`fault-backstop-pass.md`](fault-backstop-pass.md) § 8, which previously mentioned nothing
under `src/`.

**Where a reviewer was wrong, with the evidence.** The coherence review's finding 7 says the language
server's hover behaviour is "a behaviour `docs/tooling/language-server.md:607` documents", implying the
documented behaviour depends on the field being removed. It does not.
`RichHoverFactory.FindProofObligationForDiagnostic` tries `FaultSiteLinks` first and then falls back —
`RichHoverFactory.cs:302–305` selects from `compilation.Proof.Obligations` by `EmittedDiagnostic` and span
overlap when the first path returns nothing. The row at `docs/tooling/language-server.md:607` describes
the outcome ("proof-stage diagnostic + matching obligation"), not the field, and it stays true after the
removal. No edit was made there.

### `W1` — `fault-system.md` Q3, and the pass record's claim about it

**The finding.** The sheet said to keep `:324`; the diff deleted it and renamed the heading; the pass
record's `N27` row states "`:320` and `:324` stay". The substance is preserved in the re-written Q3, so
the harm is to the record.

**What was done.** [`fault-backstop-pass.md`](fault-backstop-pass.md) corrected in two places — the `N27`
row and § 3 item 3 — to say `:322` and `:324` were both deleted, why the question they carried is not
lost, and that the record was wrong. Canon is unchanged; the reviewer's reading of it is right.

### `W2` — a transient phase label in a canonical document

Applied with `B2` above.

### `W3` — three surviving sentences the pass's own sweep missed

| Site | Was | Now |
|---|---|---|
| `result-types.md:436` | "an evaluator-level fault is returned as the `EventOutcome.Faulted(Fault)` variant" | "a breach of one of the engine's own invariants is returned as …" |
| `runtime/README.md:14` | "the runtime mirror of the compiler's diagnostic system" | "each names a class of failure and the compiler diagnostic that prevents it, paired at build time" |
| `fault-system.md:326` | "Informational or warning-level **evaluator faults** are excluded" | "Informational or warning-level fault classes are excluded" |

`runtime/README.md:14` is also the coherence review's blocking finding 3.

### `W4` — two incidental facts deleted with no replacement

Both restated in `fault-system.md`, in the corrected framing rather than the old one, and both grounded in
canon that already says it: `philosophy.md:46` ("commits wholly or not at all") and `runtime-api.md:373`
("the working copy is discarded only when the operation refuses before it finishes").

- § Right-Sizing: "every one of them is fatal — an operation that returned `Faulted` did not finish, so its
  working copy is discarded and nothing it would have changed is committed."
- § Deliberate Exclusions, fault suppression: "a `Faulted` outcome ends the operation where it stands, and
  there is no multi-stage fault pipeline to suppress."

### `W5` — the register's index table has no entry for this pass's non-row edits

**What was done.** [`contradiction-register.md`](contradiction-register.md) § Where to find the detail
behind any edit gains a row. The review suggested adding it when the commit exists; it is added now with
the Commit cell reading *uncommitted, 2026-07-28*, because the register's rule at `:41` is that every edit
is reachable from that table and deferring the row leaves the rule unmet in the meantime. The row lists
the roughly fourteen non-row sites and points at both this document and the pass record. It gets its hash
when the commit lands.

### `N1`–`N4` — noted, and what was done with each

- **`N1`, the `runtime-api.md` migration correction.** The review's own verdict is that the correction is
  right and the pass record's line number is the correct one. Nothing done.
- **`N2`, new canonical content no register row covers.** § The Certificate Gate and § "What the chain
  rules out" are honestly marked and both track already-declared canon. The review records rather than
  objects. Both were touched for other findings (coherence 14 and 19), not for this one.
- **`N3`, the reduced `diagnostic-system.md` enum copies.** The review calls the reduction the better
  choice under the anti-mirroring rule. Nothing done except moving the editorial instruction out of the
  code block, which is coherence finding 19.
- **`N4`, the vocabulary sweep.** Re-run independently at § 4 below, widened with the paraphrases.

---

## 3. The coherence review, finding by finding

### Blocking

| # | Site | What was done |
|---|---|---|
| 1 | `proof-engine.md:101` | The Overview no longer lists "fault-site links" among what the `ProofLedger` carries |
| 2 | `runtime-api.md:83` | *Does NOT OWN* now reads "`Fault` and the fault classes", matching the Deliberate Exclusions bullet 360 lines later that the pass had already corrected |
| 3 | `runtime/README.md:14` | Rewritten — see `W3` above. The area map now agrees with `:37` in the same file |
| 4 | `glossary.md:87` | The **Diagnostic** entry's "The runtime mirror is **Fault**" now reads "Each prevention diagnostic is paired at build time with the **Fault** class it exists to prevent", matching the **Fault** entry the pass rewrote |
| 5 | `proof-engine.md:18` | The Non-Negotiable Rule now reads "rejects it with a diagnostic that refuses the definition" rather than "rejects it with diagnostics/fault linkage" |
| 6 | `diagnostic-system.md:72` | OWNS now describes the attribute as "the pairing of each fault class with the diagnostic that prevents it, checked when the project is built", not "the runtime-to-compiler linkage" |
| 7 | code drift | Applied as deletions `B3` above, including the two places where this review's evidence does not support its own conclusion |

### Worth fixing

| # | Site | What was done |
|---|---|---|
| 8 | `fault-system.md:11` | Downstream is `result-types.md` alone. `diagnostic-system.md` is Upstream and was listed as both |
| 9 | `fault-system.md:43` | The Overview table header is now `Diagnostic system \| Fault system`, which is what its rows compare, rather than `Compiler (pipeline) \| Runtime (evaluator)` |
| 10 | `fault-system.md:304` (Q2), `:294` (Q1) | Q2 is re-authored: it asks what context a `Fault` should carry if anything ever constructs one, and its three bullets are about the engine's own state — where in the plan, which invariant, which operation — rather than `rate = 0`. Q1's resolution now opens "should anything ever construct a `Fault` (Q3)". `:326` went with `W3` |
| 11 | `evaluator.md:434` | `Fail` gets its own banner, `// ── Fault Construction ──`, with a one-line note that nothing calls it and a pointer at § Fault Production. It no longer reads as a fourth inspection operation |
| 12 | `proof-engine.md:2369` | The one-bullet "What crosses:" list folds into prose: the constraint execution plans the runtime model carries reach it by a different route and the proof engine does not produce them |
| 13 | `proof-engine.md:1849` | Heading and Contents entry renamed together to **Requirement-to-Diagnostic Chain**, and `compiler-and-runtime-design.md:657`'s prose pointer at "the proof/fault chain" repointed with them. The section's invariants 1 and 2 are about the build-time fault/diagnostic pairing and are kept, so the lead-in now says the chain ends at the diagnostic and the pairing is checked at build time rather than being another link. `compiler-and-runtime-design.md` § Proof/fault chain keeps its name, because that chain does still run to `DiagnosticCode ↔ FaultCode` |
| 14 | `precept-builder.md:676` | § The Certificate Gate gains a paragraph: `Compilation` carries five things and a certificate is not one of them, `runtime-api.md` describes the proof record as emitted alongside the definition, and whether it becomes a sixth member, a separate argument or a file the method is pointed at is part of the unsettled shape |
| 15 | plan `:44` | Applied with `B2`. The paragraph gave condition 2's requirement (commit hashes) as condition 6's failure reason; it now gives owners, which is what condition 6 is about, and says condition 2 fails on the same rows for its own reason |
| 16 | the 21-versus-20 count | Corrected in all three places: the plan's § 1 paragraph and board row, and [`fault-backstop-pass.md`](fault-backstop-pass.md) § 9. Twenty rows were executed by editing canon; `R13` was ruled against and disposed of without an edit. All 21 are resolved, so the "about 132 done" arithmetic is unchanged — what was wrong was the word *executed* |
| 17 | register `:286` | The family's intro paragraph is past tense and records the outcome: the machinery is gone from the documents, `R13` alone ran as a ruling rather than an edit, and what remains is in the code |
| 18 | register `:315` (`N36`) | The cell no longer says the family is unexecuted. It records that the 13 survivors are now zero as well. Verified: `grep -rn "defense-in-depth\|defence-in-depth"` over `docs/language docs/compiler docs/runtime docs/philosophy.md` returns nothing |
| 19 | `diagnostic-system.md:578` | The instruction to future editors moves out of the C# sample into prose above it. The sample keeps the two members that teach the attribute shape |

### Noted — what was left, and why

The brief for this pass is to act on a Noted item only where the finding says the diff caused it. These
were left:

- **`fault-system.md:231` — "five layers" over a six-row table.** Pre-existing and untouched: the diff's
  only appearance of that line is as context (`git diff docs/runtime/fault-system.md` shows it at the hunk
  header and in unchanged context, never as a `+` or `-`). It is a count error, not residue of the removed
  model. **It is wrong and cheap to fix, and it is left only because the instruction for this pass says
  so** — worth an owner's word on whether to take it in the commit.
- **`precept-builder.md:112–113` — passes 4 and 5 listed in the opposite order to the § 7 headings, the
  diagram, the dependency table and `:305`.** Pre-existing; the diff rewrote item 6 of that list without
  touching its neighbours. A real defect and not this pass's.
- **`evaluator.md:2004–2022` — Implementation Status runs 1–6 and Resolved Design Questions resumes at 8.**
  The hole at 7 pre-dates the diff; the pass closed the one it made at 10. Not a regression.
- **`result-types.md:47`, `:56` — "8 variants" against a 7-variant code block.** Pre-existing; the original
  pass recorded it and declined it, and the review agrees with declining.
- **`compiler-and-runtime-design.md:872–874` — `Fault` as the invariant-breach outcome for `Create`/`Fire`,
  `Update` and `Restore`, where `result-types.md:136–142` and `runtime-api.md:281` give `UpdateOutcome` and
  `RestoreOutcome` no fault variant.** The decision sheet dispositions this as descriptive and folds it
  into the open producer question, and the review calls that reasonable. It resolves when that question
  does, which is the item now on the plan's § 4.
- **`graph-analyzer.md:72` — "malformed lifecycle structure would surface as runtime faults during
  evaluation".** A counterfactual about the stage's absence, so not false. `graph-analyzer.md` is not in
  the diff. Left, and named here because it teaches an evaluation-time fault mechanism the rest of canon
  says does not exist, which is worth a row of its own rather than a silent fix in this pass.
- **`tooling/mcp.md:240` — "# Precept Proofs and Runtime Faults".** The third instance of the tool-output
  heading the original pass deliberately left at `:242` and `:269`. The reasoning is unchanged: renaming it
  in the document without renaming it in `tools/Precept.Mcp/` creates drift, and renaming it in both is a
  change to a tool's output contract, which is a different decision.
- **`precept-builder.md:423` ("Nothing is stamped on an opcode for a fault.") and `evaluator.md:984` ("The
  loop runs no fault check.")** — both lead their paragraph with a negation. The review calls both
  defensible and notes canon states the same negation at `precept-language-spec.md:110` and `:112`. Left.

Two Noted prose items **were** acted on, because the review says the diff caused them:

- **"the single source of truth" in two re-authored sentences.** `CLAUDE.md` bans the noun phrase, and
  `git diff` shows both sentences rewritten by the pass. `diagnostic-system.md:551` now reads "The
  attribute is where that map is declared and the only place it is declared"; `proof-engine.md:2500` now
  reads "the only place the `DiagnosticCode` ↔ `FaultCode` map is declared". The third instance,
  `proof-engine.md:1833`, was not touched by the diff and is left.
- **`diagnostic-system.md:626` — the added paragraph reasoned about `DiagnosticCode.StackUnderflow`'s
  message template after the walkthrough above it was rewritten to use a generic member `X`.** The sentence
  no longer names a code the surrounding text does not introduce: "the diagnostic the chain demands would
  have nothing to put in its message template."

---

## 4. The paraphrase sweep, re-run

Run after all edits over `docs/language docs/compiler docs/runtime docs/tooling docs/philosophy.md
docs/compiler-and-runtime-design.md docs/glossary.md docs/contributing CONTRIBUTING.md`, case-insensitive,
`--include=*.md`. The exact type names are included alongside the spaced, hyphenated and paraphrased forms.

| Pattern | Hits | Disposition |
|---|---|---|
| `fault-site` | 0 | — |
| `fault site` | 0 | — |
| `runtime mirror` | 0 | — |
| `runtime-to-compiler` | 0 | — |
| `fault linkage` | 0 | — |
| `second line of defen` | 1 | `philosophy.md:46`, stating the negation — "not a second line of defense standing behind the proof" |
| `three layers` | 0 | — |
| `defence in depth` / `defense in depth` | 0 | — |
| `defence-in-depth` | 0 | — |
| `defense-in-depth` | 1 | `mcp.md:110` — the MCP server's catch-all around its own tool methods; about the MCP process, not the evaluator |
| `Three-Layer` | 1 | `evaluator.md:1875` — a value-representation model (`PreceptValue` / typed lane / raw lane) under Decision 14, unrelated to enforcement |
| `runtime face` | 1 | `descriptor-types.md:38` — "Descriptor types are the runtime face of declarations", about descriptors and not about faults. The fault-side use of the phrase, in `catalog-system.md`, was corrected by the original pass |
| `fault trap` | 3 | `proof-engine.md:1691`, `precept-language-spec.md:110` and `:294` — all three state that no such thing exists |
| `belt-and-suspenders` | 1 | `type-checker.md:1133` — a compile-time re-validation in a slice checklist |
| `backstop` | 0 | — |
| `impossible path` / `impossible-path` | 0 | — |
| `FaultBackstops`, `CC#6`, `CC#12`, `IsFaultCondition` | 0 | — |
| `FaultSite` | 8 | All eight are the drift notes this pass added, at `proof-engine.md:8` and `:272`, `descriptor-types.md:8` and `:195`, `precept-builder.md:943` and `:951`, `evaluator.md:2006` and `:2142`. Each names a type that exists in code, is described nowhere in the design, and is on its way out |

**A wider sweep was also run** for `safety net`, `last line of defen`, `defensive redundancy`,
`redundant check`, `defensive check`, `evaluator fault` and `runtime fault`. `evaluator fault` returns
zero. The rest return only: `compiler-and-runtime-design.md:714` (a contract digest for host applications,
unrelated); `precept-language-spec.md:110` (stating the negation); `graph-analyzer.md:72` (the
counterfactual left above); `compiler-and-runtime-design.md:175` and `soundness-and-coverage.md:77` (the
rejected fault-floor alternative, described as rejected); `precept-language-spec.md:112`, `:225`, `:266`,
`:294` and `collection-types.md:431` (all stating that fault prevention is entirely compile-time); and
`mcp.md:240` and `:242` (the tool-output headings left deliberately).

Every survivor is either canon stating the negation, a rejected alternative recorded as rejected, an
unrelated use of the same word, a tool's output contract, or one of this pass's own drift notes.

---

## 5. What this pass did not do

- **The removal was not reopened.** Every finding applied here is a correction to how the pass was
  recorded or to a document that no longer holds together — none reverses a deletion.
- **No code was changed.** The drift is recorded in the documents and tracked on the plan.
- **`docs/philosophy.md` was not edited**, and neither was anything under `docs/archive/`.
- **Nothing was committed.**
- **No condition was declared passing.** The plan's exit condition 6 now records more failure than it did
  before this pass, not less.
