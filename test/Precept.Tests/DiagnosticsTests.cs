using System;
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

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static TheoryData<DiagnosticCode> AllDiagnosticCodes()
    {
        var data = new TheoryData<DiagnosticCode>();
        foreach (var code in Enum.GetValues<DiagnosticCode>())
            data.Add(code);
        return data;
    }

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
        DiagnosticCode.EmptyChoice,
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
        DiagnosticCode.CaseInsensitiveStringOnNonCollection,
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
    };

    public static TheoryData<DiagnosticCode> ProofCodes => new()
    {
        DiagnosticCode.UnsatisfiableGuard,
        DiagnosticCode.DivisionByZero,
        DiagnosticCode.SqrtOfNegative,
    };
}
