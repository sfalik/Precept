# Frank History Archive

Archived updates moved from `history.md` during Scribe summarization.

---

## Archive Batch — 2026-05-02T19:42:01Z

---

### 2026-05-01 — Annotation rename and scope audit closed out
- Scribe merged `frank-handlesform-rename.md` into `decisions.md`; the durable per-member annotation name is `[HandlesCatalogMember]`, paired with `[HandlesCatalogExhaustively]`.
- Frank-9's full catalog-enum sweep found no currently-missing distributed-dispatch annotations anywhere in the codebase; centralized switch sites remain correctly covered by CS8509 instead.
- Legacy `[HandlesForm]` mentions in older notes should now be read as historical rename context, not active API guidance.

---

### 2026-05-01 — Analyzer audit and Phase 2e planning tracked
- Frank-9-1 completed the analyzer recommendations audit, wrote `docs/working/analyzer-recommendations.md`, and identified the PRECEPT0020-PRECEPT0023 gap set.
- Frank-10 is appending Phase 2e Slices 28-32 to `parser-gap-fixes-plan.md`; that plan update remained in flight at closeout.

---

### 2026-05-01 — Generic annotation bridge merged to ledger
- Canonical decision merged: the class marker is now `[HandlesCatalogExhaustively(typeof(T))]`, replacing the earlier parameterless `[HandlesExpressionForms]` direction.
- Durable enforcement shape locked as catalog-agnostic: `HandlesFormAttribute(object kind)` pairs with PRECEPT0019 so future catalog enums opt in without analyzer rewrites.
- Session closeout recorded the bridge under the full-vision annotation workstream and cleared the processed inbox artifacts.

---

### 2026-05-01 — Annotation-bridge coverage design recorded
- Recommended the annotation-bridge pattern for expression-form coverage: handler methods claim responsibility with `[HandlesForm(...)]`, while pipeline classes opt in with parameterless `[HandlesExpressionForms]`.
- Locked the preferred enforcement stack as PRECEPT0007 for catalog completeness, PRECEPT0019 for handler-claim completeness, and xUnit for end-to-end parser coverage.
- Rejected parser-structure-coupled analysis as the wrong maintenance tradeoff; durable enforcement should analyze metadata and attributes.

---

### 2026-05-01 — Parser coverage assertion follow-on locked
- Decision merged to canonical squad ledgers: parser coverage against `ExpressionFormKind` is worth doing, but as a follow-on slice rather than an expansion of the current parser-gap slice.
- Durable recommendation: use catalog-side explicit lead-token metadata plus xUnit assertions that parser dispatch handles every declared form.

## Archive Batch — 2026-05-02T19:42:01Z (overflow trim)

---

### 2026-05-01 — Full implementation review of 13-slice parser gap fixes
- Reviewed all 13 slices against plan, spec, and design intent. 9 slices CLEAN, 2 NOT IMPLEMENTED (Slice 2/GAP-A deferred, Slice 13 deferred), 2 CLEAN with acceptable design deviations.
- Key finding: `is set` implementation uses two separate nodes (IsSetExpression/IsNotSetExpression) instead of one with boolean — acceptable improvement. Precedence 60 vs plan's 40 — needs resolution.
- OperatorKind.IsSet/Arity.Postfix never added to catalog — the multi-token operator gap. Shane chose Full DU (Option B) for Phase 2.
- PRECEPT0019 correctly implemented as generic catalog-agnostic analyzer but stays at Warning with suppression because TypeChecker/GraphAnalyzer lack annotations.
- Authored Phase 2 extended plan: 7 work items (A–G), 3 phases, strict dependency ordering, 13-point acceptance gate. No deferrals, no holes.
- Learning: when a plan specifies "add to catalog" but the implementation hardcodes a handler instead, the result works but violates the Completeness Principle — always verify catalog additions were actually made.

---

### 2026-05-01 — Analyzer gap review and Slices 28–32 added to plan
- Authored `docs/working/analyzer-recommendations.md`: full coverage map of 19 existing analyzers, 4 identified gaps (A–D), 2 hardcoded-pattern targets, and prioritized candidate analyzer specs (PRECEPT0020–0023).
- Shane approved all items — no gaps, everything goes in the plan.
- Appended Phase 2e to `parser-gap-fixes-plan.md`: new phase header, Slices 29–32 full specs, process note for `CatalogAnalysisHelpers.CatalogEnumNames`.
- Acceptance gate expanded from 13 to 14 points to include Phase 2e completion.
- Decision inbox entry filed: `.squad/decisions/inbox/frank-analyzer-slices-added.md`.
- Key sequencing: Slice 32 (PRECEPT0023) is deferred pending Phase 2b completion; Slices 29–31 are independent and ready for George.

## Archive Batch — 2026-05-02T19:42:01Z (overflow trim)

---

### 2026-05-01T20:36:28Z — spike/Precept-V2 full review complete
- Frank's full architecture review is now durably recorded as APPROVED with 0 blockers and 3 guidance items for `spike/Precept-V2`.
- George-8 closed G3 by removing the RS1030 `Compilation.GetSemanticModel()` pattern and captured the Phase 3 G2 prerequisite as a TODO; the branch follow-on also includes coordinator commit `4d988d8` with commented-out `ConstraintKind` / `ProofRequirementKind` entries in `CatalogEnumNames` for future activation.
- Soup-Nazi-4 closed all 6 missing-test gaps from the review and the branch finished green at 2687 passing tests, so Frank's approval now stands with implementation follow-through complete.

