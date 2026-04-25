---
updated_at: 2026-04-25T00:00:00Z
focus_area: Metadata-driven catalog design review + Phase 1 completion
active_issues: []
---

# What We're Focused On

**Branch:** `precept-architecture` — v1 purge complete, v2 is the only implementation.

**Three-phase plan (Shane's direction, 2026-04-25):**

1. **Phase 1 (CURRENT): Finish the catalog design.** Working through 10 review items from Frank's full team review of `docs/catalog-system.md`. Updating adjacent pipeline design docs. Creating stubs for undocumented stages. Anti-bias principle: push metadata-driven as far as it goes — don't let AI training priors pull toward traditional compiler patterns.

2. **Phase 2: Build the full catalog in code.** Implement 7 remaining catalogs in dependency order (Types+Operators → Operations → Functions → Modifiers → Actions → Constructs). Each catalog captures the language spec in machine-readable form.

3. **Phase 3: Revisit pipeline stages.** Make Lexer, Parser, TypeChecker metadata-driven where it makes sense. Parser vocabulary tables → frozen dictionaries. TypeChecker → catalog lookups for everything.

**17 inbox files** in `.squad/decisions/inbox/` with all design decisions from the team review — pending merge after Phase 1 review completes.
