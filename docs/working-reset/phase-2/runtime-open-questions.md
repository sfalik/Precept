# Phase 2 — filing the parked fault question, and indexing the runtime documents' open questions

Uncommitted working-tree edits, made 2026-07-28 against HEAD `2ec4e2e9`. Five files touched, all under
`docs/runtime/`: `evaluator.md`, `precept-builder.md`, `descriptor-types.md`, `fault-system.md`, `README.md`.
Nothing under `src/`, `tools/`, `docs/philosophy.md` or `docs/archive/` was touched, and no open question was
resolved.

## Why

Three of the six runtime documents carried an `Open Questions / Implementation Notes` section — `runtime-api.md`,
`fault-system.md`, `result-types.md`. Three did not — `evaluator.md`, `precept-builder.md`, `descriptor-types.md` —
and all three carried open questions anyway, written inline in the middle of whatever section raised them. Nobody
could find them. One of them was invisible until an independent review went looking for something else.

## What was filed — the parked question

**The question.** When Precept itself is broken — a defect in the compiler, the builder or the evaluator, not
anything an author can write — does the caller get a structured outcome or an exception? Nothing constructs a
`Fault`; `Fault`, `FaultCode` and `EventOutcome.Faulted` are declared with no producer.

Canon says both things:

- `compiler-and-runtime-design.md` names the evaluator as where such a breach surfaces.
- `evaluator.md` § No Unclassified Exceptions and `precept-builder.md` § Builder Invariant Violations both say a
  corrupted model is an unrecoverable exception that should not be caught.
- `result-types.md` carries a design commitment that faults are *returned as a structured value inside the outcome
  hierarchy, never thrown* — which making engine defects exceptions would drop.

**The owner's disposition, recorded as given.** He has **parked** the question until the runtime design is done.
His stated lean is **exception**. His words: *"my vote would be exception, but I think we should park this until we
get to the runtime design."*

**A lean is not a ruling, and every place this is written says so.** Each of the four sites states that nothing may
be built, written or planned as though exception were the answer, and that whoever takes the question up at runtime
design time makes the call and may land the other way. The `result-types.md` commitment is stated as standing until
then, so that a reader who finds only the lean does not conclude the commitment has already been overridden.

**Where it now appears:**

| Site | What it carries |
|---|---|
| `fault-system.md` § Q3 | The full record — parked, the lean, that a lean is not a ruling, and that answering it closes both statements of the question. This is the home of the disposition; the other three point here. |
| `evaluator.md` § 9 Fault Production | Two sentences added to the existing inline blockquote — parked, the lean, do not build against it, pointers to § 13 and to Q3. The surrounding prose is unchanged. |
| `evaluator.md` § 13 Open Questions | Index entry, with the parked status and the lean stated in one line. |
| `precept-builder.md` § 13 Open Questions | Index entry stating that this document is party to the question, because § 9 Builder Invariant Violations is one of the two accounts that disagree. |

## One question or two — `fault-system.md` Q3

**Judgement: one question, written in two places. Neither statement was deleted or merged.**

Q3 is "How far the evaluator asserts about its own state." Its first half — about the author's data — is already
closed there. What remains asks whether the evaluator asserts, faults, or does neither about the engine's own state,
"and therefore whether anything ever constructs a `Fault`." That is the same thing `evaluator.md` asks, it cites the
same two disagreeing accounts (`precept-builder.md` § Builder Invariant Violations and `evaluator.md` § No
Unclassified Exceptions against `compiler-and-runtime-design.md`), and it cannot be answered one way in one document
and the other way in the other.

Both statements are kept because each is written for the reader of its own document — Q3 from the fault system's side
(does anything produce a `Fault`?), `evaluator.md` from the evaluator's (does the evaluator carry a detection
mechanism?). Neither is a junior copy of the other. Q3 now says explicitly that they are one question; the README
table lists them on one row.

## What was indexed

Each document got an `Open Questions` index: one line per question saying what somebody has to decide, and a pointer
to the section where the case for it is written. **No inline prose was moved.** The discussions depend on the material
around them, and cutting them out would make both places worse. The indexes exist to make the questions findable,
not to become their new home.

### `evaluator.md` § 13 — eleven entries

Section 13 was renamed from `Implementation Notes` to `Open Questions / Implementation Notes`, matching the heading
the three documents that already had one use, and `### Open Questions` was added as its first subsection. No section
was renumbered. Contents updated.

| Question | Indexed from |
|---|---|
| Structured outcome or exception for a defect in Precept itself (the parked one) | § 9 Fault Production |
| Which type carries what inspection reports where execution has no constraint verdict to preview | § 7.2 Inspection Operations |
| When a state carries more than one state action, whether all fire and in what order | § 6 (owned by spec § 3A.4) |
| Whether entry actions fire at construction and on a self-transition | § 6 (owned by spec § 3A.4) |
| Whether an `Update`'s ingress check is held to the field's own constraints and nothing wider | § 7.6 (disputed in spec § 0.7) |
| Four opcode semantics — `LOAD_ARG` on absent input, `BRANCH_FALSE` on numeric zero, fall-through past `RETURN`, stack pooling | § 7.3 |
| The internal field layout of `PreceptValue` | § 5 |
| Trace record shape, how a trace context is attached, how LS and MCP read it | § 11 Decision 8 |
| Working copy slot array — `stackalloc` or heap | § 13 Implementation Status |
| `Version.Slots` ownership model | § 13 Implementation Status |
| `FieldDescriptor.AccessModes` storage shape | § 7.5 and § 13 |

### `precept-builder.md` § 13 — eight entries

