Precept icon philosophy

What the product does

Precept is a .NET library. You write a .precept file that declares an entity's states, fields, events, guards, and constraints in a flat, keyword-anchored DSL. The runtime compiles that file into an immutable engine with four operations:

- CreateInstance: start an entity in its initial state with default data.
- Inspect: ask "what would happen if I fired this event?" for every event, from any state. Non-mutating. The answer is always available.
- Fire: execute a transition. If the guard passes and no constraint is violated, the state and data change. If anything fails, the transition is rejected — the invalid state never exists.
- Update: edit a field directly. Same constraint enforcement.

The engine is deterministic. Same definition + same data = same outcome, always. There is no hidden state.

What makes it different

- Prevention, not detection. Invalid states don't get caught after the fact — they are structurally impossible. The contract prevents them.
- One file, complete rules. Every guard, constraint, invariant, and transition lives in the .precept definition. There is no scattered logic across services or ORM layers.
- Full inspectability. At any point, you can preview every possible action and its outcome without executing anything. Nothing is hidden.
- Compile-time checking. The compiler catches unreachable states, dead ends, type mismatches, null-safety violations, and structural contradictions before runtime.

The word itself

"Precept" means a strict rule or principle of action. The product treats business constraints as unbreakable precepts.

What the icon should evoke

Think about what it feels like to use the product:
- You define rules, and those rules hold. Period.
- You can always look inside and see exactly what will happen.
- Some paths are open. Others are structurally closed. The boundary between them is explicit.
- The system is small, self-contained, and exact. Not sprawling, not approximate.

The icon does not need to depict a workflow, a graph, or a state machine. It needs to make someone feel that something is governed, visible, and sound.

Color guidance
- You are free to use any palette that serves the concept.
- If you want to use color semantically (e.g. one color for "open" and another for "closed"), that's fine — but it is not required.
- Dark background preferred.
- The icon should work as a strong mark even in monochrome.

What to avoid
- Anything that looks like a generic SaaS badge.
- Vague concepts that don't trace back to what the product actually does.