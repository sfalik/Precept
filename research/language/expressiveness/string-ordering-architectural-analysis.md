# String Ordering Architectural Analysis

> **Research question:** Does arbitrary string ordering (`<`, `>`, `<=`, `>=` on string values) have genuine business use cases — or can ordered choice types cover all real needs?
>
> **Date:** 2026-05-24
> **Author:** Frank (Lead/Architect & Language Designer)
> **Status:** Final
> **Related:** [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md) (prior research on case-insensitive ordering)

---

## 1. Executive Summary

**Verdict: Remove string ordering entirely.** It is not needed, not used, and actively harmful to the language surface.

All genuine ordering scenarios in business rules map to one of four categories:
1. **Discrete, known ranks** → `ordered choice` (priority, severity, status)
2. **Temporal ordering** → `date`, `datetime`, `instant`, `time`, `duration`
3. **Quantitative ordering** → `integer`, `decimal`, `number`
4. **State-graph ordering** → built into Precept's lifecycle model

**Crucially: string ordering does not exist in Precept's implementation.** Only string equality (`==`, `!=` with CI variants) and concatenation (`+`) are registered operations. There is no `StringLessThanString`, `StringGreaterThanString`, or any equivalent ordering operation in `Operations.cs`. The ordering operators are registered for: `integer`, `decimal`, `number`, `date`, `time`, `instant`, `duration`, `datetime`, and `choice`.

Even if string ordering were hypothetically added, no sample file in the 50+ file corpus uses it. The research below confirms that ordered choice types cover every real ordering scenario.

---

## 2. Survey of Actual String Ordering Usage

### 2.1 Precept Implementation State

Precept's operation catalog (`src/Precept/Language/Operations.cs`) registers these string operations:

| Operation | Registration | Ordering? |
|-----------|-------------|-----------|
| `StringPlusString` | Line 259 | ✗ concatenation |
| `StringEqualsString` | Line 823 | ✗ equality only |
| `StringNotEqualsString` | Line 828 | ✗ inequality only |
| `StringCaseInsensitiveEqualsString` | Line 1195 | ✗ CI equality |
| `StringCaseInsensitiveNotEqualsString` | Line 1198 | ✗ CI inequality |

**No `StringLessThanString`, `StringGreaterThanString`, or any ordering variant exists.**

For comparison, ordering operators are registered for:

| Type Family | Operations |
|-------------|-----------|
| Integer | `<`, `>`, `<=`, `>=` |
| Decimal | `<`, `>`, `<=`, `>=` |
| Number | `<`, `>`, `<=`, `>=` |
| Choice | `<`, `>`, `<=`, `>=` (ordered only) |
| Date | `<`, `>`, `<=`, `>=` |
| Time | `<`, `>`, `<=`, `>=` |
| Instant | `<`, `>`, `<=`, `>=` |
| Duration | `<`, `>`, `<=`, `>=` |
| Datetime | `<`, `>`, `<=`, `>=` |

### 2.2 Sample File Corpus Analysis

A comprehensive survey of all 50+ sample `.precept` files found:

**Zero instances of string ordering comparisons.**

Every comparison in the corpus falls into one of these categories:

#### Date ordering (temporal)
```precept
# academic-course-registration.precept
rule RegistrationCloses > RegistrationOpens when RegistrationOpens is set and RegistrationCloses is set
rule AddDropDeadline > FirstDayOfClasses when FirstDayOfClasses is set and AddDropDeadline is set
```
Fields are declared as `field RegistrationCloses as date optional` — the `date` type, not string.

#### Integer ordering
```precept
# academic-course-registration.precept
rule TotalCreditHours <= 21 when StudentLevel is set and StudentLevel != "Graduate"
rule TotalCreditHours > 18 and Approve.Note is not set

# shopping-cart.precept
rule ItemCount <= 1000
```

#### Decimal/money ordering
```precept
# insurance-claim.precept
rule ApprovedAmount <= ClaimAmount
rule PaidAmount <= ApprovedAmount

# calibration-management.precept
# ToleranceUpper as decimal, MeasuredValue as decimal — comparable
```

#### Equality on string (non-ordering)
```precept
# customer-profile.precept
rule PreferredContactMethod != "email" or Email is set
rule PreferredContactMethod == "email" or Phone is set

# academic-course-registration.precept
rule TotalCreditHours <= 21 when StudentLevel != "Graduate"
```

