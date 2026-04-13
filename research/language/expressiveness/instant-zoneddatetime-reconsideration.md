# Instant and ZonedDateTime Reconsideration

**Research date:** 2026-04-13
**Author:** Frank (Lead/Architect, Language Designer)
**Context:** Response to Shane's counterarguments against the exclusions in `nodatime-precept-alignment.md`. Shane challenges the blanket exclusion of `Instant` and the "Fatal" determinism rating for `ZonedDateTime`.

---

## Background

In `nodatime-precept-alignment.md`, I excluded both `Instant` and `ZonedDateTime` from Precept's type surface:

- **`Instant`** — excluded because "this is system-generated clock data; Precept governs user-declared entity data, not timestamps" and "interpreting it requires a timezone — violates one-file completeness."
- **`ZonedDateTime`** — excluded with a **Fatal** determinism rating because "same `ZonedDateTime` value behaves differently depending on the IANA database version installed."

Shane presents three counterarguments. This document engages with each honestly, re-examines the Fatal rating, and arrives at a revised position.

---

## Counterargument 1: Event Timestamps as Readonly Context

### Shane's argument

The hosting layer passes an `Instant` (e.g., `Event.FiredAt`) as read-only context. The precept doesn't declare instant fields — it references instants that the hosting layer provides. Guards like "reject if this event fired more than 24 hours after the previous one" express real SLA patterns. The hosting layer resolves any display/timezone concerns; the precept just compares two instants.

### My assessment: Shane is substantially right.

My original exclusion conflated two operations:

1. **Comparing two Instants** — deterministic. An Instant is a count of nanoseconds since epoch. `FiredAt > PreviousTimestamp` is integer comparison with a semantic type wrapper. No timezone needed. No ambiguity. Same inputs, same result, always.

2. **Extracting components from an Instant** (year, month, day, hour) — non-deterministic without a timezone. This is where the one-file-completeness concern is real.

I treated these as inseparable. They are not. NodaTime's own type design proves it: `Instant` has *no* component accessors. You must explicitly convert to `ZonedDateTime` or `LocalDateTime` to access `.Year`, `.Month`, etc. The absence of component accessors on `Instant` is a deliberate design choice — NodaTime forces you to supply a timezone before extracting human-readable components.

Precept can make the same choice: allow `Instant` with comparison and duration arithmetic, disallow component extraction entirely. The determinism guarantee holds because the operations Precept permits on instants are all operations on a single scalar quantity (nanoseconds since epoch).

### What changes in my analysis

The `Instant` row in the type mapping table was wrong. The rationale "interpreting it requires a timezone" is only true for component extraction, not for the operations Precept would actually support. The corrected assessment:

| Operation | Deterministic? | Requires TZ? |
|-----------|---------------|-------------|
| `instant == instant` | Yes | No |
| `instant < instant` | Yes | No |
| `instant - instant → duration` | Yes | No |
| `instant + duration → instant` | Yes | No |
| `instant.Year` | **No** | **Yes** |
| `instant.Hour` | **No** | **Yes** |

If Precept exposes only the first four operations and structurally prevents the last two, the determinism guarantee is preserved by construction — the same way NodaTime itself preserves it.

### The SLA pattern is real

Shane's example — "reject if this event fired more than 24 hours after the previous one" — appears in at least three of our surveyed domains:

- **Insurance claims:** "Claim must be filed within 72 hours of the incident" (HIPAA/regulatory)
- **IT helpdesk:** "Escalation SLA: if not resolved within 4 business hours, auto-escalate"
- **Maintenance work orders:** "Emergency work orders must be acknowledged within 2 hours"

These are not edge cases. They are bread-and-butter domain rules that Precept should be able to express. Without `instant`, authors encode timestamps as `number` fields (epoch seconds), losing type safety and writing guards like `when FiledAt - IncidentAt < 259200` instead of `when FiledAt - IncidentAt < hours(72)`. That is exactly the readability and type-safety failure that the `date` type was designed to prevent for calendar dates. I was creating the same gap for timestamps.

---

## Counterargument 2: Audit Trail Fields

### Shane's argument

Compliance domains (SOX, HIPAA) require fields like `LastModifiedAt` that carry timezone information. Today Precept says "handle that in the hosting layer," but if Precept governs more of the entity lifecycle, this boundary gets uncomfortable. Shane calls this "critical, especially this one."

### My assessment: Shane is right about the problem, wrong about the solution.

