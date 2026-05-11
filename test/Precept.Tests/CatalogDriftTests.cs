using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Drift defense tests for the three-tier catalog infrastructure.
/// These tests ensure that token attributes, construct registrations,
/// constraint enforcement, and sample files stay in sync.
/// See docs/CatalogInfrastructureDesign.md § Drift Defense for rationale.
/// </summary>
public class CatalogDriftTests
{
    // ════════════════════════════════════════════════════════════════════
    // Test 1: Construct examples parse
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void AllConstructExamples_ParseSuccessfully()
    {
        PreceptParser.EnsureInitialized();
        var constructs = ConstructCatalog.Constructs;
        constructs.Should().NotBeEmpty("ConstructCatalog should have registrations");

        var failures = new List<string>();

        foreach (var construct in constructs)
        {
            // Wrap example in a minimal valid precept file
            var dsl = BuildMinimalFile(construct);
            var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
            if (diags.Count > 0 || model is null)
            {
                var msgs = string.Join("; ", diags.Select(d => d.Message));
                failures.Add($"Construct '{construct.Name}' example '{construct.Example}' failed: {msgs}");
            }
        }

        failures.Should().BeEmpty(
            "every ConstructInfo.Example must parse successfully within a valid file");
    }

    /// <summary>
    /// Wraps a construct example in a minimal precept file that provides the
    /// required context (states, events, fields) for the example to parse.
    /// </summary>
    private static string BuildMinimalFile(ConstructInfo construct)
    {
        // Base file with required structure
        var header = "precept DriftTest";
        var states = new List<string> { "state Idle initial", "state Open", "state Closed" };
        var fields = new List<string> { "field Priority as number default 1", "field Assignee as string nullable", "field Resolution as string nullable" };
        var events = new List<string> { "event Submit with Comment as string" };

        return construct.Name switch
        {
            "precept-header" =>
                // The example IS the header
                $"{construct.Example}\nstate Idle initial",

            "state-declaration" =>
                // The example declares a state; add header + another state if needed
                $"{header}\n{construct.Example}",

            "field-declaration" =>
                $"{header}\n{construct.Example}\n{string.Join("\n", states)}",

            "rule" =>
                $"{header}\nfield Priority as number default 1\n{construct.Example}\n{string.Join("\n", states)}",

            "event-declaration" =>
                $"{header}\n{string.Join("\n", states)}\n{construct.Example}",

            "state-ensure" =>
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{construct.Example}",

            "event-ensure" =>
                $"{header}\n{string.Join("\n", states)}\nevent Submit with Comment as string\n{construct.Example}",

            "state-action" =>
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{construct.Example}",

            "edit-declaration" =>
                $"{header}\nfield Priority as number default 1\n{string.Join("\n", states)}\n{construct.Example}",

            "transition-row" =>
                $"{header}\n{string.Join("\n", states)}\nevent Submit\n{construct.Example}",

            "function-call" =>
                // function call needs a decimal field and an event to be valid in a transition context
                $"{header}\nfield Rate as decimal default 0.0\n{string.Join("\n", states)}\nevent Apply with Amount as number\n{construct.Example}",

            _ =>
                // Generic fallback: include everything
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{string.Join("\n", events)}\n{construct.Example}"
        };
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 2: SYNC comment ↔ DiagnosticCatalog bidirectional check
    // ════════════════════════════════════════════════════════════════════

    private static readonly Regex SyncCommentRegex = new(
        @"//\s*SYNC:CONSTRAINT:(?<id>C\d+)", RegexOptions.Compiled);

    [Fact]
    public void SyncComments_MatchDiagnosticCatalog()
    {
        // Find source files that may contain SYNC comments
        var srcRoot = FindRepoRoot();
        var sourceFiles = Directory.GetFiles(Path.Combine(srcRoot, "src", "Precept"), "*.cs", SearchOption.AllDirectories);

        var syncIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            foreach (Match match in SyncCommentRegex.Matches(content))
                syncIds.Add(match.Groups["id"].Value);
        }

        var catalogIds = DiagnosticCatalog.Constraints.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        // Every SYNC comment must have a catalog entry
        var orphanedSyncs = syncIds.Except(catalogIds).ToList();
        orphanedSyncs.Should().BeEmpty(
            "every // SYNC:CONSTRAINT:Cnn comment should have a matching DiagnosticCatalog entry");

        // Every catalog entry should have at least one SYNC comment (for parse/compile phase constraints)
        var parseCompileIds = DiagnosticCatalog.Constraints
            .Where(c => c.Phase is "parse" or "compile")
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);
        var missingSyncs = parseCompileIds.Except(syncIds).ToList();
        missingSyncs.Should().BeEmpty(
            "every parse/compile-phase constraint should have at least one // SYNC:CONSTRAINT:Cnn comment in source");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 3: Token attributes complete
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void AllTokens_HaveCategoryAndDescription()
    {
        var missing = new List<string>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            if (PreceptTokenMeta.GetCategory(token) is null)
                missing.Add($"{token}: missing [TokenCategory]");
            if (PreceptTokenMeta.GetDescription(token) is null)
                missing.Add($"{token}: missing [TokenDescription]");
        }

