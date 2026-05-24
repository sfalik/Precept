# Appendix — `catalog-system.md` Audit (Sub-Agent Report)

**Date**: 2026-05-24
**Scope**: `docs/language/catalog-system.md` (2,481 lines) vs `src/Precept/Language/` catalog implementation
**Author**: Sub-agent of the 2026-05-24 compiler-readiness review
**Findings**: 26 total — 2 P0, 16 P1, 8 P2
**Parent doc**: [../compiler-readiness-review-2026-05-24.md](../compiler-readiness-review-2026-05-24.md)

## § A. Methodology

Audited `/home/sfalik/source/repos/Precept/docs/language/catalog-system.md` against the catalog implementation in `/home/sfalik/source/repos/Precept/src/Precept/Language/`. For every member count, metadata-field claim, DU subtype enumeration, cross-catalog wiring claim, attribute claim, and "✅ Resolved" status declaration, the doc and corresponding C# source were compared.

Sources audited:
- All 14 catalog `*Kind.cs` and `*s.cs` files in `src/Precept/Language/`
- DU/meta records: `Token.cs`, `Type.cs`, `Operator.cs`, `Function.cs`, `Action.cs`, `Modifier.cs`, `Operation.cs`, `Construct.cs`, `ExpressionForms.cs`, `Outcomes.cs`, `Constraints.cs`, `ProofRequirement.cs`, `Diagnostic.cs`, `Faults.cs`
- Attribute infrastructure: `CatalogDUAttribute.cs`, `HandlesCatalogMemberAttribute.cs`, `HandlesCatalogExhaustivelyAttribute.cs`
- Analyzer files in `src/Precept.Analyzers/` (rule IDs PRECEPT0001–PRECEPT0028)
- MCP cross-check via `mcp__precept__precept_syntax`, `precept_types`, `precept_proofs`

## § B. Coverage Matrix (per catalog)

