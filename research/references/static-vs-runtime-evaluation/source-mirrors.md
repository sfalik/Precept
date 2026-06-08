# Source Mirrors — Static-vs-Runtime Expression Evaluation

Snapshots of load-bearing external sources for
`research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md`.
All fetched 2026-06-05. Verbatim excerpts preserved to defend against URL rot.

---

## C++ standard — constexpr equivalence and the FP divergence carve-out

**[expr.const] — floating-point translation-vs-execution divergence**
(C++ working draft, timsong-cpp mirror of N3337; https://timsong-cpp.github.io/cppwp/n3337/expr.const ; accessed 2026-06-05). Primary.

> "Since this International Standard imposes no restrictions on the accuracy of floating-point operations, it is unspecified whether the evaluation of a floating-point expression during translation yields the same result as the evaluation of the same expression (or the same operations on the same values) during program execution."

Illustrative example from the same note:

> "bool f() { char array[1 + int(1 + 0.2 - 0.1 - 0.1)]; int size = 1 + int(1 + 0.2 - 0.1 - 0.1); return sizeof(array) == size; }"
> "It is unspecified whether the value of f() will be true or false."

**[dcl.constexpr] — constexpr function evaluation equivalence**
(https://timsong-cpp.github.io/cppwp/dcl.constexpr ; accessed 2026-06-05). Primary.

> "An invocation of a constexpr function in a given context produces the same result as an invocation of an equivalent non-constexpr function in the same context in all respects except that"
> (exceptions: the invocation can appear in a constant expression; copy elision is not performed in a constant expression.)

---

## Zig — comptime is ordinary Zig evaluated by the compiler

Zig Language Reference (master / 0.13.0; https://ziglang.org/documentation/master/ ; accessed 2026-06-05). Primary.

> "Zig places importance on the concept of whether an expression is known at compile-time."

> "All variables declared in a `comptime` expression are implicitly `comptime` variables." — and comptime variables cause "all loads and stores of the variable to happen during semantic analysis of the program, rather than at runtime."

(The reference describes comptime as ordinary Zig code partially evaluated by the compiler during semantic analysis; it does not contain an explicit "comptime values must equal runtime values" sentence — see Threats to Validity for grade.)

---

## Rust — one MIR interpreter shared by CTFE and Miri

Rust Compiler Development Guide, "Interpreter"
(https://rustc-dev-guide.rust-lang.org/const-eval/interpret.html ; accessed 2026-06-05). Primary.

> "The interpreter is a virtual machine for executing MIR without compiling to machine code. It is usually invoked via `tcx.const_eval_*` functions. The interpreter is shared between the compiler (for compile-time function evaluation, CTFE) and the tool Miri, which uses the same virtual machine to detect Undefined Behavior in (unsafe) Rust code."

---

## GCC — cross-compiler constant folding must emulate target FP (drift evidence)

GCC Internals, "Floating Point" / "Cross-compilation and Floating Point"
(https://gcc.gnu.org/onlinedocs/gcc-5.5.0/gccint/Floating-Point.html and the
gccint Cross-compilation section; accessed 2026-06-05). Primary.

> "the cross compiler cannot safely use the host machine's floating point arithmetic; it must emulate the target's arithmetic."

> "To ensure consistency, GCC always uses emulation to work with floating point values, even when the host and target floating point formats are identical."

> "all floating point constants must be represented in the target machine's format."

> "Also, constant folding must emulate the target machine's arithmetic (or must not be done at all)." — GCC Internals, Cross-compilation section.

---

## Liquid Haskell — SMT mathematical integers vs machine integers (drift evidence)

LiquidHaskell blog, "Arithmetic Overflows" (2017-03-20;
https://ucsd-progsys.github.io/liquidhaskell-blog/2017/03/20/arithmetic-overflows.lhs/ ;
accessed 2026-06-05). Primary (project blog, authoritative authors).

> "LiquidHaskell, like some programmers, likes to make believe that `Int` represents the set of integers."

> LH "has to assume some signature for this 'foreign' machine operation, and by default, LH assumes that machine addition behaves like logical addition."

Fix is opt-in: define a `maxInt` symbolic bound and a `BoundedNum` typeclass "whose signatures capture machine arithmetic," after which LH flags potentially-overflowing operations. By default the gap (unbounded SMT integers vs fixed-width machine `Int`) is unchecked.

---

## CompCert — one formal semantics, semantic-preservation proof (conformance pole)

CompCert, "Context and motivations" (https://compcert.org/motivations.html ; accessed 2026-06-05). Primary.

> "For all source programs S and compiler-generated code C, if the compiler, applied to the source S, produces the code C, without reporting a compile-time error, then the observable behavior of C is one of the possible observable behaviors of S."

> "A formal semantics is a mathematically-defined relation between programs and their possible behaviors."

> "Every compiler that we tested has been found to crash and also to silently generate wrong code when presented with valid inputs." (the miscompilation motivation)

---

## WebAssembly — reference interpreter as semantics proxy (conformance pole)

WebAssembly reference interpreter README
(https://github.com/WebAssembly/spec/blob/main/interpreter/README.md ; accessed 2026-06-05). Primary.

> "It is intended as a playground for trying out ideas and a device for nailing down their exact semantics."
> "It is written for clarity and simplicity, _not_ speed."

The repo holds "the sources for the WebAssembly specification, a reference implementation, and the official test suite"; the script/test format adds "invocations, assertions, and conversions" as testing infrastructure.

---

## Dhall — one standardized normalizer, evaluation-order-independent (config pole)

Dhall standard, beta-normalization
(https://github.com/dhall-lang/dhall-lang/blob/master/standard/beta-normalization.md ;
accessed 2026-06-05). Primary.

Dhall is "a total language that is strongly normalizing, so evaluation order has no effect on the language semantics and a conforming implementation can select any evaluation strategy." Implementations "are encouraged to implement [these] functions and operators in more efficient ways … so long as the result of normalization is the same." Type-checking compares expressions by α/β-equivalence against the single standard normal form.
