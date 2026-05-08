using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Precept.Analyzers.Tests;

/// <summary>
/// Tests for PRECEPT0024 — Anti-mirroring enforcement for Typed* record .Syntax back-pointers.
/// Verifies that .Syntax access on guarded Typed* records fires PRECEPT0024 outside TypeChecker
/// and stays silent inside TypeChecker or for non-guarded types.
/// </summary>
public class Precept0024Tests
{
    // ── Shared stubs ──────────────────────────────────────────────────────────
    // Minimal record definitions that mirror the real SemanticIndex shapes enough
    // for the analyzer to resolve types semantically.

    private const string TypeStubs = @"
namespace Precept.Pipeline
{
    public record ParsedConstruct();

    public sealed record TypedField(
        string Name,
        ParsedConstruct Syntax);

    public sealed record TypedState(
        string Name,
        ParsedConstruct Syntax);

    public sealed record TypedEvent(
        string Name,
        ParsedConstruct Syntax);

    public sealed record TypedTransitionRow(
        string From,
        string To,
        ParsedConstruct Syntax);

    public sealed record TypedRule(
        string Name,
        ParsedConstruct Syntax);

    public sealed record TypedEnsure(
        string Name,
        ParsedConstruct Syntax);

    public sealed record TypedAccessMode(
        string Mode,
        ParsedConstruct Syntax);

    public sealed record TypedStateHook(
        string StateName,
        ParsedConstruct Syntax);

    public sealed record TypedEventHandler(
        string EventName,
        ParsedConstruct Syntax);

    public sealed record TypedEditDeclaration(
        string StateName,
        ParsedConstruct Syntax);
}
";

    // ── True positives ────────────────────────────────────────────────────────

    /// <summary>
    /// TP1: .Syntax accessed on TypedField in a GraphAnalyzer-like class → PRECEPT0024.
    /// </summary>
    private const string TP1_GraphAnalyzerAccess = @"
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class GraphAnalyzer
    {
        public void Analyze(TypedField field)
        {
            var s = field.Syntax;
        }
    }
}
";

    [Fact]
    public async Task TP1_SyntaxOnTypedField_InGraphAnalyzer_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TP1_GraphAnalyzerAccess);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0024");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("TypedField");
        d.GetMessage().Should().Contain("TypeChecker");
    }

    /// <summary>
    /// TP2: .Syntax accessed on TypedTransitionRow in a ProofEngine-like class → PRECEPT0024.
    /// </summary>
    private const string TP2_ProofEngineAccess = @"
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class ProofEngine
    {
        public void Verify(TypedTransitionRow row)
        {
            var s = row.Syntax;
        }
    }
}
";

    [Fact]
    public async Task TP2_SyntaxOnTypedTransitionRow_InProofEngine_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TP2_ProofEngineAccess);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0024");
        d.GetMessage().Should().Contain("TypedTransitionRow");
    }

    /// <summary>
    /// TP3: .Syntax accessed on TypedRule in a Builder-like class → PRECEPT0024.
    /// </summary>
    private const string TP3_BuilderAccess = @"
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class Builder
    {
        public void Build(TypedRule rule)
        {
            var s = rule.Syntax;
        }
    }
}
";

    [Fact]
    public async Task TP3_SyntaxOnTypedRule_InBuilder_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TP3_BuilderAccess);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0024");
        d.GetMessage().Should().Contain("TypedRule");
    }

    /// <summary>
    /// TP4: .Syntax accessed inside a lambda within a non-TypeChecker class → PRECEPT0024.
    /// Closures don't exempt — the enclosing type is what matters.
    /// </summary>
    private const string TP4_LambdaAccess = @"
using System;
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class SomeDownstream
    {
        public void Process(TypedEvent evt)
        {
            Action a = () => { var s = evt.Syntax; };
        }
    }
}
";

    [Fact]
    public async Task TP4_SyntaxInLambda_InNonTypeChecker_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TP4_LambdaAccess);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0024");
        d.GetMessage().Should().Contain("TypedEvent");
    }

    // ── True negatives ────────────────────────────────────────────────────────

    /// <summary>
    /// TN1: .Syntax accessed on TypedField inside the TypeChecker class → no diagnostic.
    /// </summary>
    private const string TN1_TypeCheckerAccess = @"
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class TypeChecker
    {
        public void Check(TypedField field)
        {
            var s = field.Syntax;
        }
    }
}
";

    [Fact]
    public async Task TN1_SyntaxInsideTypeChecker_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TN1_TypeCheckerAccess);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN2: .Syntax accessed on a non-Typed* class (Foo with its own Syntax property)
    /// → no diagnostic (guard is type-specific to the Precept.Pipeline Typed* records).
    /// </summary>
    private const string TN2_NonGuardedType = @"
namespace SomeOtherNamespace
{
    public class Foo
    {
        public string Syntax { get; set; } = """";
    }

    public class Consumer
    {
        public void Use(Foo foo)
        {
            var s = foo.Syntax;
        }
    }
}
";

    [Fact]
    public async Task TN2_SyntaxOnNonGuardedType_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TN2_NonGuardedType);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN3: Another property (.Name) on TypedField → no diagnostic.
    /// Only .Syntax is guarded.
    /// </summary>
    private const string TN3_OtherProperty = @"
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class GraphAnalyzer
    {
        public void Analyze(TypedField field)
        {
            var n = field.Name;
        }
    }
}
";

    [Fact]
    public async Task TN3_NonSyntaxPropertyOnTypedField_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TN3_OtherProperty);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN4: .Syntax access on TypedField inside a nested class within TypeChecker → no diagnostic.
    /// The nested class is still enclosed by TypeChecker.
    /// </summary>
    private const string TN4_NestedClassInTypeChecker = @"
using Precept.Pipeline;

namespace Precept.Pipeline
{
    public class TypeChecker
    {
        private class Helper
        {
            public void Inspect(TypedField field)
            {
                var s = field.Syntax;
            }
        }
    }
}
";

    [Fact]
    public async Task TN4_SyntaxInNestedClassInsideTypeChecker_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0024AntiMirroringEnforcement>(
            TypeStubs, TN4_NestedClassInTypeChecker);

        diagnostics.Should().BeEmpty();
    }
}
