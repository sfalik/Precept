# State Diagram Layout Redesign — Options Analysis

> **Status:** Implemented — Option B (Fix ELK) selected and implemented. See `tools/Precept.VsCode/src/extension.ts` and `tools/Precept.VsCode/webview/inspector-preview.html`.  
> **Decision date:** 2026-03-03  
> **Scope:** Inspector Preview diagram rendering in the VS Code extension

---

## Current Architecture

The diagram pipeline has three layers:

| Layer | File | Role |
|---|---|---|
| **1. ELK layout** | `tools/Precept.VsCode/src/extension.ts` | Computes node positions and edge routing via `elkjs` layered algorithm |
| **2. Post-processing** | `tools/Precept.VsCode/src/extension.ts` | Force-directed "organic" re-layout, layout stabilization, parallel-edge deconfliction, bounds normalization |
| **3. SVG rendering** | `tools/Precept.VsCode/webview/inspector-preview.html` | Builds SVG via string concatenation with custom edge routing, annotations, and state-change animation |

### Identified Problems

#### P1 — ELK Edge Output Is Discarded

`createOrganicOptimizedLayout()` runs a 92-iteration Fruchterman-Reingold force simulation that repositions nodes relative to ELK's output, but returns `edges: []`. All of ELK's crossing-minimized, properly-routed edge data is thrown away. The webview must then re-invent edge routing from scratch.

#### P2 — Three Competing Layout Systems

Three independent layout engines produce conflicting results:

1. **ELK layered** — proper graph layout in `computeLayoutForSnapshot()`
2. **Force-directed organic optimizer** — partially overrides ELK positions in `createOrganicOptimizedLayout()`
3. **Client-side fallback** — `buildStatePositions()` in the HTML (line 698), a rank-based layout with barycentric ordering that is computed independently

The "Default" vs "Optimized" toggle switches between ELK-raw (with piecewise-linearized edges) and force-directed (with no edges at all, falling back to Bézier heuristics). Neither produces good results.

#### P3 — Fixed Node Size Mismatch

Nodes are always rendered at 112×44px (hardcoded in SVG: `pos.x - 56`, `width="112"`). The size passed to ELK uses a rough `68 + (name.length * 8)` estimate. Long state names clip; short names waste space. ELK plans layout based on one size but SVG renders another.

#### P4 — Edge Routing Is Bézier Guesswork

`buildEdgePath()` (HTML line 1660) uses heuristic branches:

- **Self-loops:** hardcoded right-side cubic with magic offsets
- **Near-horizontal (|dy| < 40):** quadratic Bézier with computed lift
- **Backward edges (dx < 0):** cubic routed left of both nodes
- **Default:** cubic with control points at 34% from each end

None of these prevent edge-node overlap, edge-edge crossing, or label collision. Post-hoc patches (`deconflictParallelEdges`, `deconflictFanEdges`, `applyTargetIngressBands`) apply normal-direction offsets to polylines, but don't interact with the Bézier paths.

#### P5 — Massive Unmaintainable `drawDiagram()`

`drawDiagram()` is a single ~800-line function that builds SVG via string concatenation. It has two main branches (animation vs. static) with heavily duplicated visual logic. The animation branch alone is ~400 lines of inline SVG construction with hardcoded opacity/scale/glow values.

#### P6 — viewBox Is Poorly Sized

The viewBox defaults to 920×430 with aggressive clamping (`Math.max(700, ...)`, `Math.max(420, ...)`). Small machines (2–3 states) waste canvas. Large machines get compressed by `fitScale`.

### Affected Code Inventory

#### Extension side (`extension.ts`)

