# Audit of the uncommitted edits to `docs/philosophy.md`

**Status:** Opinion — 2026-07-27. Audits the working-tree edits to `docs/philosophy.md`
against `docs/working-reset/phase-2/philosophy-independent-review-2026-07-27.md`, the
canonical documents, and `docs/Working/what-i-want-2026-07-16.md`. Baseline for comparison is
`5e77865a`. No canonical document was edited. Nothing committed.

Evidence used: `docs/language/precept-language-spec.md` (§0.1, §0.7, §3A),
`docs/language/temporal-type-system.md`, `docs/runtime/runtime-api.md`,
`docs/compiler/soundness-and-coverage.md`, the want document, and
`docs/working-reset/phase-1/decision-register-FINDINGS.md`. No catalog, no sample, no
compiler run.

---

## 1. Edits against review findings

Six changes were made. Five map onto a numbered fix in the review; one is a new preamble that
answers a class of finding rather than a specific one.

| Review fix | Edit | Verdict |
|---|---|---|
| 1 — certificate paragraph describes an unbuilt mechanism as fact | Not edited. The new preamble answers it generically | Partly addressed — see §2.1 |
| 2 — broken research link | `research/…` → `../research/…` | Correct. Target exists at `research/philosophy/entity-first-positioning-evidence.md` |
| 3 — "Every adjacent tool detects" contradicts the DB-constraints entry | Scoped to "at the code layer", plus a parenthetical conceding DB constraints prevent | Correct — see §2.4 |
| 4 — "from any position" overstates the inspection API | → "from any version of the entity you hold, live or restored" | Correct — see §2.5 |
| 5 — overflow and the headline boundary | Not addressed | Reserved for the owner. But see §3.1 — one edit now cuts across the boundary question |
| 6 — determinism needs its own paragraph saying what it forbids | New standalone determinism paragraph; inspectability and approximation split into their own paragraph | Substantially better than the fix as written — see §2.2 |
| 7 — trust model at the embedding boundary | New paragraph in "What Precept refuses to do" | Right idea, one sentence overreaches — see §3.1 |

**Findings with no corresponding edit:**

- **Fix 1** (certificate). The paragraph at "The runtime does not take the compiler's word for
  any of this" is untouched. Note that by the evidence rules that apply here, this paragraph
  agrees with canon: `docs/runtime/runtime-api.md:254` states the certificate gate, and
  spec `§0.7` and `:1953` both rest on it. What canon *also* carries, and philosophy did not,
  is `runtime-api.md:260`: "**Not yet built.** No certificate is emitted, no checker exists,
  and `Precept.From` performs no such gate today." The preamble now supplies the missing
  half globally. That is a legitimate answer, with a cost — §2.1.
- **Fix 5** (overflow, headline boundary). Untouched, correctly, as owner-reserved. The
  preamble's framing is consistent with how the spec handles the same gap (§3.4).
- **Review §5 minor** — the declaration inventory still omits computed fields, guards, and
  actions. Not addressed; the review itself rated it optional.
- **Review §7 "underselling"** — the mandatory `because` is still stated once and never
  claimed as distinctive. Not addressed.
- **Review §7 "rhetorical weight is inverted"** — partly addressed as a side effect. The
  commitments side gained two substantial paragraphs (determinism, host contract) without the
  comparator section growing, so the ratio the review complained about improved without the
  comparators being cut.

**Edits with no finding behind them:** none, except the preamble, which is a reasonable
response to fix 1 and to the review's closing point that "the first time a reader catches the
philosophy asserting something false, every other assertion weakens."

---

## 2. Correctness of each edit

### 2.1 The preamble

> *This document states the guarantee Precept ships with — the commitments the project is
> built to keep. It is not a status report on the current build: where the compiler or runtime
> has not yet caught up to a commitment here, the commitment stands and the gap is tracked as
> work in progress in the compiler and runtime docs.*

