# Diagnostic Name & Message UX Review
> Authored by Elaine | 2026-05-13

---

## Summary

- **Total diagnostics reviewed:** 132
- **Names flagged for revision:** 34 (⚠️ 33 · 🔴 1)
- **Messages flagged for revision:** 26 (⚠️ 25 · 🔴 1)
- **High-priority fixes (commonly triggered, jargon in user-visible output):** 12

**Overall posture:** The catalog is in good shape for the majority of diagnostics — the naming style is consistent and most messages name subjects, state conditions, and repairs clearly. The problem areas cluster into five identifiable families: (1) the `Unproved*` proof-engine name prefix leaking into user-visible text, (2) graph-theory jargon (`dominate`, `back-edge`, `sink`) surfacing in both names and messages, (3) the CI-enforcement family using operator tokens as name fragments instead of a shared condition-first `CaseMismatch*` pattern, (4) collection-safety names using `guard` wording instead of the DSL's `when` clause vocabulary, and (5) a handful of terse business-domain messages that omit catalog examples.

---

## Review by Group

### Group: Lex (PRE0001–PRE0008)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0001 | `InputTooLarge` | Error | "This definition exceeds the 65,536-character security limit and cannot be processed" | ✅ Good | ✅ Good | Clear size limit in message. |
| PRE0002 | `UnterminatedStringLiteral` | Error | "Text value opened with \" is missing its closing quote — every \" must have a matching \"" | ✅ Good | ✅ Good | DSL-friendly "text value" phrasing. |
| PRE0003 | `UnterminatedTypedConstant` | Error | "Value opened with ' is missing its closing quote — every ' must have a matching '" | ✅ Good | ✅ Good | Mirrors PRE0002 style correctly. |
| PRE0004 | `UnterminatedInterpolation` | Error | "The { } section is not closed — add a closing } on the same line" | ✅ Good | ⚠️ Needs revision | "{ } section" is vague. Doesn't name what was opened. |
| PRE0005 | `InvalidCharacter` | Error | "'{0}' is not a valid character in a precept definition — remove or replace it" | ✅ Good | ✅ Good | Subject named, repair stated. |
| PRE0006 | `UnrecognizedStringEscape` | Error | "'\\{0}' is not a valid escape in a text value — use \\\" for a quote, \\\\ for a backslash, \\n for a newline, or \\t for a tab" | ✅ Good | ✅ Good | Complete list of valid escapes. |
| PRE0007 | `UnrecognizedTypedConstantEscape` | Error | "'\\{0}' is not a valid escape in a single-quoted value — use \\' for a quote, or \\\\ for a backslash" | ✅ Good | ✅ Good | Distinct from PRE0006 — good. |
| PRE0008 | `UnescapedBraceInLiteral` | Error | "Use '}}' to include a literal } in this value — a single } closes an interpolation, so it must be doubled" | ✅ Good | ⚠️ Needs revision | Technically correct but the second sentence re-explains the obvious. Trim it. |

---

### Group: Parse (PRE0009–PRE0016, PRE0127)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0009 | `ExpectedToken` | Error | "Expected {0} here, but found '{1}'" | ✅ Good | ⚠️ Needs revision | "here" is structurally vague — message has no context about what was being parsed. Acceptable for a catch-all but the word "here" adds nothing. |
| PRE0010 | `NonAssociativeComparison` | Error | "Comparisons like == and < cannot be chained — {0}" | ✅ Good | ⚠️ Needs revision | What is `{0}`? The purpose of that trailing slot is unclear without inspecting the emitter. |
| PRE0011 | `UnexpectedKeyword` | Error | "'{0}' is a keyword and cannot be used as a value — expected an expression here" | ✅ Good | ✅ Good | Names the keyword, states the constraint. |
| PRE0012 | `InvalidCallTarget` | Error | "'{0}' is not callable — only function names and member access expressions can be called" | ✅ Good | ✅ Good | Includes a concrete example form. |
| PRE0013 | `OmitDoesNotSupportGuard` | Error | "'omit' is an unconditional structural exclusion — 'when' guards are not allowed" | ✅ Good | ✅ Good | |
| PRE0014 | `EventHandlerDoesNotSupportGuard` | Error | "Event handlers ('on Event -> action') do not support 'when' guards — guards are only valid on event ensures and transition rows" | ✅ Good | ✅ Good | Names the valid alternatives. |
| PRE0015 | `PreEventGuardNotAllowed` | Error | "A 'when' guard before the event target is not supported on transition rows — place the guard after 'on Event'" | ⚠️ Needs revision | ✅ Good | "PreEvent" is a parser-internal structural term. A DSL author writes transition rows, not "pre-event" clauses. |
| PRE0016 | `ExpectedOutcome` | Error | "Expected a transition outcome ('-> transition State', '-> no transition', or '-> reject Message') but none was found" | ✅ Good | ✅ Good | Lists all three valid forms — excellent. |
| PRE0127 | `AssignmentInExpressionContext` | Error | "'=' is assignment and cannot be used inside an expression — use '==' for equality" | ⚠️ Needs revision | ✅ Good | "ExpressionContext" is compiler terminology. The name describes an internal parser concept, not the author-visible condition. |

---

### Group: Type / Naming (PRE0017, PRE0024–PRE0030, PRE0103, PRE0107)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0017 | `UndeclaredField` | Error | "Field '{0}' is not declared" | ✅ Good | ✅ Good | |
| PRE0024 | `DuplicateFieldName` | Error | "Field '{0}' is already declared" | ✅ Good | ✅ Good | |
| PRE0025 | `DuplicateStateName` | Error | "State '{0}' is already declared" | ✅ Good | ✅ Good | |
| PRE0026 | `DuplicateEventName` | Error | "Event '{0}' is already declared" | ✅ Good | ✅ Good | |
| PRE0027 | `DuplicateArgName` | Error | "Argument '{0}' is already declared on event '{1}'" | ✅ Good | ✅ Good | |
| PRE0028 | `UndeclaredState` | Error | "State '{0}' is not declared" | ✅ Good | ✅ Good | |
| PRE0029 | `UndeclaredEvent` | Error | "Event '{0}' is not declared" | ✅ Good | ✅ Good | |
| PRE0030 | `UndeclaredFunction` | Error | "'{0}' is not a recognized function" | ✅ Good | ✅ Good | |
| PRE0103 | `BindingShadowsField` | Error | "Binding variable '{0}' shadows a field with the same name — rename the binding to avoid confusion" | ✅ Good | ✅ Good | |
| PRE0107 | `UndeclaredArg` | Error | "Argument '{0}' is not declared on event '{1}'" | ✅ Good | ✅ Good | |

---

