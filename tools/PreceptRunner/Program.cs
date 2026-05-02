using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;

var dsl = args.Length > 0 ? args[0] : Console.In.ReadToEnd();
var compilation = Compiler.Compile(dsl);

var opts = new JsonSerializerOptions { WriteIndented = true };

var result = new
{
    hasErrors = compilation.HasErrors,
    diagnostics = compilation.Diagnostics.Select(d => new
    {
        severity = d.Severity.ToString(),
        stage    = d.Stage.ToString(),
        code     = d.Code,
        message  = d.Message,
        span     = new { start = d.Span.Start, end = d.Span.End, length = d.Span.Length }
    }).ToArray(),
    header = compilation.SyntaxTree.Header is { } h ? new { name = h.Name.Text } : null,
    fields = compilation.SyntaxTree.Declarations
        .OfType<FieldDeclarationNode>()
        .Select(f => new
        {
            names     = f.Names.Select(n => n.Text).ToArray(),
            type      = SerializeTypeRef(f.Type),
            modifiers = f.Modifiers.Select(SerializeModifier).ToArray(),
            computed  = f.ComputedExpression != null ? ExprText(f.ComputedExpression) : null
        }).ToArray(),
    states = compilation.SyntaxTree.Declarations
        .OfType<StateDeclarationNode>()
        .SelectMany(s => s.Entries)
        .Select(e => new
        {
            name      = e.Name.Text,
            modifiers = e.Modifiers.Select(m => m.Text).ToArray()
        }).ToArray(),
    events = compilation.SyntaxTree.Declarations
        .OfType<EventDeclarationNode>()
        .SelectMany(ed => ed.Names.Select(n => new
        {
            name      = n.Text,
            arguments = ed.Arguments.Select(a => new
            {
                name      = a.Name.Text,
                type      = SerializeTypeRef(a.Type),
                modifiers = a.Modifiers.Select(SerializeModifier).ToArray()
            }).ToArray(),
            isInitial = ed.IsInitial
        })).ToArray(),
    rules = compilation.SyntaxTree.Declarations
        .OfType<RuleDeclarationNode>()
        .Select(r => new
        {
            condition = ExprText(r.Condition),
            guard     = r.Guard != null ? ExprText(r.Guard) : null,
            message   = ExprText(r.Message)
        }).ToArray(),
    stateEnsures = compilation.SyntaxTree.Declarations
        .OfType<StateEnsureNode>()
        .Select(e => new
        {
            preposition = e.Preposition.Text,
            state       = e.State.Names.Select(n => n.Text).ToArray(),
            guard       = e.Guard != null ? ExprText(e.Guard) : null,
            condition   = ExprText(e.Condition),
            message     = ExprText(e.Message)
        }).ToArray(),
    stateActions = compilation.SyntaxTree.Declarations
        .OfType<StateActionNode>()
        .Select(a => new
        {
            preposition = a.Preposition.Text,
            state       = a.State.Names.Select(n => n.Text).ToArray(),
            guard       = a.Guard != null ? ExprText(a.Guard) : null,
            actions     = a.Actions.Select(SerializeStatement).ToArray()
        }).ToArray(),
    accessModes = compilation.SyntaxTree.Declarations
        .OfType<AccessModeNode>()
        .Select(am => new
        {
            state  = am.State.Names.Select(n => n.Text).ToArray(),
            fields = am.Fields.Names.Select(n => n.Text).ToArray(),
            mode   = am.Mode.Text,
            guard  = am.Guard != null ? ExprText(am.Guard) : null
        }).ToArray(),
    eventEnsures = compilation.SyntaxTree.Declarations
        .OfType<EventEnsureNode>()
        .Select(e => new
        {
            eventName = e.EventName.Text,
            guard     = e.Guard != null ? ExprText(e.Guard) : null,
            condition = ExprText(e.Condition),
            message   = ExprText(e.Message)
        }).ToArray(),
    transitions = compilation.SyntaxTree.Declarations
        .OfType<TransitionRowNode>()
        .Select(t => new
        {
            fromState = t.FromState.Names.Select(n => n.Text).ToArray(),
            onEvent   = t.EventName.Text,
            guard     = t.Guard != null ? ExprText(t.Guard) : null,
            actions   = t.Actions.Select(SerializeStatement).ToArray(),
            outcome   = SerializeOutcome(t.Outcome)
        }).ToArray()
};

