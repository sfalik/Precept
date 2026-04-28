# Proof Engine and Interval Arithmetic Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do real compilers and static analyzers perform interval/range analysis, prove safety properties, and report proof obligations with attribution?

---

## SPARK Ada / GNATprove

Source: https://docs.adacore.com/live/wave/spark2014/html/spark2014_ug/en/source/how_to_view_gnatprove_output.html  
Source: https://docs.adacore.com/live/wave/spark2014/html/spark2014_ug/ (SPARK User's Guide, AdaCore, 2026)

### What It Proves

GNATprove proves **absence of runtime errors (AoRTE)** for programs written in SPARK (a formally defined subset of Ada). Categories of checks:

- **Data Dependencies** — correctness of data dependency contracts
- **Flow Dependencies** — correctness of flow dependency contracts
- **Initialization** — absence of reads of uninitialized variables
- **Non-Aliasing** — absence of interferences between parameters
- **Run-time Checks** — absence of runtime errors, including:
  - Divide by zero (divisor is nonzero)
  - Range checks (value is within subtype bounds)
  - Integer overflow
  - Array index out of bounds
  - Null dereference
- **Assertions** — user-specified pragma Assert statements
- **Functional Contracts** — subprogram pre/postconditions, package contracts, type invariants
- **LSP Verification** — Liskov Substitution Principle for OOP
- **Termination** — loop variant and subprogram termination
- **Concurrency** — Ravenscar profile concurrency properties

### Interval / Domain Representation

GNATprove uses a dedicated **Interval analysis pass** separate from its SMT-solver-based proof pass. The Interval column in the summary table counts "checks (overflow and range checks) proved by a simple static analysis of bounds for floating-point expressions based on type bounds of sub-expressions." This is a lightweight interval propagation over type-declared bounds, applied before invoking external SMT provers.

Ada's type system carries explicit range constraints (e.g., `type Score is range 0 .. 100`), which GNATprove uses as initial interval bounds. The Interval pass propagates these bounds through subexpressions and checks whether the result range is compatible with the required range without invoking SMT.

### Fact Propagation Through Sequential Statements

For sequential code, GNATprove generates **verification conditions (VCs)** per statement. Each assignment `x := expr` generates a VC that the computed value of `expr` satisfies the subtype constraints of `x`. VCs flow forward: the postcondition of statement N is the precondition of statement N+1. The Interval pass performs this propagation using type bounds; the SMT pass translates the same flow into logical formulas sent to CVC5 or Z3.

The `Trivial` prover (internal simplification) discharges checks that are trivially true without any domain propagation — e.g., a range check on a literal that is syntactically within bounds.

### Loop Handling

Loops are not handled by the Interval pass. For loops, GNATprove requires user-supplied **loop invariants** (via `pragma Loop_Invariant`). Without a loop invariant, GNATprove treats all variables modified by the loop as having their full declared type range after the loop — effectively losing all interval precision across the loop boundary. The Termination check requires a `pragma Loop_Variant` that names a quantity that strictly decreases each iteration.

### Proof Obligation Classification

GNATprove classifies each check into exactly one outcome:

| Outcome | Meaning |
|---------|---------|
| **Flow** | Proved by flow analysis (data flow / initialization checks) |
| **Interval** | Proved by the lightweight bound-propagation interval pass |
| **Provers** | Proved by an SMT prover (CVC5, Z3, Trivial internal simplifier, or user-specified) |
| **Justified** | Not proved; user suppressed it with `pragma Annotate` (direct justification) |
| **Unproved** | Not proved and not justified |

The summary table in `gnatprove.out` aggregates totals per category. Example:

```
SPARK Analysis results    Total  Flow  Interval   Provers             Justified  Unproved
------------------------------------------------------------------------------------------
Run-time Checks             474     .         .   458 (CVC5 95%, Trivial 5%)  16       .
Assertions                   45     .         .     45 (CVC5 82%, Trivial 18%)   .       .
```

### Attribution and Source Location

Every check message is attributed to an exact **file:line:column** location:

```
file.adb:12:37: medium: divide by zero might fail
```

The division sign `/` is at line 12, column 37, making clear which operand is the divisor. Severity levels are `low`, `medium`, or `high` based on how likely the issue is to reflect an actual runtime error.

With `--cwe`, a CWE identifier is appended:
```
file.adb:12:37: medium: divide by zero might fail [CWE 369]
```

For unproved checks, GNATprove appends a reason in brackets:
- `[provers reached time and step limit before completing the proof]`
- `[provers gave up before completing the proof]`

