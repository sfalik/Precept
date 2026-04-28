# Access Mode Guard Design Round: Architectural Verdicts

**By:** Frank
**Date:** 2026-04-28
**Status:** Design verdicts — all questions resolved
**References:** `docs/language/precept-language-spec.md` (§ Access Mode Rules), `docs/working/catalog-parser-design-v7.md` (current parser design)

---

## Context

Shane raised three design questions about the access mode guard system during the `writable` field modifier work:

1. **A1 — Guarded `read`:** Should `read` be allowed as a guarded (conditional) access mode, symmetric with guarded `write`?
2. **A2 — Guarded `omit`:** Should `omit` be allowed as a guarded access mode?
3. **A3 — Vocabulary:** Should the access mode keywords (`read` / `write` / `omit`) be replaced with more declarative alternatives?

The current spec (rule 4) says: *"Guarded `write` is the only guarded access mode — `read` and `omit` cannot have guards."* Shane's analysis identified that the original rationale for this blanket prohibition was imprecise — it conflated two categorically different concerns (conditional mutability vs. conditional structural presence) under one sentence.

These verdicts update the access mode rule set and the vision doc sentence at line 823.

---

## A1: Guarded `read` — APPROVED (writable-only constraint)

### Both sides

**Case for:**

Shane's analysis is correct. The vision doc conflated two categorically different things under one sentence: "conditionally readable" (visibility uncertainty — field may or may not exist to read) and "conditionally writable" (mutability uncertainty — field exists, write access varies). Guarded `write` already proves conditional mutability is acceptable. A guarded `read` is the symmetric case — it's a downgrade guard rather than an upgrade guard. The field is always structurally present, always at minimum readable. The guard only toggles whether it's also mutable.

The two-layer model already has the conceptual machinery: field baseline sets the default, state-level overrides it. A guarded override that falls back to baseline on guard=false is no different from what guarded `write` already does — it's just going in the other direction.

**Case against:**

