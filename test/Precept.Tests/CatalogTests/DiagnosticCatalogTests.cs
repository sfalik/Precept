using System;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.CatalogTests;

public sealed class DiagnosticCatalogTests
{
    public static TheoryData<DiagnosticCode, DiagnosticMeta> AllDiagnostics => CatalogTestReflection.AllDiagnostics();

    [Theory]
    [MemberData(nameof(AllDiagnostics))]
    public void DiagnosticMeta_Code_MatchesADeclaredEnumMember(DiagnosticCode code, DiagnosticMeta meta)
    {
        Enum.TryParse<DiagnosticCode>(meta.Code, out var parsed).Should().BeTrue(
            $"DiagnosticCode.{code} must serialize to a parsable diagnostic code string");
        parsed.Should().Be(code,
            $"DiagnosticCode.{code} should round-trip through DiagnosticMeta.Code");
        Enum.IsDefined(parsed).Should().BeTrue(
            $"DiagnosticCode.{code} must remain a declared enum member");
    }

    [Theory]
    [MemberData(nameof(AllDiagnostics))]
    public void DiagnosticMeta_MessageTemplate_IsNonEmpty(DiagnosticCode code, DiagnosticMeta meta)
        => meta.MessageTemplate.Should().NotBeNullOrWhiteSpace(
            $"DiagnosticCode.{code} must expose a non-empty message template");

    [Theory]
    [MemberData(nameof(AllDiagnostics))]
    public void DiagnosticMeta_RecoveryGuidance_IsNonEmpty(DiagnosticCode code, DiagnosticMeta meta)
    {
        var recoveryGuidance = CatalogTestReflection.ReadDiagnosticRecoveryGuidance(meta);

        recoveryGuidance.Should().NotBeEmpty(
            $"DiagnosticCode.{code} must expose RecoveryHint, RecoverySteps, or FixHint guidance");
        recoveryGuidance.Should().OnlyContain(step => !string.IsNullOrWhiteSpace(step),
            $"DiagnosticCode.{code} should not contain blank recovery guidance entries");
    }
}
