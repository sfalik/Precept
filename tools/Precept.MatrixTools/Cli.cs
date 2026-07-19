using System.Linq;
using Precept.Pipeline;

namespace Precept.MatrixTools;

/// <summary>
/// CLI: compile a .precept file and print, for every obligation (explicit rules
/// plus modifier-desugared rules), the establishment WP over the default
/// configuration and the preservation WP through each row's write plan — with
/// guard-match verdicts where the row carries a guard.
/// </summary>
public static class Cli
{
    public static int Run(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: precept-matrixtools <file.precept>");
            return 2;
        }

        var compilation = Precept.Compiler.Compile(File.ReadAllText(args[0]));
        foreach (var diagnostic in compilation.Diagnostics)
            Console.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");

        var semantics = compilation.Semantics;
        var obligations = ObligationEnumerator.Enumerate(semantics);
        if (obligations.IsEmpty)
        {
            Console.WriteLine("no obligations (no rules and no desugaring modifiers).");
            return compilation.HasErrors ? 1 : 0;
        }

        Console.WriteLine();
        Console.WriteLine("── Establishment over the default configuration ──");
        foreach (var obligation in obligations)
            Describe(WpCalculator.ComputeEstablishmentWp(semantics, [], obligation), obligation, guard: null);

        foreach (var row in semantics.EventHandlers.OfType<TypedEventRowSuccess>().Where(r => r.IsConstruction))
        {
            Console.WriteLine();
            Console.WriteLine($"── Establishment through construction row: on {row.EventName} ──");
            foreach (var obligation in obligations)
                Describe(WpCalculator.ComputeEstablishmentWp(semantics, row.Actions, obligation), obligation, row.Guard);
        }

        foreach (var row in semantics.TransitionRows.OfType<TypedTransitionRowSuccess>())
        {
            Console.WriteLine();
            Console.WriteLine($"── Preservation: from {row.FromState ?? "*"} on {row.EventName}"
                + (row.Guard is null ? "" : $" when {WpCalculator.Canonicalize(row.Guard)}") + " ──");
            PrintPreservation(row.Actions, row.Guard, obligations);
        }

        foreach (var row in semantics.EventHandlers.OfType<TypedEventRowSuccess>().Where(r => !r.IsConstruction))
        {
            Console.WriteLine();
            Console.WriteLine($"── Preservation: on {row.EventName}"
                + (row.Guard is null ? "" : $" when {WpCalculator.Canonicalize(row.Guard)}") + " ──");
            PrintPreservation(row.Actions, row.Guard, obligations);
        }

        return compilation.HasErrors ? 1 : 0;
    }

    private static void PrintPreservation(
        IReadOnlyList<TypedAction> plan,
        TypedExpression? guard,
        IReadOnlyList<ObligationSpec> obligations)
    {
        if (plan.Count == 0)
        {
            Console.WriteLine("  (no writes — every obligation frame-preserves)");
            return;
        }

        foreach (var obligation in obligations)
        {
            var result = WpCalculator.ComputePreservationWp(plan, obligation);

            // Frame detection: a WP identical to the un-substituted obligation means
            // the plan does not touch the rule.
            if (result is WpComputed computed)
            {
                var frame = WpCalculator.ComputePreservationWp([], obligation);
                if (frame is WpComputed f && f.Wp.Key == computed.Wp.Key)
                {
                    Console.WriteLine($"  {obligation.Label}: frame-preserved (write does not touch the rule)");
                    continue;
                }
            }

            Describe(result, obligation, guard);
        }
    }

    private static void Describe(WpResult result, ObligationSpec obligation, TypedExpression? guard)
    {
        switch (result)
        {
            case WpNotSupported notSupported:
                Console.WriteLine($"  {obligation.Label}: NOT SUPPORTED — {notSupported.Reason}");
                break;

            case WpComputed computed:
                var verdicts = "";
                if (guard is not null)
                {
                    bool exact = WpCalculator.GuardMatchesWp(guard, computed.Wp);
                    bool covered = WpCalculator.GuardFactsCoverWp(guard, computed.Wp);
                    verdicts = exact
                        ? "   [guard normal-form-equal to WP]"
                        : covered
                            ? "   [guard facts cover WP]"
                            : "   [guard does not cover WP]";

                    // Conditional-rule WPs are implications; report whether the guard
                    // covers the consequent (whether the activation side discharges —
                    // e.g. by vacuity — is discharge-contract content, not decided here).
                    if (!exact && !covered && computed.Wp is CanonImplies implies
                        && WpCalculator.GuardFactsCoverWp(guard, implies.Consequent))
                    {
                        verdicts = "   [guard facts cover WP consequent; activation side not decided here]";
                    }
                }
                Console.WriteLine($"  {obligation.Label}: WP = {computed.Wp}{verdicts}");
                break;
        }
    }
}
