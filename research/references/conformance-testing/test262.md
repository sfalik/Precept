# Mirror: Test262 (ECMAScript conformance suite)

Source captures for the spec-conformance-test-suite survey. Snapshotted 2026-06-01 to defend against URL rot.

## CONTRIBUTING.md — `esid` / `es6id` keys, naming transition

Source: `tc39/test262` repo `CONTRIBUTING.md` (https://github.com/tc39/test262/blob/main/CONTRIBUTING.md and raw), accessed 2026-06-01. Grade: Primary.

Verbatim:

> `esid: [spec-id]`
> "This key is required for all new feature tests. This key identifies the hash ID from the portion of the ECMAScript draft which is most recent to the date the test was added. It represents the anchors on the generated HTML version of the specs. E.g.: `esid: sec-typedarray-length`."

> `es6id: [es6-test-id]`
> "This key identifies the section number from the portion of the ES6 standard that is tested by this test _at the time the test was written_. The es6ids might not correspond to the correction section numbers in the ES6 (or later) specification because routine edits to the specification will change section numbering." [deprecated in favor of `esid`]

> "The project is currently transitioning from a naming system based on specification section numbers. There remains a substantial number of tests that conform to this outdated convention; contributors should ignore that approach when introducing new tests and instead encode this information using [the `esid` frontmatter key]."

> tests must originate "with citable, normative text in the latest draft of the ECMAScript Language Specification"

## INTERPRETING.md — negative tests (phase + type), flags

Source: `tc39/test262` repo `INTERPRETING.md` (raw), accessed 2026-06-01. Grade: Primary.

Verbatim (negative frontmatter):

> "These tests are expected to generate an uncaught exception. The value of this attribute is a YAML dictonary with two keys:
> - `phase` - the stage of the test interpretation process that the error is expected to be produced; valid phases are:
>   - `parse`: occurs while parsing the source text, or while checking it for early errors.
>   - `resolution`: occurs during module resolution.
>   - `runtime`: occurs during evaluation.
> - `type` - the name of the constructor of the expected error"

> "By default, tests signal failure by generating an uncaught exception. If execution completes without generating an exception, the test must be interpreted as 'passing.'"

> `includes`: "One or more files whose content must be evaluated in the test realm's global scope prior to test execution"

Flags include: onlyStrict, noStrict, module, raw, async, generated, CanBlockIsFalse, CanBlockIsTrue, non-deterministic.

CONTRIBUTING: parsing-error expectations "should be declared using the `negative` frontmatter flag" and must include `$DONOTEVALUATE();`; runtime-error expectations "should be defined using the `assert.throws` method and the appropriate JavaScript Error constructor function."
