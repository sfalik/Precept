using System.Collections.Generic;

namespace Precept.Analyzers;

/// <summary>
/// Allow-lists for diagnostic coverage gates. Each entry carries a root-cause cluster comment.
/// Ownership: Runtime/diagnostics maintainers who modify DiagnosticCode.cs or emission wiring
/// must update this file in the same PR.
/// </summary>
internal static class DiagnosticCoverageAllowLists
{
    /// <summary>
    /// Gate 1 allow-list: DiagnosticCode members with no emission site.
    /// Each entry must have a root-cause comment. Entries are removed as gap-closure slices ship.
    /// Cross-plan dependency: PRE0078 is removed when interval proof engine Slice 2 ships.
    /// </summary>
    internal static readonly HashSet<string> Gate1AllowList = new()
    {
        // ── Root Cause B1 — Temporal Constant Precision ──────────────────────────
        // PRE0055–0058 removed by Slice 9B (catalog-mediated emission via TypedConstantFamilyMeta)
        "InvalidTimezoneId",                  // B1: catch-all fires instead of specific code
        "UnqualifiedPeriodArithmetic",        // B1: temporal arithmetic rules not wired
        "MissingTemporalUnit",               // B1: temporal arithmetic rules not wired
        "FractionalUnitValue",               // B1: temporal arithmetic rules not wired


        // ── Root Cause B3 — Choice Value Validation ──────────────────────────────
        // PRE0086, PRE0087, PRE0089 removed by Slice 2 (choice literal/arg validation wired)

        // ── Root Cause B4 — Collection Safety Extensions ─────────────────────────
        "KeyPresenceSafety",                  // B4: obligation generator not yet on lookup accessor
        "KeyUniquenessGuard",                 // B4: obligation generator not yet on lookup put action

        // ── Root Cause C — Structural Single-Check Gaps ──────────────────────────
        "EventHandlerInStatefulPrecept",      // C: trivial structural check not wired

        // ── Root Cause D1 — Parser Expression Precision ──────────────────────────
        "NonAssociativeComparison",           // D1: parser emits generic ExpectedToken instead
        "UnexpectedKeyword",                  // D1: parser emits generic ExpectedToken instead
        "InvalidCallTarget",                  // D1: parser emits generic ExpectedToken instead

        // ── Root Cause D2 — Scattered TypeChecker Gaps ───────────────────────────
        "NullInNonNullableContext",           // D2: retired, subsumed by PRE0116 (pending removal)
        "FunctionArgConstraintViolation",     // D2: TypeMismatch fires instead (precision upgrade)
        "DuplicateArgName",                   // D2: emission site not wired
        "InvalidModifierValue",              // D2: emission site not wired
        "ComputedFieldWithDefault",           // D2: emission site not wired
        "ConflictingAccessModes",             // D2: emission site not wired
        "RedundantAccessMode",                // D2: emission site not wired
        "ListLiteralOutsideDefault",          // D2: emission site not wired
        "ScalarOperationOnCollection",        // D2: TypeMismatch fires instead (precision upgrade)
        "EventArgOutOfScope",                 // D2: emission site not wired
        "InvalidInterpolationCoercion",       // D2: TypeMismatch fires instead (precision upgrade)
        "MaxPlacesExceeded",                  // D2: emission site not wired
        "NonChoiceAssignedToChoice",          // D2: emission site not wired
        "CollectionInnerTypeError",           // D2: emission site not wired

        // ── Root Cause D3 — ProofEngine Gap (Interval Engine Dependency) ─────────
        // Cross-plan: removed when interval proof engine Slice 2 ships
        "NumericOverflow",                    // D3: owned by Strategy 7 (IntervalContainment)

        // ── Deferred — OutOfRange ────────────────────────────────────────────────
        "OutOfRange",                         // Deferred: constant-literal bounds check not wired
    };

    /// <summary>
    /// Gate 2 allow-list: emitted codes with no test reference.
    /// Starts empty — all currently emitted codes are test-referenced.
    /// Entries require explicit justification.
    /// </summary>
    internal static readonly HashSet<string> Gate2AllowList = new()
    {
        // ── Slice 6 — PRE0100 and PRE0104 have tests in TypeCheckerCollectionSafetyTests

        // ── Slice 1 (B2) — Tests in TypeCheckerCurrencyUnitTests.cs ─────────────
        // Cross-project analyzer cannot detect test references in Precept.Tests.
        "CrossCurrencyArithmetic",
        "CrossDimensionArithmetic",
        "DenominatorUnitMismatch",
        "DurationDenominatorMismatch",
        "CompoundPeriodDenominator",

        // ── Slice 2 (B3) — Tests in TypeCheckerStructuralTests.cs ───────────────
        // Cross-project analyzer cannot detect test references in Precept.Tests.
        "ChoiceLiteralNotInSet",
        "ChoiceArgOutsideFieldSet",
        "ChoiceRankConflict",

        // ── Slice 5A — Tests in TypeCheckerTypedConstantTests.cs ─────────────────
        // Cross-project analyzer cannot detect test references in Precept.Tests.
        "AmbiguousTypedConstant",
    };
}
