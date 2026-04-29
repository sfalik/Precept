## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: led feasibility passes for analyzer/runtime details, parser guardrails, catalog-consumer drift, and diagnostic exhaustiveness discipline.

## Learnings

- The highest implementation payoff comes from eliminating hardcoded parallel copies of catalog knowledge while keeping parser/checker mechanics explicit and hand-authored.
- Exhaustiveness invariants need compile-time or pinned-test enforcement: `BuildNode` switch arms, diagnostic metadata, and slot ordering all need dedicated guards.
- Permanently-locked language invariants require both structural tests and invalid-input diagnostic tests; one without the other leaves a silent gap.
- Disambiguation and recovery rules must name the real mechanism; misleading tests or prose around sync anchors create implementation drift.
- Contract alignment across docs, code, diagnostics, and samples is a prerequisite for trustworthy slice work.
- Catalog metadata additions require `None=0` sentinel values on new enums when tests need to assert "non-default" membership — the sentinel distinguishes intentionally-set values from zero-initialized defaults.
- `#pragma warning disable CS8524` is the right suppression for fully-named exhaustive switches (not CS8509): CS8509 fires for non-exhaustive coverage, CS8524 fires for unnamed enum integer values; suppressing CS8524 while letting CS8509 fire is correct when guarding against new named members.
- When adding required positional parameters to shared catalog record types, all call sites (including those using named arguments after the positional block) must be updated. Grep for `new ActionMeta(` and `new ConstructMeta(` before touching signatures.
- `IReadOnlyList<T>` does not expose `.ToFrozenDictionary()` directly — use `IEnumerable<T>` extension via the same reference (it works because `IReadOnlyList<T>` is `IEnumerable<T>`).
- `private enum` is not valid at C# namespace level (only `public`/`internal` are). "Private enum" correctness-gate tests should use a private nested enum inside a class in the target namespace — `ContainingNamespace` skips the containing type, so namespace-scope checks still fire correctly.
- When a spec calls for underlying-type tests (`byte`, `long`), verify that the analyzer's value extraction uses `Convert.ToInt64` (not a direct cast) — widening to `long` is the safe path for all integral underlying types.

## Recent Updates

### 2026-04-29 — PRECEPT0018 correctness gate cleared

Frank's B1 correctness-gate finding is closed. The original implementation commit `a7b0bb7` remains the baseline landing for the analyzer/attribute/exemption/enum work, and the follow-up commit `e7a643d` added the missing 5 regression anchors without changing runtime behavior.

- Final PRECEPT0018 status: implemented, fully spec-covered, and green at 230 analyzer tests + 2044 core tests.
- Important closeout pattern: when a reviewer specifies test IDs, backfill by spec ID rather than by total test count; the first pass matched the quantity but not the required cases.

### 2026-04-28 — PRECEPT0018: spec tests gap resolved (Frank B1 finding)

Frank's correctness gate review issued a BLOCKED verdict (B1) on the PRECEPT0018 implementation: 3 required spec tests were missing plus 2 advisory regression anchors. Added all 5 in `test/Precept.Analyzers.Tests/Precept0018Tests.cs` (commit `e7a643d`):

| New Test | Frank ID | Coverage |
|---|---|---|
| `TP7_ZeroNotFirstMember_Reports` | TP3 | Zero member at non-zero declaration index — full member scan, not just index 0 |
| `TP8_PrivateEnum_Reports` | TP4 | Private nested enum — visibility not filtered |
| `TP9_InternalEnum_Reports` | TP5 | Internal enum — visibility not filtered |
| `EC6_ByteUnderlyingType_Reports` | EC4 | `byte` underlying type — `Convert.ToInt64` path |
| `EC7_LongUnderlyingType_Reports` | EC5 | `long` underlying type — `Convert.ToInt64` widening |

Analyzer tests: 230 (was 225). Core tests: 2044. No other files touched.

### 2026-04-28 — PRECEPT0018: all semantic enums enforced to 1-based via analyzer

Frank designed and George implemented PRECEPT0018. The analyzer enforces that every enum member at integer value 0 in any `Precept.*` namespace must be either: (a) named `None` (structural sentinel), (b) in a `[Flags]` enum, or (c) marked `[AllowZeroDefault]`. All other zero-valued first members are an analyzer error.

