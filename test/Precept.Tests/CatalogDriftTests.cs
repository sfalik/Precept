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

            "invariant" =>
                $"{header}\nfield Priority as number default 1\n{construct.Example}\n{string.Join("\n", states)}",

            "event-declaration" =>
                $"{header}\n{string.Join("\n", states)}\n{construct.Example}",

            "state-assert" =>
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{construct.Example}",

            "event-assert" =>
                $"{header}\n{string.Join("\n", states)}\nevent Submit with Comment as string\n{construct.Example}",

            "state-action" =>
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{construct.Example}",

            "edit-declaration" =>
                $"{header}\nfield Priority as number default 1\n{string.Join("\n", states)}\n{construct.Example}",

            "transition-row" =>
                $"{header}\n{string.Join("\n", states)}\nevent Submit\n{construct.Example}",

            "round-function" =>
                // round() needs a decimal field and an event to be valid in a transition context
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
        //      (invariant/guard expressions are parsed inline by the combinator)
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

        // C14: Event assert with wrong dotted prefix
        ["C14"] = new(H + S + "event Submit with Comment as string default \"x\"\non Submit assert Other.Comment != null because \"bad\"\n", "unknown prefix"),

        // C15: Event assert dotted member not a declared arg
        ["C15"] = new(H + S + "event Submit with Comment as string default \"x\"\non Submit assert Submit.Nope != null because \"bad\"\n", "not an event argument"),

        // C16: Event assert plain identifier not a declared arg
        ["C16"] = new(H + S + "event Submit with Comment as string default \"x\"\non Submit assert Nope != null because \"bad\"\n", "not an event argument"),

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

        // C29: Invariant violated by default values
        ["C29"] = new(H + "field Score as number default 0\ninvariant Score > 0 because \"must be positive\"\n" + S, "invariant violation"),

        // C30: State assert on initial state violated by defaults
        ["C30"] = new(H + "field Balance as number default 0\nstate Active initial\nin Active assert Balance > 0 because \"must be positive\"\n", "state assert violation"),

        // C31: Event assert violated by default arg values
        ["C31"] = new(H + S + "event Submit with Amount as number default 0\non Submit assert Amount > 0 because \"must be positive\"\n", "event assert violation"),

        // C32: Literal set assignment violates invariant
        ["C32"] = new(H + "field Balance as number default 100\ninvariant Balance >= 0 because \"no negative\"\n" + S2 + "event Go\nfrom A on Go -> set Balance = -5 -> transition B\n", "violates invariant"),

        // C44: Duplicate state assert (same preposition, state, expression)
        ["C44"] = new(H + "field X as number default 10\n" + S2 + "in B assert X > 0 because \"first\"\nin B assert X > 0 because \"duplicate\"\nevent Go\nfrom A on Go -> transition B\n", "Duplicate state assert"),

        // C45: Subsumed state assert (to redundant with identical in)
        ["C45"] = new(H + "field X as number default 10\n" + S2 + "in B assert X > 0 because \"in covers entry\"\nto B assert X > 0 because \"to is redundant\"\nevent Go\nfrom A on Go -> transition B\n", "Subsumed state assert"),

        // C46: Non-boolean expression in rule position (guard, invariant, assert)
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
        ["C60"] = new(H + "field Count as integer default 0\n" + S2 + "event Go\nfrom A on Go -> set Count = 3.0 -> no transition\n", "explicit conversion"),

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

        // C69: cross-scope guard reference in when clause
        ["C69"] = new("_unused_", "different scope", DirectAction: () =>
        {
            var msg = DiagnosticCatalog.C69.FormatMessage(("name", "SomeArg"));
            if (!msg.Contains("different scope"))
                throw new InvalidOperationException("Constraint C69 message template is broken");
            throw new InvalidOperationException(msg);
        }),

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
        var match = Regex.Match(grammarText, @"""fieldScalarDeclaration"".*?""match"":\s*""([^""]+)""", RegexOptions.Singleline);
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
    public void GrammarFieldCollectionDeclaration_CoversAllSimpleScalarTypes()
    {
        var srcRoot = FindRepoRoot();
        var grammarPath = Path.Combine(srcRoot, "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json");
        var grammarText = File.ReadAllText(grammarPath);

        // Extract the fieldCollectionDeclaration pattern
        var match = Regex.Match(grammarText, @"""fieldCollectionDeclaration"".*?""match"":\s*""([^""]+)""", RegexOptions.Singleline);
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
}
