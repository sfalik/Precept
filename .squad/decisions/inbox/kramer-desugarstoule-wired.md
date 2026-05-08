### 2026-05-08: DesugarsToRule wired into grammar generator
**By:** Kramer (requested by Shane)
**What:** Generator now reads Modifiers.All.Where(m => m.DesugarsToRule) to emit gold-colored TextMate patterns for rule-desugaring modifiers.
**Scope used:** `keyword.other.grammar.precept`
**Why:** Catalog gap — the old hand-authored grammar gold-highlighted these modifiers but the generator had no path for it.
