# Phase 2 — corrections closing the coverage gaps the independent recheck found

**Run 2026-07-27** against the tree the recheck read (`independent-recheck.md`, HEAD `d32d8e26`).
Nothing was committed. `docs/philosophy.md`, `docs/Working/bugs.md`, `docs/working-reset/phase-5/`
and `contradiction-register.md` were not edited — the first is owner-gated with a proposal already
staged, the next two carry uncommitted owner work, and the last is being edited by another agent.

**What this closes.** The recheck's § 2.3 (twelve register rows unexecuted and disclosed nowhere),
§ 2.2's `SC-1`/`N65` pattern finding together with the three live sites it let through, § 6's
architecture-document finding, and § 4.1 / § 4.2 — the two contradictions created on the night of
the correction slices.

**What it may cite**: the canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`, the
phase-1 register and walk. No catalog, no sample, no compile.

---

## 0. Running the patterns

Every pattern below is a `/usr/bin/grep` invocation. The repository's default `grep` is a `ugrep`
wrapper that dies on a long alternation over this file set; use the absolute path.

```bash
cd "$(git rev-parse --show-toplevel)"

CANON="docs/language docs/compiler docs/runtime docs/philosophy.md"
CANON_WIDE="$CANON README.md docs/compiler-and-runtime-design.md"
CANON_WIDE_NOPHIL="docs/language docs/compiler docs/runtime README.md docs/compiler-and-runtime-design.md"
```

Two patterns are scoped narrower than `$CANON` on purpose, and each says why on its own row. A
pattern scoped for convenience would be the same defect the recheck named; a pattern scoped because
the remaining hits are a *disclosed, owned* debt is a different thing, and both cases are stated.

---

## 1. The two rewritten patterns — `SC-1` and `N65`

The recheck's § 2.2 finding: `SC-1` grepped `carries a constraint the engine can discharge` and
`N65` grepped `carries a sufficient constraint`. Both are exact single clauses, both returned zero,
and the model they were written to prove gone was still in the tree in at least four shorter
spellings. A pattern that greps a whole clause while the idea has three shorter spellings passes
while the wording stands.

**One pattern replaces both.** The two rows are about the same superseded model — the compiler
proving that an operand *carries* a discharging constraint, rather than proving the rule by
induction from the four declared premise classes — so one pattern that catches the idea in every
spelling serves both rows better than two clause-matches.

**Rewritten pattern (register rows `SC-1` and `N65`):**

```bash
/usr/bin/grep -rniE \
 'carr(y|ies|ying)[^.]{0,40}(a |its |the )?(sufficient|discharging|declared) constraint|carr(y|ies|ying)[^.]{0,20}(a |its |the )?(sufficient|discharging) fact|carries its constraint' \
 $CANON_WIDE_NOPHIL
```

Returns **zero** after the corrections in § 3 below.

**Run against the original clause it had to catch, this pattern hits all four of the spellings the
recheck listed and one more nobody had listed.** Before the corrections it returned
`compiler-and-runtime-design.md:149`, `:153`, `:165`, `:169`, `proof-engine.md:1715` and
`philosophy.md:55`. The old `SC-1` and `N65` patterns returned zero on every one of them.

**Why `docs/philosophy.md` is excluded, and it is not a convenience exclusion.**
`docs/philosophy.md:55` carries the same model in the same words — *"It requires the value to
*carry* a constraint sufficient to prove the calculation safe … The compiler proves a structural
fact — that the value carries its constraint"*. It is owner-gated and must not be edited here. It
is already staged for owner approval as **Change 6** in
[`philosophy-changes-for-approval.md`](philosophy-changes-for-approval.md) (§ *Change 6 — line 55,
the carried-constraint paragraph*), with the want document's line 19 quoted as what forces it. When
that change is approved and applied, the exclusion comes off and the pattern runs against the full
`$CANON_WIDE`. Until then, running it over `docs/philosophy.md` **should** return that one line, and
a run that returns nothing there means the philosophy change landed.

```bash
# Confirms the one excluded site is still exactly the one that is staged for approval.
/usr/bin/grep -rniE 'carr(y|ies|ying)[^.]{0,40}(a |its |the )?(sufficient|discharging|declared) constraint|carries its constraint' docs/philosophy.md
# expected: docs/philosophy.md:55 — and nothing else
```

**A companion pattern for the other half of the same defect**, which the recheck's § 6 named
separately: the sentence at `:149` did not only state the wrong discharge model, it enumerated
**three** premise sources where the want document and the corrected specification name **four**.

```bash
/usr/bin/grep -rniE \
 'narrowed (in)?to (its|the) interval[^.]{0,120}guard|declared modifier/rule[^.]{0,120}statically-known|guard fact on the path[^.]{0,80}statically-known' \
 $CANON_WIDE
