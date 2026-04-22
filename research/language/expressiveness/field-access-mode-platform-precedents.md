# Field Access Mode Platform Precedents

> External research on how schema systems, form builders, CMS/low-code platforms, and authorization languages handle conditional field presence, visibility, and editability — informing Precept's `in <StateList> define <FieldList> <mode>` declaration form.

---

## 1. JSON Schema (Draft 2020-12)

**Mechanism**: Rule-level conditional validation via `if/then/else`, `dependentRequired`, and `dependentSchemas`.

**How it works**:
- `dependentRequired` — if property A is present, properties B and C become required: `"dependentRequired": { "creditCard": ["billingAddress"] }`.
- `dependentSchemas` — if property A is present, apply an additional subschema (can require fields, add constraints, or narrow types).
- `if/then/else` — evaluate a condition subschema; if it matches, apply `then`; otherwise apply `else`. Used for patterns like "if `country` is US, then `postalCode` must match a ZIP regex."

**Key vocabulary**: `required`, `properties`, `dependentRequired`, `dependentSchemas`, `if`, `then`, `else`, `allOf`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent → present | Yes | `dependentRequired`, conditional `required` in `then` |
| Read-only | Partial | `readOnly` keyword (advisory, not enforced by default) |
| Editable | Yes | Default — any property accepting writes |

**Declaration level**: Per-rule (conditions embedded in the schema alongside the property definitions).

**Precept relevance**: JSON Schema's `if/then/else` is analogous to Precept's `when <Guard>` clauses. The `dependentRequired` pattern maps to "field X becomes required when field Y has a value." However, JSON Schema has no concept of lifecycle states — conditions are purely data-driven, evaluated at a single point in time.

---

## 2. OpenAPI 3.x

**Mechanism**: Type-level polymorphism via `discriminator` + `oneOf`/`anyOf`/`allOf` composition, plus per-property `readOnly`/`writeOnly` markers.

**How it works**:
- `discriminator` — declares a property (e.g. `petType`) whose value selects which schema variant validates the object: `discriminator: { propertyName: "petType", mapping: { dog: "#/components/schemas/Dog" } }`.
- `oneOf`/`anyOf`/`allOf` — compose schemas for polymorphic responses: different field sets per variant.
- `readOnly: true` / `writeOnly: true` — per-property annotation. `readOnly` means the property is returned in responses but ignored in requests; `writeOnly` means accepted in requests but omitted from responses (e.g. passwords).

**Key vocabulary**: `discriminator`, `propertyName`, `mapping`, `oneOf`, `anyOf`, `allOf`, `required`, `readOnly`, `writeOnly`, `deprecated`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent → present | Yes | Different `required` lists per `oneOf` variant |
| Read-only | Yes | `readOnly: true` on property |
| Write-only | Yes | `writeOnly: true` on property |
| Editable | Yes | Default (not `readOnly`, not `writeOnly`) |

**Declaration level**: Per-property (static annotations) + per-type (variant schemas).

**Precept relevance**: OpenAPI's `readOnly`/`writeOnly` are the closest analogue to Precept's access mode vocabulary. The key difference: OpenAPI modes are static per-property, not state-dependent. Precept's `in <State> edit <Field>` is strictly stronger — same field can be `readOnly` in one state and `editable` in another.

---

## 3. Salesforce (RecordType + Page Layouts + Field-Level Security)

**Mechanism**: State-level field control via RecordType → Page Layout mapping, plus profile-based Field-Level Security.

**How it works**:
- **RecordType** — entities like `Case` or `Opportunity` can have multiple record types. Each record type maps to a page layout that controls which fields are visible and editable, and which picklist values are available.
- **Page Layouts** — per-record-type UI definitions that specify field visibility, field editability, required vs. optional status, and section arrangement. A field can be "visible + read-only" or "visible + editable" or absent from the layout entirely.
- **Field-Level Security** — per-profile/permission-set control. A field can be `Visible` (read-only) or `Editable` (read-write) or hidden entirely. This is independent of page layouts.
- **Validation Rules** — cross-field rules evaluated on save; can enforce conditional requirements: `IF(ISPICKVAL(Status, "Closed"), ISBLANK(Resolution__c), false)`.

**Key vocabulary**: `RecordType`, `PageLayout`, `FieldLevelSecurity`, `Visible`, `Editable`, `Required`, `ValidationRule`, `IsActive`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent (hidden) | Yes | Omit from page layout, or FLS hidden |
| Read-only | Yes | FLS `Visible` without `Editable`, or layout read-only |
| Editable | Yes | FLS `Editable` + layout editable |
| Required | Yes | Layout "required" marker or validation rule |

