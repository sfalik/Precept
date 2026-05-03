using System.Text.Json;
using System.Text.Json.Serialization;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;

var source = Console.In.ReadToEnd();

// Only Lexer and Parser are implemented; TypeChecker/GraphAnalyzer/ProofEngine are stubs.
var tokens = Lexer.Lex(source);
var manifest = Parser.Parse(tokens);

var allDiagnostics = tokens.Diagnostics.Concat(manifest.Diagnostics).ToArray();

static object SerializeSpan(SourceSpan s) => new
{
    offset = s.Offset, length = s.Length,
    startLine = s.StartLine, startColumn = s.StartColumn,
    endLine = s.EndLine, endColumn = s.EndColumn
};

static object SerializeDiag(Diagnostic d) => new
{
    severity = d.Severity.ToString(),
    stage    = d.Stage.ToString(),
    code     = d.Code,
    message  = d.Message,
    span     = SerializeSpan(d.Span)
};

static object SerializeTypeRef(TypeRefNode t) => t switch
{
    ScalarTypeRefNode s     => new { kind = "scalar", typeName = s.TypeName.Text, qualifiers = s.Qualifiers.Select(q => q.Value?.ToString()).ToArray() },
    CollectionTypeRefNode c => new { kind = "collection", collectionKind = c.CollectionKind.Text, elementType = c.ElementType.Text },
    ChoiceTypeRefNode ch    => new { kind = "choice", options = ch.Options.Select(o => o.ToString()).ToArray() },
    _                       => new { kind = t.GetType().Name }
};

static object SerializeOutcome(OutcomeNode o) => o switch
{
    TransitionOutcomeNode t => new { kind = "transition", targetState = t.TargetState.Text },
    NoTransitionOutcomeNode => new { kind = "noTransition" },
    RejectOutcomeNode r     => new { kind = "reject", message = r.Message.ToString() },
    _                       => new { kind = o.GetType().Name }
};

static object SerializeDeclaration(Declaration d) => d switch
{
    FieldDeclarationNode f      => new
    {
        kind      = "field",
        names     = f.Names.Select(n => n.Text).ToArray(),
        type      = SerializeTypeRef(f.Type),
        modifiers = f.Modifiers.Select(m => m.ToString()).ToArray(),
        computed  = f.ComputedExpression?.ToString()
    },
    StateDeclarationNode s      => new
    {
        kind    = "state",
        entries = s.Entries.Select(e => new
        {
            name      = e.Name.Text,
            modifiers = e.Modifiers.Select(m => m.Text).ToArray()
        }).ToArray()
    },
    EventDeclarationNode e      => new
    {
        kind      = "event",
        names     = e.Names.Select(n => n.Text).ToArray(),
        arguments = e.Arguments.Select(a => new
        {
            name      = a.Name.Text,
            type      = SerializeTypeRef(a.Type),
            modifiers = a.Modifiers.Select(m => m.ToString()).ToArray()
        }).ToArray(),
        isInitial = e.IsInitial
    },
    StateEnsureNode se          => new
    {
        kind         = "stateEnsure",
        preposition  = se.Preposition.Text,
        state        = se.State.ToString(),
        condition    = se.Condition.ToString(),
        message      = se.Message.ToString()
    },
    EventEnsureNode ee          => new
    {
        kind      = "eventEnsure",
        eventName = ee.EventName.Text,
        condition = ee.Condition.ToString(),
        message   = ee.Message.ToString()
    },
    StateActionNode sa          => new
    {
        kind        = "stateAction",
        preposition = sa.Preposition.Text,
        state       = sa.State.ToString(),
        actions     = sa.Actions.Select(a => a.ToString()).ToArray()
    },
    TransitionRowNode tr        => new
    {
        kind       = "transitionRow",
        fromState  = tr.FromState.ToString(),
        eventName  = tr.EventName.Text,
        guard      = tr.Guard?.ToString(),
        actions    = tr.Actions.Select(a => a.ToString()).ToArray(),
        outcome    = SerializeOutcome(tr.Outcome)
    },
    RuleDeclarationNode r       => new
    {
        kind      = "rule",
        condition = r.Condition.ToString(),
        guard     = r.Guard?.ToString(),
        message   = r.Message.ToString()
    },
    _                           => new { kind = d.GetType().Name }
};

var result = new
{
    pipelineStagesRun    = new[] { "Lex", "Parse" },
    notYetImplemented    = new[] { "TypeCheck", "GraphAnalyze", "ProofEngine" },
    hasErrors            = allDiagnostics.Any(d => d.Severity == Severity.Error),
    diagnosticCount      = allDiagnostics.Length,
    diagnostics          = allDiagnostics.Select(SerializeDiag).ToArray(),
    syntaxTree = new
    {
        preceptName      = manifest.Header?.Name.Text,
        declarationCount = manifest.Declarations.Length,
        declarations     = manifest.Declarations.Select(SerializeDeclaration).ToArray()
    }
};

Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
{
    WriteIndented        = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
}));