### Output Format (Structured vs Prose)

Three output formats co-exist:

1. **Prose text** on stdout: one line per message, `file:line:col: severity: text`. Human-readable.
2. **`gnatprove.out`** summary file: tabular statistics with totals by check category and prover. Semi-structured text.
3. **`gnatprove.sarif`**: machine-readable SARIF (Static Analysis Results Interchange Format) in the `gnatprove/` directory. File paths in SARIF use `%SRCROOT%` base URIs. SARIF viewers can reconstruct full paths from `originalUriBaseIds`.

### Notes

- Counterexamples generated by CVC5 include a **path** through the subprogram and an assignment of concrete values to variables along that path. GNATprove internally validates counterexamples against the code before displaying them; invalid counterexamples from CVC5 are suppressed or replaced by fuzz-generated ones.
- The `--steps` switch controls the maximum reasoning steps consumed; the `gnatprove.out` file reports `max steps used for successful proof` so runs can be exactly reproduced.
- Modes: `check`, `check_all`, `flow`, `prove`, `all` — each produces a different subset of messages.

---

## Frama-C EVA (Evolved Value Analysis)

Source: https://frama-c.com/fc-plugins/eva.html  
Source: Frama-C EVA User Manual, https://frama-c.com/download/frama-c-eva-manual.pdf  
Source: Frama-C website, © 2007–2026 FRAMA-C

### What It Proves

EVA (Evolved Value Analysis) proves **absence of runtime errors** in C programs using abstract interpretation. Properties proved include:

- Signed and unsigned integer overflow
- Division by zero
- Out-of-bounds array accesses
- Null and uninitialized pointer dereferences
- Invalid memory accesses

EVA is **sound for the absence of alarms**: if no alarm is emitted for an operation, the operation is **guaranteed** not to cause a runtime error. Conversely, an alarm indicates a possible (not necessarily actual) runtime error.

EVA also computes **variation domains** — the set of possible values a variable can take at each program point — and exposes these in the Frama-C GUI and API.

### Interval / Domain Representation

EVA uses **abstract interpretation** with multiple abstract domains. The primary domain for numerical reasoning is an **interval + congruence domain**: each variable is associated with a set of values abstracted as one or more intervals and congruence classes. At the end of a function analysis, EVA reports values in the form:

```
__retres ∈ [0..2147483647]
```

This indicates the return value of the function is within the range [0, 2147483647]. The intervals are mathematical closed intervals over the integer or floating-point lattice, widened as needed to ensure termination over loops.

For floating-point, EVA accounts for all rounding errors and the effects of `-∞`, `+∞`, and `NaN`.

### Fact Propagation Through Sequential Statements

EVA computes an **abstract state** at each program point — a map from variables to abstract values (intervals, congruences). For sequential code:

- At an **assignment** `x = expr`, EVA evaluates the abstract value of `expr` in the current state, then updates the abstract state by binding `x` to that abstract value.
- At a **branch** (`if`/`else`), EVA splits the abstract state along the branch condition, propagates separately down each branch, then joins (takes the join in the interval lattice) at the merge point.
- Abstract values are over-approximations: if the analysis cannot determine the precise interval, it widens to a larger interval or to `[-∞, +∞]`.

The alarm is emitted when the abstract value of a subexpression includes a value that would cause a runtime error (e.g., the abstract value of the divisor includes 0).

Example: For `int abs(int x) { if (x < 0) return -x; else return x; }`:
- Eva reports a potential overflow alarm at `return -x` because when `x == INT_MIN`, `-x` overflows.
- The emitted alarm is the ACSL assertion `assert -x ≤ 2147483647;`.
- At function exit, `__retres ∈ [0..2147483647]`.

### Loop Handling

EVA uses **widening** to handle loops: after a bounded number of iterations, the interval is widened (enlarged) to guarantee convergence. The widening strategy is configurable. After widening converges, EVA computes a fixpoint (the loop invariant is implicit in the fixpoint computation, not user-supplied). The cost of widening is that abstract values after loops may be overly imprecise, leading to false alarms.

Note from documentation: "Recursive calls are currently not supported. Only sequential code can be analyzed at this time." (This refers to the `--eva-use-recursive-call` limitation in older versions; the manual should be consulted for current status.)

### Proof Obligation Classification

EVA does not classify by "proved / unproved" explicitly. Instead:

- **No alarm** = the property is proved to hold on all executions.
- **Alarm emitted** = the property might not hold; the alarm is a potential runtime error.

