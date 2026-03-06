# Playwright Preview UI Testing — Setup & Execution Guide

## Overview

The Precept preview inspector is a standalone HTML file (`tools/Precept.VsCode/webview/inspector-preview.html`) that communicates with VS Code via `postMessage`. Because the HTML is self-contained, it can be loaded in a headless browser and driven with Playwright — no VS Code instance required.

The key insight is that the webview calls `acquireVsCodeApi()` on load and then exchanges messages with the host via `vscodeApi.postMessage()` (outbound) and `window.addEventListener('message', ...)` (inbound). By mocking `acquireVsCodeApi` and injecting snapshot/response messages, we can fully exercise the UI.

---

## Architecture

```
┌────────────────────────┐       postMessage        ┌──────────────────────┐
│  inspector-preview.html│  ←──────────────────────  │  Playwright test     │
│  (loaded in Chromium)  │  ──────────────────────→  │  (mock host)         │
│                        │       postMessage        │                      │
│  acquireVsCodeApi()    │                          │  Intercepts outbound │
│    → mock object       │                          │  Sends inbound       │
└────────────────────────┘                          └──────────────────────┘
```

### Webview message protocol

**Outbound (webview → host):**
- `{ type: "ready" }` — sent on load, requests initial snapshot
- `{ type: "previewRequest", requestId: number, action: "snapshot"|"fire"|"reset"|"replay"|"inspect", eventName?, args?, steps? }`

**Inbound (host → webview):**
- `{ type: "snapshot", snapshotSequence: number, success: bool, snapshot: SnapshotObject }` — pushed on document change
- `{ type: "previewResponse", requestId: number, success: bool, snapshot?, error?, inspectResult?, errors? }` — response to a previewRequest

### Snapshot shape

```json
{
  "workflowName": "BugTracker",
  "currentState": "Triage",
  "states": ["Triage", "Open", "InProgress", "Blocked", "InReview", "Resolved", "Closed"],
  "transitions": [
    { "from": "Triage", "to": "Open", "event": "Assign", "guardExpression": null, "kind": "transition" }
  ],
  "events": [
    { "name": "Assign", "outcome": "enabled", "targetState": "Open", "reasons": [],
      "args": [{ "name": "User", "type": "string", "isNullable": false, "hasDefaultValue": false, "defaultValue": null }] }
  ],
  "data": { "Assignee": null, "Priority": 3, "BlockReason": null, "Resolution": null },
  "diagnostics": [],
  "ruleDefinitions": [
    { "scope": "field:Priority", "expression": "Priority >= 1 && Priority <= 5", "reason": "Priority must be between 1 and 5" }
  ]
}
```

---

## Key DOM selectors

| Element | Selector | Notes |
|---|---|---|
| Current state display | `.status` | Shows workflow name and current state |
| Reset button | `#reset-btn` | Sends `{ action: "reset" }` |
| Event buttons | `.event-btn[data-event="EventName"]` | Click to fire; hover to highlight diagram edges |
| Event arg inputs | `input[data-event="EventName"][data-event-arg="ArgName"]` | Text inputs for event arguments |
| Boolean arg buttons | `.bool-seg[data-event="..."][data-event-arg="..."][data-val="true"]` | Boolean toggle segments |
| Null toggle | `.arg-null-btn[data-event="..."][data-event-arg="..."]` | Sets nullable arg to null |
| Data list | `#data-list` | Shows current field values |
| Diagram SVG | `#diagram` | State diagram with nodes and edges |
| Event bar | `#event-bar` | Container for all event buttons and arg fields |
| Rule violations banner | `#rule-violations-banner` | Shown when invariant rules are violated |
| Diagram toast | `#diagram-toast` | Transition feedback messages |

---

## Setup (one-time)

```powershell
# From workspace root
mkdir test/Precept.PlaywrightTests
cd test/Precept.PlaywrightTests
npm init -y
npm install --save-dev @playwright/test
npx playwright install chromium
```

---

## Test anatomy

A test does three things:

1. **Inject the `acquireVsCodeApi` mock** before the page script runs (via `page.addInitScript`)
2. **Capture outbound messages** by having the mock relay them via `window.postMessage` with a distinguishing wrapper
3. **Send inbound messages** by calling `window.postMessage({ type: "snapshot", ... }, "*")` to simulate the host

### Minimal example

