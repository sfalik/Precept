# Type System Follow-ons

**Research date:** 2026-04-08
**Author:** Steinbrenner (PM)
**Relevance:** Batch 3 horizon-domain follow-on for the existing type-system corpus. This document is intentionally subordinate to `type-system-domain-survey.md` and `references/type-system-survey.md` — it is a continuation lane for residual gaps that remain **after** the main `choice` / `date` / `decimal` / `integer` sweep, not a separate type-system program.

---

## Background and Problem

The main type-system corpus already establishes the first expansion wave: `choice`, `date`, `decimal`, and `integer`. That sweep closes the broad, repeatable gaps shown across the sample corpus and the 10-domain field survey: typo-prone value sets, calendar dates modeled as numbers, financial values modeled as IEEE 754 doubles, and whole-number concepts modeled as fractional numbers.

After that sweep, only a small residual lane remains. The important planning point is that this lane should **not** create parallel proposal churn while the main type additions are still being researched, reviewed, or implemented. The remaining pressure is real, but it is narrower and more philosophy-sensitive:

- **duration / interval values** survive because some business entities store elapsed or contractual time spans directly, not just dates;
- **attachment / document-reference values** survive because many domains govern required evidence, uploaded files, or externally stored documents as part of entity integrity;
- most other candidate-looking gaps collapse into **constraints**, **paired fields**, or explicit non-goals once the main type sweep lands.

This document therefore asks a narrower question than the main type survey: **which residual domains still deserve future proposal attention once the main type work is done, and what evidence or semantic discipline must exist before that happens?**

---

## Why this is a continuation lane, not a new program

The main type-system corpus covers the structural language question: what scalar vocabulary does Precept need to stop mis-typing ordinary business fields?

This follow-on lane covers only the leftovers that remain **after** that correction:

1. cases where `choice` / `date` / `decimal` / `integer` still cannot say something domain-authors need to say;
2. cases where constraints do not solve the gap because the missing thing is not just validation, but **value semantics**;
3. cases where the new type would still fit Precept's flat, deterministic, inspectable, single-entity model.

If a candidate can be handled by:

- `decimal` + `choice` (for example money with a currency code),
- `string` / `integer` / `decimal` plus field-level constraints (for example URL, email, phone, percentage, bounded codes),
- or explicit entity modeling outside the field type system,

then it does **not** belong in this continuation lane.

---

## Residual Domain Map After the Main Sweep

| Residual area | Why the main sweep does not fully absorb it | Current status |
|---|---|---|
| Duration / interval values | `date` gives day-granularity points in time, but not reusable elapsed spans, notice periods, SLAs, grace windows, or contract terms as first-class values. | **Future-facing candidate** |
| Attachment / document-reference values | `choice`, `date`, `decimal`, and `integer` can model metadata around a document, but not the governed existence of a document-bearing value or stable document reference. | **Future-facing candidate** |
| Time-of-day / instant / timezone-aware datetime | Still blocked by determinism and deployment-context questions. | **Rejected for now** |
| Money / currency-coupled numeric types | Still better modeled as `decimal` amount + `choice(...)` currency code. | **Rejected for now** |
| Email / URL / phone / percentage / formatted identifiers | Stronger fit for constraints than for new types. | **Absorbed by constraints** |
| Record / struct / foreign-key / external reference graph types | Conflicts with flat-field, single-entity philosophy and introduces object-graph semantics. | **Rejected for now** |

The conclusion is intentionally conservative: the real residual proposal lane is short. The strongest survivors are temporal spans and document-bearing values.

---

## Sample and Domain Pressure

The current sample corpus already shows the edges of both surviving gaps.

### 1. Duration pressure

Several samples still encode elapsed or contractual spans indirectly as raw numbers:

- `clinic-appointment-scheduling.precept` uses `ScheduledDay` and `MinuteOfDay`;
- `library-book-checkout.precept` uses `CurrentDay`, `CheckoutDay`, `DueDay`, `LoanDays`, and `ExtraDays`;
- `library-hold-request.precept`, `parcel-locker-pickup.precept`, and similar samples use day counters and reminder windows;
- `travel-reimbursement.precept` uses `TripDays`;
- `maintenance-work-order.precept` uses `EstimatedHours` and `ActualHours`.

