---
status: Draft — 2026-07-23
phase-target: TBD (obligation-creation sweep; same sweep as the already-ruled value-fault arm)
comparable-systems-research-status: strong — grounded in `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` (13 systems, verbatim excerpts, stable identifiers)
sources-consulted:
  - `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md` — the comparator survey commissioned for this design; 13 systems, and it contradicts the scope this design started from
  - `research/language/expressiveness/expression-language-audit.md § L12` — the nearest in-tree prior: nullable-in-string treated as a type-checker rejection
  - `docs/language/precept-language-spec.md § 0.1` — the eleven principles; Principle 9's generated-rationale clause and Principle 10's totality clause are both load-bearing here
  - `docs/language/precept-language-spec.md § 2.4` — "A constraint modifier … desugars to the equivalent `rule` with a generated rationale"
  - `docs/language/precept-language-spec.md:1553` — "Each `{expr}` inside `\"...\"` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation."
  - `docs/language/precept-language-spec.md:1557` — typed-constant interpolation is validated against the context-determined type
  - `docs/compiler/literal-system.md § String Coercion Table` — the thirteen-row scalar table, invariant culture
  - `docs/compiler/soundness-and-coverage.md:229-230` — the two rows this design's parent fork was split into
  - `src/Precept/Pipeline/SlotValue.cs:100` — `BecauseClauseSlot(string Message, SourceSpan Span)`
  - `src/Precept/Pipeline/TypeChecker.Normalization.cs:113` — the message becomes a `TypedLiteral`
  - `samples/customer-profile.precept:27` — a live corpus rule interpolating an `optional` field into its `because`
  - `samples/insurance-claim.precept:96` — a live corpus `reject` message interpolating a field
  - live `precept_compile` probes, 2026-07-23 — recorded in § What HEAD actually does
---

# Presence-tolerant message rendering

## How to read this document

Three plain terms carry most of the weight, so they are defined once here and then used in plain words.

- **Hole** — the `{...}` inside a quoted literal. `"amount {Amount} over limit"` has one hole.
- **Message position** — the text of a `because` rationale on a `rule` or `ensure`, or the text of a
  `-> reject "..."` refusal. Its output is read by a person. Nothing in the precept computes on it.
- **Value position** — every other place a quoted literal can appear: the right-hand side of a `set`,
  a default, a condition, a guard, a function argument. Its output becomes or feeds governed data.

The question this document settles is what happens when a hole names a field that might not have a
value — an `optional` field that is currently unset.

## Goal

When done: a `rule` or `reject` message may interpolate an `optional` field without the author being
forced to guard the very field the message is reporting on, and the language states — in canon, not
by accident of an unwalked code path — exactly what an absent value renders as and exactly where that
tolerance stops.

## Scope

- **In scope**: presence of an optional value at an interpolation hole; the boundary between the
  hole's outermost rendering step and any computation inside the hole; the scope line between message
  positions and value positions; what an absent value renders as; how that rendering interacts with
  the string-length interval the proof engine already computes.
- **Out of scope**: value faults inside a message hole (division by zero, overflow, `sqrt` of a
  negative). The owner ruled that arm on 2026-07-23 — those enroll and are caught at compile time.
  This document must not contradict that ruling and does not re-open it.
- **Out of scope**: `BUG-053` (an optional event argument read in arithmetic mints nothing). That is
  a pure value position with no rendering defence; it is an independent bug.
- **Deferred to future**: the String Coercion Table's omission of the seven business-domain types and
  `choice` — named in § Adjacent gap below, not fixed here.

---

## What HEAD actually does

Every claim in this section was established by live `precept_compile` on 2026-07-23. This is not the
proposed design — it is the baseline the design changes, recorded first so no later section can drift
from it.

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
   parsing work. That is **easier** than a first reading of the flat `string` suggests.
2. *For the soundness doc*: `docs/compiler/soundness-and-coverage.md:229` says *"`CollectObligations`
   walks a constraint's `.Condition` only and never its `.Message`, so nothing is minted today"* —
   true but incomplete. Message positions are excluded **twice**: the slot holds no expression tree,
   *and* `CollectObligations` (`src/Precept/Pipeline/ProofEngine.cs:208`) enumerates constructs by
   hand and never visits `Message` even though `TypedRule` and `TypedEnsure` both carry one
   (`SemanticIndex.cs:569-585`). Both exclusions have to be removed, and the reject arms are guarded
   on the *success* subtype (`ProofEngine.cs:217-218`, `:224-225`) so reject rows are never walked at
   all. The row's "same fix shape as the default-expression rows above" is therefore wrong in the
   direction of *understating* the wiring, while overstating the parsing.

Three probes confirming the effect:

```precept
rule Amount > 0 because "unknown {NoSuchField}"      # compiles clean — the name is never resolved
rule Amount > 0 because "bad {Amount + true}"        # compiles clean — decimal + boolean, no type error
rule Amount > 0 because "tags {Tags}"                # compiles clean — a collection, which the spec
                                                     # says is a compile error in interpolation
```