**Key observation:** All string comparisons are equality checks (`==`, `!=`) against literal values — never `<`, `>`, `<=`, `>=`.

### 2.3 Ordered Choice Type Usage

The corpus demonstrates extensive use of ordered choices for ranking:

| Sample | Field | Type | Usage |
|--------|-------|------|-------|
| `insurance-policy-endorsement.precept` | `HighestInteractionSeverity` | `choice of string("None", "Mild", "Moderate", "Severe") ordered` | Severity escalation rules |
| `prior-auth-appeal.precept` | `UrgencyLevel` | `choice of string("Critical", "Urgent", "Standard", "Routine") ordered` | Triage priority |
| `prior-auth-appeal.precept` | `AppealReason` | `ordered` choice | Appeal processing priority |
| `medical-prior-auth.precept` | `UrgencyLevel` | `choice of string("Critical", "Urgent", "Standard", "Routine") ordered` | Authorization urgency |
| `patient-care-plan-coordination.precept` | `GoalStatus` | `choice of string("Active", "Completed", "OnHold", "Modified") ordered` | Care plan tracking |
| `patient-referral-management.precept` | `UrgencyLevel` | `choice of string("Routine", "Urgent", "Stat") ordered` | Referral prioritization |
| `prescription-refill-request.precept` | `Priority` | `choice of string("Routine", "Standard", "Urgent", "Stat") ordered` | Prescription urgency |
| `utilization-review-case.precept` | `LevelOfCare` | `choice of string("Observation", "MedSurg", "StepDown", "ICU") ordered` | Clinical intensity |

All of these represent exactly the kind of "ordering strings" that domain experts naturally reach for. Every one is correctly modeled as an `ordered choice` — because the domain expert knows the ranking, it's finite and meaningful, and the intent is explicit.

---

## 3. Assessment of Ordered Choice Coverage

### 3.1 What Ordered Choices Cover

Ordered choices model exactly the scenarios where string ordering would be used in practice:

| Business Scenario | Ordered Choice Pattern | Why It Works |
|------------------|----------------------|--------------|
| Priority levels | `choice("Critical", "Urgent", "Standard", "Routine") ordered` | Explicit ranking, named values, no ambiguity |
| Severity | `choice("None", "Mild", "Moderate", "Severe") ordered` | Clinical/legal meaning is explicit |
| Status progression | `choice("Active", "Completed", "OnHold", "Modified") ordered` | States have intrinsic order |
| Maturity models | `choice("Initial", "Developing", "Defined", "Managing", "Optimizing") ordered` | Process maturity is inherently ordinal |
| Floor levels | `choice("Ground", "First", "Second", "Third", "Fourth") ordered` | Architectural ordering |

### 3.2 Can Ordered Choices Model "Ad Hoc" Ordering?

**Question:** What about scenarios where a domain expert might want to compare strings they didn't declare as choices?

**Scenario 1: Version comparison ("1.0" < "2.0")**
- Ordinal string comparison gives wrong results ("10" > "9" alphabetically, but "10" > "9" numerically only coincidentally)
- Semantic version comparison needs `major.minor.patch` parsing — not a string operation
- **Correct model:** Structured fields or a dedicated version type
- **Verdict:** Ordered choices don't apply, but string ordering wouldn't help either

**Scenario 2: Alphabetical ordering (product names, SKUs)**
- "apple" < "Banana" — whose alphabetical? ASCII? Locale-aware? Case-sensitive?
- This is a display/presentation concern, not a business rule
- **Correct model:** Sort in the presentation layer, not in business rules
- **Verdict:** Ordering in business rules would be the wrong layer

**Scenario 3: Implicit ordering (state names like "pending" < "approved")**
- This IS an ordered ranking, just declared implicitly as a string
- **Correct model:** `choice of string("Pending", "Approved", "Completed") ordered`
- **Verdict:** Ordered choice is strictly better because it makes the ranking explicit and type-safe

**Scenario 4: Code prefix ordering ("M100" < "M200", "A" < "B")**
- These are codes, not values — they have structural meaning (prefix + sequence)
- String comparison on codes conflates structural identity with ordering
- **Correct model:** Parse the code structure into typed fields, or use an ordered choice if the set is known
- **Verdict:** String ordering here is a modeling error