Same treatment: section 13 renamed, `### Open Questions` added first, no renumbering, Contents updated.

| Question | Indexed from |
|---|---|
| What happens when the certificate gate refuses | § 9 The Certificate Gate |
| How the certificate reaches `Precept.From` at all | § 9 The Certificate Gate |
| Whether the `ConstraintPlanIndex` keeps its five state-and-event anchors | § 12 |
| `FieldDescriptor.AccessModes` storage shape | § 7 Pass 1 |
| Whether `ConstructMeta` should carry `ModelContribution` metadata | § 13 Pending Design Decisions |
| Stack machine or register allocation for expression compilation | § 13 Pending Design Decisions |
| Opcode array — discriminated union or flat struct array | § 13 Pending Design Decisions |
| Structured outcome or exception for a defect in Precept itself | pointer only; § 9 Builder Invariant Violations is one of the disagreeing accounts |

### `descriptor-types.md` — two entries

This document had no section 13 equivalent under that name; it had `## Notes`, sitting in exactly the template
position the other documents use for `Open Questions / Implementation Notes`, holding a note that duplicates a
paragraph in § Descriptor Shapes. `## Notes` was renamed to `## Open Questions / Implementation Notes`, with
`### Open Questions` added above the existing note, which is kept as `### Notes` and unchanged. Contents updated.

| Question | Indexed from |
|---|---|
| What replaces `ConstraintDescriptor.ReferencedFields`, currently a provisional flat list of field names | § Descriptor Shapes; waits on the attribution model that `evaluator.md` § 7.2 leaves open |
| `FieldDescriptor.AccessModes` storage shape | written in `precept-builder.md` § Pass 1 and `evaluator.md` § 7.5; indexed here because the type it changes is this document's |

**One judgement call to flag.** The `AccessModes` question is not written inside `descriptor-types.md` — that
document's printed `FieldDescriptor` shape does not carry the property at all. It is written in the two sibling
documents, about a type this document owns. Indexing it here is a pointer, not an invention, and the entry says so
in those words. If that reads as over-reach it is one line to remove.

## `docs/runtime/README.md`

A `## Open questions` section was added between § Reading Order and § Relationship to Other Docs, using the two-column
table form the README already uses. Twenty-one rows — every genuinely open question across all six documents,
deduplicated, ordered by the document that owns it, each row naming what somebody has to decide and where it is
discussed. Questions written in more than one document (the parked fault question, `AccessModes`) get one row that
names every site.

A closing paragraph names the entries that sit inside an `Open Questions / Implementation Notes` section but are not
open, so that a reader who counts entries and counts rows does not think the table is short. See the next section for
which, and why.

## Written as undecided, judged not a genuine open question

Each of these was found by the sweep and deliberately left out of the indexes and the README table.

| Item | Where | Why it is not an open question |
|---|---|---|
| Stateless precepts — `CreateInitialVersion` | `runtime-api.md` § Open Questions | Locked. The section itself says "Locked by CC#26 (2026-05-06) — Option 1". It sits under an Open Questions heading but states a decision and its full contract. |
| Q1 — evaluator result type | `fault-system.md` § Open Questions | Resolved, and titled "(resolved)". The options table is the record of how it was settled, not an open choice. |
| Arg-dependency sets; transitive unresolvability propagation; Kleene three-value guard evaluation | `result-types.md` § Open Questions | All three are requirements, not choices. The section opens "recorded here as binding requirements from R2" and each entry says what the executable model **must** carry. Nobody has to decide anything; somebody has to build it. This is the "/ Implementation Notes" half of that heading doing its job. |
| The lazy-load `ICollectionBacking` seam | `evaluator.md` § 7.4.1 | A decision that was made: ship `PreceptValue[]`, do not implement the interface, introduce it only when large-entity patterns prove the need. Deferred work with a stated trigger, not an undecided design. |
| `FaultSiteDescriptor` still declared in `Descriptors.cs`; `FaultSiteLink`, `FaultSiteAnnotation` and `ProofLedger.FaultSiteLinks` still declared | `evaluator.md` § 13, `precept-builder.md` § 16, `descriptor-types.md` § Source Files | Decided and not done. All three documents state the reasoning — an obligation that cannot be discharged refuses the definition, so there is no runtime residue to plant — and then say the removal has not been made. That is a work item, not a question. |
| `ConstraintViolation` full shape and population | `evaluator.md` § 13 Resolved Design Questions | The document says it outright: "an implementation milestone, not a design question." The 5-field shape is the designed public contract. |
| MCP DTOs still using string representations | `descriptor-types.md` § MCP DTO Updates | A consequence of the builder not populating descriptors yet. Nothing to decide; the update follows the evaluator being wired. |
| `Precept.From` throwing `NotImplementedException`; opcode and index types designed but not implemented | `evaluator.md` and `precept-builder.md` § 13 Implementation Status | Build status, which is what that subsection is for. |

One borderline call worth naming: `precept-builder.md` § 13 item 9, register-based against stack-based expression
compilation, sits under a heading that calls it a pending design decision but states the choice — "Stack VM is the
current choice; register allocation is a future optimization if profiling demands it." It was indexed, because the
document files it as pending, and the index line says accurately that a stack machine is the current design and the
move is left open against profiling evidence, rather than presenting it as an unmade choice.

## What this pass did not do

- No open question was resolved, narrowed, or answered.
- No inline discussion was moved, cut, or rewritten. The only additions to existing prose are the two sentences in
  `evaluator.md` § Fault Production recording the parked status, and the disposition paragraphs in `fault-system.md`
  § Q3.
- No section was renumbered in either numbered document.
- Nothing outside `docs/runtime/` was touched.
