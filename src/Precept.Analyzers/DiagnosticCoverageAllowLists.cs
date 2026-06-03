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
    /// Each entry carries a root-cause comment; entries are removed when the corresponding
    /// emission wires up.
    /// </summary>
    internal static readonly HashSet<string> Gate1AllowList = new()
    {
        // ── Temporal Constant Precision ─────────────────────────────────────────
        "InvalidTimezoneId",                  // catch-all fires instead of specific code
        "UnqualifiedPeriodArithmetic",        // temporal arithmetic rules not wired
        "MissingTemporalUnit",                // temporal arithmetic rules not wired
        "FractionalUnitValue",                // temporal arithmetic rules not wired

        // ── Collection Safety Extensions ────────────────────────────────────────
        "KeyPresenceSafety",                  // obligation generator not yet on lookup accessor
        "KeyUniquenessGuard",                 // obligation generator not yet on lookup put action

        // ── Retired diagnostics (pending removal) ───────────────────────────────
        "EventHandlerDoesNotSupportGuard",    // retired — guards are now valid on all on-rows

        // ── Parser Expression Precision ─────────────────────────────────────────
        "UnexpectedKeyword",                  // parser emits generic ExpectedToken instead
        "InvalidCallTarget",                  // parser emits generic ExpectedToken instead

        // ── Scattered TypeChecker Gaps ──────────────────────────────────────────
        "NullInNonNullableContext",           // retired, subsumed by PRE0116 (pending removal)
        "FunctionArgConstraintViolation",     // TypeMismatch fires instead (precision upgrade)
        "InvalidInterpolationCoercion",       // TypeMismatch fires instead (precision upgrade)

        // ── Catalog-mediated fallthrough emission ───────────────────────────────
        // InvalidTypedConstantContent is emitted from the SelectDiagnosticCode
        // coalesce fallback in TypeChecker.Expressions.cs when a typed-constant
        // family declares neither FormatErrorCode nor SemanticErrorCode. The
        // emission flows through a DiagnosticCode-typed local variable into
        // Diagnostics.Create, so the syntactic scanner can't trace it. Tests
        // exercise the path via NodaTime-validated families.
        "InvalidTypedConstantContent",

        // ── Pre-existing gaps ───────────────────────────────────────────────────
        "MissingOrderingKey",                 // reserved for missing-`by` clause emission; not yet specialized (currently ScalarOperationOnCollection catches it as a generic applicability mismatch)
        "NonOrderableCollectionExtreme",      // no emission site wired
        "DivisionByZero",                     // no emission site wired
        "SqrtOfNegative",                     // no emission site wired
        "ChoiceElementTypeMismatch",          // no emission site wired
        "ChoiceMissingElementType",           // no emission site wired

        // ── MCP tooling backstop ─────────────────────────────────────────────────
        // McpToolInternalError fires only from the McpToolSafeInvoke wrapper in
        // tools/Precept.Mcp — it never reaches the normal compile pipeline that
        // the Precept0027 analyzer scans, so it must be allow-listed here.
        "McpToolInternalError",
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
        "CollectionOperationOnScalar",
        "ScalarOperationOnCollection",
        "CompoundPeriodDenominator",
        "ComputedFieldNotWritable",
        "ComputedFieldWithDefault",
        "ConflictingAccessModes",
        "ConflictingModifiers",
        "CountDimensionBoundsAmbiguous",
        "CountBoundViolation",
        "CrossCountingUnitOperation",
        "CrossCurrencyArithmetic",
        "CurrencyMismatchInCurrencySlot",
        "CrossDimensionArithmetic",
        "DeadEndState",
        "DefaultForwardReference",
        "DenominatorUnitMismatch",
        "DimensionCategoryMismatch",
        "DimensionMismatchInUnitSlot",
        "UnprovedAssignmentQualifierCompatibility",
        "UnqualifiedEventArgReference",
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
        "InitialEventInTransitionRow",
        "InitialEventMissingAssignments",
        "ConstructionGuardReadsUninitializedField",
        "InputTooLarge",
        "InterpolatedTypedConstantHoleTypeMismatch",
        "InterpolationNotSupportedForType",
        "InvalidCharacter",
        "InvalidCurrencyCode",
        "InvalidDimensionString",
        "InvalidInterpolatedTypedConstantForm",
        "InvalidPriceQualifier",
        "InvalidQualifierCoexistence",
        "InvalidMemberAccess",
        "InvalidModifierBounds",
        "InvalidModifierForType",
        "InvalidModifierValue",
        "InvalidQuantifierTarget",
        "InvalidTemporalDimensionString",
        "InvalidTemporalUnitString",
        "InvalidDateFormat",
        "InvalidDateValue",
        "InvalidTimeValue",
        "InvalidInstantFormat",
        "InvalidUnitString",
        "IrreversibleStateHasBackEdge",
        "IsSetOnNonOptional",
        "LengthBoundViolation",
        "ListLiteralOutsideDefault",
        "MaxPlacesExceeded",
        "MaxplacesCurrencyQualifierNotStatic",
        "DegeneratePeriodComparison",
        "RequiredTraitViolation",
        "MultipleInitialEvents",
        "MultipleInitialStates",
        "MutuallyExclusiveQualifiers",
        "NoInitialState",
        "NonAssociativeComparison",
        "NonChoiceAssignedToChoice",
        "NumericOverflow",
        "OmitDoesNotSupportGuard",
        "OmittedFieldReadInState",
        "OmittedFieldSetInTargetState",
        "OutOfRange",
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
        "UnsatisfiableRule",
        "UnterminatedInterpolation",
        "UnterminatedStringLiteral",
        "UnterminatedTypedConstant",
        "UninitializedCrossFieldReadInInitialAssignment",
        "UninitializedFieldReadInInitialAssignment",
        "MaterializedFieldSelfReference",
        "EditableOnEventArg",
        "FieldNeverSet",
        "IncompatibleDimensionalProduct",
        "ContradictoryRule",
        "TautologicalGuard",
        "UnsatisfiableGuard",
        "VacuousRule",
        "ZeroConstructionRows",
        "DuplicateCompositeBasisComponent",
        "UnknownCompositeBasisComponent",
        "EmptyCompositeBasisComponent",

        // ── MCP tooling backstop ─────────────────────────────────────────────────
        // McpToolInternalError is exercised by tests in test/Precept.Mcp.Tests/ that
        // call the MCP tool wrappers directly. Those tests are not visible to the
        // Precept.Tests cross-project test scanner.
        "McpToolInternalError",
    };
}