All three produce zero diagnostics. The third contradicts two pieces of canon at once —
`precept-language-spec.md:1553` ("Collections are a type error inside string interpolation") and the
String Coercion Table's `Collection | **Compile error** — use .count` row — **and that gap is not
confined to message positions.** `ResolveInterpolatedString`
(`src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:845-869`) performs no type
validation on hole types at all, and the diagnostic intended for it, `InvalidInterpolationCoercion`
(PRE0051, *"A {0} value cannot appear inside a text interpolation"*), is allow-listed as never
emitted — `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs:39`:
`"InvalidInterpolationCoercion", // TypeMismatch fires instead (precision upgrade)`. So the
collection-in-interpolation rule is unenforced everywhere, which is a separate defect from this
design and should be filed rather than absorbed.

### Nothing renders messages, anywhere

A repo-wide sweep of `src/`, `tools/`, and `test/` for any code that substitutes hole values into a
message found **none**. The runtime evaluator is a stub with no expression walk
(`src/Precept/Runtime/Evaluator.cs:45` — `// TODO: implement Fire/Update once the executable model is
designed`); `src/Precept/Runtime/Precept.cs:84` passes a reject string through verbatim;
`src/Precept/Language/Faults.cs:51`'s `string.Format` formats the *fault catalog's* own `{0}`/`{1}`
templates, not DSL interpolation; the language server's message handling
(`RichHoverFactory.cs:2970-2980`) unwraps `TypedLiteral` and prints it raw. `ProofEngine.cs:998`
renders a `TypedInterpolatedString` as the placeholder `"<string>"`.

This matters for the design in two ways. There is **no existing rendering behavior to stay consistent
with** — the field is genuinely open. And the flattened `"unknown {}"` form is **not a viable
template**, so whatever rendering is eventually specified requires the slot-shape change regardless
of how the presence question lands.

**This corrects a claim in the already-ruled value arm.** `soundness-and-coverage.md:229` records the
value-fault fix as *"Complete trigger: obligation-creation sweep, same fix shape as the default-expression
rows above."* It is not the same fix shape. The default-expression rows have a parsed expression that
a walker skips; the message has no expression at all. Implementing the ruled value arm requires the
message to become an expression first. That is a larger change than the row states, and the row should
be corrected whether or not this design's presence proposal is adopted.

### The four positions, measured

| Position | Probe | Presence minted? | Value fault minted? | Type-checked? |
|---|---|---|---|---|
| `because "…{Opt}…"` | `rule Amount <= Limit because "note: {Note}"` | No | No — `{100 / Divisor}` is silent | **No** |
| `-> reject "…{Opt}…"` | `-> reject "note {Note}, ratio {100 / Divisor}"` | No | No | **No** |
| `set F = "…{Opt}…"` | `set Marker = "note is {Note}, ratio {100 / Divisor}"` | **Yes** (PRE0116) | **Yes** (PRE0083) | Yes |
| `set F = '…{Opt}… hours'` (typed constant) | `set Window = '{Days} hours'` | **Yes** (PRE0116) | — | Yes |

So the split at HEAD is exactly **message positions mint nothing; value positions mint everything**.
Both message positions behave identically, which is a useful fact: whatever this design decides
applies to `because` and `reject` together, with no special-casing.

### The string-length interval already reads holes

This is the finding that makes the rendering question more than cosmetic. The proof engine computes a
static character-width interval for an interpolated template and discharges `maxlength` containment
from it:

```precept
field Code as string optional minlength 3 maxlength 3
field Marker as string maxlength 5 default ""

from Draft on Stamp when Code is set
    -> set Marker = "x{Code}x"
    -> transition Done
```

compiles with `LengthContainment … Proved` — the engine derived `1 + [3..3] + 1 = [5..5]` and fitted
it inside `maxlength 5`. When a hole's width is unknown the containment fails:

```precept
    -> set Marker = "tags {Tags}"    # PRE0135: "String value has ? character(s) but field 'Marker'
                                     # requires length in [0..200]"
```

**Therefore whatever an absent value renders as, its character width joins that interval arithmetic.**
A five-character sentinel raises the upper bound of every unguarded optional hole by up to five
characters; a zero-width one lowers the floor to zero. The sentinel decision has a proof consequence,
not only a display one.

**And the length machinery already contradicts itself on exactly this point.** The doc comment on
`HoleLengthInterval` (`src/Precept/Pipeline/ProofEngine.Lengths.cs:163-168`) states the zero floor and
gives this design's reason for it:

> "The lower bound is 0 (a hole could render empty for an optional/edge value), which is sound for a
> maxlength-direction proof"

But that floor is applied only on the **non-string** path (`:180` — `return (0, digits);`). A string
hole bypasses it (`:171-172`) and inherits the field's `minlength` as its floor, via
`LengthIntervalFromModifiers` (`:235-241`), which consults declared bounds and never presence — the
file contains no reference to presence or optionality at all.

That floor is not merely advisory: `TryLengthContainmentProof` uses it to return a **provably
violating** verdict rather than an unresolved one (`:33-34` — `if (req.DeclaredMaxLength.HasValue && min > req.DeclaredMaxLength.Value) return false;`).
So if an absent optional renders as empty, the computed floor overstates the true minimum and the
provably-violating branch can fire on a satisfiable program. This should be filed as a bug and
verified independently of this design; it is recorded here because it is **prior evidence of intent**
— someone already assumed absence renders empty — and because Decision 4 cannot be made without
knowing which floor is correct.

### A live hole found while probing

```precept
field BaseCurrency as currency optional
field Rate as exchangerate in '{BaseCurrency}' to '{QuoteCurrency}' optional maxplaces 8
```

