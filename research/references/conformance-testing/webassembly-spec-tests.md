# Mirror: WebAssembly spec test suite

Snapshotted 2026-06-01.

## README.md — test suite co-located with spec + reference implementation

Source: `WebAssembly/spec` repo `README.md` (https://github.com/WebAssembly/spec/blob/main/README.md), accessed 2026-06-01. Grade: Primary.

Verbatim:

> "This repository holds the sources for the WebAssembly specification, a reference implementation, and the official test suite."

## test/ directory organization

Source: `WebAssembly/spec/tree/main/test`, accessed 2026-06-01. Grade: Primary.

- `core/` — "tests for the core semantics"
- `js-api/` — "tests for the JavaScript API"
- `html/` — "tests for the JavaScript API in a DOM environment"

> "The wast tests can be converted to JavaScript, and the JavaScript tests to HTML tests, using the `build.py` script." ... "Each wast test gets its equivalent JS test, and each JS test (including wast test) gets its equivalent WPT."

## interpreter/README.md — script assertion commands (.wast)

Source: `WebAssembly/spec/blob/main/interpreter/README.md`, accessed 2026-06-01. Grade: Primary.

Verbatim assertion semantics:

> **assert_return**: "assert action has expected results"
> **assert_trap**: "assert action traps with given failure string"
> **assert_invalid**: "assert module is invalid with given failure string"
> **assert_malformed**: "assert module cannot be decoded with given failure string"
> **assert_unlinkable**: "assert module fails to link"
> **assert_exhaustion**: "assert action exhausts system resources"

On failure strings / oracle:

> failure strings "exist for documentation purposes," and "the reference interpreter itself checks that the string is a prefix of the actual error message it generates."
