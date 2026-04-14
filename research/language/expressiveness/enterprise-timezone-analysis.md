# Enterprise Multi-Timezone Analysis

**Research date:** 2026-04-13
**Author:** Frank (Lead/Architect, Language Designer)
**Context:** Response to Shane's enterprise multi-timezone challenge against the ZonedDateTime exclusion. Shane argues that when the timezone is *entity data that varies per instance* — not a presentation concern — the "store as Instant, display as local" architecture breaks down.

---

## The Challenge

My previous analysis concluded: "store as Instant, display as local eliminates the need for ZonedDateTime in the governing contract." Shane counters with four concrete enterprise scenarios where the timezone is a **domain input that affects rule evaluation**, not a presentation concern:

1. **Filing deadline is midnight local time in the jurisdiction of record** — timezone varies per entity instance.
2. **Must be submitted during business hours in the customer's timezone** — business rule references a per-customer timezone.
3. **SLA response window is 4 business hours in the service region's timezone** — "business hours" requires local time in a specific zone.
4. **Multi-region insurance** — "11:59 PM local time on the 30th day after the incident, in the state where the incident occurred." The state (and thus timezone) is entity data.

The unifying claim: the hosting layer *cannot* pre-resolve these because the timezone depends on field values the precept governs.

---

## Question 1: Is "Hosting Layer Handles Timezone Conversion" Actually Sufficient?

### Honest answer: No. Not for the class of problems Shane describes.

The "hosting layer handles it" argument works when the timezone is known *before* the precept fires — the hosting layer converts external temporal data into UTC instants and passes them in. This covers:

- Audit timestamps (`FiledAt`, `ApprovedAt`) — the hosting layer stamps UTC at the moment of the operation.
- SLA windows measured in absolute duration — "72 hours from incident" needs only instant comparison.
- Display conversion — showing a UTC instant as "10:30 AM EDT" on a user's screen.

It **does not** cover the case where the rule *itself* needs to compute a timezone-dependent boundary:

```
"The filing deadline is midnight on the 30th day after the incident,
 in the timezone of the state where the incident occurred."
```

Here's why:

1. The precept has a field `IncidentState` (e.g., "California").
2. The timezone for "California" is `America/Los_Angeles`.
3. The deadline is midnight in that timezone on a specific date — i.e., `2026-05-13T00:00:00 America/Los_Angeles`, which is `2026-05-13T07:00:00Z` (or `T08:00:00Z` during PST).
4. The guard compares `FiledAt` (an instant) against this computed deadline instant.

The timezone is an intermediate value *inside the rule*, derived from entity data. The hosting layer can't pre-compute it because it doesn't know the rule. The hosting layer passes in `IncidentState = "California"` and `FiledAt = instant("2026-05-13T06:30:00Z")`. The precept needs to determine whether that filing is before or after midnight Pacific time on the 30th.

I made an architectural argument that the hosting layer handles all timezone work. Shane has shown a class of problems where the timezone is **inside the rule**, not outside it. The hosting layer can't reasonably pre-resolve a timezone conversion that depends on a field value the precept owns. That's pushing domain logic out of the contract and into the hosting layer — the exact fragmentation Precept exists to prevent.

**My previous argument was sufficient for single-timezone and display-conversion scenarios. It is insufficient for multi-timezone domain rules where the timezone is entity data.**

---

## Question 2: The Multi-Region Insurance Example, Concretely

### What the author wants to express

```
The filing deadline is 11:59 PM local time on the 30th day after the incident,
in the state where the incident occurred.
```

Domain fields:
- `IncidentTimestamp` — when the incident happened (instant, UTC)
- `IncidentState` — where the incident happened (string or choice — this determines the timezone)
- `FiledTimestamp` — when the claim was filed (instant, UTC)

### Attempt 1: Without any timezone support (current Precept)

The precept cannot express this rule at all. The best the author can do:

```precept
precept InsuranceClaim

field IncidentTimestamp as instant
field FiledTimestamp as instant
field IncidentState as choice("CA", "NY", "TX", "FL", "IL")
field FilingDeadline as instant  # Pre-computed by hosting layer

# The only invariant Precept can enforce:
invariant FiledTimestamp <= FilingDeadline because "Filing deadline has passed"
```