compiles with **zero obligations**. A typed-constant hole in a *declaration* position is not walked,
so an absent currency would flow into the qualifier. Contrast `set Window = '{Days} hours'`, which
correctly mints PRE0116. This is a separate defect from the message question and should be filed;
it is noted here because it is the same family of unwalked interpolation and a reader will otherwise
assume typed-constant holes are uniformly handled.

---

## How big is the problem, actually

This section exists because the design's motivating premise did not survive measurement, and that has
to be visible rather than buried.

The premise was the owner's: enrolling presence at a message hole *"would force authors to guard the
field they are reporting on."* The draft cited two shipped samples as live instances. **Neither is
one.**

- **`samples/insurance-claim.precept:96`** — `-> reject "Police-report claims are capped at $100,000 (you submitted {ClaimAmount})"`. `ClaimAmount` is declared at `:22` as `field ClaimAmount as money in 'USD' default '0.00 USD'` — **required, with a default**. It is not optional, so it would mint no presence obligation under any reading. Miscited.
- **`samples/customer-profile.precept:27`** — the rule interpolates the optional `PreferredContactMethod`, but it already carries `when PreferredContactMethod is set`, and **that guard discharges the obligation.** Verified at HEAD in a direct-harness probe: a rule-level `when X is set` yields `PresenceProofRequirement Proved strategy=GuardInPath`, while the identical rule without the `when` yields `Unresolved` + PRE0116. So enrolling this message would cost **zero** extra guards. The design's single live motivating example does not survive.

**The corpus measurement.** Across `samples/`: 808 interpolation holes; 83 name an `optional` field;
**none of the 83 sit in a value position**; 71 of 83 already carry an `is set` guard in scope that
the engine discharges from; **11 are unguarded in scope** — `Test.precept:51`,
`academic-course-registration.precept:172`, `calibration-management.precept:173`,
`hotel-reservation-management.precept:132`, `incoming-material-inspection.precept:78`,
`library-hold-request.precept:111`, `library-inter-library-loan.precept:275`,
`maintenance-work-order.precept:98`, `non-profit-membership-renewal.precept:155` and `:163`,
`prior-auth-appeal.precept:349`.

*(The "none in a value position" figure is not merely counted — it follows: the corpus compiles clean,
and an unguarded optional in a value-position hole is a hard error at HEAD, so there cannot be one.)*

**And several of the 11 are not authoring gaps at all.** They sit under a state-scoped
`ensure X is set` that guarantees presence but that the proof engine cannot use — filed as
**BUG-060**, verified at HEAD. Those authors did guard; the engine cannot see it.

**What this means for the design.** The real, unforced demand is at most 11 holes out of 808, and
some fraction of those 11 is a proof-engine gap rather than an authoring need. That does not make the
design wrong — the rules below still have to be *stated* somewhere, because the message position is
currently unchecked in every respect and the already-ruled value-fault arm forces it open regardless.
But it does mean the design should not be justified as relieving author burden, and **BUG-060 should
close before message holes are enrolled**, or a fallback annotation will get adopted as a workaround
for a compiler weakness and become permanent noise in the corpus.

## The proposal

### The core semantic

A presence obligation exists because reading an absent optional leaves an operation **with no result**
— there is no decimal to divide, no string to compare, no length to take. Principle 10 (totality)
requires every expression to produce a result, so the compiler must prove the value is there before
any operation that is undefined without it.

Rendering a value to display text is **defined for absence**. There is a result: whatever the language
says an absent value renders as. It is a well-formed `string`. No fault mode exists.

So the proposal is not an exemption from the presence rule. It is the observation that the presence
rule's trigger — *"this operation is undefined when the value is absent"* — is simply not met by the
rendering step. The obligation is unnecessary by construction, which is the shape the owner asked for.

> **Rule R1 (rendering totality).** Coercing a value to its display text is a total operation: it is
> defined on every inhabitant of the type *and on absence*. It therefore mints no presence obligation.

> **Rule R2 (the boundary).** R1 applies to the **outermost** step of a hole and to nothing inside it.
> Every operator, accessor, and function application within a hole is an ordinary consumer and mints
> presence exactly as it would outside a string.

> **Rule R3 (the other target).** A `'...'` typed-constant hole is **not** a rendering step. Its
> content must satisfy the context type's content validator, which is not defined on absence. R1 does
> not reach it; a typed-constant hole reading an optional mints presence as usual.

### R3 is the same argument as R1, applied a second time

R1 says the render step mints nothing because *the thing it feeds accepts absence*. R3 says a typed
constant's hole mints presence because *the thing it feeds does not*. One principle, two targets —
not a rule plus an exception.

This framing was not the one this design started from, and the change is worth recording. The draft
originally justified tolerance **positionally**: message text is terminal, value text is governed,
so tolerance follows the position. The comparator survey commissioned for this design found that
framing to be both unprecedented and unnecessary:

- **Unprecedented.** *"No statically-checked language in this survey splits its presence rule by
  string position."* Rust actively refutes it — a `thiserror` `#[error("…")]` message desugars to the
  same `write!` with the same `Display` bound as any other format string, so a message gets no
  exemption. Dhall, Swift, Kotlin and C# have no such distinction either.