| Function | Lines | Purpose | Problem |
|---|---|---|---|
| `getElkEngine()` | 325–331 | Lazy-loads `elkjs/lib/elk.bundled.js` | Fine |
| `extractSnapshotStates()` | 333–343 | Reads `states` from snapshot | Fine |
| `extractSnapshotTransitions()` | 345–370 | Reads `transitions` from snapshot | Fine |
| `getPreviewLayoutMode()` | 372–383 | Reads VS Code setting | Fine |
| `getElkLayoutOptions()` | 385–436 | Returns ELK option set per mode | Suboptimal defaults; no self-loop or feedback-edge options |
| `offsetPolyline()` | 438–466 | Offsets a polyline by a normal vector | Compensates for lost ELK data |
| `deconflictParallelEdges()` | 468–510 | Spreads edges sharing same from→to | Compensates for lost ELK data |
| `deconflictFanEdges()` | 512–566 | Spreads fan-in/fan-out edges | Compensates for lost ELK data |
| `routeThroughTargetIngressBand()` | 568–594 | Adds ingress point near target | Compensates for lost ELK data |
| `applyTargetIngressBands()` | 596–646 | Applies ingress bands to edge groups | Compensates for lost ELK data |
| `stabilizeLayout()` | 648–694 | Blends current layout with prior (78/22 node, 82/18 edge) | Compensates for layout instability caused by organic optimizer |
| `normalizeLayoutBounds()` | 696–749 | Fits layout into target bounds with scale/padding | Overly rigid target (900×740) |
| `createOrganicOptimizedLayout()` | 762–860 | 92-iteration force-directed simulation anchored to ELK positions | Destroys ELK edges; marginal node improvement |
| `computeLayoutForSnapshot()` | 862–960 | Orchestrates ELK call, extracts results, runs organic optimizer | Core layout pipeline |
| `withLayout()` | 962–990 | Attaches layout to snapshot response | Fine |

#### Webview side (`inspector-preview.html`)

| Function | Lines | Purpose | Problem |
|---|---|---|---|
| `buildStatePositions()` | 698–830 | Rank-based fallback layout with barycentric crossing minimization | Entire separate layout engine; diverges from ELK |
| `pointsToPath()` | 832–860 | Converts `{x,y}[]` to SVG path with `L` (line) commands | Linearizes smooth edges |
| `parseLayoutPayload()` | 880–940 | Parses layout from extension message | Fine |
| `applySnapshot()` | 942–1020 | Applies full snapshot including layout | Fine |
| `applyRenderLayoutMode()` | 1022–1028 | Switches Default/Optimized | Controls which broken layout is used |
| `buildEdgePath()` | 1660–1700 | Hand-coded Bézier routing heuristics | Core of the edge quality problem |
| `getEdgeVisual()` | 1702–1780 | Computes stroke/marker/width/opacity per edge | Business logic; mostly fine |
| `getEdgeAnnotation()` | 1782–1820 | Computes label position via `getPointOnPath()` | Should use ELK label placement |
| `drawDiagram()` | 1614–2390 | Builds all SVG (animation + static) | 800 lines of string concatenation |

---

## Option A: Replace with Mermaid

### Summary

