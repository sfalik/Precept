using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

// ════════════════════════════════════════════════════════════════════
// Parser Tests — stateless precept parsing
// ════════════════════════════════════════════════════════════════════

public class PreceptStatelessParserTests
{
    [Fact]
    public void Parse_StatelessPrecept_IsStatelessTrue()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Email as string default ""
            field Age as number default 0
            """;

        var def = PreceptParser.Parse(dsl);

        def.IsStateless.Should().BeTrue();
        def.States.Should().BeEmpty();
        def.Fields.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_StatelessPrecept_WithRootEditAll_ParsesCorrectly()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Email as string default ""
            field Age as number default 0

            edit all
            """;

        var def = PreceptParser.Parse(dsl);

        def.IsStateless.Should().BeTrue();
        def.EditBlocks.Should().NotBeNull();
        def.EditBlocks.Should().HaveCount(1);
        def.EditBlocks![0].State.Should().BeNull();
        def.EditBlocks![0].FieldNames.Should().ContainSingle().Which.Should().Be("all");
    }

    [Fact]
    public void Parse_StatelessPrecept_WithRootEditFields_ParsesCorrectly()
    {
        const string dsl = """
            precept FeeSchedule

            field BaseFee as number default 0
            field Discount as number default 0
            field TaxRate as number default 0.1

            edit BaseFee, Discount
            """;

        var def = PreceptParser.Parse(dsl);

        def.IsStateless.Should().BeTrue();
        def.EditBlocks.Should().NotBeNull();
        def.EditBlocks.Should().HaveCount(1);
        def.EditBlocks![0].State.Should().BeNull();
        def.EditBlocks![0].FieldNames.Should().BeEquivalentTo("BaseFee", "Discount");
    }

    [Fact]
    public void Parse_RootEdit_MultipleFields_ParsesAllFields()
    {
        const string dsl = """
            precept Config

            field F1 as string default ""
            field F2 as number default 0
            field F3 as boolean default false

            edit F1, F2, F3
            """;

        var def = PreceptParser.Parse(dsl);

        def.EditBlocks!.Should().HaveCount(1);
        def.EditBlocks![0].FieldNames.Should().HaveCount(3);
        def.EditBlocks![0].FieldNames.Should().BeEquivalentTo("F1", "F2", "F3");
    }

    [Fact]
    public void Parse_C12_EmptyPrecept_ProducesDiagnostic()
    {
        const string dsl = "precept Empty";

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        diagnostics.Should().NotBeEmpty();
        diagnostics.Any(d => d.Code == "PRECEPT012").Should().BeTrue();
    }

    [Fact]
    public void Parse_C12_FieldsOnly_DoesNotFire()
    {
        const string dsl = """
            precept DataOnly

            field Name as string default ""
            field Score as number default 0
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Code == "PRECEPT012");
    }

    [Fact]
    public void Parse_C13_StatelessPrecept_DoesNotFire()
    {
        const string dsl = """
            precept DataOnly

            field Name as string default ""
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().NotContain(d => d.Code == "PRECEPT013");
    }

    [Fact]
    public void Parse_C13_StatefulNoInitial_Fires()
    {
        const string dsl = """
            precept T
            field X as number default 0
            state A
            state B
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        diagnostics.Should().Contain(d => d.Code == "PRECEPT013");
    }

    [Fact]
    public void Parse_C49_EventOnStateless_ProducesWarning()
    {
        const string dsl = """
            precept DataOnly

            field Name as string default ""

            event UpdateName with NewName as string
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT049");
    }

    [Fact]
    public void Parse_C55_RootEditWithStates_ProducesDiagnostic()
    {
        const string dsl = """
            precept Hybrid

            field F as string default ""

            state A initial

            edit F
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT055");
    }

    [Fact]
    public void Parse_C49_MultipleEvents_ProducesOneWarningPerEvent()
    {
        const string dsl = """
            precept Config
            field Setting as string default ""
            event Changed
            event Reset
            event Refreshed
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        var c49Warnings = result.Diagnostics.Where(d => d.Code == "PRECEPT049").ToList();
        c49Warnings.Should().HaveCount(3);
    }
}

