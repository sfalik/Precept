# Skill: Proposal Gate Analysis — Minimum Sign-Off Before Execution

**Category:** Project Management / Decision Architecture
**First observed:** 2026-05-01, README ship plan (Steinbrenner)
**Author:** Steinbrenner

---

## Pattern

After a proposal clears a multi-reviewer required-changes cycle, the PM's job is to identify the *minimum* set of decisions the principal (Shane) must make before execution begins — and explicitly separate those from decisions that can be made in-flight by the executor.

Conflating all open questions as "Shane items" creates unnecessary delays and back-and-forth. Conflating in-flight decisions as "already decided" creates churn when the executor hits a real fork.

---

## The Three Categories

After all required changes are applied (use `acknowledged-vs-applied` skill to verify):

### 1. Gate-Before-Start Decisions (principal required)
- Structural choices that cascade: changing them mid-execution undoes completed work
- Items where the principal has unique authority (brand, positioning, hero domain)
- Items with high churn risk if not locked (hero domain is the canonical example)

### 2. In-Flight Decisions (executor resolves)
- Copywriting judgment calls flagged as advisory or wording concerns in reviews
- Technical precision details where a domain expert (not the principal) is the right decision-maker
- Format and style calls within already-locked constraints

### 3. Out of Scope — First Pass
- Features/content that depend on unfinished work (Elaine's palette pass, unimplemented DSL features)
- Content categories the proposal explicitly deferred to docs or external links
- Aspirational additions that aren't needed for the artifact to ship

---

## Application

```
1. Read all reviewer required changes and verify they are applied (acknowledged-vs-applied skill)
2. Identify remaining open items (advisory notes, wording concerns, deferred items)
3. For each open item:
   - Does changing it mid-execution undo work? → Gate-Before-Start
   - Does it require unique principal authority? → Gate-Before-Start
   - Is it a judgment call within locked constraints? → In-Flight
   - Does it depend on unfinished work or is explicitly deferred? → Out of Scope
4. Present Gate-Before-Start items as a numbered list with: decision, options, and "default if skipped"
5. Explicitly list Out of Scope items to prevent scope creep during execution
```

---

## Key Principle

**The executor does not need permission for in-flight decisions.** Asking for it creates unnecessary synchronization points. The PM's job is to route the decision correctly, not to escalate everything.

**The principal does not need to see out-of-scope items.** Presenting them suggests they're under consideration. If they're not, don't show them.

---

## Corollary: Hero Domain Is Always a Gate

In any artifact that contains a hero example (README, landing page, talk demo), the domain selection is always a Gate-Before-Start item. Domain selection cascades into: which DSL constructs appear, what `because` messages say, what the C# variable names are, and what the overall narrative arc conveys. Changing it after the draft is done is a full rewrite, not an edit.

Lock the hero domain before drafting begins. Always.