### Group: Type / Structure & TypeSystem (PRE0018–PRE0054, PRE0120, PRE0128–PRE0132)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0018 | `TypeMismatch` | Error | "Expected a {0} value here, but got '{1}'" | ✅ Good | ✅ Good | |
| PRE0019 | `ValueMayBeAbsent` <br><sub>current: `NullInNonNullableContext`</sub> | Error | "'{0}' may be unset — this position requires a value" | ✅ Rename to `ValueMayBeAbsent` | ✅ Replace message | Emission-site audit complete: no live emitters exist in `src/Precept/`; the code is declaration/metadata-only today. Recommendation is future-safe for the deferred precision-upgrade surface and does not falsely narrow to optional-field + `when`. |
| PRE0020 | `InvalidMemberAccess` | Error | "'.{0}' is not available on {1} fields" | ✅ Good | ✅ Good | |
| PRE0021 | `FunctionArityMismatch` | Error | "'{0}' takes {1} inputs, but {2} were provided" | ⚠️ Needs revision | ✅ Good | "Arity" is a math/CS term. Message is clear; name is the issue. |
| PRE0022 | `FunctionArgConstraintViolation` | Error | "Argument {0} to '{1}' is not valid — {2}" | ⚠️ Needs revision | ⚠️ Needs revision | Name: overly long internal phrase. Message: `Argument {0}` uses a bare ordinal (1, 2…) — no indication whether that's "first", "second", or the arg's name. |
| PRE0023 | `MutuallyExclusiveQualifiers` | Error | "'in' and 'of' qualifiers cannot both appear on this type — only 'price' supports both" | ✅ Good | ✅ Good | Correctly names the exception. |
| PRE0031 | `MultipleInitialStates` | Error | "Only one state can be marked 'initial' — '{0}' and '{1}' both are" | ✅ Good | ✅ Good | |
| PRE0032 | `NoInitialState` | Error | "This precept has states but none is marked 'initial'" | ✅ Good | ✅ Good | |
| PRE0033 | `InvalidModifierForType` | Error | "The '{0}' constraint does not apply to {1} fields" | ✅ Good | ✅ Good | |
| PRE0034 | `InvalidModifierBounds` | Error | "{0} ({1}) cannot exceed {2} ({3})" | ✅ Good | ⚠️ Needs revision | Message is four opaque slots. Readers have to know the call site to understand "min (10) cannot exceed max (0)". |
| PRE0035 | `InvalidModifierValue` | Error | "The value for '{0}' must be {1}" | ✅ Good | ✅ Good | |
| PRE0036 | `DuplicateModifier` | Error | "The '{0}' constraint is already applied to this field" | ✅ Good | ✅ Good | |
| PRE0037 | `RedundantModifier` | Warning | "'{0}' is unnecessary — '{1}' already implies it" | ✅ Good | ✅ Good | |
| PRE0038 | `ComputedFieldNotWritable` | Error | "Field '{0}' is computed and cannot be assigned" | ✅ Good | ✅ Good | |
| PRE0039 | `ComputedFieldWithDefault` | Error | "Field '{0}' is computed and cannot have a default value" | ✅ Good | ✅ Good | |
| PRE0040 | `CircularComputedField` | Error | "Computed field '{0}' has a circular dependency: {1}" | ✅ Good | ✅ Good | |
| PRE0041 | `WritableOnEventArg` | Error | "The 'writable' modifier cannot appear on event argument '{0}'" | ⚠️ Needs revision | ✅ Good | Name reads right-to-left. Subject-first would be `EventArgCannotBeWritable`. |
| PRE0042 | `ConflictingAccessModes` | Error | "Field '{0}' has conflicting access modes in state '{1}'" | ✅ Good | ✅ Good | |
| PRE0043 | `RedundantAccessMode` | Error | "The '{0}' access mode for field '{1}' in state '{2}' is redundant — the effective mode is already '{0}'" | ✅ Good | ✅ Good | Minor: `{0}` appears twice but this is acceptable. |
| PRE0044 | `ListLiteralOutsideDefault` | Error | "List values can only appear in default clauses" | ✅ Good | ⚠️ Needs revision | Message doesn't say what was found or where — no subject named. |
| PRE0045 | `DuplicateChoiceValue` | Error | "Choice value '{0}' is duplicated" | ✅ Good | ✅ Good | |
| PRE0046 | `EmptyChoice` | Error | "A choice type must have at least one value" | ✅ Good | ✅ Good | |
| PRE0047 | `CollectionOperationOnScalar` | Error | "'{0}' requires a collection, but '{2}' is a single value — change '{2}' to a set, list, or queue" | ✅ Good | ⚠️ Needs revision | Template skips `{1}` — unusual and potentially a bug. If `{1}` exists, its purpose is invisible. |
| PRE0048 | `ScalarOperationOnCollection` | Error | "'{0}' cannot be used with collection field '{1}'" | ✅ Good | ⚠️ Needs revision | `{0}` is the action name. Starting the sentence with an action name like "'set' cannot be used…" is slightly awkward — "The 'set' action cannot be used with collection field '{1}'" is clearer. |
| PRE0049 | `IsSetOnNonOptional` | Error | "'{0}' always has a value — 'is set' only works on optional fields" | ✅ Good | ✅ Good | |
| PRE0050 | `EventArgOutOfScope` | Error | "Event '{0}' arguments are not accessible here" | ✅ Good | ⚠️ Needs revision | "not accessible here" gives no scope guidance. |
| PRE0051 | `InvalidInterpolationCoercion` | Error | "A {0} value cannot appear inside a text interpolation" | ⚠️ Needs revision | ✅ Good | "Coercion" is type-system jargon. The failure is that a non-text type was put into a string interpolation. |
| PRE0052 | `UnresolvedTypedConstant` | Error | "Cannot determine the type of '{0}' — the content does not match any known value pattern" | ⚠️ Needs revision | ✅ Good | "Unresolved" is compiler jargon. The condition is "type cannot be determined". Compare: PRE0091 is `AmbiguousTypedConstant` — this one is unrecognized, not ambiguous. |
| PRE0053 | `InvalidTypedConstantContent` | Error | "'{0}' is not a valid {1} — check the expected format for {1} values" | ✅ Good | ✅ Good | |
| PRE0054 | `DefaultForwardReference` | Error | "Computed expression for '{0}' cannot reference '{1}', which is declared later" | ⚠️ Needs revision | ✅ Good | "Default" is misleading — this fires on computed expressions, not `default` clauses. The name conflates two distinct author concepts. |
| PRE0120 | `ConflictingModifiers` | Error | "The '{0}' modifier cannot be combined with '{1}' — these modifiers are mutually exclusive" | ✅ Good | ✅ Good | |
| PRE0128 | `StateListContainsWildcard` | Error | "State list cannot mix named states with 'any' wildcard — use either 'any' or specific state names" | ✅ Good | ✅ Good | |
| PRE0129 | `DuplicateStateInList` | Warning | "State name '{0}' appears more than once in the state list" | ✅ Good | ✅ Good | |
| PRE0130 | `OmittedFieldReadInState` | Error | "Field '{0}' is omitted in state '{1}' and cannot be read in this expression" | ✅ Good | ✅ Good | Previously reviewed and approved by Elaine. |
| PRE0131 | `OmittedFieldSetInTargetState` | Error | "Field '{0}' is omitted in target state '{1}'; this transition cannot set it" | ✅ Good | ✅ Good | Previously reviewed and approved by Elaine. |
| PRE0132 | `RequiredFieldUnassignedOnEntry` | Error | "Required field '{0}' is omitted in '{1}' but present in '{2}'; add \`set {0} = ...\` to this transition" | ✅ Good | ✅ Good | Previously reviewed and approved by Elaine. |

---

### Group: Type / Choice (PRE0085–PRE0090)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0085 | `NonChoiceAssignedToChoice` | Error | "Field '{0}' is a choice type — only choice-compatible values can be assigned to it" | ⚠️ Needs revision | ⚠️ Needs revision | Name: "NonChoice" is not author vocabulary. Message: "choice-compatible" is internal — not a term the DSL author wrote. |
| PRE0086 | `ChoiceLiteralNotInSet` | Error | "'{0}' is not a declared value of '{1}'" | ✅ Good | ✅ Good | |
| PRE0087 | `ChoiceArgOutsideFieldSet` | Error | "Argument choice includes '{0}', which is not in field '{1}'" | ✅ Good | ✅ Good | |
| PRE0088 | `ChoiceElementTypeMismatch` | Error | "Expected a {0} literal — this choice is declared as 'choice of {0}'" | ✅ Good | ⚠️ Needs revision | `{0}` appears twice, making the message read as "Expected a number literal — this choice is declared as 'choice of number'" — redundant. |
| PRE0089 | `ChoiceRankConflict` | Error | "The order of values in this argument conflicts with the declared order of '{0}'" | ⚠️ Needs revision | ✅ Good | "Rank" is an internal ordering concept. A DSL author would say "order", not "rank". |
| PRE0090 | `ChoiceMissingElementType` | Error | "A choice type requires an explicit element type — use 'choice of string(...)', 'choice of integer(...)', etc." | ✅ Good | ✅ Good | |

---

### Group: Type / Lifecycle (PRE0091–PRE0094)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0091 | `AmbiguousTypedConstant` | Error | "Typed constant '{0}' is ambiguous between {1} and {2}" | ✅ Good | ✅ Good | |
| PRE0092 | `EventHandlerInStatefulPrecept` | Error | "Event handler '{0}' is not valid in a stateful precept" | ✅ Good | ✅ Good | |
| PRE0093 | `RequiredFieldsNeedInitialEvent` | Error | "Required field(s) {0} have no initial event to assign them" | ✅ Good | ✅ Good | |
| PRE0094 | `InitialEventMissingAssignments` | Error | "Initial event '{0}' does not assign required field(s): {1}" | ✅ Good | ✅ Good | |

---

### Group: Type / Case-Insensitive Enforcement (PRE0066, PRE0095–PRE0098)

> **Systemic name issue:** All five names in this family encode DSL operator tokens (`TildeEquals`, `TildeNotEquals`, `TildeStartsWith`, `TildeEndsWith`) instead of describing what went wrong. Names should describe the condition, not the fix. This is an author-facing smell: the name encodes the repair action, not the failure.

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0066 | `CaseInsensitiveFieldRequiresTildeEquals` | Error | "'{0}' is declared ~string (case-insensitive). Use ~= instead of == to avoid treating values like 'admin@example.com' and 'Admin@example.com' as different." | ⚠️ Needs revision | ✅ Good | Name encodes the fix (`TildeEquals`) not the condition. Message is excellent — concrete email example is ideal. |
| PRE0095 | `CaseInsensitiveFieldRequiresTildeNotEquals` | Error | "'{0}' is declared ~string (case-insensitive). Use !~ instead of != (!~ returns true when values are not equal under case-insensitive comparison)." | ⚠️ Needs revision | ⚠️ Needs revision | Name: same family smell. Message: the parenthetical redefines `!~` which the author can look up — trim it. |
| PRE0096 | `CaseInsensitiveValueInCaseSensitiveContains` | Error | "'{0}' is ~string (case-insensitive) but '{1}' is {2} (case-sensitive). A case-sensitive collection will not find values that differ only in case." | ⚠️ Needs revision | ✅ Good | Name is long but at least describes the condition rather than encoding an operator. Still, cleaner name available. |
| PRE0097 | `CaseInsensitiveFieldRequiresTildeStartsWith` | Error | "'{0}' is declared ~string (case-insensitive). Use ~startsWith instead of startsWith to avoid treating values as having different prefixes." | ⚠️ Needs revision | ✅ Good | Same family smell. Message is clear. |
| PRE0098 | `CaseInsensitiveFieldRequiresTildeEndsWith` | Error | "'{0}' is declared ~string (case-insensitive). Use ~endsWith instead of endsWith to avoid treating values as having different suffixes." | ⚠️ Needs revision | ✅ Good | Same family smell. |

---

