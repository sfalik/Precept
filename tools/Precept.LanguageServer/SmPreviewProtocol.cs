using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Precept.LanguageServer;

[Method("precept/preview/request")]
internal sealed record SmPreviewRequest(
    string Action,
    DocumentUri Uri,
    string? Text = null,
    string? EventName = null,
    IReadOnlyDictionary<string, object?>? Args = null,
    IReadOnlyList<SmPreviewReplayStep>? Steps = null) : IRequest<SmPreviewResponse>;

internal sealed record SmPreviewReplayStep(string EventName, IReadOnlyDictionary<string, object?>? Args = null);

internal sealed record SmPreviewResponse(
    bool Success,
    string? Error = null,
    SmPreviewSnapshot? Snapshot = null,
    IReadOnlyList<string>? ReplayMessages = null,
    SmPreviewEventStatus? InspectResult = null,
    IReadOnlyList<string>? Errors = null);

internal sealed record SmPreviewSnapshot(
    string WorkflowName,
    string CurrentState,
    IReadOnlyList<string> States,
    IReadOnlyList<SmPreviewTransition> Transitions,
    IReadOnlyList<SmPreviewEventStatus> Events,
    IReadOnlyDictionary<string, object?> Data,
    IReadOnlyList<SmPreviewDiagnostic> Diagnostics,
    IReadOnlyList<string>? ActiveRuleViolations = null,
    IReadOnlyList<SmPreviewRuleInfo>? RuleDefinitions = null);

internal sealed record SmPreviewRuleInfo(string Scope, string Expression, string Reason);

internal sealed record SmPreviewTransition(
    string From,
    string To,
    string Event,
    string? GuardExpression,
    string Kind);

internal sealed record SmPreviewEventStatus(
    string Name,
    string Outcome,
    string? TargetState,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<SmPreviewEventArg> Args);

internal sealed record SmPreviewEventArg(string Name, string Type, bool IsNullable, bool HasDefaultValue, object? DefaultValue);

internal sealed record SmPreviewDiagnostic(string Severity, string Message, int Line, int Character);
