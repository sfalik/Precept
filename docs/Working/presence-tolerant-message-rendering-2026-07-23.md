---
status: Draft — 2026-07-24
phase-target: TBD (obligation-creation sweep; the same sweep that must wire the already-ruled value-fault arm — both arms are equally unbuilt)
comparable-systems-research-status: strong — grounded in `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` (13 systems, verbatim excerpts, stable identifiers)
sources-consulted:
  - `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` — the comparator survey commissioned for this design; 13 systems. Its bottom line is the spine of the turned-around recommendation: no statically-checked language splits its presence rule by string position.
  - `research/language/expressiveness/expression-language-audit.md § L12` — the nearest in-tree prior: nullable-in-string treated as a type-checker rejection.
  - `docs/language/precept-language-spec.md § 0.1` — the eleven principles; Principle 1 (prevention), 3 (determinism), 4 (inspectability), 10 (totality), 11 (static completeness) are all load-bearing here.
  - `docs/language/precept-language-spec.md:1553` — "Each `{expr}` inside `\"...\"` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation."
  - `docs/language/primitive-types.md:118` — `.length` "Requires presence guard (`is set`) for optional fields." Canon: reading an optional requires a guard.
  - `docs/compiler/literal-system.md:145` — "The language has no `null` literal. Optional fields use `is set` / `is not set`."
  - `docs/compiler/literal-system.md § String Coercion Table` — the thirteen-row scalar table, invariant culture; it omits the seven business-domain types and `choice` (see § Adjacent gaps).
  - `docs/compiler/soundness-and-coverage.md:229-230` — the two rows this design is the **presence** half of. Row :229 is the value-fault arm (ruled 2026-07-23); row :230 is this arm.
  - `src/Precept/Pipeline/SlotValue.cs:100` — `BecauseClauseSlot(string Message, SourceSpan Span)`; the message is a raw string, not an expression tree.
  - `src/Precept/Pipeline/Parser.Expressions.cs:558-564` — a message's holes are parsed then flattened to the literal `{}` and discarded (the field name does not survive).
  - `src/Precept/Pipeline/ProofEngine.cs:208` — `CollectObligations` enumerates constructs by hand and never visits `.Message`.
  - `src/Precept/Pipeline/ProofEngine.cs:340-345` — the `TypedPostfixOp` skip: the walker deliberately does **not** recurse into the operand of `is set` (the basis for "read, not named").
  - `src/Precept/Pipeline/ProofEngine.cs:347-357` — the interpolation walk, and the `includeOptionalArgRefs` asymmetry (BUG-057).
  - `samples/clinic-appointment-scheduling.precept:62` — the shipped coalescing idiom `if X is set then X else "…"`; the fallback mechanism already in the corpus.
  - `samples/customer-profile.precept:27` — a live corpus rule interpolating an `optional` field; already guarded by `when … is set`.
  - `samples/insurance-claim.precept:96` — a live corpus `reject` message; interpolates a **required** field, not an optional.
  - commit `62479cae` (2026-07-23) — the value-fault owner ruling; **docs-only** (2 files, no `.cs`), so the value-fault arm is ruled-but-unbuilt.
  - `docs/Working/bugs.md` BUG-056 … BUG-061 — the six defects found while probing; referenced by number, none re-filed here.
  - live `Compiler.Compile` probes, 2026-07-24 — recorded in § What HEAD actually does. (The precept MCP server was disconnected during this pass; verification used a direct harness against `src/Precept/Precept.csproj` that prints `Diagnostics` and `Proof.Obligations`, then was removed.)
---

# Presence-tolerant message rendering

> **This document was turned around on 2026-07-24.** It began arguing that a bare `{Opt}` render is
> *tolerant* — that rendering an absent optional is a total operation minting no presence obligation.
> That spine was refuted from three independent directions (see § Why the tolerant spine was refuted)
> and is no longer the recommendation. The document now recommends the opposite: **an interpolation
> hole is a read, and refuses uniformly** — every optional field a hole reads carries a presence
> obligation, regardless of hole shape or string position. The title is kept for continuity; the
> content is the strict reading. Status stays **Draft** — this does not lock.

## How to read this document

Three plain terms carry most of the weight, so they are defined once here and then used in plain words.

- **Hole** — the `{...}` inside a quoted literal. `"amount {Amount} over limit"` has one hole.
- **Message position** — the text of a `because` rationale on a `rule` or `ensure`, or the text of a
  `-> reject "..."` refusal. Its output is read by a person. Nothing in the precept computes on it.
- **Value position** — every other place a quoted literal can appear: the right-hand side of a `set`,
  a default, a condition, a guard, a function argument. Its output becomes or feeds governed data.
- **Read** vs **named** — a hole *reads* a field when its value is coerced or computed on; it merely
  *names* a field when the field appears only as the operand of a presence test (`{Opt is set}`),
  which consumes no value. This distinction is load-bearing and is the subject of Rule R2.

The question this document settles is what happens when a hole reads a field that might not have a
value — an `optional` field that is currently unset.

## Goal

When done: the language states — in canon, not by accident of an unwalked code path — that an
interpolation hole is a **read**, and that reading an unset `optional` at a hole is refused exactly as
it is refused in any other read position, with no special case for message text. The message-vs-value
divergence visible at HEAD is closed as the walk-gap it is, not blessed as a semantic. An author who
wants to render an optional into a message writes the coalescing conditional the language already
ships — `if Opt is set then Opt else "fallback"` — which discharges presence and needs no new syntax.

## Scope

- **In scope**: presence of an optional value at an interpolation hole; the boundary between what a
  hole *reads* and what it merely *names*; the scope line between message positions and value
  positions (and the finding that there should be none); how presence enrollment interacts with the
  string-length interval the proof engine already computes.
- **Out of scope**: value faults inside a message hole (division by zero, overflow, `sqrt` of a
  negative). The owner ruled that arm on 2026-07-23 (commit `62479cae`) — those enroll and are caught
  at compile time. This document is the **presence** half of the same fork and does not re-open the
  value-fault half.
- **Out of scope**: `BUG-053` (an optional event argument read in arithmetic mints nothing). That is
  a pure value position with no rendering defence; it is an independent bug.
- **Adjacent, not fixed here**: the String Coercion Table's omission of the seven business-domain
  types and `choice`. Under the strict reading an absent value never reaches the renderer, so this is
  no longer even a mechanical prerequisite — it is a separate new-surface gap, named in § Adjacent gaps.

---

## What HEAD actually does