There is no explicit "unproved / timeout / counterexample" bucket. The alarm is a conservative overapproximation: all real errors produce an alarm, but not all alarms correspond to real errors (false positives possible).

### Attribution and Source Location

Each alarm is attributed to the exact **file:line** where the potentially erroneous operation occurs. The alarm text is the ACSL assertion that would need to hold to avoid the error. Example:

```
mytests/test.c:2:[eva] warning: signed overflow. assert -x ≤ 2147483647;
```

The category `[eva]` identifies the originating plugin. The word `warning` is the alarm level. The assertion expresses the property that was not provable.

### Output Format (Structured vs Prose)

- **Primary output**: text on stdout, one alarm per line, with `file:line:[eva] category: text` format.
- **Values output**: at the end of each analyzed function, `[eva:final-states] Values at end of function <name>:` followed by `variable ∈ [lo..hi]` lines.
- **GUI**: Frama-C's graphical interface (Ivette) shows the inferred value sets for each variable at each program point, coloring alarms visually in the source.

The analysis results are used as input by other Frama-C plugins (e.g., WP for deductive verification, PathCrawler for test generation).

### Notes

- EVA is described as having "industrialized" maturity.
- The analysis is **compositional by function** by default — each function is analyzed once, with the caller providing initial state.
- The command-line invocation: `frama-c -eva file1.c file2.c`, which runs EVA and prints alarms to stdout.

---

## Astrée Static Analyzer

Source: https://www.absint.com/astree/index.htm  
Source: AbsInt GmbH product documentation, © AbsInt GmbH, developed under license from CNRS/ENS  
Source: AbsInt release notes, versions up to 25.10

### What It Proves

Astrée is a **sound** static analyzer for safety-critical C and C++ software. "Sound" means: if Astrée reports no error, the absence of that class of error has been formally proven. Astrée detects:

- Division by zero
- Out-of-bounds array indexing
- Erroneous pointer manipulation (NULL, uninitialized, dangling pointers)
- Integer and floating-point arithmetic overflow
- Read access to uninitialized variables
- Data races (concurrent read/write without mutex)
- Inconsistent locking (lock/unlock problems)
- Invalid calls to OS services (e.g., OSEK)
- Spectre vulnerabilities
- Violations of user-defined assertions
- Dead code

Astrée is particularly used in aviation (Airbus A380 flight software since 2003, ATR aircraft), automotive (Bosch, worldwide license), nuclear (Framatome TELEPERM XS), and space (ESA Jules Verne ATV docking software).

In 2020, the US NIST determined Astrée is one of only two tools satisfying their criteria for sound static code analysis.

### Interval / Domain Representation

Astrée uses **abstract interpretation** with a combination of abstract domains. The core numerical domain is based on **intervals** and **congruences**, as with Frama-C EVA (Astrée was developed from the same research lineage at ENS Paris). However, Astrée additionally applies:

- **Relational domains** (octagon, polyhedra) for capturing relationships between variables in specific contexts.
- **Domain composition** — the domains are composed (reduced product) to improve precision while maintaining soundness.

For floating-point arithmetic, Astrée models all rounding errors, accumulation effects, and the behavior of `−∞`, `+∞`, and `NaN` values through arithmetic and comparisons. The tool claims to be "sound for floating-point computations."

### Fact Propagation Through Sequential Statements

Like all abstract interpretation tools, Astrée propagates an **abstract state** forward through the program:

- At each assignment, the abstract value of the right-hand side is evaluated in the current abstract state, and the variable is updated.
- At each branch, the state is split, propagated along each path, and joined at merge points.
- All data pointers and function pointers are resolved automatically; the soundness requirement means all potential targets must be considered.

Astrée tracks accesses to global variables, static variables, and local variables whose addresses escape their frame (e.g., passed into called functions).

### Loop Handling

Astrée uses **widening operators** with **delay** strategies to compute loop fixpoints. The user can "fine-tune precision for individual loops or data structures" by supplying external knowledge (e.g., loop bounds). With tuning, Astrée can produce exactly zero false alarms on structured sequential embedded code.

### Proof Obligation Classification

Astrée's classification is implicit in the alarm system:

- **No alarm** = property proved absent for all executions.
- **Alarm** = potential violation; the tool cannot rule out the error.
- **Dead code** = unreachable code is reported as a separate finding (not mixed with alarms).
- **User assertion violation** = alarm when a user-placed assertion might fail.

There is no explicit timeout / counterexample bucket; Astrée is a sound analyzer (no false negatives by design).

### Attribution and Source Location

