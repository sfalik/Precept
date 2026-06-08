---
status: Active — horizon groundwork
external-engagement: strong
authored: 2026-06-05
author: research (lifecycle-1)
topic: How systems that evaluate expressions at BOTH compile/analysis time and runtime relate the two so they cannot disagree (shared engine vs subset vs abstract vs conformance-tested), the empirical drift cases that prove the hazard is real, and whether comparable systems enforce non-abstractable constraints (equality/string/multi-field) at compile time by concrete evaluation or defer them to runtime
---

# Static-vs-Runtime Expression Evaluation: Prior Art on Shared Evaluation Cores and Enforcement Placement

> When a system evaluates the *same* expressions at compile/analysis time and at runtime, how does it keep the two from disagreeing — and is concretely evaluating a constraint at compile time (vs deferring it to runtime) a well-precedented design? Two questions, surveyed neutrally.

## Background

Precept's proof engine evaluates expressions **at compile time** — a bounded constant-fold (`FoldValue` / `EvaluateBinaryOp` in `src/Precept/Pipeline/ProofEngine.Analysis.cs:249-399`) over the default environment — to prove that a definition's own defaults cannot violate a rule (`precept-language-spec.md:213` Principle 11; `proof-engine.md:483`). Its runtime `Evaluator` (`src/Precept/Runtime/Evaluator.cs`, currently a stub) will evaluate the **same** `TypedExpression` trees against entity data. The header contract is explicit (`Evaluator.cs:23`):

> "if the compiler emits no errors, the evaluator should never fault."

That guarantee is **sound only if the compiler's prediction of an expression's value matches what the runtime computes.** Two structural facts make this non-trivial in Precept today:

1. **Two arithmetic implementations exist.** The proof fold hand-implements decimal/string/bool arithmetic in `EvaluateBinaryOp` (`ProofEngine.Analysis.cs:351-399`). The runtime path will execute catalog-supplied `BinaryExecutors` / `UnaryExecutors` delegates embedded in opcodes by the builder (`precept-builder.md:536`, `runtime-api.md:585`). These are *different code* computing the *same* operation — the canonical two-evaluators-can-drift surface.
2. **The builder does NOT evaluate defaults at build time.** `precept-builder.md:70` ("No Evaluation") — default-value expressions are compiled to `ExecutionPlan`, not evaluated. Yet the proof fold *does* evaluate defaults at compile time. So the value the compiler proved-against (via the fold) and the value the runtime will compute (via the plan + executor delegates) come from two separate mechanisms.

`fault-system.md:280` frames the contract: a fault on *contract* data "would indicate a **proof-engine gap** — a defect to fix" (spec §0.7). A fold-vs-runtime value mismatch is exactly such a gap. This survey grounds the forthcoming `/design` decision: **should the proof fold and the runtime evaluator share a single expression-evaluation core?**

### Spec-first check (Step 1b)

Grepping `precept-language-spec.md` / `proof-engine.md` / `runtime-api.md` for shared-evaluation decisions: the spec locks that the *two compile-time scans* (ensure-fold + rule-fold) share "one default environment and one constant-fold evaluator" (`precept-language-spec.md:213`; `proof-engine.md:1987` — "no fork"). It does **not** decide whether that compile-time fold shares an engine with the *runtime* evaluator — the evaluator is a stub and the executable-model design is pending (`Evaluator.cs:46`). So Q-A is genuinely open, not spec-decided. The precision-drift concern *is* already acknowledged: `proof-engine.md:110` mandates `decimal` magnitudes "specifically to avoid binary-floating-point rounding drift." This survey is authorized horizon groundwork for that open decision.

This builds forward from the two sibling surveys written this session — `interval-vs-value-evaluation-prior-art-2026-06-05.md` and `bounds-only-constraint-enforcement-2026-06-05.md` (the CUE "value is a singleton bound" finding) — and from the R1 runtime-evaluator survey (`research/architecture/runtime/runtime-evaluator-architecture-survey.md`). It does not duplicate them; it isolates the **shared-core / drift** axis (Q-A) and the **enforce-statically-vs-defer** axis (Q-B).

## Methodology

