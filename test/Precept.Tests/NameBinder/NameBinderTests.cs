using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.NameBinder;

/// <summary>
/// Tests for the NameBinder pipeline stage.
/// Verifies declaration collection, reference resolution, scoping, and diagnostic emission.
/// </summary>
public class NameBinderTests
{
    #region §1: Declaration Collection

    [Fact]
    public void Bind_FieldDeclarations_CollectsAllFields()
    {
        var source = """
            precept Order
            field Name as string
            field Quantity as integer
            field Total as decimal
            """;

        var symbols = CompileAndBind(source);

        symbols.Fields.Should().HaveCount(3);
        symbols.Fields.Select(f => f.Name).Should().BeEquivalentTo(["Name", "Quantity", "Total"]);
    }

    [Fact]
    public void Bind_FieldDeclarations_CapturesFieldProperties()
    {
        var source = """
            precept Order
            field Name as string
            """;

        var symbols = CompileAndBind(source);

        var field = symbols.Fields.Should().ContainSingle().Subject;
        field.Name.Should().Be("Name");
        field.Type.Should().BeOfType<SimpleTypeReference>()
            .Which.Type.Kind.Should().Be(TypeKind.String);
        field.IsComputed.Should().BeFalse();
    }

    [Fact]
    public void Bind_ComputedField_SetsIsComputedTrue()
    {
        var source = """
            precept Order
            field Quantity as integer
            field Price as decimal
            field Total as decimal <- Quantity * Price
            """;

        var symbols = CompileAndBind(source);

        var total = symbols.Fields.Single(f => f.Name == "Total");
        total.IsComputed.Should().BeTrue();
    }

    [Fact]
    public void Bind_FieldDeclarations_TracksDeclarationOrder()
    {
        var source = """
            precept Order
            field First as string
            field Second as integer
            field Third as decimal
            """;

        var symbols = CompileAndBind(source);

        symbols.Fields.Single(f => f.Name == "First").DeclarationOrder.Should().Be(0);
        symbols.Fields.Single(f => f.Name == "Second").DeclarationOrder.Should().Be(1);
        symbols.Fields.Single(f => f.Name == "Third").DeclarationOrder.Should().Be(2);
    }

    [Fact]
    public void Bind_StateDeclarations_CollectsAllStates()
    {
        var source = """
            precept Order
            state Draft
            state Submitted
            state Approved
            state Rejected
            """;

        var symbols = CompileAndBind(source);

        symbols.States.Should().HaveCount(4);
        symbols.States.Select(s => s.Name).Should().BeEquivalentTo(["Draft", "Submitted", "Approved", "Rejected"]);
    }

    [Fact]
    public void Bind_StateDeclarations_CapturesStateProperties()
    {
        var source = """
            precept Order
            state Draft
            """;

        var symbols = CompileAndBind(source);

        var state = symbols.States.Should().ContainSingle().Subject;
        state.Name.Should().Be("Draft");
        state.Syntax.Should().NotBeNull();
        state.NameSpan.Should().NotBeNull();
    }

    [Fact]
    public void Bind_EventDeclarations_CollectsAllEvents()
    {
        var source = """
            precept Order
            event Submit
            event Approve
            event Reject
            """;

        var symbols = CompileAndBind(source);

        symbols.Events.Should().HaveCount(3);
        symbols.Events.Select(e => e.Name).Should().BeEquivalentTo(["Submit", "Approve", "Reject"]);
    }

    [Fact]
    public void Bind_EventWithArgs_CollectsArgsCorrectly()
    {
        var source = """
            precept Order
            event SetPrice(newPrice as decimal)
            """;

        var symbols = CompileAndBind(source);

        var evt = symbols.Events.Should().ContainSingle().Subject;
        evt.Name.Should().Be("SetPrice");
        evt.Args.Should().ContainSingle();
        evt.Args[0].Name.Should().Be("newPrice");
        evt.Args[0].Type.ResolvedKind.Should().Be(TypeKind.Decimal);
        evt.Args[0].EventName.Should().Be("SetPrice");
    }

    [Fact]
    public void Bind_EventWithMultipleArgs_CollectsAllArgs()
    {
        var source = """
            precept Order
            event Update(approver as string, amount as number)
            """;

        var symbols = CompileAndBind(source);

        var evt = symbols.Events.Single();
        evt.Args.Should().HaveCount(2);
        evt.Args.Select(a => a.Name).Should().BeEquivalentTo(["approver", "amount"]);
        evt.Args.Select(a => a.Type.ResolvedKind).Should().BeEquivalentTo([TypeKind.String, TypeKind.Number]);
    }

