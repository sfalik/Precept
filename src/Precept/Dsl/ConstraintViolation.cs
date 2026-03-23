using System.Collections.Generic;

namespace Precept;

// ── Target hierarchy ──────────────────────────────────────────

public enum ConstraintTargetKind
{
    Field,
    EventArg,
    Event,
    State,
    Definition
}

public abstract record ConstraintTarget(ConstraintTargetKind Kind)
{
    public sealed record FieldTarget(string FieldName)
        : ConstraintTarget(ConstraintTargetKind.Field);

    public sealed record EventArgTarget(string EventName, string ArgName)
        : ConstraintTarget(ConstraintTargetKind.EventArg);

    public sealed record EventTarget(string EventName)
        : ConstraintTarget(ConstraintTargetKind.Event);

    public sealed record StateTarget(string StateName, AssertAnchor? Anchor = null)
        : ConstraintTarget(ConstraintTargetKind.State);

    public sealed record DefinitionTarget()
        : ConstraintTarget(ConstraintTargetKind.Definition);
}

// ── Source hierarchy ──────────────────────────────────────────

public enum ConstraintSourceKind
{
    Invariant,
    StateAssertion,
    EventAssertion,
    TransitionRejection
}

public abstract record ConstraintSource(ConstraintSourceKind Kind, int? SourceLine = null)
{
    public sealed record InvariantSource(string ExpressionText, string Reason, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.Invariant, SourceLine);

    public sealed record StateAssertionSource(string ExpressionText, string Reason,
        string StateName, AssertAnchor Anchor, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.StateAssertion, SourceLine);

    public sealed record EventAssertionSource(string ExpressionText, string Reason,
        string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.EventAssertion, SourceLine);

    public sealed record TransitionRejectionSource(string Reason, string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.TransitionRejection, SourceLine);
}

// ── Violation record ──────────────────────────────────────────

public sealed record ConstraintViolation(
    string Message,
    ConstraintSource Source,
    IReadOnlyList<ConstraintTarget> Targets)
{
    /// <summary>
    /// Creates a simple violation from a plain message string (for compatibility/validation errors
    /// that don't have structured source info).
    /// </summary>
    internal static ConstraintViolation Simple(string message)
        => new(message,
            new ConstraintSource.InvariantSource("", message),
            new ConstraintTarget[] { new ConstraintTarget.DefinitionTarget() });
}

// ── Expression subjects (compile-time extraction) ─────────────

public sealed record ExpressionSubjects(
    IReadOnlyList<string> FieldReferences,
    IReadOnlyList<(string Event, string Arg)> ArgReferences)
{
    public static readonly ExpressionSubjects Empty =
        new(System.Array.Empty<string>(), System.Array.Empty<(string, string)>());

    /// <summary>
    /// Walks a <see cref="PreceptExpression"/> AST and extracts referenced identifiers.
    /// Bare names → field references; dotted names (Name.Member) → arg references.
    /// </summary>
    public static ExpressionSubjects Extract(PreceptExpression expression)
    {
        var fields = new List<string>();
        var args = new List<(string, string)>();
        Walk(expression, fields, args);
        return new ExpressionSubjects(fields, args);
    }

    /// <summary>
    /// Walks an expression AST intended for an event-assert scope where bare names
    /// are arg references, not field references.
    /// </summary>
    public static ExpressionSubjects ExtractForEventAssert(PreceptExpression expression, string eventName)
    {
        var bareNames = new List<string>();
        var args = new List<(string, string)>();
        Walk(expression, bareNames, args);

        // In event-assert scope, bare identifiers are arg references, not fields
        foreach (var name in bareNames)
            args.Add((eventName, name));

        return new ExpressionSubjects(System.Array.Empty<string>(), args);
    }

    private static void Walk(PreceptExpression expr, List<string> fields, List<(string, string)> args)
    {
        switch (expr)
        {
            case PreceptIdentifierExpression id:
                if (id.Member is not null)
                    args.Add((id.Name, id.Member));
                else
                    fields.Add(id.Name);
                break;
            case PreceptBinaryExpression bin:
                Walk(bin.Left, fields, args);
                Walk(bin.Right, fields, args);
                break;
            case PreceptUnaryExpression un:
                Walk(un.Operand, fields, args);
                break;
            case PreceptParenthesizedExpression paren:
                Walk(paren.Inner, fields, args);
                break;
            case PreceptLiteralExpression:
                break;
        }
    }
}
