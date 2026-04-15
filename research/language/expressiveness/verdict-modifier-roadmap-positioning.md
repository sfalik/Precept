# Verdict Modifiers — Roadmap Positioning & Value Proposition

**Date:** 2026-04-11
**Author:** Steinbrenner (PM)
**Basis:** Frank's externally grounded research (`verdict-modifiers.md` v2 rewrite), current philosophy, M1/M2/M3 roadmap alignment
**Status:** Roadmap positioning — pending owner verdict on milestone slot and scope decisions

---

## Executive Summary

Verdict modifiers — declarations of authored intent about whether a state, event, or rule should be treated as success, warning, or error — represent a significant competitive opportunity, particularly state verdicts, which have **zero precedent in comparable workflow systems**. However, they introduce philosophy tension (intent declaration vs. prevention-first model) and scope complexity (three tiers). This document recommends a phased approach: **event verdicts in M2 (strong precedent, high debugging value)** and **rule/state verdicts in M3 (deeper philosophy work, semantic annotation tier)**. This splits the implementation risk, ships debugging value sooner, and defers novel philosophical territory to a dedicated milestone.

---

## Part 1: Value Proposition per Tier

### Event Verdicts — "Success Event, Error Event"

**What it declares:**
```precept
on Approve success
on Deny error
on Escalate warning
```

**What it means to developers authoring a precept:**
- "When this event fires, I expect it to move the entity forward (success) or explicitly block it (error) or flag a concern (warning)."
- The verdict shapes the event's row outcomes: a `success` event should never produce a Rejected or ConstraintFailure outcome; an `error` event should never produce a bare Transition outcome.

**User-facing value:**
1. **Debugging clarity** — "Why didn't Approve succeed? The event is declared success, so if any row produced Rejected/ConstraintFailure, I know something's wrong." The verdict becomes a schema for event behavior, helping authors reason about guard logic.
2. **Visual path routing** — In the preview diagram, success-event edges are colored emerald, error-event edges are rose, warning-event edges are amber. Developers scanning the diagram immediately see the happy path, error path, and concern path.
3. **Diagnostic upgrade** — When an event outcome doesn't match its declared verdict (a success event produces a rejection), emit C-level diagnostic: "Event `Approve` declared `success` but produced `Rejected` — outcome shape mismatch."
4. **AI reasoning** — MCP fire and inspect can flag verdict mismatches, helping AI agents understand behavioral contracts and detect unintended outcomes.

**Precedent:** FluentValidation (.WithSeverity), BPMN (error vs escalation events), Roslyn (diagnostic severity). **Very strong precedent.**

---

### Rule Verdicts — "Warning-Level Constraint"

**What it declares:**
```precept
invariant MonthlyPrice >= 0 because "Cannot be negative" error
invariant NotesLength < 500 because "Long notes flag review" warning
to Submitted assert Items.count > 0 because "At least one item required" error
```

**What it means to developers authoring a precept:**
- "This constraint violation is serious (error) — block the operation. OR this is advisory (warning) — flag it but let it through."
- Parallel to FluentValidation's severity model: the validation still fails, but the severity determines how the application handles it.

**User-facing value:**
1. **Progressive validation** — Warnings don't block operations; errors do. Enables workflows where "risky but allowed" configurations can proceed with a flag for review.
2. **Governance nuance** — Not every violated constraint is a structural impossibility. Some are "best practice violations" or "policy flags" that need human review. The verdict captures that nuance without inventing a new outcome kind.
3. **Compliance messaging** — Inspectors can filter by verdict to see "hard blockers" vs "soft concerns," clarifying which violations must be resolved before production.
4. **AI prioritization** — Agents can focus on error-level violations first, then triage warnings.

**Precedent:** FluentValidation (.WithSeverity), ESLint (error/warn/off), Kubernetes (Enforce/Audit/Warn actions). **Strong precedent.**

---

### State Verdicts — "Success State, Error State, Concern State" [NOVEL TERRITORY]

**What it declares:**
```precept
state Approved success
state Denied error
state OnHold warning
```