## Archive Batch — 2026-05-02T19:42:01Z (final compact pass)

---

### 2026-05-02 — Full architecture review of spike/Precept-V2: APPROVED
- Reviewed entire branch (36ccec4..4831cb3): annotation bridge (PRECEPT0019), 4 catalog integrity analyzers (PRECEPT0020–0023), parser fixes (GAP-A/B/C, is-set, method call, list literal, typed constant), ExpressionFormKind catalog (11 members), OperatorMeta DU shape, TokenMeta.IsValidAsMemberName, parser 3-file split, and docs.
- Build clean (1 pre-existing RS1030 warning). 2678 tests passing, 0 failures.
- Verdict: APPROVED with 0 blockers, 3 guidance items (G1: PRECEPT0023c naming, G2: CatalogEnumNames missing ConstraintKind/ProofRequirementKind for Phase 3, G3: pre-existing RS1030).
- Key finding: The `CatalogEnumNames` set in `CatalogAnalysisHelpers` does not yet include `ConstraintKind` or `ProofRequirementKind` — their GetMeta switches still use discard arms. When Phase 3 drops the discard arms, those enums must be added to enable PRECEPT0007 enforcement.
- The annotation bridge pattern is production-ready and catalog-agnostic. Future catalog enums opt in without analyzer changes.
- OperatorMeta DU (SingleTokenOp/MultiTokenOp) with dual indexing (ByToken + ByTokenSequence) is architecturally sound.

---

### 2026-05-02 — Iteration 7 language consistency audit complete
- **GAP-019 closed:** Verified George's implementation. `InvalidCallTarget` (12) is emitted at the infix `LeftParen` branch when `left` is not a `MemberAccessExpression`; `UnexpectedKeyword` (11) is emitted in `ParseAtom` default fallback via catalog-derived `AllKeywordKinds.Contains()`. `AllKeywordKinds = Tokens.Keywords.Values.ToFrozenSet()` is correct. Message templates in `Diagnostics.cs` match spec §2.7 exactly. `DiagnosticCode.cs` "reserved" comments removed.
- **GAP-017 false-Fixed:** Summary table said Fixed but C# changes were never applied (iteration 5 completion comment: "no C# edits"). Applied in iteration 7: Arrow `Cat_Str` → `Cat_Op`; `TwoCharOperators` filter `or Structural` clause removed; doc comment updated. Zero behavior change — Arrow was already scanned correctly via the workaround.
- **GAP-003 incomplete (GAP-025):** Docs fixed (spec + 3 supporting docs) but `Modifiers.cs` not updated. `Notempty.ApplicableTo` still `StringOnly`; spec §3.8 requires string + 8 collection types. Filed as GAP-025 (Unresolved) — TypeChecker implications, needs owner sign-off.
- **GAP-026 new gap:** `Modifiers.cs` `CollectionTypes = [Set, Queue, Stack]` is stale — 6 new TypeKind members (Log, LogBy, Bag, List, QueueBy, Lookup) missing, affecting `mincount`/`maxcount` applicability per spec §3.8. Pure omission with no ambiguity. Deferred pending TypeChecker work.
- **GAP-027 fixed:** `Tokens.cs` `Notempty` description "String constraint: non-empty" → "String or collection constraint: non-empty" (one-word doc fix; catalog description field was not updated during GAP-003 resolution).
- **GAP-028 new gap:** `Functions.cs` `sqrt` has `Integer` and `Decimal` overloads but spec §3.7 says integer/decimal inputs are type errors. Likely a doc-fix-only gap from GAP-004 that missed the catalog. Owner judgment required.
- **Audit status after iteration 7:** 28 gaps total (24 original + 4 new). Fixed: 23. Unresolved: 5 (GAP-024, GAP-025, GAP-026, GAP-028, and... actually: GAP-024, GAP-025, GAP-026, GAP-028). All remaining Unresolved gaps are Doc-Catalog with TypeChecker implications — no further language surface investigation needed before TypeChecker work begins.

- George-7 completed the mechanical propagation of Frank's catalog-agnostic annotation rename: active code, analyzer, tests, and docs now use `[HandlesCatalogMember]`.
- Future architecture guidance should treat `[HandlesForm]` as retired terminology preserved only in historical notes and archived design context.

---

## Archive Batch — 2026-05-02T21:58:21Z (scribe compaction)

---

## Recent Updates

