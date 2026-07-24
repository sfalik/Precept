# Absence in string interpolation — comparable-systems survey

**Status:** Active — grounds an open design decision
**Date:** 2026-07-23
**Consumer:** `docs/Working/presence-tolerant-message-rendering-2026-07-23.md`
**Sourcing:** all external claims are verbatim excerpts with stable identifiers and access date 2026-07-23. Unverified items are marked as such rather than smoothed over.

## The question

Given a language with (a) values that may be absent and (b) a static checker that refuses any program where an absent value could be read, does **appearing inside a string-interpolation hole** constitute a read that must satisfy the presence requirement?

And a second, sharper question: does the answer legitimately vary by *which* string — a human-facing message whose output is never computed on, versus an ordinary string value that flows onward into further constraints?

## Why this survey exists

Precept refuses to compile a program where an absent `optional` could be read. It also supports string interpolation. Whether `{Opt}` inside a `because "…"` rationale counts as a read was an open owner fork. No in-tree research covered it.

## Prior in-tree work — none on point

`research/INDEX.md`, `research/language/README.md`, and a grep of `research/` for interpolation / optional / null / Option turned up **no study covering optionality × interpolation**. The corpus has five string-*ordering* studies, expression-expansion domain work, and a flow-sensitive-check-placement survey. Two items touch the edge:

The nearest prior treats nullable-in-string as a type-checker rejection, framed as a diagnostic-quality problem rather than a semantics question:

> "**Current behavior:** The type checker requires both operands of `+` to be exactly `StaticValueKind.String` (no `Null` flag). A `string nullable` field has kind `String | Null`, which fails the `IsExactly(leftKind, StaticValueKind.String)` check."
>
> "**Impact:** String interpolation patterns with nullable fields — `"Case #" + CaseId` when `CaseId` is nullable — are rejected at compile time even when the author has not narrowed the field."

— `research/language/expressiveness/expression-language-audit.md` § L12

And `research/language/domain-map.md:86` already flags a missing `string-operations-research.md` covering "semantic contracts for `.length` on nullable strings."

The Precept spec line that any decision here would have to change:

> "Each `{expr}` inside `"..."` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation."

— `docs/language/precept-language-spec.md:1553`

---

## 1. Rust — refuses; no message-context exemption

`{}` requires `Display`, and `Display` is deliberately not universal:

> "The current mapping of types to traits is:
> - *nothing* ⇒ `Display`
> - `?` ⇒ `Debug`"

> "`fmt::Display` implementations assert that the type can be faithfully represented as a UTF-8 string at all times. It is **not** expected that all types implement the `Display` trait. `fmt::Debug` implementations should be implemented for **all** public types."

— https://doc.rust-lang.org/std/fmt/index.html

`Option<T>` implements `Debug`, not `Display`:

> `impl<T> Debug for Option<T> where T: Debug`

Display is absent from the trait-implementations list. — https://doc.rust-lang.org/std/option/enum.Option.html

The resulting compile error is a *designed* diagnostic with a fix-it pointing at `{:?}`:

```rust
#[rustc_on_unimplemented(
    on(
        from_desugaring = "FormatLiteral",
        note = "in format strings you may be able to use `{{:?}}` (or {{:#?}} for pretty-print) instead",
        label = "`{Self}` cannot be formatted with the default formatter",
    ),
    message = "`{Self}` doesn't implement `{This}`"
)]
```

— `rust-lang/rust`, `library/core/src/fmt/mod.rs`, attribute on `pub trait Display`

**Message positions get identical typing.** `panic!`:

> "When using `panic!()` you can specify a string payload that is built using [formatting syntax]." / "In Rust 2021 and later, `panic!` always requires a format string and the applicable format arguments"

— https://doc.rust-lang.org/std/macro.panic.html

And `thiserror`, the de-facto standard for user-facing error text:

> `#[error("{var}")]` ⟶ `write!("{}", self.var)`
> `#[error("{0}")]` ⟶ `write!("{}", self.0)`
> `#[error("{var:?}")]` ⟶ `write!("{:?}", self.var)`

— https://docs.rs/thiserror/latest/thiserror/

**Implication.** The clearest "interpolation IS a read, and a message is not special" data point in the survey. A `#[error("… {maybe_absent}")]` desugars to the same `write!` with the same `Display` bound as any other string. An escape hatch exists (`{x:?}`) but is **explicit and visible in the source text**.

