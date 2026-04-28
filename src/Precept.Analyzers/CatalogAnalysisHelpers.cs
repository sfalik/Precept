using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

[assembly: InternalsVisibleTo("Precept.Analyzers.Tests")]

namespace Precept.Analyzers;

/// <summary>
/// Shared helpers for walking Roslyn IOperation trees inside Precept catalog code.
/// Used by PRECEPT0005–0017 to navigate GetMeta switch expressions, object creations,
/// collection expressions, shared field references, and enum member cross-references.
/// </summary>
internal static class CatalogAnalysisHelpers
{
    // ── Namespace / scope guards ──────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="op"/>'s type lives in <c>{outerNs}.{innerNs}</c>.
    /// Example: <c>IsInNamespace(op, "Precept", "Language")</c>.
    /// </summary>
    internal static bool IsInNamespace(IOperation op, string outerNs, string innerNs)
    {
        var ns = op.Type?.ContainingNamespace;
        return ns?.Name == innerNs && ns.ContainingNamespace?.Name == outerNs;
    }

    /// <summary>
    /// Returns true if <paramref name="symbol"/> lives in <c>{outerNs}.{innerNs}</c>.
    /// </summary>
    internal static bool IsInNamespace(ISymbol symbol, string outerNs, string innerNs)
    {
        var ns = symbol.ContainingNamespace;
        return ns?.Name == innerNs && ns.ContainingNamespace?.Name == outerNs;
    }

    // ── Implicit conversion unwrapping ────────────────────────────────────────

    /// <summary>
    /// Strips implicit <see cref="IConversionOperation"/> wrappers that the compiler inserts
    /// around enum-to-underlying, derived-to-base, and similar coercions.
    /// </summary>
    internal static IOperation UnwrapConversions(IOperation op)
    {
        while (op is IConversionOperation conv && conv.IsImplicit)
            op = conv.Operand;
        return op;
    }

    // ── GetMeta switch detection ──────────────────────────────────────────────

    /// <summary>
    /// Known catalog enum type names that appear as <c>GetMeta</c> switch discriminants.
    /// </summary>
    private static readonly HashSet<string> CatalogEnumNames = new()
    {
        "TypeKind", "TokenKind", "OperatorKind", "OperationKind",
        "ModifierKind", "FunctionKind", "ActionKind", "ConstructKind",
        "DiagnosticCode", "FaultCode",
    };

    /// <summary>
    /// Returns true if <paramref name="switchOp"/> is a catalog <c>GetMeta</c> switch expression.
    /// Sets <paramref name="catalogEnumTypeName"/> to the enum type name (e.g. "TypeKind").
    /// </summary>
    internal static bool TryGetCatalogSwitchKind(
        ISwitchExpressionOperation switchOp,
        ISymbol containingSymbol,
        out string catalogEnumTypeName)
    {
        catalogEnumTypeName = "";

        // Must be inside a method named "GetMeta".
        if (containingSymbol is not IMethodSymbol method || method.Name != "GetMeta")
            return false;

        // Containing type must be in Precept.Language.
        if (!IsInNamespace(method, "Precept", "Language"))
            return false;

        // The switch input type must be a known catalog enum.
        var input = UnwrapConversions(switchOp.Value);
        var inputTypeName = input.Type?.Name;
        if (inputTypeName == null || !CatalogEnumNames.Contains(inputTypeName))
            return false;

        catalogEnumTypeName = inputTypeName;
        return true;
    }

    // ── Switch arm extraction ─────────────────────────────────────────────────

    /// <summary>
    /// Extracts the enum member name from a switch arm's constant pattern.
    /// Returns null for discard or non-constant patterns.
    /// </summary>
    internal static string? GetEnumCaseFromArm(ISwitchExpressionArmOperation arm)
    {
        if (arm.Pattern is IConstantPatternOperation { Value: IOperation value })
        {
            var unwrapped = UnwrapConversions(value);
            if (unwrapped is IFieldReferenceOperation fref)
                return fref.Field.Name;
        }
        return null;
    }