- **Research question.** (Q-A) When a system evaluates expressions at both compile/analysis time and runtime, how does it relate the two so they cannot disagree, and what is the empirical evidence that they *do* drift when not carefully related? (Q-B) For constraints an abstract domain cannot decide (equality, string, boolean, multi-field sums), do comparable systems enforce them at compile time by concretely evaluating constants/defaults, or defer them to runtime?
- **Comparators (chosen to span the spectrum, per the prompt).** Identical-engine: Rust const-eval/Miri, Zig comptime. Shared-definition-restricted-subset: C++ constexpr/consteval. Abstract-soundly-related: abstract interpretation (covered in the sibling; cited not re-derived). Independently-implemented-conformance-tested: CompCert, WebAssembly reference interpreter. Drift evidence: GCC/LLVM cross-compiler host-vs-target floating point; Liquid Haskell SMT-int-vs-machine-int. Q-B config/refinement poles: CUE and Nickel (sibling, cited), Dhall, Liquid Haskell.
- **Search strategy.** Primary sources first: Zig language reference; the Rust compiler dev-guide const-eval/interpret page; the C++ working-draft `[expr.const]` / `[dcl.constexpr]` (timsong-cpp mirror); GCC internals "Floating Point" / "Cross-compilation"; the LiquidHaskell "Arithmetic Overflows" blog; CompCert motivations; the WebAssembly reference-interpreter README; the Dhall beta-normalization standard. WebFetch/WebSearch over ziglang.org, rustc-dev-guide.rust-lang.org, timsong-cpp.github.io, gcc.gnu.org, ucsd-progsys.github.io, compcert.org, github.com/WebAssembly, github.com/dhall-lang.
- **Inclusion / exclusion.** Included: systems that evaluate or analyze an expression statically AND have (or proxy) a runtime/target semantics for the same expression. Excluded: pure runtime evaluators with no static phase (out of scope for Q-A); interactive provers (out of Precept's "no opaque solver" scope). The abstract-interpretation pole is cited from the sibling, not re-derived.
- **Source-grade mix.** Primary for all load-bearing claims (language specs, the C++ draft, GCC internals, the Rust dev-guide, CompCert/WASM official docs, the LiquidHaskell project blog). One Tertiary flag: the Zig "same semantics" framing rests partly on training-data knowledge because the master-docs comptime section was truncated at fetch (declared in Threats to Validity).
- **Time bounds.** All web fetches 2026-06-05. Language specs/standards are stable by venue; vendor/project docs (Zig, Rust dev-guide, GCC, LiquidHaskell, Dhall) are living, dates recorded per citation. Load-bearing sources mirrored to `research/references/static-vs-runtime-evaluation/source-mirrors.md`.

## Findings

### The spectrum — five points, from "literally one engine" to "deliberately different but proven-related"

| Point on spectrum | Exemplar | How the two phases are kept from disagreeing | Closest bearing on Precept |
|---|---|---|---|
| **1. Identical engine** | Rust const-eval / Miri; Zig comptime | The *same* interpreter executes both phases; there is only one implementation, so there is nothing to drift | Share one core ⇒ drift structurally impossible |
| **2. Shared definition, restricted subset** | C++ `constexpr` / `consteval` | Standard *requires* the constexpr invocation produce "the same result as an equivalent non-constexpr function" — equivalence is a normative obligation, **but FP accuracy is explicitly carved out** | Equivalence-by-mandate, with a named soundness hole for FP |
| **3. Abstract, soundly-related (NOT identical)** | Abstract interpretation — interval/octagon domain | Compile-time runs a *different* (abstract) semantics, proven a sound over-approximation of the one concrete semantics (Galois connection) | Precept's interval domain — sound *by construction*, never claims to compute the runtime value |
| **4. Independently implemented, conformance-tested** | CompCert; WebAssembly ref interpreter | Two implementations kept in sync by one *formal semantics* — either a machine-checked preservation proof (CompCert) or a reference interpreter + test suite as the oracle (WASM) | If Precept keeps two evaluators, this is the discipline that makes it sound |
| **5. Drift pole — empirical unsoundness when NOT related** | GCC/LLVM host-vs-target FP; Liquid Haskell SMT-int vs machine-int | The hazard realized: compile-time evaluation diverged from the target/runtime and caused wrong results / missed bugs | The concrete warning Precept's `decimal`-not-`double` rule already partly heeds |

### 1. Identical engine — Rust and Zig: one implementation, nothing to drift

**Rust** is the cleanest "literally the same evaluator" data point. The compiler's compile-time function evaluation (CTFE) and the Miri runtime interpreter are *one* virtual machine [Primary; access date 2026-06-05; mirrored `source-mirrors.md`]:

> "The interpreter is a virtual machine for executing MIR without compiling to machine code. It is usually invoked via `tcx.const_eval_*` functions. The interpreter is shared between the compiler (for compile-time function evaluation, CTFE) and the tool Miri, which uses the same virtual machine to detect Undefined Behavior in (unsafe) Rust code."
> — Rust Compiler Development Guide, *Interpreter* (rustc-dev-guide.rust-lang.org/const-eval/interpret.html)

The *why* is implicit but decisive: because there is exactly one MIR interpreter, the value the compiler computes at const-eval time and the value Miri computes when interpreting the same code at "runtime" are computed by identical code — a fold-vs-runtime mismatch is not a bug to chase, it is structurally unrepresentable. (The integration carries a deliberate guard so "fancy Miri features … [don't] leak into CTFE" — i.e. the *same engine*, parameterized, not two engines.)

**Zig** makes the same move at the language level: `comptime` is ordinary Zig partially evaluated by the compiler during semantic analysis, not a separate constant-expression sublanguage [Primary; access date 2026-06-05]:

> "Zig places importance on the concept of whether an expression is known at compile-time."
> — Zig Language Reference (ziglang.org/documentation/master/)

A `comptime` variable causes "all loads and stores of the variable to happen during semantic analysis of the program, rather than at runtime" (*ibid.*). The compile-time and runtime behaviors are the *same Zig*, distinguished only by *when* they run. (The master-docs comptime narrative section was truncated at fetch; the "same semantics, different phase" framing is corroborated by the const-variable wording but partly training-data — see Threats to Validity.)

**Bearing on Precept.** This is the pole where Precept's question answers itself: if the proof fold and the runtime evaluator are *the same code* over `TypedExpression`, Principle 11's soundness condition ("the compiler's prediction matches what the runtime computes") holds *by construction*. Rust pays for this by having no separate hand-written const-folder — CTFE *is* the interpreter. Precept currently has the opposite: `EvaluateBinaryOp` is a second arithmetic implementation distinct from the catalog `BinaryExecutors`.

### 2. Shared definition, restricted subset — C++ constexpr: equivalence by *mandate*, with an FP hole

C++ does not share one engine; compile-time evaluation is a *subset* of the language (a "constant expression"). But the standard makes equivalence a **normative requirement** [Primary; C++ working draft [dcl.constexpr]; access date 2026-06-05; mirrored]:

> "An invocation of a constexpr function in a given context produces the same result as an invocation of an equivalent non-constexpr function in the same context in all respects except that" [the invocation may appear in a constant expression; copy elision differs].
> — [dcl.constexpr], timsong-cpp.github.io/cppwp/dcl.constexpr

So the *equivalence is required by the spec*, not guaranteed by sharing an implementation — a compiler is free to evaluate a constant expression with separate machinery, but it is **conformance-violating** if the result differs (modulo the listed exceptions). This is the "shared definition, derived/restricted subset" model the prompt names.

**Crucially, the one carve-out is exactly Precept's drift hazard — floating point** [Primary; [expr.const]; access date 2026-06-05; mirrored]:

> "Since this International Standard imposes no restrictions on the accuracy of floating-point operations, it is unspecified whether the evaluation of a floating-point expression during translation yields the same result as the evaluation of the same expression (or the same operations on the same values) during program execution."
> — [expr.const], with the worked example `char array[1 + int(1 + 0.2 - 0.1 - 0.1)]` whose `sizeof(array) == size` is "unspecified whether … true or false."

This is a load-bearing finding: **the single most mature "equivalence by mandate" system in production carves out floating point as the one place compile-time and runtime are permitted to disagree** — precisely because FP accuracy is unconstrained. Precept's `decimal`-only proof posture (`proof-engine.md:110`) is the deliberate avoidance of this exact hole: by never folding `double`, Precept removes the operand class that even the C++ standard refuses to guarantee.

### 3. Abstract, soundly-related (NOT identical) — the interval domain is *meant* to differ

The third point is the one most easily confused with the others: abstract interpretation runs a **different** semantics at compile time (intervals/octagons), and that is *correct by design* — it is a sound over-approximation of the one concrete semantics, related by a Galois connection. This is fully grounded in the sibling survey and is **not re-derived here**; the load-bearing excerpts (Cousot POPL'77 "another universe of abstract objects"; Miné "the interval analysis is … not very precise"; integers soundly over-approximated as reals) live in `interval-vs-value-evaluation-prior-art-2026-06-05.md` §1.

The bearing for *this* survey is the contrast: **Precept's interval/octagon proof layer is a point-3 system — it never claims to compute the runtime value, only a sound bound, so it cannot drift in the Principle-11 sense.** The drift question (Q-A) applies *only* to the **value-fold** (`FoldValue`), which is a point-1-or-2 system: it computes a *concrete* value (`5 >= 10 → false`) that it asserts equals what the runtime will compute. The interval layer is sound-by-approximation; the value-fold is sound-only-if-it-matches-runtime. Conflating them is the trap — the sibling's CUE finding ("a default-check is interval-membership at a singleton `[5,5]`") is the bridge: even the value-fold *can* be reframed as a degenerate interval check, which would move it from point 1/2 toward point 3 and dissolve the drift question for defaults.

### 4. Independently implemented, conformance-tested — CompCert and WASM: one *semantics*, two implementations

When systems deliberately keep two implementations, they bind them with one **formal semantics** and a proof-or-test oracle.

**CompCert** proves a *semantic-preservation theorem* against a single mechanized semantics [Primary; access date 2026-06-05; mirrored]:

> "For all source programs S and compiler-generated code C, if the compiler, applied to the source S, produces the code C, without reporting a compile-time error, then the observable behavior of C is one of the possible observable behaviors of S."
> — CompCert, *Context and motivations* (compcert.org/motivations.html)

The motivation is precisely the drift hazard realized in ordinary compilers: "Every compiler that we tested has been found to crash and also to silently generate wrong code when presented with valid inputs." CompCert's answer is not "share one evaluator" but "prove the two phases refine one formal semantics." Note the shape of the guarantee — "if the compiler … produces the code … without reporting a compile-time error, then [behavior preserved]" — is *structurally identical to Precept's Principle 11* ("if the compiler emits no errors, the evaluator should never fault").

**WebAssembly** uses the lighter-weight version: a reference interpreter is the executable proxy for the semantics, and a conformance test suite is the oracle that keeps independent production engines in line [Primary; access date 2026-06-05; mirrored]:

> "It is intended as a playground for trying out ideas and a device for nailing down their exact semantics."
> "It is written for clarity and simplicity, _not_ speed."
> — WebAssembly reference interpreter README

The repo ships "the WebAssembly specification, a reference implementation, and the official test suite" together; the test script format adds "invocations, assertions, and conversions" as the differential-testing harness.

**Bearing on Precept.** If Precept keeps `EvaluateBinaryOp` and the catalog `BinaryExecutors` as two implementations, point 4 is the *minimum* discipline that makes that sound: a single source-of-truth semantics (the catalog `OperationMeta`) plus a conformance harness that differentially tests "fold result == executor-delegate result" for every operation × operand-type. Precept already has the conformance-suite design in flight (`research/architecture/spec-conformance-test-suite-survey.md`); a fold-vs-executor differential is a natural row in it. But CompCert's lesson is that test-based conformance is *weaker* than sharing one implementation or one proof — the WASM/CompCert pole is the fallback, not the first choice, when sharing is impossible.

### 5. Drift pole — the empirical warnings (load-bearing)

Two production cases prove the hazard is real, not hypothetical.

**(a) Cross-compiler floating point — host FP ≠ target FP.** GCC's internals state the rule that every cross-compiler must follow [Primary; GCC Internals "Floating Point" / "Cross-compilation"; access date 2026-06-05; mirrored]:

> "the cross compiler cannot safely use the host machine's floating point arithmetic; it must emulate the target's arithmetic."
> "To ensure consistency, GCC always uses emulation to work with floating point values, even when the host and target floating point formats are identical."
> "all floating point constants must be represented in the target machine's format."
> "Also, constant folding must emulate the target machine's arithmetic (or must not be done at all)."

This is the canonical drift case the prompt names: a constant-folding evaluator (the compiler, running on the *host*) computing a value that must equal what the *target* computes at runtime. GCC's solution — **emulate the target's exact arithmetic (MPFR / soft-float), never trust the host's native FP, and emulate even when formats appear identical** — is a hard-won statement that "two evaluators of the same expression silently disagree" is a real unsoundness, severe enough to justify a full target-FP emulation library rather than a fast native shortcut. LLVM follows the same discipline (APFloat is its target-exact FP class).

**(b) Liquid Haskell — SMT mathematical integers vs machine integers.** The static reasoner and the runtime use *different number systems* [Primary; LiquidHaskell "Arithmetic Overflows" blog 2017-03-20; access date 2026-06-05; mirrored]:

> "LiquidHaskell, like some programmers, likes to make believe that `Int` represents the set of integers."

LH "has to assume some signature for this 'foreign' machine operation, and by default, LH assumes that machine addition behaves like logical addition" — i.e. the SMT solver reasons over **unbounded mathematical integers**, but the runtime executes **fixed-width machine `Int`** that overflows. By default this is a soundness gap: LH will "prove" a property (`monoPlus` always increases) that the runtime violates at `maxBound`. The fix is *opt-in*: define a `maxInt` bound and a `BoundedNum` typeclass "whose signatures capture machine arithmetic," after which LH flags overflowing operations. Until opted-in, the static model and the runtime model are different mathematical objects — the abstract-vs-concrete relationship is *not* sound for overflow.

**Bearing on Precept.** Both cases say the same thing: **whenever the static evaluator uses a different number model than the runtime, you get unsoundness exactly at the boundary where the models diverge** (FP rounding; integer overflow). Precept's exposure: (i) it already mandates `decimal` not `double` in the proof engine (`proof-engine.md:110`) — heeding case (a); (ii) the LH case is the live warning for Precept's *overflow* obligations — the proof fold's `EvaluateBinaryOp` does unbounded `decimal` arithmetic, so a fold that proves `A + B == C` over `decimal` must agree with a runtime that may have a different overflow/precision boundary. If the catalog `BinaryExecutors` ever use a narrower or differently-rounding type than the fold, that is the LH gap in Precept's clothing.

### Q-B — Enforce non-abstractable constraints at compile time, or defer to runtime?

The second question: for constraints an abstract domain *cannot* decide — `A==B`, `Status=="closed"`, booleans, `A+B==C` — do comparable systems concretely evaluate constants/defaults at compile time, or defer to runtime? The comparators sort onto a clean spectrum (the CUE and Nickel poles are grounded in the sibling `interval-vs-value-evaluation-prior-art-2026-06-05.md` §3, §5 and **cited not re-derived**):

| System | Non-abstractable constraint on a *constant/default* | Mechanism |
|---|---|---|
| **CUE** | Evaluated **statically** by lattice unification | `default 5 & >=10 → ⊥`; equality/string handled as singleton lattice points — *value-folding need not be a separate mechanism* (sibling §3) |
| **Dhall** | Evaluated **statically** by normalization | One standardized normalizer; β-reduction at type-check produces the single normal form (below) |
| **C++ `constexpr`** | Evaluated **statically** by constant evaluation | The constexpr evaluator concretely computes equality/string/integer results at translation, required equal to runtime (§2) |
| **Refinement types (LH/Dafny/F\*)** | Decided **statically** *iff* in the decidable fragment (QF-UFLIA); else needs an annotation | SMT validity over the *constant's* refinement; non-decidable ⇒ left to a runtime assertion (sibling §2) |
| **Nickel** | Deferred to **runtime** | Contracts "check that a value satisfies some property at run-time" (sibling §5 — the deferral pole) |

**Dhall is the clean "evaluate-everything-statically via one normalizer" data point** [Primary; Dhall beta-normalization standard; access date 2026-06-05; mirrored]: Dhall is "a total language that is strongly normalizing, so evaluation order has no effect on the language semantics," and implementations "are encouraged to implement [these operators] in more efficient ways … so long as the result of normalization is the same." There is **one** normalizer; it runs at type-check (to decide judgmental equality) and is the same evaluation used everywhere — the point-1 "identical engine" model applied to a config language. Equality and string constraints on constants are decided by normalizing both sides to the single normal form and comparing — concrete static evaluation, not deferral.

**The synthesis across Q-A and Q-B.** The systems that **concretely evaluate non-abstractable constraints statically** (CUE, Dhall, C++ constexpr) are *exactly* the systems that either share one evaluation core (Dhall's single normalizer, CUE's single unification engine) or **mandate** result-equality (C++). The systems that keep a separate abstract reasoner (refinement types) **defer** the non-abstractable residue to runtime assertions rather than evaluate it statically. **No surveyed system concretely evaluates a constraint statically while using a separate, unrelated evaluator for runtime — because that is precisely the drift pole (point 5) that GCC and LH show is unsound.** The spectrum is self-consistent: *static concrete evaluation and a shared/mandated-equal runtime semantics travel together.*

## Threats to Validity

- **Zig "same semantics" framing partly Tertiary.** The Zig master-docs comptime narrative section was truncated at fetch (the WebFetch returned the const-variable wording but not the introductory comptime prose). The load-bearing claim "comptime is ordinary Zig evaluated by the compiler, same semantics different phase" rests on the fetched const-variable sentences (Primary) *plus* training-data knowledge of Zig's comptime model (Tertiary). Rust (point 1) carries the "identical engine" conclusion on a fully-Primary excerpt, so the conclusion does not depend on the Zig grade; Zig is corroborating, not sole support.
- **cppreference 403 / draft mirror substitution.** The cppreference `constexpr.html` and `constant_expression` pages returned HTTP 403/404 at fetch. The load-bearing constexpr-equivalence and FP-divergence claims are instead carried by the **C++ working-draft text** (timsong-cpp.github.io mirror of N3337 / the working paper) — a *stronger* (normative-text) Primary source than cppreference, so the substitution upgrades rather than weakens the evidence.
- **GCC internals — version-pinned pages.** The modern gccint URL 404'd; the verbatim FP quotes come from version-pinned internals pages (gcc-5.5.0 Floating Point; the Cross-compilation section confirmed across gcc-3.x–13.0 search hits with identical wording). The "emulate the target or don't fold" rule is stable across two decades of GCC internals, so version-pinning is not a recency risk. LLVM's APFloat parallel is asserted from training-data (Tertiary) as corroboration only; GCC carries the drift conclusion.
- **CUE / Nickel / abstract-interpretation poles cited, not re-fetched.** Q-B's CUE (value-as-singleton-bound) and Nickel (runtime-deferral) poles, and point 3's interval-domain soundness, are grounded in the sibling `interval-vs-value-evaluation-prior-art-2026-06-05.md` (all-Primary there) and cited here. If that sibling's excerpts are wrong, this survey's Q-B table inherits the error — but they were independently verified in that doc this session.
- **Dhall normalization claim from search-surfaced spec text.** The Dhall "strongly normalizing, evaluation-order-independent, same normalization result" claim comes from WebSearch surfacing the dhall-lang standard's beta-normalization.md prose; the GitHub raw file was not deep-fetched line-by-line. The claim is core, stable Dhall design (its totality is its defining feature), low risk, but flagged as not-deep-fetched.
- **Domain mismatch.** All comparators evaluate program/config expressions; Precept evaluates flat-field entity expressions over a lifecycle. The *value-equivalence* mechanics transfer cleanly (an arithmetic/equality/string op is the same hazard everywhere); the control-flow aspects (loops, widening) are irrelevant to Precept's branch-free model (`precept-language-spec.md:162`) — which makes the shared-core option *easier* for Precept than for a general compiler (no loop interpreter to share).
- **Selection bias.** Comparators were largely prompt-specified. The enumeration spans all five spectrum points the prompt named and adds Dhall; a system that concretely-evaluates-statically-with-an-unrelated-runtime-evaluator (the missing cell) would falsify the synthesis — none was found, but absence-of-evidence is noted.

## Implications for Precept

1. **The drift hazard Principle 11 rests on is empirically real, and Precept already heeds half of it.** GCC's target-FP-emulation rule and LH's SMT-int-vs-machine-int gap are the two canonical drift cases; both reduce to "static and runtime used different number models." Precept's `decimal`-not-`double` proof rule (`proof-engine.md:110`) is exactly the GCC-style mitigation. The *un*-heeded half is the LH case: the proof fold's `EvaluateBinaryOp` and the runtime catalog `BinaryExecutors` must use the *same* number model (decimal, same overflow/rounding boundary) or the fold can prove a value the runtime won't reproduce.
2. **Sharing one core is the pole where the question dissolves; two cores is sound only under CompCert/WASM discipline.** Rust/Zig/Dhall show that one evaluation core makes Principle-11 drift structurally impossible. If Precept keeps two (`EvaluateBinaryOp` + catalog executors), the minimum sound discipline is point 4: one source-of-truth semantics (the catalog `OperationMeta`) + a conformance differential ("fold result == executor result" per op × operand-type) — a natural addition to the in-flight conformance suite. CompCert's lesson: test-based conformance is the *weaker* fallback; sharing one implementation is strictly safer.
3. **C++ is the precise precedent for "equivalence by mandate, FP carved out."** C++ requires constexpr ≡ runtime *except* for floating point, which it explicitly declares "unspecified." This is the strongest evidence that (a) demanding fold≡runtime equivalence is a mainstream, shippable contract, and (b) FP is *the* place that contract is universally abandoned — validating Precept's choice to keep `double` out of the proof fold entirely rather than try to guarantee its equivalence.
4. **The value-fold is a point-1/2 system; the interval layer is a point-3 system — do not conflate their soundness arguments.** The interval/octagon layer is sound-by-approximation and cannot drift. Only `FoldValue` (concrete value) carries the Principle-11 drift obligation. The sibling's CUE finding offers an escape: reframing a concrete default-check as a singleton-interval membership test would move the value-fold from point 1/2 toward point 3 for the *default-validity* sub-case, narrowing the surface that needs fold≡runtime equivalence to genuinely-runtime data only.
5. **Q-B: concretely evaluating non-abstractable constraints at compile time is well-precedented — but only alongside a shared/mandated-equal runtime semantics.** CUE, Dhall, and C++ all concretely evaluate equality/string/multi-field constraints on *constants* statically; none does so with an unrelated runtime evaluator. So Precept's compile-time fold of non-abstractable default constraints (which the sibling `bounds-only` doc shows is *necessary* — bounds-only misses 7/8 broken non-interval defaults) is precedented — *conditional on* the fold and runtime sharing (or being proven/tested equal on) their value semantics. Deferring those checks entirely to runtime (the Nickel pole) is the only alternative the field offers, and it gives up the whole-space prevention guarantee Precept is built on.

## Conclusions

**Across the spectrum, concrete static expression evaluation and a shared-or-mandated-equal runtime semantics travel together; no surveyed system concretely evaluates an expression at compile time while leaving an unrelated runtime evaluator free to disagree — that combination is exactly the empirically-demonstrated drift pole. For Precept's Principle-11 value-fold, the soundest postures are (1) share one evaluation core, or (2) keep two but bind them with one source-of-truth semantics + a conformance differential; the interval layer needs neither because it is sound-by-approximation.**

- **Rationale.** Principle 11 ("no error ⇒ no fault") is sound only if the compiler's predicted value equals the runtime's computed value. Rust/Zig/Dhall make this structural by sharing one engine; C++ makes it normative by mandating equivalence; CompCert/WASM make it provable/testable against one semantics. The two drift cases (GCC FP, LH overflow) show that *not* relating the two — using different number models — is concretely unsound. Precept's two arithmetic implementations (`EvaluateBinaryOp` vs catalog executors) sit on the hazardous side unless deliberately related.
- **Alternatives considered and rejected.** (i) *Two unrelated evaluators, hope they agree* — rejected: this is the GCC/LH drift pole, empirically unsound, and contradicts §0.7's "a fault on contract data = proof-engine gap." (ii) *Defer all non-abstractable checks to runtime (Nickel)* — rejected: gives up the whole-space prevention guarantee; the sibling `bounds-only` doc shows the fold catches 7/8 broken defaults bounds-only misses. (iii) *Try to guarantee FP fold≡runtime equivalence* — rejected: even the C++ standard declares this "unspecified"; Precept's `decimal`-only rule sidesteps it, which is the correct move, not a gap to close.
- **Precedent.** Rust one MIR interpreter (CTFE = Miri); Zig comptime = ordinary Zig at semantic-analysis time; C++ constexpr "same result as equivalent non-constexpr function" with FP carved out; CompCert semantic-preservation theorem against one mechanized semantics; WASM reference interpreter as semantics proxy + conformance suite; GCC "constant folding must emulate the target machine's arithmetic"; LiquidHaskell's default unbounded-Int gap; Dhall single standardized normalizer; CUE value-as-singleton-bound and Nickel runtime-deferral (sibling).
- **Tradeoff accepted.** Sharing one core (option 1) couples the proof engine to the runtime executor model and forfeits the freedom to optimize each phase independently (Rust accepts exactly this — CTFE has no separate fast const-folder). Keeping two cores with a conformance differential (option 2) accepts ongoing test-maintenance and the *weaker* (test-based, not structural) guarantee CompCert warns is inferior to sharing or proving. Either way Precept accepts that the value-fold must use the runtime's exact number model (decimal, same overflow boundary), forgoing any host-native arithmetic shortcut — the same trade GCC makes by mandating target-FP emulation.

## What would change this conclusion

- **If a sound, shipping system is found that concretely evaluates expressions statically while using a deliberately separate, unrelated runtime evaluator with no shared semantics, no mandated equivalence, and no conformance oracle** — and demonstrably does not drift — the synthesis ("concrete static evaluation and shared/mandated-equal runtime travel together") would be wrong, and Precept's two-unrelated-evaluators posture would be defensible as-is.
- **If Precept's runtime executor model turns out to make a shared core impossible** (e.g. the executable model's opcode/delegate dispatch is fundamentally incompatible with the proof engine's `TypedExpression` walk), then option 1 falls and the decision collapses to "CompCert/WASM conformance discipline is mandatory," strengthening the case for a fold-vs-executor differential in the conformance suite.
- **If the proof fold's operand domain can be proven to never exceed the runtime's representable range** (e.g. all folded defaults are literals already validated against declared bounds, so overflow is impossible at fold time), the LH-style drift hazard for Precept's value-fold would be empirically void, and the equivalence obligation would reduce to "same arithmetic on in-range operands" — a much smaller conformance surface.

## Open Questions

- **Is the runtime executor delegate set (`BinaryExecutors`/`UnaryExecutors`) usable *as* the proof fold's arithmetic?** If the catalog delegates are pure `decimal`/string/bool functions, the proof fold could call them directly instead of `EvaluateBinaryOp` — collapsing to option 1 (one core) with no new infrastructure. Feasibility is an implementation question for the executable-model design (`Evaluator.cs:46`, design D8/R4).
- **Does the singleton-interval reframing (sibling CUE finding) actually subsume the value-fold's non-numeric cases?** CUE's lattice handles equality/string as lattice points; whether Precept's `NumericInterval` abstraction can represent a string/bool default as a degenerate "interval" (or whether a separate singleton-lattice mechanism is needed) is unresolved — it bears on whether the value-fold can move from point 1/2 to point 3.
- **What is the exact runtime number model?** The conclusion's tradeoff ("fold must use the runtime's exact number model") presumes the runtime uses `decimal` with a known overflow boundary. The executable-model design has not fixed the runtime numeric representation; if it differs from the fold's `decimal`, the LH-style gap is live and must be closed.

## Sources

- **Rust Compiler Development Guide — *Interpreter* (const-eval).** Primary. https://rustc-dev-guide.rust-lang.org/const-eval/interpret.html (accessed 2026-06-05). Mirrored: `research/references/static-vs-runtime-evaluation/source-mirrors.md`.
- **Zig Language Reference** (master / 0.13.0). Primary. https://ziglang.org/documentation/master/ (accessed 2026-06-05). Comptime narrative section truncated at fetch — see Threats to Validity. Mirrored as above.
- **C++ working draft — [dcl.constexpr] and [expr.const]** (timsong-cpp mirror of N3337 / working paper). Primary (normative standard text). https://timsong-cpp.github.io/cppwp/dcl.constexpr , https://timsong-cpp.github.io/cppwp/n3337/expr.const (accessed 2026-06-05). Mirrored as above.
- **GCC Internals — Floating Point / Cross-compilation and Floating Point.** Primary. https://gcc.gnu.org/onlinedocs/gcc-5.5.0/gccint/Floating-Point.html + the gccint Cross-compilation section (wording stable gcc-3.x–13.0). (accessed 2026-06-05). Mirrored as above.
- **LiquidHaskell blog — *Arithmetic Overflows*** (Vazou et al., 2017-03-20). Primary (project blog, authoritative authors). https://ucsd-progsys.github.io/liquidhaskell-blog/2017/03/20/arithmetic-overflows.lhs/ (accessed 2026-06-05). Mirrored as above.
- **CompCert — *Context and motivations*** (Leroy / INRIA). Primary. https://compcert.org/motivations.html (accessed 2026-06-05). Mirrored as above.
- **WebAssembly reference interpreter README** (WebAssembly/spec). Primary. https://github.com/WebAssembly/spec/blob/main/interpreter/README.md (accessed 2026-06-05). Mirrored as above.
- **Dhall standard — beta-normalization** (dhall-lang). Primary. https://github.com/dhall-lang/dhall-lang/blob/master/standard/beta-normalization.md (accessed 2026-06-05; surfaced via WebSearch, not deep-fetched — see Threats to Validity).
- **Internal (Cited):** `research/architecture/compiler/interval-vs-value-evaluation-prior-art-2026-06-05.md` (CUE value-as-singleton-bound, Nickel runtime-deferral pole, interval-domain soundness, abstract-interpretation Galois-connection grounding); `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md` (the value-fold catches 7/8 broken non-interval defaults bounds-only misses — Q-B necessity); `research/architecture/runtime/runtime-evaluator-architecture-survey.md` (R1 — CEL/OPA/XState/Dhall runtime evaluator architectures, the stateless-functional convergence); `research/architecture/spec-conformance-test-suite-survey.md` (conformance-harness design for a fold-vs-executor differential). Precept-internal: `ProofEngine.Analysis.cs:249-399`, `Evaluator.cs`, `proof-engine.md:110,483,1987`, `precept-builder.md:70,536`, `runtime-api.md:585,646,656`, `fault-system.md:280`, `precept-language-spec.md:162,213`.
