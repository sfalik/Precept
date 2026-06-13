# Appendix — Collections + Grammar Docs Audit (Sub-Agent Report)

**Date**: 2026-05-24
**Scope**: `docs/language/collection-types.md` (1,517 lines) + `docs/language/precept-grammar.md` (939 lines) vs implementation
**Author**: Sub-agent of the 2026-05-24 compiler-readiness review
**Findings**: 17 — 6 P0, 5 P1, 3 P2, 3 P3
**Parent doc**: [../compiler-readiness-review-2026-05-24.md](../compiler-readiness-review-2026-05-24.md)

## § A. Methodology

Two docs read in full. Cross-checked against catalog (`TypeKind.cs`, `Types.cs`, `ActionKind.cs`, `Actions.cs`, `ModifierKind.cs`, `ConstructKind.cs`, `Constructs.cs`, `ConstructSlot.cs`, `ExpressionForms.cs`, `OperatorKind.cs`, `Operators.cs`, `Operations.cs`, `DiagnosticCode.cs`, `Diagnostics.cs`, `TokenKind.cs`), pipeline (`Parser.cs`, `Parser.Actions.cs`, `Parser.Types.cs`, `Parser.Expressions.cs`, `TypeChecker.cs`, `TypeChecker.Expressions.cs`, `TypeChecker.Expressions.Callables.cs`, `ProofEngine.Diagnostics.cs`, `SemanticIndex.cs`), MCP exports (`precept_types`, `precept_syntax`, `precept_operations`), and test surface (`TypeCheckerCollectionSafetyTests.cs`, `TypeCheckerQuantifierTests.cs`, `TypeCheckerOmitLookupTests.cs`, `ProofEngineTests/CollectionMutationProofTests.cs`, etc.).

## § C. Drift Findings (Full)

### F-LANG-COLL-04 — `.at(N)` index-bounds proof obligation missing [P0]
**Evidence**: `collection-types.md:319,399,677-680,1017` claims `N >= 0 and N < F.count`. `Types.cs:205-211,229-235,259-265` declares only `NumericProofRequirement(self.count > 0)`.
**Description**: Documented compile-time safety silently absent. `Items.at(99)` on 3-element list accepted.
**Fix**: Introduce `IndexBoundsProofRequirement` (subject = index expression, lower = 0, upper = `F.count`). Attach to `.at` on Log/LogBy/List. Wire proof engine. Update tests.
**Effort**: L

### F-LANG-COLL-05 — `log of T by P` append uniqueness has no static obligation [P0]
**Evidence**: `collection-types.md:366,374,413` claims `append F Expr by P` requires `when not (F contains P)` guard. `Actions.cs:145-152` AppendBy has **no `ProofRequirements`**.
**Description**: Unguarded `append AuditLog X by P` against duplicate `P` statically accepted; would only surface (if at all) at runtime.
**Fix**: Add `KeyAbsenceProofRequirement` to `AppendBy`. Extend `ProofEngine.Diagnostics.cs` to map to `UnguardedCollectionAccess` (or new `DuplicateOrderingKey` code). Tests.
**Effort**: L

### F-LANG-COLL-06 — Qualified inner types (`set of money in 'USD'`) cannot be parsed [P0]
**Evidence**: `collection-types.md:535-596` ~60 lines on qualified inner types. `Parser.Types.cs:203-230` `ParseInnerTypeReference` reads only bare type token + `~` prefix. Zero tests, zero samples.
**Description**: Level-A feature documented but unparseable.
**Fix**: Extend `ParseInnerTypeReference` to invoke `TryParseQualifiers`. Propagate qualifiers to element type for SI-base normalization. Add sample (`samples/qualified-collection-inner-types.precept`) + tests.
**Effort**: L

### F-LANG-COLL-07 — `queue of T by P` two-field quantifier binding not implemented [P0]
**Evidence**: `collection-types.md:1175-1214` documents two-field binding `.value` (T), `.by` (P). `TypeChecker.Expressions.Callables.cs:385-433` `ResolveQuantifier` pushes single `(name, elementType, isCI)` tuple.
**Description**: Rule `rule no claim in ClaimQueue (claim.by == "critical")` is compile error today. Doc itself flags as Open Question §1255.
**Fix**: Extend binding shape — represent two-type-parameter collection element bindings as structural projection with `value`/`by` accessors. Wire `ResolveIdentifier`/`ResolveMemberAccess`. Tests.
**Effort**: L