    [Fact]
    public void Bind_InitialEvent_SetsIsInitialTrue()
    {
        var source = """
            precept Order
            event Create initial
            """;

        var symbols = CompileAndBind(source);

        var evt = symbols.Events.Should().ContainSingle().Subject;
        evt.IsInitial.Should().BeTrue();
    }

    [Fact]
    public void Bind_NonInitialEvent_SetsIsInitialFalse()
    {
        var source = """
            precept Order
            event Submit
            """;

        var symbols = CompileAndBind(source);

        var evt = symbols.Events.Should().ContainSingle().Subject;
        evt.IsInitial.Should().BeFalse();
    }

    #endregion

    #region §2: Duplicate Detection

    [Fact]
    public void Bind_DuplicateFieldName_EmitsDiagnostic()
    {
        var source = """
            precept Order
            field Name as string
            field Name as integer
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.DuplicateFieldName));
    }

    [Fact]
    public void Bind_DuplicateStateName_EmitsDiagnostic()
    {
        var source = """
            precept Order
            state Draft
            state Draft
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.DuplicateStateName));
    }

    [Fact]
    public void Bind_DuplicateEventName_EmitsDiagnostic()
    {
        var source = """
            precept Order
            event Submit
            event Submit
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.DuplicateEventName));
    }

    [Fact]
    public void Bind_DuplicateDeclarations_FirstDeclarationStored()
    {
        var source = """
            precept Order
            field Name as string
            field Name as integer
            """;

        var symbols = CompileAndBind(source);

        // First declaration wins
        symbols.Fields.Should().ContainSingle();
        symbols.Fields.Single().Type.Should().BeOfType<SimpleTypeReference>()
            .Which.Type.Kind.Should().Be(TypeKind.String);
    }

    [Fact]
    public void Bind_DuplicatesAcrossDifferentKinds_NoDiagnostic()
    {
        // Field, state, and event can share names (different namespaces)
        var source = """
            precept Order
            field Submit as string
            state Submit
            event Submit
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().NotContain(d => 
            d.Code == nameof(DiagnosticCode.DuplicateFieldName) ||
            d.Code == nameof(DiagnosticCode.DuplicateStateName) ||
            d.Code == nameof(DiagnosticCode.DuplicateEventName));
    }

    #endregion

    #region §3: Reference Resolution - Basic

    [Fact]
    public void Bind_FieldReference_ResolvesToFieldTarget()
    {
        var source = """
            precept Order
            field Quantity as integer
            field Total as decimal <- Quantity * 10
            """;

        var symbols = CompileAndBind(source);

        var quantityRef = symbols.References.FirstOrDefault(r => r.Name == "Quantity");
        quantityRef.Should().NotBeNull();
        quantityRef!.Resolution.Should().BeOfType<FieldTarget>();
    }

    [Fact]
    public void Bind_StateReference_ResolvesToStateTarget()
    {
        var source = """
            precept Order
            state Draft initial
            state Submitted
            event Submit
            
            from Draft on Submit -> transition Submitted
            """;

        var symbols = CompileAndBind(source);

        var draftRef = symbols.References.FirstOrDefault(r => r.Name == "Draft");
        draftRef.Should().NotBeNull();
        draftRef!.Resolution.Should().BeOfType<StateTarget>();
    }

    [Fact]
    public void Bind_EventReference_ResolvesToEventTarget()
    {
        var source = """
            precept Order
            state Draft initial
            state Submitted
            event Submit
            
            from Draft on Submit -> transition Submitted
            """;

        var symbols = CompileAndBind(source);

        var submitRef = symbols.References.FirstOrDefault(r => r.Name == "Submit" && r.Resolution is EventTarget);
        submitRef.Should().NotBeNull();
    }

    [Fact]
    public void Bind_MultipleReferences_AllResolved()
    {
        var source = """
            precept Order
            field A as integer
            field B as integer
            field C as integer <- A + B
            """;

        var symbols = CompileAndBind(source);

        var aRef = symbols.References.FirstOrDefault(r => r.Name == "A");
        var bRef = symbols.References.FirstOrDefault(r => r.Name == "B");

        aRef.Should().NotBeNull();
        bRef.Should().NotBeNull();
        aRef!.Resolution.Should().BeOfType<FieldTarget>();
        bRef!.Resolution.Should().BeOfType<FieldTarget>();
    }

    #endregion

    #region §4: Undeclared References

    [Fact]
    public void Bind_UndeclaredField_EmitsDiagnostic()
    {
        var source = """
            precept Order
            field Total as decimal <- Quantity * 10
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Bind_UndeclaredField_ResolutionIsUnresolved()
    {
        var source = """
            precept Order
            field Total as decimal <- Quantity * 10
            """;

        var symbols = CompileAndBind(source);

        var quantityRef = symbols.References.FirstOrDefault(r => r.Name == "Quantity");
        quantityRef.Should().NotBeNull();
        quantityRef!.Resolution.Should().BeOfType<UnresolvedTarget>();
    }

    [Fact]
    public void Bind_UndeclaredState_EmitsDiagnostic()
    {
        var source = """
            precept Order
            state Draft initial
            event Submit
            
            from Draft on Submit -> transition NonexistentState
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredState));
    }

    [Fact]
    public void Bind_UndeclaredEvent_EmitsDiagnostic()
    {
        var source = """
            precept Order
            state Draft initial
            state Submitted
            
            from Draft on NonexistentEvent -> transition Submitted
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredEvent));
    }

    [Fact]
    public void Bind_UndeclaredArg_EmitsDiagnostic()
    {
        var source = """
            precept Order
            field Price as decimal
            state Active initial
            event SetPrice(newPrice as decimal)
            
            from Active on SetPrice
                -> set Price = unknownArg
                -> no transition
            """;

        var symbols = CompileAndBind(source);

        // Should fail to resolve unknownArg
        symbols.Diagnostics.Should().Contain(d => 
            d.Code == nameof(DiagnosticCode.UndeclaredArg) || 
            d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    #endregion

    #region §5: Scoping (Event Arg Context)

    [Fact]
    public void Bind_EventArg_ResolvesToArgTarget()
    {
        var source = """
            precept Order
            field Price as decimal
            state Active initial
            event SetPrice(newPrice as decimal)
            
            from Active on SetPrice
                -> set Price = SetPrice.newPrice
                -> no transition
            """;

        var symbols = CompileAndBind(source);

        // Event args are accessed via Event.argName syntax
        var newPriceRef = symbols.References.FirstOrDefault(r => r.Name == "newPrice");
        // May be null if accessed via member syntax - check for ArgTarget resolution
        var argTargets = symbols.References.Where(r => r.Resolution is ArgTarget).ToList();
        argTargets.Should().NotBeEmpty("Event arg references should resolve to ArgTarget");
    }

    [Fact]
    public void Bind_ArgOutsideEventContext_NotResolved()
    {
        // Event args should not be visible in computed field expressions
        var source = """
            precept Order
            field Quantity as integer
            event SetQuantity(qty as integer)
            field Total as decimal <- qty * 10
            """;

        var symbols = CompileAndBind(source);

        // qty should not resolve in computed field context
        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Bind_FieldReferenceInEventContext_StillResolvesToField()
    {
        var source = """
            precept Order
            field Price as decimal
            field Quantity as integer
            state Active initial
            event SetPrice(newPrice as decimal)
            
            from Active on SetPrice when Quantity > 0
                -> set Price = SetPrice.newPrice
                -> no transition
            """;

        var symbols = CompileAndBind(source);

        var quantityRef = symbols.References.FirstOrDefault(r => r.Name == "Quantity");
        quantityRef.Should().NotBeNull();
        quantityRef!.Resolution.Should().BeOfType<FieldTarget>();
    }

    #endregion

    #region §6: BindingShadowsField

    [Fact]
    public void Bind_QuantifierBindingShadowsField_EmitsDiagnostic()
    {
        var source = """
            precept Order
            field Items as list of string
            field item as string
            state Active initial
            event Submit
            
            from Active on Submit when each item in Items (item != "") -> no transition
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.BindingShadowsField));
    }

    [Fact]
    public void Bind_QuantifierBindingNoConflict_NoDiagnostic()
    {
        var source = """
            precept Order
            field Items as list of string
            state Active initial
            event Submit
            
            from Active on Submit when each x in Items (x != "") -> no transition
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.BindingShadowsField));
    }

    [Fact]
    public void Bind_QuantifierBinding_ResolvesToBindingTarget()
    {
        var source = """
            precept Order
            field Items as list of string
            state Active initial
            event Submit
            
            from Active on Submit when each x in Items (x != "") -> no transition
            """;

        var symbols = CompileAndBind(source);

        var xRef = symbols.References.FirstOrDefault(r => r.Name == "x");
        xRef.Should().NotBeNull();
        xRef!.Resolution.Should().BeOfType<BindingTarget>();
    }

    #endregion

    #region §7: Forward References

    [Fact]
    public void Bind_ForwardFieldReference_EmitsDiagnostic()
    {
        var source = """
            precept Order
            field Total as decimal <- Quantity * Price
            field Quantity as integer
            field Price as decimal
            """;

        var symbols = CompileAndBind(source);

        // Forward references to Quantity and Price should be flagged
        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Bind_BackwardFieldReference_NoDiagnostic()
    {
        var source = """
            precept Order
            field Quantity as integer
            field Price as decimal
            field Total as decimal <- Quantity * Price
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Bind_ForwardReference_DeclarationOrderDeterminesValidity()
    {
        var source = """
            precept Order
            field A as integer <- B + 1
            field B as integer
            """;

        var symbols = CompileAndBind(source);

        var fieldA = symbols.Fields.Single(f => f.Name == "A");
        var fieldB = symbols.Fields.Single(f => f.Name == "B");

        fieldA.DeclarationOrder.Should().BeLessThan(fieldB.DeclarationOrder);
    }

    #endregion

    #region §8: Diagnostics Aggregation

    [Fact]
    public void Bind_MultipleErrors_AllDiagnosticsCollected()
    {
        var source = """
            precept Order
            field Name as string
            field Name as string
            state Draft
            state Draft
            event Submit
            event Submit
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DuplicateFieldName));
        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DuplicateStateName));
        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DuplicateEventName));
    }

    [Fact]
    public void Bind_MixedErrors_DuplicatesAndUndeclared()
    {
        var source = """
            precept Order
            field Total as decimal <- Unknown * 10
            field Total as integer
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.DuplicateFieldName));
        symbols.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
    }

    [Fact]
    public void Bind_DiagnosticsHaveCorrectSpans()
    {
        var source = """
            precept Order
            field Unknown as decimal <- Missing * 10
            """;

        var symbols = CompileAndBind(source);

        var undeclaredDiag = symbols.Diagnostics.FirstOrDefault(d => d.Code == nameof(DiagnosticCode.UndeclaredField));
        undeclaredDiag.Should().NotBeNull();
        undeclaredDiag.Span.Should().NotBe(default(SourceSpan));
    }

    #endregion

    #region §9: Empty and Minimal Precepts

    [Fact]
    public void Bind_EmptyPrecept_ReturnsEmptySymbolTable()
    {
        var source = """
            precept Empty
            """;

        var symbols = CompileAndBind(source);

        symbols.Fields.Should().BeEmpty();
        symbols.States.Should().BeEmpty();
        symbols.Events.Should().BeEmpty();
        symbols.References.Should().BeEmpty();
    }

    [Fact]
    public void Bind_FieldsOnly_NoErrors()
    {
        var source = """
            precept Order
            field Name as string
            field Quantity as integer
            """;

        var symbols = CompileAndBind(source);

        symbols.Fields.Should().HaveCount(2);
        symbols.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty();
    }

    [Fact]
    public void Bind_MinimalStateMachine_WorksCorrectly()
    {
        var source = """
            precept Order
            state Draft initial
            event Submit
            
            from Draft on Submit -> no transition
            """;

        var symbols = CompileAndBind(source);

        symbols.Diagnostics.Where(d => 
            d.Code == nameof(DiagnosticCode.UndeclaredState) || 
            d.Code == nameof(DiagnosticCode.UndeclaredEvent))
            .Should().BeEmpty();
        
        symbols.States.Should().ContainSingle();
        symbols.Events.Should().ContainSingle();
    }

    #endregion

    #region Test Helpers

    private static SymbolTable CompileAndBind(string source)
    {
        var tokens = Lexer.Lex(source);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);
        return Precept.Pipeline.NameBinder.Bind(manifest);
    }

    #endregion
}