**Declaration level**: Per-state (RecordType selects layout) + per-role (FLS by profile). Two independent axes.

**Precept relevance**: Salesforce's model is the closest CRM analogue to Precept's state-scoped editability. The key insight: Salesforce separates the "which fields exist on the form" concern (page layout) from the "who can see/edit the field" concern (FLS). Precept conflates both into a single entity-level declaration, which is simpler but doesn't model role-based access.

---

## 4. ServiceNow (UI Policies + ACLs)

**Mechanism**: Rule-level conditional field behavior via UI Policies and server-side Access Control Lists (ACLs).

**How it works**:
- **UI Policies** — declarative rules tied to a table (entity) that fire when conditions are met. Each policy can specify actions on specific fields: `Mandatory` (true/false), `Visible` (true/false), `Read Only` (true/false). Conditions reference field values, including the record's state field.
- **UI Policy Actions** — each action targets a specific field and sets one of the three toggles. Example: "When State = Resolved, set Resolution Notes to Mandatory=true, Read Only=false, Visible=true."
- **ACLs** — server-side rules that control read/write/create/delete access per field, per role, per condition. Operates independently of UI policies.
- **Client Scripts** — JavaScript-based field manipulation (show/hide, enable/disable) for cases too complex for UI policies.

**Key vocabulary**: `UI Policy`, `Condition`, `Action`, `Mandatory`, `Visible`, `Read Only`, `ACL`, `Client Script`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent (hidden) | Yes | UI Policy action: `Visible = false` |
| Read-only | Yes | UI Policy action: `Read Only = true` |
| Editable | Yes | Default, or UI Policy: `Read Only = false` |
| Required/Mandatory | Yes | UI Policy action: `Mandatory = true` |

**Declaration level**: Per-rule (conditions evaluated against current field values, including state). Multiple rules can target the same field — last-writer-wins within priority order.

**Precept relevance**: ServiceNow's UI Policy model is almost exactly "when condition → set field access mode." It supports the same three modes Precept targets (absent, readonly, editable) plus mandatory. The condition-based approach mirrors Precept's `when <Guard>` pattern, but ServiceNow policies are imperative (set to X) rather than declarative (field IS X in this state).

---

## 5. Microsoft Dynamics 365 / Power Apps (Business Rules)

**Mechanism**: Rule-level conditional field behavior via entity-scoped Business Rules with condition → action chains.

**How it works**:
- **Business Rules** — declarative rules attached to an entity (table). Each rule specifies conditions (field value checks with AND/OR logic) and actions.
- **Supported actions**:
  - **Show or hide columns** — toggle field visibility on the form.
  - **Enable or disable columns** — toggle field editability (enabled = editable, disabled = read-only).
  - **Set requirement levels** — `Business Required`, `Business Recommended`, or `Not Business Required`.
  - **Set column values** — auto-populate a field.
  - **Set default values** — initial value on record creation.
  - **Validate data and show error messages** — reject invalid combinations.
- **Scope** — rules can be scoped to a specific form, all forms, or the entity (server-side).

**Key vocabulary**: `Business Rule`, `Condition`, `Action`, `Show`, `Hide`, `Enable`, `Disable`, `Required`, `Recommended`, `Not Required`, `Set Value`, `Validate`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent (hidden) | Yes | Action: `Hide` column |
| Read-only (disabled) | Yes | Action: `Disable` column |
| Editable (enabled) | Yes | Action: `Enable` column |
| Required | Yes | Action: Set requirement level to `Business Required` |

**Declaration level**: Per-rule (conditions + actions). Rules fire in defined order; multiple rules can target the same field.

**Precept relevance**: Dynamics 365 business rules use exactly the vocabulary Precept is considering: show/hide for presence, enable/disable for editability, plus requirement levels. The condition → action pattern is closest to Precept's `in <State> [when <Guard>]` model. Key difference: Dynamics rules can reference any field value in conditions, not just entity state.

---

## 6. Jira (Workflows + Screens + Field Configurations)

**Mechanism**: State-level field control via Workflow → Screen mapping per transition, plus Field Configuration Schemes.