*Honesty note from the surveyor: `format!("{}", opt)` was not compiled — no `rustc` on the machine. The rejection follows from the three quotes above, which are the authoritative mechanism, not from an observed run.*

## 2. Swift — tolerates with a warning, then shipped syntax for the fallback

Interpolating an Optional compiles, produces the debug description, and warns:

> "string interpolation produces a debug description for an optional value; did you mean to make this explicit?"

— verbatim in both SE-0477 (Motivation) https://github.com/swiftlang/swift-evolution/blob/main/proposals/0477-default-interpolation-values.md and apple/swift PR #5110 "Extend Optional-As-Any Warning to String Interpolation Segments", merged 2016-10-06, https://github.com/apple/swift/pull/5110

The fix-its make intent explicit rather than removing the value:

> PR #5110: "suggest[s] that the user either Insert `.debugDescription` [or] Insert a cast to `T?`"

SE-0477 restates the shipped guidance: "The compiler suggests two fixes: using `String(describing:)` … or providing a default value via the nil-coalescing operator."

**Swift 6.2 then shipped dedicated syntax**, because nil-coalescing does not type-check for non-`String` optionals:

> "the nil-coalescing operator (`??`) only works with values of the same type as the optional value, making it awkward or impossible to use when providing a default for non-string types."

```swift
let age: Int? = nil
print("Your age: \(age, default: "missing")")   // Prints "Your age: missing"
```

— SE-0477, "Default Value in String Interpolations", **Implemented (Swift 6.2)**

**Implication.** The closest precedent to "tolerated but flagged," and the ten-year arc is the finding: warn (2016) → *give the author first-class syntax to state the fallback* (2025). Swift's settled judgement is that absence in an interpolation hole is a real decision the author should make visible, and that the language should supply syntax for stating it rather than a silent default.

*Unverified: the diagnostic's internal identifier was not located in `include/swift/AST/DiagnosticsSema.def` (the only `string_interpolation` entry there is `expr_string_interpolation_outside_string`). The message text is verbatim from two sources; the identifier is unverified.*

## 3. C# — renders null as empty, but the hole is not exempt from flow analysis

Rendering, by documented design:

> "If the argument is `null`, the method inserts `String.Empty` into the result string. You don't have to be concerned with handling a `NullReferenceException` for null arguments."
> "`index` — … If this argument is `null`, an empty string will be included at this position in the string."

— https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-9.0

The interpolation handler encodes the same, with nullable-annotated parameters:

```csharp
public void AppendFormatted<T>(T value)
{
    if (value is null) { return; }
    ...
}
public void AppendFormatted(string? value)
```

— `dotnet/runtime`, `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/DefaultInterpolatedStringHandler.cs`

**The on-point finding, measured rather than read.** Nullable-reference-type flow analysis runs normally inside interpolation holes. It does not warn on the maybe-null value *itself* — because the target parameter is annotated `string?`/`T` — and it warns immediately on any *operation* on that value inside the hole. Probe built with `<Nullable>enable</Nullable>`, .NET 9:

| code | result |
|---|---|
| `$"A: [{a}]"`, `a: string?` | no warning |
| `$"B: [{n}]"`, `n: int?` | no warning |
| `$"D: [{c.Length}]"`, `c: string?` | `CS8602` **inside the hole** |
| `Need(a)` where `void Need(string t)` | `CS8604` |

> `Program.cs(9,26): warning CS8602: Dereference of a possibly null reference.`
> `Program.cs(12,6): warning CS8604: Possible null reference argument for parameter 't' in 'void Need(string t)'.`

The C# 8 nullable-reference-types specification does not carve out interpolation — interpolated strings appear exactly once, among expression forms whose *result* is always non-null. Nothing about the holes. The four "Warnings" subsections are headers with no body text, which is why the behavior was probed rather than read. — https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/nullable-reference-types-specification.md

**Implication — the sharpest correction the survey offers.** C# is *not* an example of "interpolation exempts you from the null checker." It is an example of **the hole being an ordinary expression position whose target type happens to accept null**. The check is not waived; the requirement is not there. That is a materially different justification from "messages are special," and it generalizes: if a language's interpolation-hole target is "anything renderable including absence," no exemption is needed; if the target is "a present scalar," it is a read.