### Group: Type / Collection Safety (PRE0063–PRE0065, PRE0099–PRE0106)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0063 | `UnguardedCollectionAccess` | Error | "'{0}' may be empty — guard with if {0}.count > 0 before accessing .{1}" | ⚠️ Needs revision | ✅ Good | Existing name uses `guard`, which is not Precept surface syntax. `CollectionAccessWithoutWhen` matches the spec's `when` clause vocabulary. |
| PRE0064 | `UnguardedCollectionMutation` | Error | "'{0}' may be empty — guard with 'when {0}.count > 0' before {1}, or apply 'notempty' to the collection field" | ⚠️ Needs revision | ✅ Good | Same issue. `CollectionMutationWithoutWhen` is the Precept-native form. |
| PRE0065 | `NonOrderableCollectionExtreme` | Error | "'.{1}' requires an orderable element type — '{0}' elements have no natural ordering" | ✅ Good | ✅ Good | |
| PRE0099 | `KeyPresenceSafety` | Error | "'{0}' may not contain key '{1}' — add a 'when {0} contains {1}' guard before this access" | ⚠️ Needs revision | ✅ Good | `KeyAccessWithoutWhen` uses the DSL's `when` vocabulary and names the missing condition directly. |
| PRE0100 | `IndexBoundsGuard` | Error | "'{0}' access at index '{1}' is not bounds-checked — add a 'when {0}.count > {1}' guard" | ⚠️ Needs revision | ✅ Good | `IndexAccessWithoutWhen` keeps the family consistent and replaces the remedy-oriented `Guard` suffix with the actual missing `when` clause. |
| PRE0101 | `KeyUniquenessGuard` | Error | "Key '{1}' may already exist in '{0}' — add a 'when not ({0} contains {1})' guard before appending" | ⚠️ Needs revision | ✅ Good | `DuplicateKeyAddWithoutWhen` keeps the same pattern for uniqueness checks. |
| PRE0102 | `InvalidQuantifierTarget` | Error | "'{0}' is not a collection field — quantifiers (each/any/no) require a collection field" | ✅ Good | ✅ Good | |
| PRE0104 | `MissingOrderingKey` | Error | "'{0}' requires a 'by P' ordering key — use '{0} of T by P'" | ✅ Good | ⚠️ Needs revision | `P` and `T` are unexplained placeholder variables in the repair hint. |
| PRE0105 | `CollectionInnerTypeError` | Error | "Expected a {0} value, but '{1}' holds elements of type {2}" | ⚠️ Needs revision | ✅ Good | "InnerType" is compiler jargon. `CollectionElementTypeMismatch` matches the existing catalog style. |
| PRE0106 | `QuantifierPredicateNotBoolean` | Error | "Quantifier predicate must be a boolean expression, but this resolves to {0}" | ✅ Good | ✅ Good | |

---

### Group: Type / Temporal (PRE0055–PRE0062, PRE0117–PRE0118)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0055 | `InvalidDateValue` | Error | "Invalid date: {0} does not exist" | ✅ Good | ⚠️ Needs revision | "Invalid date: {0} does not exist" — terse. Subject isn't named (what date?), no repair hint in the message body. |
| PRE0056 | `InvalidDateFormat` | Error | "Dates must be written as YYYY-MM-DD. Use '{0}'" | ✅ Good | ✅ Good | Suggests the corrected value. |
| PRE0057 | `InvalidTimeValue` | Error | "Invalid time: {0} must be 0–23 for hours, 0–59 for minutes and seconds" | ✅ Good | ⚠️ Needs revision | "Invalid time: {0} must be 0-23 for hours" — if `{0}` is the full time value, the sentence is grammatically awkward. |
| PRE0058 | `InvalidInstantFormat` | Error | "Instants must end with Z to indicate UTC. Use '{0}Z'" | ✅ Good | ✅ Good | |
| PRE0059 | `InvalidTimezoneId` | Error | "'{0}' is not a recognized timezone — use canonical IANA form like 'America/New_York'" | ✅ Good | ✅ Good | |
| PRE0060 | `UnqualifiedPeriodArithmetic` | Error | "Period field '{0}' may contain {1} components — use period of '{2}' to constrain it" | ✅ Good | ⚠️ Needs revision | `{1}` is opaque — "may contain date and time components" would need `{1}` to render something like "mixed" or "date and time", but this is unclear without inspecting the emitter. |
| PRE0061 | `MissingTemporalUnit` | Error | "A bare number doesn't specify a unit. Use '{0}' + '{1}' to add {1}" | ✅ Good | ⚠️ Needs revision | Casual "doesn't" style. The arithmetic form `'{0}' + '{1}'` is confusing — it looks like string concatenation. |
| PRE0062 | `FractionalUnitValue` | Error | "Unit values must be whole numbers. Use smaller units for fractions: '{0}'" | ✅ Good | ✅ Good | |
| PRE0117 | `InvalidTemporalDimensionString` | Error | "'{0}' is not a recognized temporal dimension — use 'date' or 'time'" | ✅ Good | ✅ Good | |
| PRE0118 | `InvalidTemporalUnitString` | Error | "'{0}' is not a recognized temporal unit — use 'days', 'months', 'years', 'hours', 'minutes', or 'seconds'" | ✅ Good | ✅ Good | |

---

### Group: Type / Business Domain (PRE0067–PRE0077)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0067 | `MaxPlacesExceeded` | Error | "Value has {0} decimal places, but field '{1}' allows at most {2}" | ✅ Good | ✅ Good | |
| PRE0068 | `QualifierMismatch` | Error | "Value does not match the '{0}' qualifier on field '{1}'" | ⚠️ Needs revision | ⚠️ Needs revision | Name: "Qualifier" is a type-system term; passable since authors do write `in 'USD'` and `of 'kg'`, but the name gives no indication of currency vs unit. Message: doesn't say what the value's qualifier IS, only that it doesn't match. |
| PRE0069 | `DimensionCategoryMismatch` | Error | "Dimension '{0}' does not match the declared category '{1}' on field '{2}'" | ✅ Good | ✅ Good | |
| PRE0070 | `CrossCurrencyArithmetic` | Error | "Cannot combine '{0}' ({1}) with '{2}' ({3}) — different currencies" | ✅ Good | ✅ Good | |
| PRE0071 | `CrossDimensionArithmetic` | Error | "Cannot combine '{0}' ({1}) with '{2}' ({3}) — incompatible dimensions" | ✅ Good | ✅ Good | |
| PRE0072 | `DenominatorUnitMismatch` | Error | "Denominator unit '{0}' does not match operand unit '{1}'" | ✅ Good | ✅ Good | |
| PRE0073 | `DurationDenominatorMismatch` | Error | "Duration cannot cancel '{0}' denominator — days, weeks, months, and years have variable length" | ✅ Good | ✅ Good | |
| PRE0074 | `CompoundPeriodDenominator` | Error | "Compound period '{0}' cannot cancel single-unit denominator '{1}'" | ✅ Good | ✅ Good | |
| PRE0075 | `InvalidUnitString` | Error | "'{0}' is not a valid unit" | ✅ Good | ⚠️ Needs revision | Message is four words. No hint of valid alternatives. |
| PRE0076 | `InvalidCurrencyCode` | Error | "'{0}' is not a recognized ISO 4217 currency code" | ✅ Good | ✅ Good | References the standard by name — good. |
| PRE0077 | `InvalidDimensionString` | Error | "'{0}' is not a recognized dimension" | ✅ Good | ⚠️ Needs revision | Same terse pattern as PRE0075. No hint of valid values. |

---

### Group: Type / Interpolated Typed Constants (PRE0121–PRE0124)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0121 | `InvalidInterpolatedTypedConstantForm` | Error | "'{0}' doesn't match a recognized pattern for this type — check the expected format (e.g. '{amount} USD' for money)" | ⚠️ Needs revision | ✅ Good | Name is 40 characters of internal vocabulary. "InterpolatedTypedConstantForm" is not author language. Message is very good — concrete example. |
| PRE0122 | `InterpolationNotSupportedForType` | Error | "Interpolation is not supported for '{0}' typed constants. {1}" | ✅ Good | ⚠️ Needs revision | Message ends with ". {1}" — if {1} is empty, the trailing period-space creates an awkward sentence ending. Structure should be conditional. |
| PRE0123 | `InterpolatedTypedConstantHoleTypeMismatch` | Error | "'{{{1}}}' expects a {2} value, but the expression is {0} — use a compatible field or literal" | ⚠️ Needs revision | ✅ Good | Name: "Hole" is an interpolation-theory internal term not familiar to DSL authors. "Slot" is more accessible. Name is also 44 characters. |
| PRE0124 | `DimensionMismatchInUnitSlot` | Error | "'{0}' measures {1}, but this field requires {2} — use a unit from the '{2}' dimension" | ⚠️ Needs revision | ✅ Good | "UnitSlot" is an internal interpolation term. "InterpolatedUnit" would be more accessible. |

---

### Group: Runtime / Value Safety (PRE0078–PRE0079)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0078 | `NumericOverflow` | Error | "Numeric computation exceeded the representable range on field '{0}'" | ✅ Good | ✅ Good | Also emitted by ProofEngine for interval containment. |
| PRE0079 | `OutOfRange` | Error | "Value is outside the declared bounds for field '{0}'" | ✅ Good | ✅ Good | |

---