### F-LANG-COLL-08 — `CollectionOperationOnScalar` (47) + `ScalarOperationOnCollection` (48) defined but never emitted [P0]
**Evidence**: `collection-types.md` cites 25+ times across nine collection kinds. `DiagnosticCode.cs:99-100` defines codes; `Diagnostics.cs:457-465` defines text. **`grep -rn` shows zero `Diagnostics.Create(DiagnosticCode.CollectionOperationOnScalar, …)` calls anywhere in `src/Precept/`.** TC.Callables.cs:107 reads `Actions.GetMeta(...).ProofRequirements` but **never reads `ApplicableTo`**.
**Description**: Half the documented type-error matrix is vacuous. Applying `enqueue` to `set`, `push` to `queue`, `clear` to `log`, or any wrong-kind action produces NO diagnostic.
**Fix**: In `ResolveAction` (TC.Callables.cs:102), after `ResolveActionTarget`, look up `Actions.GetMeta(parsedAction.Kind).ApplicableTo` and verify target's `TypeKind` admitted. Emit `CollectionOperationOnScalar` for wrong-kind collection actions; emit `ScalarOperationOnCollection` for scalar action on collection field. Add regression matrix test (one per documented reject case).
**Effort**: M

### F-LANG-GRAM-02 — `EventRowReject` referenced 4× in grammar doc but doesn't exist [P0]
**Evidence**: `precept-grammar.md:228,427,473,901` reference `EventRowReject`. `ConstructKind.cs` has `EventRow` (12), `ConstructionRow` (19), `ConstructionRowReject` (20), `TransitionRowReject` (21) — **no `EventRowReject`**. `Parser.cs:284-317` `ResolveRejectVariant` promotes `EventRow` → `ConstructionRowReject`. `SemanticIndex.cs:499` uses `TypedEventRowReject` (correct name on typed side).
**Description**: Grammar doc vocabulary wrong. Reader searching code for `EventRowReject` finds nothing. Naming collision across pipeline stages (parser uses `ConstructionRow*`, type checker uses `EventRow*Reject`) compounds confusion.
**Fix**: Rename `ConstructKind.ConstructionRowReject` → `ConstructKind.EventRowReject` in `ConstructKind.cs`. Update `Constructs.cs`, `Parser.cs`, `TypeChecker.Normalization.cs`, `GraphAnalyzer.cs:762`.
**Effort**: M

### F-LANG-COLL-09 — `insert F at N`/`remove F at N` proof obligations don't constrain index [P1]
**Evidence**: `collection-types.md:991,993,1032`. `Actions.cs:154-166` (Insert) declares `NumericProofRequirement(self.count, GreaterThanOrEqual, 0m)` — tautology (`.count >= 0` always true). `Actions.cs:168-180` (RemoveAt) declares `self.count > 0` — only non-emptiness.
**Description**: Index-bounds for insert/remove-at not enforced; only `RemoveAt` checks non-emptiness.
**Fix**: Introduce parametric `IndexBoundsProofRequirement` (subject = index sub-expression, bounds = 0 to F.count, inclusive for insert / exclusive for remove). Replace current obligations.
**Effort**: L

### F-LANG-COLL-10 — `notempty` applicability on `lookup` contradicts catalog [P1]
**Evidence**: `collection-types.md:739` "applies to … `lookup`". `Types.cs:711` `NotemptyApplicable: false`. MCP `precept_types` confirms.
**Description**: Doc/catalog contradiction. `field X as lookup … notempty` fails with `InvalidModifierForType`.
**Fix**: Either remove `lookup` from doc (lookup uses `KeyPresenceSafety` instead) — RECOMMENDED, OR flip catalog to true.
**Effort**: S

