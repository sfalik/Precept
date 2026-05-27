using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests.GraphAnalyzerSuite;

/// <summary>
/// F-LANG-GRAPH-04 — FieldNeverSet emission coverage. The graph stage emits a
/// Warning for every field that has no write site discoverable in the
/// type-checked program; each established write-site shape suppresses.
/// </summary>
public class FieldNeverSetEmissionTests
{
    [Fact]
    public void Trips_OnDeclaredButNeverSet()
    {
        var (_, _, graph) = Analyze("""
            precept Widget
            field Priority as integer default 0
            state Draft initial
            state Done terminal
            event Submit
            from Draft on Submit -> transition Done
            """);

        graph.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.FieldNeverSet))
            .Should().ContainSingle()
            .Which.Should().Match<Diagnostic>(d =>
                d.Severity == Severity.Warning
                && d.Message.Contains("Priority"));
    }

    [Fact]
    public void NoTrip_OnSetActionInTransitionRow()
    {
        var (_, _, graph) = Analyze("""
            precept Widget
            field Priority as integer default 0
            state Draft initial
            state Done terminal
            event Submit(NewPriority as integer)
            from Draft on Submit -> set Priority = Submit.NewPriority -> transition Done
            """);

        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.FieldNeverSet));
    }

    [Fact]
    public void NoTrip_OnConstructionEventArgAssignment()
    {
        var (_, _, graph) = Analyze("""
            precept Widget
            field Priority as integer default 0
            state Draft initial
            event Create(InitialPriority as integer) initial
            on Create -> set Priority = Create.InitialPriority
            """);

        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.FieldNeverSet));
    }

    [Fact]
    public void NoTrip_OnFieldLevelEditable()
    {
        // Decision 4: caller-side write capability (field-level `editable`)
        // counts as a write site — runtime API can mutate it.
        var (_, _, graph) = Analyze("""
            precept Widget
            field Priority as integer default 0 editable
            state Draft initial
            """);

        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.FieldNeverSet));
    }

    [Fact]
    public void NoTrip_OnPerStateModifyEditable()
    {
        // Decision 4: per-state `modify F editable` grants caller-side write
        // capability while the entity is in that state.
        var (_, _, graph) = Analyze("""
            precept Widget
            field Priority as integer default 0
            state Draft initial
            in Draft modify Priority editable
            """);

        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.FieldNeverSet));
    }

    [Fact]
    public void NoTrip_OnComputedField()
    {
        // Computed fields are derived — the `<-` expression is the implicit
        // value-establishing site.
        var (_, _, graph) = Analyze("""
            precept Widget
            field Base as integer default 10 editable
            field Doubled as integer <- Base * 2
            state Draft initial
            """);

        graph.Diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.FieldNeverSet)
            && d.Message.Contains("Doubled"));
    }

    [Fact]
    public void NoTrip_OnStateEntryHook()
    {
        var (_, _, graph) = Analyze("""
            precept Widget
            field LastTouched as boolean default false
            state Draft initial
            state Done terminal
            event Submit
            to Done -> set LastTouched = true
            from Draft on Submit -> transition Done
            """);

        graph.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.FieldNeverSet));
    }

    [Fact]
    public void Trips_OnOptionalFieldNeverSet()
    {
        // The diagnostic does not require a `default` — the criterion is
        // "no write site." An `optional` field with no writes is still stuck.
        var (_, _, graph) = Analyze("""
            precept Widget
            field Maybe as string optional
            state Draft initial
            """);

        graph.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.FieldNeverSet))
            .Should().ContainSingle()
            .Which.Message.Should().Contain("Maybe");
    }

    [Fact]
    public void Trips_OnClearOnlyField()
    {
        // Decision 6: `clear` is ClearsContents, not EstablishesValue — does
        // NOT suppress FieldNeverSet. (Acceptance criterion 4.)
        var (_, _, graph) = Analyze("""
            precept Widget
            field Tags as set of string
            state Draft initial
            state Done terminal
            event Wipe
            from Draft on Wipe -> clear Tags -> transition Done
            """);

        graph.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.FieldNeverSet)
                && d.Message.Contains("Tags"))
            .Should().ContainSingle();
    }

    [Fact]
    public void Trips_OnRemoveOnlyField()
    {
        // Same Decision 6 rule for `remove`: removing from a collection does
        // not establish a value.
        var (_, _, graph) = Analyze("""
            precept Widget
            field Tags as set of string
            state Draft initial
            state Done terminal
            event Drop(T as string)
            from Draft on Drop -> remove Tags Drop.T -> transition Done
            """);

        graph.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.FieldNeverSet)
                && d.Message.Contains("Tags"))
            .Should().ContainSingle();
    }

    [Fact]
    public void NoTrip_OnAddedField()
    {
        // `add` IS EstablishesValue — value-establishing for collections.
        var (_, _, graph) = Analyze("""
            precept Widget
            field Tags as set of string
            state Draft initial
            state Done terminal
            event Tag(T as string)
            from Draft on Tag -> add Tags Tag.T -> transition Done
            """);

        graph.Diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.FieldNeverSet)
            && d.Message.Contains("Tags"));
    }

    [Fact]
    public void NoTrip_OnPutInLookup()
    {
        var (_, _, graph) = Analyze("""
            precept Widget
            field Scores as lookup of string : integer
            state Draft initial
            state Done terminal
            event Record(K as string, V as integer)
            from Draft on Record -> put Scores Record.K = Record.V -> transition Done
            """);

        graph.Diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.FieldNeverSet)
            && d.Message.Contains("Scores"));
    }

    private static (SemanticIndex Index, IReadOnlyList<Diagnostic> Diagnostics, StateGraph Graph) Analyze(string source)
    {
        var (index, diagnostics) = TypeCheckerTestHelpers.Check(source);
        return (index, diagnostics, Precept.Pipeline.GraphAnalyzer.Analyze(index));
    }
}