**How it works**:
- **Workflows** — define statuses (states) and transitions between them. Each transition can be assigned a **Screen** — a form that collects field values during that transition.
- **Screens** — ordered lists of fields displayed during a specific operation (Create, Edit, View, or Transition). A field not on a screen is absent for that operation.
- **Field Configurations** — per-field settings: `Required` (true/false), `Hidden` (true/false), `Description`, `Renderer`. Applied via Field Configuration Schemes mapped to issue types.
- **Field Configuration Schemes** — map issue types to field configurations. Different issue types can have different required/hidden rules for the same field.
- **Behaviors (ScriptRunner / third-party)** — dynamic field rules: show/hide, enable/disable, set required, based on field values or transition context.

**Key vocabulary**: `Workflow`, `Status`, `Transition`, `Screen`, `Field Configuration`, `Required`, `Hidden`, `Renderer`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent (hidden) | Yes | Omit from screen, or Field Config `Hidden = true` |
| Read-only | Partial | No native per-field readonly on screens (needs add-on) |
| Editable | Yes | Present on screen + not hidden |
| Required | Yes | Field Configuration `Required = true` |

**Declaration level**: Per-state (transition screens) + per-issue-type (field configuration schemes). Read-only is the gap — Jira's native model doesn't distinguish "visible but not editable" from "editable" without plugins.

**Precept relevance**: Jira's Workflow → Screen model is the closest per-state field presence control. The mapping is: status (state) → transition → screen → field list. But it only controls which fields *appear*, not their access mode. Read-only fields during a transition require third-party behavior scripts.

---

## 7. Monday.com / Airtable (Conditional Field Rules)

**Mechanism**: View-level and automation-level field control; limited per-status conditional rules.

**How it works**:
- **Monday.com** — boards have Status columns. Column visibility can be toggled per-view but is not conditionally driven by status. Automations ("when Status changes to X → do Y") can set values but cannot show/hide or enable/disable columns. Monday.com's Column Permissions (Enterprise) allow per-column "edit" restrictions by role.
- **Airtable** — views can show/hide fields but not conditionally. Conditional field visibility/editability is achieved through Interface Designer forms that use conditional field visibility rules: "Show field Y when field X equals Z." Field permissions are view-level (who can edit which columns).

**Key vocabulary**: `Status Column`, `View`, `Automation`, `Column Permissions`, `Conditional Visibility` (Airtable Interface Designer), `Field Permissions`.

**Access mode coverage**:
| Mode | Monday.com | Airtable |
|------|-----------|----------|
| Absent (hidden) | Per-view only | Conditional in Interface forms |
| Read-only | Column Permissions (by role) | View-level edit restrictions |
| Editable | Default | Default |
| Conditional on status | Automations only (set value) | Interface Designer conditions |

**Declaration level**: Per-view (static) + per-automation (reactive). Neither platform has a declarative "in state X, field Y is readonly" model.

**Precept relevance**: These platforms show the demand for per-status field rules but rely on separate automation/permission layers rather than declarative schema-level definitions. This validates Precept's approach of making field access modes a first-class language concept rather than an afterthought.

---

## 8. Zod (v4 — TypeScript Schema Validation)

**Mechanism**: Type-level conditional field presence via discriminated unions, `partial`/`required`/`pick`/`omit`, and refinement-based conditional validation.

**How it works**:
- **Discriminated unions** — `z.discriminatedUnion("status", [...variants])`: each variant is an object schema with different required/optional fields. The discriminator property value selects which variant validates.
- **Partial/Required** — `.partial()` makes all properties optional; `.required()` makes all required. Both accept a key mask for selective application: `schema.partial({ field: true })`.
- **Pick/Omit** — `.pick({ field: true })` / `.omit({ field: true })`: structurally remove or retain fields.
- **Refinements** — `.refine()` with `when` parameter for conditional validation: "only run this check when these other fields are valid."
- **Optional/Nullable** — `z.optional(schema)`, `z.nullable(schema)`, `z.nullish(schema)`: per-field presence control.

**Key vocabulary**: `discriminatedUnion`, `discriminator`, `partial`, `required`, `pick`, `omit`, `optional`, `nullable`, `refine`, `superRefine`, `when`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent | Yes | Different `pick`/`omit` per variant, or `optional` |
| Required → optional | Yes | `.partial()` / `.required()` with key masks |
| Conditional presence | Yes | Discriminated union variants |
| Conditional validation | Yes | `.refine()` with `when` guard |

**Declaration level**: Per-type (discriminated unions define field sets per variant) + per-rule (refinements add conditional constraints).