// ════════════════════════════════════════════════════════════════════
// Type Checker Tests
// ════════════════════════════════════════════════════════════════════

public class PreceptStatelessTypeCheckerTests
{
    [Fact]
    public void TypeCheck_StatelessWithInvariant_EnforcedAtCompile()
    {
        const string dsl = """
            precept FeeSchedule

            field BaseFee as number default 0
            field Discount as number default 0

            invariant Discount <= BaseFee because "Discount cannot exceed base fee"

            edit BaseFee, Discount
            """;

        // Compiles OK because default values satisfy the invariant (0 <= 0)
        var result = PreceptCompiler.CompileFromText(dsl);

        result.Engine.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void TypeCheck_StatelessWithEvent_ProducesC49Warning()
    {
        const string dsl = """
            precept Profile

            field Name as string default ""

            event Rename with NewName as string
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d =>
            d.Code == "PRECEPT049" && d.Message.Contains("Rename"));
    }

    [Fact]
    public void TypeCheck_C50_UnreachableState_IsWarningNotHint()
    {
        const string dsl = """
            precept T
            field X as number default 0
            state A initial
            state B
            event Go
            from A on Go -> reject "blocked"
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        var c50 = result.Diagnostics.FirstOrDefault(d => d.Code == "PRECEPT050");
        if (c50 is not null)
            c50.Severity.Should().Be(ConstraintSeverity.Warning);
    }
}

// ════════════════════════════════════════════════════════════════════
// Runtime Tests — CreateInstance and basic lifecycle
// ════════════════════════════════════════════════════════════════════

public class PreceptStatelessRuntimeTests
{
    private static PreceptEngine CompileStateless(string dsl)
    {
        var def = PreceptParser.Parse(dsl);
        return PreceptCompiler.Compile(def);
    }

    [Fact]
    public void IsStateless_FieldsOnlyPrecept_ReturnsTrue()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            """;

        var engine = CompileStateless(dsl);

        engine.IsStateless.Should().BeTrue();
    }

    [Fact]
    public void IsStateless_StatefulPrecept_ReturnsFalse()
    {
        const string dsl = """
            precept T
            state A initial
            """;

        var engine = CompileStateless(dsl);

        engine.IsStateless.Should().BeFalse();
    }

    [Fact]
    public void CreateInstance_Stateless_CurrentStateIsNull()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            """;

        var engine = CompileStateless(dsl);
        var instance = engine.CreateInstance();

        instance.CurrentState.Should().BeNull();
    }

    [Fact]
    public void CreateInstance_Stateless_ReturnsInstanceWithNullState()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Score as number default 0
            """;

        var engine = CompileStateless(dsl);
        var instance = engine.CreateInstance();

        instance.Should().NotBeNull();
        instance.CurrentState.Should().BeNull();
        instance.WorkflowName.Should().Be("Profile");
    }

    [Fact]
    public void CreateInstance_Stateless_WithData_InitializesFieldValues()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Score as number default 0
            """;

        var engine = CompileStateless(dsl);
        var data = new Dictionary<string, object?> { ["Name"] = "Alice", ["Score"] = 42.0 };
        var instance = engine.CreateInstance(data);