Each alarm message is attributed to the **exact source location** (file, line, column). The GUI displays alarm locations inline in the source code. The detailed messages "guide you to the exact cause of each potential runtime error."

Delta analysis between code revisions produces a diff of alarms: new alarms, resolved alarms, and unchanged alarms.

### Output Format (Structured vs Prose)

- **Command-line mode**: textual alarms on stdout, one per line with source location.
- **GUI**: graphical display with source code view, control-flow graph, data-flow graph, signal-flow graph, and alarm overlay.
- **Report files**: automatically generated for documentation and certification purposes.
- **LSP integration**: Astrée supports the Language Server Protocol, enabling integration with VS Code, Eclipse, and any LSP-capable editor.
- **Qualification support kits**: for DO-178B/C, ISO 26262, EN-50128, and others. The QSK provides machine-checkable evidence of tool qualification.

### Notes

- Astrée is proprietary, available from AbsInt under node-locked, floating, or cloud licenses.
- The `RuleChecker` tool is separately integrated for coding guideline checks (MISRA, CERT, CWE, AUTOSAR, JSF).
- Delta analysis supports reviewing the impact of code changes without re-analyzing the entire codebase.
- Astrée's certification use in Airbus flight software is described in multiple published papers (Blanchet et al., 2002 PLDI).

---

## Liquid Haskell