```

Returns **zero**, `docs/philosophy.md` included.

---

## 2. Sites executed

Each row: file, line at HEAD after the edit, the text as it stood, the text now, and a pattern that
returns zero afterwards. Patterns are collected and re-run together in § 4.

---

### 2.1 `docs/compiler-and-runtime-design.md` — the carried-constraint discharge model

Recheck § 4.3 and § 6. The recheck named `:149`, `:153` and `:169`. Running the rewritten pattern
found a **fourth site in the same document, `:165`**, and a **fifth in `proof-engine.md`** — both
carrying the same model in the same words. Register row `N62` covers `:149` and `:165` and is marked
"confirmed by reading"; that confirmation was about whether the *provenance* implication survived,
not whether the *discharge model* did. All five are corrected, because a pattern that leaves any of
them standing is the defect this section exists to fix.

---

**`docs/compiler-and-runtime-design.md:149`** — the architecture overview's statement of fault
prevention. This is the site the recheck put in front of the owner first, on the grounds that
`CLAUDE.md` names this document the architecture entry point, `precept-language-spec.md:274`
delegates the composition description to it, and phase 5 builds the discharge procedure from it.

*Was:*

> The proof engine discharges each obligation by establishing that the operand carries a discharging
> fact — a declared modifier/rule narrowed into its interval, a `when`-guard fact on the path, or a
> statically-known value.

*Now:*

> The proof engine discharges each obligation by proof over the reachable configurations, from the
> **four classes of premise the author declared** (spec §0.7): field modifiers, constraints on event
> arguments, the handler's guard, and the rules that already hold in the pre-state. The base case is
> that the default configuration and every initial event establish every rule; the inductive step is
> that every handler preserves every rule. The fourth class is the one that is easy to leave out and
> cannot be: a handler that adds a non-negative amount to a balance preserves
> `Balance >= -OverdraftLimit` *only* because the rule held before the event — no guard and no
> argument bound reaches it on its own. (A statically-known operand discharges immediately; there is
> no configuration to induct over.) Where a proof cannot close, the compiler rejects and names the
> premise that would close it.

The four classes are `precept-language-spec.md:268` verbatim, which is
`what-i-want-2026-07-16.md:19` verbatim. The `Deposit` sentence is the want document's own worked
case — *"the inductive hypothesis … No guard or arg bound alone gets there"* (want:101) — written in
because the missing fourth class is precisely the premise it turns on, and an agent building from
the three-source cascade would have found `Deposit` did not close and concluded the induction was
infeasible.

---

**`docs/compiler-and-runtime-design.md:153`** — § 1.1 Composition.

*Was:* "…relies on the operand's *declared* constraint … it proves the structural "carries a
constraint" fact and rejects when it cannot."

*Now:* "…rests on a premise the author declared — a modifier on the field or the argument, the
handler's guard, or a rule that already held; ingress governance makes that premise true of the
actual value. The compiler never inspects the value — it proves the rule from the declared premises
and rejects when the proof cannot close."

Matched to `precept-language-spec.md:274`, which this paragraph is the mechanism half of.

---

**`docs/compiler-and-runtime-design.md:165`** — § 1.2 walk, step 2. *Not named in the recheck; found
by the rewritten pattern.*

*Was:* "Discharge establishes the *structural* fact that the operand carries a sufficient constraint
(a declared modifier/rule narrowed to its interval, a `when`-guard fact on the path, or a
statically-known safe value) — never the concrete runtime value."

*Now:* "Discharge proves the obligation from the premises the author declared — a field modifier, a
constraint on an event argument, the handler's guard, or a rule that already holds in the pre-state
(spec §0.7) — never from the concrete runtime value."

---

**`docs/compiler-and-runtime-design.md:169`** — § 1.2 walk, step 4, the composition seam.

*Was:* "…rests on the operand's *declared* constraint … The compiler proved "carries its
constraint"; ingress makes the constraint hold."

*Now:* "…rests on a premise the author declared … The compiler proved the rule from the premise;
ingress makes the premise hold."

---

**`docs/compiler/proof-engine.md:1715`** — the length-containment strategy. *Not named in the
recheck; found by the rewritten pattern.*

*Was:* "The proof works by the operand *carrying* a declared constraint (§0.7 Composition): an
arg/field bounded to `maxlength N` is provable into a destination of `maxlength ≥ N`…"

*Now:* "The premise is the modifier the author declared on the source (§0.7 Composition, premise
classes (a) and (b)): an arg/field bounded to `maxlength N` is provable into a destination of
`maxlength ≥ N`…"

The mechanism it describes is unchanged and correct; only the framing moves from carried-constraint
to premise class.

---

### 2.2 `N75` — `notempty`, the first of phase 1's two *don't remember* items

**`docs/language/primitive-types.md:582`**, the constraint catalog row. This is the disposal phase 1
counted as closing walk item 20b; leaving it unexecuted quietly reopened a phase-1 finish-line
condition.

*Was:*

> | `notempty` | `string`; also `set`, `queue`, `stack`, `log`, `bag`, `list`, `queue of T by P` |
> `.length > 0` on string; `.count ≥ 1` on collections (equivalent to `mincount 1`). … |

*Now:*

> | `notempty` | `string` only — including a collection's string inner type, where it binds each
> element (`set of string notempty`) | `.length > 0`. It says nothing about how many elements a
> collection holds: "the collection has at least one element" is `mincount 1`, and that is the only
> spelling of it. … |

**What decided it, without owner time.** Two canonical documents disagreed and one of them names the
consequence: `proof-engine.md:2133` — *"`notempty` is string-only and no longer carries a `count > 0`
satisfaction — it does not discharge collection non-emptiness"*. Every other canonical statement
already agrees with string-only: `precept-language-spec.md:441`, `:1129`, `:1156`, `:1677`, `:1687`,
`collection-types.md:632`, `:776`, `:794`, `type-checker.md:362`. `primitive-types.md:582` was the
single outlier, and it was the one asserting two spellings of one cardinality constraint.

I checked `collection-types.md`'s own constraint catalog in the same pass, as the register's
resolution directs. It was already correct at `:776` and `:794`; no edit needed there.

---

### 2.3 `N41` — Principle 7 and the overflow disclosure

**`docs/language/precept-language-spec.md:104`.** Principles 10 (`:110`) and 11 (`:112`) each gained
*"**Overflow is currently exempted from this principle**, by an explicit ruling deferring the
arithmetic overflow model past the first release; see §0.7"* in the same commit. Principle 7, six
lines above them in the same list, makes the same claim about the same fault and did not.

*Added, verbatim from its two siblings, after "…has no unproven evaluation faults.":*

> **Overflow is currently exempted from this principle**, by an explicit ruling deferring the
> arithmetic overflow model past the first release; see §0.7.

**The goal is not weakened.** Overflow stays in Principle 7's list of what compile-time structural
checking catches, exactly as §0.7 `:266` still names it and §0.7 `:270` still says *"the paragraph
above states the target the language is being built toward and that target is not being weakened to
match."* The disclosure records where the target is not yet met; it does not lower the target.

---

### 2.4 `N43` — the overflow disclosure gap the 2026-07-14 ruling created

`primitive-types.md:81` was corrected to disclose the deferral for `integer`. `:197` and `:236` state
the same thing flatly with nothing connecting them to it, so a reader who meets either first reads
overflow as prevented today.

**`docs/language/primitive-types.md:197`**

*Was:* "**Backing:** Exact whole number. No fractional part. Overflow is a type error."

*Now:* "…Overflow is a type error — that is the end state this section describes, and it is not
being weakened. The compile-time proof that would deliver it is deferred past the first release,
exactly as the `integer` row of the approximation-stance table above records; see [Open
Questions](#open-questions--implementation-notes) and `precept-language-spec.md` §0.7."

**`docs/language/primitive-types.md:236`**

*Was:* "| `decimal + decimal` | `decimal` | Stays in lane. Overflow is an error. |"

*Now:* "| `decimal + decimal` | `decimal` | Stays in lane. Overflow is an error — the target, with
the proof behind it deferred past the first release as for `integer` (see [Open
Questions](#open-questions--implementation-notes)). |"

---

### 2.5 `N31` and `N32` — the runtime fault trap in the number lane

**`docs/language/primitive-types.md:652`–`:656`**, the non-finite rounding table.

*Was:*

> | `+∞` | Runtime fault — integer overflow | Runtime fault — cannot round infinity |
> | `-∞` | Runtime fault — integer overflow | Runtime fault — cannot round infinity |
> | `NaN` | Runtime fault — NaN is not a number | Runtime fault — NaN is not a number |

*Now:*

> | `+∞` | Deferred with the arithmetic overflow model — no compile-time proof, and nothing at
> evaluation time either | Same deferral |
> | `-∞` | Deferred with the arithmetic overflow model — no compile-time proof, and nothing at
> evaluation time either | Same deferral |
> | `NaN` | Cannot arrive — prevented at compile time and at ingress (see below) | Cannot arrive —
> prevented at compile time and at ingress (see below) |

The three rows now say what the two paragraphs immediately below them already ruled: the owner
ruling of 2026-07-21 at `:662`–`:663` says `NaN` is prevented (every path closed or already carrying
an obligation, with the `number`-lane qualifier modifiers establishing not-`NaN` at ingress) and the
infinities are deferred under the arithmetic overflow model. The table had been contradicting the
ruling printed underneath it.

**`docs/language/primitive-types.md:658`** — the register's `N31` site.

*Was:* "…The table above states what the runtime does if a non-finite value ever reaches a rounding
function; it is not the enforcement model. Per Principle 10 …, a fault-prone operation is proven
safe or the definition is rejected — a runtime trap is defensive redundancy, never the primary
mechanism."

*Now:* "…The table above is a disposition, not a description of a check the evaluator performs: per
Principle 10 … a fault-prone operation is proven safe or the definition is rejected, and there is no
runtime trap standing behind it. The two non-finite cases are dispositioned separately, and the
section below says which is prevented and which is deferred."

**`docs/language/primitive-types.md:671`** — the third `N32` site, integer conversion overflow.

*Was:* "…This is a runtime fault — the value exceeds `long.MaxValue` / `long.MinValue`. … Statically
preventable when the proof engine can bound the expression range."

*Now:* "…it exceeds `long.MaxValue` / `long.MinValue`. … This is the conversion case of arithmetic
overflow and it carries the same deferral: the target is a compile-time proof that bounds the
expression range and rejects when it cannot, and until that ships neither the compiler nor the
evaluator stands behind it (`precept-language-spec.md` §0.7)."

This is the case the register flagged as the one place deleting the runtime trap without the
overflow ruling would leave a fault class with neither half of a guarantee and no disclosure. It now
states the gap rather than claiming either prevention or a trap.

---

### 2.6 `N33` — the same sentence, copied into a collection document

**`docs/language/collection-types.md:766`.**

*Was:* "The compiler proves emptiness safety or emits a diagnostic. Runtime fault checks exist only
as defensive redundancy for paths the compiler has already proven unreachable."

*Now:* "The compiler proves emptiness safety or rejects the definition. Nothing stands behind that at
evaluation time — there is no runtime check on an empty access, because the compiler has already made
the path unreachable (`precept-language-spec.md` §0.7)."

---

### 2.7 `N40` — element-bound containment re-anchored to rule preservation

**`docs/language/collection-types.md:627`**, the element write-site bullet. The register's direction
is to re-anchor it with the row that moved declared-bound containment off the fault list.

*Added to the bullet, after "…the same containment obligation a `set` into a bounded field carries":*

> That obligation is **rule preservation, not a fault gate**: an element bound is rule shorthand
> exactly as a field bound is (precept-language-spec.md §2.4), so keeping an element inside it is
> proven and refused under the same single constraint mechanism as any other rule, and it should not
> be quoted as one of the fault classes (`precept-language-spec.md` §0.7).

*And, on the diagnostic names in the same bullet:*

> …(`LengthBoundViolation` for length, `NumericOverflow`/`OutOfRange` for numeric — despite its name,
> `NumericOverflow` here names a value leaving a declared bound, not arithmetic overflow of the
> machine type).

That second clause is `diagnostic-system.md:163` restated where a reader of the collection document
will meet it: *"`NumericOverflow` names a computed value leaving a declared bound, not arithmetic
overflow of the machine type."* Without it, the bullet reads as though an element bound were on the
overflow list, which is what the `N40` row is about.

---

### 2.8 `N74` — the fifth item from the 2026-07-14 block inventory

**`docs/language/temporal-type-system.md:143`**, the last row of the context-aware classification
table.

*Was:* "| (none) | Period path | Duration path | TEMP005 compile error | N/A |"

*Now:* "| (none) | Compile error — no context, no type | Compile error — no context, no type |
TEMP005 compile error | N/A |"

*And a new paragraph immediately below the table, at `:145`:*

> **No expected type is always a compile error.** The `(none)` row above agrees with the
> type-resolution rules further down (§ *Type resolution rules — context-dependent*, the `No context
> / ambiguous` rows): a temporal quantity written with no context does not silently acquire a type.
> `'3 days'` on its own is rejected with "Can't determine the type of `'3 days'` without context. Use
> it in an expression like `DueDate ± '3 days'` so the type is clear." The language does not guess
> (`precept-language-spec.md` §0.1 principle 7), and a typed constant that took its type from nothing
> would be exactly that.

**What decided it.** `:1182` and `:1208` — the type-resolution-rules section, later in the document,
more specific, and carrying the author-facing message — both say no context is a compile error. The
classification table is a summary of that section. Choosing the summary would mean a typed constant
acquires a type from nothing, which contradicts `precept-language-spec.md:104` — *"The compiler
proves what it can, rejects what it can prove invalid, and does not guess."* No owner time needed:
one canonical text is the summary of the other and they disagree.

---

### 2.9 `N19` (and with it `N15`, `N20`) — `RestoreConstraintsFailed`

Canon had already written the correction and deferred it to an API cleanup no phase owned. The
register's resolution: phase 2 owns it now. Executing it here also clears the two hits that kept
`N15` and `N20` from reporting themselves done — the recheck's § 2.1 shows both rows' remaining hits
were exactly these two lines.

**`docs/runtime/runtime-api.md:273`** — removed from the caller example:

```csharp
    RestoreConstraintsFailed f      => HandleSchemaDrift(f.Violations),