Replace the entire custom layout + rendering pipeline with [Mermaid's](https://mermaid.js.org/) `stateDiagram-v2` renderer running directly in the webview.

### How It Works

**Extension side** — convert snapshot to Mermaid DSL text:

```typescript
function snapshotToMermaid(snapshot: SnapshotData): string {
  const lines: string[] = ["stateDiagram-v2"];
  const currentState = snapshot.currentState ?? snapshot.CurrentState;
  const states = snapshot.states ?? snapshot.States ?? [];
  const transitions = snapshot.transitions ?? snapshot.Transitions ?? [];
  const initialState = states[0] ?? currentState;

  // Initial transition
  if (initialState) {
    lines.push(`  [*] --> ${initialState}`);
  }

  // State transitions
  for (const t of transitions) {
    const from = t.from ?? t.From;
    const to = t.to ?? t.To;
    const event = t.event ?? t.Event;
    if (from === to) {
      // Self-loop
      lines.push(`  ${from} --> ${from} : ${event}`);
    } else {
      lines.push(`  ${from} --> ${to} : ${event}`);
    }
  }

  // Highlight current state via note
  if (currentState) {
    lines.push(`  note right of ${currentState} : ● active`);
  }

  return lines.join("\n");
}
```

**Webview side** — render Mermaid output, then apply semantic styling:

```html
<script type="module">
  import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

  mermaid.initialize({
    startOnLoad: false,
    theme: 'dark',
    themeVariables: {
      primaryColor: '#24262b',
      primaryBorderColor: '#6D7F9B',
      lineColor: '#1FFF7A',
      textColor: '#FFFFFF',
      fontSize: '13px'
    },
    stateDiagram: {
      useMaxWidth: true
    }
  });

  async function renderDiagram(mermaidText, evaluations) {
    const { svg } = await mermaid.render('precept-diagram', mermaidText);
    const diagramEl = document.getElementById('diagram-container');
    diagramEl.innerHTML = svg;

    // Post-process: apply semantic classes to edges/nodes
    applySemanticStyles(diagramEl, evaluations);
  }

  function applySemanticStyles(container, evaluations) {
    // Find node elements by their text content and add classes
    const nodeGroups = container.querySelectorAll('.stateDiagram-state');
    for (const group of nodeGroups) {
      const text = group.querySelector('text')?.textContent?.trim();
      if (text === currentState) {
        group.classList.add('precept-active');
      }
    }

    // Find edge paths and adjust stroke based on evaluation
    const edgePaths = container.querySelectorAll('.transition');
    for (const edge of edgePaths) {
      const label = edge.querySelector('text')?.textContent?.trim();
      const evaluation = evaluations[label];
      if (evaluation?.kind === 'enabled') {
        edge.classList.add('precept-enabled');
      } else if (evaluation?.kind === 'blocked') {
        edge.classList.add('precept-blocked');
      }
    }
  }
</script>

<style>
  .precept-active rect { fill: var(--ok) !important; stroke: var(--ok) !important; }
  .precept-active text { fill: var(--bg) !important; font-weight: 700 !important; }
  .precept-enabled path { stroke: var(--edge) !important; }
  .precept-blocked path { stroke: var(--edge-error) !important; stroke-dasharray: 5 4; }
</style>
```

### Code to Delete

| File | What | Lines Removed |
|---|---|---|
| `extension.ts` | Everything from `getElkEngine()` through `withLayout()` | ~660 lines |
| `inspector-preview.html` | `buildStatePositions()`, `buildEdgePath()`, `drawDiagram()`, all animation code | ~1200 lines |
| `package.json` | `elkjs` dependency | — |

### Code to Add

| File | What | Lines Added |
|---|---|---|
| `extension.ts` | `snapshotToMermaid()` function | ~40 lines |
| `inspector-preview.html` | Mermaid import, `renderDiagram()`, semantic post-processing | ~80 lines |
| `package.json` | `mermaid` dependency (webview-side, or bundled) | — |

### Pros

- **Battle-tested layout.** Mermaid uses Dagre internally, which handles crossing minimization, edge routing, self-loops, label placement, and cycle handling correctly out of the box.
- **Massive code reduction.** Eliminates ~1800 lines of custom layout + rendering code.
- **Dark theme support.** Mermaid has built-in dark theme with customizable CSS variables.
- **SVG output.** Pan/zoom can be added with a lightweight library (e.g., `svg-pan-zoom`).
- **Maintenance-free layout improvements.** Mermaid updates improve layout without any code changes.
- **Accessibility.** Mermaid generates accessible SVG with ARIA attributes.

### Cons

- **Semantic edge coloring is harder.** Mermaid doesn't expose per-edge evaluation semantics. Post-processing the SVG DOM is fragile and depends on Mermaid's internal CSS class naming, which could change across versions.
- **Transition animation is lost.** The current runner-dot animation, glow effects, and handoff rings cannot be implemented within Mermaid's rendering model. You'd need to build animation as a separate overlay layer that references Mermaid's SVG positions, which is complex and fragile.
- **Limited styling control.** Mermaid controls font sizes, shapes, and spacing. Customizing node shapes (e.g., pill vs rectangle) requires forking Mermaid's renderer.
- **Bundle size.** Mermaid is ~1.5MB (minified). It also pulls in D3, Dagre, and DOMPurify.
- **Version coupling.** Mermaid's SVG structure and CSS classes are implementation details that change between major versions. Semantic post-processing would need to be validated against each Mermaid upgrade.
- **No interactive node selection.** Mermaid's output is static SVG. Click/hover handlers would need to be attached to SVG elements found by text content matching, which is brittle.
- **Self-loop appearance.** Mermaid's self-loop rendering is adequate but not customizable (always renders as a loop above the node).

### Complexity Estimate

- **Implementation effort:** Low (1–2 days)
- **Risk:** Medium — fragile post-processing for semantic styling; animation loss
- **Ongoing maintenance:** Low for layout, medium for semantic overlay

---

## Option B: Keep ELK, Use Its Output Properly

### Summary

Fix the existing ELK-based pipeline by removing the force-directed optimizer, passing ELK's edge routing data through to the webview intact, tuning ELK options for state machines, and making node sizing dynamic. Preserves the current animation system.

### How It Works

#### B1. Remove the force-directed optimizer

Delete `createOrganicOptimizedLayout()` and all compensatory code. ELK's layered algorithm already produces proper node positions — the force simulation just adds jitter and noise.

**Functions to delete from `extension.ts`:**
- `createOrganicOptimizedLayout()` (~100 lines)
- `stabilizeLayout()` (~50 lines)
- `normalizeLayoutBounds()` (~55 lines)
- `offsetPolyline()` (~30 lines)
- `deconflictParallelEdges()` (~45 lines)
- `deconflictFanEdges()` (~55 lines)
- `routeThroughTargetIngressBand()` (~28 lines)
- `applyTargetIngressBands()` (~52 lines)
- `hashName()` (~10 lines)

**Total deleted from `extension.ts`:** ~425 lines

#### B2. Flow ELK edge routing to the webview

`computeLayoutForSnapshot()` already extracts ELK's edge sections with start/bend/end points. Currently these are sent as `layout.raw.edges` but discarded by the optimized path. The fix:

```typescript
// In computeLayoutForSnapshot(), after extracting ELK edges:
// Remove the organic optimizer call entirely.
// Return a single layout (not a raw/optimized pair).

const padding = 40;
const width = Math.max(600, Number(layoutResult.width ?? 0) + padding * 2);
const height = Math.max(300, Number(layoutResult.height ?? 0) + padding * 2);

return { width, height, nodes, edges };
```

In the webview, replace `buildEdgePath()` with a function that uses precomputed paths:

```javascript
function getEdgePath(transition, from, to, transitionIndex) {
  // Use ELK's precomputed path if available
  const precomputed = transitionPaths.get(transitionIndex);
  if (precomputed) {
    return precomputed;
  }

  // Self-loop fallback (ELK handles these, but just in case)
  if (transition.from === transition.to) {
    const loopReach = 60;
    return `M ${from.x + 56} ${from.y - 8} C ${from.x + 56 + loopReach} ${from.y - 50}, ${from.x + 56 + loopReach} ${from.y + 46}, ${from.x + 44} ${from.y + 16}`;
  }

  // Simple fallback: straight line
  return `M ${from.x} ${from.y} L ${to.x} ${to.y}`;
}
```

#### B3. Convert ELK polylines to smooth splines

ELK's `SPLINES` routing gives smooth curves directly. For `ORTHOGONAL` routing, ELK returns bend points that should be rendered as polylines (with `L` commands), which is actually correct — orthogonal edges are meant to have right angles.

When using `SPLINES`, replace the current `pointsToPath()` (which uses `L` line commands) with Catmull-Rom → cubic Bézier conversion:

```javascript
function pointsToSmoothPath(points) {
  if (points.length < 2) return '';
  if (points.length === 2) {
    return `M ${points[0].x} ${points[0].y} L ${points[1].x} ${points[1].y}`;
  }

  let d = `M ${points[0].x} ${points[0].y}`;

  for (let i = 0; i < points.length - 1; i++) {
    const p0 = points[Math.max(0, i - 1)];
    const p1 = points[i];
    const p2 = points[i + 1];
    const p3 = points[Math.min(points.length - 1, i + 2)];

    // Catmull-Rom to cubic Bézier control points (alpha = 0.5)
    const cp1x = p1.x + (p2.x - p0.x) / 6;
    const cp1y = p1.y + (p2.y - p0.y) / 6;
    const cp2x = p2.x - (p3.x - p1.x) / 6;
    const cp2y = p2.y - (p3.y - p1.y) / 6;

    d += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${p2.x} ${p2.y}`;
  }

  return d;
}
```

#### B4. Tune ELK options for state machines

Replace all five mode option sets with state-machine-optimized defaults:

```typescript
function getElkLayoutOptions(mode: PreviewLayoutMode): Record<string, string> {
  const direction = mode === "top-down" ? "DOWN" : "RIGHT";
  const nodeSpacing = mode === "compact" ? "42" : mode === "spacious" ? "80" : "55";
  const layerSpacing = mode === "compact" ? "70" : mode === "spacious" ? "140" : "90";

  return {
    "elk.algorithm": "layered",
    "elk.direction": direction,
    "elk.spacing.nodeNode": nodeSpacing,
    "elk.layered.spacing.nodeNodeBetweenLayers": layerSpacing,
    "elk.layered.spacing.edgeNodeBetweenLayers": "30",
    "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
    "elk.layered.nodePlacement.strategy": "NETWORK_SIMPLEX",
    "elk.edgeRouting": "SPLINES",
    "elk.layered.mergeEdges": "false",
    "elk.layered.feedbackEdges": "true",        // Handles cycles (e.g., Yellow→Red)
    "elk.separateConnectedComponents": "false",
    "elk.layered.cycleBreaking.strategy": "INTERACTIVE",
    "elk.insideSelfLoops.activate": "true",      // Proper self-loop routing
    "elk.edgeLabels.inline": "true",             // ELK places edge labels
    "elk.layered.considerModelOrder.strategy": "NODES_AND_EDGES"  // Respects DSL declaration order
  };
}
```

Key additions:
- **`feedbackEdges: true`** — tells ELK to handle cycles (Yellow→Red is a backward edge in a layered layout) instead of reversing them, producing more intuitive cycle visualization.
- **`insideSelfLoops.activate: true`** — makes ELK route self-loops (FlashingRed on Advance) properly instead of ignoring them.
- **`edgeLabels.inline: true`** — ELK computes optimal label positions, eliminating the need for `getPointOnPath()` and `getChipPathFraction()`.
- **`considerModelOrder`** — respects the order states/transitions appear in the DSL, producing more predictable layouts.

#### B5. Dynamic node sizing

**Extension side** — pass consistent sizes to ELK:

```typescript
function computeNodeSize(stateName: string): { width: number; height: number } {
  const charWidth = 8.5;  // Approximate for 13px Segoe UI
  const horizontalPadding = 36;
  const width = Math.max(80, Math.round(stateName.length * charWidth + horizontalPadding));
  const height = 40;
  return { width, height };
}
```

**Webview side** — render at actual size instead of hardcoded 112×44:

```javascript
// Pass node sizes from extension to webview alongside positions
// In SVG rendering:
const nodeSize = nodeSizes[name] ?? { width: 112, height: 40 };
const halfW = nodeSize.width / 2;
const halfH = nodeSize.height / 2;
const rx = halfH;  // Pill shape

