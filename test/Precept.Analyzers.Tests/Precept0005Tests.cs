using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0005Tests
{
    // Minimal self-contained type declarations — no project references available in the test harness.
    // BinaryOperationMeta and FunctionOverload are declared as classes here; the analyzer identifies
    // them by type name ("BinaryOperationMeta", "UnaryOperationMeta", "FunctionOverload").
    private const string TypeDecls = @"
namespace Precept.Language
{
    public record ParameterMeta(int Kind, string? Name = null);

    public abstract record ProofSubject;
    public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;
    public sealed record SelfSubject(object? Accessor = null) : ProofSubject;

    public abstract record ProofRequirement(ProofSubject Subject, string Description);
    public sealed record NumericProofRequirement(
        ProofSubject Subject, int Comparison, decimal Threshold, string Description)
        : ProofRequirement(Subject, Description);

    public class BinaryOperationMeta
    {
        public BinaryOperationMeta(
            int Kind, int Op,
            ParameterMeta Lhs, ParameterMeta Rhs,
            int Result, string Description,
            bool BidirectionalLookup = false,
            ProofRequirement[]? ProofRequirements = null) { }
    }

    public class UnaryOperationMeta
    {
        public UnaryOperationMeta(
            int Kind, int Op,
            ParameterMeta Operand,
            int Result, string Description) { }
    }

    public class FunctionOverload
    {
        public FunctionOverload(
            ParameterMeta[] Parameters,
            int ReturnType,
            object? Match = null,
            ProofRequirement[]? ProofRequirements = null) { }
    }
}";

    [Fact]
    public async Task ParamSubject_with_matching_param_is_not_flagged()
    {
        // p1 is in the FunctionOverload's Parameters array and is the ParamSubject arg — clean.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly FunctionOverload Overload = new(
            new ParameterMeta[] { p1 }, 0,
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new ParamSubject(p1), 0, 0m, ""Divisor must be non-zero"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task ParamSubject_with_wrong_param_is_flagged()
    {
        // p2 is NOT in the FunctionOverload's Parameters (only p1 is) — p2 must be flagged.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly ParameterMeta p2 = new(1);
        static readonly FunctionOverload Overload = new(
            new ParameterMeta[] { p1 }, 0,
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new ParamSubject(p2), 0, 0m, ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().HaveCount(1);
    }

    [Fact]
    public async Task ParamSubject_with_binary_op_matching_lhs_is_not_flagged()
    {
        // BinaryOperationMeta: both Lhs and Rhs are p1; ParamSubject(p1) is in the set — clean.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly BinaryOperationMeta Op = new(
            0, 0, p1, p1, 0, ""test"",
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new ParamSubject(p1), 0, 0m, ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task ParamSubject_with_binary_op_wrong_param_is_flagged()
    {
        // BinaryOperationMeta: Lhs=p1, Rhs=p1. ParamSubject(p2) — p2 not in {p1} — flag.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly ParameterMeta p2 = new(1);
        static readonly BinaryOperationMeta Op = new(
            0, 0, p1, p1, 0, ""test"",
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new ParamSubject(p2), 0, 0m, ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().HaveCount(1);
    }

    [Fact]
    public async Task ParamSubject_with_no_enclosing_overload_is_flagged()
    {
        // Standalone new ParamSubject(p) in a method body — no enclosing overload — always wrong.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static void Test()
        {
            var p = new ParameterMeta(0);
            var ps = new ParamSubject(p);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().HaveCount(1);
    }

    [Fact]
    public async Task SelfSubject_is_not_checked()
    {
        // SelfSubject in a proof requirement inside a FunctionOverload — never triggers PRECEPT0005.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly FunctionOverload Overload = new(
            new ParameterMeta[] { new ParameterMeta(0) }, 0,
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new SelfSubject(), 0, 0m, ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task ParamSubject_in_different_namespace_is_not_flagged()
    {
        // ParamSubject outside Precept.Language is not the catalog type — must not be flagged.
        var source = @"
namespace Some.Other.Library
{
    public record ParameterMeta(int Kind);
    public abstract record ProofSubject;
    public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;

    static class Other
    {
        static void Test()
        {
            var p = new ParameterMeta(0);
            var ps = new ParamSubject(p);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0005ParamSubjectMustReferenceOwningParameter>(source);
        diagnostics.Where(d => d.Id == PRECEPT0005ParamSubjectMustReferenceOwningParameter.DiagnosticId)
                   .Should().BeEmpty();
    }
}