        instance.InstanceData["Name"].Should().Be("Alice");
        instance.InstanceData["Score"].Should().Be(42.0);
    }

    [Fact]
    public void CreateInstance_StatelessWithState_ThrowsArgumentException()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            """;

        var engine = CompileStateless(dsl);
        var act = () => engine.CreateInstance("SomeState");

        act.Should().Throw<ArgumentException>().WithMessage("*stateless*");
    }

    [Fact]
    public void Fire_StatelessInstance_ReturnsUndefined()
    {
        const string dsl = """
            precept Profile

            field Name as string default ""

            event UpdateName with NewName as string
            """;

        var result = PreceptCompiler.CompileFromText(dsl);
        var engine = result.Engine!;
        var instance = engine.CreateInstance();

        var fireResult = engine.Fire(instance, "UpdateName",
            new Dictionary<string, object?> { ["NewName"] = "Bob" });

        fireResult.Outcome.Should().Be(TransitionOutcome.Undefined);
    }

    [Fact]
    public void Inspect_StatelessInstance_ByEvent_ReturnsUndefined()
    {
        const string dsl = """
            precept Profile

            field Name as string default ""

            event UpdateName with NewName as string
            """;

        var result = PreceptCompiler.CompileFromText(dsl);
        var engine = result.Engine!;
        var instance = engine.CreateInstance();

        var inspectResult = engine.Inspect(instance, "UpdateName");

        inspectResult.Outcome.Should().Be(TransitionOutcome.Undefined);
    }

    [Fact]
    public void Inspect_StatelessInstance_CurrentStateIsNull()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            edit all
            """;

        var engine = CompileStateless(dsl);
        var instance = engine.CreateInstance();

        var inspectionResult = engine.Inspect(instance);

        inspectionResult.CurrentState.Should().BeNull();
    }

    [Fact]
    public void Inspect_StatelessInstance_AllEditable_ReturnedInEditableFields()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            edit all
            """;

        var engine = CompileStateless(dsl);
        var instance = engine.CreateInstance();

        var inspectionResult = engine.Inspect(instance);

        inspectionResult.EditableFields.Should().NotBeNull();
        var editableNames = inspectionResult.EditableFields!.Select(f => f.FieldName).ToArray();
        editableNames.Should().Contain("Name");
        editableNames.Should().Contain("Age");
    }

    [Fact]
    public void Inspect_StatelessInstance_SpecificEdit_CorrectFields()
    {
        const string dsl = """
            precept FeeSchedule
            field BaseFee as number default 0
            field Discount as number default 0
            field TaxRate as number default 0.1
            edit BaseFee, Discount
            """;

        var engine = CompileStateless(dsl);
        var instance = engine.CreateInstance();

        var inspectionResult = engine.Inspect(instance);

        inspectionResult.EditableFields.Should().NotBeNull();
        var editableNames = inspectionResult.EditableFields!.Select(f => f.FieldName).ToArray();
        editableNames.Should().Contain("BaseFee");
        editableNames.Should().Contain("Discount");
        editableNames.Should().NotContain("TaxRate");
    }
}

// ════════════════════════════════════════════════════════════════════
// Edit / Update Tests
// ════════════════════════════════════════════════════════════════════

public class PreceptStatelessUpdateTests
{
    [Fact]
    public void Update_Stateless_EditableField_ReturnsUpdateOutcome()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            edit Name, Age
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        var result = engine.Update(instance, patch => patch.Set("Name", "Alice"));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.IsSuccess.Should().BeTrue();
        result.UpdatedInstance.Should().NotBeNull();
        result.UpdatedInstance!.InstanceData["Name"].Should().Be("Alice");
    }

    [Fact]
    public void Update_Stateless_UneditableField_ReturnsUneditableFieldOutcome()
    {
        const string dsl = """
            precept FeeSchedule
            field BaseFee as number default 0
            field TaxRate as number default 0.1
            edit BaseFee
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        var result = engine.Update(instance, patch => patch.Set("TaxRate", 0.2));

        result.Outcome.Should().Be(UpdateOutcome.UneditableField);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Update_Stateless_WithNull_CurrentState_WorksCorrectly()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            edit Name
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        // Stateless instance always has null CurrentState
        instance.CurrentState.Should().BeNull();

        var result = engine.Update(instance, patch => patch.Set("Name", "Bob"));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.UpdatedInstance!.CurrentState.Should().BeNull();
    }

    [Fact]
    public void Update_Stateless_EditAll_AllFieldsEditable()
    {
        const string dsl = """
            precept Profile
            field Name as string default ""
            field Age as number default 0
            field Score as number default 0
            edit all
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        var r1 = engine.Update(instance, patch => patch.Set("Name", "Alice"));
        var r2 = engine.Update(r1.UpdatedInstance!, patch => patch.Set("Age", 30.0));
        var r3 = engine.Update(r2.UpdatedInstance!, patch => patch.Set("Score", 95.0));

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        r3.IsSuccess.Should().BeTrue();
        r3.UpdatedInstance!.InstanceData["Name"].Should().Be("Alice");
        r3.UpdatedInstance!.InstanceData["Age"].Should().Be(30.0);
        r3.UpdatedInstance!.InstanceData["Score"].Should().Be(95.0);
    }

    [Fact]
    public void Update_Stateless_EditSpecific_OnlyNamedFieldsEditable()
    {
        const string dsl = """
            precept FeeSchedule
            field BaseFee as number default 0
            field Discount as number default 0
            field TaxRate as number default 0.1
            edit BaseFee, Discount
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        var editOk = engine.Update(instance, patch => patch.Set("BaseFee", 100.0));
        var editBlocked = engine.Update(instance, patch => patch.Set("TaxRate", 0.2));

        editOk.Outcome.Should().Be(UpdateOutcome.Update);
        editBlocked.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void Update_Stateless_InvariantViolation_ReturnsViolation()
    {
        const string dsl = """
            precept FeeSchedule
            field BaseFee as number default 50
            field Discount as number default 0
            invariant Discount <= BaseFee because "Discount cannot exceed base fee"
            edit BaseFee, Discount
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        // Set Discount to 200 while BaseFee is 50 — violates invariant
        var result = engine.Update(instance, patch => patch.Set("Discount", 200.0));

        result.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);
        result.IsSuccess.Should().BeFalse();
        result.Violations.Should().NotBeEmpty();
        result.Violations.Should().Contain(v => v.Message.Contains("Discount cannot exceed base fee"));
    }

    [Fact]
    public void Update_StatefulPrecept_EditAll_ExpandsToAllFields()
    {
        const string dsl = """
            precept Widget
            field Name as string default ""
            field Color as string default "red"
            field Weight as number default 0
            state Active initial
            in Active edit all
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance("Active");

        var result = engine.Update(instance, patch => patch.Set("Name", "Test"));

        result.Outcome.Should().Be(UpdateOutcome.Update);
        result.IsSuccess.Should().BeTrue();
        result.UpdatedInstance!.InstanceData["Name"].Should().Be("Test");
    }
}