### 2026-05-02 — George review of type checker design (RESPONDED)
- George reviewed `docs/working/type-checker-design-analysis.md` and flagged 6 concrete implementation issues. Frank accepted 5/6 (Finding 5 non-finding: GAP-032 already closed).
- Key design revisions locked: (1) Use existing `FindCandidates`/`FindUnary` + ~15-line qualifier disambiguation, no new indexes. (2) Pre-Slice 0 shape commit required before numbered slices. (3) Array-primary field storage + FrozenDictionary secondary. (4) `ActionSecondaryRole?` enum on `TypedInputAction`. (5) Per-slice `[HandlesCatalogMember]` stub migration mandatory. (6) ContentValidation becomes DU (Regex/NodaTime/ClosedSet). (7) Error recovery: always emit partial results with `TypedErrorExpression`. (8) `QualifierBinding?` on typed binary expressions for qualifier propagation. (9) MethodCallExpression = accessor lookup (Slice 3). (10) InterpolatedString = Slice 3, InterpolatedTypedConstant = Slice 4.
- Response written: `docs/working/frank-response-to-george-review.md`. Revised 10-slice plan included.
### 2026-05-02 — Historical Summary (fully compacted)
- Detailed early-May review and audit entries were moved to `history-archive.md` to keep active context under the size gate.
- Active durable takeaways: the annotation bridge is production-ready, PRECEPT0020–PRECEPT0023 remain the analyzer hardening path, and post-review parser/catalog audits should continue to prefer catalog-derived surfaces over hardcoded parser vocabulary.
- Use `history-archive.md` for the full approval/audit narrative when reconstructing the May 1–2 branch review trail.

### 2026-05-02 — Active focus snapshot
- The branch-level review baseline remains: `spike/Precept-V2` reviewed APPROVED with guidance only, and future PRECEPT0007 activation still depends on adding `ConstraintKind` / `ProofRequirementKind` only after their discard arms are removed.
- The active audit baseline remains: parser gap follow-up should keep `InvalidCallTarget` / `UnexpectedKeyword` behavior, catalog-derived vocabulary sets, and the remaining doc-catalog TypeChecker follow-ons in sync.

### 2026-05-02 — Iteration 10 catalog/spec/doc audit (Frank)

**5 new gaps filed (GAP-043–047). 3 fixed inline; 2 pending owner decision.**

**Regression checks (iteration 9 fixes):** GAP-034 (SimpleCollectionTypeLeaders catalog-derived ✅), GAP-035 (ChoiceLiteralTokens on all 5 ChoiceElement types ✅), GAP-036 (ClearApplicable = {Set,Queue,Stack,Bag,List,QueueBy,Optional} ✅), GAP-037 (Writable hover updated ✅), GAP-039 (AppendBy→LogBy, EnqueueBy→QueueBy consistent ✅), GAP-040 (countof ElementParameterAccessor on BagAccessors ✅), GAP-041 (QuantifierPredicateNotBoolean code 106 ✅), GAP-042 (dead dispatch arms removed ✅). All 8 passed.

**GAP-043 (Fixed):** `catalog-system.md` described the system as 12 catalogs throughout (Status, Overview, inventory table, "Twelve catalogs in two groups", Derive/never-duplicate, Architectural Identity). `ExpressionForms.cs` explicitly says "The 13th catalog" in its doc comment — it was added after the doc was written and the doc was never updated. Fixed: updated all "twelve"→"thirteen" instances (8 edits), added ExpressionForms as #9 in the Language Definition table (renumbering Constraints→10, ProofRequirements→11, Diagnostics→12, Faults→13), updated the escape-hatch clause to "fourteenth aspect", updated "8 language definition catalogs"→"9", and added "expression forms" to the enumerated coverage list in Derive/never-duplicate. Pattern: the 13th catalog entry note in ExpressionForms.cs was correct — the doc just never caught up. Going forward: when a new catalog file says "The Nth catalog" in its comment, that number is the signal that the catalog-system.md inventory needs updating.

**GAP-044 (Fixed):** Spec §1.2 "complete v2 reserved keyword set" code block was missing `queue` and `stack`. Both `QueueType` (ordinal 70) and `StackType` (ordinal 71) have been in the lexer since v1, are in the Tokens catalog, and are excluded from "v2 additions" / "v3 additions" / all removals lists — meaning they were simply omitted from the §1.2 block when it was originally authored. Fixed: added `queue  stack` before `bag list log lookup` in the collection-types keyword line of the §1.2 code block.

**GAP-045 (Fixed):** Spec §2.3 `ChoiceValueExpr := StringLiteral | NumberLiteral | BooleanLiteral` used `BooleanLiteral` as a terminal. `BooleanLiteral` is not a `TokenKind` — the actual tokens are `True` and `False` (text: `true`, `false`). All other boolean references in the spec use `True`/`False` or the keyword text. Fixed: changed `BooleanLiteral` → `true | false` in the grammar production.

**GAP-046 (Unresolved):** Spec §3.7 places `~startsWith`/`~endsWith` in the "Functions catalog" table under "Functions are validated against a closed catalog." But `FunctionKind` has no CI members — CI variants are handled via `HasCIVariant: true` on `StartsWith`/`EndsWith` FunctionMeta + `ExpressionFormKind.CIFunctionCall` in the ExpressionForms catalog. The spec's framing implies a `FunctionKind` entry that doesn't exist. Two valid resolutions: (1) add spec note clarifying the ExpressionForms/HasCIVariant mechanism, or (2) add `CIStartsWith`/`CIEndsWith` to `FunctionKind`. Owner decision required.

**GAP-047 (Unresolved):** Spec §3.7 documents `min`, `max`, `abs`, `clamp`, `round(value, places)` with numeric-only (integer/decimal/number) signatures. The Functions catalog has money and quantity overloads for all five. For `round(money, places)` the spec's "bridge function" framing is also wrong — it's not a lane bridge for money/quantity, it's a rounding-preserving domain operation. Owner must decide: expand §3.7 rows, or add a cross-reference note to business-domain-types.md.