The hosting layer must:
1. Look up the timezone for `IncidentState`.
2. Convert `IncidentTimestamp` to a local date in that timezone.
3. Add 30 days.
4. Set the time to 23:59:00 in that timezone.
5. Convert back to UTC.
6. Pass the result as the `FilingDeadline` argument.

**What's wrong with this:**

- **The rule is split.** Half of "filing deadline is midnight local time on the 30th day" lives in the precept (the comparison). The other half (the timezone-dependent computation) lives in the hosting layer. The "one file, complete rules" claim is visibly broken — an auditor reading the precept sees `FiledTimestamp <= FilingDeadline` but has no way to know what `FilingDeadline` means without reading the hosting layer code.
- **The hosting layer becomes a co-author of business logic.** The hosting layer now computes business-rule-relevant deadlines. If the deadline formula changes (e.g., "45 days" instead of "30 days"), two artifacts change — the hosting layer's deadline computation AND the precept's comments/documentation. The precept doesn't even contain the "30 days" number.
- **Inspectability degrades.** When you `precept_inspect` this definition, you see `FiledTimestamp <= FilingDeadline` — but the meaning of `FilingDeadline` is opaque. The inspector can't show you "the deadline is midnight Pacific time on the 30th day" because that logic isn't in the contract.
- **The hosting layer can silently compute wrong.** If the hosting layer has a bug in its timezone math (and timezone math is notoriously bug-prone), the precept enforces the wrong deadline. The contract is correct only if an external system supplies correct intermediate values — this is the exact "validator at a boundary" pattern that Precept's governance model claims to replace.

### Attempt 2: With timezone conversion functions (hypothetical)

If Precept supported timezone-aware conversion functions:

```precept
precept InsuranceClaim

field IncidentTimestamp as instant
field FiledTimestamp as instant
field IncidentState as choice("CA", "NY", "TX", "FL", "IL")

# Map state → IANA timezone (this could be a separate lookup, but for illustration)
field IncidentTimezone as string default "America/New_York"

# The complete rule — all in one file
invariant FiledTimestamp <= toInstant(
    toLocalDate(IncidentTimestamp, IncidentTimezone) + days(30),
    time(23, 59, 0),
    IncidentTimezone
) because "Claim must be filed by 11:59 PM local time on the 30th day after the incident"
```

Where:
- `toLocalDate(instant, timezone_string) → date` — extracts the local date for a given instant in a given timezone.
- `toInstant(date, time, timezone_string) → instant` — converts a local date + time + timezone back to a UTC instant.

**What's better about this:**

- **The rule is complete in one file.** "30 days," "11:59 PM," "in the incident timezone" — it's all here. An auditor reads the precept and sees the entire business rule.
- **The hosting layer just supplies data.** It provides `IncidentTimestamp` (when), `IncidentState`/`IncidentTimezone` (where), and `FiledTimestamp` (when they filed). No business logic in the hosting layer.
- **Inspectability is preserved.** `precept_inspect` can evaluate the full expression and show the computed deadline for any given entity state.
- **If the formula changes, one artifact changes.** 30 days becomes 45 days? Edit the precept. Done.

### Assessment

The gap in Attempt 1 is real. It is not a cosmetic inconvenience — it is a structural compromise of the "one file, complete rules" guarantee. When the precept author must split a business rule between the contract and the hosting layer, the contract has a hole. The hosting layer becomes a silent co-author of domain logic, which is the fragmented validation architecture Precept was designed to eliminate.

---

## Question 3: Is There a Middle Ground?

### Yes. Timezone *conversion functions* without `ZonedDateTime` as a *field type*.

This is the key insight Shane is pushing me toward, and I think it's correct. Let me be precise about what the middle ground looks like:

**What's NOT proposed:** `ZonedDateTime` as a field type. No field would ever store a `ZonedDateTime` value. No entity data would carry timezone metadata. The governing contract stores `instant` (UTC points on the timeline) and `string` (for timezone identifiers). `ZonedDateTime` as a *type* remains excluded.

**What IS proposed:** A small set of deterministic timezone conversion functions that take explicit timezone inputs:

| Function | Signature | Semantics |
|----------|-----------|-----------|
| `toLocalDate` | `(instant, string) → date` | Extract the local date for an instant in a timezone |
| `toLocalTime` | `(instant, string) → time` | Extract the local time for an instant in a timezone |
| `toInstant` | `(date, time, string) → instant` | Convert local date + time + timezone → UTC instant |

The timezone string is an explicit parameter — a field value or literal. It is not hidden in a type. It is not stored in entity metadata. It is visible in the expression.

**Properties of this approach:**

1. **No new types.** The DSL type surface stays the same: `instant`, `date`, `time`, `string`. No `ZonedDateTime` type. No `timezone` type. The conversion functions operate on existing types.

2. **The timezone is a string, not a type.** A timezone identifier like `"America/Los_Angeles"` is stored as a `string` field or passed as a `string` event argument. The precept author is responsible for the mapping (state → timezone). This keeps the entity model clean — no "timezone object" stored in entity data.

3. **Determinism scope.** The functions ARE deterministic for the same inputs (instant value + timezone string + TZ database version → same output). The TZ database dependency is explicit in the function call — you can see it in the expression. It is not hidden in a type.

4. **Composability with existing types.** `toLocalDate` returns a `date` — all date operations (comparison, `.year`, `.month`, `+ days(n)`) work. `toInstant` returns an `instant` — all instant operations (comparison, duration arithmetic) work. The conversion functions are bridges between the timeline domain and the calendar domain, which is exactly what they are in NodaTime.

5. **The philosophy gap narrows significantly.** With these functions, the multi-region insurance example is expressible entirely in the contract. The hosting layer supplies data. The precept owns the rule.

---

## Question 4: Re-examining the Determinism Argument

### Shane's challenge: "NodaTime already bundles the TZ database. Does refusing timezone operations actually avoid the dependency?"

**Honest answer: No. The dependency exists the moment we adopt NodaTime.**

Let me be precise about what's true:

1. **NodaTime bundles the IANA TZ database** via `NodaTime.TimeZones.TzdbDateTimeZoneSource`. When Precept uses NodaTime as a backing library, the TZ database is already in the dependency tree — whether Precept exposes timezone operations or not.

2. **Refusing to expose timezone operations doesn't remove the dependency.** It just means Precept pretends it doesn't exist. NodaTime's `LocalDate` arithmetic already relies on the Gregorian calendar system, which is also "external data" — it's just data that never changes. The TZ database is external data that changes ~20 times per year.

3. **The dependency IS in the binary.** If someone decompiles the Precept runtime DLL, they'll find NodaTime with its embedded TZ database. Refusing to use it in the DSL doesn't make it disappear. It just means the hosting layer uses it instead — same binary, same dependency, different code path.

**Where my previous argument was partially right:**

The dependency is in the tree, but the *exposure surface* matters. Today, Precept's determinism guarantee is: same `.precept` file + same data + same operation = same result. The input surface doesn't include "TZ database version" because no Precept operation touches timezone rules.

If we add timezone conversion functions, the input surface expands to include the TZ database version. This is a real change. But the question becomes: is this change *categorically different* from other environmental factors, or is it *the same kind of thing* that we already accept?

**Comparison with calendar system:**

- NodaTime's `LocalDate` depends on the Gregorian calendar system. If someone swapped the calendar system to Julian, `date("2026-03-15") + days(1)` would give a different result. We don't worry about this because the ISO calendar is fixed.
- The TZ database is *not* fixed — it changes. But it changes in predictable, versioned ways. And the changes are geopolitical (a country changes its DST rules), not computational.

**My revised position on the determinism argument:**

The TZ database dependency is real, but it's the same *category* of dependency as the .NET runtime version, the NuGet package version, or the NodaTime version itself. All of these are "if you change the dependency, the output might change." The TZ database just changes more frequently.

The question is whether Precept's determinism guarantee means:
- **(A)** "Same `.precept` file + same data = same result, always, on any machine, with any dependency version" — this is the strict interpretation I was defending. Under this reading, even `date` arithmetic is not truly deterministic (what if someone swapped to a Julian calendar?). The guarantee only holds given a fixed runtime environment.
- **(B)** "Same `.precept` file + same data + same runtime environment = same result" — this is what the guarantee actually means in practice. The runtime environment includes the .NET version, NodaTime version, and (if timezone functions exist) the TZ database version.

