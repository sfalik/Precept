using System;
using System.Linq;
using System.Reflection;

var asm = Assembly.LoadFrom(@"C:\Users\Shane.Falik\.nuget\packages\superpower\3.1.0\lib\net8.0\Superpower.dll");

void DumpType(string name, BindingFlags flags) {
    var t = asm.GetType(name);
    if (t == null) { Console.WriteLine($"  [Type not found: {name}]"); return; }
    foreach (var m in t.GetMethods(flags)) {
        var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({parms})");
    }
}

Console.WriteLine("=== Tokenizer<T> public ===");
DumpType("Superpower.Tokenizer1", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

Console.WriteLine("\n=== Tokenizer<T> protected ===");
var tokType = asm.GetType("Superpower.Tokenizer1");
foreach (var m in tokType!.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.IsFamily)) {
    var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Console.WriteLine($"  {(m.IsAbstract ? "abstract " : m.IsVirtual ? "virtual " : "")}{m.ReturnType.Name} {m.Name}({parms})");
}

Console.WriteLine("\n=== TokenizerBuilder<T> ===");
DumpType("Superpower.Tokenizers.TokenizerBuilder1", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

Console.WriteLine("\n=== Token (static) ===");
DumpType("Superpower.Parsers.Token", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

Console.WriteLine("\n=== Parse (static) ===");
DumpType("Superpower.Parse", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

Console.WriteLine("\n=== TokenAttribute ===");
var ta = asm.GetType("Superpower.Display.TokenAttribute");
foreach (var p in ta!.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    Console.WriteLine($"  {p.PropertyType.Name} {p.Name}");

Console.WriteLine("\n=== Combinators (token-list only, first 20) ===");
var comb = asm.GetType("Superpower.Combinators");
foreach (var m in comb!.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
    .Where(m => m.GetParameters().Any(p => p.ParameterType.Name.StartsWith("TokenListParser")))
    .Take(20)) {
    var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({parms})");
}
