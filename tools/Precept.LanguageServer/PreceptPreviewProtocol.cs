using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Precept.LanguageServer;

public sealed record PreceptPreviewRequest(
    string Action,
    DocumentUri Uri,
    string? Text = null,
    string? EventName = null,
    IReadOnlyDictionary<string, object?>? Args = null,
    IReadOnlyList<PreceptPreviewReplayStep>? Steps = null,
    IReadOnlyDictionary<string, object?>? FieldUpdates = null);

public sealed record PreceptPreviewReplayStep(string EventName, IReadOnlyDictionary<string, object?>? Args = null);

public sealed record PreceptPreviewResponse(
    bool Success,
    string? Error = null,
    PreceptPreviewSnapshot? Snapshot = null,
    IReadOnlyList<string>? ReplayMessages = null,
    PreceptPreviewEventStatus? InspectResult = null,
    IReadOnlyList<string>? Errors = null,
    IReadOnlyList<PreceptPreviewEditableField>? EditableFields = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? FieldErrors = null,
    IReadOnlyList<string>? FormErrors = null,
    bool? CanSave = null);

public sealed record PreceptPreviewSnapshot(
    string WorkflowName,
    string? CurrentState,
    string? InitialState,
    IReadOnlyList<string> States,
    IReadOnlyList<PreceptPreviewTransition> Transitions,
    IReadOnlyList<PreceptPreviewEventStatus> Events,
    IReadOnlyDictionary<string, object?> Data,
    IReadOnlyList<PreceptPreviewDiagnostic> Diagnostics,
    IReadOnlyList<string>? ActiveRuleViolations = null,
    IReadOnlyList<PreceptPreviewRuleInfo>? RuleDefinitions = null,
    IReadOnlyList<PreceptPreviewEditableField>? EditableFields = null);

public sealed record PreceptPreviewRuleInfo(string Scope, string Expression, string Reason);

public sealed record PreceptPreviewTransition(
    string From,
    string To,
    string Event,
    string? GuardExpression,
    string Kind);

public sealed record PreceptPreviewEventStatus(
    string Name,
    string Outcome,
    string? TargetState,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<PreceptPreviewEventArg> Args,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? ArgErrors = null,
    IReadOnlyList<string>? EventErrors = null);

public sealed record PreceptPreviewEventArg(string Name, string Type, bool IsNullable, bool HasDefaultValue, object? DefaultValue);

public sealed record PreceptPreviewDiagnostic(string Severity, string Message, int Line, int Character);

public sealed record PreceptPreviewEditableField(string FieldName, string FieldType, bool IsNullable, object? CurrentValue, string? Violation = null);