Every claim in this section was established by a live compile on 2026-07-24 (direct `Compiler.Compile`
harness) or by a path:line source cite. This is the baseline the design changes, recorded first so no
later section can drift from it.

### The message is not an expression

`BecauseClauseSlot` is declared at `src/Precept/Pipeline/SlotValue.cs:100`:

```csharp
public sealed record BecauseClauseSlot(string Message, SourceSpan Span)
    : SlotValue(ConstructSlotKind.BecauseClause, Span);
```

The message is a raw `string`, not an expression tree. `src/Precept/Pipeline/TypeChecker.Normalization.cs:113`
wraps it unchanged:

```csharp
var message = new TypedLiteral(TypeKind.String, becauseSlot.Message, becauseSlot.Span);
```

The same pattern recurs at `:171` and `:251`. `reject` is further out still — it never becomes a
`TypedExpression` at all, staying a bare `string?` on the row record
(`src/Precept/Pipeline/SlotValue.cs:125`, `src/Precept/Pipeline/SemanticIndex.cs:561`).

**What happens to the holes.** They *are* parsed — `ParseBecauseClause` calls the same
`ParseInterpolatedString` that value positions use, running the full expression parser on every hole
(`src/Precept/Pipeline/Parser.Expressions.cs:436-477`). The result is then flattened and thrown away:

```csharp
// src/Precept/Pipeline/Parser.Expressions.cs:558-564
message = interpolation is InterpolatedStringExpression interpolated
    ? string.Concat(interpolated.Segments.Select(segment => segment switch
    {
        TextSegment text => text.Text,
        HoleSegment => "{}",          // ← the hole expression is discarded
        _ => string.Empty,
    }))
```

So `because "unknown {NoSuchField}"` produces `Message == "unknown {}"` — **not even the field name
survives**. Two consequences worth separating:

1. *For implementation*: the information exists at parse time and is discarded. Making messages
   checkable is a slot-shape change (`string` → expression) plus wiring the existing walks, not new
   parsing work.
2. *For the scope question*: message positions are excluded **twice** — the slot holds no expression
   tree, *and* `CollectObligations` (`src/Precept/Pipeline/ProofEngine.cs:208`) enumerates constructs
   by hand and never visits `.Message` even though `TypedRule` and `TypedEnsure` both carry one
   (`SemanticIndex.cs:569-585`). Grepping `src/Precept/Pipeline/ProofEngine.cs` for `.Message`
   returns nothing. **This is why message holes mint nothing today — the walker never reaches them.
   It is a walk-gap, not a designed exemption.** That distinction is the whole of Decision 3.

Three probes confirming the effect (all compile with zero diagnostics):

```precept
rule Amount > 0 because "unknown {NoSuchField}"      # the name is never resolved
rule Amount > 0 because "bad {Amount + true}"        # decimal + boolean, no type error
rule Amount > 0 because "tags {Tags}"                # a collection, which the spec says is a compile error
```

The third contradicts canon (`precept-language-spec.md:1553`; the String Coercion Table's
`Collection | Compile error` row) — and the gap is **not** confined to message positions.
`ResolveInterpolatedString` (`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:845-869`)
performs no type validation on hole types at all, and the diagnostic intended for it,
`InvalidInterpolationCoercion` (PRE0051), is allow-listed as never emitted
(`src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs:39`). Filed as **BUG-058**; not absorbed here.

### Nothing renders messages, anywhere

A repo-wide sweep of `src/`, `tools/`, and `test/` for code that substitutes hole values into a
message found **none**. The runtime evaluator is a stub (`src/Precept/Runtime/Evaluator.cs:45`);
`src/Precept/Runtime/Precept.cs:84` passes a reject string through verbatim; `Faults.cs:51`'s
`string.Format` formats the fault catalog's own templates, not DSL interpolation; the language server
unwraps `TypedLiteral` and prints it raw (`RichHoverFactory.cs:2970-2980`); `ProofEngine.cs:998`
renders a `TypedInterpolatedString` as the placeholder `"<string>"`. There is **no existing rendering
behavior to stay consistent with**, and the flattened `"unknown {}"` form is not a viable template, so
whatever this design decides requires the slot-shape change regardless.

### The value-fault arm is ruled but unbuilt — same wiring still to do

`soundness-and-coverage.md:229` records the value-fault arm as an owner ruling (2026-07-23). That
ruling is real, but it is **not yet built**: commit `62479cae` that recorded it touched two docs and
no `.cs` file (`git show --stat 62479cae`), and `ProofEngine.cs` still never walks `.Message`. So the
value-fault arm and the presence arm are in the **same** unbuilt state — both wait on the same
slot-shape change plus obligation-creation sweep. This corrects an earlier synthesis that leaned on
the value-fault positions being "already made symmetric" with value positions; they are not, because
neither arm's message walk exists yet. The row's characterisation of the fix as "same fix shape as the
default-expression rows above" also understates it: the default rows have a parsed expression a walker
skips; the message has no expression at all until the slot is reshaped.

### The four positions, measured — and the divergence is a walk-gap

| Position | Probe | Presence minted? | Value fault minted? | Type-checked? |
|---|---|---|---|---|
| `because "…{Opt}…"` | `rule Amount <= Limit because "note: {Note}"` | No | No — `{100 / Divisor}` is silent | **No** |
| `-> reject "…{Opt}…"` | `-> reject "note {Note}, ratio {100 / Divisor}"` | No | No | **No** |
| `set F = "…{Opt}…"` | `set Marker = "note is {Note}, ratio {100 / Divisor}"` | **Yes** (PRE0116) | **Yes** (PRE0083) | Yes |
| `set F = '…{Opt}… hours'` (typed constant) | `set Window = '{Days} hours'` | **Yes** (PRE0116) | — | Yes |

At HEAD the split is **message positions mint nothing; value positions mint everything**. The design
turnaround is precisely that this split is not a semantic to preserve — it is the walk-gap documented
above (the message slot holds no expression, and `CollectObligations` never visits it). The survey
found no statically-checked language that splits its presence rule by string position; HEAD does not
either — it just fails to check messages at all. Closing the gap makes all four positions behave the
same, which is what the strict reading asks for.

### The string-length interval already reads holes — and presence is load-bearing under it

