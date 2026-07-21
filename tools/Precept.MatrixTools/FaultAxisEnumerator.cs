using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Precept.Language;

namespace Precept.MatrixTools;

/// <summary>
/// One catalog-declared proof-requirement site: a single <see cref="ProofRequirement"/>
/// value attached to a catalog entry (an operation, a function overload, an action, or a
/// type accessor). This is the denominator the obligation-discharge matrix's fault family
/// must cover — every safety precondition the language declares up front.
/// </summary>
/// <param name="SiteId">Stable path-style identifier, e.g. <c>op/IntegerDivideInteger/numeric-0</c>.</param>
/// <param name="CatalogSource">Which catalog file declares it: Operations.cs / Functions.cs / Actions.cs / Types.cs.</param>
/// <param name="DeclaringEntry">The catalog entry as authors encounter it (operator form, function signature, action verb, accessor).</param>
/// <param name="RequirementKind">The <see cref="ProofRequirementKind"/> name.</param>
/// <param name="Subject">What the requirement is about — a named parameter, or the receiver (optionally via an accessor).</param>
/// <param name="Condition">The safety condition in plain words, taken from the catalog entry's own description.</param>
/// <param name="ApplicableTypeFamilies">The operand / receiver types the entry applies to.</param>
/// <param name="Notes">Anything the row needs beyond the fields above — notably the condition under which the requirement is declared.</param>
public sealed record FaultAxisSite(
    string SiteId,
    string CatalogSource,
    string DeclaringEntry,
    string RequirementKind,
    string Subject,
    string Condition,
    ImmutableArray<string> ApplicableTypeFamilies,
    string Notes);

/// <summary>
/// Walks the four requirement-declaring catalogs — <see cref="Operations"/>,
/// <see cref="Functions"/>, <see cref="Actions"/>, <see cref="Types"/> — and emits every
/// <see cref="ProofRequirement"/> they declare as a <see cref="FaultAxisSite"/>.
///
/// This is a thin walk, not a re-declaration: the requirement kinds, subjects, and
/// descriptions all come off the catalog entries themselves. Adding a requirement to a
/// catalog entry changes this output with no edit here. The one thing this file does own
/// is the rendering of a <see cref="ProofSubject"/> into readable text, because the
/// subject types carry object references (a shared <see cref="ParameterMeta"/>, a
/// <see cref="TypeAccessor"/>) rather than display strings.
///
/// What it deliberately does NOT cover: obligations constructed in pipeline code rather
/// than declared on a catalog entry (optional-field presence, default-value bounds,
/// count containment, the action-metadata dynamic generator). Those have no catalog entry
/// to walk, so a catalog walk cannot see them; they are constructed in
/// <c>src/Precept/Pipeline/ProofEngine.cs</c>, <c>ProofEngine.Analysis.cs</c>,
/// <c>Actions.cs</c>'s obligation-generator helpers, and
/// <c>TypeChecker.Expressions.AssignmentQualifiers.cs</c>.
/// </summary>
public static class FaultAxisEnumerator
{
    /// <summary>
    /// Every catalog-declared proof-requirement site, in catalog order:
    /// Operations, then Functions, then Actions, then Types.
    /// </summary>
    public static ImmutableArray<FaultAxisSite> Enumerate()
    {
        var sites = ImmutableArray.CreateBuilder<FaultAxisSite>();
        sites.AddRange(FromOperations());
        sites.AddRange(FromFunctions());
        sites.AddRange(FromActions());
        sites.AddRange(FromTypes());
        return sites.ToImmutable();
    }

    // ── Operations ──────────────────────────────────────────────────────────────

    private static IEnumerable<FaultAxisSite> FromOperations()
    {
        foreach (var meta in Operations.All)
        {
            // Only BinaryOperationMeta carries a ProofRequirements slot; the unary subtype
            // has no such slot at all, so unary operations declare no requirement sites.
            if (meta is not BinaryOperationMeta b)
                continue;

            var entry = $"{b.Lhs.Kind.ToString().ToLowerInvariant()} {OperatorText(b.Op)} {b.Rhs.Kind.ToString().ToLowerInvariant()}";
            var families = ImmutableArray.Create(b.Lhs.Kind.ToString(), b.Rhs.Kind.ToString());
            var paramNames = Positions(b);

            var index = 0;
            foreach (var req in b.ProofRequirements)
            {
                yield return new FaultAxisSite(
                    SiteId: $"op/{b.Kind}/{KindSlug(req.Kind)}-{index++}",
                    CatalogSource: "Operations.cs",
                    DeclaringEntry: $"{b.Kind} — {entry}",
                    RequirementKind: req.Kind.ToString(),
                    Subject: DescribeSubject(req, paramNames),
                    Condition: req.Description,
                    ApplicableTypeFamilies: families,
                    Notes: OperationNotes(b, req));
            }
        }
    }