```

**`docs/runtime/runtime-api.md:281`** — the deferral note.

*Was:* "**`RestoreConstraintsFailed` is superseded** by the trusted-hydration decision (spec §0.7):
Restore does not re-validate … It should be removed when the Restore API is finalized … the example
above is left until that API cleanup lands."

*Now:* "**There is no constraint-failure outcome on restore.** Restore does not re-validate —
restored data is trusted as valid at the time it was persisted (spec §0.7) — so the only ways restore
can fail are structural: an undefined state name, unknown fields, a type mismatch. `RestoreOutcome`
carries those and nothing else."

The three structural failure kinds are `precept-language-spec.md:1951` verbatim.

---

### 2.10 `N8` — OQ-4

**`docs/runtime/result-types.md:213`.**

*Was:* "…**OQ-4 (pending):** whether `EventEnsures` should move inside `TransitionInspection`
(per-row, evaluated against each row's post-mutation state) or remain event-level."

*Now:* "…It stays **event-level**. OQ-4 used to ask whether it should move inside
`TransitionInspection`, evaluated per row against that row's post-mutation state; that option
describes a pipeline the language no longer has. An `on <Event> ensure` validates the caller's
arguments at ingress, once, before any row is matched (spec §3A.1), so there is no per-row
post-mutation state to evaluate it against and nothing to attribute to a row. What the array should
*carry* now that execution has no constraint-evaluation phase is the open question recorded below
under *what the `ConstraintResult` arrays mean now* — not where it lives."

**Only the half canon decides is decided.** The *placement* question is answered by the model change:
`precept-language-spec.md:1893` makes `on <Event> ensure` event-argument truth, and `:1909` makes
ingress the only evaluation point, so a per-row post-mutation evaluation is not a thing that exists.
The question of what the three `ConstraintResult` arrays should carry instead is **not** answered
here — `result-types.md:250` already records it as an open design question, and this edit points at
that note rather than inventing an answer for it.

---

### 2.11 `N9` — the comparator row

**`docs/runtime/result-types.md:375`.**

*Was:* "| OPA `deny[msg]` | Collect-all violation set | ConstraintResult lists, not short-circuit. |"

*Now:* "| OPA `deny[msg]` | Collect-all violation set | Ingress validation reports every violation,
not the first — `InvalidArgs` for event and construction arguments, `UpdateOutcome.ConstraintsFailed`
for a patch. Collect-all applies at ingress only; there is no post-mutation evaluation left to
collect from. |"

Collect-all survives — `precept-language-spec.md:1909` keeps it as how validation reports — but only
at ingress. The row had it attached to a constraint-result set produced after
the mutations, which is the removed model.

---

### 2.12 `N59` — the non-linear rule, an ADDITION

**`docs/language/precept-language-spec.md:270`**, a new paragraph in §0.7, immediately after the
induction paragraph at `:268` and before the overflow disclosure at `:272`. Canon had no statement of
this at all; the only `non-linear` hit was an unrelated sentence about UCUM units.

Added:

> **Non-linear rules are proven from a premise that states the condition directly, or the definition
> is rejected.** Substitution closes a linear rule quietly — a guard such as
> `Balance - Amount >= -OverdraftLimit` is literally the post-state condition, so the proof falls out.
> A rule whose post-state condition is *non-linear* — a product or quotient of two fields, such as
> `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` — has no such cancellation available: a
> write that sets one operand to a quotient and is checked against a product leaves four free
> variables and nothing to eliminate. Such a rule is proven only when premise class (c), the
> handler's guard, or class (b), a constraint on an event argument, states the post-state condition
> directly, so the proof closes against what the author wrote. Where no such premise exists the
> compiler **rejects the handler** and names the guard that would close it — "cannot prove
> `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`; add a `when` guard restating the
> post-state condition". This holds at every write site of every field the rule mentions, so lowering
> `OverdraftLimit` inherits the same obligation from the other side even though `MonthlyRepayment` is
> untouched. It is never governed by a pass over the finished configuration, and never deferred to a
> solver the compiler does not carry. The rule is stated separately here only because the linear
> cases close so quietly that the harder ones could look as though they come along for free; they do
> not.

**Every clause is the want document's.** The line-in-the-open framing, the "guards them, linearizes
them, or the compiler refuses them" disposition, and the "never a post-mutation sweep, never a
solver" pair are want:135. The worked case — `MonthlyRepayment <= OverdraftLimit *
RepaymentCapPercent`, the quotient-versus-product shape, the four free variables, and the symmetric
inheritance by the handler that lowers `OverdraftLimit` — is want:105 verbatim in substance. Nothing
here is a new design decision; it is the owner's own statement moved from the want document into the
specification, which is what an ADDITION row is for. Phase 10 needs it because want:123's mutation
row is one of the fourteen must-not-compile cases and has had no canonical statement to test against.

---

### 2.13 Recheck § 4.1 — construction-time constraint composition

**`docs/language/precept-language-spec.md:2087`**–`:2092`. A contradiction created on the night of
the correction slices: `:2080` was corrected to *"same ingress validation of the supplied arguments,
same guards, same action chain as any other event"*, and this block five lines below it still
described the post-mutation sweep in five layers.

*Was:*

> **Construction-time constraint composition.** When the initial event fires, constraints compose
> naturally with no new language surface:
>
> 1. **Arg ensures** (`on Create ensure ...`) — pre-assignment validation of caller-provided args.
> 2. **Field constraints** (rules, field-level ensures) — post-assignment truth.
> 3. **Global rules** (`rule ...`) — always evaluated.
> 4. **Entry ensures** (`to <InitialState> ensure ...`) — construction-specific truth. …
> 5. **Residency ensures** (`in <InitialState> ensure ...`) — while-in-state truth.
>
> No special "construction constraint" form is needed. `to <InitialState> ensure` is the natural
> construction-time rule: it fires when the entity enters the initial state, which is exactly what
> construction does.

*Now:*

> **Construction-time constraint composition.** Constraints compose at construction with no new
> language surface, and they compose the same way they do for every other event — which means there
> is exactly one place a constraint is *evaluated* at construction, and everything else is proven
> before the entity exists.
>
> 1. **At ingress, evaluated.** The caller's arguments are held to the constraints declared on them —
>    the modifiers on each arg and any `on <InitialEvent> ensure ...` — before the hollow version is
>    built and before any action derives a value from them. This is collect-all (§3A.1): every
>    failing argument is reported, not the first. A failure here is `InvalidArgs`; nothing was
>    mutated, so nothing is rolled back.
> 2. **Everything else, proven.** Global rules (`rule ...`), entry ensures
>    (`to <InitialState> ensure ...`) and residency ensures (`in <InitialState> ensure ...`) are the
>    base case of the induction in §0.7: the compiler proves that the default configuration and every
>    initial event *establish* them, from the constraints declared on the initial event's arguments.
>    A definition whose initial event cannot be shown to establish them is rejected before any
>    instance exists. There is no step between the action chain and the commit at which they are
>    re-evaluated, because the compiler already proved the initial event establishes them — which is
>    why `ConstraintsFailed` is not in construction's outcome space (§3A.2).
>
> No special "construction constraint" form is needed. `to <InitialState> ensure` remains the natural
> way to write a construction-time truth — it is the truth that must hold on entry into the initial
> state, which is exactly what construction produces. What changed is not where the author writes it
> but who discharges it: the compiler, once, rather than the runtime on every construction.

**What supplied the replacement**, all of it already in canon after the correction slices:
`runtime-api.md:162` (*"There is no constraint-evaluation step between the action chain and the
commit: the compiler proved that the initial event establishes every rule"*), the base case at
`precept-language-spec.md:268`, `:1909` (validation runs at ingress; rules and ensures are not
re-evaluated after the mutations complete), and `:1947` (`ConstraintsFailed` is structurally
impossible at construction). The author-facing point the old block was making — that no special
construction-constraint form is needed — is kept, because it is still true.

---

### 2.14 Recheck § 4.2 — the PRE0115 construction exemption

**`docs/language/precept-language-spec.md:2183`**–`:2185`. The second contradiction created that
night: the specification stated whole-precept suppression as settled design rationale while
`proof-engine.md:485` and `precept-language-spec.md:213` — rewritten in the same window — say the
exception is per field and the whole-precept early-return is a prevention hole.

*Was:*

> **Construction exemption for initial-state satisfiability (`PRE0115`):** When a precept declares an
> initial event with at least one construction row (`IsConstruction == true`), the proof engine skips
> the default-value satisfiability check for the initial state. The rationale: an entity with a
> construction event never inhabits the initial state with only default values — the construction
> row's action chain establishes field values before any constraint is evaluated. Checking defaults
> against ensures would be a false positive for Pattern A precepts with unguarded ensures on the
> initial state.

*Now:*

> **Construction exemption for initial-state satisfiability (`PRE0115`):** The exception the language
> allows is **per field**, and it is the one designed in §0.6 item 11: a default that the
> construction event *provably overwrites* is a placeholder that never reaches a committed
> configuration, so no rule and no initial-state ensure has to hold of it. Checking such a default
> would be a false positive for a Pattern A precept whose initial state carries unguarded ensures.
>
> A default the construction event does **not** provably assign is a different thing: it is still the
> entity's initial value, so a rule or ensure over that field is part of the base case, and
> suppressing the check leaves the base case of the induction open for that field — a prevention hole
> rather than a conservative under-emit. The engine today early-returns for the whole precept as soon
> as a construction handler exists, which is wider than this exception; that is an implementation
> gap, not settled design, and `../compiler/proof-engine.md` records it as one.

Both problems the recheck named are gone: "before any constraint is evaluated" is out, and the
specification now argues one side rather than two. The false-positive concern the old rationale was
protecting is kept — it is the reason the per-field exception exists — and the design's own words come
from `precept-language-spec.md:213` and `proof-engine.md:485`.

---

## 3. Left unedited, and why

**`docs/philosophy.md:55`** — the same carried-constraint model, in the same words, in the one
document that is owner-gated. A proposal to change it is already written and waiting:
`philosophy-changes-for-approval.md` § *Change 6*. Not touched. It is the sole reason the `SC-1`/`N65`
pattern is run against `$CANON_WIDE_NOPHIL` rather than `$CANON_WIDE`, and the exclusion has an
expiry: when Change 6 lands, drop it.

**Recheck § 4.4 — `docs/runtime/README.md:23`** ("plan execution and **constraint evaluation**"), and
**§ 4.5 — `graph-analyzer.md:538`** (`UnhandledEvent` at Warning). Neither is in this pass's scope and
neither is claimed here. § 4.5 in particular is not a wording fix: whether a declared-and-nowhere-
handled event blocks or reports is a judgement about the language, and the recheck was right not to
assert the answer. Both are still live at the line numbers the recheck gives.

**Recheck § 2.4's fault-backstop family** (R9–R14, C1, C2, N22–N30, N34, N35) — deliberately deferred
by the runtime and compiler slices for a reason both records give and this pass does not disturb.
Consequence, which the recheck asked to have said plainly and which is repeated here so it is on the
board in two places: **`precept-language-spec.md:110` states "There are no runtime fault traps" while
five canonical documents still specify them in operational detail.** Canon at HEAD contradicts itself
on a central claim of the guarantee. Three of the twelve rows executed above (`N31`, `N32`, `N33`)
were the `docs/language/` corner of that family and are now done, which is why pattern **P6** below is
scoped to `docs/language/`; run it over `$CANON` and it still returns the deferred family in the other
files, by design.

**`docs/glossary.md`** (recheck § 4.6) — outside `$CANON`, downstream of the spec, and the recheck
already recorded the debt. Not touched.

**One residue noticed and left**: `precept-language-spec.md:2096` and `runtime-api.md:199` describe
`InitialEventMissingAssignments` as meaning "post-construction state may violate constraints". Under
the corrected model the honest statement is that unassigned required fields leave the base case of the
induction open. It is a diagnostic gloss rather than a model statement, it is not a register row, and
rewriting a diagnostic's description is the kind of thing that belongs with the diagnostic's own
document. Flagged, not edited.

---

## 4. Every pattern, and the run

All thirteen were run together after the last edit. Twelve return zero; `P13` is an ADDITION and must
return more than zero.

```bash
cd "$(git rev-parse --show-toplevel)"
CANON="docs/language docs/compiler docs/runtime docs/philosophy.md"
CANON_WIDE="$CANON README.md docs/compiler-and-runtime-design.md"
CANON_WIDE_NOPHIL="docs/language docs/compiler docs/runtime README.md docs/compiler-and-runtime-design.md"

