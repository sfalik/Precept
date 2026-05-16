using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    /// <summary>
    /// CI enforcement sub-pass (Slice 8): validate <c>~string</c> usage consistency.
    /// Walks all resolved expression trees and checks the 5 CI enforcement rules
    /// from the language spec §3.8. Rules 1–2 fire on <c>==</c> / <c>!=</c> with a
    /// <c>~string</c> operand. Rules 3 fires on <c>contains</c> with a <c>~string</c>
    /// value in a case-sensitive collection. Rules 4–5 fire on <c>startsWith</c> /
    /// <c>endsWith</c> with a <c>~string</c> first argument.
    /// </summary>
    private static void ValidateCIEnforcement(CheckContext ctx)
    {
        // Collect all root expression trees from the context
        foreach (var field in ctx.Fields)
        {
            if (field.DefaultExpression is not null)
                EnforceCIInExpression(field.DefaultExpression, ctx);
            if (field.ComputedExpression is not null)
                EnforceCIInExpression(field.ComputedExpression, ctx);
        }

        foreach (var row in ctx.TransitionRows)
        {
            if (row.Guard is not null)
                EnforceCIInExpression(row.Guard, ctx);
            if (row is TypedTransitionRowSuccess successRow)
                foreach (var action in successRow.Actions)
                    EnforceCIInAction(action, ctx);
        }

        foreach (var handler in ctx.EventHandlers)
        {
            if (handler.Guard is not null)
                EnforceCIInExpression(handler.Guard, ctx);
            if (handler is TypedEventRowSuccess successHandler)
                foreach (var action in successHandler.Actions)
                    EnforceCIInAction(action, ctx);
        }

        foreach (var rule in ctx.Rules)
        {
            EnforceCIInExpression(rule.Condition, ctx);
            if (rule.Guard is not null)
                EnforceCIInExpression(rule.Guard, ctx);
            EnforceCIInExpression(rule.Message, ctx);
        }

        foreach (var ensure in ctx.Ensures)
        {
            EnforceCIInExpression(ensure.Condition, ctx);
            if (ensure.Guard is not null)
                EnforceCIInExpression(ensure.Guard, ctx);
            EnforceCIInExpression(ensure.Message, ctx);
        }
    }

    /// <summary>Check a single action for CI violations.</summary>
    private static void EnforceCIInAction(TypedAction action, CheckContext ctx)
    {
        if (action is TypedInputAction ia)
        {
            EnforceCIInExpression(ia.InputExpression, ctx);
            if (ia.SecondaryExpression is not null)
                EnforceCIInExpression(ia.SecondaryExpression, ctx);
        }
    }

    /// <summary>
    /// Recursively walk a <see cref="TypedExpression"/> tree and emit CI enforcement
    /// diagnostics at each violation site.
    /// </summary>
    private static void EnforceCIInExpression(TypedExpression expr, CheckContext ctx)
    {
        switch (expr)
        {
            case TypedBinaryOp bin:
                if (Operations.GetMeta(bin.ResolvedOp) is BinaryOperationMeta
                        { HasCIVariant: true, CIDiagnosticCode: { } binaryDiagCode } binaryMeta)
                {
                    if (binaryMeta.Op == OperatorKind.Contains)
                    {
                        TryEmitContainsCIDiagnostic(bin, binaryDiagCode, ctx);
                    }
                    else if (IsCIExpression(bin.Left) || IsCIExpression(bin.Right))
                    {
                        var ciFieldName = GetCIFieldName(bin.Left, bin.Right);
                        ctx.Diagnostics.Add(Diagnostics.Create(binaryDiagCode, bin.Span, ciFieldName));
                    }
                }

                EnforceCIInExpression(bin.Left, ctx);
                EnforceCIInExpression(bin.Right, ctx);
                break;

            case TypedFunctionCall func:
                var funcMeta = Functions.GetMeta(func.ResolvedFunction);
                if (funcMeta is { HasCIVariant: true, CIDiagnosticCode: { } functionDiagCode } &&
                    func.Arguments.Length > 0 && IsCIExpression(func.Arguments[0]))
                {
                    var ciFieldName = ((TypedFieldRef)func.Arguments[0]).FieldName;
                    ctx.Diagnostics.Add(Diagnostics.Create(functionDiagCode, func.Span, ciFieldName));
                }

                foreach (var arg in func.Arguments)
                    EnforceCIInExpression(arg, ctx);
                break;

            case TypedUnaryOp un:
                EnforceCIInExpression(un.Operand, ctx);
                break;

            case TypedConditional cond:
                EnforceCIInExpression(cond.Condition, ctx);
                EnforceCIInExpression(cond.ThenBranch, ctx);
                EnforceCIInExpression(cond.ElseBranch, ctx);
                break;

            case TypedQuantifier quant:
                EnforceCIInExpression(quant.Collection, ctx);
                EnforceCIInExpression(quant.Predicate, ctx);
                break;

            case TypedMemberAccess mem:
                EnforceCIInExpression(mem.Object, ctx);
                break;

            case TypedInterpolatedString interp:
                foreach (var seg in interp.Segments)
                {
                    if (seg is TypedHoleSegment hole)
                        EnforceCIInExpression(hole.Expression, ctx);
                }
                break;

            case TypedListLiteral list:
                foreach (var elem in list.Elements)
                    EnforceCIInExpression(elem, ctx);
                break;

            // Leaf nodes: TypedFieldRef, TypedArgRef, TypedLiteral, TypedTypedConstant,
            // TypedPostfixOp, TypedErrorExpression — no sub-expressions to walk
        }
    }

    /// <summary>Returns true if the expression resolves to a <c>~string</c> field reference.</summary>
    private static bool IsCIExpression(TypedExpression expr) =>
        expr is TypedFieldRef { IsCaseInsensitive: true };

    /// <summary>Returns the field name from whichever operand is a <c>~string</c> field reference.</summary>
    private static string GetCIFieldName(TypedExpression left, TypedExpression right) =>
        IsCIExpression(left) ? ((TypedFieldRef)left).FieldName : ((TypedFieldRef)right).FieldName;

    private static void TryEmitContainsCIDiagnostic(TypedBinaryOp bin, DiagnosticCode diagnosticCode, CheckContext ctx)
    {
        if (bin.Left is not TypedFieldRef collectionField ||
            bin.Right is not TypedFieldRef valueField ||
            !valueField.IsCaseInsensitive ||
            ctx.CIElementCollections.Contains(collectionField.FieldName) ||
            !ctx.FieldLookup.TryGetValue(collectionField.FieldName, out var field))
        {
            return;
        }

        var collectionType = Types.GetMeta(field.ResolvedType).DisplayName;
        var suggestedType = field.ResolvedType switch
        {
            TypeKind.Set => "set of ~string",
            TypeKind.Queue => "queue of ~string",
            TypeKind.Stack => "stack of ~string",
            TypeKind.Log => "log of ~string",
            TypeKind.LogBy when field.KeyType is not null => $"log of ~string by {Types.GetMeta(field.KeyType.Value).DisplayName}",
            TypeKind.Bag => "bag of ~string",
            TypeKind.List => "list of ~string",
            TypeKind.QueueBy when field.KeyType is not null => $"queue of ~string by {Types.GetMeta(field.KeyType.Value).DisplayName}",
            _ => "collection of ~string",
        };

        ctx.Diagnostics.Add(Diagnostics.Create(
            diagnosticCode,
            bin.Span,
            valueField.FieldName,
            collectionField.FieldName,
            collectionType,
            suggestedType));
    }
}
