# Semantic Visual System Manifest

Status: Working draft. This document is a first-pass canonical structure for the Semantic Visual System. It is intended to guide design and runtime alignment work, not to claim that every surface semantic below is already fully settled or publicly supported.

## Purpose and Boundary

The Semantic Visual System defines how Precept's governed integrity should be read across user-facing surfaces. Its job is to keep meaning recognizable as a reader moves between a state diagram, a timeline, and a data form.

This manifest is a faithful projection over runtime truth, not a second semantic system. It does not invent new domain meaning beyond what the runtime, the definition, and explicit host receipts can honestly support.

The manifest sets shared semantic expectations for the current target surfaces. It does not replace the DSL, the runtime, the brand system, or surface-local layout design. Brand still owns identity meaning. Runtime still owns what is true. Individual surfaces still own how they use space, interaction, and emphasis to present that truth.

## Philosophy Alignment

The Semantic Visual System must stay aligned with Precept's product philosophy.

- Precept is entity-first. The thing being read is a governed entity, not a workflow diagram in the abstract.
- Data is primary. The system exists to help users read what is true of the entity's data now, what may change, and why.
- State is instrumental and optional. When lifecycle exists, state helps users locate the entity's current governed position. When lifecycle does not exist, the visual system must still read clearly through data and rule semantics alone.
- Events are one governed change path. They are not generic buttons or transport actions. They are named attempts to change an entity configuration under declared rules.
- Direct edit is a separate governed change path exposed through `Update` and `edit` declarations. It is not event semantics in different clothing.
- Reasons must remain visible. A rule, rejection, or blocked action without an understandable reason is semantically incomplete.
- Inspectability matters. The user-facing surfaces should help a reader distinguish what is true now from what inspection says would happen, and why.

## Core Semantic Layers

The Semantic Visual System should be understood as a stack of semantic layers rather than as a single diagramming style.

1. Entity truth
The entity's current configuration. For stateful precepts, this is current state plus current data. For stateless precepts, this is current data alone. This is present-tense truth, distinct from hypothetical inspection results.

2. Governed change
The named events and direct edits that attempt to change the entity, together with the outcomes and receipts those attempts produce when such truth is available.

3. Rule and reason
The constraints, invariants, asserts, and rejections that explain why a configuration is valid or invalid, and why a surface may normalize an inspected change path as available, blocked, incomplete, accepted, or rejected.

4. Surface translation
The surface-specific forms that make the same meaning legible in a state diagram, a timeline, and a data form.

5. Scaffolding and support
Chrome, grouping, rails, labels, panes, helper text, and other support structures that organize reading without claiming semantic meaning of their own.

The first three layers are the semantic core. Surface translation should reveal them. Scaffolding should support them without becoming a hidden second vocabulary.

## Surface Set and Responsibilities

The current target set is intentionally small.

### State Diagram

The state diagram is the topology-reading surface. It should answer:

- Where can this entity be?
- Where is it now, when state exists?
- Which event paths currently inspect as available, blocked, rejected, or terminal?
- Which reasons are attached to those paths?

The state diagram is strongest when helping a user read structural possibility and current callable movement. It should not be asked to carry the entire burden of data comprehension.

### Timeline

The timeline is the temporal-reading surface. It should answer:

- What happened?
- What was attempted by event or direct edit?
- What changed?
- What reason or verdict accompanied that attempt?

The timeline is where governed attempts become legible as a sequence. It is the right place for receipts, outcomes, and visible reasons over time. A durable timeline likely needs either first-class runtime support or host-owned receipts and change history; current instance truth alone is not enough to reconstruct a trustworthy event or edit history.

### Data Form

The data form is the configuration-reading surface. It should answer:

- What data is true now?
- What can be directly edited now?
- What fields are constrained, required, satisfied, or violated?
- What event actions are available from this current configuration?

The data form is the most unresolved of the three target surfaces. It is clearly necessary, because Precept governs data first, but the exact user-facing semantics for direct editability, requiredness, latent rule pressure, event affordance placement, explanatory density, and fuller field-status contracts still need explicit design decisions. Some current field truth is public today; a fully public, runtime-complete field-status model is not.

## Construct Definitions

