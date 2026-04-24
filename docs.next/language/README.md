# language/ — Language Specification and Type System Design

Documents defining the Precept DSL surface — what the language looks like to authors — and the type system extensions that expand its expressiveness.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [precept-language-spec.md](precept-language-spec.md) | Formal language specification (v2). Grows incrementally as each compiler stage locks decisions. Sections: §1 Lexer, §2 Parser, §3 Type Checker. | Incremental |
| [precept-language-vision.md](precept-language-vision.md) | Target language surface — combines implemented features with approved proposals into a single future-state reference. | Working |
| [primitive-types.md](primitive-types.md) | Canonical reference for `string`, `integer`, `decimal`, `number`, `boolean`, `choice`. Owns numeric lane rules, conversion map, constraints, and built-in functions. | Active |
| [temporal-type-system.md](temporal-type-system.md) | NodaTime-aligned temporal types (`instant`, `localdate`, `localtime`, `localdatetime`, `period`, `duration`, `timezone`). Typed constants, operators, and bridge functions. | Draft — [Issue #107](https://github.com/sfalik/Precept/issues/107) |
| [business-domain-types.md](business-domain-types.md) | Currency, quantity, unit-of-measure, and price types. Depends on temporal design for typed constant syntax and `in` pattern. | Draft — [Issue #95](https://github.com/sfalik/Precept/issues/95) |

## Reading Order

1. **Start here:** [precept-language-vision.md](precept-language-vision.md) — the full target language surface in one document
2. **Primitive types:** [primitive-types.md](primitive-types.md) — canonical reference for primitives and numeric lane rules
3. **Formal spec:** [precept-language-spec.md](precept-language-spec.md) — locked decisions only, grows per compiler stage
4. **Implementation blueprint:** [../compiler/type-checker.md](../compiler/type-checker.md) — how the language surface is enforced (operator tables, accessor tables, comparison rules, diagnostic codes)
5. **Type extensions:** [temporal-type-system.md](temporal-type-system.md) → [business-domain-types.md](business-domain-types.md) (business types depend on temporal)

## Relationship to Other Docs

- `docs/PreceptLanguageDesign.md` — v1 implemented language spec. This folder designs the v2 replacement.
- `research/language/` — precedent surveys, rationale, and proposal research that ground the decisions here.
- `docs.next/compiler/` — the pipeline stage docs that implement this language surface.