    // ── Enum field name resolution ────────────────────────────────────────────

    /// <summary>
    /// Resolves an operation to an enum field name. Returns null if the operation
    /// is not a field reference (after unwrapping conversions).
    /// </summary>
    internal static string? ResolveEnumFieldName(IOperation op)
    {
        var unwrapped = UnwrapConversions(op);
        if (unwrapped is IFieldReferenceOperation fref)
            return fref.Field.Name;
        return null;
    }

    // ── Constructor argument extraction ───────────────────────────────────────

    /// <summary>
    /// Returns the value of the constructor argument whose parameter is named
    /// <paramref name="paramName"/>. Works for both positional and named argument syntax.
    /// Returns null if no explicit argument with that name is found.
    /// </summary>
    internal static IOperation? GetNamedArgument(
        IObjectCreationOperation? creation,
        string paramName)
    {
        if (creation == null) return null;

        foreach (var arg in creation.Arguments)
        {
            if (arg.Parameter?.Name == paramName && !arg.IsImplicit)
                return arg.Value;
        }
        return null;
    }

    /// <summary>
    /// Returns all explicit (non-implicit) constructor arguments keyed by parameter name.
    /// </summary>
    internal static IReadOnlyDictionary<string, IOperation> GetExplicitArguments(
        IObjectCreationOperation creation)
    {
        var result = new Dictionary<string, IOperation>();
        foreach (var arg in creation.Arguments)
        {
            if (!arg.IsImplicit && arg.Parameter?.Name is { } name)
                result[name] = arg.Value;
        }
        return result;
    }

    // ── Object initializer assignment extraction ──────────────────────────────

    /// <summary>
    /// Returns the value assigned to <paramref name="memberName"/> in an object initializer.
    /// Handles <c>new Foo { Bar = [...] }</c> syntax where <c>GetNamedArgument</c> returns null.
    /// </summary>
    internal static IOperation? GetInitializerAssignment(
        IObjectCreationOperation creation,
        string memberName)
    {
        if (creation.Initializer == null) return null;

        foreach (var init in creation.Initializer.Initializers)
        {
            if (init is ISimpleAssignmentOperation assignment &&
                assignment.Target is IPropertyReferenceOperation propRef &&
                propRef.Property.Name == memberName)
            {
                return assignment.Value;
            }
        }
        return null;
    }

    // ── Collection / array element enumeration ─────────────────────────────────

    /// <summary>
    /// Yields the non-spread elements from a collection expression or array initializer.
    /// Spread elements are silently skipped — guard with <see cref="CollectionHasSpread"/>.
    /// </summary>
    internal static IEnumerable<IOperation> EnumerateCollectionElements(IOperation op)
    {
        var unwrapped = UnwrapConversions(op);

        if (unwrapped is ICollectionExpressionOperation colExpr)
        {
            foreach (var element in colExpr.Elements)
            {
                if (element is not ISpreadOperation)
                    yield return UnwrapConversions(element);
            }
            yield break;
        }

        if (unwrapped is IArrayCreationOperation arrayCreate && arrayCreate.Initializer != null)
        {
            foreach (var element in arrayCreate.Initializer.ElementValues)
                yield return UnwrapConversions(element);
            yield break;
        }
    }

    /// <summary>
    /// Returns true if <paramref name="op"/> is a collection expression containing
    /// at least one spread element (<c>[..something]</c>).
    /// </summary>
    internal static bool CollectionHasSpread(IOperation op)
    {
        var unwrapped = UnwrapConversions(op);
        if (unwrapped is ICollectionExpressionOperation colExpr)
        {
            foreach (var element in colExpr.Elements)
            {
                if (element is ISpreadOperation)
                    return true;
            }
        }
        return false;
    }