## 4. Kotlin — tolerates silently

> "If an interpolated expression or variable evaluates to `null`, the Kotlin compiler inserts the text `null` into the resulting string. To replace `null` with another value, use the Elvis operator (`?:`)"

```kotlin
val text: String? = null
println("Hello, $text")                 // Hello, null
println("Hello, ${text ?: "Kotlin"}")   // Hello, Kotlin
```

— https://kotlinlang.org/docs/strings.html § "Nullable values in string templates"

**Implication.** A language whose selling point is null-safety exempts the template position — mechanically, via `Any?.toString()` being defined on the nullable receiver. Strong evidence that "absence in a display position is not a read" is a defensible position held by a serious null-safe language. But it is *silent*, which is weaker than Swift's flagged version.

## 5. SQL — the same question answered three ways inside one family

`||` propagates NULL (SQLite, general operator rule):

> "All operators generally evaluate to NULL when any operand is NULL, with specific exceptions as stated below."

`||` is not among the listed exceptions. — https://www.sqlite.org/lang_expr.html

PostgreSQL's `concat()` does the opposite:

> `concat ( val1 "any" [, val2 "any" [, ...] ] ) → text` — "Concatenates the text representations of all the arguments. **NULL arguments are ignored.**" / `concat('abcde', 2, NULL, 22)` → `abcde222`

— https://www.postgresql.org/docs/current/functions-string.html Table 9.10

MySQL's identically-spelled `CONCAT()` does the opposite of PostgreSQL's:

> "`CONCAT()` returns `NULL` if any argument is `NULL`." / `mysql> SELECT CONCAT('My', NULL, 'QL');  -> NULL`
> "`CONCAT_WS()` does not skip empty strings. However, it does skip any `NULL` values after the separator argument." / `mysql> SELECT CONCAT_WS(',', 'First name', NULL, 'Last Name');  -> 'First name,Last Name'`

— https://dev.mysql.com/doc/refman/8.4/en/string-functions.html

Oracle's `||` is a third answer — absorbing, and its own docs hedge:

> "Although Oracle treats zero-length character strings as nulls, concatenating a zero-length character string with another operand always results in the other operand, so null can result only from the concatenation of two null strings. However, this may not continue to be true in future versions of Oracle Database. To concatenate an expression that might be null, use the `NVL` function to explicitly convert the expression to a zero-length string."

— https://docs.oracle.com/en/database/oracle/oracle-database/23/sqlrf/Concatenation-Operator.html

**Implication — the strongest cautionary finding.** Four widely-deployed implementations, four behaviors, and within PostgreSQL and MySQL the answer flips between the operator and the function, with the same spelling `CONCAT()` meaning opposite things. This is what happens when a language leaves absence-in-string-context to be settled per construct rather than by one stated rule. Whatever a language picks, the lesson is that it must be **one rule, stated in the spec**, not an emergent per-position accident.

*Not verbatim-verified: PostgreSQL's own docs do not state `||`'s NULL behavior in the Table 9.9 operator entry; the `||` side rests on SQLite's general-operator rule.*

## 6. Template languages — a spectrum with an opt-in strict pole

**Mustache** — tolerate silently, no strict mode:

> "If there is no `name` key, the parent contexts will be checked recursively. If the top context is reached and the `name` key is still not found, nothing will be rendered."

— https://mustache.github.io/mustache.5.html § Variables

**Handlebars** — tolerate by default, strict is a compile flag:

> **strict:** "Run in strict mode. In this mode, templates will throw rather than silently ignore missing fields. This has the side effect of disabling inverse operations such as `{{^foo}}{{/foo}}` unless fields are explicitly included in the source object."

— https://handlebarsjs.com/api-reference/compilation.html

**Liquid** — tolerate by default; strict collects errors but still renders:

> `error_mode = :strict` "Raises a SyntaxError when invalid syntax is used" / `:warn` "Adds strict errors to template.errors but continues as normal" / `:lax` "The default mode, accepts almost anything."

```ruby
template.render({ 'x' => 1, 'z' => { 'a' => 2 } }, { strict_variables: true })
#=> '1  2 '          # renders with blanks; errors go to template.errors
```