**Deliverables (commit `a7b0bb7`):**

| Artifact | Detail |
|----------|--------|
| `AllowZeroDefaultAttribute` | New `src/Precept/AllowZeroDefaultAttribute.cs` — escape hatch for intentional zero-init |
| `PRECEPT0018SemanticEnumZeroSlot` | New analyzer in `src/Precept.Analyzers/` — DiagnosticSeverity.Error, enabled by default |
| 3 `[AllowZeroDefault]` exemptions | `LexerMode.Normal`, `QualifierMatch.Any`, `PeriodDimension.Any` |
| 23 enums made 1-based | ActionKind, AnchorScope, AnchorTarget, Arity, Associativity, ConstructKind, ConstructSlotKind, ConstraintKind, DiagnosticCategory, DiagnosticCode, FaultCode, FaultSeverity, FunctionCategory, FunctionKind, ModifierCategory, ModifierKind, OperationKind, OperatorFamily, OperatorKind, ProofRequirementKind, TokenCategory, TokenKind, TypeCategory |
| 18 new tests | 6 TP + 7 TN + 5 EC — all pass |

**Result:** Build: 0 errors, 0 warnings. All 2269 tests pass (2044 runtime + 225 analyzer).

**Learning:** `TypeCategory` was not in Frank's explicit list of 22 but would have been flagged by the analyzer. Fixed proactively to keep the build clean. When implementing an exhaustive enum-safety analyzer, always verify the full set of flagged enums by doing a dry run before finalizing the exemption list.



Frank's audit found 6 enums where a semantically meaningful named member occupied integer 0, creating silent-default risk. Applied explicit 1-based values to all 6 in a single commit (`d300b26`):

| Enum | File | Risk |
|------|------|------|
| `Severity` | `Language/Diagnostic.cs` | `default(Diagnostic)` silently gives `Severity.Info`; compiler errors masquerade as informational |
| `DiagnosticStage` | `Language/Diagnostic.cs` | Zero-constructed diagnostics silently attributed to `Lex` stage |
| `ConstraintStatus` | `Runtime/Inspection.cs` | Zero-initialized result silently marks a violated constraint as `Satisfied` |
| `Prospect` | `Runtime/Inspection.cs` | Zero-initialized prospect silently presents an impossible transition as `Certain` |
| `FieldAccessMode` | `Runtime/SharedTypes.cs` | Zero-initialized mode silently locks writable fields as `Read` |
| `TypeKind` | `Language/TypeKind.cs` | Zero-initialized kind silently treats unknown types as `String` (26 members, all renumbered) |

No tests used `(EnumName)0` or `default(EnumName)` — no test changes required. Build: 0 errors. All 2044 tests pass.

**Learning**: The 1-based layout is the right default for any enum where ALL members are semantically meaningful. Defer to it at declaration time; retrofitting is cheap but the silent-default risk can survive undetected for years.



### 2026-04-28 — ActionSyntaxShape made 1-based (explicit values)

`ActionSyntaxShape` members were given explicit integer values starting at 1 (`AssignValue=1`, `CollectionValue=2`, `CollectionInto=3`, `FieldOnly=4`). This makes `default(ActionSyntaxShape)` = `(ActionSyntaxShape)0` — an unnamed integer with no named arm in any switch. Any uninitialized `SyntaxShape` now throws `SwitchExpressionException` immediately rather than silently routing through `ParseAssignValueStatement`.

The test `Actions_ActionSyntaxShape_AllMembersHaveValue` was replaced with `Actions_ActionSyntaxShape_AllMembersAreNonZero`, which uses `((int)s).Should().BeGreaterThan(0)` — a structurally stronger guard that directly enforces the 1-based invariant.

Commit: `de2005a`. Build: 0 errors, 0 warnings. All 2044 tests pass.

**Learning**: When a zero-slot sentinel is removed, shift all named enum members to start at 1 rather than relying on the test suite alone to catch future zero-initialization bugs. The 1-based layout gives the compiler's exhaustive switch analysis an unnamed zero slot that throws without any extra code.

### 2026-04-28 — B1–B7 fixes: CS8509 enforcement complete (re-review requested)

