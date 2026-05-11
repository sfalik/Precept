using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Regression tests ensuring expression-level diagnostics emit non-zero Column values
/// (token-precise squigglies) and that every new compile-phase diagnostic is classified
/// as either expression-level or statement-level.
/// </summary>
public class DiagnosticSpanPrecisionTests
{
    // Shorthand preambles matching CatalogDriftTests conventions.
    private const string H = "precept Test\n";
    private const string S = "state A initial\n";
    private const string S2 = "state A initial\nstate B\n";

    // ════════════════════════════════════════════════════════════════
    // Classification sets — every compile-phase diagnostic must be in
    // exactly one of these two sets. Adding a new compile-phase
    // diagnostic without classifying it here will fail the
    // AllCompilePhaseDiagnostics_MustBeClassified test.
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Diagnostics about specific expressions within a line.
    /// These MUST produce Column > 0 (unless the expression starts at column 0,
    /// which never happens in practice because precept lines have leading keywords).
    /// </summary>
    private static readonly HashSet<string> ExpressionLevelDiagnostics = new()
    {
        "C32", // literal set assignment violates rule
        "C38", // unknown identifier
        "C39", // type mismatch (assignment)
        "C40", // unary operator type error
        "C41", // binary operator type error
        "C42", // nullable assigned to non-nullable
        "C43", // collection pop/dequeue type mismatch
        "C46", // non-boolean in rule position
        "C56", // nullable .length without guard
        "C60", // narrowing to integer
        "C65", // ordinal comparison on unordered choice
        "C67", // ordinal comparison between two choice fields
        "C68", // literal not in choice set
        "C69", // cross-scope guard reference
        "C71", // unknown function
        "C72", // function argument count mismatch
        "C73", // function argument type mismatch
        "C74", // round() precision not integer literal
        "C75", // pow() exponent not integer
        "C76", // sqrt() requires non-negative proof
        "C77", // nullable argument to function
        "C78", // conditional condition not boolean
        "C79", // conditional branch type mismatch
        "C92", // division by literal zero
        "C93", // divisor not provably nonzero
        "C94", // assignment outside field constraint interval
        "C97", // dead guard
        "C98", // tautological guard
    };

    /// <summary>
    /// Diagnostics about whole declarations, structural issues, or conditions
    /// where full-line highlighting (Column = 0) is correct.
    /// </summary>
    private static readonly HashSet<string> StatementLevelDiagnostics = new()
    {
        "C26", // model null (internal — not emitted as diagnostic)
        "C27", // no initial state
        "C28", // initial state not declared
        "C29", // rules violated by defaults
        "C30", // state ensure violated on initial
        "C31", // event ensure violated at defaults
        "C44", // duplicate state ensure
        "C45", // subsumed state ensure
        "C47", // duplicate guard
        "C48", // unreachable state (warning)
        "C49", // orphaned event (warning)
        "C50", // dead-end state (warning)
        "C51", // reject-only pair (warning)
        "C52", // event never succeeds (warning)
        "C53", // empty precept (hint)
        "C55", // root-level edit with states
        "C57", // constraint incompatible with type
        "C58", // duplicate/contradictory constraints
        "C59", // default violates constraint
        "C61", // maxplaces on non-decimal
        "C62", // empty choice set
        "C63", // duplicate choice value
        "C64", // default not in choice set
        "C66", // ordered on non-choice
        "C83", // computed field uses nullable field
        "C84", // computed field uses event argument
        "C85", // computed field uses unsafe accessor
        "C86", // circular dependency in computed fields
        "C87", // computed field in edit declaration
        "C88", // computed field assigned via set
        "C95", // contradictory rule
        "C96", // vacuous rule
    };

    // ════════════════════════════════════════════════════════════════
    // Future-proofing: every compile-phase diagnostic must be classified
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void AllCompilePhaseDiagnostics_MustBeClassified()
    {
        var compilePhaseIds = DiagnosticCatalog.Constraints
            .Where(c => c.Phase == "compile")
            .Select(c => c.Id)
            .ToList();

        compilePhaseIds.Should().NotBeEmpty("diagnostic catalog should have compile-phase constraints");

        var classified = ExpressionLevelDiagnostics.Union(StatementLevelDiagnostics).ToHashSet();

        var unclassified = compilePhaseIds.Where(id => !classified.Contains(id)).ToList();

        unclassified.Should().BeEmpty(
            "every compile-phase diagnostic must be classified as expression-level or statement-level " +
            "in DiagnosticSpanPrecisionTests. Unclassified: {0}. " +
            "Add each new diagnostic ID to ExpressionLevelDiagnostics (if it targets a specific expression) " +
            "or StatementLevelDiagnostics (if it highlights the whole line).",
            string.Join(", ", unclassified));
    }