---

## Archive Batch — 2026-05-02T21:58:21Z (full active-history compaction)

---

## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, and tooling work.
- Historical summary: corrected the expression-form catalog boundary, locked the separate `ExpressionForms` catalog shape and MCP propagation, authored the parser-gap implementation plan/spec audit, and established parser coverage as a metadata-backed validation problem rather than a runtime routing problem.

- G1 resolved: `DiagnosticId_MultiLeadCollision` / `MultiLeadCollisionRule` renamed to `DiagnosticId_MultiSequenceCollision` / `MultiSequenceCollisionRule` in `Precept0023OperatorsDUShapeInvariants.cs`. The diagnostic checks the full `(TokenKind, TokenKind?, TokenKind?)` sequence, not just the lead token — the old "MultiLead" name falsely implied a lead-token-only check. Sibling naming pattern (`SingleMultiCollision`, `MultiSequenceCollision`) is now consistent.

## Learnings

- **Research validation pattern established (2026-05-02):** First research validation artifact for a design doc. Pattern: (1) full analysis lives at `research/language/<design-doc-name>-research-validation.md` with front matter citing the validated doc, date, and corpus; (2) the design doc gains a `## Research Validation` section (after Deliberate Exclusions, before Cross-References) summarizing well-grounded decisions, justified divergences, and gaps; (3) working draft in `docs/working/` is marked superseded with a pointer to the promoted location; (4) `research/language/README.md` indexes all validation artifacts in a "Design validation artifacts" table. Key file paths: `research/language/type-checker-research-validation.md`, `docs/compiler/type-checker.md` (§Research Validation), `research/language/README.md` (§Design validation artifacts).

- **Canonical type checker review response (2026-05-02):** Responded to George's canonical review of `docs/compiler/type-checker.md`. Accepted all 5 concerns (CON-1–5), accepted 3 of 4 missing items (MISS-1, MISS-2, MISS-4; rejected MISS-3 transitive widening), resolved all 3 red flags (RF-1–3). Key new decisions locked (D-15 through D-25): single-hop widening, left-first widening fallback, bottom-up + context-retry for literals, EventHandlers have event arg scope, `FieldScopeMode` enum for forward-ref gate, quantifier binding stack with resolution priority (bindings > args > fields), function overload resolution algorithm (arity→exact→widened→retry), Slice 6 stays unsplit, Kramer R3 rejected (anti-mirroring), Kramer R4 accepted as placeholder. All 11 slices now implementation-ready. Canonical doc updated in-place. Critical file path correction: catalogs are at `src/Precept/Language/` not `src/Precept/Catalogs/`. AST class names differ from ExpressionFormKind names (e.g., `CallExpression` not `FunctionCallExpression`, `ParenthesizedExpression` not `GroupedExpression`).

- **GAP-046 design complete (2026-05-02):** Shane chose Option B — add `FunctionKind.TildeStartsWith = 22` and `FunctionKind.TildeEndsWith = 23` to the Functions catalog. Key design decisions locked: (1) `CIVariantOf: FunctionKind? = null` new field on `FunctionMeta` — the catalog-metadata-native way to express the CI→base relationship (inverse of `HasCIVariant`); (2) Parser Tilde null-denotation path does NOT change — it already derives CI-capable names from `HasCIVariant` on base functions, which is unchanged; (3) Overload signatures are `(string, string) → boolean` — `~string` first-arg enforcement is a TypeChecker behavioral rule, not a distinct TypeKind in the overload signature (that would require `TypeKind.CIString` and conflict with the bidirectional assignment-compatibility rule); (4) `ExpressionForms.CIFunctionCall` HoverDocs updated with cross-reference only — the form entry stays, it describes structure not semantics; (5) Spec §3.7 gets a footnote after `~endsWith` row confirming CI functions now have `FunctionKind` entries and clarifying the two-catalog structure; (6) Open deferred concern: no `DiagnosticCode` exists for `~startsWith` called with non-~string first arg (spec says "compile error" but no code is defined) — TypeChecker-slice decision, not blocked. Brief: `.squad/decisions/inbox/frank-gap046-design.md`.

- **Type Checker canonical doc consolidated (2026-05-02):** Replaced the 157-line stub at `docs/compiler/type-checker.md` with the full design spec (~500 lines) consolidated from three working documents: Frank's original analysis, George's implementer review, and Frank's response. All 13+ design decisions locked, SemanticIndex shape fully specified (array-primary + frozen dict secondary), 2-pass/3-sub-pass architecture documented, Pre-Slice 0 + 10 numbered slices with dependency graph, error recovery policy per-declaration type, HandlesCatalogMember migration protocol, and catalog gap status. Working documents in `docs/working/` preserved as design discussion record.

