# Frank — Iteration 11 doc/catalog audit findings

Date: 2026-05-02

## New gaps filed
- GAP-048 — Guarded state/event ensures are specified, but `Constructs` metadata has no `GuardClause` slot for either ensure form.
- GAP-049 — Guarded state actions are specified, but `Constructs.StateAction` does not model a guard.
- GAP-050 — Stateless event hooks are documented with a trailing `ensure`, but no construct or constraint catalog member models that surface.
- GAP-051 — Spec §3A.3 misstates the constraint taxonomy by calling rejection one of the constraint kinds and collapsing the three state-ensure anchors.
- GAP-052 — `queue of T by P ascending|descending` is documented in grammar, but `Types` metadata has no direction slot.
- GAP-053 — Quantifier grammar names `ExpectedFieldName`, but that diagnostic does not exist in the catalog.
- GAP-054 — Queue-by dequeue semantics diverge: the spec treats `by H` as a keyed selector, while the action catalog does not model that selector semantics.
- GAP-055 — `timezone`, `currency`, `unitofmeasure`, and `dimension` carry implied `notempty` in `Types`, but the spec never documents that intrinsic behavior.
- GAP-056 — `~string` function behavior is parameter-specific in the spec, but the function catalog only links CI variants at the whole-function level.

## Most significant findings
1. The construct catalog is behind the spec on three declaration surfaces: guarded ensures, guarded state actions, and stateless event-hook trailing `ensure`.
2. The semantic write-up for constraints no longer matches the actual `ConstraintKind` taxonomy.
3. Queue-by semantics are split across grammar, actions, and type metadata with no single catalog truth for direction or keyed dequeue behavior.

## Design decisions Shane should make before fixes
- Is guarded `ensure` an actual supported surface that must be cataloged, or did the spec get ahead of the intended language?
- Should stateless event handlers really support a trailing post-mutation `ensure`, or should that syntax be removed from the spec?
- For `queue of T by P`, what is the canonical meaning of direction (`ascending`/`descending`) and of `dequeue ... by H` — selector, capture, or something else?
- Do the intrinsic `notempty` semantics on identity types belong in the public spec as language behavior, or should they be demoted from catalog metadata?
- Do CI string-function rules need richer catalog metadata (e.g. CI-sensitive parameter position), or is out-of-band checker logic the intended design?