**What it means to developers authoring a precept:**
- "Approved is a success endpoint — reaching it is a positive outcome. Denied is a failure endpoint — the entity ended badly there. OnHold is a concern state — something needs attention."
- Distinct from lifecycle boundaries (`initial`, `terminal`) because these are semantic verdicts, not structural constraints.

**User-facing value:**
1. **Compliance review** — A regulatory auditor reviewing a loan precept can ask: "Show me all paths leading to Approved. Are they achievable? What guards block them?" The verdict declaration makes success endpoints first-class facts in the model, not just inferred from the diagram.
2. **Risk dashboard** — A PM can query the precept: "What percentage of initial→terminal paths end in Denied vs Approved? What's the typical decision time?" Verdict declarations enable tooling to compute these metrics automatically.
3. **Endpoint clarity** — Without state verdicts, a state diagram shows topology but hides *intent*. With verdicts, a developer reading `state Cancelled error` immediately understands this is a failure mode, not just another position.
4. **AI navigation** — When an AI agent is reasoning about workflows, verdict-marked states become semantic landmarks. "Prefer paths ending in success states; flag paths ending in error states; investigate warning states."

**Competitive differentiation — why this matters:**
- **Frank's research finding: NO comparable system puts severity on states.**
- XState has `type: 'final'` (structural) and `meta` (annotation), but no severity semantics.
- BPMN distinguishes Error and Escalation *events*, not states. A terminal state in BPMN is structurally complete but semantically neutral.
- UML has `FinalState` (structural) but no success/error discrimination.
- Kubernetes and ESLint operate on rules/events, not state graphs.
- **This is genuine novel territory.** It raises the strategic bar for justification but also increases the competitive moat.

**Caveat — the philosophy challenge:**
Precept's founding principle is **prevention**: invalid configurations become structurally impossible. State verdicts are **intent declarations**: they don't prevent anything. They are purely tooling and semantic annotation. This shifts the framing from "prevention via declared rules" to "clarity via authored intent about outcome significance." The philosophy doesn't reject this — it just requires a deliberate layer distinction: structural prevention (invariants, guards, state-conditional assertions) sits in Tier 1; semantic annotation (state verdicts, rule severity) sits in Tier 2, enabling clearer authoring and better tooling, not stricter enforcement.

---

## Part 2: Target Audience Scenarios

### Scenario 1: Compliance Officer Reviewing Entity Rules

**Actor:** Regulatory auditor for a loan application system
**Question:** "Are success paths (Approved → Funded) reachable? What could block approval?"

**Today:** The auditor opens the precept, sees states and edges, manually traces paths, traces guard conditions. High friction. Easy to miss a guard that blocks the happy path.

**With verdict modifiers:** The auditor asks the tooling: "Show me all paths to Approved (success state) from Initial. Highlight guards. Flag dead ends." The tooling surfaces the policy model directly, making review faster and more confident. State verdicts turn compliance review into a queryable exercise instead of a manual diagram study.

---

### Scenario 2: Developer Debugging Event Behavior

**Actor:** Developer writing transition rows for Approve event
**Question:** "Why does Approve produce a Rejected outcome in some cases? I declared it as success."

**Today:** The developer manually reviews all Approve rows, reads guards, evaluates them against test data, and infers why a rejection happened. Hard to correlate declared intent with actual behavior. Incomplete mental model.

**With event verdicts:** The developer views the MCP inspect output for the Approve event:
```json
{
  "event": "Approve",
  "verdict": "success",
  "outcomes": [
    { "condition": "when ApproversSignedOff.count == Required", "outcome": "Transition", "status": "✓ matches declared success" },
    { "condition": "implicit else", "outcome": "Rejected", "status": "⚠ mismatch: success event produced Rejected" }
  ]
}
```

The verdict becomes a lint rule: "success event produced an outcome that contradicts its declaration." The developer immediately sees the missing row or overly-broad implicit else.

---

### Scenario 3: PM Assessing Risk in a Precept

**Actor:** Product manager reviewing a hiring-pipeline precept
**Question:** "What's the typical flow? How often do candidates get to Offer? What blocks them?"