The manifest currently centers five semantic categories: state, event, data, rule, and scaffolding/support boundary. Direct edit is also a first-class governed change path, but it is surfaced primarily through data-form semantics rather than as a separate sixth surface vocabulary.

### State

State is the entity's current governed position when lifecycle exists. It is not the product's primary semantic object, but it is the main coordinate system for lifecycle-aware reading.

- A state should read as a condition of the entity, not as a decorative badge.
- State semantics should communicate position, currentness, entry or terminal meaning where relevant, and reachability or callability where that truth is available.
- State must remain optional in the system model. Stateless precepts cannot be treated as visually secondary or malformed.

### Event

An event is a named attempt to produce governed change.

- Events should read as attempts with consequences, not as generic commands.
- Event semantics should carry availability, surface-normalized blocked or incomplete status where inspection truth supports that reading, rejection, no-transition outcomes, and target meaning where that truth is available.
- Event reasons belong in the user-facing surface when the event is blocked, rejected, or otherwise semantically important.

### Direct Edit

Direct edit is a first-class governed change path exposed through `Update` and `edit` declarations.

- Direct edit should read as governed change to an entity configuration, not as a disguised event.
- Direct edit semantics should carry whether a field is editable now, what hypothetical patch inspection says, and what reasons or violations become visible if the change is inspected or applied.
- A surface may place direct edit affordances differently from event actions, but both must read as governed integrity rather than freeform mutation.

### Data

Data is the primary semantic object the product governs.

- Data surfaces should privilege field truth, current value, granted editability, and field-status signals where runtime truth actually supports them.
- Data should not be treated as supporting detail under the state machine. In Precept, the state machine exists to help govern data when lifecycle is present.
- In stateless precepts, data remains the full semantic center.

### Rule

Rule is the visible expression of why the entity may or may not hold a configuration or accept a change.

- Rules should be user-facing through reasons, not exposed as raw engine internals.
- Invariants express configuration truth that must always hold. Asserts are scoped checks attached to a state anchor or an event. Surfaces may simplify presentation language, but they should not erase that distinction when scope matters.
- The most important public rule artifact is the visible reason attached to a requirement, violation, block, or rejection.
- Rules may be ambient when satisfied, but they must become explicit when they shape what the user can do or understand.

### Scaffolding and Support Boundary

Scaffolding includes layout rails, chrome, section frames, labels, groupings, and supporting controls that organize reading.

- Scaffolding is necessary, but it is not itself a semantic construct.
- Support patterns may emphasize or de-emphasize semantic content, but they must not invent new business meaning.
- When a reader learns a state, event, data, or rule meaning, that meaning should travel across surfaces. Pane chrome, card shells, and rails do not carry that same obligation.

## Event Semantics as Currently Understood for User-Facing Surfaces

For user-facing surfaces, an event currently reads as an attempted governed change evaluated from the entity's current configuration. That reading is distinct from direct-edit semantics and from the hypothetical inspection result the runtime may return for a possible attempt.

- An event may inspect as callable and successful.
- An event may inspect as callable but produce no state change.
- An event may inspect as unmatched in the current configuration.
- An event may inspect as explicitly rejected.
- A surface may normalize some inspected outcomes as blocked when rule conditions prevent success in the current configuration.
- A surface may normalize missing required input as incomplete rather than merely unavailable.
- An event may require arguments before it can be meaningfully fired.

For surfaces, that leads to several practical semantic rules.

- A visible event should carry an understandable inspected outcome or normalized status, not just enabled or disabled styling.
- If current inspection truth includes a target state, that target should be legible where it helps reading, especially in the state diagram and timeline.
- If current inspection truth includes a reason for rejection or failed callability, the reason should be visible in product language.
- If input is required, the surface should communicate incomplete rather than merely unavailable, while preserving that this is a surface reading over inspection truth rather than a separate runtime ontology.
- A successful event is not only a movement token. It is an explained change to an entity configuration.

This remains a current understanding, not a closed final taxonomy. Some richer event semantics may require additional public runtime detail before they can be made canonical.

## Mapping Boundary Between Runtime Truth and Surface Semantics

This section is the critical boundary rule.

