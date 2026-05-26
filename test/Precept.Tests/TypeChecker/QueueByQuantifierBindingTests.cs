using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Two-axis quantifier-binding projection for `queue of T by P` and `log of T by P`:
/// the binding identifier exposes `.value` returning T and `.by` returning P.
/// Bare references resolve to T (single-axis convenience, matching single-type
/// collection bindings).
/// </summary>
public class QueueByQuantifierBindingTests
{
    [Fact]
    public void QueueBy_BindingByAccess_ResolvesToOrderingType()
    {
        var precept = """
            precept Widget
            field Tasks as queue of string by integer
            field MaxPriority as integer default 0
            rule no task in Tasks (task.by > MaxPriority)
                because "Tasks must have priority at or below MaxPriority"
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidMemberAccess),
            because: "task.by on a queue of T by P binding resolves to P (integer)");
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.TypeMismatch),
            because: "task.by has type integer and is comparable to MaxPriority");
    }

    [Fact]
    public void QueueBy_BindingValueAccess_ResolvesToElementType()
    {
        var precept = """
            precept Widget
            field Tasks as queue of string by integer
            field TargetTask as string default ""
            rule any task in Tasks (task.value == TargetTask)
                because "TargetTask must be present in the queue"
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidMemberAccess),
            because: "task.value on a queue of T by P binding resolves to T (string)");
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.TypeMismatch));
    }

    [Fact]
    public void QueueBy_BindingCompoundPredicate_ResolvesBothAxes()
    {
        var precept = """
            precept Widget
            field Tasks as queue of string by integer
            rule no task in Tasks (task.by > 5 and task.value == "")
                because "High-priority tasks must have a non-empty value"
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidMemberAccess));
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.TypeMismatch));
    }

    [Fact]
    public void LogBy_BindingByAccess_ResolvesToOrderingType()
    {
        var precept = """
            precept Widget
            field AuditLog as log of string by integer
            field MaxSeq as integer default 100
            rule no entry in AuditLog (entry.by > MaxSeq)
                because "Audit-log entries must have a sequence at or below MaxSeq"
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidMemberAccess),
            because: "entry.by on a log of T by P binding resolves to P (integer)");
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.TypeMismatch));
    }

    [Fact]
    public void QueueBy_BindingBareReference_StillResolvesToElement()
    {
        // Backwards compatibility: bare reference to the binding identifier resolves
        // to the element type (T), consistent with single-type collection bindings.
        // `.value` is the explicit alias; bare reference is the implicit form.
        var precept = """
            precept Widget
            field Tasks as queue of string by integer
            field TargetTask as string default ""
            rule any task in Tasks (task == TargetTask)
                because "Bare binding reference resolves to element type"
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.TypeMismatch),
            because: "bare binding reference on queue-by behaves like single-axis (resolves to T)");
    }

    [Fact]
    public void SingleAxisQueue_BindingByAccess_EmitsInvalidMemberAccess()
    {
        // Regression guard: on a plain `queue of T` (no `by P`), the binding has no
        // ordering axis. `.by` access must error (it's not a valid string accessor).
        var precept = """
            precept Widget
            field Tasks as queue of string
            field MaxValue as string default ""
            rule no task in Tasks (task.by == MaxValue)
                because "ensures task has no by accessor on a single-axis queue"
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.InvalidMemberAccess),
            because: ".by is not valid on a queue without an ordering axis");
    }
}