**Today:** The PM examines the state machine, counts transitions, manually estimates conversion rates. The diagram shows structure but hides outcome distribution.

**With state verdicts:** The PM runs a precept analyzer query on success/error/warning states. The tooling reports:
- 7 paths to Offer (success)
- 3 paths to Rejected (error)
- 2 paths to OnHold (warning)
- Coverage: 12 possible (State, Event) pairs; 8/12 routed

State verdicts let the precept declare "Offer is the win condition," turning a topology diagram into a business-process dashboard.

---

### Scenario 4: AI Agent Reasoning About Workflows (with Copilot Plugin)

**Actor:** AI agent authoring a precept, iterating with a developer

**Scenario:** The agent generates transition rows without explicit intent semantics. The developer says, "I want this to be a success event — only happy paths."

**With event verdicts:** The developer declares `on Approve success` and runs the MCP compile tool. The agent receives:
```json
{
  "violations": [
    {
      "type": "verdict-mismatch",
      "event": "Approve",
      "declaredVerdict": "success",
      "actualOutcomes": ["Transition", "Rejected"],
      "message": "Event Approve declared success but has Rejected outcome. Resolve by: (1) changing verdict to warning, (2) removing Rejected row, or (3) adding guard to prevent Rejected case."
    }
  ]
}
```

The agent uses this feedback to iteratively refine the row structure. Verdict declarations turn intent into tooling feedback, enabling closed-loop AI authoring.

---

## Part 3: Milestone Positioning — Four Options

### Option A: Late M2 — All Three Tiers Together (After Types, Before Expression Power)

**Placement:** After #29 (integer) and #25 (choice) land in M2; before #16 (functions) and expression work in M3

**Rationale:**
- Event verdicts are structurally simple (row outcome validation)
- Rule verdicts are a natural extension to invariants/asserts
- State verdicts complete the "full entity surface" positioning of M2
- Philosophically, M2 is "the complete lifecycle+field model" — verdicts are the semantic capstone

**Pros:**
- Completes the "full entity surface" story before M3 expression work
- Event verdicts ship debuggable support faster
- Aligns with "full entity" branding of M2