### Group: Graph (PRE0080–PRE0081, PRE0108–PRE0111, PRE0119, PRE0125–PRE0126)

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0080 | `UnreachableState` | Warning | "State '{0}' is unreachable from initial state '{1}'" | ✅ Good | ✅ Good | |
| PRE0081 | `UnhandledEvent` | Warning | "Event '{0}' has no transition rows in any state — it can never be fired successfully" | ✅ Good | ✅ Good | |
| PRE0108 | `DeadEndState` | Warning | "State '{0}' has no path to any terminal state — entities that enter it can never complete their lifecycle" | ✅ Good | ✅ Good | "Dead end" is natural language. |
| PRE0109 | `TerminalStateHasOutgoingEdges` | Error | "Terminal state '{0}' has outgoing transitions — terminal states must not transition to other states" | ✅ Good | ✅ Good | "Edges" in name is slightly graph-theory; "outgoing transitions" in the message is correct author vocabulary. |
| PRE0110 | `IrreversibleStateHasBackEdge` | Error | "Irreversible state '{0}' has a transition returning to an earlier state — irreversible states must not have back-edges" | ⚠️ Needs revision | ⚠️ Needs revision | Name: "BackEdge" is graph theory jargon. Message: "back-edges" leaks the same jargon into user-visible text. |
| PRE0111 | `RequiredStateDoesNotDominateTerminal` | Warning | "Required state '{0}' does not dominate any terminal state — some execution paths can bypass it entirely" | 🔴 Rename | 🔴 Rewrite | "Dominate" is graph-analysis jargon in both name and message. This is the most egregious jargon violation in the catalog. A DSL author writes `state Review required` — they have no concept of "domination". |
| PRE0119 | `StructuralSinkState` | Warning | "State '{0}' has no outgoing transitions and is not marked 'terminal'" | ⚠️ Needs revision | ✅ Good | "Sink" is graph theory jargon. "Structural" adds nothing author-facing. Message is actually very clear. |
| PRE0125 | `AlwaysRejecting` | Warning | "Event '{0}' always rejects on every path — if this event is not applicable in any state, remove all rows for it" | ✅ Good | ✅ Good | |
| PRE0126 | `StateAlwaysRejects` | Warning | "Event '{0}' always rejects from '{1}' — if this event has no meaning in '{1}', remove the row; no row means 'not applicable here'" | ✅ Good | ✅ Good | |

---

### Group: Proof (PRE0082–PRE0084, PRE0112–PRE0116)

> **Systemic name issue:** Five of the eight proof diagnostics share the `Unproved*` prefix, which is proof-engine vocabulary. A DSL author doesn't think in terms of "proof obligations" — they think in terms of "the compiler can't tell whether X is safe". The `Unproved*` prefix also makes the names feel like the author failed a formal-methods exam rather than made a natural authoring mistake.

| PRE# | Code Name | Severity | Current Message | Name Verdict | Message Verdict | Notes |
|------|-----------|----------|-----------------|--------------|-----------------|-------|
| PRE0082 | `UnsatisfiableGuard` | Warning | "Guard '{0}' on event '{1}' is unsatisfiable under the declared constraints{2} — this row can never fire" | ✅ Good | ✅ Good | "Unsatisfiable" is borderline; "this row can never fire" rescues the message. Acceptable. |
| PRE0083 | `DivisionByZero` | Error | "Division is unsafe: '{0}' can be zero{1}" | ✅ Good | ✅ Good | |
| PRE0084 | `SqrtOfNegative` | Error | "'{0}' can be negative{1}, so sqrt(...) is unsafe" | ✅ Good | ✅ Good | |
| PRE0112 | `UnprovedModifierRequirement` | Error | "Cannot prove that '{0}' satisfies the required modifier '{1}'{2}" | ⚠️ Needs revision | ⚠️ Needs revision | Name: "Unproved" prefix is proof-engine language. Message: "Cannot prove that…" mirrors it. |
| PRE0113 | `UnprovedDimensionRequirement` | Error | "'{0}' must declare \`of '{1}'\` before it can be used here{2}" | ⚠️ Needs revision | ✅ Good | Name has the `Unproved` prefix smell. Message is actually very clear and actionable — field-first, specific repair. |
| PRE0114 | `UnprovedQualifierCompatibility` | Error | "Cannot prove {2} qualifier compatibility between '{0}' [{2}: {4}] and '{1}' [{2}: {5}]{3}" | ⚠️ Needs revision | ⚠️ Needs revision | Name: `Unproved` prefix. Message: dense bracket notation `[{2}: {4}]` is hard to read. Rendered example would look like `[Currency: unknown]`. |
| PRE0115 | `UnsatisfiableInitialState` | Error | "Initial state '{0}' is unsatisfiable: {1}" | ⚠️ Needs revision | ⚠️ Needs revision | Name: "Unsatisfiable" is proof jargon. Message: "is unsatisfiable: {1}" — "unsatisfiable" is the same jargon in user-visible text, and ": {1}" is a raw continuation. |
| PRE0116 | `UnprovedPresenceRequirement` | Error | "Cannot prove that '{0}' is present{1} — guard with 'when {0} is set', initialize it earlier, or make it required" | ⚠️ Needs revision | ✅ Good | Name: `Unproved` prefix. Message: "Cannot prove" leaks into user text, but the three-part repair at the end is excellent. |

---

## High-Priority Findings

These are the diagnostics most likely to surface in everyday authoring, where the current name or message will cause friction.

> **Implementation note:** Frank's follow-up review now gives this work a three-tier execution order: Tier 1 = urgent user-facing jargon/message fixes, Tier 2 = family coherence passes, Tier 3 = cosmetic/defer. The list below concentrates on the highest-friction items that most strongly drive that ordering.

1. **PRE0111 `RequiredStateDoesNotDominateTerminal`** — 🔴 Both name and message use graph-theory jargon (`dominate`) that a DSL author has never written or read in the language. This fires on valid state-machine designs with a bypass path, which are genuinely common. Every author who hits this will need to look up what "dominate" means.

2. **PRE0110 `IrreversibleStateHasBackEdge`** — ⚠️ "BackEdge" and "back-edges" appear in both name and message. These are correct graph terms but unintelligible to a DSL author. Replace with "backward transition" or "transition back to a previous state."

3. **PRE0112–PRE0116 (proof-family names)** — ⚠️ The shared `Unproved*` / `Unsatisfiable*` vocabulary is proof-engine language. This family should move to condition-first names (`ModifierNotGuaranteed`, `DimensionQualifierMissing`, `QualifiersMayBeIncompatible`, `InitialStateConstraintUnsatisfied`, `FieldMayBeAbsent`) so the author sees the state of the definition, not the compiler's proof attempt.

4. **PRE0066/PRE0095/PRE0097/PRE0098 (CI enforcement family)** — ⚠️ Names encode operator tokens as name fragments (`TildeEquals`, `TildeNotEquals`, etc.). The names describe the fix, not the failure condition. Frank's shorter `CaseMismatch*` family is the right normalization because it fixes the jargon without creating 40+ character members.

5. **PRE0019 `NullInNonNullableContext`** — ✅ Emission-site audit complete. There are no live emitters in `src/Precept/`; the code exists only in catalog metadata (`DiagnosticCode.cs`, `Diagnostics.cs`, `FaultCode.cs`). The optional-field-specific rename remains wrong, but the block is now resolved: the correct future-facing name is `ValueMayBeAbsent`, and it should land with the deferred precision-upgrade wiring rather than as an optional-field-only rename.

6. **PRE0063/PRE0064/PRE0099/PRE0100/PRE0101 (collection `when`-clause names)** — ⚠️ `UnguardedCollectionAccess`, `UnguardedCollectionMutation`, `KeyPresenceSafety`, `IndexBoundsGuard`, and `KeyUniquenessGuard` all lean on non-native `guard` vocabulary or remedy-oriented naming. This family should use the author-facing `when` clause instead.

7. **PRE0009 `ExpectedToken`** — ⚠️ The most frequently triggered parse error has the most generic message: "Expected {0} here, but found '{1}'". The word "here" contributes nothing. High volume means this message appears constantly.

8. **PRE0068 `QualifierMismatch`** — ⚠️ Message omits what the value's actual qualifier IS. Author sees "value does not match the 'USD' qualifier" but doesn't see what qualifier the value *had*.

9. **PRE0075 `InvalidUnitString`** — ⚠️ Message is four words with no example of what's valid. Contrast with PRE0076 (`InvalidCurrencyCode`) which references ISO 4217 — PRE0075 should reference the unit catalog similarly.

10. **PRE0085 `NonChoiceAssignedToChoice`** — ⚠️ "NonChoice" is not an author concept. Message says "choice-compatible" which is also internal. Authors see this when setting a choice field — they need to know what values ARE allowed, not just that their value wasn't "choice-compatible".

11. **PRE0115 `UnsatisfiableInitialState`** — ⚠️ Fires when an author writes an `in State ensure` that can't be satisfied by defaults. "Unsatisfiable" is proof jargon in both the name and message.

12. **PRE0054 `DefaultForwardReference`** — ⚠️ Fires on computed field expressions (`<-`) that reference later-declared fields. "Default" is misleading — this has nothing to do with `default` clauses.

---

## Proposed Revisions

### Parse Group

**PRE0004 `UnterminatedInterpolation`** — Message only
- **Current:** "The { } section is not closed — add a closing } on the same line"
- **Proposed:** "An interpolation started with '{' is missing its closing '}' — add '}' to complete it on this line"
- **Rationale:** "{ } section" is vague. Naming it as an interpolation, started by `{`, aligns with how the rest of the lex group names its constructs.

**PRE0015 `PreEventGuardNotAllowed`** — Name only
- **Current:** `PreEventGuardNotAllowed`
- **Proposed:** `TransitionGuardMustFollowEvent`
- **Rationale:** "PreEvent" is a parser-internal structural label. A DSL author writes `from State on Event when Guard` — the concept they violated is that the guard must come after the event name.

**PRE0127 `AssignmentInExpressionContext`** — Name only
- **Current:** `AssignmentInExpressionContext`
- **Proposed:** `AssignmentUsedAsComparison`
- **Rationale:** "ExpressionContext" is compiler terminology. The author-visible failure is that they wrote `=` when they meant `==`.

---

### Type Group