    // ── Flags enum checking ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="op"/> is (or contains via bitwise OR) the enum member
    /// named <paramref name="flagMemberName"/> in <paramref name="flagsEnumType"/>.
    /// </summary>
    internal static bool FlagsEnumContains(
        IOperation? op,
        string flagMemberName,
        INamedTypeSymbol flagsEnumType)
    {
        if (op == null) return false;

        var unwrapped = UnwrapConversions(op);

        // Strategy 1: single field reference — exact name match.
        if (unwrapped is IFieldReferenceOperation fref &&
            SymbolEqualityComparer.Default.Equals(fref.Field.ContainingType, flagsEnumType))
        {
            return fref.Field.Name == flagMemberName;
        }

        // Strategy 2: bitwise OR tree — recurse into left and right.
        if (unwrapped is IBinaryOperation binary &&
            binary.OperatorKind == BinaryOperatorKind.Or)
        {
            return FlagsEnumContains(binary.LeftOperand, flagMemberName, flagsEnumType) ||
                   FlagsEnumContains(binary.RightOperand, flagMemberName, flagsEnumType);
        }

        // Strategy 3: constant-folded value — compare underlying integers.
        if (unwrapped.ConstantValue.HasValue)
        {
            var targetField = flagsEnumType.GetMembers(flagMemberName)
                .OfType<IFieldSymbol>()
                .FirstOrDefault();
            if (targetField?.ConstantValue != null && unwrapped.ConstantValue.Value != null)
            {
                var targetVal = System.Convert.ToInt64(targetField.ConstantValue);
                var actualVal = System.Convert.ToInt64(unwrapped.ConstantValue.Value);
                return (actualVal & targetVal) == targetVal && targetVal != 0;
            }
        }

        return false;
    }

    // ── Field initializer follower ────────────────────────────────────────────

    /// <summary>
    /// Follows a field reference to its initializer expression and returns the
    /// <see cref="IObjectCreationOperation"/> if it creates an object of type
    /// <paramref name="expectedTypeName"/>. Used for resolving shared statics like
    /// <c>PInteger</c>, <c>PMoney</c> in Operations.cs.
    /// </summary>
    internal static IObjectCreationOperation? FollowFieldInitializer(
        IFieldReferenceOperation fieldRef,
        string expectedTypeName,
        Compilation compilation)
    {
        var field = fieldRef.Field;
        var syntaxRefs = field.DeclaringSyntaxReferences;
        if (syntaxRefs.Length == 0) return null;

        var syntaxRef = syntaxRefs[0];
        var syntaxNode = syntaxRef.GetSyntax();
        var tree = syntaxNode.SyntaxTree;
        var model = compilation.GetSemanticModel(tree);

        // The declaring syntax may be a VariableDeclaratorSyntax whose initializer
        // is the EqualsValueClauseSyntax child. Try the node itself first, then walk
        // descendant nodes looking for any that yield an IOperation.
        var operation = model.GetOperation(syntaxNode);
        if (operation != null)
            return FindCreation(operation, expectedTypeName);

        // Walk descendants — covers VariableDeclarator → EqualsValueClause → expression.
        foreach (var descendant in syntaxNode.DescendantNodes())
        {
            var childOp = model.GetOperation(descendant);
            if (childOp != null)
            {
                var found = FindCreation(childOp, expectedTypeName);
                if (found != null) return found;
            }
        }

        return null;
    }

    private static IObjectCreationOperation? FindCreation(IOperation? op, string expectedTypeName)
    {
        if (op == null) return null;

        if (op is IObjectCreationOperation creation && creation.Type?.Name == expectedTypeName)
            return creation;

        // Walk into variable declarator → initializer, field initializer → value, etc.
        foreach (var child in op.ChildOperations)
        {
            var found = FindCreation(child, expectedTypeName);
            if (found != null) return found;
        }

        return null;
    }

    // ── String constant resolution ────────────────────────────────────────────

    /// <summary>
    /// Resolves an operation to its string constant value. Handles string literals,
    /// <c>nameof()</c> expressions (compiled to constants), and const field references.
    /// </summary>
    internal static string? ResolveStringConstant(IOperation op)
    {
        var unwrapped = UnwrapConversions(op);
        return unwrapped.ConstantValue.HasValue ? unwrapped.ConstantValue.Value as string : null;
    }
}