**The problem is real.** If Precept declares every field, constraint, and lifecycle rule for an entity — but the most important compliance data (`CreatedAt`, `LastModifiedAt`, `ApprovedAt`) lives outside the governing contract — then Precept's "one file, complete rules" claim has a visible gap. An auditor asks "does the contract guarantee that `LastModifiedAt` is always after `CreatedAt`?" and the answer is "the contract can't express that." That's uncomfortable, and it gets worse as Precept governs more of the entity lifecycle.

**But the solution is `Instant`, not `ZonedDateTime`.** Shane conflates "carries timezone information" with "stored as ZonedDateTime." NodaTime's own guidance is explicit: for globally-distributed systems, store as `Instant`, display in the user's timezone at the presentation layer. The audit timestamp `2026-04-13T14:30:00Z` is a point on the universal timeline. Whether it displays as "10:30 AM EDT" or "7:30 AM PDT" is a presentation concern that belongs in the hosting layer, not in the governing contract.

An `instant` field in a precept (`field LastModifiedAt as instant`) gives compliance domains exactly what they need:

```precept
# Audit ordering constraint — fully deterministic
invariant LastModifiedAt >= CreatedAt because "Modified timestamp must be at or after creation"

# Regulatory filing window
invariant FiledAt - IncidentAt <= hours(72) because "SOX: must file within 72 hours of incident"
```

The hosting layer populates `LastModifiedAt` with the current `Instant` at each operation. The precept constrains the relationships between timestamps. Display timezone is presentation, outside Precept's scope. The contract is complete for the compliance constraint surface.

### What changes in my analysis

The original exclusion statement "Audit logs, system timestamps — outside Precept's governance scope" was too broad. The *recording* of timestamps is outside Precept's scope (the hosting layer provides the values). The *constraining* of timestamp relationships is squarely inside Precept's scope — it is data integrity, the thing Precept exists to govern. Refusing to support timestamp constraints because the values originate externally is like refusing to support string constraints because the user typed the string.

The key insight: `Instant` fields are typically *hosting-layer-populated and precept-constrained*. The hosting layer owns the clock; the precept owns the rules about what the clock values must satisfy. This is a natural division of responsibility.

---

## Counterargument 3: "Store as Instant, Display as Local"

### Shane's argument

NodaTime explicitly recommends this pattern for globally-distributed systems. Precept could support `Instant` fields where storage is deterministic (nanoseconds since epoch) and any display conversion happens in the hosting layer, not in the precept.

### My assessment: Shane is exactly right, and this argument actually reinforces the ZonedDateTime exclusion.

The "store as Instant, display as local" pattern is NodaTime's canonical recommendation. It is the correct architectural pattern for any system that stores temporal data. And it maps perfectly to Precept's architecture:

| Responsibility | Owner | Type |
|---------------|-------|------|
| Record the point in time | Hosting layer | `Instant` |
| Store the point in time | Precept field | `instant` |
| Constrain relationships between points in time | Precept contract | Comparison and duration arithmetic |
| Display in the user's timezone | Hosting layer / presentation | `ZonedDateTime` (outside Precept) |

This is the same separation Precept already makes for other types. The precept stores `field Amount as decimal` — it doesn't know whether the UI displays it as "$1,234.56" or "1.234,56 €". Currency formatting is a presentation concern. Timezone display is the temporal equivalent.

The important corollary: **this pattern means `ZonedDateTime` in a precept is architecturally wrong.** If the precept stores a `ZonedDateTime`, it's storing presentation-layer information (the timezone) in the domain contract. That's the same category error as storing `"$1,234.56"` as a string instead of `1234.56` as a decimal. NodaTime's own design philosophy argues against it, and so does Precept's.

---

## Re-examining the "Fatal" Rating for ZonedDateTime

### Shane's challenge

"The argument about the TZ database doesn't really concern me — whoever is operating this has the responsibility to keep that in sync." Shane frames TZ database maintenance as an operational concern, not a language design concern.

### My revised assessment: The rating stands, but the reasoning needs refinement.

Shane is correct that TZ database freshness is an operational concern. But the determinism problem with `ZonedDateTime` in Precept is not about freshness — it's about what the determinism guarantee actually means.

**Today's guarantee (Principle #1):** Same `.precept` file + same field values + same event arguments = same outcome. Always. The input surface is: the precept definition, the current data, and the operation. That's it.

**With ZonedDateTime, the guarantee becomes:** Same `.precept` file + same field values + same event arguments + **same IANA TZ database version** = same outcome. The input surface now silently includes an external, unversioned, continuously-updating database that is invisible in the `.precept` file, invisible in the entity data, and invisible in the operation.

This is categorically different from other runtime dependencies:

| Dependency | Visible? | Versioned? | Changes silently? |
|-----------|---------|-----------|-------------------|
| NuGet package version | Yes (lock file) | Yes | No |
| Precept runtime version | Yes (package ref) | Yes | No |
| .NET runtime version | Yes (target framework) | Yes | No |
| IANA TZ database | **No** | **Loosely** | **Yes — updated ~20x/year** |

Shane says "whoever is operating this has the responsibility to keep that in sync." True. But Precept's determinism guarantee is a *structural* guarantee — it holds because the runtime *structurally prevents* nondeterministic operations, not because operators promise to be careful. "The operator will keep it in sync" is the same category of argument as "the developer will call the validator" — it's the convention-based assurance that Precept exists to replace with structural prevention.

**However,** I want to be precise about what "Fatal" means. The TZ database concern is fatal *for `ZonedDateTime` as a Precept type* — meaning guards and constraints could reference `.Hour`, `.DayOfWeek`, and other component accessors that depend on the TZ database. It is NOT fatal for `Instant`, because Instant comparison and duration arithmetic are timezone-independent.

The refined boundary:

| Type | Component accessors? | TZ database dependency? | Determinism impact |
|------|---------------------|------------------------|-------------------|
| `date` (`LocalDate`) | `.year`, `.month`, `.day` | None | None — calendar is fixed (ISO) |
| `time` (`LocalTime`) | `.hour`, `.minute` | None | None — no timezone concept |
| `instant` (`Instant`) | **None permitted** | None | **None — comparison-only** |
| `ZonedDateTime` | `.year`, `.month`, `.day`, `.hour` | **Required** | **Fatal — TZ rules change** |

The line is: **types with timezone-independent component accessors are safe. Types that require timezone rules for component extraction are not.** `Instant` without component accessors is on the safe side. `ZonedDateTime` is on the unsafe side. The Fatal rating for `ZonedDateTime` stands.

---

## The "What If You Don't Include Them" Test

This is the strongest argument for `Instant`, independent of Shane's counterarguments.

### The date type analogy

We created the `date` type because domain fields like `DueDate`, `ExpirationDate`, and `RenewalDate` have calendar semantics that `number` cannot express. Using `number` loses semantic type safety: `DueDate + MealsTotal` compiles, `DueDate + date("2026-01-15")` doesn't, and arithmetic like `+ months(1)` is impossible without a date-aware type.

The same argument applies to timestamps with greater force:

| Without `date` | Without `instant` |
|----------------|-------------------|
| `field DueDate as number` | `field FiledAt as number` |
| `DueDate + MealsTotal` compiles ✗ | `FiledAt + MealsTotal` compiles ✗ |
| `DueDate > ExpirationDate` — no domain semantics | `FiledAt > IncidentAt` — no domain semantics |
| Author writes `DueDate + 30` meaning "30 days" | Author writes `FiledAt + 259200` meaning "72 hours in seconds" |
| Reader cannot distinguish date from count | Reader cannot distinguish timestamp from arbitrary number |

If the `date` type is justified — and it is, it was justified by 30 of 100 surveyed fields needing date semantics — then `instant` is justified by the same reasoning applied to temporal points. The alternative (encoding timestamps as numbers) is exactly the semantic-type-safety failure that Precept's type system is designed to prevent.

### Domains that need instant

From the existing sample corpus and the surveyed domain set:

- **Insurance claim:** `IncidentTimestamp`, `FiledTimestamp`, `AdjustedAt` — SLA and regulatory timing constraints
- **IT helpdesk:** `ReportedAt`, `AcknowledgedAt`, `ResolvedAt` — escalation SLA enforcement
- **Maintenance work order:** `RequestedAt`, `ScheduledAt`, `CompletedAt` — service window enforcement
- **Hiring pipeline:** `AppliedAt`, `InterviewedAt`, `OfferExpiresAt` — process timing constraints
- **Loan application:** `SubmittedAt`, `ApprovedAt`, `DisbursedAt` — regulatory timing requirements

These are not exotic edge cases. They are standard compliance and SLA patterns in domains Precept already targets.

---

## Revised Position

### What changes

I am revising my position on `Instant`. The original analysis was wrong to exclude it.

**`Instant` is viable as a Precept type** under the following design constraints:

1. **Comparison operators only.** `==`, `!=`, `<`, `>`, `<=`, `>=` on instants — all fully deterministic.
2. **Duration arithmetic.** `instant - instant → duration`, `instant ± duration → instant` — all fully deterministic.
3. **No component accessors.** No `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`. These require a timezone and would violate determinism. A compile-time error (new diagnostic) should fire if an author attempts member access on an instant.
4. **ISO 8601 UTC literal.** `instant("2026-04-13T14:30:00Z")` — trailing `Z` required, always UTC.
5. **Serialization as ISO 8601 UTC string.** MCP tools emit `"2026-04-13T14:30:00Z"` — timezone-free, parseable, deterministic.