**Precept relevance**: Zod's discriminated union is the TypeScript-world analogue of per-state field shapes. Each variant (keyed by a discriminator like `status`) defines a complete field schema. Precept's `in <State> define <Field> <mode>` is more ergonomic — you declare differential access per state rather than repeating the full object shape for each variant.

---

## 9. GraphQL (Interfaces + Union Types)

**Mechanism**: Type-level conditional field presence via abstract types (Interfaces and Unions) with inline fragments for type-specific field access.

**How it works**:
- **Interface types** — define shared fields that implementing types must include. An interface `Character { name: String! }` is implemented by `Human { name, totalCredits }` and `Droid { name, primaryFunction }`.
- **Union types** — `union SearchResult = Human | Droid | Starship`: no shared fields required. Client must use inline fragments (`... on Human { height }`) to access type-specific fields.
- **Non-Null modifier** — `String!` enforces field presence (not nullable).
- **Directives** — `@deprecated(reason: "Use fullName")` for lifecycle field annotations. Custom directives can implement `@auth`, `@visible`, etc.

**Key vocabulary**: `interface`, `union`, `implements`, `inline fragment`, `Non-Null` (`!`), `@deprecated`, `@skip`, `@include`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent | Yes | Field not on this type (union member) |
| Present/required | Yes | Non-Null (`!`) modifier |
| Conditional presence (query-time) | Yes | `@include(if: $var)` / `@skip(if: $var)` directives |
| Read-only | No | No native concept (all fields are read from resolver) |

**Declaration level**: Per-type (interfaces/unions define which fields exist) + per-query (directives control inclusion).

**Precept relevance**: GraphQL's model is "different types have different field sets" — the closest analogue to Precept's "different states have different field access modes." But GraphQL operates at the query/consumption layer, not the mutation/write layer. It doesn't restrict which fields can be *written* — that's left to resolver logic.

---

## 10. Cedar (AWS Verified Permissions Policy Language)

**Mechanism**: Authorization-level attribute-based access control via `permit`/`forbid` policies with `when`/`unless` conditions.

**How it works**:
- **Policies** — `permit(principal, action, resource) when { conditions }` or `forbid(...)`. Each policy evaluates scope (who, what, on what) plus conditions.
- **Conditions** — `when { resource.private }`, `unless { principal == resource.owner }`. Can reference entity attributes, context, and relationships.
- **Default deny** — if no policy permits, the request is denied.
- **Explicit deny wins** — any `forbid` overrides all `permit` policies.
- **Annotations** — `@advice("message")`, `@id("policy-name")` for metadata (no impact on evaluation).

**Key vocabulary**: `permit`, `forbid`, `when`, `unless`, `principal`, `action`, `resource`, `context`, `in` (hierarchy membership).

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Hidden/absent | Yes | `forbid` action `read` on resource.field |
| Read-only | Yes | `permit` action `read` + `forbid` action `write` |
| Editable | Yes | `permit` action `write` |
| Conditional | Yes | `when { resource.status == "Draft" }` |

**Declaration level**: Per-policy (fine-grained attribute-based rules). Policies compose — the engine evaluates all relevant policies and applies deny-overrides.

**Precept relevance**: Cedar's `when`/`unless` syntax is strikingly close to Precept's `when <Guard>` clause. Cedar's vocabulary (`permit`/`forbid`) expresses authorization rather than field access modes directly, but the pattern is the same: "under these conditions, allow/deny this operation on this resource." Cedar separates authorization from entity schema; Precept integrates them into a single definition.

---

## 11. React Hook Form / Formik (Conditional Form Fields)

**Mechanism**: Render-level conditional field presence via JavaScript control flow; validation library integration for conditional requirements.

**How it works**:
- **React Hook Form** — fields are conditionally rendered based on `watch()` values: `{status === "Approved" && <TextField name="approvalNotes" />}`. Unregistered (unmounted) fields are absent from the form data.
- **Formik** — similar pattern: conditional JSX rendering based on `values.status`. Validation via Yup/Zod schemas can use `.when()` for conditional requirements.
- **Yup conditional validation** — `yup.string().when('status', { is: 'Approved', then: schema => schema.required() })`: field becomes required based on another field's value.

**Key vocabulary**: `watch`, `register`, `unregister`, `conditional rendering`, `.when()`, `validate`, `required`.