- **Type Checker Design Analysis (2025-07-14):** Completed full 5-section design analysis. Key architectural insight: the type checker is ~70-75% metadata interpreter (catalog lookups for Operations, Functions, Types, Modifiers, Actions) and ~25-30% structural logic (symbol tables, scope, cycle detection, choice validation). Expression typing is almost entirely catalog-driven — the Operations catalog's ~200 entries encode the full type algebra; the checker just queries it. Identified 5 catalog gaps: (1) TypedConstant ContentValidation on TypeMeta (HIGH — prevents per-type switch), (2) Scope rules (SKIP — tiny/structural), (3) TypedActionShape on ActionMeta (MEDIUM — explicit > derived), (4) CI enforcement diagnostics (LOW — stable 5-rule surface), (5) pow ProofRequirement (existing GAP-032). Proposed SemanticIndex uses ImmutableDictionary for symbols (O(1) lookup) and ImmutableArray for normalized declarations (ordered iteration). Resolution architecture: two-pass (registration → checking), where checking has 3 sub-passes: expression resolution engine (the metadata interpreter core), declaration normalization (thin per-construct wiring), structural validation (cycles, choices, cross-validation). Key missing catalog infrastructure: `Operations.BinaryBySignature` and `Operations.UnaryBySignature` frozen indexes for efficient (op, type, type) → OperationMeta lookup. 10-slice vertical decomposition proposed with clear dependency graph.

- **GAP-033 fixed (2026-05-02, Frank):** Stale XML doc comment on `ModifierKind.Notempty` updated: "Flag: string is non-empty" → "Flag: string or collection is non-empty (string + 8 collection types; Lookup excluded)." `StringAndCollectionTypes` in `Modifiers.cs` = String + Set/Queue/Stack/Log/LogBy/Bag/List/QueueBy (9 types). Lookup deliberately excluded — lookup entries are design-time defined. One-line doc-only fix. Same pattern as GAP-027 (Tokens.cs). Build clean.
- **GAP-019 (InvalidCallTarget/UnexpectedKeyword, 2026-05-02):**The "unreachable" comment at the infix LeftParen branch is misleading — it's only true for bare `identifier(` (consumed in ParseAtom), not for `42(args)` or `(A+B)(args)` which reach the branch with non-MemberAccess left operands and silently break. Fix is one line (`EmitDiagnostic(InvalidCallTarget, ...)`) before the break — this is a parser-phase fix, not TypeChecker. `UnexpectedKeyword` has no identified emit site; spec §2.7 should explicitly mark it reserved rather than listing it as active.
- **Diagnostic activation (Codes 11 & 12, 2026-05-02):** Elaine's UX review confirmed DISTINCT failure modes. Updated spec §2.7, catalog (`Diagnostics.cs`), and `DiagnosticCode.cs` doc comments. Key architectural decision: `InvalidCallTarget` category moved from `Naming` to `Structure` — it's a structural parse error (wrong expression kind as callee), not a naming problem. `UnexpectedKeyword` reduced from 2 params to 1 — the old `{1}` context param was vague. Both codes now have FixHints. George owns emission sites only.
- **GAP-024 (bag/list/log TypeQualifier, 2026-05-02):** The parser's catalog-driven qualifier acceptance (element type decides, not collection kind) is architecturally correct per the metadata-driven principle. Qualifier semantics are orthogonal to container behavior — `bag of money in 'USD'` and `set of money in 'USD'` have identical constraint semantics. The spec §2.3 restriction to only set/queue/stack was incomplete notation, not deliberate design. Spec should be updated to match the parser, not the reverse. **RESOLVED 2026-05-02:** Spec §2.3 updated — `TypeQualifier?` added to all four collection forms (`bag`, `list`, `log`, `log by`). No C# changes needed. Owner (Shane) signed off.

- **Dapr-as-runtime research (2026-05-02):** Conducted deep analysis mapping Precept's model onto Dapr building blocks. Key findings: (1) Dapr Actors are the strongest fit for hosting Precept entity instances — single-threaded turn-based execution maps cleanly to Precept's single-event-at-a-time semantics. (2) Dapr Workflows are a poor fit — they model orchestration steps, not constrained state transitions; guards/constraints would be awkward as activity return values. (3) Dapr State Store is the persistence layer regardless of hosting model — the state blob is `{state, fields, etag}`. (4) Pub/Sub is at-least-once and async — fundamentally at odds with Precept's synchronous constraint validation; requires idempotency and dehydrate/rehydrate patterns. (5) The recommended architecture is Actor-hosted Precept instances with state store persistence, service invocation as the `Fire` endpoint, and pub/sub only for external event ingestion (not constraint enforcement). (6) Key tension: Dapr adds distributed infrastructure overhead to what is currently a pure in-memory constraint engine — the value proposition only materializes when you need distributed entity hosting, not just constraint evaluation.

- §0.4.1 "No loops" prohibits general iteration, recursion, and fixpoint-requiring constructs. Bounded quantifier predicates (`each`/`any`/`no`) are not iteration constructs — they unfold to finite conjunctions/disjunctions, require no fixpoint reasoning, introduce no mutable loop variable, and terminate in bounded time over statically-declared finite collections. The spec amendment must draw this line explicitly; leaving it as a design-doc footnote is insufficient.
- The carve-out for bounded quantifiers depends on Expression Purity (§0.4.6) remaining non-negotiable. If predicates could mutate state or observe external context, the distinction between quantifier and loop collapses. The two principles are coupled.
- Philosophy compatibility: every core philosophical commitment (prevention, determinism, inspectability, compile-time-first checking) is satisfied by bounded quantifiers. No tension exists.
- Timing discipline: the spec amendment and the feature implementation belong in the same PR. Amending the spec preemptively introduces aspirational text, which is the exact problem the spec has been cleaned up to avoid.
- Q1 in collection-types.md §Open Questions is resolved by the amendment. That resolution should be recorded in the same PR that lands the amendment and the feature.