    /// <summary>
    /// The operand slots of a binary entry, labelled for display. When both operand slots share
    /// one <see cref="ParameterMeta"/> instance (every same-type entry does — the catalog reuses
    /// one instance per TypeKind), a <c>ParamSubject</c> cannot distinguish them by object
    /// identity; the label says so rather than silently picking a side.
    /// </summary>
    private static ImmutableArray<(ParameterMeta Param, string Name)> Positions(BinaryOperationMeta b) =>
        ReferenceEquals(b.Lhs, b.Rhs)
            ? [(b.Lhs, "an operand of this entry (both slots share one ParameterMeta instance)")]
            : [(b.Lhs, "left operand"), (b.Rhs, "right operand")];

    private static string OperationNotes(BinaryOperationMeta bin, ProofRequirement req)
    {
        var parts = new List<string>();

        if (bin.Match != QualifierMatch.Any)
            parts.Add($"catalog entry is selected only when the operands' qualifiers are {bin.Match.ToString().ToLowerInvariant()} "
                + "(QualifierMatch disambiguation), so the requirement applies only on that overload");
        if (bin.BidirectionalLookup && bin.Lhs.Kind != bin.Rhs.Kind)
            parts.Add("entry is indexed in both operand orders, so the requirement applies whichever side the author writes first");
        if (ReferenceEquals(bin.Lhs, bin.Rhs))
            parts.Add(req is QualifierCompatibilityProofRequirement or QualifierChainProofRequirement or DimensionalProductProofRequirement
                ? "both operand slots share one ParameterMeta instance; the requirement is dual-subject, so it is about the pair"
                : req is ModifierRequirement
                    ? "both operand slots share one ParameterMeta instance, and the requirement applies to both operands"
                    : "both operand slots share one ParameterMeta instance, so object identity cannot say which operand this is about; "
                      + "ProofEngine.ResolveParamInBinaryOp resolves it to the RIGHT operand by convention "
                      + "(src/Precept/Pipeline/ProofEngine.cs ResolveParamInBinaryOp)");

        if (req is DimensionProofRequirement dim)
            parts.Add($"required period dimension: {dim.RequiredDimension}");
        if (req is NumericProofRequirement num)
            parts.Add($"threshold: value {OperatorText(num.Comparison)} {num.Threshold}");
        if (req is QualifierCompatibilityProofRequirement qc)
            parts.Add($"qualifier axis: {qc.Axis}");
        if (req is QualifierChainProofRequirement qch)
            parts.Add($"qualifier axes: left {qch.LeftAxis}, right {qch.RightAxis}");
        if (req is ModifierRequirement mod)
            parts.Add($"required field modifier: {mod.Required}");

        return parts.Count == 0 ? "unconditional on this catalog entry" : string.Join("; ", parts);
    }

    // ── Functions ───────────────────────────────────────────────────────────────

    private static IEnumerable<FaultAxisSite> FromFunctions()
    {
        foreach (var meta in Functions.All)
        {
            for (var o = 0; o < meta.Overloads.Count; o++)
            {
                var overload = meta.Overloads[o];
                var names = overload.Parameters
                    .Select((p, i) => (Param: p, Name: p.Name ?? $"argument {i + 1}"))
                    .ToImmutableArray();
                var signature = $"{meta.Name}({string.Join(", ", overload.Parameters.Select(p => $"{p.Kind.ToString().ToLowerInvariant()} {p.Name}".TrimEnd()))})";

                var index = 0;
                foreach (var req in overload.ProofRequirements)
                {
                    yield return new FaultAxisSite(
                        SiteId: $"fn/{meta.Kind}/overload-{o}/{KindSlug(req.Kind)}-{index++}",
                        CatalogSource: "Functions.cs",
                        DeclaringEntry: $"{meta.Kind} overload {o} — {signature}",
                        RequirementKind: req.Kind.ToString(),
                        Subject: DescribeSubject(req, names),
                        Condition: req.Description,
                        ApplicableTypeFamilies: overload.Parameters.Select(p => p.Kind.ToString()).ToImmutableArray(),
                        Notes: meta.Overloads.Count > 1
                            ? $"declared on overload {o} of {meta.Overloads.Count}; the other overloads of '{meta.Name}' do not carry it"
                            : "the function's only overload, so the requirement is unconditional at every call site");
                }
            }
        }
    }