**`Duration` is viable as an expression-result type** (not necessarily a declared field type in v1):

1. The result of `instant - instant` is a `Duration` — a fixed count of nanoseconds.
2. Literal durations via constructor functions: `hours(72)`, `minutes(30)`, `seconds(3600)`.
3. Duration comparison: `d1 < d2`, `d1 == d2` — deterministic.
4. Duration accessors: `.totalHours`, `.totalMinutes`, `.totalSeconds` — deterministic (these are just unit conversions on a scalar, no timezone involved).
5. Whether `duration` becomes a declared field type is a separate proposal question. For v1 of instant, expression-only duration (as `Period` is for `date`) is sufficient.

### What doesn't change

**`ZonedDateTime` remains excluded.** The Fatal rating stands. Shane's counterarguments do not overcome the structural determinism concern:

- The "operational concern" reframing weakens Precept's structural guarantee to a convention-based guarantee.
- The "store as Instant, display as local" pattern proves the correct architecture has no `ZonedDateTime` in the domain contract — only `Instant`.
- Every use case Shane identifies (audit timestamps, SLA enforcement, compliance timing) is served by `Instant` with comparison semantics. None requires the precept to know a timezone.

**`OffsetDateTime` remains excluded.** Same reasoning — carries timezone information that creates external dependency.

### Revised type mapping table

