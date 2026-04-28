using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace Precept.Analyzers.Tests;

public class CatalogAnalysisHelpersTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Compilation helper — builds a Compilation + SemanticModel from source
    // ═══════════════════════════════════════════════════════════════════════════

    private static (Compilation compilation, SemanticModel model, SyntaxTree tree) Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return Compile(tree);
    }

    private static (Compilation compilation, SemanticModel model, SyntaxTree tree) Compile(params SyntaxTree[] trees)
    {
        var dotnetDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };
        var systemRuntime = System.IO.Path.Combine(dotnetDir, "System.Runtime.dll");
        if (System.IO.File.Exists(systemRuntime))
            refs.Add(MetadataReference.CreateFromFile(systemRuntime));

        var compilation = CSharpCompilation.Create("TestAssembly", trees, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (compilation, compilation.GetSemanticModel(trees[0]), trees[0]);
    }

    private static IOperation GetRootOperation(SemanticModel model, SyntaxTree tree)
    {
        var root = tree.GetRoot();
        // Find the first method body or expression-bodied member and get its operation.
        foreach (var node in root.DescendantNodes())
        {
            var op = model.GetOperation(node);
            if (op != null) return op;
        }
        throw new InvalidOperationException("No IOperation found in source.");
    }

    private static T FindOperation<T>(SemanticModel model, SyntaxTree tree) where T : class, IOperation
    {
        var root = tree.GetRoot();
        foreach (var node in root.DescendantNodes())
        {
            if (model.GetOperation(node) is T found)
                return found;
        }
        throw new InvalidOperationException($"No {typeof(T).Name} found in source.");
    }

    private static IEnumerable<T> FindAllOperations<T>(SemanticModel model, SyntaxTree tree) where T : class, IOperation
    {
        var root = tree.GetRoot();
        foreach (var node in root.DescendantNodes())
        {
            if (model.GetOperation(node) is T found)
                yield return found;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  IsInNamespace(IOperation, ...)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsInNamespace_Operation_matches_two_level_namespace()
    {
        var source = @"
namespace Precept.Language
{
    public record Foo(int X);
    public static class Test
    {
        public static Foo M() => new Foo(1);
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        CatalogAnalysisHelpers.IsInNamespace(creation, "Precept", "Language").Should().BeTrue();
    }

    [Fact]
    public void IsInNamespace_Operation_rejects_wrong_namespace()
    {
        var source = @"
namespace Other.Stuff
{
    public record Foo(int X);
    public static class Test
    {
        public static Foo M() => new Foo(1);
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        CatalogAnalysisHelpers.IsInNamespace(creation, "Precept", "Language").Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  IsInNamespace(ISymbol, ...)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsInNamespace_Symbol_matches_two_level_namespace()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer }
}";
        var (compilation, _, _) = Compile(source);
        var symbol = compilation.GetTypeByMetadataName("Precept.Language.TypeKind")!;
        CatalogAnalysisHelpers.IsInNamespace(symbol, "Precept", "Language").Should().BeTrue();
    }

    [Fact]
    public void IsInNamespace_Symbol_rejects_wrong_namespace()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer }
}";
        var (compilation, _, _) = Compile(source);
        var symbol = compilation.GetTypeByMetadataName("Precept.Language.TypeKind")!;
        CatalogAnalysisHelpers.IsInNamespace(symbol, "Other", "Language").Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UnwrapConversions
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UnwrapConversions_strips_implicit_conversion()
    {
        // Passing an enum value to an object parameter produces an implicit boxing conversion.
        var source = @"
namespace Test
{
    public enum Color { Red }
    public static class C
    {
        public static void Accept(object x) { }
        public static void M() => Accept(Color.Red);
    }
}";
        var (_, model, tree) = Compile(source);
        // The argument Color.Red → object is wrapped in an implicit conversion.
        var invocation = FindOperation<IInvocationOperation>(model, tree);
        var argValue = invocation.Arguments[0].Value;
        argValue.Should().BeAssignableTo<IConversionOperation>();
        var result = CatalogAnalysisHelpers.UnwrapConversions(argValue);
        // After unwrapping, we should get to the field reference (Color.Red).
        result.Should().BeAssignableTo<IFieldReferenceOperation>();
    }

    [Fact]
    public void UnwrapConversions_returns_same_if_no_conversion()
    {
        var source = @"
public class C
{
    public static int M() => 42;
}";
        var (_, model, tree) = Compile(source);
        var literal = FindOperation<ILiteralOperation>(model, tree);
        CatalogAnalysisHelpers.UnwrapConversions(literal).Should().BeSameAs(literal);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  TryGetCatalogSwitchKind
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TryGetCatalogSwitchKind_identifies_catalog_switch()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer, Text }
    public record TypeMeta(string Name);
    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new TypeMeta(""int""),
            TypeKind.Text => new TypeMeta(""text""),
            _ => throw new System.ArgumentOutOfRangeException()
        };
    }
}";
        var (compilation, model, tree) = Compile(source);
        var switchOp = FindOperation<ISwitchExpressionOperation>(model, tree);
        var method = compilation.GetTypeByMetadataName("Precept.Language.Types")!
            .GetMembers("GetMeta").OfType<IMethodSymbol>().First();

        CatalogAnalysisHelpers.TryGetCatalogSwitchKind(switchOp, method, out var name)
            .Should().BeTrue();
        name.Should().Be("TypeKind");
    }

    [Fact]
    public void TryGetCatalogSwitchKind_rejects_non_GetMeta_method()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer }
    public record TypeMeta(string Name);
    public static class Types
    {
        public static TypeMeta Other(TypeKind kind) => kind switch
        {
            TypeKind.Integer => new TypeMeta(""int""),
            _ => throw new System.ArgumentOutOfRangeException()
        };
    }
}";
        var (compilation, model, tree) = Compile(source);
        var switchOp = FindOperation<ISwitchExpressionOperation>(model, tree);
        var method = compilation.GetTypeByMetadataName("Precept.Language.Types")!
            .GetMembers("Other").OfType<IMethodSymbol>().First();

        CatalogAnalysisHelpers.TryGetCatalogSwitchKind(switchOp, method, out _)
            .Should().BeFalse();
    }

    [Fact]
    public void TryGetCatalogSwitchKind_rejects_non_catalog_enum()
    {
        var source = @"
namespace Precept.Language
{
    public enum WeirdKind { Foo }
    public record TypeMeta(string Name);
    public static class Types
    {
        public static TypeMeta GetMeta(WeirdKind kind) => kind switch
        {
            WeirdKind.Foo => new TypeMeta(""foo""),
            _ => throw new System.ArgumentOutOfRangeException()
        };
    }
}";
        var (compilation, model, tree) = Compile(source);
        var switchOp = FindOperation<ISwitchExpressionOperation>(model, tree);
        var method = compilation.GetTypeByMetadataName("Precept.Language.Types")!
            .GetMembers("GetMeta").OfType<IMethodSymbol>().First();

        CatalogAnalysisHelpers.TryGetCatalogSwitchKind(switchOp, method, out _)
            .Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetEnumCaseFromArm
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetEnumCaseFromArm_extracts_enum_member_name()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer, Text }
    public static class Types
    {
        public static string GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => ""int"",
            TypeKind.Text => ""text"",
            _ => throw new System.ArgumentOutOfRangeException()
        };
    }
}";
        var (_, model, tree) = Compile(source);
        var switchOp = FindOperation<ISwitchExpressionOperation>(model, tree);
        var arm = switchOp.Arms[0];
        CatalogAnalysisHelpers.GetEnumCaseFromArm(arm).Should().Be("Integer");
    }

    [Fact]
    public void GetEnumCaseFromArm_returns_null_for_discard()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer }
    public static class Types
    {
        public static string GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.Integer => ""int"",
            _ => throw new System.ArgumentOutOfRangeException()
        };
    }
}";
        var (_, model, tree) = Compile(source);
        var switchOp = FindOperation<ISwitchExpressionOperation>(model, tree);
        // Last arm is the discard.
        var discardArm = switchOp.Arms[^1];
        CatalogAnalysisHelpers.GetEnumCaseFromArm(discardArm).Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ResolveEnumFieldName
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ResolveEnumFieldName_resolves_field_reference()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { Integer }
    public static class Test
    {
        public static TypeKind M() => TypeKind.Integer;
    }
}";
        var (_, model, tree) = Compile(source);
        var fieldRef = FindOperation<IFieldReferenceOperation>(model, tree);
        CatalogAnalysisHelpers.ResolveEnumFieldName(fieldRef).Should().Be("Integer");
    }

    [Fact]
    public void ResolveEnumFieldName_returns_null_for_literal()
    {
        var source = @"
public static class Test
{
    public static int M() => 42;
}";
        var (_, model, tree) = Compile(source);
        var literal = FindOperation<ILiteralOperation>(model, tree);
        CatalogAnalysisHelpers.ResolveEnumFieldName(literal).Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetNamedArgument
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetNamedArgument_finds_positional_argument()
    {
        var source = @"
namespace Precept.Language
{
    public record Foo(string Name, int Value);
    public static class Test
    {
        public static Foo M() => new Foo(""hello"", 42);
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        var arg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Name");
        arg.Should().NotBeNull();
        arg!.ConstantValue.Value.Should().Be("hello");
    }

    [Fact]
    public void GetNamedArgument_returns_null_for_missing_param()
    {
        var source = @"
namespace Precept.Language
{
    public record Foo(string Name);
    public static class Test
    {
        public static Foo M() => new Foo(""hello"");
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        CatalogAnalysisHelpers.GetNamedArgument(creation, "Missing").Should().BeNull();
    }

    [Fact]
    public void GetNamedArgument_returns_null_for_null_creation()
    {
        CatalogAnalysisHelpers.GetNamedArgument(null, "Name").Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetExplicitArguments
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetExplicitArguments_returns_all_explicit_args()
    {
        var source = @"
namespace Precept.Language
{
    public record Foo(string Name, int Value);
    public static class Test
    {
        public static Foo M() => new Foo(""hello"", 42);
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        var args = CatalogAnalysisHelpers.GetExplicitArguments(creation);
        args.Should().ContainKey("Name");
        args.Should().ContainKey("Value");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GetInitializerAssignment
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetInitializerAssignment_finds_property_in_initializer()
    {
        var source = @"
namespace Precept.Language
{
    public class Foo
    {
        public string Name { get; init; } = """";
        public int[] Items { get; init; } = System.Array.Empty<int>();
    }
    public static class Test
    {
        public static Foo M() => new Foo { Name = ""hello"", Items = new[] { 1, 2 } };
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        var value = CatalogAnalysisHelpers.GetInitializerAssignment(creation, "Name");
        value.Should().NotBeNull();
        value!.ConstantValue.Value.Should().Be("hello");
    }

    [Fact]
    public void GetInitializerAssignment_returns_null_for_missing_property()
    {
        var source = @"
namespace Precept.Language
{
    public class Foo
    {
        public string Name { get; init; } = """";
    }
    public static class Test
    {
        public static Foo M() => new Foo { Name = ""hello"" };
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        CatalogAnalysisHelpers.GetInitializerAssignment(creation, "Missing").Should().BeNull();
    }

    [Fact]
    public void GetInitializerAssignment_returns_null_when_no_initializer()
    {
        var source = @"
namespace Precept.Language
{
    public record Foo(string Name);
    public static class Test
    {
        public static Foo M() => new Foo(""hello"");
    }
}";
        var (_, model, tree) = Compile(source);
        var creation = FindOperation<IObjectCreationOperation>(model, tree);
        CatalogAnalysisHelpers.GetInitializerAssignment(creation, "Name").Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  EnumerateCollectionElements
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EnumerateCollectionElements_yields_array_initializer_elements()
    {
        var source = @"
public static class Test
{
    public static int[] M() => new int[] { 1, 2, 3 };
}";
        var (_, model, tree) = Compile(source);
        var arrayCreation = FindOperation<IArrayCreationOperation>(model, tree);
        var elements = CatalogAnalysisHelpers.EnumerateCollectionElements(arrayCreation).ToList();
        elements.Should().HaveCount(3);
    }

    [Fact]
    public void EnumerateCollectionElements_returns_empty_for_non_collection()
    {
        var source = @"
public static class Test
{
    public static int M() => 42;
}";
        var (_, model, tree) = Compile(source);
        var literal = FindOperation<ILiteralOperation>(model, tree);
        CatalogAnalysisHelpers.EnumerateCollectionElements(literal).Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CollectionHasSpread — tested via absence (no spread in array init)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CollectionHasSpread_returns_false_for_array_init()
    {
        var source = @"
public static class Test
{
    public static int[] M() => new int[] { 1, 2, 3 };
}";
        var (_, model, tree) = Compile(source);
        var arrayCreation = FindOperation<IArrayCreationOperation>(model, tree);
        CatalogAnalysisHelpers.CollectionHasSpread(arrayCreation).Should().BeFalse();
    }

    [Fact]
    public void CollectionHasSpread_returns_false_for_literal()
    {
        var source = @"
public static class Test
{
    public static int M() => 42;
}";
        var (_, model, tree) = Compile(source);
        var literal = FindOperation<ILiteralOperation>(model, tree);
        CatalogAnalysisHelpers.CollectionHasSpread(literal).Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FlagsEnumContains
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FlagsEnumContains_matches_single_flag()
    {
        var source = @"
namespace Precept.Language
{
    [System.Flags]
    public enum TypeTrait
    {
        None = 0,
        Orderable = 1,
        EqualityComparable = 2,
    }
    public static class Test
    {
        public static TypeTrait M() => TypeTrait.Orderable;
    }
}";
        var (compilation, model, tree) = Compile(source);
        var fieldRef = FindOperation<IFieldReferenceOperation>(model, tree);
        var enumType = compilation.GetTypeByMetadataName("Precept.Language.TypeTrait")!;
        CatalogAnalysisHelpers.FlagsEnumContains(fieldRef, "Orderable", enumType).Should().BeTrue();
        CatalogAnalysisHelpers.FlagsEnumContains(fieldRef, "EqualityComparable", enumType).Should().BeFalse();
    }

    [Fact]
    public void FlagsEnumContains_matches_in_bitwise_or()
    {
        var source = @"
namespace Precept.Language
{
    [System.Flags]
    public enum TypeTrait
    {
        None = 0,
        Orderable = 1,
        EqualityComparable = 2,
    }
    public static class Test
    {
        public static TypeTrait M() => TypeTrait.Orderable | TypeTrait.EqualityComparable;
    }
}";
        var (compilation, model, tree) = Compile(source);
        // Find the binary OR operation.
        var binaryOp = FindOperation<IBinaryOperation>(model, tree);
        var enumType = compilation.GetTypeByMetadataName("Precept.Language.TypeTrait")!;
        CatalogAnalysisHelpers.FlagsEnumContains(binaryOp, "Orderable", enumType).Should().BeTrue();
        CatalogAnalysisHelpers.FlagsEnumContains(binaryOp, "EqualityComparable", enumType).Should().BeTrue();
    }

    [Fact]
    public void FlagsEnumContains_returns_false_for_null()
    {
        var source = @"
namespace Precept.Language
{
    [System.Flags]
    public enum TypeTrait { None = 0, Orderable = 1 }
}";
        var (compilation, _, _) = Compile(source);
        var enumType = compilation.GetTypeByMetadataName("Precept.Language.TypeTrait")!;
        CatalogAnalysisHelpers.FlagsEnumContains(null, "Orderable", enumType).Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FollowFieldInitializer
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FollowFieldInitializer_resolves_static_field_to_creation()
    {
        var source = @"
namespace Precept.Language
{
    public record ParameterMeta(int Kind, string? Name = null);
    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(0, ""lhs"");
        public static ParameterMeta Get() => PInteger;
    }
}";
        var (compilation, model, tree) = Compile(source);
        var fieldRef = FindAllOperations<IFieldReferenceOperation>(model, tree)
            .First(f => f.Field.Name == "PInteger");

        var result = CatalogAnalysisHelpers.FollowFieldInitializer(fieldRef, "ParameterMeta", compilation);
        result.Should().NotBeNull();
        result!.Type!.Name.Should().Be("ParameterMeta");
    }

    [Fact]
    public void FollowFieldInitializer_returns_null_for_wrong_type()
    {
        var source = @"
namespace Precept.Language
{
    public record ParameterMeta(int Kind, string? Name = null);
    public static class Operations
    {
        private static readonly ParameterMeta PInteger = new(0);
        public static ParameterMeta Get() => PInteger;
    }
}";
        var (compilation, model, tree) = Compile(source);
        var fieldRef = FindAllOperations<IFieldReferenceOperation>(model, tree)
            .First(f => f.Field.Name == "PInteger");

        CatalogAnalysisHelpers.FollowFieldInitializer(fieldRef, "OtherType", compilation)
            .Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ResolveStringConstant
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ResolveStringConstant_resolves_string_literal()
    {
        var source = @"
public static class Test
{
    public static string M() => ""hello"";
}";
        var (_, model, tree) = Compile(source);
        var literal = FindOperation<ILiteralOperation>(model, tree);
        CatalogAnalysisHelpers.ResolveStringConstant(literal).Should().Be("hello");
    }

    [Fact]
    public void ResolveStringConstant_resolves_nameof()
    {
        var source = @"
namespace Precept.Language
{
    public enum DiagnosticCode { InputTooLarge }
    public static class Test
    {
        public static string M() => nameof(DiagnosticCode.InputTooLarge);
    }
}";
        var (_, model, tree) = Compile(source);
        // nameof compiles to a constant string at IOperation level.
        var nameOfOp = FindOperation<INameOfOperation>(model, tree);
        CatalogAnalysisHelpers.ResolveStringConstant(nameOfOp).Should().Be("InputTooLarge");
    }

    [Fact]
    public void ResolveStringConstant_returns_null_for_non_constant()
    {
        var source = @"
public static class Test
{
    public static string M(string x) => x;
}";
        var (_, model, tree) = Compile(source);
        var paramRef = FindOperation<IParameterReferenceOperation>(model, tree);
        CatalogAnalysisHelpers.ResolveStringConstant(paramRef).Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Multi-source AnalyzerTestHelper overload
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MultiSource_overload_compiles_multiple_trees()
    {
        // Verify the multi-source overload works by running a simple analyzer
        // across two source files that reference each other.
        var source1 = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero }
}";
        var source2 = @"
namespace Precept.Language
{
    public static class Faults
    {
        public static string GetCode() => nameof(FaultCode.DivisionByZero);
    }
}";
        // Just verify it compiles without error — the multi-source overload
        // should handle cross-tree references correctly.
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0001FailMustUseFaultCode>(source1, source2);
        // No Fail() calls, so no PRECEPT0001 diagnostics expected.
        diagnostics.Should().BeEmpty();
    }
}
