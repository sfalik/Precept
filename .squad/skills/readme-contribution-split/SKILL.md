# SKILL: README Contribution Split (Form vs. Content)

**Domain:** Documentation collaboration  
**Author:** Elaine (UX Designer)  
**Applicability:** Any README or docs rewrite with multiple contributors

---

## Pattern

When two or more contributors work on a README (or similar document), split ownership along **form vs. content** lines rather than by section.

- **Content owner** (e.g., Peterman): writes the words — hook copy, feature descriptions, positioning, link text
- **Form owner** (e.g., Elaine): owns the container — layout constraints, heading rules, visual separators, CTA hierarchy, scannability formatting

This separation prevents constant overlap and allows parallel drafting without conflict.

---

## How to Apply

1. **Identify hard constraints that shape what content can say.** These are form decisions, not copy decisions. (Example: the 60-char line constraint in the README hero shapes what DSL code Peterman can write. It's Elaine's constraint, so she must validate before the hero is finalized.)

2. **Make form dependencies explicit and blocking.** If content cannot be finalized without a form decision, surface this as a hard checkpoint — not a review note.

3. **Assign heading audit to the form owner, on the full draft.** Heading hierarchy violations (H2→H4 skips, emoji prefixes, non-descriptive labels) are only reliably caught after the entire document exists. Don't audit section-by-section.

4. **Form owner applies formatting passes after content drafts complete.** Content owner does not make scannability formatting decisions. Form owner does not rewrite copy.

5. **Conflicts escalate to a decision-maker.** When a formatting pass and the intended copy hierarchy conflict, don't negotiate in-document — escalate.

---

## Direct Contribution vs. Review

| Direct Contribution | Review |
|---------------------|--------|
| You author a specific thing that cannot ship without your approval | You read and comment; someone else decides |
| Blocking dependency | Non-blocking |
| Includes: form spec, heading text, CTA structure | Includes: copy voice, word choice (unless it violates a constraint) |

---

## Example Application (Precept README)

- **Peterman owns:** hook copy, differentiation content, learn more links
- **Elaine owns:** hero format template (60-char line spec), CTA numbered structure, section headings (audit pass), `---` placement, badge row layout, above-the-fold test
- **Hard dependency:** Hero code finalization is blocked until Elaine validates line lengths

---

## When the Shape Is Already Fixed

If a restructure proposal documents the section order, heading levels, constraint table, and per-section content guidance at full precision, **skip the skeleton step**. The proposal is the skeleton. Adding a form-owner skeleton pass between the proposal and the draft is redundant overhead — a second structural artifact before any copy exists creates an interpretation surface that wasn't needed.

The form owner's authority is exercised in the **post-draft constraint audit**, not in a pre-draft blank document.

**The test:** What information would the skeleton add that the proposal doesn't already contain? If the answer is "nothing new," skip it.

## When This Skill Applies

- Any README rewrite with a brand/copy author and a UX/design author
- Any docs page where scannability, hierarchy, and CTA clarity are quality requirements
- Any situation where "review" has been assigned but "contribution" is what's actually needed
- Any situation where the shape question is genuinely open (no detailed proposal exists yet)