| NodaTime Type | Precept DSL Candidate? | DSL Surface Name | Determinism | Phase |
|---|---|---|---|---|
| `LocalDate` | **Yes** | `date` | Full | v1 (#26) |
| `LocalTime` | **Deferred** | `time` | Full | v2 |
| `LocalDateTime` | **Deferred** | `datetime` | Full | v3 |
| `Instant` | **Yes — reconsidered** | `instant` | **Full (comparison + duration arithmetic only)** | **v2** |
| `Duration` | **Expression-result only** | N/A (expression type) | Full | **v2 (with instant)** |
| `Period` | **Internal only** | N/A | Full | v2 (with month/year arithmetic) |
| `ZonedDateTime` | **No — Fatal** | N/A | **Broken — TZ database dependency** | Never |
| `OffsetDateTime` | **No** | N/A | Broken — offset without zone rules | Never |

### The clear boundary

Precept supports temporal types that are **deterministic by construction**:

- **Permitted:** Types whose every Precept-exposed operation produces the same output for the same input, with no external database, no system clock, and no deployment context. `LocalDate`, `LocalTime`, `LocalDateTime`, and `Instant` (comparison-only) all qualify.
- **Excluded:** Types whose operations depend on external state (TZ database) that is invisible in the `.precept` file. `ZonedDateTime` and `OffsetDateTime` are excluded.

The line is drawn at **operational determinism** — whether the type's *permitted operations* (not its full theoretical capability) are deterministic. `Instant` has non-deterministic *potential* operations (component extraction), but Precept does not permit those operations, so the type is deterministic within Precept's surface. This is the same principle as allowing `number` despite division-by-zero being theoretically possible — the runtime handles the specific failure mode rather than excluding the entire type.

---

## Sketch: What the DSL Surface Would Look Like

```precept
precept InsuranceClaim

field ClaimNumber as string notempty
field IncidentTimestamp as instant
field FiledTimestamp as instant
field Amount as decimal nonnegative

state Filed initial, UnderReview, Approved, Denied, Closed

# Audit ordering — fully deterministic instant comparison
invariant FiledTimestamp >= IncidentTimestamp because "Claim cannot be filed before the incident occurred"

# Regulatory SLA — duration arithmetic, no timezone
invariant FiledTimestamp - IncidentTimestamp <= hours(72) because "HIPAA: claim must be filed within 72 hours of incident"

# Event arg with instant — hosting layer provides the timestamp
event Approve with ApprovedAt as instant

# Guard using instant comparison
from UnderReview on Approve when ApprovedAt > FiledTimestamp
    -> set Amount = Amount -> transition Approved

# State assert using instant
to Approved assert ApprovedAt != null because "Approval requires a timestamp"
```

### Design notes on this sketch

1. **`instant` is a scalar type**, same as `date`, `string`, `number`. Declared with `field ... as instant` or `... as instant` in event args.
2. **`instant(...)` constructor** accepts ISO 8601 UTC strings: `instant("2026-04-13T14:30:00Z")`. The trailing `Z` is mandatory — instants are UTC by definition.
3. **Duration constructor functions** — `hours(n)`, `minutes(n)`, `seconds(n)` — produce Duration values for comparison. These are thin wrappers around `NodaTime.Duration.FromHours(n)` etc.
4. **`instant - instant`** produces a Duration. **`instant ± duration`** produces an Instant. **`duration <=> duration`** comparison is supported.
5. **No `.year`, `.month`, `.day`** etc. on instant. Attempting member access is a compile-time error. If you need "the month this happened in," convert in the hosting layer and pass a `date` or `integer`.
6. **`nullable` is natural** — not all instants are known at entity creation. `field ApprovedAt as instant nullable` is idiomatic for timestamps populated by later events.

---

## Shane's Philosophical Challenge: "Why Not Just Leverage NodaTime?"

Shane asks: "The hard part is already done by NodaTime, why not just leverage and expose it?"

The answer is: **we should leverage it. We should not expose all of it.**

NodaTime's type model is excellent. Precept should use it as the backing library for all temporal types. But NodaTime is a *general-purpose* temporal library — it provides types for every temporal concept, including concepts that violate Precept's guarantees. Precept's job is to select the subset of NodaTime's type model that preserves determinism and expose only that subset through the DSL.

This is not a failure to leverage NodaTime. It is exactly how Precept uses every backing library:

- **System.Decimal** backs `decimal`, but Precept doesn't expose `Decimal.ToString("C")` (culture-dependent formatting).
- **NodaTime.LocalDate** will back `date`, but Precept won't expose `LocalDate.WithCalendar(CalendarSystem.Hebrew)` (non-ISO calendars).
- **NodaTime.Instant** should back `instant`, but Precept won't expose `Instant.InZone(DateTimeZone)` (timezone-dependent conversion).

The pattern is consistent: use the best backing type, expose only the operations that preserve Precept's guarantees. NodaTime makes this easy because its type model already separates deterministic operations from timezone-dependent ones. Precept just draws the line at the same boundary NodaTime draws — it doesn't invent a new one.

Shane's TZ database concern — "whoever is operating this has the responsibility to keep that in sync" — is valid for the hosting layer. It is not valid for the governing contract. The governing contract must be self-contained and deterministic. The hosting layer can do whatever NodaTime supports, including `ZonedDateTime`, and pass the results into the precept as `instant` values. The precept constrains the results; the hosting layer handles the conversion. Separation of concerns, applied to temporal types.

---

## Recommendation

1. **Add `instant` to the Precept type roadmap at v2** (alongside `time` and month/year arithmetic). It should ship as a separate proposal with its own issue, design review, and implementation plan.

2. **`Duration` as expression-result type in v2.** Ships with `instant` — the arithmetic companion type. Whether `duration` becomes a declarable field type is a v3 decision based on domain demand.

3. **`ZonedDateTime` remains permanently excluded.** The Fatal rating stands. Every use case that appears to need `ZonedDateTime` is actually served by `instant` + hosting-layer display conversion.

4. **Update the `nodatime-precept-alignment.md` type mapping table** to reflect the `Instant` reclassification when the `instant` proposal is written.

5. **A new proposal issue should be created** for `instant` (type, comparison, duration arithmetic, constructor functions). It should reference this reconsideration document as the design rationale and the NodaTime feasibility analysis as the technical foundation.

---

## Acknowledgment

Shane's counterarguments exposed a real flaw in my original analysis. I conflated "Instant requires interpretation" (true for component extraction) with "Instant is non-deterministic" (false for comparison and duration arithmetic). The corrected position — `Instant` with comparison-only semantics is deterministic by construction — follows directly from the NodaTime type model I had already analyzed. I should have seen this the first time. The SLA/compliance use cases were visible in the domain survey; the type-safety gap was analogous to the `date` justification. I drew the exclusion line in the wrong place.

The ZonedDateTime exclusion was correct, and Shane's counterarguments actually strengthen it by demonstrating that the "store as Instant, display as local" pattern eliminates the need for `ZonedDateTime` in the governing contract.

---

## References

- NodaTime Instant type: `research/language/references/nodatime-type-model.md` §Instant
- Original exclusion analysis: `research/language/expressiveness/nodatime-precept-alignment.md` §2 Type Mapping
- Precept determinism guarantee: `docs/PreceptLanguageDesign.md` §Design Philosophy, Principle #1
- Precept prevention philosophy: `docs/philosophy.md` §What makes it different
- NodaTime type choice guidance: https://nodatime.org/3.2.x/userguide/type-choices