**Access mode coverage**:
| Mode | Supported? | How |
|------|-----------|-----|
| Absent | Yes | Don't render the field (unmounted = absent from data) |
| Read-only | Yes | `disabled` prop or `readOnly` prop on input |
| Editable | Yes | Default rendered state |
| Conditional | Yes | JavaScript `if`/ternary based on watched values |

**Declaration level**: Per-field (render-time logic). No centralized declaration — conditions scattered across component code.

**Precept relevance**: Form libraries demonstrate the demand for per-state field access control but solve it imperatively. The lack of a declarative model means field access rules are scattered across rendering code, validation schemas, and event handlers. Precept's single-point declaration (`in <State> define <Field> <mode>`) is specifically designed to eliminate this scatter.

---

## Cross-Platform Synthesis

### Vocabulary Convergence

Across all 11 systems, the core field access modes converge on three to four concepts:

| Precept Mode | JSON Schema | OpenAPI | Salesforce | ServiceNow | Dynamics 365 | Jira | Zod | GraphQL | Cedar | Form Libs |
|-------------|------------|---------|-----------|------------|-------------|------|-----|---------|-------|-----------|
| **absent** | not in `required` | not in variant | not on layout | `Visible=false` | `Hide` | not on screen | `omit` | not on type | `forbid read` | unmounted |
| **readonly** | `readOnly` | `readOnly` | FLS Visible | `ReadOnly=true` | `Disable` | (plugin) | — | — | `permit read` only | `disabled` |
| **editable** | default | default | FLS Editable | default | `Enable` | on screen | default | — | `permit write` | default |
| **required** | `required` | `required` | layout required | `Mandatory=true` | `Required` | Field Config | `.required()` | Non-Null (`!`) | — | `.required()` |

### Declaration Patterns

| Pattern | Systems | How |
|---------|---------|-----|
| **Per-state (lifecycle-driven)** | Salesforce (RecordType), Jira (Workflow→Screen), Precept | State determines which layout / field set applies |
| **Per-rule (condition-driven)** | JSON Schema, ServiceNow, Dynamics 365, Cedar | Conditions on field values → actions on field access |
| **Per-type (variant-driven)** | OpenAPI, Zod, GraphQL | Discriminated type determines field shape |
| **Per-render (imperative)** | React Hook Form, Formik, Monday.com | Runtime code decides field presence/mode |

### Key Findings for Precept

1. **Three modes is the consensus**: absent, readonly, and editable appear in every system. "Required" is a fourth mode that most systems support, but it's orthogonal (a field can be required AND editable, or required AND readonly in display contexts).

2. **State-driven > condition-driven for enterprise workflows**: Salesforce, Jira, and Dynamics 365 (the dominant enterprise platforms) all ultimately resolve field access modes through a state-like concept (RecordType, workflow status, business rule conditions on status). This validates Precept's `in <State>` anchor.

3. **Declarative beats imperative**: Form libraries and low-code platforms that scatter field access rules across runtime code (React Hook Form, Monday.com) create maintenance and reasoning burdens. JSON Schema's `if/then/else` and Cedar's `when/unless` show that declarative condition expressions are both readable and machine-analyzable.

4. **Vocabulary matters for AI discoverability**: OpenAPI's `readOnly`/`writeOnly` and Dynamics 365's `Show`/`Hide`/`Enable`/`Disable` are immediately understandable. JSON Schema's `dependentRequired` is technically precise but opaque. Precept's `absent`/`readonly`/`editable` vocabulary lands in the semantic sweet spot — clear intent without encoding mechanism.

5. **Per-field vs. field-list granularity**: Most systems apply access modes per-field (one rule per field). Precept's `in <State> define <FieldList> <mode>` batches multiple fields in one declaration — a DSL advantage that reduces repetition without sacrificing clarity.

6. **Guard clauses add necessary expressiveness**: ServiceNow's UI Policies, Dynamics 365 Business Rules, and Cedar all support conditions beyond state alone (field values, context, relationships). Precept's `when <Guard>` clause serves this role. Systems without guards (Jira native, Monday.com) require plugins or scripting for the same expressiveness.

---

*Research conducted 2026-07-14. Sources: JSON Schema Draft 2020-12 docs, OpenAPI 3.x specification, Salesforce developer docs, ServiceNow platform docs (from practitioner knowledge — docs redirect), Dynamics 365 Power Apps docs, Jira Cloud administration docs (from practitioner knowledge), Monday.com/Airtable product docs, Zod v4 docs, GraphQL specification, Cedar Policy Language reference, React Hook Form/Formik community patterns.*
