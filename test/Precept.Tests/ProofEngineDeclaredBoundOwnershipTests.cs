using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Declared-bound obligation ownership: numeric default bounds (OutOfRange), numeric
/// computed-field result bounds (NumericOverflow), and string-length defaults
/// (LengthBoundViolation) are proof-owned and single-coded, with the assignment-qualifier
/// residual proof-owned except the TypedConditional carve-out. Full-pipeline only
/// (Compiler.Compile) — the proof stage must run.
/// </summary>
public class ProofEngineDeclaredBoundOwnershipTests
{
    private static Diagnostic[] Of(string source, DiagnosticCode code) =>
        Compiler.Compile(source).Diagnostics.Where(d => d.Code == code.ToString()).ToArray();

    // ── Stage relabels (D3) ────────────────────────────────────────────────────

    [Fact]
    public void NumericOverflow_Stage_IsProof() =>
        Diagnostics.GetMeta(DiagnosticCode.NumericOverflow).Stage.Should().Be(DiagnosticStage.Proof);

    [Fact]
    public void OutOfRange_Stage_IsProof() =>
        Diagnostics.GetMeta(DiagnosticCode.OutOfRange).Stage.Should().Be(DiagnosticStage.Proof);

    // ── Numeric default → exactly one OutOfRange (Proof), no NumericOverflow ──────

