---
status: Locked 2026-07-24
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
> content is the strict reading. **The owner ruled refuse-uniformly on 2026-07-24 and accepted the
> domain-expert-friction cost** (§ The residual owner call, § Review record); the document is now
> **Locked**.

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
  the domain-expert-friction cost; the owner weighed it against the uniformity and **accepted it on
  2026-07-24** (§ The residual owner call, § Review record).
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

**Stakes**: high (settled — refuse uniformly; owner ruling 2026-07-24)

This was the load-bearing open question. The evidence settled it toward **refuse uniformly** — the
presence rule does not vary by string position — and the owner ruled refuse-uniformly on 2026-07-24,
accepting the friction cost (§ The residual owner call). It is fully settled; no residual remains.

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
  needs a guard or the fallback conditional. The owner weighed this against the uniformity and **accepted
  the friction cost on 2026-07-24**, with the de-risking evidence on record (demonstrated demand ≤ 11 of
  808 holes; both originally-cited motivating samples do not motivate — § How big is the problem). It is
  a friction cost, not a soundness cost.
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
- **Owner ruling (2026-07-24)**: refuse uniformly; the domain-expert-friction cost accepted (§ The
  residual owner call, § Review record). Nothing about this decision remains open.

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

### Decision 5: Where the enrollment wiring lives — reuse the value-hole walker at the obligation collector

**Stakes**: low (pipeline-internal placement; no language surface, no author-visible behavior beyond
what Decisions 1–3 already fix)

