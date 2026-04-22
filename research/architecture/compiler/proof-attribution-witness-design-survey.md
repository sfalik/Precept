# Proof Attribution and Structured Proof Witness Design Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do verification tools structure proof results, attribute obligations to source, and present witnesses as structured data for tooling consumption?

---

## SPARK Ada / GNATprove

Source: https://docs.adacore.com/live/wave/spark2014/html/spark2014_ug/en/source/how_to_view_gnatprove_output.html  
Source: https://docs.adacore.com/live/wave/spark2014/html/spark2014_ug/en/source/how_to_use_gnatprove_in_a_team.html

### Proof Output Structure / Schema

GNATprove produces two primary output artifacts:

1. `gnatprove.out` — a summary table written to the object directory. Columns: Total, Flow, Interval, Provers, Justified, Unproved. Rows represent files or units. Counts categorize how each check was resolved. The `--assumptions` switch adds an additional assumptions table listing unverified dependencies per subprogram.

2. `gnatprove.sarif` — a SARIF 2.1 format file. Fields include `run.originalUriBaseIds` for path mapping; `--sarif-base-uri=ID:PATH` configures base URI entries for repositories with multiple source roots.

Per-check messages are emitted in compiler-like format:

```
file.adb:line:col: severity: category: message [reason]
```

Check categories include: Data Dependencies, Flow Dependencies, Initialization, Non-Aliasing, Run-time Checks, Assertions, Functional Contracts, LSP Verification, Termination, Concurrency.

Session files: proof results stored in `why3session.xml` files, organized into subdirectories under `gnatprove/` corresponding to unit names and subprogram names. The shape files (`why3shapes`, `why3shapes.gz`) are kept separate and not recommended for version control.

### Source Attribution Model

Each check message is attributed to an exact source location: file, line number, and column number. The location corresponds to the property being checked (e.g., the contract annotation, the assertion expression, or the runtime check site).

SARIF output uses `uriBaseId`/`originalUriBaseIds` to map relative file paths to repository roots. The `--sarif-base-uri=ID:PATH` switch configures these base URI entries.

Session files are organized by unit name and subprogram name, creating a hierarchical directory structure under `gnatprove/` (or under `sessions/` if `Proof_Dir` is set in the project file). This structure maps proof results to program entities.

### Proved / Not Proved / Counterexample Distinction

Status values reported in `gnatprove.out` and per-check messages:

