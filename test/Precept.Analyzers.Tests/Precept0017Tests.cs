using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0017Tests
{
    // ── Passing: Kind argument is the switch value parameter ─────────────────

    /// <summary>
    /// Standard pattern: every arm passes 'kind' (the switch value) as the first arg.
    /// No diagnostic expected.
    /// </summary>
    private const string PassesKindVariable = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus, Negate }

    public sealed record TokenMeta(TokenKind Kind, string Text);
    public enum TokenKind { Plus, Minus }

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Plus  => new(kind, ""+""),
            TokenKind.Minus => new(kind, ""-""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public sealed record OperatorMeta(OperatorKind Kind, TokenMeta Token, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus   => new(kind, Tokens.GetMeta(TokenKind.Plus),   ""Addition""),
            OperatorKind.Minus  => new(kind, Tokens.GetMeta(TokenKind.Minus),  ""Subtraction""),
            OperatorKind.Negate => new(kind, Tokens.GetMeta(TokenKind.Minus),  ""Negation""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task KindVariableAsFirstArg_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(PassesKindVariable);
        diagnostics.Should().BeEmpty();
    }

    // ── Passing: Explicit enum constant matching the arm pattern ─────────────

    /// <summary>
    /// Some catalogs might use explicit constants instead of 'kind'. Legal if they match the arm.
    /// </summary>
    private const string PassesExplicitMatchingConstant = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, TypeMismatch }

    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.DivisionByZero), ""Divisor is zero""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";

    [Fact]
    public async Task ExplicitMatchingConstant_NoDiagnostic()
    {
        // FaultMeta's first arg is a string (from nameof), not an enum reference.
        // The analyzer only checks IObjectCreationOperation first args that are enum references.
        // This test verifies non-enum first args don't produce false positives.
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(PassesExplicitMatchingConstant);
        diagnostics.Should().BeEmpty();
    }

    // ── Passing: Explicit matching enum constant ─────────────────────────────

    private const string PassesExplicitEnumMatch = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(OperatorKind.Plus,  ""Addition""),
            OperatorKind.Minus => new(OperatorKind.Minus, ""Subtraction""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task ExplicitEnumMatchingArm_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(PassesExplicitEnumMatch);
        diagnostics.Should().BeEmpty();
    }

    // ── Failing: Wrong enum constant (copy-paste bug) ───────────────────────

    /// <summary>
    /// Classic copy-paste error: arm handles Plus but passes OperatorKind.Minus.
    /// </summary>
    private const string MismatchedKind = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(OperatorKind.Minus, ""Addition""),
            OperatorKind.Minus => new(OperatorKind.Minus, ""Subtraction""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task MismatchedKindConstant_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(MismatchedKind);
        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0017");
        d.Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("Plus");
        d.GetMessage().Should().Contain("Minus");
    }

    // ── Failing: Multiple mismatches in same switch ─────────────────────────

    private const string MultipleMismatches = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus, Negate }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus   => new(OperatorKind.Negate, ""Addition""),
            OperatorKind.Minus  => new(OperatorKind.Plus,   ""Subtraction""),
            OperatorKind.Negate => new(kind,                ""Negation""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task MultipleMismatches_ReportsEach()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(MultipleMismatches);
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().OnlyContain(d => d.Id == "PRECEPT0017");
        // Plus arm passes Negate, Minus arm passes Plus. Negate arm passes 'kind' (correct).
    }

    // ── Passing: TypeKind (different catalog, same check) ───────────────────

    private const string TypeKindCorrect = @"
namespace Precept.Language
{
    public enum TypeKind { String, Boolean, Integer }

    public sealed record TypeMeta(TypeKind Kind, string Name);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(kind, ""string""),
            TypeKind.Boolean => new(kind, ""boolean""),
            TypeKind.Integer => new(kind, ""integer""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task TypeKindSwitch_KindVariable_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(TypeKindCorrect);
        diagnostics.Should().BeEmpty();
    }

    // ── Failing: TypeKind mismatch ──────────────────────────────────────────

    private const string TypeKindMismatch = @"
namespace Precept.Language
{
    public enum TypeKind { String, Boolean, Integer }