**PRE0019 `NullInNonNullableContext`** — Name + Message
- **Current name:** `NullInNonNullableContext`
- **Elaine proposal:** `OptionalFieldUsedWithoutWhen`
- **Current message:** "'{0}' requires a value and cannot be empty here"
- **Audit outcome:** No live emission sites exist in `src/Precept/`. Repo-wide search found only the enum member, catalog metadata, and fault mapping; neither `TypeChecker` nor `ProofEngine` emits PRE0019 today.
- **Final recommendation:** Rename to `ValueMayBeAbsent`
- **Final message:** "'{0}' may be unset — this position requires a value"
- **Rationale:** `NonNullableContext` is C# jargon, and Elaine's optional-field-specific rename is factually narrower than the deferred precision-upgrade surface. `ValueMayBeAbsent` is Precept-native, condition-first, and broad enough for any maybe-absent expression that flows into a required position.

**PRE0021 `FunctionArityMismatch`** — Name only
- **Current:** `FunctionArityMismatch`
- **Proposed:** `WrongNumberOfArguments`
- **Rationale:** "Arity" is a mathematical term. The message ("takes {1} inputs, but {2} were provided") is clear; the name should match its plain-language quality.

**PRE0022 `FunctionArgConstraintViolation`** — Name + Message
- **Current name:** `FunctionArgConstraintViolation`
- **Proposed name:** `FunctionArgumentInvalid`
- **Current message:** "Argument {0} to '{1}' is not valid — {2}"
- **Proposed message:** "Argument {0} of '{1}' is invalid: {2}"
- **Rationale:** "ArgConstraintViolation" is verbose and internal. The message uses a bare ordinal number for `{0}` — the preposition "of" makes the relationship clearer than "to", and the colon separating the reason is more conventional.

**PRE0034 `InvalidModifierBounds`** — Message only
- **Current:** "{0} ({1}) cannot exceed {2} ({3})"
- **Proposed:** "Lower bound '{0}' ({1}) cannot be greater than upper bound '{2}' ({3})"
- **Rationale:** The four bare slots give no indication of what each pair represents. Authors need to see "lower bound" and "upper bound" explicitly.

**PRE0041 `WritableOnEventArg`** — Name only
- **Current:** `WritableOnEventArg`
- **Proposed:** `EventArgCannotBeWritable`
- **Rationale:** Subject-first pattern. The name should start with what the subject is (an event argument) and state the condition (cannot be writable).

**PRE0044 `ListLiteralOutsideDefault`** — Message only
- **Current:** "List values can only appear in default clauses"
- **Proposed:** "A list literal like [\"a\", \"b\"] can only appear in a field's 'default' clause, not in expressions or rules"
- **Rationale:** Current message doesn't say what was found. Including "like [\"a\", \"b\"]" mirrors the author's actual syntax.

**PRE0047 `CollectionOperationOnScalar`** — Message only
- **Current:** "'{0}' requires a collection, but '{2}' is a single value — change '{2}' to a set, list, or queue"
- **Proposed:** "'{0}' is a collection action but '{1}' is a single-value field — change '{1}' to a set, list, or queue to use collection actions"
- **Rationale:** Skipping `{1}` in the original format is unusual. Renumbering consistently with `{1}` as the field name removes the gap.

**PRE0048 `ScalarOperationOnCollection`** — Message only
- **Current:** "'{0}' cannot be used with collection field '{1}'"
- **Proposed:** "The '{0}' action cannot be used with collection field '{1}' — use 'add', 'remove', or other collection actions instead"
- **Rationale:** Adding "The…action" and the repair hint makes the message more readable and actionable.

**PRE0050 `EventArgOutOfScope`** — Message only
- **Current:** "Event '{0}' arguments are not accessible here"
- **Proposed:** "Event '{0}' arguments are only in scope inside that event's transition body and event ensures — they cannot be used here"
- **Rationale:** "not accessible here" gives no scope information. The author needs to know WHERE they can be used.

**PRE0051 `InvalidInterpolationCoercion`** — Name only
- **Current:** `InvalidInterpolationCoercion`
- **Proposed:** `NonTextTypeInStringInterpolation`
- **Rationale:** "Coercion" is type-system jargon. The condition is that a non-text value was put into a string interpolation.

**PRE0052 `UnresolvedTypedConstant`** — Name only
- **Current:** `UnresolvedTypedConstant`
- **Proposed:** `UnrecognizedTypedConstant`
- **Rationale:** "Unresolved" is compiler jargon meaning "type could not be determined". "Unrecognized" is more natural and distinguishes this clearly from PRE0091 `AmbiguousTypedConstant` (where the type is ambiguous between two candidates, here it matches none).

**PRE0054 `DefaultForwardReference`** — Name only
- **Current:** `DefaultForwardReference`
- **Proposed:** `FieldExpressionForwardReference`
- **Rationale:** This fires on computed field expressions (`<- expr`) that reference a later-declared field. "Default" in the name is misleading — it has nothing to do with `default` clauses. "FieldExpression" correctly scopes the failure.

**PRE0055 `InvalidDateValue`** — Message only
- **Current:** "Invalid date: {0} does not exist"
- **Proposed:** "'{0}' is not a valid calendar date — check that the day, month, and year form a date that exists"
- **Rationale:** "Invalid date: {0} does not exist" is terse and grammatically awkward when {0} is the date string. The proposed form names the value as the subject.

**PRE0057 `InvalidTimeValue`** — Message only
- **Current:** "Invalid time: {0} must be 0–23 for hours, 0–59 for minutes and seconds"
- **Proposed:** "'{0}' is not a valid time — hours must be 0–23 and minutes or seconds must be 0–59"
- **Rationale:** Current message reads "{0} must be 0–23" which implies {0} is an hour value, but {0} is probably the full time string. Quoting the value as a subject and separating the constraints reads naturally.

**PRE0060 `UnqualifiedPeriodArithmetic`** — Message only
- **Current:** "Period field '{0}' may contain {1} components — use period of '{2}' to constrain it"
- **Proposed:** "Period field '{0}' has no dimension qualifier and may mix date and time components — add 'of {2}' to constrain it to {2} arithmetic"
- **Rationale:** `{1}` ("may contain {1} components") is opaque. Spelling out "date and time" inline removes the mystery slot.

**PRE0061 `MissingTemporalUnit`** — Message only
- **Current:** "A bare number doesn't specify a unit. Use '{0}' + '{1}' to add {1}"
- **Proposed:** "A number without a unit is not a valid temporal value — write '{0} {1}' to specify {1}"
- **Rationale:** "doesn't" is casual. `'{0}' + '{1}'` looks like string concatenation. The proposed form uses a space between value and unit, matching how authors write temporal constants.

**PRE0068 `QualifierMismatch`** — Name + Message
- **Current name:** `QualifierMismatch`
- **Proposed name:** `IncompatibleFieldQualifier`
- **Current message:** "Value does not match the '{0}' qualifier on field '{1}'"
- **Proposed message:** "This value has a different {0} qualifier than field '{1}' requires — they must match"
- **Rationale:** The current message omits what the value's actual qualifier is, making the error harder to self-diagnose. The proposed message explicitly calls out the mismatch as a qualifier axis (`{0}` = currency/unit) and names the field.

**PRE0075 `InvalidUnitString`** — Message only
- **Current:** "'{0}' is not a valid unit"
- **Proposed:** "'{0}' is not a recognized unit identifier — use a catalog unit such as 'kg', 'miles', or 'liters'"
- **Rationale:** Four-word message gives no examples. PRE0076 references ISO 4217; PRE0075 should reference the unit catalog with examples.

**PRE0077 `InvalidDimensionString`** — Message only
- **Current:** "'{0}' is not a recognized dimension"
- **Proposed:** "'{0}' is not a recognized dimension name — use a catalog dimension such as 'length', 'mass', or 'volume'"
- **Rationale:** Same terse pattern as PRE0075. Examples are the repair.

**PRE0085 `NonChoiceAssignedToChoice`** — Name + Message
- **Current name:** `NonChoiceAssignedToChoice`
- **Proposed name:** `ValueNotInChoiceSet`
- **Current message:** "Field '{0}' is a choice type — only choice-compatible values can be assigned to it"
- **Proposed message:** "Field '{0}' is a choice field — only its declared values or an event argument with a compatible choice type can be assigned to it"
- **Rationale:** "NonChoice" is not author vocabulary. "Choice-compatible" is internal jargon. The repair hint should tell the author what IS valid: declared values or a compatible-choice arg.

**PRE0088 `ChoiceElementTypeMismatch`** — Message only
- **Current:** "Expected a {0} literal — this choice is declared as 'choice of {0}'"
- **Proposed:** "This choice is declared as 'choice of {0}' — all values must be {0} literals"
- **Rationale:** Moving the declaration to the front makes it the grounding fact, and removes the repetitive "Expected a {0} literal" which just repeats {0}.

**PRE0089 `ChoiceRankConflict`** — Name only
- **Current:** `ChoiceRankConflict`
- **Proposed:** `ChoiceValueOrderMismatch`
- **Rationale:** "Rank" is an internal ordering concept. Authors don't think about "rank" in choice types — they wrote values in a particular order. "Order" is the natural term.

---

### CI Enforcement Family (PRE0066, PRE0095–PRE0098)

These five names share a systemic problem: they encode operator tokens (`TildeEquals`, `TildeNotEquals`) as name fragments instead of describing what went wrong. Frank's shortened `CaseMismatch*` family solves the jargon problem without creating overlong enum members.

**Proposed renamed family:**
| Old name | Proposed name |
|----------|---------------|
| `CaseInsensitiveFieldRequiresTildeEquals` (PRE0066) | `CaseMismatchOnEquality` |
| `CaseInsensitiveFieldRequiresTildeNotEquals` (PRE0095) | `CaseMismatchOnInequality` |
| `CaseInsensitiveValueInCaseSensitiveContains` (PRE0096) | `CaseMismatchOnContains` |
| `CaseInsensitiveFieldRequiresTildeStartsWith` (PRE0097) | `CaseMismatchOnStartsWith` |
| `CaseInsensitiveFieldRequiresTildeEndsWith` (PRE0098) | `CaseMismatchOnEndsWith` |