Complexity budget. Today the rule is dead simple: guards exist on `write` only. Opening guarded `read` means two guard directions (upgrade guards on `write`, downgrade guards on `read`), which increases the surface authors must learn and the type checker must handle. It also raises the question of whether both guarded `write` and guarded `read` can coexist for different fields in the same state (answer: yes, trivially, since declarations are per field/state pair — but it's still more cognitive load).

There's also a subtlety: guarded `read` is only meaningful when the field's effective unguarded mode is `write` (either via `writable` baseline or… no, there's no other way — the fallback is always to baseline, and the only baseline that makes `read` a non-redundant guard is `writable`). So this is a feature that only applies to `writable` fields, which makes it feel niche.

### Verdict: APPROVE with writable-only constraint

The logic is airtight. Shane correctly identified that the vision doc's reasoning was imprecise — it bundled a real concern (conditional structural presence) with a non-concern (conditional mutability) under one rule. The language should not prohibit something that is logically sound just because the original rationale was sloppy.

### Fallback semantics

**For a `writable` field:**

```
in Processing read Amount when not Locked
```

- Guard = true → field is `read` (state-level override wins)
- Guard = false → field falls back to field baseline → `write` (because `writable`)
- Meaning: "Amount is read-only in Processing when Locked is absent; writable otherwise"
- This is a **downgrade guard** — conditionally restricting mutability

**For a non-`writable` field:**

```
in Processing read Amount when not Locked
```

- Guard = true → field is `read`
- Guard = false → field falls back to field baseline → `read` (D3 default)
- Both branches resolve to `read` → the guard is vacuous → **compile error** (`RedundantGuardedRead`)
- Rationale: a guarded declaration that cannot change the outcome in either branch is dead code. Precept does not tolerate dead code in access declarations.

**Interaction with explicit `write` on same pair:**

`in Processing write Amount` + `in Processing read Amount when Cond` on the same (field, state) pair does **not** need a dedicated rule-7 conflict diagnostic. Rules 4b + 4c already make the pair structurally impossible as a valid combination: the guarded `read` form is only non-redundant when the field has `writable`, while the explicit `write` form is only non-redundant when the field does **not** have `writable`. One side or the other always collapses to `RedundantAccessMode` first.

### Proposed rule 4 revision (split into 4a/4b)

**Current rule 4:**
> Guarded `write` is the only guarded access mode — `read` and `omit` cannot have guards.

**Proposed replacement:**

> **4a.** Guarded `write` and guarded `read` are the only guarded access modes — `omit` cannot have a guard because conditional structural presence breaks static per-state field maps.
>
> **4b.** Guarded `read` requires a `writable` baseline — a guarded `read` on a field without the `writable` modifier is a compile error (`RedundantGuardedRead`) because both branches resolve to `read`.

### Tradeoff accepted

Slightly larger access mode surface (two guard directions vs one). Justified by logical consistency and by correcting an imprecise prohibition in the original design.

---

## A2: Guarded `omit` — STAYS PROHIBITED (wording revised for precision)

### Why `omit` is categorically different

The distinction is between **mutability** and **structural presence**:

| Access mode | What it controls | Field present in state? | Guard-safe? |
|---|---|---|---|
| `write` | Mutability (can write) | **Always yes** | ✅ Yes — field always exists, guard toggles mutability |
| `read` | Mutability (read-only) | **Always yes** | ✅ Yes — field always exists, guard toggles mutability |
| `omit` | Structural presence | **No — field is absent** | ❌ No — see below |

`omit` does not merely restrict access — it **removes the field from the state's data shape**. The field's value is reset to default on state entry (rule 5), `set` targeting it is a compile error (rule 6), and downstream consumers (type checker, tooling, integrations) treat it as structurally absent.

If `omit` could be guarded:

```precept
in Archived omit InternalNotes when SensitiveFlag    ← hypothetical
```

The type checker must answer: "Does `InternalNotes` exist in the `Archived` state?" The answer is "sometimes" — which is not an answer. Every downstream consumer breaks:

1. **Type checker** — cannot build a static per-state field map. The (field, state) → access mode resolution becomes data-dependent, not declaration-dependent. The entire closed-world access model collapses for that pair.

2. **`set` validation (rule 6)** — `set InternalNotes = ...` in an event targeting `Archived` would be sometimes-valid, sometimes-error depending on runtime data. This cannot be checked at compile time.

3. **`omit` clears on state entry (rule 5)** — the runtime must decide at transition time whether to clear the field. But the guard may depend on data that's being mutated in the same transition's action chain. Evaluation order becomes ambiguous.

4. **Tooling and integration** — form renderers, MCP inspect output, and external integrations derive their field presence maps from the static (field, state) matrix. Conditional presence makes these maps data-dependent, which defeats the purpose of declaring access modes at all.

5. **Constraint evaluation** — `ensure` and `rule` declarations reference fields. If a field is conditionally absent, constraint evaluation must handle a third state beyond "has value" and "has default" — namely, "structurally doesn't exist right now." This infects every constraint that touches the field.

### The core principle

`write` and `read` live on the **mutability axis** — the field is always there, always has a value, always participates in constraints and sets. `omit` lives on the **presence axis** — the field is structurally gone. Conditional mutability is fine. Conditional existence is a category error.

### Verdict: STAYS PROHIBITED — wording updated

The current wording in both the spec and vision doc is imprecise:

> `read` and `omit` cannot be guarded because "conditionally readable" and "conditionally absent" break static guarantees.

This bundles two unrelated claims. "Conditionally readable" does NOT break static guarantees (as A1 establishes — the field is still always present). "Conditionally absent" DOES break static guarantees. The wording must separate them. The revised text is captured in the rule 4a/4b split above and the vision doc sentence revision below.

---

## A3: Vocabulary — KEEP `read` / `write` / `omit`

### The concern

The concern is real. `in Processing write Amount` can be parsed as imperative ("write Amount in Processing state") rather than declarative ("Amount is writable in Processing state"). The ambiguity exists in isolation.

But it evaporates in context. Three factors kill it:

1. **The `in <State>` prefix is a declaration scope.** It's not `write Amount in Processing` (imperative with postfix location). It's `in Processing write Amount` (scoped declaration). The `in` keyword front-loads the scope, establishing declarative context before the verb appears. No operation in Precept starts with `in <State>`.

2. **Structural position.** Access modes appear at the precept body level alongside other declarations (field declarations, state declarations, event declarations). They are not inside event hooks or action chains where operations live. An author reading a precept file sees access modes grouped with declarations, not with mutations.

3. **The `writable` modifier creates a bridge.** Authors who declare `writable Amount : money` already use a declarative adjective for the same concept. `in Processing write Amount` is the state-scoped refinement of that same concept. The connection is intuitive.

### Options evaluated

| Option | Example | Key pro | Key con |
|---|---|---|---|
| A: `readable`/`writable`/`omit` | `in Processing writable Amount when not Locked` | Consistent with field modifier | `writable` keyword collision between modifier and access verb — same keyword, different positions, different semantics |
| B: `view`/`edit`/`omit` | `in Processing edit Amount when not Locked` | Avoids verb ambiguity | Abandons POSIX/SQL/REST `read`/`write` precedent; `view` implies rendering, not data access |
| C: `readonly`/`writable`/`omit` | `in Processing writable Amount when not Locked` | Both clearly adjectives | Same `writable` collision; `readonly` is compound; asymmetric parts of speech |
| D: Keep `read`/`write`/`omit` | `in Processing write Amount when not Locked` | Verb-triple symmetry; no collisions; already in spec, all samples, all docs | Mild operation-verb ambiguity in isolation |

### Verdict: KEEP `read` / `write` / `omit` (Option D)

The concern is valid in the abstract but not in practice. Every alternative introduces worse problems than the mild ambiguity it solves:

- Options A and C create a `writable`-keyword collision between field modifier and access verb.
- Option B abandons the verb-triple symmetry the vision doc deliberately chose.
- All alternatives require rewriting the spec, vision doc, all 20 sample files, all documentation, the type checker's diagnostic messages, the MCP output format, and the language server's completions and hover text.

The cost-benefit ratio is terrible. The `in <State>` prefix, the structural position, and the `writable` modifier bridge are sufficient to establish declarative context. Accept the mild ambiguity. No changes needed.

---

## Updated Grammar Snippet: Access Mode Section

```
in StateTarget write FieldTarget ("when" BoolExpr)?   ← state-scoped guarded write (upgrade)
in StateTarget read FieldTarget ("when" BoolExpr)?     ← state-scoped guarded read (downgrade)
in StateTarget omit FieldTarget                         ← state-scoped unguarded omit
```

The grammar now has two guarded lines (`write` and `read`) and one unguarded line (`omit`). This replaces the previous shape of one guarded line (`write`) and two unguarded lines (`read`, `omit`).

---

## Updated Vision Doc Sentence (line 823)

**Current:**
> This is the only conditional access mode — `read` and `omit` cannot be guarded because "conditionally readable" and "conditionally absent" break static guarantees.

**Proposed:**
> `write` and `read` are the two conditional access modes. Guarded `write` upgrades a `read` baseline to `write` when the guard holds; guarded `read` downgrades a `writable` baseline to `read` when the guard holds. In both cases the field is always structurally present — only mutability varies. `omit` cannot be guarded because conditional structural presence (field sometimes exists, sometimes doesn't) breaks static per-state field maps, form rendering, and integration contracts.

---

## Redundancy Rule Decision — 2026-04-28 (Shane)

**Decision confirmed by Shane:** "lets keep it consistent."

Redundant access mode declarations are a compile error (`RedundantAccessMode`), consistent with the `RedundantModifier` precedent. Specifically:

**Unguarded redundant cases (rule 4c):**
- `in S write F` where `F` has `writable` → error: `write` is already the field's baseline; the declaration changes nothing.
- `in S read F` where `F` lacks `writable` → error: `read` is already the D3 default for that pair; the declaration changes nothing.

**Guarded redundant case (rule 4b, same principle):**
- `in S read F when Guard` where `F` lacks `writable` → error: both branches resolve to `read`; the guard is vacuous.

**Diagnostic code:** `RedundantAccessMode` — a single code covering all three cases. The previously designed-but-never-added `RedundantGuardedRead` name is superseded; it never existed in the enum.

**`omit` exemption:** `omit` is exempt from all redundancy checks. It operates on structural presence, not mutability — it always changes the effective shape of the state regardless of the field's baseline mode.

**`all` form policy (decided by Frank):** `in <State> read all` and `in <State> omit all` are exempt from redundancy checking. A broadcast declaration's effective change depends on the current field population; punishing it for being partially vacuous would create declarations that break silently as fields are added or removed. Named-field forms — where intent is precise and the effective mode is deterministic — get strict checking.

**Severity:** Error, matching `RedundantModifier`'s philosophy that a declaration changing nothing is a design error, not a style warning.

This decision is now closed. It also resolves the former rule 7 question: the supposed `write` + guarded-`read` conflict is structurally precluded by the redundancy rules already in place.

---

## Rule 7 Closed: Unguarded Write + Guarded Read on Same Pair

**Closed on 2026-04-28.**

The former Rule 7 conflict question is now resolved: this combination is **structurally precluded** by rules 4b + 4c, so no dedicated `ConflictingAccessModes` diagnostic is needed for this case.

Consider the hypothetical pair:

```precept
writable Amount : money
in Processing write Amount
in Processing read Amount when Locked
```

The two declarations cannot both be valid simultaneously:

- `in Processing read Amount when Locked` is only non-redundant under **rule 4b** when `Amount` has the `writable` modifier.
- `in Processing write Amount` is only non-redundant under **rule 4c** when `Amount` does **not** have the `writable` modifier.

Those preconditions are mutually exclusive. On any given field, one declaration or the other is necessarily dead code and is rejected as `RedundantAccessMode` before a Rule 7 conflict can ever form.

**Conclusion:** `ConflictingAccessModes` is **not needed for this specific write + guarded-read scenario**. The diagnostic still remains in `DiagnosticCode.cs` and may be needed for other conflict scenarios; this decision does **not** remove it from the enum.

---

## Pending: Runtime Feasibility

George's runtime feasibility check on guarded `read` fallback resolution is pending. The question: does the evaluator handle downgrade guards cleanly, or does the current guard machinery assume upgrade-only? This is an implementation concern, not a design concern — the semantics are clear regardless.

---

## Summary of Verdicts

| ID | Question | Verdict | Key rationale |
|---|---|---|---|
| A1 | Guarded `read` | **APPROVED** (writable-only) | Vision doc conflated mutability and presence. Guarded `read` is a symmetric downgrade guard. Compile error if field isn't `writable` (vacuous guard). |
| A2 | Guarded `omit` | **STAYS PROHIBITED** | `omit` is structural presence, not mutability. Conditional presence breaks type checker, `set` validation, clear-on-entry, tooling, and constraints. Wording revised to separate the two concerns. |
| A3 | Vocabulary | **KEEP** `read`/`write`/`omit` | Alternatives are worse. `in <State>` prefix provides declarative context. `writable` modifier bridge is intuitive. Verb-triple symmetry is deliberate. |
| A4 | Redundancy rule | **COMPILE ERROR** (`RedundantAccessMode`) | Shane confirmed consistent with `RedundantModifier`. Named-field declarations that resolve to baseline are dead code. `omit` exempt (presence axis). `all` forms exempt (broadcast declaration). |
| A5 | Rule 7 (`write` + guarded `read`) | **CLOSED — structurally precluded** | Rules 4b + 4c have mutually exclusive preconditions, so one side becomes `RedundantAccessMode` before any same-pair conflict can form. `ConflictingAccessModes` is not needed for this case. |

---

## B1: Access Mode Vocabulary — Proposal B

**Date:** 2026-05-01
**Status:** Open — recommendation pending Shane sign-off
**Context:** Shane rejected A3's "keep `read`/`write`" verdict. The concern is more fundamental than the A3 analysis acknowledged. This section restarts the vocabulary question from first principles.

---

### The Problem, Stated Precisely

A3 dismissed the operation-verb objection with three contextual arguments: the `in <State>` prefix establishes declarative scope; structural position disambiguates; the `writable` modifier bridges. Shane's response was unambiguous: these arguments are insufficient. The words are semantically wrong for a declarative language, regardless of context.

This is not a parsing concern or a context-inferability concern. It is a **semantic identity concern**. In Precept, access mode declarations are facts about what a field IS in a state context — not instructions to perform an operation on a field. `read` and `write` are the fundamental I/O verbs of computing. They do not describe what a field IS. They describe what a process DOES.

Shane is right. The A3 analysis was pragmatic but philosophically lazy. The correct starting point is: what kind of word is `omit`, and why does it work?

---

### What Makes `omit` Work

`omit` is grammatically a transitive verb. In the pattern `in State omit Field`, the reader mentally constructs: *"In [State], [the field] [Field] is omitted."* It reads as a declaration about what the field IS in that state context — its structural disposition.

Why does it feel declarative rather than operational?

**1. Word class: configuration/editorial verb, not I/O verb.**
`omit` belongs to the class of verbs used for *textual and structural composition*: include, exclude, omit, skip, elide, suppress. These verbs describe the structure of a document or configuration, not the execution of a computation. "Omit the introduction" is a directive about the document's shape, not a runtime operation. This is the opposite of I/O verbs (`read`, `write`, `fetch`, `store`, `process`), which describe operations performed at runtime.

**2. Aspectual character: telic/resultative, not process-oriented.**
In aspect theory, verbs describe either ongoing processes or achieved endpoints. `omit` is telic — it points to the RESULT STATE (absence) rather than a process (the act of omitting). "In Archived, omit Notes" = "Notes shall be absent from Archived." The result is the entire meaning. There is no duration, no intermediate state, no observable process.

`write` and `read` are quintessentially process verbs. "Read Amount" = the act of reading. "Write Amount" = the act of writing. The process is the meaning. This is why they fail as declarative configuration keywords.

**3. The `in <State>` prefix shifts `omit` toward the declarative — but doesn't rescue `write`/`read`.**
A3 argued that `in <State>` provides declarative context. This is partially true for `omit` because `omit` already has a declarative/compositional character. It is *not* true for `write` and `read`. "In Processing, write Amount when DocumentsVerified" still reads as: "when in Processing, perform a write to Amount if DocumentsVerified." The `in <State>` prefix does not neutralize an I/O verb. It cannot. The semantic weight of `write` as a computational operation is too strong.

**The criterion that falls out of this analysis:**
A replacement vocabulary must consist of words whose primary semantic identity is *structural/dispositional* rather than *operational*. Specifically:
- The word must describe what the field **IS** in the state context, not what the engine or caller **DOES** to the field.
- The word must be drawn from a configuration/structural vocabulary class, not a computational/I/O vocabulary class.
- Ideally, all three keywords belong to the same semantic family.

---

### Syntactic Position Constraint

The grammar is `in StateTarget KEYWORD FieldTarget ("when" BoolExpr)?`. The keyword occupies a fixed position between the state target and the field target. This means the keyword must function as the main predicate of a declarative clause: *"In [State], [KEYWORD] [Field] (when [Guard])"*.

Two grammatical archetypes work in this position:

**A. Predicate-verb form (directive):** The keyword is a directive verb that prescribes the field's structural role. "In Approved, [seal] Amount." The subject is implicit (the precept / the engine). The verb describes the field's configuration.

**B. Predicate-adjective form (descriptive):** The keyword is an adjective naming the field's property. "In Approved, [fixed] Amount." The reader inserts an implicit copula: "Amount is [fixed]."

Both forms can satisfy the declarative criterion. Form A requires the verb to be from a structural/configurational class, not an I/O class. Form B is inherently non-operational (adjectives describe states, not actions). Mixing forms A and B within the triplet (`omit` is Form A; most adjective candidates are Form B) creates a vocabulary family coherence question — acknowledged but not automatically fatal.

---

### Candidate Vocabularies

#### Candidate 1: `lock` / `unlock` / `omit`

**Semantics:**
- `unlock` = upgrade: field has read-only baseline, writable in this state
- `lock` = downgrade: field has writable baseline, read-only in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft unlock Amount` |
| Upgrade, guarded | `in Processing unlock Amount when not Finalized` |
| Downgrade, unguarded | `in Approved lock Amount` |
| Downgrade, guarded | `in Processing lock Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, unlock Amount" → "Amount is unlocked [for editing] in Draft" ✓
> "In Processing, unlock Amount when not Finalized" → "Amount is unlocked in Processing when not Finalized" ✓
> "In Approved, lock Amount" → "Amount is locked [from editing] in Approved" ✓
> "In Processing, lock Amount when FraudFlag" → "Amount is locked in Processing when FraudFlag" ✓

**Declarative character:** Strong. `lock` and `unlock` are configuration verbs — they describe a field's structural state, not a computation performed on it. "Locked" is a property a field has; it is not a process. Domain vocabulary confirms this: "locked account", "locked record", "locked price", "unlock for editing" are all standard business usage. The `in <State>` prefix further anchors the declaration reading.

**Vocabulary family coherence:** `lock`, `unlock`, and `omit` are all structural/configurational verbs. All three describe structural properties, not I/O operations. The family is coherent — an author sees three verbs that configure what a field IS in a state.

**Technical concerns:**
- Two new tokens: `Lock`, `Unlock`. No existing keyword conflicts in the current `Tokens` catalog.
- `lock` and `unlock` are not reserved identifiers in any common .NET or domain vocabulary that would conflict with field or state names.
- `Lock` in C# is a keyword — but Precept identifiers are not C# identifiers; the lexer is independent.
- Clear symmetry makes parser rule extension straightforward.

**Assessment:** Strong candidate. Both words are symmetric antonyms, both are structural vocabulary, both pass the declarative test. The one conceptual wrinkle: the read-only baseline isn't "locked" per se — it's just the default. Does "unlock" imply a prior locking that didn't exist? In practice, no: users understand "unlock for editing" as a permissions declaration, not a history claim.

---

#### Candidate 2: `editable` / `fixed` / `omit`

**Semantics:**
- `editable` = upgrade: field is editable (writable) in this state
- `fixed` = downgrade: field is fixed (read-only) in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft editable Amount` |
| Upgrade, guarded | `in Processing editable Amount when not Finalized` |
| Downgrade, unguarded | `in Approved fixed Amount` |
| Downgrade, guarded | `in Processing fixed Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, editable Amount" → "In Draft, Amount [is] editable" ✓
> "In Processing, editable Amount when not Finalized" → "Amount is editable in Processing when not Finalized" ✓
> "In Approved, fixed Amount" → "In Approved, Amount [is] fixed" ✓✓
> "In Processing, fixed Amount when FraudFlag" → "Amount is fixed in Processing when FraudFlag" ✓✓

**Declarative character:** Excellent for both. These are predicate adjectives — the most unambiguously declarative part of speech. No risk of operational interpretation. "Fixed Amount" is a natural domain phrase (fixed fee, fixed price, fixed amount). "Editable" is clear, if slightly programmer-adjacent.

**Vocabulary family coherence:** `editable` and `fixed` are both adjectives; `omit` is a verb. The family is not fully homogeneous. This is a real coherence cost. However, the *semantic* family is coherent: all three describe structural properties of a field in a state.

**Technical concerns:**
- Two new tokens: `Editable`, `Fixed`.
- `fixed` is contextually overloaded in natural language (repair: "I fixed the bug"; stable: "fixed price"). In the access mode context the meaning is clear, but the word carries noise.
- `editable` ends in `-able`, which is fine as English but slightly unusual for a DSL keyword.
- In the pattern `in State editable Field`, the adjective-before-noun structure requires the reader to insert an implicit copula. This works grammatically but is slightly less idiomatic than verb-form declarations.

**Assessment:** Excellent declarative character, at the cost of mild part-of-speech asymmetry with `omit` and minor `fixed` overloading. If Shane prioritizes "no verb feel at all" over vocabulary family coherence, this is the strongest option.

---

#### Candidate 3: `open` / `lock` / `omit`

**Semantics:**
- `open` = upgrade: field is open (writable) in this state
- `lock` = downgrade: field is locked (read-only) in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft open Amount` |
| Upgrade, guarded | `in Processing open Amount when not Finalized` |
| Downgrade, unguarded | `in Approved lock Amount` |
| Downgrade, guarded | `in Processing lock Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, open Amount" → "Amount is open [to editing] in Draft" — works, slightly informal
> "In Processing, open Amount when not Finalized" → "Amount is open when not Finalized" — works
> "In Approved, lock Amount" → "Amount is locked in Approved" ✓✓

**Declarative character:** `lock` is strong. `open` is weaker — it works, but it implies "open for business / open for access" rather than describing a precise access mode. "Open" in software contexts has many meanings (open a file, open a connection, open source, open ticket), which creates noise even if the structural reading is inferable from context.

**Vocabulary family coherence:** `open` and `lock` are not conventional antonyms in English (`open` ↔ `closed`; `locked` ↔ `unlocked`). This asymmetry breaks the vocabulary coherence that makes a keyword pair memorable and learnable.

**Assessment:** `lock` is excellent but pairing it with `open` creates asymmetry that would frustrate authors. A good keyword vocabulary should be predictable: if you know one member, you should be able to guess its partner. `open`/`lock` fails this test. Ranked lower than `lock`/`unlock`.

---

#### Candidate 4: `variable` / `fixed` / `omit`

**Semantics:**
- `variable` = upgrade: field is variable (writable/changeable) in this state
- `fixed` = downgrade: field is fixed (read-only/immutable) in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft variable Amount` |
| Upgrade, guarded | `in Processing variable Amount when not Locked` |
| Downgrade, unguarded | `in Approved fixed Amount` |
| Downgrade, guarded | `in Processing fixed Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, variable Amount" → "Amount is variable in Draft" — domain-natural for finance (variable rate / variable fee)
> "In Approved, fixed Amount" → "Amount is fixed in Approved" — domain-natural (fixed fee, fixed price)

**Declarative character:** Both are predicate adjectives. No operational reading possible. In financial/business domains, "variable" and "fixed" are established antonyms (variable-rate vs. fixed-rate, variable cost vs. fixed cost). The vocabulary is extremely natural for domain experts in those domains.

**Critical concern:** `variable` is a foundational programming concept — a named storage location. Developers authoring precepts will feel immediate confusion: "variable Amount" sounds like Amount is being declared as a variable. The cognitive dissonance is real and unavoidable. Precept's primary author is a domain expert, not a developer — but in practice, developers write precepts too, and this will trip them every time.

**Assessment:** `fixed` is excellent. `variable` is perfect for pure domain experts but creates programmer-vocabulary interference. The risk is non-trivial given that Precept is a developer-tool embedded in a .NET application. Ranked below `lock`/`unlock` and `editable`/`fixed`.

---

#### Candidate 5: `freeze` / `open` / `omit`

**Semantics:**
- `open` = upgrade: field is open (writable) in this state
- `freeze` = downgrade: field is frozen (read-only) in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft open Amount` |
| Upgrade, guarded | `in Processing open Amount when not Finalized` |
| Downgrade, unguarded | `in Approved freeze Amount` |
| Downgrade, guarded | `in Processing freeze Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Approved, freeze Amount" → "Amount is frozen in Approved" ✓✓ — excellent domain vocabulary (frozen assets, frozen account, frozen price)
> "In Draft, open Amount" → "Amount is open in Draft" — acceptable

**Declarative character:** `freeze` is excellent — "frozen" is a result state, not a process description. "Frozen assets" is natural domain language. `open` is weaker (see Candidate 3).

**Vocabulary family coherence:** The antonym of `freeze` is `thaw`, not `open`. The pair is semantically asymmetric. A vocabulary built on `open`/`freeze` would leave authors wondering why the words aren't paired antonyms.

**Assessment:** `freeze` as the downgrade keyword is stronger than `lock` in some domain vocabularies. But the upgrade pair problem is severe. Would need `freeze` / `thaw` to have semantic coherence, and `thaw` is not natural in business domains ("In Draft, thaw Amount" sounds like refrigerator maintenance).

---

#### Candidate 6: `allow` / `restrict` / `omit`

**Semantics:**
- `allow` = upgrade: editing is allowed on this field in this state
- `restrict` = downgrade: editing is restricted on this field in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft allow Amount` |
| Upgrade, guarded | `in Processing allow Amount when not Locked` |
| Downgrade, unguarded | `in Approved restrict Amount` |
| Downgrade, guarded | `in Processing restrict Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, allow Amount" → "Amount is allowed [to be edited] in Draft" — reads as a permission grant ✓
> "In Approved, restrict Amount" → "Amount is restricted [from editing] in Approved" ✓

**Declarative character:** Both are directive verbs that describe permissions/access. These are permission-model verbs (ACL vocabulary), not I/O verbs. Shane explicitly mentioned POSIX ACLs as a negative example. This family belongs to the ACL vocabulary domain, not the domain-integrity declaration domain. The language would start reading like a permissions system, not a declarative contract.

**Assessment:** Declarative in form, but semantically wrong for Precept's identity. Precept is not a permissions system. Access modes describe what a field IS structurally in a state, not who has permission to do what. The ACL framing undercuts the governance narrative.

---

#### Candidate 7: `editable` / `sealed` / `omit`

**Semantics:**
- `editable` = upgrade: field is editable in this state
- `sealed` = downgrade: field is sealed (read-only) in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft editable Amount` |
| Upgrade, guarded | `in Processing editable Amount when not Finalized` |
| Downgrade, unguarded | `in Approved sealed Amount` |
| Downgrade, guarded | `in Processing sealed Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, editable Amount" → "Amount is editable" ✓
> "In Approved, sealed Amount" → "Amount is sealed" — legal/formal, excellent for regulated domains

**Declarative character:** Both are adjectives. Unambiguously declarative. `sealed` is particularly strong in legal and regulated contexts: sealed records, sealed bids, sealed testimony. It conveys permanence and formal closure — exactly what a read-only field in an approved or finalized state means.

**Critical concern:** `sealed` has very strong "permanent/final" connotations. A `sealed` record cannot be opened. But in Precept, a field `sealed` in one state may be `editable` in another. The permanence implication of `sealed` creates a misleading mental model — the field is read-only here, not sealed forever.

**Assessment:** `sealed` is excellent in pure finalization contexts (closed, paid, archived states) but misleading in conditional read-only contexts. `editable` is strong. The pair's semantic coherence is weak — `editable` ↔ `not editable`, but the natural antonym of `sealed` is `open`, not `editable`.

---

#### Candidate 8: `mutable` / `immutable` / `omit`

**Semantics:**
- `mutable` = upgrade: field is mutable in this state
- `immutable` = downgrade: field is immutable in this state
- `omit` = structural exclusion

**Examples:**

| Form | Example |
|---|---|
| Upgrade, unguarded | `in Draft mutable Amount` |
| Upgrade, guarded | `in Processing mutable Amount when not Locked` |
| Downgrade, unguarded | `in Approved immutable Amount` |
| Downgrade, guarded | `in Processing immutable Amount when FraudFlag` |
| Omit | `in Archived omit Notes` |

**Reading aloud:**
> "In Draft, mutable Amount" → "Amount is mutable in Draft" — technically precise, programmer-y
> "In Approved, immutable Amount" → "Amount is immutable in Approved" — technically precise

**Declarative character:** Perfect — both are adjectives, entirely declarative, no operational connotation. These are the canonical computer-science terms for the concept.

**Assessment:** Technically correct but violates the "English-ish" constraint. These are programmer vocabulary, not domain-expert vocabulary. `mutable`/`immutable` belong in Java/C# API documentation, not in a business-facing declarative DSL. A domain expert authoring a Refund Request precept should not need to know what `mutable` means. Hard fail on English-ish.

---

### Ranking

| Rank | Triplet | Strength | Key issue |
|---|---|---|---|
| 1 | `lock` / `unlock` / `omit` | Symmetric antonyms; structural vocabulary; no I/O smell; natural domain language; all three from same verb family | "unlock" implies prior lock that wasn't explicit — minor conceptual imprecision |
| 2 | `editable` / `fixed` / `omit` | Adjectives = maximally declarative; excellent domain vocabulary; `fixed Amount` is perfect English | Adjective/verb coherence cost with `omit`; `fixed` has minor repair-meaning noise |
| 3 | `variable` / `fixed` / `omit` | Perfect domain antonyms (finance/business); both adjectives | `variable` conflicts with programmer-vocabulary (named storage location) |
| 4 | `editable` / `sealed` / `omit` | Both adjectives; `sealed` excellent in regulated contexts | `sealed` implies permanence; weak antonymic coherence with `editable` |
| 5 | `open` / `lock` / `omit` | `lock` is excellent; `open` is natural | `open`/`lock` aren't antonyms; asymmetric vocabulary |
| 6 | `freeze` / `open` / `omit` | `freeze` excellent domain language | `freeze`/`open` aren't antonyms; proper pair would be `freeze`/`thaw` (unnatural for business) |
| 7 | `allow` / `restrict` / `omit` | Both directive, no I/O smell | ACL/permissions vocabulary — wrong semantic frame for Precept |
| 8 | `mutable` / `immutable` / `omit` | Perfect technical precision | Programmer vocabulary, not domain vocabulary; fails English-ish |

---

### Recommendation

**Primary recommendation: `lock` / `unlock` / `omit`**

This triplet is the strongest because it satisfies every criterion simultaneously:

**1. Declarative — yes.**
`lock` and `unlock` describe structural states, not computational operations. "In Approved, lock Amount" declares that Amount IS locked — not that the engine is performing a lock operation. The word `lock` belongs to the configuration/structural vocabulary class: "lock the record", "lock the price", "lock the settings" are all structural declarations, not runtime operations.

**2. English-ish — yes.**
Both words are common English vocabulary that domain experts use without hesitation. "Locked account", "locked price", "locked for editing", "unlock for review" — these phrases appear in business documentation constantly. A business analyst or domain expert reading a precept with `lock`/`unlock` will have no vocabulary trouble.

**3. Symmetric with `omit` — yes.**
All three (`lock`, `unlock`, `omit`) are configuration/structural verbs describing what a field IS in a state context. None is an I/O verb. None describes a process performed on a field. The vocabulary family is coherent.

**4. Guarded forms read naturally — yes.**
- `in Processing unlock Amount when not Finalized` → "Amount is unlocked in Processing when not Finalized"
- `in Approved lock Amount when FraudFlag` → "Amount is locked in Approved when FraudFlag"
Both read as structural declarations about the field's access disposition under a condition.

**5. Conceptual model accuracy — strong.**
Fields in Precept are read-only by default — they are, conceptually, "locked" from direct mutation. The `writable` modifier is the key that makes a field eligible to be unlocked. `unlock` in a state declaration says: "in this state, the key is in the lock — the field is open for editing." `lock` in a state declaration says: "in this state, even though this field normally accepts edits, it is locked here." The vocabulary maps accurately to the underlying model.

**The one wrinkle acknowledged:**
`unlock` implies a prior explicit locking that didn't happen — the read-only default is just the default, not the result of a `lock` declaration. This is a minor imprecision. In practice, domain users do not interpret "unlock" as requiring a prior lock event. They read it as "editable here." This is a conceptual cost I accept.

---

**Alternate recommendation: `editable` / `fixed` / `omit`**

If Shane is bothered by any residual verb-feel in `lock`/`unlock`, the adjective pair `editable`/`fixed` is the next strongest choice. Both are unambiguously descriptive rather than directive. "In Approved, fixed Amount" is natural business English and carries no operational connotation whatsoever. "In Processing, editable Amount when not Finalized" is clear.

The cost: `omit` is a verb; `editable` and `fixed` are adjectives. The vocabulary is not from a single grammatical family. This coherence cost is real but not catastrophic — all three still satisfy the declarative criterion, which is the core objection.

---

### Impact Assessment (for implementation planning)

Any vocabulary change requires synchronized updates across all three impact categories:

**Runtime:**
- Tokens catalog: replace `Write`/`Read` with new token(s)
- Lexer: new keyword scan branches
- Parser: update access mode grammar rule
- Type checker: update diagnostic message templates referencing `write`/`read`
- All tests referencing access mode keyword strings

**Tooling:**
- TextMate grammar (generated from Tokens catalog — update catalog, regenerate)
- Language server completions for access mode position
- Hover text for access mode keywords
- Semantic token classification

**MCP:**
- `precept_language` vocabulary output
- `precept_compile` access mode serialization in DTOs
- Sample files (all 20 `.precept` files using `write`/`read`)
- Language spec, vision doc, working docs

This is a significant cross-cutting rename. The implementation effort is non-trivial. The vocabulary change should be treated as a language surface revision requiring a dedicated implementation plan before any coding begins.

---

## B2: Vocabulary Consistency Round

**By:** Frank
**Date:** 2026-04-28
**Status:** Recommendation presented to Shane for sign-off
**Supersedes:** B1 recommendation (`lock`/`unlock`/`omit`)

---

### 1. The Exact Problem with B1

B1 recommended `lock`/`unlock`/`omit`. The verbs pass every criterion in isolation: structural, declarative, symmetric antonyms, natural domain language. But B1 only evaluated the access mode verb position — `in S VERB F`. It did not evaluate the field-declaration modifier position.

The modifier position is the other half of the vocabulary surface. Today, field declarations use `writable`:

```
field Amount as decimal writable
```

If we adopt `lock`/`unlock`, the corresponding modifier would be `unlocked`:

```
field Amount as decimal unlocked
```

This fails. `unlocked` carries a participial/resultative reading — it implies something WAS locked and then someone unlocked it. On a field declaration, there is no prior lock. The field is being declared for the first time. The word should name a design-time disposition ("this field accepts direct edits"), not describe a state transition that already happened ("this field was freed from a lock that was never there").

Compare: `writable` doesn't imply the field was previously unwritable and then made writable. It names a static property. `unlocked` does imply a prior lock event.

The mismatch: `writable`/`editable`/`open` are **dispositional adjectives** — they name what something IS. `unlocked` is a **resultative participle** — it names what happened TO something. Dispositional adjectives belong on field declarations. Resultative participles do not.

This is not a preference call. It's a genre mismatch. Field declarations are definitional. They describe the permanent shape of an entity's data model. The modifier vocabulary must be definitional too.

### 2. The New Constraint: Vocabulary Family Consistency

Shane's feedback adds a constraint B1 didn't account for:

> The field modifier and the access mode verbs must share the same root word or be from the same semantic family.

Today, `writable` ↔ `write` share the root "write." A developer sees `writable` on a field and `write` in a state rule and immediately understands the connection. Any replacement vocabulary must preserve this property.

This means we are not looking for "the best access mode verbs" and "the best field modifier" independently. We are looking for a **single vocabulary family** that works across both surfaces simultaneously:

- **Surface 1 (field declaration):** `field Amount as decimal [MODIFIER]` — adjective/participial that names the design-time mutability disposition
- **Surface 2 (access mode rule):** `in S [KEYWORD] F` — keyword that names the per-state access disposition

The family connection must be obvious to a developer reading Precept for the first time. If you need a glossary to see why the modifier and the verbs are related, the vocabulary fails.

### 3. Key Design Insight: Modifier-as-Upgrade-Keyword

The current model already contains this pattern: `writable` (modifier) ↔ `write` (upgrade verb). The modifier is the adjective form of the upgrade keyword. The downgrade keyword (`read`) is a separate word that names the opposite disposition.

This generalizes: the strongest family design uses the **same word** for the modifier and the upgrade keyword. The field declaration says "this field is X" and the state rule says "in this state, this field is X." The tautological connection costs zero learning.

The downgrade keyword then only needs to be a **clear antonym** of the modifier — readers will infer the family connection from opposition.

### 4. Candidate Vocabulary Families

All candidates must supply both surfaces. A candidate that solves only the verb problem is incomplete.

---

#### Family 1: `editable` / `editable` + `fixed` + `omit`

**Modifier:** `field Amount as decimal editable`
**Upgrade:** `in Draft editable Amount`
**Downgrade:** `in Approved fixed Amount`
**Guarded upgrade:** `in Processing editable Amount when not Finalized`
**Guarded downgrade:** `in Processing fixed Amount when FraudFlag`
**Omit:** `in Archived omit Notes`

**Reading aloud:**
> `field Amount as decimal editable` → "Amount is a decimal that is editable" ✓ — dispositional, design-time
> `in Draft editable Amount` → "Amount is editable in Draft" ✓ — natural predicate
> `in Approved fixed Amount` → "Amount is fixed in Approved" ✓ — "fixed price", "fixed rate", "fixed fee"
> `in Processing fixed Amount when FraudFlag` → "Amount is fixed in Processing when FraudFlag" ✓

**Family connection:** `editable` appears identically in both surfaces — the connection is tautological. `fixed` is an obvious antonym of `editable` in business English. A developer reading `field Amount as decimal editable` and then `in Approved fixed Amount` immediately understands: the field is normally editable, but it's fixed here.

**Declarative character:** Both `editable` and `fixed` are adjectives. No operational or imperative reading is possible. "In Approved, fixed Amount" cannot be misread as a command — it reads as a declaration of state. This is MORE declarative than even `omit`, which has a verb form.

**Domain naturalness:** Both words are standard business English. "Fixed price" / "fixed income" / "fixed rate" — finance and business domains use `fixed` constantly. "Editable field" / "editable form" — natural for anyone who has used software.

**Risk: `fixed` "repair" meaning.** "I fixed the bug" uses `fixed` as a verb meaning "repaired." In the adjective position, this secondary meaning is suppressed — nobody reads "fixed price" as "repaired price." The DSL grammar position reinforces the adjective reading. Risk is negligible.

**Risk: grammatical heterogeneity with `omit`.** `editable` and `fixed` are adjectives; `omit` is a verb. The three access mode keywords are not grammatically homogeneous. However, `omit` is semantically distinct (structural exclusion vs. access disposition), so the grammatical distinction actually reinforces the semantic distinction. This is a feature, not a bug.

---

#### Family 2: `editable` / `editable` + `locked` + `omit`

**Modifier:** `field Amount as decimal editable`
**Upgrade:** `in Draft editable Amount`
**Downgrade:** `in Approved locked Amount`
**Guarded downgrade:** `in Processing locked Amount when FraudFlag`
**Omit:** `in Archived omit Notes`

**Reading aloud:**
> `field Amount as decimal editable` → "Amount is editable" ✓
> `in Approved locked Amount` → "Amount is locked in Approved" ✓ — "locked account", "locked record"

**Family connection:** `editable` is the modifier and upgrade keyword (tautological). `locked` is a clear antonym in context. The connection from `editable` to `locked` relies on opposition — developers will infer it, but it's not as immediately obvious as `editable` ↔ `fixed`, because `locked` carries the B1 structural-verb connotation that is tangential to `editable`.

**Declarative character:** `locked` is a participial adjective. "Amount is locked" is a state description, not a command. Declarative character is strong.

**Risk: the mirror of B1's problem.** Shane rejected `unlocked` as a modifier because the participial form implies a prior state change. `locked` has the same issue in the opposite direction — "locked Amount" implies someone locked it. In the access mode position (which IS state-scoped), this implication is less problematic than on a field declaration — fields in a specific state DO have something "happen to them" conceptually. But the asymmetry between `editable` (pure adjective) and `locked` (resultative participle) creates a mixed-register vocabulary.

**Assessment:** Workable but less clean than Family 1. The `editable`/`locked` pair is semantically coherent but grammatically asymmetric.

---

#### Family 3: `editable` / `editable` + `sealed` + `omit`

**Modifier:** `field Amount as decimal editable`
**Upgrade:** `in Draft editable Amount`
**Downgrade:** `in Approved sealed Amount`
**Guarded downgrade:** `in Processing sealed Amount when FraudFlag`
**Omit:** `in Archived omit Notes`

**Reading aloud:**
> `field Amount as decimal editable` → "Amount is editable" ✓
> `in Approved sealed Amount` → "Amount is sealed in Approved" — formal, authoritative

**Family connection:** Same tautological modifier-as-upgrade pattern. `sealed` is a strong antonym of `editable` in regulated/legal contexts: sealed records, sealed bids, sealed testimony.

**Declarative character:** `sealed` is a participial adjective. "Sealed" describes a state, not a process. Declarative character is strong.

**Risk: permanence connotation.** `sealed` implies finality — a sealed document cannot be unsealed (or at least, unsealing is extraordinary). In Precept, a field `sealed` in one state may be `editable` in the next state after a transition. The vocabulary implies permanence where none exists. A domain expert reading "sealed Amount" in the Processing state and then seeing the same field editable in Draft may experience cognitive dissonance: "I thought it was sealed?"

**Risk: legal-domain specificity.** `sealed` is natural in legal, compliance, and regulated-business domains but feels heavy in casual domains (event registration, library checkout). "In Scheduled sealed ContactEmail" reads as overwrought for what is just "read-only."

**Assessment:** Strong in regulated/formal domains; misleading permanence connotation in general use. The permanence risk is the deciding factor — Precept must work across all domains, not just legal ones.

---

#### Family 4: `editable` / `editable` + `frozen` + `omit`

**Modifier:** `field Amount as decimal editable`
**Upgrade:** `in Draft editable Amount`
**Downgrade:** `in Approved frozen Amount`
**Guarded downgrade:** `in Processing frozen Amount when FraudFlag`
**Omit:** `in Archived omit Notes`

**Reading aloud:**
> `field Amount as decimal editable` → "Amount is editable" ✓
> `in Approved frozen Amount` → "Amount is frozen in Approved" — "frozen assets", "frozen account", "account freeze"

**Family connection:** Same tautological modifier-as-upgrade pattern. `frozen` is a strong antonym of `editable` in financial/business contexts.

**Declarative character:** `frozen` is a participial adjective. "Frozen assets" is result-state language — maximally declarative. No process reading.

**Risk: same permanence issue as `sealed`, but weaker.** "Frozen" does not imply irreversibility as strongly as "sealed" — accounts get unfrozen, frozen assets get released. The financial domain regularly uses "freeze"/"unfreeze" as reversible operations. This risk is lower than Family 3.

**Risk: temperature metaphor.** `frozen` is a physical-world metaphor (temperature). It works in finance ("frozen assets") but may feel odd in non-financial domains. "In Scheduled frozen ContactEmail" is less natural than "In Scheduled fixed ContactEmail."

**Assessment:** Strong contender. `frozen` is excellent domain vocabulary with lower permanence risk than `sealed`. But the temperature metaphor is slightly narrower in domain applicability than `fixed`, which is universally business-natural.

---

#### Family 5: `open` / `open` + `closed` + `omit`

**Modifier:** `field Amount as decimal open`
**Upgrade:** `in Draft open Amount`
**Downgrade:** `in Approved closed Amount`
**Guarded downgrade:** `in Processing closed Amount when FraudFlag`
**Omit:** `in Archived omit Notes`

**Reading aloud:**
> `field Amount as decimal open` → "Amount is open" — open to what? Ambiguous
> `in Draft open Amount` → "Amount is open in Draft" — open for editing? Open for viewing?
> `in Approved closed Amount` → "Amount is closed in Approved" — closed for editing? Closed entirely?

**Family connection:** `open`/`closed` are symmetric antonyms and the modifier (`open`) is the upgrade keyword. The family is internally consistent.

**Declarative character:** Both are adjectives. Maximally declarative. No operational reading.

**Risk: semantic ambiguity.** This is the killer. `open` and `closed` are among the most polysemous words in English. "Open" can mean visible, accessible, editable, unrestricted, unresolved, public, receptive — the word is a Rorschach test. In Precept's access mode context, a developer must learn that `open` specifically means "editable" and `closed` specifically means "read-only." This learning cost contradicts Precept's "reads like English" design principle.

**Risk: `closed` ≠ read-only.** "Closed" more naturally means "done/finished" or "not accepting input at all" — closer to what `omit` does. A domain expert may confuse `closed` (read-only) with `omit` (structurally absent). The vocabulary creates an ambiguity between two distinct access dispositions that must never be confused.

**Assessment:** Symmetric and grammatically clean, but fatally ambiguous. The polysemy of `open`/`closed` creates more confusion than it resolves. The risk of `closed` being confused with `omit` is disqualifying.

---

#### Family 6: `editable` / `editable` + `guarded` + `omit`

**Modifier:** `field Amount as decimal editable`
**Upgrade:** `in Draft editable Amount`
**Downgrade:** `in Approved guarded Amount`
**Guarded downgrade:** `in Processing guarded Amount when FraudFlag`
**Omit:** `in Archived omit Notes`

**Reading aloud:**
> `field Amount as decimal editable` → "Amount is editable" ✓
> `in Approved guarded Amount` → "Amount is guarded in Approved" — protected from editing

**Family connection:** Same tautological modifier-as-upgrade. `guarded` implies protection from change, which is the opposite of `editable`.

**Declarative character:** `guarded` is a participial adjective. "Guarded" is a state description.

**Risk: collision with `when Guard` syntax.** Precept already uses the word "guard" for conditional expressions (`when FraudFlag`). Using `guarded` as an access mode keyword creates a vocabulary collision: `in Processing guarded Amount when FraudFlag` — the keyword `guarded` and the mechanism `when` (which is the guard syntax) are related but mean different things. This is confusing. A developer may think "guarded" refers to the `when` clause mechanism rather than the access disposition.

**Assessment:** The `guard`/`when` vocabulary collision is disqualifying. This family creates more confusion than it resolves.

---

### 5. Evaluation Summary

| # | Modifier | Upgrade | Downgrade | Declarative | English-ish | Consistency | Key Risk |
|---|----------|---------|-----------|-------------|-------------|-------------|----------|
| 1 | `editable` | `editable` | `fixed` | ★★★ (both adjectives) | ★★★ ("fixed price" is universal) | ★★★ (tautological + clear antonym) | `fixed` "repair" secondary meaning (negligible in adjective position) |
| 2 | `editable` | `editable` | `locked` | ★★★ | ★★★ ("locked account") | ★★☆ (mixed register: adjective + participle) | Participial form implies prior state change |
| 3 | `editable` | `editable` | `sealed` | ★★★ | ★★☆ (heavy in casual domains) | ★★☆ | Permanence connotation; domain-specific |
| 4 | `editable` | `editable` | `frozen` | ★★★ | ★★★ ("frozen assets") | ★★☆ | Temperature metaphor; narrower than `fixed` |
| 5 | `open` | `open` | `closed` | ★★★ | ★☆☆ (fatally ambiguous) | ★★★ (symmetric antonyms) | Polysemy; `closed` confused with `omit` |
| 6 | `editable` | `editable` | `guarded` | ★★★ | ★★☆ | ★★☆ | Vocabulary collision with `when Guard` syntax |

### 6. Recommendation: `editable` / `editable` + `fixed` + `omit`

**Family 1 is the answer.** Here is why, stated precisely:

**The modifier surface:** `editable` is the optimal replacement for `writable`. It names the exact same design-time disposition — "this field accepts direct edits" — without the I/O verb root. It is a standard English adjective, dispositional in character, with zero resultative or operational connotation. `field Amount as decimal editable` reads exactly as well as `field Amount as decimal writable`, and `editable` has no semantic baggage that `writable` doesn't also have.

**The access mode surface:**

Using `editable` as BOTH the modifier and the upgrade keyword creates a tautological connection: the developer sees `editable` on the field declaration and sees `editable` in the state rule, and the semantic link is self-evident. This is stronger than root-sharing (like `writable` ↔ `write`) — it's identity.

`fixed` as the downgrade keyword is the strongest antonym available:

1. **Universal business English.** "Fixed price", "fixed rate", "fixed income", "fixed fee", "fixed schedule" — every business domain uses `fixed` to mean "determined and not subject to change." No domain restriction.
2. **Pure adjective.** Cannot be misread as a command. "In Approved, fixed Amount" is a declaration, period.
3. **Clear antonym of `editable`.** The opposition is immediately obvious. A field is either editable or fixed. There is no third reading.
4. **No permanence connotation.** Unlike `sealed` or `locked`, `fixed` does not imply irreversibility. A "fixed price" in one context can become a "negotiable price" in another. This accurately models Precept's state-scoped access: a field can be `fixed` in one state and `editable` in another.
5. **"Repair" secondary meaning is inert.** "Fixed" as a verb means "repaired." But in adjective position — which is how the grammar reads it — "fixed" means "set/determined/unchangeable." Nobody reads "fixed rate" as "repaired rate."

**The complete vocabulary:**

| Access Mode | Keyword | Meaning | Replaces |
|-------------|---------|---------|----------|
| Upgrade (writable here) | `editable` | Field accepts direct edits in this state | `write` |
| Downgrade (read-only here) | `fixed` | Field is determined and not subject to change in this state | `read` |
| Exclusion (absent here) | `omit` | Field is structurally absent from this state | (unchanged) |

**Field modifier:** `editable` replaces `writable`.

**Full surface examples:**

```
field Amount as decimal editable
field Status as string optional
field FraudFlag as boolean default false

in UnderReview editable FraudFlag
in UnderReview editable AdjusterName when not FraudFlag
in Approved fixed Amount
in Processing fixed Amount when FraudFlag
in Archived omit Notes
```

Reading the first access mode rule: "In UnderReview, FraudFlag is editable." Reading the fourth: "In Processing, Amount is fixed when FraudFlag." Reading the last: "In Archived, Notes is omitted." All three read as structural declarations about field disposition. The vocabulary is coherent.

**Why not the others:**

- **`locked` (Family 2):** Works, but the participial form creates a register mismatch with the pure adjective `editable`. The same resultative-participle problem that killed `unlocked` as a modifier applies (in reduced form) to `locked` as a downgrade keyword.
- **`sealed` (Family 3):** Permanence connotation is too strong for state-scoped access. A developer will expect `sealed` to mean "forever," not "in this state."
- **`frozen` (Family 4):** Close second. Excellent in financial domains. But the temperature metaphor narrows applicability, and `fixed` is strictly more universal.
- **`open`/`closed` (Family 5):** Fatal polysemy. `closed` confused with `omit`. Disqualified.
- **`guarded` (Family 6):** Vocabulary collision with `when Guard` syntax. Disqualified.

**Tradeoff accepted:** `omit` remains a verb while `editable` and `fixed` are adjectives. The three access mode keywords are not grammatically homogeneous. This asymmetry is acceptable because `omit` is semantically distinct (structural exclusion vs. access disposition), and the grammatical difference reinforces the semantic one. Forcing all three into the same part of speech would require compromising one of them.

---

## B3: Shane's Grammar Proposal — Semantic Framing and `->` Operator Evaluation

**By:** Frank
**Date:** 2026-04-28
**Status:** Recommendation presented to Shane for sign-off
**Evaluates:** Shane's `in S -> F ADJECTIVE` grammar proposal and his omit/access-mode semantic framing

---

### 1. Does Shane's Semantic Framing Hold?

**Yes. It holds. It is the sharpest articulation of this distinction anyone has offered.**

Shane identified two categorically different operations that share the `in <State>` scope:

- **`omit`** = structural exclusion. The field is ABSENT from the state. This changes the entity's shape — form rendering, integration contracts, `set` validation, clear-on-entry behavior all respond to the field's structural absence.
- **Access modes (`editable`/`fixed`)** = mutability constraint. The field is PRESENT. Its value exists, is readable, participates in rules and constraints. The only question is whether a caller may also write to it.

One removes. The other constrains. This is not a vocabulary preference. It is a genuine categorical distinction between operations on different axes: the presence axis vs. the mutability axis.

B2 already recognized this. The grammatical heterogeneity of `omit` (verb) vs. `editable`/`fixed` (adjectives) was explicitly called "a feature, not a bug" — the part-of-speech difference reinforces the semantic difference. Shane's framing names what B2 was already encoding implicitly: these are different operations and the grammar should acknowledge it.

**What the framing implies for grammar shape:** It provides a principled reason to treat `omit` and access modes differently at the grammar level — IF the resulting grammar is cleaner than the unified form. The framing does NOT automatically require different grammar shapes. The semantic split can be encoded through vocabulary (already done in B2), through grammar shape (Shane's proposal), or through both. The question is whether shape differentiation adds clarity or just complexity.

---

### 2. The `->` Operator — Wrong Operator, Right Instinct

**`->` is the wrong operator for access mode declarations. This is a semantic objection, not an aesthetic one.**

In Precept, `->` has a single, established semantic identity: **directional pipeline flow**. Every current usage of `->` means "and then" or "produces":

| Context | Example | Meaning |
|---------|---------|---------|
| Transition row actions | `-> set ClaimAmount = Submit.Amount` | "then set..." |
| Transition row outcome | `-> transition Approved` | "then transition to..." |
| State action chain | `to Confirmed -> set PaymentReceived = true` | "on entry, then set..." |
| Computed field expression | `field Tax as decimal -> Amount * TaxRate` | "computed as..." |

The spec states this explicitly: *"The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom: each step in a transition — guard, actions, outcome — flows through the same arrow."*

In Shane's proposal, `in Draft -> Amount editable` uses `->` to mean **"targets"** or **"applies to."** This is not pipeline flow. There is no sequence of steps, no transformation, no before-and-after. The arrow points at a field to declare something about it. This is a fundamentally different semantic: selection/targeting, not flow.

Overloading `->` across two unrelated semantic domains (flow and targeting) would dilute the operator's clarity. Today, a developer reading `->` anywhere in a `.precept` file can rely on a single mental model: "this introduces the next step." Shane's proposal would require a second mental model: "unless you're in an access mode rule, where it means 'applies to.'"

**The disambiguation argument:** Shane correctly notes that the lead token (`in` vs. `from`/`to`/`on`) disambiguates for the parser. But parsers don't read precept files — humans do. Consider this block from a real precept:

```
in UnderReview -> Amount editable when not FraudFlag
in Approved ensure ApprovedAmount > 0 because "Approved claims must specify a payout amount"

to Confirmed -> set PaymentReceived = true -> set AmountDue = 0

from Draft on Submit
    -> set ClaimantName = Submit.Claimant
    -> set ClaimAmount = Submit.Amount
    -> transition Submitted
```

The reader encounters `->` three times with two different meanings. The first `->` (line 1) means "targets." The next two `->` occurrences (lines 3 and 6–8) mean "then." The lead token technically disambiguates, but the reader has to context-switch on the operator's meaning depending on which construct they're in. This is cognitive load that serves no purpose.

**The instinct is right:** Shane wants a visual signal that distinguishes access mode declarations from the verb-before-field pattern. He wants the grammar shape to say "this is a declaration about a field" rather than "this is an instruction to the field." The instinct to make the declarative nature visually explicit is sound. The `->` operator is just the wrong vehicle for that instinct.

---

### 3. Grammar Shape: Adjective-After-Field vs. Verb-Before-Field

Read both forms with real field and state names from the samples:

```
// B2 form: adjective-before-field (current settled shape)
in Draft editable Amount when not Finalized
in Approved fixed Amount
in UnderReview editable FraudFlag
in Processing fixed Amount when FraudFlag
in Archived omit Notes

// Shane's proposal: adjective-after-field with ->
in Draft -> Amount editable when not Finalized
in Approved -> Amount fixed
in UnderReview -> FraudFlag editable
in Processing -> Amount fixed when FraudFlag
in Archived omit Notes
```

**Reading aloud:**

The B2 form reads as a noun phrase with an implicit copula: *"In Draft, [the] editable Amount [when not Finalized]."* The adjective modifies the field name directly: "editable Amount" is a natural English construction, like "fixed price" or "optional parameter."

Shane's form reads as a predicate sentence: *"In Draft, Amount [is] editable when not Finalized."* The field name is the subject, the adjective is the complement. This is a more natural English sentence structure.

**Honest assessment:** The adjective-after-field order produces slightly more natural English sentences. "Amount editable" reads as "Amount is editable" — subject-complement. "Editable Amount" reads as "the editable Amount" — modifier-noun. Both are grammatical. The subject-complement form is arguably more declarative.

**But the `->` adds nothing.** If we adopt adjective-after-field, the arrow is visual noise:

```
in Draft Amount editable when not Finalized     // no arrow
in Draft -> Amount editable when not Finalized   // arrow adds what?
```

The arrow doesn't carry semantic weight. It doesn't mean "then." It doesn't mean "produces." It just... points. In a line that already has `in` scoping the state and a field name identifying the target, the arrow is redundant targeting.

**The real cost of adjective-after-field (with or without `->`):** It breaks positional consistency with every other `in`-scoped construct. Today, after `in StateTarget`, the parser always expects a keyword: `write`, `read`, `omit`, `ensure`. With adjective-after-field, after `in StateTarget`, the parser must distinguish between a keyword (`omit`, `ensure`) and an identifier (field name). This is a disambiguation tax. It's solvable — `omit` and `ensure` are keywords, field names are identifiers — but it adds a parsing branch that the current grammar avoids entirely.

---

### 4. `readonly` as Vocabulary

```
in Approved -> Amount readonly
in Approved -> Amount fixed
```

`readonly` is compound programmer vocabulary — `read` + `only`, with a camelCase genealogy from C# (`readonly`), TypeScript (`readonly`), Java (`final` but same concept). It carries I/O verb contamination: `read` is the first half of the word, and B1's entire analysis established that I/O verbs fail the declarative criterion.

In the post-field adjective position, `readonly` reads as a technical access modifier applied to a data member. "Amount readonly" sounds like a C# property declaration, not a business rule.

`fixed` reads as a business declaration. "Amount fixed" = "the amount is fixed" = "the amount is set and not subject to change." No programmer vocabulary, no I/O verbs, no camelCase genealogy.

**Verdict:** `readonly` fails for the same reason `read` and `write` failed in B1 — it's I/O-rooted programmer vocabulary in a DSL that aims for English-ish business language. `fixed` is strictly better in this position.

---

### 5. `editable` vs. `writable` in Post-Field Position

```
in Draft -> Amount editable
in Draft -> Amount writable
```

`writable` carries the same I/O verb root problem as `readonly`. It's `write` + `able` — the ability to write. It describes a capability for a process, not a property of the field.

`editable` describes the field's disposition: "this field is editable" = "this field accepts direct edits." The root is `edit`, which is a content/document verb (B1's "configuration/editorial verb" class), not an I/O verb. "Editable form," "editable field," "editable document" — all standard business English.

**Verdict:** `editable` is the correct word. This was already settled in B2. The post-field position doesn't change the analysis.

---

### 6. `omit` and the Semantic Split

Shane's framing gives us a clean test: if omit and access modes are different operations, should they share the same structural pattern?

Consider the two models:

**Model A — Unified construct (all share the same shape):**
```
in Archived omit Notes
in Draft editable Amount when not Finalized
in Approved fixed Amount
```

Here `omit`, `editable`, and `fixed` all appear in the same syntactic position (keyword-before-field). The grammar treats them as peers. The semantic difference (presence vs. mutability) is encoded only in the vocabulary, not in the shape.

**Model B — Split construct (different shapes for different operations):**
```
in Archived omit Notes                         // exclusion: verb-before-field
in Draft -> Amount editable when not Finalized  // constraint: -> field adjective
in Approved -> Amount fixed                     // constraint: -> field adjective
```

Here the grammar shapes are visually distinct. An author sees two patterns and infers (correctly) that they're different operations.

**Model C — Split construct without `->` (different word order, no arrow):**
```
in Archived omit Notes                         // exclusion: verb-before-field
in Draft Amount editable when not Finalized     // constraint: field-adjective
in Approved Amount fixed                        // constraint: field-adjective
```

Same split, no arrow noise.

**Assessment:**

Model B has the `->` problem analyzed in §2. Rejected.

Model C is interesting. It visually encodes Shane's semantic split through word order alone. But it creates the parsing ambiguity problem described in §3 — after `in StateTarget`, the parser must distinguish keywords from field identifiers. And it creates a visual inconsistency: `omit` puts the keyword first; `editable`/`fixed` put the field first. A developer learning Precept would need to learn two patterns for what looks like the same construct family.

Model A keeps the grammar unified. The semantic split is encoded where it should be: in the **vocabulary**. `omit` is a verb (exclusion). `editable`/`fixed` are adjectives (constraint). The part-of-speech difference already tells the reader these are different kinds of operations. The grammar shape doesn't need to duplicate that signal.

**Verdict:** `omit` and access modes should share the same `in StateTarget KEYWORD FieldTarget` structural pattern. The vocabulary already encodes the semantic distinction. Grammar shape divergence adds complexity (parsing ambiguity, two learning patterns) without adding information the vocabulary doesn't already carry.

---

### 7. Recommendation

**Verdict: (c) — Keep verb-before-field with B2 vocabulary (`editable` / `fixed` / `omit`).**

Shane's semantic framing is correct. `omit` and access modes are genuinely different operations — exclusion vs. constraint. This insight is sharper than anything prior rounds articulated and B2 was already encoding it (perhaps without realizing it was doing so).

But his `->` grammar proposal is wrong on two specific grounds:

1. **`->` has pipeline-flow semantics in Precept.** Using it for field targeting creates a second, unrelated meaning for an operator that the spec explicitly describes as a visual pipeline mechanism. This is not aesthetics — it's semantic coherence of the operator vocabulary.

2. **The grammar shape split is redundant.** The semantic distinction between exclusion and constraint is already encoded in the vocabulary: `omit` (verb, structural exclusion) vs. `editable`/`fixed` (adjectives, mutability constraint). The part-of-speech difference IS the encoding. Adding a grammar shape difference on top of that is belt-and-suspenders — it adds parsing complexity and a second learning pattern without adding information.

**The B2 vocabulary already does what Shane's proposal wants to do — it just does it through word class rather than grammar shape.** The adjective/verb heterogeneity that B2 called "a feature, not a bug" is precisely Shane's semantic split, expressed in vocabulary rather than syntax. B2 was right.

**Full recommended grammar (unchanged from B2):**

```
in StateTarget editable FieldTarget ("when" BoolExpr)?   ← state-scoped guarded upgrade
in StateTarget fixed FieldTarget ("when" BoolExpr)?       ← state-scoped guarded downgrade
in StateTarget omit FieldTarget                            ← state-scoped unguarded exclusion
```

**Full recommended vocabulary (unchanged from B2):**

| Surface | Keyword | Meaning | Replaces |
|---------|---------|---------|----------|
| Field modifier | `editable` | Field accepts direct edits (design-time disposition) | `writable` |
| Upgrade (editable here) | `editable` | Field is editable in this state | `write` |
| Downgrade (fixed here) | `fixed` | Field is set and not subject to change in this state | `read` |
| Exclusion (absent here) | `omit` | Field is structurally absent from this state | (unchanged) |

**What I'm accepting from Shane's contribution:**

Shane's semantic framing — the omit/access-mode split as exclusion vs. constraint — is the correct way to describe why `omit` is grammatically different from `editable`/`fixed`. It should be adopted into the spec's explanatory prose. The existing comment in B2 ("the grammatical difference reinforces the semantic one") was already pointing at this but Shane named it precisely.

**What I'm rejecting:**

- `->` as an access mode operator — wrong semantic for the operator
- Adjective-after-field word order — redundant grammar shape split, adds parsing complexity
- `readonly` — I/O-rooted programmer vocabulary, fails B1 declarative criterion
- `writable` in the access mode position — superseded by `editable` in B2

**Tradeoff accepted:** The grammar shape for `omit` and access modes is identical (`in S KEYWORD F`), even though the operations are semantically distinct. The vocabulary (verb vs. adjective) carries the semantic distinction. A reader who understands that `omit` is a verb and `editable`/`fixed` are adjectives will infer the categorical difference. A reader who doesn't notice the part-of-speech distinction will still use the keywords correctly — the grammar doesn't require understanding the semantic split to use it.

---

## B4 — `modify` Verb Evaluation

**By:** Frank
**Date:** 2026-04-28
**Status:** Recommendation presented to Shane for sign-off
**Evaluates:** Shane's `in State modify Field readonly [when Guard]` grammar proposal

---

### Verdict: recommended-with-caveats

`modify` is a genuine improvement over B2's `editable`-as-verb approach. The separation of verb (`modify`) from adjective (`readonly`/`editable`) is cleaner than using the same word in both roles. I am recommending it — with specific adjective vocabulary choices and one caveat about the verb's semantic precision.

---

### 1. Semantic Fit

**Strong, not perfect.** `modify` means "to change the form or qualities of." In `modify Amount readonly`, the declaration changes the access quality of the field in this state. That's semantically accurate.

The slight imprecision: `modify` connotes making a change to something, which implies an action — but in context, the statement is declarative. "In Draft, modify Amount readonly" reads as "in Draft, the Amount field's access is modified to readonly." The declarative reading works, but it requires the reader to interpret `modify` as "declare a modification to" rather than "perform a modification on."

Compare with `omit`: "in Archived, omit Notes" — `omit` is also technically an imperative verb used declaratively. The same declarative-reinterpretation applies. So `modify` is not worse than `omit` on this axis — they share the same verb-used-declaratively character.

**Business English:** "Modify the terms" / "modify the agreement" / "modify access" — standard business vocabulary. A domain expert would understand "modify Amount readonly" without programmer context. This passes.

### 2. Verb Parallelism with `omit`

**This is where `modify` genuinely shines.** Shane's core insight is correct.

```
in Archived omit Notes              → exclude the field
in Draft modify Amount readonly     → constrain the field's access
```

Both are verbs. Both take a field target. Both describe an operation on a field within a state scope. The operations are semantically distinct (exclusion vs. constraint), and the distinct verbs encode that distinction cleanly.

Compare B2: `in Draft editable Amount` / `in Archived omit Notes` — `editable` is an adjective pretending to be a verb in the lead-keyword position. It works, but there's a grammatical mismatch: `omit` conjugates ("omits"), `editable` doesn't. `modify` conjugates naturally ("modifies"), creating true verb parallelism with `omit`.

**The part-of-speech analysis:** B2 argued that the adjective/verb heterogeneity between `editable` and `omit` was "a feature, not a bug" — encoding the semantic distinction. That argument was sound. But `modify` + adjective achieves the same encoding more cleanly: the verb class is homogeneous (`modify`/`omit` — both verbs), while the semantic distinction moves to the operation itself. `omit` = remove. `modify` = constrain. The vocabulary is cleaner because the grammar is cleaner.

### 3. Adjective Vocabulary

With `modify` as the verb, the adjective names the access level being applied. The adjective is no longer doing verb duty — it's purely descriptive. This changes the evaluation:

**For the read-only adjective:**

| Candidate | Reading | Assessment |
|-----------|---------|------------|
| `readonly` | "modify Amount readonly" | Compound programmer vocabulary. `read` + `only`. But in this position — as an adjective following a field name after an explicit verb — the I/O contamination is significantly reduced. The verb `modify` already carries the declarative weight. `readonly` just names the target state. Still not ideal, but far less problematic than `readonly` as a standalone keyword (B3 §4). |
| `fixed` | "modify Amount fixed" | Business English. "Fixed price." Strong declarative character. But "modify Amount fixed" has a parsing tension: `modify` implies change, `fixed` implies immutability. "Modify it to be fixed" is coherent but slightly paradoxical. |
| `locked` | "modify Amount locked" | Same paradox as `fixed` but with participial baggage (B2 §Family 2). |
| `read-only` | "modify Amount read-only" | Hyphenated compound. Introduces a hyphen into the DSL token vocabulary — added lexer complexity for no semantic gain over `readonly`. Rejected. |

**For the editable adjective:**

| Candidate | Reading | Assessment |
|-----------|---------|------------|
| `editable` | "modify Amount editable" | "Modify Amount to be editable" — natural. Clean dispositional adjective. No paradox with `modify`. |
| `writable` | "modify Amount writable" | I/O-rooted. Same objection as prior rounds. |
| `mutable` | "modify Amount mutable" | Programmer jargon. Domain experts don't say "mutable." |

**Recommended pair: `readonly` and `editable`.**

Here's why I'm softening on `readonly` specifically in this grammar shape: When `readonly` appeared as a standalone keyword in the verb position (B3's rejected forms), it carried the full weight of the I/O connotation. As an adjective following `modify`, it's subordinate — `modify` is the verb doing the work, `readonly` just names the mode. The programmer-vocabulary concern is real but tolerable in the adjective slot.

`fixed` was my B2/B3 recommendation, but `modify Amount fixed` creates the modify-to-fix paradox. `readonly` avoids this: "modify the access to be readonly" has no internal tension.

**However:** If Shane or I decide the programmer-vocabulary concern with `readonly` remains too strong even in the adjective position, `fixed` is the fallback. The paradox is minor — "modify Amount fixed" reads as "in this state, Amount's access is modified: it's fixed." Workable.

### 4. Disambiguation Risk

`modify` does not exist in the current keyword catalog. Checking all token categories from the spec (§1.1):

- **Declaration keywords:** `precept`, `field`, `state`, `event`, `rule`, `ensure`, `as`, `default`, `optional`, `writable`, `because`, `initial` — no conflict.
- **Prepositions:** `in`, `to`, `from`, `on`, `of`, `into` — no conflict.
- **Control:** `when`, `if`, `then`, `else` — no conflict.
- **Actions:** `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear` — no conflict.
- **Outcomes:** `transition`, `no`, `reject` — no conflict.
- **Access modes:** `write`, `read`, `omit` — no conflict.
- **Logical operators:** `and`, `or`, `not` — no conflict.
- **Membership:** `contains`, `is` — no conflict.
- **Quantifiers:** `all`, `any` — no conflict.
- **State modifiers:** `terminal`, `required`, `irreversible`, `success`, `warning`, `error` — no conflict.
- **Constraints:** `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, etc. — no conflict.
- **Built-in types/functions:** `string`, `number`, `integer`, `decimal`, `boolean`, `date`, `choice`, `set`, `list`, `queue`, `stack`, `count`, `sum`, `avg`, `min`, `max` — no conflict.

`modify` is a clean new keyword with zero vocabulary collision. It would occupy the lead-token position after `in StateTarget`, parallel to `write`/`read`/`omit`/`ensure`. The parser dispatches on lead token — `modify` is unambiguous.

**The adjective (`readonly`/`editable`) following the field target:** These are new keywords too. `readonly` has no collision. `editable` would be a new keyword (currently not in the catalog). Neither collides with any existing token.

### 5. Comparison to B2 Recommendation

B2 recommended `editable`/`fixed`/`omit` in verb-before-field position:

```
in Draft editable Amount when not Finalized    ← B2
in Draft modify Amount editable when not Finalized  ← modify proposal
```

**B2's problem:** `editable` does double duty. On a field declaration (`field Amount as decimal editable`), it's an adjective — fine. In the `in` scope (`in Draft editable Amount`), it's being used as a verb — the lead keyword that the parser dispatches on. But `editable` is not a verb. It's an adjective masquerading as one. Shane correctly identified this as awkward.

**`modify` fixes this cleanly.** The verb is `modify` (a real verb). The adjective is `editable` or `readonly` (real adjectives). Each word does exactly one job. The grammar has no word doing double duty.

**Does `modify` escape the B2 objection about `read`/`write` (operations, not states)?** Yes. `read` and `write` describe I/O operations the caller performs. `modify` describes what the declaration does to the field's access configuration — it modifies (constrains/changes) the field's access properties. It's a meta-operation on the declaration surface, not an I/O operation on data. This is a genuine semantic improvement.

### 6. Grammar Shape

The full grammar with `modify`:

```
in StateTarget modify FieldTarget readonly ("when" BoolExpr)?   ← constrain to read-only
in StateTarget modify FieldTarget editable ("when" BoolExpr)?   ← declare editable (upgrade)
in StateTarget omit FieldTarget                                  ← structural exclusion
```

**Field modifier surface (unchanged):**
```
field Amount as decimal editable      ← design-time mutability disposition
```

**Vocabulary family connection:** `editable` appears on both the field declaration (as modifier) and in the `modify` access mode (as adjective). The connection is direct — the field is declared `editable`, and `modify Amount editable` confirms or re-applies that disposition in a specific state. `modify Amount readonly` overrides it. The family coherence is strong.

### 7. What `modify` Replaces

If adopted, `modify` would replace `write` and `read` as the access mode verb:

| Current | Proposed | Change |
|---------|----------|--------|
| `in S write F` | `in S modify F editable` | `write` verb → `modify` verb + `editable` adjective |
| `in S read F` | `in S modify F readonly` | `read` verb → `modify` verb + `readonly` adjective |
| `in S omit F` | `in S omit F` | unchanged |

The `write`/`read` keywords would be removed from the access mode catalog. `modify` + `readonly`/`editable` take their place. `omit` stays.

### 8. Open Question: `readonly` vs `fixed`

I'm recommending `readonly`/`editable` as the primary pair, but this is the one axis where I'm genuinely torn. The arguments:

**For `readonly`:** No semantic tension with `modify` ("modify to be readonly" — coherent). Universally understood meaning. Widely recognized even outside programming (read-only documents, read-only access in business software).

**For `fixed`:** Pure business English. No programmer genealogy. But "modify Amount fixed" has a slight paradox (modify → change; fixed → not changeable). Also, `fixed` on a field declaration modifier would be the antonym: `field Amount as decimal editable` ↔ `in Approved modify Amount fixed`. The `editable`/`fixed` antonym pair is strong (B2 established this).

**My lean:** `readonly`/`editable`. The paradox-free grammar wins. But if Shane has a strong preference for `fixed`, the argument is close.

### 9. Overall Assessment

| Axis | Score | Notes |
|------|-------|-------|
| Semantic fit | Strong | Real verb, accurate meaning, standard business English |
| Verb parallelism | Excellent | `modify`/`omit` — true verb-verb parallelism, both conjugate naturally |
| Adjective separation | Excellent | Verb and adjective do separate jobs — no double-duty |
| Disambiguation | Clean | Zero keyword collision, unambiguous lead token |
| Business readability | Strong | "In Draft, modify Amount readonly" reads naturally |
| Comparison to B2 | Improvement | Fixes the adjective-as-verb awkwardness Shane correctly identified |

**Verdict: recommended-with-caveats.**

Caveats:
1. **Adjective pair needs sign-off.** `readonly`/`editable` is my recommendation, but `fixed`/`editable` is a defensible alternative. This needs Shane's call.
2. **Verbosity.** `modify Amount readonly` is 3 tokens where B2's `fixed Amount` was 2. The extra token is the cost of separating verb from adjective. I consider this an acceptable trade — clarity over brevity — but it's a real cost.
3. **`modify` is not purely declarative.** Like `omit`, it's an imperative verb used declaratively. This is consistent within Precept (the DSL already uses verbs declaratively: `ensure`, `reject`, `set`, `omit`), but purists would note it.

**What I'm accepting from Shane's contribution:**
- The verb/adjective separation (`modify` = verb, `readonly`/`editable` = adjective) is a cleaner design than B2's adjective-as-verb approach. Shane was right that `editable` doing double duty was awkward.
- `modify` creates genuine verb parallelism with `omit` that B2 did not achieve.
- The post-field adjective position (Shane's grammar shape) is the right place for the access level word when `modify` is the verb.

**What changes from B3:**
- B3 rejected adjective-after-field as redundant. With `modify` as the verb, adjective-after-field is no longer redundant — it's the natural position for an adjective that follows a verb+target construction. "Modify Amount readonly" = verb-object-complement, a standard English sentence structure.
- B3's recommendation (`editable`/`fixed`/`omit` all in verb position) is superseded. `modify` + adjective is the new recommendation.

---

## B4 Final Decision — LOCKED (2026-04-28)

**Shane's selection:** `modify` verb + `readonly`/`editable` adjectives (Frank's B4 recommendation, paradox-free pair).

**Locked grammar:**

```
in StateTarget modify FieldTarget readonly ("when" BoolExpr)?   ← constrain to read-only
in StateTarget modify FieldTarget editable ("when" BoolExpr)?   ← declare editable (upgrade)
in StateTarget omit FieldTarget                                  ← structural exclusion (no guard, no adjective)
```

**Examples:**

```precept
in Draft modify Amount readonly when not Finalized
in Approved modify Amount editable
in Archived omit Notes
```

**Why this vocabulary wins:**

1. **Verb/adjective separation.** `modify` is the verb (constraint declaration), `readonly`/`editable` are adjectives (access mode names). Each word does exactly one job — no double-duty.
2. **True verb parallelism.** `modify` and `omit` are both verbs, both take field targets, both describe operations on field access. The semantic distinction (constraint vs. exclusion) is encoded by different verbs, not by part-of-speech mismatch.
3. **Paradox-free.** `readonly`/`editable` have no semantic tension with `modify`. Unlike `fixed` (which creates a modify-to-fix paradox), `readonly` names a mode without implying permanence.
4. **Vocabulary family coherence.** `editable` appears on field declarations (`field Amount as decimal writable`) and in access mode adjective position — direct family connection.

**Alternatives rejected:**
- `editable`/`fixed` as adjective-verbs (B2) — grammatically awkward; `editable` is not a verb.
- `write`/`read` (original) — I/O-rooted programmer vocabulary; fails the declarative-language test.
- `fixed` as read-only adjective (B4 §8 alternative) — `modify Amount fixed` has modify-to-fix paradox.

**Catalog impact (implementation task):**
- New tokens: `TokenKind.Modify`, `TokenKind.Readonly`, `TokenKind.Editable`
- `AccessMode` disambiguation entry: `[new(TokenKind.In, [TokenKind.Modify, TokenKind.Omit])]`
- `AccessMode` slot sequence: `[FieldTarget, AccessModeKeyword, GuardClause(opt)]` (field-then-adjective-then-guard)
- `Omit` constructs: `[FieldTarget]` only — no adjective slot, no guard
- `TokenKind.Write` and `TokenKind.Read` retired from access mode context

**Guard position:** Post-field. `in Draft modify Amount readonly when not Finalized`. The adjective precedes the guard — natural verb-object-complement-condition order.

**Reference:** B4 analysis (§ B4 above), F12 decision in `docs/working/catalog-parser-design-v7.md`.
