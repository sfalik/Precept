---
title: "Bare identifier resolution — arg-shadows-field"
status: Draft investigation — 2026-06-02
kind: decision-support
purpose: >
  Investigate how a bare identifier that is both a declared field name and an in-scope
  event argument should be resolved, and whether the arg-shadows-field case should be
  diagnosed. Grounds an owner decision and a possible /lifecycle-2-design pass. Does NOT
  lock a decision.
sources-consulted:
  - path: docs/language/precept-language-spec.md
    sections: ["§3.4 Name Resolution (UndeclaredField row)", "§3.5 Scope Rules (expression-scope table, quantifier BindingShadowsField, Event arg access)"]
    excerpt: |
      §3.4: "Undeclared field reference | IdentifierExpression in expression context does not
      match a field name (or in-scope event arg) | UndeclaredField"
      §3.5 Quantifier Shadowing: "If a field with the same name exists at global scope, the
      binding variable shadows it inside the predicate. Error: BindingShadowsField — rename
      the binding to avoid confusion."
      §3.5 Event arg access: "Event args are accessed via dotted notation: EventName.ArgName."
  - path: docs/compiler/type-checker.md
    sections: ["§ Identifier Resolution Priority", "Decision 20"]
    excerpt: |
      "When resolving an identifier expression, check scopes in this order: 1. Quantifier
      bindings 2. Event args 3. Fields 4. Error."
      D20: "Identifier resolution priority | Quantifier bindings > event args > fields |
      Innermost scope wins; shadowing is predictable and standard"
  - path: src/Precept/Pipeline/TypeChecker.Expressions.cs
    sections: ["ResolveIdentifier (lines 892–950)"]
    excerpt: |
      "// Priority (D20): quantifier bindings > event args > fields."
      Quantifier bindings checked first, then CurrentEventArgs (returns TypedArgRef), then
      FieldLookup (returns TypedFieldRef). No collision check between the arg branch and the
      field branch — the arg branch returns before the field branch is reached.
  - path: src/Precept/Pipeline/NameBinder.cs
    sections: ["WalkExpression QuantifierExpression case (lines 679–693)"]
    excerpt: |
      "case QuantifierExpression quant: // Check for binding shadowing field (Q6 — hard error)
      if (_fieldsByName.ContainsKey(quant.BindingName)) { _diagnostics.Add(...BindingShadowsField...) }"
  - path: docs/philosophy.md
    sections: ["Who authors a precept", "Full inspectability", "Prevention not detection"]
    excerpt: |
      "The primary author of a .precept file is a domain expert or a business analyst …
      not primarily software developers. The language is designed for someone who thinks in
      terms of what the data means and what it is allowed to become."
      "Nothing is hidden."
  - comparable-systems: [C# (CS0136 / IDE0003 / IDE0009), Java JLS 6.4.1, Rust (book §3.1 + clippy shadow lints), CEL langdef, Drools DRL]
  - path: samples/insurance-policy-endorsement.precept
    sections: ["field NewPremium (29) / event Approve arg NewPremium (102) / set sites (138,139,170)"]
    excerpt: |
      Field and event-arg deliberately share the name 'NewPremium'; the arg is always read via
      the dotted 'Approve.NewPremium', and bare 'NewPremium' only appears as a set-target (write)
      or in a transition where the arg is out of scope. The collision idiom is pervasive across
      the sample corpus; bare→arg on a colliding name is avoided in practice.
---

# Bare identifier resolution — arg-shadows-field

## Problem statement

A bare identifier `X` in an expression can resolve to (in priority order) a quantifier
binding, an in-scope event argument, or a declared field — `TypeChecker.Expressions.cs`
`ResolveIdentifier`, Decision 20. When a field `X` and an in-scope event arg `X` both exist,
the bare `X` **silently binds to the arg**. Meanwhile the parallel case — a quantifier
binding `X` that shadows a same-named field — is a **hard error** (`BindingShadowsField`,
PRE0103). The language diagnoses one shadowing case but not the structurally parallel one.

This investigation establishes whether that asymmetry is principled, whether there is any
soundness risk, surveys how comparable systems treat name-is-both-a-parameter-and-a-field,
and recommends an option with four-leg rationale.

---

## Finding 1 — The asymmetry is real and undocumented as a decision

### Spec / impl quotes (verbatim)

**Quantifier binding shadows field → ERROR** (`precept-language-spec.md` §3.5, Quantifier
binding variable scope table, **Shadowing** row):

> If a field with the same name exists at global scope, the binding variable shadows it
> inside the predicate. **Error: `BindingShadowsField` — rename the binding to avoid
> confusion.**

Emitted in `NameBinder.cs` (lines 680–687):

```csharp
case QuantifierExpression quant:
    // Check for binding shadowing field (Q6 — hard error)
    if (_fieldsByName.ContainsKey(quant.BindingName))
    {
        _diagnostics.Add(Diagnostics.Create(
            DiagnosticCode.BindingShadowsField,
            quant.Span,
            quant.BindingName));
    }
```

**Event arg shadows field → SILENT** (`TypeChecker.Expressions.cs` §`ResolveIdentifier`,
lines 892–932). The arg branch returns a `TypedArgRef` before the field branch is reached;
there is no collision check:

```csharp
// 2. Event args (second priority)
if (ctx.CurrentEventArgs is not null &&
    ctx.CurrentEventArgs.TryGetValue(name, out var arg))
{
    ctx.ArgReferences.Add(new ArgReference(arg, id.Span));
    return new TypedArgRef(arg.ResolvedType, arg.EventName, arg.Name, arg.DeclaredQualifiers, id.Span);
}

// 3. Fields (lowest priority)
if (ctx.FieldLookup.TryGetValue(name, out var field)) { ... }
```

### What rationale does D20 actually give?

`type-checker.md` Decision 20 (decisions table, row 20):

> **Identifier resolution priority** | Not addressed | **Quantifier bindings > event args >
> fields** | **Innermost scope wins; shadowing is predictable and standard**

D20's rationale is entirely about **resolution order** ("innermost scope wins") — the
ordering that makes a bare name deterministic. It says **nothing** about whether a
field/arg *collision* should be *diagnosed*. The `BindingShadowsField` decision lives in a
different stage (the binder, `Bind`) and a different design lineage (the quantifier-scope
"Q6 — hard error"). The two decisions were never reconciled against each other.

**Verdict on the asymmetry:** it is an **oversight of reconciliation, not a principled
distinction.** D20 chose an order (innermost wins) and stopped there; the quantifier path
independently chose to *forbid* the collision rather than silently shadow. No document
argues that arg-shadows-field is acceptable while binding-shadows-field is not. There is no
recorded rationale that draws the line where the code currently draws it.

A secondary observation reinforces this: `BindingShadowsField`'s own diagnostic message —
"rename the binding **to avoid confusion**" — is a *clarity* rationale, not a
*soundness* one. The exact confusion it prevents (a bare name silently meaning the binding,
not the field the author may have intended) is the same confusion the arg case produces.

---

## Finding 2 — Soundness vs clarity

The runtime is still a stub, so there is no executing binding to mismatch the compile-time
binding **today**. But the relevant question is whether the *design* (compile-time
resolution = arg) is the binding the runtime will be built to honor. The proof engine
already reasons over the compile-time binding (see Finding 3 probe: the divisor obligation
is computed against the **arg's** constraint surface, not the field's). So:

- **No latent compile-vs-runtime divergence is designed in** — there is one binding
  (the arg), and both the type checker and proof engine use it. A runtime built to match
  will honor the same binding. This is internally consistent.
- **The real issue is authoring clarity and prevention-surface honesty.** A bare `Position`
  that the author *believes* refers to the `min 5` field actually refers to an
  unconstrained arg. The proof engine then correctly reports the arg can be zero / out of
  range — but the author's mental model ("Position is the field, which is ≥ 5") is silently
  wrong. The diagnostic that *does* fire (e.g. `DivisionByZero`) points at the symptom, not
  the cause (the silent rebind). For a domain-expert author (philosophy: "not primarily
  software developers… thinks in terms of what the data means"), the silent rebind is a
  footgun: the same surface syntax means two different things depending on whether an event
  arg happens to share the field's name.

This is squarely a **clarity / "nothing is hidden"** concern, not a soundness hole. That
matters for option selection: it lowers the urgency from "must fix to be sound" to "should
fix to honor the inspectability and domain-author commitments," and it means a **warning**
is a defensible severity (a soundness hole would demand an error).

---

## Finding 3 — `precept_compile` probe results (MCP reconnected, 2026-06-02)

All probes run against the live MCP server.

### Probe A — bare divisor binds to the arg, not the field (re-confirmed)

```precept
precept ArgFieldShadow
field Position as integer min 5
field Other as integer
event Move(Position as integer)
on Move
    -> set Other = 100 / Position
```

Result: `PRE0083` — *"Division is unsafe: 'Position' can be zero in event handler 'Move'"*.
The field `Position` carries `min 5` (never zero); the arg `Position` is unconstrained. The
divisor was treated as the **arg** (can be zero) → confirms arg-shadows-field. **No
shadowing diagnostic emitted.**

### Probe B — decisive type-disagreement probe

Field and arg given **different types** so the binding choice produces a different
compile outcome:

```precept
precept TypeShadowProbe
field Position as string default "x"
event Move(Position as integer)
on Move
    -> set Position2 = Position + 1
field Position2 as integer default 0
```

Result: **`success: true`** (only an unrelated PRE0158 write-site warning on the field).
`Position + 1` is legal only if `Position` is the **integer arg**; if it bound to the
`string` field it would be a type error. → Bare `Position` bound to the **arg**, silently.

### Probe C — reverse control (no shadowing arg)

```precept
precept TypeShadowReverse
field Position as string default "x"
field Position2 as integer default 0
event Move(Other as integer)
on Move
    -> set Position2 = Position + 1
```

Result: **`PRE0018`** — *"Expected a string value here, but got 'integer'"*. With no
same-named arg, bare `Position` binds to the `string` field and `+ 1` is a type error. →
Confirms that the *only* difference between B (clean) and C (type error) is the presence of
the shadowing arg. The arg silently changes what `Position` means.

### Probe D — ensure context

```precept
precept ArgFieldShadowEnsure
field Position as integer min 5
event Move(Position as integer)
on Move ensure Position >= 5 because "must be at least 5"
on Move
    -> set Position = Move.Position
```

Result: the bare `Position` in the `ensure` resolves to the arg (same priority), so the
field's `min 5` does not gate the bare reference; behaves as an arg constraint. Confirms the
rebind applies in guard/ensure contexts too, not just action RHS.

**Probe takeaway:** arg-shadows-field is real, silent, and applies across action RHS,
guards, and ensures. No diagnostic of any severity is emitted for the collision itself.

> Note (out of scope): Probes with `min 5` fields-as-divisors also surfaced that the proof
> engine did not discharge a `100 / Position` obligation from the field's `min 5` in a
> *rule* context (`FieldOnlyDivisor` still reported PRE0083). That is a separate
> proof-engine matter, not part of this shadowing question, and is not relied on here — the
> decisive evidence is the **type-disagreement** Probe B/C pair, which is independent of any
> min-proof behavior.

---

## Finding 4 — Comparable systems

How do other languages/DSLs resolve and/or diagnose a name that is both a
parameter/argument/binding and an instance field/member?

| System | Resolution | Diagnosed? | Disambiguation | Verbatim excerpt + citation |
|---|---|---|---|---|
| **C#** (param vs field) | Parameter wins (innermost) | **Silent** by default | `this.field` | CS0136 governs only **local-vs-local** in nested blocks, *not* param-vs-field: *"A local variable named 'var' cannot be declared in this scope because it would give a different meaning to 'var', which is already used in a 'parent or current/child' scope…"* — [CS0136](https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0136). `this.` qualification is a **configurable style preference, off by default**: IDE0003 (prefer no `this.`) / IDE0009 (prefer `this.`) — [IDE0003/IDE0009](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0003-ide0009). |
| **Java** (param vs field) | Parameter wins (innermost) | **Silent** (allowed) | `this.field` | JLS 6.4.1: *"A declaration d of a local variable or exception parameter named n shadows … the declarations of any other fields named n that are in scope…"*; *"The keyword `this` can also be used to access a shadowed field x, using the form this.x. Indeed, this idiom typically appears in constructors."*; *"In general, however, it is considered poor style to have local variables with the same names as fields."* — [JLS SE17 §6.4.1](https://docs.oracle.com/javase/specs/jls/se17/html/jls-6.html). |
| **Rust** (let-binding shadow) | Inner binding wins (innermost) | **Silent / intentional** | re-`let` / new name | Rust Book §3.1: *"you can declare a new variable with the same name as a previous variable … the first variable is shadowed by the second, which means that the second variable is what the compiler will see when you use the name."* — [Book ch03-01](https://doc.rust-lang.org/book/ch03-01-variables-and-mutability.html). Clippy's `shadow_unrelated` is in the **restriction group (allow-by-default)** — opt-in only — [Clippy shadow lints](https://github.com/rust-lang/rust-clippy/blob/master/clippy_lints/src/shadow.rs). |
| **CEL** (binding vs name) | Comprehension (binding) var wins (innermost) | **Silent / deterministic** | leading `.` for root scope | *"When resolving a name within a comprehension body, the name is first compared against the variables declared by the comprehension. If there is a match, the name resolves to that variable, taking precedence over the package-based resolution rules…"* — [cel-spec langdef](https://github.com/google/cel-spec/blob/master/doc/langdef.md). |
| **Drools** (DRL var vs field) | Bound variable distinguished by sigil | Convention, **not enforced** | `$var` naming convention | *"To differentiate more easily between variables and fields in a rule, use the standard format $variable for variables… This convention is helpful but not required in DRL."* — [Drools Language Reference](https://kie.apache.org/docs/10.0.x/drools/drools/language-reference-traditional/index.html). |

### Consensus

The field's mainstream consensus is **shadow-silently with innermost-wins**, with
disambiguation **available but optional**:

- **Resolution order:** universally **innermost scope wins** (param/binding over field).
  Precept's D20 ("quantifier bindings > event args > fields") matches this consensus
  exactly. The *ordering* is not in question.
- **Diagnosis:** **No surveyed mainstream language makes param-shadows-field a hard error.**
  Java and C# allow it silently and provide `this.` to disambiguate; the strongest signals
  are *style-level*: JLS calls it "poor style," C#'s `this.` enforcement is an opt-in style
  rule (off by default), Rust's `shadow_unrelated` is opt-in (restriction group), Drools is
  convention-only.
- **Disambiguation surface:** every system that allows the shadow provides an *explicit*
  disambiguation form for the outer name (`this.x`, leading `.`, `$var` convention). Precept
  already has one for the inner name's source: the dotted `EventName.ArgName`.

A nuance worth surfacing for the owner: the strongest precedent for *erroring* on a
field/binding collision in this codebase is **Precept's own** `BindingShadowsField` — i.e.,
Precept is already *more* opinionated than C#/Java/Rust/CEL on the quantifier path. So the
asymmetry can be read two ways: "the arg path is too lax relative to the binding path" *or*
"the binding path is stricter than the field consensus." Either reading points at the same
fix surface (reconcile the two), but they differ on direction.

---

## Finding 5 — Sample corpus: the name-collision is idiomatic, but bare→arg is avoided in practice

A grep of `samples/*.precept` for events whose arg name equals a same-precept field name
shows the collision is **pervasive and deliberate** — calibration, insurance (policy
endorsement, renewal, subrogation), medical (prior-auth, device-tracking, referral), library,
and patient samples all reuse a field's name as an event arg's name. The canonical idiom is
"event `Approve` carries arg `NewPremium`, which is `set` into field `NewPremium`."

Crucially, in those samples the colliding name is **never referenced bare to mean the arg**.
Concrete witness — `insurance-policy-endorsement.precept`:

```precept
field NewPremium as money in 'USD' default '0.00 USD' nonnegative      # line 29
event Approve(ApprovedBy as string …, NewPremium as money in 'USD' positive)   # line 102
…
-> set NewPremium = Approve.NewPremium                                  # line 138 (in Approve handler)
-> set PremiumAdjustment = Approve.NewPremium - CurrentPremium          # line 139
…
-> set CurrentPremium = NewPremium                                      # line 170 (different transition, arg NOT in scope → field)
```

- Line 138 LHS `NewPremium` is a **`set` target** (a write position, not an
  identifier-resolution *read* — so it is the field, unambiguously, and would **not** trip a
  read-side collision diagnostic).
- The arg is always read via the **dotted** `Approve.NewPremium` (lines 138, 139).
- Line 170's bare `NewPremium` is in a transition where `Approve`'s args are **not** in scope,
  so it correctly means the field — no collision.

**Implications for option selection and severity:**

- The corpus authors already *instinctively avoid* bare→arg on a colliding name, using the
  dotted form whenever the arg is in scope. This is direct evidence that the silent bare→arg
  rebind is a latent footgun the idiom routes around, not a feature anyone relies on.
- A **read-side** collision diagnostic (Option B) would fire on **near-zero** existing sample
  sites, because `set`-target LHS positions are writes, not reads, and every in-scope arg read
  is already dotted. This makes even an **error**-severity Option B low-breakage on the current
  corpus — but step 7 below still requires a full audit before choosing `error`.
- Option C (bare = field, dotted-only args) would *not* break these samples either (they
  already dot their arg reads), which is a point in C's favor — but C still discards the
  innermost-wins ergonomics for the *non-colliding* arg case, where bare arg reference is
  legitimate and convenient.

---

## Per-option analysis

### Option A — Keep bare→arg silent shadowing (D20 as-is); document the bare form in §3.5

**What:** Accept the current behavior. Clarify §3.5's "Event arg access" section — which
today documents *only* dotted `EventName.ArgName` — to state that a bare identifier also
resolves to an in-scope arg, and that an arg shadows a same-named field silently (innermost
wins).

- **Aligns with:** the field consensus (C#/Java/Rust/CEL all shadow silently). Zero new
  diagnostic surface. Matches D20's stated "predictable and standard."
- **Against:** leaves the asymmetry with `BindingShadowsField` unreconciled and unexplained
  — a domain author sees the *quantifier* collision rejected but the *arg* collision
  accepted, with no stated reason. Most directly in tension with philosophy's "nothing is
  hidden" and the domain-expert-author framing: the silent rebind is exactly the "different
  meaning to the same name" footgun, and the surveyed precedents that tolerate it
  (C#/Java/Rust) target *developers*, not domain experts.
- **Doc-only cost:** spec §3.5 edit; no code, no diagnostic.

### Option B — Diagnose arg-shadows-field, mirroring `BindingShadowsField`

**What:** When a bare identifier is both a declared field and an in-scope event arg, emit a
diagnostic ("rename the arg, or use `EventName.Arg` / qualify the field"). Bare→arg still
resolves normally when there is **no** field collision. Severity is the open sub-question
(warning vs error — see below).

- **Aligns with:** Precept's *own* established `BindingShadowsField` decision — restores
  symmetry: every name-collision-with-a-field path is treated the same way. Strongly aligns
  with "nothing is hidden" / domain-author clarity. The author is told *at the collision
  site* rather than discovering it via a downstream symptom (DivisionByZero on a value they
  thought was constrained).
- **Against:** more opinionated than the C#/Java/Rust/CEL field consensus (none of which
  error). If `error`, it forbids a pattern those languages allow; the dotted
  `EventName.ArgName` remains available, so the author is never *blocked* from referring to
  the arg — only from doing so via the *bare colliding* name. Adds one diagnostic.
- **Severity nuance:** `BindingShadowsField` is an **error**. Symmetry argues error. But
  Finding 2 establishes this is a *clarity* not a *soundness* issue, which makes **warning**
  defensible and softens the "more opinionated than the field" tradeoff. The
  symmetry-vs-severity tension is the main thing for the owner to resolve, and is a
  legitimate `/lifecycle-2-design` question.
- **Doc + code cost:** new `DiagnosticCode`; emission in the binder (alongside
  `BindingShadowsField`) or the checker; spec §3.4/§3.5 + `diagnostic-system.md` +
  `type-checker.md` D20 updates.

### Option C — Bare = field always; event args only via dotted `EventName.ArgName`

**What:** Disallow bare→arg entirely. A bare identifier always means the field. Event args
are reachable *only* through the dotted form (which §3.5's "Event arg access" already
presents as *the* access path).

- **Aligns with:** the **current written spec most literally** — §3.5 "Event arg access"
  documents *only* `EventName.ArgName` and never mentions a bare arg form, so the
  implementation's bare→arg path is arguably **already undocumented surface / drift**. Makes
  a bare name mean exactly one thing forever (maximally "one obvious meaning"). Eliminates
  the collision class entirely rather than diagnosing it.
- **Against:** the most behavior-breaking — every existing bare-arg reference in samples
  (e.g. would need auditing) becomes either a field reference or an error. It also *removes*
  the innermost-wins ergonomics that the entire field consensus endorses, and that D20
  deliberately chose. Verbose: every arg use becomes dotted, even when there is no field
  collision at all (penalizing the common, non-colliding case to fix the rare colliding
  one).
- **Doc + code cost:** remove the bare-arg branch in `ResolveIdentifier`; rewrite §3.5 +
  D20; audit/repair all samples; likely new "use EventName.ArgName" diagnostic for bare
  references that match an arg. (Note: an `EventArgOutOfScope`/PRE0050 path *already* exists
  for out-of-scope arg names — Option C would extend that family.)

---

## Recommendation — **Option B** (diagnose arg-shadows-field), severity to be set in design

> This recommendation grounds an owner decision and a possible `/lifecycle-2-design`. It is
> **not** a locked decision. The arg-shadows-field collision is **new diagnostic surface**;
> per the Pre-Design Owner Consultation gate it requires owner conversation before any
> design/agent work, and the warning-vs-error severity is itself a design question.

### Four-leg rationale

- **Rationale.** The collision is a genuine authoring footgun (Findings 1–3: the same bare
  name means the field in one precept and the arg in another, decided silently by whether an
  arg happens to share the name — Probe B/C make this decisive). Precept has **already
  decided** that a field-name collision with an inner binder is worth a diagnostic
  (`BindingShadowsField`, with the rationale "to avoid confusion"). The arg case is the
  structurally identical collision with the identical confusion. Diagnosing it
  **reconciles the asymmetry by making the language internally consistent**, and honors the
  "nothing is hidden" / domain-expert-author commitments (philosophy) more faithfully than
  any silent-shadow option. Crucially, Option B is *non-breaking for the non-colliding
  case*: bare→arg still works whenever there is no same-named field, preserving the
  innermost-wins ergonomics the field consensus endorses (Finding 4). It surgically targets
  only the ambiguous overlap.

- **Alternatives considered and rejected.**
  - *Option A (keep silent + document):* rejected as the primary path because it leaves the
    asymmetry standing with no stated rationale and most directly violates "nothing is
    hidden" for the domain-expert author. It is the strongest *fallback* if the owner judges
    field-consensus alignment (silent shadow) more important than internal symmetry — that
    is a legitimate call, which is why this is routed to the owner rather than locked.
  - *Option C (bare = field, dotted-only args):* rejected as too blunt — it discards the
    innermost-wins ergonomics the entire field consensus endorses and that D20 deliberately
    chose, penalizes the common non-colliding case with mandatory dotting, and is the most
    breaking. Its one real virtue (the bare-arg form is arguably undocumented spec drift
    today) is better addressed by *documenting* the bare form (a sub-step of A or B) than by
    removing it.

- **Precedent.**
  - *Internal, decisive:* `BindingShadowsField` (PRE0103) — Precept's own locked decision to
    error on a field-name collision with an inner binder, with the explicit "to avoid
    confusion" rationale. Option B extends an existing Precept decision rather than importing
    an external one.
  - *External, supporting the disambiguation shape:* Java JLS 6.4.1 ("considered poor style
    to have local variables with the same names as fields") and CEL/C#/Drools all provide an
    *explicit* disambiguation form for the colliding name. Precept already has both forms —
    `EventName.ArgName` for the arg, and the field's own name once the arg is renamed — so a
    diagnostic that says "disambiguate" has a concrete, already-existing fix path.
  - *External, qualifying the severity:* no surveyed language makes the collision a hard
    *error* (they warn or rely on convention). This is the precedent that argues the Precept
    diagnostic should likely be a **warning**, not an error — diverging from
    `BindingShadowsField`'s error severity. The internal-symmetry pull (error) vs the
    field-consensus pull (warning) is the precise tension for the design pass to resolve.

- **Tradeoff accepted.** Option B makes Precept *more opinionated than the C#/Java/Rust/CEL
  field consensus*, which all permit param-shadows-field silently. We accept being stricter
  than those general-purpose languages because (a) Precept's author is a domain expert, not
  a developer trained to read `this.` disambiguation, and (b) Precept already took the
  stricter stance on the quantifier path, so the *consistent* choice is to extend it rather
  than carve an unexplained exception. If severity lands at `error`, we additionally accept
  forbidding a bare reference pattern those languages allow — mitigated by the always-available
  dotted `EventName.ArgName` escape hatch, so no arg is ever unreachable.

---

## Concrete changes the recommended option implies

> Enumerated for the owner / a future `/lifecycle-2-design`; **not** to be applied in this
> investigation. Severity (`Warning` vs `Error`) is left open as the design decision.

1. **New diagnostic** — `diagnostic-system.md` and `DiagnosticCode.cs`/`Diagnostics.cs`:
   add a code (working name `ArgShadowsField`) in the `Bind` stage (to sit beside
   `BindingShadowsField`, which is already `Bind`). Message shape:
   *"Event arg '{0}' shadows a field with the same name — rename the arg, or write
   `{event}.{arg}` to refer to the arg and `{0}` to refer to the field."* Category `Naming`,
   severity per design. Related-codes link to `BindingShadowsField` (PRE0103) and
   `EventArgOutOfScope` (PRE0050).

2. **Emission site** — emit when an event arg name collides with a declared field name and
   the arg is in scope. Cleanest home is the name binder (it already owns
   `BindingShadowsField`, `_fieldsByName`, and event-arg registration), making the collision
   a *declaration-time* check (fires once per colliding arg) rather than a *per-reference*
   check in `ResolveIdentifier`. Decide in design: declaration-time (binder) vs
   reference-time (checker). Declaration-time is recommended — symmetric with
   `BindingShadowsField`, and avoids per-use noise.

3. **Spec §3.4** (`precept-language-spec.md`, Name Resolution table): the `UndeclaredField`
   row already parenthesizes "(or in-scope event arg)". Add a row (or note) for the
   field/arg collision pointing at the new diagnostic.

4. **Spec §3.5** (`precept-language-spec.md`, Scope Rules):
   - In the expression-scope table / "Event arg access" subsection, **document the bare-arg
     resolution form** (currently only the dotted form is described — this is the latent
     drift Option C flagged). State that a bare identifier resolves to an in-scope arg, and
     that a same-named field collision is diagnosed (the new code), mirroring the existing
     quantifier `BindingShadowsField` row.
   - Cross-reference the quantifier `BindingShadowsField` row so the two shadowing rules read
     as one consistent family.

5. **`type-checker.md` Decision 20**: extend D20's rationale beyond "resolution order" to
   record that a field/arg collision is *diagnosed* (not silently shadowed), pointing at the
   binder-owned diagnostic. Reconcile the wording with the §13 "Binding shadowing:
   quantifier bindings shadow event args and fields" line so the doc no longer implies silent
   field-shadowing by args.

6. **Tests** — add type-checker/binder tests: (a) field + same-named arg referenced bare →
   new diagnostic; (b) bare→arg with **no** field collision → still resolves clean (no
   regression of innermost-wins); (c) dotted `EventName.ArgName` alongside a same-named field
   → field reachable bare, arg reachable dotted, no diagnostic if design says the dotted form
   suppresses it (a design sub-question).

7. **Sample audit** — grep `samples/*.precept` for any event whose arg name equals a field
   name in the same precept; none should exist if the recommendation lands as a warning, but
   confirm before choosing `error` severity (an undiscovered sample collision would become a
   build break under `error`).