I was defending (A) for timezone operations while accepting (B) for everything else. That's inconsistent. The honest reading is (B) applies uniformly. And under reading (B), timezone conversion functions are deterministic — same inputs (including TZ database version as part of the runtime environment) always produce the same output.

**What this does NOT mean:** This does not make `ZonedDateTime` as a *field type* acceptable. The issue with `ZonedDateTime` as a type is not just determinism — it's that the type carries timezone identity *as entity data*, which couples the entity to a specific timezone interpretation. Timezone conversion functions are *operations on existing types* (instant, date, time, string), not a new type that carries zone identity.

---

## Question 5: What Does "Deterministic for the Same Inputs" Mean in Practice?

### The two-machine scenario

Machine A has IANA TZ database version 2026a. Machine B has version 2025f. Both evaluate:

```
toLocalDate(instant("2026-03-10T07:00:00Z"), "America/Los_Angeles")
```

If between 2025f and 2026a, the US changed its DST rules for March 2026 (it didn't, but hypothetically), these machines would produce different results.

**Is that "the same inputs" or "different inputs"?**

Under reading (B) from Question 4: these are **different inputs** — the runtime environment differs. The same way two machines running different .NET versions might get different floating-point rounding results for `decimal` operations. The guarantee is: same runtime environment + same precept + same data = same result. The TZ database version is part of the runtime environment.

**In practice, this matters much less than it sounds.**

- TZ database changes affect a tiny number of timezones per update. The 2024h update, for example, changed rules for exactly one country (Jordan).
- For the specific use case of US multi-state insurance deadlines, the US last changed its DST rules in 2007 (Energy Policy Act of 2005). The timezone rules for US states are *extremely* stable.
- Operators who run compliance-critical systems already manage TZ database freshness. This is standard operational practice for any system that handles timezone-sensitive data — banking, healthcare, aviation, logistics. It's not a new burden Precept introduces.
- NodaTime's `TzdbDateTimeZoneSource` has a `TzdbVersion` property. Precept could expose this in diagnostic/inspect output, making the TZ database version explicitly visible to operators.

**The practical risk is: near zero for the domains Precept targets.** The theoretical risk is: determinism has a wider input surface than "just the precept file." Both are true. The practical risk does not justify refusing to express real business rules.

---

## Question 6: The Philosophy Gap

### If Precept can't express "filing deadline is midnight in the jurisdiction of record," what does that mean?

Shane is right to frame this as a philosophy question. The philosophy says:

> "Every field, invariant, assertion, and transition lives in the `.precept` definition. There is no scattered logic across service layers."

If the filing-deadline rule must be split between the precept and the hosting layer, then the philosophy claim is violated for multi-timezone compliance domains. The prevention guarantee doesn't have a "gap" per se — the precept still prevents invalid configurations. But the *definition of what's invalid* is partly outside the contract. The hosting layer computes the deadline; the precept just enforces the comparison. The *meaning* of the deadline is lost from the contract.

This is a scope question. Two possible answers:

**(A) Precept's scope explicitly excludes timezone-dependent rules.** The philosophy could say: "Precept governs entity integrity for rules that are timezone-independent. Timezone-dependent rules require hosting-layer cooperation." This is honest but limiting. It means insurance, healthcare, and financial compliance domains — arguably Precept's primary targets — have a category of rules that the contract can reference but not fully express.

**(B) Precept's scope includes timezone-dependent rules through explicit conversion functions.** The philosophy's "one file, complete rules" claim holds for this domain, with the TZ database as an acknowledged runtime dependency (same as the .NET runtime or NodaTime package version). This extends the expressiveness surface without introducing new types.

### My assessment

**(B) is the stronger position.** Precept targets exactly the domains where timezone-dependent compliance rules appear. Excluding them creates a visible gap in the product's primary value proposition. The TZ database dependency is real but manageable — it's the same kind of environmental dependency that every production system already manages.

---

## What Changes in My Position

I am revising my position on timezone *operations* in the Precept DSL. My position on `ZonedDateTime` as a *type* does not change. Let me be precise:

### What changes

1. **Timezone conversion functions are viable.** A small set of functions — `toLocalDate(instant, string) → date`, `toLocalTime(instant, string) → time`, `toInstant(date, time, string) → instant` — can express timezone-dependent rules without introducing `ZonedDateTime` as a type.

2. **The determinism argument was overstated.** I applied a stricter determinism standard to timezone operations than to everything else. The honest reading is: determinism is relative to a fixed runtime environment, which includes the TZ database version. This is the same standard that applies to all other dependencies.

3. **The "hosting layer handles it" argument has a real boundary.** It works for single-timezone systems and for display conversion. It breaks down for multi-timezone domain rules where the timezone is entity data. I was wrong to present it as universally sufficient.

4. **The philosophy gap is real.** Without timezone conversion functions, Precept cannot fully express rules in its primary target domains (insurance, healthcare, finance). This is not a theoretical concern — Shane's multi-region insurance example is a commonplace compliance pattern.

### What doesn't change

1. **`ZonedDateTime` as a field type is still excluded.** Entity data should not carry timezone identity. Store instants (UTC), store timezone identifiers as strings, and convert in expressions when needed. `ZonedDateTime` as a type couples entity data to timezone interpretation — this remains architecturally wrong.

2. **The Fatal rating for `ZonedDateTime` as a type stands.** The concerns about component accessors, the expanded input surface, and the architectural principle of "store as Instant, display as local" are all valid — for the *type*. They are not valid objections to *conversion functions* that take explicit timezone inputs.

3. **Instant remains comparison-and-arithmetic only.** No component accessors on `instant`. If you need the local date or local time of an instant, you call a conversion function with an explicit timezone. This keeps the type clean and the timezone dependency visible.

### The revised architecture

| Responsibility | Owner | How |
|---------------|-------|-----|
| Record the point in time | Hosting layer | Passes `instant` value |
| Store the point in time | Precept field | `field FiledAt as instant` |
| Store the timezone identity | Precept field | `field JurisdictionTimezone as string` |
| Compute timezone-dependent boundaries | Precept expression | `toLocalDate(IncidentAt, JurisdictionTimezone) + days(30)` |
| Constrain relationships | Precept invariant | Full rule in the contract |
| Manage TZ database freshness | Operator | Operational concern (same as runtime/package versions) |
| Display in user's timezone | Hosting layer / presentation | Outside Precept's scope |

---

## Concrete DSL Sketch: Multi-Region Insurance

```precept
precept MultiRegionInsuranceClaim

field ClaimNumber as string notempty
field IncidentTimestamp as instant
field FiledTimestamp as instant
field IncidentState as choice("CA", "NY", "TX", "FL", "IL")
field IncidentTimezone as string notempty
field ClaimAmount as decimal default 0 nonnegative maxplaces 2
field ApprovedAmount as decimal default 0 nonnegative maxplaces 2

state Filed initial, UnderReview, Approved, Denied

# --- Timezone-dependent compliance rule (the entire rule in one place) ---

# Filing deadline: 11:59 PM local time on the 30th day after the incident,
# in the timezone where the incident occurred.
invariant FiledTimestamp <= toInstant(
    toLocalDate(IncidentTimestamp, IncidentTimezone) + days(30),
    time(23, 59, 0),
    IncidentTimezone
) because "Claim must be filed by 11:59 PM local time on the 30th day after the incident"

# --- Standard constraints ---
invariant ApprovedAmount <= ClaimAmount because "Approved amount cannot exceed claim amount"
invariant FiledTimestamp >= IncidentTimestamp because "Claim cannot be filed before the incident"

event Submit with Claimant as string, Amount as decimal, Timestamp as instant, State as string, Timezone as string
event Review
event Approve with Amount as decimal
event Deny

from Filed on Submit
    -> set ClaimAmount = Submit.Amount
    -> set IncidentTimestamp = Submit.Timestamp
    -> set FiledTimestamp = Submit.Timestamp
    -> set IncidentState = Submit.State
    -> set IncidentTimezone = Submit.Timezone
    -> transition UnderReview

from UnderReview on Approve when Approve.Amount <= ClaimAmount
    -> set ApprovedAmount = Approve.Amount
    -> transition Approved

from UnderReview on Deny -> transition Denied
```

### What the auditor sees

Reading only the `.precept` file, the auditor can verify:
1. The filing deadline formula (30 days, 11:59 PM, incident timezone).
2. That the timezone used is the *incident* timezone, not the claimant's or adjuster's.
3. That the comparison is against `FiledTimestamp`.
4. That all the numbers (30 days, 11:59 PM) are in the contract.

Without timezone conversion functions, the auditor sees `FiledTimestamp <= FilingDeadline` and has to read the hosting layer code to verify any of this.

---

## Design Considerations for Timezone Functions

### Validation of timezone strings

`toLocalDate(instant, "Not/A/Timezone")` should fail at runtime with a clear diagnostic, not silently produce garbage. The runtime should validate the timezone string against the TZ database and produce a constraint-violation-style rejection if invalid.

Open question: should the compiler warn on *literal* timezone strings that aren't valid IANA identifiers? Literal validation is feasible at compile time; field-value validation must be runtime.

### The `toInstant` ambiguity problem

When converting local date + time + timezone → instant, DST transitions can create ambiguity:

- **Gap:** Clocks spring forward. `2026-03-08T02:30:00 America/New_York` doesn't exist (clocks jump from 2:00 to 3:00).
- **Overlap:** Clocks fall back. `2026-11-01T01:30:00 America/New_York` is ambiguous (it occurs twice — once in EDT, once in EST).

NodaTime handles this with `ZoneLocalMappingResolver` — explicit strategies for gaps and overlaps. Precept would need a deterministic default:

- **Gaps:** Map to the instant *after* the gap (the standard "spring forward" behavior). NodaTime calls this `Resolvers.LenientResolver` for gaps.
- **Overlaps:** Map to the *later* instant (the offset *after* the transition). This matches NodaTime's `Resolvers.LenientResolver` for overlaps and is the safer choice for deadline calculations (it gives the later, more generous deadline).

The chosen resolution strategy should be documented in the language design doc and deterministic — no author choice, no hidden parameter. Same inputs = same output.

### Performance

Timezone conversions involve TZ database lookups. These are fast (NodaTime caches zone rules in memory), but they are not free. For the constraint-evaluation context (evaluating an invariant or guard), this is negligible — constraints run once per operation. It would matter more if Precept supported bulk evaluation, but it doesn't and won't in the foreseeable architecture.

### Phasing

These functions should NOT ship with the initial `instant` type. The phasing:

1. **v2:** `instant` type with comparison + duration arithmetic. No timezone operations.
2. **v2.x or v3:** Timezone conversion functions, gated on demand signal from enterprise domain adoption. This gives us time to get the `instant` foundation right before adding the timezone bridge.

The phasing is important because timezone conversion functions are a *policy decision* about determinism scope. `instant` comparison is unambiguously deterministic. Timezone conversion is "deterministic given the same TZ database version" — a weaker but defensible guarantee. Shipping them separately lets us validate the `instant` foundation independently.

---

## The Bottom Line

Shane's enterprise multi-timezone challenge exposes a real gap in my previous analysis. I was right that `ZonedDateTime` as a type is wrong for Precept. I was wrong that all timezone operations are categorically forbidden. The middle ground — timezone *conversion functions* that take explicit string inputs and return existing types — preserves the type model while closing the expressiveness gap.

The determinism concern was overstated. The TZ database is already in Precept's dependency tree via NodaTime. The conversion functions make the dependency *visible and explicit* in the DSL expression, which is better than pretending it doesn't exist while the hosting layer uses it behind the scenes.

The strongest argument is the philosophy one: Precept's target domains (insurance, healthcare, finance) are exactly the domains with timezone-dependent compliance rules. If the contract can't express those rules, it has a gap in its primary market. Timezone conversion functions close that gap without compromising the type model.

---

## References

- Previous analysis: `research/language/expressiveness/instant-zoneddatetime-reconsideration.md`
- NodaTime type model: `research/language/references/nodatime-type-model.md`
- Precept determinism guarantee: `docs/PreceptLanguageDesign.md` §Design Philosophy, Principle #1
- Precept prevention philosophy: `docs/philosophy.md` §What makes it different
- NodaTime zone resolver: https://nodatime.org/3.2.x/userguide/zoneddatetime-patterns
- IANA TZ database update frequency: https://data.iana.org/time-zones/releases/
