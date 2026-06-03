using System;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogCapability;

public sealed class ModifierCatalogCapabilityTests
{
    // ── Axis-disjointness (the live guard mirroring Precept0029ModifierAxisDisjoint) ──
    //
    // No value modifier's ApplicableTo may span both a collection kind and a scalar kind.
    // The Roslyn analyzer enforces this at C# compile time over the GetMeta source; this
    // test asserts the same invariant over the live catalog (Modifiers.All), classifying a
    // TypeKind as a collection via the catalog's own TypeCategory.Collection — never a
    // parallel list.

    private static bool IsCollectionKind(TypeKind kind)
        => Types.GetMeta(kind).Category == TypeCategory.Collection;

    [Fact]
    public void NoValueModifierApplicableTo_SpansBothCollectionAndScalarKinds()
    {
        foreach (var meta in Modifiers.All.OfType<ValueModifierMeta>())
        {
            var kinds = meta.ApplicableTo
                .Where(t => t.Kind is not null)
                .Select(t => t.Kind!.Value)
                .ToList();

            // AnyType (empty ApplicableTo) is exempt — optional/default apply to all types
            // as presence/value modifiers, not as a value-bound axis overlap.
            if (kinds.Count == 0)
                continue;

            var hasCollection = kinds.Any(IsCollectionKind);
            var hasScalar = kinds.Any(k => !IsCollectionKind(k));

            (hasCollection && hasScalar).Should().BeFalse(
                $"ModifierKind.{meta.Kind} ApplicableTo must name only one axis — use mincount/maxcount " +
                $"for collection cardinality and a scalar modifier for element/value constraints");
        }
    }

    // ── Arm-shape coverage for element-routable modifiers ────────────────────────
    //
    // BuildElementValueBounds binds a routed element modifier by matching its
    // ProofSatisfactions against four known shapes (length bound, numeric bound,
    // non-empty flag, sign flag). Every element-routable modifier (those in
    // ElementPositionValueTokens) must carry at least one Numeric satisfaction matching
    // one of those four shapes, so none is silently dropped (bound to nothing).

    private static bool MatchesKnownElementBoundShape(ProofSatisfaction.Numeric s)
        => (s.Projection, s.Bound) switch
        {
            // Length bound: Accessor("length") <= / >= DeclarationValue → minlength/maxlength.
            (SatisfactionProjection.Accessor { Name: "length" }, NumericBoundSource.DeclarationValue)
                => s.Comparison is OperatorKind.GreaterThanOrEqual or OperatorKind.LessThanOrEqual,

            // Numeric bound: SelfValue <= / >= DeclarationValue → declared min/max.
            (SatisfactionProjection.SelfValue, NumericBoundSource.DeclarationValue)
                => s.Comparison is OperatorKind.GreaterThanOrEqual or OperatorKind.LessThanOrEqual,

            // Non-empty: Accessor("length"|"count") > Constant(0) → NotEmpty.
            (SatisfactionProjection.Accessor { Name: "length" or "count" }, NumericBoundSource.Constant { Value: 0m })
                => s.Comparison is OperatorKind.GreaterThan,

            // Sign flag: SelfValue >=/>/!= Constant → numeric flag.
            (SatisfactionProjection.SelfValue, NumericBoundSource.Constant)
                => s.Comparison is OperatorKind.GreaterThanOrEqual
                                or OperatorKind.GreaterThan
                                or OperatorKind.NotEquals,

            _ => false,
        };

