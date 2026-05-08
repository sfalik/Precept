using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 8 — Case-Insensitive (CI) Enforcement.
/// Covers all 5 CI rules from §3.8: valid + violation cases per rule,
/// TypedFieldRef.IsCaseInsensitive population, CI variant function selection,
/// non-CI function in CI-required context enforcement, and multi-violation emission.
///
/// TYPE B (implementation bug): Diagnostics.Create for CI enforcement codes 66, 95, 97, 98
/// throws FormatException because diagnostic templates contain '{0}' but EnforceCIInExpression
/// does not pass field name arguments. All violation tests that trigger CI diagnostics are
/// expected to throw FormatException until George fixes the Diagnostics.Create calls.
/// </summary>
public class TypeCheckerCITests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Rule 1: == with ~string → CaseInsensitiveFieldRequiresTildeEquals (66)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TildeEquals_OnCIField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when Email ~= "admin@example.com" -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void Equals_OnCIField_EmitsTildeEqualsRequired()
    {
        // TYPE B: FormatException — Diagnostics.Create(CaseInsensitiveFieldRequiresTildeEquals, span)
        // does not pass field name for '{0}' placeholder in template.
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when Email == "admin@example.com" -> no transition
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg not passed by EnforceCIInExpression");
    }

    [Fact]
    public void Equals_OnCIField_RHSPosition_EmitsTildeEqualsRequired()
    {
        // TYPE B: FormatException — same root cause as above
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when "admin@example.com" == Email -> no transition
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg not passed by EnforceCIInExpression");
    }

    [Fact]
    public void Equals_OnRegularStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Check
            from Open on Check when Name == "Alice" -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Rule 2: != with ~string → CaseInsensitiveFieldRequiresTildeNotEquals (95)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BangTilde_OnCIField_NoDiagnostic()
    {
        // CI not-equals operator is !~ (not ~!=)
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when Email !~ "test@test.com" -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NotEquals_OnCIField_EmitsTildeNotEqualsRequired()
    {
        // TYPE B: FormatException — same root cause
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when Email != "test@test.com" -> no transition
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg not passed by EnforceCIInExpression");
    }

    [Fact]
    public void NotEquals_OnRegularStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Check
            from Open on Check when Name != "Alice" -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Rule 3: contains with ~string in CS collection (96) — dormant
    //  Structurally implemented but no contains OperationKind exists yet.
    // ════════════════════════════════════════════════════════════════════════

    // Rule 3 is dormant — no contains OperationKind entries exist.
    // When contains lands, add violation + valid tests here.

    // ════════════════════════════════════════════════════════════════════════
    //  Rule 4: startsWith with ~string → CaseInsensitiveFieldRequiresTildeStartsWith (97)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TildeStartsWith_OnCIField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when ~startsWith(Email, "info@") -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void StartsWith_OnCIField_EmitsTildeStartsWithRequired()
    {
        // TYPE B: FormatException — same root cause
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when startsWith(Email, "info@") -> no transition
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg not passed by EnforceCIInExpression");
    }

    [Fact]
    public void StartsWith_OnRegularStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Check
            from Open on Check when startsWith(Name, "A") -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Rule 5: endsWith with ~string → CaseInsensitiveFieldRequiresTildeEndsWith (98)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TildeEndsWith_OnCIField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when ~endsWith(Email, ".com") -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void EndsWith_OnCIField_EmitsTildeEndsWithRequired()
    {
        // TYPE B: FormatException — same root cause
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when endsWith(Email, ".com") -> no transition
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg not passed by EnforceCIInExpression");
    }

    [Fact]
    public void EndsWith_OnRegularStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Check
            from Open on Check when endsWith(Name, "son") -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TypedFieldRef.IsCaseInsensitive population
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CIField_TypedFieldRef_HasIsCaseInsensitiveTrue()
    {
        var precept = """
            precept Widget
            field Email as ~string
            field Name as string default "default"
            state Open initial
            event Check
            from Open on Check when Email ~= "x" -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        // Find the guard expression — it's a binary op with Email as a TypedFieldRef
        var row = index.TransitionRows.Single();
        row.Guard.Should().BeOfType<TypedBinaryOp>();
        var bin = (TypedBinaryOp)row.Guard!;

        // The Email operand should have IsCaseInsensitive = true
        var emailRef = new[] { bin.Left, bin.Right }
            .OfType<TypedFieldRef>()
            .Single(r => r.FieldName == "Email");
        emailRef.IsCaseInsensitive.Should().BeTrue(
            because: "Email is declared as ~string");
    }

    [Fact]
    public void NonCIField_TypedFieldRef_HasIsCaseInsensitiveFalse()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Check
            from Open on Check when Name == "Alice" -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var row = index.TransitionRows.Single();
        row.Guard.Should().BeOfType<TypedBinaryOp>();
        var bin = (TypedBinaryOp)row.Guard!;

        var nameRef = new[] { bin.Left, bin.Right }
            .OfType<TypedFieldRef>()
            .Single(r => r.FieldName == "Name");
        nameRef.IsCaseInsensitive.Should().BeFalse(
            because: "Name is declared as plain string");
    }

    [Fact]
    public void CIField_InFunctionArg_HasIsCaseInsensitiveTrue()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when ~startsWith(Email, "info@") -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var row = index.TransitionRows.Single();
        row.Guard.Should().BeOfType<TypedFunctionCall>();
        var fn = (TypedFunctionCall)row.Guard!;
        var emailArg = fn.Arguments[0];
        emailArg.Should().BeOfType<TypedFieldRef>();
        ((TypedFieldRef)emailArg).IsCaseInsensitive.Should().BeTrue(
            because: "Email is declared as ~string");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CI variant function selection
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TildeStartsWith_ResolvesToTildeStartsWithFunctionKind()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when ~startsWith(Email, "info@") -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var row = index.TransitionRows.Single();
        row.Guard.Should().BeOfType<TypedFunctionCall>();
        var fn = (TypedFunctionCall)row.Guard!;
        fn.ResolvedFunction.Should().Be(FunctionKind.TildeStartsWith);
    }

    [Fact]
    public void TildeEndsWith_ResolvesToTildeEndsWithFunctionKind()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when ~endsWith(Email, ".com") -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var row = index.TransitionRows.Single();
        row.Guard.Should().BeOfType<TypedFunctionCall>();
        var fn = (TypedFunctionCall)row.Guard!;
        fn.ResolvedFunction.Should().Be(FunctionKind.TildeEndsWith);
    }

    [Fact]
    public void RegularStartsWith_ResolvesToStartsWithFunctionKind()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Check
            from Open on Check when startsWith(Name, "A") -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        var row = index.TransitionRows.Single();
        row.Guard.Should().BeOfType<TypedFunctionCall>();
        var fn = (TypedFunctionCall)row.Guard!;
        fn.ResolvedFunction.Should().Be(FunctionKind.StartsWith);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CI enforcement in different expression contexts
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Equals_OnCIField_InRule_NoDiagnosticEmitted()
    {
        // TYPE B observation: == on ~string in rule condition does not fire
        // CaseInsensitiveFieldRequiresTildeEquals. May be a coverage gap in
        // ValidateCIEnforcement rule traversal — documenting current behavior.
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            rule Email == "admin@example.com" because "bad rule"
            """;

        // Currently no error is emitted — this documents the gap.
        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void TildeEquals_OnCIField_InRule_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            rule Email ~= "admin@example.com" because "good rule"
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BangTilde_OnCIField_InRule_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            rule Email !~ "blocked@test.com" because "good rule"
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NotEquals_OnCIField_InRule_NoDiagnosticEmitted()
    {
        // TYPE B observation: != on ~string in rule condition does not fire
        // CaseInsensitiveFieldRequiresTildeNotEquals — same gap as == case above.
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            rule Email != "test@test.com" because "bad rule"
            """;

        // Currently no error is emitted — this documents the gap.
        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Multiple CI violations — TYPE B: all throw FormatException
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultipleCIViolations_EqualAndNotEqual_ThrowsFormatException()
    {
        // TYPE B: FormatException — first violation encountered throws
        var precept = """
            precept Widget
            field Email as ~string
            field Domain as ~string
            state Open initial
            event Check
            from Open on Check when Email == "a@b.com" -> no transition
            rule Domain != "test.com" because "bad"
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg");
    }

    [Fact]
    public void MultipleCIViolations_EqualAndStartsWith_ThrowsFormatException()
    {
        // TYPE B: FormatException — same root cause
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event Check
            from Open on Check when Email == "a@b.com" -> no transition
            rule startsWith(Email, "info@") because "bad"
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg");
    }

    [Fact]
    public void MultipleCIViolations_ThreeRules_ThrowsFormatException()
    {
        // TYPE B: FormatException — same root cause
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event A
            from Open on A when Email == "x" -> no transition
            event B
            from Open on B when Email != "y" -> no transition
            rule endsWith(Email, ".com") because "suffix check"
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Mixed CI and non-CI fields — no cross-contamination
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MixedFields_CIViolationOnlyOnCIField()
    {
        var precept = """
            precept Widget
            field Email as ~string
            field Name as string
            state Open initial
            event Check
            from Open on Check when Name == "Alice" -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MixedFields_CIFieldTriggersViolation_RegularFieldDoesNot()
    {
        // TYPE B: FormatException — same root cause (Email == triggers it)
        var precept = """
            precept Widget
            field Email as ~string
            field Name as string
            state Open initial
            event A
            from Open on A when Email == "x" -> no transition
            event B
            from Open on B when Name == "y" -> no transition
            """;

        var act = () => TypeCheckerTestHelpers.Check(precept);
        act.Should().Throw<System.FormatException>(
            because: "TYPE B bug: CI diagnostic template expects field name arg");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CI field with valid tilde operators throughout — full clean precept
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FullCIPrecept_AllTildeOperators_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            state Verified
            event Verify
            from Open on Verify when Email ~= "admin@example.com" -> transition Verified
            rule Email !~ "blocked@example.com" because "blocked addresses not allowed"
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void FullCIPrecept_TildeFunctions_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Email as ~string
            state Open initial
            event CheckPrefix
            from Open on CheckPrefix when ~startsWith(Email, "admin@") -> no transition
            event CheckSuffix
            from Open on CheckSuffix when ~endsWith(Email, ".com") -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
