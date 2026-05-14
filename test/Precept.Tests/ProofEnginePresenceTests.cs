using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

// ════════════════════════════════════════════════════════════════════════════════
//  Slice 12 — Presence Obligation Generation Tests
//
//  Design reference: docs/Working/interval-proof-engine-design.md
//    §8.2  Slice 12 spec (PresenceProofRequirement generation for optional field refs)
//
//  Coverage:
//  - Direct set action: optional field as value source (unguarded → PRE0116)
//  - Binary operation: optional field as operand (unguarded → PRE0116)
//  - Function argument: optional field passed to a function (unguarded → PRE0116)
//  - Member access receiver: optional field as .count receiver (unguarded → PRE0116)
//  - Rule condition: optional field in rule body (no transition guard → PRE0116)
//  - Interpolation hole: optional field in "{field}" (unguarded → PRE0116)
//  - Guarded positive case: `when X is set` guard discharges the obligation
//  - Required field: no presence obligation generated
//  - FaultCode mapping: UnexpectedNull → UnprovedPresenceRequirement (not NullInNonNullableContext)
// ════════════════════════════════════════════════════════════════════════════════

public class ProofEnginePresenceTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Obligation generation: optional field ref in value position
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_InValuePosition_GeneratesPresenceObligation()
    {
        const string precept = @"
precept PresenceTest
field Source as number optional
field Target as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Target = Source -> transition Done";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Should().NotBeEmpty("an optional field used as a value source must generate a PresenceProofRequirement");
    }

    [Fact]
    public void RequiredField_InValuePosition_GeneratesNoPresenceObligation()
    {
        const string precept = @"
precept PresenceTest
field Source as number default 0
field Target as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Target = Source -> transition Done";

        var result = Compiler.Compile(precept);

        result.Proof.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .Should().BeEmpty("a required field carries Guaranteed presence — no presence obligation should be generated");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Unguarded access — PRE0116 (UnprovedPresenceRequirement)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_InSetAction_WithoutGuard_GeneratesPRE0116()
    {
        const string precept = @"
precept PresenceTest
field Source as number optional
field Target as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Target = Source -> transition Done";

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeTrue("unguarded access to an optional field must generate PRE0116");
        result.Diagnostics.Should().Contain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "unguarded optional field reference in a set action value must emit UnprovedPresenceRequirement");
    }

    [Fact]
    public void OptionalField_InBinaryOp_WithoutGuard_GeneratesPRE0116()
    {
        const string precept = @"
precept PresenceTest
field Base as number optional
field Result as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Result = Base + 10 -> transition Done";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "optional field as a binary-op operand without guard must emit UnprovedPresenceRequirement");
    }

    [Fact]
    public void OptionalField_InFunctionArg_WithoutGuard_GeneratesPRE0116()
    {
        const string precept = @"
precept PresenceTest
field Price as number optional
field Root as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Root = sqrt(Price) -> transition Done";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "optional field passed as a function argument without guard must emit UnprovedPresenceRequirement");
    }

    [Fact]
    public void OptionalField_InMemberAccess_WithoutGuard_GeneratesPRE0116()
    {
        const string precept = @"
precept PresenceTest
field Tags as list of string optional
field CountOfTags as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set CountOfTags = Tags.count -> transition Done";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "optional collection field used as .count receiver without guard must emit UnprovedPresenceRequirement");
    }

    [Fact]
    public void OptionalField_InRuleCondition_WithoutGuard_GeneratesPRE0116()
    {
        const string precept = @"
precept PresenceTest
field Discount as number optional
state Active initial
rule Discount > 0 because ""Discount must be positive""";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "optional field referenced in a rule condition without presence guard must emit UnprovedPresenceRequirement");
    }

    [Fact]
    public void OptionalField_InInterpolationHole_WithoutGuard_GeneratesPRE0116()
    {
        const string precept = @"
precept PresenceTest
field Note as string optional
field Message as string
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Message = ""Note: {Note}"" -> transition Done";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "optional field in an interpolation hole without guard must emit UnprovedPresenceRequirement");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Guarded access — obligation discharged, no PRE0116
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_WithIsSetGuard_DischargesPresenceObligation()
    {
        const string precept = @"
precept PresenceTest
field Source as number optional
field Target as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete when Source is set -> set Target = Source -> transition Done
from Draft on Complete -> no transition";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().NotContain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "a `when X is set` guard must discharge the PresenceProofRequirement for that field");

        result.Proof.Obligations
            .Where(o => o.Requirement is PresenceProofRequirement)
            .All(o => o.Disposition == ProofDisposition.Proved)
            .Should().BeTrue("all presence obligations for guarded optional field refs must be proved");
    }

    [Fact]
    public void OptionalField_WithIsSetGuard_InBinaryOp_DischargesPresenceObligation()
    {
        const string precept = @"
precept PresenceTest
field Base as number optional
field Result as number
state Draft initial
state Done terminal
event Complete
from Draft on Complete when Base is set -> set Result = Base + 10 -> transition Done
from Draft on Complete -> no transition";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().NotContain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "`when Base is set` guard must discharge the presence obligation on Base in the binary operation");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TypedPostfixOp (`X is set`) is a presence CHECK — not a value usage
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalField_UsedInIsSetCheck_DoesNotGeneratePresenceObligation()
    {
        // `Email is set` in a rule is a presence CHECK, not a value read.
        // No PresenceProofRequirement should be generated for the operand of `is set`.
        const string precept = @"
precept PresenceTest
field Email as string optional
state Active initial
rule Email is set because ""Email must be provided""";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().NotContain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "`X is set` is a presence check — the optional field is its operand, not a value usage");
    }

    [Fact]
    public void OptionalField_UsedInIsNotSetCheck_DoesNotGeneratePresenceObligation()
    {
        const string precept = @"
precept PresenceTest
field Tag as string optional
state Active initial
rule Tag is not set because ""Tag must be absent""";

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().NotContain(
            d => d.Code == DiagnosticCode.UnprovedPresenceRequirement.ToString(),
            "`X is not set` is a presence check — no value usage and no presence obligation");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  FaultCode mapping: UnexpectedNull → UnprovedPresenceRequirement
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FaultCode_UnexpectedNull_PointsToUnprovedPresenceRequirement()
    {
        var field = typeof(FaultCode).GetField(nameof(FaultCode.UnexpectedNull))!;
        var attr = field.GetCustomAttribute<StaticallyPreventableAttribute>();

        attr.Should().NotBeNull("FaultCode.UnexpectedNull must carry [StaticallyPreventable]");
        attr!.Code.Should().Be(DiagnosticCode.UnprovedPresenceRequirement,
            "FaultCode.UnexpectedNull is prevented by PRE0116 (UnprovedPresenceRequirement), " +
            "not by PRE0019 (NullInNonNullableContext) — optional-field presence guards are the compile-time backstop");
    }
}
