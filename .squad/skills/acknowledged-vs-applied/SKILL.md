# Skill: Acknowledged ≠ Applied — Review Gap Audit

**Category:** Document QA / Proposal Integrity  
**First observed:** 2026-04-07, README restructure proposal review gap pass  
**Author:** J. Peterman

---

## Pattern

A document that summarizes reviewer feedback without applying it to the body is structurally dishonest — it looks resolved but isn't. This creates a gap between the document's claims about itself ("all review items addressed") and its actual content.

**Signature:** A "changes made" summary table, trim log, or revision note lists reviewer items — but searching the body for the original text finds it still present verbatim.

---

## When to Apply This Skill

Use this audit whenever:
- A document has a revision history, trim summary, or "changes per review" section
- A peer review has required changes and the document claims they are addressed
- You are about to use a proposal/spec as the authoritative source for a downstream artifact (README, copy, code)

---

## Audit Method

1. **Read the reviewer feedback** — extract every required change as a specific, searchable claim (exact text, API name, or phrasing to be changed)
2. **Search the proposal body** for the original text — not the summary, the body
3. **Verify the replacement text** is present where the original was
4. **Check the summary** is not the *only* place the fix appears

If a required change appears only in the summary table (as "will be fixed" or "fixed:"), the body is still wrong.

---

## Application

```
For each required change RC-n in reviewer feedback:
  1. Identify the exact original text (quote it from the review)
  2. Search proposal body for that text
  3. If found: apply the fix the reviewer specified
  4. If not found: confirm the fix was already applied correctly
  5. Update the summary to distinguish "applied" from "acknowledged"
```

---

## Key Principle

**The trim summary is orientation, not correction.** A summary that says "maintenance anxiety phrasing replaced" while the body still says "maintenance anxiety" is a false signal. The body is the artifact. The summary describes it. If they disagree, the body wins — and is wrong.

---

## Corollary: Fabricated API Names

API names in specs and proposals must be verified against source before they enter the document. A fictional method name (`RestoreInstance`) in a hero spec will propagate into README copy, developer attempts, and AI context windows — all of which will fail silently. The cost of leaving one fabricated API name unfixed is higher than any other editorial error in a technical proposal.

**Always verify:** method names, class names, parameter signatures, and enum values against the actual implementation before they enter a copywriting spec.