- **Unnecessary.** C# supplies the better mechanism. Its nullable-reference flow analysis is *not*
  exempt inside an interpolation hole — `CS8602` fires on `{c.Length}` where `c` is `string?`
  (measured, not read). The reason `{c}` alone does not warn is that the target parameter is
  annotated `string?`. As the survey puts it: *"The check isn't waived; the requirement isn't there."*
  That is exactly R1, and it needs no position predicate.
- **Jinja2 draws the same line one axis over.** Its default `Undefined` *"evaluate[s] to an empty
  string if printed or iterated over, and … fail[s] for every other operation"* — print tolerates,
  compute refuses. That is R1 plus R2. Jinja's split is by **operation kind**, not by which string
  literal you are in, and the survey notes that under Jinja's rule a `set Note = "…{X}"` would count
  as a print.

The remaining question the survey does not settle is whether Precept should *nonetheless* keep the
positional restriction for reasons of its own. That is Decision 3, and it is deliberately left open.

The typed-constant case is what makes R3 non-negotiable at its edge. A typed constant's content is
validated against a content validator for the context type:

```precept
field Days as integer optional
from Draft on Stamp
    -> set Window = '{Days} hours'      # today: PRE0116, correctly
```

If absence rendered tolerantly here, an unset `Days` would produce `'<sentinel> hours'`, which no
duration validator accepts — a runtime content failure from a clean-compiling precept. That is a
direct Principle 11 breach. Rendering tolerance cannot reach typed constants — not because of where
they sit, but because what they feed does not accept absence.

### The boundary cases, resolved

R2 generates these rather than enumerating them. Each row assumes R1 is in play for the hole itself —
i.e. the hole sits wherever Decision 3 finally lands — so the question here is only what R2 says about
the hole's *interior*. These answers are the same under either candidate scope.

| Hole | Outermost step | Presence obligation on `Opt`? | Why |
|---|---|---|---|
| `{Opt}` | render | **No** | The only consumer is the render step; R1 applies |
| `{10 / Opt}` | render of a division | **Yes** | `/` consumes `Opt` and is undefined on absence. The divisor obligation also enrolls, per the 2026-07-23 value-fault ruling |
| `{Opt.length}` | render of an accessor result | **Yes** | `.length` is undefined on absence |
| `{trim(Opt)}` | render of a call result | **Yes** | A function argument is an ordinary consumer |
| `{Opt + "x"}` | render of a concatenation | **Yes** | `+` requires a string operand; absence is not a string |
| `"{Opt} and {Other}"` | two renders | **No** for either | Each hole is independent; R1 applies per hole |
| `"{Opt} costs {10 / D}"` | one render, one division | **No** for `Opt`, **yes** for `D` | Per-hole, not per-template |
| `'{Opt} hours'` (typed constant) | content contribution | **Yes** | R3 — typed-constant content is governed data, not terminal display |

Two cases from the brief do not exist in the language and are struck rather than answered:

- **A conditional arm.** Precept has no ternary. `because "{Note is set ? Note : "x"}"` fails at the
  lexer — `'?' is not a valid character in a precept definition` (PRE0005, verified). There is no
  conditional-expression case to dispose of.
- **A nested interpolation.** Nesting is supported (spec § 1.4, mode stack depth 8), but it composes
  with no new rule: an inner hole is a hole, R1 and R2 apply to it at its own boundary.

**The pleasant property of R2.** The tolerant form is also the form an author would naturally write.
`"{Opt}x"` is both idiomatic and tolerant; `"{Opt + \"x\"}"` is neither. The rule does not ask authors
to learn an exception — it rewards the phrasing they were already going to use.

### What an absent value renders as

**Deliberately unresolved in this draft.** See Decision 4. A neutral survey is running; its
recommendation is an input, not a formality, and the width consequence recorded above means the choice
is load-bearing rather than cosmetic.

---

## Adjacent gap, named not inherited

The String Coercion Table (`docs/compiler/literal-system.md § String Coercion Table`) covers thirteen
scalar types plus a Collection row. It omits the seven business-domain types (`money`, `currency`,
`quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`) and `choice` entirely. An absence
row would join that table, so the table is about to be edited either way — and editing it while
leaving eight types undefined would bless the omission by proximity.

The probe `set Marker = "paid {Amt} in {Cur}"` with `Amt as money` compiles the money hole without a
type error, so *something* renders it, but nothing in canon says what. That gap is not fixed here. It
is named so the promotion pass that adds an absence row is on notice that the table it is editing is
incomplete in a second, unrelated way.

---

## Decisions

### Decision 1: Rendering is a total operation and mints no presence obligation

**Stakes**: high

- **Rationale**: The presence obligation's trigger is that an operation has no result when its operand
  is absent. Rendering has a result for absence once the language says what that result is. Framing
  the tolerance as a property of the *operation* rather than as an exemption for a *position* means
  Principle 10 is satisfied by construction rather than carved out — which was the explicit
  requirement the owner relayed.
- **Tradeoff accepted**: The language gains a second total-on-absence operation alongside `is set` /
  `is not set`. Every future operation must be classified as total-or-not on absence, and the
  classification has to live somewhere durable rather than in the walker's control flow.