Some of that pressure disappears once `date` and `integer` ship. But not all of it disappears. A field like `TripDays` or `EstimatedHours` is not a calendar date. It is a stored span. The gap that remains is: **Precept can represent points in time, but not elapsed contractual or operational time spans as a distinct value kind.**

### 2. Document / attachment pressure

The sample corpus also shows domains where integrity depends on document-bearing evidence:

- `insurance-claim.precept` models `MissingDocuments` as `set of string`;
- `loan-application.precept` gates approval on `DocumentsVerified`;
- document collection, proof submission, and evidence completion recur in the insurance, lending, and outage-reporting style examples.

The domain survey already found the stronger cross-domain result: attachment or document-reference needs keep reappearing even after the main scalar sweep. That is exactly the kind of residual evidence this lane exists to hold: not a broad type-system hole, but a repeated domain concept that still lacks a clean value contract.

---

## Precedent Survey

This section stays narrow on purpose: not a new omnibus survey, but the precedents most relevant to the two strongest residual candidates.

### Duration / interval precedents

| System | What it shows | Why it matters for Precept |
|---|---|---|
| PostgreSQL `interval` — <https://www.postgresql.org/docs/current/datatype-datetime.html> | `interval` is a distinct type from `date`, `time`, and `timestamp`, and supports field-restricted variants like `YEAR TO MONTH` and `DAY TO SECOND`. | Confirms that "point in time" and "span of time" are separate type problems, and that spans split into materially different semantic families. |
| Camunda FEEL temporal expressions — <https://docs.camunda.io/docs/components/modeler/feel/language-guide/feel-temporal-expressions/> | FEEL distinguishes **days-time duration** from **years-months duration**, and defines different arithmetic for dates, times, date-times, and durations. | Strong warning against a single fuzzy `duration` type that mixes elapsed time with calendar-relative periods. |
| .NET `TimeSpan` — <https://learn.microsoft.com/en-us/dotnet/api/system.timespan> | `TimeSpan` is explicitly a time interval, with component and total-value accessors. | Good precedent for elapsed-duration semantics on a .NET runtime, especially if Precept ever wants an "hours/minutes/days" style span that is not month-aware. |
| Python `timedelta` — <https://docs.python.org/3/library/datetime.html#timedelta-objects> | Python keeps date values, datetime values, and `timedelta` separate, and documents naive/aware temporal distinctions clearly. | Reinforces the split between stable local value semantics and timezone-bearing values, which matters for Precept's determinism promise. |

**Precedent takeaway:** duration is plausible, but only when its semantic family is sharply defined. The evidence does **not** support a catch-all temporal blob that merges date, time, timezone, and duration into one type surface.

### Attachment / document-reference precedents

| System | What it shows | Why it matters for Precept |
|---|---|---|
| Dataverse file columns — <https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-column-data> | File columns are not ordinary scalar columns: retrieval returns a file id, uploads use dedicated operations, and file metadata lives alongside a `FileAttachment` relationship. | Strong evidence that document-bearing values are usually reference-and-metadata semantics, not simple inline scalars. |
| Dataverse files/images overview — <https://learn.microsoft.com/en-us/power-apps/developer/data-platform/files-images-overview> | Dataverse explicitly separates file columns, image columns, and legacy attachments, and does not return file contents inline with ordinary record retrieval. | Confirms that binary payload governance and ordinary field retrieval are different product surfaces. |
| Salesforce `ContentDocument` / `ContentDocumentLink` — <https://developer.salesforce.com/docs/atlas.en-us.object_reference.meta/object_reference/sforce_api_objects_contentdocument.htm>, <https://developer.salesforce.com/docs/atlas.en-us.object_reference.meta/object_reference/sforce_api_objects_contentdocumentlink.htm> | Salesforce models the file and the file-to-record link as separate objects. | Reinforces the "document link plus metadata" pattern instead of inline blob semantics. |
| ServiceNow Attachment API — <https://www.servicenow.com/docs/r/api-reference/rest-apis/c_AttachmentAPI.html> | Attachments live in attachment records with dedicated upload/retrieve/delete APIs, not as ordinary scalar fields. | Again confirms that document-bearing values have lifecycle and transport semantics beyond plain strings or numbers. |
| FHIR `Attachment` — <https://hl7.org/fhir/R4/datatypes.html#Attachment> | A healthcare-standard attachment value carries structured metadata like content type, language, data/url, size, hash, and title. | Important precedent for what a medically or regulatorily serious attachment value actually needs to say. |
| FHIR `DocumentReference` — <https://hl7.org/fhir/R4/documentreference.html> | Distinguishes the document itself from the metadata used to discover and manage that document. | This is the cleanest precedent for a governed **document reference** rather than a governed inline document payload. |

