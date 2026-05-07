# Proposal Canonicalization

**When to use:** A working proposal (in `docs/working/`) has been finalized — all blocking OQs are closed and the decision is approved. Its content must be propagated into canonical docs and the proposal archived.

## Steps

1. **Inventory downstream docs.** Read the proposal's "What Gets Unblocked" or equivalent section. Identify every canonical doc that references or conflicts with the proposal's content. Typical targets: `docs/runtime/result-types.md`, `docs/runtime/evaluator.md`, `docs/tooling/language-server.md`, `docs/tooling/mcp.md`.

2. **Read before you write.** For each target doc, read the existing section fully before modifying. Understand the current shape so edits are surgical replacements, not blind overwrites.

3. **Update all downstream docs in one pass.** Do not partially update — that creates a window where docs are mutually inconsistent. For each target:
   - Replace type definitions with the canonical shape from the proposal
   - Remove stale open questions that the proposal resolved
   - Preserve open questions the proposal explicitly left pending (e.g., OQ-4)
   - Add new types/concepts the proposal introduced (e.g., new DU variants, new record types)

4. **Update cross-cutting references.** In `docs/working/cross-cutting-decisions.md`:
   - Verify the CC status table points at **canonical docs** (not the working proposal) as the authoritative home
   - Update the CC body's resolution text to reference canonical doc paths

5. **Archive the proposal.** Move to `docs/working/archive/`. The archive preserves the full deliberation history (rationale, alternatives rejected, OQ closures) for provenance.

6. **Write inbox entry.** Summarize what was moved, which docs were updated, and what remains pending (unclosed OQs). Drop in `.squad/decisions/inbox/`.

7. **Update agent history.** Append a learning entry in `.squad/agents/{agent}/history.md` covering the canonicalization.

## Anti-patterns

- **Partial propagation.** Updating `result-types.md` but not `evaluator.md` leaves the docs inconsistent. Always update all targets in one pass.
- **Closing pending OQs.** If the proposal says "pending Shane's call," do not close it during canonicalization. Note it as pending in the canonical doc.
- **Redesigning during canonicalization.** This is a mechanical propagation task, not a design session. If you spot a design issue, flag it — do not resolve it inline.
- **Leaving CC references pointing at the proposal.** After canonicalization, the proposal is archived. CC references must point at canonical docs.