Source: https://ucsd-progsys.github.io/liquidhaskell/specifications/  
Source: https://ucsd-progsys.github.io/liquidhaskell/ (LiquidHaskell documentation, University of California San Diego)  
Source: Jhala, R. and Vazou, N., "Refinement Types: A Tutorial," ESOP 2021 (https://arxiv.org/abs/2010.07763)

### What It Proves

Liquid Haskell is a **refinement type checker** for Haskell. It extends Haskell's type system with **refinement predicates** — logical formulas that constrain the values that a type can contain. Properties it can prove include:

- **Range safety**: a value is within a specified numeric range (e.g., `{v:Int | 0 <= v && v < 100}`).
- **Non-negativity**: e.g., `{v:Int | v >= 0}` (aliased as `Nat`).
- **Sorted list invariants**: e.g., every element is less than the next.
- **Division safety**: a divisor is nonzero (by typing the divisor as `{v:Int | v /= 0}`).
- **Termination**: every recursive function is accompanied by a decreasing metric, and Liquid Haskell proves the metric strictly decreases.
- **User-defined invariants**: arbitrary SMT-expressible predicates.

### Interval / Domain Representation

Liquid Haskell does not use interval abstraction in the abstract interpretation sense. Instead, it uses **refinement predicates** over the theory of linear arithmetic (and other SMT theories). A refinement type `{v:Int | lo <= v && v <= hi}` expresses an exact range as a logical formula. The SMT solver (Z3) decides satisfiability of these formulas exactly.

Refinement predicates use the formal grammar:

- **Constants**: integers, booleans
- **Expressions**: variables, constants, `(e + e)`, `(e - e)`, `(c * e)`, uninterpreted function application `(v e1 ... en)`, `(if p then e else e)`
- **Relations**: `==`, `/=`, `>=`, `<=`, `>`, `<`
- **Predicates**: binary relation, application, `&&`, `||`, `=>`, `not`, `true`, `false`

Type aliases like `Nat = {v:Int | v >= 0}` and `Pos = {v:Int | v > 0}` provide reusable interval-like constraints.

### Fact Propagation Through Sequential Statements

In Haskell, sequencing is via **`let` bindings** and **function application**. Liquid Haskell propagates refinements as follows:

- For `let x = expr in body`: the inferred refinement type of `expr` is bound to `x` in the environment for type-checking `body`.
- For a function call `f arg`: the precondition (input refinement) of `f` is checked against the actual type of `arg`; the postcondition (output refinement) of `f` is propagated as the type of the call expression.
- For branches (`if cond then e1 else e2`): the condition is added to the typing environment in each branch (path-sensitive refinement splitting), then the results are joined at the merge point.

This is called the **Hindley-Milner + refinement** approach: standard type inference provides base types; the refinement solver adds the predicates.

### Loop Handling

Haskell does not have loops; recursion is the iteration mechanism. Liquid Haskell handles recursion via **termination metrics**:

- A `decreases` annotation `/ [expr]` gives a metric that must strictly decrease at each recursive call. Dafny uses the same concept under the same keyword.
- For ADTs, the default metric is the structural size (e.g., list length). The `autosize` annotation makes Liquid Haskell derive this automatically.
- For mutually recursive functions, lexicographic metrics `/ [m, n]` are used.
- If termination cannot be proved, the function can be marked `{-@ lazy foo @-}` to assume termination.

For functions without loops or recursion, Liquid Haskell's verification is purely constraint-based — no fixpoint computation needed.

### Proof Obligation Classification

Liquid Haskell does not use Flow/Interval/Prover columns. Its classification is:

- **SAFE**: all refinement type checks passed; the SMT solver proved all generated constraints.
- **UNSAFE**: at least one constraint failed; Liquid Haskell reports a **Liquid Type Mismatch** error.
- **`{-@ fail foo @-}`**: a declaration explicitly marking a definition as expected to be unsafe; if `foo` passes, Liquid Haskell reports an error.

There is no timeout category in the output; the SMT query either succeeds within the allotted resources or the tool reports a failure.

### Attribution and Source Location

Each type error is attributed to a **source file and line**. The error message shows:

1. The inferred type at the error site.
2. The required (expected) type.

Example (from termination checking documentation):

```
Liquid Type Mismatch

The inferred type
  VV : {v : Foo a | fooLen v == myLen xs && v == Foo xs}

is not a subtype of the required type
  VV : {VV : Foo a | fooLen VV < fooLen ?a && fooLen VV >= 0}
```

This shows that the required property (`fooLen VV < fooLen ?a`) — the strict decrease of the termination metric — is not satisfied by the inferred type.

### Output Format (Structured vs Prose)

- Liquid Haskell runs as a **GHC plugin** (`{-# OPTIONS_GHC -fplugin=LiquidHaskell #-}`).
- Errors appear as **GHC compiler errors**, integrated with any Haskell IDE or CI tool.
- Each error is a `Liquid Type Mismatch` with the inferred vs. required type shown in a structured block.
- The `Try Online` tool at `liquidhaskell.goto.ucsd.edu` provides an interactive checker.

### Notes

- Liquid Haskell dispatches proof obligations to Z3 via a **Fixpoint constraint** system (Horn clause solving).
- The **PLE (Proof by Logical Evaluation)** extension allows Liquid Haskell to unfold function definitions automatically, reducing the need for user-supplied lemmas.
- Relational specifications (`{-@ relational f ~ g :: ... @-}`) can compare two functions against each other, e.g., to prove monotonicity.
- Liquid Haskell's approach is different from GNATprove: it works via subtyping constraints in the type system, not by generating VCs from an imperative semantics.

---

## Dafny

Source: https://dafny.org/dafny/OnlineTutorial/guide  
Source: Dafny Reference Manual, https://dafny.org/latest/DafnyRef/DafnyRef  
Source: Leino, K.R.M., "Dafny: An Automatic Program Verifier for Functional Correctness," LPAR 2010

### What It Proves

Dafny is a **verification-aware programming language** that proves functional correctness and safety properties. Properties it can prove include:

- **Precondition and postcondition satisfaction** (`requires` / `ensures`): every method call satisfies the callee's precondition; every method exit satisfies its postcondition.
- **Absence of runtime errors**: array index out of bounds, null dereference, division by zero (all generated automatically as implicit assertions).
- **User-specified assertions** (`assert`): arbitrary boolean properties at any point in the code.
- **Termination**: all loops and recursive calls terminate (via `decreases` metrics).
- **Loop invariants**: properties that hold on every iteration of a loop.
- **Framing**: that a method only modifies the memory it declares in `modifies` clauses.

### Interval / Domain Representation

Dafny does not use interval abstraction. It translates the program into **logical formulas** (verification conditions) that are dispatched to an SMT solver (Z3). Ranges over integers are expressed as formulas like `0 <= v && v < a.Length`. No interval widening is performed; the SMT solver reasons exactly over the formula.

The key design point: **Dafny treats each method in isolation**. When analyzing a method, Dafny "forgets" the bodies of all called methods and only uses their pre/postconditions. This means the accuracy of verification depends entirely on the quality of the annotations.

### Fact Propagation Through Sequential Statements

For sequential code, Dafny generates verification conditions using **weakest precondition** (WP) calculus:

- For an assignment `x := expr`, the WP of the subsequent postcondition `P` with respect to the assignment is `P[expr/x]` (substitute `expr` for `x` in `P`).
- For a sequential composition `S1; S2`, the WP of `S2; postcondition` becomes the postcondition of `S1`.
- For an `if` branch: each branch is verified separately, with the branch condition added to the logical context.
- For a method call `y := f(args)`: the callee's precondition is checked against the current state; after the call, the postcondition is the new fact available.

This is implemented by translating Dafny code to **Boogie** (an intermediate verification language), which then invokes Z3.

### Loop Handling

Loops require **user-supplied loop invariants** (`invariant` keyword). Dafny proves:

1. **Establishment**: the invariant holds when the loop is first entered.
2. **Preservation**: assuming the invariant holds at the start of an iteration, it still holds after the iteration body executes.
3. **Useful on exit**: the invariant, combined with the negation of the loop guard, implies the desired postcondition.

Without a loop invariant, Dafny cannot verify properties that depend on what the loop computes. In the Fibonacci method example, the invariants `b == fib(i)` and `a == fib(i-1)` are required.

The `decreases` annotation provides the termination metric. Dafny can often infer `n - i` as the decreasing quantity for a typical counting loop.

### Proof Obligation Classification

Dafny's classification per check:

| Outcome | Meaning |
|---------|---------|
| **Verified** | All VCs passed (Z3 returned `unsat` to the negation of each VC) |
| **Error: postcondition might not hold** | Z3 could not prove the postcondition VC |
| **Error: assertion might not hold** | Z3 could not prove an `assert` statement |
| **Error: loop invariant might not hold** | Establishment or preservation failed |
| **Error: decreases clause might not decrease** | Termination metric not decreasing |
| **Error: precondition might not hold** | Caller does not satisfy callee's `requires` |

Dafny does not report a "timeout" bucket explicitly — if Z3 times out, the check is reported as an error.

### Attribution and Source Location

Each error is attributed to the **specific source line** of the failing annotation. For a postcondition failure at line 13: `file.dfy:13:14: Error: postcondition might not hold`. The error includes the location of the failing `ensures` clause, not just the call site.

Counterexamples are supported: Dafny (via Boogie/Z3) can generate concrete witness values that satisfy the negation of the VC. These are displayed as specific variable assignments, e.g., `e.g. when X'Old = 0` (shown in the GNATprove documentation for a similar concept; Dafny's format varies by IDE integration).

### Output Format (Structured vs Prose)

- **Dafny CLI**: text output, one error per line, in the form `file:line:col: Error: description`.
- **IDE integration** (VS Code Dafny extension): inline error squiggles and hover messages.
- **Boogie output** (internal): Dafny can expose the Boogie translation for inspection.
- For verified programs: `Dafny program verifier finished with N verified, 0 errors`.

### Notes

- Dafny compiles verified programs to C#, Java, JavaScript, Go, or Python; the verification is a compile-time step, not a runtime check.
- Dafny's approach to isolation (forgetting method bodies) is a key usability feature: it enables modular verification but requires accurate postconditions on all called methods.
- For loop-free code, Dafny verification is particularly efficient: no fixpoint computation, no invariant annotation needed; the Z3 query is a finite formula over the sequential assignments.
- The `function` keyword in Dafny creates a **pure mathematical function** whose body is visible to the verifier (unlike methods). Functions can appear in specifications, and their definitions are unfolded during verification.

---

## CBMC (C Bounded Model Checker)

Source: https://www.cprover.org/cbmc/  
Source: CPROVER Manual, http://www.cprover.org/cprover-manual/  
Source: Clarke, E., Kroening, D., Lerda, F., "A Tool for Checking ANSI-C Programs," TACAS 2004

### What It Proves

CBMC is a **bounded model checker** for C and C++ programs. It verifies:

- **Memory safety**: array bounds checks, safe use of pointers (null dereference, use after free)
- **Undefined behavior**: signed overflow, invalid pointer arithmetic, uninitialized variable reads
- **User-specified assertions**: `assert(condition)` in C code
- **I/O equivalence**: checking that a C implementation is equivalent to a specification in another language (e.g., Verilog)

CBMC also supports Java bytecode via JBMC and Rust via Kani.

### Interval / Domain Representation

CBMC is not an abstract interpretation tool and does not use interval abstraction. Instead, it uses **bounded model checking (BMC)**:

1. **Loop unrolling**: all loops in the program are unrolled to a user-specified bound (e.g., `--unwind 10`).
2. **SSA (Static Single Assignment) transformation**: the program is converted to SSA form, creating a fresh variable for each assignment.
3. **Bit-vector encoding**: the SSA program is encoded as a **bit-precise formula** over bitvector arithmetic, encoding all operations exactly as they would execute on hardware (including integer widths, overflow behavior, pointer sizes).
4. **SAT/SMT solving**: the conjunction of the program semantics and the negation of the property to prove is passed to a SAT solver (built-in MiniSat) or an SMT solver (CVC5, Z3, Boolector).

If the formula is **unsatisfiable**, the property holds for all executions within the unwind bound. If **satisfiable**, the satisfying assignment is a counterexample trace.

### Fact Propagation Through Sequential Statements

SSA transformation handles sequential statements directly. Each assignment `x = expr` creates a new version `x_k` of the variable, and all subsequent uses of `x` refer to `x_k`. The encoding is exact — no abstraction or approximation. The program semantics are faithfully encoded in the formula.

For branching: both branches are encoded, with a conditional guard (`if cond then x = a else x = b` becomes `x_k = (cond ? a : b)`). All paths through the program are encoded simultaneously in the formula; the SAT solver searches for a satisfying assignment representing a concrete execution path that violates the property.

### Loop Handling

CBMC handles loops via **bounded unrolling**:

- The loop body is replicated `k` times in the formula, where `k` is the unwind bound.
- An **unwind assertion** is added at the unwind boundary: if the loop continues past the bound, a counterexample is generated (the property "the loop terminates within k iterations" is checked).
- For loop-free code, no unrolling is needed; CBMC produces the exact encoding of the program as a single formula. This is where CBMC is most precise and most efficient.

The documentation notes: "The verification is performed by unwinding the loops in the program and passing the resulting equation to a decision procedure."

### Proof Obligation Classification

CBMC classifies results as:

| Outcome | Meaning |
|---------|---------|
| **VERIFICATION SUCCESSFUL** | All checks pass for all paths within the unwind bound |
| **VERIFICATION FAILED** | At least one check fails; a counterexample trace is produced |

There is no intermediate "unproved" or "timeout" outcome distinct from failure — a timeout is a failure to verify within available resources.

Starting from CBMC version 6, undefined behavior checks are on by default (`--no-standard-checks` to disable).

### Attribution and Source Location

When CBMC finds a counterexample, it produces a **violation trace**: a step-by-step execution trace showing:

- The sequence of statements executed
- The value of each variable at each step
- The precise source location (file:line) of each step
- The failing assertion and its source location

This trace is sufficient to understand exactly which input values triggered the violation and how the program arrived at the failing state.

### Output Format (Structured vs Prose)

- **Primary output**: text on stdout. The trace format is prose with structure: each step is labeled with source location, statement, and variable values.
- **Machine-readable**: CBMC can output JSON or XML for tool integration.
- **No GUI**: CBMC is a command-line tool only (no graphical interface).

### Notes

- CBMC is most precise on **loop-free, bounded programs** — the encoding is exact and the SAT/SMT solver returns a definitive answer.
- For real-world programs with loops, the accuracy depends on the unwind bound; a bound too small may miss bugs, a bound too large may be intractably slow.
- CBMC version 6 defaults: `malloc` may return `NULL`, calls to functions without body trigger verification errors, default verbosity increased.
- The primary CBMC solver for bit-vector formulas is built-in MiniSat. External SMT solvers (CVC5, Z3, Boolector) are alternative backends.

---

## Infer (Meta/Facebook)

Source: https://fbinfer.com/docs/about-Infer  
Source: https://fbinfer.com/docs/infer-workflow  
Source: https://fbinfer.com/docs/checker-bufferoverrun/  
Source: https://fbinfer.com/docs/checker-biabduction/  
Source: https://fbinfer.com/docs/separation-logic-and-bi-abduction  
Source: Infer documentation, © 2026 Facebook, Inc.

### What It Proves

Infer is a **static program analyzer** for Java, C, C++, Objective-C, and Erlang, written in OCaml. It is deployed at Meta in the continuous integration pipeline for the main app family (Facebook, Instagram, Messenger, WhatsApp).

Infer detects:

- **Null pointer dereferences** (Biabduction, Pulse)
- **Memory leaks and resource leaks** (Biabduction, Pulse)
- **Buffer overruns and out-of-bounds array accesses** (InferBO)
- **Integer overflow** (InferBO)
- **Data races** (RacerD)
- **Use-after-free, retain cycles** (Pulse, Biabduction — deprecated)
- **Divide by zero** (Biabduction — deprecated; replaced by Pulse)

For interval arithmetic specifically, the **InferBO (Buffer Overrun Analysis)** checker performs the relevant analysis.

### Interval / Domain Representation

InferBO uses **interval abstract interpretation** to reason about buffer accesses and integer values. The domain tracks:

- For each integer variable: an interval `[lo, hi]`
- For each array: the interval of valid indices
- For allocations: the size (as an interval)

Issue types classified by confidence level (L = "definitely wrong", higher L = more uncertain):

| Issue Type | Meaning |
|-----------|---------|
| `BUFFER_OVERRUN_L1` | Definite buffer overrun |
| `BUFFER_OVERRUN_L2–L5` | Decreasing confidence levels |
| `BUFFER_OVERRUN_S2` | Syntactic buffer overrun (less semantic reasoning) |
| `BUFFER_OVERRUN_U5` | Uncertain, may be a false positive |
| `INTEGER_OVERFLOW_L1` | Definite integer overflow |
| `INTEGER_OVERFLOW_L2, L5, U5` | Decreasing confidence |
| `INFERBO_ALLOC_IS_BIG` | Allocation size is definitely too large |
| `INFERBO_ALLOC_IS_NEGATIVE` | Allocation size is definitely negative |
| `INFERBO_ALLOC_IS_ZERO` | Allocation size is definitely zero |
| `INFERBO_ALLOC_MAY_BE_BIG` | Allocation size might be too large |
| `INFERBO_ALLOC_MAY_BE_NEGATIVE` | Allocation size might be negative |

### Fact Propagation Through Sequential Statements

Infer analyzes **each function and method separately** (per-function modular analysis):

1. **Capture phase**: source code is compiled as normal; Infer intercepts the build, translates source files to an internal intermediate representation (`infer-out/` directory).
2. **Analysis phase**: each function/method is analyzed in isolation. The analysis for each function starts with an abstract initial state and propagates abstract values (intervals for InferBO) through the function body statement by statement.

For sequential statements:
- Each assignment updates the abstract state: if the right-hand side has an abstract value `[lo, hi]`, the left-hand side variable is updated to `[lo, hi]`.
- Branch conditions narrow the abstract interval (similar to abstract interpretation for other tools).
- Function calls use the callee's abstract summary (pre/post conditions computed for the callee in an earlier pass).

Infer is **inter-procedural**: it computes abstract summaries for callees and uses them when analyzing callers. This is more powerful than purely local analysis.

### Loop Handling

InferBO uses **widening** over intervals to compute fixpoints for loops. Like other abstract interpretation tools, the widening operator enlarges the interval when convergence is not achieved in a bounded number of iterations. After widening, Infer computes a sound over-approximation of the values that variables can take across all loop iterations.

### Proof Obligation Classification

Infer classifies bugs not as "proved/unproved" but by:

1. **Issue type** (e.g., `NULL_DEREFERENCE`, `BUFFER_OVERRUN_L1`)
2. **Confidence level** (L1 = high confidence / definite bug; L5 or U5 = low confidence / possible false positive)
3. **Filtered vs. reported**: Infer applies internal filtering to show only the most likely real bugs; the full report includes all issues.

When a function analysis encounters an error, Infer **stops analyzing that function at that point** but continues analyzing other functions. The final report lists all bugs found across all functions.

### Attribution and Source Location

Each bug report includes:

- **File and line number** of the bug
- **Bug type** (e.g., `NULL_DEREFERENCE`, `BUFFER_OVERRUN_L1`)
- **Procedure name** where the bug occurs

The `infer explore` command provides an **interactive bug trace** showing the step-by-step execution path leading to the bug, including:

- Each step's source file and line
- The statement executed at each step
- The relevant variable values and conditions at each step

This step-by-step attribution is the primary mechanism for understanding deeply nested inter-procedural bugs.

### Output Format (Structured vs Prose)

- **Primary output**: text on stdout, listing bug type, file, line, and procedure.
- **`infer-out/report.txt`**: the same information in a file, one bug per line.
- **`infer explore`**: interactive terminal-based trace viewer. Outputs a formatted prose trace with each step numbered and annotated with source location.
- The results directory (`infer-out/`) also contains JSON files for tool integration.
- Infer supports **reactive / differential workflow**: with `--reactive`, only changed files and their dependents are re-analyzed, enabling fast CI integration.

### Notes

- Biabduction (the original Infer analysis based on separation logic and bi-abduction) has been **deprecated** and replaced by Pulse for memory safety analysis. InferBO (interval-based) remains the primary checker for buffer overrun and integer overflow.
- InferBO was developed specifically for array bounds analysis and is described in "InferBO: Infer-based Buffer Overrun Analyzer" (available via the Infer blog).
- Infer is used at Meta's CI scale: analyzing every code modification across a large polyglot codebase. This motivates the per-function modular design and the differential workflow.
- The OCaml implementation of Infer uses an **abstract domain framework** (`absint-framework` in the Infer contributor documentation) that allows plugging in new checkers with new abstract domains.
```

---