**Precedent takeaway:** the dominant pattern across enterprise platforms and standards is separation:

- document content is stored elsewhere or handled through dedicated operations;
- the main entity stores metadata, identifiers, or links;
- "document required" integrity is enforced against the presence and properties of that reference, not against inline file bytes.

That pattern aligns much better with Precept than an inline binary type would.

---

## Philosophy Fit

### 1. Duration / interval values

Duration fits Precept **only** if it remains explicit, deterministic, and algebraically small.

**Why it can fit**

- It protects domain intent better than raw `integer` counts when the field's meaning is "grace period", "lead time", "retention window", or "estimated work duration".
- It is inspectable if the literal form is explicit and the allowed operators are tightly closed.
- It can stay flat and keyword-anchored if it is declared inline like other field types and uses constructor-style literals rather than overloaded strings.

**Why it can easily fail the philosophy screen**

- A single duration type that mixes hours, days, months, and years hides multiple incompatible calendars under one keyword.
- Timezone-aware or clock-aware duration math starts to drag Precept toward host-environment temporal semantics.
- A rich temporal library with formatting, parsing modes, and locale behavior would quickly exceed the language's domain-integrity purpose.

**Philosophy conclusion**

Duration is a valid continuation candidate only if Precept can define a very small, deterministic contract around it. If that contract cannot stay small, the type should remain deferred.

### 2. Attachment / document-reference values

Document-bearing values fit Precept **only** if Precept governs their declarative presence and metadata, not their transport workflow.

**Why they can fit**

- Many governed entities are invalid without required evidence: ID documents, police reports, invoices, approvals, scans, photos, signed forms.
- "Does this entity possess the required document reference with the right declared metadata?" is a genuine integrity question, not just UI plumbing.
- A stable reference value can be inspectable: filename, content type, size, hash, reference id, or URL are all declarative facts about the entity.

**Why they can fail the philosophy screen**

- If the runtime has to upload, download, stream, preview, virus-scan, or dereference remote content implicitly, the field stops being a declarative value and becomes an integration subsystem.
- If document references become cross-entity foreign keys with ambient repository semantics, Precept leaves the single-entity governance lane.
- If the type requires nested object modeling or arbitrary metadata bags, it collides with the flat-field philosophy.

**Philosophy conclusion**

The only philosophy-fitting version of this idea is a **reference-oriented** contract: a stable, inert, inspectable document value or document-reference value that can participate in constraints and transitions without hidden I/O.

---

## Semantic Contracts the Future Proposals Would Need

### Duration / interval

Before duration becomes a proposal, the contract must answer the following explicitly:

1. **Which duration family exists?**
   - elapsed-only (`days`, `hours`, `minutes`) like `.NET TimeSpan`;
   - calendar-relative (`months`, `years`) like FEEL `years-months-duration`;
   - or a split model with two distinct duration kinds.

2. **What is the literal / constructor form?**
   - constructor-style is the most likely fit because it keeps non-trivial temporal literals explicit.

3. **What arithmetic is closed?**
   - `date + duration`;
   - `date - duration`;
   - `duration + duration`;
   - `duration / integer`;
   - and which combinations are type errors.

4. **What accessors exist?**
   - component accessors must be fixed and deterministic;
   - no locale-aware or formatting-heavy surface.

5. **What is explicitly out of scope?**
   - timezones, daylight-saving interpretation, locale formatting, and host clock dependence.

### Attachment / document-reference

Before document-bearing fields become a proposal, the contract must answer the following explicitly:

1. **Is the value a document reference, an attachment metadata value, or two separate concepts?**
2. **What fields are intrinsic to the value?**
   - likely candidates: content type, file name, byte size, hash, stable id, URL/reference target.
3. **What operations are allowed in expressions?**
   - presence checks, equality, metadata comparisons, maybe collection membership;
   - no implicit fetching or content inspection.
4. **How does nullability work?**
   - absent document versus present-but-incomplete metadata must be unambiguous.
5. **What is not the runtime's job?**
   - upload/download, repository traversal, foreign-key navigation, content parsing, OCR, or antivirus concerns.

---

## Dead Ends and Rejected Directions

### 1. One giant temporal type

`date`, `time`, timezone-aware `datetime`, and duration should not be collapsed into one temporal mega-type. The precedent set points the opposite direction: systems split these concepts because their operator rules differ materially. Precept should keep that separation.

### 2. Inline binary / blob fields

A `blob` or raw `bytes` field would turn Precept into a storage and transport surface rather than a governance language. The dominant platform precedents treat file payloads as specialized operations or external resources. Precept should not pull binary streaming into ordinary field semantics.

### 3. Attachment fields as hidden external lookups

A field that silently dereferences external repositories or cross-entity records would violate inspectability and deterministic reasoning. If a document-bearing value arrives in Precept, it must arrive as an already-declared value or stable reference, not as an implicit fetch.

### 4. Pattern-heavy string subtypes as type churn

Email, URL, phone, formatted identifiers, percentages, and similar shapes do not justify new type keywords just because they are common. Once field-level constraints exist, these are better modeled as constrained existing types than as a swarm of narrow built-in types.

### 5. Reopening `money` through the back door

The main survey already settled this: `decimal` amount plus `choice(...)` currency code is the right pattern. Document-oriented follow-ons and duration follow-ons should not be used as excuses to reintroduce a dedicated money type debate.

---

## Criteria for When These Should Become Real Proposals

No follow-on should become a proposal merely because the concept exists in other systems. It should become a proposal only when **all** of the following are true:

1. **Main type-system wave is stable.** The core `choice` / `date` / `decimal` / `integer` corpus must be settled enough that the team is not still changing basic coercion, literal, or tooling rules.
2. **The gap survives constraints.** The missing capability must still be a real semantic hole after applying ordinary constraints, paired fields, and current collection types.
3. **The contract is small.** The type must have a bounded operator surface, a bounded literal story, and a bounded nullability story.
4. **Tooling impact is containable.** Parser, checker, runtime, grammar, language server, MCP, and docs can be updated with one coherent semantic model rather than a growing bundle of exceptions.
5. **It preserves single-entity determinism.** No ambient timezone, repository lookup, host integration, or cross-entity graph traversal is required to understand the value.

### Candidate-specific gates

#### Duration / interval should become a proposal only when:

- authors repeatedly need stored spans that are not well-modeled as `integer` counts plus naming conventions;
- the design can commit to either one narrow elapsed-duration model or an explicit two-family split;
- the proposal can publish a complete arithmetic table up front;
- the feature still reads as a field-level domain value, not as a mini date/time library.

#### Attachment / document-reference should become a proposal only when:

- the proposal can define a **reference-first** value contract, not a binary transport feature;
- the required metadata surface is explicit and flat;
- inspect / fire / update semantics remain declarative and side-effect free;
- the runtime can remain storage-agnostic about where the actual file bytes live.

If those gates are not met, the correct action is to keep the topic in research, not to open a proposal issue prematurely.

---

## PM Readout

The durable conclusion is deliberately narrow:

- **duration / interval values** are the strongest true type follow-on after the main sweep, but only if Precept chooses a sharply bounded semantic family and refuses temporal sprawl;
- **attachment / document-reference values** are the strongest non-scalar residual domain gap, but they fit Precept only as inert, inspectable, reference-and-metadata values — not as inline blobs or hidden integrations;
- almost every other residual pressure should stay out of the type roadmap because it is either constraint territory or an explicit philosophy non-goal.

That keeps Batch 3 in the right posture: evidence-preserving, future-facing, and clearly subordinate to the main type-system corpus rather than competing with it.