### B.1 Tokens
- **`TokenKind`** count: doc says 138 / 90+; actual **139 members**. ⚠️ off-by-one in topology; "90+" claim badly stale.
- **`TokenMeta`** fields: doc matches actual `Token.cs:35-69`. ✅
- **`TokenMeta.HoverDescription` (CC#19 "✅ Resolved 2026-05-06")**: field does not exist in code. 🔴 DRIFT — F-LANG-CAT-01.
- `Tokens.Keywords`, `Tokens.KeywordsValidAsMemberName`, `Tokens.AccessModeKeywords`: all exist; some undocumented but consistent.
- `TokenCategory`: 17 values, matches.

### B.2 Types
- **`TypeKind`** count: doc says 32 / 26; actual **32**. ⚠️ inventory line 933 stale.
- **Collection categorization**: doc says "Collection (3): Set/Queue/Stack"; actual Collection = **9** (adds Log, LogBy, Bag, List, QueueBy, Lookup). 🔴 DRIFT.
- **`TypeMeta` shape**: actual adds undocumented `ImpliedQualifiers` and `RequiredBoundQualifierAxes`. 🔴 DRIFT — F-LANG-CAT-21.
- **`TypeMeta.IsUserFacing` (CC#16 "✅ Resolved")**: field does not exist. 🔴 DRIFT — F-LANG-CAT-20.
- **`TypeTrait`**: doc shows `Orderable, EqualityComparable`; actual adds `ChoiceElement`. ⚠️ partial.
- **`QualifierAxis`**: doc says 9; actual **10** (adds `PriceIn`). 🔴 DRIFT.
- **`QualifierShape`**: doc shows `(Slots, InOfExclusive)`; actual adds `OfRequiresCurrencyIn`. 🔴 DRIFT.
- `TypeAccessor` DU: matches.
- `ContentValidation` DU: 8 subtypes (RegexValidation, NodaTimeValidation, UcumValidation, MoneyValidation, QuantityValidation, PriceValidation, ExchangeRateValidation, ClosedSetValidation); never enumerated in doc.

### B.3 Functions
- `FunctionKind`: 23 members, matches count but doc contradicts itself ("21" in body).
- Category breakdown: matches.
- **`FunctionMeta`**: adds undocumented `CIDiagnosticCode: DiagnosticCode?`. 🔴 minor.
- **`FunctionOverload`**: adds undocumented `IntervalTransfer` init-only delegate. 🔴 minor.

### B.4 Operators
- `OperatorKind`: 21 members, matches.
- All shape/enum claims match. ✅ Cleanest catalog in the doc.

### B.5 Operations
- **`OperationKind`** count: doc says "198"; actual **203 members** / 203 switch arms. 🔴 DRIFT — F-LANG-CAT-16.
- Unary: 9, matches.
- Binary: 194 (203 − 9), not 189.
- `BidirectionalLookup`: matches.
- **`BinaryOperationMeta`**: adds undocumented `HasCIVariant`, `CIDiagnosticCode`, `ResultQualifierPolicy`. 🔴 DRIFT — F-LANG-CAT-17.
- **`ResultQualifierPolicy` enum**: 5 values entirely undocumented. 🔴.
- **`Operations.Resolve` method**: doc shows full code sample; method does not exist. Only `FindUnary`, `FindCandidates` ship. 🔴 — F-LANG-CAT-15.

### B.6 Modifiers
- `ModifierKind`: 29 members, matches.
- Subtype distribution: matches.
- **`ModifierMeta` base**: adds undocumented `DesugarsToRule` field. 🔴 DRIFT — F-LANG-CAT-18.
- `ValueModifierMeta`: matches.
- Other meta subtypes: each adds `DesugarsToRule` undocumented.
- **Event modifier table (line 1523)**: lists 9 modifiers (`entry, advancing, settling, completing, absorbing, guarded, isolated, universal`); only `InitialEvent` exists in code. 🔴 DRIFT — F-LANG-CAT-19.

### B.7 Actions
- `ActionKind`: 15 members, matches.
- **`ActionMeta`**: adds undocumented `DynamicObligationGenerator: Func<TypedAction, SemanticIndex, ImmutableArray<ProofObligation>>?`. 🔴 DRIFT — F-LANG-CAT-24.

### B.8 Constructs
- **`ConstructKind`** count: doc says "12"; actual **15 members** (adds `EventRow=12`, `ConstructionRow=19`, `ConstructionRowReject=20`, `TransitionRowReject=21`). 🔴 DRIFT — F-LANG-CAT-03.
- Doc lists "EventHandler" as construct kind; actual is `EventRow` (no `EventHandler`). 🔴.
- `ConstructMeta`: matches.
- `RoutingFamily`: adds undocumented `None = 0` sentinel.
- `DisambiguationEntry`: matches.
- **`ConstructSlot`**: adds undocumented `IsList`, `IsChainable`, `ItemIntroducerToken`, `Vocabulary: SlotVocabulary` (4 fields). 🔴 DRIFT — F-LANG-CAT-05.
- **`ConstructSlotKind`** count: doc says "17"; actual **20 members** (adds `RejectClause=18`, `SuccessOutcome=19`, `EventEntryList=20`). 🔴 DRIFT — F-LANG-CAT-04.
- **`SlotVocabulary` enum**: 13 members entirely undocumented. 🔴.

### B.9 ExpressionForms
- **`ExpressionFormKind`** count: doc says "14" (and "11" in one place); actual **15** (adds `InterpolatedTypedConstant`). 🔴 DRIFT — F-LANG-CAT-07.
- `ExpressionFormMeta` shape: matches.
- `ExpressionCategory`: 4 values, matches.

### B.10 Constraints
- `ConstraintKind`: 5 members, matches. ✅

### B.11 ProofRequirements
- **`ProofRequirementKind`** count: doc says "5"; actual **10 members** (adds `QualifierChain`, `IntervalContainment`, `LengthContainment`, `CountContainment`, `KeyPresence`). 🔴 DRIFT — **F-LANG-CAT-08** (P0).
- `ProofRequirementMeta` DU: 5 in doc, 10 in code.
- **`ProofRequirementMeta.DiagnosticCode` field**: entirely undocumented. 🔴.
- `ProofRequirement` instance DU: 5 in doc, 10 in code.
- `ProofSubject` DU: matches.
- **`ProofSatisfaction` DU**: 5 in doc, 9 in code; payloads on existing subtypes (`Dimension(DimensionSource)`, `Modifier(ModifierKind)`, `QualifierCompatibility(QualifierAxis)`) all undocumented. 🔴 — F-LANG-CAT-10.
- Supporting DUs `DimensionSource`, `SatisfactionProjection`, `NumericBoundSource`: 3 entirely undocumented.

### B.12 Outcomes
- All counts/subtypes/indexes match. ✅

### B.13 Diagnostics
- **`DiagnosticCode`** count: doc says "78" / "106"; actual **148**. 🔴 DRIFT — F-LANG-CAT-11.
- Stage breakdown grossly understated.
- **`DiagnosticMeta`**: adds 5 undocumented fields (`SuggestionSources`, `TriggerCondition`, `RecoverySteps`, `ExampleBefore`, `ExampleAfter`). 🔴 — F-LANG-CAT-12.
- **`SuggestionSource` enum**: 4 members entirely undocumented.

### B.14 Faults
- **`FaultCode`** count: doc says "13"; actual **15 members** (adds `LengthBoundViolation`, `CountBoundViolation`). 🔴 — F-LANG-CAT-13.
- **`FaultCode.AmbiguousDispatch` (CC#13 "✅ Resolved 2026-05-06")**: neither this nor `DiagnosticCode.AmbiguousDispatch` exists in code. 🔴 **P0** — F-LANG-CAT-02.
- `FaultMeta`: adds undocumented `Severity`, `RecoveryHint`. 🔴 — F-LANG-CAT-14.

### B.15-B.18 (Misc)
- Construct Slot Model section: stale (describes non-existent `SlotKind` enum). 🔴 — F-LANG-CAT-06.
- `DisambiguationEntry`: matches.
- **Roslyn rules**: doc covers PRECEPT0001-0006, 0019 (7 total); actual fleet is **22+** rules (PRECEPT0001..PRECEPT0028). 🔴 DRIFT — F-LANG-CAT-23.

## § C. Drift Findings (Full List)

Findings F-LANG-CAT-01 through F-LANG-CAT-26 are listed in the main remediation doc (`../compiler-readiness-review-2026-05-24.md`). Highlights:

- **P0**: F-LANG-CAT-02 (AmbiguousDispatch claimed-resolved but missing), F-LANG-CAT-08 (ProofRequirementKind 5→10)
- **P1** (16): catalog count drifts, undocumented meta fields, stale "Construct Slot Model" section, missing `Operations.Resolve`, `TokenMeta.HoverDescription` / `TypeMeta.IsUserFacing` claimed-resolved but missing, aspirational event modifiers
- **P2** (8): undocumented analyzer fleet, minor field drift, catalog count narrative inconsistency (13 vs 14)

## § D. Implementation-only features

These are catalog surfaces with no doc presence:

1. `SemanticTokenTypes` catalog (15th catalog candidate)
2. `SlotVocabulary` enum (13 members)
3. `ConstructSlot` extra fields (`IsList`, `IsChainable`, `ItemIntroducerToken`)
4. `ResultQualifierPolicy` enum (5 members)
5. `ProofSatisfaction` family with payloads + supporting DUs
6. Proof requirement instance hierarchy beyond the 5 documented
7. `Constraints.ByToken` index
8. `Modifiers.ByStateToken` / `ByEventToken` / `ByAccessToken` / `ByAnchorToken`
9. `Actions.ByTokenKind`
10. Interval transfer functions on `UnaryOperationMeta`/`BinaryOperationMeta`/`FunctionOverload`
11. `SuggestionSource` enum
12. `DiagnosticMeta` triage fields (`TriggerCondition`, `RecoverySteps`, `ExampleBefore`, `ExampleAfter`)
13. Parser `[HandlesCatalogExhaustively]` triple-stack (only one documented)
14. 22+ analyzer rules vs the 7 documented
15. `AllowZeroDefault` attribute
16. `CatalogDUAttribute`

## § E. Confidence statement per catalog area

| Catalog area | Doc-to-code fidelity |
|---|---|
| Tokens | Medium |
| Types | Low-Medium |
| Functions | High |
| Operators | High (cleanest) |
| Operations | Low-Medium |
| Modifiers | Medium |
| Actions | High |
| Constructs | Low |
| ExpressionForms | Medium |
| Constraints | High |
| ProofRequirements | Very Low (biggest single-catalog drift) |
| Outcomes | High |
| Diagnostics | Low |
| Faults | Low |
| Roslyn rules | Very Low |

## Summary

**Headline counts wrong**: 14 of 14 catalogs have at least one count discrepancy. Most consequential: `ProofRequirementKind` 5→10 (+100%), `DiagnosticCode` 78/106→148 (+90%), `ConstructSlotKind` 17→20, `ConstructKind` 12→15, `FaultCode` 13→15, `ExpressionFormKind` 14→15.

**Two "✅ Resolved" CC claims are false**: F-LANG-CAT-01 (`TokenMeta.HoverDescription`) and F-LANG-CAT-02 (`FaultCode.AmbiguousDispatch`). Strong signal that CC resolution process isn't gating doc updates against actual implementation.

**One major API surface invented in doc but missing**: `Operations.Resolve` described with full code sample; doesn't exist.

**Entire architectural section stale**: "Construct Slot Model" describes a `SlotKind` enum and `ConstructSlot` record shape that bear no resemblance to actual code.

**Analyzer fleet under-reported**: 22+ Roslyn rules implemented; 7 documented.