Every factual claim in it checks out. The compiler and runtime docs do track the two gaps a
reader would trip over: `runtime-api.md:260` for the certificate, and
`docs/compiler/soundness-and-coverage.md:226` for overflow ("in-scope, not-yet-implemented —
the arithmetic-overflow (representable-range) lane is GATE-O, post-MVP"). The framing also
matches the spec's own, at `§0.7`: "The paragraph above states the target the language is
being built toward and that target is not being weakened to match."

It is nonetheless the one thing in this document that steps outside the present indicative,
and it does so for the whole document at once. See §3.4.

### 2.2 The determinism paragraph

Checked clause by clause against `docs/language/temporal-type-system.md`:

- "`now()` is the sole construct that reads the environment" — `temporal-type-system.md:562`:
  "`now()` is the sole way to construct an `instant` from the runtime environment."
- "an author who writes it into a rule is declaring in the open that the rule's correctness
  depends on when it is evaluated" — `:568`, almost verbatim: "the author is explicitly
  claiming 'this rule's correctness depends on when it is evaluated.'"
- "within a single operation the clock is one consistent value — same inputs, same outputs" —
  `:568`: "Precept guarantees internal consistency (same inputs, same outputs within a single
  fire) but cannot freeze the clock."

**This edit is better grounded than the review finding that prompted it.** The review asked
the paragraph to forbid ambient time and to explain why `today()` is absent, asserting that
"a future proposal to add `today()` to the language would be a determinism violation." Canon
says otherwise on both counts: ambient time is admitted through `now()`, and `today()` was
rejected at `temporal-type-system.md:601` as "redundant (`now().inZone(tz).date` is the right
pattern); encourages timezone-implicit date construction" — a timezone-explicitness rejection,
not a determinism one. The edit declined the review's premise and wrote what canon actually
holds. That is the right call, and it is worth recording that the reviewer was wrong here.

Two things to put to the owner rather than treat as defects (§3.2, §3.3).

### 2.3 The host-contract paragraph

The core of it is right and well argued, and the restore-is-not-precedent move is exactly what
fix 7 asked for: "Trusting data on the way back from storage extends the guarantee; trusting
new values on the way in would end it." That reasoning is consistent with spec `§0.7`, which
trusts restored state because "the prior committed state satisfied the rules in effect when it
was written."

The final sentence does not hold. See §3.1.

### 2.4 "Every adjacent tool at the code layer detects"

Correct, and the parenthetical's "with the costs described below" is accurate — the
DB-constraints comparator does list them (no lifecycle model, no inspectability,
database-specific, untestable in CI without a live database, invisible to application
tooling). "Precept prevents where the business logic runs" is consistent with the same
comparator's closing sentence, "Precept provides the same structural enforcement at the code
layer."

One stylistic cost: a parenthetical concession now sits between the section's opening claim
and its answering claim, in a paragraph built as a punch. The same content placed after
"Precept prevents where the business logic runs" would lose nothing and keep the opening
intact. Minor, not a defect.

### 2.5 "from any version of the entity you hold, live or restored"

Correct against `docs/runtime/runtime-api.md`. Inspection is `Version.InspectFire` /
`Version.InspectUpdate` (`:320–326`), and `Version` is the entity snapshot (`:62`), which you
obtain from `Create`, `Restore`, or a prior operation. "Live or restored" is precise.

Slightly incomplete rather than wrong: `Precept.InspectCreate` (`:186`, `:219`) previews
construction before any version exists, so the pre-entity preview is now outside the wording.
Not worth a further edit.

---

## 3. What got worse

### 3.1 The host paragraph's last sentence asserts something canon denies

> A host that needs to change a governed entity's data uses the operations the definition
> declares, or the change does not happen.

Spec `§0.7`, "The boundary of the guarantee", says the opposite in two places:

> Host-injected values that bypass the engine are likewise outside the envelope.

> A host that writes a value into an entity by some route other than the contract has stepped
> outside the guarantee, and nothing inside the engine will notice.

The change *does* happen; the engine does not notice. Everything before that sentence in the
new paragraph is about the engine's public API, and is true of it — there is no privileged
write path in the API. The last sentence silently widens the claim from "the API offers no
back door" to "no other route exists", which is the one thing the spec spends a paragraph
saying is false.

It also contradicts this document, three paragraphs into "What Precept is": "Where the entity
is stored and how it is presented remain the application's business." An application that owns
storage can write to storage. Both sentences cannot be read literally at once.