- GAP-7/GAP-8/GAP-11 resolved in the language spec: `TypeQualifier` includes `to` for `ExchangeRate`, `contains` documents `BinaryExpression(Contains, ...)`, and number/boolean literals document the unified `LiteralExpression` node.
- GAP-6 resolved: period negation added to spec §3.6. Catalog entry NegatePeriod = 8 in OperationKind.cs. NodaTime Period.Negate() is the backing implementation.
-The two exhaustiveness enforcement strategies (CS8509 for centralized switches, `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` for distributed dispatch) are topology-dependent — the decision is made at the commit introducing the dispatcher, not retrofitted.
- Annotation naming must be catalog-agnostic from day one: `[HandlesForm]` was ExpressionForm-specific naming on a system designed to be universal. Renamed to `[HandlesCatalogMember]` for symmetry with `[HandlesCatalogExhaustively]` and C#'s standard "member" terminology for enum values.
- Catalog inclusion is decided by **language surface**, not by whether a current consumer already needs the data.
- Multi-token operators such as `is set` still belong in the catalog; parser structure and catalog completeness answer different questions.
- Implementation plans must name exact insertion points inside methods, not just the method names.
- GAP-2 does **not** require removing `When` from parser boundary tokens; the boundary is already correct.

- **GAP-029 fixed (2026-05-02, Frank):** `IsOutcomeAhead()` in `Parser.cs` was hardcoding `TokenKind.Transition or TokenKind.No or TokenKind.Reject` instead of deriving from the catalog. Added `OutcomeKeywords` static `FrozenSet<TokenKind>` field derived from `Tokens.All.Where(m => m.Categories.Contains(TokenCategory.Outcome))`, following the same pattern as `ActionKeywords`, `ModifierKeywords`, etc. Updated `IsOutcomeAhead()` to use `OutcomeKeywords.Contains(next.Kind)`. Pure drift-prevention fix — no behavior change since the three tokens are identical to what the catalog holds today. 2690 tests green.
- **GAP-031 fixed (2026-05-02):** Replaced three hardcoded binding power literals in `Parser.Expressions.cs` with direct catalog lookups: `not` → `Operators.ByToken[(TokenKind.Not, Arity.Unary)].Precedence` (25), unary negate → `Operators.ByToken[(TokenKind.Minus, Arity.Unary)].Precedence` (65), `is set` guard → `Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)!.Precedence` (60). Values are identical to what was hardcoded — this is a drift-prevention fix, not a behavior change. `ByToken` keying on `(TokenKind, Arity)` is the correct disambiguation for the `Minus`/`Negate` ambiguity. The `is not set` branch shares the same guard and needs no separate lookup. 2690 tests green. Completed 6-dimension pre-TypeChecker consistency audit. Dimensions 1, 2, 5, 6 clean. Two new gaps: (1) GAP-032 — `Functions.cs` `pow(integer, integer)` overload missing `ProofRequirement` for `exp >= 0`; spec §0.6 item 4 lists this alongside `sqrt` as an explicit non-negative proof obligation; `sqrt` has the correct `NumericProofRequirement`; `pow` has none. Fix: add `NumericProofRequirement(PPowExp, GreaterThanOrEqual, 0m, ...)` to the Integer^Integer overload. (2) GAP-033 — `ModifierKind.cs` line 22 XML comment "Flag: string is non-empty" is stale after GAP-025 expanded applicability; analogous to GAP-027 (Tokens.cs) but in the enum file; one-line fix. Prior gaps GAP-025/026/028 all confirmed fixed. Parser vocabulary sets confirmed fully catalog-derived. `contains`/`is set`/`for` absent from Operations.cs is intentional design. `Dot`/`LeftParen` binding powers 80/90 are legitimate hardcodes (structural grammar constructs, not cataloged operators). Audit closed at 33 gaps total: 28 Fixed, 5 Unresolved.

- **GAP-025/026/028 resolved (2026-05-02, Frank):** Three catalog/implementation mismatches fixed in a single pass. Pattern: doc fixes (GAP-003, GAP-004) from prior iterations updated specs but left the actual C# catalogs (`Modifiers.cs`, `Functions.cs`) stale — the classic "doc-fixed-but-catalog-not-updated" failure mode. Key specifics: (1) `Notempty.ApplicableTo`: added `StringAndCollectionTypes` array (9 types: String + 8 collection kinds; Lookup excluded because lookup entries are defined at design time). `StringOnly` array was NOT removed — still used by `Minlength`/`Maxlength`. (2) `CollectionTypes`: extended from 3 to 9 members by adding Log/LogBy/Bag/List/QueueBy/Lookup — Lookup IS included here (contrast with Notempty) because constraining how many lookup entries exist is meaningful for mincount/maxcount. (3) `sqrt`: Integer→Number and Decimal→Number overloads removed per spec §3.7 "Number-lane only; use approximate() to convert first." Dead `PSqrtInteger`/`PSqrtDecimal` fields cleaned up. All 6 test failures cascaded from 3 catalog changes; test count updates: overload total 49→47, sqrt test assertions trimmed. Pre-TypeChecker audit closed: 28/28 gaps resolved.
- Philosophy/spec wording must match actual runtime guarantees; if they drift, flag the gap rather than silently rewriting either side.
- Durable research should preserve rationale, rejected alternatives, and concrete examples.
- Pratt parsing discovers expression form by reading tokens, so `ExpressionFormKind` is a coverage/validation axis, not a runtime parser routing key.
- Coverage enforcement should consume stable metadata and annotations, not parser implementation internals.