# P1  SC-1 + N65 rewritten — the carried-constraint discharge model, every spelling
/usr/bin/grep -rniE 'carr(y|ies|ying)[^.]{0,40}(a |its |the )?(sufficient|discharging|declared) constraint|carr(y|ies|ying)[^.]{0,20}(a |its |the )?(sufficient|discharging) fact|carries its constraint' $CANON_WIDE_NOPHIL

# P2  the three-source premise enumeration where there are four
/usr/bin/grep -rniE 'narrowed (in)?to (its|the) interval[^.]{0,120}guard|declared modifier/rule[^.]{0,120}statically-known|guard fact on the path[^.]{0,80}statically-known' $CANON_WIDE

# P3  N75 — notempty asserted as collection cardinality
/usr/bin/grep -rni "notempty" $CANON | /usr/bin/grep -iE 'also[^|]{0,20}(set|queue|stack|log|bag|list)|applies to[^|]{0,60}(set|queue|stack|log|bag|list)|equivalent to[^|]{0,10}.?mincount|count *(≥|>=|>) *[01][^|]{0,20}on collection|notempty[^|]{0,60}(discharges|satisfies)[^|]{0,40}(non-empt|cardinal)'

# P4  N41 — a numbered design principle claiming overflow without the exemption
/usr/bin/grep -nE '^[0-9]+\. \*\*.*overflow' docs/language/precept-language-spec.md | /usr/bin/grep -v 'exempted from this principle'

