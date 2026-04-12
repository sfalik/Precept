using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class LanguageTool
{
    [McpServerTool(Name = "precept_language")]
    [Description("Return the full structured language reference — vocabulary, constructs, constraints, expression scopes, fire pipeline, outcome kinds.")]
    public static LanguageResult Run()
    {
        PreceptParser.EnsureInitialized();
        var vocabulary = BuildVocabulary();
        var constructs = ConstructCatalog.Constructs
            .Select(c => new ConstructDto(c.Form, c.Context, c.Description, c.Example))
            .ToList();
        var constraints = DiagnosticCatalog.Constraints
            .Select(c => new ConstraintDto(c.Id, c.Phase, c.Rule))
            .ToList();
        var functions = BuildFunctionCatalog();

        return new LanguageResult(vocabulary, constructs, constraints, ExpressionScopes, functions, FirePipeline, OutcomeKinds);
    }

    private static VocabularyDto BuildVocabulary()
    {
        var controlKeywords = new List<string>();
        var actionKeywords = new List<string>();
        var declarationKeywords = new List<string>();
        var grammarKeywords = new List<string>();
        var outcomeKeywords = new List<string>();
        var typeKeywords = new List<string>();
        var constraintKeywords = new List<string>();
        var literalKeywords = new List<string>();
        var operators = new List<OperatorDto>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var categories = PreceptTokenMeta.GetCategories(token);
            var symbol = PreceptTokenMeta.GetSymbol(token);
            var description = PreceptTokenMeta.GetDescription(token);

            if (categories.Count == 0) continue;

            foreach (var category in categories)
            {
                switch (category)
                {
                    case TokenCategory.Control when symbol is not null:
                        controlKeywords.Add(symbol);
                        break;
                    case TokenCategory.Declaration when symbol is not null:
                        declarationKeywords.Add(symbol);
                        break;
                    case TokenCategory.Action when symbol is not null:
                        actionKeywords.Add(symbol);
                        break;
                    case TokenCategory.Outcome when symbol is not null:
                        outcomeKeywords.Add(symbol);
                        break;
                    case TokenCategory.Grammar when symbol is not null:
                        grammarKeywords.Add(symbol);
                        break;
                    case TokenCategory.Constraint when symbol is not null:
                        constraintKeywords.Add(symbol);
                        break;
                    case TokenCategory.Type when symbol is not null:
                        typeKeywords.Add(symbol);
                        break;
                    case TokenCategory.Literal when symbol is not null:
                        literalKeywords.Add(symbol);
                        break;
                    case TokenCategory.Operator when symbol is not null:
                        var (precedence, arity) = GetOperatorInfo(token);
                        operators.Add(new OperatorDto(symbol, precedence, arity, description ?? ""));
                        break;
                }
            }
        }

        return new VocabularyDto(
                    controlKeywords,
                    actionKeywords,
                    declarationKeywords,
                    grammarKeywords,
                    outcomeKeywords,
                    typeKeywords,
                    constraintKeywords,
                    literalKeywords,
                    operators);
    }

    private static IReadOnlyList<FunctionDto> BuildFunctionCatalog()
    {
        return FunctionRegistry.AllFunctions
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => new FunctionDto(
                f.Name,
                f.Description,
                f.Overloads.Select(o => new FunctionSignatureDto(
                    o.Parameters.Select(p => new FunctionParamDto(
                        p.Name,
                        FormatValueKind(p.AcceptedTypes),
                        FormatArgConstraint(p.Constraint)
                    )).ToList(),
                    FormatValueKind(o.ReturnType),
                    o.MinArity.HasValue
                )).ToList()
            ))
            .ToList();
    }

    private static string FormatValueKind(StaticValueKind kind)
    {
        var allNumeric = StaticValueKind.Number | StaticValueKind.Integer | StaticValueKind.Decimal;
        if ((kind & allNumeric) == allNumeric)
            return "number";

        var parts = new List<string>();
        if (kind.HasFlag(StaticValueKind.Number)) parts.Add("number");
        if (kind.HasFlag(StaticValueKind.Integer)) parts.Add("integer");
        if (kind.HasFlag(StaticValueKind.Decimal)) parts.Add("decimal");
        if (kind.HasFlag(StaticValueKind.String)) parts.Add("string");
        if (kind.HasFlag(StaticValueKind.Boolean)) parts.Add("boolean");
        return parts.Count > 0 ? string.Join(" | ", parts) : "unknown";
    }

    private static string? FormatArgConstraint(FunctionArgConstraint constraint) => constraint switch
    {
        FunctionArgConstraint.MustBeIntegerLiteral => "must be integer literal",
        _ => null
    };

    private static (int Precedence, string Arity) GetOperatorInfo(PreceptToken token) => token switch
    {
        PreceptToken.Or => (1, "binary"),
        PreceptToken.And => (2, "binary"),
        PreceptToken.DoubleEquals => (3, "binary"),
        PreceptToken.NotEquals => (3, "binary"),
        PreceptToken.GreaterThan => (4, "binary"),
        PreceptToken.GreaterThanOrEqual => (4, "binary"),
        PreceptToken.LessThan => (4, "binary"),
        PreceptToken.LessThanOrEqual => (4, "binary"),
        PreceptToken.Contains => (4, "binary"),
        PreceptToken.Plus => (5, "binary"),
        PreceptToken.Minus => (5, "binary"),
        PreceptToken.Star => (6, "binary"),
        PreceptToken.Slash => (6, "binary"),
        PreceptToken.Percent => (6, "binary"),
        PreceptToken.Not => (7, "unary"),
        PreceptToken.Assign => (0, "binary"),
        _ => (0, "binary")
    };

    private static readonly IReadOnlyList<ExpressionScopeDto> ExpressionScopes =
    [
        new("invariant expression", "All data fields, collection accessors"),
        new("state assert expression", "All data fields, collection accessors"),
        new("event assert expression", "That event's args only (bare ArgName or EventName.ArgName)"),
        new("when guard", "All data fields, EventName.ArgName, collection accessors"),
        new("set RHS", "All data fields (read-your-writes), EventName.ArgName, collection accessors")
    ];

    private static readonly IReadOnlyList<FirePipelineStageDto> FirePipeline =
    [
        new(1, "Event asserts", "Validate event args against 'on <Event> assert' rules. Failure → Rejected."),
        new(2, "Row selection", "Iterate transition rows for (state, event) in source order. First 'when' match wins. No match → Unmatched."),
        new(3, "Exit actions", "Run 'from <SourceState> ->' automatic mutations."),
        new(4, "Row mutations", "Execute the matched row's '-> set/add/remove/...' action chain in declaration order."),
        new(5, "Entry actions", "Run 'to <TargetState> ->' automatic mutations."),
        new(6, "Validation", "Check invariants, state asserts (in/to/from with temporal scoping). Any failure → full rollback, ConstraintFailure.")
    ];

    private static readonly IReadOnlyList<OutcomeKindDto> OutcomeKinds =
    [
        new("Transition", "Event handled, state changed.", true),
        new("NoTransition", "Event handled via 'no transition', data may change but state stays.", true),
        new("Rejected", "Event matched but explicitly rejected by the authored workflow.", false),
        new("ConstraintFailure", "Event matched but was rolled back because a constraint failed.", false),
        new("Undefined", "No transition rows exist for this event in the current state.", false),
        new("Unmatched", "Transition rows exist but no 'when' guard matched.", false)
    ];
}