return `<rect x="${pos.x - halfW}" y="${pos.y - halfH}" width="${nodeSize.width}" height="${nodeSize.height}" rx="${rx}" .../>`;
```

#### B6. Responsive viewBox

Replace fixed 920×430 with ELK's computed dimensions:

```typescript
// In computeLayoutForSnapshot():
const padding = 50;
const width = Number(layoutResult.width ?? 0) + padding * 2;
const height = Number(layoutResult.height ?? 0) + padding * 2;
// No Math.max clamping — let the CSS container handle minimum display size
```

In the webview CSS, use `preserveAspectRatio="xMidYMid meet"` on the SVG to center small diagrams.

#### B7. Refactor `drawDiagram()` into composable functions

Break the monolithic function into focused pieces:

```javascript
function drawDiagram() {
  const visualState = computeVisualState();  // evaluations, selection, hover

  if (stateChangeAnimation) {
    drawAnimatedDiagram(visualState);
  } else {
    drawStaticDiagram(visualState);
  }
}

function drawStaticDiagram(visualState) {
  const defs = buildSvgDefs();
  const edgeSvg = renderEdges(visualState);
  const annotationSvg = renderAnnotations(visualState);
  const nodeSvg = renderNodes(visualState);
  diagram.innerHTML = `${defs}${edgeSvg}${annotationSvg}${nodeSvg}`;
}