**Message revision — PRE0095 only:**
- **Current:** "'{0}' is declared ~string (case-insensitive). Use !~ instead of != (!~ returns true when values are not equal under case-insensitive comparison)."
- **Proposed:** "'{0}' is ~string (case-insensitive). Use '!~' instead of '!=' for case-insensitive inequality."
- **Rationale:** The parenthetical in the current message redefines `!~` redundantly — the author using it knows what it means. Trim to match the concise style of PRE0066's message.

---

### Collection Safety `when`-Clause Names (PRE0063, PRE0064, PRE0099–PRE0101)

The language spec's surface term is the `when` clause (`When` → `when` → "Guard clause"). DSL authors write `when`; they do not write `guard`. This family should therefore name the missing `when` clause directly instead of using `Unguarded*`, `*Safety`, or `*Guard`.

| Old name | Proposed name |
|----------|---------------|
| `UnguardedCollectionAccess` (PRE0063) | `CollectionAccessWithoutWhen` |
| `UnguardedCollectionMutation` (PRE0064) | `CollectionMutationWithoutWhen` |
| `KeyPresenceSafety` (PRE0099) | `KeyAccessWithoutWhen` |
| `IndexBoundsGuard` (PRE0100) | `IndexAccessWithoutWhen` |
| `KeyUniquenessGuard` (PRE0101) | `DuplicateKeyAddWithoutWhen` |

**PRE0104 `MissingOrderingKey`** — Message only
- **Current:** "'{0}' requires a 'by P' ordering key — use '{0} of T by P'"
- **Proposed:** "'{0}' requires a 'by' ordering key — use 'queue of string by integer' (or the appropriate element and key types)"
- **Rationale:** `P` and `T` are unexplained variables. Replacing them with a concrete example like `queue of string by integer` is more immediately useful.

**PRE0105 `CollectionInnerTypeError`** — Name only
- **Current:** `CollectionInnerTypeError`
- **Proposed:** `CollectionElementTypeMismatch`
- **Rationale:** "InnerType" is compiler jargon. "ElementType" matches how Precept DSL describes collection contents (`set of string`, `list of number`).

---

### Graph Group

**PRE0110 `IrreversibleStateHasBackEdge`** — Name + Message
- **Current name:** `IrreversibleStateHasBackEdge`
- **Proposed name:** `IrreversibleStateHasBackwardTransition`
- **Current message:** "Irreversible state '{0}' has a transition returning to an earlier state — irreversible states must not have back-edges"
- **Proposed message:** "Irreversible state '{0}' has a transition returning to a previous state — irreversible states can only move forward in the lifecycle"
- **Rationale:** "BackEdge" and "back-edges" are graph-theory terminology. Authors write states and transitions; they would understand "backward transition" immediately.

**PRE0111 `RequiredStateDoesNotDominateTerminal`** — 🔴 Name + Message
- **Current name:** `RequiredStateDoesNotDominateTerminal`
- **Proposed name:** `RequiredStateCanBeBypassed`
- **Current message:** "Required state '{0}' does not dominate any terminal state — some execution paths can bypass it entirely"
- **Proposed message:** "Required state '{0}' can be bypassed — there are paths to a terminal state that skip it entirely. Mark it 'required' only if every lifecycle path must pass through it."
- **Rationale:** "Dominate" is a precise graph-reachability term that no Precept DSL author will recognize. The proposed name and message replace jargon with plain lifecycle language. The extra sentence anchors when `required` is appropriate.

**PRE0119 `StructuralSinkState`** — Name only
- **Current:** `StructuralSinkState`
- **Proposed:** `StateWithNoWayOut`
- **Rationale:** "Sink" is a graph-theory node classification. The message ("has no outgoing transitions and is not marked 'terminal'") is very clear — the name should match it. "StateWithNoWayOut" is colloquial but immediately understandable. An alternative: `UnterminalStateWithNoTransitions`, though that's long. A shorter option keeping catalog style: `NonTerminalLeafState`.

---

### Proof Group

**Systemic revision — condition-first proof names (PRE0112–PRE0116):**

The `Unproved*` / `Unsatisfiable*` prefix is proof-engine language. Frank is right to reject `CannotVerify*` as still compiler-centered. These should use condition-first names that describe the author's definition:

| Old name | Proposed name |
|----------|---------------|
| `UnprovedModifierRequirement` (PRE0112) | `ModifierNotGuaranteed` |
| `UnprovedDimensionRequirement` (PRE0113) | `DimensionQualifierMissing` |
| `UnprovedQualifierCompatibility` (PRE0114) | `QualifiersMayBeIncompatible` |
| `UnsatisfiableInitialState` (PRE0115) | `InitialStateConstraintUnsatisfied` |
| `UnprovedPresenceRequirement` (PRE0116) | `FieldMayBeAbsent` |

> These names tell the author what condition exists (`FieldMayBeAbsent`, `ModifierNotGuaranteed`) rather than narrating what the compiler could not prove.

**PRE0112 `UnprovedModifierRequirement`** — Message also
- **Current:** "Cannot prove that '{0}' satisfies the required modifier '{1}'{2}"
- **Proposed:** "'{0}' is used where the '{1}' modifier is required — declare '{0}' with '{1}', or restructure the expression so this requirement no longer applies{2}"
- **Rationale:** "Cannot prove that…satisfies" is proof-engine language. The proposed form puts the field as subject and leads with the repair.

**PRE0114 `UnprovedQualifierCompatibility`** — Message also
- **Current:** "Cannot prove {2} qualifier compatibility between '{0}' [{2}: {4}] and '{1}' [{2}: {5}]{3}"
- **Proposed:** "'{0}' and '{1}' may have incompatible {2} qualifiers — '{0}' resolves to {4} and '{1}' resolves to {5}{3}"
- **Rationale:** "Cannot prove…compatibility" is proof jargon and the bracket notation `[Currency: USD]` is dense. The proposed form puts both operands as co-subjects and states their resolved values plainly.

**PRE0115 `UnsatisfiableInitialState`** — Message also
- **Current:** "Initial state '{0}' is unsatisfiable: {1}"
- **Proposed:** "The constraint on initial state '{0}' cannot be satisfied with the current field defaults: {1}"
- **Rationale:** "Is unsatisfiable" is proof jargon. "Cannot be satisfied with the current field defaults" is plain English that names the actual cause — defaults don't satisfy the constraint.

---

### Interpolated Typed Constants Group

**PRE0121 `InvalidInterpolatedTypedConstantForm`** — Name only
- **Current:** `InvalidInterpolatedTypedConstantForm`
- **Proposed:** `InvalidInterpolatedValuePattern`
- **Rationale:** 40-character name encodes internal "form" concept. "Pattern" is accessible and shorter.

**PRE0122 `InterpolationNotSupportedForType`** — Message only
- **Current:** "Interpolation is not supported for '{0}' typed constants. {1}"
- **Proposed:** "Interpolation is not supported for '{0}' typed constants{1} — write a complete literal or use arithmetic for dynamic values"
- **Rationale:** Trailing ". {1}" creates an awkward sentence boundary when `{1}` is non-empty and a dangling period when it's empty. Integrating the type-specific detail inline and adding a repair hint fixes both.

**PRE0123 `InterpolatedTypedConstantHoleTypeMismatch`** — Name only
- **Current:** `InterpolatedTypedConstantHoleTypeMismatch`
- **Proposed:** `InterpolatedConstantSlotTypeMismatch`
- **Rationale:** "Hole" is interpolation-theory terminology. "Slot" is more accessible (as in "a slot in the pattern") and saves 10 characters.

**PRE0124 `DimensionMismatchInUnitSlot`** — Name only
- **Current:** `DimensionMismatchInUnitSlot`
- **Proposed:** `DimensionMismatchInInterpolatedUnit`
- **Rationale:** "UnitSlot" is an interpolation-internal term. "InterpolatedUnit" scopes the failure to the interpolation context without requiring knowledge of the slot concept.

---

## Patterns & Systemic Issues

### Pattern 1 — Graph-theory jargon surfacing in user text
**Affected:** PRE0110 (`back-edge`), PRE0111 (`dominate`), PRE0119 (`sink`)
**Convention:** When a graph-analysis concept has a plain lifecycle equivalent, use the lifecycle term in both the name and the message. "Backward transition" for back-edge, "can be bypassed" for not-dominating. Reserve "sink", "dominate", and "back-edge" for internal comments and algorithm code only.

### Pattern 2 — `Unproved*` proof prefix leaking to user names
**Affected:** PRE0112–PRE0116
**Convention:** Proof diagnostic names should describe the condition in the author's definition, not the proof engine's internal failure. Replace `Unproved*` / `Unsatisfiable*` names with condition-first forms such as `ModifierNotGuaranteed`, `DimensionQualifierMissing`, `QualifiersMayBeIncompatible`, `InitialStateConstraintUnsatisfied`, and `FieldMayBeAbsent`. No `CannotVerify*`, `Unproved*`, or `FailedToProve*` prefixes.

### Pattern 3 — CI enforcement names encoding the fix, not the failure
**Affected:** PRE0066, PRE0095, PRE0097, PRE0098 (PRE0096 belongs to the same family even though it already describes a condition)
**Convention:** Diagnostic names should describe what WENT WRONG, not what the fix is. `CaseInsensitiveFieldRequiresTildeEquals` names the required fix. `CaseMismatchOnEquality` names the failure. Use shared `CaseMismatchOn*` names across the family and any future operator-enforcement diagnostics.