    // ── Actions ─────────────────────────────────────────────────────────────────

    private static IEnumerable<FaultAxisSite> FromActions()
    {
        foreach (var meta in Actions.All)
        {
            var names = meta.Parameters
                .Select((p, i) => (Param: p, Name: p.Name ?? $"parameter {i + 1}"))
                .ToImmutableArray();

            var index = 0;
            foreach (var req in meta.ProofRequirements)
            {
                yield return new FaultAxisSite(
                    SiteId: $"action/{meta.Kind}/{KindSlug(req.Kind)}-{index++}",
                    CatalogSource: "Actions.cs",
                    DeclaringEntry: $"{meta.Kind} — {meta.Token.Text ?? meta.Kind.ToString().ToLowerInvariant()} ({meta.SyntaxShape})",
                    RequirementKind: req.Kind.ToString(),
                    Subject: DescribeSubject(req, names),
                    Condition: req.Description,
                    ApplicableTypeFamilies: meta.ApplicableTo.Select(TargetText).ToImmutableArray(),
                    Notes: ActionNotes(meta, req));
            }
        }
    }

    private static string ActionNotes(ActionMeta meta, ProofRequirement req)
    {
        var parts = new List<string>
        {
            $"applies whenever the action targets one of its applicable field types ({string.Join(", ", meta.ApplicableTo.Select(t => TargetText(t).ToLowerInvariant()))})",
        };

        if (req is IndexBoundsProofRequirement ib)
            parts.Add($"bounds mode {ib.Mode} against the receiver's .{ib.UpperBoundAccessor.Name}");
        if (req is KeyPresenceProofRequirement kp)
            parts.Add(kp.RequireAbsence ? "requires the key to be ABSENT" : "requires the key to be PRESENT");
        if (req is NumericProofRequirement num)
            parts.Add($"threshold: value {OperatorText(num.Comparison)} {num.Threshold}");
        if (meta.DynamicObligationGenerator is not null)
            parts.Add("this action also carries a dynamic obligation generator, whose obligations are NOT part of this catalog walk");

        return string.Join("; ", parts);
    }

    // ── Types (accessors) ───────────────────────────────────────────────────────

    private static IEnumerable<FaultAxisSite> FromTypes()
    {
        foreach (var type in Types.All)
        {
            foreach (var accessor in type.Accessors)
            {
                var names = accessor.Parameters
                    .Select((p, i) => (Param: p, Name: p.Name ?? $"parameter {i + 1}"))
                    .ToImmutableArray();

                var index = 0;
                foreach (var req in accessor.ProofRequirements)
                {
                    yield return new FaultAxisSite(
                        SiteId: $"accessor/{type.Kind}/{accessor.Name}/{KindSlug(req.Kind)}-{index++}",
                        CatalogSource: "Types.cs",
                        DeclaringEntry: $"{type.DisplayName ?? type.Kind.ToString().ToLowerInvariant()} accessor .{accessor.Name}"
                            + (accessor.ParameterType is null ? "" : $"({accessor.ParameterType.Value.ToString().ToLowerInvariant()})"),
                        RequirementKind: req.Kind.ToString(),
                        Subject: DescribeSubject(req, names),
                        Condition: req.Description,
                        ApplicableTypeFamilies: [type.Kind.ToString()],
                        Notes: AccessorNotes(type, accessor, req));
                }
            }
        }
    }

    private static string AccessorNotes(TypeMeta type, TypeAccessor accessor, ProofRequirement req)
    {
        var parts = new List<string>();

        if (accessor.RequiredTraits != TypeTrait.None)
            parts.Add($"accessor itself is available only on element types with the {accessor.RequiredTraits} trait, "
                + "so the requirement exists only for those element types");
        if (req is IndexBoundsProofRequirement ib)
            parts.Add($"bounds mode {ib.Mode} against the receiver's .{ib.UpperBoundAccessor.Name}");
        if (req is NumericProofRequirement num)
            parts.Add($"threshold: receiver projection {OperatorText(num.Comparison)} {num.Threshold}");

        parts.Add($"declared on the {type.Kind.ToString().ToLowerInvariant()} accessor table");
        return string.Join("; ", parts);
    }

    // ── Subject rendering ───────────────────────────────────────────────────────