This is refutation direction #1 for the tolerant spine, so it is recorded as a measured baseline here
and argued in § Why the tolerant spine was refuted. The proof engine computes a static character-width
interval for an interpolated template and discharges length containment from it. For a **string** hole
it takes the field's declared `minlength` as the floor via `LengthIntervalFromModifiers`
(`src/Precept/Pipeline/ProofEngine.Lengths.cs:235-241`), and that floor drives a *provably-contained*
or *provably-violating* verdict (`:33-36`). The file has no reference to presence or optionality
anywhere (`grep -n "Presence\|Optional" src/Precept/Pipeline/ProofEngine.Lengths.cs` → nothing).

**Verified probe, 2026-07-24** (harness). Both `Opt` and `Marker` declared `minlength 5 maxlength 5`:

```precept
field Opt as string optional minlength 5 maxlength 5 editable
field Marker as string minlength 5 maxlength 5 default "xxxxx"
from Draft on Stamp
    -> set Marker = "{Opt}"
    -> transition Done
```

Ledger, unguarded:

```
LengthContainmentProofRequirement  Proved   strat=LengthContainment
PresenceProofRequirement           Unresolved
DIAG UnprovedPresenceRequirement (PRE0116) on 'Opt'
```

The length proof **passes** — the engine derives the hole width as `[5..5]` from `Opt`'s declared
floor of 5 and fits it inside `Marker`'s `[5..5]`. The **only** diagnostic holding the program is the
presence obligation. Add the guard and everything clears:

```precept
from Draft on Stamp when Opt is set
    -> set Marker = "{Opt}"     # PresenceProofRequirement Proved strat=GuardInPath, no diagnostics
```

So presence is the single load-bearing check on this program. Remove it (the tolerant spine's
recommendation) and an **absent** `Opt` — which renders empty, width 0 — flows into a field that
forbids anything shorter than 5, with the length proof having falsely certified `[5..5]`. This is
BUG-059's territory (the floor should be 0 for an optional, per the code's own comment at
`ProofEngine.Lengths.cs:163-168`), but it is more than a bug: it shows the presence obligation is not
"unnecessary by construction." It is the check keeping the length proof honest.

### Optional event-arg holes mint no presence even in a value position (BUG-057)

**Verified probe, 2026-07-24** (harness). `Stamp.Note` is an **optional event argument**:

```precept
field Log as string maxlength 200 default ""
event Stamp(Note as string optional maxlength 100)
from Draft on Stamp
    -> set Log = "note is {Stamp.Note}"     # compiles CLEAN — zero presence obligation
```

The ledger carries only length obligations; there is **no** presence obligation on `Stamp.Note`, even
though this is a value position (`set` RHS) where a field read of an unset optional would be PRE0116.
The cause is `ProofEngine.cs:347-357`: a plain `TypedInterpolatedString` walks its holes passing
`includeOptionalArgRefs` through unchanged (default `false`), while an `InterpolatedTypedConstant`
forces it `true`. So the value walker itself has a hole for event-argument reads. **This is why "just
reuse the value walker verbatim" ships a soundness hole** — the value walker under-mints for arg refs.
Filed as **BUG-057**; it must close before message holes are enrolled by copying the value path.

### The coalescing idiom already works inside a hole — and narrows for args but not fields

The language already ships `if X is set then X else "fallback"` as the presence-discharging idiom
(`samples/clinic-appointment-scheduling.precept:62`). It works **inside an interpolation hole**.

**Verified probe, 2026-07-24** (harness), event-arg operand:

```precept
event Stamp(Note as string optional maxlength 100)
from Draft on Stamp
    -> set Log = "note: {if Stamp.Note is set then Stamp.Note else "none"}"   # compiles CLEAN
```

The conditional discharges presence and the whole hole is clean — no PRE0116. This is the fallback
mechanism, and it needs no new language surface (see Decision 4).

**But it narrows for an event arg and not for a stored field.** Same shape, field operand:

```precept
field Opt as string optional maxlength 20 editable
from Draft on Stamp
    -> set Marker = if Opt is set then Opt else "unset"   # PRE0116 on 'Opt', obligation Unresolved
```

The `then`-branch read is provably present (the expression is pure; nothing writes between the
`is set` test and the read), yet the field read is not discharged while the arg read is. Filed as
**BUG-061**; it must close for the fallback idiom to cover field reads without an outer `when` guard.

### A live hole found while probing (BUG-056)

```precept
field BaseCurrency as currency optional
field Rate as exchangerate in '{BaseCurrency}' to '{QuoteCurrency}' optional maxplaces 8
```

compiles with **zero obligations**. A typed-constant hole in a *declaration* position is not walked,
so an absent currency flows into the qualifier unchecked. Contrast `set Window = '{Days} hours'`, which
correctly mints PRE0116. Filed as **BUG-056**; the same family of unwalked interpolation.

---

## Why the tolerant spine was refuted

The document began recommending **R1-tolerant**: a bare `{Opt}` render mints no presence obligation
because rendering is total on absence. Three independent passes refuted it.

**1 — The length machinery makes presence load-bearing (measured).** § "The string-length interval
already reads holes" above shows a program whose *only* diagnostic is the presence obligation, kept
sound by that obligation alone. Removing it lets an absent optional render the wrong width into a
field that forbids it. The tolerant spine's claim that the obligation is "unnecessary by construction"
is false against live machinery. And no single language-chosen sentinel repairs it: to satisfy a
`minlength 5` field a sentinel must be ≥ 5 characters, but to satisfy a `maxlength 3` field elsewhere
it must be ≤ 3 — no string is simultaneously wide enough and narrow enough for every field, so **no
sentinel is sound**. The only sound disposition is to keep the absent value out of the renderer, which
is what refusal does.

**2 — A three-lens design panel disqualified the tolerant candidate unanimously.** Reviewed through
soundness, philosophy, and buildability, the tolerant candidate re-permits the exact silent
empty-string coercion that Precept spent three revisions deleting:

1. `git show c4d0abf8:docs/LiteralSystemDesign.md:121` — the original broad tolerant rule: *"In
   expressions, `null` … is coerced to empty string `\"\"` in string interpolation contexts."*
2. `git show 9ab60e47` (EvaluatorDesign.md, "Null Handling") — reversed it: *"`.length` on a `null`
   value produces an evaluation error … `null` is not coerced to empty string."*