    [Fact]
    public void ClassificationSets_DoNotOverlap()
    {
        var overlap = ExpressionLevelDiagnostics.Intersect(StatementLevelDiagnostics).ToList();
        overlap.Should().BeEmpty(
            "a diagnostic cannot be both expression-level and statement-level. Overlapping: {0}",
            string.Join(", ", overlap));
    }

    /// <summary>
    /// Ensures every expression-level diagnostic has an actual span test — either
    /// an [InlineData] on the Theory or a dedicated [Fact]. Classifying without testing
    /// is not enough; this closes the gap.
    /// </summary>
    [Fact]
    public void AllExpressionLevelDiagnostics_HaveSpanTest()
    {
        // IDs covered by the parameterized Theory via [InlineData]
        var method = typeof(DiagnosticSpanPrecisionTests)
            .GetMethod(nameof(ExpressionLevelDiagnostic_HasNonZeroSpan))!;

        var theoryTestedIds = method
            .GetCustomAttributes<InlineDataAttribute>()
            .SelectMany(attr => attr.GetData(method))
            .Select(row => (string)row[0])
            .ToHashSet();

        // IDs covered by dedicated [Fact] tests (update this set when adding new special-case tests)
        var specialCaseTestedIds = new HashSet<string> { "C71", "C43" };

        var allTestedIds = theoryTestedIds.Union(specialCaseTestedIds).ToHashSet();

        var untested = ExpressionLevelDiagnostics.Except(allTestedIds).ToList();

        untested.Should().BeEmpty(
            "every expression-level diagnostic must have a span test. " +
            "Add an [InlineData] to ExpressionLevelDiagnostic_HasNonZeroSpan or a dedicated [Fact]. " +
            "Untested: {0}",
            string.Join(", ", untested));
    }

    // ════════════════════════════════════════════════════════════════
    // Expression-level diagnostics must have a real span
    // ════════════════════════════════════════════════════════════════
    //
    // Each InlineData triggers exactly one target diagnostic via a
    // minimal precept snippet compiled through CompileFromText.
    //
    // Note: Some diagnostics (C71) can only be triggered via direct
    // model construction (the parser rejects invalid function names).
    // These are tested separately below.
    // ════════════════════════════════════════════════════════════════