function drawAnimatedDiagram(visualState) {
  const defs = buildSvgDefs();
  const mutedEdgeSvg = renderMutedEdges(visualState);
  const fromEdgeSvg = renderAnimatedFromEdges(visualState);
  const transitionEffectSvg = renderTransitionEffect(visualState);
  const nodeSvg = renderAnimatedNodes(visualState);
  diagram.innerHTML = `${defs}${mutedEdgeSvg}${fromEdgeSvg}${transitionEffectSvg}${nodeSvg}`;
}

function renderEdges(visualState) {
  return transitions.map((t, index) => {
    const pathD = getEdgePath(t, statePositions[t.from], statePositions[t.to], index);
    const visual = computeEdgeVisual(t, index, visualState);
    return `<path d="${pathD}" fill="none" stroke="${visual.stroke}" ... />`;
  }).join('');
}

// etc.
```

### Pros

- **Preserves animation system.** Runner dots, glow effects, handoff rings, state transition animation all continue to work with minimal changes.
- **Preserves semantic edge coloring.** The enabled/blocked/muted visual logic is untouched.
- **Dramatic quality improvement.** ELK's layered algorithm with proper options produces layouts comparable to Mermaid/Dagre.
- **Large code reduction.** Removes ~425 lines of compensatory post-processing from `extension.ts` and ~300 lines of `buildEdgePath()` + deconfliction from the webview.
- **Proper edge routing.** ELK handles crossing minimization, self-loops, backward edges, and parallel edges correctly.
- **No new dependencies.** `elkjs` is already in the project.
- **Incremental approach.** Each sub-step (B1–B7) can be done and tested independently.

### Cons

- **ELK bundle size.** `elkjs` is ~1.2MB (bundled). Already paid, but larger than Dagre.
- **ELK configuration complexity.** ELK has hundreds of options. Finding the right combination for state machines requires experimentation.
- **Async layout.** ELK runs layout asynchronously (returns a Promise). Layout computation on every keystroke needs debouncing (already implemented via snapshot sequencing).
- **Self-loop quality depends on ELK version.** Some ELK versions have mediocre self-loop routing. Needs testing with current `elkjs` version.
- **Label placement.** ELK's `edgeLabels.inline` places labels, but extracting label positions from ELK's output requires parsing `edge.labels[].x/y` — not currently done.

### Complexity Estimate

- **Implementation effort:** Medium (2–4 days)
- **Risk:** Low — incremental changes to existing working system
- **Ongoing maintenance:** Low — ELK handles the hard graph problems

---

## Option C: Replace ELK with Dagre

### Summary

Replace `elkjs` with [`@dagrejs/dagre`](https://github.com/dagrejs/dagre), the same layout engine that Mermaid uses internally. Dagre is simpler, smaller, and produces excellent layered layouts for state-machine-scale graphs. Keeps full control over SVG rendering and animation.

### How It Works

#### C1. Replace `elkjs` with `@dagrejs/dagre`

```bash
cd tools/Precept.VsCode
npm uninstall elkjs
npm install @dagrejs/dagre
```

#### C2. New layout function

Replace `computeLayoutForSnapshot()` and all its helpers:

```typescript
import dagre from '@dagrejs/dagre';