- Runtime truth owns what the entity is, what events exist, what current data exists, what current state exists, which fields are editable, what outcomes inspect and fire return, and what reasons are explicitly present in the contract or result.
- Runtime truth also distinguishes current instance truth from hypothetical inspection truth. Current state and current data describe what is now. `Inspect` and hypothetical `Update` results describe what would happen for a possible event or patch from that current configuration.
- Surface semantics own how that truth is made legible to a human reader across the three target surfaces.
- The manifest may normalize presentation language, signal precedence, and cross-surface consistency, including labels such as available, blocked, or incomplete when they are faithful readings over inspect or update-preview truth.
- The manifest may not invent missing causal history, hidden rule reasons, synthetic target states, complete field-status semantics, or lifecycle significance that the runtime or host cannot substantiate.

In practice, that means:

- Current-state semantics are mostly projection work. The current public runtime already exposes much of what a surface needs for present-tense reading.
- Hypothetical semantics are partly projection work. Inspection and update-preview results can support event and edit affordance reading, but surfaces must keep that distinct from what is currently true of the entity.
- History semantics are not yet fully projection work. A timeline that claims durable event history or field-change history needs either first-class runtime support or host-owned receipts and history.
- Richer surface semantics should be treated as provisional until their supporting truth is public and stable.

## Required Runtime Support

The Semantic Visual System should stay honest about what it can already rely on and what still needs public support. The key distinction is between current truth, hypothetical inspection truth, and durable past-tense receipt truth.

### Mostly Available for Current-State Surfaces

Current public contracts appear sufficient for much of the present-tense semantic layer, including:

- Current state when lifecycle exists
- Current field data
- Stateless versus stateful distinction
- Declared events and required event inputs
- Many event inspection outcomes, including target-state semantics where the inspected path actually resolves one
- Baseline editable field information for the current configuration
- Hypothetical patch inspection for live form feedback

This is enough to ground significant portions of the state diagram and the present-tense plus hypothetical portions of the data form.

### Likely Needed for Timeline and Richer Surface Semantics

The following likely need additional public runtime support, or a clearly sanctioned host-owned receipt model, before the manifest can treat them as fully canonical:

- Durable event-attempt history across time
- Durable field-edit history across time
- Receipts for blocked, rejected, unmatched, and successful attempts that can be rendered later as timeline truth
- Consistent structured reason coverage across outcomes, without losing the reasons already present in current public contracts
- Clear support for showing what changed as the result of an accepted event or edit, likely through receipt-level change-set detail

The practical boundary is simple: if a surface needs a trustworthy past-tense narrative, current instance state is not enough by itself.

### Likely Needed for the Data Form's Richer Semantics

The data form is also where the public/runtime truth model is least complete today. It may need clearer public contracts for:

- Field-level requiredness as a surface semantic rather than an inferred styling choice
- Field-level rule pressure before commit versus after inspection
- Normalized violation and explanation detail suitable for inline form guidance
- A fuller stable contract to distinguish editable, locked, invalid, conditionally editable, contextually relevant, and inspect-violated fields

These are design-system needs, but they depend on a public truth model if they are to be treated as shared canonical guidance.

## Open Questions

This draft intentionally leaves several questions open.

### Data Form Questions

- What is the primary semantic unit of the data form: field row, grouped section, or full configuration card?
- How should the form present rule pressure before a user edits anything?
- How should requiredness differ visually from invalidity, and both from simple editability?
- Where should event actions live in relation to the form: inline with fields, in a separate action region, or in a hybrid decision area?
- How much explanation should be always visible versus progressively disclosed?
- What is the right stateless specimen for proving that the form is not merely a workflow adjunct?

### Cross-Surface Questions

- Which semantic modifiers are universal across all three target surfaces, and which are surface-specific translations?
- Which event outcome categories are canonical enough to standardize now, and which should remain provisional?
- How should the system present successful no-transition outcomes so they read as meaningful change rather than as anticlimax?

### Runtime Boundary Questions

- Should timeline semantics be grounded in first-class runtime receipts, host-owned history, or both?
- What is the minimum public result shape required for reasons to remain visible without exposing engine-internal noise?
- Which richer surface semantics belong in the shared manifest, and which should remain local to a preview or product surface until runtime support matures?

This document should evolve only as runtime truth, product philosophy, and surface design converge. If those three drift apart, the manifest must report the gap rather than hide it.