### Pattern 4 — Collection-safety names must use `when`-clause vocabulary
**Affected:** PRE0063 (`UnguardedCollectionAccess`), PRE0064 (`UnguardedCollectionMutation`), PRE0099 (`KeyPresenceSafety`), PRE0100 (`IndexBoundsGuard`), PRE0101 (`KeyUniquenessGuard`)
**Convention:** The language reference gives authors a `when` clause, not a `guard` keyword. This family should therefore use subject + `WithoutWhen` names (`CollectionAccessWithoutWhen`, `IndexAccessWithoutWhen`) so the condition is visible in Precept-native vocabulary. Avoid `Unguarded*`, `*Safety`, and `*Guard` names.

### Pattern 5 — Terse business-domain messages with no catalog examples
**Affected:** PRE0075 (`InvalidUnitString`), PRE0077 (`InvalidDimensionString`)
**Convention:** When a value is rejected because it's not in a catalog (units, dimensions, currencies, temporal units), include at least two example valid values in the message. PRE0076, PRE0118, and PRE0117 already do this — PRE0075 and PRE0077 should match.

### Pattern 6 — Trailing `. {1}` and opaque format slots
**Affected:** PRE0122 (trailing ". {1}"), PRE0060 (opaque `{1}` component count), PRE0010 (opaque trailing `{0}`)
**Convention:** Every `{n}` slot in a message template should be self-explanatory in context, or the slot content should be introduced with a label. Avoid trailing continuation slots (`. {1}`) that create awkward sentences when non-empty and dangling punctuation when empty.

### Pattern 7 — Naming convention holdovers from compiler-theory vocabulary
**Affected:** PRE0019 (`NullInNonNullableContext`), PRE0021 (`FunctionArityMismatch`), PRE0022 (`FunctionArgConstraintViolation`), PRE0051 (`InvalidInterpolationCoercion`), PRE0052 (`UnresolvedTypedConstant`), PRE0105 (`CollectionInnerTypeError`)
**Convention:** Precept diagnostic names should be readable by someone who has only read the Precept language reference, not compiler theory textbooks. If a term doesn't appear in the DSL spec or the language reference, it probably shouldn't appear in a diagnostic name. Check: "Would a DSL author encountering this name in the Problems panel understand what went wrong without looking it up?"

### Pattern 8 — Messages should stay AI-parseable as well as human-readable
**Affected:** PRE0116 already shows the target shape; PRE0010, PRE0060, and PRE0122 show the failure modes.
**Convention:** Prefer `Subject — Condition — Repair` structure. Put the quoted subject first, follow with the condition, and place any repair after an em dash. Avoid unlabeled continuation slots and sentence fragments that break structural parsing.

### Pattern 9 — Rename passes must update diagnostic metadata in the same edit
**Affected:** PRE0054 immediately; any future rename whose `FixHint`, `RecoverySteps`, `TriggerCondition`, or examples still carry the old vocabulary.
**Convention:** A diagnostic rename is not complete until the surrounding metadata is checked for the same language drift. No partial renames.

---

## Frank's Architectural Review

> Reviewed by Frank | 2026-05-13

### Overall Verdict

**APPROVED WITH CONDITIONS**

This is a strong piece of work. Elaine correctly identified the five systemic problem families, her source citations are accurate against the live codebase, and her pattern analysis is exactly the kind of structural thinking diagnostic UX needs. The conventions she proposes are architecturally sound and I'm adopting six of the seven as standards. Two conditions still block implementation: (1) the `CannotVerify*` prefix must be replaced with condition-first names, and (2) several proposed CI-enforcement renames are longer than the names they replace, which trades one smell for another. PRE0019 is no longer blocked: the emission-site audit found no live emitters in `src/Precept/`, and the correct future-facing rename is `ValueMayBeAbsent`.

### Accuracy

Elaine's source citations are correct. Every diagnostic name, message template, and severity she quotes matches the live `DiagnosticCode.cs` and `Diagnostics.cs` sources. Verified:

- Total count: 132 diagnostics — matches the enum.
- All message templates quoted are character-accurate against the `DiagnosticMeta` entries.
- Pipeline phase groupings (Lex, Parse, Type, Graph, Proof) match the enum's section comments.

**One staging error:** PRE0088 `ChoiceElementTypeMismatch` is listed under "Group: Type / Choice" in her review table, which is reasonable by domain, but the actual `DiagnosticStage` in the source is `Parse`, not `Type`. This matters for pipeline phase clarity — any rename discussion should acknowledge it fires during parsing, not type-checking.

**One FixHint propagation miss:** PRE0054 `DefaultForwardReference` — Elaine correctly flags that the *name* says "Default" when the diagnostic fires on computed expressions. But the `FixHint` in the source also says "the referenced field appears before the one with the default" — the word "default" appears in the FixHint too. The rename to `FieldExpressionForwardReference` must include a FixHint update in the same pass.

No missed diagnostics. All 132 are accounted for.

### Architectural Assessment

**Structurally safe renames:** The `DiagnosticCode` enum feeds `DiagnosticCatalog` through `Diagnostics.GetMeta()`, which uses `nameof()` to derive the string code. Renaming an enum member automatically propagates to:
- The `DiagnosticMeta.Code` string (via `nameof()`)
- The `DiagnosticCatalog` entries (derived from `Diagnostics.GetMeta()`)
- MCP tool output (thin wrapper over catalog)

What does NOT auto-propagate and must be manually updated:
- Test fixtures that assert `DiagnosticCode.OldName` — these will fail to compile, which is the correct safety net.
- The `ProofEngine.Diagnostics.cs` emission sites — already verified, these reference `DiagnosticCode.X` directly.
- Any language server code that switches on specific diagnostic codes.
- The Gate 1 allow-list in the diagnostic enforcement plan.

**Verdict:** Renames are structurally safe to execute. The compiler will catch stale references. No catalog coherence risk.

**Pipeline phase clarity:** All proposed renames preserve phase boundaries. Elaine's renames describe author-visible conditions, not pipeline internals, which is correct — the enum's section comments and `DiagnosticStage` field already encode the phase, so the name doesn't need to.

**Specific rename assessments:**

| Proposed Rename | Verdict | Notes |
|----------------|---------|-------|
| `PreEventGuardNotAllowed` → `TransitionGuardMustFollowEvent` | ✅ Adopt | Correct — names the author action. |
| `AssignmentInExpressionContext` → `AssignmentUsedAsComparison` | ✅ Adopt | Accurate — the message already says "use '==' for equality". |
| `NullInNonNullableContext` → `ValueMayBeAbsent` | ✅ Adopt (with wiring) | Emission-site audit found no live emitters in `src/Precept/`; PRE0019 is declaration/metadata-only today. Elaine's optional-field-specific rename stays rejected, but `ValueMayBeAbsent` is the correct condition-first name for the deferred precision-upgrade surface. |
| `FunctionArityMismatch` → `WrongNumberOfArguments` | ✅ Adopt | Plain language, no information loss. |
| `FunctionArgConstraintViolation` → `FunctionArgumentInvalid` | ✅ Adopt | Shorter, clearer. |
| `WritableOnEventArg` → `EventArgCannotBeWritable` | ✅ Adopt | Subject-first pattern is correct. |
| `InvalidInterpolationCoercion` → `NonTextTypeInStringInterpolation` | ✅ Adopt | Describes the condition accurately. |
| `UnresolvedTypedConstant` → `UnrecognizedTypedConstant` | ✅ Adopt | Good distinction from PRE0091 `Ambiguous`. |
| `DefaultForwardReference` → `FieldExpressionForwardReference` | ✅ Adopt | Must also update FixHint. |
| `NonChoiceAssignedToChoice` → `ValueNotInChoiceSet` | ✅ Adopt | Author vocabulary. |
| `ChoiceRankConflict` → `ChoiceValueOrderMismatch` | ✅ Adopt | "Order" is the right term. |
| `CollectionInnerTypeError` → `CollectionElementTypeMismatch` | ✅ Adopt | Matches `ChoiceElementTypeMismatch`. |
| `InvalidInterpolatedTypedConstantForm` → `InvalidInterpolatedValuePattern` | ✅ Adopt | Much shorter, still accurate. |
| `InterpolatedTypedConstantHoleTypeMismatch` → `InterpolatedConstantSlotTypeMismatch` | ✅ Adopt | "Slot" over "hole" is correct. |
| `DimensionMismatchInUnitSlot` → `DimensionMismatchInInterpolatedUnit` | ✅ Adopt | Scopes correctly. |
| `KeyPresenceSafety` → `UnguardedKeyAccess` | ✅ Adopt | Matches PRE0063/0064 pattern. |
| `IndexBoundsGuard` → `UnguardedIndexAccess` | ✅ Adopt | Same. |
| `KeyUniquenessGuard` → `UnguardedDuplicateKeyAdd` | ✅ Adopt | Same. |
| `IrreversibleStateHasBackEdge` → `IrreversibleStateHasBackwardTransition` | ✅ Adopt | Author language. |
| `RequiredStateDoesNotDominateTerminal` → `RequiredStateCanBeBypassed` | ✅ Adopt — highest priority | The most egregious jargon in the catalog. |
| `StructuralSinkState` → `StateWithNoWayOut` | ⚠️ Modify | Too colloquial for a `DiagnosticCode` enum member. Counter-proposal: `NonTerminalDeadEnd` — short, accurate, uses the lifecycle term "dead end" that PRE0108 already established as precedent. |
| `QualifierMismatch` → `IncompatibleFieldQualifier` | ✅ Adopt | More specific. |
| CI enforcement family (4 renames) | ⚠️ Modify | See disputed conventions below. |
| `Unproved*` family (5 renames) | ⛔ Reject prefix — adopt alternatives | See disputed conventions below. |