public sealed record LanguageResult(
    VocabularyDto Vocabulary,
    IReadOnlyList<ConstructDto> Constructs,
    IReadOnlyList<ConstraintDto> Constraints,
    IReadOnlyList<ExpressionScopeDto> ExpressionScopes,
    IReadOnlyList<FunctionDto> Functions,
    IReadOnlyList<FirePipelineStageDto> FirePipeline,
    IReadOnlyList<OutcomeKindDto> OutcomeKinds);

public sealed record VocabularyDto(
    IReadOnlyList<string> ControlKeywords,
    IReadOnlyList<string> ActionKeywords,
    IReadOnlyList<string> DeclarationKeywords,
    IReadOnlyList<string> GrammarKeywords,
    IReadOnlyList<string> OutcomeKeywords,
    IReadOnlyList<string> TypeKeywords,
    IReadOnlyList<string> ConstraintKeywords,
    IReadOnlyList<string> LiteralKeywords,
    IReadOnlyList<OperatorDto> Operators);

public sealed record OperatorDto(string Symbol, int Precedence, string Arity, string Description);
public sealed record ConstructDto(string Form, string Context, string Description, string Example);
public sealed record ConstraintDto(string Id, string Phase, string Rule);
public sealed record ExpressionScopeDto(string Position, string Allowed);
public sealed record FunctionDto(string Name, string Description, IReadOnlyList<FunctionSignatureDto> Signatures);
public sealed record FunctionSignatureDto(IReadOnlyList<FunctionParamDto> Parameters, string ReturnType, bool IsVariadic);
public sealed record FunctionParamDto(string Name, string Type, string? Constraint);
public sealed record FirePipelineStageDto(int Stage, string Name, string Description);
public sealed record OutcomeKindDto(string Kind, string Description, bool Mutated);