interface DagreLayoutResult {
  width: number;
  height: number;
  nodes: Record<string, { x: number; y: number; width: number; height: number }>;
  edges: Array<{
    transitionIndex: number;
    points: Array<{ x: number; y: number }>;
    labelPosition?: { x: number; y: number };
  }>;
}

function getLayoutDirection(mode: PreviewLayoutMode): string {
  return mode === "top-down" ? "TB" : "LR";
}

function getLayoutSpacing(mode: PreviewLayoutMode): { ranksep: number; nodesep: number; edgesep: number } {
  switch (mode) {
    case "compact":
      return { ranksep: 60, nodesep: 35, edgesep: 15 };
    case "spacious":
      return { ranksep: 120, nodesep: 70, edgesep: 30 };
    default:
      return { ranksep: 80, nodesep: 50, edgesep: 20 };
  }
}

function computeNodeSize(stateName: string): { width: number; height: number } {
  const charWidth = 8.5;
  const padding = 36;
  return {
    width: Math.max(80, Math.round(stateName.length * charWidth + padding)),
    height: 40
  };
}

function computeLayoutForSnapshot(snapshot: Record<string, unknown>): DagreLayoutResult | undefined {
  const states = extractSnapshotStates(snapshot);
  if (states.length === 0) {
    return undefined;
  }

  const transitions = extractSnapshotTransitions(snapshot);
  const layoutMode = getPreviewLayoutMode();
  const spacing = getLayoutSpacing(layoutMode);

  const g = new dagre.graphlib.Graph({ multigraph: true });
  g.setGraph({
    rankdir: getLayoutDirection(layoutMode),
    ranksep: spacing.ranksep,
    nodesep: spacing.nodesep,
    edgesep: spacing.edgesep,
    marginx: 40,
    marginy: 40
  });
  g.setDefaultEdgeLabel(() => ({}));

  // Add nodes with measured sizes
  for (const state of states) {
    const size = computeNodeSize(state);
    g.setNode(state, { width: size.width, height: size.height });
  }

  // Add edges with labels — use multigraph to handle parallel edges
  for (let i = 0; i < transitions.length; i++) {
    const t = transitions[i];
    const labelWidth = Math.max(32, t.event.length * 7 + 16);
    g.setEdge(t.from, t.to, {
      label: t.event,
      width: labelWidth,
      height: 16,
      transitionIndex: i
    }, `transition-${i}`);  // Named edge for multigraph
  }

  dagre.layout(g);

  // Extract results
  const graphInfo = g.graph();
  const nodes: Record<string, { x: number; y: number; width: number; height: number }> = {};
  for (const state of states) {
    const node = g.node(state);
    nodes[state] = {
      x: node.x,
      y: node.y,
      width: node.width,
      height: node.height
    };
  }

  const edges: DagreLayoutResult['edges'] = [];
  for (const edgeObj of g.edges()) {
    const edgeData = g.edge(edgeObj);
    edges.push({
      transitionIndex: edgeData.transitionIndex,
      points: edgeData.points.map((p: { x: number; y: number }) => ({ x: p.x, y: p.y })),
      labelPosition: edgeData.x !== undefined && edgeData.y !== undefined
        ? { x: edgeData.x, y: edgeData.y }
        : undefined
    });
  }

  return {
    width: graphInfo.width ?? 600,
    height: graphInfo.height ?? 300,
    nodes,
    edges
  };
}
```

#### C3. Self-loop handling

Dagre doesn't natively route self-loops. Add explicit handling:

```typescript
// After dagre.layout(g), handle self-loops:
for (let i = 0; i < transitions.length; i++) {
  const t = transitions[i];
  if (t.from !== t.to) continue;

  const node = g.node(t.from);
  const halfW = node.width / 2;
  const loopReach = 55;

  edges.push({
    transitionIndex: i,
    points: [
      { x: node.x + halfW, y: node.y - 6 },
      { x: node.x + halfW + loopReach, y: node.y - 40 },
      { x: node.x + halfW + loopReach, y: node.y + 36 },
      { x: node.x + halfW - 12, y: node.y + node.height / 2 }
    ],
    labelPosition: { x: node.x + halfW + loopReach + 10, y: node.y }
  });
}
```

#### C4. Edge smoothing

Dagre returns control points as an array of `{x, y}` objects. Convert to smooth SVG paths using the same Catmull-Rom → Bézier technique described in Option B (B3).

#### C5. Webview changes

Same as Option B: remove `buildEdgePath()`, use precomputed paths, make node sizes dynamic, update viewBox. The refactoring of `drawDiagram()` (B7) applies equally.

### Functions Deleted from `extension.ts`

Everything from `getElkEngine()` through `normalizeLayoutBounds()` plus `createOrganicOptimizedLayout()` — the entire ~500-line block. Replaced by the ~80-line Dagre function above.

### Pros

- **Smaller bundle.** `@dagrejs/dagre` is ~30KB minified (vs. ~1.2MB for `elkjs`). Significant improvement to extension load time.
- **Synchronous layout.** Dagre runs synchronously — no Promises, no race conditions, simpler code. (ELK is async because it supports web workers.)
- **Simpler API.** Dagre has ~10 configuration options vs. ELK's ~200. Less to get wrong.
- **Proven quality.** This is literally what Mermaid uses. The layout quality for state-machine-scale graphs is excellent.
- **Multigraph support.** Dagre's multigraph mode handles parallel edges (e.g., multiple transitions between the same pair of states) correctly out of the box.
- **Label positioning.** Dagre computes edge label positions (x, y) directly — no `getPointOnPath()` heuristics needed.
- **Preserves animation.** Same as Option B — full control over SVG rendering means animation system is preserved.
- **Preserves semantic styling.** Same as Option B.
- **Dramatic code reduction.** ~500 lines of extension layout code → ~80 lines.

### Cons

- **No self-loop routing.** Dagre does not natively support self-loop edges. Must be handled with custom code (see C3 above). This is manageable but adds a maintenance point.
- **Simpler algorithm.** Dagre uses a basic layered layout (Sugiyama). ELK's layered algorithm is more sophisticated with better options for edge routing, constraint handling, and port placement. For complex machines (20+ states), ELK may produce meaningfully better results.
- **No spline routing.** Dagre returns control points as a polyline. Smooth curves require post-processing (Catmull-Rom conversion). ELK can return proper splines directly.
- **Less active development.** Dagre development is slow. `@dagrejs/dagre` is a maintained fork, but feature additions are rare. ELK receives regular updates from the Eclipse team.
- **No orthogonal routing.** Dagre only supports polyline edges. If orthogonal (right-angle) routing is desired, it must be implemented manually or by switching to ELK.
- **Dependency change.** Removing `elkjs` and adding `@dagrejs/dagre` changes the dependency tree. Need to verify compatibility with the extension bundling (esbuild/webpack).
- **Backward-edge handling.** Dagre reverses backward edges during layout and then un-reverses them. For state machines with cycles (which are common), this can sometimes produce unintuitive edge directions. ELK's `feedbackEdges` option handles this more gracefully.

### Complexity Estimate

- **Implementation effort:** Medium (2–3 days)
- **Risk:** Low-medium — well-understood library, but self-loop and backward-edge handling need careful testing
- **Ongoing maintenance:** Low

---

## Comparison Matrix

| Criterion | Option A (Mermaid) | Option B (Fix ELK) | Option C (Dagre) |
|---|---|---|---|
| **Layout quality** | Excellent (Dagre inside) | Excellent (ELK layered) | Excellent (Dagre direct) |
| **Edge routing** | Excellent (auto) | Excellent (ELK splines) | Good (polyline + smoothing) |
| **Self-loop handling** | Automatic | Requires ELK option tuning | Manual implementation needed |
| **Parallel edge handling** | Automatic | Automatic (ELK) | Automatic (multigraph) |
| **Backward edge / cycles** | Automatic | Excellent (`feedbackEdges`) | Adequate (edge reversal) |
| **Label placement** | Automatic | Good (ELK labels) | Good (Dagre label x/y) |
| **Semantic edge coloring** | Hard (SVG post-processing) | Native (full control) | Native (full control) |
| **Transition animation** | Lost | Preserved | Preserved |
| **Runner dot / glow effects** | Lost | Preserved | Preserved |
| **Interactive hover/select** | Fragile (DOM scraping) | Native | Native |
| **Bundle size impact** | +1.5MB (Mermaid) | No change (ELK already present) | -1.17MB (ELK→Dagre swap) |
| **Lines removed** | ~1800 | ~725 | ~500 (net, after new code) |
| **Lines added** | ~120 | ~150 | ~180 |
| **Implementation effort** | 1–2 days | 2–4 days | 2–3 days |
| **Risk** | Medium | Low | Low-medium |
| **Orthogonal routing** | No (Mermaid limitation) | Yes (ELK native) | No |
| **Extensibility** | Limited by Mermaid API | High (full ELK config) | Moderate |
| **Stack simplicity** | Simplest (delegate everything) | Most complex (ELK config surface) | Middle ground |

---

## Decision

**Option B (Fix ELK) was selected and implemented** (2026-03-03). All seven sub-steps (B1–B7) are complete:

- Removed the force-directed optimizer and all compensatory post-processing (~425 lines deleted from `extension.ts`)
- ELK edge routing data flows through to the webview intact
- Catmull-Rom → cubic Bézier spline conversion in the webview replaces hand-coded Bézier heuristics
- ELK options tuned for state machines: top-down direction, SPLINES routing, `feedbackEdges`, `insideSelfLoops`, `edgeLabels.inline`, `considerModelOrder`
- Dynamic node sizing (8.5px char width, 36px horizontal padding, 80px minimum, 40px height)
- Responsive viewBox from ELK-computed dimensions with 50px padding, no min-size clamping
- `drawDiagram()` architecture preserved; no structural refactor beyond removing dead code

The rationale below explains why this option was chosen over the alternatives.

## Original Recommendation

**Option B (Fix ELK)** was recommended for this project because:

1. It preserves the animation system, which is a significant differentiator for the Inspector Preview.
2. It preserves full control over semantic styling (enabled/blocked/muted edges), which is core to the Inspector's value.
3. It requires no new dependencies — `elkjs` is already installed and working.
4. The core problem is not ELK itself but the post-processing that discards ELK's output. Fixing that is lower-risk than replacing the layout engine entirely.
5. ELK handles state-machine cycles and backward edges better than Dagre thanks to `feedbackEdges`.
6. Each sub-step (B1–B7) can be implemented and tested independently — minimal blast radius.

Option C (Dagre) is a strong runner-up if `elkjs` bundle size becomes a concern or if ELK's configuration complexity proves problematic in practice.

Option A (Mermaid) would only be preferred if the animation and semantic styling features were deemed unnecessary — which conflicts with the Inspector Preview's core design goals.
