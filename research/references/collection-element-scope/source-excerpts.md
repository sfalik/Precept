# Source excerpts — element-vs-aggregate constraint scope across schema/constraint systems

Mirror of the load-bearing external excerpts cited by
[`research/language/collection-element-aggregate-scope-syntax.md`](../../language/collection-element-aggregate-scope-syntax.md).
Snapshotted to defend against URL rot. Access dates are 2026-06-03 unless noted.

---

## JSON Schema (Draft 2020-12)

Source: *Understanding JSON Schema — Arrays*,
<https://json-schema.org/understanding-json-schema/reference/array> (accessed 2026-06-03). Primary (official project documentation).

- Element constraints nest inside `items`:
  > "{ 'type': 'array', 'items': { 'type': 'number' }}" — the `items` keyword
  > contains a schema object that applies to each element.
- Aggregate/length constraints are top-level array keywords:
  > "The length of the array can be specified using the `minItems` and `maxItems` keywords."
  > "A schema can ensure that each of the items in an array is unique. Simply set the `uniqueItems` keyword to `true`."
- `minItems`, `maxItems`, `uniqueItems` sit at the same structural level as `type: array`, **not** nested inside element schemas.
- Page documents Draft 2020-12 (with `unevaluatedItems` "New in draft 2019-09", `contains` "New in draft 6").

## CUE

Source: *CUE Tour — Lists* (types/lists), <https://cuelang.org/docs/tour/types/lists/> (accessed 2026-06-03). Primary (official language documentation).

- Element constraints live **inside** the `[...]` brackets:
  > "[1, 2, 3, ...int]" means "any additional elements must be ints."
  > "Open lists may contain some predefined elements, followed by `...` and an optional value that constrains any elements that follow."
- Length/cardinality is controlled by closed-vs-open (presence/absence of `...`):
  > a "closed list statically defines its length each and every time its elements are specified"; `[int, int, ...int]` is "an open list containing at least 2 ints."
- Distinction: element type constraints appear inside the brackets (`...int`); length flexibility is the `...` operator at the list's end.

## protovalidate (Buf)

Source: *protovalidate — Standard rules*, <https://protovalidate.com/schemas/standard-rules/> (accessed 2026-06-03). Secondary (vendor documentation, Buf).

- Per-element rules nest under `repeated.items`:
  > "Use `items` to apply validation rules to each element in a repeated field"
  ```protobuf
  repeated string tags = 2 [(buf.validate.field).repeated.items.string = {
    min_len: 1
    max_len: 50
  }];
  ```
  ("all tags must be between 1 and 50 characters.")
- Aggregate count rules are sibling keys on `repeated`:
  > "Use `min_items` and `max_items` for repeated fields"
  ```protobuf
  repeated string members = 1 [(buf.validate.field).repeated.min_items = 1];
  ```
  ("must have at least one member.")
- Aggregate uniqueness:
  > "Use `unique` to ensure all items in a repeated field are unique. Works for scalar and enum types only"
  ```protobuf
  repeated string user_ids = 1 [(buf.validate.field).repeated.unique = true];
  ```

## XSD / XML Schema (W3C XML Schema Part 0: Primer)

Source: *XML Schema Part 0: Primer*, <https://www.w3.org/TR/xmlschema-0/> (accessed 2026-06-03). Primary (W3C Recommendation, primer companion).

- Occurrence (aggregate cardinality) is on the element particle via `minOccurs`/`maxOccurs`:
  > "The maximum number of times an element may appear is determined by the value of a maxOccurs attribute in its declaration."
  > "The default value for both the minOccurs and the maxOccurs attributes is 1."
- Per-element value constraint is a separately-derived `simpleType` using `restriction` + facets:
  > "New simple types are defined by deriving them from existing simple types (built-in's and derived)."
  > "We use the restriction element to indicate the existing (base) type, and to identify the 'facets' that constrain the range of values."
- The two axes are separate: occurrence governs *how many*; the simpleType/facet derivation governs *what values are allowed*.

## Ada (Ada Reference Manual 3.6 — Array Types)

Source: *Ada Reference Manual, 3.6 Array Types*, <https://ada-lang.io/docs/arm/AA-3/AA-3.6/> (accessed 2026-06-03). Primary (Ada Reference Manual, ISO/IEC 8652 community mirror).

- Array type definition names a **component subtype** (per-element type, with its own constraint) separately from the **index** definition (which bounds cardinality):
  - `index_subtype_definition ::= subtype_mark | range <>`
  - `component_definition` carries an optional `aliased` before the subtype indication.
- All components share one subtype:
  > "All components of an array have the same subtype. In particular, for an array of components that are one-dimensional arrays, this means that all components have the same bounds and hence the same length."
- The component_definition establishes the per-element type (a definite subtype); index constraints specify array bounds. (Index constraint syntax lives in §3.6.1.)

## SQL — PostgreSQL CREATE DOMAIN + array column

Source: *PostgreSQL 18 — CREATE DOMAIN*, <https://www.postgresql.org/docs/current/sql-createdomain.html> and *§8.15 Arrays*, <https://www.postgresql.org/docs/current/arrays.html> (accessed 2026-06-03). Primary (official PostgreSQL documentation).

- A DOMAIN is a **named type carrying a per-value CHECK constraint** (uses the keyword `VALUE`):
  > "CHECK clauses specify integrity constraints or tests which values of the domain must satisfy. Each constraint must be an expression producing a Boolean result. It should use the key word VALUE to refer to the value being tested."
  ```sql
  CREATE DOMAIN us_postal_code AS TEXT
  CHECK( VALUE ~ '^\d{5}$' OR VALUE ~ '^\d{5}-\d{4}$' );
  ```
- The domain's underlying type can be an array:
  > "The underlying data type of the domain. This can include array specifiers."
- Aggregate (array-length) constraints are expressed via functions like `array_length(value, 1)` in a CHECK on a domain over an array type (mailing-list example, Tertiary):
  ```sql
  create domain myintarray as int[] check (
    (array_length(value,1) > 0) and (array_length(value,2) is null) );
  ```
  Source for the array_length example: PostgreSQL pgsql-bugs list message
  <https://www.postgresql.org/message-id/4AFC5BBC.90202@phlo.org> (accessed 2026-06-03; Tertiary — mailing-list post).
- The dominant SQL idiom: lift the per-element value rule into a **named domain** (`positive_int`), then use `positive_int[]` as the column type. Element constraint = named separate type; aggregate constraint = function-on-the-array.