// ════════════════════════════════════════════════════════════════════
// Diagnostics Code Tests
// ════════════════════════════════════════════════════════════════════

public class PreceptStatelessDiagnosticsTests
{
    [Fact]
    public void Diagnostics_C12_Code_IsPRECEPT012()
    {
        var result = PreceptCompiler.CompileFromText("precept Empty");

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT012");
    }

    [Fact]
    public void Diagnostics_C55_Code_IsPRECEPT055()
    {
        const string dsl = """
            precept T
            field F as string default ""
            state A initial
            edit F
            """;

        var result = PreceptCompiler.CompileFromText(dsl);

        result.Diagnostics.Should().Contain(d => d.Code == "PRECEPT055");
    }

    [Fact]
    public void Diagnostics_C50_Severity_IsWarning()
    {
        var c50 = DiagnosticCatalog.C50;

        c50.Severity.Should().Be(ConstraintSeverity.Warning);
    }

    [Fact]
    public void Diagnostics_C49_Severity_IsWarning()
    {
        var c49 = DiagnosticCatalog.C49;

        c49.Severity.Should().Be(ConstraintSeverity.Warning);
    }

    [Fact]
    public void Diagnostics_C55_Severity_IsError()
    {
        var c55 = DiagnosticCatalog.C55;

        c55.Severity.Should().Be(ConstraintSeverity.Error);
    }
}

// ════════════════════════════════════════════════════════════════════
// Integration Tests
// ════════════════════════════════════════════════════════════════════