Console.WriteLine(JsonSerializer.Serialize(result, opts));

static object SerializeTypeRef(TypeRefNode t) => t switch
{
    ScalarTypeRefNode s     => (object)new { kind = "scalar", typeName = s.TypeName.Text, qualifiers = s.Qualifiers.Select(q => new { keyword = q.Keyword.Text, value = ExprText(q.Value) }).ToArray() },
    CollectionTypeRefNode c => new { kind = c.CollectionKind.Text, elementType = c.ElementType.Text, caseInsensitive = c.CaseInsensitive },
    ChoiceTypeRefNode ch    => new { kind = "choice", elementType = ch.ElementType?.Text, options = ch.Options.Select(ExprText).ToArray() },
    _                       => new { kind = t.GetType().Name, typeName = (string?)null, qualifiers = Array.Empty<object>() }
};

static object SerializeStatement(Statement s) => s switch
{
    SetStatement st    => new { kind = "set",    field = st.Field.Text, value = ExprText(st.Value) },
    AddStatement st    => new { kind = "add",    field = st.Field.Text, value = ExprText(st.Value) },
    RemoveStatement st => new { kind = "remove", field = st.Field.Text, value = ExprText(st.Value) },
    ClearStatement st  => new { kind = "clear",  field = st.Field.Text, value = (string?)null },
    EnqueueStatement st => new { kind = "enqueue", field = st.Field.Text, value = ExprText(st.Value) },
    DequeueStatement st => new { kind = "dequeue", field = st.Field.Text, value = st.IntoField?.Text },
    PushStatement st   => new { kind = "push",   field = st.Field.Text, value = ExprText(st.Value) },
    PopStatement st    => new { kind = "pop",    field = st.Field.Text, value = st.IntoField?.Text },
    _                  => new { kind = s.GetType().Name, field = (string?)null, value = (string?)null }
};

static object SerializeOutcome(OutcomeNode o) => o switch
{
    TransitionOutcomeNode t  => new { kind = "transition", target = t.TargetState.Text },
    NoTransitionOutcomeNode  => new { kind = "noTransition", target = (string?)null },
    RejectOutcomeNode r      => new { kind = "reject", target = ExprText(r.Message) },
    _                        => new { kind = o.GetType().Name, target = (string?)null }
};

static string ExprText(Expression e) => e switch
{
    LiteralExpression l              => l.Token.Text,
    IdentifierExpression id          => id.Token.Text,
    BinaryExpression b               => $"{ExprText(b.Left)} {b.Operator.Text} {ExprText(b.Right)}",
    UnaryExpression u                => $"{u.Operator.Text}{ExprText(u.Operand)}",
    MemberAccessExpression m         => $"{ExprText(m.Target)}.{m.Member.Text}",
    MethodCallExpression mc          => $"{ExprText(mc.Target)}.{mc.Method.Text}({string.Join(", ", mc.Arguments.Select(ExprText))})",
    CallExpression c                 => $"{c.FunctionName.Text}({string.Join(", ", c.Arguments.Select(ExprText))})",
    ParenthesizedExpression p        => $"({ExprText(p.Inner)})",
    IsSetExpression s                => $"{ExprText(s.Target)} is set",
    IsNotSetExpression s             => $"{ExprText(s.Target)} is not set",
    ConditionalExpression c          => $"{ExprText(c.Condition)} ? {ExprText(c.WhenTrue)} : {ExprText(c.WhenFalse)}",
    _                                => e.GetType().Name
};