```typescript
import { test, expect } from '@playwright/test';
import path from 'path';

const PREVIEW_PATH = path.resolve(__dirname, '../../tools/Precept.VsCode/webview/inspector-preview.html');

// Minimal snapshot for a two-state machine
const SNAPSHOT = {
  workflowName: 'TestMachine',
  currentState: 'A',
  states: ['A', 'B'],
  transitions: [
    { from: 'A', to: 'B', event: 'Go', guardExpression: null, kind: 'transition' },
  ],
  events: [
    { name: 'Go', outcome: 'enabled', targetState: 'B', reasons: [], args: [] },
  ],
  data: {},
  diagnostics: [],
  ruleDefinitions: [],
};

test.describe('Preview Inspector', () => {
  test.beforeEach(async ({ page }) => {
    // Mock acquireVsCodeApi BEFORE the page loads
    await page.addInitScript(() => {
      const messages: any[] = [];
      (window as any).__testMessages = messages;
      (window as any).acquireVsCodeApi = () => ({
        postMessage: (msg: any) => {
          messages.push(msg);
          // Echo back to the page so tests can listen
          window.postMessage({ type: '__test_outbound', payload: msg }, '*');
        },
        getState: () => ({}),
        setState: () => {},
      });
    });

    await page.goto(`file:///${PREVIEW_PATH.replace(/\\/g, '/')}`);

    // Wait for the webview to send "ready", then feed initial snapshot
    await page.waitForFunction(() => (window as any).__testMessages?.some((m: any) => m.type === 'ready'));
    await injectSnapshot(page, SNAPSHOT);
    // Let render settle
    await page.waitForTimeout(100);
  });

  test('shows current state', async ({ page }) => {
    const status = page.locator('.status');
    await expect(status).toContainText('A');
  });

  test('shows event button for Go', async ({ page }) => {
    const goBtn = page.locator('.event-btn[data-event="Go"]');
    await expect(goBtn).toBeVisible();
    await expect(goBtn).toContainText('Go');
  });

  test('clicking Go sends fire request', async ({ page }) => {
    const goBtn = page.locator('.event-btn[data-event="Go"]');

    // Listen for outbound fire message
    const firePromise = page.waitForFunction(() => {
      return (window as any).__testMessages?.find(
        (m: any) => m.type === 'previewRequest' && m.action === 'fire' && m.eventName === 'Go'
      );
    });

    await goBtn.click();
    const result = await firePromise;
    expect(result).toBeTruthy();
  });

  test('fire response updates state display', async ({ page }) => {
    const goBtn = page.locator('.event-btn[data-event="Go"]');
    await goBtn.click();

    // Simulate host responding with updated snapshot
    await injectFireResponse(page, {
      ...SNAPSHOT,
      currentState: 'B',
    });

    await page.waitForTimeout(200); // allow animation
    const status = page.locator('.status');
    await expect(status).toContainText('B');
  });
});

// ── Helpers ──────────────────────────────────────────────────────────

async function injectSnapshot(page: any, snapshot: any, seq = 1) {
  await page.evaluate(
    ([snap, sequence]: [any, number]) => {
      window.postMessage(
        { type: 'snapshot', snapshotSequence: sequence, success: true, snapshot: snap },
        '*'
      );
    },
    [snapshot, seq]
  );
}

async function injectFireResponse(page: any, snapshot: any) {
  // Find the requestId of the last fire request
  const requestId = await page.evaluate(() => {
    const msgs = (window as any).__testMessages ?? [];
    const fire = [...msgs].reverse().find((m: any) => m.action === 'fire');
    return fire?.requestId ?? 1;
  });

  await page.evaluate(
    ([rid, snap]: [number, any]) => {
      window.postMessage(
        { type: 'previewResponse', requestId: rid, success: true, snapshot: snap },
        '*'
      );
    },
    [requestId, snapshot]
  );
}
```

---

## Realistic test scenarios

### Event with arguments (BugTracker: Assign)

```typescript
test('Assign with User arg transitions Triage → Open', async ({ page }) => {
  // Inject BugTracker snapshot (currentState: Triage, Assign event with User arg)
  await injectSnapshot(page, bugTrackerSnapshot);
  await page.waitForTimeout(100);

  // Type into the User arg field
  const userInput = page.locator('input[data-event="Assign"][data-event-arg="User"]');
  await userInput.fill('alice');

  // Click Assign
  await page.locator('.event-btn[data-event="Assign"]').click();

  // Verify outbound message includes args
  const msg = await page.evaluate(() => {
    return (window as any).__testMessages?.find(
      (m: any) => m.action === 'fire' && m.eventName === 'Assign'
    );
  });
  expect(msg.args.User).toBe('alice');
});
```

### Blocked event shows error

```typescript
test('blocked fire shows error feedback', async ({ page }) => {
  await page.locator('.event-btn[data-event="Go"]').click();

  // Simulate blocked response
  const requestId = await page.evaluate(() => {
    const msgs = (window as any).__testMessages ?? [];
    return [...msgs].reverse().find((m: any) => m.action === 'fire')?.requestId ?? 1;
  });

  await page.evaluate(([rid]: [number]) => {
    window.postMessage({
      type: 'previewResponse',
      requestId: rid,
      success: false,
      error: 'Guard failed: Balance >= 0',
      errors: ['Guard failed: Balance >= 0'],
      snapshot: null,
    }, '*');
  }, [requestId]);

  // Error feedback should appear
  await expect(page.locator('.diagram-toast, .event-reason')).toContainText('Guard failed');
});
```

### Reset button

```typescript
test('reset button sends reset action', async ({ page }) => {
  await page.locator('#reset-btn').click();

  const msg = await page.evaluate(() => {
    return (window as any).__testMessages?.find((m: any) => m.action === 'reset');
  });
  expect(msg).toBeTruthy();
});
```

---

## Running

```powershell
cd test/Precept.PlaywrightTests
npx playwright test --headed   # visible browser
npx playwright test             # headless (CI)
```

---

## Building snapshot fixtures from real .precept files

Instead of hand-crafting snapshot JSON, use the existing `PreceptPreviewHandler` from a C# test to generate them:

```csharp
var dsl = File.ReadAllText("samples/bugtracker.precept");
var handler = new PreceptPreviewHandler();
var uri = DocumentUri.From("file:///test.precept");
var response = await handler.Handle(
    new PreceptPreviewRequest("snapshot", uri, Text: dsl),
    CancellationToken.None);
