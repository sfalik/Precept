# Audit of `philosophy-independent-review-2026-07-27.md`

**Status:** Audit — 2026-07-27. Judges the review, not `docs/philosophy.md`.
Measured against `docs/philosophy.md` as it stood at `5e77865a` (104 lines), the state the
review was reading. No file was edited except this one.

---

## 1. Every citation, checked

The review makes about thirty checkable claims. Twenty-five hold. Five do not.

### Quotations that are accurate, at the line claimed

| Review's claim | Verdict |
|---|---|
| `:29` "Every adjacent tool detects: it checks data at a boundary, when called, and reports what it finds." | Exact, at `:29`. |
| `:17` "every possible event, from any position — without executing anything." | Exact, at `:17`. |
| `:13` "Every rule carries a mandatory reason" | Exact. And true of the language: `precept-language-spec.md:0.8` item 2 — "A rule without a `because` is rejected by the parser." |
| `:37` arithmetic overflow among the compile-time impossibilities | Exact. |
| `:41` "no overflow… no rule that any operation can break" and "A definition that compiles without diagnostics therefore has none of these defects" | Exact. |
| `:39` "The proof travels with the definition, and before an engine will govern under it, it verifies that proof for itself" | Exact. |
| `:59–82` the comparator section; `:65` the `CancelledOrder` with `Total > 0` | Both correct. |
| `:82` link resolves to `docs/research/philosophy/…`, which does not exist | Correct. `docs/research/` is absent; `research/philosophy/entity-first-positioning-evidence.md` is present. The fix given (`../research/…`) is right and has since been applied. |
| No banned vocabulary in `philosophy.md` | Correct — grep over the file at `5e77865a` returns nothing for any of the banned terms. |
| `:98–102` the refusal section, and the two quotations from `:100` and `:102` | Correct. |
| `:19` the closed-set paragraph; `:21` the determinism clause; `:15` the closed-operation sentence; `:23` storage and presentation | All correct. |
| `src/Precept/Runtime/Inspection.cs`, `Version.InspectFire` / `InspectUpdate` exist | Correct. |
| Zero hits for `Certificate` in `src/**/*.cs` | Correct — zero files. |
| `Precept.From(Compilation)` checks only `compilation.HasErrors` | Correct (`src/Precept/Runtime/Precept.cs:35–39`). |
| `runtime-api.md:277` — restore does not re-validate | Correct. |
| `precept-language-spec.md:1789` — stateless precepts valid | Correct. |
| `docs/Working/certificate-steps-membership-2026-07-12.md` exists | Correct. |

### Claims that do not hold

**1. `runtime-api.md:254` "describes the same certificate gate as if built." False.**

The section is `### Definition Load and the Certificate Gate`, beginning at `:250`. Six lines
below the sentence the review cites, at `:260`, the same section carries:

> **Not yet built.** No certificate is emitted, no checker exists, and `Precept.From` performs
> no such gate today. This section states the contract the runtime is being built to; the shape
> of the certificate record and the checker's API are not settled here.

The runtime documentation discloses the gap in the same section, in a marked callout. The review
read line 254 and did not read to line 260. This matters because the claim is used to argue that
the certificate passage is a documentation-wide overclaim, and the strongest form of that argument
— "another canonical doc repeats it as fact" — is simply not true.

**2. The `today()` argument under "Determinism is stated at the rank of an incidental."**

The observation that determinism is underweighted is fair, and the owner acted on it. The argument
supporting it is wrong three ways.

- It is grounded on `samples/academic-course-registration.precept:44–46`. The comment is there and
  says what the review says. But `CLAUDE.md` § *The samples are examples, not a specification*
  forbids exactly this: "Absence in the samples is not evidence." A comment in one hand-written
  example is not the reason a construct is absent from the language.
- Canon already answers it, with a locked rejection the review did not look for.
  `temporal-type-system.md:601`, under **Alternatives rejected**: "`today() → date` shorthand —
  redundant (`now().inZone(tz).date` is the right pattern); encourages timezone-implicit date
  construction." The reason is redundancy and timezone hygiene, not determinism.
- The premise is false. `now()` is in the specification's function table at
  `precept-language-spec.md:1605` (`() → instant`). The language already admits a construct that
  reads the environment, so "a proposal to add `today()` would be a determinism violation" is not
  the rule the document would apply. The determinism paragraph the owner has since written says so
  in the open: `now()` is admitted deliberately, as the single environmental read.

**3. The whole of section 2, "Claims vs. reality," and fix items 1 and 5, invert a ruling made
the day before the review.**

`phase-1/the-walk.md:565–582`, item 23, marked **☑ mine** on 2026-07-26, in the owner's words:

> philosophy.md is a statement and guiding principle for where we are going. It's aspirational,
> the code will be made eventually consistent and we need to validate the code against it, not the
> other way around.

and the walk's own closing note on that item:

> `philosophy.md:53`'s unqualified "compile-time impossibilities" is not an overclaim to soften and
> does **not** go on phase 2's correction list. It is the target; the gap is the code's… Phase 2
> must not weaken philosophy.md to match current behaviour.

The phase-2 contradiction register carries this forward and records the two philosophy overflow
sites as deliberately excluded — `P5` and `P6`, both **"NOT A ROW — EXPLICITLY EXCLUDED… Philosophy
states the target. Do not add it."** The register's § 5 header repeats it: "Phase 2 states the gap,
it does not delete the goal."

The review's method statement (its lines 9–14) declares the opposite approach: spot-check the
claims "against the compiler and runtime — via `precept_compile` probes and a grep of `src/` —
rather than trusting the document's own account." Under item 23 the compiler is not the measure of
this document. The consequence is fix item 1, which asks that `:39` be reworded to
"explicit-commitment phrasing" or held until the certificate work lands — a request to weaken a
target statement to match current build state, which is the one thing the ruling forbids.

The internal inconsistency makes the point on its own terms. By the review's standard, `:9`
("the compiler proves, before any entity exists, that no operation can ever violate what you
declared"), `:33`, `:35` and `:37` are equally unbuilt-as-fact. The review singles out `:39` because
`Certificate` is greppable. That is selection by ease of probing, not by what is actually wrong.

**4. Line-number errors.** Four.

- `:71` for the database-constraints entry. That entry is `:68`. `:71` is *Industry data standards
  (FHIR, ACORD, ISO 20022)*. The quoted words are real, three lines up.
- `:94` for "Neither can produce a definition the compiler has not proven." That is `:92`; `:94` is
  the developer-embedding paragraph.
- `:90` for "Determinism, one-file completeness, language server + MCP server." Those live at `:21`,
  `:13` and `:92`. `:90` is the paragraph about what makes the domain-expert author plausible.
- "eleven bullets" in the comparator list. Ten (`:63`–`:72`). Three more sit in the complements
  list, which the sentence is not counting.

None changes a conclusion. All of them cost a reader time, and in a document whose entire value is
that its citations can be checked, four out of roughly twenty is a poor rate.

**5. "Restore has an in-document precedent to stretch." Overstated.**

The review argues a future contributor could reason from restore's trust of persisted data to
trusted host writes, and that the document "never says why restore's trust of persisted data is not
a precedent." Line 17 gives the reason in the same sentence it describes restore: "it returns a
configuration that was already committed." Already committed means already governed. The
rationalization the review constructs is rebutted by the clause it is quoting. The gap is real —
the document does not say the *API* offers nothing else — but it is a smaller gap than the review
claims, and the rebuttal material was already on the page.

---

## 2. Severity

Three misjudged, in both directions.

**Inflated.** Fix item 1 — the certificate paragraph — is ranked first, above a broken link, and
flagged as needing an owner decision. Under item 23 it is not a defect at all. The rank comes from
the review having measured a target document against a half-built compiler.

**Inflated.** "Rhetorical weight is inverted" is a preference about proportion, presented in the
same list and the same voice as the findings that are checkable. There is no defect here: the
comparator section is long because positioning claims each need their own comparator, and the
review itself concedes "the comparators earn their place." Its supporting count is also wrong.

**Understated.** The declaration inventory at `:13` is dismissed as "Minor… fine for a philosophy
doc." See § 3 — the omission next door to it, in the closed-set paragraph, is not minor, and the
review had the right instinct pointed at the wrong sentence.

Correctly rated: the broken link (mechanical, and right), the `:29` / `:68` tension (real, and the
right fix — scope the opener to the code layer), and "from any position" (mild overstatement, right
correction).

---

## 3. The two items reserved for the owner

Both appear in the review. Neither is credited here.

**Overflow, unqualified at `:37` and `:41`.** Handled acceptably in its disposition and badly in its
method. Fix item 5 says the right thing — already open with the owner, nothing new to add. But the
review reached it by reproducing a compiler probe, and then reported the result as a defect of the
document ("currently false in the shipped compiler") in a section titled *Claims vs. reality*. Under
item 23 the correct handling is to state the gap where the build state is tracked and leave the
philosophy's sentence alone. The specification already does this, in its own voice, at
`precept-language-spec.md:266`: "**Overflow is where this does not hold today, and saying so is the
point of this paragraph.**" The review does not cite it, and so misses that the disclosure it is
implicitly asking for has already been written in the document that owns build state.

**The headline guarantee's missing boundary.** Raised twice — as gap 5 and again as the "routing
hole." The review states the specification owns it (`§0.7`) and routes the philosophy side to the
owner, which is correct. What it gets wrong is calling it "the one substantive missing section" and
"everything else in `docs/` assumes the boundary exists." Everything else in `docs/` does not assume
it — `precept-language-spec.md § 0.7` **states** it, under the heading **The boundary of the
guarantee**, and follows it with a paragraph beginning "That makes one assumption, and it is worth
stating as an assumption rather than leaving it inside a proof." Host-injected values that bypass
the engine are named there explicitly. The gap is that `philosophy.md` does not carry the boundary,
not that the project has not written one.

---

## 4. What the review missed

Its coverage is the easy half. It read `philosophy.md` and the four commits that produced it, then
probed the compiler. It did not read the want document, the phase-1 walk, or the phase-2
contradiction register — the three documents that record what this file is measured against and
which of its claims have already been ruled on. Everything in this section follows from that.

**The largest miss: the closed-set paragraph understates construction, and construction is where
the induction starts.**

`philosophy.md:15` reads:

> Only a closed set of operations can ever change a governed entity's data. It is created in its
> initial state with its declared defaults. An event moves it, carrying typed arguments. A field its
> definition declares editable is edited. Every way the data can change is declared in the
> definition, and every value that enters from outside — **an event argument, an edited field** — is
> held to its declared constraints at the moment it enters.

Two problems, both checkable against documents the review did not open.

Creation is described as defaults only. The want document's base case is not defaults only —
`what-i-want-2026-07-16.md:18`: "The default configuration satisfies every rule, and **every initial
event *establishes* every rule, provable from the constraints declared on its args**." Its worked
example turns on this: `event OpenAccount(OpeningBalance as decimal min 0.0, OverdraftLimit as
decimal nonnegative) initial`, and the deletion table's row for removing `min 0.0` reads "Base case
fails: the rule is not established at construction." An entity created with defaults alone would
have no `Balance` to establish the rule about.

The ingress list names two of three points. `want:141`: "Ingress is exactly two points: **event
args** (*including on initial/construction events*) and **editable-field writes**." The
specification splits the same set three ways and names all three, at `precept-language-spec.md:274`:
"Every value entering the entity from outside the definition — **event arguments, construction
inputs, direct field edits** — is held to the constraints declared on the slot it enters." It is
consistent at `:1911`, `:1925` and `:2016`. `philosophy.md:15` names event arguments and edited
fields and stops.

This is the paragraph the review praises twice — as "a conviction stated as a mechanism" and as the
`5e77865a` split that "works." It checked the paragraph's rhetoric and never checked its
enumeration. The correction is a phrase, and it is still unmade in the working tree, where every
other applied finding of the review has already landed.

**Second: the proof boundary stops at one precept, and the document does not say so.**

`want:191–195`: "For now, the promise stops at the edge of a single precept, and cross-entity data
is out of scope… After the MVP, I envision adding the capability for one precept to invoke an event
on another (message passing), and **saga precepts** that govern across multiple precepts. That is
not MVP scope." `philosophy.md` states its guarantee without qualification and never mentions the
edge. The register considered the nearest site and declined it for a reason that does not cover
this: its § 2 excludes `philosophy.md:82` because the saga reference there is "positioning against
MassTransit, not a claim about Precept's proof boundary" — which is exactly the observation that
there is no statement of the proof boundary anywhere in the file. This is a second missing boundary,
distinct from the host-write one the owner is holding, and it is unraised by anyone.

**Third: the uniform guarantee is the want document's answer to a question the philosophy leaves
half-asked.** `want:173`: "the **guarantee is uniform** — a rule holds, period; there is no
author-facing distinction between 'compile-time-checked rule' and 'runtime-governed rule,' no tiers
of trustworthiness to reason about." `philosophy.md` describes both mechanisms — proof at `:33`/`:37`,
ingress constraints at `:15` — and gets close at the end of `:37` ("not a second line of defense
standing behind the proof") without ever telling the author they never have to weigh one against the
other. For a document whose stated audience is the domain expert, that is the sentence the audience
most needs, and it is the review's declared second-pass question — will this document decide a hard
case — applied to the one place it did not look.

**Fourth, and cheapest: the review never checked whether the rework discharged the want document's
own philosophy flag.** `want:215` names one sentence in `philosophy.md` by hand and says it "needs a
deliberate, owner-approved rewording." The register tracks it as row `P1` and records it executed at
`20021deb`, refined by `5e77865a`. Verifying that a rework did the one thing the owner's own
document asked of it is the first check an independent review of that rework should run. It is not
in the review.

---

## 5. Where the review landed

Five of its seven fix items were accepted and are in the working tree: the link, the `:29` scoping,
"from any position", the determinism paragraph, and the host-boundary paragraph in the refusal
section. Two were not: the certificate rewording and the overflow item — the two that ran against
walk item 23. That pattern is the review in miniature. Read cold and judged as prose, it is a good
review, and the four findings that came from reading the document carefully all landed. Everything
that came from measuring the document against the build was either rejected or rested on a claim
that does not survive checking.

One note on the review's own text: its line 10 uses "load-bearing," which `CLAUDE.md` § *How to
write* bans by name, in the same document whose section 4 certifies the absence of that vocabulary
elsewhere. Its sections are also numbered 1, 2, 3, 4, 5, 7, 6.
