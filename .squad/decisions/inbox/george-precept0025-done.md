# George — PRECEPT0025 Done

**Date:** 2026-05-09  
**Task:** Implement PRECEPT0025 — CatalogDU Wildcard Prohibition  
**Commits:** `ea91cf3d` (attribute + analyzer + tests), `07ab8782` (Phase 3 enablement)

---

## What PRECEPT0025 Does

PRECEPT0025 catches the class of bug that caused diagnostic code 116 (`UnprovedPresenceRequirement`) to be unreachable: when a new sealed subtype is added to an abstract record hierarchy (a catalog DU), a `_ =>` wildcard arm in a downstream type-pattern switch silently absorbs it instead of forcing an explicit branch.

The analyzer registers on `SwitchExpression` operations. For each switch:

1. It walks the switch value's type hierarchy looking for a type carrying `[CatalogDU]`.
2. If found, it inspects each arm. Any arm with:
   - A discard pattern (`_ =>`)
   - A declaration pattern over the abstract base (`SomeDUBase x =>`)
   - A type pattern over the abstract base (`SomeDUBase =>`)
   …is reported as PRECEPT0025 at Error severity.
3. Suppressed in test files (file path contains `.Tests`) to allow partial scaffolded switches.

The diagnostic message names the `[CatalogDU]` abstract base type and instructs the developer to add explicit arms.

---

## `[CatalogDU]` Attribute

**File:** `src/Precept/Language/CatalogDUAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class CatalogDUAttribute : Attribute { }
```

The attribute lives in `src/Precept/Language/` alongside other catalog attribute definitions (`HandlesCatalogMemberAttribute`, `HandlesCatalogExhaustivelyAttribute`). The analyzer reads it by name (string comparison `attr.AttributeClass?.Name == "CatalogDUAttribute"`) — no direct project reference from the analyzer assembly to the analyzed project.

### `[CatalogDU]` types applied so far

None yet. **See the open item below.**

---

## Open Item: `[CatalogDU]` NOT Applied to `ProofRequirement`

The task called for applying `[CatalogDU]` to the `ProofRequirement` abstract record. I investigated and found that Kramer's fix is **partially complete**:

- ✅ `PresenceProofRequirement presence =>` was added to `CreateDiagnostic` (code 116 now reachable)
- ✅ `PresenceProofRequirement => ...` was added to `CreateFaultSiteLink`
- ❌ The `_ => Diagnostics.Create(...)` fallback arm in `CreateDiagnostic` is **still present** (dead code)
- ❌ The `_ => DiagnosticCode.DivisionByZero` fallback arm in `CreateFaultSiteLink` is **still present** (dead code)

If I applied `[CatalogDU]` to `ProofRequirement` now, PRECEPT0025 would fire on those two dead `_ =>` arms in `ProofEngine.cs`, breaking the `src/Precept/` build. Since the task constraint says "Do not modify ProofEngine.cs — Kramer owns those fixes," and the build must be clean, I deferred the attribute application.

**Action needed from Kramer:** Remove the two dead `_ =>` arms from `CreateDiagnostic` and `CreateFaultSiteLink` in `ProofEngine.cs`. Once removed, apply `[CatalogDU]` to `ProofRequirement` in `src/Precept/Language/ProofRequirement.cs`. The attribute placement is straightforward:

```csharp
[CatalogDU]
public abstract record ProofRequirement(ProofRequirementKind Kind, string Description);
```

After that, PRECEPT0025 will guard all future switches over `ProofRequirement` subtypes.

Other catalog DU bases worth tagging in a follow-on pass: `ProofSubject`, `ProofRequirementMeta`, `ProofSatisfaction`, `SatisfactionProjection`, `NumericBoundSource`, `DimensionSource`, `ConstraintMeta`, `ObligationContext` (if it's a DU).

---

## Phase 3 Enablement

**Enabled:** Both `ConstraintKind` and `ProofRequirementKind` are now in `CatalogEnumNames` in `CatalogAnalysisHelpers.cs`.

**Why it was safe:** Both `Constraints.GetMeta` and `ProofRequirements.GetMeta` already have explicit arms for every member of their respective enums. PRECEPT0007 only reports *missing* members — it does not object to a `_ => throw` fallback arm being present alongside exhaustive explicit arms. No new violations arose: `dotnet build src/Precept/` is clean at 0 warnings, 0 errors.

**Why it was previously deferred:** The TODO was written before Kramer's Phase 2 completion. At the time, some members may have been missing from the GetMeta switches. Now they are all covered.

---

## Test Coverage

9 tests added in `test/Precept.Analyzers.Tests/Precept0025Tests.cs`:

| Test | What it covers |
|------|----------------|
| TP1: `DiscardArm_OverCatalogDUType_Reports` | Pure `_ =>` arm fires |
| TP2: `DeclarationPattern_OverAbstractBase_Reports` | `Shape x =>` fires |
| TP3: `MultipleWildcardArms_ReportsEach` | Each offending arm reported independently |
| TP4: `SwitchOverDerivedType_WalksHierarchyAndReports` | Walks base hierarchy to find `[CatalogDU]` |
| TN1: `ExhaustiveSwitch_NoDiagnostic` | No `_` arm = no diagnostic |
| TN2: `DiscardArm_OverNonCatalogDUType_NoDiagnostic` | Non-`[CatalogDU]` type is ignored |
| TN3: `GuardedAndConcretePatterns_NoDiagnostic` | Specific subtype patterns don't fire |
| TN4: `DiscardArm_OnEnum_NoDiagnostic` | Enum switches are not affected |
| TN5: `DiscardArm_InTestFile_Suppressed` | File path `.Tests` suppression works |

Full suite: 272/272 analyzer tests pass. Main Precept tests: 3629/3631 (2 pre-existing `TokensTests` failures, unrelated to this work).

---

## Design Note

The analyzer uses type hierarchy walking (`FindCatalogDUBase`) rather than checking only the exact switch expression type. This means a switch over a concrete subtype (`Circle c => ...`) is also governed if `Circle`'s base `Shape` has `[CatalogDU]`. This is intentional — it prevents the pattern `new List<Circle> { ... }.Select(...) switch { Circle => ..., _ => ... }` from slipping through.

The catch-all declaration pattern check (`IDeclarationPatternOperation where MatchedType == catalogDUBase`) ensures that `ProofRequirement r =>` — a named binding over the abstract base — is treated the same as `_`. Both are structurally equivalent catch-alls.
