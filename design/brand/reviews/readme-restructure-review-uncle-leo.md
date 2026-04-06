# README Restructure Proposal — Editorial Review

**Reviewer:** Uncle Leo (Copy/Editorial)  
**Date:** 2026-04-06  
**Input:** `brand/references/readme-restructure-proposal.md` (J. Peterman, 2026-04-05)  
**Scope:** Clarity, readability, redundancy, confusing/overblown phrasing, narrative smoothness, section sequencing. Technical accuracy is George's lane; architecture is Frank's. This review does not touch those.

---

## Verdict

**Approve with required changes.**

The proposal is structurally sound and well-argued. The writing is clear and direct in most places. However, five editorial issues need resolution before the rewrite begins: two are required fixes (they will cause confusion or contradict the proposal's own logic), and three are wording concerns that weaken what is otherwise a confident, authoritative document.

---

## What Works

**1. The progressive disclosure narrative is coherent and well-sequenced.**  
The proposal follows its own principle: it defines the problem (current README teaches before it proves), states the fix (prove before teaching), and walks every section in that order. The argument doesn't loop back on itself.

**2. The constraints table (Section: Hard Constraints) is the right instrument.**  
Surfacing Elaine's 16 constraints as a numbered reference table at the top — not buried in rationale paragraphs — means any reader can check compliance while reviewing each section. This is the correct editorial structure for a proposal with hard pre-conditions.

**3. The CTA strategy section earns its place.**  
Calling out Primary / Secondary / Tertiary CTAs with explicit location, action, and rationale is exactly the level of specificity a rewriter needs. No ambiguity about what "subordinated" means.

**4. The dual-audience tables (Human / AI) are tight and useful.**  
Two columns, two audiences, each row maps a need to a structural choice. No sentence bloat. This format is reusable as a QA checklist during rewrite.

**5. Voice checks are appropriately brief and grounded.**  
The inline voice checks ("'structurally impossible' is precise," "'replacing three separate libraries' is concrete, not comparative") are helpful without being preachy. They reinforce brand voice at the point of use.

**6. "What This Proposal Does Not Decide" is appropriately scoped.**  
This section prevents scope creep during review. Clear delineation of deferred decisions (hero domain, logo SVG, badge values, docs URLs) means reviewers won't spend time on things that aren't being decided here.

**7. The closing summary is clean.**  
"The current README teaches before it proves. The restructured README proves before it teaches." — one-sentence contrast, says everything. "One file. Every rule. Prove it in 30 seconds." is punchy. Both are strong.

---

## Required Changes

### RC-1: The "Recommended Section Order" block shows the wrong subsection names for Section 2

In the Recommended Section Order (the code-fenced section map), Section 2 is labeled:

```
## Quick Example
### The Contract (precept file name)
### The Execution (C#)
```

But throughout the Section-by-Section Rationale (Section 2: Quick Example, Hero Treatment), the labels are rendered as **bold inline labels**, not H3 headings:

> `**The Contract** (\`filename.precept\`):`  
> `**The Execution** (C#):`

These are not subheadings — they are bold lead-ins preceding code fences. If implemented as H3 headings, they violate constraint #10's progressive disclosure sequence and add two more heading levels under a section that has no other H3 content. More practically: the heading hierarchy in the section map implies H3 headings, but the Hero Treatment section specifies bold inline labels. The rewriter will get conflicting signals.

**Required fix:** Update the Recommended Section Order block to remove the H3 markers under `## Quick Example` and replace them with a note like `[bold lead-in: The Contract / The Execution — see Hero Treatment section]`. Alternatively, explicitly state in one place whether these are H3 headings or bold inline labels, and make the section map consistent with that decision.

---

### RC-2: Section 3 (Getting Started) contains a context reminder Elaine required — but it is described as "implicit," which it isn't

The proposal states:

> "Section-level context reminder (Elaine): Each section after the hero includes a one-sentence context reminder... Here it's implicit in step 1's description ('You'll see syntax highlighting and live diagnostics as you type')"

That sentence ("You'll see syntax highlighting and live diagnostics as you type") is not a context reminder — it is a benefit statement for the extension. A context reminder, per Elaine's requirement, re-anchors the reader to what Precept is, so that a developer who jumped directly to Getting Started without reading the hook isn't disoriented.

Elaine's own example (from her research):

> "Precept is a domain integrity engine for .NET. Install the VS Code extension for authoring support:"

The proposed copy doesn't include this. Calling the benefit statement "implicit" context reminding is inaccurate and will result in the rewriter omitting the reminder entirely.

**Required fix:** Either (a) add an explicit one-sentence context reminder before step 1 in the Getting Started template copy, or (b) remove the claim that the context reminder is present and add a note that this is still an open requirement for the rewriter to satisfy.

---

## Wording Concerns

### WC-1: "Badge walls signal maintenance anxiety, not quality" — tone is off

Section 0 rationale contains:

> "Keep badge count to three — badge walls signal maintenance anxiety, not quality."

The three-badge recommendation is correct. But "badge walls signal maintenance anxiety" is editorializing, not rationale. It's slightly condescending in a proposal document and distracts from the clean authoritative voice the rest of the doc maintains.

**Suggested replacement:** "Keep badge count to three — additional badges add visual noise without adding signal at the awareness stage."

---

### WC-2: "Precept should *lead* with the AI tooling story in its differentiation section, not bury it" — contradicts the section's own framing

Section 4a (AI-Native Tooling) rationale says:

> "Precept should *lead* with the AI tooling story in its differentiation section, not bury it."

This is fine. But the sentence immediately before it says:

> "Of the 13 projects in Peterman's study, only NRules mentioned AI tooling — and it was a single link to a custom GPT in the Getting Started section."

The implied conclusion is that NRules buried theirs in Getting Started, so Precept should lead with it in differentiation instead. But "lead with it in differentiation" and "don't bury it" are doing double duty here — the first sentence establishes that AI tooling leads within the differentiation section, and the second sentence warns against burying it earlier. These two ideas are different and should be stated separately to avoid the reader thinking the argument is circular.

**Suggested rewrite:** Split into two sentences. First: "Precept is the only tool in the category that ships a dedicated MCP server, Copilot plugin, and full LSP as a unified AI tooling story — AI-Native Tooling leads within this section for that reason." Second: "Placing it in Getting Started (where NRules put theirs) would front-load a power-user feature before the developer has committed to Precept."

---

### WC-3: The closing tagline "One file. Every rule. Prove it in 30 seconds." appears in the proposal body

The closing summary ends with:

> "One file. Every rule. Prove it in 30 seconds."

This is good brand copy — but it appears here without a label, so it reads ambiguously as either (a) proposed marketing copy for the README itself, or (b) a summary flourish in the proposal document. If it's proposed README copy, it needs to be labeled as such (and checked against the two-sentence max for prose sections, constraint #6). If it's just a rhetorical close to the proposal, it should stay but shouldn't be mistaken for a copy recommendation.

**Suggested fix:** Add a parenthetical: "*(proposed tagline for README hook — confirm or substitute during rewrite)*" — or move it to a clearly labeled "Proposed hero tagline" callout and remove it from the prose summary.

---

## Section Order Observation (Non-Blocking)

The proposal's own section order in the document puts "Hero Treatment (Detailed)" and "CTA Strategy" after the full Section-by-Section Rationale, followed by "Serving Human and AI Readers," then "Explicit Constraints." This ordering makes sense for a first read but is slightly awkward for reference use: a rewriter returning to the proposal to check the hero specification has to scroll past four pages of rationale to find it.

This is not a required change — the proposal is not a technical spec, and a reader working from it will read it end-to-end at least once. But if a second revision is made, consider moving "Hero Treatment (Detailed)" to immediately follow Section 2's rationale, and "CTA Strategy" to immediately follow Section 3's rationale. Alternatively, a table of contents at the top would resolve this with no restructuring.

---

## Summary

| # | Type | Issue | Status |
|---|------|-------|--------|
| RC-1 | Required | Section map shows H3 headings; Hero Treatment specifies bold inline labels — conflict | Fix before rewrite |
| RC-2 | Required | Getting Started "context reminder" is labeled implicit but is actually absent — context reminder must be added | Fix before rewrite |
| WC-1 | Wording | "Badge walls signal maintenance anxiety" — tone inconsistent with the rest of the document | Suggested replacement provided |
| WC-2 | Wording | "Lead with AI tooling / don't bury it" — two arguments conflated in one sentence | Suggested rewrite provided |
| WC-3 | Wording | Closing tagline ambiguous — proposal copy or README copy? | Add label or move to callout |
| SO-1 | Observation | Hero Treatment and CTA Strategy are separated from their sections by 4+ pages | Non-blocking; addressable in revision |

The proposal is ready to proceed to rewrite after RC-1 and RC-2 are resolved. WC items can be addressed during the rewrite itself if the author prefers to move quickly.

---

*Review complete. No edits made to README.md or to the proposal file.*