    public sealed record TypeMeta(TypeKind Kind, string Name);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(TypeKind.Boolean, ""string""),
            TypeKind.Boolean => new(kind,             ""boolean""),
            TypeKind.Integer => new(kind,             ""integer""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task TypeKindMismatch_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(TypeKindMismatch);
        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("String");
        diagnostics[0].GetMessage().Should().Contain("Boolean");
    }

    // ── Scope guard: wrong method name → no diagnostic ──────────────────────

    private const string NotGetMetaMethod = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta Lookup(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(OperatorKind.Minus, ""wrong""),
            OperatorKind.Minus => new(OperatorKind.Plus,  ""wrong""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task WrongMethodName_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(NotGetMetaMethod);
        diagnostics.Should().BeEmpty();
    }

    // ── Scope guard: wrong namespace → no diagnostic ────────────────────────

    private const string WrongNamespace = @"
namespace SomethingElse
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(OperatorKind.Minus, ""wrong""),
            OperatorKind.Minus => new(OperatorKind.Plus,  ""wrong""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(WrongNamespace);
        diagnostics.Should().BeEmpty();
    }

    // ── Scope guard: non-catalog enum → no diagnostic ───────────────────────

    private const string NonCatalogEnum = @"
namespace Precept.Language
{
    public enum MyCustomKind { Foo, Bar }

    public sealed record CustomMeta(MyCustomKind Kind, string Name);

    public static class Custom
    {
        public static CustomMeta GetMeta(MyCustomKind kind) => kind switch
        {
            MyCustomKind.Foo => new(MyCustomKind.Bar, ""wrong""),
            MyCustomKind.Bar => new(MyCustomKind.Foo, ""wrong""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task NonCatalogEnum_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(NonCatalogEnum);
        diagnostics.Should().BeEmpty();
    }

    // ── Edge: discard-only arms → no diagnostic (nothing to check) ──────────

    private const string DiscardOnlyArms = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            _ => new(kind, ""fallback""),
        };
    }
}";

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(DiscardOnlyArms);
        diagnostics.Should().BeEmpty();
    }

    // ── Edge: arm body is not an object creation → no diagnostic ────────────

    private const string ArmBodyIsMethodCall = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        private static OperatorMeta Make(OperatorKind k, string d) => new(k, d);

        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => Make(OperatorKind.Minus, ""wrong""),
            OperatorKind.Minus => Make(OperatorKind.Plus,  ""wrong""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task ArmBodyIsMethodCall_NoDiagnostic()
    {
        // The analyzer only checks IObjectCreationOperation, not method invocations.
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(ArmBodyIsMethodCall);
        diagnostics.Should().BeEmpty();
    }

    // ── Edge: no constructor args → no diagnostic ───────────────────────────

    private const string NoConstructorArgs = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta();

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(),
            OperatorKind.Minus => new(),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task NoConstructorArgs_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(NoConstructorArgs);
        diagnostics.Should().BeEmpty();
    }

    // ── Diagnostic message format ───────────────────────────────────────────

    [Fact]
    public async Task DiagnosticMessage_ContainsCatalogEnumAndBothNames()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(MismatchedKind);
        diagnostics.Should().ContainSingle();
        var msg = diagnostics[0].GetMessage();
        msg.Should().Contain("OperatorKind"); // catalog enum type
        msg.Should().Contain("Plus");          // arm case
        msg.Should().Contain("Minus");         // actual value passed
        msg.Should().Contain("must be");       // prescription
    }

    // ── Real-world pattern: TokenKind in Tokens catalog ─────────────────────

    private const string TokenKindCorrect = @"
namespace Precept.Language
{
    public enum TokenKind { Or, And, Not }

    public sealed record TokenMeta(TokenKind Kind, string Text);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Or  => new(kind, ""or""),
            TokenKind.And => new(kind, ""and""),
            TokenKind.Not => new(kind, ""not""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task TokenKindSwitch_Correct_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(TokenKindCorrect);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TokenKindSwitch_Mismatch_Reports()
    {
        var source = @"
namespace Precept.Language
{
    public enum TokenKind { Or, And, Not }

    public sealed record TokenMeta(TokenKind Kind, string Text);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Or  => new(TokenKind.And, ""or""),
            TokenKind.And => new(kind,          ""and""),
            TokenKind.Not => new(kind,          ""not""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0017OperatorsCrossRef>(source);
        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("Or");
        diagnostics[0].GetMessage().Should().Contain("And");
    }
}