## Recent Updates

### 2026-05-02T21:58:21Z — Canonical type checker batch closed
- Frank's canonical response is now durable: George's 5 concerns were accepted, 3 of 4 missing items were accepted, transitive widening stayed rejected, and the revised checker plan now marks all 11 slices implementation-ready.
- The active checker baseline now also incorporates cross-agent follow-through: Kramer's tooling review remains non-blocking, TypedTransitionRow.ResolvedArgs stays rejected as anti-mirroring, TypedEditDeclaration is retained only as a placeholder, and Soup-Nazi's 450-550 test estimate plus 3 non-negotiable gates define the implementation test bar.
- Supporting artifacts now live in docs/compiler/type-checker.md, docs/working/frank-response-to-george-canonical-review.md, docs/working/kramer-tooling-review.md, docs/working/soup-nazi-test-strategy-review.md, and the research cross-reference at docs/working/type-checker-research-crossref.md.

### 2026-05-02 — Active focus snapshot
- Immediate open design items remain GAP-046 (CI function catalog membership direction) and GAP-047 (spec coverage for money/quantity overloads).
- The canonical checker design is now the implementation baseline; future work must preserve bottom-up plus context-retry literal resolution, array-primary field ordering, single-hop widening, qualifier propagation, and slice-by-slice [HandlesCatalogMember] migration discipline.

### 2026-05-02 — Historical Summary (recompacted)
- Older recent-update detail was moved to history-archive.md during Scribe closeout to keep active context under the 15 KB gate.
- Use the archive for the full early-May slice logs, audit notes, and prior branch closeout narrative.

## Archive Batch — 2026-05-03T02:52:51Z

---

## Recent Updates

### 2026-05-03T00:51:29Z — Outcomes catalog ruling recorded
- Scribe merged Frank's outcomes-catalog batch into `.squad/decisions.md`, cleared both inbox variants, and recorded the manifest outcome as the durable team ruling.
- Durable rule: outcomes stay DU-only; do not add `OutcomeKind` / `Outcomes.cs` unless Shane explicitly reopens the decision. If radical-parser outcome handling needs more structure, keep it at `OutcomeProd()` and token-level metadata.
- The same batch carried forward the construct/action/outcome/slot explanation and noted the paired design-doc updates in `docs/compiler/parser-radical.md` and `docs/compiler/type-checker-radical.md`.

### 2026-05-02T21:03:20-04:00 — Outcomes catalog ruling REVERSED
- Reversed section 0.8 of `docs/compiler/parser-radical.md`: outcomes now get a catalog (`OutcomeKind` + `OutcomeMeta` + `Outcomes.cs`), same two-level pattern as Actions.
- The `no transition` composition gap was the decisive argument — token-level enumeration cannot reconstruct outcome-level abstractions without hardcoding domain knowledge.
- Updated section 0.7 and section 0.8 in `parser-radical.md`, cross-reference in `type-checker-radical.md`, and wrote decision to `.squad/decisions/inbox/frank-outcomes-catalog-revised.md`.
### 2026-05-03T00:26:00Z — Grammar primer docs recorded
- Added §0 "The Grammar of Precept" to docs/compiler/parser-radical.md and a concise cross-reference summary to docs/compiler/type-checker-radical.md.
- Grounded the primer in samples/trafficlight.precept, samples/insurance-claim.precept, and samples/loan-application.precept; the pass confirmed the flat, keyword-anchored grammar thesis rather than changing the design.

### 2026-05-03T00:15:16Z — Radical parser slot field removed
- The radical parser doc now removes `ImmutableArray<ConstructSlot> Slots` from `ConstructMeta`; named parse positions live only as `Tag` nodes inside `Grammar`.
- Tooling/documentation should derive ordered capture names via `ExtractNamedCaptures(ParseRule)` at startup rather than maintain a parallel slot list.
- The parser rebuild recommendation remains Path C only on risk grounds; AI velocity collapses the schedule argument, so unresolved stashed-guard, split-modifier, and variant-action gaps are the only surviving case.

