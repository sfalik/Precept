using System.Linq;
using Precept;
using Precept.MatrixTools;
using Precept.Pipeline;

namespace Precept.MatrixTools.Tests;

/// <summary>Shared fixture helpers: compile sources through the real pipeline and pick rows/rules out.</summary>
internal static class Px
{
    public const string Abc = """
        field A as decimal default 0.0
        field B as decimal default 0.0
        field C as decimal default 0.0
        """;

    public static Compilation Compile(string source) => Compiler.Compile(source);

    /// <summary>Compiles decls plus one rule statement and returns the rule's typed condition.</summary>
    public static TypedExpression Rule(string decls, string expr) =>
        Compile(decls + "\nrule " + expr + " because \"t\"\n").Semantics.Rules.Last().Condition;

    /// <summary>Rule condition over the A/B/C decimal fixture.</summary>
    public static TypedExpression Expr(string boolExpr) => Rule(Abc, boolExpr);

    /// <summary>Normal-form equality of two boolean expressions over the A/B/C fixture.</summary>
    public static bool Eq(string left, string right) =>
        WpCalculator.AreNormalFormEqual(Expr(left), Expr(right));

    public static TypedEventRowSuccess EventRow(Compilation c, string eventName) =>
        c.Semantics.EventHandlers.OfType<TypedEventRowSuccess>().First(r => r.EventName == eventName);

    public static TypedTransitionRowSuccess TransitionRow(Compilation c, string eventName) =>
        c.Semantics.TransitionRows.OfType<TypedTransitionRowSuccess>()
            .First(r => r.EventName == eventName && !r.Actions.IsEmpty);

    public static ObligationSpec Obligation(Compilation c, string label) =>
        ObligationEnumerator.Enumerate(c.Semantics).Single(o => o.Label == label);

    public static WpComputed Wp(this WpResult result) =>
        result is WpComputed computed
            ? computed
            : throw new Xunit.Sdk.XunitException($"expected WpComputed, got {result}");
}
