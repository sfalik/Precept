using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Runtime;
using Xunit;

namespace Precept.Tests.Runtime;

/// <summary>
/// Slice 8 runtime tests: EventOutcome.Created, Precept.Create(),
/// Version.AvailableEvents filtering, and fire-once enforcement.
/// </summary>
public class RuntimeConstructionTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Compiles a precept DSL string and builds a Precept runtime object.</summary>
    private static global::Precept.Runtime.Precept Build(string preceptText)
    {
        var compilation = Compiler.Compile(preceptText);
        compilation.HasErrors.Should().BeFalse(
            because: $"test precept should compile cleanly but got: {string.Join(", ", compilation.Diagnostics.AsEnumerable().Select(d => d.Message))}");
        return global::Precept.Runtime.Precept.From(compilation);
    }

    // ── EventOutcome.Created shape ───────────────────────────────────────────

    [Fact]
    public void EventOutcome_Created_Exists()
    {
        typeof(EventOutcome.Created).IsSealed.Should().BeTrue();
        typeof(EventOutcome).IsAbstract.Should().BeTrue();
        typeof(EventOutcome.Created).IsSubclassOf(typeof(EventOutcome)).Should().BeTrue();
    }

    [Fact]
    public void EventOutcome_Created_IsPatternMatchable()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            """);

        EventOutcome outcome = precept.Create((System.Text.Json.JsonElement?)null);
        var matched = outcome switch { EventOutcome.Created c => c.Result, _ => null };
        matched.Should().NotBeNull();
    }

    // ── Precept.Create() ─────────────────────────────────────────────────────

    [Fact]
    public void Precept_Create_ReturnsCreated()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Create initial
            on Create -> set Count = 42
            """);

        precept.Create((System.Text.Json.JsonElement?)null).Should().BeOfType<EventOutcome.Created>();
    }

    [Fact]
    public void Precept_Create_ReturnsRejected()
    {
        // Guarded success row (skipped at spike level) + unconditional reject row.
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Create(InputCount as integer) initial
            on Create when InputCount > 0 -> set Count = InputCount
            on Create -> reject "construction not allowed"
            """);

        var outcome = precept.Create((System.Text.Json.JsonElement?)null);
        outcome.Should().BeOfType<EventOutcome.Rejected>();
        ((EventOutcome.Rejected)outcome).Reason.Should().Be("construction not allowed");
    }

    [Fact]
    public void Precept_Create_SetsInitialState()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Create initial
            on Create -> set Count = 1
            """);

        var outcome = precept.Create((System.Text.Json.JsonElement?)null);
        outcome.Should().BeOfType<EventOutcome.Created>();
        ((EventOutcome.Created)outcome).Result.State.Should().Be("Draft");
    }

    [Fact]
    public void Precept_Create_NoInitialEvent_ReturnsCreated()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Open initial terminal
            """);

        var outcome = precept.Create((System.Text.Json.JsonElement?)null);
        outcome.Should().BeOfType<EventOutcome.Created>();
        ((EventOutcome.Created)outcome).Result.State.Should().Be("Open");
    }

    // ── Version.AvailableEvents filtering ───────────────────────────────────

    [Fact]
    public void Version_AvailableEvents_ExcludesInitial()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial
            state Done terminal
            event Create initial
            event Complete
            on Create -> set Count = 1
            from Draft on Complete -> transition Done
            """);

        var outcome = precept.Create((System.Text.Json.JsonElement?)null);
        outcome.Should().BeOfType<EventOutcome.Created>();
        var version = ((EventOutcome.Created)outcome).Result;

        version.AvailableEvents.Select(e => e.Name).ToList()
            .Should().NotContain("Create", because: "initial events must be excluded from AvailableEvents");
    }

    [Fact]
    public void Version_AvailableEvents_IncludesNonInitial()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial
            state Done terminal
            event Create initial
            event Complete
            on Create -> set Count = 1
            from Draft on Complete -> transition Done
            """);

        var outcome = precept.Create((System.Text.Json.JsonElement?)null);
        outcome.Should().BeOfType<EventOutcome.Created>();
        var version = ((EventOutcome.Created)outcome).Result;

        version.AvailableEvents.Select(e => e.Name).ToList()
            .Should().Contain("Complete", because: "non-initial events must appear in AvailableEvents");
    }

    // ── Fire-once enforcement ────────────────────────────────────────────────

    [Fact]
    public void Fire_InitialEvent_FireOnce_FirstSucceeds()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Create initial
            on Create -> set Count = 1
            """);

        precept.Create((System.Text.Json.JsonElement?)null)
            .Should().BeOfType<EventOutcome.Created>(
                because: "the first firing of an initial event via Create() must succeed");
    }

    [Fact]
    public void Fire_InitialEvent_FireOnce_Enforced()
    {
        var precept = Build("""
            precept Widget
            field Count as integer default 0
            state Draft initial terminal
            event Create initial
            on Create -> set Count = 1
            """);

        var createOutcome = precept.Create((System.Text.Json.JsonElement?)null);
        createOutcome.Should().BeOfType<EventOutcome.Created>();
        var version = ((EventOutcome.Created)createOutcome).Result;

        version.Fire("Create", (System.Text.Json.JsonElement?)null)
            .Should().BeOfType<EventOutcome.Rejected>(
                because: "initial events can only be used during construction — re-firing must be rejected");
    }
}