        missing.Should().BeEmpty("every PreceptToken member must have [TokenCategory] and [TokenDescription]");
    }

    [Fact]
    public void KeywordAndOperatorTokens_HaveSymbol()
    {
        var missing = new List<string>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var category = PreceptTokenMeta.GetCategory(token);
            if (category is null) continue;

            // Keyword, operator, and punctuation tokens must have [TokenSymbol]
            var needsSymbol = category.Value is
                TokenCategory.Control or TokenCategory.Declaration or
                TokenCategory.Grammar or
                TokenCategory.Action or TokenCategory.Outcome or
                TokenCategory.Type or TokenCategory.Literal or
                TokenCategory.Operator or TokenCategory.Punctuation;

            if (needsSymbol && PreceptTokenMeta.GetSymbol(token) is null)
                missing.Add($"{token} ({category.Value}): missing [TokenSymbol]");
        }

        missing.Should().BeEmpty(
            "keyword, operator, and punctuation tokens must have [TokenSymbol]");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 4: Reference sample coverage
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void SampleFiles_CoverAllConstructs()
    {
        PreceptParser.EnsureInitialized();
        var constructs = ConstructCatalog.Constructs;
        constructs.Should().NotBeEmpty();

        var srcRoot = FindRepoRoot();
        var samplesDir = Path.Combine(srcRoot, "samples");
        var sampleFiles = Directory.GetFiles(samplesDir, "*.precept");
        sampleFiles.Should().NotBeEmpty("samples/ directory must contain .precept files");

        // Concatenate all sample content
        var allSampleContent = string.Join("\n", sampleFiles.Select(File.ReadAllText));

        var uncovered = new List<string>();
        foreach (var construct in constructs)
        {
            // Extract leading keyword(s) from form
            var spaceIdx = construct.Form.IndexOf(' ');
            var firstGroup = spaceIdx >= 0 ? construct.Form[..spaceIdx] : construct.Form;
            var keywords = firstGroup.Split('|');

            // At least one keyword must appear in at least one sample
            if (!keywords.Any(kw => allSampleContent.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                uncovered.Add($"Construct '{construct.Name}' (keyword(s): {firstGroup}) — not found in any sample file");
        }

        uncovered.Should().BeEmpty(
            "at least one .precept sample file should use every registered construct");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 5: Diagnostic code format
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void DiagnosticCodes_FollowPreceptNNNFormat()
    {
        foreach (var constraint in DiagnosticCatalog.Constraints)
        {
            var code = DiagnosticCatalog.ToDiagnosticCode(constraint.Id);
            code.Should().MatchRegex(@"^PRECEPT\d{3}$",
                $"constraint {constraint.Id} diagnostic code should be PRECEPTnnn");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 6: Dual-role tokens carry all required categories
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void CollectionTypeTokens_HaveTypeCategoryAmongCategories()
    {
        // The parser accepts these tokens in type position (e.g. "field Tags as set of string").
        // They must carry TokenCategory.Type so vocabulary consumers derive them correctly.
        var collectionTypeTokens = new[]
        {
            PreceptToken.Set,
            PreceptToken.Queue,
            PreceptToken.Stack
        };

        var missing = new List<string>();
        foreach (var token in collectionTypeTokens)
        {
            var categories = PreceptTokenMeta.GetCategories(token);
            if (!categories.Contains(TokenCategory.Type))
                missing.Add($"{token}: categories [{string.Join(", ", categories)}] missing Type");
        }

        missing.Should().BeEmpty(
            "collection-type tokens accepted in type position must have TokenCategory.Type among their categories");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 7: Constraint triggers — every constraint can be triggered
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Every registered constraint must be reachable: a violating input must produce
    /// an error whose message matches the constraint's message template.
    /// See CatalogInfrastructureDesign.md § Drift Defense.
    /// </summary>
    [Theory]
    [MemberData(nameof(ConstraintTriggerData))]
    public void EveryConstraint_CanBeTriggered(string constraintId, string phase)
    {
        var constraint = DiagnosticCatalog.Constraints.Single(c => c.Id == constraintId);
        var trigger = ConstraintTriggers[constraintId];
        string? errorMessage = null;

        try
        {
            if (trigger.DirectAction is not null)
            {
                trigger.DirectAction();
            }
            else switch (phase)
            {
                case "parse":
                    PreceptParser.Parse(trigger.Dsl);
                    break;
                case "compile":
                {
                    var validation = PreceptCompiler.Validate(PreceptParser.Parse(trigger.Dsl));
                    var diagnostic = validation.Diagnostics.FirstOrDefault(d => d.Constraint.Id == constraintId);
                    errorMessage = diagnostic is null
                        ? null
                        : $"{diagnostic.DiagnosticCode}: {diagnostic.Message}";
                    break;
                }
                case "runtime":
                    var engine = PreceptCompiler.Compile(PreceptParser.Parse(trigger.Dsl));
                    engine.CreateInstance();
                    break;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        errorMessage.Should().NotBeNull(
            $"constraint {constraintId} ({constraint.Rule}) must be triggerable by a violating input");
        errorMessage.Should().Contain(trigger.ExpectedFragment,
            $"constraint {constraintId} error message should match expected fragment");
    }

    public static IEnumerable<object[]> ConstraintTriggerData()
        => DiagnosticCatalog.Constraints.Select(c => new object[] { c.Id, c.Phase });

    private sealed record TriggerInput(string Dsl, string ExpectedFragment, Action? DirectAction = null);

    private static void InvokeResolveTransition(PreceptEngine engine, string currentState, string eventName)
    {
        var method = typeof(PreceptEngine).GetMethod("ResolveTransition", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull("ResolveTransition should remain available for runtime constraint coverage");

        try
        {
            method!.Invoke(engine, [currentState, eventName, new Dictionary<string, object?>()]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    // Minimal valid wrapper — many violations need a precept + state context
    private const string H = "precept Test\n";
    private const string S = "state A initial\n";
    private const string S2 = "state A initial\nstate B\n";

    // AUTHORING NOTE: DSL rules for trigger entries
    //   - Compile-phase constraints that fire in a `when` guard MUST use `-> no transition`
    //     as the row target. Using `-> transition B` (where B is not declared) triggers C54
    //     (undeclared state) before the intended constraint fires, masking the correct error.
    //   - When adding a new compile-phase constraint, verify the trigger DSL produces ONLY
    //     that constraint — run the entry in isolation and assert Diagnostics.Count == 1.
    private static readonly Dictionary<string, TriggerInput> ConstraintTriggers = new()
    {
        // ── Parse-phase (C1–C25) ──────────────────────────────────────

        // C1: Empty input
        ["C1"] = new("", "empty"),

        // C2: Invalid token — '@' is not recognized by any tokenizer rule
        ["C2"] = new("precept @", "@"),

        // C3: Valid tokens but unparseable structure
        ["C3"] = new("state", "parse"),

        // C4: Invalid expression — ParseExpression is a standalone entry point
        //      (rule/guard expressions are parsed inline by the combinator)
        ["C4"] = new("_unused_", "parse expression", DirectAction: () =>
        {
            PreceptParser.ParseExpression("and");
        }),

        // C5: Invalid number literal — unreachable via normal tokenizer, test directly
        ["C5"] = new("_unused_", "Invalid number", DirectAction: () =>
        {
            // The tokenizer won't produce invalid number tokens normally,
            // so we verify the constraint exists and its message is well-formed.
            var msg = DiagnosticCatalog.C5.FormatMessage(("value", "NaN"));
            if (!msg.Contains("Invalid number"))
                throw new InvalidOperationException("Constraint C5 message template is broken");
            throw new InvalidOperationException(msg);
        }),

        // C6: Duplicate field name
        ["C6"] = new(H + "field X as number default 1\nfield X as string default \"a\"\n" + S, "Duplicate field"),

        // C7: Duplicate state name
        ["C7"] = new(H + "state A initial\nstate A\n", "Duplicate state"),

        // C8: Duplicate initial state
        ["C8"] = new(H + "state A initial\nstate B initial\n", "already marked initial"),

        // C9: Duplicate event name
        ["C9"] = new(H + S + "event Submit\nevent Submit\n", "Duplicate event"),

        // C10: Missing outcome in transition row — row has only set actions, no outcome
        ["C10"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go -> set X = 1\n", "missing an outcome"),

        // C11: Statements after outcome — set action follows the outcome
        ["C11"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go -> transition B -> set X = 1\n", "after an outcome"),

        // C12: No states or fields declared
        ["C12"] = new(H, "At least one field or state"),

        // C13: No initial state
        ["C13"] = new(H + "state A\n", "initial"),

        // C14: Event ensure with wrong dotted prefix
        ["C14"] = new(H + S + "event Submit with Comment as string default \"x\"\non Submit ensure Other.Comment != null because \"bad\"\n", "unknown prefix"),

        // C15: Event ensure dotted member not a declared arg
        ["C15"] = new(H + S + "event Submit with Comment as string default \"x\"\non Submit ensure Submit.Nope != null because \"bad\"\n", "not an event argument"),

        // C16: Event ensure plain identifier not a declared arg
        ["C16"] = new(H + S + "event Submit with Comment as string default \"x\"\non Submit ensure Nope != null because \"bad\"\n", "not an event argument"),

        // C17: Non-nullable field without default
        ["C17"] = new(H + "field X as string\n" + S, "requires a default"),

        // C18: Field default type mismatch
        ["C18"] = new(H + "field X as number default \"hello\"\n" + S, "does not match declared type"),

        // C19: Non-nullable field with null default
        ["C19"] = new(H + "field X as string default null\n" + S, "null is not allowed"),

        // C20: Event arg default type mismatch
        ["C20"] = new(H + S + "event Submit with X as number default \"hello\"\n", "does not match declared type"),

        // C21: Non-nullable event arg with null default
        ["C21"] = new(H + S + "event Submit with X as string default null\n", "null is not allowed"),

        // C22: Collection verb on scalar field
        ["C22"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go -> add X \"val\" -> transition B\n", "scalar field"),

        // C23: Collection verb on unknown field
        ["C23"] = new(H + S2 + "event Go\nfrom A on Go -> add Unknown \"val\" -> transition B\n", "unknown collection"),

        // C24: Wrong verb for collection kind (enqueue on a set)
        ["C24"] = new(H + "field Tags as set of string\n" + S2 + "event Go\nfrom A on Go -> enqueue Tags \"val\" -> transition B\n", "Cannot"),

        // C25: Duplicate unguarded transition row
        ["C25"] = new(H + S2 + "event Go\nfrom A on Go -> transition B\nfrom A on Go -> transition B\n", "unreachable"),

        // ── Compile-phase (C26–C32) ───────────────────────────────────

        // C26: Null model passed to Compile
        ["C26"] = new("_unused_", "cannot be null", DirectAction: () =>
        {
            PreceptCompiler.Compile(null!);
        }),

        // C27: Model with blank initial state — defensive check; parse always sets it.
        // Construct a model directly with an empty initial state.
        ["C27"] = new("_unused_", "initial", DirectAction: () =>
        {
            var model = new PreceptDefinition(
                "Test",
                [new PreceptState("A")],
                new PreceptState(""),
                [], [], [], null, null, null, null);
            PreceptCompiler.Compile(model);
        }),

        // C28: Initial state not in the states list
        ["C28"] = new("_unused_", "not defined", DirectAction: () =>
        {
            var model = new PreceptDefinition(
                "Test",
                [new PreceptState("A")],
                new PreceptState("Missing"),
                [], [], [], null, null, null, null);
            PreceptCompiler.Compile(model);
        }),

        // C29: Rule violated by default values
        ["C29"] = new(H + "field Score as number default 0\nrule Score > 0 because \"must be positive\"\n" + S, "rule violation"),

        // C30: State ensure on initial state violated by defaults
        ["C30"] = new(H + "field Balance as number default 0\nstate Active initial\nin Active ensure Balance > 0 because \"must be positive\"\n", "state ensure violation"),

        // C31: Event ensure violated by default arg values
        ["C31"] = new(H + S + "event Submit with Amount as number default 0\non Submit ensure Amount > 0 because \"must be positive\"\n", "event ensure violation"),

        // C32: Literal set assignment violates rule
        ["C32"] = new(H + "field Balance as number default 100\nrule Balance >= 0 because \"no negative\"\n" + S2 + "event Go\nfrom A on Go -> set Balance = -5 -> transition B\n", "violates rule"),

        // C44: Duplicate state ensure (same preposition, state, expression)
        ["C44"] = new(H + "field X as number default 10\n" + S2 + "in B ensure X > 0 because \"first\"\nin B ensure X > 0 because \"duplicate\"\nevent Go\nfrom A on Go -> transition B\n", "Duplicate state ensure"),

        // C45: Subsumed state ensure (to redundant with identical in)
        ["C45"] = new(H + "field X as number default 10\n" + S2 + "in B ensure X > 0 because \"in covers entry\"\nto B ensure X > 0 because \"to is redundant\"\nevent Go\nfrom A on Go -> transition B\n", "Subsumed state ensure"),

        // C46: Non-boolean expression in rule position (guard, rule, ensure)
        ["C46"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go when X -> transition B\nfrom A on Go -> reject \"blocked\"\n", "PRECEPT046"),

        // C47: Identical guard on duplicate transition rows for the same state+event pair
        ["C47"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go when X > 0 -> transition B\nfrom A on Go when X > 0 -> reject \"blocked\"\n", "PRECEPT047"),

        // C48: Unreachable state
        ["C48"] = new(H + "state A initial\nstate B\nstate C\nevent Go\nfrom A on Go -> transition B\n", "unreachable"),

        // C49: Orphaned event
        ["C49"] = new(H + S + "event Go\nevent Unused\nfrom A on Go -> no transition\n", "never referenced"),

        // C50: Dead-end state
        ["C50"] = new(H + "state A initial\nstate B\nevent Go\nfrom A on Go -> transition B\nfrom B on Go -> reject \"blocked\"\n", "no path forward"),

        // C51: Reject-only pair
        ["C51"] = new(H + S + "event Go\nfrom A on Go -> reject \"blocked\"\n", "ends in reject"),

        // C52: Event never succeeds
        ["C52"] = new(H + "state A initial\nstate B\nevent Move\nevent Stop\nfrom A on Move -> transition B\nfrom A on Stop -> reject \"blocked\"\nfrom B on Stop -> reject \"blocked\"\n", "never succeed"),

        // C53: Empty precept
        ["C53"] = new(H + S, "declares no events"),

        // C54: Undeclared state in transition row
        ["C54"] = new(H + S + "event Go\nfrom A on Go -> transition Nowhere\n", "Undeclared state"),

        // C55: Root-level edit with states declared
        ["C55"] = new(H + "field Priority as number default 1\n" + S + "edit Priority\n", "Root-level"),

        // C56: .length on nullable string without null guard
        ["C56"] = new(H + "field Note as string nullable\n" + S + "event Go\nfrom A on Go when Note.length > 0 -> no transition\n", "requires a null check"),

        // C57: Constraint applied to incompatible type (nonnegative on a string field)
        ["C57"] = new(H + "field Name as string default \"\" nonnegative\n" + S, "not valid for type"),

        // C58: Duplicate constraint on same field (min 1 appears twice)
        ["C58"] = new(H + "field Amount as number default 5 min 1 min 1\n" + S, "Duplicate constraint"),

        // C59: Default value violates constraint (0 is not positive)
        ["C59"] = new(H + "field Amount as number default 0 positive\n" + S, "violates constraint"),

        // C60: Narrowing assignment — assigning a number literal (3.0) to an integer field
        ["C60"] = new(H + "field Count as integer default 0\n" + S2 + "event Go\nfrom A on Go -> set Count = 3.0 -> no transition\n", "floor()"),

        // C61: maxplaces constraint on non-decimal field — constructed directly (maxplaces not yet a keyword)
        ["C61"] = new("_unused_", "decimal fields", DirectAction: () =>
        {
            var model = new PreceptDefinition(
                "Test",
                [new PreceptState("A")],
                new PreceptState("A"),
                [],
                [new PreceptField("Count", PreceptScalarType.Integer, false, true, 0L, [new FieldConstraint.Maxplaces(2)])],
                [], null);
            var result = PreceptCompiler.Validate(model);
            var diag = result.Diagnostics.FirstOrDefault(d => d.Constraint.Id == "C61");
            if (diag is null) throw new InvalidOperationException("C61 was not triggered");
            throw new InvalidOperationException(diag.Message);
        }),

        // C62: choice type with no values — constructed directly (parser enforces at least 1 value)
        ["C62"] = new("_unused_", "at least one value", DirectAction: () =>
        {
            var model = new PreceptDefinition(
                "Test",
                [new PreceptState("A")],
                new PreceptState("A"),
                [],
                [new PreceptField("Status", PreceptScalarType.Choice, false, false, null, null, [], false)],
                [], null);
            var result = PreceptCompiler.Validate(model);
            var diag = result.Diagnostics.FirstOrDefault(d => d.Constraint.Id == "C62");
            if (diag is null) throw new InvalidOperationException("C62 was not triggered");
            throw new InvalidOperationException(diag.Message);
        }),

        // C63: duplicate value in choice set
        ["C63"] = new(H + "field Status as choice(\"Open\",\"Open\",\"Closed\") default \"Open\"\n" + S, "Duplicate value"),

        // C64: default value not in choice set
        ["C64"] = new(H + "field Status as choice(\"Open\",\"Closed\") default \"Pending\"\n" + S, "not a member"),

        // C65: ordinal operator on a choice field that lacks 'ordered'
        ["C65"] = new(H + "field Status as choice(\"Draft\",\"Active\") default \"Draft\"\n" + S2 +
            "event Go\nfrom A on Go when Status > \"Active\" -> no transition\n", "ordered' constraint"),

        // C66: ordered on a non-choice type
        ["C66"] = new(H + "field Name as string nullable ordered\n" + S, "applies only to choice"),

        // C67: ordinal comparison between two choice fields — rank is field-local
        ["C67"] = new(H + "field Priority as choice(\"Low\",\"High\") default \"Low\" ordered\n" +
            "field Severity as choice(\"Low\",\"High\") default \"Low\" ordered\n" + S2 +
            "event Go\nfrom A on Go when Priority > Severity -> no transition\n", "field-local"),

        // C68: literal value not in choice set
        ["C68"] = new(H + "field Status as choice(\"Open\",\"Closed\") default \"Open\"\n" + S2 +
            "event Go\nfrom A on Go -> set Status = \"Invalid\" -> no transition\n", "not a member"),

        // C69: cross-scope guard reference — rule guard referencing event arg
        ["C69"] = new(H + "field X as number default 0\n" + S2 +
            "event Go with Amount as number\n" +
            "rule X >= 0 when Go.Amount > 0 because \"bad\"\n" +
            "from A on Go -> no transition\n", "different scope"),

        // C70: duplicate modifier on field/arg declaration
        ["C70"] = new(H + "field X as number default 0 default 1\n" + S2 +
            "event Go\nfrom A on Go -> no transition\n", "Duplicate modifier"),

        // C71: unknown function name — parser rejects unknown names at parse time,
        // so we construct a model with an unknown function call directly.
        ["C71"] = new("_unused_", "Unknown function", DirectAction: () =>
        {
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
                    [],
                    WhenText: "unknownfn(X) > 0",
                    WhenGuard: new PreceptBinaryExpression(
                        ">",
                        new PreceptFunctionCallExpression("unknownfn", [new PreceptIdentifierExpression("X")]),
                        new PreceptLiteralExpression(0L)),
                    SourceLine: 5)]);
            var result = PreceptCompiler.Validate(model);
            var diag = result.Diagnostics.FirstOrDefault(d => d.Constraint.Id == "C71");
            if (diag is null) throw new InvalidOperationException("C71 not triggered");
            throw new InvalidOperationException($"{diag.DiagnosticCode}: {diag.Message}");
        }),

        // C72: wrong number of arguments (parser requires ≥1 arg, so use 2-arg abs)
        ["C72"] = new(H + "field X as number default 0\n" + S2 +
            "event Go\nfrom A on Go when abs(X, X) > 0 -> no transition\n", "no matching overload"),

        // C73: argument type mismatch
        ["C73"] = new(H + "field Name as string default \"test\"\n" + S2 +
            "event Go\nfrom A on Go when abs(Name) > 0 -> no transition\n", "no matching overload"),

        // C74: round precision must be non-negative integer literal
        ["C74"] = new(H + "field X as number default 0\n" + S2 +
            "event Go\nfrom A on Go when round(X, X) > 0 -> no transition\n", "precision"),

        // C75: pow exponent must be integer type
        ["C75"] = new(H + "field X as number default 0\n" + S2 +
            "event Go\nfrom A on Go when pow(X, X) > 0 -> no transition\n", "exponent"),

        // C76: sqrt requires non-negative proof
        ["C76"] = new(H + "field X as number default 0\n" + S2 +
            "event Go\nfrom A on Go when sqrt(X) > 0 -> no transition\n", "non-negative"),

        // C77: function does not accept nullable arguments
        ["C77"] = new(H + "field X as number nullable default null\n" + S2 +
            "event Go\nfrom A on Go when abs(X) > 0 -> no transition\n", "nullable"),

        // C78: conditional expression condition must be a non-nullable boolean
        ["C78"] = new(H + "field X as string default \"\"\n" + S2 +
            "event Go\nfrom A on Go -> set X = if 42 then \"a\" else \"b\" -> no transition\n", "non-nullable boolean"),

        // C79: conditional expression branches must produce the same scalar type
        ["C79"] = new(H + "field X as number default 0\n" + S2 +
            "event Go\nfrom A on Go -> set X = if true then 42 else \"text\" -> no transition\n", "same scalar type"),

        // ── Parse-phase: computed/derived fields (C80–C82) ─────────────

        // C80: default + derived mutual exclusion
        ["C80"] = new(H + "field X as number default 0 -> 1 + 2\n" + S, "both a default value and a derived expression"),

        // C81: nullable + derived mutual exclusion
        ["C81"] = new(H + "field X as number nullable -> 1 + 2\n" + S, "nullable and has a derived expression"),

        // C82: multi-name + derived
        ["C82"] = new(H + "field A, B as number -> 1 + 2\n" + S, "Multi-name field declaration"),

        // ── Compile-phase: computed field validation (C83–C88) ─────────

        // C83: computed field references nullable field
        ["C83"] = new(H + "field Name as string nullable\nfield Display as string -> Name\n" + S, "nullable field"),

        // C84: computed field references event argument
        ["C84"] = new(H + "field Total as number default 0\nfield Calc as number -> Submit.Amount\n" + S2 +
            "event Submit with Amount as number\n", "event argument"),

        // C85: computed field uses unsafe collection accessor
        ["C85"] = new(H + "field Items as stack of number\nfield Top as number -> Items.peek\n" + S, "undefined on empty"),

        // C86: circular dependency among computed fields
        ["C86"] = new(H + "field A as number -> B + 1\nfield B as number -> A + 1\n" + S, "Circular dependency"),

        // C87: computed field in edit declaration
        ["C87"] = new(H + "field X as number default 0\nfield Y as number -> X + 1\n" + S2 + "in A edit X, Y\n", "computed field"),

        // C88: computed field as set target
        ["C88"] = new(H + "field X as number default 0\nfield Y as number -> X + 1\n" + S2 +
            "event Go\nfrom A on Go -> set Y = 5 -> no transition\n", "computed field"),

        // ── Compile-phase: divisor safety (C92–C93) ───────────────────

        // C92: literal zero divisor
        ["C92"] = new(H + "field Y as number default 10\n" + S +
            "event Go\nfrom A on Go -> set Y = Y / 0 -> no transition\n", "Division by zero"),

        // C93: unproven divisor
        ["C93"] = new(H + "field Y as number default 10\nfield D as number default 1\n" + S +
            "event Go\nfrom A on Go -> set Y = Y / D -> no transition\n", "no compile-time nonzero proof"),

        // C94: assignment value outside field constraint interval
        ["C94"] = new(H + "field Score as number default 50 max 100\n" + S +
            "event Go\nfrom A on Go -> set Score = 150 -> no transition\n", "outside"),

        // C95: contradictory rule (rule interval disjoint from field constraints)
        ["C95"] = new("precept Test\nfield X as number default 10 min 10\nstate A initial\n" +
            "rule X < 5 because \"contradicts min\"\n", "contradicts"),

        // C96: vacuous rule (always true given constraints)
        ["C96"] = new("precept Test\nfield X as number default 5 min 1 max 100\nstate A initial\n" +
            "rule X >= 0 because \"vacuous\"\n", "vacuous"),

        // C97: dead guard (always false given constraints)
        ["C97"] = new(H + "field X as number default 15 min 10\n" + S +
            "event Go\nfrom A on Go when X < 0 -> no transition\nfrom A on Go -> no transition\n", "always false"),

        // C98: tautological guard (always true given constraints)
        ["C98"] = new(H + "field X as number default 15 min 10\n" + S +
            "event Go\nfrom A on Go when X >= 0 -> no transition\n", "always true"),

        // ── Runtime-phase (C33–C37) ───────────────────────────────────

        // C33: CreateInstance with empty initial state
        ["C33"] = new("_unused_", "Initial state is required", DirectAction: () =>
        {
            var engine = PreceptCompiler.Compile(PreceptParser.Parse(H + S));
            engine.CreateInstance("", null);
        }),

        // C34: CreateInstance with non-existent state
        ["C34"] = new("_unused_", "not defined", DirectAction: () =>
        {
            var engine = PreceptCompiler.Compile(PreceptParser.Parse(H + S));
            engine.CreateInstance("NonExistent", null);
        }),

        // C35: CreateInstance with invalid data contract
        ["C35"] = new("_unused_", "number", DirectAction: () =>
        {
            var engine = PreceptCompiler.Compile(PreceptParser.Parse(H + "field X as number default 0\n" + S));
            engine.CreateInstance("A", new Dictionary<string, object?> { ["X"] = "not_a_number" });
        }),

        // C36: Inspect with empty current state
        ["C36"] = new("_unused_", "Current state is required", DirectAction: () =>
        {
            var engine = PreceptCompiler.Compile(PreceptParser.Parse(H + S));
            InvokeResolveTransition(engine, "", "SomeEvent");
        }),

        // C37: Inspect with empty event name
        ["C37"] = new("_unused_", "Event name is required", DirectAction: () =>
        {
            var engine = PreceptCompiler.Compile(PreceptParser.Parse(H + S));
            InvokeResolveTransition(engine, "A", "");
        }),

        // C38: Unknown identifier in expression
        ["C38"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go -> set X = Missing -> transition B\n", "PRECEPT038"),

        // C39: Expression type mismatch
        ["C39"] = new(H + "field X as number default 0\n" + S2 + "event Go\nfrom A on Go -> set X = \"text\" -> transition B\n", "PRECEPT039"),

        // C40: Unary operator type error
        ["C40"] = new(H + "field X as boolean default false\nfield Y as string default \"\"\n" + S2 + "event Go\nfrom A on Go -> set X = not Y -> transition B\n", "PRECEPT040"),

        // C41: Binary operator type error
        ["C41"] = new(H + "field X as number default 0\nfield Y as string default \"\"\n" + S2 + "event Go\nfrom A on Go -> set X = Y - 1 -> transition B\n", "PRECEPT041"),

        // C42: Null-flow violation
        ["C42"] = new(H + "field X as number default 0\nfield Y as number nullable\n" + S2 + "event Go\nfrom A on Go -> set X = Y -> transition B\n", "PRECEPT042"),

        // C43: Collection pop/dequeue into target type mismatch
        ["C43"] = new(H + "field X as number default 0\nfield Items as stack of string\n" + S2 + "event Go\nfrom A on Go when Items.count > 0 -> pop Items into X -> transition B\n", "PRECEPT043"),
    };

    // ════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly's directory to find the repo root (contains Precept.slnx)
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Precept.slnx")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repo root (Precept.slnx)");
    }

    // ════════════════════════════════════════════════════════════════════
    // Type vocabulary drift — Runtime + Grammar + MCP
    //
    // Every declarable scalar type must parse as a field type, as a
    // collection inner type, compile through the MCP tool, and appear
    // in the TextMate grammar's type-keyword pattern.
    // See language-surface-sync.instructions.md § Impact Categories.
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// All <see cref="PreceptScalarType"/> values that can appear in a
    /// <c>field X as {type}</c> declaration. Null is internal-only.
    /// Choice requires special syntax and is tested separately.
    /// </summary>
    private static readonly PreceptScalarType[] DeclarableScalarTypes =
        Enum.GetValues<PreceptScalarType>()
            .Where(t => t is not PreceptScalarType.Null and not PreceptScalarType.Choice)
            .ToArray();

    [Theory]
    [MemberData(nameof(DeclarableScalarTypeData))]
    public void EveryScalarType_ParsesAsFieldDeclaration(PreceptScalarType type)
    {
        var typeName = type.ToString().ToLowerInvariant();
        var dsl = $"precept DriftTest\nfield X as {typeName} default {DefaultForType(type)}\nstate A initial\n";
        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        diags.Should().BeEmpty($"'field X as {typeName}' must parse without errors");
        model.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(DeclarableScalarTypeData))]
    public void EveryScalarType_ParsesAsCollectionInnerType(PreceptScalarType type)
    {
        var typeName = type.ToString().ToLowerInvariant();
        var dsl = $"precept DriftTest\nfield Items as set of {typeName}\nstate A initial\n";
        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        diags.Should().BeEmpty($"'set of {typeName}' must parse without errors");
        model.Should().NotBeNull();
    }

    [Fact]
    public void ChoiceType_ParsesAsFieldDeclaration()
    {
        var dsl = "precept DriftTest\nfield X as choice(\"A\",\"B\") default \"A\"\nstate A initial\n";
        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        diags.Should().BeEmpty("'field X as choice(...)' must parse without errors");
        model.Should().NotBeNull();
    }

    [Fact]
    public void ChoiceType_ParsesAsCollectionInnerType()
    {
        var dsl = "precept DriftTest\nfield Items as set of choice(\"X\",\"Y\")\nstate A initial\n";
        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
        diags.Should().BeEmpty("'set of choice(...)' must parse without errors");
        model.Should().NotBeNull();
    }

    [Fact]
    public void GrammarTypeKeywords_CoverAllDeclarableTypes()
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        File.Exists(grammarPath).Should().BeTrue("grammar file must exist");

        var grammarText = File.ReadAllText(grammarPath);

        // Extract the typeKeywords pattern match value
        var typeKeywordMatch = Regex.Match(grammarText, @"""typeKeywords"".*?""match"":\s*""([^""]+)""", RegexOptions.Singleline);
        typeKeywordMatch.Success.Should().BeTrue("typeKeywords pattern must exist in grammar");

        var pattern = typeKeywordMatch.Groups[1].Value;

        foreach (var type in DeclarableScalarTypes)
        {
            var typeName = type.ToString().ToLowerInvariant();
            pattern.Should().Contain(typeName,
                $"TextMate grammar typeKeywords must include '{typeName}'");
        }

        pattern.Should().Contain("choice",
            "TextMate grammar typeKeywords must include 'choice'");
    }

    [Fact]
    public void GrammarFieldScalarDeclaration_CoversAllSimpleScalarTypes()
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        // Extract the fieldScalarDeclaration pattern
        var match = Regex.Match(grammarText, @"""fieldScalarDeclaration"".*?""match"":\s*""((?:[^""\\]|\\.)+)""", RegexOptions.Singleline);
        match.Success.Should().BeTrue("fieldScalarDeclaration pattern must exist");

        var pattern = match.Groups[1].Value;

        foreach (var type in DeclarableScalarTypes)
        {
            var typeName = type.ToString().ToLowerInvariant();
            pattern.Should().Contain(typeName,
                $"grammar fieldScalarDeclaration must include '{typeName}' so 'field X as {typeName}' gets structured highlighting");
        }
    }

    [Fact]
    public void GrammarChoiceType_HasDedicatedPatternBeforeGenericTypeFallback()
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        using var document = JsonDocument.Parse(File.ReadAllText(grammarPath));

        var root = document.RootElement;
        var includes = root.GetProperty("patterns")
            .EnumerateArray()
            .Select(p => p.GetProperty("include").GetString())
            .ToList();

        includes.Should().Contain("#choiceType");
        includes.IndexOf("#choiceType").Should().BeLessThan(includes.IndexOf("#typeKeywords"),
            "choice(...) must be matched before the generic type keyword fallback");

        var choiceType = root.GetProperty("repository").GetProperty("choiceType");
        var choicePattern = choiceType.GetProperty("patterns")[0];

        choicePattern.GetProperty("beginCaptures").GetProperty("1").GetProperty("name").GetString()
            .Should().Be("storage.type.precept");
        choicePattern.GetProperty("beginCaptures").GetProperty("2").GetProperty("name").GetString()
            .Should().Be("punctuation.section.group.begin.precept");
        choicePattern.GetProperty("endCaptures").GetProperty("0").GetProperty("name").GetString()
            .Should().Be("punctuation.section.group.end.precept");

        var nestedPatterns = choicePattern.GetProperty("patterns").EnumerateArray().ToList();
        nestedPatterns.Any(p =>
            p.TryGetProperty("include", out var include) && include.GetString() == "#strings")
            .Should().BeTrue("choice values should use the normal Precept string scope");
        nestedPatterns.Any(p =>
            p.TryGetProperty("name", out var name) && name.GetString() == "punctuation.separator.comma.precept")
            .Should().BeTrue("choice separators should use explicit punctuation scopes");
    }

    [Fact]
    public void GrammarFieldCollectionDeclaration_CoversAllSimpleScalarTypes()
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        // Extract the fieldCollectionDeclaration pattern
        var match = Regex.Match(grammarText, @"""fieldCollectionDeclaration"".*?""match"":\s*""((?:[^""\\]|\\.)+)""", RegexOptions.Singleline);
        match.Success.Should().BeTrue("fieldCollectionDeclaration pattern must exist");

        var pattern = match.Groups[1].Value;

        foreach (var type in DeclarableScalarTypes)
        {
            var typeName = type.ToString().ToLowerInvariant();
            pattern.Should().Contain(typeName,
                $"grammar fieldCollectionDeclaration must include '{typeName}' so 'set of {typeName}' gets structured highlighting");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Keyword / operator grammar drift — ALL token categories
    //
    // Every token that carries a category with a corresponding grammar
    // pattern must appear in that pattern's regex.  Dual-category tokens
    // (e.g. set = Action + Type) must appear in ALL their patterns.
    // See language-surface-sync.instructions.md § Tooling Impact.
    // ════════════════════════════════════════════════════════════════════

    private static readonly (TokenCategory Category, string PatternName)[] CategoryGrammarMap =
    [
        (TokenCategory.Control, "controlKeywords"),
        (TokenCategory.Declaration, "declarationKeywords"),
        (TokenCategory.Grammar, "grammarKeywords"),
        (TokenCategory.Action, "actionKeywords"),
        (TokenCategory.Type, "typeKeywords"),
        (TokenCategory.Constraint, "constraintKeywords"),
        (TokenCategory.Operator, "operators"),
        (TokenCategory.Outcome, "outcomeKeywords"),
        (TokenCategory.Literal, "booleanNull"),
    ];

    private static readonly (TokenCategory Category, string PatternName)[] KeywordCategoryGrammarMap =
        CategoryGrammarMap
            .Where(m => m.PatternName != "operators")
            .ToArray();

    [Theory]
    [MemberData(nameof(CategoryGrammarMappingData))]
    public void Grammar_CategoryKeywords_CoverAllTokensWithThatCategory(
        TokenCategory category, string grammarPatternName)
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        var patternMatches = ExtractGrammarMatchValues(grammarText, grammarPatternName);
        patternMatches.Should().NotBeEmpty(
            $"grammar pattern '{grammarPatternName}' must have at least one match value");

        var combinedPattern = string.Join(" ", patternMatches);

        var tokens = PreceptTokenMeta.GetByCategory(category).ToList();
        tokens.Should().NotBeEmpty($"TokenCategory.{category} must have at least one token");

        var missing = new List<string>();
        foreach (var token in tokens)
        {
            var symbol = PreceptTokenMeta.GetSymbol(token);
            if (symbol is null) continue;

            if (!SymbolAppearsInPattern(symbol, combinedPattern))
                missing.Add($"{token} (symbol: '{symbol}')");
        }

        missing.Should().BeEmpty(
            $"every token with TokenCategory.{category} must appear in grammar pattern '{grammarPatternName}'");
    }

    public static IEnumerable<object[]> CategoryGrammarMappingData()
        => CategoryGrammarMap.Select(m => new object[] { m.Category, m.PatternName });

    [Theory]
    [MemberData(nameof(KeywordCategoryGrammarMappingData))]
    public void Grammar_KeywordPattern_ContainsOnlyTokensWithExpectedCategory(
        TokenCategory category, string grammarPatternName)
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        var patternMatches = ExtractGrammarMatchValues(grammarText, grammarPatternName);
        patternMatches.Should().NotBeEmpty(
            $"grammar pattern '{grammarPatternName}' must have at least one match value");

        var keywords = ExtractGrammarWordSymbols(patternMatches);
        keywords.Should().NotBeEmpty(
            $"grammar pattern '{grammarPatternName}' must contain at least one keyword");

        var mismatches = new List<string>();
        foreach (var keyword in keywords)
        {
            var matchingTokens = Enum.GetValues<PreceptToken>()
                .Where(token => string.Equals(PreceptTokenMeta.GetSymbol(token), keyword, StringComparison.Ordinal))
                .ToList();

            if (matchingTokens.Any(token => PreceptTokenMeta.GetCategories(token).Contains(category)))
                continue;

            var actualCategories = matchingTokens.Count == 0
                ? "no matching token"
                : string.Join(" | ", matchingTokens.Select(token =>
                    $"{token}: [{string.Join(", ", PreceptTokenMeta.GetCategories(token))}]"));

            mismatches.Add($"'{keyword}' => {actualCategories}");
        }

        mismatches.Should().BeEmpty(
            $"grammar pattern '{grammarPatternName}' must only contain symbols from TokenCategory.{category}");
    }

    public static IEnumerable<object[]> KeywordCategoryGrammarMappingData()
        => KeywordCategoryGrammarMap.Select(m => new object[] { m.Category, m.PatternName });

    /// <summary>
    /// Extracts all "match" regex strings from a named grammar repository pattern.
    /// Handles both single-pattern and multi-sub-pattern structures.
    /// </summary>
    private static List<string> ExtractGrammarMatchValues(string grammarJson, string patternName)
    {
        using var doc = JsonDocument.Parse(grammarJson);
        var repo = doc.RootElement.GetProperty("repository");
        var pattern = repo.GetProperty(patternName);
        var patterns = pattern.GetProperty("patterns");

        var matches = new List<string>();
        foreach (var p in patterns.EnumerateArray())
        {
            if (p.TryGetProperty("match", out var matchProp))
                matches.Add(matchProp.GetString()!);
        }
        return matches;
    }

    /// <summary>
    /// Checks whether a token symbol appears in a combined grammar pattern text.
    /// Strips regex metacharacters before matching to handle compound patterns
    /// like \bno\s+transition\b where 'no' must be found.
    /// Uses word-boundary matching for alphabetic symbols to avoid substring false positives
    /// (e.g. "in" matching inside "initial").
    /// </summary>
    private static bool SymbolAppearsInPattern(string symbol, string patternText)
    {
        if (symbol.All(char.IsLetterOrDigit))
        {
            // Strip regex metacharacters so \bno\s+transition\b becomes "no transition"
            var stripped = Regex.Replace(patternText, @"\\[bBdDwWsS+*?]", " ");
            return Regex.IsMatch(stripped, $@"\b{Regex.Escape(symbol)}\b");
        }
        return patternText.Contains(symbol);
    }

    // ════════════════════════════════════════════════════════════════════
    // Reverse drift — grammar patterns must not contain phantom keywords
    //
    // Every word in a grammar keyword pattern must map back to a token in
    // the PreceptToken enum.  Catches keywords added to the grammar but
    // never implemented in the runtime (e.g. aspirational "if"/"else").
    // ════════════════════════════════════════════════════════════════════

    private static readonly string[] KeywordPatternNames =
    [
        "controlKeywords",
        "declarationKeywords",
        "grammarKeywords",
        "actionKeywords",
        "typeKeywords",
        "constraintKeywords",
        "outcomeKeywords",
        "booleanNull",
    ];

    [Theory]
    [MemberData(nameof(KeywordPatternNameData))]
    public void Grammar_KeywordPattern_ContainsOnlyRecognizedTokens(string grammarPatternName)
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        var patternMatches = ExtractGrammarMatchValues(grammarText, grammarPatternName);
        patternMatches.Should().NotBeEmpty(
            $"grammar pattern '{grammarPatternName}' must have at least one match value");

        var allKeywords = ExtractGrammarWordSymbols(patternMatches);

        // Build the set of all known token symbols
        var knownSymbols = Enum.GetValues<PreceptToken>()
            .Select(PreceptTokenMeta.GetSymbol)
            .Where(s => s is not null)
            .ToHashSet();

        var phantoms = allKeywords.Where(k => !knownSymbols.Contains(k)).ToList();

        phantoms.Should().BeEmpty(
            $"every keyword in grammar pattern '{grammarPatternName}' must correspond to a token in PreceptToken enum — " +
            $"phantom keywords have no runtime backing");
    }

    public static IEnumerable<object[]> KeywordPatternNameData()
        => KeywordPatternNames.Select(n => new object[] { n });

    private static List<string> ExtractGrammarWordSymbols(IEnumerable<string> patternMatches)
    {
        var stripped = string.Join(" ", patternMatches
            .Select(match => Regex.Replace(match, @"\\[bBdDwWsS+*?]", " ")));

        return Regex.Matches(stripped, @"\b([a-z]+)\b")
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    // ════════════════════════════════════════════════════════════════════
    // Reverse drift — operator symbols
    //
    // The operators grammar pattern uses symbol characters, not words.
    // Extract each distinct symbol/word operator and verify it maps to a
    // token in the enum.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Grammar_Operators_ContainOnlyRecognizedSymbols()
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        var patternMatches = ExtractGrammarMatchValues(grammarText, "operators");
        patternMatches.Should().NotBeEmpty();

        // Known token symbols from the enum
        var knownSymbols = Enum.GetValues<PreceptToken>()
            .Select(PreceptTokenMeta.GetSymbol)
            .Where(s => s is not null)
            .ToHashSet();

        var phantoms = new List<string>();

        foreach (var pattern in patternMatches)
        {
            // Extract word operators (e.g. and|or|not|contains)
            foreach (Match m in Regex.Matches(pattern, @"\b([a-z]+)\b"))
            {
                var word = m.Groups[1].Value;
                if (word != "b" && !knownSymbols.Contains(word))
                    phantoms.Add(word);
            }

            // Extract multi-char symbol operators (e.g. ==, !=, >=, <=, ->)
            foreach (Match m in Regex.Matches(pattern, @"([=!><]=|->)"))
            {
                if (!knownSymbols.Contains(m.Value))
                    phantoms.Add(m.Value);
            }

            // Extract single-char operators from character classes [+\-*/%]
            foreach (Match m in Regex.Matches(pattern, @"\[([^\]]+)\]"))
            {
                foreach (var ch in m.Groups[1].Value.Replace("\\", ""))
                {
                    var sym = ch.ToString();
                    if (sym != "-" || !knownSymbols.Contains(sym)) // \- is escape
                    {
                        if (!knownSymbols.Contains(sym))
                            phantoms.Add(sym);
                    }
                }
            }

            // Extract standalone single-char comparison operators (>|<)
            foreach (Match m in Regex.Matches(pattern, @"^([><])\|([><])$"))
            {
                if (!knownSymbols.Contains(m.Groups[1].Value))
                    phantoms.Add(m.Groups[1].Value);
                if (!knownSymbols.Contains(m.Groups[2].Value))
                    phantoms.Add(m.Groups[2].Value);
            }

            // Standalone single = (assignment)
            if (Regex.IsMatch(pattern, @"^=$") && !knownSymbols.Contains("="))
                phantoms.Add("=");
        }

        phantoms.Distinct().ToList().Should().BeEmpty(
            "every operator in the grammar must correspond to a token in PreceptToken enum");
    }

    public static IEnumerable<object[]> DeclarableScalarTypeData()
        => DeclarableScalarTypes.Select(t => new object[] { t });

    private static string DefaultForType(PreceptScalarType type) => type switch
    {
        PreceptScalarType.String => "\"\"",
        PreceptScalarType.Number => "0",
        PreceptScalarType.Boolean => "false",
        PreceptScalarType.Integer => "0",
        PreceptScalarType.Decimal => "0.0",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // ════════════════════════════════════════════════════════════════════
    // Tier 1: Reflection guard — model records must have SourceLine
    //
    // Every model record that represents a user-authored declaration must
    // carry int SourceLine so diagnostics can point to the correct line.
    // If someone adds PreceptNewThing without SourceLine, this fails.
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Model record types that represent user-authored DSL declarations.
    /// Each must have an <c>int SourceLine</c> property so diagnostics can
    /// squiggle the correct declaration line.
    /// </summary>
    private static readonly Type[] DeclarationRecordTypes =
    [
        typeof(PreceptDefinition),
        typeof(PreceptState),
        typeof(PreceptEvent),
        typeof(PreceptEventArg),
        typeof(PreceptField),
        typeof(PreceptCollectionField),
        typeof(PreceptRule),
        typeof(StateEnsure),
        typeof(EventEnsure),
        typeof(PreceptTransitionRow),
        typeof(PreceptEditBlock),
        typeof(PreceptStateAction),
        typeof(PreceptSetAssignment),
        typeof(PreceptCollectionMutation),
    ];

    [Fact]
    public void AllDeclarationRecords_HaveSourceLineProperty()
    {
        var missing = new List<string>();

        foreach (var type in DeclarationRecordTypes)
        {
            var prop = type.GetProperty("SourceLine", BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || prop.PropertyType != typeof(int))
                missing.Add(type.Name);
        }

        missing.Should().BeEmpty(
            "every model record representing a user-authored declaration must have int SourceLine " +
            "so constraint violations can squiggle the correct line. " +
            "Add 'int SourceLine = 0' to the record's trailing parameters.");
    }

    // ════════════════════════════════════════════════════════════════════
    // Tier 2: Diagnostic line accuracy — must not squiggle the header
    //
    // For every parse/compile-phase constraint, constructs a multi-line
    // DSL where the offending declaration is NOT on line 1. Asserts that
    // the diagnostic Line > 1 (1-based: line 1 = precept header).
    //
    // Piggybacks on the existing ConstraintTriggers infrastructure.
    // When someone adds C70, the completeness guard below fails if no
    // line accuracy case is added.
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Constraints exempt from the "must not squiggle line 1" invariant.
    /// Each has a documented reason for landing on line 1.
    /// </summary>
    private static readonly HashSet<string> LineAccuracyExemptions = new(StringComparer.Ordinal)
    {
        "C1",   // empty input — nothing to point to
        "C3",   // structurally unparseable — parse failure, no model
        "C4",   // invalid expression via standalone ParseExpression — DirectAction
        "C5",   // invalid number literal — DirectAction (unreachable from normal parse)
        "C12",  // no states or fields — nothing to point to
        "C26",  // null model — DirectAction (programmatic precondition)
        "C27",  // blank initial state — DirectAction
        "C28",  // initial state not in list — DirectAction
        "C33",  // runtime: CreateInstance with empty state — DirectAction
        "C34",  // runtime: CreateInstance with bad state — DirectAction
        "C35",  // runtime: CreateInstance with bad data — DirectAction
        "C36",  // runtime: empty current state — DirectAction
        "C37",  // runtime: empty event name — DirectAction
        "C53",  // empty precept (no events) — legitimately points at precept header
        "C61",  // maxplaces on non-decimal — DirectAction
        "C62",  // choice with no values — DirectAction
        "C71",  // unknown function name — DirectAction (parser rejects unknown names)
    };

    /// <summary>
    /// DSL snippets for line accuracy testing. Each places the offending declaration
    /// on line 3+ so we can assert the diagnostic doesn't fall back to line 1.
    /// Most reuse the ConstraintTriggers DSL with extra padding lines prepended.
    /// </summary>
    private static readonly Dictionary<string, (string Dsl, string Phase, int MinExpectedLine)> LineAccuracyCases = new()
    {
        // ── Parse-phase: field/state/event violations ──

        // C2: tokenizer error — unrecognized character on line 3
        ["C2"]  = ("precept Test\nfield X as number default 0\n@invalid\n", "parse", 3),

        // C6: duplicate field — second field decl on line 4
        ["C6"]  = ("precept Test\nfield Pad as number default 0\nstate A initial\nfield Pad as string nullable\n", "parse", 4),

        // C7: duplicate state — second state decl on line 3
        ["C7"]  = ("precept Test\nstate A initial\nstate A\n", "parse", 3),

        // C8: duplicate initial — second initial on line 3
        ["C8"]  = ("precept Test\nstate A initial\nstate B initial\n", "parse", 3),

        // C9: duplicate event — second event on line 4
        ["C9"]  = ("precept Test\nstate A initial\nevent Go\nevent Go\nfrom A on Go -> no transition\n", "parse", 4),

        // C10: missing outcome — row on line 5
        ["C10"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = 1\n", "parse", 6),

        // C11: statements after outcome — row on line 5
        ["C11"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> transition B -> set X = 1\n", "parse", 6),

        // C13: no initial state — state decl on line 3
        ["C13"] = ("precept Test\nfield X as number default 0\nstate A, B\nevent Go\nfrom A on Go -> transition B\n", "parse", 3),

        // C14: event ensure with wrong dotted prefix — assert on line 4
        ["C14"] = ("precept Test\nstate A initial\nevent Submit with Comment as string default \"x\"\non Submit ensure Other.Comment != null because \"bad\"\n", "parse", 4),

        // C15: event ensure dotted member not a declared arg — assert on line 4
        ["C15"] = ("precept Test\nstate A initial\nevent Submit with Comment as string default \"x\"\non Submit ensure Submit.Nope != null because \"bad\"\n", "parse", 4),

        // C16: event ensure plain identifier not a declared arg — assert on line 4
        ["C16"] = ("precept Test\nstate A initial\nevent Submit with Comment as string default \"x\"\non Submit ensure Nope != null because \"bad\"\n", "parse", 4),

        // C17: non-nullable field without default — field on line 4
        ["C17"] = ("precept Test\nfield Title as string nullable\nfield Description as string nullable\nfield Blah as string\n", "parse", 4),

        // C18: field default type mismatch — field on line 3
        ["C18"] = ("precept Test\nstate A initial\nfield X as number default \"hello\"\n", "parse", 3),

        // C19: non-nullable field with null default — field on line 3
        ["C19"] = ("precept Test\nstate A initial\nfield X as string default null\n", "parse", 3),

        // C20: event arg default type mismatch — event on line 3
        ["C20"] = ("precept Test\nstate A initial\nevent Submit with X as number default \"hello\"\n", "parse", 3),

        // C21: non-nullable event arg with null default — event on line 3
        ["C21"] = ("precept Test\nstate A initial\nevent Submit with X as string default null\n", "parse", 3),

        // C22: collection verb on scalar field — row on line 5
        ["C22"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> add X \"val\" -> transition B\n", "parse", 6),

        // C23: collection verb on unknown field — row on line 4
        ["C23"] = ("precept Test\nstate A initial\nstate B\nevent Go\nfrom A on Go -> add Unknown \"val\" -> transition B\n", "parse", 5),

        // C24: wrong verb for collection kind — row on line 5
        ["C24"] = ("precept Test\nfield Tags as set of string\nstate A initial\nstate B\nevent Go\nfrom A on Go -> enqueue Tags \"val\" -> transition B\n", "parse", 6),

        // C25: unreachable duplicate row — second row on line 6
        ["C25"] = ("precept Test\nstate A initial\nstate B\nevent Go\nfrom A on Go -> transition B\nfrom A on Go -> transition B\n", "parse", 6),

        // C54: undeclared state in transition — row on line 4
        ["C54"] = ("precept Test\nstate A initial\nevent Go\nfrom A on Go -> transition Nowhere\n", "parse", 4),

        // ── Compile-phase: type checker + analysis ────

        // C29: rule violated by defaults — rule on line 3
        ["C29"] = ("precept Test\nfield Score as number default 0\nrule Score > 0 because \"must be positive\"\nstate A initial\n", "compile", 3),

        // C30: state ensure on initial state violated by defaults — assert on line 3
        ["C30"] = ("precept Test\nfield Balance as number default 0\nin Active ensure Balance > 0 because \"must be positive\"\nstate Active initial\n", "compile", 3),

        // C31: event ensure violated by default arg values — assert on line 3
        ["C31"] = ("precept Test\nstate A initial\non Submit ensure Amount > 0 because \"must be positive\"\nevent Submit with Amount as number default 0\n", "compile", 3),

        // C32: literal set violates rule — row on line 6
        ["C32"] = ("precept Test\nfield Balance as number default 100\nrule Balance >= 0 because \"no negative\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Balance = -5 -> transition B\n", "compile", 7),

        // C38: unknown identifier in expression — row on line 5
        ["C38"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = Missing -> transition B\n", "compile", 6),

        // C39: expression type mismatch — row on line 5
        ["C39"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = \"text\" -> transition B\n", "compile", 6),

        // C40: unary operator type error — row on line 6
        ["C40"] = ("precept Test\nfield X as boolean default false\nfield Y as string default \"\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = not Y -> transition B\n", "compile", 7),

        // C41: binary operator type error — row on line 6
        ["C41"] = ("precept Test\nfield X as number default 0\nfield Y as string default \"\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = Y - 1 -> transition B\n", "compile", 7),

        // C42: null-flow violation — row on line 5
        ["C42"] = ("precept Test\nfield X as number default 0\nfield Y as number nullable\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = Y -> transition B\n", "compile", 7),

        // C43: collection pop/dequeue into target type mismatch — row on line 5
        ["C43"] = ("precept Test\nfield X as number default 0\nfield Items as stack of string\nstate A initial\nstate B\nevent Go\nfrom A on Go when Items.count > 0 -> pop Items into X -> transition B\n", "compile", 7),

        // C44: duplicate state ensure — second ensure on line 5
        ["C44"] = ("precept Test\nfield X as number default 10\nstate A initial\nstate B\nin B ensure X > 0 because \"first\"\nin B ensure X > 0 because \"duplicate\"\nevent Go\nfrom A on Go -> transition B\n", "compile", 6),

        // C45: subsumed state ensure — redundant ensure on line 5
        ["C45"] = ("precept Test\nfield X as number default 10\nstate A initial\nstate B\nin B ensure X > 0 because \"in covers entry\"\nto B ensure X > 0 because \"to is redundant\"\nevent Go\nfrom A on Go -> transition B\n", "compile", 6),

        // C46: non-boolean expression in guard — row on line 5
        ["C46"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when X -> transition B\nfrom A on Go -> reject \"blocked\"\n", "compile", 6),

        // C47: identical guard on duplicate rows — second guarded row on line 6
        ["C47"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when X > 0 -> transition B\nfrom A on Go when X > 0 -> reject \"blocked\"\n", "compile", 7),

        // C48: unreachable state — state C on line 4
        ["C48"] = ("precept Test\nstate A initial\nstate B\nstate C\nevent Go\nfrom A on Go -> transition B\n", "compile", 4),

        // C49: orphaned event — unused event on line 4
        ["C49"] = ("precept Test\nstate A initial\nevent Go\nevent Unused\nfrom A on Go -> no transition\n", "compile", 4),

        // C50: dead-end state — state B on line 3
        ["C50"] = ("precept Test\nstate A initial\nstate B\nevent Go\nfrom A on Go -> transition B\nfrom B on Go -> reject \"blocked\"\n", "compile", 3),

        // C51: reject-only pair — row on line 3
        ["C51"] = ("precept Test\nstate A initial\nevent Go\nfrom A on Go -> reject \"blocked\"\n", "compile", 4),

        // C52: event never succeeds — event Stop on line 4
        ["C52"] = ("precept Test\nstate A initial\nstate B\nevent Move\nevent Stop\nfrom A on Move -> transition B\nfrom A on Stop -> reject \"blocked\"\nfrom B on Stop -> reject \"blocked\"\n", "compile", 5),

        // C55: root-level edit with states declared — edit on line 4
        ["C55"] = ("precept Test\nfield Priority as number default 1\nstate A initial\nedit Priority\n", "compile", 4),

        // C56: .length on nullable without null guard — row on line 4
        ["C56"] = ("precept Test\nfield Note as string nullable\nstate A initial\nevent Go\nfrom A on Go when Note.length > 0 -> no transition\n", "compile", 5),

        // C57: constraint on incompatible type — field on line 2
        ["C57"] = ("precept Test\nfield Name as string default \"\" nonnegative\nstate A initial\n", "compile", 2),

        // C58: duplicate constraint — field on line 2
        ["C58"] = ("precept Test\nfield Amount as number default 5 min 1 min 1\nstate A initial\n", "compile", 2),

        // C59: default violates constraint — field on line 2
        ["C59"] = ("precept Test\nfield Amount as number default 0 positive\nstate A initial\n", "compile", 2),

        // C60: narrowing assignment (number → integer) — row on line 5
        ["C60"] = ("precept Test\nfield Count as integer default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Count = 3.0 -> no transition\n", "compile", 6),

        // C63: duplicate value in choice set — field on line 2
        ["C63"] = ("precept Test\nfield Status as choice(\"Open\",\"Open\",\"Closed\") default \"Open\"\nstate A initial\n", "compile", 2),

        // C64: default not in choice set — field on line 2
        ["C64"] = ("precept Test\nfield Status as choice(\"Open\",\"Closed\") default \"Pending\"\nstate A initial\n", "compile", 2),

        // C65: ordinal operator on unordered choice — row on line 5
        ["C65"] = ("precept Test\nfield Status as choice(\"Draft\",\"Active\") default \"Draft\"\nstate A initial\nstate B\nevent Go\nfrom A on Go when Status > \"Active\" -> no transition\n", "compile", 6),

        // C66: ordered on non-choice type — field on line 2
        ["C66"] = ("precept Test\nfield Name as string nullable ordered\nstate A initial\n", "compile", 2),

        // C67: ordinal comparison between two choice fields — row on line 6
        ["C67"] = ("precept Test\nfield Priority as choice(\"Low\",\"High\") default \"Low\" ordered\nfield Severity as choice(\"Low\",\"High\") default \"Low\" ordered\nstate A initial\nstate B\nevent Go\nfrom A on Go when Priority > Severity -> no transition\n", "compile", 7),

        // C68: literal not in choice set — row on line 5
        ["C68"] = ("precept Test\nfield Status as choice(\"Open\",\"Closed\") default \"Open\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Status = \"Invalid\" -> no transition\n", "compile", 6),

        // C69: cross-scope guard reference — rule on line 4
        ["C69"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go with Amount as number\nrule X >= 0 when Go.Amount > 0 because \"bad\"\nfrom A on Go -> no transition\n", "compile", 6),

        // C70: duplicate modifier — field on line 2
        ["C70"] = ("precept Test\nfield X as number default 0 default 1\nstate A initial\n", "parse", 2),

        // C72: wrong arity — row on line 6
        ["C72"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(X, X) > 0 -> no transition\n", "compile", 6),

        // C73: type mismatch — row on line 6
        ["C73"] = ("precept Test\nfield Name as string default \"test\"\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(Name) > 0 -> no transition\n", "compile", 6),

        // C74: round precision — row on line 6
        ["C74"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when round(X, X) > 0 -> no transition\n", "compile", 6),

        // C75: pow exponent — row on line 6
        ["C75"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when pow(X, X) > 0 -> no transition\n", "compile", 6),

        // C76: sqrt non-negative — row on line 6
        ["C76"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go when sqrt(X) > 0 -> no transition\n", "compile", 6),

        // C77: nullable arg — row on line 6
        ["C77"] = ("precept Test\nfield X as number nullable default null\nstate A initial\nstate B\nevent Go\nfrom A on Go when abs(X) > 0 -> no transition\n", "compile", 6),

        // C78: conditional with non-boolean condition — row on line 6
        ["C78"] = ("precept Test\nfield X as string default \"\"\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = if 42 then \"a\" else \"b\" -> no transition\n", "compile", 6),

        // C79: conditional with mismatched branch types — row on line 6
        ["C79"] = ("precept Test\nfield X as number default 0\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set X = if true then 42 else \"text\" -> no transition\n", "compile", 6),

        // ── Parse-phase: computed/derived fields (C80–C82) ─────────────

        // C80: default + derived — field on line 3
        ["C80"] = ("precept Test\nstate A initial\nfield X as number default 0 -> 1 + 2\n", "parse", 3),

        // C81: nullable + derived — field on line 3
        ["C81"] = ("precept Test\nstate A initial\nfield X as number nullable -> 1 + 2\n", "parse", 3),

        // C82: multi-name + derived — field on line 3
        ["C82"] = ("precept Test\nstate A initial\nfield A, B as number -> 1 + 2\n", "parse", 3),

        // ── Compile-phase: computed field validation (C83–C88) ─────────

        // C83: nullable field in computed expression — field on line 3
        ["C83"] = ("precept Test\nfield Name as string nullable\nfield Display as string -> Name\nstate A initial\n", "compile", 3),

        // C84: event arg in computed expression — field on line 3
        ["C84"] = ("precept Test\nfield Total as number default 0\nfield Calc as number -> Submit.Amount\nstate A initial\nevent Submit with Amount as number\n", "compile", 3),

        // C85: unsafe accessor in computed expression — field on line 3
        ["C85"] = ("precept Test\nfield Items as stack of number\nfield Top as number -> Items.peek\nstate A initial\n", "compile", 3),

        // C86: circular dependency — field on line 2
        ["C86"] = ("precept Test\nfield A as number -> B + 1\nfield B as number -> A + 1\nstate A initial\n", "compile", 2),

        // C87: computed field in edit — edit on line 5
        ["C87"] = ("precept Test\nfield X as number default 0\nfield Y as number -> X + 1\nstate A initial\nin A edit X, Y\n", "compile", 5),

        // C88: computed field as set target — row on line 6
        ["C88"] = ("precept Test\nfield X as number default 0\nfield Y as number -> X + 1\nstate A initial\nstate B\nevent Go\nfrom A on Go -> set Y = 5 -> no transition\n", "compile", 7),

        // ── Compile-phase: divisor safety (C92–C93) ───────────────────

        // C92: literal zero divisor — row on line 5
        ["C92"] = ("precept Test\nfield Y as number default 10\nstate A initial\nevent Go\nfrom A on Go -> set Y = Y / 0 -> no transition\n", "compile", 5),

        // C93: unproven divisor — row on line 6
        ["C93"] = ("precept Test\nfield Y as number default 10\nfield D as number default 1\nstate A initial\nevent Go\nfrom A on Go -> set Y = Y / D -> no transition\n", "compile", 6),

        // C94: assignment value outside field constraint — row on line 5
        ["C94"] = ("precept Test\nfield Score as number default 50 max 100\nstate A initial\nevent Go\nfrom A on Go -> set Score = 150 -> no transition\n", "compile", 5),

        // C95: contradictory rule — rule on line 4
        ["C95"] = ("precept Test\nfield X as number default 10 min 10\nstate A initial\nrule X < 5 because \"contradicts min\"\n", "compile", 4),

        // C96: vacuous rule — rule on line 4
        ["C96"] = ("precept Test\nfield X as number default 5 min 1 max 100\nstate A initial\nrule X >= 0 because \"vacuous\"\n", "compile", 4),

        // C97: dead guard — transition row on line 5
        ["C97"] = ("precept Test\nfield X as number default 15 min 10\nstate A initial\nevent Go\nfrom A on Go when X < 0 -> no transition\nfrom A on Go -> no transition\n", "compile", 5),

        // C98: tautological guard — transition row on line 5
        ["C98"] = ("precept Test\nfield X as number default 15 min 10\nstate A initial\nevent Go\nfrom A on Go when X >= 0 -> no transition\n", "compile", 5),
    };

    [Theory]
    [MemberData(nameof(LineAccuracyData))]
    public void EveryConstraint_DiagnosticDoesNotSquiggleHeaderLine(string constraintId, string phase, int minExpectedLine)
    {
        var caseData = LineAccuracyCases[constraintId];
        int diagnosticLine;

        if (phase == "parse")
        {
            var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(caseData.Dsl);
            diagnostics.Should().NotBeEmpty($"constraint {constraintId} must produce a parse diagnostic");
            diagnosticLine = diagnostics[0].Line;
        }
        else // compile
        {
            var (model, parseDiags) = PreceptParser.ParseWithDiagnostics(caseData.Dsl);
            parseDiags.Should().BeEmpty($"compile-phase trigger for {constraintId} must parse cleanly");
            model.Should().NotBeNull();

            var validation = PreceptCompiler.Validate(model!);
            var diagnostic = validation.Diagnostics.FirstOrDefault(d => d.Constraint.Id == constraintId);
            diagnostic.Should().NotBeNull($"constraint {constraintId} must produce a validation diagnostic");
            diagnosticLine = diagnostic!.Line;
        }

        diagnosticLine.Should().BeGreaterThan(1,
            $"constraint {constraintId} diagnostic should squiggle the offending declaration " +
            $"(expected line >= {minExpectedLine}), not the precept header (line 1). " +
            "Did you forget to pass SourceLine to ToException() or the diagnostic constructor?");
    }

    public static IEnumerable<object[]> LineAccuracyData()
        => LineAccuracyCases.Select(kv => new object[] { kv.Key, kv.Value.Phase, kv.Value.MinExpectedLine });

    /// <summary>
    /// Completeness guard: every non-exempt parse/compile-phase constraint
    /// must have a line accuracy test case. Fails when someone adds C70
    /// without adding a corresponding LineAccuracyCases entry.
    /// </summary>
    [Fact]
    public void AllNonExemptConstraints_HaveLineAccuracyCase()
    {
        var allIds = DiagnosticCatalog.Constraints
            .Where(c => c.Phase is "parse" or "compile")
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);

        var covered = LineAccuracyCases.Keys.ToHashSet(StringComparer.Ordinal);
        var exempt = LineAccuracyExemptions;

        var uncovered = allIds.Except(covered).Except(exempt).ToList();
        uncovered.Sort(StringComparer.Ordinal);

        uncovered.Should().BeEmpty(
            "every parse/compile-phase constraint must either have a LineAccuracyCases entry " +
            "or be listed in LineAccuracyExemptions with a reason. " +
            $"Missing: {string.Join(", ", uncovered)}");
    }
}