**Cons:**
- Scope explosion: M2 already carries types (#25, #29), named rules (#8), conditional invariants (#14), and absorb shorthand (#11)
- State verdicts are novel territory requiring deep philosophy work; M2 is supposed to be execution-focused, not research-heavy
- MCP and grammar sync burden is high when stacked with type-system work

**Risk:** High chance of M2 schedule slip due to philosophy contention around state verdicts

---

### Option B: Early M3 — All Three Tiers Alongside Expression Power

**Placement:** Start of M3, alongside #16 (functions), #9 (ternary), #10 (string.length)

**Rationale:**
- M3 is positioned as "Expression Power" and semantic richness — verdict modifiers fit this framing
- Verdict modifiers are tooling-heavy (MCP, preview rendering, analyzer) — bundles with expression work that also has rich diagnostics
- Defers complex philosophy work until after M2 ships and core types are stable
- Less crowded roadmap for M2 (keeps it focused on types, named rules, conditionals)

**Pros:**
- Phased approach reduces scope pressure on M2
- M3 gets semantically rich features (expressions + verdict annotations)
- State verdicts research can proceed unrushed during M2

**Cons:**
- Delays debugging clarity (event verdicts) further
- Bundles structurally simple event verdicts with complex expression work; mismatched priorities
- Requires deeper philosophy articulation before M3 starts

**Risk:** Moderate — philosophy work during M3 could affect expression work if clarity gaps emerge

---

### Option C: New M4 — Dedicated "Authored Intent" / "Semantic Governance" Milestone

**Placement:** Post-M3, as a distinct milestone focused on intent declaration and semantic annotation

**Name:** M4 "Authored Intent" or "Semantic Governance"

**Contents:** Event, rule, and state verdicts + future annotation modifiers + tooling for intent validation

**Rationale:**
- Verdict modifiers are fundamentally about **authored intent** and semantics, not structural capability
- Bundling with M3 expression work obscures the actual focus: "what does this state mean?" vs. "what can I compute?"
- A dedicated M4 milestone signals a strategic pivot: from "what the entity can do" (M1-M3) to "what the entity means" (M4)
- Buys time for philosophy clarification without impacting M2 or M3

**Pros:**
- Cleanest philosophy story: M1-M3 are capability expansion; M4 is semantic enrichment
- Zero pressure on M2 or M3
- Signals a distinct strategic offering: Precept not only prevents invalid configs but helps you understand what valid configs *mean*
- Strong positioning against competitors who lack semantic annotations

**Cons:**
- Furthest timeline for shipping any verdict modifier
- Requires articulating a new milestone to stakeholders (brand/DevRel effort)
- May feel like a "nice to have" rather than "must have" vs. core M3 expression features

**Risk:** Low technical risk; moderate stakeholder adoption risk (is "semantic governance" compelling enough to justify a milestone?)

---

### Option D: Phased — Event Verdict in M2, Rule+State Verdict in M3 [RECOMMENDED]

**Placement:**
1. **Event verdicts:** Late M2 (after types, as dedicated slice)
2. **Rule verdicts:** Early M3 (with expression work)
3. **State verdicts:** Mid-M3 (after rule verdicts land and philosophy is solid)

**Rationale:**
- Respects structural provability tiers: event verdicts are the most provable (row outcome validation) and have the strongest precedent
- Event verdicts are high-ROI debugging features — ship sooner for developer benefit
- Rule verdicts are natural extension of invariant/assert foundation — pairs naturally with M3 expression enhancements
- State verdicts (novel territory) ship last, after philosophy work is embedded in rule verdicts
- Splits the MCP/grammar/analyzer sync burden across two releases

**Milestone sketch:**
- **M2 Slice 3 (Event Verdicts):** Parser keyword, 2-outcome validation per event, grammar, MCP snapshot, preview highlight, C-level diagnostic
- **M3 Slice 2a (Rule Verdicts):** Invariant/assert syntax, outcome filtering, MCP DTO, diagnostics
- **M3 Slice 2b (State Verdicts):** State declaration syntax, reachability analysis, compliance query tooling, AI agent support

**Pros:**
- Mitigates philosophy risk by phasing the novel work (states) last
- Event verdicts ship in M2 for developer debugging wins
- Rule verdicts ship in M3 aligned with expression work (both add semantic richness)
- State verdicts ship once philosophy is proven on events and rules
- No new milestone required; fits cleanly into existing three-milestone roadmap
- Spreads MCP and grammar sync across two releases

**Cons:**
- Three separate implementation efforts instead of one coordinated batch
- Risk of philosophy inconsistency if events and rules ship with different intent-model semantics

---

## Part 4: Proposal Readiness Assessment

### What's Still Open (Research Gaps)

1. **Event verdict syntax specificity:**
   - Should `on Approve success` attach to all rows, or can individual rows override?
   - Proposed: verdict attaches to the event; individual rows cannot override (simpler, first pass)
   - Need: concrete syntax spec with scope semantics

2. **Rule verdict syntax scope:**
   - Should `invariant MonthlyPrice >= 0 because "..." error` apply to all conditions, or only specific states?
   - Proposed: applies globally unless guarded; `invariant when Draft MonthlyPrice >= 0 because "..." error` for state-specific verdicts
   - Need: conditional verdict syntax and compile-time interaction with state guards

3. **State verdict philosophy lock:**
   - Should state verdicts participate in structural checks (e.g., error C53 if unreachable error state exists), or are they pure annotation?
   - Proposed: pure annotation; no structural errors, only diagnostics and tooling directives
   - Need: explicit philosophy decision from owner confirming intent-declaration vs. prevention split

4. **MCP DTO contract for verdicts:**
   - Should Fire output include outcome-verdict mismatch details inline, or require a separate query?
   - Proposed: inline (verdict + actualOutcome + match status) so AI agents see mismatches immediately
   - Need: DTO spec signed off by MCP tool author

5. **Designer intent vs. runtime enforcement:**
   - When an event produces an outcome that contradicts its verdict, is this:
     - (A) A hard compile-time error (prevention-first: invalid precept)?
     - (B) A C-level diagnostic (warning: likely unintended)
     - (C) Just informational metadata (intent-only: no enforcement)?
   - Proposed: (B) — diagnostic only, preserving philosophy that verdicts are intent, not structural constraints
   - Need: owner call on whether this is acceptable philosophy risk

---

### Design Decisions Required Before Proposal

1. **Verdict-outcome mismatch handling** — (A), (B), or (C) above
2. **Rule verdict scope interaction** — global, state-scoped, or both?
3. **State verdict structural participation** — annotation-only or structural constraints enabled?
4. **MCP/tooling feedback contract** — inline verdicts in Fire output or separate query?
5. **Philosophy lock: intent-declaration tier** — confirm Precept's semantic annotation tier is compatible with prevention-first positioning

---

### Minimum Viable Scope for First Proposal

**Phase 0 (MVP):** Event verdicts only, one proposal, focuses on debugging value

```precept
on EventName success | error | warning
```

**Phase 1 (after event verdicts ship):** Rule verdicts, separate proposal

Phase 2 (research + philosophy): State verdicts, separate proposal

**Why MVP first?**
- Event verdicts are highest-confidence (strongest precedent, most provable)
- Simplest scope: validate row outcomes match declared verdict
- Delivers debugging value sooner
- Philosophy load is lower: "event verdicts declare outcome intent" is easier to defend than "state verdicts declare endpoint meaning"
- De-risks state verdicts by proving the intent-declaration tier works with events first

---

## Part 5: Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| **Scope creep: 3 tiers merge into one massive feature** | Medium | High | Lock MVP to event verdicts. Separate proposals for rules and states. Milestone sequencing creates natural batching points. |
| **Philosophy tension: intent-declaration layer conflicts with prevention-first ethos** | Medium | High | Owner decision on philosophy lock required before M2 late slice. Explicitly document that verdicts are semantic annotation, not structural constraints. Align with industry precedent (FluentValidation, BPMN). |
| **Adoption friction: users don't understand verdict modifiers or see no value** | Medium | Medium | Target compliance/governance use case strongly in launch messaging. Show concrete compliance-review and risk-dashboard tooling based on verdicts. AI agent integration reduces learning curve. |
| **Implementation complexity: MCP, grammar, analyzer, language server all need updates simultaneously** | Low | High | Phased approach (Option D) spreads work across M2 and M3. Event verdicts only touch outcome validation logic. Rule verdicts can follow existing invariant paths. State verdicts can build on rule work. |
| **Diagnostic wording overload: "verdict" + "outcome" + "verdict-mismatch" becomes terminology soup** | Low | Medium | Consistent terminology in docs/completions/MCP: "declared verdict" vs. "actual outcome"; "verdict mismatch" diagnostic code. User guide clarifying the three-tier semantics. |
| **State verdicts backslash: research shows they violate prevention-first principle** | Low | Medium | Bounded by decision guard in Part 4 (philosophy lock). If rejected, verdicts stay at events+rules tier; state semantic marking defers to future modifiers research. |
| **Competitive window closes: another tool ships verdict modifiers first** | Low | Medium | Event verdicts in M2 get first-mover advantage on debugging; state verdicts (novelty) remain differentiated regardless of timing. |

---

## Recommendation

**Milestone:** Option D (phased — event verdicts in M2 late slice, rule+state in M3)

**Sequence:**
1. Event Verdicts (M2, after types) — high ROI, strong precedent, shipping by Q2
2. Rule Verdicts (M3, early) — aligned with expression enrichment, medium precedent
3. State Verdicts (M3, mid) — novel territory, ships after philosophy is hardened on events+rules

**Gate:** Owner decision on philosophy lock (Is intent-declaration tier acceptable? Are verdicts annotation-only or do they trigger enforcement?)

**First Proposal:** Event verdicts, scoped narrowly to debugging and outcome validation.

**Competitive story:** "Precept is the only state machine governance engine that lets you declare success and failure endpoints as authored intent, built into the precept definition. No other system combines verdict modifiers for states, events, and rules in one unified semantics."

---