    [Theory]
    // literal (bare decimal)
    [InlineData("precept E\nfield A as decimal nonnegative default -1")]
    [InlineData("precept E\nfield A as decimal positive default 0")]
    [InlineData("precept E\nfield A as number min 10 max 100 default 5")]
    // typed-constant
    [InlineData("precept E\nfield A as money in 'USD' nonnegative default '-1.00 USD'")]
    [InlineData("precept E\nfield A as money in 'USD' min '100.00 USD' default '50.00 USD'")]
    [InlineData("precept E\nfield A as duration nonnegative default '-14 hours'")]
    [InlineData("precept E\nfield A as period nonnegative default '-1 month'")]
    public void NumericFieldDefault_BelowBound_EmitsExactlyOneOutOfRange_Proof(string source)
    {
        var compilation = Compiler.Compile(source);
        var outOfRange = compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.OutOfRange)).ToArray();
        outOfRange.Should().ContainSingle("a numeric default below its declared bound emits exactly one OutOfRange");
        outOfRange[0].Stage.Should().Be(DiagnosticStage.Proof);
        compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty("a declared default value is OutOfRange, never NumericOverflow");
    }

    [Fact]
    public void NumericArgDefault_BelowBound_EmitsExactlyOneOutOfRange_Proof()
    {
        const string source = """
            precept E
            state Open initial
            event Begin(Amount as money in 'USD' nonnegative default '-1.00 USD')
            from Open on Begin -> transition Open
            """;
        var compilation = Compiler.Compile(source);
        var outOfRange = compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.OutOfRange)).ToArray();
        outOfRange.Should().ContainSingle("an arg default below its declared bound emits exactly one OutOfRange (no double-emit)");
        outOfRange[0].Stage.Should().Be(DiagnosticStage.Proof);
        compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow)).Should().BeEmpty();
    }

    [Fact]
    public void NumericDefault_ViolatingMultipleBounds_EmitsExactlyOneOutOfRange()
    {
        // default 0 violates BOTH `min 10` and `nonzero`; the prior type-stage check reported
        // the first declared bound and stopped, so the relocation must still emit exactly one.
        var field = Compiler.Compile("precept E\nfield A as number min 10 nonzero default 0")
            .Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.OutOfRange)).ToArray();
        field.Should().ContainSingle("a default violating several numeric bounds emits one OutOfRange, not one per bound");
        field[0].Message.Should().Contain("min", "the first declared violated bound is reported, matching the prior check");

        const string argSource = """
            precept E
            state Open initial
            event Begin(N as number min 10 nonzero default 0)
            from Open on Begin -> transition Open
            """;
        Compiler.Compile(argSource).Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.OutOfRange))
            .Should().ContainSingle("an arg default violating several bounds also emits exactly one OutOfRange");
    }

    [Fact]
    public void DecimalDefault_DisplayValue_IsCultureInvariant()
    {
        // The OutOfRange display value must be InvariantCulture — a decimal renders "1.50",
        // never the locale-formatted "1,50" — so the message is deterministic across locales.
        var prior = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var diags = Of("precept E\nfield A as decimal min 2 default 1.50", DiagnosticCode.OutOfRange);
            diags.Should().ContainSingle();
            diags[0].Message.Should().Contain("1.50").And.NotContain("1,50");
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = prior;
        }
    }

    [Fact]
    public void NumericFieldDefault_ResolvableInterpolated_ExceedsBound_EmitsOutOfRange_Proof()
    {
        // '{n} kg' with n max 10 → interval max 10000 g exceeds field max 5000 g.
        const string source = """
            precept E
            field n as integer max 10 default 0
            field x as quantity in 'kg' max '5 kg' default '{n} kg'
            state Active initial
            """;
        var compilation = Compiler.Compile(source);
        var outOfRange = compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.OutOfRange)).ToArray();
        outOfRange.Should().ContainSingle("a resolvable-interval interpolated default exceeding its bound emits OutOfRange");
        outOfRange[0].Stage.Should().Be(DiagnosticStage.Proof);
        compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow)).Should().BeEmpty();
    }

    [Fact]
    public void NumericFieldDefault_WithinBound_CompilesClean()
    {
        const string source = "precept E\nfield A as number min 0 max 100 default 50";
        Compiler.Compile(source).Diagnostics
            .Where(d => d.Code is nameof(DiagnosticCode.OutOfRange) or nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty("an in-bounds default has no bound violation");
    }

    // ── Computed-field result vs bounds (D4) → NumericOverflow (Proof) ───────────

    [Fact]
    public void ComputedNumericField_ResultExceedsBound_EmitsOneNumericOverflow_Proof()
    {
        // base max 50; computed c <- base + base → interval up to 100 > 60.
        const string source = """
            precept E
            field b as integer min 0 max 50 default 0
            field c as integer max 60 <- b + b
            state Active initial
            """;
        var overflow = Of(source, DiagnosticCode.NumericOverflow);
        overflow.Should().ContainSingle("a computed numeric field whose result interval exceeds its declared max emits NumericOverflow");
        overflow[0].Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Fact]
    public void ComputedNumericField_ResultWithinBound_CompilesClean()
    {
        const string source = """
            precept E
            field b as integer min 0 max 20 default 0
            field c as integer max 60 <- b + b
            state Active initial
            """;
        Compiler.Compile(source).Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty("base+base ≤ 40 ≤ 60 — within bound");
    }

    // ── String-length defaults (D8) → LengthBoundViolation (Proof) ───────────────

    [Fact]
    public void StringFieldDefault_TooShort_EmitsOneLengthBoundViolation_Proof()
    {
        const string source = "precept E\nfield Code as string minlength 5 maxlength 10 default \"ab\"";
        var v = Of(source, DiagnosticCode.LengthBoundViolation);
        v.Should().ContainSingle("a string default below minlength emits one LengthBoundViolation");
        v[0].Stage.Should().Be(DiagnosticStage.Proof);
    }

    [Fact]
    public void StringFieldDefault_TooLong_EmitsLengthBoundViolation_Proof()
    {
        const string source = "precept E\nfield Code as string maxlength 3 default \"abcdef\"";
        Of(source, DiagnosticCode.LengthBoundViolation).Should().ContainSingle();
    }

    [Fact]
    public void StringFieldDefault_WithinBounds_CompilesClean()
    {
        const string source = "precept E\nfield Code as string minlength 2 maxlength 8 default \"abcd\"";
        Of(source, DiagnosticCode.LengthBoundViolation).Should().BeEmpty();
    }

    [Fact]
    public void StringFieldDefault_NotEmpty_EmptyString_EmitsLengthBoundViolation()
    {
        // notempty folds to minlength 1 — an empty default violates it.
        const string source = "precept E\nfield Code as string notempty default \"\"";
        Of(source, DiagnosticCode.LengthBoundViolation).Should().ContainSingle("notempty string default \"\" violates the min=1 length bound");
    }

    [Fact]
    public void StringFieldDefault_NotEmpty_NonEmptyString_CompilesClean()
    {
        const string source = "precept E\nfield Code as string notempty default \"x\"";
        Of(source, DiagnosticCode.LengthBoundViolation).Should().BeEmpty();
    }

    [Fact]
    public void StringArgDefault_TooShort_EmitsLengthBoundViolation_Proof()
    {
        const string source = """
            precept E
            state Open initial
            event Begin(Code as string minlength 5 default "ab")
            from Open on Begin -> transition Open
            """;
        var v = Of(source, DiagnosticCode.LengthBoundViolation);
        v.Should().ContainSingle();
        v[0].Stage.Should().Be(DiagnosticStage.Proof);
    }

    // ── Qualifier residual (D5): default/computed → Proof; conditional → Type ────

    // The producing stage of PRE0141 is not observable via Diagnostic.Stage (that is the code's
    // meta stage, always Proof). The observable proof-ownership signal is whether the type checker
    // STAMPED an AssignmentQualifier proof obligation (proof-owned) vs emitted the diagnostic
    // directly (type-owned, the TypedConditional carve-out).

    [Fact]
    public void OpenQualifierFieldDefault_PRE0141_IsProofOwned()
    {
        // referenced fields declared first (backward ref); the default reads f1.unit where f1 is an
        // open quantity → open-axis assignment-qualifier residual stamped as a proof obligation.
        const string source = """
            precept E
            field f1 as quantity default '1 kg'
            field n as integer default 1
            field target as quantity of 'mass' default '{n} {f1.unit}'
            state Active initial
            """;
        var compilation = Compiler.Compile(source);
        compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility))
            .Should().NotBeEmpty("an open-qualifier field default emits PRE0141");
        compilation.Proof.Obligations
            .Where(o => o.Requirement is AssignmentQualifierProofRequirement && o.Context is FieldDefaultContext)
            .Should().NotBeEmpty("the open-qualifier field default residual is a stamped proof obligation (proof-owned)");
    }

    [Fact]
    public void TypedConditionalQualifier_FieldDefault_PRE0141_StaysTypeOwned()
    {
        // A conditional-valued open default keeps the type-stage carve-out (cannot be sited
        // per-branch) — PRE0141 emits directly, NO assignment-qualifier obligation is stamped.
        const string source = """
            precept E
            field left as quantity default '1 kg'
            field right as quantity default '2 kg'
            field flag as boolean default true
            field target as quantity of 'mass' default (if flag then left else right)
            state Active initial
            """;
        var compilation = Compiler.Compile(source);
        compilation.Diagnostics.Where(d => d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility))
            .Should().NotBeEmpty("a conditional-valued open default emits PRE0141");
        compilation.Proof.Obligations
            .Where(o => o.Requirement is AssignmentQualifierProofRequirement && o.Context is FieldDefaultContext)
            .Should().BeEmpty("a TypedConditional-valued open default is type-owned — no stamped obligation (the carve-out)");
    }
}
