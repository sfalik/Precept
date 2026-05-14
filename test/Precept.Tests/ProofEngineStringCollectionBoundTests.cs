using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

// ════════════════════════════════════════════════════════════════════════════════
//  Slice 11 — String/Collection Constraint Obligation Tests
//
//  Design reference: docs/Working/interval-proof-engine-design.md
//    §8.2  Slice 11 spec (LengthContainment / CountContainment obligations)
//
//  Strategy: only literal string assignments to bounded string fields generate
//  obligations. Non-literal assignments (arg refs, field refs) produce no
//  obligation (correct: conservative V1 approach to avoid false positives on
//  dynamically-provided values).
// ════════════════════════════════════════════════════════════════════════════════

public class ProofEngineStringCollectionBoundTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  TypeChecker: bounds extraction for minlength/maxlength/mincount/maxcount
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TypeChecker_StringField_WithMaxLength_PopulatesDeclaredMaxLength()
    {
        const string precept = @"
precept BoundsTest
field Note as string optional maxlength 100
state Active initial";

        var result = Compiler.Compile(precept);
        result.Semantics.FieldsByName.Should().ContainKey("Note");
        var field = result.Semantics.FieldsByName["Note"];
        field.DeclaredMaxLength.Should().Be(100, "maxlength 100 should be extracted");
        field.DeclaredMinLength.Should().BeNull("no minlength declared");
    }

    [Fact]
    public void TypeChecker_StringField_WithMinLengthAndMaxLength_PopulatesBothBounds()
    {
        const string precept = @"
precept BoundsTest
field Code as string optional minlength 3 maxlength 10
state Active initial";

        var result = Compiler.Compile(precept);
        var field = result.Semantics.FieldsByName["Code"];
        field.DeclaredMinLength.Should().Be(3);
        field.DeclaredMaxLength.Should().Be(10);
    }

    [Fact]
    public void TypeChecker_StringField_NoBoundModifiers_LeavesLengthNull()
    {
        const string precept = @"
precept BoundsTest
field Name as string optional
state Active initial";

        var result = Compiler.Compile(precept);
        var field = result.Semantics.FieldsByName["Name"];
        field.DeclaredMinLength.Should().BeNull();
        field.DeclaredMaxLength.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Proof: string literal within maxlength → proved, no diagnostic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringField_MaxLength_SetToShorterLiteral_Proved()
    {
        const string precept = @"
precept BoundsTest
field Note as string optional maxlength 20
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Note = ""Short note"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.HasErrors.Should().BeFalse("literal 'Short note' (10 chars) fits within maxlength 20");
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("no length violation expected");
    }

    [Fact]
    public void StringField_MaxLength_SetToExactlyMaxLiteral_Proved()
    {
        const string precept = @"
precept BoundsTest
field Code as string optional maxlength 5
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Code = ""ABCDE"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("exactly 5 chars == maxlength 5, should pass");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Proof: string literal exceeds maxlength → unresolved → LengthBoundViolation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringField_MaxLength_SetToLongerLiteral_EmitsDiagnostic()
    {
        const string precept = @"
precept BoundsTest
field Note as string optional maxlength 5
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Note = ""This is way too long"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.HasErrors.Should().BeTrue("literal 'This is way too long' (20 chars) exceeds maxlength 5");
        result.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString(),
            "LengthBoundViolation should be emitted for strings exceeding maxlength");
    }

    [Fact]
    public void StringField_MinLength_SetToEmptyLiteral_EmitsDiagnostic()
    {
        const string precept = @"
precept BoundsTest
field Code as string optional minlength 3
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Code = """" -> transition Done";

        var result = Compiler.Compile(precept);
        result.HasErrors.Should().BeTrue("empty string (0 chars) is below minlength 3");
        result.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString(),
            "LengthBoundViolation should be emitted for strings below minlength");
    }

    [Fact]
    public void StringField_MinLength_SetToSufficientLiteral_Proved()
    {
        const string precept = @"
precept BoundsTest
field Code as string optional minlength 3
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Code = ""ABCD"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("'ABCD' (4 chars) >= minlength 3");
    }

    [Fact]
    public void StringField_BothBounds_LiteralWithinBounds_Proved()
    {
        const string precept = @"
precept BoundsTest
field Code as string optional minlength 2 maxlength 8
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Code = ""ABC"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("'ABC' (3 chars) is within [2..8]");
    }

    [Fact]
    public void StringField_BothBounds_LiteralTooShort_EmitsDiagnostic()
    {
        const string precept = @"
precept BoundsTest
field Code as string optional minlength 5 maxlength 10
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Code = ""AB"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Should().Contain(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString(),
            "literal 'AB' (2 chars) is below minlength 5");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Critical regression: non-literal assignments produce NO obligation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StringField_SetFromNonLiteralArg_NoObligationGenerated()
    {
        // Key regression test: event arg assignment to bounded field must not emit diagnostic
        // This matches the FullPrecept pattern in TypeCheckerAssemblyTests
        const string precept = @"
precept BoundsTest
field DecisionNote as string optional maxlength 500
state Draft initial
state Done terminal
event Approve(Note as string)
event Deny(Note as string)
from Draft on Approve -> set DecisionNote = Approve.Note -> transition Done
from Draft on Deny -> set DecisionNote = Deny.Note -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("non-literal event arg assignments to bounded fields must not generate LengthBoundViolation");
        result.HasErrors.Should().BeFalse("no errors expected when assigning event args to bounded string field");
    }

    [Fact]
    public void StringField_SetFromFieldRef_NoObligationGenerated()
    {
        const string precept = @"
precept BoundsTest
field Source as string optional
field Target as string optional maxlength 10
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Target = Source -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("field ref assignment to bounded field must not generate LengthBoundViolation");
    }

    [Fact]
    public void StringField_NoBoundsModifiers_NoObligation()
    {
        const string precept = @"
precept BoundsTest
field Note as string optional
state Draft initial
state Done terminal
event Complete
from Draft on Complete -> set Note = ""any value"" -> transition Done";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.LengthBoundViolation.ToString())
            .Should().BeEmpty("no bounds = no obligation");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Collection count: no obligation generated (V1)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CollectionField_WithMinMaxCount_NoObligationGeneratedInV1()
    {
        // Collections cannot be set directly (type checker rejects it);
        // add/remove actions don't generate count containment obligations in V1.
        // This test ensures mincount/maxcount are extracted without erroring.
        const string precept = @"
precept BoundsTest
field Tags as set of string mincount 1 maxcount 10
state Active initial";

        var result = Compiler.Compile(precept);
        result.Diagnostics.Where(d => d.Code == DiagnosticCode.CountBoundViolation.ToString())
            .Should().BeEmpty("no CountBoundViolation expected in V1 — no obligation generator for add/remove yet");
    }

    [Fact]
    public void CollectionField_WithCountBounds_BoundsExtractedInSemanticIndex()
    {
        const string precept = @"
precept BoundsTest
field Tags as set of string mincount 2 maxcount 5
state Active initial";

        var result = Compiler.Compile(precept);
        result.Semantics.FieldsByName.Should().ContainKey("Tags");
        var field = result.Semantics.FieldsByName["Tags"];
        field.DeclaredMinCount.Should().Be(2);
        field.DeclaredMaxCount.Should().Be(5);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ProofRequirementKind catalog coverage
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProofRequirementKind_LengthContainment_HasMetaEntry()
    {
        var meta = ProofRequirements.GetMeta(ProofRequirementKind.LengthContainment);
        meta.Should().NotBeNull();
        meta.Kind.Should().Be(ProofRequirementKind.LengthContainment);
    }

    [Fact]
    public void ProofRequirementKind_CountContainment_HasMetaEntry()
    {
        var meta = ProofRequirements.GetMeta(ProofRequirementKind.CountContainment);
        meta.Should().NotBeNull();
        meta.Kind.Should().Be(ProofRequirementKind.CountContainment);
    }

    [Fact]
    public void DiagnosticCode_LengthBoundViolation_HasMetaEntry()
    {
        var meta = Diagnostics.GetMeta(DiagnosticCode.LengthBoundViolation);
        meta.Should().NotBeNull();
        meta.Stage.Should().Be(DiagnosticStage.Proof);
        meta.Severity.Should().Be(Severity.Error);
        meta.PreventsFault.Should().Be(FaultCode.LengthBoundViolation);
    }

    [Fact]
    public void DiagnosticCode_CountBoundViolation_HasMetaEntry()
    {
        var meta = Diagnostics.GetMeta(DiagnosticCode.CountBoundViolation);
        meta.Should().NotBeNull();
        meta.Stage.Should().Be(DiagnosticStage.Proof);
        meta.Severity.Should().Be(Severity.Error);
        meta.PreventsFault.Should().Be(FaultCode.CountBoundViolation);
    }
}
