# Skill: PR Implementation Plan

**Confidence:** high
**Domain:** pull request quality, implementation tracking

## What this skill covers

How to write a PR body for a Precept implementation PR that is useful throughout the PR's lifetime — not just at creation. Covers structure, checklist granularity, design decision capture, and checkbox maintenance.

## Relationship to CONTRIBUTING.md

This skill operationalizes §3 (Implementation) of `CONTRIBUTING.md`. The key rules from CONTRIBUTING.md that this skill enforces:

- The PR body checklist is **ephemeral** — it is discarded after merge. It does not need to outlive the PR.
- Checkboxes are updated **as work progresses**, not at the end. After each slice or logical group, update the PR body.
- **Full design rationale (alternatives, precedent, tradeoff analysis) does NOT go in the PR body** — that belongs in the issue (during design) and in `research/` (at merge time). The PR body's `## Why` section is concise reviewer context for the shipped change, not a duplicate of the proposal's full rationale.
- The **slice order** for cross-cutting changes is defined in CONTRIBUTING.md §3 step 5: Parser + model + diagnostics → Type checker → Runtime → Language server → Grammar → MCP → Tests → Samples → Docs. The checklist component sections below follow this order.

## When to use

- Creating a draft PR for any implementation issue
- Retrofitting an existing PR body that was auto-generated with a sparse checklist
- Reviewing a PR body before marking it ready for review

## Structure

A good PR body has four required sections:

```
## Summary
[2–4 sentences: what changed in reviewer-facing terms. Keep this current if the shipped scope changes.]

## Linked Issue
- Closes #N

## Why
[1–3 bullets or a short paragraph explaining why this PR exists, what user/problem it addresses, and any implementation-specific reviewer context. Do NOT duplicate the issue's full design rationale or alternatives analysis.]

## Implementation Plan
[Sections by component. Each section has granular checkboxes.
 See component sections below.]
```

Optional sections if they add value:

```
## Implementation Notes
[Things discovered DURING implementation that weren't obvious from the issue design.]

## Critical Review Focus
[1–3 items reviewers should explicitly verify.]
```

## Implementation checklist — component sections

Use these standard sections for Precept language/runtime PRs. Include only sections relevant to the PR. Every checkbox should be specific enough that someone can verify it by looking at one file.

### Parser
- [ ] `PreceptToken` enum: new token(s) added with correct `[TokenCategory]` attribute
- [ ] Tokenizer: new keyword(s) recognized
- [ ] Parser: new production rule / combinator added
- [ ] `PreceptModel.cs`: new AST node or record extension

### Type checker
- [ ] New symbols registered in relevant scope(s) (name each scope: guard/invariant/assert)
- [ ] Diagnostic code(s) defined in `DiagnosticCatalog.cs`
- [ ] `// SYNC:CONSTRAINT:Cnn` comment added in type checker source
- [ ] Nullable / narrowing behavior correct (specify the narrowing pattern if applicable)

### Runtime evaluator
- [ ] `EvaluateIdentifier` / dispatch handles new expression form
- [ ] Null / missing-key / wrong-type failure paths return `EvaluationResult.Fail` (not throw)

### Language server
- [ ] `PreceptAnalyzer.cs`: new completions in the correct context(s)
- [ ] Semantic tokens: new token kind picked up via `[TokenCategory]` (no manual handler change needed for standard additions)

### Grammar
- [ ] `precept.tmLanguage.json`: new keyword(s) in correct pattern(s)
- [ ] Pattern ordering verified (specific before general)
- [ ] Both collection and string accessor patterns updated if applicable

### Tests
- [ ] Parser: accepts valid form(s)
- [ ] Type checker: valid form → no diagnostics
- [ ] Type checker: invalid form → correct Cnn diagnostic
- [ ] Nullable path: without guard → Cnn; with guard → no diagnostic
- [ ] Runtime: correct evaluation value
- [ ] Runtime: null/missing field → `EvaluationResult.Fail` (not throw)
- [ ] Guard routing: event fires on correct branch
- [ ] `CatalogDriftTests.cs`: new Cnn entry with `-> no transition` (not `-> transition B`)

### Docs & samples
- [ ] `PreceptLanguageDesign.md`: new section or update
- [ ] Sample files (≥ 3) updated

## Checklist granularity rules

**Too sparse (reject):**
- `- [ ] Type checker: Return number type, reject on non-string, nullable diagnostics`

**Good:**
- `- [ ] Type checker: `TryInferKind` resolves SubMember key in symbol table`
- `- [ ] C56: fires for nullable string field without null guard (guard scope)`
- `- [ ] C56: fires for nullable string field without null guard (invariant scope — separate symbol injection path)`

Each checkbox should name a file, a behavior, or a specific case. If you can't tell from the checkbox whether it was done without reading the code, it's too vague.

## Checkbox maintenance (Scribe responsibility)

After every push to the PR branch:
1. Re-read the PR body to get current checkbox state
2. Check which items were completed in the most recent work batch (from agent summaries)
3. Toggle `[ ]` → `[x]` for completed items only
4. Update via `gh pr edit {number} --body-file {tempfile}`

**Rules:**
- Never uncheck a previously checked item
- When in doubt, leave unchecked
- If an entire section is done, verify each item individually before checking the section header

## CatalogDriftTests footgun (Precept-specific — applies to any PR adding a Cnn diagnostic)

When adding a `CatalogDriftTests` entry for a compile-phase constraint that fires in a `when` guard:
- Use `-> no transition` as the row target
- Using `-> transition B` (undeclared state) triggers C54 before the intended constraint, masking the error
- Always verify the trigger DSL produces ONLY the intended constraint in isolation
