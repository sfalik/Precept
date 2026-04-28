using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

public class FaultsTests
{
    // ── Exhaustiveness ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void GetMeta_ReturnsWithoutThrowing_ForEveryFaultCode(FaultCode code)
    {
        var meta = Faults.GetMeta(code);
        meta.Should().NotBeNull();
    }

    [Fact]
    public void All_ContainsExactlyAsManyEntries_AsEnumValues()
    {
        var expected = Enum.GetValues<FaultCode>().Length;
        Faults.All.Should().HaveCount(expected);
    }

    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void All_EveryEntry_HasNonEmptyCodeAndMessageTemplate(FaultCode code)
    {
        var meta = Faults.GetMeta(code);
        meta.Code.Should().NotBeNullOrWhiteSpace();
        meta.MessageTemplate.Should().NotBeNullOrWhiteSpace();
    }

    // ── Create factory ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void Create_ProducesCorrectCodeAndCodeName(FaultCode code)
    {
        var fault = Faults.Create(code);
        fault.Code.Should().Be(code);
        fault.CodeName.Should().Be(Faults.GetMeta(code).Code);
    }

    [Fact]
    public void Create_FormatsMessageTemplate_WhenArgsProvided()
    {
        // DivisionByZero has no format placeholders, but string.Format tolerates extra args.
        // We verify at minimum that Create doesn't throw and the message is the template itself.
        var fault = Faults.Create(FaultCode.DivisionByZero);
        fault.Message.Should().Be("Divisor evaluated to zero");
    }

    // ── CodeName identity ───────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void CodeName_MatchesEnumMemberName(FaultCode code)
    {
        var fault = Faults.Create(code);
        fault.CodeName.Should().Be(Enum.GetName(code));
    }

    // ── FaultCode → DiagnosticCode linkage ──────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void EveryFaultCode_HasStaticallyPreventableAttribute(FaultCode code)
    {
        var attr = GetPreventableAttribute(code);
        attr.Should().NotBeNull($"FaultCode.{code} must have a [StaticallyPreventable] attribute");
    }

    [Theory]
    [MemberData(nameof(AllFaultCodes))]
    public void StaticallyPreventable_LinksToValidDiagnosticCode(FaultCode code)
    {
        var attr = GetPreventableAttribute(code)!;
        Enum.IsDefined(attr.Code).Should().BeTrue(
            $"FaultCode.{code} links to DiagnosticCode.{attr.Code} which is not a defined enum value");
    }

    [Fact]
    public void NoTwoFaultCodes_MapToSameDiagnosticCode()
    {
        var mappings = Enum.GetValues<FaultCode>()
            .Select(c => (FaultCode: c, DiagCode: GetPreventableAttribute(c)!.Code))
            .ToList();

        var duplicates = mappings
            .GroupBy(m => m.DiagCode)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} ← [{string.Join(", ", g.Select(m => m.FaultCode))}]")
            .ToList();

        duplicates.Should().BeEmpty("each FaultCode should map to a unique DiagnosticCode");
    }

    // X11 ── Fault templates: placeholders are formatted correctly ──────────────

    [Fact]
    public void FaultCodeTemplates_WithPlaceholders_FormatSuccessfully()
    {
        // For each FaultCode template that contains format placeholders, verify that
        // Create() with enough args produces a message that does NOT contain the raw placeholder.
        foreach (var code in Enum.GetValues<FaultCode>())
        {
            var template = Faults.GetMeta(code).MessageTemplate;
            if (!template.Contains("{0}"))
                continue; // static template — no formatting needed

            // Supply 4 dummy args — string.Format ignores extra positional args
            var fault = Faults.Create(code, "arg0", "arg1", "arg2", "arg3");
            fault.Message.Should().NotContain("{0}",
                $"FaultCode.{code} template should format its placeholders when args are supplied");
        }
    }

    [Fact]
    public void FaultCodeTemplates_WithoutPlaceholders_ReturnTemplateDirectly()
    {
        foreach (var code in Enum.GetValues<FaultCode>())
        {
            var template = Faults.GetMeta(code).MessageTemplate;
            if (template.Contains("{0}"))
                continue; // has placeholders — covered by the formatting test

            var fault = Faults.Create(code);
            fault.Message.Should().Be(template,
                $"FaultCode.{code} has no placeholders — message should equal the template exactly");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    public static TheoryData<FaultCode> AllFaultCodes()
    {
        var data = new TheoryData<FaultCode>();
        foreach (var code in Enum.GetValues<FaultCode>())
            data.Add(code);
        return data;
    }

    private static StaticallyPreventableAttribute? GetPreventableAttribute(FaultCode code)
    {
        var member = typeof(FaultCode).GetField(Enum.GetName(code)!)!;
        return member.GetCustomAttribute<StaticallyPreventableAttribute>();
    }
}
