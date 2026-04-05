## Core Context

- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-05 - Proposal bodies expanded for issues #11-#13
- Expanded GitHub issues #11, #12, and #13 into fuller proposal narratives with before/after Precept examples, reference-language snippets, and explicit architectural cautions.
- Logged the wave placement and guardrails in .squad/decisions.md so the issue bodies stay aligned with keyword-anchored flat statements and first-match routing.

### 2026-04-05 - Trunk consolidation dissent logged
- Audited the repo topology and argued for force-promoting 'feature/language-redesign' to 'main' because 'main' still carries only placeholder history.
- The team did not adopt that path: Uncle Leo's review blocked direct trunk replacement, so Frank's recommendation now stands as a documented dissent pending Shane sign-off.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

## Learnings

### 2026-04-05 - Language proposal review sequencing
- Reviewed language proposal issues #8-#13 against the DSL expressiveness research and the current language-design constraints.
- Recommended first-wave candidates: `#10` string `.length` and `#8` named guards; second wave: `#9` ternary-in-`set`, then `#11` absorb shorthand; last wave: `#12` inline `else reject` and `#13` field-level constraints.
- Reaffirmed that keyword-anchored flat statements and first-match routing are architectural guardrails; proposals that pressure either surface need explicit containment or they will sprawl.

### 2026-04-05 - Language proposal body expansion (#11, #12, #13)
- Expanded issues #11, #12, #13 from acceptance-criteria stubs into full proposal writeups with real Precept examples drawn from existing sample files and reference-language code (xstate, Polly, Zod, FluentValidation).
- **#11 (`absorb`):** `absorb` must be event-scoped (not bare); explicit `set` takes precedence; language server must warn on zero-match absorb. Last wave.
- **#12 (`else reject`):** Scope locked to `reject` only — never `else transition` or `else set`. Only one `else reject` per event+state pair; multi-guard scenarios must use standalone fallback rows. The multi-else-reject interaction must be resolved in a design doc before any code. Second-to-last wave.
- **#13 (field-level constraints):** Shape A (inline `min`/`max`) violates the keyword-anchor principle — research README already rejected it. Shape B (`constrain` keyword) preserves the principle but creates two constraint pathways. Neither shape is implementation-ready without a Shane sign-off on which to adopt. Last wave.
- Decisions inbox entry written at `.squad/decisions/inbox/frank-expand-language-proposals.md`.