- **Rationale**: Under refuse-uniformly there is no "total-on-absence render" classification to place
  (the tolerant spine's Decision 5) — a hole is an ordinary read, so the work is (1) reshape
  `BecauseClauseSlot.Message` (and the `reject` string) from `string` to an expression the type checker
  and proof engine already understand, and (2) let `CollectObligations` (`ProofEngine.cs:208`) visit
  `.Message`, reusing the existing `WalkExpression` interpolation path (`ProofEngine.cs:347-357`) —
  **after** BUG-057 closes that path's arg-ref hole. The obligation collector is the correct placement
  because it is already where every value-position interpolation hole is walked; message holes are the
  one construct family it skips, so the fix is to remove that skip, not to add a parallel walk. No new
  catalog entry for a render-tolerance classification is needed.
- **Alternatives considered and rejected**:
  - *A second, message-specific walker.* Rejected — it would duplicate the value-hole walk and let the
    two drift (exactly the parallel-logic smell CLAUDE.md's catalog rules forbid). The read-vs-named and
    recursion behavior (Decisions 1–2) must be identical to value positions, which is guaranteed only by
    reusing one walker.
  - *Enroll at the type checker instead of the proof engine.* Rejected — presence is a proof obligation
    discharged by the proof engine's strategies (`GuardInPath`, etc.); the type checker does not carry
    the flow-sensitive narrowing that discharges a guarded read. Enrolling there would either double-mint
    or lose the guard discharge.
- **Precedent**: value positions already route interpolation holes through `WalkExpression`
  (`ProofEngine.cs:347-357`); this extends the same path to message positions rather than inventing a
  second one. The `TransitionRow` success-subtype guard at `ProofEngine.cs:213-225` is the existing
  pattern that decides *which* rows the collector walks — lifting it to reach reject-arm messages is a
  local extension of machinery already in place, not new architecture.
- **Tradeoff accepted**: the slot-shape change touches every construct that carries a message
  (`TypedRule`, `TypedEnsure`, reject rows) and the language server's raw-string message handling
  (`RichHoverFactory.cs:2970-2980`). Accepted — it is the same change the value-fault arm needs, so it
  is paid once for both arms.
- **Reversibility**: `Low`-cost. The placement is internal wiring behind the semantic conclusions of
  Decisions 1–3; if a later stage finds a better collector site, moving the walk does not change any
  author-facing rule or any diagnostic. Nothing external depends on where inside the pipeline the
  obligation is minted.

---

## The residual owner call — ruled 2026-07-24

The original doc framed the owner's decision as "refuse now vs. invest in fallback syntax later."
**That fork dissolved**: the fallback idiom already exists (Decision 4), so there is nothing to invest
in and no new surface to gate. The evidence settled soundness and precedent toward refuse uniformly
(Decisions 1–3). One genuine call remained — a judgment, not a soundness question — and **the owner has
now made it.**

> **Owner ruling (2026-07-24): "refuse uniformly, close the friction question."** The
> domain-expert-friction cost is **accepted**; refuse-uniformly is the settled semantics across all four
> positions. No message-position relaxation.

The call the owner weighed: **refuse-uniformly imposes a guard-or-conditional ceremony on human-facing
prose.** A `because` rationale that reads on an optional — arguably the most natural thing a failure
message does — now requires the author to guard the field or write `if Opt is set then Opt else "…"`.
The owner had wanted to explore relaxing exactly this friction. The evidence (kept below, unchanged,
because it grounds the ruling) de-risked refusal hard:

- **Demonstrated corpus demand is ≤ 11 of 808 interpolation holes.** 83 name an optional; 71 already
  carry a discharging `is set` guard; 11 are unguarded (§ How big is the problem), and some of those 11
  are BUG-060's blindness, not authoring need.
- **Both samples originally cited as motivating do not motivate.** `insurance-claim.precept:96`
  interpolates `ClaimAmount`, which `:22` declares `money in 'USD' default '0.00 USD'` — required, not
  optional, so it mints no presence obligation under any reading. `customer-profile.precept:27` already
  carries `when PreferredContactMethod is set`, which discharges — verified (guarded → `Proved
  strategy=GuardInPath`; unguarded → `Unresolved` + PRE0116). The design's two live motivating examples
  both cost zero extra ceremony.

**Outcome.** The owner accepted the small, bounded prose-ceremony cost in exchange for one uniform rule
— the evidence's recommendation. The rejected alternative (a message-position relaxation) would have
been the positional split the survey found unprecedented, re-opening Decision 3's rejected alternative
(A) as a deliberate call against the survey; the owner did not take it. The friction cost now lives
honestly in the Philosophy Alignment companion-commitments paragraph (domain-expert-primary-author), not
as an open question.

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

## Philosophy Alignment

The eleven design principles (`precept-language-spec.md § 0.1`), every one a row. Refuse-uniformly is a
*strengthening* of the surface, so most rows are served rather than merely not-violated; the few genuine
N/A rows carry a one-line reason.

| # | Principle | How this design stands |
|---|---|---|
| 1 | Prevention, not detection | **Served maximally.** Refusing an unprovable optional read at a hole makes "a message renders an absent value" structurally impossible before any change is committed — not a runtime check, not a lint. This is the design's core: prevention extended to the one read position (`{Opt}`) canon had not yet reached. |
| 2 | One file, complete rules | **Served.** The guard and the fallback (`if Opt is set then Opt else "…"`) both live in the same `.precept` definition as the message they defend; no rendering config, no external fallback table. Nothing scatters. |
| 3 | Deterministic semantics | **Served, with an adjacent gap named.** Presence enrollment adds no nondeterminism; scalar render coercion is invariant-culture (String Coercion Table). The *separate* question of how a present business-domain value (`money`, `quantity`, `choice`) renders to display text is unfinished P3 surface — flagged in § Adjacent gaps, explicitly out of scope for the presence arm and not settled here. |
| 4 | Full inspectability | **Served by elimination.** Because an absent optional never reaches the renderer, there is no ambiguous sentinel that would hide, at the moment of explanation, that the value a message reports was never there. A message that silently rendered empty would conceal exactly that; refusal keeps it visible. |
| 5 | Keyword-anchored readability | **Served.** The fallback reuses the shipped `if … then … else …` keyword conditional (Decision 4). No new operator, no symbol-heavy `??`/`\(x, default:)` surface; the language stays flat. |
| 6 | Explicit domain meaning over primitive convenience | **N/A to the presence rule.** Presence enrollment is type-agnostic (any optional, any type). The domain-type *render format* question is P3 surface (row 3), not this design. |
| 7 | Compile-time-first static checking | **Served.** Presence is proven at compile time; an unprovable hole is rejected with the weakest precondition named, never compiled in the hope a runtime check catches an empty render. No guessing. |
| 8 | Approximation honesty | **N/A.** This design introduces no approximate value or operation; render coercion of a present scalar is exact. |
| 9 | Mandatory rationale | **Served / companion.** Where the language cannot derive the human-facing text — the fallback string in `else "…"` — the domain expert authors it, exactly as they author the mandatory `because`. Fallback text is the same category as a `because`: human-facing meaning the compiler cannot supply. |
| 10 | Totality | **Served maximally.** A presence obligation exists precisely because reading an absent optional leaves an operation with no result. Enrolling the render step means every hole that compiles reads a present value, so the render evaluates to a result for every compiling program — totality by construction rather than by carving an exception into it. |
| 11 | Static completeness | **Served.** A message hole that compiles without diagnostics cannot fault at render on absence; the fault path (render of an absent optional) is made unreachable at compile time, which is the compiler-to-evaluator bridge P11 names. |

**Companion commitments.** Two commitments outside the numbered eleven bear on this design and are
stated honestly:

- **Stateless precepts are first-class.** Nothing here depends on lifecycle. A `rule … because "…{Opt}…"`
  in a data-only, stateless precept enrolls presence identically to one inside a `from … on …` row; the
  read rule is over the hole, not over any state. The design does not privilege stateful precepts.
- **The domain expert is the primary author — and this is where the accepted cost lands.** Refuse-uniformly
  imposes a guard-or-conditional ceremony on message prose that reads as pure display, and the primary
  author paying it is the domain expert, not a developer. This is a real friction against the
  domain-expert-primary-author commitment. It is **not** waved away: the owner weighed it and **accepted
  it on 2026-07-24** (§ The residual owner call), on evidence that demonstrated demand is ≤ 11 of 808
  corpus holes and both originally-cited motivating samples cost zero extra ceremony. The cost is stated,
  bounded, and owned — not hidden.

## Semantic Rules

The three rules stated as presence-enrollment judgments. Read `Γ ⊢ e ▷ P` as "in context Γ, elaborating
expression `e` enrolls the presence-obligation set `P`"; `reads(e)` is the set of optional fields `e`
consumes a value from (as opposed to merely names). A `Presence(f)` obligation is discharged by the proof
engine's existing strategies or, unresolved, surfaces as PRE0116.

**R1 — a hole is a read.** Coercing a value to display text is a read; a hole enrolls presence for every
optional it reads, identical to a value-position read, regardless of hole shape or string position.

```
        e reads optional field f          (render coerces e's value)
    ────────────────────────────────────────────────────────────────
        Γ ⊢ {e} ▷ Presence(f)             in message and value positions alike
```

**R2 — read, not named; recursive at every depth.** The obligation attaches to `reads(e)`, not to every
field `e` names, and R1 is applied at each nesting boundary.

```
    reads(f is set) = ∅                    (is set names f; operand skipped — ProofEngine.cs:340-345)
    ────────────────────────────
        Γ ⊢ {f is set} ▷ ∅

        Γ ⊢ {e_inner} ▷ P                  (a nested render is a hole at its own boundary)
    ─────────────────────────────────────
        Γ ⊢ {"… {e_inner} …"} ▷ P
```

**R3 — typed-constant holes enroll for two reasons.** A `'…'` typed-constant hole reads its operand *and*
feeds a content validator that is not defined on absence, so it enrolls presence and is excluded from any
would-be render tolerance.

```
    e reads optional f,   '…{e}…' validated against context type T,   validate_T undefined on absence
    ─────────────────────────────────────────────────────────────────────────────────────────────────
        Γ ⊢ '…{e}…' ▷ Presence(f)
```

**Soundness preservation (Principles 10, 11).** Let a program `P` compile with no diagnostics. By R1–R3,
every message-hole read of an optional `f` carries a discharged `Presence(f)`. A discharged presence
obligation means `f` is proven present on every path reaching the hole. Therefore the render step never
faces an absent value: it always has characters to produce (**P10 totality** — the render evaluates to a
result), and the evaluator's "render of an absent optional" fault path is unreachable in any compiling
program (**P11 static completeness** — the compiler has made that evaluator error path unreachable). The
enrollment does not merely detect the fault; it removes the state in which it could occur.

## Language Design Grounding

Grounded in the commissioned comparator survey,
`research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` (13 systems,
verbatim excerpts).

- **Rust — interpolation is a read, message positions are not special.** `{}` requires `Display`;
  `Option<T>` implements `Debug`, not `Display`, so `{opt}` does not compile, with a fix-it pointing at
  `{:?}`. Crucially the message layer inherits this unchanged: `panic!`, `assert!`, and `thiserror`'s
  `#[error("{var}")]` all desugar to the same `write!`/`Display` bound as any other string (survey § 1).
  This is the field's cleanest data point that "a message is not exempt," and it is the external
  comparator this design leans on.
- **Dhall — uniform refusal, the strictest comparator.** Every interpolated expression must be proven
  `Text`; an `Optional Text` must be eliminated (`merge`/`Optional/fold`) before it can enter a hole. No
  message position, no escape hatch (survey § 7, judgment block). Refuse-uniformly is precisely Dhall's
  shape.
- **The survey's bottom line, verbatim:** *"No statically-checked language in this survey splits its
  presence rule by string position."* A per-position rule would be doing something the surveyed field does
  not do; refuse-uniformly is the shape that has precedent (Rust, Dhall), and Decision 3 leans on that
  precedent honestly rather than on an invented positional cut.
- **Kotlin — the strongest counter, and why it does not move the decision.** Kotlin is a serious
  null-safe language that *does* exempt the template position (`"$text"` renders `null`). But it does so
  **silently and mechanically** (via `Any?.toString()` on the nullable receiver), which the survey rates
  weaker than Swift's flagged version — and silent tolerance of absence in a string is exactly the
  permissive-pole choice Precept's own null-coercion trajectory has already rejected for its own surface.
  A serious counter, answered, not ignored.
- **Precept-internal grounding — the null-coercion trajectory.** The internal precedent runs *toward* the
  strict reading: `c4d0abf8` (original broad "`null` coerces to empty string in interpolation") →
  `9ab60e47` (reversed: "`null` is not coerced to empty string") → current canon (the `null` literal
  removed, `literal-system.md:145`; absence made a first-class runtime value, `evaluator.md:228`; reading
  an optional made to require a guard, `primitive-types.md:118`). Refuse-uniformly continues that arc;
  tolerant rendering would have walked it back.

## Audience and Teachability

**Worked example (plausible domain).** A benefits-eligibility precept has an optional appeal deadline and
a domain expert writing the refusal rationale:

```precept
field AppealDeadline as date optional editable
rule DaysElapsed <= AppealWindow
    because "appeal is closed; the deadline was {AppealDeadline}"    # PRE0116 on AppealDeadline
```

The hole reads an optional, so under R1 it enrolls presence and — unguarded — is refused. The author has
two one-line fixes, both already in the language:

```precept
    because "appeal is closed; the deadline was {if AppealDeadline is set then AppealDeadline else "not recorded"}"
```

or, guarding the whole rule where the field is known present:

```precept
rule DaysElapsed <= AppealWindow when AppealDeadline is set
    because "appeal is closed; the deadline was {AppealDeadline}"
```

**The diagnostic the author sees** is PRE0116, whose text and recovery hints already point exactly at
these fixes (verbatim, `Diagnostics.cs:888-899`):

> `Cannot prove that 'AppealDeadline' is present — guard with 'when AppealDeadline is set', initialize it
> earlier, or make it required`

Recovery steps: *"Guard the usage with `when AppealDeadline is set`."* / *"Assign the field before this
path is reachable."* / *"Remove `optional` if the field should always be present."* The one addition this
design implies is that the recovery hint should also name the coalescing conditional as the inline option
for a display position — see § Doc-update enumeration (diagnostic-system.md).

**≤ 10-minute teaching path** (2–5 artifacts, in order):

1. `docs/language/primitive-types.md:118` — reading an optional requires a presence guard (the general
   rule this design extends).
2. `samples/customer-profile.precept:27` — a live rule interpolating an optional, already guarded with
   `when … is set` (the guard form).
3. `samples/clinic-appointment-scheduling.precept:62` — the `if X is set then X else "…"` coalescing
   idiom (the inline-fallback form).
4. This § Audience worked example — the two fixes side by side.

A domain expert who reads those four reaches "a message that names an optional must guard it or supply a
fallback, using forms the language already has" in well under ten minutes.

## Architecture Grounding

- **Layer placement.** This is proof-pipeline walker/obligation wiring, not new surface. Two touch
  points: (1) the message slot stops being a flat `string` and becomes an expression the type checker and
  proof engine already understand (`SlotValue.cs:100`, `Parser.Expressions.cs:558-564`); (2) the
  obligation collector visits `.Message`, reusing the value-hole walker (`ProofEngine.cs:208`,
  `:347-357`). No new pipeline stage.
- **Catalog visibility of read-vs-named.** The read/named distinction (R2) is already catalog-visible: it
  is the Presence operator family (`is set` / `is not set`), whose operand the walker skips
  (`ProofEngine.cs:340-345`). The design adds no new catalog entry — it relies on the existing operator
  family's meaning, which is why "reuse the walker" is sound rather than a parallel definition.
- **Cross-component propagation.**
  - *Runtime* — the render path is where the guarantee is *cashed*: because no absent optional reaches
    it, the evaluator's render never handles absence. The evaluator is currently a stub
    (`Evaluator.cs:45`), so there is no behavior to change today; the design constrains what the render
    path must satisfy when built (it may assume presence). Not a change to make now, but a stated
    contract on the runtime.
  - *Tooling / Language server* — the slot-shape change touches the LS's raw-string message handling
    (`RichHoverFactory.cs:2970-2980`), which today unwraps a `TypedLiteral` and prints it raw; it must
    handle a message that is now an expression tree with holes. Propagation required.
  - *MCP* — **None.** No MCP tool surface changes; the compile tool already returns whatever diagnostics
    the pipeline emits, so PRE0116 at a message hole flows through unchanged.
- **Breaking changes — none.** Precept is pre-release; nothing is shipped to external authors. The only
  affected precepts are in-tree samples (≤ 11 holes, each a one-line fix), fixed in the same sweep.
- **External comparator (excerpt).** Rust's `Display`-bound-in-message-macros is the cleanest external
  precedent for the layer placement — the message layer does *not* get its own relaxed typing; it reduces
  to the same bound:

  > `#[error("{var}")]` ⟶ `write!("{}", self.var)` — `docs.rs/thiserror`

  Precept's equivalent: a message hole reduces to the same presence obligation as any value hole, rather
  than routing through a separate message-only path.

## Inventory of what will be built

File-level, all reusing existing machinery — **no new obligation kinds**:

- **The `.Message` slot stops flattening.** `BecauseClauseSlot.Message` (`SlotValue.cs:100`) changes from
  `string` to an expression (the interpolated-string form the type checker already produces), and
  `Parser.Expressions.cs:558-564` stops discarding each hole to the literal `{}`. The `reject` string
  (`SlotValue.cs:125`) is reshaped the same way. Same change the value-fault arm needs — paid once.
- **`CollectObligations` visits `.Message`.** `ProofEngine.cs:208` gains a walk over the message
  expression on `TypedRule`/`TypedEnsure` (and the reject-arm message), reusing the existing
  `WalkExpression` interpolation path (`ProofEngine.cs:347-357`).
- **The reject-arm success-subtype guard is lifted.** `CollectObligations` today walks only
  `TypedTransitionRowSuccess` rows (`ProofEngine.cs:213-225`); reject-arm messages live outside that
  subtype, so the guard must be widened to reach them (or the message walk hoisted above it) for
  `-> reject "…{Opt}…"` to enroll.
- **`includeOptionalArgRefs` is forced on for message holes.** The value walker passes this flag through
  as `false` for a plain `TypedInterpolatedString` (`ProofEngine.cs:347-357`), which under-mints optional
  event-argument reads — **BUG-057**. Message-hole walking must force it `true` (as
  `InterpolatedTypedConstant` already does), and BUG-057 must close so value positions mint arg refs too.
- **No new catalog entry, no new diagnostic code.** PRE0116 already exists and already carries the right
  wording; the read/named distinction is the existing Presence operator family.

## Acceptance criteria

Test-shaped; each is a compile assertion against the built sweep:

1. `rule … because "…{Opt}…"` with `Opt` optional and **unguarded** → **PRE0116** on `Opt`.
2. The same rule with `when Opt is set` → **clean** (guard discharges, `strategy=GuardInPath`).
3. The same rule rendering `{if Opt is set then Opt else "…"}` → **clean** (conditional discharges; once
   BUG-061 closes, for a field operand too).
4. `because "{Opt is set}"` → **clean** — named, not read (R2; the `is set` operand is skipped).
5. Nested render `because "{"prefix {Opt}"}"` → **PRE0116** on `Opt` (R2 recursion; the inner hole is a
   hole at its own depth).
6. `-> reject "…{Opt}…"` unguarded → **PRE0116** (parity with `because`; refuse uniformly across
   positions).
7. Typed-constant hole `set Window = '{Days} hours'` with `Days` optional → **PRE0116**, unchanged from
   HEAD (R3; regression guard that the reshape did not relax typed constants).
8. A required-field hole (`{Amount}` with `Amount` non-optional) → **clean** (no presence obligation;
   guard that enrollment did not over-mint).

## Doc-update enumeration

Per the CLAUDE.md routing table, updated at promotion (`/promote`), not now:

- **`docs/compiler/soundness-and-coverage.md:230`** — the presence-arm row: restate from "deferred —
  owner-fork, still open" to the owner ruling (refuse uniformly, 2026-07-24) and mark the complete
  trigger as the obligation-creation sweep, mirroring the value-fault row :229. (The row's stale claim
  that the fix "would force authors to guard the field they are reporting on" is corrected by § How big
  is the problem.)
- **`docs/compiler/literal-system.md` § String Coercion Table** — the "what does absence render as"
  question dissolves for the presence arm (absence never reaches the renderer); the table gains no
  absence row. Separately, name the business-domain / `choice` omission as new P3 determinism surface
  (§ Adjacent gaps) so the editor does not treat it as a mechanical fill-in.
- **`docs/language/precept-language-spec.md` § interpolation (`:1553` area)** — state that an
  interpolation hole is a **read**, that reading an optional at a hole enrolls presence identically to any
  value position, and that the `is set` operand is named-not-read.
- **`docs/compiler/diagnostic-system.md`** — only if PRE0116's recovery hint is extended to name the
  `if … then … else …` coalescing conditional as the inline-fallback option for a display position
  (§ Audience). If the hint is left as-is, no change here.

## Falsifiers

Post-ship observations that would force a redesign (2–5), stated so a later audit can check them:

1. **Corpus friction exceeds the bound.** If, *after* BUG-057/060/061 close, more than a handful of real
   corpus message holes still need a bare `when` guard that authors report as unnatural ceremony, the
   friction cost the owner accepted was mis-estimated and message-position handling should be revisited.
2. **The fallback conditional proves too verbose at scale.** If across a meaningful number of real holes
   (say 20+) authors consistently reach for the `if … then … else …` fallback and find it too heavy, the
   "no `??`, reuse the conditional" call (Decision 4) is falsified and a nil-coalescing operator warrants
   its own `/design`.
3. **A domain needs to render an optional with no meaningful fallback.** If a real domain surfaces where a
   message must report an optional, no fallback text is meaningful, and the guard is pure ceremony that
   changes nothing, the uniform rule is imposing cost for no soundness gain and message-position
   relaxation (the rejected alternative A) deserves re-examination.
4. **Business-domain coercion forces a sentinel that reintroduces silent absence.** If resolving the
   adjacent P3 coercion gap (§ Adjacent gaps) ends up requiring a sentinel that can stand in for an absent
   domain value, R3's "excluded from render tolerance" boundary is under pressure and must be re-argued.

## Downstream and sequencing — no open design questions

The residual design question (§ The residual owner call) is **ruled**; no open design questions remain.
What is left is downstream execution and standalone doc hygiene, none of it blocking the lock:

1. **The `soundness-and-coverage.md:229` correction is a standalone doc fix.** The observation that the
   value-fault fix is *not* the same shape as the default-expression rows (the message is not an
   expression until the slot is reshaped), and that the value-fault arm is unbuilt, is true independent of
   this design and may be applied now rather than waiting on this promotion.
2. **Six live defects, all filed, none absorbed:** **BUG-056** (declaration-position typed-constant hole
   never walked), **BUG-057** (value walker under-mints optional arg refs), **BUG-058** (PRE0051 dead —
   collection-in-interpolation unenforced), **BUG-059** (string-hole length floor contradicts its own
   comment), **BUG-060** (state-scoped `ensure` doesn't discharge), **BUG-061** (conditional narrows for
   args but not fields). **BUG-057, BUG-060, BUG-061 are hard sequencing dependencies** that must close
   before enrollment (§ Preconditions and dependencies) — implementation ordering for `/plan` → `/execute`,
   not design-blocking.

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
  - **Status stayed Draft** pending the owner's friction judgment and the still-to-write gate sections.
- **2026-07-24, owner ruling + lock.** The owner ruled **"refuse uniformly, close the friction
  question."** The single residual — the domain-expert-friction judgment — is **closed in favor of
  refuse-uniformly, friction accepted**, on the de-risking evidence on record (demonstrated demand
  ≤ 11 of 808 holes; both originally-cited motivating samples do not motivate). Decision 3 and § The
  residual owner call were rewritten from open to ruled; the accepted cost now lives in the Philosophy
  Alignment companion-commitments paragraph (domain-expert-primary-author). The gate sections the
  `/design` skill requires for a language-surface change were completed: **Philosophy Alignment** (full
  eleven-principle matrix + companion commitments), **Semantic Rules** (R1/R2/R3 as enrollment judgments
  + the P10/P11 soundness-preservation claim), **Language Design Grounding** (Rust/Dhall/Kotlin from the
  survey + the null-coercion trajectory), **Audience and Teachability** (worked example, verbatim PRE0116
  wording, ≤ 10-minute path), **Architecture Grounding** (walker placement, cross-component propagation,
  Rust comparator), **Inventory**, **Acceptance criteria**, **Doc-update enumeration**, and
  **Falsifiers**. Decision 5 was promoted from an exploratory sketch to a **low-stakes decision with all
  four legs** (reuse the value-hole walker at the obligation collector; no exploratory decisions remain).
  No open design questions remain; the BUG-057/060/061 dependencies are implementation ordering, not
  design blockers. **Status advanced to Locked 2026-07-24.**