Frank's deep re-review found 7 blocking issues after the initial catalog-extensibility implementation. All resolved in one commit (`5e5b2f9`) on `feature/catalog-extensibility`:

- **B1**: Removed `None = 0` from `ActionSyntaxShape` (`Action.cs`). Enum now has exactly 4 real members: `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`.
- **B2**: Removed `ActionSyntaxShape.None => throw` arm from outer `ParseActionStatement` switch. The 4-arm exhaustive switch + `#pragma CS8524` pair is clean.
- **B3–B6**: Replaced all four `_ => throw` wildcards in the inner `ActionKind` switches (`ParseAssignValueStatement`, `ParseCollectionValueStatement`, `ParseCollectionIntoStatement`, `ParseFieldOnlyStatement`) with exhaustive explicit named-arm patterns. Each switch now lists every `ActionKind` not belonging to its shape as a named throw arm, plus `#pragma CS8524` pair. CS8509 fires on each switch when a new `ActionKind` is added.
- **B7**: Removed `_ => throw` from `InvokeSlotParser`. Added `#pragma CS8524` pair. Replaced the misleading comment ("wildcard covers unnamed numeric values") with the accurate statement: "CS8509 enforces named-value coverage here; #pragma CS8524 suppresses unnamed-integer noise."
- **Test fix**: `Actions_ActionSyntaxShape_AllMembersHaveValue` updated from `NotBe((ActionSyntaxShape)0)` to `Enum.IsDefined(meta.SyntaxShape)` — the old guard was only meaningful when `None = 0` existed as a sentinel; with it gone `AssignValue` is 0 so the old assertion was a false positive.

Build: 0 errors, 0 warnings (TreatWarningsAsErrors=true). All 2044 tests pass.

**Learnings**: When removing a `None = 0` sentinel, always audit tests that compare against `(EnumType)0` — they become false positives the moment a real member takes that slot.


- Implemented all 7 slices of the catalog extensibility plan (v3, approved by Frank) on branch `feature/catalog-extensibility`.
- Slice 1: `ExpressionBoundaryTokens` now derives from `Constructs.LeadingTokens` + structural literals — no more hardcoded construct-leading tokens in parser.
- Slice 2: `BuildNode` wildcard removed; CS8509 now fires on missing arms; `#pragma disable CS8524` suppresses the unnamed-integer variant.
- Slice 3: `RoutingFamily` enum added to `ConstructMeta`; all 12 catalog entries populated; `None=0` sentinel enables default-initialization test.
- Slice 4: `ActionSyntaxShape` enum added to `ActionMeta`; `Actions.ByTokenKind` FrozenDictionary enables O(1) token→meta lookup; `None=0` sentinel.
- Slice 3b: Both `DisambiguateAndParse` switches made exhaustive: `null =>` catches ambiguous tokens; explicit wrong-family throw arms catch routing bugs.
- Slice 5: `ParseActionStatement` refactored to two-level CS8509: outer switch on `SyntaxShape`, inner switch on `ActionKind` per shape — 4 helper methods.
- Slice 6: `TokenMeta.IsAccessModeAdjective` flag added; `Tokens.AccessModeKeywords` derived set; `ParseAccessModeKeywordDirect` uses catalog instead of hardcoded `is TokenKind.Readonly or TokenKind.Editable`.
- All 2044 tests pass across 3 commits.

### 2026-04-28 — Access-mode migration and shorthand sync
- Confirmed the B4 migration: `Modify`, `Readonly`, `Editable`, and separate `OmitDeclaration` landed cleanly across catalog, samples, and tests.
- Synced the shorthand/AST direction: shared `FieldTarget` shapes stay available to both `modify` and `omit`, while `omit` remains guardless and structurally separate.

### 2026-04-28 — v8 review cycle closed
- george-4 reviewed `docs/working/catalog-parser-design-v8.md` and blocked on 4 concrete issues: missing omit guard diagnostic coverage, unspecified pre-stashed guard handling when routed to `OmitDeclaration`, unclear sync-anchor mechanism wording, and an underspecified 2.1 slice split.
- frank-5 applied all requested fixes. george-5 re-reviewed the targeted areas, verified each fix, and approved v8.
- Phase 1 is complete; proceed to Phase 2 with the corrected v8 document as the canonical parser-design anchor.