    [Theory]
    // C32: literal set assignment violates rule — "0" violates positive constraint
    [InlineData("C32", "PRECEPT032",
        "precept Test\nfield Rate as number default 5 positive\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Rate = 0 -> transition B\n")]
    // C38: unknown identifier
    [InlineData("C38", "PRECEPT038",
        "precept Test\nfield Total as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Total = Missing -> transition B\n")]
    // C39: type mismatch — assigning string to number
    [InlineData("C39", "PRECEPT039",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = \"text\" -> transition B\n")]
    // C40: unary operator type error — negating a string
    [InlineData("C40", "PRECEPT040",
        "precept Test\nfield Name as string default \"x\"\nfield X as number default 0\nstate A initial\nevent Go\nfrom A on Go -> set X = -Name -> no transition\n")]
    // C41: binary operator type error — adding string and number
    [InlineData("C41", "PRECEPT041",
        "precept Test\nfield Name as string default \"x\"\nfield X as number default 0\nstate A initial\nevent Go\nfrom A on Go when Name + X > 0 -> no transition\n")]
    // C42: nullable assigned to non-nullable
    [InlineData("C42", "PRECEPT042",
        "precept Test\nfield Value as number default 0\nfield RetryCount as number nullable\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Value = RetryCount -> transition B\n")]
    // C46: non-boolean in rule position — number in when guard
    [InlineData("C46", "PRECEPT046",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when X -> transition B\nfrom A on Go -> reject \"blocked\"\n")]
    // C56: nullable .length without null guard
    [InlineData("C56", "PRECEPT056",
        "precept Test\nfield Note as string nullable\nstate A initial\nevent Go\nfrom A on Go when Note.length > 0 -> no transition\n")]
    // C60: narrowing to integer — assigning 3.0 to integer field
    [InlineData("C60", "PRECEPT060",
        "precept Test\nfield Count as integer default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Count = 3.0 -> no transition\n")]
    // C65: ordinal comparison on unordered choice
    [InlineData("C65", "PRECEPT065",
        "precept Test\nfield Status as choice(\"Draft\",\"Active\") default \"Draft\"\nstate A initial\nstate B\nevent Go\nfrom A on Go when Status > \"Active\" -> no transition\n")]
    // C67: ordinal comparison between two choice fields
    [InlineData("C67", "PRECEPT067",
        "precept Test\nfield Priority as choice(\"Low\",\"High\") default \"Low\" ordered\nfield Severity as choice(\"Low\",\"High\") default \"Low\" ordered\nstate A initial\nstate B\nevent Go\nfrom A on Go when Priority > Severity -> no transition\n")]
    // C68: literal not in choice set
    [InlineData("C68", "PRECEPT068",
        "precept Test\nfield Status as choice(\"Open\",\"Closed\") default \"Open\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Status = \"Invalid\" -> no transition\n")]
    // C69: cross-scope guard reference — rule guard referencing event arg
    [InlineData("C69", "PRECEPT069",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go with Amount as number\nrule X >= 0 when Go.Amount > 0 because \"bad\"\nfrom A on Go -> no transition\n")]
    // C72: wrong number of arguments — abs(X, X)
    [InlineData("C72", "PRECEPT072",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(X, X) > 0 -> no transition\n")]
    // C73: argument type mismatch — abs(string)
    [InlineData("C73", "PRECEPT073",
        "precept Test\nfield Name as string default \"test\"\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(Name) > 0 -> no transition\n")]
    // C74: round precision must be non-negative integer literal
    [InlineData("C74", "PRECEPT074",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when round(X, X) > 0 -> no transition\n")]
    // C75: pow exponent must be integer type
    [InlineData("C75", "PRECEPT075",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when pow(X, X) > 0 -> no transition\n")]
    // C76: sqrt requires non-negative proof
    [InlineData("C76", "PRECEPT076",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when sqrt(X) > 0 -> no transition\n")]
    // C77: nullable argument to function
    [InlineData("C77", "PRECEPT077",
        "precept Test\nfield X as number nullable default null\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(X) > 0 -> no transition\n")]
    // C78: conditional condition must be boolean — using 42
    [InlineData("C78", "PRECEPT078",
        "precept Test\nfield X as string default \"\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = if 42 then \"a\" else \"b\" -> no transition\n")]
    // C79: conditional branch type mismatch
    [InlineData("C79", "PRECEPT079",
        "precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = if true then 42 else \"text\" -> no transition\n")]
    // C92: division by literal zero
    [InlineData("C92", "PRECEPT092",
        "precept Test\nfield Y as number default 10\nstate A initial\nevent Go\nfrom A on Go -> set Y = Y / 0 -> no transition\n")]
    // C93: divisor not provably nonzero
    [InlineData("C93", "PRECEPT093",
        "precept Test\nfield Y as number default 10\nfield D as number default 1\nstate A initial\nevent Go\nfrom A on Go -> set Y = Y / D -> no transition\n")]
    // C94: assignment outside field constraint interval
    [InlineData("C94", "PRECEPT094",
        "precept Test\nfield Score as number default 50 max 100\nstate A initial\nevent Go\nfrom A on Go -> set Score = 150 -> no transition\n")]
    // C97: dead guard
    [InlineData("C97", "PRECEPT097",
        "precept Test\nfield X as number default 15 min 10\nstate A initial\nevent Go\nfrom A on Go when X < 0 -> no transition\nfrom A on Go -> no transition\n")]
    // C98: tautological guard
    [InlineData("C98", "PRECEPT098",
        "precept Test\nfield X as number default 15 min 10\nstate A initial\nevent Go\nfrom A on Go when X >= 0 -> no transition\n")]
    public void ExpressionLevelDiagnostic_HasNonZeroSpan(string constraintId, string expectedCode, string dsl)
    {
        var result = PreceptCompiler.CompileFromText(dsl);

        var diagnostic = result.Diagnostics.FirstOrDefault(d => d.Code == expectedCode);
        diagnostic.Should().NotBeNull(
            "snippet for {0} should trigger diagnostic {1}. Got: [{2}]",
            constraintId, expectedCode,
            string.Join(", ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));

        diagnostic!.Column.Should().BeGreaterThan(0,
            "expression-level diagnostic {0} ({1}) should have Column > 0 for token-precise highlighting. " +
            "Message: {2}",
            constraintId, expectedCode, diagnostic.Message);

        diagnostic.EndColumn.Should().BeGreaterThan(diagnostic.Column,
            "expression-level diagnostic {0} ({1}) should emit EndColumn > Column for a real highlighted span, not a collapsed point. " +
            "Message: {2}",
            constraintId, expectedCode, diagnostic.Message);
    }

    [Fact]
    public void C92_FlowedZeroIdentifierDivisor_UsesFullIdentifierSpan()
    {
        const string dsl = "precept Test\nfield Amount as number default 100\nfield Rate as number default 5 positive\nfield Result as number default 0\nstate Active initial\nevent Break\nfrom Active on Break\n    -> set Rate = 0\n    -> set Result = Amount / Rate\n    -> no transition\n";

        var result = PreceptCompiler.CompileFromText(dsl);
        var diagnostic = result.Diagnostics.First(d => d.Code == "PRECEPT092");
        var lineText = dsl.Split('\n')[8];
        var expectedStart = lineText.LastIndexOf("Rate", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Rate".Length;

        diagnostic.Line.Should().Be(9,
            "PRECEPT092 should point at the divisor expression line after the prior set mutates Rate to zero");
        diagnostic.Column.Should().Be(expectedStart,
            "PRECEPT092 should start at the multi-character identifier divisor, not the whole expression");
        diagnostic.EndColumn.Should().Be(expectedEnd,
            "PRECEPT092 should cover the full identifier divisor span for precise squiggles");
    }

    [Fact]
    public void C93_IdentifierDivisor_UsesFullIdentifierSpan()
    {
        const string dsl = "precept Test\nfield Amount as number default 100\nfield Rate as number default 1\nfield Result as number default 0\nstate Active initial\nevent Break\nfrom Active on Break\n    -> set Result = Amount / Rate\n    -> no transition\n";

        var result = PreceptCompiler.CompileFromText(dsl);
        var diagnostic = result.Diagnostics.First(d => d.Code == "PRECEPT093");
        var lineText = dsl.Split('\n')[7];
        var expectedStart = lineText.LastIndexOf("Rate", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Rate".Length;

        diagnostic.Line.Should().Be(8,
            "PRECEPT093 should be reported on the divisor expression line");
        diagnostic.Column.Should().Be(expectedStart,
            "PRECEPT093 should start at the identifier divisor, not the start of the set expression");
        diagnostic.EndColumn.Should().Be(expectedEnd,
            "PRECEPT093 should cover the full identifier divisor span for precise squiggles");
    }

    [Fact]
    public void C94_AssignmentConstraintViolation_UsesFullExpressionSpan()
    {
        const string dsl = "precept Test\nfield Score as number default 50 min 0 max 100\nfield Boost as number default 200 min 200 max 500\nstate Review initial\nevent AddBoost\nfrom Review on AddBoost -> set Score = Score + Boost -> no transition\n";

        var result = PreceptCompiler.CompileFromText(dsl);
        var diagnostic = result.Diagnostics.First(d => d.Code == "PRECEPT094");
        var lineText = dsl.Split('\n')[5];
        var expectedStart = lineText.IndexOf("Score + Boost", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Score + Boost".Length;

        diagnostic.Line.Should().Be(6,
            "PRECEPT094 should be reported on the assignment expression line");
        diagnostic.Column.Should().Be(expectedStart,
            "PRECEPT094 should start at the first token of the violating RHS expression");
        diagnostic.EndColumn.Should().Be(expectedEnd,
            "PRECEPT094 should cover the full RHS expression span for precise squiggles");
    }

    [Fact]
    public void C76_SqrtArgument_UsesFullArgumentSpan()
    {
        const string dsl = "precept Test\nfield Measurement as number default 0\nfield Root as number default 0\nstate Active initial\nevent Compute\nfrom Active on Compute -> set Root = sqrt(Measurement) -> no transition\n";

        var result = PreceptCompiler.CompileFromText(dsl);
        var diagnostic = result.Diagnostics.First(d => d.Code == "PRECEPT076");
        var lineText = dsl.Split('\n')[5];
        var expectedStart = lineText.IndexOf("Measurement", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Measurement".Length;

        diagnostic.Line.Should().Be(6,
            "PRECEPT076 should be reported on the sqrt argument line");
        diagnostic.Column.Should().Be(expectedStart,
            "PRECEPT076 should start at the argument expression, not at the start of sqrt()");
        diagnostic.EndColumn.Should().Be(expectedEnd,
            "PRECEPT076 should cover the full argument span for precise squiggles");
    }

    [Fact]
    public void C98_TautologicalGuard_UsesGuardExpressionSpan()
    {
        const string dsl = "precept Test\nfield X as number default 15 min 10\nstate A initial\nevent Go\nfrom A on Go\n    when X >= 0\n    -> no transition\n";

        var result = PreceptCompiler.CompileFromText(dsl);
        var diagnostic = result.Diagnostics.First(d => d.Code == "PRECEPT098");
        var lineText = dsl.Split('\n')[5];
        var expectedStart = lineText.IndexOf("X >= 0", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "X >= 0".Length;

        diagnostic.Line.Should().Be(6,
            "C98 should be reported on the multiline when-clause line, not the from-row line");
        diagnostic.Column.Should().Be(expectedStart,
            "C98 should start at the guard expression, not the start of the transition row");
        diagnostic.EndColumn.Should().Be(expectedEnd,
            "C98 should cover the full guard expression span for precise squiggles");
    }

    // ════════════════════════════════════════════════════════════════
    // Diagnostics that require direct model construction
    // (parser rejects the invalid syntax before the type checker runs)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void C71_UnknownFunction_HasNonZeroSpan_WhenPositionProvided()
    {
        // C71 can only be triggered via model construction since the parser
        // rejects unknown function names. When Position is set on the
        // expression, the type checker should use it.
        var model = new PreceptDefinition(
            "Test",
            [new PreceptState("A", SourceLine: 2), new PreceptState("B", SourceLine: 3)],
            new PreceptState("A", SourceLine: 2),
            [new PreceptEvent("Go", [])],
            [new PreceptField("X", PreceptScalarType.Number, false, true, 0L)],
            [],
            TransitionRows: [new PreceptTransitionRow(
                "A", "Go",
                new NoTransition(),
                [new PreceptSetAssignment("X",
                    "unknownfn(X)",
                    new PreceptFunctionCallExpression("unknownfn",
                        [new PreceptIdentifierExpression("X")])
                        { Position = new SourceSpan(20, 33) },
                    SourceLine: 5)],
                SourceLine: 5)]);

        var result = PreceptCompiler.Validate(model);
        var diag = result.Diagnostics.FirstOrDefault(d => d.Constraint.Id == "C71");
        diag.Should().NotBeNull("C71 should be triggered for unknown function 'unknownfn'");
        diag!.Column.Should().BeGreaterThan(0,
            "C71 should use the function expression's Position for column precision");
        diag.EndColumn.Should().BeGreaterThan(diag.Column,
            "C71 should preserve the full function-call span when Position is available");
    }

    // ════════════════════════════════════════════════════════════════
    // C43 requires a type mismatch on dequeue/pop into — needs a
    // collection with a different inner type than the target field.
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void C43_CollectionTypeMismatch_UsesIntoFieldSpan()
    {
        // dequeue a number collection into a string field → C43
        const string dsl = """
            precept Test
            field Target as string default ""
            field Numbers as queue of number
            state A initial
            state B
            event Go
            from A on Go -> dequeue Numbers into Target -> transition B
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        var diagnostic = result.Diagnostics.FirstOrDefault(d => d.Code == "PRECEPT043");
        diagnostic.Should().NotBeNull("C43 should fire for type mismatch on dequeue into");
        var lineText = dsl.Split('\n')[6];
        var expectedStart = lineText.LastIndexOf("Target", StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Target".Length;

        diagnostic!.Column.Should().Be(expectedStart,
            "C43 should point at the into-target field identifier, which is the author-facing source of the assignment mismatch");
        diagnostic.EndColumn.Should().Be(expectedEnd,
            "C43 should cover the full into-target identifier span for precise editor squiggles");
    }
}