- **Alternatives considered**:
  - *Enroll presence like any other read.* Rejected on the owner's stated ground: it forces an author
    to guard the field the message is reporting on. **But see § How big is the problem, actually —
    that ground is much weaker than this design assumed, and the two corpus instances originally
    cited for it do not support it.** This alternative is closer to live than the draft first
    suggested, and Decision 1 should not be locked until § How big is the problem is resolved.
  - *A positional carve-out ("messages are exempt from presence").* Rejected because it states an
    exception to Principle 10 rather than satisfying it. An exception is a place where the totality
    guarantee is known not to hold, which is precisely what the philosophy refuses to accumulate.
- **Precedent**: The language already has a total-on-absence operation — `is set` / `is not set`
  (`precept-language-spec.md:1484`, "`optional` field | Yes | `boolean`"). Presence testing consumes
  an optional and mints no presence obligation, for the same reason: it is defined on absence. R1
  adds a second member to a category that already exists rather than inventing one.
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md § 0.1` Principle 10 — "Every expression evaluates to a
    result — never silent `NaN`, `Infinity`, or `null`. The evaluation surface has no undefined
    behavior."
  - `docs/language/precept-language-spec.md:1484` — the `is set` typing row: "| `optional` field | Yes | `boolean` |"
  - `docs/language/precept-language-spec.md:1553` — "Each `{expr}` inside `\"...\"` is type-checked
    independently. Any scalar type is coercible to string." *Spec-first check: the spec states holes
    are type-checked and coercible; it is silent on presence at a hole. Grepped
    `precept-language-spec.md`, `primitive-types.md`, `literal-system.md`, `proof-engine.md` for
    "interpolat" ∧ "presence"/"optional" — no prior settlement.*
  - `samples/customer-profile.precept:27` — the corpus instance quoted above.
- **Strongest counter-evidence**: Principle 4 (full inspectability) argues the other way. A message
  that silently renders a sentinel hides, at the moment of explanation, that the value it is reporting
  was never there. The reader of the failure message sees the sentinel and cannot tell whether the
  field was unset or literally held that text. *Response*: this is a real cost and it is the reason
  the sentinel choice is a separate decision with its own weight rather than a detail. A sentinel that
  cannot be confused with authored data answers it; a sentinel that can (the empty string, or any
  plausible domain word) does not. Decision 4 must carry this.
- **Reversibility**: `Hard`. Once authors write messages that interpolate unguarded optionals, adding
  the obligation later breaks those precepts.
- **Blast radius**: Catalogs — a total-on-absence classification for the render step (placement is
  Decision 5). Docs — `precept-language-spec.md § 3.7`, `literal-system.md` String Coercion Table,
  `soundness-and-coverage.md:230`, `proof-engine.md`. Samples — none break; `customer-profile.precept:27`
  and `insurance-claim.precept:96` become explicitly legal rather than accidentally legal. External
  consumers — none; pre-release.

### Decision 2: Tolerance attaches to the hole's outermost step only

**Stakes**: high

- **Rationale**: The tolerance is justified by a property of the rendering operation. Nothing about a
  string literal makes the operations *inside* a hole total — `10 / Opt` is exactly as undefined on
  absence inside a message as outside one. Attaching tolerance to the hole boundary rather than to the
  hole's contents is the only reading that keeps the justification and the rule the same shape.
- **Tradeoff accepted**: An author who writes `"{Opt + " units"}"` gets a presence error while
  `"{Opt} units"` compiles. That is a real surprise the first time, and the diagnostic has to carry
  the fix.
- **Alternatives considered**:
  - *Whole-hole tolerance — anything inside a hole tolerates absence.* Rejected: it would make
    `{10 / Opt}` compile with an absent divisor, contradicting the owner's 2026-07-23 value-fault
    ruling, which the same holes are already enrolled under.
  - *Whole-template tolerance.* Rejected for the same reason, more broadly.
- **Precedent**: Precept already scopes narrowing per-expression rather than per-statement — the
  `GuardInPath` strategy discharges presence for the specific reference it dominates, not for the
  whole row (probe: `"x{Code}x"` under `when Code is set` discharged `Presence … strategy: GuardInPath`).
  Per-hole scoping is the same granularity the engine already reasons at.
- **Sources consulted for this decision**:
  - `docs/compiler/soundness-and-coverage.md:229` — the value-fault row: "**owner ruling, 2026-07-23:
    value-fault expressions in a constraint message enroll and are caught at compile time.**"
  - Live probe, `set Marker = "x{Code}x"` under a presence guard — obligation ledger returned
    `{"kind":"Presence","disposition":"Proved","strategy":"GuardInPath"}`.
  - *Spec-first check: grepped `precept-language-spec.md § 2.5 Interpolation Reassembly` and
    `literal-system.md` for any statement scoping semantics to a hole versus a template — the spec
    describes reassembly mechanics only and is silent on obligation scope.*