# P5  N43 — a flat overflow-is-an-error claim with no deferral on the same line
/usr/bin/grep -rniE 'overflow is (a type error|an error)' $CANON | /usr/bin/grep -viE 'defer'

# P6  N31/N32/N33 — an affirmative runtime-trap claim. Scoped to docs/language/: see § 3.
/usr/bin/grep -rniE 'defensive redundancy|runtime trap|runtime fault (check|trap)|\| *Runtime fault|is a runtime fault' docs/language | /usr/bin/grep -viE 'no runtime (fault )?(trap|check)|there is no runtime|do not exist|does not exist|never the primary|not the primary'

# P7  N40 — a declared or element bound described as a fault
/usr/bin/grep -rniE '(declared |element )bound[^.]{0,120}fault|fault[^.]{0,120}(declared |element )bound' docs/language | /usr/bin/grep -viE 'not a fault|neither list|rule preservation|rule shorthand|fault-prone|fault classes'

# P8  N74 — a temporal quantity taking a type from no context
/usr/bin/grep -rniE '\| *\(none\) *\|[^|]*(Period path|Duration path)|no context[^|]{0,40}\|[^|]{0,30}(period|duration) path' docs/language/temporal-type-system.md

# P9  N8 + recheck § 4.1 + § 4.2 — constraints, rules or ensures evaluated after the mutations
/usr/bin/grep -rniE '(post-mutation|post-assignment)[^.]{0,90}(constraint|rule|ensure|violation|truth|evaluat|sweep)|(constraint|rule|ensure|violation)[^.]{0,70}(post-mutation|post-assignment|against the finished|after the mutations complete)|before any constraint is evaluated' $CANON_WIDE | /usr/bin/grep -viE 'no post-mutation|not a post-mutation|there is no post|not re-evaluated|no longer|is not |never |does not|structurally impossible|nothing left|survives the removal|removal of the post|Nothing is evaluated|post-mutation (count )?interval|post-mutation value'

