using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class ProofEngineSlice7ObligationCollectionTests
{
    [Fact]
    public void ObligationCollection_MoneyFieldWithBounds_GeneratesIntervalObligation()
    {
        var field = MakeBoundedField("Amount", TypeKind.Money);

        CountIntervalContainmentObligations(field).Should().Be(1);
    }

    [Fact]
    public void ObligationCollection_QuantityFieldWithBounds_GeneratesIntervalObligation()
    {
        var field = MakeBoundedField("Quantity", TypeKind.Quantity);

        CountIntervalContainmentObligations(field).Should().Be(1);
    }

    [Fact]
    public void ObligationCollection_PriceFieldWithBounds_GeneratesIntervalObligation()
    {
        var field = MakeBoundedField("UnitPrice", TypeKind.Price);

        CountIntervalContainmentObligations(field).Should().Be(1);
    }

    private static int CountIntervalContainmentObligations(TypedField field)
    {
        var action = new TypedInputAction(
            ActionKind.Set,
            field.Name,
            field.ResolvedType,
            new TypedFieldRef(field.ResolvedType, field.Name, false, null, SourceSpan.Missing),
            null,
            null,
            ImmutableArray<ProofRequirement>.Empty,
            SourceSpan.Missing);

        var row = new TypedTransitionRowSuccess
        {
            FromState = null,
            EventName = "Update",
            TargetState = null,
            Guard = null,
            Actions = [action],
            ResultQualifier = null,
            RowSpan = SourceSpan.Missing,
            Syntax = MakeSyntax(ConstructKind.TransitionRow),
        };

        var semantics = SemanticIndex.Empty with
        {
            Fields = [field],
            FieldsByName = (new[] { field }).ToFrozenDictionary(f => f.Name),
            TransitionRows = [row]
        };

        var ledger = ProofEngine.Prove(semantics, StateGraph.Empty);
        return ledger.Obligations.Count(o => o.Requirement.Kind == ProofRequirementKind.IntervalContainment);
    }

    private static TypedField MakeBoundedField(string name, TypeKind type)
        => new(
            Name: name,
            ResolvedType: type,
            ElementType: null,
            KeyType: null,
            Modifiers: [ModifierKind.Min, ModifierKind.Max],
            ImpliedModifiers: ImmutableArray<ModifierKind>.Empty,
            DefaultExpression: null,
            ComputedExpression: null,
            Qualifier: null,
            IsComputed: false,
            IsOptional: false,
            IsWritable: true,
            Presence: new DeclaredPresenceMeta.Guaranteed(),
            DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty,
            NameSpan: SourceSpan.Missing,
            Syntax: MakeSyntax(ConstructKind.FieldDeclaration),
            DeclaredMin: 0m,
            DeclaredMax: 1000m);

    private static ParsedConstruct MakeSyntax(ConstructKind kind)
        => new(Constructs.GetMeta(kind), ImmutableArray<SlotValue>.Empty, SourceSpan.Missing);
}