- **Strongest counter-evidence**: Nothing found arguing for whole-hole tolerance after checking the
  spec's interpolation sections, the soundness doc, and the obligation-discharge matrix's
  message-interpolation cells (`fault-6-division-primitive-message-interpolation.cells.json`, whose
  eight cells all treat the hole's *interior* as a real evaluation site with real obligations). The
  matrix actively contradicts whole-hole tolerance.
- **Reversibility**: `Hard` — same reason as Decision 1.
- **Blast radius**: Same surfaces as Decision 1; no additional.

### Decision 3: Whether tolerance is restricted to message positions — OPEN

**Stakes**: exploratory

This is the load-bearing open question, and it is open because the evidence moved during the design
pass. It needs an owner ruling; the analysis below is complete enough to make that ruling on.

- **Exploratory because**: The design pass began from the architect's message-scoped framing, and
  built a positional justification for it (message text is terminal, value text is governed). The
  comparator survey commissioned for this design then found that justification unprecedented, and
  supplied a cleaner mechanism that does not need the position predicate at all. Choosing to keep
  the positional restriction anyway is a legitimate call, but it is now a call *against* the survey
  rather than one supported by it, and it is the owner's to make. Widening scope beyond the framing
  the owner was consulted on is exactly what the pre-design gate exists to prevent, so this design
  will not widen it unilaterally.

- **The two candidates, stated plainly**:

  **(A) Message-only.** `"{Opt}"` is tolerated in a `because` rationale and a `reject` reason. In a
  `set` right-hand side it still requires proven presence, exactly as today.

  **(B) All `"..."` holes.** `"{Opt}"` is tolerated wherever a double-quoted string is interpolated.
  Typed-constant `'...'` holes are excluded either way, per R3.

- **What the survey says.** From
  `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md`:

  > "**No statically-checked language in this survey splits its presence rule by string position.**
  > The message-vs-value distinction exists in the field but lives at the *library/runtime* layer
  > (logging must not crash), and Jinja2's split is by *operation kind* (print vs. compute), not by
  > *which string literal you're in*."

  and, on the mechanism that makes (B) coherent without a position predicate:

  > "C# is *not* an example of 'interpolation exempts you from the null checker.' It is an example of
  > **the hole being an ordinary expression position whose target type happens to accept null**. The
  > check isn't waived; the requirement isn't there."

  and, as the standing warning against settling this per-construct:

  > "Four widely-deployed implementations, four behaviors … the same spelling `CONCAT()` meaning
  > opposite things in MySQL and PostgreSQL … Oracle's own doc hedges that its behavior 'may not
  > continue to be true.' … it must be **one rule, stated in the spec**, not an emergent per-position
  > accident."

  Rust is the sharpest counter to (A): a `thiserror` `#[error("…")]` message desugars to
  `write!("{}", self.var)` with the same `Display` bound as any other format string. A message string
  gets no exemption anywhere in Rust.

- **The argument for (A) that survives the survey.** Not precedent — there is none — but a
  Precept-specific one. A `set` right-hand side becomes stored, governed data; a sentinel there is
  indistinguishable from an author who wrote that text. Principle 1 is about what is stored.

  **This argument is weaker than it first looks, and the draft overstated it.** Two checks:
  1. A sentinel in a stored string is not an *invalid configuration* unless a rule forbids it, and
     any rule that forbids it still applies — governance is not bypassed.
  2. The length-interval machinery already catches the interesting case. `field Marker as string notempty`
     with `set Marker = "{Opt}"` and a zero-width sentinel yields an interval whose floor is 0, which
     fails the `minlength 1` containment and rejects. The protection (A) was invoked to provide is
     already provided by machinery that exists.

  So (A)'s remaining content is a judgement that authors *should* be made to guard before rendering
  into storage, not a soundness requirement.

- **The argument for (B).** One rule, one justification, no position predicate; it is the shape the
  survey's two most relevant mechanisms (C#'s target-type framing, Jinja2's print-vs-compute axis)
  both take. It also removes an asymmetry a domain expert would otherwise trip on: under (A),
  `"{Opt}"` compiles in a `because` and is rejected in a `set` two lines away.

- **The cost of (B), stated honestly.** It relaxes a check that fires correctly today —
  `set Marker = "note is {Note}"` → PRE0116, verified. Relaxations are where soundness holes hide,
  and this one has not been adversarially attacked yet.

- **Recommendation**: **(B)**, on the grounds that (A)'s justification did not survive scrutiny and
  (B) needs no unprecedented positional cut. Recorded as a recommendation, not a decision.

- **Decision needed before**: any spec text is written. Both Decision 1 and Decision 2 are unaffected
  by which way this lands.

- **The prior-canon trajectory — and it runs against tolerance, not toward it.** An earlier draft of
  this design read the history backwards; the corrected reading is load-bearing enough to lay out in
  full, because it is the closest thing canon has to a settled direction on this exact question.

  1. **`git show c4d0abf8:docs/LiteralSystemDesign.md:121`** stated the broad tolerant rule: *"In
     expressions, `null` … is coerced to empty string `\"\"` in string interpolation contexts"* — all
     interpolation, silent, no author involvement.
  2. **`git show 9ab60e47` (EvaluatorDesign.md, "Null Handling")** then *reversed* it: *"`.length` on
     a `null` value produces an evaluation error … `null` is not coerced to empty string."* This is
     not a deletion with a void premise — it is a deliberate replacement of silent coercion with
     error-on-access.
  3. **Current canon** went further than either. The `null` literal was removed entirely
     (`docs/compiler/literal-system.md:145` — *"The language has no `null` literal. Optional fields
     use `is set` / `is not set`"*), absence became a first-class runtime value
     (`docs/runtime/evaluator.md:228` — `PreceptValue` *"is the unified value representation for every
     scalar, reference, and absent value"*, with an explicit `IsAbsent`), and access to an optional
     was made to *require a guard*: `docs/language/primitive-types.md:118` — `.length` *"Requires
     presence guard (`is set`) for optional fields."*

  Every step moved **away** from silent tolerance of absence in a string. So the deleted rule is not
  evidence for (B) — it is the position canon has spent three revisions walking back. **This corrects
  the earlier draft, which cited it as pointing toward (B); it points the other way**, toward the
  strict reading and toward Decision 4's "no language-chosen sentinel" recommendation.

- **Spec-first check — and canon is not silent, contrary to the earlier draft.** Grepping
  `primitive-types.md`, `literal-system.md`, and `evaluator.md` for absence/coercion:
  - Canon **does** settle the general question: absence is *not* silently coerced anywhere, and
    accessing an optional requires a presence guard (`primitive-types.md:118`, `literal-system.md:145`,
    `evaluator.md:228`). R2 is consistent with this — an accessor like `.length` is a consumer that
    needs a guard, which is exactly what `primitive-types.md:118` already says.
  - Canon is silent on **one** narrower point only: the bare-render position `{Opt}`, where the value
    is coerced to display text and nothing is accessed. That is the sliver this design occupies, and
    it is where R1 lives.
  So this is a genuine design decision on the bare-render sliver — but it is a decision that must
  **swim upstream against a documented three-revision trajectory**, which is a materially different
  situation from the "no live canon settles this" the earlier draft claimed. R1 (tolerate the bare
  render) is the part in tension with that trajectory and needs the strongest justification; R2
  (consumers need guards) is already canon.

### Decision 4: What an absent value renders as — TBD

**Stakes**: exploratory

- **Exploratory because**: A neutrally-framed survey is running and has not returned. The owner
  explicitly declined to pre-commit — *"i'm not married to unset, let an unbiased agent run and see
  what they recommend"* — so committing here would defeat the instruction. Independently, the width
  finding recorded above means the choice has a proof consequence that no candidate has yet been
  evaluated against.
- **The neutral recommendation, returned 2026-07-23**: **no language-chosen sentinel at all.** An
  absent value renders as text the author wrote at the hole; if the author wrote none and presence is
  not provable, it does not compile. Every language-chosen sentinel — empty string, word,
  typographic mark, parenthetical, field name, type-specific — is rejected. Its strongest arguments:

  - **The two systems that lived with this longest both moved away from a language-chosen default,
    and neither moved toward a better word.** Swift shipped a default with a warning in 2016 and ten
    years later shipped `\(age, default: "missing")`. Jinja2 shipped the empty string and then shipped
    `StrictUndefined` because the default was a production footgun. Picking a sentinel now is picking
    the thing both mature comparators eventually replaced.
  - **Precept already makes this exact move.** Principle 9 refuses to let the language generate the
    human text explaining a constraint — `because` is mandatory. Absent-case rendering is the same
    category of text: human-facing and not derivable from anything in the definition.
  - **It is the only option that gives the author a lever on the length interval.** A fallback is a
    literal in the file, so both endpoints of the hole's width interval are author-visible and
    author-controlled. Every language-chosen sentinel imposes a width the author cannot see or change.
  - **Specific disqualifications worth recording**: `null` is out — `precept-language-spec.md:610`
    removed the literal entirely, and rendering the word reintroduces the concept in the surface the
    domain expert reads. An em dash is out — it is Precept's house message punctuation, so against
    `customer-profile.precept:27` it renders *"…method is — but no phone number is stored — the
    preference is unreachable…"*, two em dashes in one sentence, one data and one punctuation. The
    field's own name is out — it renders as a *broken template*, telling the reader the engine failed
    when it worked. Refuse-to-compile *alone* is out because Precept has no `??` and no ternary, so
    an author who wants to name the absent case would have no way to say it.

- **Why this stays exploratory rather than being adopted.** The recommendation introduces **new
  language surface** — a fallback annotation at the hole. That is a fresh Pre-Design Owner
  Consultation item in its own right, not something this design can absorb: the owner was consulted
  on presence tolerance for message rendering, not on adding syntax to the interpolation hole. It
  also interacts with Decision 3 (the recommendation argues for one rule across both positions) and
  with the § How big is the problem finding above, which weakens the case for doing anything at all.

- **Exploratory because**: the recommendation is off the candidate list it was given and requires an
  owner conversation before it can be a decision.

- **Decision needed before**: any spec text is written, and before the obligation-creation sweep
  implements the value-fault arm — the sweep touches the same code path.
- **Decision needed before**: any spec text is written, and before the obligation-creation sweep
  implements the value-fault arm — the sweep touches the same code path.
- **Open questions**:
  1. Does the sentinel's character width widen the length interval, or does the design instead
     require presence-unproven holes to carry an unknown width (failing containment, as collections
     do today)? These are different answers with different author-visible consequences.
  2. Must the sentinel be un-collidable with authored data — i.e. must a reader of a rendered message
     be able to tell "this field was unset" from "this field held that text"? Principle 4 argues yes;
     no candidate on the list satisfies it except a typographic mark or a refusal.
  3. Does the answer differ between `because` and `reject`? Nothing found suggests it should, but the
     survey was asked to say so if it disagrees.

### Decision 5: Where the total-on-absence classification lives

**Stakes**: medium

- **Rationale**: Placeholder — this is a catalog-placement question that depends on Decision 4's
  shape and on the archaeology of how holes are represented once messages become expressions. It is
  named here so the doc does not silently omit it; it will be written when those land.
- **Tradeoff accepted**: TBD.
- *(This decision is incomplete and blocks Lock.)*

---

## Sections still to write

This draft is honest about being partial. The following are required by the design skill for a
language-surface change and are **not yet written**:

- **Philosophy Alignment** — the eleven-principle matrix. Principles 1, 4, 10, and 11 all have real
  content here and three of them cut in different directions; the matrix is the place that gets
  resolved, and it should not be written before Decision 4 lands.
- **Language Design Grounding** — blocked on the commissioned comparator survey. `research/language/`
  and `research/INDEX.md` were grepped for "interpolation", "optional", "null", "Option": **no
  existing study covers this domain.** That gap is now recorded; the survey will either fill it or
  the design will cite an inline survey.
- **Audience and Teachability** — worked example, the `set`-position diagnostic wording promised under
  Decision 3, and the 10-minute path.
- **Semantic Rules** — R1/R2/R3 stated above in prose need reduction and typing-rule notation, plus
  the soundness-preservation claim naming Principles 10 and 11 explicitly.
- **Architecture Grounding** — layer placement (Decision 5), cross-component propagation, external
  comparator.
- **Inventory**, **Acceptance criteria**, **Doc-update enumeration**, **Falsifiers**.

## Open questions

1. Decision 4 (the sentinel) and Decision 5 (placement) are unresolved. Both block Lock.
2. Should the correction to `soundness-and-coverage.md:229` — that the value-fault fix is *not* the
   same shape as the default-expression rows, because the message is not an expression — be applied
   now as a standalone doc fix, or carried by this design's promotion? It is true independent of this
   design's outcome, which argues for now.
3. Four live defects were found while probing and reading source for this design. All are filed and
   none is absorbed into this design: **BUG-056** (declaration-position typed-constant hole never
   walked), **BUG-057** (optional event-arg refs treated differently in the two interpolation forms),
   **BUG-058** (`InvalidInterpolationCoercion` has zero emission sites, so the collection rule is
   unenforced everywhere), **BUG-059** (string-hole length floor contradicts its own doc comment and
   feeds a provably-violating verdict). BUG-059 is the one that interacts with this design —
   Decision 4 cannot be made without knowing which floor is correct.

## Review record

- **2026-07-23** — Draft written. Boundary cases probed against HEAD with `precept_compile`; results
  recorded in § What HEAD actually does rather than asserted. Two brief-supplied cases (a conditional
  arm, the `deleted-canon precedent as authority`) were struck on evidence: the language has no
  ternary, and the deleted rule's premise is void. One brief-supplied claim was corrected: the
  soundness doc's "same fix shape" characterisation of the value-fault fix is wrong.
- **2026-07-23, second pass** — a commissioned comparator survey and an independent source-archaeology
  pass both returned, and **both moved the document**:
  - The survey (now at `research/language/expressiveness/absence-in-string-interpolation-survey-2026-07-23.md`)
    found the draft's positional justification for Decision 3 **unprecedented** — no statically-checked
    language in 13 surveyed splits its presence rule by string position, and Rust actively refutes it.
    It also supplied a better mechanism (C#'s target-type framing, Jinja2's print-vs-compute axis) that
    reaches the same tolerance without a position predicate. Decision 3 was rewritten from a locked
    high-stakes decision into an **open two-sided fork** with a recommendation, and R3 was re-derived
    as the same argument as R1 rather than a separate positional rule.
  - The archaeology corrected the draft's claim that message holes "are never parsed." They *are*
    parsed by the same `ParseInterpolatedString` value positions use, then flattened to the literal
    `{}` and discarded (`Parser.Expressions.cs:558-564`) — the field name does not survive. It also
    established that **nothing anywhere in the repo renders a message**, and surfaced the length-floor
    contradiction now filed as BUG-059.
- **2026-07-23, third pass** — the neutrally-framed sentinel agent returned and delivered **three
  corrections, two of which were verified independently before being accepted** (the precept MCP
  server was down, so verification used a direct `Compiler.Compile` harness against
  `src/Precept/Precept.csproj`):
  - `samples/insurance-claim.precept:96` interpolates `ClaimAmount`, which `:22` declares
    `money in 'USD' default '0.00 USD'` — **required, not optional**. Miscited by this design.
    *Verified by reading both lines.*
  - `samples/customer-profile.precept:27` already carries `when PreferredContactMethod is set`, and a
    rule-level guard **discharges** presence. *Verified: guarded rule → `Proved strategy=GuardInPath`;
    the same rule without the `when` → `Unresolved` + PRE0116.* The design's single live motivating
    example does not survive; § How big is the problem records this.
  - A state-scoped `ensure X is set` does **not** discharge presence for a read in that state.
    *Verified with the harness — `Unresolved` + PRE0116.* Filed as **BUG-060**, and it is a sequencing
    dependency: it should close before message holes are enrolled.
  The recommendation itself — no language-chosen sentinel; author-supplied fallback text or refuse —
  is recorded under Decision 4 and left exploratory, because it introduces new language surface that
  needs its own owner conversation.
- **Adversarial pass against R1/R2: NOT YET RUN.** The survey and archaeology attacked the *scope* and
  the *mechanism claims*; neither tried to find a program that R1 or R2 mis-classifies. Five validity
  arguments and three cross-cutting rules written in prose without adversarial review on 2026-07-23
  were all refuted within hours. This draft must not advance past `Draft` until that pass runs.