### Agreed Conventions

I adopt the following six of Elaine's seven systemic patterns as architectural standards, effective immediately:

1. **Pattern 1 — Graph-theory jargon must use lifecycle equivalents.** Adopted as stated. "Backward transition" for back-edge, "can be bypassed" for not-dominating, "dead end" for sink. Graph terms are for algorithm comments only.

2. **Pattern 3 — Diagnostic names describe the failure, not the fix.** Adopted as stated. `CaseInsensitiveFieldRequiresTildeEquals` encodes the fix. The name must encode what went wrong.

3. **Pattern 4 — Collection-safety diagnostics follow `Unguarded*` convention.** Adopted. PRE0063/0064 are the template. PRE0099–0101 must match.

4. **Pattern 5 — Catalog-rejected values include example valid values in the message.** Adopted. Two or more examples. PRE0075, PRE0077 must match PRE0076/PRE0117/PRE0118 style.

5. **Pattern 6 — Format slots must be self-explanatory or labeled.** Adopted. No trailing `. {n}` patterns. No skipped slot indices. Every `{n}` must be comprehensible from the surrounding sentence without knowing the call site.

6. **Pattern 7 — Names use DSL-author vocabulary, not compiler-theory vocabulary.** Adopted. The litmus test: "Would a DSL author encountering this name in the Problems panel understand what went wrong without looking it up?" If not, rename.

### Disputed or Modified Conventions

**Pattern 2 — `Unproved*` prefix:** I agree the `Unproved*` prefix is wrong, but I reject the `CannotVerify*` replacement. `CannotVerify*` still describes what the *compiler* failed to do rather than what the *author* needs to know. It's the same smell wearing different clothes.

Elaine's own alternatives listed in her "Note" are better. I'm adopting the **condition-first** naming convention for proof diagnostics:

| Code | Elaine's `CannotVerify*` (rejected) | Adopted name |
|------|--------------------------------------|-------------|
| PRE0112 | `CannotVerifyModifierSatisfied` | `ModifierNotGuaranteed` |
| PRE0113 | `CannotVerifyDimensionQualifier` | `DimensionQualifierMissing` |
| PRE0114 | `CannotVerifyQualifierCompatibility` | `QualifiersMayBeIncompatible` |
| PRE0115 | `CannotVerifyInitialStateConstraint` → `InitialStateConstraintCannotBeSatisfied` | `InitialStateConstraintUnsatisfied` |
| PRE0116 | `CannotVerifyFieldIsPresent` | `FieldMayBeAbsent` |

**Rationale:** These names describe the *condition* the author produced, not what the compiler did or couldn't do. `FieldMayBeAbsent` tells the author what's wrong. `CannotVerifyFieldIsPresent` tells the author about the compiler's internal process. The author doesn't care about the compiler's verification process — they care about their field.

**Convention as adopted:** Proof diagnostic names describe the *state of the author's definition* (the condition), not the *outcome of the compiler's proof attempt* (the mechanism). No `CannotVerify*`, `Unproved*`, or `FailedToProve*` prefixes.

**CI enforcement family names:** Elaine's proposed renames (`CaseSensitiveEqualityOnCaseInsensitiveField`, etc.) are *longer* than the originals. `CaseSensitiveEqualityOnCaseInsensitiveField` is 46 characters. The originals are 41–48 characters. This trades operator-token jargon for a different verbosity problem.

**Counter-proposal for the CI family:**

| Code | Adopted name |
|------|-------------|
| PRE0066 | `CaseMismatchOnEquality` |
| PRE0095 | `CaseMismatchOnInequality` |
| PRE0096 | `CaseMismatchOnContains` |
| PRE0097 | `CaseMismatchOnStartsWith` |
| PRE0098 | `CaseMismatchOnEndsWith` |

**Rationale:** The shared `CaseMismatch` prefix makes the family immediately identifiable. The `On*` suffix names the operation that triggered it. Short, scannable, and describes what went wrong ("the case semantics don't match between the field and the operation") without encoding either the fix or the operator token.

### Additional Conventions

**Convention 8 — AI-parseable message structure.** Elaine's review focuses on human readability but does not explicitly address AI legibility, which is a first-class architectural concern for Precept. Messages should follow a parseable structure:

> **Subject** — **Condition** — **Repair** (optional)

This is already the implicit pattern in the best messages (e.g., PRE0116: "Cannot prove that '{0}' is present{1} — guard with 'when {0} is set', initialize it earlier, or make it required"). Codify it:

- The subject (field name, state name, value) appears first, quoted with single quotes.
- The condition follows immediately after the subject.
- If a repair hint is included, it follows an em-dash separator.
- No bare continuation slots (`. {n}`) that break structural parsing.

This convention does not require reformatting messages that already follow the pattern. It constrains new messages and any messages being rewritten for other reasons.

**Convention 9 — Rename PRs must update FixHint and RecoverySteps in the same pass.** The `DiagnosticMeta` record includes `FixHint`, `RecoverySteps`, `TriggerCondition`, `ExampleBefore`, and `ExampleAfter`. When a diagnostic is renamed and the old name's vocabulary appeared in these metadata fields (as with PRE0054 "default"), the metadata must be updated in the same commit. No partial renames.

### Priority Ordering

**Tier 1 — Urgent (user impact + AI legibility).** These fire on common authoring patterns, surface jargon in the Problems panel, and should be fixed in the first implementation pass:

- PRE0111 `RequiredStateDoesNotDominateTerminal` → `RequiredStateCanBeBypassed` (🔴 — the single worst jargon violation)
- PRE0110 `IrreversibleStateHasBackEdge` → `IrreversibleStateHasBackwardTransition` (name + message)
- PRE0116 `UnprovedPresenceRequirement` → `FieldMayBeAbsent` (fires constantly on optional fields)
- PRE0112 `UnprovedModifierRequirement` → `ModifierNotGuaranteed` (name + message)
- PRE0054 `DefaultForwardReference` → `FieldExpressionForwardReference` (misleading name, wrong FixHint)
- PRE0034 `InvalidModifierBounds` — message rewrite (four opaque slots)
- PRE0075 `InvalidUnitString` — message rewrite (no examples)
- PRE0077 `InvalidDimensionString` — message rewrite (no examples)
- PRE0085 `NonChoiceAssignedToChoice` → `ValueNotInChoiceSet` (name + message)

**Tier 2 — Important (consistency, family coherence).** These fix systemic inconsistencies and should be batched by family:

- PRE0099/0100/0101 collection-safety guard names → `Unguarded*` family (3 renames)
- PRE0066/0095/0097/0098 CI enforcement family → `CaseMismatch*` family (4 renames, with PRE0095 message)
- PRE0113 `UnprovedDimensionRequirement` → `DimensionQualifierMissing`
- PRE0114 `UnprovedQualifierCompatibility` → `QualifiersMayBeIncompatible` (name + message)
- PRE0115 `UnsatisfiableInitialState` → `InitialStateConstraintUnsatisfied` (name + message)
- PRE0119 `StructuralSinkState` → `NonTerminalDeadEnd`
- PRE0105 `CollectionInnerTypeError` → `CollectionElementTypeMismatch`
- PRE0089 `ChoiceRankConflict` → `ChoiceValueOrderMismatch`
- PRE0121/0123/0124 interpolation name renames (3 renames)

**Tier 3 — Cosmetic / defer.** These are improvements but not urgent:

- PRE0015 `PreEventGuardNotAllowed` → `TransitionGuardMustFollowEvent`
- PRE0127 `AssignmentInExpressionContext` → `AssignmentUsedAsComparison`
- PRE0021 `FunctionArityMismatch` → `WrongNumberOfArguments`
- PRE0022 `FunctionArgConstraintViolation` → `FunctionArgumentInvalid` (name + message)
- PRE0041 `WritableOnEventArg` → `EventArgCannotBeWritable`
- PRE0051 `InvalidInterpolationCoercion` → `NonTextTypeInStringInterpolation`
- PRE0052 `UnresolvedTypedConstant` → `UnrecognizedTypedConstant`
- PRE0068 `QualifierMismatch` → `IncompatibleFieldQualifier` (name + message)
- PRE0004/0008/0009/0010/0044/0047/0048/0050/0055/0057/0060/0061/0088/0104/0122 — message-only revisions (batch as a single "message polish" pass)

### Blocking Issues

1. **PRE0019 audit resolved (no longer blocking).** Repo-wide search found **no live PRE0019 emitters** in `src/Precept/`; only the enum member, `DiagnosticMeta`, and fault mapping exist. The previous optional-field-vs-general-surface dispute is therefore resolved in favor of a generalized future-facing rename. If/when the deferred precision upgrade is wired, use `ValueMayBeAbsent` with the message `"'{0}' may be unset — this position requires a value"` and do **not** revive the optional-field-specific rename.

2. **`CannotVerify*` prefix rejected.** The five proof diagnostic renames must use condition-first names as specified above, not the `CannotVerify*` prefix from the proposal. Implementation must use the adopted names from the "Disputed or Modified Conventions" table.

3. **CI enforcement family names must use the `CaseMismatch*` convention**, not Elaine's longer proposed names. Implementation must use the adopted names from the counter-proposal table above.

4. **PRE0119 `StructuralSinkState`** must become `NonTerminalDeadEnd`, not `StateWithNoWayOut`. Enum member names are API surface — colloquialisms are not acceptable.

None of these are hard blockers — they're rename adjustments that can be resolved during implementation planning. The review itself is approved for execution with these modifications incorporated.
