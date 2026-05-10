using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class ProofLedgerTests
{
    private static ParsedConstruct SyntaxFor(ConstructKind kind)
        => new(Constructs.GetMeta(kind), ImmutableArray<SlotValue>.Empty, SourceSpan.Missing);

    private static TypedTransitionRow CreateRow()
        => new(
            FromState: "Open",
            EventName: "Submit",
            TargetState: null,
            Guard: null,
            Actions: ImmutableArray<TypedAction>.Empty,
            Outcome: TransitionRowOutcome.NoTransition,
            RejectReason: null,
            ResultQualifier: null,
            Syntax: SyntaxFor(ConstructKind.TransitionRow));

    private static TypedField CreateField()
        => new(
            Name: "Amount",
            ResolvedType: TypeKind.Integer,
            ElementType: null,
            KeyType: null,
            Modifiers: ImmutableArray<ModifierKind>.Empty,
            ImpliedModifiers: ImmutableArray<ModifierKind>.Empty,
            DefaultExpression: null,
            ComputedExpression: null,
            Qualifier: null,
            IsComputed: false,
            IsOptional: false,
            IsWritable: false,
            Presence: new DeclaredPresenceMeta.Guaranteed(),
            DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty,
            NameSpan: SourceSpan.Missing,
            Syntax: SyntaxFor(ConstructKind.FieldDeclaration));

    private static TypedStateHook CreateStateHook()
        => new(
            Scope: AnchorScope.InState,
            StateName: "Open",
            Guard: null,
            Actions: ImmutableArray<TypedAction>.Empty,
            Syntax: SyntaxFor(ConstructKind.StateAction));

    private static TypedEventHandler CreateEventHandler()
        => new(
            EventName: "Submit",
            Actions: ImmutableArray<TypedAction>.Empty,
            Syntax: SyntaxFor(ConstructKind.EventHandler));

    [Fact]
    public void ObligationContext_TransitionRowContext_HoldsRow()
    {
        var row = CreateRow();
        var context = new TransitionRowContext(row);

        context.Row.Should().BeSameAs(row);
    }

    [Fact]
    public void ObligationContext_ConstraintContext_HoldsIdentity()
    {
        var identity = new RuleIdentity(1);
        var context = new ConstraintContext(identity);

        context.Constraint.Should().BeSameAs(identity);
    }

    [Fact]
    public void ObligationContext_AllFiveSubtypes_AreDistinct()
    {
        ObligationContext[] contexts =
        [
            new TransitionRowContext(CreateRow()),
            new ConstraintContext(new RuleIdentity(1)),
            new StateHookContext(CreateStateHook()),
            new EventHandlerContext(CreateEventHandler()),
            new FieldExpressionContext(CreateField()),
        ];

        var kinds = contexts.Select(context => context switch
        {
            TransitionRowContext => nameof(TransitionRowContext),
            ConstraintContext => nameof(ConstraintContext),
            StateHookContext => nameof(StateHookContext),
            EventHandlerContext => nameof(EventHandlerContext),
            FieldExpressionContext => nameof(FieldExpressionContext),
            _ => throw new Xunit.Sdk.XunitException("Unexpected obligation context subtype"),
        });

        kinds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ProofObligation_IncludesContext()
    {
        var site = new TypedLiteral(TypeKind.Integer, 1m, SourceSpan.Missing);
        var context = new FieldExpressionContext(CreateField());
        var obligation = new ProofObligation(
            Requirement: new NumericProofRequirement(new SelfSubject(), OperatorKind.GreaterThan, 0m, "test"),
            Site: site,
            Context: context,
            Disposition: ProofDisposition.Proved,
            Strategy: ProofStrategy.Literal,
            EmittedDiagnostic: null);

        obligation.Context.Should().BeSameAs(context);
        obligation.Site.Should().BeSameAs(site);
    }
}