    [Fact]
    public void EveryElementRoutableModifier_MatchesAKnownElementBoundShape()
    {
        // Two element-routable modifiers are NOT value bounds that BuildElementValueBounds binds,
        // so they legitimately carry no Numeric ProofSatisfaction and are exempt from this guard:
        //  - `ordered` — a structural ordering qualifier on a `choice` inner type, carried by the
        //    choice-element path (flows through accessors so `.min`/`.max`/`.first`/`.last` discharge).
        //  - `maxplaces` — a decimal-scale precision constraint, handled by `ValidateMaxPlaces`, not
        //    the value-bound carrier (per the collection-inner-type-value-modifiers design).
        // The guard then asserts every element-routable *value-bound* modifier maps to a known shape —
        // so a future value modifier added to the routable set without a handled shape fails loudly.
        var nonValueBound = new[] { ModifierKind.Ordered, ModifierKind.Maxplaces };
        var elementRoutable = Modifiers.All
            .OfType<ValueModifierMeta>()
            .Where(m => Modifiers.ElementPositionValueTokens.Contains(m.Token.Kind))
            .Where(m => !nonValueBound.Contains(m.Kind))
            .ToList();

        // Guard: the set is non-empty (the routing derivation actually populates it).
        elementRoutable.Should().NotBeEmpty();

        foreach (var meta in elementRoutable)
        {
            var numeric = meta.ProofSatisfactions.OfType<ProofSatisfaction.Numeric>().ToList();

            numeric.Should().NotBeEmpty(
                $"ModifierKind.{meta.Kind} is element-routable but carries no Numeric ProofSatisfaction — " +
                "BuildElementValueBounds would silently bind no element bound for it");

            numeric.Should().OnlyContain(
                s => MatchesKnownElementBoundShape(s),
                $"every Numeric ProofSatisfaction on element-routable ModifierKind.{meta.Kind} must match one of " +
                "the four element-bound shapes BuildElementValueBounds handles (length / numeric / non-empty / sign)");
        }
    }

    [Fact]
    public void ValueModifierMeta_UsesCanonicalTypeName()
        => ValueModifierTestAccess.RuntimeTypeName(ModifierKind.Default)
            .Should().Be("ValueModifierMeta");

    [Fact]
    public void ValueModifierMeta_NoLongerExposesApplicableToEventArgs()
        => ValueModifierTestAccess.GetMeta(ModifierKind.Default).RuntimeInstance.GetType()
            .GetProperty("ApplicableToEventArgs")
            .Should().BeNull();

    [Fact]
    public void ValueModifierMeta_ExposesDeclarationSiteApplicabilityShape()
    {
        var property = ValueModifierTestAccess.FindDeclarationSiteProperty(
            ValueModifierTestAccess.GetMeta(ModifierKind.Default));
        var names = Enum.GetNames(property!.PropertyType);

        property.Should().NotBeNull();
        property.PropertyType.IsEnum.Should().BeTrue();
        property.PropertyType.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Should().NotBeEmpty();
        names.Should().Contain("FieldDeclaration");
        (Array.IndexOf(names, "EventArgument") >= 0 || Array.IndexOf(names, "EventArgDeclaration") >= 0)
            .Should().BeTrue();
    }

    [Fact]
    public void Min_BoundCounterpart_IsMax()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Min), "BoundCounterpart")
            .Should().Be(ModifierKind.Max);

    [Fact]
    public void Max_BoundCounterpart_IsMin()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Max), "BoundCounterpart")
            .Should().Be(ModifierKind.Min);

    [Fact]
    public void Minlength_BoundCounterpart_IsMaxlength()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Minlength), "BoundCounterpart")
            .Should().Be(ModifierKind.Maxlength);

    [Fact]
    public void Mincount_BoundCounterpart_IsMaxcount()
        => CatalogCapabilityReflection.GetInstanceValue(
                Modifiers.GetMeta(ModifierKind.Mincount), "BoundCounterpart")
            .Should().Be(ModifierKind.Maxcount);

    [Fact]
    public void Default_IncludesEventArgumentDeclarations()
    {
        var meta = ValueModifierTestAccess.GetMeta(ModifierKind.Default);
        ValueModifierTestAccess.HasDeclarationSiteFlag(meta, "FieldDeclaration").Should().BeTrue();
        ValueModifierTestAccess.HasAnyDeclarationSiteFlag(meta, "EventArgument", "EventArgDeclaration").Should().BeTrue();
    }
}