— https://github.com/Shopify/liquid/blob/master/README.md

**Jinja2** — the one that answers the second question:

> "If a variable or attribute does not exist, you will get back an undefined value. What you can do with that kind of value depends on the application configuration: **the default behavior is to evaluate to an empty string if printed or iterated over, and to fail for every other operation.**"

— https://jinja.palletsprojects.com/en/stable/templates/

> `jinja2.Undefined` — "The default undefined type. **This can be printed, iterated, and treated as a boolean. Any other operation will raise an `UndefinedError`.**"
> ```python
> >>> foo = Undefined(name='foo')
> >>> str(foo)
> ''
> >>> foo + 42
> jinja2.exceptions.UndefinedError: 'foo' is undefined
> ```
> `jinja2.StrictUndefined` — "An undefined that barks on print and iteration as well as boolean tests and all kinds of comparisons. In other words: **you can do nothing with it except checking if it's defined.**"
> `jinja2.DebugUndefined` — "An undefined that returns the debug info when printed." `str(foo)` → `'{{ foo }}'`

— https://jinja.palletsprojects.com/en/stable/api/

**Implication.** Jinja2's default `Undefined` is a direct precedent for a split — and note precisely where the line falls. The tolerated positions are **print and iterate**; the refused positions are arithmetic, comparison, and every other computation. Two caveats: (a) the split is per-*operation*, not per-*string-literal* — an assignment whose right-hand side is a template would count as a *print* under Jinja's rule even though the result flows onward; (b) Jinja ships `StrictUndefined` precisely because the lenient default burns people, and it is widely recommended for anything non-cosmetic.

## 7. Message/diagnostic context vs. value context — what actually exists

### Systems that do tolerate more in a diagnostic context

**Go `fmt`** — formatting failures are contained, not propagated:

> "If an `Error` or `String` method triggers a panic when called by a print routine, the fmt package reformats the error message from the panic … For example, if a String method calls `panic("bad")`, the resulting formatted message will look like `%!s(PANIC=bad)`. … **If the panic is caused by a nil receiver to an Error, String, or GoString method, however, the output is the undecorated string, `<nil>`.**"

> "Too few arguments: `%!verb(MISSING)` … All errors begin with the string "%!" followed sometimes by a single character (the verb) and end with a parenthesized description."

— https://pkg.go.dev/fmt

**SLF4J** — null renders `"null"`; a throwing `toString()` is caught and inlined:

```java
if (o == null) { sbuf.append("null"); return; }
...
try { sbuf.append(o.toString()); }
catch (Throwable t) { Reporter.error(...); sbuf.append("[FAILED toString()]"); }
```

— `qos-ch/slf4j`, `slf4j-api/src/main/java/org/slf4j/helpers/MessageFormatter.java`

**Serilog** — stated as an explicit product commitment:

> "Serilog takes the view that, all things considered, logging is a lower priority than other application code and should *never avoidably impact* the operation of a running application."
> "Methods on `ILogger` and the static `Log` class silently ignore invalid arguments"
> "If these properties throw, Serilog will catch the error, write to `SelfLog`, and include the error message instead of the property value"

— https://github.com/serilog/serilog/wiki/Reliability

**Java `MessageFormat`** — missing arguments degrade to placeholder text:

> "An argument is *unavailable* if `arguments` is `null` or has fewer than argumentIndex+1 elements."
> Subformat: any | Argument: unavailable | Formatted Text: `"{" + argumentIndex + "}"`
> Subformat: null | Argument: `"null"` | Formatted Text: `"null"`

— https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/text/MessageFormat.html § `format`

**Elixir `Logger`** — lazy message evaluation:

> "Log functions also accept a zero-arity anonymous function as a message" / "the arguments given to the Logger macros are only evaluated if required by the current log level"

— https://logger.hexdocs.pm/Logger.html. *Unverified: the docs say nothing about what happens if that function raises.*

### The critical qualifier on all of the above

**Every one of these is a runtime-robustness property of a logging or printing library, not a static-typing rule.** None says the type checker relaxes for message strings. They say the printing path must not take down the process. Go's format/verb mismatch is caught by `go vet`, not the compiler; SLF4J and Serilog have no static presence checking at all; `MessageFormat` takes `Object...`.

### Systems that explicitly do not distinguish

