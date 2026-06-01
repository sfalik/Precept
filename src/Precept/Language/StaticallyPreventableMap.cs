using System.Collections.Frozen;
using System.Reflection;

namespace Precept.Language;

/// <summary>
/// The single source of truth for the <c>DiagnosticCode → FaultCode</c> bijective core,
/// derived by reflecting over <see cref="StaticallyPreventableAttribute"/> on
/// <see cref="FaultCode"/> members. Each <c>[StaticallyPreventable(code)]</c> declaration says
/// "this fault is the runtime consequence of failing to statically prevent <c>code</c>" — the
/// inverse of that relation is the diagnostic-to-fault mapping consumers read.
///
/// This is the bijective part only. The collection-safety many-to-one collapse and the
/// conservative proof-only backstop are runtime policy that the attribute cannot express; they
/// live as explicit code at the consuming site.
/// </summary>
public static class StaticallyPreventableMap
{
    /// <summary>
    /// The diagnostic codes whose attribute-declared inverse the proof engine consults as the
    /// bijective core. A diagnostic code is included only when its 1:1 fault is the historical
    /// fault-link outcome — the runtime backstop, not the attribute, governs the remaining proof
    /// obligation families (notably <see cref="DiagnosticCode.UnprovedPresenceRequirement"/>,
    /// whose attribute partner <see cref="FaultCode.UnexpectedNull"/> is a design-time linkage,
    /// while the runtime fault for an unproved presence obligation is the shared conservative
    /// backstop).
    /// </summary>
    private static readonly FrozenSet<DiagnosticCode> BijectiveDiagnosticCodes = new[]
    {
        DiagnosticCode.DivisionByZero,
        DiagnosticCode.SqrtOfNegative,
        DiagnosticCode.UnguardedCollectionAccess,
        DiagnosticCode.UnguardedCollectionMutation,
        DiagnosticCode.NumericOverflow,
        DiagnosticCode.LengthBoundViolation,
        DiagnosticCode.CountBoundViolation,
    }.ToFrozenSet();

    private static readonly FrozenDictionary<DiagnosticCode, FaultCode> InverseAttributeMap =
        BuildInverseAttributeMap();

    /// <summary>
    /// The <c>DiagnosticCode → FaultCode</c> bijective core: the attribute-declared inverse,
    /// restricted to the diagnostic codes whose 1:1 fault is the runtime fault-link outcome.
    /// </summary>
    public static readonly FrozenDictionary<DiagnosticCode, FaultCode> BijectiveFaultRows =
        BuildBijectiveFaultRows();

    /// <summary>
    /// Resolves the bijective fault for a diagnostic code, or <c>null</c> when the code is not a
    /// bijective row (it is then governed by the collapse/backstop policy at the call site).
    /// </summary>
    public static FaultCode? TryGetBijectiveFault(DiagnosticCode diagnosticCode) =>
        BijectiveFaultRows.TryGetValue(diagnosticCode, out var fault) ? fault : null;

    private static FrozenDictionary<DiagnosticCode, FaultCode> BuildInverseAttributeMap()
    {
        var map = new Dictionary<DiagnosticCode, FaultCode>();
        foreach (var fault in Enum.GetValues<FaultCode>())
        {
            var attr = typeof(FaultCode).GetField(Enum.GetName(fault)!)!
                .GetCustomAttribute<StaticallyPreventableAttribute>();
            if (attr is not null)
                map[attr.Code] = fault;
        }

        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<DiagnosticCode, FaultCode> BuildBijectiveFaultRows()
    {
        var map = new Dictionary<DiagnosticCode, FaultCode>();
        foreach (var diagnosticCode in BijectiveDiagnosticCodes)
            map[diagnosticCode] = InverseAttributeMap[diagnosticCode];

        return map.ToFrozenDictionary();
    }
}
