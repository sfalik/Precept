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