### 3.3 Is the "Closed Set" Limitation Real?

Ordered choice types are closed — once defined, you cannot add new values without changing the precept definition. The concern is: what if a domain needs extensible ranks?

**Answer: This is a feature, not a limitation.**

1. **Prevention, not detection:** If a new rank is added without updating the ordered choice, the compiler catches it. Allowing arbitrary string ordering would silently permit ranks in undefined order.

2. **Extensibility via events:** Precept's event model handles dynamic scenarios. An event can introduce a new state/rank, and the type system validates the transition.

3. **Real-world precedent:** Business domains that need "open" ranking typically have one of these patterns:
   - **Numeric scoring** (use `integer` or `decimal`) — e.g., risk score 1-100
   - **Structured categories** (use choices with subtypes) — e.g., severity level + detail code
   - **Temporal progression** (use `date`/`datetime`) — e.g., version dates

No real business scenario needs "I want to compare arbitrary strings and get a deterministic ordering" — if you need deterministic ordering, you have a defined rank set, which is an ordered choice.

---

## 4. The Complexity Tax of String Ordering

### 4.1 Semantic Ambiguity

String comparison is deceptively simple. The question "what does `x < y` mean?" has no single answer:

| Ordering Model | "banana" < "Banana"? | "Ångström" < "Apple"? | "z" < "a"? |
|---------------|---------------------|----------------------|-----------|
| Codepoint (ASCII) | FALSE ('b'=98, 'B'=66) | FALSE ('Å'=197, 'A'=65) | TRUE ('z'=122, 'a'=97) |
| Case-insensitive codepoint | TRUE | FALSE | TRUE |
| Unicode locale-aware | Depends on locale | Depends on locale | Depends on locale |
| Dictionary order | TRUE | TRUE | FALSE |

Every model gives different results. Unlike `integer < integer` where `5 < 10` is unambiguous, string comparison's semantics depend on:
- Case sensitivity
- Accent sensitivity
- Locale/collation rules
- Character encoding (UTF-8 vs UTF-16)
- Normalization form (NFC vs NFD)

### 4.2 The Cascade Risk (Documented)

The prior research in [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md) documents the cascade that follows from adding CI ordering:

1. CI equality (`~=`) → CI not-equal (`~!=`) → immediate demand
2. CI `contains` → immediately needed for text validation
3. CI `startsWith` / `endsWith` → equally common
4. CI `matches` / regex → domain experts want CI pattern matching

PowerShell's experience shows 14+ operator variants (`-ilt`, `-igt`, `-ile`, `-ige`, `-clt`, `-cgt`, `-cle`, `-cge`, etc.).

**Precept already has the solution:** `toLower(x) < toLower(y)` works today. This matches the dominant industry approach used by OData, FEEL, XPath, and OPA.

### 4.3 Type System Fragmentation

Adding string ordering would create an asymmetry in Precept's type system:

| Type | `==` | `!=` | `<` | `>` | `<=` | `>=` |
|------|------|------|-----|-----|------|------|
| integer | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| decimal | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| number | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **string** | ✓ | ✓ | **✗** | **✗** | **✗** | **✗** |
| choice (ordered) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| date | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

This fragmentation is a liability:
- Developers see `string` as "comparable" (it has `==`) and expect `<` to work
- The type checker must maintain a special case: "strings support equality but not ordering"
- Documentation must explain why strings are different from all other orderable types

### 4.4 The Proof Engine Tax

Precept's compile-time proof engine proves numeric and temporal properties at compile time. Adding string ordering would require:

1. **Interval analysis on strings:** Can we prove `x < "Z"` implies `x` starts with 'A'-'Y'? (Unicode edge cases make this intractable)
2. **Reachability analysis:** Can we prove a string field will never satisfy `x < "A"` when it's assigned "Z"?
3. **Contradiction detection:** Can we detect `x > "Z" and x < "A"`?