var json = JsonSerializer.Serialize(response.Snapshot, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("test/Precept.PlaywrightTests/fixtures/bugtracker-snapshot.json", json);
```

Then load in Playwright:

```typescript
import snapshot from './fixtures/bugtracker-snapshot.json';
```

---

## Limitations

- **No VS Code theme variables** — CSS custom properties like `--vscode-editor-background` won't resolve. The webview uses its own hardcoded dark theme, so this works fine.
- **No ELK layout** — the diagram layout engine (`elkjs`) is loaded by the VS Code extension and sends layout data via message. Tests should either skip diagram position assertions or include a `layout` field in the snapshot fixture.
- **No real language server** — all responses are mocked. This tests the UI layer only; backend logic is covered by the xUnit `PreceptPreviewHandler` tests.

---

## Parked Status (2026-03-05)

This effort was paused after a repeatable blocker in standalone Playwright mode.

### What was attempted

- Created a temporary harness at `test/Precept.PlaywrightTests/` with `@playwright/test`.
- Implemented a BankLoan smoke test against `tools/Precept.VsCode/webview/inspector-preview.html`.
- Mocked `acquireVsCodeApi`, captured outbound `previewRequest` messages, and replied with `previewResponse` snapshots.
- Tried both message paths:
  - push-style: `{ type: "snapshot", ... }`
  - request/response style: `{ type: "previewRequest", action: "snapshot" }` -> `{ type: "previewResponse", requestId, snapshot }`

### Observed blocker

- The webview consistently rendered:
  - `No events available from current state.`
  - `Layout engine did not produce a result`
- Assertion failure was consistent: `.event-btn[data-event="Submit"]` not found.

### Likely cause

- The standalone HTML flow appears to rely on runtime state that is normally produced in extension-host context (for example, inspect-refresh behavior and/or layout payload cadence), and the mocked messages did not reproduce enough of that lifecycle to populate the event dock.

### Recommended resume path

1. Prefer true extension-host E2E first (`@vscode/test-electron`) to validate the full VS Code -> language server -> webview loop.
2. Export real snapshots from `PreceptPreviewHandler` for `samples/bank-loan.precept` and feed those fixtures to Playwright.
3. In standalone mode, instrument the page during test runs to log internal values (`currentState`, `transitions.length`, `currentEventStatuses.length`, `getOrderedCurrentEvents()`) before asserting event buttons.

### Cleanup completed

- Temporary harness folder `test/Precept.PlaywrightTests/` was removed.
- `.gitignore` was reverted to remove temporary Playwright-specific ignore entries.

---

## Copilot Prompt for Future Sessions

Copy and paste this prompt to have Copilot set up and run the Playwright tests:

> **Prompt:**
>
> Set up and run Playwright UI tests for the Precept preview inspector. Follow the instructions in `docs/PlaywrightPreviewTesting.md`. Steps:
>
> 1. Create `test/Precept.PlaywrightTests/` with `package.json` and install `@playwright/test` + Chromium.
> 2. Generate snapshot fixtures from `samples/bugtracker.precept` and `samples/trafficlight.precept` using the `PreceptPreviewHandler` C# test approach documented in the guide (write a small xUnit test that serializes snapshots to JSON files in `test/Precept.PlaywrightTests/fixtures/`).
> 3. Write Playwright tests in `test/Precept.PlaywrightTests/tests/preview.spec.ts` covering:
>    - Initial render: current state displayed, event buttons visible, data fields shown
>    - Clicking an event button sends the correct `fire` previewRequest with `eventName` and `args`
>    - Typing into arg input fields and verifying args are included in the outbound message
>    - Injecting a successful fire response updates the state display
>    - Injecting a blocked fire response shows error text
>    - Reset button sends reset action
>    - Boolean arg toggle buttons work
>    - Nullable arg null-toggle button works
> 4. Run the tests headless and report results.
>
> The webview is at `tools/Precept.VsCode/webview/inspector-preview.html`. Mock `acquireVsCodeApi` via `page.addInitScript` as described in the doc. Use `window.postMessage` to inject snapshot and response messages. Capture outbound messages via the `__testMessages` array on window.
