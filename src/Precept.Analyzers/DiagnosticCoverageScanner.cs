using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// Shared semantic scanner for diagnostic coverage gates.
/// Computes three sets from a compilation:
/// - All DiagnosticCode enum members (the catalog)
/// - Emitted codes (real emission contexts)
/// - Test-referenced codes (DiagnosticCode.X in test sources)
///
/// Emission detection covers:
/// 1. Diagnostics.Create(DiagnosticCode.X, ...) — direct emission
/// 2. CIDiagnosticCode: DiagnosticCode.X — catalog-mediated emission (Operations/Functions)
/// 3. ProofEngine dispatch branches referencing DiagnosticCode.X in Diagnostics.Create calls
///
/// Excludes:
/// - Diagnostics.GetMeta(DiagnosticCode.X) — catalog reads
/// - Enum definition references
/// - Comment/doc references (stripped via trivia check)
/// </summary>
internal static class DiagnosticCoverageScanner
{
    /// <summary>
    /// Result of scanning a compilation for diagnostic coverage.
    /// </summary>
    internal sealed class ScanResult
    {
        public HashSet<string> AllCodes { get; set; } = new HashSet<string>();
        public HashSet<string> EmittedCodes { get; set; } = new HashSet<string>();
        public HashSet<string> TestReferencedCodes { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// Scans the compilation for DiagnosticCode coverage information.
    /// </summary>
    internal static ScanResult Scan(Compilation compilation)
    {
        var allCodes = new HashSet<string>();
        var emittedCodes = new HashSet<string>();
        var testReferencedCodes = new HashSet<string>();

        // Find the DiagnosticCode enum type.
        var diagnosticCodeType = FindDiagnosticCodeEnum(compilation);
        if (diagnosticCodeType == null)
            return new ScanResult { AllCodes = allCodes, EmittedCodes = emittedCodes, TestReferencedCodes = testReferencedCodes };

        // Collect all enum members.
        foreach (var member in diagnosticCodeType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.HasConstantValue)
                allCodes.Add(member.Name);
        }

        // Walk all syntax trees in the compilation to find DiagnosticCode references.
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var isTestFile = IsTestFilePath(tree.FilePath);

            foreach (var node in root.DescendantNodes())
            {
                var operation = model.GetOperation(node);
                if (operation == null) continue;

                // Look for field references to DiagnosticCode members.
                if (operation is IFieldReferenceOperation fieldRef &&
                    SymbolEqualityComparer.Default.Equals(fieldRef.Field.ContainingType, diagnosticCodeType) &&
                    fieldRef.Field.HasConstantValue)
                {
                    var memberName = fieldRef.Field.Name;

                    if (isTestFile)
                    {
                        testReferencedCodes.Add(memberName);
                        continue;
                    }

                    // Determine if this is an emission context.
                    if (IsEmissionContext(fieldRef, model))
                    {
                        emittedCodes.Add(memberName);
                    }
                }
            }
        }

        return new ScanResult { AllCodes = allCodes, EmittedCodes = emittedCodes, TestReferencedCodes = testReferencedCodes };
    }

    /// <summary>
    /// Finds the DiagnosticCode enum type in the compilation.
    /// Searches in Precept.Language namespace first, falls back to any matching type.
    /// </summary>
    private static INamedTypeSymbol? FindDiagnosticCodeEnum(Compilation compilation)
    {
        // Try Precept.Language.DiagnosticCode first.
        var type = compilation.GetTypeByMetadataName("Precept.Language.DiagnosticCode");
        if (type != null && type.TypeKind == TypeKind.Enum)
            return type;

        // Fall back to searching all types (for test scenarios with simplified namespaces).
        return FindEnumByName(compilation.GlobalNamespace, "DiagnosticCode");
    }

    private static INamedTypeSymbol? FindEnumByName(INamespaceSymbol ns, string name)
    {
        foreach (var type in ns.GetTypeMembers(name))
        {
            if (type.TypeKind == TypeKind.Enum)
                return type;
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            var found = FindEnumByName(childNs, name);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Determines if a DiagnosticCode field reference is in an emission context.
    /// </summary>
    private static bool IsEmissionContext(IFieldReferenceOperation fieldRef, SemanticModel model)
    {
        // Walk up the operation tree to find the containing context.
        var current = fieldRef.Parent;
        while (current != null)
        {
            // Pattern 1: Diagnostics.Create(DiagnosticCode.X, ...) — first argument.
            if (current is IInvocationOperation invocation)
            {
                if (IsDiagnosticsCreateCall(invocation) &&
                    IsFirstArgument(invocation, fieldRef))
                {
                    return true;
                }

                // Not an emission call — check if this is a GetMeta call (catalog read).
                if (IsGetMetaCall(invocation))
                    return false;
            }

            // Pattern 2: CIDiagnosticCode: DiagnosticCode.X or CIDiagnosticCode = DiagnosticCode.X
            if (current is ISimpleAssignmentOperation assignment)
            {
                if (assignment.Target is IPropertyReferenceOperation propRef &&
                    propRef.Property.Name == "CIDiagnosticCode")
                {
                    return true;
                }
            }

            // Pattern 2b: Named argument CIDiagnosticCode: in constructor.
            if (current is IArgumentOperation argument &&
                argument.Parameter?.Name == "CIDiagnosticCode")
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if the invocation is a call to a method named "Create" on a type named "Diagnostics".
    /// </summary>
    private static bool IsDiagnosticsCreateCall(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        return method.Name == "Create" &&
               method.ContainingType?.Name == "Diagnostics";
    }

    /// <summary>
    /// Checks if the invocation is a call to a method named "GetMeta".
    /// </summary>
    private static bool IsGetMetaCall(IInvocationOperation invocation)
    {
        return invocation.TargetMethod.Name == "GetMeta";
    }

    /// <summary>
    /// Checks if the field reference is the first positional argument in the invocation.
    /// </summary>
    private static bool IsFirstArgument(IInvocationOperation invocation, IFieldReferenceOperation fieldRef)
    {
        if (invocation.Arguments.Length == 0) return false;

        var firstArg = invocation.Arguments[0];
        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(firstArg.Value);
        return unwrapped == fieldRef ||
               (unwrapped is IFieldReferenceOperation fref &&
                fref.Field.Name == fieldRef.Field.Name &&
                SymbolEqualityComparer.Default.Equals(fref.Field.ContainingType, fieldRef.Field.ContainingType));
    }

    /// <summary>
    /// Checks if a file path indicates a test file.
    /// </summary>
    private static bool IsTestFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        return filePath!.IndexOf(".Tests", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               filePath.IndexOf("Tests\\", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               filePath.IndexOf("Tests/", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
