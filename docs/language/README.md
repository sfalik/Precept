# language/ — Language Specification and Type System Design

Documents defining the Precept DSL surface — what the language looks like to authors — and the type system extensions that expand its expressiveness.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [precept-language-spec.md](precept-language-spec.md) | Formal language specification (v2). Grows incrementally as each compiler stage locks decisions. Sections: §1 Lexer, §2 Parser, §3 Type Checker. | Incremental |
| [precept-language-vision.md](../archive/language-design/precept-language-vision.md) | **Archived.** Former target language surface — superseded by the spec. | Archived |
| [primitive-types.md](primitive-types.md) | Canonical reference for `string`, `integer`, `decimal`, `number`, `boolean`, `choice`. Owns numeric lane rules, conversion map, constraints, and built-in functions. | Active |
| [temporal-type-system.md](temporal-type-system.md) | NodaTime-aligned temporal types (`date`, `time`, `datetime`, `instant`, `period`, `duration`, `timezone`, `zoneddatetime`). Typed constants, operators, and bridge functions. | Draft — [Issue #107](https://github.com/sfalik/Precept/issues/107) |
| [business-domain-types.md](business-domain-types.md) | Currency, quantity, unit-of-measure, and price types. Depends on temporal design for typed constant syntax and `in` pattern. | Draft — [Issue #95](https://github.com/sfalik/Precept/issues/95) |
| [collection-types.md](collection-types.md) | Canonical reference for `set of T`, `queue of T`, `stack of T`. Actions, accessors, emptiness safety, constraints, `~string` inner type. Includes proposed extensions (quantifiers, field constraints). | Draft |

## Reading Order

1. **Start here:** [precept-language-spec.md](precept-language-spec.md) — the single canonical language specification
2. **Primitive types:** [primitive-types.md](primitive-types.md) — canonical reference for primitives and numeric lane rules
3. **Implementation blueprint:** [../compiler/type-checker.md](../compiler/type-checker.md) — how the language surface is enforced (operator tables, accessor tables, comparison rules, diagnostic codes)
4. **Collection types:** [collection-types.md](collection-types.md) — canonical reference for set, queue, stack
5. **Type extensions:** [temporal-type-system.md](temporal-type-system.md) → [business-domain-types.md](business-domain-types.md) (business types depend on temporal)

## Relationship to Other Docs

- `docs/PreceptLanguageDesign.md` — v1 implemented language spec. This folder designs the v2 replacement.
- `research/language/` — precedent surveys, rationale, and proposal research that ground the decisions here.
- `docs/compiler/` — the pipeline stage docs that implement this language surface.