- **Rust** — `panic!`, `assert!`, `#[error("…")]` all reduce to `format_args!`/`write!` with the identical `Display` bound. Zero exemption.
- **Swift** — the warning fires on every interpolation regardless of destination; there is no message position to exempt.
- **Kotlin, C#** — tolerate everywhere, so nothing is distinguished; the uniformity is at the permissive pole.
- **Dhall** — the strictest comparator, refusing uniformly:

  ```
  Γ ⊢ t : Text   Γ ⊢ "ss…" : Text
  ───────────────────────────────
  Γ ⊢ "s${t}ss…" : Text
  ```

  — `dhall-lang/dhall-lang`, `standard/type-inference.md`. Every interpolated expression must be proven `Text`; an `Optional Text` must be eliminated (`merge` / `Optional/fold`) before it can enter a hole. No message position, no escape hatch. *Cite the judgement block, not any prose gloss — the sentence "Interpolated expressions must have type Text" was a fetcher's paraphrase.*

**ICU MessageFormat: unverified.** The user guide covers pattern syntax, plural/select, and quoting, but does not document missing- or null-argument behavior. — https://unicode-org.github.io/icu/userguide/format_parse/messages/

---

## What the field actually does

**Refuse — interpolation is a read, no exemption:** Rust, Dhall, Jinja2 `StrictUndefined`, Handlebars `strict: true`.

**Tolerate with a visible flag:** Swift alone. Warning since 3.1; in 6.2 shipped `\(x, default: "…")` so the author states the fallback in source.

**Tolerate silently:** Kotlin (`null`), C# (`String.Empty`), Mustache (nothing), Liquid default, Handlebars default, Jinja2 default in print position, PostgreSQL `concat()`, Oracle `||`, SLF4J (`"null"`), Java `MessageFormat`, Go (`<nil>`).

**Context-dependent:** Jinja2 default `Undefined` (print/iterate tolerate, everything else raises); SQL (`||` vs `concat`, differing per engine); Go/Serilog/SLF4J (runtime containment in the printing path only, not a typing rule).

### Weight

- **Most weight sits on "tolerate"** by raw count and deployment — but this is largely a selection artifact. Most of those systems have no presence checker to exempt from. Kotlin is the only one with a real null-safety system that also exempts the template position, and it does so silently and mechanically, not as a considered design statement that could be sourced.

- Among systems that **actually carry a static presence obligation** — Rust, Dhall, Swift, C#-with-NRT, Jinja2-strict, Handlebars-strict — the balance flips hard toward **refuse or flag**. Rust and Dhall refuse outright. Swift warns, then invested a whole Evolution proposal in explicit fallback syntax. C# does not refuse, for a reason that *undercuts the message-context argument*: the hole is not exempt from flow analysis at all — the target parameter is simply `string?`. That is "the requirement was never there," not "messages are special."

- **Least weight is behind the message-vs-value split.** Rust actively refutes it. Dhall, Swift, Kotlin, and C# have no such distinction. The only genuine in-language split is Jinja2's, cut on a different axis (operation kind, not literal position) — and Jinja ships `StrictUndefined` precisely because the lenient side proved a footgun.

- **The strongest cautionary evidence is SQL**: four engines, four answers, the same spelling meaning opposite things, and one vendor warning that its own behavior may change. That is the cost of settling this per construct instead of by one stated rule.

### Bottom line for a consuming design

**No statically-checked language in this survey splits its presence rule by string position.** The message-vs-value distinction exists in the field, but it lives at the library/runtime layer, and Jinja2's split is by operation kind. A design that adopts a per-position rule is doing something the surveyed field does not do — not automatically wrong, but its Precedent leg cannot lean on precedent and must say so plainly.

A design that instead frames tolerance as a property of the *rendering operation's target type* — "the hole accepts anything renderable, including absence, so no requirement was ever there" — has direct precedent in C#'s mechanism and Jinja2's print-vs-compute axis, and does not need the unprecedented positional cut.

## Not reached / unverified

ICU MessageFormat missing-argument semantics; Swift's internal diagnostic identifier; PostgreSQL's own verbatim statement of `||` NULL propagation; whether Elixir `Logger` swallows a raising message function; Rust behavior not compiled locally (no `rustc` available) — the Rust claims rest on three primary doc/source quotes.
