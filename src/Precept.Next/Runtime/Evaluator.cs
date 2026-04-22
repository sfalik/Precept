namespace Precept.Runtime;

/// <summary>
/// Runtime expression evaluator. Walks the executable model's expression trees
/// against entity data. Pure: same inputs, same result, no side effects.
/// </summary>
/// <remarks>
/// Static by design — aligned with the pipeline pattern (Lexer.Lex, Parser.Parse,
/// TypeChecker.Check). The evaluator holds no per-invocation state; the working copy
/// is allocated inside the evaluation function and never escapes the operation boundary.
///
/// Survey grounding (R1): CEL's Interpretable.Eval(Activation), XState's
/// transition(machine, state, event), and Dhall's eval(env, expr) all converge on
/// stateless/functional evaluation for pure-expression languages.
///
/// Every failure path must go through <see cref="Fail"/> with a classified
/// <see cref="FaultCode"/>. This is enforced by PREC0001. Every FaultCode
/// must carry <see cref="StaticallyPreventableAttribute"/> linking it to the
/// <see cref="Pipeline.DiagnosticCode"/> the compiler should have emitted —
/// enforced by PREC0002. Together these guarantee fault–diagnostic correspondence:
/// if the compiler emits no errors, the evaluator should never fault.
/// </remarks>
public static class Evaluator
{
    // TODO: Fire / Edit / Inspect signatures pending R2 (result type taxonomy)
    // and R4 (executable model contract).

    internal static Fault Fail(FaultCode code, params object?[] args)
        => Faults.Create(code, args);
}