### F-LANG-COLL-11 — `MissingOrderingKey` (PRE0104) repurposed [P1]
**Evidence**: `collection-types.md:438` reserves for missing-`by`-clause on `append`. `TC.Callables.cs:880` emits PRE0104 for any RequiredTraits violation (notably `.min`/`.max` on non-orderable inner types).
**Description**: Code 104 doing different work than doc describes.
**Fix**: Rename PRE0104 → `RequiredTraitViolation` in DiagnosticCode/Diagnostics. Allocate new code for "missing `by P` on append", OR route through `CollectionOperationOnScalar` (after F-LANG-COLL-08 fixed). Update doc.
**Effort**: M

### F-LANG-GRAM-01 — Construct count: doc says "14", code has 15 [P1]
**Evidence**: `precept-grammar.md:211`. `ConstructKind.cs:6-59` has 15. Extra is `ConstructionRow` (kind 19).
**Fix**: Clarify status of `ConstructionRow` (per `Constructs.cs:191` comment, parser no longer produces it — vestigial DU kind for `TypedEventRowReject` promotion). Either delete the enum value (preferred) or document its role. Update doc count.
**Effort**: M

### F-LANG-GRAM-03 — Slot kind count: doc says "18", code has 20 [P1]
**Evidence**: `precept-grammar.md:534`. `ConstructSlot.cs:7-29` has 20 (extras: `SuccessOutcome` (19), `EventEntryList` (20)).
**Fix**: Update count to "20" and add missing rows to table (lines 555-556). For `SuccessOutcome`, clarify relationship to `Outcome`.
**Effort**: S

### F-LANG-GRAM-04 — EventDeclaration slot decomposition incorrectly documented [P1]
**Evidence**: `precept-grammar.md:298-305` says `IdentifierList + ArgumentList + InitialMarker`. `Constructs.cs:90-102` uses single `[SlotEventEntryList]`.
**Fix**: Rewrite §3 to describe `EventEntryList` as composite list slot.
**Effort**: S

### F-LANG-GRAM-06 — ExpressionFormKind count: doc says "14", code has 15 [P1]
**Evidence**: `precept-grammar.md:645`. `ExpressionForms.cs:8-40` has 15 (extra: `InterpolatedTypedConstant`).
**Fix**: Update doc count and add `InterpolatedTypedConstant` to table (lines 647-655).
**Effort**: S

### F-LANG-COLL-03 — `set of choice of T(...) ordered` ordering trait propagation unverified [P2]
**Evidence**: `collection-types.md:132`. `TC.Callables.cs:870-885` PRE0104 RequiredTraits check. Trait propagation onto inner `Choice` type in `Types.cs` choice-meta unverified.
**Fix**: Add positive `.min`/`.max` test once F-LANG-COLL-02 resolved. Fix type-meta if trait doesn't propagate.
**Effort**: M

### F-LANG-GRAM-05 — `Outcome`/`RejectClause`/`SuccessOutcome` conflation in appendix [P2]
**Evidence**: `precept-grammar.md:926` lumps "Outcome / ActionChain" one row. `ConstructSlot.cs:9,17,26,27` has distinct slot kinds.
**Fix**: Split appendix row into three. Amend `Outcome` description in §5 line 547.
**Effort**: S

### F-LANG-GRAM-07 — Expression tree representation documented as "deferred" but implemented [P2]
**Evidence**: `precept-grammar.md:672-674` says only `SourceSpan` stored, full tree deferred. `ParsedExpression.cs` has full DU hierarchy. `Parser.Expressions.cs` builds trees.
**Fix**: Replace §6.4 with implementation note pointing to `ParsedExpression.cs`. Remove "open design question" framing.
**Effort**: S

### F-LANG-GRAM-08 — Catalog count: doc says "13", code has ≥14 grammar-relevant catalogs [P2]
**Evidence**: `precept-grammar.md:800`. `src/Precept/Language/` includes `Outcomes.cs` (grammar-relevant, consumed by parser/TC for terminal outcome dispatch) not in the §9 catalog list.
**Fix**: Add `Outcomes` to §9 table. Update "13" → "14".
**Effort**: S