    /// <summary>
    /// Renders a requirement's subject(s) in words. Dual-subject requirements name both.
    /// Parameter subjects are resolved by object identity against the declaring entry's
    /// parameter list — that identity IS the binding contract (see ProofRequirement.cs
    /// <c>ParamSubject</c>), so a name that fails to resolve is reported as such rather
    /// than guessed at.
    /// </summary>
    private static string DescribeSubject(
        ProofRequirement req,
        ImmutableArray<(ParameterMeta Param, string Name)> names) => req switch
    {
        QualifierCompatibilityProofRequirement q => Pair(q.LeftSubject, q.RightSubject, names),
        QualifierChainProofRequirement q => Pair(q.LeftSubject, q.RightSubject, names),
        DimensionalProductProofRequirement d => Pair(d.LeftSubject, d.RightSubject, names),
        NumericProofRequirement n => One(n.Subject, names),
        PresenceProofRequirement p => One(p.Subject, names),
        DimensionProofRequirement d => One(d.Subject, names),
        ModifierRequirement m => One(m.Subject, names),
        AssignmentQualifierProofRequirement a => One(a.Subject, names),
        IntervalContainmentProofRequirement i => One(i.Subject, names),
        LengthContainmentProofRequirement l => One(l.Subject, names),
        CountContainmentProofRequirement c => One(c.Subject, names),
        KeyPresenceProofRequirement k => One(k.Subject, names),
        IndexBoundsProofRequirement ix => One(ix.Subject, names),
        _ => $"subject shape not rendered by this enumerator: {req.GetType().Name}",
    };

    /// <summary>
    /// Renders a dual-subject requirement's two subjects. When both name the same
    /// <see cref="ParameterMeta"/> instance, the pair is the two operand slots of the entry —
    /// naming the shared label twice would read as a duplicate rather than as a pair.
    /// </summary>
    private static string Pair(
        ProofSubject left,
        ProofSubject right,
        ImmutableArray<(ParameterMeta Param, string Name)> names) =>
        left is ParamSubject lp && right is ParamSubject rp && ReferenceEquals(lp.Parameter, rp.Parameter)
            ? "the left operand and the right operand (both slots share one ParameterMeta instance)"
            : $"{One(left, names)} and {One(right, names)}";

    private static string One(
        ProofSubject subject,
        ImmutableArray<(ParameterMeta Param, string Name)> names) => subject switch
    {
        ParamSubject p => names.FirstOrDefault(n => ReferenceEquals(n.Param, p.Parameter)).Name
            ?? $"parameter '{p.Parameter.Name ?? p.Parameter.Kind.ToString().ToLowerInvariant()}' "
               + "(not resolvable by object identity against the declaring entry's parameter list)",
        SelfSubject { Accessor: null } => "the receiver itself",
        SelfSubject s => $"the receiver's .{s.Accessor!.Name}",
        _ => $"subject shape not rendered by this enumerator: {subject.GetType().Name}",
    };

    // ── Text helpers ────────────────────────────────────────────────────────────

    private static string KindSlug(ProofRequirementKind kind) => kind.ToString().ToLowerInvariant();

    /// <summary>
    /// The authored spelling of an operator. Read off the Operators catalog so the
    /// enumerator does not keep its own operator-symbol table.
    /// </summary>
    private static string OperatorText(OperatorKind op) => Operators.GetMeta(op) switch
    {
        SingleTokenOp single => single.Token.Text ?? op.ToString(),
        MultiTokenOp multi => string.Join(" ", multi.Tokens.Select(t => t.Text ?? "?")),
        _ => op.ToString(),
    };

    /// <summary>The applicable-type entry of an action, where a null Kind means "any field type".</summary>
    private static string TargetText(TypeTarget target) => target.Kind?.ToString() ?? "AnyType";

    // ── JSON emission ───────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Serializes the site list plus the counts a reader needs to sanity-check it:
    /// total sites, sites per catalog, and sites per requirement kind.
    /// </summary>
    public static string ToJson(ImmutableArray<FaultAxisSite> sites)
    {
        var payload = new
        {
            totalSites = sites.Length,
            byCatalog = sites.GroupBy(s => s.CatalogSource)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count()),
            byRequirementKind = sites.GroupBy(s => s.RequirementKind)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count()),
            kindsWithNoCatalogSite = Enum.GetValues<ProofRequirementKind>()
                .Select(k => k.ToString())
                .Where(k => !sites.Any(s => s.RequirementKind == k))
                .ToArray(),
            sites,
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
