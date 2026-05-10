using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class ModifierValidationTests
{
    [Fact]
    public void ChoiceLiteral_InComparisonContext_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field Priority as choice of string("Low", "Medium", "High") default "Low" writable
            state Open initial
            state Done terminal
            event Escalate
            from Open on Escalate when Priority == "High" -> transition Done
            """);

        index.TransitionRows.Single().Guard.Should().BeOfType<TypedBinaryOp>();
    }

    [Fact]
    public void RedundantModifier_SubsumedConstraint_EmitsSingleWarning()
    {
        var (_, diagnostics) = TypeCheckerTestHelpers.Check("""
            precept Example
            field Score as number nonnegative positive default 1
            """);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.RedundantModifier));
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.InvalidModifierForType));
        diagnostics.Single(d => d.Code == nameof(DiagnosticCode.RedundantModifier)).Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void InvalidModifierBounds_MinExceedsMax_EmitsError()
    {
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field Score as number min 100 max 50
            """, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void InvalidModifierBounds_MinlengthExceedsMaxlength_EmitsError()
    {
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field Name as string minlength 10 maxlength 5
            """, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void InvalidModifierBounds_MincountExceedsMaxcount_EmitsError()
    {
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field Tags as set of string mincount 3 maxcount 1
            """, DiagnosticCode.InvalidModifierBounds);
    }

    [Fact]
    public void QueueBy_PeekbyAccessor_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field Tasks as queue of string by integer notempty
            field NextPriority as integer <- Tasks.peekby
            """);

        index.FieldsByName["NextPriority"].ComputedExpression.Should().BeOfType<TypedMemberAccess>();
    }

    [Fact]
    public void QuantifierBinding_CIString_RequiresTildeEquals()
    {
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field Tags as set of ~string notempty
            rule each Tag in Tags (Tag == "required") because "required tag missing"
            """, DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals);
    }
}