- **Proved** — resolved by one of: flow analysis, interval analysis, a named external prover (CVC5, Alt-Ergo, Z3). With `--report=provers`, the info message includes the specific prover that discharged each check.
- **Justified** — check message suppressed via `pragma Annotate(GNATprove, False_Positive|Intentional, Pattern, Reason)`. The `Reason` string appears in GNATprove report output. Categories `False_Positive` (check cannot fail but tool couldn't prove it) and `Intentional` (check can fail but is not a bug) have no behavioral difference; they serve documentation purposes.
- **Unproved** — no prover could discharge the check. May include a bracketed reason: `[provers reached time and step limit before completing the proof]` or `[provers gave up before completing the proof]`.
- **Timeout** — prover ran out of time.

Counterexamples: activated with `--counterexamples=on`. The CVC5 solver is used to generate a failing execution path with concrete variable assignments at each step. GNATprove validates the counterexample before displaying it (to filter spurious ones). In GNAT Studio, counterexamples are shown as virtual inserted lines in the source editor, annotating variable values at each step of the path.

`--proof-warnings=on` enables additional proof-based warnings about unreachable branches, dead code, and always-false assertions. Each such warning requires calling a prover.

### Structured Data vs. Prose

- `gnatprove.out` table: semi-structured text; machine-parseable with known column layout.
- `gnatprove.sarif`: fully structured JSON (SARIF 2.1 schema); designed for consumption by SAST platforms.
- Per-check messages: compiler-like text format; consumed by qualimetry tools (GNATdashboard, SonarQube, SQUORE) as documented in the "Possible Workflows" section.
- `--cwe` flag: adds CWE (Common Weakness Enumeration) IDs to check messages, enabling mapping to security vulnerability databases.
- `--report=provers` flag: adds prover names to info messages (e.g., "proved by cvc5").
- `why3session.xml`: XML-based; records prover decisions for each proof obligation. Consumed by Why3 and GNATprove `--replay`.

### Constraint / Assumption Linkage

`--assumptions` switch generates an assumptions table in `gnatprove.out`. The table lists, per analyzed subprogram, the remaining assumptions that must be verified by other means (testing, review, etc.). Assumption categories include: "effects on parameters and global variables", "absence of run-time errors", "the postcondition".

Assumptions arise when a subprogram is marked `SPARK_Mode => Off`, or when it has unproved checks, or when its body is not provided. Assumption propagation is across the call graph.

Justification mechanisms:
- `pragma Annotate(GNATprove, Category, Pattern, Reason)` — direct justification; Pattern is a glob matching the check message text; Reason is a free-form string recorded in the report.
- `pragma Assume(Condition, "Reason")` — indirect justification; introduces a verified assumption that enables downstream proofs.

Both justification forms are listed as `[SPARK_JUSTIFICATION]` assumptions in the complete assumption taxonomy.

### Tooling Consumption (IDE, CI, API)

- **GNAT Studio**: Analysis Report Panel (SPARK → Show Report); click-to-navigate to source location; counterexample overlay in source editor.
- **GNATbench**: Eclipse-based plugin with equivalent UI integration.
- **CI/qualimetry**: compiler-like text output consumed by GNATdashboard, SonarQube, SQUORE (documented workflow 3).
- **SARIF consumers**: `gnatprove.sarif` for GitHub Code Scanning and other SARIF-compatible platforms.
- **Session sharing**: `why3session.xml` committed to version control; `--replay` option re-checks proofs using stored decisions without re-running SMT solvers.
- **Proof caching**: `--memcached-server=file:<dir>` (file-based) or `--memcached-server=hostname:port` (Memcached server) for sharing proof results across runs and across team members.

### Human vs. Machine Readability

- Per-check text messages: human-readable; designed for console and CI log viewing.
- `gnatprove.out` table: semi-structured; both human and machine.
- `gnatprove.sarif`: machine-readable; structured JSON.
- `why3session.xml`: machine-readable; consumed by Why3 and GNATprove.
- Counterexample display in IDE: human-readable (annotated source lines).
- `--report=all` / `--report=provers` / `--report=statistics`: controls verbosity level for human review.

---

## Dafny

Source: https://github.com/dafny-lang/dafny/blob/master/Source/DafnyCore/DafnyMain.cs  
Source: https://dafny.org/dafny/DafnyRef/DafnyRef

### Proof Output Structure / Schema

Dafny delegates verification to the Boogie intermediate verification language backend, which generates SMT-LIB2 queries for Z3 (default) or CVC4. The pipeline stages are:

1. Dafny source → parser → type checker → resolver
2. Resolved program → Boogie IR translation
3. Boogie IR → per-procedure verification conditions (VCs)
4. VCs → SMT solver

Two diagnostic output modes are supported via `DiagnosticsFormats` enum:

- `DiagnosticsFormats.PlainText` → `ConsoleErrorReporter`: human-readable compiler-style messages
- `DiagnosticsFormats.JSON` → `JsonConsoleErrorReporter`: JSON diagnostic output

Pipeline outcome is captured in `PipelineOutcome` enum: `Done`, `VerificationCompleted`, `ResolutionError`, `TypeCheckingError`, `ResolvedAndTypeChecked`.

Pipeline statistics in `PipelineStatistics`: `ErrorCount`, `InconclusiveCount`, `TimeoutCount`, `OutOfResourceCount`, `OutOfMemoryCount`. `IsBoogieVerified()` returns true when all counts are zero and outcome is `Done` or `VerificationCompleted`.

### Source Attribution Model

Each error message is attributed to an exact source location: file path, line number, column number. The location identifies the specific Dafny assertion, postcondition, invariant, or precondition that could not be verified.

Standard error messages include:
- "assertion might not hold" — the `assert` expression
- "postcondition might not hold on path ..." — the `ensures` clause on the specific exit path
- "invariant might not hold" — the `invariant` clause in a loop
- "decreases expression might not decrease" — the `decreases` clause

LSP integration: the Dafny Language Server exposes errors as `textDocument/publishDiagnostics` notifications with standard LSP `Diagnostic` objects (range with start/end positions, severity, message, source).

### Proved / Not Proved / Counterexample Distinction

- **Proved**: no error emitted for a verification condition. Under `--verbose`, "verification successful" may be logged per procedure.
- **Not proved** ("might not hold"): the SMT solver found no proof within the time limit, or determined the VC is not universally valid.
- **Counterexample**: when an assertion fails, Z3 can produce a model (set of concrete values) satisfying the negation of the VC. Dafny IDE integrations display these model values inline at the failing assertion. `TimeoutCount` and `InconclusiveCount` track cases where no determination was made.

`PipelineStatistics.ErrorCount` distinguishes definite errors (type errors, parse errors) from verification failures.

### Structured Data vs. Prose

- `--format:json` (or `--error-format json`): activates `JsonConsoleErrorReporter`; outputs JSON diagnostics matching the LSP `Diagnostic` schema.
- `--format:text`: compiler-style human-readable messages.
- JSON diagnostic format: `{"severity": "error", "range": {"start": {"line": N, "character": N}, "end": {...}}, "message": "..."}` (LSP-compatible shape).
- Boogie backend: generates SMTLIB2 for the SMT solver; these queries are not surfaced to users by default but can be inspected via Boogie command-line flags.
- Exit code: non-zero when any error or verification failure is present.

### Constraint / Assumption Linkage

Contracts are expressed as Dafny language constructs:
- `requires P` — precondition; checked at all call sites
- `ensures Q` — postcondition; checked in function body for all exit paths
- `invariant I` — loop invariant; checked on entry and after each iteration
- `decreases E` — termination measure; checked to decrease on each recursive/loop step

`assume E` statement: introduces an unverified assumption; marked in output as an assumption site. Callers see the effect of the assumption in the verified postcondition.

Inter-procedural verification is modular: the callee body is not inlined; instead the caller verifies that the precondition holds, and the callee's postcondition is assumed by the caller.

`reveal` / opaque functions: control which function bodies the verifier can unfold when reasoning.

### Tooling Consumption (IDE, CI, API)

- **VS Code**: Dafny VS Code extension uses the Dafny Language Server (LSP). Diagnostics displayed inline. Counterexample model values shown in hover overlays.
- **CI**: `dafny verify` command; exit code 0/non-zero for CI pass/fail. `--format:json` for structured output.
- **Command-line pipeline**: `dafny verify`, `dafny build`, `dafny run` with consistent flag surface.
- **GitHub Actions**: via `dafny verify` in workflow steps; JSON output can be parsed by downstream steps.

### Human vs. Machine Readability

- `--format:text`: human-readable, designed for console.
- `--format:json`: machine-readable JSON, consumed by LSP clients and CI tooling.
- Counterexample model values shown inline in IDE: human-readable annotations.
- LSP `Diagnostic` schema: dual-purpose (human label + machine range).

---

## Liquid Haskell

Source: https://ucsd-progsys.github.io/liquidhaskell/specifications/  
Source: https://github.com/ucsd-progsys/liquidhaskell

### Proof Output Structure / Schema

Liquid Haskell runs as a GHC compiler plugin (`-fplugin=LiquidHaskell`). Verification errors surface as GHC compiler diagnostics at the site of the failing refinement check. There is no separate output file format.

The verification pipeline:
1. GHC type-checks the module
2. LH plugin intercepts the GHC-typed AST
3. LH generates Horn clause constraints for each refinement type check
4. `liquid-fixpoint` library solves the constraints via a fixpoint computation over a lattice of refinement predicates
5. Unsatisfied constraints map back to source locations and produce GHC diagnostic messages

Refinement type annotations are written in `{-@ ... @-}` Haskell block comments. Refinement syntax: `{v: T | predicate}` where predicate is a Boolean expression over program variables.

SMT solver: Z3 by default; CVC5 also supported. The SMT solver is called by `liquid-fixpoint` to check satisfiability of generated constraints.

### Source Attribution Model

Source attribution uses GHC's `SrcSpan` mechanism: file, start line, end line, start column, end column. The attribution point is the expression or annotation where the refinement type mismatch occurs.

The error message identifies:
- The inferred type at the site (what LH computed)
- The required type at the site (what the annotation demands)

The logical variable `VV` (value variable) is used in refinement predicates to refer to the value being checked.

Via Haskell Language Server (HLS): LH diagnostics are converted to LSP `Diagnostic` objects with standard range fields.

### Proved / Not Proved / Counterexample Distinction

- **Proved (implicit)**: no diagnostic emitted. A refinement check that passes produces no output.
- **Not proved — type mismatch**: "Liquid Type Mismatch" diagnostic is emitted. The message shows:
  ```
  The inferred type
    VV : {v : Foo a | fooLen v == myLen xs && v == Foo xs}
  is not a subtype of the required type
    VV : {VV : Foo a | fooLen VV < fooLen ?a && fooLen VV >= 0}
  ```
- **Termination error**: "non-terminating expression" at the site of a suspected non-terminating recursive call.
- **No counterexample**: LH does not expose concrete counterexample witnesses to users. The SMT solver internally finds a model violating the subtype relation, but this is not surfaced in the diagnostic message.

### Structured Data vs. Prose

- No JSON output mode from LH itself.
- LH errors surface as GHC compiler diagnostics in GHC's native format.
- GHC 9.4+ supports `--fdiagnostics-format=json`, which outputs structured GHC diagnostics including LH errors (as arbitrary plugin messages without LH-specific structure).
- Primary output: human-readable text to stderr, using GHC's standard error formatting (with source location, caret, and type expression).
- Refinement types in error messages are shown as Haskell-style type expressions with logical predicates.

### Constraint / Assumption Linkage

- `{-@ assume foo :: T @-}` — introduces an unverified assumption about `foo`'s type without checking the body.
- `{-@ measure @-}` — user-defined measure functions (uninterpreted functions in the SMT theory) used in refinement predicates.
- `{-@ lazy @-}` — disables termination checking for a specific function.
- `{-@ LIQUID "--no-termination" @-}` — module-level pragma disabling termination checking.
- `liquid-fixpoint` constraints: each refinement check generates a Horn clause of the form `∀ env. ρ ⊆ σ`, where ρ is the inferred predicate and σ is the required predicate. The fixpoint solver checks satisfiability of the conjunction of all such constraints.

### Tooling Consumption (IDE, CI, API)

- **Haskell Language Server (HLS)**: runs LH as part of the type-checking phase; converts LH errors to LSP `Diagnostic` objects displayed inline in editors (VS Code, IntelliJ via HLS).
- **Command-line**: `stack exec -- liquid file.hs` or `cabal run liquid -- file.hs`.
- **CI**: non-zero exit code on refinement failure; text output to stderr.
- No REST API or structured programmatic interface beyond GHC plugin API.

### Human vs. Machine Readability

- All output: human-readable text (Haskell type expressions, logical predicates).
- No structured JSON output from LH natively.
- Via GHC `--fdiagnostics-format=json`: GHC-level structure, but LH errors have no additional schema beyond the GHC diagnostic envelope.
- Via HLS: LSP `Diagnostic` objects (machine-readable range + message) for IDE tooling.

---

## Infer (Meta/Facebook)

Source: https://fbinfer.com/docs/infer-workflow  
Source: https://fbinfer.com/docs/all-issue-types  
Source: https://github.com/facebook/infer/blob/main/infer/src/base/IssueType.ml

### Proof Output Structure / Schema

Infer produces two output files in the `infer-out/` directory:

- `infer-out/report.json` — machine-readable; a JSON array of bug objects.
- `infer-out/report.txt` — human-readable; one bug per entry with trace.

JSON bug object fields:
- `bug_type` — string identifier for the issue class (e.g., `"NULL_DEREFERENCE"`, `"RESOURCE_LEAK"`, `"MEMORY_LEAK_C"`)
- `qualifier` — human-readable description of the specific bug instance
- `severity` — `"ERROR"` or `"WARNING"`
- `file` — source file path (relative to project root)
- `line` — line number (integer)
- `column` — column number (integer)
- `procedure` — fully qualified procedure/method name
- `procedure_start_line` — line where the procedure begins
- `bug_trace` — ordered array of trace steps
- `key` — deduplication key (string hash of bug identity)
- `hash` — content-based hash for differential comparison
- `infer_source` — the checker that produced the bug (e.g., `"biabduction"`, `"pulse"`)

Each `bug_trace` step:
- `level` — integer nesting depth (0 = top level)
- `filename` — source file for this step
- `line_number` — line number for this step
- `description` — human-readable description of what happens at this step
- `node_tags` — array of tag strings for categorization

Issue type internal schema (`IssueType.ml`): each issue type has `unique_id`, `checker`, `category` (enum: Concurrency, LogicError, MemoryError, NoCategory, NullPointerDereference, PerfRegression, ResourceLeak, RuntimeException, SensitiveDataFlow, UngatedCode, UserDefinedProperty), `visibility` (User/Developer/Silent), `default_severity` (Info/Advice/Warning/Error), `enabled` (bool), `hum` (human-readable name).

Text format:
```
file.java:5: error: NULL_DEREFERENCE
  object s last assigned on line 4 could be null and is dereferenced at line 5.
```

### Source Attribution Model

Each bug report is attributed to:
- The primary bug location: `file`, `line`, `column` — the site of the potential failure
- The procedure: `procedure`, `procedure_start_line` — the function containing the bug

The `bug_trace` provides inter-procedural attribution: each step is attributed to a specific `filename` + `line_number` with a `description` of what the analysis observed at that location. Traces cross procedure boundaries, tracking the path from an allocation or initial assignment to the eventual failure site.

Two-phase analysis: capture phase translates source to Infer IR; analysis phase runs per-function. Inter-procedural reasoning uses bi-abduction to automatically infer pre- and post-conditions (heap footprints) for each procedure.

### Proved / Not Proved / Counterexample Distinction

Infer does not emit "proved safe" signals. Absence of a bug report for a property implies no detected violation; it does not assert proof of absence.

- **Active bugs** (`severity: "ERROR"`): potential failures reachable without additional preconditions.
- **Latent bugs**: issues requiring a specific calling context to manifest; stored with `_LATENT` suffix on `bug_type` (e.g., `NULLPTR_DEREFERENCE_LATENT`). Latent issues are disabled by default in the report.
- **Warnings** (`severity: "WARNING"`): lower-confidence issues; heuristic-based.

`bug_trace` serves as the witness: it documents the execution path the analyzer followed to reach the bug. The trace is a concrete (but potentially abstract) path through the program, not a formal counterexample.

### Structured Data vs. Prose

- `report.json`: fully structured JSON array; machine-parseable.
- `report.txt`: prose-style human-readable text.
- `infer explore`: interactive CLI tool for navigating bug traces; supports `--select N` to view a specific bug and step through its trace interactively.
- No XML format.
- `--cost-report-csv`: CSV format for cost analysis (execution cost) results.
- Differential mode: compare two `infer-out/` directories to find newly introduced bugs between code versions.

### Constraint / Assumption Linkage

Bi-abduction (used by the biabduction checker): automatically discovers missing preconditions (the "frame" of the heap that must exist for the procedure to be safe) and propagates them up the call graph. This is the mechanism by which inter-procedural reasoning is achieved without requiring user-written contracts.

The Pulse checker (successor to bi-abduction): tracks symbolic value states (including ownership, validity, nullability) across procedure boundaries using "pre-/post-condition summaries" for each procedure.

Annotations used as constraints:
- `@Nonnull`, `@Nullable` (Java/ObjC): inform the nullability analysis
- `@SuppressLint("BUG_TYPE")`: suppresses a specific issue type for a method
- `@NonBlocking`: tells Infer a method makes no blocking calls (starvation analysis)

`--pulse-model-*` flags: allow users to provide models for library functions that Infer cannot analyze.

### Tooling Consumption (IDE, CI, API)

- **CI integration**: `report.json` consumed by Facebook's internal CI system; incremental analysis (`--incremental-analysis`) re-analyzes only changed files.
- **Differential analysis**: compare two Infer runs on original vs. modified code to find regressions; used in the Facebook code review pipeline.
- **GitHub Actions**: `infer` CLI invocation in workflow; exit code reflects bug count.
- **`infer explore`**: interactive human navigation of traces in terminal.
- No official VS Code extension; integration via tasks or terminal.

### Human vs. Machine Readability

- `report.json`: designed for machine consumption and CI pipeline integration.
- `report.txt`: designed for human review.
- `infer explore`: interactive human-facing trace navigation.
- `qualifier` field: the only human-readable description in the JSON (no separate `rendered` field like rustc).
- Bug trace `description` fields: human-readable prose, not structured data.

---

## CBMC (C Bounded Model Checker)

Source: https://www.cprover.org/cprover-manual/cbmc/tutorial/  
Source: https://www.cprover.org/cprover-manual/

### Proof Output Structure / Schema

CBMC supports three output formats selected by CLI flag:

- Text (default): human-readable; one line per property.
- XML: `--xml-ui`; structured XML document.
- JSON: `--json-ui`; equivalent structure as JSON.

Text format per property:
```
[function_name.kind.N] file.c line L function F description: STATUS
```

Status values: `SUCCESS`, `FAILURE`, `UNKNOWN`.

Property ID format: `[function_name.kind.N]` — e.g., `[main.pointer_dereference.6]`, `[check_input.array_bounds.1]`. The `kind` component identifies the category of check (pointer_dereference, array_bounds, signed_overflow, assertion, etc.).

XML structure:
```xml
<result>
  <property name="[property_id]">
    <description>...</description>
    <status>FAILURE</status>
    <location file="..." line="..." function="..."/>
    <trace>
      <step type="assignment|assume|assert|function_call">
        <thread>0</thread>
        <location file="..." line="..." function="..."/>
        <description>...</description>
        <assignment lhs="x" type="int">
          <value>42</value>
        </assignment>
      </step>
      ...
    </trace>
  </property>
  ...
</result>
```

`--show-vcc`: outputs the verification conditions (logical formulas) as they are passed to the SAT/SMT solver, before solving.

`--graphml-witness`: outputs counterexamples in GraphML witness automaton format (SV-COMP standard).

### Source Attribution Model

Each property is attributed to:
- Source file, line number, function name — the location of the assertion or auto-generated check.
- The property description — derived from the check type (e.g., "pointer NULL") or from user-supplied label.

Auto-generated properties: created from flags such as `--bounds-check` (array access bounds), `--pointer-check` (pointer dereferences), `--signed-overflow-check`, `--undefined-shift-check`, etc. Each flag creates properties at every relevant expression in the source.

User-defined assertions:
- `assert(cond)` — C standard assertion; property description is "assertion cond".
- `__CPROVER_assert(cond, "description")` — CPROVER-specific; the `"description"` string becomes the property description in the output.

`--function name`: specifies an alternate entry point for partial program analysis; changes which function's call graph is analyzed.

### Proved / Not Proved / Counterexample Distinction

- `SUCCESS`: the property holds on all execution paths up to the loop unwind bound. Does not constitute a proof for unbounded programs unless `--unwinding-assertions` is used and also succeeds.
- `FAILURE`: a counterexample exists. The `--trace` flag outputs the counterexample trace.
- `UNKNOWN`: the solver could not determine a result (solver timeout or incompleteness).

Counterexample trace (activated with `--trace`):
- Sequence of steps from the program entry (or `--function` entry) to the failing assertion.
- Step types: `assignment` (variable assigned a value), `assume` (path condition narrowed), `assert` (assertion checked), `function_call` (call to a function).
- Each step attributes: thread ID, file, line, function, description, and for assignments: the LHS variable name, its type, and the concrete value.

`--unwinding-assertions`: adds a verification property checking that the loop bound is sufficient. If any loop runs more iterations than `--unwind N` allows, this property reports `FAILURE`.

GraphML witness: SV-COMP standard format for counterexample exchange between tools. Consumed by witness validators such as CPAchecker and Frama-C.

### Structured Data vs. Prose

- XML format (`--xml-ui`): fully structured; property, status, location, and trace all in XML elements. Consumed by SV-COMP toolchain.
- JSON format (`--json-ui`): equivalent structure in JSON; machine-parseable.
- GraphML witness (`--graphml-witness`): machine-readable witness automaton; SV-COMP standard.
- Text output: human-readable; not designed for machine parsing.
- `--show-vcc`: outputs logical formulas; machine-readable but in SMT-LIB-adjacent notation, not a documented API.

### Constraint / Assumption Linkage

- `__CPROVER_assume(cond)` / `assume(cond)`: introduces a path constraint; prunes all execution paths where `cond` is false. Does not generate a verification property; purely for constraining the analysis.
- `__CPROVER_precondition(cond, "label")`: asserts a precondition at the current program point; generates a verification property.
- `--unwind N`: all loops are syntactically unrolled to N iterations. Combined with `--unwinding-assertions`, this creates a bounded but sound verification if all assertions pass.
- `--partial-loops`: allows loops to be unrolled fewer times than their actual iteration count; creates unsound but faster analysis for bug finding.
- `--entry-point` / `--function`: allows specification of initial assumptions by choosing an alternate analysis entry.

### Tooling Consumption (IDE, CI, API)

- **Command-line**: `cbmc source.c [flags]`; exit code reflects pass/fail.
- **CI**: XML or JSON output parsed by build systems; exit code 0 on all SUCCESS, non-zero on FAILURE.
- **SV-COMP**: XML output and GraphML witness; consumed by SV-COMP evaluation framework.
- **Eclipse CDT**: integration via plugin.
- **CMake/CTest**: integration via exit code and test driver wrappers.
- **Witness validators**: CPAchecker, Frama-C, and other SV-COMP tools consume GraphML witnesses.

### Human vs. Machine Readability

- Text output: human-readable; formatted for developer console review.
- XML/JSON: machine-readable; structured for tool integration.
- GraphML witness: machine-readable; cross-tool witness exchange format.
- Trace text (with `--trace`): human-readable step-by-step execution narrative.

---

## Rust Borrow Checker (rustc_borrowck)

Source: https://doc.rust-lang.org/rustc/json.html  
Source: https://rustc-dev-guide.rust-lang.org/diagnostics.html

### Proof Output Structure / Schema

JSON output is activated with `rustc --error-format=json`. The format is newline-delimited JSON: one JSON object per line to stderr. The stream is not a valid JSON array as a whole; each line is an independent JSON object.

Top-level diagnostic object fields:
- `$message_type` — always `"diagnostic"` for diagnostic messages
- `message` — main error message text (string)
- `code` — `{"code": "E0505", "explanation": "..."}` or `null`; error code with long-form explanation text
- `level` — `"error"` | `"warning"` | `"note"` | `"help"` | `"failure-note"` | `"error: internal compiler error"`
- `spans` — array of `Span` objects (may be empty)
- `children` — array of sub-diagnostic objects (same structure; never nested deeper than one level)
- `rendered` — the complete human-readable diagnostic as it would appear on the terminal (string)

`Span` object fields:
- `file_name` — source file path (string)
- `byte_start`, `byte_end` — byte offsets from start of file (integers)
- `line_start`, `line_end`, `column_start`, `column_end` — 1-based line and column numbers
- `is_primary` — boolean; true for the primary cause location
- `text` — array of `{"text": "source line text", "highlight_start": N, "highlight_end": N}`; the source lines covered by this span with highlight ranges
- `label` — optional string; description of what this span represents (e.g., "first mutable borrow occurs here")
- `suggested_replacement` — suggested code to replace the span content (nullable string)
- `suggestion_applicability` — `"MachineApplicable"` | `"HasPlaceholders"` | `"MaybeIncorrect"` | `"Unspecified"` (nullable)
- `expansion` — `{"span": {...}, "macro_decl_name": "vec!", "def_site_span": {...}}` for spans originating in macro expansions

### Source Attribution Model

The borrow checker uses a **multi-span model**: a single diagnostic can reference multiple source locations simultaneously. The standard borrow checker pattern:
- Span 1 (`is_primary: true`, label: "first mutable borrow occurs here") — where the conflicting borrow was created
- Span 2 (`is_primary: true`, label: "second mutable borrow occurs here") — where the second borrow occurs
- Span 3 (`is_primary: false`, label: "first borrow later used here") — where the first borrow is still live

Multiple `is_primary: true` spans are permitted within a single diagnostic to represent a causal chain.

The `expansion` field traces spans through macro expansions to the original macro invocation site and the macro definition site (`def_site_span`).

Internal representation: `rustc_span::Span` is a compact `u64`-encoded structure. The `SourceMap` maps spans to file positions. `DiagCtxt` methods `span_err`, `struct_span_err`, `span_warn` build diagnostic objects. The `Diag` builder pattern allows attaching multiple spans, notes, and suggestions before emitting.

### Proved / Not Proved / Counterexample Distinction

The borrow checker does not emit "proved safe" signals. Safety is implicit in successful compilation (zero errors).

- `level: "error"` — compilation fails; the borrowing violation is definite (the specific code path violates the borrow rules).
- `level: "warning"` — compilation succeeds; issue flagged but not fatal.
- `level: "note"` — additional context attached as `children` to an error.
- `level: "help"` — suggestion attached as `children`; may include `suggested_replacement`.

Error codes (e.g., E0505 "cannot move out of … because it is borrowed", E0502 "cannot borrow … as mutable because it is also borrowed as immutable") classify the category of borrow rule violation. `rustc --explain E0505` outputs a long-form explanation.

The multi-span model serves as the "witness" for the violation: it shows both the cause (first borrow) and the conflict (second borrow), together constituting a proof that the program violates the single-owner or aliasing rules.

### Structured Data vs. Prose

- `--error-format=json`: fully structured machine-readable output; one JSON object per diagnostic per line.
- `rendered` field: embeds the complete human-readable terminal output as a string within the JSON; allows clients to display the original formatting without re-rendering.
- `suggestion_applicability: "MachineApplicable"`: machine-actionable metadata signaling that the `suggested_replacement` can be automatically applied to fix the code.
- `--error-format=human` (default): human-readable colored terminal output.
- `--error-format=short`: compact human-readable (file:line:col: message), one line per diagnostic.
- `cargo fix` / `rustfix`: consume JSON output to collect `MachineApplicable` suggestions and apply them to source files automatically.

### Constraint / Assumption Linkage

- Lifetime annotations (`'a`, `'b`): explicit constraint declarations in function signatures, struct fields, and trait bounds. The borrow checker enforces that values live at least as long as their borrows.
- `unsafe` blocks: suppress borrow checker enforcement within the block. No verification is performed; the programmer accepts responsibility.
- `PhantomData<&'a T>`: type-level lifetime constraint carrier for structures with lifetimes not represented in fields.
- NLL (Non-Lexical Lifetimes): the borrow checker computes liveness regions via dataflow analysis rather than syntactic block structure. A borrow's lifetime ends when the last use occurs, not at the end of the enclosing block.
- Polonius (experimental): alternative borrow checker formulation using Datalog (Souffle) for constraint propagation; handles more programs correctly by tracking flows more precisely.

### Tooling Consumption (IDE, CI, API)

- **rust-analyzer (LSP)**: converts rustc JSON diagnostics to LSP `Diagnostic` objects; displays errors and suggestions inline in editors (VS Code, IntelliJ, Neovim, etc.).
- **`cargo check`**: runs type checker and borrow checker without generating artifacts; fastest path to diagnostics.
- **`cargo fix --allow-dirty`**: applies `MachineApplicable` suggestions from the JSON output automatically.
- **`clippy`**: uses the same JSON diagnostic format; additional lints beyond the compiler's built-in checks.
- **GitHub Actions**: `cargo check` or `cargo test` exit code; JSON format available by passing `--message-format=json` to cargo.

### Human vs. Machine Readability

- `--error-format=json`: machine-readable; structured JSON for programmatic consumption.
- `rendered` field: human-readable string embedded in machine-readable JSON; dual-purpose.
- `--error-format=human` (default): human-readable ANSI-colored output for terminal.
- `--error-format=short`: compact human-readable for scripts and CI logs.
- `suggestion_applicability`: machine-actionable metadata for automated fix application.

---

## Frama-C / WP Plugin

Source: https://frama-c.com/fc-plugins/wp.html  
Source: https://frama-c.com/api/frama-c-wp/index.html

### Proof Output Structure / Schema

The WP (Weakest Precondition) plugin verifies ACSL (ANSI/ISO C Specification Language) annotations. ACSL annotations are written in C comments: `/*@ ... */`.

The verification pipeline:
1. Frama-C parses C source and ACSL annotations
2. WP computes weakest preconditions for each ACSL property
3. Generated proof obligations ("goals") are discharged by:
   - **Qed**: WP's internal algebraic simplifier; discharges trivially valid goals without external solvers
   - **Alt-Ergo**: primary external SMT prover
   - **Z3, CVC4**: additional external SMT provers
   - **Coq**: interactive theorem prover for goals requiring inductive reasoning
   - All external provers accessed via the **Why3** platform (Why3 session protocol)

Goal status values per ACSL annotation:
- **Valid** — proved (by Qed or an external prover)
- **Unknown** — no prover could discharge; no counterexample found
- **Invalid** — disproved; a model violating the goal was found
- **Timeout** — prover exceeded the configured time limit
- **Running** — proof in progress

Output artifacts:
- CLI summary table: per-function, per-annotation status table in text format
- Why3 session: `why3session.xml` stores proof decisions for replay
- Goal files: exported in Why3 format (`.why`) or SMT-LIB2 (`.smt2`) for external solver consumption

### Source Attribution Model

Each goal is attributed to the ACSL annotation at a specific source location (file and line). Attribution granularity:

- `requires` clauses: attributed to the function declaration, checked at each call site
- `ensures` clauses: attributed to the function declaration, verified for all exit paths of the function body
- `assert` clauses: attributed to the specific statement in the function body
- `loop invariant`: attributed to the loop header (while/for statement line)
- `loop assigns` / `loop variant`: attributed to the same loop header

RTE (Runtime Error) plugin integration (`-wp-rte`): generates additional goals for runtime error freedom. Each goal is attributed to the specific expression (e.g., array subscript, pointer dereference, arithmetic operation) at which a runtime error might occur.

### Proved / Not Proved / Counterexample Distinction

- **Valid**: the goal is proved. Qed discharges the goal internally; otherwise, an external prover returns "Valid".
- **Unknown**: no counterexample found, but no proof found either. The goal remains unverified.
- **Invalid**: a prover (Alt-Ergo with model generation, or CBMC integration) found concrete values that violate the goal. This serves as a counterexample witness.
- **Timeout**: the time limit was reached before a decision was made.

Why3 IDE integration: for goals that Alt-Ergo or Z3 cannot discharge, the goal can be exported to Why3 IDE for interactive proof development using Coq tactics, transformations, and splitting.

### Structured Data vs. Prose

- **Ivette (GUI)**: graphical display of proof status per annotation; color-coded (green = Valid, orange = Unknown, red = Invalid). Interactive; allows re-running provers on individual goals.
- **CLI text**: summary table of proof statuses per function and per property; not machine-parseable in a standard schema.
- **Why3 session** (`why3session.xml`): XML-structured; records which prover proved each goal and with what parameters. Consumed by Why3 and WP `--replay`.
- **SMT-LIB2 export**: machine-readable; goals exported for consumption by external SMT solvers directly.
- **OCaml API** (`frama-c-wp` library): programmatic access to goal structures, statuses, and proof sessions. Documented at https://frama-c.com/api/frama-c-wp/index.html.
- **CSV/JSON report plugins**: third-party Frama-C plugins can produce structured output from the proof session state.

### Constraint / Assumption Linkage

ACSL contract structure:

- `/*@ requires P; */` — precondition; checked at all call sites; assumed true inside the function body during verification.
- `/*@ ensures Q; */` — postcondition; verified for all exit paths.
- `/*@ assumes C; */` (in contract behaviors) — enables conditional contract selection; the behavior applies when `C` holds.
- `/*@ assigns \nothing; */` — frame condition; specifies what the function is allowed to modify.

WP computes goals under the assumption that the precondition holds. The caller's obligation (precondition check) is a separate goal attributed to the call site.

`-wp-rte` (Runtime Error goals): WP generates additional goals from RTE plugin annotations for every potentially unsafe operation (array access, pointer dereference, signed arithmetic overflow, etc.). These goals are treated identically to user-written annotations.

Model-based context: the goal context exported to Why3 includes the full logical context (variable values, heap state, path conditions) as a conjunction of hypotheses. The goal is then the conclusion to be proved against these hypotheses.

### Tooling Consumption (IDE, CI, API)

- **Command-line**: `frama-c -wp file.c`; exit code 0 if all goals proved (configurable threshold via `-wp-smoke-tests` and similar options).
- **Ivette**: Frama-C's graphical interface; proof status shown per annotation with color coding; supports re-running provers interactively.
- **Why3 IDE**: for interactive proof development on complex goals; supports Coq tactic-based proofs, transformations, and splitting.
- **CI**: exit code reflects proof completeness; text summary consumed by CI logs.
- **OCaml API**: `frama-c-wp` library for programmatic integration; allows querying goal statuses, triggering prover runs, and processing proof session data.
- **Why3 session replay**: `--replay` equivalent via Why3 session files for sharing proof state across team members.

### Human vs. Machine Readability

- CLI text summary: human-readable; formatted table.
- Ivette GUI: visual; human-only.
- Why3 session (`why3session.xml`): machine-readable XML; consumed by Why3 and WP.
- SMT-LIB2 goal export: machine-readable; for direct SMT solver integration.
- OCaml API: programmatic machine-readable interface.
- Goal descriptions: human-readable prose in CLI output; structured logical formulas in Why3/SMT-LIB2 export.
```

---
