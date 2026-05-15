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
        // EventHandlerInStatefulPrecept — wired (Slice 8)

        // ── Root Cause D1 — Parser Expression Precision ──────────────────────────
        "NonAssociativeComparison",           // D1: parser emits generic ExpectedToken instead
        "UnexpectedKeyword",                  // D1: parser emits generic ExpectedToken instead
        "InvalidCallTarget",                  // D1: parser emits generic ExpectedToken instead

        // ── Root Cause D2 — Scattered TypeChecker Gaps ───────────────────────────
        "NullInNonNullableContext",           // D2: retired, subsumed by PRE0116 (pending removal)
        "FunctionArgConstraintViolation",     // D2: TypeMismatch fires instead (precision upgrade)
        // DuplicateArgName — wired (Slice 8)
        // InvalidModifierValue — wired (Slice 8)
        // ComputedFieldWithDefault — wired (Slice 8)
        // ConflictingAccessModes — wired (Slice 8)
        // RedundantAccessMode — wired (Slice 8)
        // ListLiteralOutsideDefault — wired (Slice 8)
        "ScalarOperationOnCollection",        // D2: TypeMismatch fires instead (precision upgrade)
        // EventArgOutOfScope — wired (Slice 8)
        "InvalidInterpolationCoercion",       // D2: TypeMismatch fires instead (precision upgrade)
        // MaxPlacesExceeded — wired (Slice 8)
        // NonChoiceAssignedToChoice — wired (Slice 8)
        // CollectionInnerTypeError — wired (Slice 8)

        // ── Root Cause D3 — ProofEngine Gap (Interval Engine Dependency) ─────────
        // NumericOverflow — already has emission site in ProofEngine Strategy 7

        // ── Deferred — OutOfRange ────────────────────────────────────────────────
        "OutOfRange",                         // Deferred: constant-literal bounds check not wired

        // ── Pre-existing gaps (not in Slice 8 scope) ─────────────────────────────
        "CollectionOperationOnScalar",        // no emission site wired
        "InvalidTypedConstantContent",        // no emission site wired
        "InvalidDateValue",                   // no emission site wired
        "InvalidDateFormat",                  // no emission site wired
        "InvalidTimeValue",                   // no emission site wired
        "InvalidInstantFormat",               // no emission site wired
        "NonOrderableCollectionExtreme",      // no emission site wired
        "UnsatisfiableGuard",                 // no emission site wired
        "DivisionByZero",                     // no emission site wired
        "SqrtOfNegative",                     // no emission site wired
        "ChoiceElementTypeMismatch",          // no emission site wired
        "ChoiceMissingElementType",           // no emission site wired
    };

    /// <summary>
    /// Gate 2 allow-list: emitted codes with no test reference.
    /// Starts empty — all currently emitted codes are test-referenced.
    /// Entries require explicit justification.
    /// </summary>
    internal static readonly HashSet<string> Gate2AllowList = new()
    {
        // ── Cross-project test detection gap ─────────────────────────────────────
        // The cross-project analyzer cannot detect test references in Precept.Tests.
        // All codes below have tests in test/Precept.Tests/ TypeChecker/ProofEngine/Parser test files.
        "AlwaysRejecting",
        "AmbiguousTypedConstant",
        "AssignmentInExpressionContext",
        "BindingShadowsField",
        "BoundsQualifierMismatch",
        "BoundsRequireQualifier",
        "CaseInsensitiveFieldRequiresTildeEndsWith",
        "CaseInsensitiveFieldRequiresTildeEquals",
        "CaseInsensitiveFieldRequiresTildeNotEquals",
        "CaseInsensitiveFieldRequiresTildeStartsWith",
        "CaseInsensitiveValueInCaseSensitiveContains",
        "ChoiceArgOutsideFieldSet",
        "ChoiceLiteralNotInSet",
        "ChoiceRankConflict",
        "CircularComputedField",
        "CollectionInnerTypeError",
        "CompoundPeriodDenominator",
        "ComputedFieldNotWritable",
        "ComputedFieldWithDefault",
        "ConflictingAccessModes",
        "ConflictingModifiers",
        "CountDimensionBoundsAmbiguous",
        "CountBoundViolation",
        "CrossCountingUnitOperation",
        "CrossCurrencyArithmetic",
        "CrossDimensionArithmetic",
        "DeadEndState",
        "DefaultForwardReference",
        "DenominatorUnitMismatch",
        "DimensionCategoryMismatch",
        "DimensionMismatchInUnitSlot",
        "DuplicateArgName",
        "DuplicateChoiceValue",
        "DuplicateEventName",
        "DuplicateFieldName",
        "DuplicateModifier",
        "DuplicateStateInList",
        "DuplicateStateName",
        "DurationDenominatorMismatch",
        "EmptyChoice",
        "EventArgOutOfScope",
        "EventHandlerDoesNotSupportGuard",
        "EventHandlerInStatefulPrecept",
        "ExpectedOutcome",
        "ExpectedToken",
        "FunctionArityMismatch",
        "IndexBoundsGuard",
        "InitialEventMissingAssignments",
        "InputTooLarge",
        "InterpolatedTypedConstantHoleTypeMismatch",
        "InterpolationNotSupportedForType",
        "InvalidCharacter",
        "InvalidCurrencyCode",
        "InvalidDimensionString",
        "InvalidInterpolatedTypedConstantForm",
        "InvalidMemberAccess",
        "InvalidModifierBounds",
        "InvalidModifierForType",
        "InvalidModifierValue",
        "InvalidQuantifierTarget",
        "InvalidTemporalDimensionString",
        "InvalidTemporalUnitString",
        "InvalidUnitString",
        "IrreversibleStateHasBackEdge",
        "IsSetOnNonOptional",
        "LengthBoundViolation",
        "ListLiteralOutsideDefault",
        "MaxPlacesExceeded",
        "MissingOrderingKey",
        "MultipleInitialStates",
        "MutuallyExclusiveQualifiers",
        "NoInitialState",
        "NonChoiceAssignedToChoice",
        "NumericOverflow",
        "OmitDoesNotSupportGuard",
        "OmittedFieldReadInState",
        "OmittedFieldSetInTargetState",
        "PreEventGuardNotAllowed",
        "QualifierMismatch",
        "QuantifierPredicateNotBoolean",
        "RedundantAccessMode",
        "RedundantModifier",
        "RequiredFieldsNeedInitialEvent",
        "RequiredFieldUnassignedOnEntry",
        "RequiredStateDoesNotDominateTerminal",
        "StateAlwaysRejects",
        "StateListContainsWildcard",
        "StructuralSinkState",
        "TerminalStateHasOutgoingEdges",
        "TypeMismatch",
        "UndeclaredArg",
        "UndeclaredEvent",
        "UndeclaredField",
        "UndeclaredFunction",
        "UndeclaredState",
        "UnescapedBraceInLiteral",
        "UnguardedCollectionAccess",
        "UnguardedCollectionMutation",
        "UnhandledEvent",
        "UnprovedDimensionRequirement",
        "UnprovedModifierRequirement",
        "UnprovedPresenceRequirement",
        "UnprovedQualifierCompatibility",
        "UnreachableState",
        "UnrecognizedStringEscape",
        "UnrecognizedTypedConstantEscape",
        "UnresolvedTypedConstant",
        "UnsatisfiableInitialState",
        "UnterminatedInterpolation",
        "UnterminatedStringLiteral",
        "UnterminatedTypedConstant",
        "WritableOnEventArg",
    };
}
