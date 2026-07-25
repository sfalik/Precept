# language/ — Language Specification and Type System Design

> [!IMPORTANT]
> **The catalog-system.md [§ Architectural Identity](catalog-system.md#architectural-identity-metadata-driven) section is non-negotiable.** Before implementing any language feature, verify:
>
> - Does this language element get cataloged? (tokens, types, operators, actions, modifiers, constructs, constraints, proof requirements — all get cataloged)
> - Does any pipeline stage switch on enum identity to apply per-member behavior? That behavior belongs in the catalog entry.
> - Are shapes uniform or varied? Flat `sealed record` vs discriminated union — no nullable fields to paper over shape differences.
> - Is anything deriving from the catalog, or maintaining a parallel copy?
>
> See also: **[contributing/catalog-driven-checklist.md](../contributing/catalog-driven-checklist.md)**

Documents defining the Precept DSL surface — what the language looks like to authors — and the type system extensions that expand its expressiveness.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [precept-language-spec.md](precept-language-spec.md) | Formal language specification (v2). Grows incrementally as each compiler stage locks decisions. Sections: § 0 Preamble, § 1 Lexer, § 2 Parser, § 3 Name Binding and Type Checking, § 3A Language Semantics, § 4 Graph Analyzer, § 5 Proof Engine. | Incremental |
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

- `docs/archive/language-design/precept-language-vision.md` — archived v1 language vision. Superseded by the v2 spec in this folder.
- `research/language/` — precedent surveys, rationale, and proposal research that ground the decisions here. See `research/language/README.md` for the domain index.
- `docs/compiler/` — the pipeline stage docs that implement this language surface.
- `docs/runtime/runtime-api.md` — the public surface that exposes the language at runtime.

## Cross-cutting concerns

- **Catalog discipline.** Every language element (token, type, operator, modifier, action, construct, expression form, accessor, constraint, proof requirement, outcome) gets a catalog entry first. The [!IMPORTANT] callout at the top of this README enumerates the questions to answer. Pipeline code derives from catalogs; it never re-encodes language knowledge.
- **Authoring audience.** The primary author of `.precept` is the **domain expert**, not the developer. See [`philosophy.md § Who authors a precept`](../philosophy.md). This constrains keyword choices, error message wording, modifier surface complexity, and which expressions are allowed at the surface.
- **Approximation honesty.** Every type doc states the type family's stance on approximation — exact, admits approximation in cases X/Y, or approximate-by-design. See the Approximation Stance sections in the type docs.