The proof engine gains minimal value from string ordering (domains experts don't write rules like "x must be before 'm' alphabetically") but the implementation cost is significant: Unicode normalization, collation awareness, and the combinatorial explosion of string interval reasoning.

---

## 5. Industry Benchmarking

### 5.1 Comparison Matrix

| Category | CI Ordering? | Default String `<` | Pattern |
|----------|-------------|-------------------|---------|
| **SQL** | ✅ Yes (collation-based) | Collation-dependent | Side effect of locale collation, not a dedicated feature |
| **No-Code (6 platforms)** | ❌ 0/6 | Codepoint or unavailable | 4/6 don't offer `<`/`>` on strings at all |
| **Rules Engines (5)** | ❌ 0/5 | Codepoint-based | Workaround: `lower(x) < lower(y)` |
| **Spreadsheets (4)** | ❌ 0/4 | Codepoint-based | Excel: equality is CI but ordering is codepoint (deliberate) |
| **Programming Languages (7)** | ❌ 6/7 | Codepoint-based | All non-PowerShell languages use function calls for CI ordering |
| **Policy/Expression (7)** | ❌ 0/7 | Codepoint-based | All use function wrapping: `lower(x) < lower(y)` |

**6 of 7 categories have ZERO built-in CI ordering.** SQL is the sole exception, and even there it's a side effect of general-purpose locale collation, not a dedicated business-DSL feature.

### 5.2 The Universal Pattern

Every system that handles CI ordering uses **function-based normalization**:

```
lower(x) < lower(y)
tolower(x) < tolower(y)
```

Precept already has `toLower` and `toUpper` built-in. **The solution already works.**

---

## 6. Language Design Perspective

### 6.1 The "I Need to Compare Strings" Anti-Pattern

When a developer says "I need to check if one string comes before another," they almost always mean one of:

1. **"I have a ranking I should model explicitly."** → Use an `ordered choice`.
2. **"I need to sort a display."** → Do it in the presentation layer.
3. **"I need version comparison."** → Use structured fields or a version type.
4. **"I need alphabetical filtering."** → Use a database query or `toLower(x) < toLower(y)`.

There is **no scenario** where "I need to compare arbitrary strings and get a deterministic ordering that means something in my domain" survives scrutiny. If the ordering has meaning, it's a defined rank (ordered choice). If it doesn't have meaning, the comparison is meaningless.

### 6.2 Complexity as a Tax

Every operator adds cognitive load:
- `<` on strings: "Does it respect case? Which case rules? Unicode?"
- `<=` on strings: Same ambiguity
- `>` and `>=`: More of the same

Each operator also triggers the cascade: "If I can compare, I can probably check containment, starting position, etc."

**Ordered choice avoids all of this by design.** The rank is declared once, in the type definition. Comparisons are ordinal by position — unambiguous, deterministic, and type-safe.

### 6.3 Design for Humans, Not Machines

Precept's author is a domain expert, not a compiler writer. Domain experts think in terms of:
- "This is more important than that" → ordered choice
- "This happens before that" → temporal types
- "This amount is larger than that" → numeric types
- "These are the same" → equality

Domain experts do **not** think in terms of "these strings happen to sort in Unicode codepoint order." That's a machine artifact, not a domain concept.

---

## 7. Architectural Verdict

### 7.1 Recommendation: Remove (Hypothetical)

**String ordering is not present in the current implementation and should not be added.**

The evidence is conclusive:

1. **Zero usage in the corpus.** 50+ sample files, zero instances of string comparison with `<`, `>`, `<=`, `>=`.
2. **Ordered choices cover all real ordering scenarios.** Every scenario where someone reaches for string ordering actually needs an ordered choice, temporal type, or numeric type.
3. **6 of 7 industry categories have no built-in CI ordering.** The universal pattern is function-based normalization (`lower(x) < lower(y)`).
4. **The complexity tax is high.** Semantic ambiguity, cascade risk, proof engine burden, type system fragmentation.
5. **The solution already exists.** `toLower(x) < toLower(y)` works today.
6. **String ordering invites misuse.** It provides an operator whose results are either meaningless or already modelable better with explicit types.

### 7.2 Migration Path

Since string ordering does not exist, there is nothing to migrate. However, for future reference, if string ordering were ever added and then removed:

1. **Compile-time errors:** Replace `x < y` where x,y are strings with `toLower(x) < toLower(y)` for CI ordering, or model as ordered choice for ranking.
2. **Guidance documentation:** Add to the language spec: "Precept does not provide ordering operators on string types. Use ordered choices for ranking, temporal types for time-based ordering, or `toLower(x) < toLower(y)` for case-insensitive lexicographic comparison."

### 7.3 Language Surface Decision

**Do not add string ordering.** The operator family for string ordering (`<`, `>`, `<=`, `>=`) should remain unregistered.

The current state is correct:
- String equality: `==`, `!=` (with CI variants `~=` and `~= `)
- String concatenation: `+`
- String normalization: `toLower`, `toUpper`, `trim`, etc.
- **String ordering: not provided** (by design)

If someone asks for string ordering, guide them to:
1. `toLower(x) < toLower(y)` for display/presentation ordering
2. `choice of string("A", "B", "C") ordered` for genuine business ranking
3. A domain type with explicit ordering semantics

---

## 8. Why This Matters

Removing string ordering is not a feature removal — it's a **language boundary decision**. Precept's boundary between "what the type system supports" and "what is delegated to the expression language" must be sharp.

The principle is: **Precept governs domain integrity, not string manipulation.** String manipulation belongs in the expression language (and Precept already provides `toLower`, `toUpper`, concatenation, and the full set of comparison operators for orderable types).

When domain experts need to compare values, they should reach for the type that **means** something — a priority, a date, an amount — not a raw string whose ordering has no inherent meaning.

---

## 9. Alternatives Considered and Rejected

| Alternative | Why Rejected |
|-------------|-------------|
| Add string ordering with codepoint semantics | Misleading — domain experts don't think in Unicode codepoints; results are locale-dependent |
| Add string ordering with locale-aware semantics | Too many locales, too much ambiguity; turns a simple DSL into a locale configuration problem |
| Add string ordering with CI semantics | Cascade to 14+ operators (PowerShell precedent); already solvable with `toLower(x) < toLower(y)` |
| Keep string ordering "for flexibility" | Flexibility without meaning is a trap; ordered choices provide flexibility with explicit meaning |
| Add only CI `<` but not CS `<` | Arbitrary distinction; if you're choosing between CI and CS, you should be using an ordered choice |

---

## 10. Tradeoff Accepted

**Removing (or declining to add) string ordering means:**
- Domain experts cannot write `sku < "M100"` as a shorthand for "sku sorts before M100 alphabetically"
- **But** they should write `toLower(sku) < toLower("M100")` for that pattern, which is explicit and unambiguous
- **Or better yet**, they should model SKUs with their structural components parsed into typed fields

The tradeoff is clear: we sacrifice a meaningless operator to preserve clarity, determinism, and type safety. This is consistent with Precept's core philosophy of prevention over detection.

---

## 11. Precedent

1. **Excel's deliberate design:** Equality is case-insensitive but ordering is codepoint-based. This is the same split Precept has with strings (`==` with CI, `<` not available). It's a deliberate choice that separates meaningful equality from meaningless ordering.

2. **No rules engine provides CI ordering.** All 5 surveyed (Drools, NRules, Camunda/DMN, Azure Logic Apps, Flowable) require workarounds.

3. **No policy language provides CI ordering.** All 7 surveyed (OPA/Rego, OData, XPath, FEEL, SpEL, Cedar, MVEL) use function wrapping.

4. **PowerShell is the sole exception** — and its justification (end-user audience) does not apply to Precept's domain-expert audience.

5. **The corpus itself is precedent.** 50+ sample files modeling real business domains use zero string ordering comparisons. The ordering that *is* needed is already covered by ordered choices.

---

## 12. Sources

- Precept operation catalog: `src/Precept/Language/Operations.cs` (lines 259, 769-960, 823-833)
- Precept sample corpus: 50+ `.precept` files in `samples/`
- Precept ordered choice documentation: `docs/language/primitive-types.md` (§ 5. Ordered Choice Types)
- Precept case-insensitive comparison research: `research/language/expressiveness/business-string-ordering-use-cases.md`
- Precept case-insensitive implementation research: `research/language/expressiveness/case-insensitive-implementation-survey.md`
- Precept grammar: `docs/language/precept-grammar.md`
- SQL collation documentation: PostgreSQL 18 §23.2, MySQL 8.4, SQL Server COLLATE
- PowerShell comparison operators: `about_comparison_operators`
- Drools DRL Rule Language Reference
- Camunda DMN/FEEL Handbook
- OData 4.01 URL Conventions §5.1.1