This is the edit I judge wrong. It is also the one that touches the owner-reserved boundary
question: fix 7 asked for a paragraph stating where the guarantee stops, and the paragraph
delivered instead states that it does not stop. Before these edits the boundary was *absent*
from philosophy; it is now *contradicted* by it. That is a step back on the reserved item, and
the fix is small — end the sentence at the API ("the engine's public API offers no other
route"), and let the spec's boundary paragraph be summarized rather than overridden.

### 3.2 The determinism paragraph settles a question the owner left open

The want document, at its "Open questions" section, reserves this explicitly:

> **Time-referencing rules.** A rule like `ExpiryDate > today` can become false with no event
> in flight … Do rules that reference the current moment exist at all, or does time belong
> only in guards (evaluated at event time, like any other premise)? Deliberately left open.

`docs/working-reset/phase-1/decision-register-FINDINGS.md:91` records the same reservation.
The new philosophy paragraph writes `now()`-in-a-rule into the governing document as settled
and honest practice. It is consistent with `temporal-type-system.md`, which shows
`rule now() - IncidentAt <= '72 hours'`, so the edit is not inventing anything — but
philosophy is the document the others are measured against, and hardening an owner-open
question there is a decision, not a wording fix. Worth one sentence to the owner before it
commits.

### 3.3 One clause binds a post-MVP roadmap item

"no lookup against another system, **another entity**, or the host's state in the middle of a
rule." The want document plans "message passing" and "saga precepts" after the MVP. The
qualifier "in the middle of a rule" probably keeps message passing clear — invoking an event
is not a rule-time read — but a saga precept governing across precepts would sit awkwardly
against a permanent determinism commitment written this way. Flagging, not objecting.

### 3.4 A hedge reintroduced, document-wide

The preamble is factually clean (§2.1) and answers a real problem. Its cost is that it is the
only softener in a document that had nine rounds of them removed, and it is scoped to
everything. Every present-indicative claim in the body — including "Invalid configurations are
not caught and rejected — they are structurally impossible" — is now readable as a statement
of intent.

Note how the canonical docs solve the same problem: `runtime-api.md:260` puts "Not yet built"
directly under the certificate section, and spec `§0.7` puts the overflow exemption directly
under the fault-prevention paragraph. Both are pinpoint notes at the claim they qualify, and
neither weakens anything else in its document. A document-level disclaimer buys the same
honesty at a much higher price.

This is a judgement call, not an error, and it may be exactly what the owner wants given the
commit message quoted in the review. If it stays, the narrower alternative is worth
considering: keep the first sentence ("This document states the guarantee Precept ships
with"), which is a statement of what kind of document this is and costs nothing, and move the
"has not yet caught up" concession to the certificate paragraph, where it belongs and where
canon already puts it.

### 3.5 Not worse, checked and clear

- No banned vocabulary in any added text.
- The claim strength of the determinism paragraph is *higher* than what it replaced — "same
  definition, same data, same outcome, every time, anywhere", plus a list of what it forbids,
  where the old text had half a clause. It also says "That is not a performance property — it
  is what makes the contract checkable, and it forbids things", which is the structure the
  review asked for.
- Splitting inspectability and approximation into their own paragraph did not weaken either;
  the text is unchanged.
- Overflow is untouched and the preamble's framing of it matches spec `§0.7`. Not made worse.

One residual mismatch, minor: the inspection claim was scoped in one place ("from any version
of the entity you hold") but the parallel claim two paragraphs later still reads "at any point
you can preview every possible operation", unscoped. It is not an overstatement in the way the
first one was, so no edit is needed, but the two now differ in precision.

---

## 4. Is the document better than at `5e77865a`?

**Yes** — with one sentence that should come out before it commits.

Four of the five mechanical or near-mechanical fixes are correct: the link, the code-layer
scoping, the inspection wording, and the paragraph split. The determinism paragraph is a real
gain and is the best-grounded edit in the set — it takes the document from stating determinism
as an incidental to stating what determinism forbids, and it does so against canon rather than
against the review's mistaken account of canon. The host-contract paragraph closes the
rationalization route the review found, and its restore-is-not-precedent argument is sound.

Against that: the last sentence of the host paragraph asserts something spec `§0.7` explicitly
denies and this document's own storage sentence contradicts, and it does so on the one topic
reserved for the owner. And the preamble buys honesty with a document-wide hedge where canon
uses pinpoint ones.

The first of those is a defect and should be corrected. The second is a call for the owner. On
balance the document is more accurate, more forceful where it matters most, and no longer
carries a broken link or a claim its own comparator section refutes.