public class PreceptStatelessIntegrationTests
{
    [Fact]
    public void Integration_StatelessPrecept_EditAll_FullWorkflow()
    {
        const string dsl = """
            precept CustomerProfile

            field Name as string default ""
            field Email as string default ""
            field Age as number default 0

            edit all
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        // Verify stateless
        engine.IsStateless.Should().BeTrue();
        instance.CurrentState.Should().BeNull();

        // Inspect: editable fields returned
        var inspection = engine.Inspect(instance);
        inspection.CurrentState.Should().BeNull();
        inspection.EditableFields.Should().NotBeNull();
        inspection.EditableFields!.Select(f => f.FieldName).Should().Contain("Name", "Email", "Age");

        // Update fields in sequence
        var r1 = engine.Update(instance, p => p.Set("Name", "Alice"));
        r1.IsSuccess.Should().BeTrue();

        var r2 = engine.Update(r1.UpdatedInstance!, p => p.Set("Email", "alice@example.com"));
        r2.IsSuccess.Should().BeTrue();
        r2.UpdatedInstance!.CurrentState.Should().BeNull();
        r2.UpdatedInstance!.InstanceData["Name"].Should().Be("Alice");
        r2.UpdatedInstance!.InstanceData["Email"].Should().Be("alice@example.com");
    }

    [Fact]
    public void Integration_StatelessPrecept_EditSpecific_FullWorkflow()
    {
        const string dsl = """
            precept FeeSchedule

            field BaseFee as number default 100
            field Discount as number default 0
            field TaxRate as number default 0.1

            edit BaseFee, Discount
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        // Editable fields work
        var goodResult = engine.Update(instance, p => p.Set("BaseFee", 200.0));
        goodResult.IsSuccess.Should().BeTrue();

        // Non-editable field blocked
        var blockedResult = engine.Update(instance, p => p.Set("TaxRate", 0.2));
        blockedResult.Outcome.Should().Be(UpdateOutcome.UneditableField);
    }

    [Fact]
    public void Integration_StatelessWithInvariant_ViolationCaughtOnUpdate()
    {
        const string dsl = """
            precept BudgetProfile

            field Budget as number default 1000
            field Spent as number default 0

            invariant Spent <= Budget because "Cannot exceed budget"

            edit Budget, Spent
            """;

        var engine = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = engine.CreateInstance();

        // Valid update
        var good = engine.Update(instance, p => p.Set("Spent", 500.0));
        good.IsSuccess.Should().BeTrue();

        // Violation: spent exceeds budget
        var bad = engine.Update(good.UpdatedInstance!, p => p.Set("Spent", 2000.0));
        bad.Outcome.Should().Be(UpdateOutcome.ConstraintFailure);
        bad.Violations.Should().Contain(v => v.Message.Contains("Cannot exceed budget"));
    }

    [Fact]
    public void Integration_Graduation_StatelessToStateful_CompilesBothClean()
    {
        const string statelessDsl = """
            precept ProductCatalog

            field SKU as string default ""
            field Price as number default 0
            field InStock as boolean default true

            edit all
            """;

        const string statefulDsl = """
            precept OrderWorkflow

            field OrderId as string default ""
            field Total as number default 0

            state Draft initial
            state Submitted
            state Fulfilled

            event Submit
            event Fulfill

            from Draft on Submit -> transition Submitted
            from Submitted on Fulfill -> transition Fulfilled
            """;

        var statelessResult = PreceptCompiler.CompileFromText(statelessDsl);
        var statefulResult = PreceptCompiler.CompileFromText(statefulDsl);

        statelessResult.HasErrors.Should().BeFalse();
        statelessResult.Engine!.IsStateless.Should().BeTrue();

        statefulResult.HasErrors.Should().BeFalse();
        statefulResult.Engine!.IsStateless.Should().BeFalse();
    }

    [Fact]
    public void Regression_AllExistingSamples_CompileClean()
    {
        var samplesDir = FindSamplesDir();
        var sampleFiles = Directory.GetFiles(samplesDir, "*.precept");

        sampleFiles.Should().NotBeEmpty("samples directory should contain .precept files");

        var failures = new List<string>();
        foreach (var path in sampleFiles)
        {
            var text = File.ReadAllText(path);
            var result = PreceptCompiler.CompileFromText(text);
            var errors = result.Diagnostics.Where(d => d.Severity == ConstraintSeverity.Error).ToList();
            if (errors.Count > 0)
            {
                var msgs = string.Join("; ", errors.Select(e => $"{e.Code}: {e.Message}"));
                failures.Add($"{Path.GetFileName(path)}: {msgs}");
            }
        }

        failures.Should().BeEmpty("all sample .precept files should compile without errors");
    }

    private static string FindSamplesDir()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples");
            if (Directory.Exists(candidate) &&
                File.Exists(Path.Combine(current.FullName, "Precept.slnx")))
                return candidate;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate samples/ from test output directory.");
    }
}
