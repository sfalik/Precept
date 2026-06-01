using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Characterizes the <c>DiagnosticCode → FaultCode</c> bijective mapping that the proof
/// engine applies when producing a <see cref="Precept.Pipeline.FaultSiteLink"/>. The mapping
/// has three parts: a bijective core (1:1 per <see cref="StaticallyPreventableAttribute"/>),
/// a many-to-one collapse (collection-safety codes that share a runtime fault), and a
/// conservative backstop for proof-only obligation families.
///
/// These tests pin the bijective core so that deriving it from the attribute stays
/// behavior-identical to the prior hand-written rows.
/// </summary>
public class FaultMapDerivationTests
{
    /// <summary>
    /// The bijective core as the proof engine historically applied it — the diagnostic codes that
    /// map 1:1 to a fault, captured here as the golden the attribute-derived map must reproduce.
    /// </summary>
    private static readonly IReadOnlyDictionary<DiagnosticCode, FaultCode> PriorBijectiveRows =
        new Dictionary<DiagnosticCode, FaultCode>
        {
            [DiagnosticCode.DivisionByZero]             = FaultCode.DivisionByZero,
            [DiagnosticCode.SqrtOfNegative]             = FaultCode.SqrtOfNegative,
            [DiagnosticCode.UnguardedCollectionAccess]  = FaultCode.CollectionEmptyOnAccess,
            [DiagnosticCode.UnguardedCollectionMutation] = FaultCode.CollectionEmptyOnMutation,
            [DiagnosticCode.NumericOverflow]            = FaultCode.NumericOverflow,
            [DiagnosticCode.LengthBoundViolation]       = FaultCode.LengthBoundViolation,
            [DiagnosticCode.CountBoundViolation]        = FaultCode.CountBoundViolation,
        };

    /// <summary>
    /// Builds the inverse map (<c>DiagnosticCode → FaultCode</c>) by reflecting over
    /// <see cref="StaticallyPreventableAttribute"/> on <see cref="FaultCode"/> members — the same
    /// derivation the proof engine uses for its bijective core.
    /// </summary>
    private static Dictionary<DiagnosticCode, FaultCode> ReflectedInverseMap()
    {
        var map = new Dictionary<DiagnosticCode, FaultCode>();
        foreach (var fault in Enum.GetValues<FaultCode>())
        {
            var attr = typeof(FaultCode).GetField(Enum.GetName(fault)!)!
                .GetCustomAttribute<StaticallyPreventableAttribute>();
            if (attr is not null)
                map[attr.Code] = fault;
        }

        return map;
    }

    [Fact]
    public void ReflectedInverseMap_ReproducesPriorBijectiveRows()
    {
        var reflected = ReflectedInverseMap();

        foreach (var (diagnosticCode, faultCode) in PriorBijectiveRows)
        {
            reflected.Should().ContainKey(diagnosticCode,
                $"the bijective row DiagnosticCode.{diagnosticCode} must be derivable from [StaticallyPreventable]");
            reflected[diagnosticCode].Should().Be(faultCode,
                $"DiagnosticCode.{diagnosticCode} historically mapped to FaultCode.{faultCode}");
        }
    }

    [Fact]
    public void PublicDerivedBijectiveMap_EqualsPriorBijectiveRows_Exactly()
    {
        // The proof engine exposes the derived bijective core it actually consults; it must equal
        // the prior hand-written rows exactly — no extra rows (which would change behavior for
        // diagnostic codes that historically fell to the collapse/backstop), none missing.
        var derived = StaticallyPreventableMap.BijectiveFaultRows;

        derived.Should().HaveCount(PriorBijectiveRows.Count);
        foreach (var (diagnosticCode, faultCode) in PriorBijectiveRows)
        {
            derived.Should().ContainKey(diagnosticCode);
            derived[diagnosticCode].Should().Be(faultCode);
        }
    }
}