### F-LANG-COLL-01..02, F-LANG-COLL-12 — P3 informational [P3]
- F-LANG-COLL-01: `lookup` `contains` confirms match.
- F-LANG-COLL-02: `set of choice of T(...)` v1 limit enforced by parser; user-facing diagnostic could be more specific.
- F-LANG-COLL-12: **CLOSED AS AUDIT ERROR (2026-05-26, Phase 4 W-F).** Initial finding called `Countof` (137) / `Peekby` (138) "inert." Re-grep confirms both are live keyword tokens for `bag.countof(E)` (Types.cs:245 — `ElementParameterAccessor`) and `queue-of-T-by-P.peekby` (Types.cs:281 — `TypeAccessor`; TypeChecker.Expressions.Callables.cs dispatches on `accessor.Name == "peekby"`); full TokenMeta entries (Tokens.cs:413-415); MemberNameValid coverage (TokenMetaMemberNameTests.cs:13-14); parser tests (MemberAccessTests.cs:33-37, 76-80, 167-177); accessor-presence tests (TypesTests.cs:594-598, 602-607, 692-697); VS Code grammar (tmLanguage.json:117, 839); doc (collection-types.md:640-641). `.squad/decisions-archive.md` lock note: *"`countof` / `peekby` stay as member-name-legal compound accessors."* **No code change required.**

## § D. Implementation-Only Features

1. `ConstructionRow` and `ConstructionRowReject` ConstructKinds — not in grammar doc table.
2. `SuccessOutcome` ConstructSlotKind — not in grammar doc slot table.
3. `EventEntryList` ConstructSlotKind — not in grammar doc slot table.
4. `InterpolatedTypedConstant` ExpressionFormKind — not in grammar doc form table.
5. `Outcomes` catalog — not in grammar doc §9 catalog list.
6. `SlotPreVerbGuardModify` slot variant.
7. `FieldTarget` with vocabulary "field name or 'all'" — `all` form for stateless-precept `edit` declarations (deferred per D24).
8. `DynamicObligationGenerator` on `Actions.Set` (Actions.cs:73,227-285).
9. `ParsedConstruct`, `ParsedExpression`, `ParsedAction` DU hierarchies with full slot data — grammar doc §6.4 implies deferred; implemented.
10. `SemanticTokenTypes`, `Quickstart` catalogs — tooling-adjacent.

## § E. Confidence Statement

- **`collection-types.md` — Confidence: Medium-Low (5/10).** Structural vocabulary correct (9 collection kinds, 15 actions, diagnostic codes, accessor names, proof infrastructure all exist). Loses fidelity in enforcement: PRE0047/0048 defined but never emitted (F-08), `.at(N)`/positional cite proof requirements not encoded (F-04, F-09), `log of T by P` uniqueness has no `ProofRequirements` (F-05), qualified inner types unparseable (F-06), two-field quantifier projection unimplemented (F-07), `notempty` on `lookup` doc/catalog contradiction (F-10). These are P0/P1 findings, all in proof and parse surface. Simpler operations on set/queue/stack/bag/list/log are accurate.
- **`precept-grammar.md` — Confidence: Medium (6/10).** High-level grammar design principles accurate (flat constructs, keyword anchoring, catalog-driven dispatch, pre-verb guard handling, six invariants). Primary failure mode: counting and naming. "14 constructs" → 15, "18 slot kinds" → 20, "14 expression forms" → 15, "13 catalogs" → ≥14, `EventRowReject` (named 4×) doesn't exist, `EventDeclaration` slot breakdown fictional, expression-tree "open design question" resolved in code but unresolved in doc.

The pattern across both docs: **structural design correct; enumeration accuracy stale; proof-obligation depth documented at higher level than catalog encodes.** Single most load-bearing fix for runtime-readiness: **F-LANG-COLL-08** (action `ApplicableTo` enforcement) — without it, half the documented type-error matrix is vacuous and user writing `enqueue MyStack X` gets no diagnostic.

**Findings summary by severity**:
- **P0 (6)**: F-LANG-COLL-04, -05, -06, -07, -08, F-LANG-GRAM-02
- **P1 (5)**: F-LANG-COLL-09, -10, -11, F-LANG-GRAM-01, -03, -04, -06
- **P2 (3)**: F-LANG-COLL-03, F-LANG-GRAM-05, -07, -08
- **P3 (3)**: F-LANG-COLL-01, -02, -12