### 2026-05-02T19:11:32-04:00 — Spec challenge response: implicit-knowledge argument withdrawn
- Shane challenged the `implicit grammar knowledge'' regression argument: if the parser was built from spec with AI, a rebuild from the same spec reproduces the same result; any divergence is a spec gap, not a reason to preserve code.
- Conceded fully. The regression argument is dead. Path C recommendation now rests solely on the three unsolved design gaps (stashed-guard, split-modifier, variant-action). If those are resolved on paper or shown to be spec-covered, Path C has no remaining case.
- Response written to docs/working/frank-spec-challenge-response.md.

### 2026-05-02T22:22:24Z — Iteration 11 audit session recorded
- Scribe merged Frank's iteration-11 findings into the canonical ledger, cleared all current decision inbox files, and wrote the audit closeout logs.
- Cross-agent context to retain: the durable batch now bundles the spike type-checker directive, both catalog-driven type-checker reviews, GAP-047 closure, Frank's GAP-048–056 doc/catalog gaps, and George's GAP-062–067 catalog-impl gaps.
- Health gate result: decisions archive ran under the 7-day rule before merge; no history summarization was required after propagation.

### 2026-05-02T22:22:24Z — Iteration 11 doc/catalog audit pass
- Filed GAP-048 through GAP-056 in the language-consistency ledger; Frank's pass added 9 unresolved doc/catalog gaps. Combined ledger state now stands at 64 total gaps, 49 fixed / 15 unresolved after the parallel iteration 11 catalog-impl pass.
- The dominant pattern is catalog lag behind the spec on declaration-shape metadata: guarded ensures, guarded state actions, and stateless event-hook trailing `ensure` all exist in the spec without matching `Constructs`/`Constraints` metadata.
- Queue-by semantics now need owner clarification in two places: whether `ascending` / `descending` belongs in the type catalog, and whether `dequeue ... by H` means keyed selection or something else.

### 2026-05-02T21:58:21Z — Canonical type checker batch closed
- Frank's canonical response is now durable end-to-end: George's 5 concerns were accepted, 3 of 4 missing items were accepted, transitive widening stayed rejected, and the checker plan now marks all 11 slices implementation-ready.
- Cross-agent follow-through is part of the active baseline: Kramer's tooling review remains non-blocking but derivation-first, and Soup-Nazi's 450-550 test estimate plus 3 non-negotiable gates define the expected checker validation bar.

### 2026-05-02 — Active focus snapshot
- Immediate open design work has shifted back to checker implementation: GAP-047 is now closed, while the rest of the checker shape questions are locked in docs/compiler/type-checker.md.
- Use docs/working/type-checker-research-crossref.md, docs/working/kramer-tooling-review.md, and docs/working/soup-nazi-test-strategy-review.md as the supporting context set behind the canonical checker doc.

### 2026-05-02 — Historical Summary (fully compacted)
- Older active-history detail was moved to history-archive.md during Scribe closeout to keep Frank under the 15 KB gate.
- Use the archive for the earlier Dapr research notes, gap-by-gap audit trail, and prior batch closeout sequence.

### 2026-05-02T22:14:44Z — GAP-047 closed
- Spec §3.7 now explicitly documents the money/quantity overloads for `min`, `max`, `abs`, `clamp`, and `round(value, places)`, including same-qualifier requirements and qualifier-preserving results.
- The working gap ledger is fully closed for this audit pass: GAP-047 is Fixed, and the primitive numeric-lane shorthand is now explicitly separated from domain-type overload semantics.

### 2026-05-03T01:07:30Z — Outcomes catalog reversal recorded
- Scribe corrected the canonical ledger to match Frank-5's reversed ruling: outcomes now use the two-level catalog pattern (`OutcomeKind` + `OutcomeMeta` + `Outcomes.cs`) while retaining `OutcomeNode` as the syntax-layer DU.
- Durable reason to keep front-of-mind: `no transition` is one outcome-level abstraction composed from two tokens, so token-category derivation alone cannot provide complete outcome enumerability without hardcoded composition logic.

- **Upstream pipeline coverage completed (2026-05-02):** Extended `docs/working/catalog-driven-pipeline.md` with §3.0.1 (Lexer), §3.0.2 (Parser), §3.0.3 (Precept Builder) — the three stages that sit before the type checker. Key findings: the lexer is already ~95% catalog-driven (keyword/operator/punctuation tables derived from `Tokens.All`); the parser under the radical design achieves ~85% (construct dispatch generic, Pratt loop irreducible); the precept builder is the MOST catalog-drivable stage of all — pure structural assembly with a vanishingly small irreducible kernel (cross-construct name resolution only). The thesis update now lists all 8 pipeline stages. The builder is identified as the natural proof-of-concept stage for the catalog-driven inversion.

- **Radical AST options explored (2026-05-02):** Explored 6 options for eliminating per-construct AST node classes (Universal bag, flat array, source-generated, no-AST, CST-only, hybrid generic+typed). The hybrid (Option F: generic `ParsedConstruct` internal + thin typed accessor functions at consumption boundaries) is the most promising — it makes the "parser is untouched" claim fully true while preserving type safety via ~5-line accessor functions per construct. The key tradeoff Shane is weighing: loss of C# pattern matching on node types (ergonomic regression) vs. elimination of per-construct AST classes (architectural purity). Option C (source generation) is the fallback if type safety cannot be compromised. CST-only (E) and raw array (B) are rejected as over-engineered or too fragile for Precept's problem size.

### 2026-05-03T01:07:30Z — Radical AST options note recorded
- Scribe merged Frank's late-arriving AST design note into the ledger as a pending-owner-ruling record: Option F keeps generic `ParsedConstruct` internally, thin typed accessors at consumer call sites, and typed MCP DTOs at the boundary.
- Durable tradeoff: the hybrid model preserves parser zero-touch growth but replaces node-type pattern matching with `ConstructKind` dispatch plus accessors; Option C remains the explicit fallback if that ergonomics cost is rejected.