# P10 N19 (with N15, N20) — superseded constraint-failure outcome names
/usr/bin/grep -rniE 'RestoreConstraintsFailed|EventConstraintsFailed|UpdateConstraintsFailed' $CANON

# P11 N9 — collect-all described over a constraint-result set rather than ingress
/usr/bin/grep -rniE 'collect.all[^.]{0,80}(violation set|ConstraintResult|rules and ensures)|(violation set|ConstraintResult)[^.]{0,60}collect.all' $CANON | /usr/bin/grep -viE 'ingress|InvalidArgs|argument|patch'

# P12 recheck § 4.2 — whole-precept PRE0115 suppression stated as settled design
/usr/bin/grep -rniE 'skips the default-value satisfiability check|whole.precept (suppression|early.return)' $CANON | /usr/bin/grep -viE 'implementation gap|wider than|not settled design|per-field'

# P13 N59 ADDITION — must return MORE than zero
/usr/bin/grep -rn 'Non-linear rules are proven from a premise' docs/language/precept-language-spec.md
```

| Pattern | Row(s) | Expected | Result |
|---|---|---|---|
| P1 | `SC-1`, `N65` | zero | zero |
| P2 | `N62` (discharge-model half) | zero | zero |
| P3 | `N75` | zero | zero |
| P4 | `N41` | zero | zero |
| P5 | `N43` | zero | zero |
| P6 | `N31`, `N32`, `N33` | zero in `docs/language/` | zero |
| P7 | `N40` | zero | zero |
| P8 | `N74` | zero | zero |
| P9 | `N8`, recheck § 4.1, § 4.2 | zero | zero |
| P10 | `N19`, `N15`, `N20` | zero | zero |
| P11 | `N9` | zero | zero |
| P12 | recheck § 4.2 | zero | zero |
| P13 | `N59` (ADDITION) | more than zero | 1 |

---

## 5. For whoever merges this into the register

The register's commit column is being filled in by another agent and was not touched here. Two things
this pass owes it:

1. **Replace `SC-1`'s and `N65`'s recorded patterns with P1**, and record the exclusion of
   `docs/philosophy.md` together with its expiry (Change 6 in `philosophy-changes-for-approval.md`).
   The clause-matches those two rows carry today return zero against text that contains the thing
   they test for, which is worse than having no pattern.
2. **`N62` needs a second pattern.** The row's own question — whether the provenance implication
   survived — was answered correctly. The same sentence carried the discharge model, and nothing
   asked about that. P2 is the pattern for the half that was missed.

The rows executed here are: `N8`, `N9`, `N19`, `N31`, `N32`, `N33`, `N40`, `N41`, `N43`, `N59`,
`N74`, `N75`, plus `SC-1`, `N65`, `N62` (site correction), and `N15` / `N20` clear as a consequence of
`N19`. The two recheck findings with no row — § 4.1 and § 4.2 — are executed above and want rows of
their own.
