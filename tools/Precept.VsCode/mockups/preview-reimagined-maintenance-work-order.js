(function () {
  const FIELD_ORDER = [
    "RequesterName",
    "Location",
    "IssueSummary",
    "AssignedTechnician",
    "EstimatedHours",
    "ActualHours",
    "Urgent",
    "PartsApproved",
    "CompletionNote",
    "CancellationReason",
  ];

  const FIELD_LABELS = {
    RequesterName: "Requester",
    Location: "Location",
    IssueSummary: "Issue",
    AssignedTechnician: "Technician",
    EstimatedHours: "Estimate",
    ActualHours: "Actual",
    Urgent: "Urgent",
    PartsApproved: "Parts Approved",
    CompletionNote: "Completion Note",
    CancellationReason: "Cancellation Reason",
  };

  const STATE_DESCRIPTIONS = {
    Draft: "Draft work order before submission.",
    Open: "Accepted and waiting for assignment.",
    Scheduled: "Assigned and ready to start once conditions hold.",
    InProgress: "Work is underway.",
    Completed: "Closed with a completion note.",
    Cancelled: "Closed without execution.",
  };

  const TRANSITIONS = [
    { from: "Draft", to: "Open", label: "Submit" },
    { from: "Open", to: "Scheduled", label: "Assign" },
    { from: "Open", to: "Cancelled", label: "Cancel" },
    { from: "Scheduled", to: "InProgress", label: "StartWork" },
    { from: "Scheduled", to: "Cancelled", label: "Cancel" },
    { from: "Scheduled", to: "Scheduled", label: "ApproveParts" },
    { from: "InProgress", to: "InProgress", label: "RecordProgress" },
    { from: "InProgress", to: "Completed", label: "Complete" },
  ];

  const BASE_SIM = {
    currentState: "Scheduled",
    data: {
      RequesterName: "Marta Silva",
      Location: "Plant 2 / Chiller Loop",
      IssueSummary: "Compressor trips every 15 minutes",
      AssignedTechnician: "Ari Patel",
      EstimatedHours: 6,
      ActualHours: 0,
      Urgent: true,
      PartsApproved: false,
      CompletionNote: null,
      CancellationReason: null,
    },
    history: [
      {
        step: 1,
        event: "Submit",
        outcome: "transition",
        from: "Draft",
        to: "Open",
        summary: "Marta filed the repair request.",
        changes: [
          { field: "RequesterName", before: null, after: "Marta Silva" },
          { field: "Location", before: null, after: "Plant 2 / Chiller Loop" },
          { field: "IssueSummary", before: null, after: "Compressor trips every 15 minutes" },
          { field: "Urgent", before: false, after: true },
        ],
      },
      {
        step: 2,
        event: "Assign",
        outcome: "transition",
        from: "Open",
        to: "Scheduled",
        summary: "Ari took the job with a six hour estimate.",
        changes: [
          { field: "AssignedTechnician", before: null, after: "Ari Patel" },
          { field: "EstimatedHours", before: 0, after: 6 },
        ],
      },
    ],
    changedFields: ["AssignedTechnician", "EstimatedHours"],
  };

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function esc(value) {
    return String(value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/\"/g, "&quot;");
  }

  function formatValue(value) {
    if (value === null || value === undefined) return "null";
    if (typeof value === "boolean") return value ? "true" : "false";
    return String(value);
  }

  function lastStep(sim) {
    return sim.history.length ? sim.history[sim.history.length - 1].step : 0;
  }

  function buildRules(sim, actionId, nextData) {
    const urgentGateOpen = !nextData.Urgent || nextData.PartsApproved;
    const estimateWindowOk = nextData.ActualHours <= nextData.EstimatedHours + 4;
    const noteReady = nextData.CompletionNote !== null;
    const stateRule = sim.currentState === "Scheduled"
      ? nextData.AssignedTechnician !== null
      : (sim.currentState !== "Completed" || noteReady);

    const rules = [
      {
        label: "Urgent gate",
        expr: "!Urgent || PartsApproved",
        ok: urgentGateOpen,
        detail: urgentGateOpen ? "StartWork is allowed." : "Urgent work still needs parts approval.",
      },
      {
        label: "Estimate window",
        expr: "ActualHours <= EstimatedHours + 4",
        ok: estimateWindowOk,
        detail: estimateWindowOk ? "Complete is still within the tolerance window." : "Completion now needs an extra review lane.",
      },
      {
        label: "Completion note",
        expr: "Completed => CompletionNote != null",
        ok: sim.currentState !== "Completed" || noteReady,
        detail: noteReady ? "A completion note exists once the order closes." : "A completion note is still missing.",
      },
      {
        label: "Scheduled ownership",
        expr: "Scheduled => AssignedTechnician != null",
        ok: stateRule,
        detail: stateRule ? "The scheduled order has an owner." : "Scheduled work cannot exist without a technician.",
      },
    ];

    if (actionId === "startWork") {
      rules[0].detail = urgentGateOpen ? "The gate is open for StartWork." : "StartWork will reject until PartsApproved turns true.";
    }
    if (actionId === "complete") {
      rules[1].detail = estimateWindowOk ? "Complete can close the order right now." : "Complete will reject because the overrun is too high.";
    }

    return rules;
  }

  function buildAction(sim, spec) {
    const nextData = clone(sim.data);
    spec.apply(nextData);
    const rules = buildRules(sim, spec.id, nextData);
    return {
      id: spec.id,
      label: spec.label,
      kind: spec.kind,
      enabled: spec.enabled,
      reason: spec.reason,
      summary: spec.summary,
      changes: spec.changes,
      nextState: spec.nextState,
      nextData,
      rules,
      commit(targetSim) {
        spec.apply(targetSim.data);
        targetSim.currentState = spec.nextState;
        targetSim.history.push({
          step: lastStep(targetSim) + 1,
          event: spec.label,
          outcome: spec.kind,
          from: targetSim.currentStateBefore,
          to: spec.nextState,
          summary: spec.summary,
          changes: clone(spec.changes),
        });
        targetSim.changedFields = spec.changes.map(change => change.field);
      },
    };
  }

  function availableActions(sim) {
    const actions = [];

    if (sim.currentState === "Scheduled") {
      actions.push(buildAction(sim, {
        id: "approveParts",
        label: "ApproveParts",
        kind: "no-transition",
        enabled: !sim.data.PartsApproved,
        reason: sim.data.PartsApproved ? "Parts are already approved." : "Turns the urgent-work gate green.",
        summary: "Parts approval clears the urgent-work gate.",
        nextState: "Scheduled",
        changes: [{ field: "PartsApproved", before: sim.data.PartsApproved, after: true }],
        apply(nextData) {
          nextData.PartsApproved = true;
        },
      }));

      const canStart = !sim.data.Urgent || sim.data.PartsApproved;
      actions.push(buildAction(sim, {
        id: "startWork",
        label: "StartWork",
        kind: canStart ? "transition" : "rejected",
        enabled: canStart,
        reason: canStart ? "Moves the order into active work." : "Urgent work must approve parts before it starts.",
        summary: canStart ? "The work order moves into active execution." : "StartWork rejects until parts are approved.",
        nextState: canStart ? "InProgress" : "Scheduled",
        changes: [],
        apply() {},
      }));

      actions.push(buildAction(sim, {
        id: "cancel",
        label: "Cancel",
        kind: "transition",
        enabled: true,
        reason: "Closes the order without execution.",
        summary: "The work order is cancelled before execution.",
        nextState: "Cancelled",
        changes: [{ field: "CancellationReason", before: sim.data.CancellationReason, after: "Vendor outage" }],
        apply(nextData) {
          nextData.CancellationReason = "Vendor outage";
        },
      }));
    }

    if (sim.currentState === "InProgress") {
      actions.push(buildAction(sim, {
        id: "recordProgress",
        label: "RecordProgress",
        kind: "no-transition",
        enabled: true,
        reason: "Adds two hours of execution time.",
        summary: "Progress is recorded without changing state.",
        nextState: "InProgress",
        changes: [{ field: "ActualHours", before: sim.data.ActualHours, after: sim.data.ActualHours + 2 }],
        apply(nextData) {
          nextData.ActualHours += 2;
        },
      }));

      const canComplete = sim.data.ActualHours <= sim.data.EstimatedHours + 4;
      actions.push(buildAction(sim, {
        id: "complete",
        label: "Complete",
        kind: canComplete ? "transition" : "rejected",
        enabled: canComplete,
        reason: canComplete ? "Closes the order with a note." : "Actual hours exceed the estimate window.",
        summary: canComplete ? "The work order closes with a completion note." : "Complete rejects because the estimate overrun is too high.",
        nextState: canComplete ? "Completed" : "InProgress",
        changes: [{ field: "CompletionNote", before: sim.data.CompletionNote, after: "Replaced the compressor relay and pressure-tested the loop." }],
        apply(nextData) {
          nextData.CompletionNote = "Replaced the compressor relay and pressure-tested the loop.";
        },
      }));
    }

    return actions;
  }

  function fieldDetails(sim, selectedField) {
    return FIELD_ORDER.map((name) => {
      let provenance = "Seed data";
      for (let index = sim.history.length - 1; index >= 0; index -= 1) {
        const change = sim.history[index].changes.find((entry) => entry.field === name);
        if (change) {
          provenance = `Step ${sim.history[index].step} - ${sim.history[index].event}`;
          break;
        }
      }
      return {
        name,
        label: FIELD_LABELS[name],
        value: sim.data[name],
        selected: name === selectedField,
        changed: (sim.changedFields || []).includes(name),
        provenance,
      };
    });
  }

  function makeStateNodes(sim) {
    const visited = new Set(["Draft"]);
    sim.history.forEach((entry) => {
      visited.add(entry.from);
      visited.add(entry.to);
    });
    return ["Draft", "Open", "Scheduled", "InProgress", "Completed", "Cancelled"].map((name) => ({
      name,
      current: name === sim.currentState,
      visited: visited.has(name),
      terminal: name === "Completed" || name === "Cancelled",
      description: STATE_DESCRIPTIONS[name],
    }));
  }

  window.createMaintenanceWorkOrderDemo = function createMaintenanceWorkOrderDemo() {
    let sim = clone(BASE_SIM);
    let snapshots = [clone(BASE_SIM)];
    let selectedAction = "startWork";
    let selectedField = "PartsApproved";
    const listeners = [];

    function currentActions() {
      const actions = availableActions(sim);
      if (!actions.find((action) => action.id === selectedAction)) {
        selectedAction = actions.length ? actions[0].id : null;
      }
      return actions;
    }

    function getView() {
      const actions = currentActions();
      const preview = actions.find((action) => action.id === selectedAction) || null;
      return {
        sim: clone(sim),
        actions,
        preview,
        selectedAction,
        selectedField,
        fields: fieldDetails(sim, selectedField),
        stateNodes: makeStateNodes(sim),
        transitions: TRANSITIONS,
      };
    }

    function notify() {
      const view = getView();
      listeners.forEach((listener) => listener(view));
    }

    function pushSnapshot() {
      snapshots.push(clone(sim));
    }

    function commitAction(id) {
      const action = currentActions().find((entry) => entry.id === id);
      if (!action || !action.enabled) {
        notify();
        return;
      }
      sim.currentStateBefore = sim.currentState;
      action.commit(sim);
      delete sim.currentStateBefore;
      pushSnapshot();
      notify();
    }

    function reset() {
      sim = clone(BASE_SIM);
      snapshots = [clone(BASE_SIM)];
      selectedAction = "startWork";
      selectedField = "PartsApproved";
      notify();
    }

    function undo() {
      if (snapshots.length <= 1) return;
      snapshots.pop();
      sim = clone(snapshots[snapshots.length - 1]);
      notify();
    }

    function toggleUrgent() {
      const before = sim.data.Urgent;
      sim.data.Urgent = !sim.data.Urgent;
      sim.history.push({
        step: lastStep(sim) + 1,
        event: "ToggleUrgent",
        outcome: "no-transition",
        from: sim.currentState,
        to: sim.currentState,
        summary: sim.data.Urgent ? "The order is now marked urgent." : "The order is no longer urgent.",
        changes: [{ field: "Urgent", before, after: sim.data.Urgent }],
      });
      sim.changedFields = ["Urgent"];
      pushSnapshot();
      notify();
    }

    function setEstimate(rawValue) {
      const value = Math.max(1, Number(rawValue) || 1);
      if (value === sim.data.EstimatedHours) return;
      const before = sim.data.EstimatedHours;
      sim.data.EstimatedHours = value;
      sim.history.push({
        step: lastStep(sim) + 1,
        event: "AdjustEstimate",
        outcome: "no-transition",
        from: sim.currentState,
        to: sim.currentState,
        summary: `Estimated hours changed from ${before} to ${value}.`,
        changes: [{ field: "EstimatedHours", before, after: value }],
      });
      sim.changedFields = ["EstimatedHours"];
      pushSnapshot();
      notify();
    }

    function setSelectedAction(id) {
      selectedAction = id;
      notify();
    }

    function selectField(name) {
      selectedField = name;
      notify();
    }

    return {
      subscribe(listener) {
        listeners.push(listener);
        listener(getView());
      },
      getView,
      commitAction,
      reset,
      undo,
      toggleUrgent,
      setEstimate,
      setSelectedAction,
      selectField,
    };
  };

  window.mwoEsc = esc;
  window.mwoFormatValue = formatValue;
})();