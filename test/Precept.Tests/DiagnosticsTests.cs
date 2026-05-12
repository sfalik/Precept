using System;
using System.Collections.Immutable;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

// The four exhaustiveness tests (GetMeta_ReturnsWithoutThrowing, All count,
// non-empty Code/MessageTemplate, CodeName == enum member name) are intentionally
// left in TypeCheckerTests.cs where they were originally written. This file adds
// coverage that is not present there: the Create factory, stage group invariants,
// and severity spot-checks.

public class DiagnosticsTests
{
    // ── Create factory ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllDiagnosticCodes))]
    public void Create_ProducesCorrectCodeString_ForEveryDiagnosticCode(DiagnosticCode code)
    {
        // Supply 4 placeholder args so string.Format never throws for any template.
        var diagnostic = Diagnostics.Create(code, SourceSpan.Missing, "x", "x", "x", "x");
        diagnostic.Code.Should().Be(Diagnostics.GetMeta(code).Code);
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticCodes))]
    public void Create_ProducesCorrectSeverity_ForEveryDiagnosticCode(DiagnosticCode code)
    {
        var diagnostic = Diagnostics.Create(code, SourceSpan.Missing, "x", "x", "x", "x");
        diagnostic.Severity.Should().Be(Diagnostics.GetMeta(code).Severity);
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticCodes))]
    public void Create_ProducesCorrectStage_ForEveryDiagnosticCode(DiagnosticCode code)
    {
        var diagnostic = Diagnostics.Create(code, SourceSpan.Missing, "x", "x", "x", "x");
        diagnostic.Stage.Should().Be(Diagnostics.GetMeta(code).Stage);
    }

    // ── Source span propagation ─────────────────────────────────────────────────

    [Fact]
    public void Create_PreservesSpan_OnProducedDiagnostic()
    {
        var span = new SourceSpan(10, 5, 3, 7, 3, 12);
        var diagnostic = Diagnostics.Create(DiagnosticCode.InputTooLarge, span);
        diagnostic.Span.Should().Be(span);
    }

    [Fact]
    public void Create_InitializesRelatedSpans_AsEmpty()
    {
        var diagnostic = Diagnostics.Create(DiagnosticCode.InputTooLarge, SourceSpan.Missing);
        diagnostic.RelatedSpans.Should().BeEmpty();
    }

    [Fact]
    public void Diagnostic_WithRelatedSpans_PreservesEntries()
    {
        var primary = Diagnostics.Create(DiagnosticCode.DuplicateFieldName, SourceSpan.Missing, "amount");
        var related = new RelatedSpan(new SourceSpan(25, 3, 8, 9, 8, 14), "Original declaration is here");

        var diagnostic = primary with
        {
            RelatedSpans = ImmutableArray.Create(related),
        };

        diagnostic.RelatedSpans.Should().ContainSingle().Which.Should().Be(related);
    }

    // ── Stage group invariants ──────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(LexCodes))]
    public void LexStageCodes_AllHaveLexStage(DiagnosticCode code)
    {
        Diagnostics.GetMeta(code).Stage.Should().Be(DiagnosticStage.Lex);
    }

    [Theory]
    [MemberData(nameof(ParseCodes))]
    public void ParseStageCodes_AllHaveParseStage(DiagnosticCode code)
    {
        Diagnostics.GetMeta(code).Stage.Should().Be(DiagnosticStage.Parse);
    }

    [Theory]
    [MemberData(nameof(TypeCodes))]
    public void TypeStageCodes_AllHaveTypeStage(DiagnosticCode code)
    {
        Diagnostics.GetMeta(code).Stage.Should().Be(DiagnosticStage.Type);
    }

    [Theory]
    [MemberData(nameof(GraphCodes))]
    public void GraphStageCodes_AllHaveGraphStage(DiagnosticCode code)
    {
        Diagnostics.GetMeta(code).Stage.Should().Be(DiagnosticStage.Graph);
    }

    [Theory]
    [MemberData(nameof(ProofCodes))]
    public void ProofStageCodes_AllHaveProofStage(DiagnosticCode code)
    {
        Diagnostics.GetMeta(code).Stage.Should().Be(DiagnosticStage.Proof);
    }

    // ── Severity spot-checks ────────────────────────────────────────────────────

    [Fact]
    public void RedundantModifier_HasWarningSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.RedundantModifier).Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void UnreachableState_HasWarningSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnreachableState).Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void UnhandledEvent_HasWarningSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnhandledEvent).Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void DeadEndState_HasWarningSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.DeadEndState).Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void UnsatisfiableGuard_HasWarningSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnsatisfiableGuard).Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void DivisionByZero_HasErrorSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.DivisionByZero).Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void WritableOnEventArg_HasErrorSeverity()
    {
        Diagnostics.GetMeta(DiagnosticCode.WritableOnEventArg).Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void UnprovedModifierRequirement_HasProofStage()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnprovedModifierRequirement).Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Fact]
    public void UnprovedDimensionRequirement_HasProofStage()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnprovedDimensionRequirement).Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Fact]
    public void UnprovedQualifierCompatibility_HasProofStage()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnprovedQualifierCompatibility).Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Fact]
    public void UnsatisfiableInitialState_HasProofStage()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnsatisfiableInitialState).Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Fact]
    public void UnprovedPresenceRequirement_HasProofStage()
    {
        Diagnostics.GetMeta(DiagnosticCode.UnprovedPresenceRequirement).Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Theory]
    [MemberData(nameof(UpdatedMessageTemplates))]
    public void UpdatedDiagnostics_ExposeApprovedMessageTemplates(DiagnosticCode code, string expectedTemplate)
    {
        Diagnostics.GetMeta(code).MessageTemplate.Should().Be(expectedTemplate);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static TheoryData<DiagnosticCode> AllDiagnosticCodes()
    {
        var data = new TheoryData<DiagnosticCode>();
        foreach (var code in Enum.GetValues<DiagnosticCode>())
            data.Add(code);
        return data;
    }

    public static TheoryData<DiagnosticCode, string> UpdatedMessageTemplates => new()
    {
        { DiagnosticCode.FunctionArgConstraintViolation, "Argument {0} to '{1}' is not valid — {2}" },
        { DiagnosticCode.CollectionOperationOnScalar, "'{0}' requires a collection, but '{2}' is a single value — change '{2}' to a set, list, or queue" },
        { DiagnosticCode.InvalidTypedConstantContent, "'{0}' is not a valid {1} — check the expected format for {1} values" },
        { DiagnosticCode.UnsatisfiableGuard, "Guard '{0}' on event '{1}' is unsatisfiable under the declared constraints{2} — this row can never fire" },
        { DiagnosticCode.DivisionByZero, "Division is unsafe: '{0}' can be zero{1}" },
        { DiagnosticCode.SqrtOfNegative, "'{0}' can be negative{1}, so sqrt(...) is unsafe" },
        { DiagnosticCode.UnprovedModifierRequirement, "Cannot prove that '{0}' satisfies the required modifier '{1}'{2}" },
        { DiagnosticCode.UnprovedDimensionRequirement, "'{0}' must declare `of '{1}'` before it can be used here{2}" },
        { DiagnosticCode.UnprovedQualifierCompatibility, "Cannot prove {2} qualifier compatibility between '{0}' [{2}: {4}] and '{1}' [{2}: {5}]{3}" },
        { DiagnosticCode.UnsatisfiableInitialState, "Initial state '{0}' is unsatisfiable: {1}" },
        { DiagnosticCode.UnprovedPresenceRequirement, "Cannot prove that '{0}' is present{1} — guard with 'when {0} is set', initialize it earlier, or make it required" },
        { DiagnosticCode.InvalidInterpolatedTypedConstantForm, "'{0}' doesn't match a recognized pattern for this type — check the expected format (e.g. '{{amount}} USD' for money)" },
        { DiagnosticCode.InterpolatedTypedConstantHoleTypeMismatch, "'{{{1}}}' expects a {2} value, but the expression is {0} — use a compatible field or literal" },
        { DiagnosticCode.DimensionMismatchInUnitSlot, "'{0}' measures {1}, but this field requires {2} — use a unit from the '{2}' dimension" },
    };

    public static TheoryData<DiagnosticCode> LexCodes => new()
    {
        DiagnosticCode.InputTooLarge,
        DiagnosticCode.UnterminatedStringLiteral,
        DiagnosticCode.UnterminatedTypedConstant,
        DiagnosticCode.UnterminatedInterpolation,
        DiagnosticCode.InvalidCharacter,
        DiagnosticCode.UnrecognizedStringEscape,
        DiagnosticCode.UnrecognizedTypedConstantEscape,
        DiagnosticCode.UnescapedBraceInLiteral,
    };

    public static TheoryData<DiagnosticCode> ParseCodes => new()
    {
        DiagnosticCode.ExpectedToken,
        DiagnosticCode.UnexpectedKeyword,
        DiagnosticCode.NonAssociativeComparison,
        DiagnosticCode.InvalidCallTarget,
        DiagnosticCode.AssignmentInExpressionContext,
        DiagnosticCode.EmptyChoice,
        DiagnosticCode.ChoiceMissingElementType,
        DiagnosticCode.ChoiceElementTypeMismatch,
    };

    public static TheoryData<DiagnosticCode> TypeCodes => new()
    {
        // ── Type (main) ──────────────────────────────────────────────────────────
        DiagnosticCode.UndeclaredField,
        DiagnosticCode.TypeMismatch,
        DiagnosticCode.NullInNonNullableContext,
        DiagnosticCode.InvalidMemberAccess,
        DiagnosticCode.FunctionArityMismatch,
        DiagnosticCode.FunctionArgConstraintViolation,
        DiagnosticCode.DuplicateFieldName,
        DiagnosticCode.DuplicateStateName,
        DiagnosticCode.DuplicateEventName,
        DiagnosticCode.DuplicateArgName,
        DiagnosticCode.UndeclaredState,
        DiagnosticCode.UndeclaredEvent,
        DiagnosticCode.UndeclaredFunction,
        DiagnosticCode.MultipleInitialStates,
        DiagnosticCode.NoInitialState,
        DiagnosticCode.InvalidModifierForType,
        DiagnosticCode.InvalidModifierBounds,
        DiagnosticCode.InvalidModifierValue,
        DiagnosticCode.DuplicateModifier,
        DiagnosticCode.RedundantModifier,
        DiagnosticCode.ComputedFieldNotWritable,
        DiagnosticCode.ComputedFieldWithDefault,
        DiagnosticCode.CircularComputedField,
        DiagnosticCode.WritableOnEventArg,
        DiagnosticCode.ConflictingAccessModes,
        DiagnosticCode.ListLiteralOutsideDefault,
        DiagnosticCode.DuplicateChoiceValue,
        // ── Type (choice) — new codes ───────────────────────────────────────────────
        DiagnosticCode.NonChoiceAssignedToChoice,
        DiagnosticCode.ChoiceLiteralNotInSet,
        DiagnosticCode.ChoiceArgOutsideFieldSet,
        DiagnosticCode.ChoiceRankConflict,
        DiagnosticCode.CollectionOperationOnScalar,
        DiagnosticCode.ScalarOperationOnCollection,
        DiagnosticCode.IsSetOnNonOptional,
        DiagnosticCode.EventArgOutOfScope,
        DiagnosticCode.InvalidInterpolationCoercion,
        DiagnosticCode.UnresolvedTypedConstant,
        DiagnosticCode.InvalidTypedConstantContent,
        DiagnosticCode.DefaultForwardReference,
        // ── Type (temporal) ──────────────────────────────────────────────────────
        DiagnosticCode.InvalidDateValue,
        DiagnosticCode.InvalidDateFormat,
        DiagnosticCode.InvalidTimeValue,
        DiagnosticCode.InvalidInstantFormat,
        DiagnosticCode.InvalidTimezoneId,
        DiagnosticCode.UnqualifiedPeriodArithmetic,
        DiagnosticCode.MissingTemporalUnit,
        DiagnosticCode.FractionalUnitValue,
        // ── Type (collection safety) ─────────────────────────────────────────────
        DiagnosticCode.UnguardedCollectionAccess,
        DiagnosticCode.UnguardedCollectionMutation,
        DiagnosticCode.NonOrderableCollectionExtreme,
        DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals,
        // ── Type (CI enforcement) ────────────────────────────────────────────────
        DiagnosticCode.CaseInsensitiveFieldRequiresTildeNotEquals,
        DiagnosticCode.CaseInsensitiveValueInCaseSensitiveContains,
        DiagnosticCode.CaseInsensitiveFieldRequiresTildeStartsWith,
        DiagnosticCode.CaseInsensitiveFieldRequiresTildeEndsWith,
        // ── Type (collection safety — new) ───────────────────────────────────────
        DiagnosticCode.KeyPresenceSafety,
        DiagnosticCode.IndexBoundsGuard,
        DiagnosticCode.KeyUniquenessGuard,
        DiagnosticCode.InvalidQuantifierTarget,
        DiagnosticCode.BindingShadowsField,
        DiagnosticCode.MissingOrderingKey,
        DiagnosticCode.CollectionInnerTypeError,
        DiagnosticCode.QuantifierPredicateNotBoolean,
        // ── Type (business-domain) ───────────────────────────────────────────────
        DiagnosticCode.MaxPlacesExceeded,
        DiagnosticCode.QualifierMismatch,
        DiagnosticCode.DimensionCategoryMismatch,
        DiagnosticCode.CrossCurrencyArithmetic,
        DiagnosticCode.CrossDimensionArithmetic,
        DiagnosticCode.DenominatorUnitMismatch,
        DiagnosticCode.DurationDenominatorMismatch,
        DiagnosticCode.CompoundPeriodDenominator,
        DiagnosticCode.InvalidUnitString,
        DiagnosticCode.InvalidCurrencyCode,
        DiagnosticCode.InvalidDimensionString,
        DiagnosticCode.MutuallyExclusiveQualifiers,
    };

    public static TheoryData<DiagnosticCode> GraphCodes => new()
    {
        DiagnosticCode.UnreachableState,
        DiagnosticCode.UnhandledEvent,
        DiagnosticCode.DeadEndState,
        DiagnosticCode.StructuralSinkState,
    };

    public static TheoryData<DiagnosticCode> ProofCodes=> new()
    {
        DiagnosticCode.UnsatisfiableGuard,
        DiagnosticCode.DivisionByZero,
        DiagnosticCode.SqrtOfNegative,
        DiagnosticCode.UnprovedModifierRequirement,
        DiagnosticCode.UnprovedDimensionRequirement,
        DiagnosticCode.UnprovedQualifierCompatibility,
        DiagnosticCode.UnsatisfiableInitialState,
        DiagnosticCode.UnprovedPresenceRequirement,
    };
}