3. Current canon — went further: the `null` literal was removed
   (`docs/compiler/literal-system.md:145`), absence became a first-class runtime value with an
   explicit `IsAbsent` (`docs/runtime/evaluator.md:228`), and reading an optional was made to *require
   a guard* (`docs/language/primitive-types.md:118`: `.length` *"Requires presence guard (`is set`)
   for optional fields."*).

Every step moved **away** from silent tolerance of absence in a string. Tolerant rendering would walk
that back — reintroduce the empty-string coercion in the one position (`{Opt}`) canon had not yet
closed. Under Principle 4 (full inspectability), a message that silently renders empty also hides, at
the moment of explanation, that the value it reports was never there.

**3 — A red-team confirmed the refutation** and, in doing so, corrected the strict framing in three
places that are folded into the rules below: the rule must be stated over what a hole **reads**, not
what it names (else guards break); it must be **recursive** at every nesting depth (a nested render is
a hole); and the value-fault arm it leans on is **unbuilt**, not accomplished.

---

## The proposal — refuse uniformly

### The core semantic: a hole is a read

A presence obligation exists because reading an absent optional leaves an operation **with no result**
— no decimal to divide, no string to take the length of. Principle 10 (totality) requires every
expression to produce a result, so the compiler must prove the value is present before any read.

Coercing a value to display text is a **read**. It consumes the value exactly as `.length` or `+`
does — it must produce the value's characters, which for an absent optional do not exist without a
language-chosen fabrication that § Why the tolerant spine was refuted showed is unsound. There is no
privileged "bare-render" tier that reads nothing: the render step reads. So a hole reading an optional
is an ordinary read and enrolls presence like any other.

> **Rule R1 (a hole is a read).** Coercing a value to its display text is a read. A hole that reads an
> `optional` field mints a presence obligation (PRE0116) **identical** to that field's obligation in a
> value read position — regardless of hole shape (`{Opt}`, `{Opt.length}`, `{10 / Opt}`) or string
> position (message `because`/`reject` vs value `set`/default/guard). One rule generates every
> disposition; there is no split by string position.

> **Rule R2 (read, not named; at every depth).** The obligation attaches to what a hole *reads*, not
> what it *names*, applied **recursively** at every nesting depth.
> - `{Opt is set}` names `Opt` but does not read it: `is set` is a presence test whose operand the
>   walker deliberately does not recurse into (`src/Precept/Pipeline/ProofEngine.cs:340-345` — the
>   `TypedPostfixOp` skip, whose own comment says recursing "would generate a spurious
>   PresenceProofRequirement on an optional X, defeating the purpose of the presence guard"). So
>   `because "{Opt is set}"` mints **nothing** — and stating the rule over "named" instead of "read"
>   would break every guard.
> - A nested render `{"prefix {Opt}"}` is a hole containing an inner hole. R1 applies to the inner
>   hole at its own boundary — it reads `Opt` and mints presence. There is no "outermost hole reads
>   nothing" carve-out; a hole is a hole at every depth.

> **Rule R3 (typed-constant holes).** A `'...'` typed-constant hole reads its operand **and** feeds a
> content validator that is not defined on absence. It mints presence for both reasons and is excluded
> from any would-be render tolerance. This rule held against the red-team unchanged.

R3's independent justification is what makes it non-negotiable at its edge. A typed constant's content
is validated against the context type:

```precept
field Days as integer optional
from Draft on Stamp
    -> set Window = '{Days} hours'      # PRE0116, correctly — verified at HEAD
```

If absence rendered tolerantly here, an unset `Days` would produce `'<sentinel> hours'`, which no
duration validator accepts — a runtime content failure from a clean-compiling precept, a direct
Principle 11 breach. Under refuse-uniformly this case refuses anyway; R3 records *why* it must, so no
future relaxation of R1 can accidentally reach typed constants.

### The boundary cases, resolved

R1 and R2 generate these rather than enumerating them. Under refuse-uniformly the answer no longer
depends on string position, so there is a single column.

| Hole | What it reads | Presence obligation on `Opt`? | Why |
|---|---|---|---|
| `{Opt}` | `Opt` | **Yes** | The render step reads `Opt`; R1 |
| `{Opt is set}` | *(nothing)* | **No** | Names but does not read `Opt`; R2 — the `is set` operand is skipped |
| `{if Opt is set then Opt else "x"}` | `Opt`, under a discharging test | **No** (once BUG-061 closes) | The conditional narrows presence; the fallback idiom |
| `{10 / Opt}` | `Opt` | **Yes** | `/` reads `Opt`; the divisor also enrolls per the value-fault ruling |
| `{Opt.length}` | `Opt` | **Yes** | `.length` reads `Opt`; already canon (`primitive-types.md:118`) |
| `{trim(Opt)}` | `Opt` | **Yes** | A function argument is a read |
| `{Opt + "x"}` | `Opt` | **Yes** | `+` reads `Opt` |
| `"{Opt} and {Other}"` | `Opt`, `Other` | **Yes** for each | Per-hole; R1 applies to each independently |
| `{"prefix {Opt}"}` (nested render) | `Opt` (inner hole) | **Yes** | R2 — the inner hole is a hole at its own depth |
| `'{Opt} hours'` (typed constant) | `Opt` | **Yes** | R3 — reads, and feeds a validator not total on absence |

One case from the original brief does not exist and is struck rather than answered:

- **A ternary conditional arm.** Precept has no `? :` ternary. `because "{Note is set ? Note : "x"}"`
  fails at the lexer (`'?' is not a valid character`, PRE0005, verified). The conditional the language
  *does* have is the `if … then … else …` keyword form, which is the fallback idiom and is handled in
  the table above and Decision 4.

### What an absent value renders as — moot for presence

Under refuse-uniformly an absent optional **never reaches the renderer**: the program does not compile
until presence is proven or a fallback supplied. So there is no language-chosen sentinel to pick, and
the whole "what does absence render as" question dissolves for the presence arm. What an author writes
when they *want* to render an optional is the coalescing conditional — see Decision 4. (The separate
question of how a *present* business-domain value renders to display text is a real, unfixed gap — see
§ Adjacent gaps — but it is determinism surface, not presence.)

---

## Adjacent gaps, named not inherited

**Business-domain coercion is new determinism surface, not a settled prerequisite.** The String
Coercion Table (`docs/compiler/literal-system.md § String Coercion Table`) covers thirteen scalar
types plus a Collection row. It omits the seven business-domain types (`money`, `currency`,
`quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`) and `choice`. The probe
`set Marker = "paid {Amt} in {Cur}"` with `Amt as money` compiles the money hole without a type error,
so *something* renders it, but nothing in canon says what. Deciding how `money`/`quantity`/`choice`/etc.
render to display text is **new Principle-3 (determinism) surface** — each format (`"1000.00 USD"` vs
`"$1,000.00"` vs `"USD 1000"`) is a design decision needing its own rationale, precedent, and tradeoff.
This is not a mechanical row this design can just fill in on its way past; it is an adjacent surface
gap that needs its own consultation. Named here so the promotion that eventually edits that table is on
notice, and explicitly **not** claimed as a prerequisite this design has settled.

**Collection-in-interpolation is unenforced everywhere (BUG-058).** Canon says collections are a
compile error inside interpolation; PRE0051 has zero emission sites. Independent of this design; filed.

---

## Preconditions and dependencies

The strict reading is only sound once the walker it will reuse is itself sound. These are verified
defects (see `docs/Working/bugs.md`), listed in the order they gate this design:

- **BUG-057 — must close before enrollment.** Optional **event-arg** holes mint no presence even in a
  value position (`"note is {Stamp.Note}"` → clean, zero obligation; verified 2026-07-24). "Reuse the
  value walker verbatim" ships this hole, because the value walker under-mints arg refs
  (`ProofEngine.cs:347-357`, the `includeOptionalArgRefs` gate). Enrolling message holes by copying
  the value path would inherit it.
- **BUG-060 — must close before enrollment.** A state-scoped `ensure X is set` does not discharge
  presence for a read in that state (verified 2026-07-24). Several of the corpus's unguarded message
  holes rely on exactly this shape; enrolling before this closes gives legitimately-guarded authors a
  false error caused by the engine's blindness, and any workaround they reach for becomes permanent.
- **BUG-061 — must close for the fallback idiom to cover field reads.** `if X is set then X else …`
  discharges presence for an event arg but not for a stored field (verified 2026-07-24). Since the
  fallback mechanism (Decision 4) is that idiom, it must narrow for fields too, or authors are pushed
  back onto an outer `when` guard for cases the idiom should cover inline.
- **BUG-059 — interacts with the length machinery.** A string hole's length floor uses the field's
  `minlength` while the code's own comment says it must be 0 for optionals, and that floor drives a
  provably-violating verdict. Under refuse-uniformly an absent value never renders, which removes the
  *unsound-acceptance* pressure, but the two length paths still disagree and should be reconciled.
- **BUG-056 — same family.** A declaration-position typed-constant hole is never walked; an optional
  flows into a qualifier unchecked. Should close as part of making interpolation walks uniform.
- **BUG-058 — same surface.** Collection-in-interpolation rule unenforced (PRE0051 dead).
- **The value-fault arm is unbuilt (`62479cae` docs-only).** Both arms wait on the same slot-shape
  change (`BecauseClauseSlot.Message` → expression) plus the obligation-creation sweep. The presence
  arm does not ride on the value arm being done — it is not.

---

## Decisions

### Decision 1: An interpolation hole is a read; it enrolls presence

**Stakes**: high

- **Rationale**: Coercing a value to display text consumes the value — it must produce the value's
  characters, which for an absent optional do not exist without a fabricated sentinel that the length
  machinery proves is unsound (§ Why the tolerant spine was refuted, direction 1). So the presence
  obligation's trigger — *"this read has no result when the value is absent"* — **is** met by the
  render step. Enrolling presence satisfies Principle 10 directly rather than carving an exception
  into it, and closing the message/value divergence as the walk-gap it is (rather than a semantic)
  keeps one rule across all four positions.
- **Alternatives considered and rejected**:
  - *R1-tolerant — a bare render mints nothing because rendering is total on absence.* This was the
    original recommendation. **Rejected on three independent grounds** (§ Why the tolerant spine was
    refuted): the length machinery makes the presence obligation load-bearing and no sentinel is sound;
    a three-lens panel found tolerance re-permits the silent empty-string coercion canon spent three
    revisions deleting; a red-team confirmed it. Its motivating premise — that enrolling would force
    authors to guard the field they report on — also did not survive measurement (§ How big is the
    problem, actually).
  - *A positional carve-out ("messages are exempt from presence").* Rejected: the survey found no
    statically-checked language splits its presence rule by string position, and Rust actively refutes
    it (a `thiserror` `#[error("…")]` desugars to the same `write!`/`Display` bound as any string). A
    carve-out would also state an exception to Principle 10 rather than satisfying it.
- **Precedent**: C#'s nullable-flow analysis is *not* waived inside an interpolation hole — `CS8602`
  fires on `{c.Length}` for `c: string?` (survey, measured). The reason `{c}` alone does not warn is
  that the target parameter is `string?` — "the requirement isn't there," not "the check is waived."
  Precept's hole target is a present scalar (canon coerces *scalars*), so the requirement **is** there.
  Dhall refuses uniformly: every interpolated expression must be proven `Text`; an `Optional Text` must
  be eliminated first. Precept's own canon already requires a guard to read an optional
  (`primitive-types.md:118`); R1 extends the position that rule already governs to the render step.
- **Tradeoff accepted**: An author who interpolates an optional into a human-facing message must guard
  it or write the fallback conditional — a small ceremony on prose that reads as pure display. This is
  the domain-expert-friction cost, and it is the one residual the owner should weigh (§ The residual
  owner call).
- **Sources consulted**:
  - `docs/language/precept-language-spec.md § 0.1` Principle 10 (totality) and Principle 1 (prevention).
  - `docs/language/primitive-types.md:118` — reading an optional requires a presence guard.
  - `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` — the
    C#/Dhall/Rust findings and the "no language splits by string position" bottom line.
  - *Spec-first check*: grepped `precept-language-spec.md`, `primitive-types.md`, `literal-system.md`,
    `proof-engine.md` for "interpolat" ∧ "presence"/"optional" — canon settles the general rule
    (absence is never silently coerced; reading an optional needs a guard) and is silent only on the
    bare-render sliver `{Opt}`. R1 resolves that sliver *consistently with* the general rule.
- **Reversibility**: `Medium`. Enrolling a check that some corpus files currently sidestep can break
  those files — but the corpus impact is bounded (≤ 11 holes, § How big is the problem) and each has a
  one-line fix (a guard or the fallback idiom). This is the reverse of the tolerant spine's `Hard`
  reversibility, where *relaxing* a check and later re-adding it would break authored precepts.
- **Blast radius**: Docs — `precept-language-spec.md § 3.7`, `literal-system.md`,
  `soundness-and-coverage.md:230`, `proof-engine.md`. Code — the slot-shape change plus the
  obligation-creation sweep (shared with the value-fault arm). Samples — up to 11 unguarded message
  holes need a guard or fallback; both originally-cited motivating samples already compile clean.
  External consumers — none; pre-release.

### Decision 2: The obligation attaches to what a hole reads, at every depth

**Stakes**: high

- **Rationale**: Nothing about a string literal makes the reads *inside* a hole total — `10 / Opt` is
  as undefined on absence inside a message as outside one, and a nested render `{"prefix {Opt}"}` is a
  hole at its own depth. Stating the rule over what a hole *reads* (not what it *names*) and applying
  it recursively is the only formulation that both keeps guards working and reaches nested holes.
- **The wording fix that makes this correct**: "read," not "named." `{Opt is set}` names `Opt` without
  reading it; the walker deliberately skips the `is set` operand (`ProofEngine.cs:340-345`). A rule
  phrased over "every field a hole *names*" would mint a spurious obligation on the guarded field and
  break `is set` everywhere. This is the red-team correction that the original strict draft missed.
- **Alternatives considered and rejected**:
  - *State the rule over named fields.* Rejected — breaks `is set`, as above.
  - *Apply only to the outermost hole ("nothing inside the hole counts").* Rejected — the red-team
    showed `{"prefix {Opt}"}` is a nested render whose inner hole reads `Opt`; a flat "outermost only"
    rule would miss it. The rule must be recursive.
  - *Whole-hole or whole-template tolerance.* Rejected — would make `{10 / Opt}` compile with an absent
    divisor, contradicting the 2026-07-23 value-fault ruling the same holes enroll under.
- **Precedent**: The engine already scopes narrowing per-expression — `GuardInPath` discharges presence
  for the specific reference it dominates, not the whole row (verified: `"{Opt}"` under `when Opt is set`
  → `Presence Proved strategy=GuardInPath`). The `TypedPostfixOp` skip is itself existing precedent that
  the engine distinguishes read from named. Per-hole, recursive scoping is the granularity the engine
  already reasons at.
- **Tradeoff accepted**: The recursive "read vs named" rule is more to *state* than "a message is
  exempt" — the spec must define read-vs-named and the nesting behavior explicitly. Accepted because
  the alternative formulations are unsound or break guards.
- **Sources consulted**:
  - `src/Precept/Pipeline/ProofEngine.cs:340-345` — the `TypedPostfixOp` skip (read ≠ named).
  - `src/Precept/Pipeline/ProofEngine.cs:347-357` — the interpolation walk and its recursion into holes.
  - `docs/compiler/soundness-and-coverage.md:229` — the value-fault row the interior reads enroll under.
  - Live probe: `{Opt is set}` mints nothing; `{if Opt is set then Opt else …}` mints nothing for an
    arg (clean); the same over a field is PRE0116 (BUG-061).
- **Strongest counter-evidence**: none found for whole-hole tolerance after checking the spec's
  interpolation sections, the soundness doc, and the obligation-discharge matrix's message-interpolation
  cells — those cells treat the hole's interior as a real evaluation site with real obligations, which
  actively contradicts whole-hole tolerance.
- **Reversibility**: `Medium` — same as Decision 1.
- **Blast radius**: Same surfaces as Decision 1; no additional.

### Decision 3: Tolerance is not restricted to message positions — refuse uniformly

**Stakes**: high (settled toward refuse-uniformly; one residual open — see § The residual owner call)

This was the load-bearing open question. The evidence settles it toward **refuse uniformly**: the
presence rule does not vary by string position.

- **Rationale**: The message/value divergence at HEAD is a walk-gap (the message slot holds no
  expression and `CollectObligations` never visits `.Message`, `ProofEngine.cs:208`), not a designed
  semantic. There is no reason of Precept's own to keep it: the length machinery makes presence
  load-bearing in value positions and the same machinery will apply to message positions once they are
  walked; the survey found no precedent for a positional split; and one rule across all four positions
  is what the domain expert can hold in their head. Under refuse-uniformly, `"{Opt}"` behaves the same
  in a `because` and in a `set` two lines away.
- **Alternatives considered and rejected**:
  - *(A) Message-only tolerance* — `"{Opt}"` tolerated in `because`/`reject`, refused in `set`.
    Rejected: its only surviving argument was that a `set` RHS becomes stored governed data while a
    message is terminal, so a sentinel is worse in storage. That argument is weak — a sentinel in
    storage is not an invalid configuration unless a rule forbids it (and any such rule still applies),
    and the length machinery already rejects the interesting case (`notempty` + zero-width sentinel).
    And it is *unprecedented*: no statically-checked language in the survey splits by string position;
    Rust refutes it directly. Choosing (A) would be a call *against* the survey, not one it supports.
  - *(B) All `"..."` holes tolerated (the tolerant spine).* Rejected for the three reasons in
    § Why the tolerant spine was refuted.
- **Precedent**: The survey's bottom line, verbatim: *"No statically-checked language in this survey
  splits its presence rule by string position. The message-vs-value distinction exists in the field but
  lives at the library/runtime layer … and Jinja2's split is by operation kind."* Refuse-uniformly is
  the shape Dhall takes (prove `Text` or eliminate the `Optional` first) and the shape Rust's
  `Display` bound enforces including in message macros.
- **Tradeoff accepted**: Refuse-uniformly relaxes nothing and *tightens* message positions, which is
  where the domain-expert-friction cost lands — a human-facing rationale that reads on an optional now
  needs a guard or the fallback conditional. That is the residual the owner should weigh; it is a
  friction cost, not a soundness cost.
- **Sources consulted**:
  - `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` § Bottom
    line and §§ 1 (Rust), 3 (C#), 5 (SQL cautionary), 8 (Dhall).
  - `src/Precept/Pipeline/ProofEngine.cs:208`, `Parser.Expressions.cs:558-564` — the walk-gap.
  - *Spec-first check*: canon's null-coercion trajectory (c4d0abf8 → 9ab60e47 → current) runs toward
    the strict reading, not away from it; `literal-system.md:145`, `primitive-types.md:118`,
    `evaluator.md:228`.
- **Strongest counter-evidence**: Kotlin — a serious null-safe language — exempts the template position
  (`"$text"` renders `null`), silently and mechanically via `Any?.toString()`. *Response*: it is
  silent, which the survey rates weaker than Swift's flagged version, and it is a permissive-pole choice
  Precept's whole null-coercion trajectory has already rejected for its own surface.
- **The residual**: the domain-expert-friction judgment is left to the owner (§ The residual owner
  call). Everything else about this decision is settled.

### Decision 4: How an author renders an optional — the coalescing idiom already exists

**Stakes**: medium (owner-corrected 2026-07-24; no new surface)

The original doc and the design synthesis both held that the only tolerance form would be
author-supplied fallback text, and that this was **new language surface** needing its own owner
consultation. **The owner corrected this on 2026-07-24, and it is verified false.**

- **The mechanism already ships.** `if X is set then X else "fallback"` is a keyword conditional in
  the language, in the corpus at `samples/clinic-appointment-scheduling.precept:62`, and it discharges
  presence. It works **inside an interpolation hole**:

  ```precept
  event Stamp(Note as string optional maxlength 100)
  from Draft on Stamp
      -> set Log = "note: {if Stamp.Note is set then Stamp.Note else "none"}"   # compiles CLEAN, verified 2026-07-24
  ```

- **So there is nothing to invest in.** Under refuse-uniformly, an author who wants to render an
  optional in a message writes the existing conditional. No `??` operator, no `\(x, default:)` syntax,
  no fallback annotation on the hole — none of it is needed. The "new surface / consultation gate"
  framing the earlier draft carried is dropped entirely.
- **The sentinel question is moot.** A language-chosen sentinel only matters if an absent value can
  reach the renderer; under refuse-uniformly it never does. There is no empty-string / em-dash /
  field-name / `null` choice to make. Everything the earlier Decision 4 weighed about sentinels is
  removed as no longer live.
- **Rationale**: reusing the shipped conditional keeps the language surface flat (Principle 5,
  keyword-anchored) and puts the human-facing fallback text where Precept already puts human-facing
  text the language cannot derive — authored by the domain expert (the same category as the mandatory
  `because`, Principle 9).
- **Alternatives considered and rejected**:
  - *A dedicated `??` / hole-default syntax.* Rejected — redundant with the existing conditional, and
    net-new surface for zero added expressiveness. (Swift shipped `\(x, default:)` only because its
    `??` does not type-check for non-`String` optionals; Precept's `if…then…else` has no such limit.)
  - *A language-chosen sentinel.* Rejected — moot under refuse-uniformly, and § Why the tolerant spine
    was refuted showed no sentinel is sound against the length machinery anyway.
- **Precedent**: Kotlin's Elvis operator (`${text ?: "…"}`) and Swift's `\(age, default: "missing")`
  are the field's two shipped "author states the fallback in source" mechanisms; Precept already has
  the equivalent as `if…then…else`, so it matches the precedent without adding surface.
- **Tradeoff accepted**: the conditional is more verbose than a `??` operator would be. Accepted —
  verbosity in exchange for no new surface and one obvious way to do it.
- **Dependency**: BUG-061 must close for this to cover **field** reads inline (it works for args
  today; a field operand is currently PRE0116). Until then, a field author falls back to an outer
  `when … is set` guard.

### Decision 5: Where the enrollment wiring lives

**Stakes**: medium

- **Rationale**: Under refuse-uniformly there is no "total-on-absence render" classification to place
  (the tolerant spine's Decision 5) — a hole is an ordinary read, so the work is (1) reshape
  `BecauseClauseSlot.Message` (and the `reject` string) from `string` to an expression the type checker
  and proof engine already understand, and (2) let `CollectObligations` visit `.Message`, reusing the
  existing `WalkExpression` interpolation path (`ProofEngine.cs:347-357`) — **after** BUG-057 closes
  that path's arg-ref hole. No new catalog entry for a render-tolerance classification is needed.
- **Tradeoff accepted**: the slot-shape change touches every construct that carries a message
  (`TypedRule`, `TypedEnsure`, reject rows) and the language server's raw-string message handling
  (`RichHoverFactory.cs:2970-2980`). Accepted — it is the same change the value-fault arm needs, so
  it is paid once for both arms.
- **Precedent**: value positions already route interpolation holes through `WalkExpression`; this
  extends the same path to message positions rather than inventing a second one.
- *(This decision is implementation-shaped and does not block the design's semantic conclusions; it is
  named so the plan/execute stages inherit it. It still needs the four-leg treatment filled out at
  plan time — precedent and reversibility above are sketches.)*

---

## The residual owner call

The original doc framed the owner's decision as "refuse now vs. invest in fallback syntax later."
**That fork mostly dissolves**: the fallback idiom already exists (Decision 4), so there is nothing to
invest in and no new surface to gate. The evidence settles soundness and precedent toward refuse
uniformly (Decisions 1–3). One genuine call remains, and it is a judgment, not a soundness question:

**Refuse-uniformly imposes a guard-or-conditional ceremony on human-facing prose.** A `because`
rationale that reads on an optional — arguably the most natural thing a failure message does — now
requires the author to guard the field or write `if Opt is set then Opt else "…"`. The owner said he
wanted to explore relaxing exactly this friction. The evidence de-risks refusal hard, and this is
presented neutrally so the owner can weigh the friction against the uniformity:

- **Demonstrated corpus demand is ≤ 11 of 808 interpolation holes.** 83 name an optional; 71 already
  carry a discharging `is set` guard; 11 are unguarded (§ How big is the problem), and some of those 11
  are BUG-060's blindness, not authoring need.
- **Both samples originally cited as motivating do not motivate.** `insurance-claim.precept:96`
  interpolates `ClaimAmount`, which `:22` declares `money in 'USD' default '0.00 USD'` — required, not
  optional, so it mints no presence obligation under any reading. `customer-profile.precept:27` already
  carries `when PreferredContactMethod is set`, which discharges — verified (guarded → `Proved
  strategy=GuardInPath`; unguarded → `Unresolved` + PRE0116). The design's two live motivating examples
  both cost zero extra ceremony.

So the owner's residual is: accept the small, bounded prose-ceremony cost for one uniform rule (the
evidence's recommendation), or ask for a relaxation on message positions — which would be the
positional split the survey found unprecedented, re-opening Decision 3's rejected alternative (A) as a
deliberate call against the survey. Framed neutrally; the owner's to make.

## How big is the problem, actually

The premise the design started from was the owner's: enrolling presence at a message hole *"would force
authors to guard the field they are reporting on."* Measurement weakened it to near-nothing.

- **`samples/insurance-claim.precept:96`** — `-> reject "…(you submitted {ClaimAmount})"`. `ClaimAmount`
  is `:22` `field ClaimAmount as money in 'USD' default '0.00 USD'` — **required, with a default**. Not
  optional; mints nothing under any reading. Miscited.
- **`samples/customer-profile.precept:27`** — interpolates optional `PreferredContactMethod`, but
  already carries `when PreferredContactMethod is set`, and that guard **discharges**. Verified: guarded
  → `Proved strategy=GuardInPath`; identical rule without the `when` → `Unresolved` + PRE0116. Zero
  extra guards.

**The corpus measurement.** Across `samples/`: 808 interpolation holes; 83 name an `optional` field;
**none of the 83 sit in a value position** (the corpus compiles clean and an unguarded optional in a
value-position hole is a hard error at HEAD, so there cannot be one); 71 of 83 already carry an
`is set` guard the engine discharges from; **11 are unguarded** — `Test.precept:51`,
`academic-course-registration.precept:172`, `calibration-management.precept:173`,
`hotel-reservation-management.precept:132`, `incoming-material-inspection.precept:78`,
`library-hold-request.precept:111`, `library-inter-library-loan.precept:275`,
`maintenance-work-order.precept:98`, `non-profit-membership-renewal.precept:155` and `:163`,
`prior-auth-appeal.precept:349`. Several of the 11 sit under a state-scoped `ensure X is set` that
guarantees presence but that the proof engine cannot use — **BUG-060**. Those authors did guard; the
engine cannot see it.

**What this means.** Refuse-uniformly's demonstrated ceremony cost is ≤ 11 holes out of 808, some
fraction of which is a compiler gap rather than an authoring need — which is why **BUG-060 must close
before enrollment**, or a guard-workaround gets adopted for a compiler weakness and calcifies.

---

## Sections still to write

Required by the design skill for a language-surface change and **not yet written** (this stays Draft):

- **Philosophy Alignment** — the eleven-principle matrix. Principles 1, 3, 4, 10, 11 all have content;
  the strict reading aligns with all five (it prevents rather than tolerates), but the matrix should be
  written out.
- **Language Design Grounding** — largely satisfied by the commissioned survey; needs the section
  written citing it.
- **Audience and Teachability** — the worked example (`"{Opt}"` → PRE0116 → author writes
  `{if Opt is set then Opt else "…"}`), the diagnostic wording, and the 10-minute path.
- **Semantic Rules** — R1/R2/R3 reduced to typing-rule notation, plus the soundness-preservation claim
  naming Principles 10 and 11.
- **Architecture Grounding** — Decision 5 filled to four legs; cross-component propagation.
- **Inventory**, **Acceptance criteria**, **Doc-update enumeration**, **Falsifiers**.

## Open questions

1. The residual owner call (§ The residual owner call) — accept the bounded prose-ceremony cost of
   refuse-uniformly, or ask for a message-position relaxation (the survey-unprecedented positional
   split). This is the one genuine open decision; it does not block the soundness conclusions.
2. The correction to `soundness-and-coverage.md:229` — that the value-fault fix is *not* the same shape
   as the default-expression rows (the message is not an expression), and that the arm is unbuilt — is
   true independent of this design and could be applied now as a standalone doc fix rather than waiting
   on this promotion.
3. Six live defects were found while probing (all filed, none absorbed): **BUG-056** (declaration-position
   typed-constant hole never walked), **BUG-057** (value walker under-mints optional arg refs),
   **BUG-058** (PRE0051 dead — collection-in-interpolation unenforced), **BUG-059** (string-hole length
   floor contradicts its own comment), **BUG-060** (state-scoped `ensure` doesn't discharge),
   **BUG-061** (conditional narrows for args but not fields). BUG-057, BUG-060, BUG-061 are hard
   sequencing dependencies (§ Preconditions and dependencies).

## Review record

- **2026-07-23** — Draft written. Boundary cases probed against HEAD. Two brief-supplied cases (a
  ternary arm, "deleted-canon as authority") were struck on evidence. The soundness doc's "same fix
  shape" characterisation was corrected.
- **2026-07-23, second pass** — commissioned comparator survey + source-archaeology returned. The
  survey found the positional justification unprecedented; the archaeology corrected "messages are
  never parsed" (they are parsed then flattened to `{}`) and surfaced BUG-059. Decision 3 was opened
  into a two-sided fork.
- **2026-07-23, third pass** — the neutrally-framed sentinel agent returned three corrections, two
  verified by harness: `insurance-claim.precept:96` interpolates a required field; `customer-profile.precept:27`
  is already guarded; a state-scoped `ensure` does not discharge (BUG-060). The recommendation then on
  file — "no language-chosen sentinel; author-supplied fallback is new surface" — was recorded as
  exploratory.
- **2026-07-24, turnaround** — the tolerant spine was **refuted three ways** and the recommendation
  reversed to **refuse uniformly**:
  - *Length machinery (verified live).* `set Marker = "{Opt}"` with both `Opt` and `Marker` at
    `minlength 5 maxlength 5` proves length containment on `Opt`'s declared floor of 5; the presence
    obligation is the *only* diagnostic holding the program (harness, 2026-07-24). Presence is
    load-bearing; "unnecessary by construction" is false; no single sentinel is simultaneously ≥ every
    field's minlength and ≤ every field's maxlength, so no sentinel is sound.
  - *Three-lens panel.* Tolerance re-permits the silent empty-string coercion canon deleted across
    c4d0abf8 → 9ab60e47 → current (null literal removed, absence first-class, reads require a guard).
    Disqualified unanimously.
  - *Red-team confirmed*, and corrected the strict framing: the rule is stated over what a hole
    **reads**, not what it names (`{Opt is set}` names but does not read — `ProofEngine.cs:340-345`);
    it is **recursive** at every depth (`{"prefix {Opt}"}` is a nested hole); the value-fault arm it
    leans on is **unbuilt** (`62479cae` docs-only), not accomplished.
  - *Owner correction on the fallback (verified live).* The coalescing idiom `if X is set then X else
    "…"` already exists (`clinic-appointment-scheduling.precept:62`), discharges presence, and works
    **inside** an interpolation hole (harness, 2026-07-24). Decision 4 dropped the "new surface /
    consultation gate" framing; the sentinel question is moot under refuse-uniformly.
  - *Two synthesis corrections folded.* The value-fault ruling did **not** make the two positions
    symmetric — both arms are equally unbuilt. The business-domain String Coercion rows are **new
    determinism (P3) surface**, each format a decision needing rationale — an adjacent gap, not a
    settled prerequisite.
  - *New verified probes added* (harness, 2026-07-24): the length-machinery refutation; the optional
    event-arg hole minting nothing in a value position (BUG-057); the conditional fallback working
    inside a hole for an arg; the field-vs-arg narrowing asymmetry (BUG-061).
  - *Bugs filed/referenced*: BUG-056 … BUG-061; BUG-057/060/061 are hard sequencing dependencies.
  - **Status stays Draft.** The one residual is the owner's friction judgment (§ The residual owner
    call); Sections still to write remain, so this does not lock.
