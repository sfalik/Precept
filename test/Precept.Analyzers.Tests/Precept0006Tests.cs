using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0006Tests
{
    // Minimal self-contained type declarations.
    // BinaryOperationMeta declared as a class for the operation context tests (rules 2 needs it).
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

    public sealed record PresenceProofRequirement(ProofSubject Subject, string Description)
        : ProofRequirement(Subject, Description);

    public class BinaryOperationMeta
    {
        public BinaryOperationMeta(
            int Kind, int Op,
            ParameterMeta Lhs, ParameterMeta Rhs,
            int Result, string Description,
            ProofRequirement[]? ProofRequirements = null) { }
    }
}";

    // ── Rule 1: PresenceProofRequirement subject validity ────────────────────────────

    [Fact]
    public async Task NumericProofRequirement_with_ParamSubject_is_valid()
    {
        // ParamSubject is legal in NumericProofRequirement — rule 1 only guards PresenceProofRequirement.
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task PresenceProofRequirement_with_SelfSubject_is_valid()
    {
        // SelfSubject (no accessor) in PresenceProofRequirement — the canonical valid pattern.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly BinaryOperationMeta Op = new(
            0, 0, p1, p1, 0, ""test"",
            ProofRequirements: new ProofRequirement[] {
                new PresenceProofRequirement(new SelfSubject(), ""Field must be set"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task PresenceProofRequirement_with_ParamSubject_is_flagged()
    {
        // ParamSubject in PresenceProofRequirement — rule 1 fires.
        // Presence proof is about the field itself, not a function parameter.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly BinaryOperationMeta Op = new(
            0, 0, p1, p1, 0, ""test"",
            ProofRequirements: new ProofRequirement[] {
                new PresenceProofRequirement(new ParamSubject(p1), ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().HaveCount(1);
    }

    // ── Rule 2: SelfSubject accessor placement ───────────────────────────────────────

    [Fact]
    public async Task SelfSubject_with_accessor_in_NumericProofRequirement_inside_operation_is_flagged()
    {
        // SelfSubject(accessor) in NumericProofRequirement inside a BinaryOperationMeta — rule 2 fires.
        // The accessor here likely belongs in PresenceProofRequirement, not a numeric constraint.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly object SomeAccessor = new object();
        static readonly BinaryOperationMeta Op = new(
            0, 0, p1, p1, 0, ""test"",
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new SelfSubject(SomeAccessor), 0, 0m, ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().HaveCount(1);
    }

    [Fact]
    public async Task SelfSubject_without_accessor_in_NumericProofRequirement_is_valid()
    {
        // SelfSubject() (null accessor) in NumericProofRequirement — accessor is absent, rule 2 does not fire.
        var source = TypeDecls + @"
namespace Precept.Language
{
    static class Catalog
    {
        static readonly ParameterMeta p1 = new(0);
        static readonly BinaryOperationMeta Op = new(
            0, 0, p1, p1, 0, ""test"",
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new SelfSubject(), 0, 0m, ""test"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task SelfSubject_with_accessor_outside_operation_context_is_not_flagged()
    {
        // SelfSubject(accessor) in NumericProofRequirement that is NOT inside a
        // BinaryOperationMeta/UnaryOperationMeta/FunctionOverload — rule 2 does not fire.
        // This mirrors the TypeAccessor.ProofRequirements pattern in Types.cs (count > 0).
        var source = TypeDecls + @"
namespace Precept.Language
{
    public class TypeAccessor
    {
        public TypeAccessor(string Name, ProofRequirement[]? ProofRequirements = null) { }
    }

    static class Catalog
    {
        static readonly object CountAccessor = new object();
        static readonly TypeAccessor Accessor = new TypeAccessor(""peek"",
            ProofRequirements: new ProofRequirement[] {
                new NumericProofRequirement(new SelfSubject(CountAccessor), 0, 0m, ""must be non-empty"")
            });
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task ProofSubject_in_different_namespace_is_not_flagged()
    {
        // PresenceProofRequirement outside Precept.Language is not the catalog type — not flagged.
        var source = @"
namespace Some.Other.Library
{
    public record ParameterMeta(int Kind);
    public abstract record ProofSubject;
    public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;
    public sealed record PresenceProofRequirement(ProofSubject Subject, string Description);

    static class Other
    {
        static readonly ParameterMeta p = new(0);
        static readonly PresenceProofRequirement R = new(new ParamSubject(p), ""test"");
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0006ProofSubjectPlacementIsValid>(source);
        diagnostics.Where(d => d.Id == PRECEPT0006ProofSubjectPlacementIsValid.DiagnosticId)
                   .Should().BeEmpty();
    }
}
